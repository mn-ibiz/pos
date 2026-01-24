using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job for expiring pending referrals that have passed their deadline.
/// </summary>
public class ReferralExpiryJob : BackgroundService, IReferralExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReferralExpiryJob> _logger;

    /// <summary>
    /// Time between job runs (default: 1 hour).
    /// </summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the last run summary.
    /// </summary>
    public ReferralExpiryJobSummary? LastRunSummary { get; private set; }

    /// <summary>
    /// Gets when the job last ran.
    /// </summary>
    public DateTime? LastRunTime { get; private set; }

    public ReferralExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReferralExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReferralExpiryJob started");

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var summary = await RunNowAsync(stoppingToken);
                LastRunSummary = summary;
                LastRunTime = DateTime.UtcNow;

                if (summary.ExpiredCount > 0)
                {
                    _logger.LogInformation(
                        "ReferralExpiryJob completed: {ExpiredCount} referrals expired",
                        summary.ExpiredCount);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReferralExpiryJob execution");
            }

            // Wait for next interval
            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ReferralExpiryJob stopped");
    }

    /// <summary>
    /// Runs the referral expiry job immediately.
    /// </summary>
    public async Task<ReferralExpiryJobSummary> RunNowAsync(CancellationToken cancellationToken = default)
    {
        var summary = new ReferralExpiryJobSummary
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            // Check if referral program is active
            var config = await context.ReferralConfigurations
                .Where(c => c.StoreId == null && c.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null || !config.IsProgramActive)
            {
                _logger.LogDebug("Referral program is not active, skipping expiry job");
                summary.Skipped = true;
                summary.SkipReason = "Referral program is not active";
                summary.CompletedAt = DateTime.UtcNow;
                return summary;
            }

            // Find pending referrals that have expired
            var now = DateTime.UtcNow;
            var expiredReferrals = await context.Referrals
                .Where(r => r.Status == ReferralStatus.Pending &&
                            r.ExpiresAt <= now &&
                            r.IsActive)
                .Include(r => r.Referrer)
                .Include(r => r.Referee)
                .ToListAsync(cancellationToken);

            summary.TotalPendingChecked = expiredReferrals.Count;

            foreach (var referral in expiredReferrals)
            {
                try
                {
                    referral.Status = ReferralStatus.Expired;
                    referral.UpdatedAt = now;

                    _logger.LogDebug(
                        "Expired referral {ReferralId}: Referrer {ReferrerId} -> Referee {RefereeId}",
                        referral.Id, referral.ReferrerId, referral.RefereeId);

                    summary.ExpiredCount++;
                    summary.ExpiredReferralIds.Add(referral.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error expiring referral {ReferralId}",
                        referral.Id);
                    summary.ErrorCount++;
                    summary.Errors.Add($"Referral {referral.Id}: {ex.Message}");
                }
            }

            if (summary.ExpiredCount > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            // Also send warnings for referrals about to expire (optional)
            await SendExpiryWarningsAsync(context, config, cancellationToken, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in referral expiry job");
            summary.Errors.Add($"Job error: {ex.Message}");
            summary.ErrorCount++;
        }

        summary.CompletedAt = DateTime.UtcNow;
        return summary;
    }

    private async Task SendExpiryWarningsAsync(
        POSDbContext context,
        ReferralConfiguration config,
        CancellationToken cancellationToken,
        ReferralExpiryJobSummary summary)
    {
        try
        {
            // Find referrals expiring in the next 7 days that haven't been warned
            var warningThreshold = DateTime.UtcNow.AddDays(7);
            var now = DateTime.UtcNow;

            var expiringReferrals = await context.Referrals
                .Where(r => r.Status == ReferralStatus.Pending &&
                            r.ExpiresAt > now &&
                            r.ExpiresAt <= warningThreshold &&
                            r.IsActive)
                .Include(r => r.Referee)
                .ToListAsync(cancellationToken);

            foreach (var referral in expiringReferrals)
            {
                var daysLeft = (int)(referral.ExpiresAt - now).TotalDays;

                _logger.LogDebug(
                    "Referral {ReferralId} for member {RefereeName} ({RefereeId}) expires in {Days} days",
                    referral.Id,
                    referral.Referee?.Name ?? "Unknown",
                    referral.RefereeId,
                    daysLeft);

                summary.WarningsSent++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry warnings");
            summary.Errors.Add($"Warning error: {ex.Message}");
        }
    }
}
