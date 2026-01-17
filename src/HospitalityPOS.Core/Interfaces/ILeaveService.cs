// src/HospitalityPOS.Core/Interfaces/ILeaveService.cs
// Service interface for employee leave management
// Story 45-4: Leave Management

using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for employee leave management.
/// Handles leave types, requests, approvals, balances, and reporting.
/// </summary>
public interface ILeaveService
{
    #region Leave Types

    /// <summary>
    /// Creates a new leave type.
    /// </summary>
    /// <param name="request">Leave type request.</param>
    /// <returns>Created leave type.</returns>
    Task<LeaveType> CreateLeaveTypeAsync(LeaveTypeRequest request);

    /// <summary>
    /// Updates an existing leave type.
    /// </summary>
    /// <param name="request">Leave type request.</param>
    /// <returns>Updated leave type.</returns>
    Task<LeaveType> UpdateLeaveTypeAsync(LeaveTypeRequest request);

    /// <summary>
    /// Deactivates a leave type.
    /// </summary>
    /// <param name="leaveTypeId">Leave type ID.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateLeaveTypeAsync(int leaveTypeId);

    /// <summary>
    /// Gets a leave type by ID.
    /// </summary>
    /// <param name="leaveTypeId">Leave type ID.</param>
    /// <returns>Leave type or null.</returns>
    Task<LeaveType?> GetLeaveTypeAsync(int leaveTypeId);

    /// <summary>
    /// Gets all active leave types.
    /// </summary>
    /// <returns>List of leave types.</returns>
    Task<IReadOnlyList<LeaveType>> GetActiveLeaveTypesAsync();

    #endregion

    #region Leave Requests

    /// <summary>
    /// Submits a new leave request.
    /// </summary>
    /// <param name="submission">Leave request submission.</param>
    /// <returns>Result of the submission.</returns>
    Task<LeaveResult> SubmitRequestAsync(LeaveRequestSubmission submission);

    /// <summary>
    /// Approves or rejects a leave request.
    /// </summary>
    /// <param name="request">Approval request.</param>
    /// <returns>Result of the approval.</returns>
    Task<LeaveResult> ProcessApprovalAsync(LeaveApprovalRequest request);

    /// <summary>
    /// Cancels a leave request.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <param name="employeeId">Employee requesting cancellation.</param>
    /// <param name="reason">Cancellation reason.</param>
    /// <returns>Result of the cancellation.</returns>
    Task<LeaveResult> CancelRequestAsync(int requestId, int employeeId, string? reason = null);

    /// <summary>
    /// Gets a leave request by ID.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <returns>Leave request or null.</returns>
    Task<LeaveRequest?> GetRequestAsync(int requestId);

    /// <summary>
    /// Gets pending requests for a manager to review.
    /// </summary>
    /// <param name="managerId">Manager user ID.</param>
    /// <returns>List of pending requests.</returns>
    Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsAsync(int? managerId = null);

    /// <summary>
    /// Gets leave requests for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="year">Optional year filter.</param>
    /// <returns>List of requests.</returns>
    Task<IReadOnlyList<LeaveRequest>> GetEmployeeRequestsAsync(int employeeId, int? year = null);

    /// <summary>
    /// Gets approved leave requests for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <returns>List of approved requests.</returns>
    Task<IReadOnlyList<LeaveRequest>> GetApprovedRequestsAsync(
        DateOnly startDate, DateOnly endDate, int? departmentId = null);

    #endregion

    #region Leave Balances

    /// <summary>
    /// Gets leave allocations for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="year">Year.</param>
    /// <returns>List of allocations.</returns>
    Task<IReadOnlyList<LeaveAllocation>> GetEmployeeAllocationsAsync(int employeeId, int year);

    /// <summary>
    /// Gets leave balance summary for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="year">Year.</param>
    /// <returns>Leave balance summary.</returns>
    Task<EmployeeLeaveBalance> GetEmployeeBalanceAsync(int employeeId, int year);

    /// <summary>
    /// Initializes leave allocations for a new year.
    /// </summary>
    /// <param name="year">Year to initialize.</param>
    /// <param name="employeeIds">Optional specific employees.</param>
    /// <returns>Number of allocations created.</returns>
    Task<int> InitializeYearAllocationsAsync(int year, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Carries over leave balances to new year.
    /// </summary>
    /// <param name="fromYear">Source year.</param>
    /// <param name="toYear">Target year.</param>
    /// <returns>Number of carry-overs processed.</returns>
    Task<int> ProcessCarryOverAsync(int fromYear, int toYear);

    /// <summary>
    /// Makes a manual adjustment to leave balance.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="leaveTypeId">Leave type ID.</param>
    /// <param name="year">Year.</param>
    /// <param name="days">Days to adjust (positive or negative).</param>
    /// <param name="reason">Adjustment reason.</param>
    /// <param name="adjustedByUserId">User making adjustment.</param>
    /// <returns>Updated allocation.</returns>
    Task<LeaveAllocation> AdjustBalanceAsync(
        int employeeId, int leaveTypeId, int year, decimal days, string reason, int adjustedByUserId);

    /// <summary>
    /// Checks if employee has sufficient balance for request.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="leaveTypeId">Leave type ID.</param>
    /// <param name="days">Days requested.</param>
    /// <returns>True if sufficient balance.</returns>
    Task<bool> HasSufficientBalanceAsync(int employeeId, int leaveTypeId, decimal days);

    #endregion

    #region Calendar

    /// <summary>
    /// Gets leave calendar view.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <returns>Calendar view.</returns>
    Task<LeaveCalendarView> GetCalendarViewAsync(DateOnly startDate, DateOnly endDate, int? departmentId = null);

    /// <summary>
    /// Checks coverage for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <returns>List of coverage info by day.</returns>
    Task<IReadOnlyList<DayCoverage>> CheckCoverageAsync(DateOnly startDate, DateOnly endDate, int? departmentId = null);

    /// <summary>
    /// Checks if there are coverage conflicts for a proposed leave.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of conflict warnings.</returns>
    Task<IReadOnlyList<string>> CheckCoverageConflictsAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    #endregion

    #region Reports

    /// <summary>
    /// Generates leave balance report.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <returns>Balance report.</returns>
    Task<LeaveBalanceReport> GenerateBalanceReportAsync(int year, int? departmentId = null);

    /// <summary>
    /// Gets leave history for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Leave history.</returns>
    Task<EmployeeLeaveHistory> GetEmployeeHistoryAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Generates leave utilization report.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <returns>Utilization report.</returns>
    Task<LeaveUtilizationReport> GenerateUtilizationReportAsync(int year, int? departmentId = null);

    #endregion

    #region Utilities

    /// <summary>
    /// Calculates working days between dates.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="excludeWeekends">Exclude weekends.</param>
    /// <param name="excludeHolidays">Exclude public holidays.</param>
    /// <returns>Number of working days.</returns>
    Task<decimal> CalculateWorkingDaysAsync(DateOnly startDate, DateOnly endDate, bool excludeWeekends = true, bool excludeHolidays = true);

    /// <summary>
    /// Gets public holidays for a year.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <returns>List of holiday dates.</returns>
    Task<IReadOnlyList<DateOnly>> GetPublicHolidaysAsync(int year);

    /// <summary>
    /// Adds a public holiday.
    /// </summary>
    /// <param name="date">Holiday date.</param>
    /// <param name="name">Holiday name.</param>
    /// <returns>True if added.</returns>
    Task<bool> AddPublicHolidayAsync(DateOnly date, string name);

    #endregion

    #region Settings

    /// <summary>
    /// Gets leave settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<LeaveSettings> GetSettingsAsync();

    /// <summary>
    /// Updates leave settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<LeaveSettings> UpdateSettingsAsync(LeaveSettings settings);

    #endregion

    #region Events

    /// <summary>Raised when a leave request is submitted.</summary>
    event EventHandler<LeaveEventArgs>? RequestSubmitted;

    /// <summary>Raised when a leave request is approved.</summary>
    event EventHandler<LeaveEventArgs>? RequestApproved;

    /// <summary>Raised when a leave request is rejected.</summary>
    event EventHandler<LeaveEventArgs>? RequestRejected;

    /// <summary>Raised when a leave request is cancelled.</summary>
    event EventHandler<LeaveEventArgs>? RequestCancelled;

    #endregion
}
