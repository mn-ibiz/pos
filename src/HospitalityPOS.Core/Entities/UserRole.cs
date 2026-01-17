namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Users and Roles.
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
