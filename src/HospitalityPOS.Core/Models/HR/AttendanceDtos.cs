// src/HospitalityPOS.Core/Models/HR/AttendanceDtos.cs
// DTOs for Time and Attendance Management
// Story 45-1: Time and Attendance

namespace HospitalityPOS.Core.Models.HR;

/// <summary>
/// Status of an attendance record.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Employee was present on time.</summary>
    Present,

    /// <summary>Employee arrived late.</summary>
    Late,

    /// <summary>Employee was absent.</summary>
    Absent,

    /// <summary>Employee worked partial day.</summary>
    HalfDay,

    /// <summary>Employee on approved leave.</summary>
    OnLeave,

    /// <summary>Record is incomplete (e.g., no clock out).</summary>
    Incomplete
}

/// <summary>
/// Current clock status of an employee.
/// </summary>
public enum ClockStatus
{
    /// <summary>Employee is not clocked in.</summary>
    NotClockedIn,

    /// <summary>Employee is clocked in and working.</summary>
    ClockedIn,

    /// <summary>Employee is on break.</summary>
    OnBreak,

    /// <summary>Employee has clocked out for the day.</summary>
    ClockedOut
}

/// <summary>
/// Type of attendance event.
/// </summary>
public enum AttendanceEventType
{
    /// <summary>Clock in event.</summary>
    ClockIn,

    /// <summary>Clock out event.</summary>
    ClockOut,

    /// <summary>Break start event.</summary>
    BreakStart,

    /// <summary>Break end event.</summary>
    BreakEnd
}

/// <summary>
/// Settings for attendance tracking.
/// </summary>
public class AttendanceSettings
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Grace period in minutes before marking late.</summary>
    public int GracePeriodMinutes { get; set; } = 15;

    /// <summary>Hours threshold before overtime kicks in.</summary>
    public decimal OvertimeThresholdHours { get; set; } = 8;

    /// <summary>Duration of paid break in minutes.</summary>
    public int PaidBreakMinutes { get; set; } = 0;

    /// <summary>Duration of unpaid break in minutes.</summary>
    public int UnpaidBreakMinutes { get; set; } = 60;

    /// <summary>Whether PIN is required for clock in.</summary>
    public bool RequirePinForClockIn { get; set; } = true;

    /// <summary>Whether remote clock in is allowed.</summary>
    public bool AllowRemoteClockIn { get; set; } = false;

    /// <summary>Standard work day start time.</summary>
    public TimeOnly StandardStartTime { get; set; } = new TimeOnly(9, 0);

    /// <summary>Standard work day end time.</summary>
    public TimeOnly StandardEndTime { get; set; } = new TimeOnly(17, 0);

    /// <summary>Whether to auto clock out at end of day.</summary>
    public bool AutoClockOutEnabled { get; set; } = false;

    /// <summary>Time to auto clock out if enabled.</summary>
    public TimeOnly? AutoClockOutTime { get; set; } = new TimeOnly(23, 59);

    /// <summary>Weekly overtime threshold in hours.</summary>
    public decimal WeeklyOvertimeThresholdHours { get; set; } = 40;

    /// <summary>Overtime multiplier rate (e.g., 1.5 for time and a half).</summary>
    public decimal OvertimeMultiplier { get; set; } = 1.5m;
}

/// <summary>
/// An attendance record for a single day.
/// </summary>
public class AttendanceRecord
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name (for display).</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Date of attendance.</summary>
    public DateOnly AttendanceDate { get; set; }

    /// <summary>Clock in time.</summary>
    public DateTime? ClockInTime { get; set; }

    /// <summary>Clock out time.</summary>
    public DateTime? ClockOutTime { get; set; }

    /// <summary>Break start time.</summary>
    public DateTime? BreakStartTime { get; set; }

    /// <summary>Break end time.</summary>
    public DateTime? BreakEndTime { get; set; }

    /// <summary>Total worked minutes (calculated).</summary>
    public int? TotalWorkedMinutes { get; set; }

    /// <summary>Total break minutes.</summary>
    public int? TotalBreakMinutes { get; set; }

    /// <summary>Attendance status.</summary>
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    /// <summary>Notes about the attendance.</summary>
    public string? Notes { get; set; }

    /// <summary>Created timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last updated timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Scheduled start time (if available).</summary>
    public TimeOnly? ScheduledStartTime { get; set; }

    /// <summary>Scheduled end time (if available).</summary>
    public TimeOnly? ScheduledEndTime { get; set; }

    /// <summary>Whether this record has been edited by a manager.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Current clock status (derived).</summary>
    public ClockStatus CurrentStatus
    {
        get
        {
            if (ClockOutTime.HasValue) return ClockStatus.ClockedOut;
            if (BreakStartTime.HasValue && !BreakEndTime.HasValue) return ClockStatus.OnBreak;
            if (ClockInTime.HasValue) return ClockStatus.ClockedIn;
            return ClockStatus.NotClockedIn;
        }
    }

    /// <summary>Formatted clock in time.</summary>
    public string ClockInDisplay => ClockInTime?.ToString("h:mm tt") ?? "--:--";

    /// <summary>Formatted clock out time.</summary>
    public string ClockOutDisplay => ClockOutTime?.ToString("h:mm tt") ?? "--:--";

    /// <summary>Worked hours display.</summary>
    public string WorkedHoursDisplay
    {
        get
        {
            if (!TotalWorkedMinutes.HasValue) return "--:--";
            var hours = TotalWorkedMinutes.Value / 60;
            var minutes = TotalWorkedMinutes.Value % 60;
            return $"{hours}h {minutes}m";
        }
    }

    /// <summary>Calculates minutes late based on scheduled start time.</summary>
    public int? MinutesLate
    {
        get
        {
            if (!ClockInTime.HasValue || !ScheduledStartTime.HasValue) return null;
            var clockInTimeOnly = TimeOnly.FromDateTime(ClockInTime.Value);
            if (clockInTimeOnly <= ScheduledStartTime.Value) return 0;
            return (int)(clockInTimeOnly.ToTimeSpan() - ScheduledStartTime.Value.ToTimeSpan()).TotalMinutes;
        }
    }
}

/// <summary>
/// Audit record for attendance edits.
/// </summary>
public class AttendanceEdit
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Attendance record ID that was edited.</summary>
    public int AttendanceRecordId { get; set; }

    /// <summary>User who made the edit.</summary>
    public int EditedByUserId { get; set; }

    /// <summary>User name who made the edit.</summary>
    public string EditedByUserName { get; set; } = string.Empty;

    /// <summary>Field that was edited.</summary>
    public string FieldEdited { get; set; } = string.Empty;

    /// <summary>Old value.</summary>
    public string? OldValue { get; set; }

    /// <summary>New value.</summary>
    public string NewValue { get; set; } = string.Empty;

    /// <summary>Reason for the edit.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Timestamp of the edit.</summary>
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of attendance hours for reporting.
/// </summary>
public class AttendanceSummary
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Period start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Period end date.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Total hours worked.</summary>
    public decimal TotalHours { get; set; }

    /// <summary>Regular hours (non-overtime).</summary>
    public decimal RegularHours { get; set; }

    /// <summary>Overtime hours.</summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>Break hours.</summary>
    public decimal BreakHours { get; set; }

    /// <summary>Days present.</summary>
    public int DaysPresent { get; set; }

    /// <summary>Days late.</summary>
    public int DaysLate { get; set; }

    /// <summary>Days absent.</summary>
    public int DaysAbsent { get; set; }

    /// <summary>Total late minutes.</summary>
    public int TotalLateMinutes { get; set; }

    /// <summary>Average hours per day worked.</summary>
    public decimal AverageHoursPerDay => DaysPresent > 0 ? TotalHours / DaysPresent : 0;

    /// <summary>Status summary (e.g., "Good standing").</summary>
    public string Status => DaysLate == 0 && DaysAbsent == 0 ? "Good Standing" :
                            DaysAbsent > 2 ? "Needs Attention" : "Minor Issues";
}

/// <summary>
/// Daily attendance entry for a report.
/// </summary>
public class DailyAttendanceEntry
{
    /// <summary>Date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Day of week.</summary>
    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    /// <summary>Day name.</summary>
    public string DayName => DayOfWeek.ToString();

    /// <summary>Clock in time.</summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>Clock out time.</summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>Break duration in minutes.</summary>
    public int BreakMinutes { get; set; }

    /// <summary>Hours worked.</summary>
    public decimal HoursWorked { get; set; }

    /// <summary>Regular hours.</summary>
    public decimal RegularHours { get; set; }

    /// <summary>Overtime hours.</summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>Status.</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>Notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Formatted display.</summary>
    public string ClockInDisplay => ClockIn?.ToString("h:mm tt") ?? "--:--";
    public string ClockOutDisplay => ClockOut?.ToString("h:mm tt") ?? "--:--";
    public string HoursDisplay => $"{HoursWorked:N1}";
}

/// <summary>
/// Request to clock in an employee.
/// </summary>
public class ClockInRequest
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>PIN for authentication.</summary>
    public string Pin { get; set; } = string.Empty;

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Clock in time (defaults to now).</summary>
    public DateTime? ClockInTime { get; set; }
}

/// <summary>
/// Request to clock out an employee.
/// </summary>
public class ClockOutRequest
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>PIN for authentication.</summary>
    public string Pin { get; set; } = string.Empty;

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Clock out time (defaults to now).</summary>
    public DateTime? ClockOutTime { get; set; }
}

/// <summary>
/// Result of a clock in/out operation.
/// </summary>
public class ClockResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Message for display.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The attendance record.</summary>
    public AttendanceRecord? Record { get; set; }

    /// <summary>Formatted time string.</summary>
    public string TimeDisplay { get; set; } = string.Empty;

    /// <summary>Total hours worked today.</summary>
    public string? HoursWorkedToday { get; set; }

    /// <summary>Error code if failed.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Creates a success result.</summary>
    public static ClockResult Succeeded(AttendanceRecord record, string message, string timeDisplay)
    {
        return new ClockResult
        {
            Success = true,
            Record = record,
            Message = message,
            TimeDisplay = timeDisplay
        };
    }

    /// <summary>Creates a failure result.</summary>
    public static ClockResult Failed(string message, string? errorCode = null)
    {
        return new ClockResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Request to edit an attendance record.
/// </summary>
public class AttendanceEditRequest
{
    /// <summary>Attendance record ID to edit.</summary>
    public int AttendanceRecordId { get; set; }

    /// <summary>Manager's user ID.</summary>
    public int ManagerUserId { get; set; }

    /// <summary>Manager's PIN for authentication.</summary>
    public string ManagerPin { get; set; } = string.Empty;

    /// <summary>New clock in time.</summary>
    public DateTime? NewClockInTime { get; set; }

    /// <summary>New clock out time.</summary>
    public DateTime? NewClockOutTime { get; set; }

    /// <summary>New break start time.</summary>
    public DateTime? NewBreakStartTime { get; set; }

    /// <summary>New break end time.</summary>
    public DateTime? NewBreakEndTime { get; set; }

    /// <summary>Reason for the edit.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to add a missed punch.
/// </summary>
public class MissedPunchRequest
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Date of the missed punch.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Type of punch to add.</summary>
    public AttendanceEventType PunchType { get; set; }

    /// <summary>Time of the punch.</summary>
    public DateTime PunchTime { get; set; }

    /// <summary>Manager's user ID.</summary>
    public int ManagerUserId { get; set; }

    /// <summary>Manager's PIN.</summary>
    public string ManagerPin { get; set; } = string.Empty;

    /// <summary>Reason for adding.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Employee currently on shift.
/// </summary>
public class EmployeeOnShift
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Clock in time.</summary>
    public DateTime ClockInTime { get; set; }

    /// <summary>Current status.</summary>
    public ClockStatus Status { get; set; }

    /// <summary>Whether late.</summary>
    public bool IsLate { get; set; }

    /// <summary>Minutes late (if late).</summary>
    public int? MinutesLate { get; set; }

    /// <summary>Hours worked so far.</summary>
    public decimal HoursWorkedSoFar { get; set; }

    /// <summary>Department (if applicable).</summary>
    public string? Department { get; set; }

    /// <summary>Role name.</summary>
    public string? RoleName { get; set; }

    /// <summary>Status display.</summary>
    public string StatusDisplay => Status switch
    {
        ClockStatus.ClockedIn => IsLate ? $"In: {ClockInTime:h:mm tt} (Late)" : $"In: {ClockInTime:h:mm tt}",
        ClockStatus.OnBreak => "On Break",
        _ => Status.ToString()
    };
}

/// <summary>
/// Today's attendance summary for dashboard.
/// </summary>
public class TodayAttendanceSummary
{
    /// <summary>Date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Employees currently clocked in.</summary>
    public List<EmployeeOnShift> EmployeesOnShift { get; set; } = new();

    /// <summary>Count of clocked in employees.</summary>
    public int ClockedInCount => EmployeesOnShift.Count(e => e.Status == ClockStatus.ClockedIn);

    /// <summary>Count of employees on break.</summary>
    public int OnBreakCount => EmployeesOnShift.Count(e => e.Status == ClockStatus.OnBreak);

    /// <summary>Count of late arrivals.</summary>
    public int LateArrivalsCount => EmployeesOnShift.Count(e => e.IsLate);

    /// <summary>Total employees expected today.</summary>
    public int ExpectedEmployees { get; set; }

    /// <summary>Employees not yet arrived.</summary>
    public int NotYetArrivedCount => ExpectedEmployees - EmployeesOnShift.Count;

    /// <summary>Summary text.</summary>
    public string SummaryText => $"{ClockedInCount} on shift, {OnBreakCount} on break, {LateArrivalsCount} late";
}

/// <summary>
/// Attendance report for date range.
/// </summary>
public class AttendanceReport
{
    /// <summary>Report start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Report end date.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Generated timestamp.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>Employee summaries.</summary>
    public List<AttendanceSummary> EmployeeSummaries { get; set; } = new();

    /// <summary>Total hours across all employees.</summary>
    public decimal TotalHours => EmployeeSummaries.Sum(e => e.TotalHours);

    /// <summary>Total overtime hours.</summary>
    public decimal TotalOvertimeHours => EmployeeSummaries.Sum(e => e.OvertimeHours);

    /// <summary>Total late instances.</summary>
    public int TotalLateInstances => EmployeeSummaries.Sum(e => e.DaysLate);

    /// <summary>Total absences.</summary>
    public int TotalAbsences => EmployeeSummaries.Sum(e => e.DaysAbsent);

    /// <summary>Average hours per employee.</summary>
    public decimal AverageHoursPerEmployee => EmployeeSummaries.Count > 0
        ? TotalHours / EmployeeSummaries.Count : 0;
}

/// <summary>
/// Detailed attendance report for a single employee.
/// </summary>
public class EmployeeAttendanceReport
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Report start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Report end date.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Daily entries.</summary>
    public List<DailyAttendanceEntry> DailyEntries { get; set; } = new();

    /// <summary>Summary.</summary>
    public AttendanceSummary Summary { get; set; } = new();

    /// <summary>Edit history.</summary>
    public List<AttendanceEdit> EditHistory { get; set; } = new();
}

/// <summary>
/// Export format for payroll integration.
/// </summary>
public class PayrollExportData
{
    /// <summary>Export period start.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Export period end.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Generated timestamp.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>Employee records.</summary>
    public List<EmployeePayrollEntry> Employees { get; set; } = new();
}

/// <summary>
/// Payroll entry for a single employee.
/// </summary>
public class EmployeePayrollEntry
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Employee code/number.</summary>
    public string? EmployeeCode { get; set; }

    /// <summary>Regular hours.</summary>
    public decimal RegularHours { get; set; }

    /// <summary>Overtime hours.</summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>Total hours.</summary>
    public decimal TotalHours => RegularHours + OvertimeHours;

    /// <summary>Days worked.</summary>
    public int DaysWorked { get; set; }

    /// <summary>Days absent.</summary>
    public int DaysAbsent { get; set; }

    /// <summary>Days late.</summary>
    public int DaysLate { get; set; }

    /// <summary>Total late deduction minutes.</summary>
    public int LateDeductionMinutes { get; set; }

    /// <summary>Notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Event args for attendance events.
/// </summary>
public class AttendanceEventArgs : EventArgs
{
    /// <summary>Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Event type.</summary>
    public AttendanceEventType EventType { get; set; }

    /// <summary>Event timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Attendance record.</summary>
    public AttendanceRecord? Record { get; set; }
}
