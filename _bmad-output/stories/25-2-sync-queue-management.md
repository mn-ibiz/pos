# Story 25.2: Sync Queue Management

## Story
**As the** system,
**I want to** queue changes for cloud synchronization,
**So that** data is eventually consistent with central systems.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 25: Offline-First Architecture & Cloud Sync**

## Acceptance Criteria

### AC1: Change Queueing
**Given** data changes locally
**When** saving
**Then** change is added to sync queue with priority

### AC2: Priority Sync
**Given** queue has items
**When** connection is available
**Then** items sync in priority order (eTIMS first)

### AC3: Retry Logic
**Given** sync fails
**When** retrying
**Then** uses exponential backoff and logs failures

## Technical Notes
```csharp
public class SyncQueueItem
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public SyncOperation Operation { get; set; }  // Create, Update, Delete
    public string Payload { get; set; }  // JSON serialized entity
    public SyncPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public int AttemptCount { get; set; }
    public SyncItemStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}

public enum SyncPriority
{
    Critical = 1,   // eTIMS submissions
    High = 2,       // Payments, receipts
    Normal = 3,     // Inventory, products
    Low = 4         // Reports, analytics
}

public enum SyncItemStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public class SyncQueueService
{
    public async Task EnqueueAsync<T>(T entity, SyncOperation operation);
    public async Task<List<SyncQueueItem>> GetPendingItemsAsync(int limit);
    public async Task ProcessQueueAsync(CancellationToken cancellationToken);
    public async Task RetryFailedItemsAsync();
}

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 5;
    public int BaseDelaySeconds { get; set; } = 30;
    // Exponential: 30s, 1m, 2m, 4m, 8m
    public int GetDelaySeconds(int attemptCount) =>
        BaseDelaySeconds * (int)Math.Pow(2, attemptCount - 1);
}
```

## Definition of Done
- [x] Sync queue table and service
- [x] Priority-based processing
- [x] Exponential backoff retry
- [x] Failed item logging
- [x] Queue size monitoring
- [x] Unit tests passing

## Implementation Summary

### Interface Created (ISyncQueueService.cs)
Comprehensive interface with:
- **Queue Operations**: `EnqueueAsync`, `GetPendingItemsAsync`, `GetPendingItemsByTypeAsync`, `GetFailedItemsAsync`, `GetByIdAsync`
- **Processing**: `ProcessQueueAsync`, `ProcessItemAsync`, `MarkAsInProgressAsync`, `MarkAsCompletedAsync`, `MarkAsFailedAsync`, `CancelItemAsync`
- **Retry Operations**: `RetryFailedItemsAsync`, `RetryItemAsync`, `GetItemsDueForRetryAsync`, `CalculateRetryDelaySeconds`
- **Queue Status**: `GetQueueSummaryAsync`, `GetQueueCountByPriorityAsync`, `GetQueueCountByEntityTypeAsync`, `GetPendingCountAsync`, `GetFailedCountAsync`, `IsQueueEmptyAsync`
- **Cleanup**: `CleanupCompletedItemsAsync`, `ClearQueueAsync`, `ResetStuckItemsAsync`
- **Bulk Operations**: `EnqueueBatchAsync`, `CancelBatchAsync`, `RetryBatchAsync`

### Supporting Types (ISyncQueueService.cs)
- `SyncProcessingResult` - Result of queue processing with success/failure counts
- `SyncRetryPolicy` - Configurable retry policy with exponential backoff

### Service Implementation (SyncQueueService.cs)
Full implementation including:
- Priority-based processing (Critical > High > Normal > Low)
- Exponential backoff retry: 30s, 60s, 120s, 240s, 480s... (capped at MaxDelaySeconds)
- Automatic retry scheduling when items fail
- Failed item logging with error messages
- Queue status monitoring with summary statistics
- Cleanup of completed items after configurable retention period
- Reset of stuck items that remain InProgress too long
- JSON serialization of entity payloads
- Batch operations for bulk enqueue/cancel/retry

### Entity (SyncQueueItem - from Story 25.1)
Already created in SyncEntities.cs with:
- `SyncQueueItemStatus` enum: Pending, InProgress, Completed, Failed, Cancelled, Conflict
- `SyncQueuePriority` enum: Low, Normal, High, Critical
- `SyncQueueOperationType` enum: Create, Update, Delete
- Full entity with retry tracking fields

### Unit Tests (SyncQueueServiceTests.cs)
Comprehensive test coverage (40+ tests) including:
- Constructor validation tests
- Enqueue tests with priority and payload serialization
- Get pending/failed items tests with filtering and ordering
- Processing tests with cancellation support
- Status transition tests (InProgress, Completed, Failed)
- Retry tests with exponential backoff verification
- Queue status and summary tests
- Cleanup and reset tests
- Bulk operation tests
- SyncRetryPolicy tests
- SyncProcessingResult tests
