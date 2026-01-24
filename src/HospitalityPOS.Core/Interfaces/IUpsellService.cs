using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for AI-powered upsell and cross-sell recommendations.
/// </summary>
public interface IUpsellService
{
    #region Real-time Suggestions

    /// <summary>
    /// Gets upsell suggestions based on the current cart context.
    /// </summary>
    Task<List<UpsellSuggestion>> GetSuggestionsAsync(UpsellContext context);

    /// <summary>
    /// Gets suggestions for a specific product.
    /// </summary>
    Task<List<UpsellSuggestion>> GetSuggestionsForProductAsync(int productId, int? customerId = null, int? storeId = null);

    /// <summary>
    /// Gets suggestions for a cart of products.
    /// </summary>
    Task<List<UpsellSuggestion>> GetSuggestionsForCartAsync(List<int> productIds, int? customerId = null, int? storeId = null);

    #endregion

    #region Personalized Recommendations

    /// <summary>
    /// Gets personalized product recommendations for a customer.
    /// </summary>
    Task<List<ProductRecommendation>> GetPersonalizedRecommendationsAsync(int customerId, int maxResults = 5);

    /// <summary>
    /// Gets trending products.
    /// </summary>
    Task<List<ProductRecommendation>> GetTrendingProductsAsync(int? storeId = null, int maxResults = 5);

    /// <summary>
    /// Gets new arrival products.
    /// </summary>
    Task<List<ProductRecommendation>> GetNewArrivalsAsync(int? storeId = null, int maxResults = 5);

    #endregion

    #region Suggestion Tracking

    /// <summary>
    /// Records a suggestion that was shown.
    /// </summary>
    Task<int> RecordSuggestionAsync(RecordSuggestionRequest request);

    /// <summary>
    /// Records that a suggestion was accepted.
    /// </summary>
    Task RecordAcceptanceAsync(int suggestionLogId, int quantity, decimal value);

    /// <summary>
    /// Records that a suggestion was rejected.
    /// </summary>
    Task RecordRejectionAsync(int suggestionLogId);

    /// <summary>
    /// Records multiple suggestions shown at once.
    /// </summary>
    Task<List<int>> RecordSuggestionsAsync(int receiptId, List<UpsellSuggestion> suggestions, int? userId = null, int? customerId = null, int? storeId = null);

    #endregion

    #region Association Mining

    /// <summary>
    /// Rebuilds all product associations (background job).
    /// </summary>
    Task<AssociationRebuildResult> RebuildAssociationsAsync();

    /// <summary>
    /// Rebuilds associations for a specific date range.
    /// </summary>
    Task<AssociationRebuildResult> RebuildAssociationsAsync(DateTime from, DateTime to, int? storeId = null);

    /// <summary>
    /// Updates associations for a specific product.
    /// </summary>
    Task UpdateAssociationsForProductAsync(int productId);

    /// <summary>
    /// Gets all associations.
    /// </summary>
    Task<List<ProductAssociationDto>> GetAssociationsAsync(int? storeId = null);

    /// <summary>
    /// Gets associations for a specific product.
    /// </summary>
    Task<List<ProductAssociationDto>> GetAssociationsForProductAsync(int productId);

    /// <summary>
    /// Calculates association metrics for a specific product pair.
    /// </summary>
    Task<AssociationMetrics?> CalculateAssociationMetricsAsync(int productA, int productB, DateTime from, DateTime to);

    #endregion

    #region Manual Rules

    /// <summary>
    /// Creates an upsell rule.
    /// </summary>
    Task<UpsellRuleDto> CreateRuleAsync(CreateUpsellRuleRequest request);

    /// <summary>
    /// Updates an upsell rule.
    /// </summary>
    Task<UpsellRuleDto> UpdateRuleAsync(int ruleId, CreateUpsellRuleRequest request);

    /// <summary>
    /// Deletes an upsell rule (soft delete).
    /// </summary>
    Task DeleteRuleAsync(int ruleId);

    /// <summary>
    /// Gets all upsell rules.
    /// </summary>
    Task<List<UpsellRuleDto>> GetRulesAsync(bool includeInactive = false, int? storeId = null);

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    Task<UpsellRuleDto?> GetRuleAsync(int ruleId);

    /// <summary>
    /// Gets rules for a specific product.
    /// </summary>
    Task<List<UpsellRuleDto>> GetRulesForProductAsync(int productId);

    /// <summary>
    /// Enables a rule.
    /// </summary>
    Task EnableRuleAsync(int ruleId);

    /// <summary>
    /// Disables a rule.
    /// </summary>
    Task DisableRuleAsync(int ruleId);

    #endregion

    #region Customer Preferences

    /// <summary>
    /// Updates preferences for a customer (recalculates from history).
    /// </summary>
    Task UpdateCustomerPreferencesAsync(int customerId);

    /// <summary>
    /// Updates preferences for all customers (background job).
    /// </summary>
    Task UpdateAllCustomerPreferencesAsync();

    /// <summary>
    /// Gets preferences for a customer.
    /// </summary>
    Task<List<CustomerPreferenceDto>> GetCustomerPreferencesAsync(int customerId, int top = 10);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets upsell analytics for a date range.
    /// </summary>
    Task<UpsellAnalytics> GetAnalyticsAsync(DateTime from, DateTime to, int? storeId = null);

    /// <summary>
    /// Gets performance report.
    /// </summary>
    Task<UpsellPerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to, int? storeId = null);

    /// <summary>
    /// Gets top upsell products.
    /// </summary>
    Task<List<TopUpsellProduct>> GetTopUpsellProductsAsync(DateTime from, DateTime to, int top = 10, int? storeId = null);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets upsell configuration.
    /// </summary>
    Task<UpsellConfigurationDto> GetConfigurationAsync(int? storeId = null);

    /// <summary>
    /// Updates upsell configuration.
    /// </summary>
    Task<UpsellConfigurationDto> UpdateConfigurationAsync(UpsellConfigurationDto config);

    #endregion
}

/// <summary>
/// Interface for the association mining background job.
/// </summary>
public interface IAssociationMiningJob
{
    /// <summary>
    /// Gets the last rebuild result.
    /// </summary>
    AssociationRebuildResult? LastResult { get; }

    /// <summary>
    /// Triggers an immediate rebuild.
    /// </summary>
    Task TriggerRebuildAsync();
}

/// <summary>
/// Interface for the customer preference update job.
/// </summary>
public interface ICustomerPreferenceJob
{
    /// <summary>
    /// Gets the last run time.
    /// </summary>
    DateTime? LastRunTime { get; }

    /// <summary>
    /// Gets the count of preferences updated in last run.
    /// </summary>
    int LastUpdatedCount { get; }

    /// <summary>
    /// Triggers an immediate update.
    /// </summary>
    Task TriggerUpdateAsync();
}
