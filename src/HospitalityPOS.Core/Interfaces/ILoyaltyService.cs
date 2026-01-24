using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for loyalty program operations.
/// </summary>
public interface ILoyaltyService
{
    /// <summary>
    /// Enrolls a new customer in the loyalty program.
    /// </summary>
    /// <param name="dto">The enrollment data.</param>
    /// <param name="enrolledByUserId">The user ID performing the enrollment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enrollment result.</returns>
    Task<EnrollmentResult> EnrollCustomerAsync(EnrollCustomerDto dto, int enrolledByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loyalty member by phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number to search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member if found; otherwise, null.</returns>
    Task<LoyaltyMemberDto?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loyalty member by membership number.
    /// </summary>
    /// <param name="membershipNumber">The membership number to search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member if found; otherwise, null.</returns>
    Task<LoyaltyMemberDto?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loyalty member by ID.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member if found; otherwise, null.</returns>
    Task<LoyaltyMemberDto?> GetByIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a phone number is already registered.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the phone number exists; otherwise, false.</returns>
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for loyalty members by name or phone number.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching members.</returns>
    Task<IEnumerable<LoyaltyMemberDto>> SearchMembersAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a phone number format.
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate.</param>
    /// <returns>True if valid Kenya phone format; otherwise, false.</returns>
    bool ValidatePhoneNumber(string phoneNumber);

    /// <summary>
    /// Normalizes a phone number to 254XXXXXXXXX format.
    /// </summary>
    /// <param name="phoneNumber">The phone number to normalize.</param>
    /// <returns>The normalized phone number or null if invalid.</returns>
    string? NormalizePhoneNumber(string phoneNumber);

    /// <summary>
    /// Generates a new membership number.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique membership number in format LM-YYYYMMDD-XXXXX.</returns>
    Task<string> GenerateMembershipNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a loyalty member's information.
    /// </summary>
    /// <param name="memberId">The member ID to update.</param>
    /// <param name="name">New name (null to keep existing).</param>
    /// <param name="email">New email (null to keep existing).</param>
    /// <param name="updatedByUserId">The user ID performing the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if member not found.</returns>
    Task<bool> UpdateMemberAsync(int memberId, string? name, string? email, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a loyalty member.
    /// </summary>
    /// <param name="memberId">The member ID to deactivate.</param>
    /// <param name="deactivatedByUserId">The user ID performing the deactivation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if member not found.</returns>
    Task<bool> DeactivateMemberAsync(int memberId, int deactivatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a deactivated loyalty member.
    /// </summary>
    /// <param name="memberId">The member ID to reactivate.</param>
    /// <param name="reactivatedByUserId">The user ID performing the reactivation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if member not found.</returns>
    Task<bool> ReactivateMemberAsync(int memberId, int reactivatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a LoyaltyMember entity to a DTO.
    /// </summary>
    /// <param name="member">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    LoyaltyMemberDto MapToDto(LoyaltyMember member);

    // ================== Points Earning Methods ==================

    /// <summary>
    /// Calculates points to be earned for a transaction amount.
    /// </summary>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <param name="discountAmount">The discount amount (if not earning on discounted items).</param>
    /// <param name="taxAmount">The tax amount (if not earning on tax).</param>
    /// <param name="memberId">Optional member ID for tier-based bonuses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The calculated points earning result.</returns>
    Task<PointsCalculationResult> CalculatePointsAsync(
        decimal transactionAmount,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        int? memberId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Awards points to a loyalty member after a settled receipt.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="receiptNumber">The receipt number for reference.</param>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <param name="discountAmount">The discount amount applied.</param>
    /// <param name="taxAmount">The tax amount.</param>
    /// <param name="processedByUserId">The user ID awarding the points.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The points award result.</returns>
    Task<PointsAwardResult> AwardPointsAsync(
        int memberId,
        int receiptId,
        string receiptNumber,
        decimal transactionAmount,
        decimal discountAmount,
        decimal taxAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active points configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default points configuration.</returns>
    Task<PointsConfiguration?> GetPointsConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new points configuration.
    /// </summary>
    /// <param name="configuration">The configuration to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created configuration.</returns>
    Task<PointsConfiguration> CreatePointsConfigurationAsync(PointsConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing points configuration.
    /// </summary>
    /// <param name="configuration">The configuration to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<PointsConfiguration> UpdatePointsConfigurationAsync(PointsConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tier-based bonus multiplier for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bonus multiplier (1.0 = no bonus).</returns>
    Task<decimal> GetTierBonusMultiplierAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a visit and updates member statistics.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="spendAmount">The amount spent in this visit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the visit was recorded; false if member was not found.</returns>
    Task<bool> UpdateMemberVisitAsync(int memberId, decimal spendAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction history for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of loyalty transactions.</returns>
    Task<IEnumerable<LoyaltyTransactionDto>> GetTransactionHistoryAsync(
        int memberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated transaction history for a member with filtering options.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="type">Optional transaction type filter.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of loyalty transactions.</returns>
    Task<PagedTransactionHistoryResult> GetPagedTransactionHistoryAsync(
        int memberId,
        LoyaltyTransactionType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    // ================== Points Redemption Methods ==================

    /// <summary>
    /// Calculates and previews redemption options for a member and transaction amount.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="transactionAmount">The transaction amount in KES.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A preview of redemption options showing what can be redeemed.</returns>
    Task<RedemptionPreviewResult> CalculateRedemptionAsync(
        int memberId,
        decimal transactionAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems points for a member as payment towards a transaction.
    /// </summary>
    /// <param name="memberId">The loyalty member ID.</param>
    /// <param name="pointsToRedeem">The number of points to redeem.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="receiptNumber">The receipt number for reference.</param>
    /// <param name="transactionAmount">The original transaction amount in KES.</param>
    /// <param name="processedByUserId">The user ID processing the redemption.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The redemption result with the value applied.</returns>
    Task<RedemptionResult> RedeemPointsAsync(
        int memberId,
        decimal pointsToRedeem,
        int receiptId,
        string receiptNumber,
        decimal transactionAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts points to KES value based on current configuration.
    /// </summary>
    /// <param name="points">The number of points.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The KES value of the points.</returns>
    Task<decimal> ConvertPointsToValueAsync(decimal points, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts KES value to points based on current configuration.
    /// </summary>
    /// <param name="value">The KES value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of points equivalent to the value.</returns>
    Task<decimal> ConvertValueToPointsAsync(decimal value, CancellationToken cancellationToken = default);

    // ================== Tier Management Methods ==================

    /// <summary>
    /// Gets all tier configurations ordered by threshold.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of tier configurations.</returns>
    Task<IEnumerable<TierConfigurationDto>> GetTierConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration for a specific tier.
    /// </summary>
    /// <param name="tier">The tier to get configuration for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tier configuration or null if not found.</returns>
    Task<TierConfigurationDto?> GetTierConfigurationAsync(MembershipTier tier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a member's tier based on their current spend and points.
    /// Does not automatically update - use for preview/display.
    /// </summary>
    /// <param name="memberId">The member ID to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result showing current and potential tier.</returns>
    Task<TierEvaluationResult?> EvaluateMemberTierAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a member qualifies for a tier upgrade and performs it if so.
    /// Should be called after transactions.
    /// </summary>
    /// <param name="memberId">The member ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result with any tier change.</returns>
    Task<TierEvaluationResult?> CheckAndUpgradeTierAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs annual tier review for a member, potentially downgrading if thresholds not met.
    /// </summary>
    /// <param name="memberId">The member ID to review.</param>
    /// <param name="periodSpend">The spend during the evaluation period.</param>
    /// <param name="periodPoints">The points earned during the evaluation period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result with any tier change.</returns>
    Task<TierEvaluationResult?> PerformAnnualTierReviewAsync(
        int memberId,
        decimal periodSpend,
        decimal periodPoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually upgrades a member to a specific tier (admin function).
    /// </summary>
    /// <param name="memberId">The member ID to upgrade.</param>
    /// <param name="newTier">The tier to upgrade to.</param>
    /// <param name="reason">Reason for the manual upgrade.</param>
    /// <param name="upgradedByUserId">User ID performing the upgrade.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if member not found.</returns>
    Task<bool> ManualTierUpgradeAsync(
        int memberId,
        MembershipTier newTier,
        string reason,
        int upgradedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the member's progress towards the next tier.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Progress information or null if member not found.</returns>
    Task<TierEvaluationResult?> GetTierProgressAsync(int memberId, CancellationToken cancellationToken = default);

    // ================== Customer Analytics Methods ==================

    /// <summary>
    /// Gets comprehensive analytics for a loyalty member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer analytics or null if member not found.</returns>
    Task<CustomerAnalyticsDto?> GetCustomerAnalyticsAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top spending categories for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="maxCategories">Maximum number of categories to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of category spending data.</returns>
    Task<IEnumerable<CategorySpendDto>> GetTopCategoriesAsync(int memberId, int maxCategories = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports customer data for marketing campaigns.
    /// </summary>
    /// <param name="filter">Export filter options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The export result with file content.</returns>
    Task<CustomerExportResult> ExportCustomerDataAsync(CustomerExportFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the engagement score for a member based on RFM analysis.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The engagement score (0-100) or null if member not found.</returns>
    Task<int?> CalculateEngagementScoreAsync(int memberId, CancellationToken cancellationToken = default);
}
