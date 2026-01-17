using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for store data synchronization between stores and HQ.
/// </summary>
public interface IStoreSyncService
{
    #region Sync Configuration Management

    /// <summary>
    /// Gets sync configuration for a store.
    /// </summary>
    Task<SyncConfigurationDto?> GetSyncConfigurationAsync(int storeId);

    /// <summary>
    /// Gets all sync configurations.
    /// </summary>
    Task<List<SyncConfigurationDto>> GetAllSyncConfigurationsAsync();

    /// <summary>
    /// Creates a new sync configuration for a store.
    /// </summary>
    Task<SyncConfigurationDto> CreateSyncConfigurationAsync(CreateSyncConfigurationDto dto);

    /// <summary>
    /// Updates an existing sync configuration.
    /// </summary>
    Task<SyncConfigurationDto> UpdateSyncConfigurationAsync(int configId, CreateSyncConfigurationDto dto);

    /// <summary>
    /// Deletes a sync configuration.
    /// </summary>
    Task<bool> DeleteSyncConfigurationAsync(int configId);

    /// <summary>
    /// Enables or disables sync for a store.
    /// </summary>
    Task<bool> SetSyncEnabledAsync(int storeId, bool isEnabled);

    #endregion

    #region Entity Rules Management

    /// <summary>
    /// Gets entity rules for a sync configuration.
    /// </summary>
    Task<List<SyncEntityRuleDto>> GetEntityRulesAsync(int syncConfigId);

    /// <summary>
    /// Adds an entity rule to a sync configuration.
    /// </summary>
    Task<SyncEntityRuleDto> AddEntityRuleAsync(int syncConfigId, CreateSyncEntityRuleDto dto);

    /// <summary>
    /// Updates an entity rule.
    /// </summary>
    Task<SyncEntityRuleDto> UpdateEntityRuleAsync(int ruleId, CreateSyncEntityRuleDto dto);

    /// <summary>
    /// Removes an entity rule.
    /// </summary>
    Task<bool> RemoveEntityRuleAsync(int ruleId);

    /// <summary>
    /// Gets the sync direction for a specific entity type.
    /// </summary>
    Task<SyncDirection?> GetEntitySyncDirectionAsync(int storeId, SyncEntityType entityType);

    #endregion

    #region Sync Operations

    /// <summary>
    /// Starts a sync operation for a store.
    /// </summary>
    Task<SyncResultDto> StartSyncAsync(StartSyncRequestDto request);

    /// <summary>
    /// Uploads data from store to HQ.
    /// </summary>
    Task<SyncResultDto> UploadDataAsync(int storeId, SyncEntityType entityType, bool force = false);

    /// <summary>
    /// Downloads data from HQ to store.
    /// </summary>
    Task<SyncResultDto> DownloadDataAsync(int storeId, SyncEntityType entityType, bool force = false);

    /// <summary>
    /// Performs bidirectional sync for a store.
    /// </summary>
    Task<SyncResultDto> BidirectionalSyncAsync(int storeId, SyncEntityType entityType, bool force = false);

    /// <summary>
    /// Cancels an in-progress sync batch.
    /// </summary>
    Task<bool> CancelSyncAsync(int batchId);

    /// <summary>
    /// Retries a failed sync batch.
    /// </summary>
    Task<SyncResultDto> RetrySyncAsync(int batchId);

    #endregion

    #region Batch Management

    /// <summary>
    /// Gets details of a sync batch.
    /// </summary>
    Task<SyncBatchDto?> GetSyncBatchAsync(int batchId);

    /// <summary>
    /// Gets all sync batches for a store.
    /// </summary>
    Task<List<SyncBatchDto>> GetStoreSyncBatchesAsync(int storeId, int? limit = null);

    /// <summary>
    /// Gets active sync batches across all stores.
    /// </summary>
    Task<List<SyncBatchDto>> GetActiveBatchesAsync();

    /// <summary>
    /// Gets pending sync batches.
    /// </summary>
    Task<List<SyncBatchDto>> GetPendingBatchesAsync(int? storeId = null);

    /// <summary>
    /// Gets failed sync batches.
    /// </summary>
    Task<List<SyncBatchDto>> GetFailedBatchesAsync(int? storeId = null);

    /// <summary>
    /// Cleans up old completed batches.
    /// </summary>
    Task<int> CleanupOldBatchesAsync(int daysToKeep = 30);

    #endregion

    #region Conflict Management

    /// <summary>
    /// Gets unresolved conflicts.
    /// </summary>
    Task<List<SyncConflictDto>> GetUnresolvedConflictsAsync(int? storeId = null);

    /// <summary>
    /// Gets conflicts for a specific batch.
    /// </summary>
    Task<List<SyncConflictDto>> GetBatchConflictsAsync(int batchId);

    /// <summary>
    /// Gets a specific conflict.
    /// </summary>
    Task<SyncConflictDto?> GetConflictAsync(int conflictId);

    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    Task<SyncConflictDto> ResolveConflictAsync(ResolveConflictDto dto);

    /// <summary>
    /// Resolves multiple conflicts with the same resolution.
    /// </summary>
    Task<int> BulkResolveConflictsAsync(List<int> conflictIds, ConflictWinner resolution, string? notes = null);

    /// <summary>
    /// Gets conflict count by entity type.
    /// </summary>
    Task<Dictionary<SyncEntityType, int>> GetConflictCountsByEntityTypeAsync(int? storeId = null);

    #endregion

    #region Sync Status & Monitoring

    /// <summary>
    /// Gets sync status for a store.
    /// </summary>
    Task<StoreSyncStatusDto> GetStoreSyncStatusAsync(int storeId);

    /// <summary>
    /// Gets sync status for all stores.
    /// </summary>
    Task<List<StoreSyncStatusDto>> GetAllStoreSyncStatusesAsync();

    /// <summary>
    /// Gets the chain-wide sync dashboard.
    /// </summary>
    Task<ChainSyncDashboardDto> GetChainSyncDashboardAsync();

    /// <summary>
    /// Checks if a store is currently syncing.
    /// </summary>
    Task<bool> IsStoreSyncingAsync(int storeId);

    /// <summary>
    /// Gets time since last successful sync.
    /// </summary>
    Task<TimeSpan?> GetTimeSinceLastSyncAsync(int storeId);

    #endregion

    #region Sync Logs

    /// <summary>
    /// Gets sync logs based on query.
    /// </summary>
    Task<List<SyncLogDto>> GetSyncLogsAsync(SyncLogQueryDto query);

    /// <summary>
    /// Gets recent sync logs for a store.
    /// </summary>
    Task<List<SyncLogDto>> GetRecentLogsAsync(int storeId, int count = 50);

    /// <summary>
    /// Gets error logs.
    /// </summary>
    Task<List<SyncLogDto>> GetErrorLogsAsync(int? storeId = null, DateTime? since = null);

    /// <summary>
    /// Logs a sync operation.
    /// </summary>
    Task LogSyncOperationAsync(int storeId, int? batchId, string operation, bool isSuccess, string? details = null, string? errorMessage = null, long? durationMs = null);

    #endregion

    #region Data Payloads

    /// <summary>
    /// Creates an upload payload for a specific entity type.
    /// </summary>
    Task<UploadSyncPayloadDto> CreateUploadPayloadAsync(int storeId, SyncEntityType entityType, DateTime? sinceTimestamp = null);

    /// <summary>
    /// Processes an upload payload from a store.
    /// </summary>
    Task<SyncResultDto> ProcessUploadPayloadAsync(UploadSyncPayloadDto payload);

    /// <summary>
    /// Creates a download payload for a store.
    /// </summary>
    Task<DownloadSyncPayloadDto> CreateDownloadPayloadAsync(int storeId, SyncEntityType entityType, DateTime? sinceTimestamp = null);

    /// <summary>
    /// Applies a download payload to a store.
    /// </summary>
    Task<SyncResultDto> ApplyDownloadPayloadAsync(DownloadSyncPayloadDto payload);

    #endregion

    #region Auto-Sync Management

    /// <summary>
    /// Checks and runs auto-sync for stores that need it.
    /// </summary>
    Task<List<SyncResultDto>> RunAutoSyncAsync();

    /// <summary>
    /// Checks if a store needs sync based on interval.
    /// </summary>
    Task<bool> NeedsSyncAsync(int storeId);

    /// <summary>
    /// Gets stores that need syncing.
    /// </summary>
    Task<List<int>> GetStoresNeedingSyncAsync();

    #endregion

    #region Statistics

    /// <summary>
    /// Gets sync statistics for a store.
    /// </summary>
    Task<SyncStatisticsDto> GetSyncStatisticsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets chain-wide sync statistics.
    /// </summary>
    Task<ChainSyncStatisticsDto> GetChainSyncStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    #endregion
}

/// <summary>
/// Sync statistics for a single store.
/// </summary>
public class SyncStatisticsDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int SuccessfulBatches { get; set; }
    public int FailedBatches { get; set; }
    public int TotalRecordsSynced { get; set; }
    public int TotalConflicts { get; set; }
    public int ResolvedConflicts { get; set; }
    public double AverageSyncDurationMs { get; set; }
    public double SuccessRate => TotalBatches > 0 ? (double)SuccessfulBatches / TotalBatches * 100 : 0;
    public DateTime? FirstSyncAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public Dictionary<SyncEntityType, int> RecordsByEntityType { get; set; } = new();
}

/// <summary>
/// Chain-wide sync statistics.
/// </summary>
public class ChainSyncStatisticsDto
{
    public int TotalStores { get; set; }
    public int ActiveStores { get; set; }
    public int TotalBatches { get; set; }
    public int SuccessfulBatches { get; set; }
    public int FailedBatches { get; set; }
    public int TotalRecordsSynced { get; set; }
    public int TotalConflicts { get; set; }
    public int UnresolvedConflicts { get; set; }
    public double AverageSyncDurationMs { get; set; }
    public double OverallSuccessRate => TotalBatches > 0 ? (double)SuccessfulBatches / TotalBatches * 100 : 0;
    public DateTime? LastChainWideSync { get; set; }
    public List<SyncStatisticsDto> StoreStatistics { get; set; } = new();
    public Dictionary<SyncEntityType, int> RecordsByEntityType { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
