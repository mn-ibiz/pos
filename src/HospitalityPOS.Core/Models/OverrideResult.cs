namespace HospitalityPOS.Core.Models;

/// <summary>
/// Result of a permission override attempt.
/// </summary>
public class OverrideResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the override was authorized.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who authorized the override.
    /// </summary>
    public int? AuthorizingUserId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user who authorized the override.
    /// </summary>
    public string? AuthorizingUserName { get; set; }

    /// <summary>
    /// Gets or sets the error message if authorization failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the permission that was being overridden.
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// Gets or sets the description of the action being authorized.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Creates a successful override result.
    /// </summary>
    public static OverrideResult Success(int authorizingUserId, string authorizingUserName, string permissionName, string? actionDescription = null) => new()
    {
        IsAuthorized = true,
        AuthorizingUserId = authorizingUserId,
        AuthorizingUserName = authorizingUserName,
        PermissionName = permissionName,
        ActionDescription = actionDescription
    };

    /// <summary>
    /// Creates a failed override result.
    /// </summary>
    public static OverrideResult Failure(string errorMessage) => new()
    {
        IsAuthorized = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a cancelled override result.
    /// </summary>
    public static OverrideResult Cancelled() => new()
    {
        IsAuthorized = false,
        ErrorMessage = null
    };
}
