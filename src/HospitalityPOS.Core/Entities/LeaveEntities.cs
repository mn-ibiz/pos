// src/HospitalityPOS.Core/Entities/LeaveEntities.cs
// Leave management entities for Kenya Employment Act 2007 compliance.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Leave request status enumeration.
/// </summary>
public enum LeaveRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

/// <summary>
/// Leave adjustment type enumeration.
/// </summary>
public enum LeaveAdjustmentType
{
    Allocation = 0,      // Initial allocation
    CarryOver = 1,       // Carry-over from previous year
    Adjustment = 2,      // Manual adjustment
    Encashment = 3,      // Leave encashment (converted to cash)
    Forfeiture = 4       // Forfeited leave (expired)
}

/// <summary>
/// Represents a type of leave (Annual, Sick, Maternity, etc.)
/// Configured based on Kenya Employment Act 2007.
/// </summary>
public class LeaveType : BaseEntity
{
    /// <summary>
    /// Leave type name (e.g., "Annual Leave", "Sick Leave").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Leave type code for system reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of the leave type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default number of days allocated per year.
    /// </summary>
    public decimal DefaultDaysPerYear { get; set; }

    /// <summary>
    /// Whether this leave type is paid.
    /// </summary>
    public bool IsPaid { get; set; } = true;

    /// <summary>
    /// Whether unused leave can be carried over to next year.
    /// </summary>
    public bool AllowCarryOver { get; set; }

    /// <summary>
    /// Maximum days that can be carried over.
    /// </summary>
    public decimal MaxCarryOverDays { get; set; }

    /// <summary>
    /// Whether documentation is required (e.g., medical certificate for sick leave).
    /// </summary>
    public bool RequiresDocumentation { get; set; }

    /// <summary>
    /// Minimum notice days before leave can start.
    /// </summary>
    public int? MinimumNoticeDays { get; set; }

    /// <summary>
    /// Maximum consecutive days allowed.
    /// </summary>
    public int? MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Minimum service months required before this leave is available.
    /// </summary>
    public int? MinServiceMonthsRequired { get; set; }

    /// <summary>
    /// Whether this is a statutory leave type per Kenya Employment Act.
    /// </summary>
    public bool IsStatutory { get; set; }

    /// <summary>
    /// Whether this leave type is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display color for calendar view (hex code).
    /// </summary>
    public string? DisplayColor { get; set; }

    /// <summary>
    /// Display order in UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual ICollection<LeaveAllocation> Allocations { get; set; } = new List<LeaveAllocation>();
    public virtual ICollection<LeaveRequest> Requests { get; set; } = new List<LeaveRequest>();
}

/// <summary>
/// Tracks leave balance allocated to an employee for a specific year.
/// </summary>
public class LeaveAllocation : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Leave type ID.
    /// </summary>
    public int LeaveTypeId { get; set; }

    /// <summary>
    /// Year of allocation.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Days allocated for this year.
    /// </summary>
    public decimal AllocatedDays { get; set; }

    /// <summary>
    /// Days already used/taken.
    /// </summary>
    public decimal UsedDays { get; set; }

    /// <summary>
    /// Days carried over from previous year.
    /// </summary>
    public decimal CarriedOverDays { get; set; }

    /// <summary>
    /// Days pending approval (in submitted requests).
    /// </summary>
    public decimal PendingDays { get; set; }

    /// <summary>
    /// Total available days (allocated + carried over).
    /// </summary>
    public decimal TotalAvailable => AllocatedDays + CarriedOverDays;

    /// <summary>
    /// Remaining days (total - used - pending).
    /// </summary>
    public decimal RemainingDays => TotalAvailable - UsedDays - PendingDays;

    /// <summary>
    /// Days available for new requests.
    /// </summary>
    public decimal AvailableForRequest => RemainingDays;

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual ICollection<LeaveBalanceAdjustment> Adjustments { get; set; } = new List<LeaveBalanceAdjustment>();
}

/// <summary>
/// Employee leave request with approval workflow.
/// </summary>
public class LeaveRequest : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Leave type ID.
    /// </summary>
    public int LeaveTypeId { get; set; }

    /// <summary>
    /// Start date of leave.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of leave.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Number of days requested (calculated).
    /// </summary>
    public decimal DaysRequested { get; set; }

    /// <summary>
    /// Reason for leave request.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    /// <summary>
    /// Whether start date is half day.
    /// </summary>
    public bool IsHalfDayStart { get; set; }

    /// <summary>
    /// Whether end date is half day.
    /// </summary>
    public bool IsHalfDayEnd { get; set; }

    /// <summary>
    /// Path to supporting documentation (e.g., medical certificate).
    /// </summary>
    public string? DocumentationPath { get; set; }

    /// <summary>
    /// User ID who reviewed the request.
    /// </summary>
    public int? ReviewedByUserId { get; set; }

    /// <summary>
    /// Date/time when request was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Notes from reviewer.
    /// </summary>
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Contact number while on leave.
    /// </summary>
    public string? ContactWhileOnLeave { get; set; }

    /// <summary>
    /// Handover notes for colleagues.
    /// </summary>
    public string? HandoverNotes { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual User? ReviewedByUser { get; set; }
}

/// <summary>
/// Manual adjustments to leave balances.
/// </summary>
public class LeaveBalanceAdjustment : BaseEntity
{
    /// <summary>
    /// Leave allocation ID.
    /// </summary>
    public int LeaveAllocationId { get; set; }

    /// <summary>
    /// Type of adjustment.
    /// </summary>
    public LeaveAdjustmentType AdjustmentType { get; set; }

    /// <summary>
    /// Days adjusted (positive for addition, negative for deduction).
    /// </summary>
    public decimal Days { get; set; }

    /// <summary>
    /// Reason for adjustment.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User who made the adjustment.
    /// </summary>
    public int AdjustedByUserId { get; set; }

    // Navigation properties
    public virtual LeaveAllocation? LeaveAllocation { get; set; }
    public virtual User? AdjustedByUser { get; set; }
}

/// <summary>
/// Public holidays for Kenya.
/// </summary>
public class PublicHoliday : BaseEntity
{
    /// <summary>
    /// Holiday date.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Holiday name (e.g., "Jamhuri Day", "Madaraka Day").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an annually recurring holiday.
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Recurring month (1-12) for recurring holidays.
    /// </summary>
    public int? RecurringMonth { get; set; }

    /// <summary>
    /// Recurring day (1-31) for recurring holidays.
    /// </summary>
    public int? RecurringDay { get; set; }

    /// <summary>
    /// Whether this is a gazetted national holiday.
    /// </summary>
    public bool IsGazetted { get; set; } = true;

    /// <summary>
    /// Description or notes about the holiday.
    /// </summary>
    public string? Description { get; set; }
}
