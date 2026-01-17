// src/HospitalityPOS.Core/Interfaces/ISchedulingService.cs
// Service interface for employee shift scheduling and workforce management
// Story 45-2: Shift Scheduling

using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for employee shift scheduling and workforce management.
/// Handles shift creation, recurring patterns, swap requests, and coverage analysis.
/// </summary>
public interface ISchedulingService
{
    #region Shift Management

    /// <summary>
    /// Creates a new shift.
    /// </summary>
    /// <param name="request">Shift creation request.</param>
    /// <param name="userId">User creating the shift.</param>
    /// <returns>Result of the operation.</returns>
    Task<ShiftResult> CreateShiftAsync(ShiftRequest request, int userId);

    /// <summary>
    /// Updates an existing shift.
    /// </summary>
    /// <param name="request">Shift update request.</param>
    /// <param name="userId">User updating the shift.</param>
    /// <returns>Result of the operation.</returns>
    Task<ShiftResult> UpdateShiftAsync(ShiftRequest request, int userId);

    /// <summary>
    /// Deletes a shift.
    /// </summary>
    /// <param name="shiftId">Shift ID.</param>
    /// <param name="userId">User deleting the shift.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteShiftAsync(int shiftId, int userId);

    /// <summary>
    /// Gets a shift by ID.
    /// </summary>
    /// <param name="shiftId">Shift ID.</param>
    /// <returns>Shift or null.</returns>
    Task<Shift?> GetShiftAsync(int shiftId);

    /// <summary>
    /// Gets all shifts for an employee in a date range.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of shifts.</returns>
    Task<IReadOnlyList<Shift>> GetEmployeeShiftsAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets all shifts for a date.
    /// </summary>
    /// <param name="date">Date.</param>
    /// <returns>List of shifts.</returns>
    Task<IReadOnlyList<Shift>> GetShiftsByDateAsync(DateOnly date);

    /// <summary>
    /// Gets all shifts for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of shifts.</returns>
    Task<IReadOnlyList<Shift>> GetShiftsAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Recurring Patterns

    /// <summary>
    /// Creates a recurring shift pattern.
    /// </summary>
    /// <param name="pattern">Pattern to create.</param>
    /// <param name="userId">User creating the pattern.</param>
    /// <returns>Created pattern.</returns>
    Task<RecurringShiftPattern> CreatePatternAsync(RecurringShiftPattern pattern, int userId);

    /// <summary>
    /// Updates a recurring shift pattern.
    /// </summary>
    /// <param name="pattern">Pattern to update.</param>
    /// <param name="userId">User updating the pattern.</param>
    /// <returns>Updated pattern.</returns>
    Task<RecurringShiftPattern> UpdatePatternAsync(RecurringShiftPattern pattern, int userId);

    /// <summary>
    /// Deactivates a recurring pattern.
    /// </summary>
    /// <param name="patternId">Pattern ID.</param>
    /// <param name="userId">User deactivating.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivatePatternAsync(int patternId, int userId);

    /// <summary>
    /// Gets all active patterns for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>List of patterns.</returns>
    Task<IReadOnlyList<RecurringShiftPattern>> GetEmployeePatternsAsync(int employeeId);

    /// <summary>
    /// Gets all active patterns.
    /// </summary>
    /// <returns>List of patterns.</returns>
    Task<IReadOnlyList<RecurringShiftPattern>> GetAllPatternsAsync();

    /// <summary>
    /// Generates shifts from recurring patterns for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="userId">User generating shifts.</param>
    /// <returns>Result with generated shifts.</returns>
    Task<ShiftResult> GenerateFromPatternsAsync(DateOnly startDate, DateOnly endDate, int userId);

    #endregion

    #region Shift Swap

    /// <summary>
    /// Initiates a shift swap request.
    /// </summary>
    /// <param name="request">Swap initiation request.</param>
    /// <returns>Result of the operation.</returns>
    Task<SwapResult> InitiateSwapAsync(SwapInitiateRequest request);

    /// <summary>
    /// Responds to a swap request (accept/reject).
    /// </summary>
    /// <param name="request">Response request.</param>
    /// <returns>Result of the operation.</returns>
    Task<SwapResult> RespondToSwapAsync(SwapResponseRequest request);

    /// <summary>
    /// Manager approves or rejects a swap request.
    /// </summary>
    /// <param name="request">Approval request.</param>
    /// <returns>Result of the operation.</returns>
    Task<SwapResult> ProcessSwapApprovalAsync(SwapApprovalRequest request);

    /// <summary>
    /// Gets swap requests for an employee (as requester or target).
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="includeHistory">Include past requests.</param>
    /// <returns>List of swap requests.</returns>
    Task<IReadOnlyList<ShiftSwapRequest>> GetEmployeeSwapRequestsAsync(int employeeId, bool includeHistory = false);

    /// <summary>
    /// Gets pending swap requests requiring manager approval.
    /// </summary>
    /// <returns>List of pending requests.</returns>
    Task<IReadOnlyList<ShiftSwapRequest>> GetPendingApprovalRequestsAsync();

    /// <summary>
    /// Gets a swap request by ID.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <returns>Swap request or null.</returns>
    Task<ShiftSwapRequest?> GetSwapRequestAsync(int requestId);

    #endregion

    #region Conflict Detection

    /// <summary>
    /// Checks for conflicts with a proposed shift.
    /// </summary>
    /// <param name="request">Shift request to check.</param>
    /// <returns>List of conflicts.</returns>
    Task<IReadOnlyList<ShiftConflict>> CheckConflictsAsync(ShiftRequest request);

    /// <summary>
    /// Validates a shift swap for conflicts.
    /// </summary>
    /// <param name="request">Swap request to validate.</param>
    /// <returns>List of conflicts.</returns>
    Task<IReadOnlyList<ShiftConflict>> ValidateSwapAsync(SwapInitiateRequest request);

    /// <summary>
    /// Gets all conflicts for an employee's schedule.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of conflicts.</returns>
    Task<IReadOnlyList<ShiftConflict>> GetEmployeeConflictsAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    #endregion

    #region Coverage Analysis

    /// <summary>
    /// Gets or creates coverage requirements.
    /// </summary>
    /// <returns>List of coverage requirements.</returns>
    Task<IReadOnlyList<CoverageRequirement>> GetCoverageRequirementsAsync();

    /// <summary>
    /// Updates coverage requirements.
    /// </summary>
    /// <param name="requirements">Requirements to update.</param>
    /// <returns>Updated requirements.</returns>
    Task<IReadOnlyList<CoverageRequirement>> UpdateCoverageRequirementsAsync(IEnumerable<CoverageRequirement> requirements);

    /// <summary>
    /// Analyzes coverage for a date.
    /// </summary>
    /// <param name="date">Date to analyze.</param>
    /// <returns>Coverage analysis results.</returns>
    Task<IReadOnlyList<CoverageAnalysis>> AnalyzeCoverageAsync(DateOnly date);

    /// <summary>
    /// Gets daily coverage summary for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of daily summaries.</returns>
    Task<IReadOnlyList<DailyCoverageSummary>> GetCoverageSummaryAsync(DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets understaffed periods for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of understaffed periods.</returns>
    Task<IReadOnlyList<CoverageAnalysis>> GetUnderstaffedPeriodsAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Schedule Views

    /// <summary>
    /// Gets weekly schedule view for calendar display.
    /// </summary>
    /// <param name="weekStartDate">Start date of the week (usually Sunday or Monday).</param>
    /// <returns>Weekly schedule view.</returns>
    Task<WeeklyScheduleView> GetWeeklyScheduleAsync(DateOnly weekStartDate);

    /// <summary>
    /// Gets an employee's personal schedule view.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Personal schedule view.</returns>
    Task<MyScheduleView> GetMyScheduleAsync(int employeeId);

    /// <summary>
    /// Gets coworkers on shift for a specific shift.
    /// </summary>
    /// <param name="shiftId">Shift ID.</param>
    /// <returns>List of coworkers.</returns>
    Task<IReadOnlyList<CoworkerOnShift>> GetCoworkersForShiftAsync(int shiftId);

    #endregion

    #region Attendance Integration

    /// <summary>
    /// Compares scheduled vs actual attendance for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="employeeIds">Optional filter by employees.</param>
    /// <returns>List of comparisons.</returns>
    Task<IReadOnlyList<ScheduleAttendanceComparison>> CompareScheduleToAttendanceAsync(
        DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Generates schedule adherence report.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="employeeIds">Optional filter by employees.</param>
    /// <returns>Adherence report.</returns>
    Task<ScheduleAdherenceReport> GenerateAdherenceReportAsync(
        DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Gets today's shift for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Today's shift or null.</returns>
    Task<Shift?> GetTodayShiftAsync(int employeeId);

    /// <summary>
    /// Marks a shift based on attendance (completed, no-show, etc.).
    /// </summary>
    /// <param name="shiftId">Shift ID.</param>
    /// <param name="status">New status.</param>
    /// <returns>Updated shift.</returns>
    Task<Shift> UpdateShiftStatusAsync(int shiftId, ShiftStatus status);

    #endregion

    #region Settings

    /// <summary>
    /// Gets scheduling settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<SchedulingSettings> GetSettingsAsync();

    /// <summary>
    /// Updates scheduling settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<SchedulingSettings> UpdateSettingsAsync(SchedulingSettings settings);

    #endregion

    #region Events

    /// <summary>Raised when a shift is created.</summary>
    event EventHandler<ScheduleEventArgs>? ShiftCreated;

    /// <summary>Raised when a shift is updated.</summary>
    event EventHandler<ScheduleEventArgs>? ShiftUpdated;

    /// <summary>Raised when a shift is deleted.</summary>
    event EventHandler<ScheduleEventArgs>? ShiftDeleted;

    /// <summary>Raised when a swap request is created.</summary>
    event EventHandler<SwapRequestEventArgs>? SwapRequested;

    /// <summary>Raised when a swap is completed.</summary>
    event EventHandler<SwapRequestEventArgs>? SwapCompleted;

    #endregion
}
