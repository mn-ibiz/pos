// src/HospitalityPOS.Core/Models/HR/SchedulingDtos.cs
// DTOs for shift scheduling and workforce management
// Story 45-2: Shift Scheduling

namespace HospitalityPOS.Core.Models.HR;

#region Enums

/// <summary>
/// Status of a scheduled shift.
/// </summary>
public enum ShiftStatus
{
    /// <summary>Shift is scheduled but not yet started.</summary>
    Scheduled,
    /// <summary>Employee is currently working this shift.</summary>
    InProgress,
    /// <summary>Shift was completed normally.</summary>
    Completed,
    /// <summary>Employee did not show up for shift.</summary>
    NoShow,
    /// <summary>Shift was swapped with another employee.</summary>
    Swapped,
    /// <summary>Shift was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Status of a shift swap request.
/// </summary>
public enum SwapRequestStatus
{
    /// <summary>Request pending employee acceptance.</summary>
    Pending,
    /// <summary>Target employee accepted the swap.</summary>
    Accepted,
    /// <summary>Manager approved the swap.</summary>
    Approved,
    /// <summary>Request was rejected.</summary>
    Rejected,
    /// <summary>Request was cancelled by requester.</summary>
    Cancelled,
    /// <summary>Request expired without response.</summary>
    Expired
}

/// <summary>
/// Type of schedule conflict.
/// </summary>
public enum ConflictType
{
    /// <summary>Employee is scheduled twice at the same time.</summary>
    DoubleBooked,
    /// <summary>Employee would exceed maximum hours per week.</summary>
    MaxHoursExceeded,
    /// <summary>Insufficient rest time between shifts.</summary>
    InsufficientRest,
    /// <summary>Employee is on leave during scheduled shift.</summary>
    OnLeave,
    /// <summary>Employee lacks required certification/skill.</summary>
    MissingQualification,
    /// <summary>Position already fully staffed.</summary>
    OverStaffed
}

/// <summary>
/// Day of week for recurring patterns.
/// </summary>
[Flags]
public enum DaysOfWeek
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    AllDays = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

#endregion

#region Settings

/// <summary>
/// Scheduling configuration settings.
/// </summary>
public class SchedulingSettings
{
    /// <summary>Maximum hours per week per employee.</summary>
    public decimal MaxHoursPerWeek { get; set; } = 48m;

    /// <summary>Minimum rest hours between shifts.</summary>
    public decimal MinRestHours { get; set; } = 8m;

    /// <summary>Maximum consecutive work days.</summary>
    public int MaxConsecutiveDays { get; set; } = 6;

    /// <summary>Allow overtime scheduling.</summary>
    public bool AllowOvertimeScheduling { get; set; } = true;

    /// <summary>Require manager approval for shift swaps.</summary>
    public bool RequireSwapApproval { get; set; } = true;

    /// <summary>Days in advance employees can view schedule.</summary>
    public int ScheduleVisibilityDays { get; set; } = 14;

    /// <summary>Days in advance to post schedule.</summary>
    public int SchedulePostingDays { get; set; } = 7;

    /// <summary>Allow employees to request shift swaps.</summary>
    public bool AllowShiftSwaps { get; set; } = true;

    /// <summary>Notify employees of schedule changes.</summary>
    public bool NotifyOnScheduleChange { get; set; } = true;

    /// <summary>Default shift duration in hours.</summary>
    public decimal DefaultShiftDuration { get; set; } = 8m;

    /// <summary>Grace period minutes for late arrival (before flagging).</summary>
    public int LateArrivalGraceMinutes { get; set; } = 15;
}

#endregion

#region Shift Models

/// <summary>
/// Represents a single work shift.
/// </summary>
public class Shift
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Notes { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;
    public int? RecurringPatternId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Gets the shift duration in hours.</summary>
    public decimal DurationHours => CalculateDuration();

    /// <summary>Gets the shift start as DateTime.</summary>
    public DateTime StartDateTime => ShiftDate.ToDateTime(StartTime);

    /// <summary>Gets the shift end as DateTime.</summary>
    public DateTime EndDateTime => EndTime < StartTime
        ? ShiftDate.AddDays(1).ToDateTime(EndTime) // Overnight shift
        : ShiftDate.ToDateTime(EndTime);

    private decimal CalculateDuration()
    {
        var duration = EndDateTime - StartDateTime;
        return (decimal)duration.TotalHours;
    }
}

/// <summary>
/// Request to create or update a shift.
/// </summary>
public class ShiftRequest
{
    public int? Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? PositionId { get; set; }
    public int? DepartmentId { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public DaysOfWeek RecurringDays { get; set; }
    public DateOnly? RecurringEndDate { get; set; }
}

/// <summary>
/// Result of a shift operation.
/// </summary>
public class ShiftResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Shift? Shift { get; set; }
    public List<ShiftConflict> Conflicts { get; set; } = new();
    public List<Shift>? CreatedShifts { get; set; }

    public static ShiftResult Succeeded(Shift shift, string message = "Shift created successfully")
        => new() { Success = true, Message = message, Shift = shift };

    public static ShiftResult SucceededMultiple(List<Shift> shifts, string message)
        => new() { Success = true, Message = message, CreatedShifts = shifts };

    public static ShiftResult Failed(string message, List<ShiftConflict>? conflicts = null)
        => new() { Success = false, Message = message, Conflicts = conflicts ?? new() };
}

/// <summary>
/// Represents a schedule conflict.
/// </summary>
public class ShiftConflict
{
    public ConflictType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ConflictingShiftId { get; set; }
    public bool IsWarning { get; set; } // Warning vs blocking error

    public static ShiftConflict DoubleBooked(int existingShiftId, string message)
        => new() { Type = ConflictType.DoubleBooked, Message = message, ConflictingShiftId = existingShiftId };

    public static ShiftConflict MaxHours(string message)
        => new() { Type = ConflictType.MaxHoursExceeded, Message = message, IsWarning = true };

    public static ShiftConflict InsufficientRest(string message)
        => new() { Type = ConflictType.InsufficientRest, Message = message, IsWarning = true };
}

#endregion

#region Recurring Patterns

/// <summary>
/// Defines a recurring shift pattern.
/// </summary>
public class RecurringShiftPattern
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DaysOfWeek DaysOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets list of day names for this pattern.</summary>
    public List<string> GetDayNames()
    {
        var days = new List<string>();
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Sunday)) days.Add("Sun");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Monday)) days.Add("Mon");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Tuesday)) days.Add("Tue");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Wednesday)) days.Add("Wed");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Thursday)) days.Add("Thu");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Friday)) days.Add("Fri");
        if (DaysOfWeek.HasFlag(HR.DaysOfWeek.Saturday)) days.Add("Sat");
        return days;
    }
}

#endregion

#region Shift Swap

/// <summary>
/// Request to swap shifts between employees.
/// </summary>
public class ShiftSwapRequest
{
    public int Id { get; set; }
    public int RequestingEmployeeId { get; set; }
    public string RequestingEmployeeName { get; set; } = string.Empty;
    public int OriginalShiftId { get; set; }
    public Shift? OriginalShift { get; set; }
    public int TargetEmployeeId { get; set; }
    public string TargetEmployeeName { get; set; } = string.Empty;
    public int? TargetShiftId { get; set; }
    public Shift? TargetShift { get; set; }
    public SwapRequestStatus Status { get; set; } = SwapRequestStatus.Pending;
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Request to initiate a shift swap.
/// </summary>
public class SwapInitiateRequest
{
    public int RequestingEmployeeId { get; set; }
    public int OriginalShiftId { get; set; }
    public int TargetEmployeeId { get; set; }
    public int? TargetShiftId { get; set; } // Optional if just giving away shift
    public string? Reason { get; set; }
}

/// <summary>
/// Response to a swap request.
/// </summary>
public class SwapResponseRequest
{
    public int SwapRequestId { get; set; }
    public bool Accept { get; set; }
    public string? ResponseReason { get; set; }
}

/// <summary>
/// Manager approval for a swap.
/// </summary>
public class SwapApprovalRequest
{
    public int SwapRequestId { get; set; }
    public int ManagerUserId { get; set; }
    public bool Approve { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Result of a swap operation.
/// </summary>
public class SwapResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ShiftSwapRequest? SwapRequest { get; set; }
    public List<ShiftConflict> Conflicts { get; set; } = new();

    public static SwapResult Succeeded(ShiftSwapRequest request, string message)
        => new() { Success = true, Message = message, SwapRequest = request };

    public static SwapResult Failed(string message, List<ShiftConflict>? conflicts = null)
        => new() { Success = false, Message = message, Conflicts = conflicts ?? new() };
}

#endregion

#region Coverage

/// <summary>
/// Defines required staffing coverage.
/// </summary>
public class CoverageRequirement
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MinimumStaff { get; set; }
    public int? OptimalStaff { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
}

/// <summary>
/// Coverage analysis for a time period.
/// </summary>
public class CoverageAnalysis
{
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int RequiredStaff { get; set; }
    public int ScheduledStaff { get; set; }
    public int? OptimalStaff { get; set; }
    public List<string> ScheduledEmployees { get; set; } = new();
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public bool IsUnderstaffed => ScheduledStaff < RequiredStaff;
    public bool IsOverstaffed => OptimalStaff.HasValue && ScheduledStaff > OptimalStaff.Value;
    public int Variance => ScheduledStaff - RequiredStaff;
    public string CoverageStatus => IsUnderstaffed ? "Understaffed" :
                                    IsOverstaffed ? "Overstaffed" : "Adequate";
}

/// <summary>
/// Daily coverage summary.
/// </summary>
public class DailyCoverageSummary
{
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int TotalScheduledHours { get; set; }
    public int TotalRequiredHours { get; set; }
    public int UnderstaffedPeriods { get; set; }
    public int OverstaffedPeriods { get; set; }
    public int TotalEmployeesScheduled { get; set; }
    public List<CoverageAnalysis> Periods { get; set; } = new();

    public decimal CoveragePercentage => TotalRequiredHours > 0
        ? (decimal)TotalScheduledHours / TotalRequiredHours * 100
        : 100m;
}

#endregion

#region Schedule Views

/// <summary>
/// Weekly schedule view for calendar display.
/// </summary>
public class WeeklyScheduleView
{
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public List<EmployeeWeekSchedule> EmployeeSchedules { get; set; } = new();
    public List<DailyCoverageSummary> DailyCoverage { get; set; } = new();
    public int TotalScheduledHours { get; set; }
    public int TotalEmployees { get; set; }
}

/// <summary>
/// Employee's schedule for a week.
/// </summary>
public class EmployeeWeekSchedule
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public Dictionary<DayOfWeek, List<Shift>> ShiftsByDay { get; set; } = new();
    public decimal TotalHours { get; set; }
    public bool HasConflicts { get; set; }
    public List<ShiftConflict> Conflicts { get; set; } = new();
}

/// <summary>
/// Employee's personal schedule view.
/// </summary>
public class MyScheduleView
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<Shift> UpcomingShifts { get; set; } = new();
    public List<ShiftSwapRequest> PendingSwapRequests { get; set; } = new();
    public decimal HoursThisWeek { get; set; }
    public decimal HoursNextWeek { get; set; }
    public List<CoworkerOnShift> TodaysCoworkers { get; set; } = new();
}

/// <summary>
/// Coworker information for shift.
/// </summary>
public class CoworkerOnShift
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

#endregion

#region Attendance Integration

/// <summary>
/// Scheduled vs actual hours comparison.
/// </summary>
public class ScheduleAttendanceComparison
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly? ScheduledStart { get; set; }
    public TimeOnly? ScheduledEnd { get; set; }
    public TimeOnly? ActualStart { get; set; }
    public TimeOnly? ActualEnd { get; set; }
    public decimal ScheduledHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal Variance => ActualHours - ScheduledHours;
    public bool WasLate { get; set; }
    public int LateMinutes { get; set; }
    public bool WasEarlyDeparture { get; set; }
    public bool WasNoShow { get; set; }
}

/// <summary>
/// Summary of schedule adherence.
/// </summary>
public class ScheduleAdherenceReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<ScheduleAttendanceComparison> Records { get; set; } = new();
    public int TotalScheduledShifts { get; set; }
    public int TotalCompletedShifts { get; set; }
    public int TotalLateArrivals { get; set; }
    public int TotalEarlyDepartures { get; set; }
    public int TotalNoShows { get; set; }
    public decimal TotalScheduledHours { get; set; }
    public decimal TotalActualHours { get; set; }
    public decimal OverallVariance => TotalActualHours - TotalScheduledHours;
    public decimal AdherencePercentage => TotalScheduledShifts > 0
        ? (decimal)TotalCompletedShifts / TotalScheduledShifts * 100
        : 100m;
}

#endregion

#region Events

/// <summary>
/// Event args for schedule changes.
/// </summary>
public class ScheduleEventArgs : EventArgs
{
    public Shift Shift { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }
    public int? ChangedByUserId { get; }

    public ScheduleEventArgs(Shift shift, string eventType, int? changedByUserId = null)
    {
        Shift = shift;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
        ChangedByUserId = changedByUserId;
    }
}

/// <summary>
/// Event args for swap request changes.
/// </summary>
public class SwapRequestEventArgs : EventArgs
{
    public ShiftSwapRequest SwapRequest { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public SwapRequestEventArgs(ShiftSwapRequest request, string eventType)
    {
        SwapRequest = request;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
