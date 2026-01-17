// src/HospitalityPOS.Core/Interfaces/ITimePricingService.cs
// Interface for Time-Based Pricing Service
// Story 44-2: Happy Hour / Time-Based Pricing

using HospitalityPOS.Core.Models.Pricing;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for time-based pricing (Happy Hour, Lunch Specials, etc.)
/// Manages pricing rules and calculates effective prices based on time of day.
/// </summary>
public interface ITimePricingService
{
    #region Rule Management

    /// <summary>
    /// Creates a new time pricing rule.
    /// </summary>
    /// <param name="rule">Rule to create.</param>
    /// <returns>Created rule with ID.</returns>
    Task<TimePricingRule> CreateRuleAsync(TimePricingRule rule);

    /// <summary>
    /// Updates an existing time pricing rule.
    /// </summary>
    /// <param name="rule">Rule to update.</param>
    /// <returns>Updated rule.</returns>
    Task<TimePricingRule> UpdateRuleAsync(TimePricingRule rule);

    /// <summary>
    /// Deletes a time pricing rule.
    /// </summary>
    /// <param name="ruleId">Rule ID to delete.</param>
    Task DeleteRuleAsync(int ruleId);

    /// <summary>
    /// Gets a rule by ID.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <returns>Rule or null if not found.</returns>
    Task<TimePricingRule?> GetRuleAsync(int ruleId);

    /// <summary>
    /// Gets all time pricing rules.
    /// </summary>
    /// <returns>List of all rules.</returns>
    Task<IReadOnlyList<TimePricingRule>> GetAllRulesAsync();

    /// <summary>
    /// Gets only active rules.
    /// </summary>
    /// <returns>List of active rules.</returns>
    Task<IReadOnlyList<TimePricingRule>> GetActiveRulesAsync();

    /// <summary>
    /// Enables or disables a rule.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <param name="isActive">Active state.</param>
    Task SetRuleActiveAsync(int ruleId, bool isActive);

    #endregion

    #region Price Calculation

    /// <summary>
    /// Gets the effective price for a product at the current time.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <param name="regularPrice">Regular product price.</param>
    /// <returns>Pricing info including any active discount.</returns>
    Task<ProductPricingInfo> GetEffectivePriceAsync(int productId, int categoryId, decimal regularPrice);

    /// <summary>
    /// Gets the effective price for a product at a specific time.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <param name="regularPrice">Regular product price.</param>
    /// <param name="atTime">Time to check.</param>
    /// <returns>Pricing info including any active discount.</returns>
    Task<ProductPricingInfo> GetEffectivePriceAtTimeAsync(int productId, int categoryId, decimal regularPrice, DateTime atTime);

    /// <summary>
    /// Gets effective prices for multiple products at the current time.
    /// </summary>
    /// <param name="products">List of products (ID, CategoryID, RegularPrice).</param>
    /// <returns>List of pricing info for each product.</returns>
    Task<IReadOnlyList<ProductPricingInfo>> GetEffectivePricesAsync(
        IEnumerable<(int ProductId, int CategoryId, decimal RegularPrice, string ProductName)> products);

    /// <summary>
    /// Checks if a specific product has an active time-based discount.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <returns>True if discount is active.</returns>
    Task<bool> HasActiveDiscountAsync(int productId, int categoryId);

    #endregion

    #region Active Promotions

    /// <summary>
    /// Gets the current active promotions status.
    /// </summary>
    /// <returns>Active promotion status.</returns>
    Task<ActivePromotionStatus> GetActivePromotionsAsync();

    /// <summary>
    /// Gets rules that are currently active at this moment.
    /// </summary>
    /// <returns>List of currently active rules.</returns>
    Task<IReadOnlyList<TimePricingRule>> GetCurrentlyActiveRulesAsync();

    /// <summary>
    /// Gets rules that apply to a specific product.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <returns>List of applicable rules.</returns>
    Task<IReadOnlyList<TimePricingRule>> GetRulesForProductAsync(int productId, int categoryId);

    /// <summary>
    /// Gets product IDs affected by currently active rules.
    /// </summary>
    /// <returns>Set of affected product IDs.</returns>
    Task<IReadOnlySet<int>> GetAffectedProductIdsAsync();

    /// <summary>
    /// Gets category IDs affected by currently active rules.
    /// </summary>
    /// <returns>Set of affected category IDs.</returns>
    Task<IReadOnlySet<int>> GetAffectedCategoryIdsAsync();

    #endregion

    #region Order Integration

    /// <summary>
    /// Applies time-based pricing to order items.
    /// </summary>
    /// <param name="items">Order items with regular prices.</param>
    /// <returns>Order items with time-based pricing applied.</returns>
    Task<IReadOnlyList<TimePricedOrderItem>> ApplyTimePricingToOrderAsync(
        IEnumerable<(int ProductId, int CategoryId, string ProductName, decimal RegularPrice, int Quantity)> items);

    /// <summary>
    /// Records time-based discounts applied to an order (for analytics).
    /// </summary>
    /// <param name="orderId">Order ID.</param>
    /// <param name="items">Order items with time pricing applied.</param>
    Task RecordTimePricingDiscountsAsync(int orderId, IEnumerable<TimePricedOrderItem> items);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets time pricing analytics for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Analytics data.</returns>
    Task<TimePricingAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets today's time pricing analytics.
    /// </summary>
    /// <returns>Today's analytics.</returns>
    Task<TimePricingAnalytics> GetTodayAnalyticsAsync();

    /// <summary>
    /// Gets performance data for a specific rule.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Performance data.</returns>
    Task<PromotionPerformance> GetRulePerformanceAsync(int ruleId, DateTime startDate, DateTime endDate);

    #endregion

    #region Time Management

    /// <summary>
    /// Gets time until next promotion starts.
    /// </summary>
    /// <returns>TimeSpan until next activation, or null if none scheduled.</returns>
    Task<TimeSpan?> GetTimeUntilNextPromotionAsync();

    /// <summary>
    /// Gets time until current promotion ends.
    /// </summary>
    /// <returns>TimeSpan until end, or null if none active.</returns>
    Task<TimeSpan?> GetTimeUntilPromotionEndsAsync();

    /// <summary>
    /// Checks for promotion changes and raises events if needed.
    /// Call this periodically (e.g., every minute) to detect time-based changes.
    /// </summary>
    Task CheckPromotionChangesAsync();

    #endregion

    #region Events

    /// <summary>
    /// Raised when promotion activation status changes.
    /// </summary>
    event EventHandler<PromotionActivationChangedEventArgs>? PromotionActivationChanged;

    /// <summary>
    /// Raised when a rule is about to start (configurable lead time).
    /// </summary>
    event EventHandler<TimePricingRule>? PromotionStarting;

    /// <summary>
    /// Raised when a rule is about to end (configurable lead time).
    /// </summary>
    event EventHandler<TimePricingRule>? PromotionEnding;

    #endregion
}
