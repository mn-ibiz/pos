using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating, sending, and verifying one-time passwords for loyalty points redemption.
/// </summary>
public class OtpService : IOtpService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ISmsService _smsService;
    private readonly ILogger _logger;

    private const int OtpLength = 6;
    private const int OtpValidityMinutes = 5;
    private const int MaxAttempts = 3;
    private const int ResendCooldownSeconds = 60;

    public OtpService(
        IDbContextFactory<POSDbContext> contextFactory,
        ISmsService smsService,
        ILogger logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OtpGenerationResult> GenerateRedemptionOtpAsync(
        int loyaltyMemberId,
        decimal pointsToRedeem,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // Get member
            var member = await context.LoyaltyMembers
                .FirstOrDefaultAsync(m => m.Id == loyaltyMemberId && m.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (member == null)
                return OtpGenerationResult.Failed("Member not found", OtpErrorCodes.MemberNotFound);

            if (!member.IsActive)
                return OtpGenerationResult.Failed("Member account is inactive", OtpErrorCodes.MemberInactive);

            // Invalidate existing pending OTPs
            var pendingOtps = await context.RedemptionOtps
                .Where(o => o.LoyaltyMemberId == loyaltyMemberId && o.IsActive && !o.IsVerified)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var pending in pendingOtps)
            {
                pending.IsActive = false;
            }

            // Generate secure OTP code
            var code = GenerateSecureOtpCode();
            var expiresAt = DateTime.UtcNow.AddMinutes(OtpValidityMinutes);

            // Create OTP record
            var otp = new RedemptionOtp
            {
                LoyaltyMemberId = loyaltyMemberId,
                Code = code,
                PhoneNumber = member.PhoneNumber,
                ExpiresAt = expiresAt,
                MaxAttempts = MaxAttempts,
                AuthorizedPoints = pointsToRedeem,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.RedemptionOtps.Add(otp);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Send SMS (fire and forget with error handling)
            _ = SendOtpSmsAsync(member.PhoneNumber, code, member.Name, pointsToRedeem);

            var maskedPhone = MaskPhoneNumber(member.PhoneNumber);

            _logger.Information(
                "OTP generated for member {MemberId}, OTP ID: {OtpId}, expires at {ExpiresAt}",
                loyaltyMemberId, otp.Id, expiresAt);

            return OtpGenerationResult.Succeeded(otp.Id, maskedPhone, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating OTP for member {MemberId}", loyaltyMemberId);
            return OtpGenerationResult.Failed("Failed to generate verification code", OtpErrorCodes.GenerationError);
        }
    }

    public async Task<OtpVerificationResult> VerifyOtpAsync(
        int otpId,
        string code,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var otp = await context.RedemptionOtps
                .FirstOrDefaultAsync(o => o.Id == otpId, cancellationToken)
                .ConfigureAwait(false);

            if (otp == null)
                return OtpVerificationResult.Failed("Invalid verification request", 0);

            // Check expiry
            if (otp.IsExpired)
            {
                _logger.Warning("OTP {OtpId} has expired", otpId);
                return OtpVerificationResult.Failed(
                    "Verification code has expired. Please request a new code.",
                    0, isExpired: true);
            }

            // Check lockout
            if (otp.IsLocked)
            {
                _logger.Warning("OTP {OtpId} is locked", otpId);
                return OtpVerificationResult.Failed(
                    "Too many incorrect attempts. Please request a new code.",
                    0, isLocked: true);
            }

            // Check already verified
            if (otp.IsVerified)
            {
                _logger.Warning("OTP {OtpId} already verified", otpId);
                return OtpVerificationResult.Failed("This code has already been used", 0);
            }

            // Increment attempt count
            otp.AttemptCount++;

            // Verify code (case-insensitive)
            if (!string.Equals(otp.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.Warning(
                    "Invalid OTP attempt for {OtpId}, attempt {Attempt}/{Max}",
                    otpId, otp.AttemptCount, otp.MaxAttempts);

                return OtpVerificationResult.Failed(
                    $"Incorrect code. {otp.RemainingAttempts} attempts remaining.",
                    otp.RemainingAttempts);
            }

            // Success - mark as verified
            otp.IsVerified = true;
            otp.VerifiedAt = DateTime.UtcNow;
            otp.VerifiedByUserId = userId;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.Information(
                "OTP {OtpId} verified successfully by user {UserId}",
                otpId, userId);

            return OtpVerificationResult.Verified(otp.AuthorizedPoints);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error verifying OTP {OtpId}", otpId);
            return OtpVerificationResult.Failed("Verification failed. Please try again.", 0);
        }
    }

    public async Task<OtpGenerationResult> ResendOtpAsync(
        int otpId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var otp = await context.RedemptionOtps
            .FirstOrDefaultAsync(o => o.Id == otpId, cancellationToken)
            .ConfigureAwait(false);

        if (otp == null)
            return OtpGenerationResult.Failed("Invalid request", OtpErrorCodes.InvalidOtp);

        // Check cooldown
        var secondsSinceCreation = (DateTime.UtcNow - otp.CreatedAt).TotalSeconds;
        if (secondsSinceCreation < ResendCooldownSeconds)
        {
            var remaining = ResendCooldownSeconds - (int)secondsSinceCreation;
            return new OtpGenerationResult
            {
                Success = false,
                ErrorMessage = $"Please wait {remaining} seconds before requesting a new code",
                ErrorCode = OtpErrorCodes.Cooldown,
                CanResend = false,
                ResendCooldownSeconds = remaining
            };
        }

        // Generate new OTP
        return await GenerateRedemptionOtpAsync(
            otp.LoyaltyMemberId,
            otp.AuthorizedPoints,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RedemptionOtp?> GetPendingOtpAsync(
        int loyaltyMemberId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.RedemptionOtps
            .Where(o =>
                o.LoyaltyMemberId == loyaltyMemberId &&
                o.IsActive &&
                !o.IsVerified &&
                o.ExpiresAt > DateTime.UtcNow &&
                o.AttemptCount < o.MaxAttempts)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task InvalidatePendingOtpsAsync(
        int loyaltyMemberId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var pendingOtps = await context.RedemptionOtps
            .Where(o =>
                o.LoyaltyMemberId == loyaltyMemberId &&
                o.IsActive &&
                !o.IsVerified)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var otp in pendingOtps)
        {
            otp.IsActive = false;
        }

        if (pendingOtps.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.Debug("Invalidated {Count} pending OTPs for member {MemberId}",
                pendingOtps.Count, loyaltyMemberId);
        }
    }

    public async Task MarkOtpUsedAsync(
        int otpId,
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var otp = await context.RedemptionOtps
            .FirstOrDefaultAsync(o => o.Id == otpId, cancellationToken)
            .ConfigureAwait(false);

        if (otp != null && otp.IsVerified)
        {
            otp.ReceiptId = receiptId;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #region Private Methods

    private static string GenerateSecureOtpCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return number.ToString("D6"); // Pad to 6 digits
    }

    private static string MaskPhoneNumber(string phone)
    {
        // Convert 254712345678 to "07XX XXX X78"
        if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            return "****";

        var local = phone.StartsWith("254") ? "0" + phone[3..] : phone;
        if (local.Length >= 10)
            return $"{local[..2]}XX XXX X{local[^2..]}";
        return $"{local[..2]}XX XXX XX";
    }

    private async Task SendOtpSmsAsync(string phone, string code, string? name, decimal points)
    {
        try
        {
            var displayName = string.IsNullOrEmpty(name) ? "Customer" : name;
            var message = $"Hi {displayName}, your redemption code is {code}. " +
                          $"Valid for 5 minutes. Points: {points:N0}. " +
                          "Do not share this code.";

            await _smsService.SendSmsAsync(phone, message, CancellationToken.None).ConfigureAwait(false);
            _logger.Debug("OTP SMS sent to {Phone}", MaskPhoneNumber(phone));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to send OTP SMS to {Phone}", MaskPhoneNumber(phone));
            // Don't throw - OTP is valid even if SMS fails
        }
    }

    #endregion
}
