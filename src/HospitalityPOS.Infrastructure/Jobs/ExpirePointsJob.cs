using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job for processing expired loyalty points.
/// </summary>
public class ExpirePointsJob : BackgroundService, IExpirePointsJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirePointsJob> _logger;

    /// <summary>
    /// Time between job runs (default: 24 hours).
    /// </summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Hour of day to run the job (0-23, default: 3 AM).
    /// </summary>
    public int RunHour { get; set; } = 3;

    public ExpirePointsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirePointsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpirePointsJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait until the configured run hour
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(RunHour);
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogDebug("Next expiry job run scheduled for {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Run the jobs
                await ProcessExpiredPointsAsync(stoppingToken);
                await SendExpiryWarningsAsync(30, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpirePointsJob execution");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("ExpirePointsJob stopped");
    }

    public async Task<int> ProcessExpiredPointsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        // Get points configuration
        var config = await context.PointsConfigurations
            .Where(c => c.IsDefault && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null || config.PointsExpiryDays <= 0)
        {
            _logger.LogDebug("Points expiry is disabled or no configuration found");
            return 0;
        }

        var expiryDate = DateTime.UtcNow.AddDays(-config.PointsExpiryDays);

        // Find members with points who haven't had activity since expiry date
        var expiredMembers = await context.LoyaltyMembers
            .Where(m => m.IsActive &&
                        m.PointsBalance > 0 &&
                        m.LastVisit.HasValue &&
                        m.LastVisit.Value < expiryDate)
            .ToListAsync(cancellationToken);

        var processedCount = 0;

        foreach (var member in expiredMembers)
        {
            try
            {
                var expiredPoints = member.PointsBalance;

                // Create expiry transaction
                var transaction = new LoyaltyTransaction
                {
                    LoyaltyMemberId = member.Id,
                    TransactionType = LoyaltyTransactionType.Expired,
                    Points = -expiredPoints,
                    BalanceAfter = 0,
                    Description = $"Points expired due to {config.PointsExpiryDays} days of inactivity",
                    TransactionDate = DateTime.UtcNow
                };

                context.LoyaltyTransactions.Add(transaction);

                // Update member balance
                member.PointsBalance = 0;
                member.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Expired {Points} points for member {MemberId} ({Phone})",
                    expiredPoints, member.Id, member.PhoneNumber);

                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error expiring points for member {MemberId}",
                    member.Id);
            }
        }

        _logger.LogInformation(
            "Processed points expiry for {Count} members",
            processedCount);

        return processedCount;
    }

    public async Task<int> SendExpiryWarningsAsync(int daysBeforeExpiry = 30, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        // Get points configuration
        var config = await context.PointsConfigurations
            .Where(c => c.IsDefault && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null || config.PointsExpiryDays <= 0)
        {
            _logger.LogDebug("Points expiry is disabled or no configuration found");
            return 0;
        }

        // Calculate the warning window
        var expiryDate = DateTime.UtcNow.AddDays(-config.PointsExpiryDays);
        var warningWindowStart = expiryDate.AddDays(daysBeforeExpiry);
        var warningWindowEnd = expiryDate.AddDays(daysBeforeExpiry + 1);

        // Find members in the warning window
        var membersToWarn = await context.LoyaltyMembers
            .Where(m => m.IsActive &&
                        m.PointsBalance > 0 &&
                        m.LastVisit.HasValue &&
                        m.LastVisit.Value >= warningWindowStart &&
                        m.LastVisit.Value < warningWindowEnd)
            .ToListAsync(cancellationToken);

        var warnedCount = 0;

        // Note: SMS sending would be done here with ISmsService
        // For now, we just log the warnings
        foreach (var member in membersToWarn)
        {
            var daysUntilExpiry = config.PointsExpiryDays -
                (int)(DateTime.UtcNow - member.LastVisit!.Value).TotalDays;

            _logger.LogInformation(
                "Expiry warning: Member {MemberId} ({Phone}) has {Points} points expiring in {Days} days",
                member.Id, member.PhoneNumber, member.PointsBalance, daysUntilExpiry);

            warnedCount++;
        }

        _logger.LogInformation(
            "Sent expiry warnings to {Count} members",
            warnedCount);

        return warnedCount;
    }

    public async Task<IEnumerable<PointsExpiryInfo>> GetMembersApproachingExpiryAsync(
        int daysUntilExpiry = 30,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        // Get points configuration
        var config = await context.PointsConfigurations
            .Where(c => c.IsDefault && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null || config.PointsExpiryDays <= 0)
        {
            return Enumerable.Empty<PointsExpiryInfo>();
        }

        var expiryThreshold = DateTime.UtcNow.AddDays(-(config.PointsExpiryDays - daysUntilExpiry));

        var members = await context.LoyaltyMembers
            .Where(m => m.IsActive &&
                        m.PointsBalance > 0 &&
                        m.LastVisit.HasValue &&
                        m.LastVisit.Value <= expiryThreshold)
            .Select(m => new PointsExpiryInfo
            {
                MemberId = m.Id,
                Name = m.Name,
                PhoneNumber = m.PhoneNumber,
                PointsBalance = m.PointsBalance,
                LastActivity = m.LastVisit,
                DaysUntilExpiry = config.PointsExpiryDays -
                    (int)EF.Functions.DateDiffDay(m.LastVisit!.Value, DateTime.UtcNow),
                WarningAlreadySent = false // Would track this in a separate table
            })
            .ToListAsync(cancellationToken);

        return members;
    }
}
