using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a payslip (earning or deduction).
/// </summary>
public class PayslipDetail : BaseEntity
{
    public int PayslipId { get; set; }
    public int SalaryComponentId { get; set; }
    public ComponentType ComponentType { get; set; }
    public decimal Amount { get; set; }

    // Navigation properties
    public virtual Payslip Payslip { get; set; } = null!;
    public virtual SalaryComponent SalaryComponent { get; set; } = null!;
}
