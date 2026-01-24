using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for birthday and one-time rewards management.
/// </summary>
public interface IBirthdayRewardService
{
    // ================== One-Time Reward Template Management ==================

    /// <summary>
    /// Gets all one-time reward templates.
    /// </summary>
    /// <param name="activeOnly">If true, only returns active templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of reward templates.</returns>
    Task<IEnumerable<OneTimeRewardDto>> GetRewardTemplatesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a one-time reward template by ID.
    /// </summary>
    /// <param name="rewardId">The reward template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reward template or null if not found.</returns>
    Task<OneTimeRewardDto?> GetRewardTemplateByIdAsync(
        int rewardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active birthday reward template.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The birthday reward template or null if not configured.</returns>
    Task<OneTimeRewardDto?> GetBirthdayRewardTemplateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new one-time reward template.
    /// </summary>
    /// <param name="dto">The reward template data.</param>
    /// <param name="createdByUserId">The user creating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created reward template.</returns>
    Task<OneTimeRewardDto> CreateRewardTemplateAsync(
        OneTimeRewardDto dto,
        int createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing one-time reward template.
    /// </summary>
    /// <param name="dto">The reward template data.</param>
    /// <param name="updatedByUserId">The user updating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated reward template or null if not found.</returns>
    Task<OneTimeRewardDto?> UpdateRewardTemplateAsync(
        OneTimeRewardDto dto,
        int updatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a reward template.
    /// </summary>
    /// <param name="rewardId">The reward template ID.</param>
    /// <param name="deactivatedByUserId">The user deactivating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated; false if not found.</returns>
    Task<bool> DeactivateRewardTemplateAsync(
        int rewardId,
        int deactivatedByUserId,
        CancellationToken cancellationToken = default);

    // ================== Birthday Reward Processing ==================

    /// <summary>
    /// Issues a birthday reward to a specific member.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="processedByUserId">The user processing the reward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of issuing the reward.</returns>
    Task<BirthdayRewardResult> IssueBirthdayRewardAsync(
        int memberId,
        int processedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes birthday rewards for all members with birthdays in the specified date range.
    /// </summary>
    /// <param name="targetDate">The target date to process (usually today).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the processing job.</returns>
    Task<BirthdayRewardJobSummary> ProcessBirthdayRewardsAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets members with birthdays on a specific date (month and day).
    /// </summary>
    /// <param name="targetDate">The target date (month and day are used).</param>
    /// <param name="daysBeforeIssue">Days before the birthday to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of members with birthdays.</returns>
    Task<IEnumerable<LoyaltyMemberDto>> GetMembersWithBirthdayAsync(
        DateOnly targetDate,
        int daysBeforeIssue = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a member has already received a birthday reward for the specified year.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="year">The year to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if already received; false otherwise.</returns>
    Task<bool> HasReceivedBirthdayRewardAsync(
        int memberId,
        int year,
        CancellationToken cancellationToken = default);

    // ================== Member Reward Management ==================

    /// <summary>
    /// Gets all rewards for a specific member.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of member rewards.</returns>
    Task<IEnumerable<MemberRewardDto>> GetMemberRewardsAsync(
        int memberId,
        MemberRewardStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (redeemable) rewards for a member.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active rewards.</returns>
    Task<IEnumerable<MemberRewardDto>> GetActiveRewardsAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a member reward by redemption code.
    /// </summary>
    /// <param name="redemptionCode">The redemption code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member reward or null if not found.</returns>
    Task<MemberRewardDto?> GetRewardByCodeAsync(
        string redemptionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a reward can be redeemed for a transaction.
    /// </summary>
    /// <param name="redemptionCode">The redemption code.</param>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any error message.</returns>
    Task<(bool IsValid, string? ErrorMessage, MemberRewardDto? Reward)> ValidateRewardRedemptionAsync(
        string redemptionCode,
        decimal transactionAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems a reward for a transaction.
    /// </summary>
    /// <param name="redemptionCode">The redemption code.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <param name="processedByUserId">The user processing the redemption.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The redemption result.</returns>
    Task<RewardRedemptionResult> RedeemRewardAsync(
        string redemptionCode,
        int receiptId,
        decimal transactionAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the discount amount for a reward.
    /// </summary>
    /// <param name="reward">The member reward.</param>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <returns>The discount amount in KES.</returns>
    decimal CalculateRewardDiscount(MemberRewardDto reward, decimal transactionAmount);

    // ================== Expiry Processing ==================

    /// <summary>
    /// Processes expired rewards, updating their status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rewards marked as expired.</returns>
    Task<int> ProcessExpiredRewardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends expiry warning notifications to members with soon-to-expire rewards.
    /// </summary>
    /// <param name="daysBeforeExpiry">Days before expiry to warn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of warnings sent.</returns>
    Task<int> SendExpiryWarningsAsync(
        int daysBeforeExpiry = 3,
        CancellationToken cancellationToken = default);

    // ================== Utility Methods ==================

    /// <summary>
    /// Generates a unique redemption code.
    /// </summary>
    /// <returns>A unique redemption code in format RWD-YYYYMMDD-XXXXX.</returns>
    Task<string> GenerateRedemptionCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a OneTimeReward entity to a DTO.
    /// </summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    OneTimeRewardDto MapToDto(OneTimeReward entity);

    /// <summary>
    /// Maps a MemberReward entity to a DTO.
    /// </summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    MemberRewardDto MapToDto(MemberReward entity);
}

/// <summary>
/// Interface for the birthday reward background job.
/// </summary>
public interface IBirthdayRewardJob
{
    /// <summary>
    /// Processes birthday rewards for today's birthdays.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the processing job.</returns>
    Task<BirthdayRewardJobSummary> ProcessTodaysBirthdaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes expired rewards.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rewards expired.</returns>
    Task<int> ProcessExpiredRewardsAsync(CancellationToken cancellationToken = default);
}
