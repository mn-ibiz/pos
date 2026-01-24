using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Prep timing mode determining how items are grouped for simultaneous completion.
/// </summary>
public enum PrepTimingMode
{
    /// <summary>Prep timing disabled.</summary>
    Disabled = 0,

    /// <summary>All items in a course ready together.</summary>
    CourseLevel = 1,

    /// <summary>All items in an order ready together.</summary>
    OrderLevel = 2,

    /// <summary>All items at the same station ready together.</summary>
    StationLevel = 3
}

/// <summary>
/// Status of an item in the fire schedule.
/// </summary>
public enum ItemFireStatus
{
    /// <summary>Not yet time to fire.</summary>
    Waiting = 0,

    /// <summary>Scheduled to fire at specific time.</summary>
    Scheduled = 1,

    /// <summary>Past scheduled time, awaiting action.</summary>
    ReadyToFire = 2,

    /// <summary>Sent to prep station.</summary>
    Fired = 3,

    /// <summary>In progress at station.</summary>
    Preparing = 4,

    /// <summary>Completed.</summary>
    Done = 5,

    /// <summary>On hold.</summary>
    Held = 6,

    /// <summary>Cancelled.</summary>
    Cancelled = 7
}

/// <summary>
/// How modifier prep time affects the parent item.
/// </summary>
public enum PrepTimeAdjustmentType
{
    /// <summary>Adds to parent item prep time (e.g., well-done steak).</summary>
    Integral = 1,

    /// <summary>Has its own prep time, fires separately.</summary>
    Independent = 2,

    /// <summary>Doesn't affect timing.</summary>
    Ignored = 3
}

#endregion

#region Configuration

/// <summary>
/// Store-level configuration for prep timing system.
/// </summary>
public class PrepTimingConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID for store-specific config.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Whether prep timing is enabled for this store.
    /// </summary>
    public bool EnablePrepTiming { get; set; }

    /// <summary>
    /// Default prep time in seconds for items without configured time.
    /// </summary>
    public int DefaultPrepTimeSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Minimum fire delay in seconds (prevents immediate fire).
    /// </summary>
    public int MinPrepTimeSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Buffer time in seconds added before target ready time.
    /// </summary>
    public int TargetReadyBufferSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Whether manual fire override is allowed.
    /// </summary>
    public bool AllowManualFireOverride { get; set; } = true;

    /// <summary>
    /// Whether to show waiting items on station display.
    /// </summary>
    public bool ShowWaitingItemsOnStation { get; set; } = true;

    /// <summary>
    /// Prep timing mode.
    /// </summary>
    public PrepTimingMode Mode { get; set; } = PrepTimingMode.CourseLevel;

    /// <summary>
    /// Whether to auto-fire items when their scheduled time arrives.
    /// </summary>
    public bool AutoFireEnabled { get; set; } = true;

    /// <summary>
    /// Seconds after scheduled fire time before item is considered overdue.
    /// </summary>
    public int OverdueThresholdSeconds { get; set; } = 120; // 2 minutes

    /// <summary>
    /// Whether to alert on overdue items.
    /// </summary>
    public bool AlertOnOverdue { get; set; } = true;

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion

#region Fire Schedule

/// <summary>
/// Schedule for when a KDS item should be fired to a prep station.
/// </summary>
public class ItemFireSchedule : BaseEntity
{
    /// <summary>
    /// KDS order item this schedule is for.
    /// </summary>
    public int KdsOrderItemId { get; set; }

    /// <summary>
    /// KDS order this item belongs to.
    /// </summary>
    public int KdsOrderId { get; set; }

    /// <summary>
    /// Course number if using course-based timing.
    /// </summary>
    public int? CourseNumber { get; set; }

    /// <summary>
    /// Product ID for this item.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Station this item will be sent to.
    /// </summary>
    public int? StationId { get; set; }

    /// <summary>
    /// Total prep time in seconds (including modifiers).
    /// </summary>
    public int PrepTimeSeconds { get; set; }

    /// <summary>
    /// When the order was received.
    /// </summary>
    public DateTime OrderReceivedAt { get; set; }

    /// <summary>
    /// Target time when item should be ready.
    /// </summary>
    public DateTime TargetReadyAt { get; set; }

    /// <summary>
    /// Scheduled time to fire the item to the station.
    /// </summary>
    public DateTime ScheduledFireAt { get; set; }

    /// <summary>
    /// When the item was actually fired.
    /// </summary>
    public DateTime? ActualFiredAt { get; set; }

    /// <summary>
    /// When the item was actually completed.
    /// </summary>
    public DateTime? ActualReadyAt { get; set; }

    /// <summary>
    /// Current status of this item.
    /// </summary>
    public ItemFireStatus Status { get; set; } = ItemFireStatus.Waiting;

    /// <summary>
    /// Whether this item was manually fired before its scheduled time.
    /// </summary>
    public bool WasManuallyFired { get; set; }

    /// <summary>
    /// User who manually fired this item.
    /// </summary>
    public int? FiredByUserId { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Notes or reason for manual override.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual KdsOrderItem? KdsOrderItem { get; set; }
    public virtual KdsOrder? KdsOrder { get; set; }
    public virtual Product? Product { get; set; }
    public virtual KdsStation? Station { get; set; }
    public virtual User? FiredByUser { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Product Prep Time Extension

/// <summary>
/// Prep time configuration for a product.
/// Stored separately to allow category-level defaults.
/// </summary>
public class ProductPrepTimeConfig : BaseEntity
{
    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Prep time in minutes.
    /// </summary>
    public int PrepTimeMinutes { get; set; }

    /// <summary>
    /// Additional seconds (0-59).
    /// </summary>
    public int PrepTimeSeconds { get; set; }

    /// <summary>
    /// Total prep time in seconds (calculated).
    /// </summary>
    public int TotalPrepTimeSeconds => (PrepTimeMinutes * 60) + PrepTimeSeconds;

    /// <summary>
    /// Whether prep timing is enabled for this product.
    /// </summary>
    public bool UsesPrepTiming { get; set; } = true;

    /// <summary>
    /// Whether this item's timing is integral to parent order timing.
    /// </summary>
    public bool IsTimingIntegral { get; set; } = true;

    /// <summary>
    /// Store ID for store-specific overrides, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Prep time adjustment for a modifier.
/// </summary>
public class ModifierPrepTimeAdjustment : BaseEntity
{
    /// <summary>
    /// Modifier item ID.
    /// </summary>
    public int ModifierItemId { get; set; }

    /// <summary>
    /// Adjustment in seconds (positive adds time, negative reduces).
    /// </summary>
    public int AdjustmentSeconds { get; set; }

    /// <summary>
    /// How this adjustment affects the parent item.
    /// </summary>
    public PrepTimeAdjustmentType AdjustmentType { get; set; } = PrepTimeAdjustmentType.Integral;

    /// <summary>
    /// Store ID for store-specific overrides, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual ModifierItem? ModifierItem { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Category Default Prep Time

/// <summary>
/// Default prep time for a category (used when product doesn't have specific time).
/// </summary>
public class CategoryPrepTimeDefault : BaseEntity
{
    /// <summary>
    /// Category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Default prep time in minutes.
    /// </summary>
    public int DefaultPrepTimeMinutes { get; set; } = 5;

    /// <summary>
    /// Additional seconds (0-59).
    /// </summary>
    public int DefaultPrepTimeSeconds { get; set; }

    /// <summary>
    /// Total default prep time in seconds.
    /// </summary>
    public int TotalPrepTimeSeconds => (DefaultPrepTimeMinutes * 60) + DefaultPrepTimeSeconds;

    /// <summary>
    /// Store ID for store-specific overrides, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Prep Timing Analytics

/// <summary>
/// Daily prep timing accuracy metrics.
/// </summary>
public class PrepTimingDailyMetrics : BaseEntity
{
    /// <summary>
    /// Date of metrics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Total items scheduled.
    /// </summary>
    public int TotalItemsScheduled { get; set; }

    /// <summary>
    /// Items fired on time.
    /// </summary>
    public int ItemsFiredOnTime { get; set; }

    /// <summary>
    /// Items fired late.
    /// </summary>
    public int ItemsFiredLate { get; set; }

    /// <summary>
    /// Items manually fired.
    /// </summary>
    public int ItemsManuallyFired { get; set; }

    /// <summary>
    /// Items completed within target time.
    /// </summary>
    public int ItemsCompletedOnTarget { get; set; }

    /// <summary>
    /// Items completed early (more than 30 sec before target).
    /// </summary>
    public int ItemsCompletedEarly { get; set; }

    /// <summary>
    /// Items completed late (more than 30 sec after target).
    /// </summary>
    public int ItemsCompletedLate { get; set; }

    /// <summary>
    /// Average deviation from target time in seconds.
    /// </summary>
    public int AverageDeviationSeconds { get; set; }

    /// <summary>
    /// Accuracy rate (items completed within +/- 30 sec of target).
    /// </summary>
    public decimal AccuracyRate { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Product-level prep time accuracy tracking.
/// </summary>
public class ProductPrepTimeAccuracy : BaseEntity
{
    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Configured prep time in seconds.
    /// </summary>
    public int ConfiguredPrepTimeSeconds { get; set; }

    /// <summary>
    /// Average actual prep time in seconds.
    /// </summary>
    public int AverageActualPrepTimeSeconds { get; set; }

    /// <summary>
    /// Number of samples used for average.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Standard deviation in seconds.
    /// </summary>
    public decimal StandardDeviationSeconds { get; set; }

    /// <summary>
    /// Minimum actual prep time observed.
    /// </summary>
    public int MinPrepTimeSeconds { get; set; }

    /// <summary>
    /// Maximum actual prep time observed.
    /// </summary>
    public int MaxPrepTimeSeconds { get; set; }

    /// <summary>
    /// Suggested prep time based on actual data.
    /// </summary>
    public int SuggestedPrepTimeSeconds { get; set; }

    /// <summary>
    /// Accuracy rate for this product.
    /// </summary>
    public decimal AccuracyRate { get; set; }

    /// <summary>
    /// Last time accuracy was calculated.
    /// </summary>
    public DateTime LastCalculatedAt { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion
