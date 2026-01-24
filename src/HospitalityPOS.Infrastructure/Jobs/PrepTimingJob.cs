using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that processes scheduled item fires.
/// Runs every 15 seconds to check for items ready to fire.
/// </summary>
public class PrepTimingJob : BackgroundService, IPrepTimingJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrepTimingJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);
    private PrepTimingJobResult? _lastResult;

    public PrepTimingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PrepTimingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the last job run result.
    /// </summary>
    public PrepTimingJobResult? LastResult => _lastResult;

    /// <summary>
    /// Triggers an immediate processing run.
    /// </summary>
    public async Task TriggerProcessingAsync()
    {
        _logger.LogInformation("Manual prep timing processing triggered");
        await ProcessScheduledFiresAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrepTimingJob started. Running every {Interval} seconds", _interval.TotalSeconds);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledFiresAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PrepTimingJob execution");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("PrepTimingJob stopped");
    }

    private async Task ProcessScheduledFiresAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var prepTimingService = scope.ServiceProvider.GetRequiredService<IKdsPrepTimingService>();

        try
        {
            _lastResult = await prepTimingService.ProcessScheduledFiresAsync();

            if (_lastResult.ItemsFired > 0 || _lastResult.ItemsMarkedOverdue > 0)
            {
                _logger.LogInformation(
                    "PrepTimingJob processed: {Processed} items, {Fired} fired, {Overdue} overdue in {Duration}ms",
                    _lastResult.ItemsProcessed,
                    _lastResult.ItemsFired,
                    _lastResult.ItemsMarkedOverdue,
                    _lastResult.DurationMs);
            }

            if (_lastResult.Errors.Any())
            {
                foreach (var error in _lastResult.Errors)
                {
                    _logger.LogWarning("PrepTimingJob error: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled fires");
            _lastResult = new PrepTimingJobResult
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Errors = { ex.Message }
            };
        }
    }
}
