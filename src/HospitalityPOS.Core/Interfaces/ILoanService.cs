// src/HospitalityPOS.Core/Interfaces/ILoanService.cs
// Service interface for employee loans and salary advances.
// Compliant with Kenya Employment Act 2007 Section 17 & 19.

using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing employee loans and salary advances.
/// </summary>
public interface ILoanService
{
    #region Loan/Advance CRUD

    /// <summary>
    /// Creates a new loan application.
    /// </summary>
    Task<EmployeeLoan> CreateLoanApplicationAsync(LoanApplicationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loan by ID.
    /// </summary>
    Task<EmployeeLoan?> GetLoanByIdAsync(int loanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loan by loan number.
    /// </summary>
    Task<EmployeeLoan?> GetLoanByNumberAsync(string loanNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all loans for an employee.
    /// </summary>
    Task<IReadOnlyList<EmployeeLoan>> GetEmployeeLoansAsync(int employeeId, bool includeCompleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending loan approvals.
    /// </summary>
    Task<IReadOnlyList<EmployeeLoan>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active loans (being repaid).
    /// </summary>
    Task<IReadOnlyList<EmployeeLoan>> GetActiveLoansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all loans with optional filters.
    /// </summary>
    Task<IReadOnlyList<EmployeeLoan>> GetLoansAsync(LoanFilterRequest? filter = null, CancellationToken cancellationToken = default);

    #endregion

    #region Approval Workflow

    /// <summary>
    /// Approves a loan application.
    /// </summary>
    Task<LoanResult> ApproveLoanAsync(int loanId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a loan application.
    /// </summary>
    Task<LoanResult> RejectLoanAsync(int loanId, int approverUserId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a loan (only before disbursement).
    /// </summary>
    Task<LoanResult> CancelLoanAsync(int loanId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Disbursement

    /// <summary>
    /// Marks a loan as disbursed.
    /// </summary>
    Task<LoanResult> MarkAsDisbursedAsync(int loanId, DateOnly disbursementDate, CancellationToken cancellationToken = default);

    #endregion

    #region Repayment

    /// <summary>
    /// Records a manual repayment.
    /// </summary>
    Task<LoanResult> RecordRepaymentAsync(int loanId, decimal amount, DateOnly paymentDate, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the repayment schedule for a loan.
    /// </summary>
    Task<IReadOnlyList<LoanRepayment>> GetRepaymentScheduleAsync(int loanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total pending deduction amount for an employee for the next payroll.
    /// </summary>
    Task<decimal> GetPendingDeductionAsync(int employeeId, DateOnly payrollDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a payroll deduction for loan repayment.
    /// </summary>
    Task RecordPayrollDeductionAsync(int loanId, int payslipDetailId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates repayment schedule for a loan.
    /// </summary>
    Task<IReadOnlyList<LoanRepayment>> GenerateRepaymentScheduleAsync(int loanId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation (Kenya Compliance)

    /// <summary>
    /// Checks if an employee is eligible for a loan.
    /// </summary>
    Task<LoanEligibilityResult> CheckEligibilityAsync(int employeeId, decimal requestedAmount, LoanType loanType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the maximum loan amount an employee can receive.
    /// Based on Kenya law: salary advances max 2 months, loans based on deduction capacity.
    /// </summary>
    Task<decimal> CalculateMaxLoanAmountAsync(int employeeId, LoanType loanType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the maximum deduction allowed for this employee.
    /// Kenya law: loan deduction max 50% of wages after other deductions.
    /// </summary>
    Task<decimal> CalculateMaxDeductionAsync(int employeeId, decimal grossSalary, decimal otherDeductions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a new deduction would violate the 2/3 take-home rule.
    /// Kenya law: employee must take home at least 1/3 of salary.
    /// </summary>
    Task<bool> WouldExceedTwoThirdsRuleAsync(int employeeId, decimal newDeduction, decimal grossSalary, decimal existingDeductions, CancellationToken cancellationToken = default);

    #endregion

    #region Reports

    /// <summary>
    /// Generates a loan summary report.
    /// </summary>
    Task<LoanSummaryReport> GenerateSummaryReportAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an employee loan statement.
    /// </summary>
    Task<EmployeeLoanStatement> GenerateEmployeeStatementAsync(int employeeId, int loanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outstanding loan balance for an employee.
    /// </summary>
    Task<decimal> GetOutstandingBalanceAsync(int employeeId, CancellationToken cancellationToken = default);

    #endregion

    #region Utility

    /// <summary>
    /// Generates a unique loan number.
    /// </summary>
    Task<string> GenerateLoanNumberAsync(LoanType loanType, CancellationToken cancellationToken = default);

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Request for creating a loan application.
/// </summary>
public record LoanApplicationRequest(
    int EmployeeId,
    LoanType LoanType,
    decimal Amount,
    int NumberOfInstallments,
    string? Purpose,
    DateOnly RequestedDisbursementDate,
    int? GuarantorEmployeeId = null
);

/// <summary>
/// Filter for querying loans.
/// </summary>
public record LoanFilterRequest(
    int? EmployeeId = null,
    LoanType? LoanType = null,
    LoanStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
);

/// <summary>
/// Result of a loan operation.
/// </summary>
public record LoanResult(
    bool Success,
    string? Message,
    EmployeeLoan? Loan = null
);

/// <summary>
/// Result of loan eligibility check.
/// </summary>
public record LoanEligibilityResult(
    bool IsEligible,
    decimal MaxEligibleAmount,
    decimal CurrentOutstandingLoans,
    decimal CurrentMonthlyDeductions,
    decimal AvailableDeductionCapacity,
    List<string> Warnings,
    List<string> Errors
);

/// <summary>
/// Loan summary report.
/// </summary>
public record LoanSummaryReport(
    DateOnly AsOfDate,
    int TotalActiveLoans,
    decimal TotalOutstandingPrincipal,
    decimal TotalOutstandingInterest,
    decimal TotalOutstandingBalance,
    int TotalPendingApplications,
    decimal TotalPendingAmount,
    int OverdueInstallmentsCount,
    decimal OverdueAmount,
    List<LoanByTypeBreakdown> ByType
);

/// <summary>
/// Loan breakdown by type.
/// </summary>
public record LoanByTypeBreakdown(
    LoanType LoanType,
    int Count,
    decimal TotalAmount,
    decimal OutstandingBalance
);

/// <summary>
/// Employee loan statement.
/// </summary>
public record EmployeeLoanStatement(
    int LoanId,
    string LoanNumber,
    int EmployeeId,
    string EmployeeName,
    LoanType LoanType,
    decimal PrincipalAmount,
    decimal TotalInterest,
    decimal TotalAmountDue,
    decimal AmountPaid,
    decimal OutstandingBalance,
    DateOnly? DisbursementDate,
    DateOnly ExpectedCompletionDate,
    IReadOnlyList<LoanRepayment> Repayments
);

#endregion
