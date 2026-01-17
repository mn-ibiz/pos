using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for handling permission override requests via PIN authentication.
/// </summary>
public interface IPermissionOverrideService
{
    /// <summary>
    /// Validates a PIN and authorizes an action if the PIN owner has the required permission.
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <param name="requiredPermission">The permission name required for the action.</param>
    /// <param name="actionDescription">A description of the action being authorized.</param>
    /// <param name="originalUserId">The ID of the user requesting the override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The override result indicating success or failure.</returns>
    Task<OverrideResult> ValidatePinAndAuthorizeAsync(
        string pin,
        string requiredPermission,
        string actionDescription,
        int originalUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user associated with a PIN without performing any authorization.
    /// Used to preview who would be authorizing.
    /// </summary>
    /// <param name="pin">The PIN to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetUserByPinAsync(string pin, CancellationToken cancellationToken = default);
}
