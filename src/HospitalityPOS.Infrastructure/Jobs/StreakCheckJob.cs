using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that checks for broken streaks and sends at-risk warnings.
/// Runs every hour to ensure timely notifications.
/// </summary>
public class StreakCheckJob : BackgroundService, IStreakCheckJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StreakCheckJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public StreakCheckJobSummary? LastRunSummary { get; private set; }

    public StreakCheckJob(
        IServiceScopeFactory scopeFactory,
        ILogger<StreakCheckJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StreakCheckJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either the interval or a manual trigger
                var delayTask = Task.Delay(_checkInterval, stoppingToken);
                var triggerTask = WaitForTriggerAsync(stoppingToken);

                await Task.WhenAny(delayTask, triggerTask);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await RunCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StreakCheckJob execution loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("StreakCheckJob stopped");
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

    private async Task RunCheckAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Streak check already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting streak check");

            using var scope = _scopeFactory.CreateScope();
            var gamificationService = scope.ServiceProvider.GetRequiredService<IGamificationService>();

            LastRunSummary = await gamificationService.ProcessBrokenStreaksAsync();

            _logger.LogInformation(
                "Streak check completed: {MembersChecked} members checked, {BrokenStreaks} broken, {WarningsSent} warnings in {Duration}ms",
                LastRunSummary.TotalMembersChecked,
                LastRunSummary.BrokenStreaks,
                LastRunSummary.AtRiskWarningsSent,
                LastRunSummary.DurationMs);

            if (LastRunSummary.ErrorCount > 0)
            {
                _logger.LogWarning("Streak check completed with {ErrorCount} errors", LastRunSummary.ErrorCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running streak check");
            LastRunSummary = new StreakCheckJobSummary
            {
                CompletedAt = DateTime.UtcNow,
                ErrorCount = 1,
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            _runSemaphore.Release();
        }
    }
}
