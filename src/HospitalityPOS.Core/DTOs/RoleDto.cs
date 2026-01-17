namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Data transfer object for creating and updating roles.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of permission IDs to assign to the role.
    /// </summary>
    public List<int> PermissionIds { get; set; } = [];
}
