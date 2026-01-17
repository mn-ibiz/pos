namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an employee attendance record.
/// </summary>
public class Attendance : BaseEntity
{
    public int EmployeeId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public TimeSpan? ClockIn { get; set; }
    public TimeSpan? ClockOut { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public int? ApprovedByUserId { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
}

/// <summary>
/// Attendance status enumeration.
/// </summary>
public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    OnLeave = 5,
    Holiday = 6
}
