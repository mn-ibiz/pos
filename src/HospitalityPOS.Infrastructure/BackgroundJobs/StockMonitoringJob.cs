using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Configuration options for stock monitoring.
/// </summary>
public class StockMonitoringOptions
{
    public const string SectionName = "StockMonitoring";

    /// <summary>
    /// Interval in minutes between stock checks. Default is 15 minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Hour of day when monitoring starts (0-23). Default is 6 AM.
    /// </summary>
    public int EnabledHoursStart { get; set; } = 6;

    /// <summary>
    /// Hour of day when monitoring ends (0-23). Default is 10 PM.
    /// </summary>
    public int EnabledHoursEnd { get; set; } = 22;

    /// <summary>
    /// Whether to run on weekends. Default is false.
    /// </summary>
    public bool RunOnWeekends { get; set; } = false;

    /// <summary>
    /// Maximum suggestions to generate per run. Default is 100.
    /// </summary>
    public int MaxSuggestionsPerRun { get; set; } = 100;

    /// <summary>
    /// Maximum consecutive errors before applying extended backoff. Default is 3.
    /// </summary>
    public int MaxConsecutiveErrors { get; set; } = 3;
}

/// <summary>
/// Interface for manual stock monitoring operations.
/// </summary>
public interface IStockMonitoringService
{
    /// <summary>
    /// Manually triggers a stock check and returns generated suggestions.
    /// </summary>
    Task<List<ReorderSuggestion>> RunStockCheckNowAsync();

    /// <summary>
    /// Gets the time of the last successful run.
    /// </summary>
    DateTime? GetLastRunTime();

    /// <summary>
    /// Gets whether a stock check is currently running.
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Background job that periodically monitors inventory levels and generates reorder suggestions.
/// Provides automation backbone for the auto-PO generation feature.
/// </summary>
public class StockMonitoringJob : BackgroundService, IStockMonitoringService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockMonitoringJob> _logger;
    private readonly StockMonitoringOptions _options;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    private DateTime? _lastRunTime;
    private int _consecutiveErrors;
    private volatile bool _isRunning;

    public bool IsRunning => _isRunning;

    public StockMonitoringJob(
        IServiceProvider serviceProvider,
        ILogger<StockMonitoringJob> logger,
        IOptions<StockMonitoringOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new StockMonitoringOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "StockMonitoringJob started with {Interval} minute interval, running {Start}:00-{End}:00",
            _options.IntervalMinutes,
            _options.EnabledHoursStart,
            _options.EnabledHoursEnd);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldRunNow())
                {
                    await ProcessStockCheckAsync(stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Skipping stock check - outside business hours or weekend");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stock monitoring");
                _consecutiveErrors++;
            }

            // Calculate next delay with exponential backoff on errors
            var delay = CalculateNextDelay();
            await Task.Delay(delay, stoppingToken);
        }
    }

    public DateTime? GetLastRunTime() => _lastRunTime;

    public async Task<List<ReorderSuggestion>> RunStockCheckNowAsync()
    {
        _logger.LogInformation("Manual stock check triggered");
        return await ProcessStockCheckAsync(CancellationToken.None);
    }

    private bool ShouldRunNow()
    {
        var now = DateTime.Now;

        // Check business hours
        if (now.Hour < _options.EnabledHoursStart || now.Hour >= _options.EnabledHoursEnd)
        {
            return false;
        }

        // Check weekends
        if (!_options.RunOnWeekends && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday))
        {
            return false;
        }

        return true;
    }

    private TimeSpan CalculateNextDelay()
    {
        var baseDelay = TimeSpan.FromMinutes(_options.IntervalMinutes);

        // Apply exponential backoff if there are consecutive errors
        if (_consecutiveErrors >= _options.MaxConsecutiveErrors)
        {
            var backoffMultiplier = Math.Min(Math.Pow(2, _consecutiveErrors - _options.MaxConsecutiveErrors + 1), 8);
            return TimeSpan.FromMinutes(baseDelay.TotalMinutes * backoffMultiplier);
        }

        return baseDelay;
    }

    private async Task<List<ReorderSuggestion>> ProcessStockCheckAsync(CancellationToken cancellationToken)
    {
        // Prevent concurrent runs
        if (!await _runLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Stock check already running, skipping this cycle");
            return [];
        }

        try
        {
            _isRunning = true;
            _logger.LogInformation("Starting stock level check at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<ISystemConfigurationService>();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IInventoryAnalyticsService>();

            // Check if auto-generation is enabled
            var config = await configService.GetConfigurationAsync();
            if (config is null || !config.AutoGeneratePurchaseOrders)
            {
                _logger.LogDebug("Auto-generate POs is disabled, skipping stock check");
                return [];
            }

            // Generate reorder suggestions for all stores
            var suggestions = await analyticsService.GenerateReorderSuggestionsAsync(storeId: null);
            var generatedSuggestions = suggestions?.ToList() ?? [];

            if (generatedSuggestions.Count > 0)
            {
                _logger.LogInformation("Generated {Count} reorder suggestions", generatedSuggestions.Count);

                // If auto-send is enabled and configured, create and send POs
                if (config.AutoSendPurchaseOrders)
                {
                    await CreateAndSendPurchaseOrdersAsync(scope.ServiceProvider, generatedSuggestions, cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("No reorder suggestions generated - all stock levels OK");
            }

            _lastRunTime = DateTime.UtcNow;
            _consecutiveErrors = 0; // Reset on success

            _logger.LogInformation("Stock level check completed at {Time}", DateTime.UtcNow);
            return generatedSuggestions;
        }
        finally
        {
            _isRunning = false;
            _runLock.Release();
        }
    }

    private async Task CreateAndSendPurchaseOrdersAsync(
        IServiceProvider serviceProvider,
        List<ReorderSuggestion> suggestions,
        CancellationToken cancellationToken)
    {
        try
        {
            var analyticsService = serviceProvider.GetRequiredService<IInventoryAnalyticsService>();

            // Get approved suggestion IDs (only auto-create for critical/high priority)
            var suggestionIds = suggestions
                .Where(s => s.Priority is "Critical" or "High")
                .Select(s => s.Id)
                .Take(_options.MaxSuggestionsPerRun)
                .ToList();

            if (suggestionIds.Count == 0)
            {
                _logger.LogDebug("No critical/high priority suggestions to auto-convert to POs");
                return;
            }

            var purchaseOrders = await analyticsService.ConvertSuggestionsToPurchaseOrdersAsync(suggestionIds);

            _logger.LogInformation(
                "Auto-created {Count} purchase orders from {SuggestionCount} critical/high priority suggestions",
                purchaseOrders.Count,
                suggestionIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase orders from suggestions");
        }
    }

    public override void Dispose()
    {
        _runLock.Dispose();
        base.Dispose();
    }
}
