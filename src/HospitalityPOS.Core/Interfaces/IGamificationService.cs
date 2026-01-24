using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for gamification features (badges, challenges, streaks).
/// </summary>
public interface IGamificationService
{
    #region Badge Management

    /// <summary>
    /// Gets all available badges.
    /// </summary>
    Task<List<BadgeDto>> GetAllBadgesAsync(int? storeId = null, bool includeSecret = false);

    /// <summary>
    /// Gets a badge by ID.
    /// </summary>
    Task<BadgeDto?> GetBadgeByIdAsync(int badgeId);

    /// <summary>
    /// Gets badges by category.
    /// </summary>
    Task<List<BadgeDto>> GetBadgesByCategoryAsync(BadgeCategory category, int? storeId = null);

    /// <summary>
    /// Creates a new badge.
    /// </summary>
    Task<BadgeDto> CreateBadgeAsync(BadgeDto badge);

    /// <summary>
    /// Updates an existing badge.
    /// </summary>
    Task<BadgeDto> UpdateBadgeAsync(BadgeDto badge);

    /// <summary>
    /// Deletes a badge (soft delete).
    /// </summary>
    Task<bool> DeleteBadgeAsync(int badgeId);

    #endregion

    #region Member Badges

    /// <summary>
    /// Gets all badges earned by a member.
    /// </summary>
    Task<List<MemberBadgeDto>> GetMemberBadgesAsync(int memberId);

    /// <summary>
    /// Gets a member's badge collection summary.
    /// </summary>
    Task<BadgeCollectionSummary> GetBadgeCollectionSummaryAsync(int memberId);

    /// <summary>
    /// Awards a badge to a member.
    /// </summary>
    Task<BadgeAwardResult> AwardBadgeAsync(AwardBadgeRequest request);

    /// <summary>
    /// Checks and awards eligible badges after a transaction.
    /// </summary>
    Task<BadgeCheckResult> CheckAndAwardBadgesAsync(int memberId, int? orderId, int? storeId);

    /// <summary>
    /// Marks a badge as viewed by the member.
    /// </summary>
    Task<bool> MarkBadgeViewedAsync(int memberBadgeId);

    /// <summary>
    /// Toggles badge pin status on member profile.
    /// </summary>
    Task<bool> ToggleBadgePinAsync(int memberBadgeId);

    /// <summary>
    /// Gets unviewed badges for a member.
    /// </summary>
    Task<List<MemberBadgeDto>> GetUnviewedBadgesAsync(int memberId);

    #endregion

    #region Challenge Management

    /// <summary>
    /// Gets all active challenges.
    /// </summary>
    Task<List<ChallengeDto>> GetActiveChallengesAsync(int? storeId = null, LoyaltyTier? memberTier = null);

    /// <summary>
    /// Gets a challenge by ID.
    /// </summary>
    Task<ChallengeDto?> GetChallengeByIdAsync(int challengeId);

    /// <summary>
    /// Gets challenges by period type.
    /// </summary>
    Task<List<ChallengeDto>> GetChallengesByPeriodAsync(ChallengePeriod period, int? storeId = null);

    /// <summary>
    /// Creates a new challenge.
    /// </summary>
    Task<ChallengeDto> CreateChallengeAsync(ChallengeDto challenge);

    /// <summary>
    /// Updates an existing challenge.
    /// </summary>
    Task<ChallengeDto> UpdateChallengeAsync(ChallengeDto challenge);

    /// <summary>
    /// Deletes a challenge (soft delete).
    /// </summary>
    Task<bool> DeleteChallengeAsync(int challengeId);

    #endregion

    #region Member Challenges

    /// <summary>
    /// Gets a member's active challenges.
    /// </summary>
    Task<List<MemberChallengeDto>> GetMemberActiveChallengesAsync(int memberId);

    /// <summary>
    /// Gets a member's completed challenges.
    /// </summary>
    Task<List<MemberChallengeDto>> GetMemberCompletedChallengesAsync(int memberId);

    /// <summary>
    /// Gets a member's challenge history.
    /// </summary>
    Task<List<MemberChallengeDto>> GetMemberChallengeHistoryAsync(int memberId, int? limit = null);

    /// <summary>
    /// Joins a member to a challenge.
    /// </summary>
    Task<ChallengeJoinResult> JoinChallengeAsync(int memberId, int challengeId);

    /// <summary>
    /// Updates challenge progress after a transaction.
    /// </summary>
    Task<ChallengeProgressResult> UpdateChallengeProgressAsync(int memberId, int? orderId, decimal? spendAmount, int? storeId);

    /// <summary>
    /// Gets challenge leaderboard.
    /// </summary>
    Task<ChallengeLeaderboard> GetChallengeLeaderboardAsync(int challengeId, int? memberId = null, int topCount = 10);

    /// <summary>
    /// Auto-enrolls member in eligible challenges.
    /// </summary>
    Task<List<ChallengeJoinResult>> AutoEnrollMemberInChallengesAsync(int memberId, int? storeId = null);

    #endregion

    #region Streak Management

    /// <summary>
    /// Gets all streak milestone definitions.
    /// </summary>
    Task<List<StreakMilestoneDefinitionDto>> GetStreakMilestoneDefinitionsAsync(StreakType? streakType = null);

    /// <summary>
    /// Creates a streak milestone definition.
    /// </summary>
    Task<StreakMilestoneDefinitionDto> CreateStreakMilestoneDefinitionAsync(StreakMilestoneDefinitionDto milestone);

    /// <summary>
    /// Updates a streak milestone definition.
    /// </summary>
    Task<StreakMilestoneDefinitionDto> UpdateStreakMilestoneDefinitionAsync(StreakMilestoneDefinitionDto milestone);

    #endregion

    #region Member Streaks

    /// <summary>
    /// Gets all streaks for a member.
    /// </summary>
    Task<List<MemberStreakDto>> GetMemberStreaksAsync(int memberId);

    /// <summary>
    /// Gets a member's streak summary.
    /// </summary>
    Task<StreakSummary> GetStreakSummaryAsync(int memberId);

    /// <summary>
    /// Gets a specific streak for a member.
    /// </summary>
    Task<MemberStreakDto?> GetMemberStreakAsync(int memberId, StreakType streakType, int? storeId = null);

    /// <summary>
    /// Updates streaks after a member activity (visit/purchase).
    /// </summary>
    Task<StreakCheckResult> UpdateStreaksAsync(int memberId, DateTime activityTime, int? storeId = null);

    /// <summary>
    /// Freezes a streak using a freeze token.
    /// </summary>
    Task<StreakFreezeResult> FreezeStreakAsync(int memberId, StreakType streakType, int? storeId = null);

    /// <summary>
    /// Unfreezes a streak.
    /// </summary>
    Task<bool> UnfreezeStreakAsync(int memberStreakId);

    /// <summary>
    /// Gets at-risk streaks for a member.
    /// </summary>
    Task<List<MemberStreakDto>> GetAtRiskStreaksAsync(int memberId);

    /// <summary>
    /// Checks for broken streaks and updates status (for background job).
    /// </summary>
    Task<StreakCheckJobSummary> ProcessBrokenStreaksAsync();

    #endregion

    #region Configuration

    /// <summary>
    /// Gets gamification configuration.
    /// </summary>
    Task<GamificationConfigurationDto> GetConfigurationAsync(int? storeId = null);

    /// <summary>
    /// Updates gamification configuration.
    /// </summary>
    Task<GamificationConfigurationDto> UpdateConfigurationAsync(GamificationConfigurationDto config);

    #endregion

    #region Profile & Transaction Processing

    /// <summary>
    /// Gets complete gamification profile for a member.
    /// </summary>
    Task<MemberGamificationProfile> GetMemberGamificationProfileAsync(int memberId);

    /// <summary>
    /// Processes all gamification updates after a transaction.
    /// </summary>
    Task<TransactionGamificationResult> ProcessTransactionAsync(int memberId, int orderId, decimal spendAmount, int? storeId);

    #endregion

    #region Challenge Expiry (Background Job)

    /// <summary>
    /// Processes expired challenges and creates recurring instances.
    /// </summary>
    Task<ChallengeExpiryJobSummary> ProcessExpiredChallengesAsync();

    #endregion
}

/// <summary>
/// Interface for streak check background job.
/// </summary>
public interface IStreakCheckJob
{
    /// <summary>
    /// Gets the last run summary.
    /// </summary>
    StreakCheckJobSummary? LastRunSummary { get; }

    /// <summary>
    /// Triggers an immediate run of the streak check.
    /// </summary>
    Task TriggerRunAsync();
}

/// <summary>
/// Interface for challenge expiry background job.
/// </summary>
public interface IChallengeExpiryJob
{
    /// <summary>
    /// Gets the last run summary.
    /// </summary>
    ChallengeExpiryJobSummary? LastRunSummary { get; }

    /// <summary>
    /// Triggers an immediate run of the challenge expiry check.
    /// </summary>
    Task TriggerRunAsync();
}
