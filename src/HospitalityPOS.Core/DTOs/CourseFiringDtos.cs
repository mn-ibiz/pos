using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Course Definition DTOs

/// <summary>
/// DTO for course definition.
/// </summary>
public class CourseDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CourseNumber { get; set; }
    public int DefaultDelayMinutes { get; set; }
    public string? IconUrl { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int StoreId { get; set; }
}

/// <summary>
/// DTO for creating/updating a course definition.
/// </summary>
public class CourseDefinitionCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int CourseNumber { get; set; }
    public int DefaultDelayMinutes { get; set; } = 10;
    public string? IconUrl { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public int StoreId { get; set; }
}

#endregion

#region Course Configuration DTOs

/// <summary>
/// DTO for course configuration.
/// </summary>
public class CourseConfigurationDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public bool EnableCoursing { get; set; }
    public CourseFireMode FireMode { get; set; }
    public string FireModeDisplay => FireMode switch
    {
        CourseFireMode.Manual => "Manual Fire Only",
        CourseFireMode.AutoOnBump => "Auto-Fire When Previous Bumped",
        CourseFireMode.AutoTimed => "Auto-Fire on Timer",
        CourseFireMode.AutoTimedWithConfirm => "Timed with Confirmation",
        _ => "Unknown"
    };
    public int DefaultCoursePacingMinutes { get; set; }
    public bool AutoFireOnPreviousBump { get; set; }
    public bool ShowHeldCoursesOnPrepStation { get; set; }
    public bool RequireExpoConfirmation { get; set; }
    public bool AllowManualFireOverride { get; set; }
    public bool AllowRushMode { get; set; }
    public bool AutoFireFirstCourse { get; set; }
    public int FireGracePeriodSeconds { get; set; }
    public bool ShowCountdownToNextCourse { get; set; }
    public bool AlertOnReadyToFire { get; set; }
    public string? FireAlertSound { get; set; }
}

/// <summary>
/// DTO for updating course configuration.
/// </summary>
public class CourseConfigurationUpdateDto
{
    public bool EnableCoursing { get; set; }
    public CourseFireMode FireMode { get; set; }
    public int DefaultCoursePacingMinutes { get; set; }
    public bool AutoFireOnPreviousBump { get; set; }
    public bool ShowHeldCoursesOnPrepStation { get; set; }
    public bool RequireExpoConfirmation { get; set; }
    public bool AllowManualFireOverride { get; set; }
    public bool AllowRushMode { get; set; }
    public bool AutoFireFirstCourse { get; set; }
    public int FireGracePeriodSeconds { get; set; }
    public bool ShowCountdownToNextCourse { get; set; }
    public bool AlertOnReadyToFire { get; set; }
    public string? FireAlertSound { get; set; }
}

#endregion

#region Course State DTOs

/// <summary>
/// DTO for course state.
/// </summary>
public class KdsCourseStateDto
{
    public int Id { get; set; }
    public int KdsOrderId { get; set; }
    public int CourseNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public CourseStatus Status { get; set; }
    public string StatusDisplay => Status switch
    {
        CourseStatus.Pending => "Pending",
        CourseStatus.Held => "On Hold",
        CourseStatus.Scheduled => "Scheduled",
        CourseStatus.Fired => "Fired",
        CourseStatus.InProgress => "In Progress",
        CourseStatus.Ready => "Ready",
        CourseStatus.Served => "Served",
        _ => "Unknown"
    };
    public DateTime? ScheduledFireAt { get; set; }
    public DateTime? FiredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }
    public DateTime? HeldAt { get; set; }
    public int TargetMinutesAfterPrevious { get; set; }
    public string? DisplayColor { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int ProgressPercentage { get; set; }
    public bool IsCurrent { get; set; }
    public TimeSpan? TimeUntilFire => ScheduledFireAt.HasValue && ScheduledFireAt > DateTime.UtcNow
        ? ScheduledFireAt.Value - DateTime.UtcNow
        : null;
    public TimeSpan? ElapsedSinceFire => FiredAt.HasValue
        ? DateTime.UtcNow - FiredAt.Value
        : null;
    public List<KdsCourseItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for an item within a course.
/// </summary>
public class KdsCourseItemDto
{
    public int Id { get; set; }
    public int KdsOrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Modifiers { get; set; }
    public string? SpecialInstructions { get; set; }
    public ItemFireStatus FireStatus { get; set; }
    public KdsItemStatus ItemStatus { get; set; }
    public DateTime? ScheduledFireAt { get; set; }
    public DateTime? FiredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }
    public int? StationId { get; set; }
    public string? StationName { get; set; }
}

#endregion

#region Course Firing Result DTOs

/// <summary>
/// Result of firing a course.
/// </summary>
public class FireCourseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CourseNumber { get; set; }
    public List<KdsCourseItemDto> FiredItems { get; set; } = new();
    public DateTime FiredAt { get; set; }
    public int? NextCourseNumber { get; set; }
    public DateTime? NextCourseScheduledAt { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of holding a course.
/// </summary>
public class HoldCourseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CourseNumber { get; set; }
    public string? HoldReason { get; set; }
    public DateTime HeldAt { get; set; }
    public int HeldByUserId { get; set; }
    public string? HeldByUserName { get; set; }
}

/// <summary>
/// Result of completing a course.
/// </summary>
public class CourseCompletionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CourseNumber { get; set; }
    public CourseStatus NewStatus { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public bool AllItemsComplete { get; set; }
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
}

/// <summary>
/// Information about a held course.
/// </summary>
public class HeldCourse
{
    public int KdsOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public int CourseNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? HoldReason { get; set; }
    public DateTime HeldAt { get; set; }
    public string HeldByUserName { get; set; } = string.Empty;
    public TimeSpan HeldDuration => DateTime.UtcNow - HeldAt;
    public int ItemCount { get; set; }
}

#endregion

#region Course Timing DTOs

/// <summary>
/// Course timing information for an order.
/// </summary>
public class CourseTiming
{
    public int KdsOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderReceivedAt { get; set; }
    public List<CourseTimingEntry> Courses { get; set; } = new();
    public TimeSpan EstimatedTotalTime { get; set; }
    public DateTime EstimatedCompletionTime { get; set; }
    public bool IsOnTrack { get; set; }
    public TimeSpan? DelayAmount { get; set; }
}

/// <summary>
/// Timing entry for a single course.
/// </summary>
public class CourseTimingEntry
{
    public int CourseNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public CourseStatus Status { get; set; }
    public DateTime? ScheduledFireAt { get; set; }
    public DateTime? ActualFiredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public TimeSpan? TargetDelay { get; set; }
    public TimeSpan? ActualDelay { get; set; }
    public bool IsOnTrack { get; set; }
    public TimeSpan? Variance => ActualDelay.HasValue && TargetDelay.HasValue
        ? ActualDelay.Value - TargetDelay.Value
        : null;
}

/// <summary>
/// Calculated course schedule.
/// </summary>
public class CourseSchedule
{
    public int KdsOrderId { get; set; }
    public List<CourseScheduleEntry> Entries { get; set; } = new();
    public DateTime EstimatedOrderCompletion { get; set; }
}

/// <summary>
/// Schedule entry for a single course.
/// </summary>
public class CourseScheduleEntry
{
    public int CourseNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime ScheduledFireAt { get; set; }
    public int DelayMinutesFromPrevious { get; set; }
    public int ItemCount { get; set; }
    public int EstimatedPrepTimeMinutes { get; set; }
}

#endregion

#region Course Status Summary DTOs

/// <summary>
/// Summary of course statuses for an order.
/// </summary>
public class CourseStatusSummary
{
    public int KdsOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public int TotalCourses { get; set; }
    public int PendingCourses { get; set; }
    public int FiredCourses { get; set; }
    public int ReadyCourses { get; set; }
    public int ServedCourses { get; set; }
    public int HeldCourses { get; set; }
    public int? CurrentCourseNumber { get; set; }
    public CourseStatus? CurrentCourseStatus { get; set; }
    public DateTime? NextCourseFireAt { get; set; }
    public TimeSpan? TimeToNextCourse => NextCourseFireAt.HasValue && NextCourseFireAt > DateTime.UtcNow
        ? NextCourseFireAt.Value - DateTime.UtcNow
        : null;
    public List<CourseStatusEntry> Courses { get; set; } = new();
}

/// <summary>
/// Status entry for a single course in the summary.
/// </summary>
public class CourseStatusEntry
{
    public int CourseNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public CourseStatus Status { get; set; }
    public bool IsOnHold { get; set; }
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
    public int ProgressPercentage => TotalItems > 0 ? (CompletedItems * 100) / TotalItems : 0;
    public DateTime? ScheduledFireAt { get; set; }
    public DateTime? FiredAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? ServedAt { get; set; }
}

#endregion

#region Reorder DTOs

/// <summary>
/// Request to reorder courses.
/// </summary>
public class ReorderCoursesRequest
{
    public List<int> CourseIds { get; set; } = new();
}

/// <summary>
/// Request to set course delay.
/// </summary>
public class SetCourseDelayRequest
{
    public int DelayMinutes { get; set; }
}

#endregion

#region Course Firing Action DTOs

/// <summary>
/// Request to fire a course.
/// </summary>
public class FireCourseRequest
{
    public int CourseNumber { get; set; }
    public bool OverridePendingHolds { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to hold a course.
/// </summary>
public class HoldCourseRequest
{
    public int CourseNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to release a course hold.
/// </summary>
public class ReleaseCourseHoldRequest
{
    public int CourseNumber { get; set; }
    public bool FireImmediately { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to rush an order (fire all courses).
/// </summary>
public class RushOrderRequest
{
    public string? Reason { get; set; }
}

#endregion
