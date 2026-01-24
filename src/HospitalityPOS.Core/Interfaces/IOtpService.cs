using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for OTP (One-Time Password) management for loyalty redemption verification.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate and send OTP for loyalty points redemption.
    /// Invalidates any existing pending OTPs for the member.
    /// </summary>
    /// <param name="loyaltyMemberId">Member requesting redemption.</param>
    /// <param name="pointsToRedeem">Amount of points to authorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with OTP ID for tracking verification.</returns>
    Task<OtpGenerationResult> GenerateRedemptionOtpAsync(
        int loyaltyMemberId,
        decimal pointsToRedeem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify an OTP code entered by the cashier.
    /// Increments attempt count on failure.
    /// </summary>
    /// <param name="otpId">The OTP record ID.</param>
    /// <param name="code">The 6-digit code entered.</param>
    /// <param name="userId">User performing verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result with authorized points if successful.</returns>
    Task<OtpVerificationResult> VerifyOtpAsync(
        int otpId,
        string code,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend OTP to customer. Respects 60-second cooldown.
    /// Generates new code and invalidates old one.
    /// </summary>
    /// <param name="otpId">The existing OTP record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New OTP generation result.</returns>
    Task<OtpGenerationResult> ResendOtpAsync(
        int otpId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if member has a pending (valid, unexpired, unlocked) OTP.
    /// </summary>
    /// <param name="loyaltyMemberId">Member to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending OTP if exists, null otherwise.</returns>
    Task<RedemptionOtp?> GetPendingOtpAsync(
        int loyaltyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate all pending OTPs for a member.
    /// Called when generating new OTP or cancelling redemption.
    /// </summary>
    /// <param name="loyaltyMemberId">Member whose OTPs to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePendingOtpsAsync(
        int loyaltyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark OTP as used for a specific receipt.
    /// Called after successful redemption transaction.
    /// </summary>
    /// <param name="otpId">The verified OTP ID.</param>
    /// <param name="receiptId">The receipt ID it was used for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkOtpUsedAsync(
        int otpId,
        int receiptId,
        CancellationToken cancellationToken = default);
}
