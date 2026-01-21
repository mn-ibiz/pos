// src/HospitalityPOS.Core/Entities/DisciplinaryEntities.cs
// Disciplinary deduction entities for fines/penalties.
// Compliant with Kenya Employment Act 2007 Section 19.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of deduction reason per Kenya Employment Act Section 19.
/// </summary>
public enum DeductionReasonType
{
    /// <summary>
    /// Damage to employer's property due to wilful default - Section 19(1)(b).
    /// </summary>
    DamageToProperty = 0,

    /// <summary>
    /// Absence without leave or lawful cause - Section 19(1)(c).
    /// Capped at 1 day's wages per day absent.
    /// </summary>
    AbsenceWithoutLeave = 1,

    /// <summary>
    /// Cash shortage due to negligence or dishonesty - Section 19(1)(d).
    /// Only for employees entrusted with handling money.
    /// </summary>
    CashShortage = 2,

    /// <summary>
    /// Recovery of overpayment/excess wages - Section 19(1)(e).
    /// </summary>
    Overpayment = 3,

    /// <summary>
    /// Other deduction with employee written consent.
    /// </summary>
    Other = 4
}

/// <summary>
/// Status of a disciplinary deduction.
/// </summary>
public enum DisciplinaryDeductionStatus
{
    Pending = 0,            // Awaiting approval
    Approved = 1,           // Approved for deduction
    Rejected = 2,           // Rejected
    Deducted = 3,           // Applied to payroll
    Cancelled = 4,          // Cancelled
    Appealed = 5,           // Under appeal
    AppealUpheld = 6,       // Appeal successful, deduction cancelled
    AppealRejected = 7      // Appeal rejected, proceed with deduction
}

/// <summary>
/// Disciplinary deduction (fine/penalty) record.
/// Compliant with Kenya Employment Act Section 19.
/// </summary>
public class DisciplinaryDeduction : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Auto-generated reference number.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of deduction reason.
    /// </summary>
    public DeductionReasonType ReasonType { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public DisciplinaryDeductionStatus Status { get; set; } = DisciplinaryDeductionStatus.Pending;

    /// <summary>
    /// Date of the incident.
    /// </summary>
    public DateOnly IncidentDate { get; set; }

    /// <summary>
    /// Description of the incident.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount to be deducted.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Number of days absent (for absence deductions).
    /// </summary>
    public int? DaysAbsent { get; set; }

    /// <summary>
    /// Daily wage rate used for calculation.
    /// </summary>
    public decimal? DailyWageRate { get; set; }

    /// <summary>
    /// Path to evidence documentation.
    /// </summary>
    public string? EvidenceDocumentPath { get; set; }

    /// <summary>
    /// Actual loss/damage amount.
    /// </summary>
    public decimal? ActualLossAmount { get; set; }

    /// <summary>
    /// Whether employee has acknowledged the deduction.
    /// </summary>
    public bool EmployeeAcknowledged { get; set; }

    /// <summary>
    /// Date/time of acknowledgment.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Employee's response/explanation.
    /// </summary>
    public string? EmployeeResponse { get; set; }

    /// <summary>
    /// User who approved the deduction.
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

    /// <summary>
    /// Payslip ID where deduction was applied.
    /// </summary>
    public int? DeductedInPayslipId { get; set; }

    /// <summary>
    /// Date deduction was applied.
    /// </summary>
    public DateOnly? DeductionDate { get; set; }

    /// <summary>
    /// Whether employee has appealed.
    /// </summary>
    public bool IsAppealed { get; set; }

    /// <summary>
    /// Reason for appeal.
    /// </summary>
    public string? AppealReason { get; set; }

    /// <summary>
    /// Date/time of appeal.
    /// </summary>
    public DateTime? AppealedAt { get; set; }

    /// <summary>
    /// User who reviewed the appeal.
    /// </summary>
    public int? AppealReviewedByUserId { get; set; }

    /// <summary>
    /// Appeal decision details.
    /// </summary>
    public string? AppealDecision { get; set; }

    /// <summary>
    /// Date/time appeal was decided.
    /// </summary>
    public DateTime? AppealDecidedAt { get; set; }

    /// <summary>
    /// Witness employee ID (if any).
    /// </summary>
    public int? WitnessEmployeeId { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Employee? WitnessEmployee { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual User? AppealReviewedByUser { get; set; }
    public virtual Payslip? DeductedInPayslip { get; set; }
}
