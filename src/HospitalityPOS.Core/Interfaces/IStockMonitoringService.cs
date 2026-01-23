namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Result of a stock monitoring run.
/// </summary>
public class StockMonitoringResult
{
    /// <summary>
    /// Whether the monitoring run was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of reorder suggestions generated.
    /// </summary>
    public int SuggestionsGenerated { get; set; }

    /// <summary>
    /// Number of purchase orders created.
    /// </summary>
    public int PurchaseOrdersCreated { get; set; }

    /// <summary>
    /// Number of notifications sent.
    /// </summary>
    public int NotificationsSent { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the monitoring run.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// When the monitoring run started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the monitoring run completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Service for stock monitoring and automatic reorder generation.
/// </summary>
public interface IStockMonitoringService
{
    /// <summary>
    /// Gets whether the monitoring service is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the last run result.
    /// </summary>
    StockMonitoringResult? LastResult { get; }

    /// <summary>
    /// Runs stock monitoring for all stores or a specific store.
    /// </summary>
    Task<StockMonitoringResult> RunStockMonitoringAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers a stock monitoring run.
    /// </summary>
    Task<StockMonitoringResult> TriggerManualRunAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of the monitoring service.
    /// </summary>
    Task<StockMonitoringStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of the stock monitoring service.
/// </summary>
public class StockMonitoringStatus
{
    /// <summary>
    /// Whether the service is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether the service is currently running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Last run time.
    /// </summary>
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// Next scheduled run time.
    /// </summary>
    public DateTime? NextRunTime { get; set; }

    /// <summary>
    /// Check interval in minutes.
    /// </summary>
    public int CheckIntervalMinutes { get; set; }

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Last error message (if any).
    /// </summary>
    public string? LastErrorMessage { get; set; }
}
