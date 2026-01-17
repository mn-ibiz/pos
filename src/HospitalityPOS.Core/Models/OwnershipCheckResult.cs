namespace HospitalityPOS.Core.Models;

/// <summary>
/// Result of an ownership check operation.
/// </summary>
public class OwnershipCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the ownership check passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the reason if the check failed.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the ID of the owner.
    /// </summary>
    public int? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the owner.
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether access was granted via override.
    /// </summary>
    public bool WasOverridden { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who authorized the override.
    /// </summary>
    public int? OverrideAuthorizingUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who authorized the override.
    /// </summary>
    public string? OverrideAuthorizingUserName { get; set; }

    /// <summary>
    /// Creates a successful ownership check result.
    /// </summary>
    public static OwnershipCheckResult Valid() => new()
    {
        IsValid = true
    };

    /// <summary>
    /// Creates a successful ownership check result with override information.
    /// </summary>
    public static OwnershipCheckResult ValidWithOverride(int authorizingUserId, string authorizingUserName) => new()
    {
        IsValid = true,
        WasOverridden = true,
        OverrideAuthorizingUserId = authorizingUserId,
        OverrideAuthorizingUserName = authorizingUserName
    };

    /// <summary>
    /// Creates a failed ownership check result.
    /// </summary>
    public static OwnershipCheckResult Invalid(int ownerId, string ownerName, string reason = "Not Authorized - Owner Only") => new()
    {
        IsValid = false,
        OwnerId = ownerId,
        OwnerName = ownerName,
        Reason = reason
    };

    /// <summary>
    /// Creates a failed ownership check result for entity not found.
    /// </summary>
    public static OwnershipCheckResult NotFound(string entityType) => new()
    {
        IsValid = false,
        Reason = $"{entityType} not found"
    };
}
