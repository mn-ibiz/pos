using System.Net.Http.Json;
using System.Text.Json;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Configuration options for cloud API synchronization.
/// </summary>
public class CloudApiSettings
{
    public const string SectionName = "CloudApi";

    /// <summary>
    /// Base URL for the cloud sync API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.hospitalitypos.co.ke";

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Store/tenant ID for multi-tenant isolation.
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in seconds for API calls.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable sync (can be disabled for offline-only mode).
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Payload for sync API requests.
/// </summary>
public class SyncPayload
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Data { get; set; }
    public DateTime LocalTimestamp { get; set; }
}

/// <summary>
/// Service for managing the sync queue with priority-based processing and exponential backoff retry.
/// </summary>
public class SyncQueueService : ISyncQueueService
{
    private readonly IRepository<SyncQueueItem> _queueRepository;
    private readonly ILogger<SyncQueueService> _logger;
    private readonly SyncRetryPolicy _retryPolicy;
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService? _connectivityService;
    private readonly CloudApiSettings _apiSettings;

    public SyncQueueService(
        IRepository<SyncQueueItem> queueRepository,
        ILogger<SyncQueueService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<CloudApiSettings>? apiSettings = null,
        IConnectivityService? connectivityService = null,
        SyncRetryPolicy? retryPolicy = null)
    {
        _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = retryPolicy ?? new SyncRetryPolicy();
        _apiSettings = apiSettings?.Value ?? new CloudApiSettings();
        _connectivityService = connectivityService;

        _httpClient = httpClientFactory.CreateClient("CloudSync");
        _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_apiSettings.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_apiSettings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiSettings.ApiKey);
        }

        if (!string.IsNullOrEmpty(_apiSettings.StoreId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Store-Id", _apiSettings.StoreId);
        }
    }

    #region Queue Operations

    /// <inheritdoc />
    public async Task<SyncQueueItemDto> EnqueueAsync<T>(T entity, SyncQueueOperationType operationType, SyncQueuePriority priority = SyncQueuePriority.Normal) where T : BaseEntity
    {
        var entityType = typeof(T).Name;
        var payload = JsonSerializer.Serialize(entity);

        return await EnqueueAsync(entityType, entity.Id, operationType, payload, priority);
    }

    /// <inheritdoc />
    public async Task<SyncQueueItemDto> EnqueueAsync(string entityType, int entityId, SyncQueueOperationType operationType, string? payload = null, SyncQueuePriority priority = SyncQueuePriority.Normal)
    {
        var item = new SyncQueueItem
        {
            EntityType = entityType,
            EntityId = entityId,
            OperationType = operationType,
            Priority = priority,
            Status = SyncQueueItemStatus.Pending,
            Payload = payload,
            RetryCount = 0,
            MaxRetries = _retryPolicy.MaxRetries,
            CreatedAt = DateTime.UtcNow
        };

        await _queueRepository.AddAsync(item);
        _logger.LogInformation("Enqueued sync item: {EntityType}:{EntityId} with priority {Priority}",
            entityType, entityId, priority);

        return MapToDto(item);
    }

    /// <inheritdoc />
    public async Task<List<SyncQueueItemDto>> GetPendingItemsAsync(int limit = 100)
    {
        var allItems = await _queueRepository.GetAllAsync();
        var pendingItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Pending && i.IsActive)
            .OrderByDescending(i => (int)i.Priority) // Critical = 4, High = 3, Normal = 2, Low = 1
            .ThenBy(i => i.CreatedAt)
            .Take(limit)
            .ToList();

        return pendingItems.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<SyncQueueItemDto>> GetPendingItemsByTypeAsync(string entityType, int limit = 100)
    {
        var allItems = await _queueRepository.GetAllAsync();
        var items = allItems
            .Where(i => i.EntityType == entityType && i.Status == SyncQueueItemStatus.Pending && i.IsActive)
            .OrderByDescending(i => (int)i.Priority)
            .ThenBy(i => i.CreatedAt)
            .Take(limit)
            .ToList();

        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<SyncQueueItemDto>> GetFailedItemsAsync(int limit = 100)
    {
        var allItems = await _queueRepository.GetAllAsync();
        var failedItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Failed && i.IsActive)
            .OrderByDescending(i => i.LastAttemptAt)
            .Take(limit)
            .ToList();

        return failedItems.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<SyncQueueItemDto?> GetByIdAsync(int id)
    {
        var item = await _queueRepository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    #endregion

    #region Processing

    /// <inheritdoc />
    public async Task<SyncProcessingResult> ProcessQueueAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var processed = 0;
        var succeeded = 0;
        var failed = 0;
        var errors = new List<string>();

        _logger.LogInformation("Starting sync queue processing with batch size {BatchSize}", batchSize);

        try
        {
            var pendingItems = await GetPendingItemsAsync(batchSize);

            foreach (var item in pendingItems)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Sync processing cancelled. Processed {Count} items.", processed);
                    break;
                }

                try
                {
                    var success = await ProcessItemAsync(item.Id);
                    processed++;

                    if (success)
                    {
                        succeeded++;
                    }
                    else
                    {
                        failed++;
                        errors.Add($"Failed to process {item.EntityType}:{item.EntityId}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Error processing {item.EntityType}:{item.EntityId}: {ex.Message}");
                    _logger.LogError(ex, "Error processing sync item {ItemId}", item.Id);
                }
            }

            _logger.LogInformation("Sync processing completed. Processed: {Processed}, Succeeded: {Succeeded}, Failed: {Failed}",
                processed, succeeded, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync queue processing");
            errors.Add($"Queue processing error: {ex.Message}");
        }

        return failed > 0
            ? SyncProcessingResult.WithFailures(processed, succeeded, failed, errors, startedAt)
            : SyncProcessingResult.Success(processed, succeeded, startedAt);
    }

    /// <inheritdoc />
    public async Task<bool> ProcessItemAsync(int itemId)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null)
        {
            _logger.LogWarning("Sync item {ItemId} not found", itemId);
            return false;
        }

        if (item.Status != SyncQueueItemStatus.Pending && item.Status != SyncQueueItemStatus.Failed)
        {
            _logger.LogDebug("Sync item {ItemId} is not in pending/failed state", itemId);
            return false;
        }

        // Check connectivity before attempting sync
        if (_connectivityService != null)
        {
            var connectivityStatus = await _connectivityService.CheckConnectivityAsync();
            if (connectivityStatus == ConnectivityStatus.Offline)
            {
                _logger.LogWarning("Cannot sync item {ItemId}: System is offline", itemId);
                return false;
            }
        }

        // Check if sync is enabled
        if (!_apiSettings.Enabled)
        {
            _logger.LogDebug("Sync is disabled. Item {ItemId} will remain pending.", itemId);
            return false;
        }

        try
        {
            // Mark as in progress
            await MarkAsInProgressAsync(itemId);

            // Build the API endpoint based on entity type and operation
            var endpoint = GetSyncEndpoint(item.EntityType, item.OperationType);

            _logger.LogDebug("Syncing item {ItemId}: {EntityType}:{EntityId} to {Endpoint}",
                itemId, item.EntityType, item.EntityId, endpoint);

            // Perform the actual API call
            var success = await ExecuteSyncOperationAsync(item, endpoint);

            if (success)
            {
                await MarkAsCompletedAsync(itemId);
                return true;
            }
            else
            {
                await MarkAsFailedAsync(itemId, "Sync operation returned unsuccessful response");
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error syncing item {ItemId}", itemId);
            await MarkAsFailedAsync(itemId, $"HTTP Error: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout syncing item {ItemId}", itemId);
            await MarkAsFailedAsync(itemId, "Request timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing item {ItemId}", itemId);
            await MarkAsFailedAsync(itemId, ex.Message);
            return false;
        }
    }

    private string GetSyncEndpoint(string entityType, SyncQueueOperationType operationType)
    {
        // Map entity types to API endpoints
        var entityEndpoint = entityType.ToLowerInvariant() switch
        {
            "receipt" => "/api/v1/sync/receipts",
            "order" => "/api/v1/sync/orders",
            "product" => "/api/v1/sync/products",
            "category" => "/api/v1/sync/categories",
            "inventory" => "/api/v1/sync/inventory",
            "customer" => "/api/v1/sync/customers",
            "loyaltymember" => "/api/v1/sync/loyalty",
            "loyaltytransaction" => "/api/v1/sync/loyalty/transactions",
            "stocktransfer" => "/api/v1/sync/transfers",
            "productbatch" => "/api/v1/sync/batches",
            "user" => "/api/v1/sync/users",
            "etimsinvoice" => "/api/v1/sync/etims",
            "mpesatransaction" => "/api/v1/sync/mpesa",
            _ => $"/api/v1/sync/{entityType.ToLowerInvariant()}"
        };

        return entityEndpoint;
    }

    private async Task<bool> ExecuteSyncOperationAsync(SyncQueueItem item, string endpoint)
    {
        HttpResponseMessage response;

        switch (item.OperationType)
        {
            case SyncQueueOperationType.Create:
                response = await _httpClient.PostAsJsonAsync(endpoint, new SyncPayload
                {
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    Operation = "create",
                    Data = item.Payload,
                    LocalTimestamp = item.CreatedAt
                });
                break;

            case SyncQueueOperationType.Update:
                response = await _httpClient.PutAsJsonAsync($"{endpoint}/{item.EntityId}", new SyncPayload
                {
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    Operation = "update",
                    Data = item.Payload,
                    LocalTimestamp = item.UpdatedAt ?? item.CreatedAt
                });
                break;

            case SyncQueueOperationType.Delete:
                response = await _httpClient.DeleteAsync($"{endpoint}/{item.EntityId}");
                break;

            default:
                _logger.LogWarning("Unknown operation type {OperationType} for item {ItemId}",
                    item.OperationType, item.Id);
                return false;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully synced {EntityType}:{EntityId} ({Operation})",
                item.EntityType, item.EntityId, item.OperationType);
            return true;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogWarning("Sync failed for {EntityType}:{EntityId}. Status: {Status}, Response: {Response}",
            item.EntityType, item.EntityId, response.StatusCode, errorContent);

        // Check for conflict (409)
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Mark as conflict for resolution
            item.Status = SyncQueueItemStatus.Conflict;
            item.LastError = $"Conflict detected: {errorContent}";
            await _queueRepository.UpdateAsync(item);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsInProgressAsync(int itemId)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        item.Status = SyncQueueItemStatus.InProgress;
        item.LastAttemptAt = DateTime.UtcNow;
        await _queueRepository.UpdateAsync(item);

        _logger.LogDebug("Marked sync item {ItemId} as InProgress", itemId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsCompletedAsync(int itemId)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        item.Status = SyncQueueItemStatus.Completed;
        item.UpdatedAt = DateTime.UtcNow;
        await _queueRepository.UpdateAsync(item);

        _logger.LogInformation("Sync item {ItemId} completed: {EntityType}:{EntityId}",
            itemId, item.EntityType, item.EntityId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsFailedAsync(int itemId, string errorMessage)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        item.Status = SyncQueueItemStatus.Failed;
        item.LastError = errorMessage;
        item.RetryCount++;
        item.UpdatedAt = DateTime.UtcNow;

        // Calculate next retry time
        if (item.RetryCount < item.MaxRetries)
        {
            var delaySeconds = CalculateRetryDelaySeconds(item.RetryCount);
            item.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            _logger.LogWarning("Sync item {ItemId} failed (attempt {Attempt}/{MaxRetries}). Next retry at {NextRetry}",
                itemId, item.RetryCount, item.MaxRetries, item.NextRetryAt);
        }
        else
        {
            _logger.LogError("Sync item {ItemId} failed permanently after {MaxRetries} attempts: {Error}",
                itemId, item.MaxRetries, errorMessage);
        }

        await _queueRepository.UpdateAsync(item);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelItemAsync(int itemId)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        if (item.Status == SyncQueueItemStatus.Completed)
        {
            return false; // Can't cancel completed items
        }

        item.Status = SyncQueueItemStatus.Cancelled;
        item.UpdatedAt = DateTime.UtcNow;
        await _queueRepository.UpdateAsync(item);

        _logger.LogInformation("Cancelled sync item {ItemId}", itemId);
        return true;
    }

    #endregion

    #region Retry Operations

    /// <inheritdoc />
    public async Task<int> RetryFailedItemsAsync()
    {
        var itemsDueForRetry = await GetItemsDueForRetryAsync();
        var retryCount = 0;

        foreach (var item in itemsDueForRetry)
        {
            var success = await RetryItemAsync(item.Id);
            if (success) retryCount++;
        }

        _logger.LogInformation("Scheduled {Count} items for retry", retryCount);
        return retryCount;
    }

    /// <inheritdoc />
    public async Task<bool> RetryItemAsync(int itemId)
    {
        var item = await _queueRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        if (item.Status != SyncQueueItemStatus.Failed)
        {
            return false;
        }

        if (item.RetryCount >= item.MaxRetries)
        {
            _logger.LogWarning("Cannot retry item {ItemId}: max retries exceeded", itemId);
            return false;
        }

        // Reset to pending for retry
        item.Status = SyncQueueItemStatus.Pending;
        item.NextRetryAt = null;
        await _queueRepository.UpdateAsync(item);

        _logger.LogInformation("Scheduled sync item {ItemId} for retry", itemId);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<SyncQueueItemDto>> GetItemsDueForRetryAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        var now = DateTime.UtcNow;

        var itemsDue = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Failed
                        && i.IsActive
                        && i.RetryCount < i.MaxRetries
                        && i.NextRetryAt.HasValue
                        && i.NextRetryAt.Value <= now)
            .OrderByDescending(i => (int)i.Priority)
            .ThenBy(i => i.NextRetryAt)
            .ToList();

        return itemsDue.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public int CalculateRetryDelaySeconds(int retryCount)
    {
        return _retryPolicy.GetDelaySeconds(retryCount);
    }

    #endregion

    #region Queue Status

    /// <inheritdoc />
    public async Task<SyncSummaryDto> GetQueueSummaryAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        var activeItems = allItems.Where(i => i.IsActive).ToList();
        var today = DateTime.UtcNow.Date;

        var summary = new SyncSummaryDto
        {
            PendingCount = activeItems.Count(i => i.Status == SyncQueueItemStatus.Pending),
            InProgressCount = activeItems.Count(i => i.Status == SyncQueueItemStatus.InProgress),
            FailedCount = activeItems.Count(i => i.Status == SyncQueueItemStatus.Failed),
            CompletedToday = activeItems.Count(i => i.Status == SyncQueueItemStatus.Completed
                                                    && i.UpdatedAt.HasValue
                                                    && i.UpdatedAt.Value.Date == today),
            ConflictCount = activeItems.Count(i => i.Status == SyncQueueItemStatus.Conflict),
            IsConnected = true // This would be set by connection monitoring
        };

        // Calculate last sync time from most recent completed item
        var lastCompleted = activeItems
            .Where(i => i.Status == SyncQueueItemStatus.Completed && i.UpdatedAt.HasValue)
            .OrderByDescending(i => i.UpdatedAt)
            .FirstOrDefault();

        if (lastCompleted?.UpdatedAt != null)
        {
            summary.LastSyncTime = lastCompleted.UpdatedAt;
            summary.TimeSinceLastSync = DateTime.UtcNow - lastCompleted.UpdatedAt.Value;
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<Dictionary<SyncQueuePriority, int>> GetQueueCountByPriorityAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        var pendingItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Pending && i.IsActive)
            .ToList();

        return pendingItems
            .GroupBy(i => i.Priority)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetQueueCountByEntityTypeAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        var pendingItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Pending && i.IsActive)
            .ToList();

        return pendingItems
            .GroupBy(i => i.EntityType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc />
    public async Task<int> GetPendingCountAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        return allItems.Count(i => i.Status == SyncQueueItemStatus.Pending && i.IsActive);
    }

    /// <inheritdoc />
    public async Task<int> GetFailedCountAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        return allItems.Count(i => i.Status == SyncQueueItemStatus.Failed && i.IsActive);
    }

    /// <inheritdoc />
    public async Task<bool> IsQueueEmptyAsync()
    {
        var pendingCount = await GetPendingCountAsync();
        return pendingCount == 0;
    }

    #endregion

    #region Cleanup

    /// <inheritdoc />
    public async Task<int> CleanupCompletedItemsAsync(int daysOld = 7)
    {
        var allItems = await _queueRepository.GetAllAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldCompletedItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.Completed
                        && i.UpdatedAt.HasValue
                        && i.UpdatedAt.Value < cutoffDate
                        && i.IsActive)
            .ToList();

        var count = 0;
        foreach (var item in oldCompletedItems)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _queueRepository.UpdateAsync(item);
            count++;
        }

        _logger.LogInformation("Cleaned up {Count} completed sync items older than {Days} days", count, daysOld);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> ClearQueueAsync()
    {
        var allItems = await _queueRepository.GetAllAsync();
        var activeItems = allItems.Where(i => i.IsActive).ToList();

        var count = 0;
        foreach (var item in activeItems)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _queueRepository.UpdateAsync(item);
            count++;
        }

        _logger.LogWarning("Cleared {Count} items from sync queue", count);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> ResetStuckItemsAsync(int timeoutMinutes = 30)
    {
        var allItems = await _queueRepository.GetAllAsync();
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        var stuckItems = allItems
            .Where(i => i.Status == SyncQueueItemStatus.InProgress
                        && i.LastAttemptAt.HasValue
                        && i.LastAttemptAt.Value < cutoffTime
                        && i.IsActive)
            .ToList();

        var count = 0;
        foreach (var item in stuckItems)
        {
            item.Status = SyncQueueItemStatus.Pending;
            item.UpdatedAt = DateTime.UtcNow;
            await _queueRepository.UpdateAsync(item);
            count++;

            _logger.LogWarning("Reset stuck sync item {ItemId} (stuck since {StuckSince})",
                item.Id, item.LastAttemptAt);
        }

        return count;
    }

    #endregion

    #region Bulk Operations

    /// <inheritdoc />
    public async Task<List<SyncQueueItemDto>> EnqueueBatchAsync<T>(IEnumerable<T> entities, SyncQueueOperationType operationType, SyncQueuePriority priority = SyncQueuePriority.Normal) where T : BaseEntity
    {
        var results = new List<SyncQueueItemDto>();

        foreach (var entity in entities)
        {
            var item = await EnqueueAsync(entity, operationType, priority);
            results.Add(item);
        }

        _logger.LogInformation("Enqueued batch of {Count} items", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<int> CancelBatchAsync(IEnumerable<int> itemIds)
    {
        var count = 0;
        foreach (var id in itemIds)
        {
            if (await CancelItemAsync(id))
            {
                count++;
            }
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<int> RetryBatchAsync(IEnumerable<int> itemIds)
    {
        var count = 0;
        foreach (var id in itemIds)
        {
            if (await RetryItemAsync(id))
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region Private Helpers

    private static SyncQueueItemDto MapToDto(SyncQueueItem item)
    {
        return new SyncQueueItemDto
        {
            Id = item.Id,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            OperationType = MapOperationType(item.OperationType),
            Priority = MapPriority(item.Priority),
            Status = MapStatus(item.Status),
            Payload = item.Payload,
            RetryCount = item.RetryCount,
            CreatedAt = item.CreatedAt,
            LastAttemptAt = item.LastAttemptAt,
            LastError = item.LastError,
            NextRetryAt = item.NextRetryAt
        };
    }

    private static SyncOperationType MapOperationType(SyncQueueOperationType operationType)
    {
        return operationType switch
        {
            SyncQueueOperationType.Create => SyncOperationType.Create,
            SyncQueueOperationType.Update => SyncOperationType.Update,
            SyncQueueOperationType.Delete => SyncOperationType.Delete,
            _ => SyncOperationType.Create
        };
    }

    private static SyncPriority MapPriority(SyncQueuePriority priority)
    {
        return priority switch
        {
            SyncQueuePriority.Critical => SyncPriority.Critical,
            SyncQueuePriority.High => SyncPriority.High,
            SyncQueuePriority.Normal => SyncPriority.Normal,
            SyncQueuePriority.Low => SyncPriority.Low,
            _ => SyncPriority.Normal
        };
    }

    private static SyncQueueStatus MapStatus(SyncQueueItemStatus status)
    {
        return status switch
        {
            SyncQueueItemStatus.Pending => SyncQueueStatus.Pending,
            SyncQueueItemStatus.InProgress => SyncQueueStatus.InProgress,
            SyncQueueItemStatus.Completed => SyncQueueStatus.Completed,
            SyncQueueItemStatus.Failed => SyncQueueStatus.Failed,
            SyncQueueItemStatus.Cancelled => SyncQueueStatus.Cancelled,
            SyncQueueItemStatus.Conflict => SyncQueueStatus.Conflict,
            _ => SyncQueueStatus.Pending
        };
    }

    #endregion
}
