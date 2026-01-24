using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Badge category types.
/// </summary>
public enum BadgeCategory
{
    Visits = 1,
    Spending = 2,
    Products = 3,
    Social = 4,
    Loyalty = 5,
    Seasonal = 6,
    Special = 7
}

/// <summary>
/// How a badge is triggered/awarded.
/// </summary>
public enum BadgeTriggerType
{
    Automatic = 1,      // System awards based on criteria
    Manual = 2,         // Staff awards manually
    Scan = 3,           // QR code scan at location
    Event = 4,          // Special event participation
    Referral = 5,       // Referral-based achievement
    Purchase = 6,       // Specific purchase trigger
    Streak = 7,         // Streak milestone achievement
    Challenge = 8       // Challenge completion
}

/// <summary>
/// Badge rarity levels for display/gamification.
/// </summary>
public enum BadgeRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

/// <summary>
/// Challenge period types.
/// </summary>
public enum ChallengePeriod
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Custom = 5
}

/// <summary>
/// Challenge status.
/// </summary>
public enum ChallengeStatus
{
    Active = 1,
    Completed = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5
}

/// <summary>
/// Streak type for tracking consecutive activities.
/// </summary>
public enum StreakType
{
    DailyVisit = 1,
    WeeklyVisit = 2,
    ConsecutivePurchase = 3,
    WeekendVisit = 4,
    MorningVisit = 5,
    LunchVisit = 6,
    DinnerVisit = 7
}

/// <summary>
/// Challenge goal type.
/// </summary>
public enum ChallengeGoalType
{
    VisitCount = 1,
    SpendAmount = 2,
    ProductCount = 3,
    SpecificProduct = 4,
    CategoryPurchase = 5,
    PointsEarned = 6,
    ReferralCount = 7,
    ReviewCount = 8,
    CheckInCount = 9
}

#endregion

#region Badge Entities

/// <summary>
/// Badge definition - template for achievements members can earn.
/// </summary>
public class Badge : BaseEntity
{
    /// <summary>
    /// Badge name (e.g., "First Visit", "Big Spender").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Badge description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Badge category.
    /// </summary>
    public BadgeCategory Category { get; set; }

    /// <summary>
    /// How the badge is triggered.
    /// </summary>
    public BadgeTriggerType TriggerType { get; set; }

    /// <summary>
    /// Badge rarity level.
    /// </summary>
    public BadgeRarity Rarity { get; set; } = BadgeRarity.Common;

    /// <summary>
    /// URL to badge icon/image.
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Badge color for display.
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    /// <summary>
    /// Points awarded when badge is earned.
    /// </summary>
    public int PointsAwarded { get; set; }

    /// <summary>
    /// Whether this badge is secret (hidden until earned).
    /// </summary>
    public bool IsSecret { get; set; }

    /// <summary>
    /// Whether badge can be earned multiple times.
    /// </summary>
    public bool IsRepeatable { get; set; }

    /// <summary>
    /// Maximum times badge can be earned (0 = unlimited if repeatable).
    /// </summary>
    public int MaxEarnings { get; set; }

    /// <summary>
    /// Criteria JSON for automatic badge awards.
    /// </summary>
    [MaxLength(2000)]
    public string? CriteriaJson { get; set; }

    /// <summary>
    /// Required threshold value (visits, spend, etc.).
    /// </summary>
    public decimal? ThresholdValue { get; set; }

    /// <summary>
    /// Specific product ID if product-based badge.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Specific category ID if category-based badge.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Start date for seasonal/limited badges.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for seasonal/limited badges.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Display order for badge listings.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Store ID for store-specific badges (null = all stores).
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Category? ProductCategory { get; set; }
    public virtual Store? Store { get; set; }
    public virtual ICollection<MemberBadge> MemberBadges { get; set; } = new List<MemberBadge>();
}

/// <summary>
/// Member's earned badge instance.
/// </summary>
public class MemberBadge : BaseEntity
{
    /// <summary>
    /// Member who earned the badge.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Badge that was earned.
    /// </summary>
    public int BadgeId { get; set; }

    /// <summary>
    /// When the badge was earned.
    /// </summary>
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How many times earned (for repeatable badges).
    /// </summary>
    public int TimesEarned { get; set; } = 1;

    /// <summary>
    /// Points awarded for this earning.
    /// </summary>
    public int PointsAwarded { get; set; }

    /// <summary>
    /// Order ID that triggered the badge (if applicable).
    /// </summary>
    public int? TriggeredByOrderId { get; set; }

    /// <summary>
    /// Store where badge was earned.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether member has viewed this badge.
    /// </summary>
    public bool IsViewed { get; set; }

    /// <summary>
    /// When member viewed the badge.
    /// </summary>
    public DateTime? ViewedAt { get; set; }

    /// <summary>
    /// Whether badge is pinned/featured on profile.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Optional notes about how badge was earned.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual LoyaltyMember Member { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;
    public virtual Order? TriggeredByOrder { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Challenge Entities

/// <summary>
/// Challenge definition - time-bound goal for members.
/// </summary>
public class Challenge : BaseEntity
{
    /// <summary>
    /// Challenge name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Challenge description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Challenge period type.
    /// </summary>
    public ChallengePeriod Period { get; set; }

    /// <summary>
    /// Goal type for this challenge.
    /// </summary>
    public ChallengeGoalType GoalType { get; set; }

    /// <summary>
    /// Target value to achieve (visits, spend amount, etc.).
    /// </summary>
    public decimal TargetValue { get; set; }

    /// <summary>
    /// Points awarded on completion.
    /// </summary>
    public int RewardPoints { get; set; }

    /// <summary>
    /// Badge awarded on completion (optional).
    /// </summary>
    public int? RewardBadgeId { get; set; }

    /// <summary>
    /// Bonus multiplier applied during challenge (e.g., 2x points).
    /// </summary>
    public decimal? BonusMultiplier { get; set; }

    /// <summary>
    /// URL to challenge icon/image.
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Challenge color for display.
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    /// <summary>
    /// Start date of the challenge.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the challenge.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether challenge is recurring (auto-creates new instance).
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Specific product ID if product-based challenge.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Specific category ID if category-based challenge.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Minimum tier required to participate.
    /// </summary>
    public LoyaltyTier? MinimumTier { get; set; }

    /// <summary>
    /// Maximum participants (0 = unlimited).
    /// </summary>
    public int MaxParticipants { get; set; }

    /// <summary>
    /// Whether to show leaderboard for this challenge.
    /// </summary>
    public bool ShowLeaderboard { get; set; } = true;

    /// <summary>
    /// Store ID for store-specific challenges (null = all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Display order for challenge listings.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Badge? RewardBadge { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Category? ProductCategory { get; set; }
    public virtual Store? Store { get; set; }
    public virtual ICollection<MemberChallenge> MemberChallenges { get; set; } = new List<MemberChallenge>();
}

/// <summary>
/// Member's challenge progress and completion.
/// </summary>
public class MemberChallenge : BaseEntity
{
    /// <summary>
    /// Member participating in challenge.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Challenge being attempted.
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// Current progress toward goal.
    /// </summary>
    public decimal CurrentProgress { get; set; }

    /// <summary>
    /// Challenge status.
    /// </summary>
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;

    /// <summary>
    /// When member joined the challenge.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When challenge was completed (if completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Points awarded upon completion.
    /// </summary>
    public int PointsAwarded { get; set; }

    /// <summary>
    /// Badge awarded upon completion (if any).
    /// </summary>
    public int? AwardedBadgeId { get; set; }

    /// <summary>
    /// Last progress update time.
    /// </summary>
    public DateTime? LastProgressAt { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => Challenge != null && Challenge.TargetValue > 0
        ? Math.Min(100, (CurrentProgress / Challenge.TargetValue) * 100)
        : 0;

    // Navigation properties
    public virtual LoyaltyMember Member { get; set; } = null!;
    public virtual Challenge Challenge { get; set; } = null!;
    public virtual MemberBadge? AwardedBadge { get; set; }
}

#endregion

#region Streak Entities

/// <summary>
/// Member's streak tracking for consecutive activities.
/// </summary>
public class MemberStreak : BaseEntity
{
    /// <summary>
    /// Member with the streak.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Type of streak being tracked.
    /// </summary>
    public StreakType StreakType { get; set; }

    /// <summary>
    /// Current streak count.
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Longest streak ever achieved.
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// When streak was started.
    /// </summary>
    public DateTime StreakStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity that continued the streak.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total times streak has been broken and restarted.
    /// </summary>
    public int TimesReset { get; set; }

    /// <summary>
    /// Deadline for next activity to maintain streak.
    /// </summary>
    public DateTime? NextActivityDeadline { get; set; }

    /// <summary>
    /// Whether streak is currently at risk (near deadline).
    /// </summary>
    public bool IsAtRisk { get; set; }

    /// <summary>
    /// Store ID for store-specific streaks (null = any store).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether streak is currently frozen (grace period).
    /// </summary>
    public bool IsFrozen { get; set; }

    /// <summary>
    /// When freeze expires.
    /// </summary>
    public DateTime? FreezeExpiresAt { get; set; }

    /// <summary>
    /// Number of freeze tokens remaining.
    /// </summary>
    public int FreezeTokensRemaining { get; set; }

    // Navigation properties
    public virtual LoyaltyMember Member { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual ICollection<StreakMilestone> Milestones { get; set; } = new List<StreakMilestone>();
}

/// <summary>
/// Streak milestone definition - rewards at specific streak counts.
/// </summary>
public class StreakMilestoneDefinition : BaseEntity
{
    /// <summary>
    /// Type of streak this milestone applies to.
    /// </summary>
    public StreakType StreakType { get; set; }

    /// <summary>
    /// Streak count required for this milestone.
    /// </summary>
    public int StreakCount { get; set; }

    /// <summary>
    /// Milestone name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Milestone description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Points awarded at this milestone.
    /// </summary>
    public int RewardPoints { get; set; }

    /// <summary>
    /// Badge awarded at this milestone.
    /// </summary>
    public int? RewardBadgeId { get; set; }

    /// <summary>
    /// Bonus freeze tokens awarded.
    /// </summary>
    public int FreezeTokensAwarded { get; set; }

    /// <summary>
    /// URL to milestone icon.
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Badge? RewardBadge { get; set; }
}

/// <summary>
/// Record of streak milestones achieved by a member.
/// </summary>
public class StreakMilestone : BaseEntity
{
    /// <summary>
    /// Member who achieved the milestone.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Streak this milestone was achieved on.
    /// </summary>
    public int MemberStreakId { get; set; }

    /// <summary>
    /// Milestone definition achieved.
    /// </summary>
    public int MilestoneDefinitionId { get; set; }

    /// <summary>
    /// Streak count when milestone was achieved.
    /// </summary>
    public int AchievedAtStreak { get; set; }

    /// <summary>
    /// When milestone was achieved.
    /// </summary>
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Points awarded.
    /// </summary>
    public int PointsAwarded { get; set; }

    /// <summary>
    /// Badge awarded (if any).
    /// </summary>
    public int? AwardedBadgeId { get; set; }

    /// <summary>
    /// Freeze tokens awarded.
    /// </summary>
    public int FreezeTokensAwarded { get; set; }

    // Navigation properties
    public virtual LoyaltyMember Member { get; set; } = null!;
    public virtual MemberStreak MemberStreak { get; set; } = null!;
    public virtual StreakMilestoneDefinition MilestoneDefinition { get; set; } = null!;
    public virtual MemberBadge? AwardedBadge { get; set; }
}

#endregion

#region Configuration

/// <summary>
/// Global gamification configuration settings.
/// </summary>
public class GamificationConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID for store-specific config (null = global).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether gamification is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether badges are enabled.
    /// </summary>
    public bool BadgesEnabled { get; set; } = true;

    /// <summary>
    /// Whether challenges are enabled.
    /// </summary>
    public bool ChallengesEnabled { get; set; } = true;

    /// <summary>
    /// Whether streaks are enabled.
    /// </summary>
    public bool StreaksEnabled { get; set; } = true;

    /// <summary>
    /// Default freeze tokens for new members.
    /// </summary>
    public int DefaultFreezeTokens { get; set; } = 3;

    /// <summary>
    /// Hours before streak deadline to show "at risk" warning.
    /// </summary>
    public int StreakAtRiskHours { get; set; } = 12;

    /// <summary>
    /// Whether to show badges on receipts.
    /// </summary>
    public bool ShowBadgesOnReceipt { get; set; } = true;

    /// <summary>
    /// Maximum badges to show on receipt.
    /// </summary>
    public int MaxBadgesOnReceipt { get; set; } = 3;

    /// <summary>
    /// Whether to notify on badge earned.
    /// </summary>
    public bool NotifyOnBadgeEarned { get; set; } = true;

    /// <summary>
    /// Whether to notify on challenge progress.
    /// </summary>
    public bool NotifyOnChallengeProgress { get; set; } = true;

    /// <summary>
    /// Progress notification threshold (e.g., notify at 50%, 75%).
    /// </summary>
    [MaxLength(100)]
    public string? ProgressNotificationThresholds { get; set; } = "50,75,90";

    /// <summary>
    /// Whether to notify on streak at risk.
    /// </summary>
    public bool NotifyOnStreakAtRisk { get; set; } = true;

    /// <summary>
    /// Whether to auto-enroll members in challenges.
    /// </summary>
    public bool AutoEnrollInChallenges { get; set; } = true;

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion
