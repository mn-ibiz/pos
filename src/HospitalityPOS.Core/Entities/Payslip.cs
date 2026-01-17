using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an employee payslip for a payroll period.
/// </summary>
public class Payslip : BaseEntity
{
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public PayslipPaymentStatus PaymentStatus { get; set; } = PayslipPaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
    public virtual ICollection<PayslipDetail> PayslipDetails { get; set; } = new List<PayslipDetail>();
}
