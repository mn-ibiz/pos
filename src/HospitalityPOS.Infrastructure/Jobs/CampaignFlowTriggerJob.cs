using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that processes daily campaign flow triggers.
/// Runs daily at 6 AM to check for birthdays, anniversaries, inactivity, etc.
/// </summary>
public class CampaignFlowTriggerJob : BackgroundService, ICampaignFlowTriggerJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignFlowTriggerJob> _logger;
    private readonly TimeSpan _dailyRunTime = TimeSpan.FromHours(6); // 6 AM
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public TriggerProcessingResult? LastRunResult { get; private set; }

    public CampaignFlowTriggerJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignFlowTriggerJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignFlowTriggerJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next 6 AM
                var now = DateTime.UtcNow;
                var nextRun = now.Date.Add(_dailyRunTime);
                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                _logger.LogInformation("Next campaign trigger run scheduled for {NextRun}", nextRun);

                // Wait for either the scheduled time or a manual trigger
                var delayTask = Task.Delay(delay, stoppingToken);
                var triggerTask = WaitForTriggerAsync(stoppingToken);

                await Task.WhenAny(delayTask, triggerTask);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessTriggersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CampaignFlowTriggerJob execution loop");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("CampaignFlowTriggerJob stopped");
    }

    private async Task WaitForTriggerAsync(CancellationToken stoppingToken)
    {
        _triggerCompletion = new TaskCompletionSource<bool>();
        using var registration = stoppingToken.Register(() => _triggerCompletion.TrySetCanceled());
        await _triggerCompletion.Task;
    }

    public async Task TriggerRunAsync()
    {
        _triggerCompletion?.TrySetResult(true);

        // Wait briefly for the run to start
        await Task.Delay(100);

        // Wait for the run to complete
        await _runSemaphore.WaitAsync();
        _runSemaphore.Release();
    }

    private async Task ProcessTriggersAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Trigger processing already running, skipping");
            return;
        }

        var result = new TriggerProcessingResult();

        try
        {
            _logger.LogInformation("Starting campaign flow trigger processing");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();
            var triggerService = scope.ServiceProvider.GetRequiredService<ICampaignFlowTriggerService>();
            var config = await GetConfigurationAsync(context);

            var today = DateTime.UtcNow.Date;

            // Process birthdays
            result.BirthdayFlowsTriggered = await ProcessBirthdaysAsync(context, triggerService, config, today);

            // Process anniversaries
            result.AnniversaryFlowsTriggered = await ProcessAnniversariesAsync(context, triggerService, today);

            // Process win-back (inactivity)
            result.WinBackFlowsTriggered = await ProcessInactivityAsync(context, triggerService, config);

            // Process points expiry
            result.PointsExpiryFlowsTriggered = await ProcessPointsExpiryAsync(context, triggerService, config, today);

            result.TotalEnrollments = result.BirthdayFlowsTriggered +
                                     result.AnniversaryFlowsTriggered +
                                     result.WinBackFlowsTriggered +
                                     result.PointsExpiryFlowsTriggered;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Fatal error: {ex.Message}");
            _logger.LogError(ex, "Error processing campaign flow triggers");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            LastRunResult = result;
            _runSemaphore.Release();
        }

        _logger.LogInformation(
            "Campaign trigger processing completed: {Birthdays} birthdays, {Anniversaries} anniversaries, {WinBack} win-back, {PointsExpiry} points expiry in {Duration}ms",
            result.BirthdayFlowsTriggered,
            result.AnniversaryFlowsTriggered,
            result.WinBackFlowsTriggered,
            result.PointsExpiryFlowsTriggered,
            result.DurationMs);
    }

    private async Task<CampaignFlowConfiguration?> GetConfigurationAsync(POSDbContext context)
    {
        return await context.CampaignFlowConfigurations
            .FirstOrDefaultAsync(c => c.IsActive && c.StoreId == null);
    }

    private async Task<int> ProcessBirthdaysAsync(
        POSDbContext context,
        ICampaignFlowTriggerService triggerService,
        CampaignFlowConfiguration? config,
        DateTime today)
    {
        var count = 0;
        var birthdayStartDays = config?.BirthdayFlowStartDays ?? 7;

        // Find members with birthday in X days
        var targetDate = today.AddDays(birthdayStartDays);

        var birthdayMembers = await context.LoyaltyMembers
            .Where(m => m.IsActive &&
                       m.DateOfBirth.HasValue &&
                       m.DateOfBirth.Value.Month == targetDate.Month &&
                       m.DateOfBirth.Value.Day == targetDate.Day)
            .ToListAsync();

        foreach (var member in birthdayMembers)
        {
            try
            {
                // Check if already enrolled in birthday flow this year
                var alreadyEnrolled = await context.MemberFlowEnrollments
                    .AnyAsync(e => e.MemberId == member.Id &&
                                  e.Flow.Trigger == CampaignFlowTrigger.OnBirthday &&
                                  e.TriggerDate.Year == today.Year);

                if (!alreadyEnrolled)
                {
                    await triggerService.OnBirthdayAsync(member.Id);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger birthday flow for member {MemberId}", member.Id);
            }
        }

        return count;
    }

    private async Task<int> ProcessAnniversariesAsync(
        POSDbContext context,
        ICampaignFlowTriggerService triggerService,
        DateTime today)
    {
        var count = 0;

        // Find members with signup anniversary today
        var anniversaryMembers = await context.LoyaltyMembers
            .Where(m => m.IsActive &&
                       m.EnrollmentDate.Month == today.Month &&
                       m.EnrollmentDate.Day == today.Day &&
                       m.EnrollmentDate.Year < today.Year) // At least 1 year ago
            .ToListAsync();

        foreach (var member in anniversaryMembers)
        {
            try
            {
                // Check if already enrolled in anniversary flow this year
                var alreadyEnrolled = await context.MemberFlowEnrollments
                    .AnyAsync(e => e.MemberId == member.Id &&
                                  e.Flow.Trigger == CampaignFlowTrigger.OnAnniversary &&
                                  e.TriggerDate.Year == today.Year);

                if (!alreadyEnrolled)
                {
                    await triggerService.OnAnniversaryAsync(member.Id);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger anniversary flow for member {MemberId}", member.Id);
            }
        }

        return count;
    }

    private async Task<int> ProcessInactivityAsync(
        POSDbContext context,
        ICampaignFlowTriggerService triggerService,
        CampaignFlowConfiguration? config)
    {
        var count = 0;
        var inactivityDays = config?.WinBackInactivityDays ?? 30;
        var cutoffDate = DateTime.UtcNow.AddDays(-inactivityDays);

        // Find members who haven't purchased in X days
        var inactiveMembers = await context.LoyaltyMembers
            .Where(m => m.IsActive && m.LastVisitDate.HasValue && m.LastVisitDate < cutoffDate)
            .ToListAsync();

        foreach (var member in inactiveMembers)
        {
            try
            {
                var daysSinceVisit = (int)(DateTime.UtcNow - (member.LastVisitDate ?? member.EnrollmentDate)).TotalDays;

                // Only trigger at specific intervals (30, 45, 60, 90 days)
                var triggerPoints = new[] { 30, 45, 60, 90 };
                if (!triggerPoints.Contains(daysSinceVisit))
                    continue;

                // Check if already enrolled in win-back flow for this interval
                var recentEnrollment = await context.MemberFlowEnrollments
                    .Where(e => e.MemberId == member.Id &&
                               e.Flow.Trigger == CampaignFlowTrigger.OnInactivity &&
                               e.EnrolledAt > DateTime.UtcNow.AddDays(-14))
                    .AnyAsync();

                if (!recentEnrollment)
                {
                    await triggerService.OnInactivityDetectedAsync(member.Id, daysSinceVisit);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger inactivity flow for member {MemberId}", member.Id);
            }
        }

        return count;
    }

    private async Task<int> ProcessPointsExpiryAsync(
        POSDbContext context,
        ICampaignFlowTriggerService triggerService,
        CampaignFlowConfiguration? config,
        DateTime today)
    {
        var count = 0;
        var notifyDays = config?.PointsExpiryNotifyDays ?? 30;
        var targetExpiryDate = today.AddDays(notifyDays);

        // Find loyalty configuration to check expiry
        var loyaltyConfig = await context.LoyaltyProgramConfigurations
            .FirstOrDefaultAsync(c => c.IsActive);

        if (loyaltyConfig?.PointsExpiryDays == null)
            return 0;

        // Find members with points expiring in X days
        var membersWithExpiringPoints = await context.LoyaltyMembers
            .Where(m => m.IsActive && m.PointsBalance > 0)
            .ToListAsync();

        foreach (var member in membersWithExpiringPoints)
        {
            try
            {
                // Calculate expiry based on oldest transaction
                var oldestTransaction = await context.Set<LoyaltyPointsTransaction>()
                    .Where(t => t.MemberId == member.Id &&
                               t.TransactionType == PointsTransactionType.Earned &&
                               !t.ExpiresAt.HasValue || t.ExpiresAt > DateTime.UtcNow)
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (oldestTransaction?.ExpiresAt != null)
                {
                    var daysUntilExpiry = (int)(oldestTransaction.ExpiresAt.Value - today).TotalDays;

                    if (daysUntilExpiry == notifyDays)
                    {
                        // Check if already notified
                        var alreadyNotified = await context.MemberFlowEnrollments
                            .AnyAsync(e => e.MemberId == member.Id &&
                                          e.Flow.Trigger == CampaignFlowTrigger.OnPointsExpiry &&
                                          e.EnrolledAt > DateTime.UtcNow.AddDays(-7));

                        if (!alreadyNotified)
                        {
                            var expiringPoints = oldestTransaction.Points;
                            await triggerService.OnPointsExpiryApproachingAsync(member.Id, daysUntilExpiry, expiringPoints);
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger points expiry flow for member {MemberId}", member.Id);
            }
        }

        return count;
    }
}
