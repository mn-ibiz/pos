using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing the sync queue.
/// </summary>
public interface ISyncQueueService
{
    #region Queue Operations

    /// <summary>
    /// Enqueues an entity for synchronization.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="entity">The entity to sync.</param>
    /// <param name="operationType">The operation type (Create, Update, Delete).</param>
    /// <param name="priority">Sync priority.</param>
    /// <returns>The created sync queue item.</returns>
    Task<SyncQueueItemDto> EnqueueAsync<T>(T entity, SyncQueueOperationType operationType, SyncQueuePriority priority = SyncQueuePriority.Normal) where T : BaseEntity;

    /// <summary>
    /// Enqueues a sync item with explicit entity type and ID.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="payload">JSON serialized payload.</param>
    /// <param name="priority">Sync priority.</param>
    /// <returns>The created sync queue item.</returns>
    Task<SyncQueueItemDto> EnqueueAsync(string entityType, int entityId, SyncQueueOperationType operationType, string? payload = null, SyncQueuePriority priority = SyncQueuePriority.Normal);

    /// <summary>
    /// Gets pending sync items ordered by priority.
    /// </summary>
    /// <param name="limit">Maximum items to return.</param>
    /// <returns>List of pending items.</returns>
    Task<List<SyncQueueItemDto>> GetPendingItemsAsync(int limit = 100);

    /// <summary>
    /// Gets pending items by entity type.
    /// </summary>
    /// <param name="entityType">The entity type to filter by.</param>
    /// <param name="limit">Maximum items to return.</param>
    /// <returns>List of pending items for the entity type.</returns>
    Task<List<SyncQueueItemDto>> GetPendingItemsByTypeAsync(string entityType, int limit = 100);

    /// <summary>
    /// Gets all failed items.
    /// </summary>
    /// <param name="limit">Maximum items to return.</param>
    /// <returns>List of failed items.</returns>
    Task<List<SyncQueueItemDto>> GetFailedItemsAsync(int limit = 100);

    /// <summary>
    /// Gets a sync queue item by ID.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The sync queue item or null.</returns>
    Task<SyncQueueItemDto?> GetByIdAsync(int id);

    #endregion

    #region Processing

    /// <summary>
    /// Processes the sync queue.
    /// </summary>
    /// <param name="batchSize">Number of items to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing result.</returns>
    Task<SyncProcessingResult> ProcessQueueAsync(int batchSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a single queue item.
    /// </summary>
    /// <param name="itemId">The item ID to process.</param>
    /// <returns>True if successful.</returns>
    Task<bool> ProcessItemAsync(int itemId);

    /// <summary>
    /// Marks an item as in progress.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> MarkAsInProgressAsync(int itemId);

    /// <summary>
    /// Marks an item as completed.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> MarkAsCompletedAsync(int itemId);

    /// <summary>
    /// Marks an item as failed with error message.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if successful.</returns>
    Task<bool> MarkAsFailedAsync(int itemId, string errorMessage);

    /// <summary>
    /// Cancels a pending item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> CancelItemAsync(int itemId);

    #endregion

    #region Retry Operations

    /// <summary>
    /// Retries failed items using exponential backoff.
    /// </summary>
    /// <returns>Number of items scheduled for retry.</returns>
    Task<int> RetryFailedItemsAsync();

    /// <summary>
    /// Retries a specific failed item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>True if retry was scheduled.</returns>
    Task<bool> RetryItemAsync(int itemId);

    /// <summary>
    /// Gets items due for retry.
    /// </summary>
    /// <returns>List of items ready to retry.</returns>
    Task<List<SyncQueueItemDto>> GetItemsDueForRetryAsync();

    /// <summary>
    /// Calculates the next retry time using exponential backoff.
    /// </summary>
    /// <param name="retryCount">Current retry count.</param>
    /// <returns>Next retry delay in seconds.</returns>
    int CalculateRetryDelaySeconds(int retryCount);

    #endregion

    #region Queue Status

    /// <summary>
    /// Gets the sync queue summary.
    /// </summary>
    /// <returns>Queue summary statistics.</returns>
    Task<SyncSummaryDto> GetQueueSummaryAsync();

    /// <summary>
    /// Gets queue statistics by priority.
    /// </summary>
    /// <returns>Dictionary of priority to count.</returns>
    Task<Dictionary<SyncQueuePriority, int>> GetQueueCountByPriorityAsync();

    /// <summary>
    /// Gets queue statistics by entity type.
    /// </summary>
    /// <returns>Dictionary of entity type to count.</returns>
    Task<Dictionary<string, int>> GetQueueCountByEntityTypeAsync();

    /// <summary>
    /// Gets the total count of pending items.
    /// </summary>
    /// <returns>Number of pending items.</returns>
    Task<int> GetPendingCountAsync();

    /// <summary>
    /// Gets the total count of failed items.
    /// </summary>
    /// <returns>Number of failed items.</returns>
    Task<int> GetFailedCountAsync();

    /// <summary>
    /// Checks if the queue is empty.
    /// </summary>
    /// <returns>True if queue is empty.</returns>
    Task<bool> IsQueueEmptyAsync();

    #endregion

    #region Cleanup

    /// <summary>
    /// Removes completed items older than specified days.
    /// </summary>
    /// <param name="daysOld">Number of days to keep.</param>
    /// <returns>Number of items removed.</returns>
    Task<int> CleanupCompletedItemsAsync(int daysOld = 7);

    /// <summary>
    /// Clears all items from the queue.
    /// </summary>
    /// <returns>Number of items cleared.</returns>
    Task<int> ClearQueueAsync();

    /// <summary>
    /// Resets items that have been stuck in InProgress for too long.
    /// </summary>
    /// <param name="timeoutMinutes">Timeout in minutes.</param>
    /// <returns>Number of items reset.</returns>
    Task<int> ResetStuckItemsAsync(int timeoutMinutes = 30);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Enqueues multiple entities for sync.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="entities">The entities to sync.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="priority">Sync priority.</param>
    /// <returns>List of created queue items.</returns>
    Task<List<SyncQueueItemDto>> EnqueueBatchAsync<T>(IEnumerable<T> entities, SyncQueueOperationType operationType, SyncQueuePriority priority = SyncQueuePriority.Normal) where T : BaseEntity;

    /// <summary>
    /// Cancels multiple items.
    /// </summary>
    /// <param name="itemIds">The item IDs to cancel.</param>
    /// <returns>Number of items cancelled.</returns>
    Task<int> CancelBatchAsync(IEnumerable<int> itemIds);

    /// <summary>
    /// Retries multiple failed items.
    /// </summary>
    /// <param name="itemIds">The item IDs to retry.</param>
    /// <returns>Number of items scheduled for retry.</returns>
    Task<int> RetryBatchAsync(IEnumerable<int> itemIds);

    #endregion
}

/// <summary>
/// Result of sync queue processing.
/// </summary>
public class SyncProcessingResult
{
    public int TotalProcessed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public long DurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => Failed == 0;

    public static SyncProcessingResult Success(int processed, int succeeded, DateTime started)
    {
        return new SyncProcessingResult
        {
            TotalProcessed = processed,
            Succeeded = succeeded,
            Failed = 0,
            StartedAt = started,
            CompletedAt = DateTime.UtcNow
        };
    }

    public static SyncProcessingResult WithFailures(int processed, int succeeded, int failed, List<string> errors, DateTime started)
    {
        return new SyncProcessingResult
        {
            TotalProcessed = processed,
            Succeeded = succeeded,
            Failed = failed,
            StartedAt = started,
            CompletedAt = DateTime.UtcNow,
            Errors = errors
        };
    }
}

/// <summary>
/// Configuration for sync queue retry policy.
/// </summary>
public class SyncRetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Base delay in seconds for exponential backoff.
    /// </summary>
    public int BaseDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay in seconds.
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Calculates the delay for a retry attempt using exponential backoff.
    /// </summary>
    /// <param name="attemptCount">Current attempt count (1-based).</param>
    /// <returns>Delay in seconds.</returns>
    public int GetDelaySeconds(int attemptCount)
    {
        if (attemptCount <= 0) return BaseDelaySeconds;

        // Exponential backoff: 30s, 60s, 120s, 240s, 480s...
        var delay = BaseDelaySeconds * (int)Math.Pow(2, attemptCount - 1);
        return Math.Min(delay, MaxDelaySeconds);
    }
}
