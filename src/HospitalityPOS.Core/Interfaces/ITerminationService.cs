// src/HospitalityPOS.Core/Interfaces/ITerminationService.cs
// Service interface for employee termination and final settlement.
// Compliant with Kenya Employment Act 2007.

using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing employee terminations and final settlements.
/// </summary>
public interface ITerminationService
{
    #region Termination CRUD

    /// <summary>
    /// Initiates an employee termination.
    /// </summary>
    Task<EmployeeTermination> InitiateTerminationAsync(TerminationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a termination by ID.
    /// </summary>
    Task<EmployeeTermination?> GetByIdAsync(int terminationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a termination by reference number.
    /// </summary>
    Task<EmployeeTermination?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets termination for an employee.
    /// </summary>
    Task<EmployeeTermination?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all terminations with optional filters.
    /// </summary>
    Task<IReadOnlyList<EmployeeTermination>> GetTerminationsAsync(TerminationFilterRequest? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending terminations awaiting action.
    /// </summary>
    Task<IReadOnlyList<EmployeeTermination>> GetPendingTerminationsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Calculations

    /// <summary>
    /// Calculates the complete final settlement for an employee.
    /// </summary>
    Task<TerminationCalculation> CalculateFinalSettlementAsync(int employeeId, TerminationType type, DateOnly effectiveDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates severance pay.
    /// Kenya law: 15 days' basic wage per completed year of service (redundancy only).
    /// </summary>
    Task<decimal> CalculateSeverancePayAsync(int employeeId, DateOnly terminationDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates leave encashment (payment for untaken leave).
    /// </summary>
    Task<decimal> CalculateLeaveEncashmentAsync(int employeeId, DateOnly terminationDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates notice pay (if notice period not served).
    /// </summary>
    Task<decimal> CalculateNoticePayAsync(int employeeId, bool noticePeriodServed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates pro-rata salary for days worked in final month.
    /// </summary>
    Task<decimal> CalculateProRataSalaryAsync(int employeeId, DateOnly lastWorkingDay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the required notice period for an employee.
    /// Based on Kenya Employment Act and contract terms.
    /// </summary>
    Task<int> GetNoticePeriodDaysAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates years and months of service.
    /// </summary>
    Task<ServiceDuration> CalculateServiceDurationAsync(int employeeId, DateOnly asOfDate, CancellationToken cancellationToken = default);

    #endregion

    #region Workflow

    /// <summary>
    /// Approves a termination.
    /// </summary>
    Task<TerminationResult> ApproveTerminationAsync(int terminationId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates final settlement (after changes).
    /// </summary>
    Task<TerminationResult> RecalculateSettlementAsync(int terminationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes final payment.
    /// </summary>
    Task<TerminationResult> ProcessFinalPaymentAsync(int terminationId, string paymentMethod, string? paymentReference = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks termination as complete.
    /// </summary>
    Task<TerminationResult> CompleteTerminationAsync(int terminationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a termination (only if not completed).
    /// </summary>
    Task<TerminationResult> CancelTerminationAsync(int terminationId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Clearance

    /// <summary>
    /// Completes a departmental clearance.
    /// </summary>
    Task<TerminationResult> CompleteClearanceAsync(int terminationId, ClearanceType clearanceType, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets clearance status for a termination.
    /// </summary>
    Task<ClearanceStatus> GetClearanceStatusAsync(int terminationId, CancellationToken cancellationToken = default);

    #endregion

    #region Documents

    /// <summary>
    /// Generates Certificate of Service.
    /// Required by Kenya Employment Act upon termination.
    /// </summary>
    Task<byte[]> GenerateCertificateOfServiceAsync(int terminationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates Final Settlement Statement.
    /// </summary>
    Task<byte[]> GenerateFinalSettlementStatementAsync(int terminationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks Certificate of Service as issued.
    /// </summary>
    Task<TerminationResult> MarkCertificateIssuedAsync(int terminationId, string? documentPath = null, CancellationToken cancellationToken = default);

    #endregion

    #region Exit Interview

    /// <summary>
    /// Records exit interview completion.
    /// </summary>
    Task<TerminationResult> RecordExitInterviewAsync(int terminationId, string notes, CancellationToken cancellationToken = default);

    #endregion

    #region Reports

    /// <summary>
    /// Generates termination summary report.
    /// </summary>
    Task<TerminationSummaryReport> GenerateSummaryReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets turnover statistics.
    /// </summary>
    Task<TurnoverStatistics> GetTurnoverStatisticsAsync(int year, CancellationToken cancellationToken = default);

    #endregion

    #region Utility

    /// <summary>
    /// Generates a unique reference number.
    /// </summary>
    Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if employee has any blockers for termination.
    /// </summary>
    Task<TerminationBlockers> CheckTerminationBlockersAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Request for initiating a termination.
/// </summary>
public record TerminationRequest(
    int EmployeeId,
    TerminationType TerminationType,
    DateOnly NoticeDate,
    DateOnly EffectiveDate,
    DateOnly LastWorkingDay,
    string Reason,
    string? DetailedNotes = null,
    bool NoticePeriodServed = true
);

/// <summary>
/// Filter for querying terminations.
/// </summary>
public record TerminationFilterRequest(
    TerminationType? TerminationType = null,
    TerminationStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? DepartmentId = null
);

/// <summary>
/// Result of a termination operation.
/// </summary>
public record TerminationResult(
    bool Success,
    string? Message,
    EmployeeTermination? Termination = null
);

/// <summary>
/// Complete termination calculation breakdown.
/// </summary>
public record TerminationCalculation(
    int EmployeeId,
    string EmployeeName,
    DateOnly TerminationDate,
    TerminationType TerminationType,
    int YearsOfService,
    int MonthsOfService,
    // Earnings
    decimal ProRataSalary,
    decimal LeaveEncashment,
    int AccruedLeaveDays,
    decimal NoticePay,
    decimal SeverancePay,
    decimal OtherEarnings,
    decimal TotalEarnings,
    // Deductions
    decimal OutstandingLoans,
    decimal OutstandingAdvances,
    decimal PendingDeductions,
    decimal TaxPayable,
    decimal OtherDeductions,
    decimal TotalDeductions,
    // Net
    decimal NetSettlement,
    // Details
    string? EarningsBreakdown,
    string? DeductionsBreakdown
);

/// <summary>
/// Service duration (years and months).
/// </summary>
public record ServiceDuration(
    int Years,
    int Months,
    int TotalMonths
);

/// <summary>
/// Clearance type enumeration.
/// </summary>
public enum ClearanceType
{
    IT = 0,
    Finance = 1,
    HR = 2,
    Operations = 3
}

/// <summary>
/// Clearance status for all departments.
/// </summary>
public record ClearanceStatus(
    bool ITCleared,
    DateOnly? ITClearanceDate,
    string? ITNotes,
    bool FinanceCleared,
    DateOnly? FinanceClearanceDate,
    string? FinanceNotes,
    bool HRCleared,
    DateOnly? HRClearanceDate,
    string? HRNotes,
    bool OperationsCleared,
    DateOnly? OperationsClearanceDate,
    string? OperationsNotes,
    bool AllCleared
);

/// <summary>
/// Termination blockers check result.
/// </summary>
public record TerminationBlockers(
    bool HasBlockers,
    bool HasActiveLoans,
    decimal OutstandingLoanAmount,
    bool HasPendingDeductions,
    decimal PendingDeductionAmount,
    bool HasPendingLeaveRequests,
    int PendingLeaveRequestCount,
    List<string> BlockerMessages
);

/// <summary>
/// Termination summary report.
/// </summary>
public record TerminationSummaryReport(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalTerminations,
    decimal TotalSettlementPaid,
    List<TerminationByTypeBreakdown> ByType,
    List<TerminationByDepartmentBreakdown> ByDepartment,
    decimal AverageServiceYears,
    decimal AverageSettlement
);

/// <summary>
/// Termination breakdown by type.
/// </summary>
public record TerminationByTypeBreakdown(
    TerminationType TerminationType,
    int Count,
    decimal TotalSettlement
);

/// <summary>
/// Termination breakdown by department.
/// </summary>
public record TerminationByDepartmentBreakdown(
    string Department,
    int Count,
    decimal TotalSettlement
);

/// <summary>
/// Annual turnover statistics.
/// </summary>
public record TurnoverStatistics(
    int Year,
    int TotalTerminations,
    int TotalNewHires,
    decimal TurnoverRate,
    decimal VoluntaryTurnoverRate,
    decimal InvoluntaryTurnoverRate,
    int AverageHeadcount,
    List<MonthlyTurnover> MonthlyBreakdown
);

/// <summary>
/// Monthly turnover data.
/// </summary>
public record MonthlyTurnover(
    int Month,
    string MonthName,
    int Terminations,
    int NewHires,
    int Headcount
);

#endregion
