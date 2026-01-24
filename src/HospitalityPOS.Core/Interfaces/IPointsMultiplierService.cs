using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing product-specific points multipliers and promotional rules.
/// </summary>
public interface IPointsMultiplierService
{
    #region Item-Level Points Calculation

    /// <summary>
    /// Calculates points for a transaction with item-level breakdown.
    /// </summary>
    /// <param name="items">The transaction items.</param>
    /// <param name="memberId">Optional member ID for tier-based calculations.</param>
    /// <param name="storeId">Optional store ID for store-specific rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed points calculation with item breakdown.</returns>
    Task<DetailedPointsCalculationResult> CalculateItemPointsAsync(
        List<TransactionItemDto> items,
        int? memberId = null,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective multiplier for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="memberId">Optional member ID for tier-based multipliers.</param>
    /// <param name="storeId">Optional store ID for store-specific rules.</param>
    /// <param name="checkTime">The time to check against time-based rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective multiplier and its source.</returns>
    Task<(decimal multiplier, string source, string? ruleName)> GetEffectiveMultiplierAsync(
        int productId,
        int? memberId = null,
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product is excluded from earning loyalty points.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if excluded, false otherwise.</returns>
    Task<bool> IsProductExcludedAsync(int productId, CancellationToken cancellationToken = default);

    #endregion

    #region Product/Category Multiplier Configuration

    /// <summary>
    /// Gets the points configuration for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product points configuration.</returns>
    Task<ProductPointsConfigDto?> GetProductPointsConfigAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets points configuration for multiple products.
    /// </summary>
    /// <param name="productIds">The product IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of product points configurations.</returns>
    Task<List<ProductPointsConfigDto>> GetProductPointsConfigsAsync(List<int> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by category with their points configuration.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of product points configurations.</returns>
    Task<List<ProductPointsConfigDto>> GetCategoryProductPointsConfigsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the points multiplier for a product.
    /// </summary>
    /// <param name="dto">The update request.</param>
    /// <param name="updatedByUserId">The user making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateProductPointsMultiplierAsync(UpdateProductPointsDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the points multiplier for a category.
    /// </summary>
    /// <param name="dto">The update request.</param>
    /// <param name="updatedByUserId">The user making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateCategoryPointsMultiplierAsync(UpdateCategoryPointsDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates product points multipliers.
    /// </summary>
    /// <param name="updates">List of update requests.</param>
    /// <param name="updatedByUserId">The user making the changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of products updated.</returns>
    Task<int> BulkUpdateProductPointsMultipliersAsync(List<UpdateProductPointsDto> updates, int updatedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Points Multiplier Rules

    /// <summary>
    /// Gets all multiplier rules.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of rules.</returns>
    Task<List<PointsMultiplierRuleDto>> GetRulesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a multiplier rule by ID.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rule or null.</returns>
    Task<PointsMultiplierRuleDto?> GetRuleAsync(int ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active rules applicable to a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="checkTime">The time to check against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of applicable rules.</returns>
    Task<List<PointsMultiplierRuleDto>> GetApplicableRulesForProductAsync(
        int productId,
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active global promotional rules.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="checkTime">The time to check against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active global rules.</returns>
    Task<List<PointsMultiplierRuleDto>> GetActivePromotionsAsync(
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new multiplier rule.
    /// </summary>
    /// <param name="dto">The rule details.</param>
    /// <param name="createdByUserId">The user creating the rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created rule.</returns>
    Task<MultiplierRuleResult> CreateRuleAsync(PointsMultiplierRuleDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing multiplier rule.
    /// </summary>
    /// <param name="dto">The updated rule details.</param>
    /// <param name="updatedByUserId">The user updating the rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated rule.</returns>
    Task<MultiplierRuleResult> UpdateRuleAsync(PointsMultiplierRuleDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a multiplier rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="deactivatedByUserId">The user deactivating the rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeactivateRuleAsync(int ruleId, int deactivatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a multiplier rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="deletedByUserId">The user deleting the rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteRuleAsync(int ruleId, int deletedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Rule Usage Tracking

    /// <summary>
    /// Records usage of a multiplier rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="memberId">The member ID.</param>
    /// <param name="transactionId">Optional loyalty transaction ID.</param>
    /// <param name="receiptId">Optional receipt ID.</param>
    /// <param name="basePoints">The base points before multiplier.</param>
    /// <param name="bonusPoints">The bonus points earned.</param>
    /// <param name="multiplier">The multiplier applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordRuleUsageAsync(
        int ruleId,
        int memberId,
        int? transactionId,
        int? receiptId,
        decimal basePoints,
        decimal bonusPoints,
        decimal multiplier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the usage count for a member on a specific rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage count.</returns>
    Task<int> GetMemberRuleUsageCountAsync(int ruleId, int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a member can use a specific rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="memberId">The member ID.</param>
    /// <param name="checkTime">The time to check against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the member can use the rule, false otherwise.</returns>
    Task<(bool canUse, string? reason)> CanMemberUseRuleAsync(
        int ruleId,
        int memberId,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets statistics about rule usage.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="startDate">Start date for statistics.</param>
    /// <param name="endDate">End date for statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage statistics.</returns>
    Task<RuleUsageStatisticsDto> GetRuleUsageStatisticsAsync(
        int ruleId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with the highest bonus points earned.
    /// </summary>
    /// <param name="topN">Number of products to return.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products with bonus points data.</returns>
    Task<List<ProductBonusPointsSummary>> GetTopBonusPointsProductsAsync(
        int topN,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Statistics about rule usage.
/// </summary>
public class RuleUsageStatisticsDto
{
    /// <summary>
    /// The rule ID.
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// The rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of times the rule was used.
    /// </summary>
    public int TotalUsages { get; set; }

    /// <summary>
    /// Number of unique members who used the rule.
    /// </summary>
    public int UniqueMembersCount { get; set; }

    /// <summary>
    /// Total bonus points awarded through this rule.
    /// </summary>
    public decimal TotalBonusPointsAwarded { get; set; }

    /// <summary>
    /// Average bonus points per usage.
    /// </summary>
    public decimal AverageBonusPoints => TotalUsages > 0 ? TotalBonusPointsAwarded / TotalUsages : 0;

    /// <summary>
    /// Remaining usages (if limited).
    /// </summary>
    public int? RemainingUsages { get; set; }
}

/// <summary>
/// Summary of bonus points earned by product.
/// </summary>
public class ProductBonusPointsSummary
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The category name.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Total bonus points earned from this product.
    /// </summary>
    public decimal TotalBonusPoints { get; set; }

    /// <summary>
    /// Number of times bonus points were earned.
    /// </summary>
    public int BonusEarnCount { get; set; }

    /// <summary>
    /// The current multiplier configured.
    /// </summary>
    public decimal CurrentMultiplier { get; set; }
}
