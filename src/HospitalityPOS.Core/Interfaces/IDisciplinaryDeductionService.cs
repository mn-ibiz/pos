// src/HospitalityPOS.Core/Interfaces/IDisciplinaryDeductionService.cs
// Service interface for disciplinary deductions (fines/penalties).
// Compliant with Kenya Employment Act 2007 Section 19.

using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing disciplinary deductions.
/// </summary>
public interface IDisciplinaryDeductionService
{
    #region CRUD Operations

    /// <summary>
    /// Creates a new disciplinary deduction record.
    /// </summary>
    Task<DisciplinaryDeduction> CreateDeductionAsync(DisciplinaryDeductionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a deduction by ID.
    /// </summary>
    Task<DisciplinaryDeduction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a deduction by reference number.
    /// </summary>
    Task<DisciplinaryDeduction?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all deductions for an employee.
    /// </summary>
    Task<IReadOnlyList<DisciplinaryDeduction>> GetEmployeeDeductionsAsync(int employeeId, bool includeApplied = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending approval deductions.
    /// </summary>
    Task<IReadOnlyList<DisciplinaryDeduction>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all deductions with optional filters.
    /// </summary>
    Task<IReadOnlyList<DisciplinaryDeduction>> GetDeductionsAsync(DeductionFilterRequest? filter = null, CancellationToken cancellationToken = default);

    #endregion

    #region Workflow Operations

    /// <summary>
    /// Approves a disciplinary deduction.
    /// </summary>
    Task<DeductionResult> ApproveDeductionAsync(int deductionId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a disciplinary deduction.
    /// </summary>
    Task<DeductionResult> RejectDeductionAsync(int deductionId, int reviewerUserId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records employee acknowledgment of the deduction.
    /// </summary>
    Task<DeductionResult> RecordEmployeeAcknowledgmentAsync(int deductionId, string? response = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a deduction (only before it's applied to payroll).
    /// </summary>
    Task<DeductionResult> CancelDeductionAsync(int deductionId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Appeal Operations

    /// <summary>
    /// Submits an appeal for a deduction.
    /// </summary>
    Task<DeductionResult> SubmitAppealAsync(int deductionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an appeal decision.
    /// </summary>
    Task<DeductionResult> ProcessAppealAsync(int deductionId, int reviewerUserId, bool upheld, string decision, CancellationToken cancellationToken = default);

    #endregion

    #region Payroll Integration

    /// <summary>
    /// Gets total pending deductions for an employee for the next payroll.
    /// </summary>
    Task<decimal> GetPendingDeductionsAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a deduction has been applied to a payslip.
    /// </summary>
    Task RecordPayrollDeductionAsync(int deductionId, int payslipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deductions ready to be applied in the next payroll.
    /// </summary>
    Task<IReadOnlyList<DisciplinaryDeduction>> GetDeductionsForPayrollAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a new deduction would violate the take-home rule.
    /// Kenya law: employee must take home at least 1/3 of salary.
    /// </summary>
    Task<bool> WouldViolateTakeHomeRuleAsync(int employeeId, decimal deductionAmount, decimal grossSalary, decimal otherDeductions, CancellationToken cancellationToken = default);

    #endregion

    #region Helper Methods for Specific Deduction Types

    /// <summary>
    /// Creates an absence deduction (1 day's wages per day absent).
    /// </summary>
    Task<DisciplinaryDeduction> CreateAbsenceDeductionAsync(int employeeId, DateOnly[] absenceDates, string description, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a damage deduction.
    /// </summary>
    Task<DisciplinaryDeduction> CreateDamageDeductionAsync(int employeeId, decimal damageAmount, string description, string? evidencePath, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a cash shortage deduction.
    /// </summary>
    Task<DisciplinaryDeduction> CreateCashShortageDeductionAsync(int employeeId, decimal shortageAmount, DateOnly incidentDate, string description, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an overpayment recovery deduction.
    /// </summary>
    Task<DisciplinaryDeduction> CreateOverpaymentDeductionAsync(int employeeId, decimal overpaymentAmount, string description, int createdByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Reports

    /// <summary>
    /// Generates a disciplinary deduction summary report.
    /// </summary>
    Task<DeductionSummaryReport> GenerateSummaryReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deduction history for an employee.
    /// </summary>
    Task<EmployeeDeductionHistory> GetEmployeeHistoryAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion

    #region Utility

    /// <summary>
    /// Generates a unique reference number.
    /// </summary>
    Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the daily wage rate for an employee.
    /// </summary>
    Task<decimal> CalculateDailyWageAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Request for creating a disciplinary deduction.
/// </summary>
public record DisciplinaryDeductionRequest(
    int EmployeeId,
    DeductionReasonType ReasonType,
    DateOnly IncidentDate,
    string Description,
    decimal Amount,
    int? DaysAbsent = null,
    string? EvidenceDocumentPath = null,
    decimal? ActualLossAmount = null,
    int? WitnessEmployeeId = null,
    string? Notes = null
);

/// <summary>
/// Filter for querying deductions.
/// </summary>
public record DeductionFilterRequest(
    int? EmployeeId = null,
    DeductionReasonType? ReasonType = null,
    DisciplinaryDeductionStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
);

/// <summary>
/// Result of a deduction operation.
/// </summary>
public record DeductionResult(
    bool Success,
    string? Message,
    DisciplinaryDeduction? Deduction = null
);

/// <summary>
/// Disciplinary deduction summary report.
/// </summary>
public record DeductionSummaryReport(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDeductions,
    decimal TotalAmount,
    int ApprovedCount,
    decimal ApprovedAmount,
    int RejectedCount,
    int PendingCount,
    int AppealedCount,
    List<DeductionByTypeBreakdown> ByType
);

/// <summary>
/// Deduction breakdown by type.
/// </summary>
public record DeductionByTypeBreakdown(
    DeductionReasonType ReasonType,
    int Count,
    decimal TotalAmount
);

/// <summary>
/// Employee deduction history.
/// </summary>
public record EmployeeDeductionHistory(
    int EmployeeId,
    string EmployeeName,
    int TotalDeductions,
    decimal TotalAmount,
    decimal TotalDeducted,
    decimal TotalPending,
    IReadOnlyList<DisciplinaryDeduction> Deductions
);

#endregion
