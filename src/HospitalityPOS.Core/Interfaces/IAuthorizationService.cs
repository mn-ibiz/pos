namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for checking user permissions.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    bool HasPermission(string permissionName);

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissionNames">The permission names to check.</param>
    /// <returns>True if the user has at least one of the permissions; otherwise, false.</returns>
    bool HasAnyPermission(params string[] permissionNames);

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    /// <param name="permissionNames">The permission names to check.</param>
    /// <returns>True if the user has all of the permissions; otherwise, false.</returns>
    bool HasAllPermissions(params string[] permissionNames);

    /// <summary>
    /// Gets all permission names for the current user.
    /// </summary>
    /// <returns>A collection of permission names.</returns>
    IReadOnlySet<string> GetCurrentUserPermissions();

    /// <summary>
    /// Refreshes the cached permissions for the current user.
    /// Call this when the user's roles or permissions change.
    /// </summary>
    void RefreshPermissions();

    /// <summary>
    /// Checks if the current user can apply a discount up to the specified percentage.
    /// </summary>
    /// <param name="discountPercent">The discount percentage to check.</param>
    /// <returns>True if the user can apply the discount; otherwise, false.</returns>
    bool CanApplyDiscount(decimal discountPercent);
}
