namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents employee-specific salary component configuration.
/// </summary>
public class EmployeeSalaryComponent : BaseEntity
{
    public int EmployeeId { get; set; }
    public int SalaryComponentId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual SalaryComponent SalaryComponent { get; set; } = null!;
}
