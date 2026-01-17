namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Roles and Permissions.
/// </summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
