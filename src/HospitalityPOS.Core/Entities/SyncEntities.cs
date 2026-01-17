namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Direction of data synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>Store to HQ upload.</summary>
    Upload = 1,
    /// <summary>HQ to Store download.</summary>
    Download = 2,
    /// <summary>Two-way sync.</summary>
    Bidirectional = 3
}

/// <summary>
/// Status of a sync batch.
/// </summary>
public enum SyncBatchStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    PartiallyCompleted = 3,
    Failed = 4,
    Cancelled = 5
}

/// <summary>
/// Conflict resolution winner.
/// </summary>
public enum ConflictWinner
{
    HQ = 1,
    Store = 2,
    LatestTimestamp = 3,
    Manual = 4
}

/// <summary>
/// Type of entity being synced.
/// </summary>
public enum SyncEntityType
{
    Product = 1,
    Category = 2,
    Price = 3,
    Promotion = 4,
    Receipt = 5,
    Order = 6,
    Inventory = 7,
    StockMovement = 8,
    Employee = 9,
    Customer = 10,
    LoyaltyMember = 11
}

/// <summary>
/// Configuration for store synchronization.
/// </summary>
public class SyncConfiguration : BaseEntity
{
    public int StoreId { get; set; }
    public int SyncIntervalSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;
    public bool AutoSyncOnStartup { get; set; } = true;
    public int MaxBatchSize { get; set; } = 100;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 60;
    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public string? LastSyncError { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual ICollection<SyncEntityRule> EntityRules { get; set; } = new List<SyncEntityRule>();
}

/// <summary>
/// Rule for syncing a specific entity type.
/// </summary>
public class SyncEntityRule : BaseEntity
{
    public int SyncConfigurationId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public SyncDirection Direction { get; set; }
    public ConflictWinner ConflictResolution { get; set; } = ConflictWinner.HQ;
    public bool FlagConflictsForReview { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    // Navigation
    public virtual SyncConfiguration? SyncConfiguration { get; set; }
}

/// <summary>
/// A batch of records being synchronized.
/// </summary>
public class SyncBatch : BaseEntity
{
    public int StoreId { get; set; }
    public SyncDirection Direction { get; set; }
    public SyncEntityType EntityType { get; set; }
    public int RecordCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int ConflictCount { get; set; }
    public SyncBatchStatus Status { get; set; } = SyncBatchStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BatchData { get; set; } // JSON serialized data

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual ICollection<SyncRecord> Records { get; set; } = new List<SyncRecord>();
    public virtual ICollection<SyncConflict> Conflicts { get; set; } = new List<SyncConflict>();
}

/// <summary>
/// Individual record in a sync batch.
/// </summary>
public class SyncRecord : BaseEntity
{
    public int SyncBatchId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string EntityData { get; set; } = string.Empty; // JSON serialized
    public DateTime EntityTimestamp { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public virtual SyncBatch? SyncBatch { get; set; }
}

/// <summary>
/// A sync conflict requiring resolution.
/// </summary>
public class SyncConflict : BaseEntity
{
    public int SyncBatchId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string LocalData { get; set; } = string.Empty;
    public string RemoteData { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
    public bool IsResolved { get; set; }
    public ConflictWinner? Resolution { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigation
    public virtual SyncBatch? SyncBatch { get; set; }
}

/// <summary>
/// Log of sync operations.
/// </summary>
public class SyncLog : BaseEntity
{
    public int StoreId { get; set; }
    public int? SyncBatchId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long? DurationMs { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual SyncBatch? SyncBatch { get; set; }
}

/// <summary>
/// Status of a sync queue item.
/// </summary>
public enum SyncQueueItemStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Conflict = 6
}

/// <summary>
/// Priority level for sync queue items.
/// </summary>
public enum SyncQueuePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4  // For eTIMS and compliance-related
}

/// <summary>
/// Type of sync queue operation.
/// </summary>
public enum SyncQueueOperationType
{
    Create = 1,
    Update = 2,
    Delete = 3
}

/// <summary>
/// Queue item for pending sync operations.
/// </summary>
public class SyncQueueItem : BaseEntity
{
    /// <summary>Type of entity being synced.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID of the entity being synced.</summary>
    public int EntityId { get; set; }

    /// <summary>Type of operation.</summary>
    public SyncQueueOperationType OperationType { get; set; }

    /// <summary>Priority of this sync item.</summary>
    public SyncQueuePriority Priority { get; set; } = SyncQueuePriority.Normal;

    /// <summary>Current status.</summary>
    public SyncQueueItemStatus Status { get; set; } = SyncQueueItemStatus.Pending;

    /// <summary>Serialized payload data.</summary>
    public string? Payload { get; set; }

    /// <summary>Number of retry attempts.</summary>
    public int RetryCount { get; set; }

    /// <summary>Maximum retry attempts allowed.</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>Time of last sync attempt.</summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Error from last failed attempt.</summary>
    public string? LastError { get; set; }

    /// <summary>Scheduled time for next retry.</summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>Store this sync item belongs to.</summary>
    public int? StoreId { get; set; }

    /// <summary>User who created this sync item.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Reference to the sync batch if part of one.</summary>
    public int? SyncBatchId { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual SyncBatch? SyncBatch { get; set; }
}
