using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job for processing birthday rewards daily.
/// </summary>
public class BirthdayRewardJob : BackgroundService, IBirthdayRewardJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BirthdayRewardJob> _logger;

    /// <summary>
    /// Hour of day to run the job (0-23, default: 6 AM).
    /// </summary>
    public int RunHour { get; set; } = 6;

    /// <summary>
    /// Minute of the hour to run the job (0-59, default: 0).
    /// </summary>
    public int RunMinute { get; set; } = 0;

    public BirthdayRewardJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BirthdayRewardJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayRewardJob started. Scheduled to run at {Hour}:{Minute:D2} daily",
            RunHour, RunMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate next run time
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(RunHour).AddMinutes(RunMinute);

                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogDebug("Next birthday reward job run scheduled for {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Run the jobs
                await ProcessTodaysBirthdaysAsync(stoppingToken);
                await ProcessExpiredRewardsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BirthdayRewardJob execution");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("BirthdayRewardJob stopped");
    }

    public async Task<BirthdayRewardJobSummary> ProcessTodaysBirthdaysAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting birthday rewards processing for {Date}", DateOnly.FromDateTime(DateTime.UtcNow));

        using var scope = _scopeFactory.CreateScope();
        var birthdayRewardService = scope.ServiceProvider.GetRequiredService<IBirthdayRewardService>();

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await birthdayRewardService.ProcessBirthdayRewardsAsync(targetDate, cancellationToken);

        _logger.LogInformation(
            "Birthday rewards processing completed. Total: {Total}, Issued: {Issued}, Skipped: {Skipped}, Failed: {Failed}",
            summary.TotalMembersWithBirthdays,
            summary.RewardsIssued,
            summary.RewardsSkipped,
            summary.RewardsFailed);

        return summary;
    }

    public async Task<int> ProcessExpiredRewardsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing expired rewards");

        using var scope = _scopeFactory.CreateScope();
        var birthdayRewardService = scope.ServiceProvider.GetRequiredService<IBirthdayRewardService>();

        var expiredCount = await birthdayRewardService.ProcessExpiredRewardsAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} rewards as expired", expiredCount);

        // Also send expiry warnings for rewards expiring in 3 days
        var warningsCount = await birthdayRewardService.SendExpiryWarningsAsync(3, cancellationToken);
        _logger.LogInformation("Sent {Count} expiry warnings", warningsCount);

        return expiredCount;
    }
}
