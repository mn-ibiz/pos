namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a referral.
/// </summary>
public enum ReferralStatus
{
    /// <summary>
    /// Referred but hasn't made qualifying purchase.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Qualifying purchase made, rewards issued.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Time limit exceeded without qualifying purchase.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Manually cancelled by admin.
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Unique referral code assigned to a loyalty member.
/// </summary>
public class ReferralCode : BaseEntity
{
    /// <summary>
    /// The loyalty member who owns this code.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The unique referral code (e.g., "JOHN2024", "MBR-A1B2C3").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Shareable URL for this referral code.
    /// </summary>
    public string? ShareableUrl { get; set; }

    /// <summary>
    /// Number of times this code has been used successfully.
    /// </summary>
    public int TimesUsed { get; set; }

    /// <summary>
    /// Total points earned from referrals using this code.
    /// </summary>
    public int TotalPointsEarned { get; set; }

    /// <summary>
    /// Optional expiry date for this code.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The member who owns this code.
    /// </summary>
    public virtual LoyaltyMember? Member { get; set; }

    /// <summary>
    /// Referrals made using this code.
    /// </summary>
    public virtual ICollection<Referral> Referrals { get; set; } = new List<Referral>();

    /// <summary>
    /// Checks if this code is currently valid for use.
    /// </summary>
    public bool IsValid => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
}

/// <summary>
/// Represents a referral relationship between two members.
/// </summary>
public class Referral : BaseEntity
{
    /// <summary>
    /// The member who made the referral (existing member).
    /// </summary>
    public int ReferrerId { get; set; }

    /// <summary>
    /// The member who was referred (new member).
    /// </summary>
    public int RefereeId { get; set; }

    /// <summary>
    /// The referral code used.
    /// </summary>
    public int ReferralCodeId { get; set; }

    /// <summary>
    /// Current status of the referral.
    /// </summary>
    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

    /// <summary>
    /// Bonus points awarded to the referrer.
    /// </summary>
    public int ReferrerBonusPoints { get; set; }

    /// <summary>
    /// Bonus points awarded to the referee.
    /// </summary>
    public int RefereeBonusPoints { get; set; }

    /// <summary>
    /// When the referral was made (signup date).
    /// </summary>
    public DateTime ReferredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the qualifying purchase was made.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The receipt ID of the qualifying purchase.
    /// </summary>
    public int? QualifyingReceiptId { get; set; }

    /// <summary>
    /// The amount of the qualifying purchase.
    /// </summary>
    public decimal? QualifyingAmount { get; set; }

    /// <summary>
    /// When this referral expires if not completed.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Reason if cancelled.
    /// </summary>
    public string? CancellationReason { get; set; }

    // Navigation properties

    /// <summary>
    /// The member who made the referral.
    /// </summary>
    public virtual LoyaltyMember? Referrer { get; set; }

    /// <summary>
    /// The member who was referred.
    /// </summary>
    public virtual LoyaltyMember? Referee { get; set; }

    /// <summary>
    /// The referral code used.
    /// </summary>
    public virtual ReferralCode? ReferralCode { get; set; }

    /// <summary>
    /// The qualifying receipt.
    /// </summary>
    public virtual Receipt? QualifyingReceipt { get; set; }

    /// <summary>
    /// Checks if this referral is still pending and not expired.
    /// </summary>
    public bool IsPendingAndNotExpired => Status == ReferralStatus.Pending && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Configuration settings for the referral program.
/// </summary>
public class ReferralConfiguration : BaseEntity
{
    /// <summary>
    /// The store this configuration applies to (null for global).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Points awarded to the referrer when referral completes.
    /// </summary>
    public int ReferrerBonusPoints { get; set; } = 500;

    /// <summary>
    /// Points awarded to the referee when referral completes.
    /// </summary>
    public int RefereeBonusPoints { get; set; } = 200;

    /// <summary>
    /// Minimum purchase amount to complete the referral (KES).
    /// </summary>
    public decimal MinPurchaseAmount { get; set; } = 500m;

    /// <summary>
    /// Number of days to complete the referral.
    /// </summary>
    public int ExpiryDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of successful referrals per member (null = unlimited).
    /// </summary>
    public int? MaxReferralsPerMember { get; set; }

    /// <summary>
    /// Whether to enable the referral leaderboard.
    /// </summary>
    public bool EnableLeaderboard { get; set; } = true;

    /// <summary>
    /// Whether the referee must be a new member (never enrolled before).
    /// </summary>
    public bool RequireNewMember { get; set; } = true;

    /// <summary>
    /// Whether the program is currently active.
    /// </summary>
    public bool IsProgramActive { get; set; } = true;

    /// <summary>
    /// SMS template for notifying referrer on completion.
    /// </summary>
    public string? ReferrerSmsTemplate { get; set; }

    /// <summary>
    /// SMS template for notifying referee on completion.
    /// </summary>
    public string? RefereeSmsTemplate { get; set; }

    /// <summary>
    /// Base URL for shareable referral links.
    /// </summary>
    public string? ShareableLinkBaseUrl { get; set; }

    // Navigation properties

    /// <summary>
    /// The store this configuration applies to.
    /// </summary>
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Milestone achievement for referral program.
/// </summary>
public class ReferralMilestone : BaseEntity
{
    /// <summary>
    /// Name of the milestone (e.g., "Referral Champion").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the milestone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of successful referrals required.
    /// </summary>
    public int ReferralCount { get; set; }

    /// <summary>
    /// Bonus points awarded when milestone is reached.
    /// </summary>
    public int BonusPoints { get; set; }

    /// <summary>
    /// Badge icon URL for this milestone.
    /// </summary>
    public string? BadgeIcon { get; set; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; set; }

    // Navigation properties

    /// <summary>
    /// Members who have achieved this milestone.
    /// </summary>
    public virtual ICollection<MemberReferralMilestone> MemberMilestones { get; set; } = new List<MemberReferralMilestone>();
}

/// <summary>
/// Tracks when a member achieves a referral milestone.
/// </summary>
public class MemberReferralMilestone : BaseEntity
{
    /// <summary>
    /// The loyalty member who achieved the milestone.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The milestone achieved.
    /// </summary>
    public int MilestoneId { get; set; }

    /// <summary>
    /// When the milestone was achieved.
    /// </summary>
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bonus points awarded.
    /// </summary>
    public int BonusPointsAwarded { get; set; }

    /// <summary>
    /// The referral count at time of achievement.
    /// </summary>
    public int ReferralCountAtAchievement { get; set; }

    // Navigation properties

    /// <summary>
    /// The member who achieved the milestone.
    /// </summary>
    public virtual LoyaltyMember? Member { get; set; }

    /// <summary>
    /// The milestone achieved.
    /// </summary>
    public virtual ReferralMilestone? Milestone { get; set; }
}
