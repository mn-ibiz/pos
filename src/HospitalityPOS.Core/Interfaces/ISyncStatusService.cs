using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for sync status dashboard and monitoring.
/// </summary>
public interface ISyncStatusService
{
    #region Dashboard

    /// <summary>
    /// Gets the complete sync status dashboard data.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Full dashboard data.</returns>
    Task<SyncStatusDashboardDto> GetDashboardAsync(int? storeId = null);

    /// <summary>
    /// Gets a compact status for status bar display.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Status bar data.</returns>
    Task<SyncStatusBarDto> GetStatusBarAsync(int? storeId = null);

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    SyncConnectionState ConnectionState { get; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    SyncHealthStatus HealthStatus { get; }

    /// <summary>
    /// Gets whether the system is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Gets whether a sync operation is currently in progress.
    /// </summary>
    bool IsSyncing { get; }

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    DateTime? LastSyncTime { get; }

    #endregion

    #region Queue Status

    /// <summary>
    /// Gets the sync queue summary.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Queue summary.</returns>
    Task<SyncQueueSummaryDto> GetQueueSummaryAsync(int? storeId = null);

    /// <summary>
    /// Gets the count of pending sync items.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Pending count.</returns>
    Task<int> GetPendingCountAsync(int? storeId = null);

    /// <summary>
    /// Gets the count of failed sync items.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Failed count.</returns>
    Task<int> GetFailedCountAsync(int? storeId = null);

    #endregion

    #region Errors

    /// <summary>
    /// Gets recent sync errors.
    /// </summary>
    /// <param name="limit">Maximum number of errors to return.</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>List of recent errors.</returns>
    Task<List<SyncErrorDto>> GetRecentErrorsAsync(int limit = 20, int? storeId = null);

    /// <summary>
    /// Gets error details for a specific queue item.
    /// </summary>
    /// <param name="queueItemId">The queue item ID.</param>
    /// <returns>Error details.</returns>
    Task<SyncErrorDto?> GetErrorDetailsAsync(int queueItemId);

    /// <summary>
    /// Clears errors for specific items or all items.
    /// </summary>
    /// <param name="queueItemIds">Specific item IDs, or null to clear all.</param>
    /// <returns>Number of errors cleared.</returns>
    Task<int> ClearErrorsAsync(IEnumerable<int>? queueItemIds = null);

    #endregion

    #region Sync Operations

    /// <summary>
    /// Triggers a manual sync operation.
    /// </summary>
    /// <param name="request">The sync request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync result.</returns>
    Task<ManualSyncResultDto> TriggerManualSyncAsync(ManualSyncRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed sync items.
    /// </summary>
    /// <param name="request">The retry request.</param>
    /// <returns>Retry result.</returns>
    Task<RetryResultDto> RetryFailedItemsAsync(RetryFailedItemsRequestDto request);

    /// <summary>
    /// Retries a specific failed item.
    /// </summary>
    /// <param name="queueItemId">The queue item ID.</param>
    /// <returns>True if retry was successful.</returns>
    Task<bool> RetryItemAsync(int queueItemId);

    /// <summary>
    /// Cancels a pending sync item.
    /// </summary>
    /// <param name="queueItemId">The queue item ID.</param>
    /// <returns>True if cancelled successfully.</returns>
    Task<bool> CancelItemAsync(int queueItemId);

    #endregion

    #region Metrics

    /// <summary>
    /// Gets sync performance metrics.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>Metrics data.</returns>
    Task<SyncMetricsDto> GetMetricsAsync(int? storeId = null);

    /// <summary>
    /// Gets recent sync activity.
    /// </summary>
    /// <param name="limit">Maximum number of activities to return.</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <returns>List of recent activities.</returns>
    Task<List<SyncActivityDto>> GetRecentActivityAsync(int limit = 50, int? storeId = null);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<SyncConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    event EventHandler<SyncHealthStatus>? HealthStatusChanged;

    /// <summary>
    /// Event raised when sync progress updates.
    /// </summary>
    event EventHandler<SyncProgressDto>? SyncProgressChanged;

    /// <summary>
    /// Event raised when a sync error occurs.
    /// </summary>
    event EventHandler<SyncErrorDto>? SyncErrorOccurred;

    /// <summary>
    /// Event raised when dashboard data updates.
    /// </summary>
    event EventHandler<SyncStatusDashboardDto>? DashboardUpdated;

    #endregion

    #region Health Calculation

    /// <summary>
    /// Calculates the current health status based on queue state.
    /// </summary>
    /// <param name="summary">Queue summary.</param>
    /// <param name="lastSyncTime">Last sync time.</param>
    /// <param name="isOnline">Whether connected.</param>
    /// <returns>Calculated health status.</returns>
    SyncHealthStatus CalculateHealthStatus(SyncQueueSummaryDto summary, DateTime? lastSyncTime, bool isOnline);

    /// <summary>
    /// Gets a human-readable health message.
    /// </summary>
    /// <param name="health">Health status.</param>
    /// <param name="summary">Queue summary.</param>
    /// <returns>Health message.</returns>
    string GetHealthMessage(SyncHealthStatus health, SyncQueueSummaryDto summary);

    #endregion

    #region Connection Management

    /// <summary>
    /// Checks the connection to the sync server.
    /// </summary>
    /// <returns>True if connected.</returns>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Forces a reconnection attempt.
    /// </summary>
    /// <returns>True if reconnected successfully.</returns>
    Task<bool> ReconnectAsync();

    /// <summary>
    /// Sets the connection state (for testing or manual override).
    /// </summary>
    /// <param name="state">The new connection state.</param>
    void SetConnectionState(SyncConnectionState state);

    #endregion

    #region Background Updates

    /// <summary>
    /// Starts background status updates.
    /// </summary>
    /// <param name="intervalSeconds">Update interval in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartBackgroundUpdatesAsync(int intervalSeconds = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops background status updates.
    /// </summary>
    Task StopBackgroundUpdatesAsync();

    /// <summary>
    /// Forces an immediate status refresh.
    /// </summary>
    Task RefreshStatusAsync();

    #endregion
}
