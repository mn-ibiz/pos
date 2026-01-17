using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for checking entity ownership and authorizing access.
/// </summary>
public interface IOwnershipService
{
    /// <summary>
    /// Checks if the current user is the owner of the specified receipt.
    /// </summary>
    /// <param name="receiptId">The receipt ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the current user owns the receipt; otherwise false.</returns>
    Task<bool> IsReceiptOwnerAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user can modify the specified receipt.
    /// This includes ownership check and permission verification.
    /// </summary>
    /// <param name="receiptId">The receipt ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the current user can modify the receipt; otherwise false.</returns>
    Task<bool> CanModifyReceiptAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ownership of a receipt and returns detailed result.
    /// </summary>
    /// <param name="receiptId">The receipt ID to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ownership check result with details.</returns>
    Task<OwnershipCheckResult> ValidateReceiptOwnershipAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes access to a receipt with an override when the user is not the owner.
    /// This records the override in the audit log.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="authorizingUserId">The ID of the user authorizing the override.</param>
    /// <param name="authorizingUserName">The name of the user authorizing the override.</param>
    /// <param name="reason">The reason for the override.</param>
    /// <param name="actionDescription">Description of the action being performed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ownership check result with override information.</returns>
    Task<OwnershipCheckResult> AuthorizeWithOverrideAsync(
        int receiptId,
        int authorizingUserId,
        string authorizingUserName,
        string reason,
        string actionDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an ownership check failure for audit purposes.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="ownerId">The owner's ID.</param>
    /// <param name="attemptingUserId">The ID of the user attempting access.</param>
    /// <param name="actionDescription">Description of the action being attempted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogOwnershipDenialAsync(
        int receiptId,
        int ownerId,
        int attemptingUserId,
        string actionDescription,
        CancellationToken cancellationToken = default);
}
