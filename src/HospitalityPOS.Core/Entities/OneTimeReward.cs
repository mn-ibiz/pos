using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Defines a one-time reward template (e.g., birthday, anniversary, signup).
/// </summary>
public class OneTimeReward : BaseEntity
{
    /// <summary>
    /// Gets or sets the reward name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reward description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of one-time reward.
    /// </summary>
    public OneTimeRewardType RewardType { get; set; }

    /// <summary>
    /// Gets or sets how the reward value is applied.
    /// </summary>
    public RewardValueType ValueType { get; set; }

    /// <summary>
    /// Gets or sets the reward value.
    /// For FixedPoints: number of points.
    /// For PercentageDiscount: percentage (e.g., 10 for 10%).
    /// For FixedDiscount: amount in KES.
    /// For PointsMultiplier: multiplier (e.g., 2 for 2x).
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the minimum tier required to receive this reward.
    /// </summary>
    public MembershipTier? MinimumTier { get; set; }

    /// <summary>
    /// Gets or sets the number of days the reward is valid after issuance.
    /// </summary>
    public int ValidityDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum purchase amount required to redeem the reward (KES).
    /// </summary>
    public decimal? MinimumPurchaseAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum discount amount for percentage discounts (KES).
    /// </summary>
    public decimal? MaximumDiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the product ID for FreeItem rewards.
    /// </summary>
    public int? FreeItemProductId { get; set; }

    /// <summary>
    /// Gets or sets the SMS notification template.
    /// Placeholders: {Name}, {RewardName}, {Value}, {ExpiryDate}, {Code}
    /// </summary>
    public string? SmsTemplate { get; set; }

    /// <summary>
    /// Gets or sets the email notification template.
    /// </summary>
    public string? EmailTemplate { get; set; }

    /// <summary>
    /// Gets or sets whether to send SMS notification when reward is issued.
    /// </summary>
    public bool SendSmsNotification { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send email notification when reward is issued.
    /// </summary>
    public bool SendEmailNotification { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days before the event to issue the reward.
    /// For birthdays: days before birthday to issue (default 0 = on birthday).
    /// </summary>
    public int DaysBeforeToIssue { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of days the reward window extends after the event.
    /// For birthdays: how many days after birthday the reward remains valid.
    /// </summary>
    public int DaysAfterEventValid { get; set; } = 7;

    // Navigation properties

    /// <summary>
    /// Gets or sets the free item product for FreeItem rewards.
    /// </summary>
    public virtual Product? FreeItemProduct { get; set; }

    /// <summary>
    /// Gets or sets the collection of member rewards issued from this template.
    /// </summary>
    public virtual ICollection<MemberReward> MemberRewards { get; set; } = new List<MemberReward>();
}
