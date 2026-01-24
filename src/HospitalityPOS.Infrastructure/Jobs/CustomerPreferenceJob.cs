using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that updates customer preferences daily.
/// Runs every day at 3 AM.
/// </summary>
public class CustomerPreferenceJob : BackgroundService, ICustomerPreferenceJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CustomerPreferenceJob> _logger;
    private readonly TimeSpan _dailyRunTime = TimeSpan.FromHours(3); // 3 AM
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _triggerCompletion;

    public DateTime? LastRunTime { get; private set; }
    public int LastUpdatedCount { get; private set; }

    public CustomerPreferenceJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CustomerPreferenceJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CustomerPreferenceJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next 3 AM
                var now = DateTime.UtcNow;
                var nextRun = now.Date.Add(_dailyRunTime);
                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                _logger.LogInformation("Next customer preference update scheduled for {NextRun}", nextRun);

                // Wait for either the scheduled time or a manual trigger
                var delayTask = Task.Delay(delay, stoppingToken);
                var triggerTask = WaitForTriggerAsync(stoppingToken);

                await Task.WhenAny(delayTask, triggerTask);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await UpdatePreferencesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CustomerPreferenceJob execution loop");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("CustomerPreferenceJob stopped");
    }

    private async Task WaitForTriggerAsync(CancellationToken stoppingToken)
    {
        _triggerCompletion = new TaskCompletionSource<bool>();
        using var registration = stoppingToken.Register(() => _triggerCompletion.TrySetCanceled());
        await _triggerCompletion.Task;
    }

    public async Task TriggerUpdateAsync()
    {
        _triggerCompletion?.TrySetResult(true);

        // Wait briefly for the run to start
        await Task.Delay(100);

        // Wait for the run to complete
        await _runSemaphore.WaitAsync();
        _runSemaphore.Release();
    }

    private async Task UpdatePreferencesAsync(CancellationToken stoppingToken)
    {
        if (!await _runSemaphore.WaitAsync(0))
        {
            _logger.LogDebug("Customer preference update already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting daily customer preference update");

            using var scope = _scopeFactory.CreateScope();
            var upsellService = scope.ServiceProvider.GetRequiredService<IUpsellService>();

            await upsellService.UpdateAllCustomerPreferencesAsync();

            LastRunTime = DateTime.UtcNow;

            _logger.LogInformation("Customer preference update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer preference update");
        }
        finally
        {
            _runSemaphore.Release();
        }
    }
}
