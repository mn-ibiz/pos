namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Result of OTP generation for loyalty points redemption.
/// </summary>
public class OtpGenerationResult
{
    /// <summary>
    /// Whether the OTP was generated and sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ID of the generated OTP record for tracking verification.
    /// </summary>
    public int? OtpId { get; set; }

    /// <summary>
    /// Masked phone number the OTP was sent to (e.g., "07XX XXX X78").
    /// </summary>
    public string? MaskedPhone { get; set; }

    /// <summary>
    /// When the OTP expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Time until expiry in seconds.
    /// </summary>
    public int? ExpiresInSeconds { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether a new OTP can be requested (cooldown respected).
    /// </summary>
    public bool CanResend { get; set; }

    /// <summary>
    /// Seconds remaining before a new OTP can be requested.
    /// </summary>
    public int? ResendCooldownSeconds { get; set; }

    /// <summary>
    /// Creates a successful OTP generation result.
    /// </summary>
    public static OtpGenerationResult Succeeded(int otpId, string maskedPhone, DateTime expiresAt)
    {
        return new OtpGenerationResult
        {
            Success = true,
            OtpId = otpId,
            MaskedPhone = maskedPhone,
            ExpiresAt = expiresAt,
            ExpiresInSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            CanResend = false,
            ResendCooldownSeconds = 60
        };
    }

    /// <summary>
    /// Creates a failed OTP generation result.
    /// </summary>
    public static OtpGenerationResult Failed(string errorMessage, string? errorCode = null)
    {
        return new OtpGenerationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Result of OTP verification for loyalty points redemption.
/// </summary>
public class OtpVerificationResult
{
    /// <summary>
    /// Whether the OTP was verified successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the OTP has expired.
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether the OTP is locked due to too many failed attempts.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Number of attempts remaining before lockout.
    /// </summary>
    public int RemainingAttempts { get; set; }

    /// <summary>
    /// Points amount authorized for redemption (only set on success).
    /// </summary>
    public decimal? AuthorizedPoints { get; set; }

    /// <summary>
    /// Error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful verification result.
    /// </summary>
    public static OtpVerificationResult Verified(decimal authorizedPoints)
    {
        return new OtpVerificationResult
        {
            Success = true,
            AuthorizedPoints = authorizedPoints
        };
    }

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static OtpVerificationResult Failed(
        string message,
        int remainingAttempts,
        bool isExpired = false,
        bool isLocked = false)
    {
        return new OtpVerificationResult
        {
            Success = false,
            ErrorMessage = message,
            RemainingAttempts = remainingAttempts,
            IsExpired = isExpired,
            IsLocked = isLocked
        };
    }
}

/// <summary>
/// Error codes for OTP operations.
/// </summary>
public static class OtpErrorCodes
{
    /// <summary>Loyalty member doesn't exist.</summary>
    public const string MemberNotFound = "MEMBER_NOT_FOUND";

    /// <summary>Member account is deactivated.</summary>
    public const string MemberInactive = "MEMBER_INACTIVE";

    /// <summary>OTP record not found.</summary>
    public const string InvalidOtp = "INVALID_OTP";

    /// <summary>OTP has expired.</summary>
    public const string Expired = "EXPIRED";

    /// <summary>Too many failed attempts.</summary>
    public const string Locked = "LOCKED";

    /// <summary>OTP was already used.</summary>
    public const string AlreadyVerified = "ALREADY_VERIFIED";

    /// <summary>Must wait before resending.</summary>
    public const string Cooldown = "COOLDOWN";

    /// <summary>Failed to generate OTP.</summary>
    public const string GenerationError = "GENERATION_ERROR";

    /// <summary>SMS sending failed.</summary>
    public const string SmsSendFailed = "SMS_SEND_FAILED";
}
