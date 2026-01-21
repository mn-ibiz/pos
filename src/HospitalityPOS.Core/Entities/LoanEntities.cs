// src/HospitalityPOS.Core/Entities/LoanEntities.cs
// Employee loan and salary advance entities.
// Compliant with Kenya Employment Act 2007 Section 17 & 19.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of loan/advance.
/// </summary>
public enum LoanType
{
    /// <summary>
    /// Short-term salary advance, usually 1-2 months max per Kenya law.
    /// </summary>
    SalaryAdvance = 0,

    /// <summary>
    /// Longer-term employee loan with defined repayment schedule.
    /// </summary>
    EmployeeLoan = 1,

    /// <summary>
    /// Emergency loan for special circumstances.
    /// </summary>
    EmergencyLoan = 2
}

/// <summary>
/// Status of a loan/advance.
/// </summary>
public enum LoanStatus
{
    Pending = 0,            // Awaiting approval
    Approved = 1,           // Approved but not yet disbursed
    Rejected = 2,           // Rejected
    Active = 3,             // Disbursed and being repaid
    Completed = 4,          // Fully repaid
    WrittenOff = 5,         // Bad debt written off
    Cancelled = 6           // Cancelled before disbursement
}

/// <summary>
/// Employee loan or salary advance.
/// Compliant with Kenya Employment Act Section 17 & 19.
/// </summary>
public class EmployeeLoan : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Auto-generated loan reference number.
    /// </summary>
    public string LoanNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of loan/advance.
    /// </summary>
    public LoanType LoanType { get; set; }

    /// <summary>
    /// Current status of the loan.
    /// </summary>
    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    /// <summary>
    /// Principal amount requested/approved.
    /// </summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>
    /// Annual interest rate (0 for salary advances).
    /// </summary>
    public decimal InterestRate { get; set; }

    /// <summary>
    /// Total interest over loan period.
    /// </summary>
    public decimal TotalInterest { get; set; }

    /// <summary>
    /// Total amount to be repaid (principal + interest).
    /// </summary>
    public decimal TotalAmountDue { get; set; }

    /// <summary>
    /// Amount already paid.
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Outstanding balance.
    /// </summary>
    public decimal OutstandingBalance => TotalAmountDue - AmountPaid;

    /// <summary>
    /// Number of installments.
    /// </summary>
    public int NumberOfInstallments { get; set; }

    /// <summary>
    /// Monthly installment amount.
    /// </summary>
    public decimal MonthlyInstallment { get; set; }

    /// <summary>
    /// Date of disbursement.
    /// </summary>
    public DateOnly? DisbursementDate { get; set; }

    /// <summary>
    /// Application date.
    /// </summary>
    public DateOnly ApplicationDate { get; set; }

    /// <summary>
    /// Requested disbursement date.
    /// </summary>
    public DateOnly? RequestedDisbursementDate { get; set; }

    /// <summary>
    /// Date of first installment/deduction.
    /// </summary>
    public DateOnly? FirstInstallmentDate { get; set; }

    /// <summary>
    /// Expected completion date.
    /// </summary>
    public DateOnly ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Number of installments already paid.
    /// </summary>
    public int InstallmentsPaid { get; set; }

    /// <summary>
    /// Date of last payment received.
    /// </summary>
    public DateOnly? LastPaymentDate { get; set; }

    /// <summary>
    /// Whether the loan requires a guarantor.
    /// </summary>
    public bool RequiresGuarantor { get; set; }

    /// <summary>
    /// Actual completion date (when fully repaid).
    /// </summary>
    public DateOnly? ActualCompletionDate { get; set; }

    /// <summary>
    /// Purpose/reason for the loan.
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Path to signed loan agreement document.
    /// </summary>
    public string? AgreementDocumentPath { get; set; }

    /// <summary>
    /// User who approved the loan.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date/time.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Approval date (DateOnly for convenience).
    /// </summary>
    public DateOnly? ApprovalDate { get; set; }

    /// <summary>
    /// Approval notes.
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// User who rejected the loan (if rejected).
    /// </summary>
    public int? RejectedByUserId { get; set; }

    /// <summary>
    /// Rejection date/time.
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Rejection reason.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Warning flag: Loan exceeds 2 months salary (Kenya law limit for court recovery).
    /// </summary>
    public bool ExceedsTwoMonthsSalary { get; set; }

    /// <summary>
    /// Employee's basic salary at time of application (for validation).
    /// </summary>
    public decimal EmployeeBasicSalaryAtApplication { get; set; }

    /// <summary>
    /// Guarantor employee ID (if required).
    /// </summary>
    public int? GuarantorEmployeeId { get; set; }

    /// <summary>
    /// Notes or additional information.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Employee? Guarantor { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual User? RejectedByUser { get; set; }
    public virtual ICollection<LoanRepayment> Repayments { get; set; } = new List<LoanRepayment>();
}

/// <summary>
/// Individual loan repayment record.
/// </summary>
public class LoanRepayment : BaseEntity
{
    /// <summary>
    /// Employee loan ID.
    /// </summary>
    public int EmployeeLoanId { get; set; }

    /// <summary>
    /// Installment number (1, 2, 3, etc.).
    /// </summary>
    public int InstallmentNumber { get; set; }

    /// <summary>
    /// Due date for this installment.
    /// </summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Amount due for this installment.
    /// </summary>
    public decimal AmountDue { get; set; }

    /// <summary>
    /// Amount actually paid.
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Date of payment.
    /// </summary>
    public DateOnly? PaidDate { get; set; }

    /// <summary>
    /// Whether this installment is fully paid.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Whether this was deducted via payroll.
    /// </summary>
    public bool IsFromPayroll { get; set; }

    /// <summary>
    /// Link to payslip detail if deducted via payroll.
    /// </summary>
    public int? PayslipDetailId { get; set; }

    /// <summary>
    /// Principal portion of this payment.
    /// </summary>
    public decimal PrincipalPortion { get; set; }

    /// <summary>
    /// Interest portion of this payment.
    /// </summary>
    public decimal InterestPortion { get; set; }

    /// <summary>
    /// Running balance after this payment.
    /// </summary>
    public decimal BalanceAfterPayment { get; set; }

    /// <summary>
    /// Notes about this repayment.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual EmployeeLoan? EmployeeLoan { get; set; }
    public virtual PayslipDetail? PayslipDetail { get; set; }
}
