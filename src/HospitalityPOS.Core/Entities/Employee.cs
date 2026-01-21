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

    /// <summary>
    /// Alternative name for HireDate, for compatibility.
    /// </summary>
    public DateTime? DateOfJoining => HireDate;

    /// <summary>
    /// Current employment status (Active, Terminated, etc.).
    /// </summary>
    public string EmploymentStatus { get; set; } = "Active";

    /// <summary>
    /// Total allowances amount.
    /// </summary>
    public decimal Allowances { get; set; }

    /// <summary>
    /// Department ID - FK to Department entity.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Legacy department name field for backward compatibility.
    /// Use DepartmentId for new implementations.
    /// </summary>
    public string? Department { get; set; }

    public string? Position { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public decimal BasicSalary { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Monthly;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranchCode { get; set; }
    public string? TaxId { get; set; } // KRA PIN
    public string? NssfNumber { get; set; }
    public string? NhifNumber { get; set; }

    /// <summary>
    /// HELB loan number if employee has student loan.
    /// </summary>
    public string? HelbNumber { get; set; }

    /// <summary>
    /// Whether employee has active HELB deductions.
    /// </summary>
    public bool HasHelbDeduction { get; set; }

    /// <summary>
    /// Path to employee photo image file.
    /// </summary>
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Path to thumbnail version of the photo for list views.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Emergency contact name.
    /// </summary>
    public string? EmergencyContactName { get; set; }

    /// <summary>
    /// Emergency contact phone number.
    /// </summary>
    public string? EmergencyContactPhone { get; set; }

    /// <summary>
    /// Emergency contact relationship (e.g., Spouse, Parent, Sibling).
    /// </summary>
    public string? EmergencyContactRelationship { get; set; }

    /// <summary>
    /// Gender for HR reporting.
    /// </summary>
    public Gender? Gender { get; set; }

    /// <summary>
    /// Marital status for HR records.
    /// </summary>
    public MaritalStatus? MaritalStatus { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Department? DepartmentEntity { get; set; }
    public virtual ICollection<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; } = new List<EmployeeSalaryComponent>();
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
    public virtual ICollection<EmployeeLoan> Loans { get; set; } = new List<EmployeeLoan>();
    public virtual ICollection<LeaveAllocation> LeaveAllocations { get; set; } = new List<LeaveAllocation>();
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<DisciplinaryDeduction> DisciplinaryDeductions { get; set; } = new List<DisciplinaryDeduction>();
}

/// <summary>
/// Gender enumeration for HR records.
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}

/// <summary>
/// Marital status enumeration.
/// </summary>
public enum MaritalStatus
{
    Single = 0,
    Married = 1,
    Divorced = 2,
    Widowed = 3,
    Separated = 4
}
