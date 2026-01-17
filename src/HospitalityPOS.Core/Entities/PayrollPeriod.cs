using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a payroll period for salary processing.
/// </summary>
public class PayrollPeriod : BaseEntity
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PayDate { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public int? ProcessedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual User? ProcessedByUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
