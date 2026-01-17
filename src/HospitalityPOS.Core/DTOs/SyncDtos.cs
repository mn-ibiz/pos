using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO for sync configuration.
/// </summary>
public class SyncConfigurationDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int SyncIntervalSeconds { get; set; }
    public bool IsEnabled { get; set; }
    public bool AutoSyncOnStartup { get; set; }
    public int MaxBatchSize { get; set; }
    public int RetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public string? LastSyncError { get; set; }
    public List<SyncEntityRuleDto> EntityRules { get; set; } = new();
}

/// <summary>
/// DTO for creating/updating sync configuration.
/// </summary>
public class CreateSyncConfigurationDto
{
    public int StoreId { get; set; }
    public int SyncIntervalSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;
    public bool AutoSyncOnStartup { get; set; } = true;
    public int MaxBatchSize { get; set; } = 100;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 60;
    public List<CreateSyncEntityRuleDto>? EntityRules { get; set; }
}

/// <summary>
/// DTO for sync entity rule.
/// </summary>
public class SyncEntityRuleDto
{
    public int Id { get; set; }
    public SyncEntityType EntityType { get; set; }
    public string EntityTypeName => EntityType.ToString();
    public SyncDirection Direction { get; set; }
    public string DirectionName => Direction.ToString();
    public ConflictWinner ConflictResolution { get; set; }
    public string ConflictResolutionName => ConflictResolution.ToString();
    public bool FlagConflictsForReview { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// DTO for creating sync entity rule.
/// </summary>
public class CreateSyncEntityRuleDto
{
    public SyncEntityType EntityType { get; set; }
    public SyncDirection Direction { get; set; }
    public ConflictWinner ConflictResolution { get; set; } = ConflictWinner.HQ;
    public bool FlagConflictsForReview { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// DTO for sync batch.
/// </summary>
public class SyncBatchDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public SyncDirection Direction { get; set; }
    public string DirectionName => Direction.ToString();
    public SyncEntityType EntityType { get; set; }
    public string EntityTypeName => EntityType.ToString();
    public int RecordCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int ConflictCount { get; set; }
    public SyncBatchStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public double ProgressPercent => RecordCount > 0 ? (double)ProcessedCount / RecordCount * 100 : 0;
}

/// <summary>
/// DTO for sync conflict.
/// </summary>
public class SyncConflictDto
{
    public int Id { get; set; }
    public int SyncBatchId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public string EntityTypeName => EntityType.ToString();
    public int EntityId { get; set; }
    public string LocalData { get; set; } = string.Empty;
    public string RemoteData { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
    public bool IsResolved { get; set; }
    public ConflictWinner? Resolution { get; set; }
    public string? ResolutionName => Resolution?.ToString();
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// DTO for resolving a conflict.
/// </summary>
public class ResolveConflictDto
{
    public int ConflictId { get; set; }
    public ConflictWinner Resolution { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for sync log entry.
/// </summary>
public class SyncLogDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int? SyncBatchId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public long? DurationMs { get; set; }
}

/// <summary>
/// Overall sync status for a store.
/// </summary>
public class StoreSyncStatusDto
{
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public string? LastSyncError { get; set; }
    public int PendingUploadCount { get; set; }
    public int PendingDownloadCount { get; set; }
    public int UnresolvedConflicts { get; set; }
    public SyncBatchDto? CurrentBatch { get; set; }
}

/// <summary>
/// Request to start a sync operation.
/// </summary>
public class StartSyncRequestDto
{
    public int StoreId { get; set; }
    public SyncDirection? Direction { get; set; }
    public List<SyncEntityType>? EntityTypes { get; set; }
    public bool Force { get; set; }
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResultDto
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int BatchId { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsSucceeded { get; set; }
    public int RecordsFailed { get; set; }
    public int ConflictsDetected { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static SyncResultDto Success(int batchId, int processed, int succeeded, int conflicts, DateTime started)
    {
        var completed = DateTime.UtcNow;
        return new SyncResultDto
        {
            IsSuccess = true,
            BatchId = batchId,
            RecordsProcessed = processed,
            RecordsSucceeded = succeeded,
            RecordsFailed = processed - succeeded,
            ConflictsDetected = conflicts,
            StartedAt = started,
            CompletedAt = completed,
            DurationMs = (long)(completed - started).TotalMilliseconds
        };
    }

    public static SyncResultDto Failure(string error, DateTime started)
    {
        var completed = DateTime.UtcNow;
        return new SyncResultDto
        {
            IsSuccess = false,
            ErrorMessage = error,
            StartedAt = started,
            CompletedAt = completed,
            DurationMs = (long)(completed - started).TotalMilliseconds,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Chain-wide sync status dashboard.
/// </summary>
public class ChainSyncDashboardDto
{
    public int TotalStores { get; set; }
    public int StoresOnline { get; set; }
    public int StoresOffline { get; set; }
    public int StoresSyncing { get; set; }
    public int TotalPendingUploads { get; set; }
    public int TotalPendingDownloads { get; set; }
    public int TotalUnresolvedConflicts { get; set; }
    public DateTime? LastChainWideSync { get; set; }
    public List<StoreSyncStatusDto> StoreStatuses { get; set; } = new();
    public List<SyncBatchDto> ActiveBatches { get; set; } = new();
    public List<SyncLogDto> RecentLogs { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data payload for upload sync.
/// </summary>
public class UploadSyncPayloadDto
{
    public int StoreId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public List<SyncRecordDto> Records { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data payload for download sync.
/// </summary>
public class DownloadSyncPayloadDto
{
    public int StoreId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public DateTime? SinceTimestamp { get; set; }
    public List<SyncRecordDto> Records { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual sync record.
/// </summary>
public class SyncRecordDto
{
    public int EntityId { get; set; }
    public string EntityData { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty; // Create, Update, Delete
}

/// <summary>
/// Query for sync logs.
/// </summary>
public class SyncLogQueryDto
{
    public int? StoreId { get; set; }
    public int? SyncBatchId { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Limit { get; set; } = 100;
}

#region Local Database DTOs

/// <summary>
/// Configuration for local database.
/// </summary>
public class LocalDatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "POS_Local";
    public string DataDirectory { get; set; } = string.Empty;
    public bool AutoBackup { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 7;
    public string BackupDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Status of the local database.
/// </summary>
public class DatabaseStatusDto
{
    public bool IsInitialized { get; set; }
    public string Version { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime? LastBackup { get; set; }
    public int PendingSyncItems { get; set; }
    public bool IsOnline { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public DateTime LastMigration { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
}

/// <summary>
/// Result of a database operation.
/// </summary>
public class DatabaseOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;

    public static DatabaseOperationResult Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static DatabaseOperationResult Failed(string message, string? errorDetails = null) =>
        new() { Success = false, Message = message, ErrorDetails = errorDetails };
}

/// <summary>
/// Backup information.
/// </summary>
public class BackupInfoDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
}

#endregion

#region Sync Queue DTOs

/// <summary>
/// Status of sync queue item.
/// </summary>
public enum SyncQueueStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Conflict = 6
}

/// <summary>
/// Priority of sync item.
/// </summary>
public enum SyncPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4  // For eTIMS and compliance-related
}

/// <summary>
/// Type of sync operation.
/// </summary>
public enum SyncOperationType
{
    Create = 1,
    Update = 2,
    Delete = 3
}

/// <summary>
/// Sync queue item.
/// </summary>
public class SyncQueueItemDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public SyncOperationType OperationType { get; set; }
    public SyncPriority Priority { get; set; }
    public SyncQueueStatus Status { get; set; }
    public string? Payload { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Create sync queue item request.
/// </summary>
public class CreateSyncQueueItemDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public SyncOperationType OperationType { get; set; }
    public SyncPriority Priority { get; set; } = SyncPriority.Normal;
    public string? Payload { get; set; }
}

/// <summary>
/// Sync summary for dashboard.
/// </summary>
public class SyncSummaryDto
{
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int FailedCount { get; set; }
    public int CompletedToday { get; set; }
    public int ConflictCount { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public bool IsConnected { get; set; }
    public TimeSpan? TimeSinceLastSync { get; set; }
}

/// <summary>
/// Sync progress update.
/// </summary>
public class SyncProgressDto
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public bool IsComplete { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public decimal ProgressPercent => TotalItems > 0 ? (decimal)ProcessedItems / TotalItems * 100 : 0;
}

#endregion

#region Offline Capability DTOs

/// <summary>
/// Offline operation capability.
/// </summary>
public class OfflineCapabilityDto
{
    public string OperationName { get; set; } = string.Empty;
    public bool IsAvailableOffline { get; set; }
    public bool RequiresSyncOnReconnect { get; set; }
    public string? LimitationsOffline { get; set; }
}

/// <summary>
/// List of operations available offline.
/// </summary>
public static class OfflineOperations
{
    public static readonly List<string> SupportedOperations = new()
    {
        "CreateOrder",
        "SettleReceipt",
        "ProcessCashPayment",
        "PrintReceipt",
        "PrintKOT",
        "ManageInventory",
        "ViewReports",
        "StockAdjustment",
        "OpenWorkPeriod",
        "CloseWorkPeriod",
        "VoidReceipt",
        "ApplyDiscount"
    };

    public static readonly List<string> RequiresOnline = new()
    {
        "ProcessMPesaPayment",
        "SubmitETIMS",
        "CloudBackup",
        "RemoteReporting",
        "MultiStoreSync"
    };
}

#endregion

#region Connection Status DTOs

/// <summary>
/// Connection status.
/// </summary>
public class ConnectionStatusDto
{
    public bool IsConnected { get; set; }
    public string ConnectionType { get; set; } = string.Empty; // Local, Cloud, Hybrid
    public DateTime? LastPingTime { get; set; }
    public int LatencyMs { get; set; }
    public string ServerUrl { get; set; } = string.Empty;
    public bool IsSignalRConnected { get; set; }
}

/// <summary>
/// Full sync dashboard data.
/// </summary>
public class SyncDashboardDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
    public DateTime? LastOnline { get; set; }
    public SyncSummaryDto Summary { get; set; } = new();
    public List<SyncQueueItemDto> RecentFailures { get; set; } = new();
    public List<SyncConflictDto> UnresolvedConflicts { get; set; } = new();
    public SyncStatisticsDto Statistics { get; set; } = new();
    public DatabaseStatusDto DatabaseStatus { get; set; } = new();
    public ConnectionStatusDto ConnectionStatus { get; set; } = new();
}

/// <summary>
/// Sync statistics.
/// </summary>
public class SyncStatisticsDto
{
    public int TotalSyncedToday { get; set; }
    public int TotalSyncedThisWeek { get; set; }
    public int TotalSyncedThisMonth { get; set; }
    public decimal AverageSyncTimeMs { get; set; }
    public int FailureRatePercent { get; set; }
    public Dictionary<string, int> SyncByEntityType { get; set; } = new();
}

#endregion

#region SignalR Sync DTOs

/// <summary>
/// Configuration for SignalR sync service.
/// </summary>
public class SignalRConfiguration
{
    public string HubUrl { get; set; } = string.Empty;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectAttempts { get; set; } = 10;
    public int KeepAliveIntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 50;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public bool AutoReconnect { get; set; } = true;
    public string? AccessToken { get; set; }
}

/// <summary>
/// SignalR connection state.
/// </summary>
public enum SignalRConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3
}

/// <summary>
/// SignalR connection status event args.
/// </summary>
public class SignalRConnectionStatusDto
{
    public SignalRConnectionState State { get; set; }
    public bool IsConnected => State == SignalRConnectionState.Connected;
    public DateTime? LastConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public int ReconnectAttempts { get; set; }
    public string? LastError { get; set; }
    public string ServerUrl { get; set; } = string.Empty;
    public TimeSpan? ConnectionDuration => LastConnectedAt.HasValue && IsConnected
        ? DateTime.UtcNow - LastConnectedAt.Value
        : null;
}

/// <summary>
/// Sync item received from SignalR hub.
/// </summary>
public class SignalRSyncItemDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty; // Create, Update, Delete
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int StoreId { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Batch sync request.
/// </summary>
public class SignalRBatchSyncRequestDto
{
    public int StoreId { get; set; }
    public DateTime? SinceTimestamp { get; set; }
    public List<string>? EntityTypes { get; set; }
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Batch sync response.
/// </summary>
public class SignalRBatchSyncResponseDto
{
    public bool Success { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public List<SignalRSyncItemDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public DateTime? NextCursor { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Sync acknowledgment.
/// </summary>
public class SignalRSyncAckDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Progress update for batch sync.
/// </summary>
public class SignalRSyncProgressDto
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public decimal ProgressPercent => TotalItems > 0
        ? (decimal)CompletedItems / TotalItems * 100 : 0;
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public bool IsComplete => CompletedItems + FailedItems >= TotalItems;
}

/// <summary>
/// Heartbeat message for connection health.
/// </summary>
public class SignalRHeartbeatDto
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int StoreId { get; set; }
    public int PendingSyncCount { get; set; }
    public string ClientVersion { get; set; } = string.Empty;
}

#endregion

#region Conflict Resolution DTOs

/// <summary>
/// Status of a sync conflict.
/// </summary>
public enum ConflictStatus
{
    /// <summary>Conflict has been detected but not processed.</summary>
    Detected = 1,
    /// <summary>Conflict was automatically resolved by rules.</summary>
    AutoResolved = 2,
    /// <summary>Conflict requires manual resolution.</summary>
    PendingManual = 3,
    /// <summary>Conflict has been manually resolved.</summary>
    Resolved = 4,
    /// <summary>Conflict was ignored/skipped.</summary>
    Ignored = 5
}

/// <summary>
/// Type of conflict resolution to apply.
/// </summary>
public enum ConflictResolutionType
{
    /// <summary>Local (store) version wins.</summary>
    LocalWins = 1,
    /// <summary>Remote (HQ) version wins.</summary>
    RemoteWins = 2,
    /// <summary>Most recently modified version wins.</summary>
    LastWriteWins = 3,
    /// <summary>Values are merged from both versions.</summary>
    Merged = 4,
    /// <summary>Requires manual resolution.</summary>
    Manual = 5
}

/// <summary>
/// Rule for resolving conflicts for a specific entity/property.
/// </summary>
public class ConflictResolutionRuleDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }  // null = entire entity
    public ConflictResolutionType DefaultResolution { get; set; }
    public bool RequireManualReview { get; set; }
    public int Priority { get; set; } = 100;  // Lower = higher priority
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Default conflict resolution rules.
/// </summary>
public static class DefaultConflictRules
{
    public static readonly List<ConflictResolutionRuleDto> Rules = new()
    {
        // Receipts: Store's local data wins (financial integrity)
        new() { EntityType = "Receipt", DefaultResolution = ConflictResolutionType.LocalWins, Priority = 10, Description = "Receipts always use local data for financial integrity" },

        // Prices: HQ wins (pricing consistency across chain)
        new() { EntityType = "Product", PropertyName = "Price", DefaultResolution = ConflictResolutionType.RemoteWins, Priority = 20, Description = "HQ controls pricing" },
        new() { EntityType = "Product", PropertyName = "CostPrice", DefaultResolution = ConflictResolutionType.RemoteWins, Priority = 20, Description = "HQ controls cost prices" },

        // Products: HQ wins for master data
        new() { EntityType = "Product", DefaultResolution = ConflictResolutionType.RemoteWins, Priority = 30, Description = "HQ controls product master data" },

        // Categories: HQ wins
        new() { EntityType = "Category", DefaultResolution = ConflictResolutionType.RemoteWins, Priority = 30, Description = "HQ controls categories" },

        // Inventory: Last write wins (most recent stock level)
        new() { EntityType = "Inventory", DefaultResolution = ConflictResolutionType.LastWriteWins, Priority = 40, Description = "Latest inventory count used" },
        new() { EntityType = "StockMovement", DefaultResolution = ConflictResolutionType.LocalWins, Priority = 40, Description = "Stock movements from local store" },

        // Customer points: Requires manual review to prevent fraud
        new() { EntityType = "Customer", PropertyName = "PointsBalance", DefaultResolution = ConflictResolutionType.Manual, RequireManualReview = true, Priority = 50, Description = "Points changes require manual review" },
        new() { EntityType = "LoyaltyMember", PropertyName = "Points", DefaultResolution = ConflictResolutionType.Manual, RequireManualReview = true, Priority = 50, Description = "Loyalty points require manual review" },

        // Orders: Local wins (transaction integrity)
        new() { EntityType = "Order", DefaultResolution = ConflictResolutionType.LocalWins, Priority = 10, Description = "Orders use local data for transaction integrity" }
    };
}

/// <summary>
/// Extended conflict DTO with resolution details.
/// </summary>
public class ConflictDetailDto
{
    public int Id { get; set; }
    public int SyncBatchId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string LocalData { get; set; } = string.Empty;
    public string RemoteData { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
    public ConflictStatus Status { get; set; }
    public ConflictResolutionType? AppliedResolution { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByUserName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? ConflictingFields { get; set; }  // JSON array of field names
    public ConflictResolutionRuleDto? ApplicableRule { get; set; }
}

/// <summary>
/// Request to manually resolve a conflict.
/// </summary>
public class ManualResolveConflictDto
{
    public int ConflictId { get; set; }
    public ConflictResolutionType Resolution { get; set; }
    public string? Notes { get; set; }
    public int ResolvedByUserId { get; set; }
    public string? MergedData { get; set; }  // JSON for merged resolution
}

/// <summary>
/// Result of a conflict resolution attempt.
/// </summary>
public class ConflictResolutionResultDto
{
    public bool Success { get; set; }
    public int ConflictId { get; set; }
    public ConflictResolutionType AppliedResolution { get; set; }
    public ConflictStatus NewStatus { get; set; }
    public string? ResultingData { get; set; }  // The data that was applied
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public bool WasAutoResolved { get; set; }

    public static ConflictResolutionResultDto Succeeded(int conflictId, ConflictResolutionType resolution, string? resultingData, bool autoResolved) =>
        new()
        {
            Success = true,
            ConflictId = conflictId,
            AppliedResolution = resolution,
            NewStatus = autoResolved ? ConflictStatus.AutoResolved : ConflictStatus.Resolved,
            ResultingData = resultingData,
            WasAutoResolved = autoResolved
        };

    public static ConflictResolutionResultDto Failed(int conflictId, string errorMessage) =>
        new()
        {
            Success = false,
            ConflictId = conflictId,
            ErrorMessage = errorMessage,
            NewStatus = ConflictStatus.PendingManual
        };
}

/// <summary>
/// Summary of conflicts for dashboard.
/// </summary>
public class ConflictSummaryDto
{
    public int TotalConflicts { get; set; }
    public int PendingManual { get; set; }
    public int AutoResolved { get; set; }
    public int ManuallyResolved { get; set; }
    public int Ignored { get; set; }
    public Dictionary<string, int> ByEntityType { get; set; } = new();
    public List<ConflictDetailDto> RecentConflicts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Query for filtering conflicts.
/// </summary>
public class ConflictQueryDto
{
    public ConflictStatus? Status { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? StoreId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public bool IncludeResolved { get; set; } = false;
}

/// <summary>
/// Conflict audit log entry.
/// </summary>
public class ConflictAuditDto
{
    public int Id { get; set; }
    public int ConflictId { get; set; }
    public string Action { get; set; } = string.Empty;  // Detected, AutoResolved, ManualResolved, Ignored
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request to create a new conflict resolution rule.
/// </summary>
public class CreateConflictRuleDto
{
    public string EntityType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public ConflictResolutionType DefaultResolution { get; set; }
    public bool RequireManualReview { get; set; }
    public int Priority { get; set; } = 100;
    public string? Description { get; set; }
}

#endregion

#region Sync Status Dashboard DTOs

/// <summary>
/// Overall connection state for sync.
/// </summary>
public enum SyncConnectionState
{
    /// <summary>Connected to cloud and syncing normally.</summary>
    Online = 1,
    /// <summary>No network connection, operating in offline mode.</summary>
    Offline = 2,
    /// <summary>Attempting to establish connection.</summary>
    Connecting = 3,
    /// <summary>Currently syncing data.</summary>
    Syncing = 4,
    /// <summary>Connection error state.</summary>
    Error = 5
}

/// <summary>
/// Overall health status of sync system.
/// </summary>
public enum SyncHealthStatus
{
    /// <summary>All synced, no errors, connected.</summary>
    Healthy = 1,
    /// <summary>Some items pending but no errors.</summary>
    Warning = 2,
    /// <summary>Many pending items or some errors.</summary>
    Degraded = 3,
    /// <summary>Many errors, long time since sync, or critical items pending.</summary>
    Critical = 4
}

/// <summary>
/// Complete sync status dashboard data.
/// </summary>
public class SyncStatusDashboardDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public SyncConnectionState ConnectionState { get; set; }
    public SyncHealthStatus HealthStatus { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public TimeSpan? TimeSinceLastSync => LastSyncTime.HasValue
        ? DateTime.UtcNow - LastSyncTime.Value
        : null;
    public bool IsOnline => ConnectionState == SyncConnectionState.Online || ConnectionState == SyncConnectionState.Syncing;
    public bool IsSyncing => ConnectionState == SyncConnectionState.Syncing;
    public SyncQueueSummaryDto QueueSummary { get; set; } = new();
    public List<SyncErrorDto> RecentErrors { get; set; } = new();
    public ConflictSummaryDto ConflictSummary { get; set; } = new();
    public SyncMetricsDto Metrics { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string HealthMessage { get; set; } = string.Empty;
}

/// <summary>
/// Summary of sync queue status.
/// </summary>
public class SyncQueueSummaryDto
{
    public int TotalPending { get; set; }
    public int CriticalPending { get; set; }  // eTIMS and compliance items
    public int HighPending { get; set; }
    public int NormalPending { get; set; }
    public int LowPending { get; set; }
    public int FailedItems { get; set; }
    public int ItemsSyncedToday { get; set; }
    public int ItemsSyncedThisHour { get; set; }
    public DateTime? OldestPendingItem { get; set; }
    public TimeSpan? OldestPendingAge => OldestPendingItem.HasValue
        ? DateTime.UtcNow - OldestPendingItem.Value
        : null;

    public bool HasCriticalItems => CriticalPending > 0;
    public bool HasFailures => FailedItems > 0;
}

/// <summary>
/// Sync error details.
/// </summary>
public class SyncErrorDto
{
    public int QueueItemId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime OccurredAt { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public bool CanRetry => AttemptCount < MaxAttempts;
    public DateTime? NextRetryAt { get; set; }
    public SyncPriority Priority { get; set; }
}

/// <summary>
/// Sync performance metrics.
/// </summary>
public class SyncMetricsDto
{
    public int TotalSyncedToday { get; set; }
    public int TotalSyncedThisWeek { get; set; }
    public int TotalSyncedThisMonth { get; set; }
    public double AverageSyncTimeMs { get; set; }
    public double AverageItemsPerMinute { get; set; }
    public int SuccessRatePercent { get; set; }
    public int FailureRatePercent => 100 - SuccessRatePercent;
    public long TotalDataSyncedBytes { get; set; }
    public Dictionary<string, int> SyncCountByEntityType { get; set; } = new();
    public Dictionary<string, int> ErrorCountByEntityType { get; set; } = new();
    public List<SyncActivityDto> RecentActivity { get; set; } = new();
}

/// <summary>
/// Recent sync activity entry.
/// </summary>
public class SyncActivityDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Request to trigger manual sync.
/// </summary>
public class ManualSyncRequestDto
{
    public int StoreId { get; set; }
    public bool SyncAll { get; set; } = true;
    public bool IncludeFailedItems { get; set; } = true;
    public List<string>? EntityTypes { get; set; }
    public SyncPriority? MinPriority { get; set; }
}

/// <summary>
/// Result of manual sync operation.
/// </summary>
public class ManualSyncResultDto
{
    public bool Success { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public int TotalItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Summary { get; set; }

    public static ManualSyncResultDto Succeeded(DateTime started, int total, int successful, int failed)
    {
        var completed = DateTime.UtcNow;
        return new ManualSyncResultDto
        {
            Success = failed == 0,
            StartedAt = started,
            CompletedAt = completed,
            TotalItems = total,
            SuccessfulItems = successful,
            FailedItems = failed,
            Summary = $"Synced {successful}/{total} items in {(completed - started).TotalSeconds:F1}s"
        };
    }

    public static ManualSyncResultDto Failed(DateTime started, string error)
    {
        return new ManualSyncResultDto
        {
            Success = false,
            StartedAt = started,
            CompletedAt = DateTime.UtcNow,
            Errors = new List<string> { error },
            Summary = $"Sync failed: {error}"
        };
    }
}

/// <summary>
/// Sync status bar data for compact display.
/// </summary>
public class SyncStatusBarDto
{
    public SyncConnectionState State { get; set; }
    public SyncHealthStatus Health { get; set; }
    public int PendingCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime? LastSync { get; set; }
    public bool IsSyncing { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;  // Icon name/path

    public string GetStatusText()
    {
        if (IsSyncing)
            return "Syncing...";
        if (State == SyncConnectionState.Offline)
            return "Offline";
        if (ErrorCount > 0)
            return $"{ErrorCount} errors";
        if (PendingCount > 0)
            return $"{PendingCount} pending";
        return "Up to date";
    }

    public string GetHealthColor()
    {
        return Health switch
        {
            SyncHealthStatus.Healthy => "#22C55E",   // Green
            SyncHealthStatus.Warning => "#EAB308",   // Yellow
            SyncHealthStatus.Degraded => "#F97316", // Orange
            SyncHealthStatus.Critical => "#EF4444", // Red
            _ => "#6B7280"  // Gray
        };
    }
}

/// <summary>
/// Retry failed items request.
/// </summary>
public class RetryFailedItemsRequestDto
{
    public int StoreId { get; set; }
    public List<int>? SpecificItemIds { get; set; }
    public bool RetryAll { get; set; } = true;
    public string? EntityType { get; set; }
}

/// <summary>
/// Result of retry operation.
/// </summary>
public class RetryResultDto
{
    public bool Success { get; set; }
    public int ItemsRetried { get; set; }
    public int ItemsSucceeded { get; set; }
    public int ItemsStillFailing { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
