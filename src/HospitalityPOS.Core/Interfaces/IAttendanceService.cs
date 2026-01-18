// src/HospitalityPOS.Core/Interfaces/IAttendanceService.cs
// Service interface for employee attendance tracking
// Story 45-1: Time and Attendance (Enhanced)

using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models.HR;
using AttendanceSummaryModel = HospitalityPOS.Core.Models.HR.AttendanceSummary;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for employee attendance tracking.
/// Handles clock in/out, break tracking, reporting, and payroll integration.
/// </summary>
public interface IAttendanceService
{
    #region Clock Operations (Enhanced)

    /// <summary>
    /// Clocks in an employee with PIN authentication.
    /// </summary>
    /// <param name="request">Clock in request with employee ID and PIN.</param>
    /// <returns>Result of the clock in operation.</returns>
    Task<ClockResult> ClockInWithPinAsync(ClockInRequest request);

    /// <summary>
    /// Clocks out an employee with PIN authentication.
    /// </summary>
    /// <param name="request">Clock out request with employee ID and PIN.</param>
    /// <returns>Result of the clock out operation.</returns>
    Task<ClockResult> ClockOutWithPinAsync(ClockOutRequest request);

    /// <summary>
    /// Starts a break for an employee with PIN authentication.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="pin">Employee PIN.</param>
    /// <returns>Result of the break start operation.</returns>
    Task<ClockResult> StartBreakWithPinAsync(int employeeId, string pin);

    /// <summary>
    /// Ends a break for an employee with PIN authentication.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="pin">Employee PIN.</param>
    /// <returns>Result of the break end operation.</returns>
    Task<ClockResult> EndBreakWithPinAsync(int employeeId, string pin);

    /// <summary>
    /// Gets the current clock status for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Current clock status.</returns>
    Task<ClockStatus> GetCurrentStatusAsync(int employeeId);

    // Legacy clock operations (for backward compatibility)
    Task<Attendance> ClockInAsync(int employeeId, TimeSpan? clockInTime = null, CancellationToken cancellationToken = default);
    Task<Attendance> ClockOutAsync(int employeeId, TimeSpan? clockOutTime = null, CancellationToken cancellationToken = default);
    Task<Attendance> StartBreakAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<Attendance> EndBreakAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion

    #region Attendance Records

    /// <summary>
    /// Gets today's attendance record for an employee (enhanced).
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Today's attendance record or null.</returns>
    Task<AttendanceRecord?> GetTodayRecordAsync(int employeeId);

    /// <summary>
    /// Gets attendance record for an employee on a specific date.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="date">Date.</param>
    /// <returns>Attendance record or null.</returns>
    Task<AttendanceRecord?> GetRecordAsync(int employeeId, DateOnly date);

    /// <summary>
    /// Gets attendance records for an employee in a date range (enhanced).
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of attendance records.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetRecordsAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets all attendance records for a specific date (enhanced).
    /// </summary>
    /// <param name="date">Date.</param>
    /// <returns>List of attendance records.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetRecordsForDateAsync(DateOnly date);

    // Legacy attendance records (for backward compatibility)
    Task<Attendance?> GetTodayAttendanceAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<Attendance?> GetAttendanceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetAttendanceByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetEmployeeAttendanceAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Manager Operations

    /// <summary>
    /// Edits an attendance record (manager override).
    /// </summary>
    /// <param name="request">Edit request with changes and reason.</param>
    /// <returns>Result of the edit operation.</returns>
    Task<ClockResult> EditRecordAsync(AttendanceEditRequest request);

    /// <summary>
    /// Adds a missed punch (manager override).
    /// </summary>
    /// <param name="request">Missed punch request.</param>
    /// <returns>Result of the operation.</returns>
    Task<ClockResult> AddMissedPunchAsync(MissedPunchRequest request);

    /// <summary>
    /// Marks an employee as absent for a date.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="date">Date.</param>
    /// <param name="reason">Reason for absence.</param>
    /// <param name="managerUserId">Manager user ID.</param>
    /// <returns>Created absence record.</returns>
    Task<AttendanceRecord> MarkAbsentAsync(int employeeId, DateOnly date, string reason, int managerUserId);

    /// <summary>
    /// Gets edit history for an attendance record.
    /// </summary>
    /// <param name="recordId">Attendance record ID.</param>
    /// <returns>List of edits.</returns>
    Task<IReadOnlyList<AttendanceEdit>> GetEditHistoryAsync(int recordId);

    /// <summary>
    /// Validates manager PIN.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="pin">PIN.</param>
    /// <returns>True if valid manager.</returns>
    Task<bool> ValidateManagerPinAsync(int userId, string pin);

    // Legacy manual entry (for backward compatibility)
    Task<Attendance> CreateManualAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<Attendance> UpdateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<bool> DeleteAttendanceAsync(int id, CancellationToken cancellationToken = default);

    #endregion

    #region Dashboard & Status

    /// <summary>
    /// Gets employees currently on shift.
    /// </summary>
    /// <returns>List of employees on shift.</returns>
    Task<IReadOnlyList<EmployeeOnShift>> GetEmployeesOnShiftAsync();

    /// <summary>
    /// Gets today's attendance summary for dashboard.
    /// </summary>
    /// <returns>Today's summary.</returns>
    Task<TodayAttendanceSummary> GetTodaySummaryAsync();

    /// <summary>
    /// Gets late arrivals for a date.
    /// </summary>
    /// <param name="date">Date.</param>
    /// <returns>List of late arrival records.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetLateArrivalsAsync(DateOnly date);

    /// <summary>
    /// Gets early departures for a date.
    /// </summary>
    /// <param name="date">Date.</param>
    /// <returns>List of early departure records.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetEarlyDeparturesAsync(DateOnly date);

    #endregion

    #region Reporting (Enhanced)

    /// <summary>
    /// Generates attendance report for date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="employeeIds">Optional filter by employee IDs.</param>
    /// <returns>Attendance report.</returns>
    Task<AttendanceReport> GenerateReportAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Generates detailed attendance report for a single employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Employee attendance report.</returns>
    Task<EmployeeAttendanceReport> GenerateEmployeeReportAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Calculates attendance summary for an employee (enhanced).
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Attendance summary.</returns>
    Task<AttendanceSummaryModel> CalculateSummaryAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    // Legacy reports (for backward compatibility)
    Task<AttendanceSummary> GetEmployeeAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceSummary>> GetAttendanceSummaryByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> CalculateOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Payroll Integration

    /// <summary>
    /// Exports attendance data for payroll.
    /// </summary>
    /// <param name="startDate">Period start date.</param>
    /// <param name="endDate">Period end date.</param>
    /// <param name="employeeIds">Optional filter by employee IDs.</param>
    /// <returns>Payroll export data.</returns>
    Task<PayrollExportData> ExportForPayrollAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Calculates regular and overtime hours.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Tuple of (regular hours, overtime hours).</returns>
    Task<(decimal RegularHours, decimal OvertimeHours)> CalculateHoursAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    #endregion

    #region Settings

    /// <summary>
    /// Gets attendance settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<AttendanceSettings> GetSettingsAsync();

    /// <summary>
    /// Updates attendance settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<AttendanceSettings> UpdateSettingsAsync(AttendanceSettings settings);

    #endregion

    #region Events

    /// <summary>
    /// Raised when an employee clocks in.
    /// </summary>
    event EventHandler<AttendanceEventArgs>? EmployeeClockIn;

    /// <summary>
    /// Raised when an employee clocks out.
    /// </summary>
    event EventHandler<AttendanceEventArgs>? EmployeeClockOut;

    /// <summary>
    /// Raised when an employee starts break.
    /// </summary>
    event EventHandler<AttendanceEventArgs>? EmployeeBreakStart;

    /// <summary>
    /// Raised when an employee ends break.
    /// </summary>
    event EventHandler<AttendanceEventArgs>? EmployeeBreakEnd;

    #endregion
}

/// <summary>
/// Attendance summary for reporting (legacy - kept for backward compatibility).
/// </summary>
public class AttendanceSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int TotalWorkDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int HalfDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AttendancePercentage => TotalWorkDays > 0 ? (decimal)PresentDays / TotalWorkDays * 100 : 0;
}
