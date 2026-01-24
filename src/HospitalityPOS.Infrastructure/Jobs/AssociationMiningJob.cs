using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that rebuilds product associations weekly.
/// Runs every Sunday at 2 AM.
/// </summary>
public class AssociationMiningJob : BackgroundService, IAssociationMiningJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssociationMiningJob> _logger;
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public AssociationRebuildResult? LastResult { get; private set; }

    public AssociationMiningJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AssociationMiningJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AssociationMiningJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next Sunday 2 AM
                var now = DateTime.UtcNow;
                var nextRun = GetNextSundayAt2AM(now);

                var delay = nextRun - now;
                if (delay.TotalMilliseconds < 0)
                    delay = TimeSpan.Zero;

                _logger.LogInformation("Next association mining scheduled for {NextRun}", nextRun);

                // Wait for either the scheduled time or a manual trigger
                var delayTask = Task.Delay(delay, stoppingToken);
                var triggerTask = WaitForTriggerAsync(stoppingToken);

                await Task.WhenAny(delayTask, triggerTask);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await RebuildAssociationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssociationMiningJob execution loop");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("AssociationMiningJob stopped");
    }

    private static DateTime GetNextSundayAt2AM(DateTime from)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && from.Hour >= 2)
            daysUntilSunday = 7;

        return from.Date.AddDays(daysUntilSunday).AddHours(2);
    }

    private async Task WaitForTriggerAsync(CancellationToken stoppingToken)
    {
        _triggerCompletion = new TaskCompletionSource<bool>();
        using var registration = stoppingToken.Register(() => _triggerCompletion.TrySetCanceled());
        await _triggerCompletion.Task;
    }

    public async Task TriggerRebuildAsync()
    {
        _triggerCompletion?.TrySetResult(true);

        // Wait briefly for the run to start
        await Task.Delay(100);

        // Wait for the run to complete
        await _runSemaphore.WaitAsync();
        _runSemaphore.Release();
    }

    private async Task RebuildAssociationsAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Association mining already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting weekly association mining");

            using var scope = _scopeFactory.CreateScope();
            var upsellService = scope.ServiceProvider.GetRequiredService<IUpsellService>();

            LastResult = await upsellService.RebuildAssociationsAsync();

            _logger.LogInformation(
                "Association mining completed: {Analyzed} transactions analyzed, {Found} associations found, {Stored} stored in {Duration}ms",
                LastResult.TransactionsAnalyzed,
                LastResult.AssociationsFound,
                LastResult.AssociationsStored,
                LastResult.DurationMs);

            if (LastResult.Errors.Any())
            {
                _logger.LogWarning("Association mining completed with {ErrorCount} errors", LastResult.Errors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during association mining");
            LastResult = new AssociationRebuildResult
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            _runSemaphore.Release();
        }
    }
}
