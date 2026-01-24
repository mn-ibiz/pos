using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Trigger types for dynamic pricing rules.
/// </summary>
public enum DynamicPricingTrigger
{
    /// <summary>Happy hour, lunch special.</summary>
    TimeOfDay = 1,

    /// <summary>Weekend pricing.</summary>
    DayOfWeek = 2,

    /// <summary>Holiday pricing.</summary>
    DateRange = 3,

    /// <summary>Surge pricing based on demand.</summary>
    DemandLevel = 4,

    /// <summary>Low stock premium / overstock discount.</summary>
    StockLevel = 5,

    /// <summary>Clearance pricing for items approaching expiry.</summary>
    ExpiryApproaching = 6,

    /// <summary>Weather-based pricing (hot day = cold drinks discount).</summary>
    WeatherCondition = 7,

    /// <summary>Match day pricing, special events.</summary>
    SpecialEvent = 8,

    /// <summary>Multiple conditions combined.</summary>
    Combination = 9
}

/// <summary>
/// Types of price adjustments.
/// </summary>
public enum PriceAdjustmentType
{
    /// <summary>Percentage discount (-10%).</summary>
    PercentageDiscount = 1,

    /// <summary>Percentage increase (+15%).</summary>
    PercentageIncrease = 2,

    /// <summary>Fixed discount (-50 KSh).</summary>
    FixedDiscount = 3,

    /// <summary>Fixed increase (+100 KSh).</summary>
    FixedIncrease = 4,

    /// <summary>Set to specific price.</summary>
    SetPrice = 5,

    /// <summary>Round to nearest amount (e.g., 50 KSh).</summary>
    RoundTo = 6
}

/// <summary>
/// Status of a pending price change.
/// </summary>
public enum PriceChangeStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Applied = 5
}

#endregion

#region Configuration

/// <summary>
/// Store-level configuration for dynamic pricing.
/// </summary>
public class DynamicPricingConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Whether dynamic pricing is enabled for this store.
    /// </summary>
    public bool EnableDynamicPricing { get; set; }

    /// <summary>
    /// Whether manager approval is required for price changes.
    /// </summary>
    public bool RequireManagerApproval { get; set; }

    /// <summary>
    /// Maximum allowed price increase percentage (safety limit).
    /// </summary>
    public decimal MaxPriceIncreasePercent { get; set; } = 25m;

    /// <summary>
    /// Maximum allowed price decrease percentage (safety limit).
    /// </summary>
    public decimal MaxPriceDecreasePercent { get; set; } = 50m;

    /// <summary>
    /// How often to recalculate prices in minutes.
    /// </summary>
    public int PriceUpdateIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to show original price with strike-through.
    /// </summary>
    public bool ShowOriginalPrice { get; set; } = true;

    /// <summary>
    /// Whether to notify on price changes.
    /// </summary>
    public bool NotifyOnPriceChange { get; set; } = true;

    /// <summary>
    /// Minimum margin percentage to maintain.
    /// </summary>
    public decimal MinMarginPercent { get; set; } = 10m;

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion

#region Pricing Rules

/// <summary>
/// Dynamic pricing rule configuration.
/// </summary>
public class DynamicPricingRule : BaseEntity
{
    /// <summary>
    /// Rule name (e.g., "Happy Hour", "Weekend Premium").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// What triggers this rule.
    /// </summary>
    public DynamicPricingTrigger Trigger { get; set; }

    /// <summary>
    /// How the price is adjusted.
    /// </summary>
    public PriceAdjustmentType AdjustmentType { get; set; }

    /// <summary>
    /// Adjustment value (percentage or fixed amount).
    /// </summary>
    public decimal AdjustmentValue { get; set; }

    /// <summary>
    /// Floor price - minimum allowed price.
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Ceiling price - maximum allowed price.
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Specific product this rule applies to (null = category or all).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Category this rule applies to (null = all or specific product).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Whether this rule applies to all products.
    /// </summary>
    public bool AppliesToAllProducts { get; set; }

    /// <summary>
    /// Priority (higher wins when multiple rules apply).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether the rule is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether price changes from this rule require approval.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Store this rule applies to (null = all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// User who created this rule.
    /// </summary>
    public int CreatedByUserId { get; set; }

    #region Time-based conditions

    /// <summary>
    /// Start time of day when rule is active.
    /// </summary>
    public TimeOnly? ActiveFromTime { get; set; }

    /// <summary>
    /// End time of day when rule is active.
    /// </summary>
    public TimeOnly? ActiveToTime { get; set; }

    /// <summary>
    /// Days of week when rule is active (comma-separated: 0-6).
    /// </summary>
    [MaxLength(20)]
    public string? ActiveDays { get; set; }

    /// <summary>
    /// Start date for date-range rules.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for date-range rules.
    /// </summary>
    public DateTime? EndDate { get; set; }

    #endregion

    #region Demand-based conditions

    /// <summary>
    /// Demand threshold above which to increase price.
    /// </summary>
    public decimal? DemandThresholdHigh { get; set; }

    /// <summary>
    /// Demand threshold below which to decrease price.
    /// </summary>
    public decimal? DemandThresholdLow { get; set; }

    #endregion

    #region Inventory-based conditions

    /// <summary>
    /// Stock level below which to apply rule (low stock premium).
    /// </summary>
    public int? StockThresholdLow { get; set; }

    /// <summary>
    /// Stock level above which to apply rule (overstock discount).
    /// </summary>
    public int? StockThresholdHigh { get; set; }

    /// <summary>
    /// Days to expiry threshold for expiry-based discounts.
    /// </summary>
    public int? DaysToExpiry { get; set; }

    #endregion

    #region Weather/Event conditions

    /// <summary>
    /// Weather condition that triggers this rule.
    /// </summary>
    [MaxLength(50)]
    public string? WeatherCondition { get; set; }

    /// <summary>
    /// Special event name that triggers this rule.
    /// </summary>
    [MaxLength(100)]
    public string? EventName { get; set; }

    #endregion

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Category? Category { get; set; }
    public virtual Store? Store { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<DynamicPricingException> Exceptions { get; set; } = new List<DynamicPricingException>();
}

/// <summary>
/// Product exception from a pricing rule.
/// </summary>
public class DynamicPricingException : BaseEntity
{
    /// <summary>
    /// Rule this exception belongs to.
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// Product excluded from the rule.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Reason for exception.
    /// </summary>
    [MaxLength(200)]
    public string? Reason { get; set; }

    // Navigation properties
    public virtual DynamicPricingRule? Rule { get; set; }
    public virtual Product? Product { get; set; }
}

#endregion

#region Price Logging

/// <summary>
/// Log of dynamic price changes.
/// </summary>
public class DynamicPriceLog : BaseEntity
{
    /// <summary>
    /// Product affected.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Rule that caused the change (null for manual changes).
    /// </summary>
    public int? RuleId { get; set; }

    /// <summary>
    /// Original base price.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// New adjusted price.
    /// </summary>
    public decimal AdjustedPrice { get; set; }

    /// <summary>
    /// Amount of adjustment.
    /// </summary>
    public decimal AdjustmentAmount { get; set; }

    /// <summary>
    /// Percentage of adjustment.
    /// </summary>
    public decimal AdjustmentPercent { get; set; }

    /// <summary>
    /// Reason for the price change.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// When the price was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// When this price adjustment expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User who approved this change (if approval required).
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Store where price was applied.
    /// </summary>
    public int StoreId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual DynamicPricingRule? Rule { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Pending price change awaiting approval.
/// </summary>
public class PendingPriceChange : BaseEntity
{
    /// <summary>
    /// Product to change price for.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Rule that triggered this change (null for manual).
    /// </summary>
    public int? RuleId { get; set; }

    /// <summary>
    /// Current price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Proposed new price.
    /// </summary>
    public decimal ProposedPrice { get; set; }

    /// <summary>
    /// Reason for the change.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public PriceChangeStatus Status { get; set; } = PriceChangeStatus.Pending;

    /// <summary>
    /// User who requested the change.
    /// </summary>
    public int RequestedByUserId { get; set; }

    /// <summary>
    /// User who approved/rejected the change.
    /// </summary>
    public int? ReviewedByUserId { get; set; }

    /// <summary>
    /// When the request was made.
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// When the request was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Rejection reason if rejected.
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When this request expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Store for this price change.
    /// </summary>
    public int StoreId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual DynamicPricingRule? Rule { get; set; }
    public virtual User? RequestedByUser { get; set; }
    public virtual User? ReviewedByUser { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Current Prices

/// <summary>
/// Current dynamic price for a product (cached for fast lookup).
/// </summary>
public class CurrentDynamicPrice : BaseEntity
{
    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Base price from product.
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Current adjusted price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Rule that applied this price (null if base price).
    /// </summary>
    public int? AppliedRuleId { get; set; }

    /// <summary>
    /// When this price was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// When this price expires (needs recalculation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether price is currently adjusted from base.
    /// </summary>
    public bool IsAdjusted { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
    public virtual DynamicPricingRule? AppliedRule { get; set; }
}

#endregion

#region Analytics

/// <summary>
/// Daily dynamic pricing metrics.
/// </summary>
public class DynamicPricingDailyMetrics : BaseEntity
{
    /// <summary>
    /// Date of metrics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Total products with dynamic pricing applied.
    /// </summary>
    public int ProductsWithDynamicPricing { get; set; }

    /// <summary>
    /// Total price changes made.
    /// </summary>
    public int TotalPriceChanges { get; set; }

    /// <summary>
    /// Number of price increases.
    /// </summary>
    public int PriceIncreases { get; set; }

    /// <summary>
    /// Number of price decreases.
    /// </summary>
    public int PriceDecreases { get; set; }

    /// <summary>
    /// Average adjustment percentage.
    /// </summary>
    public decimal AverageAdjustmentPercent { get; set; }

    /// <summary>
    /// Estimated additional revenue from dynamic pricing.
    /// </summary>
    public decimal EstimatedRevenueImpact { get; set; }

    /// <summary>
    /// Number of rules active during this day.
    /// </summary>
    public int ActiveRulesCount { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Rule-level performance metrics.
/// </summary>
public class DynamicPricingRuleMetrics : BaseEntity
{
    /// <summary>
    /// Rule ID.
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// Date of metrics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Number of times rule was applied.
    /// </summary>
    public int TimesApplied { get; set; }

    /// <summary>
    /// Number of products affected.
    /// </summary>
    public int ProductsAffected { get; set; }

    /// <summary>
    /// Total sales while rule was active.
    /// </summary>
    public decimal TotalSalesValue { get; set; }

    /// <summary>
    /// Number of items sold while rule was active.
    /// </summary>
    public int ItemsSold { get; set; }

    /// <summary>
    /// Estimated revenue impact from this rule.
    /// </summary>
    public decimal EstimatedRevenueImpact { get; set; }

    // Navigation properties
    public virtual DynamicPricingRule? Rule { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion
