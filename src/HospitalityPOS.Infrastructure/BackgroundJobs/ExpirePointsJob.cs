using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Configuration options for points expiry.
/// </summary>
public class PointsExpiryOptions
{
    public const string SectionName = "Loyalty:PointsExpiry";

    /// <summary>
    /// Number of months after which earned points expire. Default is 12 months.
    /// </summary>
    public int ExpiryMonths { get; set; } = 12;

    /// <summary>
    /// Whether to send SMS notifications before points expire.
    /// </summary>
    public bool SendExpiryWarnings { get; set; } = true;

    /// <summary>
    /// Number of days before expiry to send warning. Default is 30 days.
    /// </summary>
    public int WarningDaysBeforeExpiry { get; set; } = 30;

    /// <summary>
    /// Minimum points to expire (ignore small balances). Default is 10.
    /// </summary>
    public decimal MinimumPointsToExpire { get; set; } = 10m;
}

/// <summary>
/// Background job that expires loyalty points after a configurable period.
/// Runs on the 1st of each month by default.
/// </summary>
public class ExpirePointsJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpirePointsJob> _logger;
    private readonly PointsExpiryOptions _options;

    public ExpirePointsJob(
        IServiceProvider serviceProvider,
        ILogger<ExpirePointsJob> logger,
        IOptions<PointsExpiryOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new PointsExpiryOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpirePointsJob started with {Months} month expiry period", _options.ExpiryMonths);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate time until next run (1st of next month at midnight)
            var now = DateTime.Now;
            var nextRunDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var delay = nextRunDate - now;

            _logger.LogInformation("ExpirePointsJob next run at {NextRun}", nextRunDate);

            await Task.Delay(delay, stoppingToken);

            try
            {
                await ProcessExpiredPointsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired points");
            }
        }
    }

    private async Task ProcessExpiredPointsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting points expiry processing at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var expiryThreshold = DateTime.UtcNow.AddMonths(-_options.ExpiryMonths);

        // Find members with points that should expire based on their last activity
        // Points expire if there's been no earning activity for ExpiryMonths
        var membersToExpire = await dbContext.Set<LoyaltyMember>()
            .Where(m => m.PointsBalance >= _options.MinimumPointsToExpire)
            .Where(m => !m.Transactions
                .Where(t => t.TransactionType == LoyaltyTransactionType.Earned)
                .Any(t => t.TransactionDate > expiryThreshold))
            .Include(m => m.Transactions.OrderByDescending(t => t.TransactionDate).Take(1))
            .ToListAsync(cancellationToken);

        if (membersToExpire.Count == 0)
        {
            _logger.LogInformation("No points to expire");
            return;
        }

        _logger.LogInformation("Found {Count} members with expiring points", membersToExpire.Count);

        var totalExpiredPoints = 0m;
        var processedCount = 0;

        foreach (var member in membersToExpire)
        {
            try
            {
                var pointsToExpire = member.PointsBalance;
                var previousBalance = member.PointsBalance;

                // Create expiry transaction
                var expiryTransaction = new LoyaltyTransaction
                {
                    LoyaltyMemberId = member.Id,
                    TransactionType = LoyaltyTransactionType.Expired,
                    Points = -pointsToExpire, // Negative to deduct
                    MonetaryValue = 0,
                    BalanceAfter = 0, // Balance after expiry
                    Description = $"Points expired after {_options.ExpiryMonths} months of inactivity",
                    ReferenceNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{member.Id}",
                    TransactionDate = DateTime.UtcNow,
                    ProcessedByUserId = 1 // System user
                };

                dbContext.Set<LoyaltyTransaction>().Add(expiryTransaction);

                // Update member balance
                member.PointsBalance = 0;

                totalExpiredPoints += pointsToExpire;
                processedCount++;

                _logger.LogInformation(
                    "Expired {Points} points for member {MemberNumber} ({Phone}). Previous balance: {Previous}",
                    pointsToExpire,
                    member.MembershipNumber,
                    member.PhoneNumber,
                    previousBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring points for member {MemberId}", member.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Points expiry complete. Expired {TotalPoints} points from {Count} members",
            totalExpiredPoints,
            processedCount);

        // Send notifications if enabled
        if (_options.SendExpiryWarnings)
        {
            await SendExpiryWarningsAsync(scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task SendExpiryWarningsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<POSDbContext>();
            var warningThreshold = DateTime.UtcNow.AddMonths(-_options.ExpiryMonths).AddDays(_options.WarningDaysBeforeExpiry);

            // Find members whose points will expire soon
            var membersToWarn = await dbContext.Set<LoyaltyMember>()
                .Where(m => m.PointsBalance >= _options.MinimumPointsToExpire)
                .Where(m => !m.Transactions
                    .Where(t => t.TransactionType == LoyaltyTransactionType.Earned)
                    .Any(t => t.TransactionDate > warningThreshold))
                .ToListAsync(cancellationToken);

            if (membersToWarn.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Sending expiry warnings to {Count} members", membersToWarn.Count);

            // TODO: Integrate with SMS service when available
            // var smsService = serviceProvider.GetService<ISmsService>();
            foreach (var member in membersToWarn)
            {
                var message = $"Your {member.PointsBalance:N0} loyalty points will expire in {_options.WarningDaysBeforeExpiry} days. " +
                             "Visit us soon to use them!";

                _logger.LogInformation(
                    "Would send SMS to {Phone}: {Message}",
                    member.PhoneNumber,
                    message);

                // await smsService?.SendAsync(member.PhoneNumber, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry warnings");
        }
    }
}
