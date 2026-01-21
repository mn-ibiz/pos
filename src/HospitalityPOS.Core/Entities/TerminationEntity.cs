// src/HospitalityPOS.Core/Entities/TerminationEntity.cs
// Employee termination and final settlement entity.
// Compliant with Kenya Employment Act 2007.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of termination.
/// </summary>
public enum TerminationType
{
    /// <summary>
    /// Voluntary resignation by employee.
    /// </summary>
    Resignation = 0,

    /// <summary>
    /// Redundancy due to restructuring/business needs.
    /// Employee entitled to severance pay.
    /// </summary>
    Redundancy = 1,

    /// <summary>
    /// Dismissal for cause (misconduct).
    /// No severance pay.
    /// </summary>
    Dismissal = 2,

    /// <summary>
    /// Contract end (for fixed-term contracts).
    /// </summary>
    EndOfContract = 3,

    /// <summary>
    /// Normal retirement.
    /// </summary>
    Retirement = 4,

    /// <summary>
    /// Death of employee.
    /// </summary>
    Death = 5,

    /// <summary>
    /// Mutual agreement between employer and employee.
    /// </summary>
    MutualAgreement = 6,

    /// <summary>
    /// Medical incapacity.
    /// </summary>
    MedicalIncapacity = 7,

    /// <summary>
    /// Abandonment of employment.
    /// </summary>
    Abandonment = 8
}

/// <summary>
/// Status of termination process.
/// </summary>
public enum TerminationStatus
{
    Initiated = 0,          // Termination initiated
    PendingApproval = 1,    // Awaiting HR/management approval
    Approved = 2,           // Termination approved
    Processing = 3,         // Calculating final dues
    AwaitingClearance = 4,  // Awaiting departmental clearance
    AwaitingPayment = 5,    // Final dues calculated, awaiting payment
    Completed = 6,          // Process complete
    Cancelled = 7           // Termination cancelled
}

/// <summary>
/// Employee termination record with final settlement calculation.
/// Kenya Employment Act 2007 compliant.
/// </summary>
public class EmployeeTermination : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Auto-generated termination reference number.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of termination.
    /// </summary>
    public TerminationType TerminationType { get; set; }

    /// <summary>
    /// Current status of termination process.
    /// </summary>
    public TerminationStatus Status { get; set; } = TerminationStatus.Initiated;

    /// <summary>
    /// Date notice was given.
    /// </summary>
    public DateOnly NoticeDate { get; set; }

    /// <summary>
    /// Effective termination date.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// Last actual working day.
    /// </summary>
    public DateOnly LastWorkingDay { get; set; }

    /// <summary>
    /// Required notice period in days per contract/law.
    /// </summary>
    public int NoticePeriodDays { get; set; }

    /// <summary>
    /// Whether notice period was served.
    /// </summary>
    public bool NoticePeriodServed { get; set; }

    /// <summary>
    /// Reason for termination.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Detailed notes about the termination.
    /// </summary>
    public string? DetailedNotes { get; set; }

    /// <summary>
    /// Years of service calculated.
    /// </summary>
    public int YearsOfService { get; set; }

    /// <summary>
    /// Months of service (partial year).
    /// </summary>
    public int MonthsOfService { get; set; }

    // Final Settlement - Earnings
    /// <summary>
    /// Days worked in final month.
    /// </summary>
    public decimal DaysWorkedInFinalMonth { get; set; }

    /// <summary>
    /// Pro-rata basic salary for days worked.
    /// </summary>
    public decimal ProRataBasicSalary { get; set; }

    /// <summary>
    /// Number of accrued leave days.
    /// </summary>
    public decimal AccruedLeaveDays { get; set; }

    /// <summary>
    /// Payment for accrued leave.
    /// </summary>
    public decimal LeavePayment { get; set; }

    /// <summary>
    /// Notice pay (if notice period not served).
    /// </summary>
    public decimal NoticePay { get; set; }

    /// <summary>
    /// Severance pay (15 days per year for redundancy).
    /// Kenya Employment Act: 15 days' basic wage per completed year.
    /// </summary>
    public decimal SeverancePay { get; set; }

    /// <summary>
    /// Any other earnings (commissions, bonuses, etc.).
    /// </summary>
    public decimal OtherEarnings { get; set; }

    /// <summary>
    /// Description of other earnings.
    /// </summary>
    public string? OtherEarningsDescription { get; set; }

    /// <summary>
    /// Total earnings in final settlement.
    /// </summary>
    public decimal TotalEarnings { get; set; }

    // Final Settlement - Deductions
    /// <summary>
    /// Outstanding loan balance.
    /// </summary>
    public decimal OutstandingLoans { get; set; }

    /// <summary>
    /// Outstanding advance balance.
    /// </summary>
    public decimal OutstandingAdvances { get; set; }

    /// <summary>
    /// Pending disciplinary deductions.
    /// </summary>
    public decimal PendingDeductions { get; set; }

    /// <summary>
    /// Tax on termination benefits.
    /// </summary>
    public decimal TaxOnTermination { get; set; }

    /// <summary>
    /// Other deductions.
    /// </summary>
    public decimal OtherDeductions { get; set; }

    /// <summary>
    /// Description of other deductions.
    /// </summary>
    public string? OtherDeductionsDescription { get; set; }

    /// <summary>
    /// Total deductions.
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Net final settlement amount.
    /// </summary>
    public decimal NetFinalSettlement { get; set; }

    // Payment
    /// <summary>
    /// Date of final payment.
    /// </summary>
    public DateOnly? PaymentDate { get; set; }

    /// <summary>
    /// Payment reference/transaction ID.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    public string? PaymentMethod { get; set; }

    // Approval
    /// <summary>
    /// User who approved the termination.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date/time.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Approval notes.
    /// </summary>
    public string? ApprovalNotes { get; set; }

    // Certificate of Service
    /// <summary>
    /// Whether certificate of service has been issued.
    /// </summary>
    public bool CertificateIssued { get; set; }

    /// <summary>
    /// Date certificate was issued.
    /// </summary>
    public DateOnly? CertificateIssuedDate { get; set; }

    /// <summary>
    /// Path to certificate document.
    /// </summary>
    public string? CertificateDocumentPath { get; set; }

    // Clearance
    /// <summary>
    /// IT department clearance.
    /// </summary>
    public bool ITClearance { get; set; }

    /// <summary>
    /// IT clearance date.
    /// </summary>
    public DateOnly? ITClearanceDate { get; set; }

    /// <summary>
    /// IT clearance notes.
    /// </summary>
    public string? ITClearanceNotes { get; set; }

    /// <summary>
    /// Finance department clearance.
    /// </summary>
    public bool FinanceClearance { get; set; }

    /// <summary>
    /// Finance clearance date.
    /// </summary>
    public DateOnly? FinanceClearanceDate { get; set; }

    /// <summary>
    /// Finance clearance notes.
    /// </summary>
    public string? FinanceClearanceNotes { get; set; }

    /// <summary>
    /// HR department clearance.
    /// </summary>
    public bool HRClearance { get; set; }

    /// <summary>
    /// HR clearance date.
    /// </summary>
    public DateOnly? HRClearanceDate { get; set; }

    /// <summary>
    /// HR clearance notes.
    /// </summary>
    public string? HRClearanceNotes { get; set; }

    /// <summary>
    /// Store/Operations clearance.
    /// </summary>
    public bool OperationsClearance { get; set; }

    /// <summary>
    /// Operations clearance date.
    /// </summary>
    public DateOnly? OperationsClearanceDate { get; set; }

    /// <summary>
    /// Operations clearance notes.
    /// </summary>
    public string? OperationsClearanceNotes { get; set; }

    /// <summary>
    /// Whether all clearances are complete.
    /// </summary>
    public bool AllClearancesComplete => ITClearance && FinanceClearance && HRClearance && OperationsClearance;

    // Exit interview
    /// <summary>
    /// Whether exit interview was conducted.
    /// </summary>
    public bool ExitInterviewConducted { get; set; }

    /// <summary>
    /// Exit interview date.
    /// </summary>
    public DateOnly? ExitInterviewDate { get; set; }

    /// <summary>
    /// Exit interview notes/feedback.
    /// </summary>
    public string? ExitInterviewNotes { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual User? ApprovedByUser { get; set; }
}
