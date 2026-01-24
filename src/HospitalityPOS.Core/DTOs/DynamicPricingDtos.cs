using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Configuration DTOs

/// <summary>
/// Dynamic pricing configuration DTO.
/// </summary>
public class DynamicPricingConfigurationDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public bool EnableDynamicPricing { get; set; }
    public bool RequireManagerApproval { get; set; }
    public decimal MaxPriceIncreasePercent { get; set; }
    public decimal MaxPriceDecreasePercent { get; set; }
    public int PriceUpdateIntervalMinutes { get; set; }
    public bool ShowOriginalPrice { get; set; }
    public bool NotifyOnPriceChange { get; set; }
    public decimal MinMarginPercent { get; set; }
}

#endregion

#region Rule DTOs

/// <summary>
/// Dynamic pricing rule DTO.
/// </summary>
public class DynamicPricingRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DynamicPricingTrigger Trigger { get; set; }
    public PriceAdjustmentType AdjustmentType { get; set; }
    public decimal AdjustmentValue { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool AppliesToAllProducts { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresApproval { get; set; }
    public int? StoreId { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    // Time-based conditions
    public TimeOnly? ActiveFromTime { get; set; }
    public TimeOnly? ActiveToTime { get; set; }
    public List<DayOfWeek> ActiveDays { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Demand-based conditions
    public decimal? DemandThresholdHigh { get; set; }
    public decimal? DemandThresholdLow { get; set; }

    // Inventory-based conditions
    public int? StockThresholdLow { get; set; }
    public int? StockThresholdHigh { get; set; }
    public int? DaysToExpiry { get; set; }

    // Weather/Event conditions
    public string? WeatherCondition { get; set; }
    public string? EventName { get; set; }

    public List<int> ExcludedProductIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create a dynamic pricing rule.
/// </summary>
public class CreateDynamicPricingRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DynamicPricingTrigger Trigger { get; set; }
    public PriceAdjustmentType AdjustmentType { get; set; }
    public decimal AdjustmentValue { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public bool AppliesToAllProducts { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public int? StoreId { get; set; }

    // Time-based conditions
    public TimeOnly? ActiveFromTime { get; set; }
    public TimeOnly? ActiveToTime { get; set; }
    public List<DayOfWeek> ActiveDays { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Demand-based conditions
    public decimal? DemandThresholdHigh { get; set; }
    public decimal? DemandThresholdLow { get; set; }

    // Inventory-based conditions
    public int? StockThresholdLow { get; set; }
    public int? StockThresholdHigh { get; set; }
    public int? DaysToExpiry { get; set; }

    // Weather/Event conditions
    public string? WeatherCondition { get; set; }
    public string? EventName { get; set; }

    public List<int> ExcludedProductIds { get; set; } = new();
}

#endregion

#region Price DTOs

/// <summary>
/// Current dynamic price for a product.
/// </summary>
public class DynamicPriceDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal AdjustmentPercent { get; set; }
    public bool IsAdjusted { get; set; }
    public int? AppliedRuleId { get; set; }
    public string? AppliedRuleName { get; set; }
    public string? AdjustmentReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Context for calculating dynamic prices.
/// </summary>
public class DynamicPricingContext
{
    public DateTime CurrentTime { get; set; } = DateTime.Now;
    public DayOfWeek DayOfWeek { get; set; } = DateTime.Now.DayOfWeek;
    public int? CurrentStockLevel { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? RecentDemandLevel { get; set; }
    public string? WeatherCondition { get; set; }
    public bool IsSpecialEvent { get; set; }
    public string? EventName { get; set; }
    public int StoreId { get; set; }
}

/// <summary>
/// Result of a batch price update operation.
/// </summary>
public class PriceUpdateResult
{
    public int ProductsEvaluated { get; set; }
    public int PricesChanged { get; set; }
    public int PricesIncreased { get; set; }
    public int PricesDecreased { get; set; }
    public int PendingApprovals { get; set; }
    public List<PriceChangePreview> Changes { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Preview of a price change.
/// </summary>
public class PriceChangePreview
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public int? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
}

#endregion

#region Pending Changes DTOs

/// <summary>
/// Pending price change DTO.
/// </summary>
public class PendingPriceChangeDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ProposedPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public string? Reason { get; set; }
    public PriceChangeStatus Status { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Request to approve a price change.
/// </summary>
public class ApprovePriceChangeRequest
{
    public int PendingChangeId { get; set; }
    public int ApproverUserId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to reject a price change.
/// </summary>
public class RejectPriceChangeRequest
{
    public int PendingChangeId { get; set; }
    public int RejecterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

#endregion

#region Simulation DTOs

/// <summary>
/// Simulation of a price change impact.
/// </summary>
public class PricingSimulation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal ProposedPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal EstimatedDemandChange { get; set; }
    public decimal EstimatedRevenueChange { get; set; }
    public decimal EstimatedProfitChange { get; set; }
    public decimal PriceElasticity { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public List<string> Risks { get; set; } = new();
    public List<string> Opportunities { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Product affected by a pricing rule.
/// </summary>
public class AffectedProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ProjectedPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
}

#endregion

#region Analytics DTOs

/// <summary>
/// Dynamic pricing analytics summary.
/// </summary>
public class DynamicPricingAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalPriceChanges { get; set; }
    public int ProductsAffected { get; set; }
    public int RulesApplied { get; set; }
    public decimal TotalRevenueImpact { get; set; }
    public decimal AverageAdjustmentPercent { get; set; }
    public int PriceIncreases { get; set; }
    public int PriceDecreases { get; set; }
    public List<RulePerformance> TopPerformingRules { get; set; } = new();
    public List<DailyPricingMetrics> DailyMetrics { get; set; } = new();
}

/// <summary>
/// Daily pricing metrics.
/// </summary>
public class DailyPricingMetrics
{
    public DateTime Date { get; set; }
    public int TotalPriceChanges { get; set; }
    public int ProductsAffected { get; set; }
    public decimal AverageAdjustmentPercent { get; set; }
    public decimal EstimatedRevenueImpact { get; set; }
}

/// <summary>
/// Rule performance metrics.
/// </summary>
public class RulePerformance
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int TimesApplied { get; set; }
    public int ProductsAffected { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal EstimatedRevenueImpact { get; set; }
    public decimal EffectivenessScore { get; set; }
}

/// <summary>
/// Price elasticity report for a product.
/// </summary>
public class PriceElasticityReport
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal Elasticity { get; set; }
    public decimal OptimalPrice { get; set; }
    public decimal PotentialRevenueIncrease { get; set; }
    public List<PricePoint> HistoricalPricePoints { get; set; } = new();
    public string ElasticityCategory { get; set; } = string.Empty; // Elastic, Inelastic, Unit Elastic
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Historical price point with demand data.
/// </summary>
public class PricePoint
{
    public decimal Price { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>
/// Revenue impact report for a rule.
/// </summary>
public class RevenueImpactReport
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenueWithRule { get; set; }
    public decimal EstimatedRevenueWithoutRule { get; set; }
    public decimal RevenueImpact { get; set; }
    public decimal ImpactPercent { get; set; }
    public int TimesApplied { get; set; }
    public int ProductsAffected { get; set; }
    public List<ProductImpact> ProductImpacts { get; set; } = new();
}

/// <summary>
/// Revenue impact for a specific product.
/// </summary>
public class ProductImpact
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal RevenueWithRule { get; set; }
    public decimal EstimatedRevenueWithoutRule { get; set; }
    public decimal Impact { get; set; }
    public int QuantitySold { get; set; }
}

#endregion

#region Background Job DTOs

/// <summary>
/// Result of the dynamic pricing background job.
/// </summary>
public class DynamicPricingJobResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ProductsEvaluated { get; set; }
    public int PricesUpdated { get; set; }
    public int PendingApprovalsCreated { get; set; }
    public int RulesEvaluated { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;
}

/// <summary>
/// Result of the expiry pricing job.
/// </summary>
public class ExpiryPricingJobResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ProductsWithExpiringBatches { get; set; }
    public int PricesDiscounted { get; set; }
    public decimal TotalDiscountValue { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;
}

#endregion
