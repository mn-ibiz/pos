// src/HospitalityPOS.Core/Entities/DepartmentEntity.cs
// Department entity for organizational structure.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a department in the organization.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Department code (e.g., "SALES", "ADMIN", "KITCHEN").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the department.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Manager/supervisor employee ID.
    /// </summary>
    public int? ManagerEmployeeId { get; set; }

    /// <summary>
    /// Parent department ID for hierarchical structure.
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Cost center code for accounting.
    /// </summary>
    public string? CostCenter { get; set; }

    /// <summary>
    /// Department location/branch.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Contact phone for department.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email for department.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Display order in UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this department is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Employee? Manager { get; set; }
    public virtual Department? ParentDepartment { get; set; }
    public virtual ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
