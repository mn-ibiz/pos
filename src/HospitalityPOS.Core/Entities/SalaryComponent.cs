using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a salary component (earning or deduction) for payroll.
/// </summary>
public class SalaryComponent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComponentType ComponentType { get; set; }
    public bool IsFixed { get; set; } = true;
    public decimal? DefaultAmount { get; set; }
    public decimal? DefaultPercent { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsStatutory { get; set; } // PAYE, NHIF, NSSF
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual ICollection<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; } = new List<EmployeeSalaryComponent>();
    public virtual ICollection<PayslipDetail> PayslipDetails { get; set; } = new List<PayslipDetail>();
}
