using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for sync status dashboard and monitoring.
/// </summary>
public class SyncStatusService : ISyncStatusService, IDisposable
{
    private readonly ISyncQueueService _syncQueueService;
    private readonly ISyncHubService? _syncHubService;
    private readonly IConflictResolutionService? _conflictResolutionService;
    private readonly IRepository<SyncQueueItem> _queueRepository;
    private readonly IRepository<SyncLog> _logRepository;
    private readonly ILogger<SyncStatusService> _logger;

    private SyncConnectionState _connectionState = SyncConnectionState.Offline;
    private SyncHealthStatus _healthStatus = SyncHealthStatus.Warning;
    private DateTime? _lastSyncTime;
    private bool _isSyncing;
    private CancellationTokenSource? _backgroundUpdateCts;
    private readonly List<SyncActivityDto> _recentActivity = new();
    private readonly object _stateLock = new();

    public SyncStatusService(
        ISyncQueueService syncQueueService,
        IRepository<SyncQueueItem> queueRepository,
        IRepository<SyncLog> logRepository,
        ILogger<SyncStatusService> logger,
        ISyncHubService? syncHubService = null,
        IConflictResolutionService? conflictResolutionService = null)
    {
        _syncQueueService = syncQueueService ?? throw new ArgumentNullException(nameof(syncQueueService));
        _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncHubService = syncHubService;
        _conflictResolutionService = conflictResolutionService;

        // Subscribe to sync hub events if available
        if (_syncHubService != null)
        {
            _syncHubService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _syncHubService.SyncProgressChanged += OnSyncProgressChanged;
        }
    }

    #region Properties

    public SyncConnectionState ConnectionState
    {
        get
        {
            lock (_stateLock)
            {
                return _connectionState;
            }
        }
    }

    public SyncHealthStatus HealthStatus
    {
        get
        {
            lock (_stateLock)
            {
                return _healthStatus;
            }
        }
    }

    public bool IsOnline => ConnectionState == SyncConnectionState.Online || ConnectionState == SyncConnectionState.Syncing;

    public bool IsSyncing
    {
        get
        {
            lock (_stateLock)
            {
                return _isSyncing;
            }
        }
    }

    public DateTime? LastSyncTime
    {
        get
        {
            lock (_stateLock)
            {
                return _lastSyncTime;
            }
        }
    }

    #endregion

    #region Events

    public event EventHandler<SyncConnectionState>? ConnectionStateChanged;
    public event EventHandler<SyncHealthStatus>? HealthStatusChanged;
    public event EventHandler<SyncProgressDto>? SyncProgressChanged;
    public event EventHandler<SyncErrorDto>? SyncErrorOccurred;
    public event EventHandler<SyncStatusDashboardDto>? DashboardUpdated;

    #endregion

    #region Dashboard

    public async Task<SyncStatusDashboardDto> GetDashboardAsync(int? storeId = null)
    {
        var queueSummary = await GetQueueSummaryAsync(storeId);
        var recentErrors = await GetRecentErrorsAsync(10, storeId);
        var metrics = await GetMetricsAsync(storeId);

        var dashboard = new SyncStatusDashboardDto
        {
            StoreId = storeId ?? 0,
            ConnectionState = ConnectionState,
            HealthStatus = CalculateHealthStatus(queueSummary, LastSyncTime, IsOnline),
            LastSyncTime = LastSyncTime,
            QueueSummary = queueSummary,
            RecentErrors = recentErrors,
            Metrics = metrics
        };

        // Get conflict summary if available
        if (_conflictResolutionService != null)
        {
            dashboard.ConflictSummary = await _conflictResolutionService.GetConflictSummaryAsync(storeId);
        }

        dashboard.HealthMessage = GetHealthMessage(dashboard.HealthStatus, queueSummary);

        lock (_stateLock)
        {
            _healthStatus = dashboard.HealthStatus;
        }

        return dashboard;
    }

    public async Task<SyncStatusBarDto> GetStatusBarAsync(int? storeId = null)
    {
        var queueSummary = await GetQueueSummaryAsync(storeId);

        var statusBar = new SyncStatusBarDto
        {
            State = ConnectionState,
            Health = CalculateHealthStatus(queueSummary, LastSyncTime, IsOnline),
            PendingCount = queueSummary.TotalPending,
            ErrorCount = queueSummary.FailedItems,
            LastSync = LastSyncTime,
            IsSyncing = IsSyncing
        };

        statusBar.StatusText = statusBar.GetStatusText();

        return statusBar;
    }

    #endregion

    #region Queue Status

    public async Task<SyncQueueSummaryDto> GetQueueSummaryAsync(int? storeId = null)
    {
        var items = await _queueRepository.FindAsync(q =>
            q.IsActive &&
            (storeId == null || q.StoreId == storeId));

        var itemList = items.ToList();
        var today = DateTime.UtcNow.Date;
        var thisHour = DateTime.UtcNow.AddHours(-1);

        var summary = new SyncQueueSummaryDto
        {
            TotalPending = itemList.Count(q => q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress),
            CriticalPending = itemList.Count(q => q.Priority == SyncQueuePriority.Critical &&
                (q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress)),
            HighPending = itemList.Count(q => q.Priority == SyncQueuePriority.High &&
                (q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress)),
            NormalPending = itemList.Count(q => q.Priority == SyncQueuePriority.Normal &&
                (q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress)),
            LowPending = itemList.Count(q => q.Priority == SyncQueuePriority.Low &&
                (q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress)),
            FailedItems = itemList.Count(q => q.Status == SyncQueueItemStatus.Failed),
            ItemsSyncedToday = itemList.Count(q => q.Status == SyncQueueItemStatus.Completed && q.UpdatedAt >= today),
            ItemsSyncedThisHour = itemList.Count(q => q.Status == SyncQueueItemStatus.Completed && q.UpdatedAt >= thisHour)
        };

        // Find oldest pending item
        var oldestPending = itemList
            .Where(q => q.Status == SyncQueueItemStatus.Pending)
            .OrderBy(q => q.CreatedAt)
            .FirstOrDefault();

        summary.OldestPendingItem = oldestPending?.CreatedAt;

        return summary;
    }

    public async Task<int> GetPendingCountAsync(int? storeId = null)
    {
        var items = await _queueRepository.FindAsync(q =>
            q.IsActive &&
            (q.Status == SyncQueueItemStatus.Pending || q.Status == SyncQueueItemStatus.InProgress) &&
            (storeId == null || q.StoreId == storeId));

        return items.Count();
    }

    public async Task<int> GetFailedCountAsync(int? storeId = null)
    {
        var items = await _queueRepository.FindAsync(q =>
            q.IsActive &&
            q.Status == SyncQueueItemStatus.Failed &&
            (storeId == null || q.StoreId == storeId));

        return items.Count();
    }

    #endregion

    #region Errors

    public async Task<List<SyncErrorDto>> GetRecentErrorsAsync(int limit = 20, int? storeId = null)
    {
        var failedItems = await _queueRepository.FindAsync(q =>
            q.IsActive &&
            q.Status == SyncQueueItemStatus.Failed &&
            (storeId == null || q.StoreId == storeId));

        return failedItems
            .OrderByDescending(q => q.LastAttemptAt ?? q.UpdatedAt)
            .Take(limit)
            .Select(MapToErrorDto)
            .ToList();
    }

    public async Task<SyncErrorDto?> GetErrorDetailsAsync(int queueItemId)
    {
        var item = await _queueRepository.GetByIdAsync(queueItemId);
        return item != null ? MapToErrorDto(item) : null;
    }

    public async Task<int> ClearErrorsAsync(IEnumerable<int>? queueItemIds = null)
    {
        var failedItems = await _queueRepository.FindAsync(q =>
            q.IsActive && q.Status == SyncQueueItemStatus.Failed);

        var items = queueItemIds != null
            ? failedItems.Where(q => queueItemIds.Contains(q.Id))
            : failedItems;

        var count = 0;
        foreach (var item in items)
        {
            item.Status = SyncQueueItemStatus.Cancelled;
            item.UpdatedAt = DateTime.UtcNow;
            count++;
        }

        _logger.LogInformation("Cleared {Count} sync errors", count);
        return count;
    }

    #endregion

    #region Sync Operations

    public async Task<ManualSyncResultDto> TriggerManualSyncAsync(ManualSyncRequestDto request, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;

        lock (_stateLock)
        {
            if (_isSyncing)
            {
                return ManualSyncResultDto.Failed(started, "Sync already in progress");
            }
            _isSyncing = true;
            _connectionState = SyncConnectionState.Syncing;
        }

        ConnectionStateChanged?.Invoke(this, SyncConnectionState.Syncing);

        try
        {
            _logger.LogInformation("Starting manual sync for store {StoreId}", request.StoreId);

            // Process sync queue
            var result = await _syncQueueService.ProcessQueueAsync(100, cancellationToken);

            lock (_stateLock)
            {
                _lastSyncTime = DateTime.UtcNow;
                _isSyncing = false;
                _connectionState = SyncConnectionState.Online;
            }

            ConnectionStateChanged?.Invoke(this, SyncConnectionState.Online);

            // Record activity
            RecordActivity("ManualSync", "System", 0, result.SuccessCount > 0,
                $"Processed {result.TotalProcessed} items", (long)(DateTime.UtcNow - started).TotalMilliseconds);

            return ManualSyncResultDto.Succeeded(started, result.TotalProcessed, result.SuccessCount, result.FailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed");

            lock (_stateLock)
            {
                _isSyncing = false;
                _connectionState = SyncConnectionState.Error;
            }

            ConnectionStateChanged?.Invoke(this, SyncConnectionState.Error);

            return ManualSyncResultDto.Failed(started, ex.Message);
        }
    }

    public async Task<RetryResultDto> RetryFailedItemsAsync(RetryFailedItemsRequestDto request)
    {
        var result = new RetryResultDto();

        try
        {
            int retryCount;

            if (request.SpecificItemIds?.Any() == true)
            {
                foreach (var itemId in request.SpecificItemIds)
                {
                    var success = await RetryItemAsync(itemId);
                    if (success)
                        result.ItemsSucceeded++;
                    else
                        result.ItemsStillFailing++;
                    result.ItemsRetried++;
                }
            }
            else
            {
                retryCount = await _syncQueueService.RetryFailedItemsAsync();
                result.ItemsRetried = retryCount;
                result.ItemsSucceeded = retryCount;  // Assume success for now
            }

            result.Success = result.ItemsStillFailing == 0;
            _logger.LogInformation("Retried {Count} failed items, {Succeeded} succeeded",
                result.ItemsRetried, result.ItemsSucceeded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry items");
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<bool> RetryItemAsync(int queueItemId)
    {
        try
        {
            var item = await _queueRepository.GetByIdAsync(queueItemId);
            if (item == null || item.Status != SyncQueueItemStatus.Failed)
            {
                return false;
            }

            item.Status = SyncQueueItemStatus.Pending;
            item.RetryCount = 0;
            item.NextRetryAt = null;
            item.LastError = null;
            item.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Queued item {ItemId} for retry", queueItemId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry item {ItemId}", queueItemId);
            return false;
        }
    }

    public async Task<bool> CancelItemAsync(int queueItemId)
    {
        try
        {
            var item = await _queueRepository.GetByIdAsync(queueItemId);
            if (item == null)
            {
                return false;
            }

            if (item.Status == SyncQueueItemStatus.InProgress)
            {
                _logger.LogWarning("Cannot cancel in-progress item {ItemId}", queueItemId);
                return false;
            }

            item.Status = SyncQueueItemStatus.Cancelled;
            item.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Cancelled sync item {ItemId}", queueItemId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel item {ItemId}", queueItemId);
            return false;
        }
    }

    #endregion

    #region Metrics

    public async Task<SyncMetricsDto> GetMetricsAsync(int? storeId = null)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var monthAgo = DateTime.UtcNow.AddMonths(-1);

        var items = await _queueRepository.FindAsync(q => q.IsActive && (storeId == null || q.StoreId == storeId));
        var itemList = items.ToList();

        var completedItems = itemList.Where(q => q.Status == SyncQueueItemStatus.Completed).ToList();
        var failedItems = itemList.Where(q => q.Status == SyncQueueItemStatus.Failed).ToList();

        var totalCompleted = completedItems.Count;
        var totalFailed = failedItems.Count;
        var total = totalCompleted + totalFailed;

        var metrics = new SyncMetricsDto
        {
            TotalSyncedToday = completedItems.Count(q => q.UpdatedAt >= today),
            TotalSyncedThisWeek = completedItems.Count(q => q.UpdatedAt >= weekAgo),
            TotalSyncedThisMonth = completedItems.Count(q => q.UpdatedAt >= monthAgo),
            SuccessRatePercent = total > 0 ? (int)((double)totalCompleted / total * 100) : 100,
            SyncCountByEntityType = completedItems
                .GroupBy(q => q.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorCountByEntityType = failedItems
                .GroupBy(q => q.EntityType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        lock (_stateLock)
        {
            metrics.RecentActivity = _recentActivity.Take(20).ToList();
        }

        return metrics;
    }

    public Task<List<SyncActivityDto>> GetRecentActivityAsync(int limit = 50, int? storeId = null)
    {
        lock (_stateLock)
        {
            return Task.FromResult(_recentActivity.Take(limit).ToList());
        }
    }

    #endregion

    #region Health Calculation

    public SyncHealthStatus CalculateHealthStatus(SyncQueueSummaryDto summary, DateTime? lastSyncTime, bool isOnline)
    {
        // Critical: Many errors, critical items pending, or very long time since sync
        if (summary.CriticalPending > 0)
            return SyncHealthStatus.Critical;

        if (summary.FailedItems > 10)
            return SyncHealthStatus.Critical;

        if (lastSyncTime.HasValue && (DateTime.UtcNow - lastSyncTime.Value).TotalHours > 24)
            return SyncHealthStatus.Critical;

        // Degraded: Some errors or many pending items
        if (summary.FailedItems > 0)
            return SyncHealthStatus.Degraded;

        if (summary.TotalPending > 100)
            return SyncHealthStatus.Degraded;

        if (!isOnline && summary.TotalPending > 0)
            return SyncHealthStatus.Degraded;

        // Warning: Some pending items
        if (summary.TotalPending > 0)
            return SyncHealthStatus.Warning;

        if (lastSyncTime.HasValue && (DateTime.UtcNow - lastSyncTime.Value).TotalHours > 1)
            return SyncHealthStatus.Warning;

        // Healthy: All synced, online, no errors
        return SyncHealthStatus.Healthy;
    }

    public string GetHealthMessage(SyncHealthStatus health, SyncQueueSummaryDto summary)
    {
        return health switch
        {
            SyncHealthStatus.Critical when summary.CriticalPending > 0 =>
                $"Critical: {summary.CriticalPending} critical items pending sync",
            SyncHealthStatus.Critical when summary.FailedItems > 10 =>
                $"Critical: {summary.FailedItems} sync failures need attention",
            SyncHealthStatus.Critical =>
                "Critical: Sync has not completed in over 24 hours",
            SyncHealthStatus.Degraded when summary.FailedItems > 0 =>
                $"Degraded: {summary.FailedItems} items failed to sync",
            SyncHealthStatus.Degraded when summary.TotalPending > 100 =>
                $"Degraded: {summary.TotalPending} items waiting to sync",
            SyncHealthStatus.Degraded =>
                "Degraded: Offline with pending items",
            SyncHealthStatus.Warning when summary.TotalPending > 0 =>
                $"Warning: {summary.TotalPending} items pending sync",
            SyncHealthStatus.Warning =>
                "Warning: No recent sync activity",
            SyncHealthStatus.Healthy =>
                "All data synced successfully",
            _ => "Status unknown"
        };
    }

    #endregion

    #region Connection Management

    public async Task<bool> CheckConnectionAsync()
    {
        if (_syncHubService == null)
        {
            _logger.LogDebug("No sync hub service configured, assuming offline");
            return false;
        }

        return _syncHubService.IsConnected;
    }

    public async Task<bool> ReconnectAsync()
    {
        if (_syncHubService == null)
        {
            _logger.LogWarning("Cannot reconnect - no sync hub service configured");
            return false;
        }

        lock (_stateLock)
        {
            _connectionState = SyncConnectionState.Connecting;
        }

        ConnectionStateChanged?.Invoke(this, SyncConnectionState.Connecting);

        try
        {
            var result = await _syncHubService.ConnectAsync();

            lock (_stateLock)
            {
                _connectionState = result ? SyncConnectionState.Online : SyncConnectionState.Error;
            }

            ConnectionStateChanged?.Invoke(this, ConnectionState);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconnection failed");

            lock (_stateLock)
            {
                _connectionState = SyncConnectionState.Error;
            }

            ConnectionStateChanged?.Invoke(this, SyncConnectionState.Error);

            return false;
        }
    }

    public void SetConnectionState(SyncConnectionState state)
    {
        lock (_stateLock)
        {
            if (_connectionState != state)
            {
                _connectionState = state;
                ConnectionStateChanged?.Invoke(this, state);
            }
        }
    }

    #endregion

    #region Background Updates

    public async Task StartBackgroundUpdatesAsync(int intervalSeconds = 10, CancellationToken cancellationToken = default)
    {
        await StopBackgroundUpdatesAsync();

        _backgroundUpdateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _backgroundUpdateCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshStatusAsync();
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background status update failed");
                }
            }
        }, token);

        _logger.LogInformation("Started background status updates every {Seconds}s", intervalSeconds);
    }

    public Task StopBackgroundUpdatesAsync()
    {
        if (_backgroundUpdateCts != null)
        {
            _backgroundUpdateCts.Cancel();
            _backgroundUpdateCts.Dispose();
            _backgroundUpdateCts = null;
            _logger.LogInformation("Stopped background status updates");
        }

        return Task.CompletedTask;
    }

    public async Task RefreshStatusAsync()
    {
        try
        {
            var dashboard = await GetDashboardAsync();

            var oldHealth = HealthStatus;
            lock (_stateLock)
            {
                _healthStatus = dashboard.HealthStatus;
            }

            if (oldHealth != dashboard.HealthStatus)
            {
                HealthStatusChanged?.Invoke(this, dashboard.HealthStatus);
            }

            DashboardUpdated?.Invoke(this, dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Status refresh failed");
        }
    }

    #endregion

    #region Event Handlers

    private void OnConnectionStatusChanged(object? sender, SignalRConnectionStatusDto status)
    {
        var newState = status.State switch
        {
            SignalRConnectionState.Connected => SyncConnectionState.Online,
            SignalRConnectionState.Connecting => SyncConnectionState.Connecting,
            SignalRConnectionState.Reconnecting => SyncConnectionState.Connecting,
            SignalRConnectionState.Disconnected => SyncConnectionState.Offline,
            _ => SyncConnectionState.Offline
        };

        SetConnectionState(newState);
    }

    private void OnSyncProgressChanged(object? sender, SignalRSyncProgressDto progress)
    {
        var syncProgress = new SyncProgressDto
        {
            TotalItems = progress.TotalItems,
            ProcessedItems = progress.CompletedItems,
            SuccessfulItems = progress.CompletedItems - progress.FailedItems,
            FailedItems = progress.FailedItems,
            IsComplete = progress.IsComplete,
            CurrentItem = progress.CurrentItem
        };

        SyncProgressChanged?.Invoke(this, syncProgress);

        if (progress.IsComplete)
        {
            lock (_stateLock)
            {
                _lastSyncTime = DateTime.UtcNow;
                _isSyncing = false;
            }
        }
    }

    #endregion

    #region Private Helpers

    private static SyncErrorDto MapToErrorDto(SyncQueueItem item)
    {
        return new SyncErrorDto
        {
            QueueItemId = item.Id,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            Operation = item.OperationType.ToString(),
            ErrorMessage = item.LastError ?? "Unknown error",
            OccurredAt = item.LastAttemptAt ?? item.UpdatedAt,
            AttemptCount = item.RetryCount,
            MaxAttempts = item.MaxRetries,
            NextRetryAt = item.NextRetryAt,
            Priority = (SyncPriority)item.Priority
        };
    }

    private void RecordActivity(string action, string entityType, int entityId, bool success, string? message, long durationMs)
    {
        var activity = new SyncActivityDto
        {
            Timestamp = DateTime.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Success = success,
            Message = message,
            DurationMs = durationMs
        };

        lock (_stateLock)
        {
            _recentActivity.Insert(0, activity);
            if (_recentActivity.Count > 100)
            {
                _recentActivity.RemoveRange(100, _recentActivity.Count - 100);
            }
        }
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Cancel background updates without blocking
            if (_backgroundUpdateCts != null)
            {
                _backgroundUpdateCts.Cancel();
                _backgroundUpdateCts.Dispose();
                _backgroundUpdateCts = null;
            }

            // Unsubscribe from events
            if (_syncHubService != null)
            {
                _syncHubService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _syncHubService.SyncProgressChanged -= OnSyncProgressChanged;
            }
        }

        _disposed = true;
    }

    ~SyncStatusService()
    {
        Dispose(false);
    }

    #endregion
}
