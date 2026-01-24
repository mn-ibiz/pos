namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Entity for storing OTP (One-Time Password) records for loyalty points redemption verification.
/// Customers must verify their identity via SMS OTP before redeeming loyalty points.
/// </summary>
public class RedemptionOtp : BaseEntity
{
    /// <summary>
    /// Reference to the loyalty member requesting redemption.
    /// </summary>
    public int LoyaltyMemberId { get; set; }

    /// <summary>
    /// The 6-digit OTP code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Phone number the OTP was sent to.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the OTP expires (5 minutes from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of verification attempts made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum allowed attempts (default: 3).
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Whether the OTP has been successfully verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// When the OTP was verified (null if not verified).
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Points amount this OTP authorizes for redemption.
    /// </summary>
    public decimal AuthorizedPoints { get; set; }

    /// <summary>
    /// Receipt/transaction this OTP was used for (null until used).
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// User who processed the verification.
    /// </summary>
    public int? VerifiedByUserId { get; set; }

    // Navigation properties
    public virtual LoyaltyMember LoyaltyMember { get; set; } = null!;
    public virtual Receipt? Receipt { get; set; }
    public virtual User? VerifiedByUser { get; set; }

    // Computed properties
    /// <summary>
    /// Returns true if the OTP has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Returns true if the OTP is locked due to too many failed attempts.
    /// </summary>
    public bool IsLocked => AttemptCount >= MaxAttempts;

    /// <summary>
    /// Returns true if the OTP can still be verified.
    /// </summary>
    public bool CanVerify => !IsExpired && !IsLocked && !IsVerified;

    /// <summary>
    /// Returns the number of remaining verification attempts.
    /// </summary>
    public int RemainingAttempts => Math.Max(0, MaxAttempts - AttemptCount);
}
