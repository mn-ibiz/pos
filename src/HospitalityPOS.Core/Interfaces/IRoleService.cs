using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for role and permission management.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Gets all roles in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all roles.</returns>
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its ID with permissions loaded.
    /// </summary>
    /// <param name="id">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found; otherwise null.</returns>
    Task<Role?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its name.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found; otherwise null.</returns>
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role with the specified permissions.
    /// </summary>
    /// <param name="dto">The role data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role.</returns>
    Task<Role> CreateRoleAsync(RoleDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="id">The role ID to update.</param>
    /// <param name="dto">The updated role data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful; false if role not found or is system role.</returns>
    Task<bool> UpdateRoleAsync(int id, RoleDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing role with a new name.
    /// </summary>
    /// <param name="sourceRoleId">The ID of the role to clone.</param>
    /// <param name="newName">The name for the cloned role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cloned role.</returns>
    Task<Role> CloneRoleAsync(int sourceRoleId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role. System roles cannot be deleted.
    /// </summary>
    /// <param name="id">The role ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found, is system role, or has assigned users.</returns>
    Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all permissions.</returns>
    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions grouped by category.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of category to permissions.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<Permission>>> GetPermissionsByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns permissions to a role, replacing any existing assignments.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="permissionIds">The permission IDs to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if role not found.</returns>
    Task<bool> AssignPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is unique.
    /// </summary>
    /// <param name="name">The role name to check.</param>
    /// <param name="excludeRoleId">Optional role ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the name is unique; otherwise false.</returns>
    Task<bool> IsRoleNameUniqueAsync(string name, int? excludeRoleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role can be deleted (not system role and no assigned users).
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role can be deleted; otherwise false.</returns>
    Task<bool> CanDeleteRoleAsync(int roleId, CancellationToken cancellationToken = default);
}
