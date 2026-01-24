namespace HospitalityPOS.Core.Entities;

#region Course Enums

/// <summary>
/// Status of a course in an order.
/// </summary>
public enum CourseStatus
{
    /// <summary>
    /// Course is not yet time to fire.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Course is manually held.
    /// </summary>
    Held = 1,

    /// <summary>
    /// Course is scheduled to fire at a specific time.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Course has been sent to prep stations.
    /// </summary>
    Fired = 3,

    /// <summary>
    /// Course items are being prepared.
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// All items done, waiting at expo.
    /// </summary>
    Ready = 5,

    /// <summary>
    /// Course has been delivered to guest.
    /// </summary>
    Served = 6
}

/// <summary>
/// Mode for automatically firing courses.
/// </summary>
public enum CourseFireMode
{
    /// <summary>
    /// All courses must be manually fired.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Fire next course when previous is bumped from expo.
    /// </summary>
    AutoOnBump = 2,

    /// <summary>
    /// Fire courses based on configured timing.
    /// </summary>
    AutoTimed = 3,

    /// <summary>
    /// Timed firing but requires expo confirmation.
    /// </summary>
    AutoTimedWithConfirm = 4
}

/// <summary>
/// Fire status of an individual item within a course.
/// </summary>
public enum ItemFireStatus
{
    /// <summary>
    /// Item is waiting for course to fire.
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// Time to fire, awaiting trigger.
    /// </summary>
    ReadyToFire = 1,

    /// <summary>
    /// Item has been sent to prep station.
    /// </summary>
    Fired = 2,

    /// <summary>
    /// Item is being prepared.
    /// </summary>
    Preparing = 3,

    /// <summary>
    /// Item preparation is complete.
    /// </summary>
    Done = 4,

    /// <summary>
    /// Item is on hold.
    /// </summary>
    Held = 5
}

#endregion

#region Course Entities

/// <summary>
/// Defines a course type (e.g., Drinks, Appetizers, Mains, Desserts).
/// </summary>
public class CourseDefinition : BaseEntity
{
    /// <summary>
    /// Name of the course (e.g., "Appetizers", "Mains", "Desserts").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Course number for ordering (1, 2, 3, 4...).
    /// </summary>
    public int CourseNumber { get; set; }

    /// <summary>
    /// Default delay in minutes after previous course fires.
    /// </summary>
    public int DefaultDelayMinutes { get; set; } = 10;

    /// <summary>
    /// Icon URL for UI display.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Color code for UI display (hex format).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Description of this course.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Store this course definition belongs to.
    /// </summary>
    public int StoreId { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual ICollection<KdsCourseState> CourseStates { get; set; } = new List<KdsCourseState>();
}

/// <summary>
/// Tracks the state of a course within a specific KDS order.
/// </summary>
public class KdsCourseState : BaseEntity
{
    /// <summary>
    /// The KDS order this course belongs to.
    /// </summary>
    public int KdsOrderId { get; set; }

    /// <summary>
    /// The course definition.
    /// </summary>
    public int? CourseDefinitionId { get; set; }

    /// <summary>
    /// Course number within the order.
    /// </summary>
    public int CourseNumber { get; set; }

    /// <summary>
    /// Name of the course (cached for display).
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the course.
    /// </summary>
    public CourseStatus Status { get; set; } = CourseStatus.Pending;

    /// <summary>
    /// When the course is scheduled to fire.
    /// </summary>
    public DateTime? ScheduledFireAt { get; set; }

    /// <summary>
    /// When the course was actually fired.
    /// </summary>
    public DateTime? FiredAt { get; set; }

    /// <summary>
    /// User who fired the course.
    /// </summary>
    public int? FiredByUserId { get; set; }

    /// <summary>
    /// When all items in the course were completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the course was marked as served.
    /// </summary>
    public DateTime? ServedAt { get; set; }

    /// <summary>
    /// User who marked the course as served.
    /// </summary>
    public int? ServedByUserId { get; set; }

    /// <summary>
    /// Whether the course is on hold.
    /// </summary>
    public bool IsOnHold { get; set; }

    /// <summary>
    /// Reason for holding the course.
    /// </summary>
    public string? HoldReason { get; set; }

    /// <summary>
    /// User who put the course on hold.
    /// </summary>
    public int? HeldByUserId { get; set; }

    /// <summary>
    /// When the course was put on hold.
    /// </summary>
    public DateTime? HeldAt { get; set; }

    /// <summary>
    /// Target minutes after previous course.
    /// </summary>
    public int TargetMinutesAfterPrevious { get; set; } = 10;

    /// <summary>
    /// Display color (inherited from CourseDefinition or overridden).
    /// </summary>
    public string? DisplayColor { get; set; }

    /// <summary>
    /// Total items in this course.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of completed items.
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Calculates progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => TotalItems > 0 ? (CompletedItems * 100) / TotalItems : 0;

    /// <summary>
    /// Whether this course is the current/active one.
    /// </summary>
    public bool IsCurrent => Status == CourseStatus.Fired || Status == CourseStatus.InProgress;

    // Navigation properties
    public virtual KdsOrder? KdsOrder { get; set; }
    public virtual CourseDefinition? CourseDefinition { get; set; }
    public virtual User? FiredByUser { get; set; }
    public virtual User? ServedByUser { get; set; }
    public virtual User? HeldByUser { get; set; }
    public virtual ICollection<KdsOrderItem> Items { get; set; } = new List<KdsOrderItem>();
}

/// <summary>
/// Store-level configuration for course firing.
/// </summary>
public class CourseConfiguration : BaseEntity
{
    /// <summary>
    /// Store this configuration applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Whether coursing is enabled for this store.
    /// </summary>
    public bool EnableCoursing { get; set; }

    /// <summary>
    /// Mode for firing courses.
    /// </summary>
    public CourseFireMode FireMode { get; set; } = CourseFireMode.AutoOnBump;

    /// <summary>
    /// Default pacing between courses in minutes.
    /// </summary>
    public int DefaultCoursePacingMinutes { get; set; } = 10;

    /// <summary>
    /// Whether to auto-fire next course when previous is bumped.
    /// </summary>
    public bool AutoFireOnPreviousBump { get; set; } = true;

    /// <summary>
    /// Whether to show held courses on prep stations (grayed out).
    /// </summary>
    public bool ShowHeldCoursesOnPrepStation { get; set; }

    /// <summary>
    /// Whether expo confirmation is required before firing.
    /// </summary>
    public bool RequireExpoConfirmation { get; set; }

    /// <summary>
    /// Whether to allow manual fire override.
    /// </summary>
    public bool AllowManualFireOverride { get; set; } = true;

    /// <summary>
    /// Whether to enable rush mode (fire all courses at once).
    /// </summary>
    public bool AllowRushMode { get; set; } = true;

    /// <summary>
    /// Whether first course fires automatically on order receipt.
    /// </summary>
    public bool AutoFireFirstCourse { get; set; } = true;

    /// <summary>
    /// Grace period in seconds before scheduled fire.
    /// </summary>
    public int FireGracePeriodSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to show countdown timer to next course.
    /// </summary>
    public bool ShowCountdownToNextCourse { get; set; } = true;

    /// <summary>
    /// Whether to alert when course is ready to fire.
    /// </summary>
    public bool AlertOnReadyToFire { get; set; } = true;

    /// <summary>
    /// Sound file for course fire alert.
    /// </summary>
    public string? FireAlertSound { get; set; }

    // Navigation property
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Log entry for course firing actions.
/// </summary>
public class CourseFiringLog : BaseEntity
{
    /// <summary>
    /// The KDS order this log entry belongs to.
    /// </summary>
    public int KdsOrderId { get; set; }

    /// <summary>
    /// The course state this log entry belongs to.
    /// </summary>
    public int? CourseStateId { get; set; }

    /// <summary>
    /// Course number affected.
    /// </summary>
    public int CourseNumber { get; set; }

    /// <summary>
    /// Type of action performed.
    /// </summary>
    public CourseFiringAction Action { get; set; }

    /// <summary>
    /// Previous status before the action.
    /// </summary>
    public CourseStatus? PreviousStatus { get; set; }

    /// <summary>
    /// New status after the action.
    /// </summary>
    public CourseStatus NewStatus { get; set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Timestamp of the action.
    /// </summary>
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the action.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Station ID if action was performed at a station.
    /// </summary>
    public int? StationId { get; set; }

    // Navigation properties
    public virtual KdsOrder? KdsOrder { get; set; }
    public virtual KdsCourseState? CourseState { get; set; }
    public virtual User? User { get; set; }
    public virtual KdsStation? Station { get; set; }
}

/// <summary>
/// Types of course firing actions.
/// </summary>
public enum CourseFiringAction
{
    /// <summary>
    /// Course was initialized.
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// Course was scheduled for firing.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Course was manually fired.
    /// </summary>
    ManualFire = 3,

    /// <summary>
    /// Course was auto-fired on bump.
    /// </summary>
    AutoFiredOnBump = 4,

    /// <summary>
    /// Course was auto-fired on timer.
    /// </summary>
    AutoFiredOnTimer = 5,

    /// <summary>
    /// Course was put on hold.
    /// </summary>
    Held = 6,

    /// <summary>
    /// Course hold was released.
    /// </summary>
    HoldReleased = 7,

    /// <summary>
    /// Course was rushed (all items fired).
    /// </summary>
    Rushed = 8,

    /// <summary>
    /// Course was marked ready.
    /// </summary>
    MarkedReady = 9,

    /// <summary>
    /// Course was marked served.
    /// </summary>
    MarkedServed = 10,

    /// <summary>
    /// Course delay was changed.
    /// </summary>
    DelayChanged = 11
}

#endregion
