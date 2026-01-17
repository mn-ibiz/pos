using System.Text.Json;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for store data synchronization between stores and HQ.
/// </summary>
public class StoreSyncService : IStoreSyncService
{
    private readonly IRepository<SyncConfiguration> _syncConfigRepository;
    private readonly IRepository<SyncEntityRule> _entityRuleRepository;
    private readonly IRepository<SyncBatch> _batchRepository;
    private readonly IRepository<SyncRecord> _recordRepository;
    private readonly IRepository<SyncConflict> _conflictRepository;
    private readonly IRepository<SyncLog> _logRepository;
    private readonly IRepository<Store> _storeRepository;

    public StoreSyncService(
        IRepository<SyncConfiguration> syncConfigRepository,
        IRepository<SyncEntityRule> entityRuleRepository,
        IRepository<SyncBatch> batchRepository,
        IRepository<SyncRecord> recordRepository,
        IRepository<SyncConflict> conflictRepository,
        IRepository<SyncLog> logRepository,
        IRepository<Store> storeRepository)
    {
        _syncConfigRepository = syncConfigRepository ?? throw new ArgumentNullException(nameof(syncConfigRepository));
        _entityRuleRepository = entityRuleRepository ?? throw new ArgumentNullException(nameof(entityRuleRepository));
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _recordRepository = recordRepository ?? throw new ArgumentNullException(nameof(recordRepository));
        _conflictRepository = conflictRepository ?? throw new ArgumentNullException(nameof(conflictRepository));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    #region Sync Configuration Management

    public async Task<SyncConfigurationDto?> GetSyncConfigurationAsync(int storeId)
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        if (config == null)
            return null;

        return await MapToConfigurationDtoAsync(config);
    }

    public async Task<List<SyncConfigurationDto>> GetAllSyncConfigurationsAsync()
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var result = new List<SyncConfigurationDto>();

        foreach (var config in configs)
        {
            result.Add(await MapToConfigurationDtoAsync(config));
        }

        return result;
    }

    public async Task<SyncConfigurationDto> CreateSyncConfigurationAsync(CreateSyncConfigurationDto dto)
    {
        var config = new SyncConfiguration
        {
            StoreId = dto.StoreId,
            SyncIntervalSeconds = dto.SyncIntervalSeconds,
            IsEnabled = dto.IsEnabled,
            AutoSyncOnStartup = dto.AutoSyncOnStartup,
            MaxBatchSize = dto.MaxBatchSize,
            RetryAttempts = dto.RetryAttempts,
            RetryDelaySeconds = dto.RetryDelaySeconds,
            IsActive = true
        };

        await _syncConfigRepository.AddAsync(config);

        if (dto.EntityRules != null)
        {
            foreach (var ruleDto in dto.EntityRules)
            {
                var rule = new SyncEntityRule
                {
                    SyncConfigurationId = config.Id,
                    EntityType = ruleDto.EntityType,
                    Direction = ruleDto.Direction,
                    ConflictResolution = ruleDto.ConflictResolution,
                    FlagConflictsForReview = ruleDto.FlagConflictsForReview,
                    Priority = ruleDto.Priority,
                    IsEnabled = ruleDto.IsEnabled,
                    IsActive = true
                };
                await _entityRuleRepository.AddAsync(rule);
            }
        }

        return await MapToConfigurationDtoAsync(config);
    }

    public async Task<SyncConfigurationDto> UpdateSyncConfigurationAsync(int configId, CreateSyncConfigurationDto dto)
    {
        var config = await _syncConfigRepository.GetByIdAsync(configId);
        if (config == null)
            throw new InvalidOperationException($"Sync configuration {configId} not found");

        config.SyncIntervalSeconds = dto.SyncIntervalSeconds;
        config.IsEnabled = dto.IsEnabled;
        config.AutoSyncOnStartup = dto.AutoSyncOnStartup;
        config.MaxBatchSize = dto.MaxBatchSize;
        config.RetryAttempts = dto.RetryAttempts;
        config.RetryDelaySeconds = dto.RetryDelaySeconds;

        await _syncConfigRepository.UpdateAsync(config);
        return await MapToConfigurationDtoAsync(config);
    }

    public async Task<bool> DeleteSyncConfigurationAsync(int configId)
    {
        var config = await _syncConfigRepository.GetByIdAsync(configId);
        if (config == null)
            return false;

        config.IsActive = false;
        await _syncConfigRepository.UpdateAsync(config);
        return true;
    }

    public async Task<bool> SetSyncEnabledAsync(int storeId, bool isEnabled)
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        if (config == null)
            return false;

        config.IsEnabled = isEnabled;
        await _syncConfigRepository.UpdateAsync(config);
        return true;
    }

    #endregion

    #region Entity Rules Management

    public async Task<List<SyncEntityRuleDto>> GetEntityRulesAsync(int syncConfigId)
    {
        var rules = await _entityRuleRepository.GetAllAsync();
        return rules
            .Where(r => r.SyncConfigurationId == syncConfigId)
            .Select(MapToEntityRuleDto)
            .ToList();
    }

    public async Task<SyncEntityRuleDto> AddEntityRuleAsync(int syncConfigId, CreateSyncEntityRuleDto dto)
    {
        var rule = new SyncEntityRule
        {
            SyncConfigurationId = syncConfigId,
            EntityType = dto.EntityType,
            Direction = dto.Direction,
            ConflictResolution = dto.ConflictResolution,
            FlagConflictsForReview = dto.FlagConflictsForReview,
            Priority = dto.Priority,
            IsEnabled = dto.IsEnabled,
            IsActive = true
        };

        await _entityRuleRepository.AddAsync(rule);
        return MapToEntityRuleDto(rule);
    }

    public async Task<SyncEntityRuleDto> UpdateEntityRuleAsync(int ruleId, CreateSyncEntityRuleDto dto)
    {
        var rule = await _entityRuleRepository.GetByIdAsync(ruleId);
        if (rule == null)
            throw new InvalidOperationException($"Entity rule {ruleId} not found");

        rule.EntityType = dto.EntityType;
        rule.Direction = dto.Direction;
        rule.ConflictResolution = dto.ConflictResolution;
        rule.FlagConflictsForReview = dto.FlagConflictsForReview;
        rule.Priority = dto.Priority;
        rule.IsEnabled = dto.IsEnabled;

        await _entityRuleRepository.UpdateAsync(rule);
        return MapToEntityRuleDto(rule);
    }

    public async Task<bool> RemoveEntityRuleAsync(int ruleId)
    {
        var rule = await _entityRuleRepository.GetByIdAsync(ruleId);
        if (rule == null)
            return false;

        rule.IsActive = false;
        await _entityRuleRepository.UpdateAsync(rule);
        return true;
    }

    public async Task<SyncDirection?> GetEntitySyncDirectionAsync(int storeId, SyncEntityType entityType)
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        if (config == null)
            return null;

        var rules = await _entityRuleRepository.GetAllAsync();
        var rule = rules.FirstOrDefault(r => r.SyncConfigurationId == config.Id && r.EntityType == entityType && r.IsEnabled);

        return rule?.Direction;
    }

    #endregion

    #region Sync Operations

    public async Task<SyncResultDto> StartSyncAsync(StartSyncRequestDto request)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var configs = await _syncConfigRepository.GetAllAsync();
            var config = configs.FirstOrDefault(c => c.StoreId == request.StoreId);

            if (config == null)
                return SyncResultDto.Failure("Store sync configuration not found", startedAt);

            if (!config.IsEnabled && !request.Force)
                return SyncResultDto.Failure("Sync is disabled for this store", startedAt);

            var rules = await _entityRuleRepository.GetAllAsync();
            var storeRules = rules.Where(r => r.SyncConfigurationId == config.Id && r.IsEnabled).ToList();

            if (request.EntityTypes != null && request.EntityTypes.Any())
            {
                storeRules = storeRules.Where(r => request.EntityTypes.Contains(r.EntityType)).ToList();
            }

            var totalProcessed = 0;
            var totalSucceeded = 0;
            var totalConflicts = 0;
            var errors = new List<string>();
            var batchId = 0;

            foreach (var rule in storeRules.OrderBy(r => r.Priority))
            {
                var direction = request.Direction ?? rule.Direction;
                SyncResultDto result;

                if (direction == SyncDirection.Upload)
                {
                    result = await UploadDataAsync(request.StoreId, rule.EntityType, request.Force);
                }
                else if (direction == SyncDirection.Download)
                {
                    result = await DownloadDataAsync(request.StoreId, rule.EntityType, request.Force);
                }
                else
                {
                    result = await BidirectionalSyncAsync(request.StoreId, rule.EntityType, request.Force);
                }

                batchId = result.BatchId;
                totalProcessed += result.RecordsProcessed;
                totalSucceeded += result.RecordsSucceeded;
                totalConflicts += result.ConflictsDetected;
                errors.AddRange(result.Errors);
            }

            config.LastAttemptedSync = DateTime.UtcNow;
            if (errors.Count == 0)
            {
                config.LastSuccessfulSync = DateTime.UtcNow;
                config.LastSyncError = null;
            }
            else
            {
                config.LastSyncError = string.Join("; ", errors.Take(3));
            }
            await _syncConfigRepository.UpdateAsync(config);

            var syncResult = SyncResultDto.Success(batchId, totalProcessed, totalSucceeded, totalConflicts, startedAt);
            syncResult.Errors = errors;
            return syncResult;
        }
        catch (Exception ex)
        {
            await LogSyncOperationAsync(request.StoreId, null, "StartSync", false, null, ex.Message);
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    public async Task<SyncResultDto> UploadDataAsync(int storeId, SyncEntityType entityType, bool force = false)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var batch = new SyncBatch
            {
                StoreId = storeId,
                Direction = SyncDirection.Upload,
                EntityType = entityType,
                Status = SyncBatchStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _batchRepository.AddAsync(batch);
            await LogSyncOperationAsync(storeId, batch.Id, $"Upload {entityType}", true, "Started upload");

            // Create and process upload payload
            var payload = await CreateUploadPayloadAsync(storeId, entityType);
            batch.RecordCount = payload.Records.Count;

            // Simulate processing records
            foreach (var record in payload.Records)
            {
                var syncRecord = new SyncRecord
                {
                    SyncBatchId = batch.Id,
                    EntityType = entityType,
                    EntityId = record.EntityId,
                    EntityData = record.EntityData,
                    EntityTimestamp = record.Timestamp,
                    IsProcessed = true,
                    IsSuccess = true,
                    ProcessedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _recordRepository.AddAsync(syncRecord);
                batch.ProcessedCount++;
                batch.SuccessCount++;
            }

            batch.Status = SyncBatchStatus.Completed;
            batch.CompletedAt = DateTime.UtcNow;
            await _batchRepository.UpdateAsync(batch);

            await LogSyncOperationAsync(storeId, batch.Id, $"Upload {entityType}", true,
                $"Completed: {batch.SuccessCount}/{batch.RecordCount} records",
                null, (long)(DateTime.UtcNow - startedAt).TotalMilliseconds);

            return SyncResultDto.Success(batch.Id, batch.ProcessedCount, batch.SuccessCount, batch.ConflictCount, startedAt);
        }
        catch (Exception ex)
        {
            await LogSyncOperationAsync(storeId, null, $"Upload {entityType}", false, null, ex.Message);
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    public async Task<SyncResultDto> DownloadDataAsync(int storeId, SyncEntityType entityType, bool force = false)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var batch = new SyncBatch
            {
                StoreId = storeId,
                Direction = SyncDirection.Download,
                EntityType = entityType,
                Status = SyncBatchStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _batchRepository.AddAsync(batch);
            await LogSyncOperationAsync(storeId, batch.Id, $"Download {entityType}", true, "Started download");

            // Create download payload
            var payload = await CreateDownloadPayloadAsync(storeId, entityType);
            batch.RecordCount = payload.Records.Count;

            // Apply payload
            var result = await ApplyDownloadPayloadAsync(payload);
            batch.ProcessedCount = result.RecordsProcessed;
            batch.SuccessCount = result.RecordsSucceeded;
            batch.FailedCount = result.RecordsFailed;
            batch.ConflictCount = result.ConflictsDetected;

            batch.Status = batch.FailedCount > 0 ? SyncBatchStatus.PartiallyCompleted : SyncBatchStatus.Completed;
            batch.CompletedAt = DateTime.UtcNow;
            await _batchRepository.UpdateAsync(batch);

            await LogSyncOperationAsync(storeId, batch.Id, $"Download {entityType}", true,
                $"Completed: {batch.SuccessCount}/{batch.RecordCount} records",
                null, (long)(DateTime.UtcNow - startedAt).TotalMilliseconds);

            return SyncResultDto.Success(batch.Id, batch.ProcessedCount, batch.SuccessCount, batch.ConflictCount, startedAt);
        }
        catch (Exception ex)
        {
            await LogSyncOperationAsync(storeId, null, $"Download {entityType}", false, null, ex.Message);
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    public async Task<SyncResultDto> BidirectionalSyncAsync(int storeId, SyncEntityType entityType, bool force = false)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            // Upload first
            var uploadResult = await UploadDataAsync(storeId, entityType, force);
            if (!uploadResult.IsSuccess)
                return uploadResult;

            // Then download
            var downloadResult = await DownloadDataAsync(storeId, entityType, force);

            var totalProcessed = uploadResult.RecordsProcessed + downloadResult.RecordsProcessed;
            var totalSucceeded = uploadResult.RecordsSucceeded + downloadResult.RecordsSucceeded;
            var totalConflicts = uploadResult.ConflictsDetected + downloadResult.ConflictsDetected;

            var result = SyncResultDto.Success(downloadResult.BatchId, totalProcessed, totalSucceeded, totalConflicts, startedAt);
            result.Errors.AddRange(uploadResult.Errors);
            result.Errors.AddRange(downloadResult.Errors);
            return result;
        }
        catch (Exception ex)
        {
            await LogSyncOperationAsync(storeId, null, $"Bidirectional {entityType}", false, null, ex.Message);
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    public async Task<bool> CancelSyncAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null)
            return false;

        if (batch.Status != SyncBatchStatus.Pending && batch.Status != SyncBatchStatus.InProgress)
            return false;

        batch.Status = SyncBatchStatus.Cancelled;
        batch.CompletedAt = DateTime.UtcNow;
        await _batchRepository.UpdateAsync(batch);

        await LogSyncOperationAsync(batch.StoreId, batchId, "CancelSync", true, "Batch cancelled");
        return true;
    }

    public async Task<SyncResultDto> RetrySyncAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null)
            return SyncResultDto.Failure("Batch not found", DateTime.UtcNow);

        if (batch.Status != SyncBatchStatus.Failed && batch.Status != SyncBatchStatus.PartiallyCompleted)
            return SyncResultDto.Failure("Batch is not in a retryable state", DateTime.UtcNow);

        return batch.Direction switch
        {
            SyncDirection.Upload => await UploadDataAsync(batch.StoreId, batch.EntityType, true),
            SyncDirection.Download => await DownloadDataAsync(batch.StoreId, batch.EntityType, true),
            _ => await BidirectionalSyncAsync(batch.StoreId, batch.EntityType, true)
        };
    }

    #endregion

    #region Batch Management

    public async Task<SyncBatchDto?> GetSyncBatchAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null)
            return null;

        return await MapToBatchDtoAsync(batch);
    }

    public async Task<List<SyncBatchDto>> GetStoreSyncBatchesAsync(int storeId, int? limit = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var storeBatches = batches
            .Where(b => b.StoreId == storeId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit ?? 100);

        var result = new List<SyncBatchDto>();
        foreach (var batch in storeBatches)
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<List<SyncBatchDto>> GetActiveBatchesAsync()
    {
        var batches = await _batchRepository.GetAllAsync();
        var activeBatches = batches.Where(b => b.Status == SyncBatchStatus.InProgress);

        var result = new List<SyncBatchDto>();
        foreach (var batch in activeBatches)
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<List<SyncBatchDto>> GetPendingBatchesAsync(int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var pendingBatches = batches.Where(b => b.Status == SyncBatchStatus.Pending);

        if (storeId.HasValue)
            pendingBatches = pendingBatches.Where(b => b.StoreId == storeId.Value);

        var result = new List<SyncBatchDto>();
        foreach (var batch in pendingBatches)
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<List<SyncBatchDto>> GetFailedBatchesAsync(int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var failedBatches = batches.Where(b => b.Status == SyncBatchStatus.Failed);

        if (storeId.HasValue)
            failedBatches = failedBatches.Where(b => b.StoreId == storeId.Value);

        var result = new List<SyncBatchDto>();
        foreach (var batch in failedBatches)
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<int> CleanupOldBatchesAsync(int daysToKeep = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var batches = await _batchRepository.GetAllAsync();
        var oldBatches = batches.Where(b =>
            (b.Status == SyncBatchStatus.Completed || b.Status == SyncBatchStatus.Cancelled) &&
            b.CompletedAt < cutoffDate).ToList();

        foreach (var batch in oldBatches)
        {
            batch.IsActive = false;
            await _batchRepository.UpdateAsync(batch);
        }

        return oldBatches.Count;
    }

    #endregion

    #region Conflict Management

    public async Task<List<SyncConflictDto>> GetUnresolvedConflictsAsync(int? storeId = null)
    {
        var conflicts = await _conflictRepository.GetAllAsync();
        var unresolvedConflicts = conflicts.Where(c => !c.IsResolved);

        if (storeId.HasValue)
        {
            var batches = await _batchRepository.GetAllAsync();
            var storeBatchIds = batches.Where(b => b.StoreId == storeId.Value).Select(b => b.Id).ToHashSet();
            unresolvedConflicts = unresolvedConflicts.Where(c => storeBatchIds.Contains(c.SyncBatchId));
        }

        return unresolvedConflicts.Select(MapToConflictDto).ToList();
    }

    public async Task<List<SyncConflictDto>> GetBatchConflictsAsync(int batchId)
    {
        var conflicts = await _conflictRepository.GetAllAsync();
        return conflicts.Where(c => c.SyncBatchId == batchId).Select(MapToConflictDto).ToList();
    }

    public async Task<SyncConflictDto?> GetConflictAsync(int conflictId)
    {
        var conflict = await _conflictRepository.GetByIdAsync(conflictId);
        return conflict == null ? null : MapToConflictDto(conflict);
    }

    public async Task<SyncConflictDto> ResolveConflictAsync(ResolveConflictDto dto)
    {
        var conflict = await _conflictRepository.GetByIdAsync(dto.ConflictId);
        if (conflict == null)
            throw new InvalidOperationException($"Conflict {dto.ConflictId} not found");

        conflict.Resolution = dto.Resolution;
        conflict.ResolutionNotes = dto.Notes;
        conflict.IsResolved = true;
        conflict.ResolvedAt = DateTime.UtcNow;

        await _conflictRepository.UpdateAsync(conflict);
        return MapToConflictDto(conflict);
    }

    public async Task<int> BulkResolveConflictsAsync(List<int> conflictIds, ConflictWinner resolution, string? notes = null)
    {
        var count = 0;
        foreach (var conflictId in conflictIds)
        {
            var conflict = await _conflictRepository.GetByIdAsync(conflictId);
            if (conflict != null && !conflict.IsResolved)
            {
                conflict.Resolution = resolution;
                conflict.ResolutionNotes = notes;
                conflict.IsResolved = true;
                conflict.ResolvedAt = DateTime.UtcNow;
                await _conflictRepository.UpdateAsync(conflict);
                count++;
            }
        }
        return count;
    }

    public async Task<Dictionary<SyncEntityType, int>> GetConflictCountsByEntityTypeAsync(int? storeId = null)
    {
        var unresolvedConflicts = await GetUnresolvedConflictsAsync(storeId);
        return unresolvedConflicts
            .GroupBy(c => c.EntityType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion

    #region Sync Status & Monitoring

    public async Task<StoreSyncStatusDto> GetStoreSyncStatusAsync(int storeId)
    {
        var stores = await _storeRepository.GetAllAsync();
        var store = stores.FirstOrDefault(s => s.Id == storeId);

        if (store == null)
            throw new InvalidOperationException($"Store {storeId} not found");

        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        var batches = await _batchRepository.GetAllAsync();
        var storeBatches = batches.Where(b => b.StoreId == storeId).ToList();

        var currentBatch = storeBatches.FirstOrDefault(b => b.Status == SyncBatchStatus.InProgress);
        var pendingBatches = storeBatches.Where(b => b.Status == SyncBatchStatus.Pending).ToList();

        var conflicts = await GetUnresolvedConflictsAsync(storeId);

        return new StoreSyncStatusDto
        {
            StoreId = storeId,
            StoreCode = store.Code,
            StoreName = store.Name,
            IsConfigured = config != null,
            IsEnabled = config?.IsEnabled ?? false,
            IsOnline = IsStoreOnline(config?.LastSuccessfulSync),
            LastSuccessfulSync = config?.LastSuccessfulSync,
            LastAttemptedSync = config?.LastAttemptedSync,
            LastSyncError = config?.LastSyncError,
            PendingUploadCount = pendingBatches.Count(b => b.Direction == SyncDirection.Upload),
            PendingDownloadCount = pendingBatches.Count(b => b.Direction == SyncDirection.Download),
            UnresolvedConflicts = conflicts.Count,
            CurrentBatch = currentBatch != null ? await MapToBatchDtoAsync(currentBatch) : null
        };
    }

    public async Task<List<StoreSyncStatusDto>> GetAllStoreSyncStatusesAsync()
    {
        var stores = await _storeRepository.GetAllAsync();
        var result = new List<StoreSyncStatusDto>();

        foreach (var store in stores)
        {
            result.Add(await GetStoreSyncStatusAsync(store.Id));
        }

        return result;
    }

    public async Task<ChainSyncDashboardDto> GetChainSyncDashboardAsync()
    {
        var storeStatuses = await GetAllStoreSyncStatusesAsync();
        var activeBatches = await GetActiveBatchesAsync();
        var recentLogs = await GetSyncLogsAsync(new SyncLogQueryDto { Limit = 50 });

        return new ChainSyncDashboardDto
        {
            TotalStores = storeStatuses.Count,
            StoresOnline = storeStatuses.Count(s => s.IsOnline),
            StoresOffline = storeStatuses.Count(s => !s.IsOnline),
            StoresSyncing = storeStatuses.Count(s => s.CurrentBatch != null),
            TotalPendingUploads = storeStatuses.Sum(s => s.PendingUploadCount),
            TotalPendingDownloads = storeStatuses.Sum(s => s.PendingDownloadCount),
            TotalUnresolvedConflicts = storeStatuses.Sum(s => s.UnresolvedConflicts),
            LastChainWideSync = storeStatuses.Max(s => s.LastSuccessfulSync),
            StoreStatuses = storeStatuses,
            ActiveBatches = activeBatches,
            RecentLogs = recentLogs
        };
    }

    public async Task<bool> IsStoreSyncingAsync(int storeId)
    {
        var batches = await _batchRepository.GetAllAsync();
        return batches.Any(b => b.StoreId == storeId && b.Status == SyncBatchStatus.InProgress);
    }

    public async Task<TimeSpan?> GetTimeSinceLastSyncAsync(int storeId)
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        if (config?.LastSuccessfulSync == null)
            return null;

        return DateTime.UtcNow - config.LastSuccessfulSync.Value;
    }

    #endregion

    #region Sync Logs

    public async Task<List<SyncLogDto>> GetSyncLogsAsync(SyncLogQueryDto query)
    {
        var logs = await _logRepository.GetAllAsync();
        var filteredLogs = logs.AsQueryable();

        if (query.StoreId.HasValue)
            filteredLogs = filteredLogs.Where(l => l.StoreId == query.StoreId.Value);

        if (query.SyncBatchId.HasValue)
            filteredLogs = filteredLogs.Where(l => l.SyncBatchId == query.SyncBatchId.Value);

        if (query.IsSuccess.HasValue)
            filteredLogs = filteredLogs.Where(l => l.IsSuccess == query.IsSuccess.Value);

        if (query.FromDate.HasValue)
            filteredLogs = filteredLogs.Where(l => l.Timestamp >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            filteredLogs = filteredLogs.Where(l => l.Timestamp <= query.ToDate.Value);

        return filteredLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(query.Limit ?? 100)
            .Select(l => MapToLogDto(l))
            .ToList();
    }

    public async Task<List<SyncLogDto>> GetRecentLogsAsync(int storeId, int count = 50)
    {
        return await GetSyncLogsAsync(new SyncLogQueryDto { StoreId = storeId, Limit = count });
    }

    public async Task<List<SyncLogDto>> GetErrorLogsAsync(int? storeId = null, DateTime? since = null)
    {
        return await GetSyncLogsAsync(new SyncLogQueryDto
        {
            StoreId = storeId,
            IsSuccess = false,
            FromDate = since
        });
    }

    public async Task LogSyncOperationAsync(int storeId, int? batchId, string operation, bool isSuccess, string? details = null, string? errorMessage = null, long? durationMs = null)
    {
        var log = new SyncLog
        {
            StoreId = storeId,
            SyncBatchId = batchId,
            Operation = operation,
            Details = details,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            DurationMs = durationMs,
            IsActive = true
        };

        await _logRepository.AddAsync(log);
    }

    #endregion

    #region Data Payloads

    public async Task<UploadSyncPayloadDto> CreateUploadPayloadAsync(int storeId, SyncEntityType entityType, DateTime? sinceTimestamp = null)
    {
        // This would be implemented to gather data from the store's local database
        // For now, return an empty payload
        return new UploadSyncPayloadDto
        {
            StoreId = storeId,
            EntityType = entityType,
            Records = new List<SyncRecordDto>(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<SyncResultDto> ProcessUploadPayloadAsync(UploadSyncPayloadDto payload)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var batch = new SyncBatch
            {
                StoreId = payload.StoreId,
                Direction = SyncDirection.Upload,
                EntityType = payload.EntityType,
                RecordCount = payload.Records.Count,
                Status = SyncBatchStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _batchRepository.AddAsync(batch);

            foreach (var recordDto in payload.Records)
            {
                var record = new SyncRecord
                {
                    SyncBatchId = batch.Id,
                    EntityType = payload.EntityType,
                    EntityId = recordDto.EntityId,
                    EntityData = recordDto.EntityData,
                    EntityTimestamp = recordDto.Timestamp,
                    IsProcessed = true,
                    IsSuccess = true,
                    ProcessedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _recordRepository.AddAsync(record);
                batch.ProcessedCount++;
                batch.SuccessCount++;
            }

            batch.Status = SyncBatchStatus.Completed;
            batch.CompletedAt = DateTime.UtcNow;
            await _batchRepository.UpdateAsync(batch);

            return SyncResultDto.Success(batch.Id, batch.ProcessedCount, batch.SuccessCount, 0, startedAt);
        }
        catch (Exception ex)
        {
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    public async Task<DownloadSyncPayloadDto> CreateDownloadPayloadAsync(int storeId, SyncEntityType entityType, DateTime? sinceTimestamp = null)
    {
        // This would be implemented to gather data from HQ database for the store
        // For now, return an empty payload
        return new DownloadSyncPayloadDto
        {
            StoreId = storeId,
            EntityType = entityType,
            SinceTimestamp = sinceTimestamp,
            Records = new List<SyncRecordDto>(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<SyncResultDto> ApplyDownloadPayloadAsync(DownloadSyncPayloadDto payload)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var processed = 0;
            var succeeded = 0;
            var conflicts = 0;

            foreach (var record in payload.Records)
            {
                processed++;
                // In real implementation, apply the record to local database
                // Check for conflicts based on entity rules
                succeeded++;
            }

            return SyncResultDto.Success(0, processed, succeeded, conflicts, startedAt);
        }
        catch (Exception ex)
        {
            return SyncResultDto.Failure(ex.Message, startedAt);
        }
    }

    #endregion

    #region Auto-Sync Management

    public async Task<List<SyncResultDto>> RunAutoSyncAsync()
    {
        var storesNeedingSync = await GetStoresNeedingSyncAsync();
        var results = new List<SyncResultDto>();

        foreach (var storeId in storesNeedingSync)
        {
            var result = await StartSyncAsync(new StartSyncRequestDto
            {
                StoreId = storeId,
                Force = false
            });
            results.Add(result);
        }

        return results;
    }

    public async Task<bool> NeedsSyncAsync(int storeId)
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var config = configs.FirstOrDefault(c => c.StoreId == storeId);

        if (config == null || !config.IsEnabled)
            return false;

        if (!config.LastSuccessfulSync.HasValue)
            return true;

        var timeSinceLastSync = DateTime.UtcNow - config.LastSuccessfulSync.Value;
        return timeSinceLastSync.TotalSeconds >= config.SyncIntervalSeconds;
    }

    public async Task<List<int>> GetStoresNeedingSyncAsync()
    {
        var configs = await _syncConfigRepository.GetAllAsync();
        var storesNeedingSync = new List<int>();

        foreach (var config in configs.Where(c => c.IsEnabled))
        {
            if (await NeedsSyncAsync(config.StoreId))
            {
                storesNeedingSync.Add(config.StoreId);
            }
        }

        return storesNeedingSync;
    }

    #endregion

    #region Statistics

    public async Task<SyncStatisticsDto> GetSyncStatisticsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var stores = await _storeRepository.GetAllAsync();
        var store = stores.FirstOrDefault(s => s.Id == storeId);

        if (store == null)
            throw new InvalidOperationException($"Store {storeId} not found");

        var batches = await _batchRepository.GetAllAsync();
        var storeBatches = batches.Where(b => b.StoreId == storeId);

        if (fromDate.HasValue)
            storeBatches = storeBatches.Where(b => b.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            storeBatches = storeBatches.Where(b => b.CreatedAt <= toDate.Value);

        var batchList = storeBatches.ToList();

        var logs = await _logRepository.GetAllAsync();
        var storeLogs = logs.Where(l => l.StoreId == storeId && l.DurationMs.HasValue).ToList();

        var conflicts = await _conflictRepository.GetAllAsync();
        var storeBatchIds = batchList.Select(b => b.Id).ToHashSet();
        var storeConflicts = conflicts.Where(c => storeBatchIds.Contains(c.SyncBatchId)).ToList();

        var records = await _recordRepository.GetAllAsync();
        var storeRecords = records.Where(r => storeBatchIds.Contains(r.SyncBatchId)).ToList();

        return new SyncStatisticsDto
        {
            StoreId = storeId,
            StoreName = store.Name,
            TotalBatches = batchList.Count,
            SuccessfulBatches = batchList.Count(b => b.Status == SyncBatchStatus.Completed),
            FailedBatches = batchList.Count(b => b.Status == SyncBatchStatus.Failed),
            TotalRecordsSynced = storeRecords.Count(r => r.IsSuccess),
            TotalConflicts = storeConflicts.Count,
            ResolvedConflicts = storeConflicts.Count(c => c.IsResolved),
            AverageSyncDurationMs = storeLogs.Count > 0 ? storeLogs.Average(l => l.DurationMs!.Value) : 0,
            FirstSyncAt = batchList.MinBy(b => b.CreatedAt)?.CreatedAt,
            LastSyncAt = batchList.MaxBy(b => b.CreatedAt)?.CreatedAt,
            RecordsByEntityType = storeRecords
                .GroupBy(r => r.EntityType)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<ChainSyncStatisticsDto> GetChainSyncStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var stores = await _storeRepository.GetAllAsync();
        var storeStats = new List<SyncStatisticsDto>();

        foreach (var store in stores)
        {
            storeStats.Add(await GetSyncStatisticsAsync(store.Id, fromDate, toDate));
        }

        var batches = await _batchRepository.GetAllAsync();
        if (fromDate.HasValue)
            batches = batches.Where(b => b.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            batches = batches.Where(b => b.CreatedAt <= toDate.Value);

        var batchList = batches.ToList();

        var logs = await _logRepository.GetAllAsync();
        var logsWithDuration = logs.Where(l => l.DurationMs.HasValue).ToList();

        var conflicts = await GetUnresolvedConflictsAsync();

        return new ChainSyncStatisticsDto
        {
            TotalStores = stores.Count(),
            ActiveStores = storeStats.Count(s => s.LastSyncAt.HasValue),
            TotalBatches = batchList.Count,
            SuccessfulBatches = batchList.Count(b => b.Status == SyncBatchStatus.Completed),
            FailedBatches = batchList.Count(b => b.Status == SyncBatchStatus.Failed),
            TotalRecordsSynced = storeStats.Sum(s => s.TotalRecordsSynced),
            TotalConflicts = storeStats.Sum(s => s.TotalConflicts),
            UnresolvedConflicts = conflicts.Count,
            AverageSyncDurationMs = logsWithDuration.Count > 0 ? logsWithDuration.Average(l => l.DurationMs!.Value) : 0,
            LastChainWideSync = storeStats.Max(s => s.LastSyncAt),
            StoreStatistics = storeStats,
            RecordsByEntityType = storeStats
                .SelectMany(s => s.RecordsByEntityType)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value))
        };
    }

    #endregion

    #region Private Helpers

    private async Task<SyncConfigurationDto> MapToConfigurationDtoAsync(SyncConfiguration config)
    {
        var stores = await _storeRepository.GetAllAsync();
        var store = stores.FirstOrDefault(s => s.Id == config.StoreId);

        var rules = await _entityRuleRepository.GetAllAsync();
        var configRules = rules.Where(r => r.SyncConfigurationId == config.Id).ToList();

        return new SyncConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            StoreName = store?.Name ?? "Unknown",
            SyncIntervalSeconds = config.SyncIntervalSeconds,
            IsEnabled = config.IsEnabled,
            AutoSyncOnStartup = config.AutoSyncOnStartup,
            MaxBatchSize = config.MaxBatchSize,
            RetryAttempts = config.RetryAttempts,
            RetryDelaySeconds = config.RetryDelaySeconds,
            LastSuccessfulSync = config.LastSuccessfulSync,
            LastAttemptedSync = config.LastAttemptedSync,
            LastSyncError = config.LastSyncError,
            EntityRules = configRules.Select(MapToEntityRuleDto).ToList()
        };
    }

    private SyncEntityRuleDto MapToEntityRuleDto(SyncEntityRule rule)
    {
        return new SyncEntityRuleDto
        {
            Id = rule.Id,
            EntityType = rule.EntityType,
            Direction = rule.Direction,
            ConflictResolution = rule.ConflictResolution,
            FlagConflictsForReview = rule.FlagConflictsForReview,
            Priority = rule.Priority,
            IsEnabled = rule.IsEnabled
        };
    }

    private async Task<SyncBatchDto> MapToBatchDtoAsync(SyncBatch batch)
    {
        var stores = await _storeRepository.GetAllAsync();
        var store = stores.FirstOrDefault(s => s.Id == batch.StoreId);

        return new SyncBatchDto
        {
            Id = batch.Id,
            StoreId = batch.StoreId,
            StoreName = store?.Name ?? "Unknown",
            Direction = batch.Direction,
            EntityType = batch.EntityType,
            RecordCount = batch.RecordCount,
            ProcessedCount = batch.ProcessedCount,
            SuccessCount = batch.SuccessCount,
            FailedCount = batch.FailedCount,
            ConflictCount = batch.ConflictCount,
            Status = batch.Status,
            CreatedAt = batch.CreatedAt,
            StartedAt = batch.StartedAt,
            CompletedAt = batch.CompletedAt,
            ErrorMessage = batch.ErrorMessage
        };
    }

    private SyncConflictDto MapToConflictDto(SyncConflict conflict)
    {
        return new SyncConflictDto
        {
            Id = conflict.Id,
            SyncBatchId = conflict.SyncBatchId,
            EntityType = conflict.EntityType,
            EntityId = conflict.EntityId,
            LocalData = conflict.LocalData,
            RemoteData = conflict.RemoteData,
            LocalTimestamp = conflict.LocalTimestamp,
            RemoteTimestamp = conflict.RemoteTimestamp,
            IsResolved = conflict.IsResolved,
            Resolution = conflict.Resolution,
            ResolvedAt = conflict.ResolvedAt,
            ResolutionNotes = conflict.ResolutionNotes
        };
    }

    private SyncLogDto MapToLogDto(SyncLog log)
    {
        return new SyncLogDto
        {
            Id = log.Id,
            StoreId = log.StoreId,
            SyncBatchId = log.SyncBatchId,
            Operation = log.Operation,
            Details = log.Details,
            IsSuccess = log.IsSuccess,
            ErrorMessage = log.ErrorMessage,
            Timestamp = log.Timestamp,
            DurationMs = log.DurationMs
        };
    }

    private bool IsStoreOnline(DateTime? lastSync)
    {
        if (!lastSync.HasValue)
            return false;

        // Consider online if synced within last 5 minutes
        return (DateTime.UtcNow - lastSync.Value).TotalMinutes < 5;
    }

    #endregion
}
