using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a one-time reward issued to a specific loyalty member.
/// </summary>
public class MemberReward : BaseEntity
{
    /// <summary>
    /// Gets or sets the loyalty member ID.
    /// </summary>
    public int LoyaltyMemberId { get; set; }

    /// <summary>
    /// Gets or sets the one-time reward template ID.
    /// </summary>
    public int OneTimeRewardId { get; set; }

    /// <summary>
    /// Gets or sets the unique redemption code for this reward.
    /// Format: RWD-YYYYMMDD-XXXXX
    /// </summary>
    public string RedemptionCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of this reward.
    /// </summary>
    public MemberRewardStatus Status { get; set; } = MemberRewardStatus.Active;

    /// <summary>
    /// Gets or sets the date/time when this reward was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date/time when this reward expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date/time when this reward was redeemed.
    /// </summary>
    public DateTime? RedeemedAt { get; set; }

    /// <summary>
    /// Gets or sets the receipt ID where this reward was redeemed.
    /// </summary>
    public int? RedeemedOnReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the discount amount applied when redeemed (KES).
    /// </summary>
    public decimal? RedeemedValue { get; set; }

    /// <summary>
    /// Gets or sets the points awarded when redeemed (for point rewards).
    /// </summary>
    public decimal? PointsAwarded { get; set; }

    /// <summary>
    /// Gets or sets the year this reward is for (e.g., 2024 for birthday 2024).
    /// Prevents duplicate issuance for the same year.
    /// </summary>
    public int RewardYear { get; set; }

    /// <summary>
    /// Gets or sets the event date this reward celebrates (e.g., actual birthday date).
    /// </summary>
    public DateOnly? EventDate { get; set; }

    /// <summary>
    /// Gets or sets whether SMS notification was sent.
    /// </summary>
    public bool SmsNotificationSent { get; set; }

    /// <summary>
    /// Gets or sets the date/time SMS notification was sent.
    /// </summary>
    public DateTime? SmsNotificationSentAt { get; set; }

    /// <summary>
    /// Gets or sets whether email notification was sent.
    /// </summary>
    public bool EmailNotificationSent { get; set; }

    /// <summary>
    /// Gets or sets the date/time email notification was sent.
    /// </summary>
    public DateTime? EmailNotificationSentAt { get; set; }

    /// <summary>
    /// Gets or sets additional notes about this reward.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the loyalty member who received this reward.
    /// </summary>
    public virtual LoyaltyMember LoyaltyMember { get; set; } = null!;

    /// <summary>
    /// Gets or sets the one-time reward template.
    /// </summary>
    public virtual OneTimeReward OneTimeReward { get; set; } = null!;

    /// <summary>
    /// Gets or sets the receipt where this reward was redeemed.
    /// </summary>
    public virtual Receipt? RedeemedOnReceipt { get; set; }
}
