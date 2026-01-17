using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for sending SMS notifications.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a welcome SMS to a newly enrolled loyalty member.
    /// </summary>
    /// <param name="phoneNumber">The phone number in 254XXXXXXXXX format.</param>
    /// <param name="memberName">The member's name (optional).</param>
    /// <param name="membershipNumber">The membership number.</param>
    /// <param name="pointsBalance">The starting points balance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SMS sending result.</returns>
    Task<SmsResult> SendWelcomeSmsAsync(
        string phoneNumber,
        string? memberName,
        string membershipNumber,
        decimal pointsBalance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic SMS message.
    /// </summary>
    /// <param name="phoneNumber">The phone number in 254XXXXXXXXX format.</param>
    /// <param name="message">The message content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SMS sending result.</returns>
    Task<SmsResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a points earned notification SMS.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="pointsEarned">Points earned in this transaction.</param>
    /// <param name="newBalance">New total points balance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SMS sending result.</returns>
    Task<SmsResult> SendPointsEarnedSmsAsync(
        string phoneNumber,
        decimal pointsEarned,
        decimal newBalance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a points redeemed notification SMS.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="pointsRedeemed">Points redeemed in this transaction.</param>
    /// <param name="amountSaved">Amount saved (KES).</param>
    /// <param name="remainingBalance">Remaining points balance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SMS sending result.</returns>
    Task<SmsResult> SendPointsRedeemedSmsAsync(
        string phoneNumber,
        decimal pointsRedeemed,
        decimal amountSaved,
        decimal remainingBalance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if SMS service is configured and available.
    /// </summary>
    /// <returns>True if SMS can be sent; otherwise, false.</returns>
    bool IsConfigured();
}
