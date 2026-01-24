using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that processes scheduled campaign flow steps.
/// Runs every 15 minutes to execute due steps.
/// </summary>
public class CampaignFlowProcessorJob : BackgroundService, ICampaignFlowProcessorJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignFlowProcessorJob> _logger;
    private readonly TimeSpan _processInterval = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public FlowProcessingResult? LastRunResult { get; private set; }

    public CampaignFlowProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignFlowProcessorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignFlowProcessorJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either the interval or a manual trigger
                var delayTask = Task.Delay(_processInterval, stoppingToken);
                var triggerTask = WaitForTriggerAsync(stoppingToken);

                await Task.WhenAny(delayTask, triggerTask);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessStepsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CampaignFlowProcessorJob execution loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("CampaignFlowProcessorJob stopped");
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

    private async Task ProcessStepsAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Flow processing already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting campaign flow step processing");

            using var scope = _scopeFactory.CreateScope();
            var flowService = scope.ServiceProvider.GetRequiredService<ICampaignFlowService>();

            LastRunResult = await flowService.ProcessScheduledStepsAsync();

            _logger.LogInformation(
                "Campaign flow processing completed: {Processed} processed, {Executed} executed, {Skipped} skipped, {Failed} failed, {Completed} enrollments completed in {Duration}ms",
                LastRunResult.StepsProcessed,
                LastRunResult.StepsExecuted,
                LastRunResult.StepsSkipped,
                LastRunResult.StepsFailed,
                LastRunResult.EnrollmentsCompleted,
                LastRunResult.DurationMs);

            if (LastRunResult.Errors.Any())
            {
                _logger.LogWarning("Flow processing completed with {ErrorCount} errors", LastRunResult.Errors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing campaign flow steps");
            LastRunResult = new FlowProcessingResult
            {
                CompletedAt = DateTime.UtcNow,
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            _runSemaphore.Release();
        }
    }
}
