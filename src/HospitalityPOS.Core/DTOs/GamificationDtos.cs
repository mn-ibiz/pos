using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Badge DTOs

/// <summary>
/// Badge display information.
/// </summary>
public class BadgeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BadgeCategory Category { get; set; }
    public string CategoryName => Category.ToString();
    public BadgeTriggerType TriggerType { get; set; }
    public BadgeRarity Rarity { get; set; }
    public string RarityName => Rarity.ToString();
    public string? IconUrl { get; set; }
    public string? Color { get; set; }
    public int PointsAwarded { get; set; }
    public bool IsSecret { get; set; }
    public bool IsRepeatable { get; set; }
    public int MaxEarnings { get; set; }
    public decimal? ThresholdValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAvailable => (!StartDate.HasValue || StartDate <= DateTime.UtcNow) &&
                               (!EndDate.HasValue || EndDate >= DateTime.UtcNow);
    public int DisplayOrder { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public int TotalEarned { get; set; }  // Total times earned by all members
}

/// <summary>
/// Member's earned badge information.
/// </summary>
public class MemberBadgeDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public BadgeDto Badge { get; set; } = null!;
    public DateTime EarnedAt { get; set; }
    public int TimesEarned { get; set; }
    public int PointsAwarded { get; set; }
    public int? TriggeredByOrderId { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsViewed { get; set; }
    public bool IsPinned { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Badge award result.
/// </summary>
public class BadgeAwardResult
{
    public bool Success { get; set; }
    public MemberBadgeDto? Badge { get; set; }
    public int PointsAwarded { get; set; }
    public bool IsNewBadge { get; set; }
    public bool IsRepeatEarning { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Request to award a badge manually.
/// </summary>
public class AwardBadgeRequest
{
    public int MemberId { get; set; }
    public int BadgeId { get; set; }
    public int? OrderId { get; set; }
    public int? StoreId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Badge collection summary for a member.
/// </summary>
public class BadgeCollectionSummary
{
    public int MemberId { get; set; }
    public int TotalBadgesEarned { get; set; }
    public int UniqueBadgesEarned { get; set; }
    public int TotalBadgesAvailable { get; set; }
    public int TotalPointsFromBadges { get; set; }
    public Dictionary<BadgeCategory, int> BadgesByCategory { get; set; } = new();
    public Dictionary<BadgeRarity, int> BadgesByRarity { get; set; } = new();
    public List<MemberBadgeDto> RecentBadges { get; set; } = new();
    public List<MemberBadgeDto> PinnedBadges { get; set; } = new();
    public List<BadgeDto> NextBadgesToEarn { get; set; } = new();
    public int SecretBadgesUnlocked { get; set; }
}

/// <summary>
/// Badge check result after transaction.
/// </summary>
public class BadgeCheckResult
{
    public int MemberId { get; set; }
    public List<BadgeAwardResult> AwardedBadges { get; set; } = new();
    public int TotalPointsAwarded { get; set; }
    public int BadgesEarned => AwardedBadges.Count(b => b.Success);
}

#endregion

#region Challenge DTOs

/// <summary>
/// Challenge display information.
/// </summary>
public class ChallengeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChallengePeriod Period { get; set; }
    public string PeriodName => Period.ToString();
    public ChallengeGoalType GoalType { get; set; }
    public string GoalTypeName => GoalType.ToString();
    public decimal TargetValue { get; set; }
    public int RewardPoints { get; set; }
    public int? RewardBadgeId { get; set; }
    public BadgeDto? RewardBadge { get; set; }
    public decimal? BonusMultiplier { get; set; }
    public string? IconUrl { get; set; }
    public string? Color { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public LoyaltyTier? MinimumTier { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool ShowLeaderboard { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && IsEnabled;
    public bool IsEnabled { get; set; }
    public TimeSpan TimeRemaining => EndDate > DateTime.UtcNow ? EndDate - DateTime.UtcNow : TimeSpan.Zero;
}

/// <summary>
/// Member's challenge progress.
/// </summary>
public class MemberChallengeDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int ChallengeId { get; set; }
    public ChallengeDto Challenge { get; set; } = null!;
    public decimal CurrentProgress { get; set; }
    public decimal TargetValue => Challenge?.TargetValue ?? 0;
    public decimal ProgressPercentage => TargetValue > 0 ? Math.Min(100, (CurrentProgress / TargetValue) * 100) : 0;
    public ChallengeStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime JoinedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PointsAwarded { get; set; }
    public int? AwardedBadgeId { get; set; }
    public DateTime? LastProgressAt { get; set; }
    public bool IsCompleted => Status == ChallengeStatus.Completed;
    public decimal RemainingProgress => Math.Max(0, TargetValue - CurrentProgress);
}

/// <summary>
/// Challenge join result.
/// </summary>
public class ChallengeJoinResult
{
    public bool Success { get; set; }
    public MemberChallengeDto? MemberChallenge { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Challenge completion result.
/// </summary>
public class ChallengeCompletionResult
{
    public bool Success { get; set; }
    public int ChallengeId { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public BadgeAwardResult? BadgeAwarded { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Challenge progress update result.
/// </summary>
public class ChallengeProgressResult
{
    public int MemberId { get; set; }
    public List<MemberChallengeDto> UpdatedChallenges { get; set; } = new();
    public List<ChallengeCompletionResult> CompletedChallenges { get; set; } = new();
    public int TotalPointsAwarded { get; set; }
}

/// <summary>
/// Challenge leaderboard entry.
/// </summary>
public class ChallengeLeaderboardEntry
{
    public int Rank { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberPhotoUrl { get; set; }
    public LoyaltyTier Tier { get; set; }
    public decimal Progress { get; set; }
    public decimal ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Challenge leaderboard.
/// </summary>
public class ChallengeLeaderboard
{
    public int ChallengeId { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public List<ChallengeLeaderboardEntry> Entries { get; set; } = new();
    public int TotalParticipants { get; set; }
    public int CompletedCount { get; set; }
    public ChallengeLeaderboardEntry? CurrentMemberRank { get; set; }
}

#endregion

#region Streak DTOs

/// <summary>
/// Member streak information.
/// </summary>
public class MemberStreakDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public StreakType StreakType { get; set; }
    public string StreakTypeName => StreakType.ToString();
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime StreakStartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? NextActivityDeadline { get; set; }
    public bool IsAtRisk { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsFrozen { get; set; }
    public DateTime? FreezeExpiresAt { get; set; }
    public int FreezeTokensRemaining { get; set; }
    public TimeSpan? TimeUntilDeadline => NextActivityDeadline.HasValue && NextActivityDeadline > DateTime.UtcNow
        ? NextActivityDeadline.Value - DateTime.UtcNow
        : null;
    public List<StreakMilestoneDto> AchievedMilestones { get; set; } = new();
    public StreakMilestoneDefinitionDto? NextMilestone { get; set; }
}

/// <summary>
/// Streak milestone definition.
/// </summary>
public class StreakMilestoneDefinitionDto
{
    public int Id { get; set; }
    public StreakType StreakType { get; set; }
    public int StreakCount { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RewardPoints { get; set; }
    public int? RewardBadgeId { get; set; }
    public BadgeDto? RewardBadge { get; set; }
    public int FreezeTokensAwarded { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// Achieved streak milestone.
/// </summary>
public class StreakMilestoneDto
{
    public int Id { get; set; }
    public int MilestoneDefinitionId { get; set; }
    public StreakMilestoneDefinitionDto Milestone { get; set; } = null!;
    public int AchievedAtStreak { get; set; }
    public DateTime AchievedAt { get; set; }
    public int PointsAwarded { get; set; }
    public int FreezeTokensAwarded { get; set; }
}

/// <summary>
/// Streak update result.
/// </summary>
public class StreakUpdateResult
{
    public bool Success { get; set; }
    public MemberStreakDto? Streak { get; set; }
    public bool IsNewStreak { get; set; }
    public bool StreakExtended { get; set; }
    public int PreviousStreak { get; set; }
    public int NewStreak { get; set; }
    public StreakMilestoneDto? MilestoneAchieved { get; set; }
    public int PointsAwarded { get; set; }
    public int FreezeTokensAwarded { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Streak check result for all member streaks.
/// </summary>
public class StreakCheckResult
{
    public int MemberId { get; set; }
    public List<StreakUpdateResult> StreakUpdates { get; set; } = new();
    public int TotalPointsAwarded { get; set; }
    public int TotalFreezeTokensAwarded { get; set; }
    public List<StreakUpdateResult> BrokenStreaks { get; set; } = new();
    public List<MemberStreakDto> AtRiskStreaks { get; set; } = new();
}

/// <summary>
/// Streak freeze result.
/// </summary>
public class StreakFreezeResult
{
    public bool Success { get; set; }
    public MemberStreakDto? Streak { get; set; }
    public int TokensUsed { get; set; }
    public int TokensRemaining { get; set; }
    public DateTime? FreezeExpiresAt { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Streak summary across all types.
/// </summary>
public class StreakSummary
{
    public int MemberId { get; set; }
    public List<MemberStreakDto> ActiveStreaks { get; set; } = new();
    public List<MemberStreakDto> AtRiskStreaks { get; set; } = new();
    public int TotalFreezeTokens { get; set; }
    public Dictionary<StreakType, int> LongestStreaks { get; set; } = new();
    public int TotalMilestonesAchieved { get; set; }
    public int TotalPointsFromStreaks { get; set; }
}

#endregion

#region Configuration DTOs

/// <summary>
/// Gamification configuration.
/// </summary>
public class GamificationConfigurationDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsEnabled { get; set; }
    public bool BadgesEnabled { get; set; }
    public bool ChallengesEnabled { get; set; }
    public bool StreaksEnabled { get; set; }
    public int DefaultFreezeTokens { get; set; }
    public int StreakAtRiskHours { get; set; }
    public bool ShowBadgesOnReceipt { get; set; }
    public int MaxBadgesOnReceipt { get; set; }
    public bool NotifyOnBadgeEarned { get; set; }
    public bool NotifyOnChallengeProgress { get; set; }
    public string? ProgressNotificationThresholds { get; set; }
    public bool NotifyOnStreakAtRisk { get; set; }
    public bool AutoEnrollInChallenges { get; set; }
}

#endregion

#region Summary DTOs

/// <summary>
/// Complete gamification profile for a member.
/// </summary>
public class MemberGamificationProfile
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public LoyaltyTier Tier { get; set; }

    // Badges
    public BadgeCollectionSummary BadgeCollection { get; set; } = new();

    // Challenges
    public List<MemberChallengeDto> ActiveChallenges { get; set; } = new();
    public List<MemberChallengeDto> CompletedChallenges { get; set; } = new();
    public int TotalChallengesCompleted { get; set; }

    // Streaks
    public StreakSummary StreakSummary { get; set; } = new();

    // Overall stats
    public int TotalGamificationPoints { get; set; }
    public int GamificationLevel { get; set; }
    public int GamificationXP { get; set; }
    public int XPToNextLevel { get; set; }
}

/// <summary>
/// Transaction gamification result - what happened after a transaction.
/// </summary>
public class TransactionGamificationResult
{
    public int MemberId { get; set; }
    public int? OrderId { get; set; }

    // Badges earned
    public BadgeCheckResult BadgeResults { get; set; } = new();

    // Challenge progress
    public ChallengeProgressResult ChallengeResults { get; set; } = new();

    // Streak updates
    public StreakCheckResult StreakResults { get; set; } = new();

    // Summary
    public int TotalPointsAwarded { get; set; }
    public int TotalBadgesEarned { get; set; }
    public int TotalChallengesCompleted { get; set; }
    public int TotalStreakMilestones { get; set; }
    public List<string> Notifications { get; set; } = new();
}

/// <summary>
/// Background job summaries.
/// </summary>
public class StreakCheckJobSummary
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalMembersChecked { get; set; }
    public int BrokenStreaks { get; set; }
    public int AtRiskWarningsSent { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;
}

public class ChallengeExpiryJobSummary
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int ExpiredChallengesCount { get; set; }
    public int FailedChallengesCount { get; set; }
    public int RecurringChallengesCreated { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;
}

#endregion
