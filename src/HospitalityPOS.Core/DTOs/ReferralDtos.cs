using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO representing a member's referral code.
/// </summary>
public class ReferralCodeDto
{
    /// <summary>
    /// The referral code ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The member ID who owns this code.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The member's name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// The unique referral code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Shareable URL for this code.
    /// </summary>
    public string? ShareableUrl { get; set; }

    /// <summary>
    /// Number of times used successfully.
    /// </summary>
    public int TimesUsed { get; set; }

    /// <summary>
    /// Total points earned from this code.
    /// </summary>
    public int TotalPointsEarned { get; set; }

    /// <summary>
    /// When the code was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the code expires (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the code is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the code is currently valid for use.
    /// </summary>
    public bool IsValid => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
}

/// <summary>
/// DTO representing a referral relationship.
/// </summary>
public class ReferralDto
{
    /// <summary>
    /// The referral ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The referrer's member ID.
    /// </summary>
    public int ReferrerId { get; set; }

    /// <summary>
    /// The referrer's name.
    /// </summary>
    public string? ReferrerName { get; set; }

    /// <summary>
    /// The referrer's phone number.
    /// </summary>
    public string? ReferrerPhone { get; set; }

    /// <summary>
    /// The referee's member ID.
    /// </summary>
    public int RefereeId { get; set; }

    /// <summary>
    /// The referee's name.
    /// </summary>
    public string? RefereeName { get; set; }

    /// <summary>
    /// The referee's phone number.
    /// </summary>
    public string? RefereePhone { get; set; }

    /// <summary>
    /// The referral code used.
    /// </summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public ReferralStatus Status { get; set; }

    /// <summary>
    /// Status display name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Bonus points awarded to referrer.
    /// </summary>
    public int ReferrerBonusPoints { get; set; }

    /// <summary>
    /// Bonus points awarded to referee.
    /// </summary>
    public int RefereeBonusPoints { get; set; }

    /// <summary>
    /// When the referral was made.
    /// </summary>
    public DateTime ReferredAt { get; set; }

    /// <summary>
    /// When the referral was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the referral expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Amount of the qualifying purchase.
    /// </summary>
    public decimal? QualifyingAmount { get; set; }

    /// <summary>
    /// Days until expiry (for pending referrals).
    /// </summary>
    public int DaysUntilExpiry => Status == ReferralStatus.Pending
        ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalDays)
        : 0;
}

/// <summary>
/// Result of validating a referral code.
/// </summary>
public class ReferralCodeValidation
{
    /// <summary>
    /// Whether the code is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if invalid.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The referral code if valid.
    /// </summary>
    public ReferralCodeDto? ReferralCode { get; set; }

    /// <summary>
    /// The referrer's info if valid.
    /// </summary>
    public string? ReferrerName { get; set; }

    /// <summary>
    /// Points the new member will receive.
    /// </summary>
    public int BonusPointsForNewMember { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ReferralCodeValidation Valid(ReferralCodeDto code, string referrerName, int bonusPoints) => new()
    {
        IsValid = true,
        ReferralCode = code,
        ReferrerName = referrerName,
        BonusPointsForNewMember = bonusPoints
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ReferralCodeValidation Invalid(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of completing a referral.
/// </summary>
public class ReferralCompletionResult
{
    /// <summary>
    /// Whether the referral was completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error or success message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The completed referral.
    /// </summary>
    public ReferralDto? Referral { get; set; }

    /// <summary>
    /// Points awarded to the referrer.
    /// </summary>
    public int ReferrerPointsAwarded { get; set; }

    /// <summary>
    /// Points awarded to the referee.
    /// </summary>
    public int RefereePointsAwarded { get; set; }

    /// <summary>
    /// Milestone achieved by referrer (if any).
    /// </summary>
    public MilestoneCheckResult? MilestoneResult { get; set; }

    /// <summary>
    /// Whether SMS was sent to referrer.
    /// </summary>
    public bool ReferrerNotified { get; set; }

    /// <summary>
    /// Whether SMS was sent to referee.
    /// </summary>
    public bool RefereeNotified { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ReferralCompletionResult Completed(
        ReferralDto referral,
        int referrerPoints,
        int refereePoints,
        MilestoneCheckResult? milestone = null) => new()
    {
        Success = true,
        Message = "Referral completed successfully!",
        Referral = referral,
        ReferrerPointsAwarded = referrerPoints,
        RefereePointsAwarded = refereePoints,
        MilestoneResult = milestone
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static ReferralCompletionResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };

    /// <summary>
    /// Creates a result indicating no pending referral.
    /// </summary>
    public static ReferralCompletionResult NoPendingReferral() => new()
    {
        Success = false,
        Message = "No pending referral found for this member."
    };
}

/// <summary>
/// Result of processing a referral signup.
/// </summary>
public class ReferralSignupResult
{
    /// <summary>
    /// Whether the signup was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The created referral.
    /// </summary>
    public ReferralDto? Referral { get; set; }

    /// <summary>
    /// Welcome bonus points for the new member (separate from referral bonus).
    /// </summary>
    public int WelcomeBonusPoints { get; set; }

    /// <summary>
    /// Days to complete the referral.
    /// </summary>
    public int DaysToComplete { get; set; }

    /// <summary>
    /// Minimum purchase to complete the referral.
    /// </summary>
    public decimal MinPurchaseAmount { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ReferralSignupResult Successful(ReferralDto referral, int daysToComplete, decimal minPurchase) => new()
    {
        Success = true,
        Referral = referral,
        DaysToComplete = daysToComplete,
        MinPurchaseAmount = minPurchase
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static ReferralSignupResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Statistics for a member's referral activity.
/// </summary>
public class ReferralStats
{
    /// <summary>
    /// Total referrals made.
    /// </summary>
    public int TotalReferrals { get; set; }

    /// <summary>
    /// Successful (completed) referrals.
    /// </summary>
    public int SuccessfulReferrals { get; set; }

    /// <summary>
    /// Pending referrals.
    /// </summary>
    public int PendingReferrals { get; set; }

    /// <summary>
    /// Expired referrals.
    /// </summary>
    public int ExpiredReferrals { get; set; }

    /// <summary>
    /// Total points earned from referrals.
    /// </summary>
    public int TotalPointsEarned { get; set; }

    /// <summary>
    /// Conversion rate (successful / total).
    /// </summary>
    public decimal ConversionRate => TotalReferrals > 0
        ? (decimal)SuccessfulReferrals / TotalReferrals * 100
        : 0;

    /// <summary>
    /// Current leaderboard rank.
    /// </summary>
    public int? LeaderboardRank { get; set; }

    /// <summary>
    /// Number of referrals to next milestone.
    /// </summary>
    public int? ReferralsToNextMilestone { get; set; }

    /// <summary>
    /// Next milestone name.
    /// </summary>
    public string? NextMilestoneName { get; set; }
}

/// <summary>
/// Result of checking for milestone achievements.
/// </summary>
public class MilestoneCheckResult
{
    /// <summary>
    /// Whether a new milestone was achieved.
    /// </summary>
    public bool MilestoneAchieved { get; set; }

    /// <summary>
    /// The milestone that was achieved.
    /// </summary>
    public ReferralMilestoneDto? Milestone { get; set; }

    /// <summary>
    /// Bonus points awarded for the milestone.
    /// </summary>
    public int BonusPointsAwarded { get; set; }

    /// <summary>
    /// Total referral count at time of achievement.
    /// </summary>
    public int TotalReferrals { get; set; }

    /// <summary>
    /// Creates a result when milestone achieved.
    /// </summary>
    public static MilestoneCheckResult Achieved(ReferralMilestoneDto milestone, int bonusPoints, int totalReferrals) => new()
    {
        MilestoneAchieved = true,
        Milestone = milestone,
        BonusPointsAwarded = bonusPoints,
        TotalReferrals = totalReferrals
    };

    /// <summary>
    /// Creates a result when no milestone achieved.
    /// </summary>
    public static MilestoneCheckResult NoMilestone() => new()
    {
        MilestoneAchieved = false
    };
}

/// <summary>
/// DTO for a referral milestone.
/// </summary>
public class ReferralMilestoneDto
{
    /// <summary>
    /// The milestone ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The milestone name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the milestone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of referrals required.
    /// </summary>
    public int ReferralCount { get; set; }

    /// <summary>
    /// Bonus points awarded.
    /// </summary>
    public int BonusPoints { get; set; }

    /// <summary>
    /// Badge icon URL.
    /// </summary>
    public string? BadgeIcon { get; set; }

    /// <summary>
    /// Whether this milestone is achieved by the current member.
    /// </summary>
    public bool IsAchieved { get; set; }

    /// <summary>
    /// When it was achieved (if applicable).
    /// </summary>
    public DateTime? AchievedAt { get; set; }
}

/// <summary>
/// Progress toward a milestone.
/// </summary>
public class MilestoneProgress
{
    /// <summary>
    /// Current successful referral count.
    /// </summary>
    public int CurrentReferrals { get; set; }

    /// <summary>
    /// List of all milestones with progress.
    /// </summary>
    public List<MilestoneProgressItem> Milestones { get; set; } = new();

    /// <summary>
    /// Next milestone to achieve.
    /// </summary>
    public MilestoneProgressItem? NextMilestone { get; set; }
}

/// <summary>
/// Progress toward a single milestone.
/// </summary>
public class MilestoneProgressItem
{
    /// <summary>
    /// The milestone.
    /// </summary>
    public ReferralMilestoneDto Milestone { get; set; } = null!;

    /// <summary>
    /// Current progress count.
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Target count for this milestone.
    /// </summary>
    public int TargetCount { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => TargetCount > 0
        ? Math.Min(100, (decimal)CurrentCount / TargetCount * 100)
        : 0;

    /// <summary>
    /// Number remaining to achieve.
    /// </summary>
    public int Remaining => Math.Max(0, TargetCount - CurrentCount);

    /// <summary>
    /// Whether this milestone is achieved.
    /// </summary>
    public bool IsAchieved { get; set; }
}

/// <summary>
/// Entry in the referral leaderboard.
/// </summary>
public class ReferralLeaderboardEntry
{
    /// <summary>
    /// Rank position.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// The member ID.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The member's name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// The member's membership number.
    /// </summary>
    public string? MembershipNumber { get; set; }

    /// <summary>
    /// Number of successful referrals.
    /// </summary>
    public int SuccessfulReferrals { get; set; }

    /// <summary>
    /// Total points earned from referrals.
    /// </summary>
    public int TotalPointsEarned { get; set; }

    /// <summary>
    /// The member's tier.
    /// </summary>
    public MembershipTier Tier { get; set; }

    /// <summary>
    /// Tier display name.
    /// </summary>
    public string TierName => Tier.ToString();
}

/// <summary>
/// Leaderboard time period.
/// </summary>
public enum LeaderboardPeriod
{
    ThisWeek = 1,
    ThisMonth = 2,
    ThisYear = 3,
    AllTime = 4
}

/// <summary>
/// DTO for referral program configuration.
/// </summary>
public class ReferralConfigurationDto
{
    /// <summary>
    /// The configuration ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Points awarded to referrer.
    /// </summary>
    public int ReferrerBonusPoints { get; set; }

    /// <summary>
    /// Points awarded to referee.
    /// </summary>
    public int RefereeBonusPoints { get; set; }

    /// <summary>
    /// Minimum purchase to complete referral.
    /// </summary>
    public decimal MinPurchaseAmount { get; set; }

    /// <summary>
    /// Days to complete the referral.
    /// </summary>
    public int ExpiryDays { get; set; }

    /// <summary>
    /// Maximum referrals per member.
    /// </summary>
    public int? MaxReferralsPerMember { get; set; }

    /// <summary>
    /// Whether leaderboard is enabled.
    /// </summary>
    public bool EnableLeaderboard { get; set; }

    /// <summary>
    /// Whether to require new members only.
    /// </summary>
    public bool RequireNewMember { get; set; }

    /// <summary>
    /// Whether the program is active.
    /// </summary>
    public bool IsProgramActive { get; set; }

    /// <summary>
    /// SMS template for referrer notification.
    /// </summary>
    public string? ReferrerSmsTemplate { get; set; }

    /// <summary>
    /// SMS template for referee notification.
    /// </summary>
    public string? RefereeSmsTemplate { get; set; }
}

/// <summary>
/// Analytics for the referral program.
/// </summary>
public class ReferralAnalytics
{
    /// <summary>
    /// Total referrals in period.
    /// </summary>
    public int TotalReferrals { get; set; }

    /// <summary>
    /// Completed referrals in period.
    /// </summary>
    public int CompletedReferrals { get; set; }

    /// <summary>
    /// Pending referrals.
    /// </summary>
    public int PendingReferrals { get; set; }

    /// <summary>
    /// Expired referrals in period.
    /// </summary>
    public int ExpiredReferrals { get; set; }

    /// <summary>
    /// Overall conversion rate.
    /// </summary>
    public decimal ConversionRate => TotalReferrals > 0
        ? (decimal)CompletedReferrals / TotalReferrals * 100
        : 0;

    /// <summary>
    /// Total points distributed.
    /// </summary>
    public int TotalPointsDistributed { get; set; }

    /// <summary>
    /// Unique referrers in period.
    /// </summary>
    public int UniqueReferrers { get; set; }

    /// <summary>
    /// Average referrals per referrer.
    /// </summary>
    public decimal AvgReferralsPerReferrer => UniqueReferrers > 0
        ? (decimal)TotalReferrals / UniqueReferrers
        : 0;

    /// <summary>
    /// Average time to completion (days).
    /// </summary>
    public decimal AvgDaysToCompletion { get; set; }

    /// <summary>
    /// Top referrers in period.
    /// </summary>
    public List<ReferralLeaderboardEntry> TopReferrers { get; set; } = new();

    /// <summary>
    /// Daily referral counts for charting.
    /// </summary>
    public List<DailyReferralCount> DailyBreakdown { get; set; } = new();
}

/// <summary>
/// Daily referral count for analytics.
/// </summary>
public class DailyReferralCount
{
    /// <summary>
    /// The date.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// New referrals on this date.
    /// </summary>
    public int NewReferrals { get; set; }

    /// <summary>
    /// Completed referrals on this date.
    /// </summary>
    public int CompletedReferrals { get; set; }
}

/// <summary>
/// Summary of referral expiry job execution.
/// </summary>
public class ReferralExpiryJobSummary
{
    /// <summary>
    /// When the job started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of referrals that expired.
    /// </summary>
    public int ExpiredCount { get; set; }

    /// <summary>
    /// IDs of the expired referrals.
    /// </summary>
    public List<int> ExpiredReferralIds { get; set; } = new();

    /// <summary>
    /// Total pending referrals checked.
    /// </summary>
    public int TotalPendingChecked { get; set; }

    /// <summary>
    /// Number of warnings sent for soon-to-expire referrals.
    /// </summary>
    public int WarningsSent { get; set; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Error messages if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the job was skipped.
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Reason for skipping (if skipped).
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Job duration in milliseconds.
    /// </summary>
    public long DurationMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : 0;
}
