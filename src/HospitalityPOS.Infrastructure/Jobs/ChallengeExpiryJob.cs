using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that processes expired challenges and creates recurring instances.
/// Runs every hour to handle challenge lifecycle.
/// </summary>
public class ChallengeExpiryJob : BackgroundService, IChallengeExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChallengeExpiryJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public ChallengeExpiryJobSummary? LastRunSummary { get; private set; }

    public ChallengeExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ChallengeExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChallengeExpiryJob started");

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

                await RunExpiryCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChallengeExpiryJob execution loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("ChallengeExpiryJob stopped");
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

    private async Task RunExpiryCheckAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Challenge expiry check already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting challenge expiry check");

            using var scope = _scopeFactory.CreateScope();
            var gamificationService = scope.ServiceProvider.GetRequiredService<IGamificationService>();

            LastRunSummary = await gamificationService.ProcessExpiredChallengesAsync();

            _logger.LogInformation(
                "Challenge expiry check completed: {Expired} expired, {Failed} failed, {Recurring} recurring created in {Duration}ms",
                LastRunSummary.ExpiredChallengesCount,
                LastRunSummary.FailedChallengesCount,
                LastRunSummary.RecurringChallengesCreated,
                LastRunSummary.DurationMs);

            if (LastRunSummary.ErrorCount > 0)
            {
                _logger.LogWarning("Challenge expiry check completed with {ErrorCount} errors", LastRunSummary.ErrorCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running challenge expiry check");
            LastRunSummary = new ChallengeExpiryJobSummary
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
