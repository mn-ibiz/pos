using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing the referral program.
/// </summary>
public interface IReferralService
{
    #region Referral Code Management

    /// <summary>
    /// Generates a new referral code for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="customCode">Optional custom code (null for auto-generated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated referral code.</returns>
    Task<ReferralCodeDto> GenerateReferralCodeAsync(int memberId, string? customCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the referral code for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member's referral code or null.</returns>
    Task<ReferralCodeDto?> GetReferralCodeAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a referral code for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member's referral code.</returns>
    Task<ReferralCodeDto> GetOrCreateReferralCodeAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates the referral code for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="reason">Reason for regeneration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new referral code.</returns>
    Task<ReferralCodeDto> RegenerateReferralCodeAsync(int memberId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a referral code.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <param name="newMemberPhone">Optional phone of new member to check for self-referral.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ReferralCodeValidation> ValidateReferralCodeAsync(string code, string? newMemberPhone = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a member's referral code.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="reason">Reason for deactivation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateReferralCodeAsync(int memberId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Referral Processing

    /// <summary>
    /// Processes a referral signup (when new member uses a referral code).
    /// </summary>
    /// <param name="code">The referral code used.</param>
    /// <param name="newMemberId">The new member's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The referral signup result.</returns>
    Task<ReferralSignupResult> ProcessReferralSignupAsync(string code, int newMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a referral when the referee makes a qualifying purchase.
    /// </summary>
    /// <param name="refereeId">The referee's member ID.</param>
    /// <param name="receiptId">The receipt ID of the qualifying purchase.</param>
    /// <param name="amount">The purchase amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion result.</returns>
    Task<ReferralCompletionResult> CompleteReferralAsync(int refereeId, int receiptId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires old referrals that have passed their deadline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of referrals expired.</returns>
    Task<int> ExpireOldReferralsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending referral.
    /// </summary>
    /// <param name="referralId">The referral ID.</param>
    /// <param name="reason">Reason for cancellation.</param>
    /// <param name="cancelledByUserId">User performing the cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelReferralAsync(int referralId, string reason, int cancelledByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Referral Queries

    /// <summary>
    /// Gets referrals for a member (as referrer or referee).
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="asReferrer">True to get referrals where member is the referrer.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of referrals.</returns>
    Task<List<ReferralDto>> GetMemberReferralsAsync(int memberId, bool asReferrer = true, ReferralStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets referral statistics for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Referral statistics.</returns>
    Task<ReferralStats> GetMemberReferralStatsAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pending referral for a referee (to check if they have an incomplete referral).
    /// </summary>
    /// <param name="refereeId">The referee's member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pending referral or null.</returns>
    Task<ReferralDto?> GetPendingReferralAsync(int refereeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a referral by ID.
    /// </summary>
    /// <param name="referralId">The referral ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The referral or null.</returns>
    Task<ReferralDto?> GetReferralAsync(int referralId, CancellationToken cancellationToken = default);

    #endregion

    #region Leaderboard

    /// <summary>
    /// Gets the referral leaderboard.
    /// </summary>
    /// <param name="period">Time period for the leaderboard.</param>
    /// <param name="top">Number of entries to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Leaderboard entries.</returns>
    Task<List<ReferralLeaderboardEntry>> GetReferralLeaderboardAsync(LeaderboardPeriod period = LeaderboardPeriod.AllTime, int top = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a member's rank in the leaderboard.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="period">Time period for the leaderboard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member's rank (1-based) or null if not ranked.</returns>
    Task<int?> GetMemberLeaderboardRankAsync(int memberId, LeaderboardPeriod period, CancellationToken cancellationToken = default);

    #endregion

    #region Milestones

    /// <summary>
    /// Checks and awards any earned milestones for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Milestone check result.</returns>
    Task<MilestoneCheckResult> CheckAndAwardMilestonesAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available milestones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of milestones.</returns>
    Task<List<ReferralMilestoneDto>> GetAvailableMilestonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets milestones achieved by a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of achieved milestones.</returns>
    Task<List<ReferralMilestoneDto>> GetMemberMilestonesAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets milestone progress for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Milestone progress.</returns>
    Task<MilestoneProgress> GetMilestoneProgressAsync(int memberId, CancellationToken cancellationToken = default);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the referral program configuration.
    /// </summary>
    /// <param name="storeId">Optional store ID for store-specific config.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration.</returns>
    Task<ReferralConfigurationDto> GetConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the referral program configuration.
    /// </summary>
    /// <param name="config">The updated configuration.</param>
    /// <param name="updatedByUserId">User making the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<ReferralConfigurationDto> UpdateConfigurationAsync(ReferralConfigurationDto config, int updatedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets referral program analytics.
    /// </summary>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics data.</returns>
    Task<ReferralAnalytics> GetReferralAnalyticsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Interface for the referral expiry background job.
/// </summary>
public interface IReferralExpiryJob
{
    /// <summary>
    /// Runs the referral expiry job immediately.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of expired referrals.</returns>
    Task<ReferralExpiryJobSummary> RunNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last run summary.
    /// </summary>
    ReferralExpiryJobSummary? LastRunSummary { get; }

    /// <summary>
    /// Gets when the job last ran.
    /// </summary>
    DateTime? LastRunTime { get; }
}
