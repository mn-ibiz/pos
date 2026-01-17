using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an employee for payroll management (Supermarket feature).
/// </summary>
public class Employee : BaseEntity
{
    public int? UserId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public decimal BasicSalary { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Monthly;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? TaxId { get; set; } // KRA PIN
    public string? NssfNumber { get; set; }
    public string? NhifNumber { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; } = new List<EmployeeSalaryComponent>();
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
