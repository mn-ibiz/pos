using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for dynamic pricing operations.
/// </summary>
public interface IDynamicPricingService
{
    #region Configuration

    /// <summary>
    /// Gets dynamic pricing configuration for a store.
    /// </summary>
    Task<DynamicPricingConfigurationDto> GetConfigurationAsync(int storeId);

    /// <summary>
    /// Updates dynamic pricing configuration.
    /// </summary>
    Task<DynamicPricingConfigurationDto> UpdateConfigurationAsync(DynamicPricingConfigurationDto config);

    /// <summary>
    /// Checks if dynamic pricing is enabled for a store.
    /// </summary>
    Task<bool> IsDynamicPricingEnabledAsync(int storeId);

    #endregion

    #region Price Calculation

    /// <summary>
    /// Gets the current dynamic price for a product.
    /// </summary>
    Task<DynamicPriceDto> GetDynamicPriceAsync(int productId, int storeId, DateTime? asOf = null);

    /// <summary>
    /// Gets dynamic prices for multiple products.
    /// </summary>
    Task<List<DynamicPriceDto>> GetDynamicPricesAsync(List<int> productIds, int storeId);

    /// <summary>
    /// Calculates price based on provided context.
    /// </summary>
    Task<DynamicPriceDto> CalculatePriceAsync(int productId, DynamicPricingContext context);

    /// <summary>
    /// Quick lookup of current price for POS.
    /// </summary>
    Task<decimal> GetCurrentPriceAsync(int productId, int storeId);

    /// <summary>
    /// Gets all current dynamic prices for a store (for cache refresh).
    /// </summary>
    Task<List<DynamicPriceDto>> GetAllCurrentPricesAsync(int storeId);

    #endregion

    #region Rule Management

    /// <summary>
    /// Gets all pricing rules.
    /// </summary>
    Task<List<DynamicPricingRuleDto>> GetRulesAsync(int? storeId = null, bool includeInactive = false);

    /// <summary>
    /// Gets all active pricing rules.
    /// </summary>
    Task<List<DynamicPricingRuleDto>> GetActiveRulesAsync(int? storeId = null);

    /// <summary>
    /// Gets rules applicable to a specific product.
    /// </summary>
    Task<List<DynamicPricingRuleDto>> GetRulesForProductAsync(int productId, int storeId);

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    Task<DynamicPricingRuleDto?> GetRuleAsync(int ruleId);

    /// <summary>
    /// Creates a new pricing rule.
    /// </summary>
    Task<DynamicPricingRuleDto> CreateRuleAsync(CreateDynamicPricingRuleRequest request, int userId);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    Task<DynamicPricingRuleDto> UpdateRuleAsync(int ruleId, CreateDynamicPricingRuleRequest request);

    /// <summary>
    /// Deletes a pricing rule.
    /// </summary>
    Task DeleteRuleAsync(int ruleId);

    /// <summary>
    /// Activates a pricing rule.
    /// </summary>
    Task ActivateRuleAsync(int ruleId);

    /// <summary>
    /// Deactivates a pricing rule.
    /// </summary>
    Task DeactivateRuleAsync(int ruleId);

    /// <summary>
    /// Adds a product exception to a rule.
    /// </summary>
    Task AddRuleExceptionAsync(int ruleId, int productId, string? reason = null);

    /// <summary>
    /// Removes a product exception from a rule.
    /// </summary>
    Task RemoveRuleExceptionAsync(int ruleId, int productId);

    #endregion

    #region Batch Operations

    /// <summary>
    /// Applies dynamic pricing to all applicable products.
    /// </summary>
    Task<PriceUpdateResult> ApplyDynamicPricingAsync(int storeId);

    /// <summary>
    /// Refreshes all prices (background job entry point).
    /// </summary>
    Task<DynamicPricingJobResult> RefreshAllPricesAsync(int storeId);

    /// <summary>
    /// Previews price changes without applying.
    /// </summary>
    Task<PriceUpdateResult> PreviewPriceChangesAsync(int storeId);

    /// <summary>
    /// Applies expiry-based discounts.
    /// </summary>
    Task<ExpiryPricingJobResult> ApplyExpiryDiscountsAsync(int storeId);

    #endregion

    #region Approval Workflow

    /// <summary>
    /// Requests a price change (for approval workflow).
    /// </summary>
    Task<PendingPriceChangeDto> RequestPriceChangeAsync(int productId, decimal newPrice, string reason, int requestedByUserId, int storeId);

    /// <summary>
    /// Approves a pending price change.
    /// </summary>
    Task<PendingPriceChangeDto> ApprovePriceChangeAsync(int pendingChangeId, int approverUserId);

    /// <summary>
    /// Rejects a pending price change.
    /// </summary>
    Task<PendingPriceChangeDto> RejectPriceChangeAsync(int pendingChangeId, int rejecterUserId, string reason);

    /// <summary>
    /// Gets all pending approvals for a store.
    /// </summary>
    Task<List<PendingPriceChangeDto>> GetPendingApprovalsAsync(int storeId);

    /// <summary>
    /// Gets pending approvals count.
    /// </summary>
    Task<int> GetPendingApprovalsCountAsync(int storeId);

    /// <summary>
    /// Expires old pending changes.
    /// </summary>
    Task ExpirePendingChangesAsync();

    #endregion

    #region Simulation

    /// <summary>
    /// Simulates the impact of a price change.
    /// </summary>
    Task<PricingSimulation> SimulatePriceChangeAsync(int productId, decimal newPrice, int storeId);

    /// <summary>
    /// Simulates the impact of a pricing rule.
    /// </summary>
    Task<PricingSimulation> SimulateRuleAsync(CreateDynamicPricingRuleRequest rule, int storeId);

    /// <summary>
    /// Gets products that would be affected by a rule.
    /// </summary>
    Task<List<AffectedProduct>> GetAffectedProductsAsync(int ruleId);

    /// <summary>
    /// Gets products that would be affected by a new rule.
    /// </summary>
    Task<List<AffectedProduct>> PreviewAffectedProductsAsync(CreateDynamicPricingRuleRequest rule, int storeId);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets dynamic pricing analytics for a date range.
    /// </summary>
    Task<DynamicPricingAnalytics> GetAnalyticsAsync(DateTime from, DateTime to, int? storeId = null);

    /// <summary>
    /// Gets price elasticity report for a product.
    /// </summary>
    Task<PriceElasticityReport> GetElasticityReportAsync(int productId, DateTime from, DateTime to);

    /// <summary>
    /// Gets revenue impact report for a rule.
    /// </summary>
    Task<RevenueImpactReport> GetRevenueImpactAsync(int ruleId, DateTime from, DateTime to);

    /// <summary>
    /// Gets top performing rules.
    /// </summary>
    Task<List<RulePerformance>> GetTopPerformingRulesAsync(int storeId, int count = 10);

    /// <summary>
    /// Updates daily metrics for a store.
    /// </summary>
    Task UpdateDailyMetricsAsync(int storeId, DateTime date);

    #endregion

    #region Price History

    /// <summary>
    /// Gets price change history for a product.
    /// </summary>
    Task<List<DynamicPriceLog>> GetPriceHistoryAsync(int productId, DateTime from, DateTime to);

    /// <summary>
    /// Logs a price change.
    /// </summary>
    Task LogPriceChangeAsync(int productId, int? ruleId, decimal originalPrice, decimal adjustedPrice, string reason, int storeId, int? approvedByUserId = null);

    #endregion
}

/// <summary>
/// Interface for the dynamic pricing background job.
/// </summary>
public interface IDynamicPricingJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    DynamicPricingJobResult? LastResult { get; }

    /// <summary>
    /// Triggers an immediate price refresh.
    /// </summary>
    Task TriggerRefreshAsync();
}

/// <summary>
/// Interface for the expiry pricing background job.
/// </summary>
public interface IExpiryPricingJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    ExpiryPricingJobResult? LastResult { get; }

    /// <summary>
    /// Triggers an immediate expiry pricing run.
    /// </summary>
    Task TriggerRunAsync();
}
