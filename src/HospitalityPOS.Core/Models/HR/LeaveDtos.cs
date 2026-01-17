// src/HospitalityPOS.Core/Models/HR/LeaveDtos.cs
// DTOs for employee leave management
// Story 45-4: Leave Management

namespace HospitalityPOS.Core.Models.HR;

#region Enums

/// <summary>
/// Status of a leave request.
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>Request pending review.</summary>
    Pending,
    /// <summary>Request approved.</summary>
    Approved,
    /// <summary>Request rejected.</summary>
    Rejected,
    /// <summary>Request cancelled by employee.</summary>
    Cancelled
}

/// <summary>
/// Reason for leave accrual/adjustment.
/// </summary>
public enum LeaveAdjustmentType
{
    /// <summary>Annual allocation.</summary>
    Allocation,
    /// <summary>Leave taken.</summary>
    Usage,
    /// <summary>Carried over from previous year.</summary>
    CarryOver,
    /// <summary>Manual adjustment by HR.</summary>
    Adjustment,
    /// <summary>Forfeited (expired carry-over).</summary>
    Forfeited
}

#endregion

#region Leave Types

/// <summary>
/// Leave type configuration.
/// </summary>
public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowCarryOver { get; set; }
    public int MaxCarryOverDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public int? MinimumNoticeDays { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Accrual settings
    public bool AccruesMonthly { get; set; }
    public decimal? MonthlyAccrualRate { get; set; }

    // Eligibility
    public int? MinServiceMonthsRequired { get; set; }
}

/// <summary>
/// Request to create or update a leave type.
/// </summary>
public class LeaveTypeRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowCarryOver { get; set; }
    public int MaxCarryOverDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public int? MinimumNoticeDays { get; set; }
    public int? MaxConsecutiveDays { get; set; }
}

#endregion

#region Leave Allocations

/// <summary>
/// Employee's leave allocation for a year.
/// </summary>
public class LeaveAllocation
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal AllocatedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal PendingDays { get; set; } // Days in pending requests
    public DateTime? LastUpdated { get; set; }

    public decimal TotalAvailable => AllocatedDays + CarriedOverDays;
    public decimal RemainingDays => TotalAvailable - UsedDays;
    public decimal AvailableForRequest => RemainingDays - PendingDays;
    public decimal UtilizationPercent => TotalAvailable > 0 ? (UsedDays / TotalAvailable) * 100 : 0;
}

/// <summary>
/// Summary of all leave balances for an employee.
/// </summary>
public class EmployeeLeaveBalance
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int Year { get; set; }
    public List<LeaveAllocation> Allocations { get; set; } = new();
    public decimal TotalAllocated => Allocations.Sum(a => a.TotalAvailable);
    public decimal TotalUsed => Allocations.Sum(a => a.UsedDays);
    public decimal TotalRemaining => Allocations.Sum(a => a.RemainingDays);
}

/// <summary>
/// Leave balance adjustment record.
/// </summary>
public class LeaveBalanceAdjustment
{
    public int Id { get; set; }
    public int AllocationId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveAdjustmentType AdjustmentType { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public int AdjustedByUserId { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Leave Requests

/// <summary>
/// Employee leave request.
/// </summary>
public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Documentation
    public bool DocumentationRequired { get; set; }
    public bool DocumentationProvided { get; set; }
    public string? DocumentationNotes { get; set; }

    // Half days
    public bool IsHalfDayStart { get; set; }
    public bool IsHalfDayEnd { get; set; }
}

/// <summary>
/// Request to submit a leave request.
/// </summary>
public class LeaveRequestSubmission
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
    public bool IsHalfDayStart { get; set; }
    public bool IsHalfDayEnd { get; set; }
}

/// <summary>
/// Request to approve or reject a leave request.
/// </summary>
public class LeaveApprovalRequest
{
    public int RequestId { get; set; }
    public int ReviewerUserId { get; set; }
    public bool Approve { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of a leave operation.
/// </summary>
public class LeaveResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LeaveRequest? Request { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static LeaveResult Succeeded(LeaveRequest request, string message = "Request submitted")
        => new() { Success = true, Message = message, Request = request };

    public static LeaveResult Failed(string message)
        => new() { Success = false, Message = message };
}

#endregion

#region Calendar

/// <summary>
/// Leave calendar entry.
/// </summary>
public class LeaveCalendarEntry
{
    public int RequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Days { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public string? Color { get; set; } // For UI display
}

/// <summary>
/// Leave calendar view.
/// </summary>
public class LeaveCalendarView
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<LeaveCalendarEntry> Entries { get; set; } = new();
    public List<DayCoverage> DailyCoverage { get; set; } = new();
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Coverage information for a day.
/// </summary>
public class DayCoverage
{
    public DateOnly Date { get; set; }
    public int TotalEmployees { get; set; }
    public int EmployeesOnLeave { get; set; }
    public int EmployeesAvailable => TotalEmployees - EmployeesOnLeave;
    public List<string> EmployeesOnLeaveNames { get; set; } = new();
    public bool HasCoverageIssue { get; set; }
}

#endregion

#region Reports

/// <summary>
/// Leave balance report.
/// </summary>
public class LeaveBalanceReport
{
    public int Year { get; set; }
    public DateOnly GeneratedDate { get; set; }
    public List<EmployeeLeaveBalance> Balances { get; set; } = new();
    public List<LeaveTypeSummary> ByLeaveType { get; set; } = new();
    public int TotalEmployees { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal OverallUtilization => TotalAllocated > 0 ? (TotalUsed / TotalAllocated) * 100 : 0;
}

/// <summary>
/// Summary by leave type.
/// </summary>
public class LeaveTypeSummary
{
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal TotalAllocated { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRemaining { get; set; }
    public int EmployeesWithBalance { get; set; }
    public decimal UtilizationPercent => TotalAllocated > 0 ? (TotalUsed / TotalAllocated) * 100 : 0;
}

/// <summary>
/// Employee leave history.
/// </summary>
public class EmployeeLeaveHistory
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<LeaveRequest> Requests { get; set; } = new();
    public List<LeaveBalanceAdjustment> Adjustments { get; set; } = new();
    public Dictionary<string, decimal> DaysByType { get; set; } = new();
    public decimal TotalDaysTaken { get; set; }
}

/// <summary>
/// Leave utilization report.
/// </summary>
public class LeaveUtilizationReport
{
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<MonthlyUtilization> MonthlyBreakdown { get; set; } = new();
    public List<DepartmentUtilization> ByDepartment { get; set; } = new();
    public int TotalRequestsApproved { get; set; }
    public int TotalRequestsRejected { get; set; }
    public int TotalRequestsPending { get; set; }
    public decimal AverageApprovalDays { get; set; }
}

/// <summary>
/// Monthly leave utilization.
/// </summary>
public class MonthlyUtilization
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalDaysTaken { get; set; }
    public int RequestCount { get; set; }
    public int EmployeesOnLeave { get; set; }
}

/// <summary>
/// Department leave utilization.
/// </summary>
public class DepartmentUtilization
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal UtilizationPercent => TotalAllocated > 0 ? (TotalUsed / TotalAllocated) * 100 : 0;
}

#endregion

#region Settings

/// <summary>
/// Leave management settings.
/// </summary>
public class LeaveSettings
{
    /// <summary>Year start month for leave calculations.</summary>
    public int LeaveYearStartMonth { get; set; } = 1; // January

    /// <summary>Allow requests for past dates.</summary>
    public bool AllowBackdatedRequests { get; set; } = false;

    /// <summary>Maximum days in past for backdated requests.</summary>
    public int MaxBackdateDays { get; set; } = 7;

    /// <summary>Require manager approval.</summary>
    public bool RequireManagerApproval { get; set; } = true;

    /// <summary>Auto-approve if manager doesn't respond within days.</summary>
    public int? AutoApproveAfterDays { get; set; }

    /// <summary>Send reminder to manager after days.</summary>
    public int ReminderAfterDays { get; set; } = 2;

    /// <summary>Calculate leave excluding weekends.</summary>
    public bool ExcludeWeekends { get; set; } = true;

    /// <summary>Calculate leave excluding public holidays.</summary>
    public bool ExcludePublicHolidays { get; set; } = true;

    /// <summary>Minimum days in advance for leave request.</summary>
    public int DefaultMinimumNoticeDays { get; set; } = 0;

    /// <summary>Allow half-day leave.</summary>
    public bool AllowHalfDayLeave { get; set; } = true;

    /// <summary>Carry over deadline (months after year start).</summary>
    public int CarryOverDeadlineMonths { get; set; } = 3;
}

#endregion

#region Events

/// <summary>
/// Event args for leave events.
/// </summary>
public class LeaveEventArgs : EventArgs
{
    public LeaveRequest Request { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public LeaveEventArgs(LeaveRequest request, string eventType)
    {
        Request = request;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
