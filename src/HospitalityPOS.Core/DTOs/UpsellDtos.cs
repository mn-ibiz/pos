using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Suggestion DTOs

/// <summary>
/// Context for generating upsell suggestions.
/// </summary>
public class UpsellContext
{
    /// <summary>
    /// Product IDs currently in the cart.
    /// </summary>
    public List<int> CartProductIds { get; set; } = new();

    /// <summary>
    /// Loyalty member ID if customer is identified.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Current cart total amount.
    /// </summary>
    public decimal CurrentTotal { get; set; }

    /// <summary>
    /// Current time of day.
    /// </summary>
    public TimeOfDay TimeOfDay { get; set; }

    /// <summary>
    /// Day of week.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Maximum suggestions to return.
    /// </summary>
    public int MaxSuggestions { get; set; } = 3;

    /// <summary>
    /// Whether to include personalized suggestions.
    /// </summary>
    public bool IncludePersonalized { get; set; } = true;

    /// <summary>
    /// Whether to include trending suggestions.
    /// </summary>
    public bool IncludeTrending { get; set; } = true;

    /// <summary>
    /// Store ID for store-specific filtering.
    /// </summary>
    public int? StoreId { get; set; }
}

/// <summary>
/// An upsell/cross-sell suggestion to display.
/// </summary>
public class UpsellSuggestion
{
    /// <summary>
    /// Suggested product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Suggestion text to display ("Customers also bought...").
    /// </summary>
    public string SuggestionText { get; set; } = string.Empty;

    /// <summary>
    /// Reason for this suggestion (for analytics).
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Type of upsell suggestion.
    /// </summary>
    public UpsellRuleType Type { get; set; }

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Savings amount for upgrades.
    /// </summary>
    public decimal? SavingsAmount { get; set; }

    /// <summary>
    /// Product that triggered this suggestion.
    /// </summary>
    public int? SourceProductId { get; set; }

    /// <summary>
    /// Association ID if association-based.
    /// </summary>
    public int? AssociationId { get; set; }

    /// <summary>
    /// Rule ID if rule-based.
    /// </summary>
    public int? RuleId { get; set; }

    /// <summary>
    /// Priority for display ordering.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Category ID for diversity filtering.
    /// </summary>
    public int? CategoryId { get; set; }
}

/// <summary>
/// A product recommendation.
/// </summary>
public class ProductRecommendation
{
    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Type of recommendation.
    /// </summary>
    public RecommendationType Type { get; set; }

    /// <summary>
    /// Reason for recommendation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score.
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Category ID.
    /// </summary>
    public int? CategoryId { get; set; }
}

#endregion

#region Association DTOs

/// <summary>
/// Product association DTO.
/// </summary>
public class ProductAssociationDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int AssociatedProductId { get; set; }
    public string AssociatedProductName { get; set; } = string.Empty;
    public AssociationType Type { get; set; }
    public decimal Support { get; set; }
    public decimal Confidence { get; set; }
    public decimal Lift { get; set; }
    public int TransactionCount { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Association rule from market basket analysis.
/// </summary>
public class AssociationRule
{
    /// <summary>
    /// Products on the left side of the rule (if customer buys these...).
    /// </summary>
    public List<int> Antecedent { get; set; } = new();

    /// <summary>
    /// Products on the right side (suggest these).
    /// </summary>
    public List<int> Consequent { get; set; } = new();

    /// <summary>
    /// Support: Proportion of transactions containing all items.
    /// </summary>
    public decimal Support { get; set; }

    /// <summary>
    /// Confidence: P(Consequent | Antecedent).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Lift: Improvement over random.
    /// </summary>
    public decimal Lift { get; set; }

    /// <summary>
    /// Number of transactions supporting this rule.
    /// </summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// Association metrics for a product pair.
/// </summary>
public class AssociationMetrics
{
    public int ProductAId { get; set; }
    public int ProductBId { get; set; }
    public decimal Support { get; set; }
    public decimal Confidence { get; set; }
    public decimal Lift { get; set; }
    public int CoOccurrenceCount { get; set; }
    public int ProductACount { get; set; }
    public int ProductBCount { get; set; }
    public int TotalTransactions { get; set; }
}

/// <summary>
/// Result of association rebuild job.
/// </summary>
public class AssociationRebuildResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TransactionsAnalyzed { get; set; }
    public int AssociationsFound { get; set; }
    public int AssociationsStored { get; set; }
    public int AssociationsRemoved { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;
}

#endregion

#region Rule DTOs

/// <summary>
/// Upsell rule DTO.
/// </summary>
public class UpsellRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SourceProductId { get; set; }
    public string? SourceProductName { get; set; }
    public int? SourceCategoryId { get; set; }
    public string? SourceCategoryName { get; set; }
    public int TargetProductId { get; set; }
    public string TargetProductName { get; set; } = string.Empty;
    public UpsellRuleType Type { get; set; }
    public string? SuggestionText { get; set; }
    public decimal? SavingsAmount { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxSuggestionsPerDay { get; set; }
    public TimeOfDay? TimeOfDayFilter { get; set; }
    public int? StoreId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create/update an upsell rule.
/// </summary>
public class CreateUpsellRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SourceProductId { get; set; }
    public int? SourceCategoryId { get; set; }
    public int TargetProductId { get; set; }
    public UpsellRuleType Type { get; set; }
    public string? SuggestionText { get; set; }
    public decimal? SavingsAmount { get; set; }
    public int Priority { get; set; } = 1;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxSuggestionsPerDay { get; set; }
    public TimeOfDay? TimeOfDayFilter { get; set; }
    public int? StoreId { get; set; }
}

#endregion

#region Customer Preference DTOs

/// <summary>
/// Customer preference DTO.
/// </summary>
public class CustomerPreferenceDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int PurchaseCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageQuantity { get; set; }
    public DateTime LastPurchased { get; set; }
    public DateTime FirstPurchased { get; set; }
    public decimal PreferenceScore { get; set; }
}

#endregion

#region Configuration DTOs

/// <summary>
/// Upsell configuration DTO.
/// </summary>
public class UpsellConfigurationDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public bool IsEnabled { get; set; }
    public int MaxSuggestions { get; set; }
    public decimal MinConfidenceScore { get; set; }
    public decimal MinSupport { get; set; }
    public decimal MinAssociationConfidence { get; set; }
    public decimal MinLift { get; set; }
    public int AnalysisDays { get; set; }
    public bool IncludePersonalized { get; set; }
    public bool IncludeTrending { get; set; }
    public bool EnforceCategoryDiversity { get; set; }
    public int ExcludeRecentPurchaseDays { get; set; }
    public decimal RuleWeight { get; set; }
    public decimal AssociationWeight { get; set; }
    public decimal PersonalizedWeight { get; set; }
    public decimal TrendingWeight { get; set; }
    public int TrendingDays { get; set; }
    public bool ShowSavingsAmount { get; set; }
    public string DefaultSuggestionText { get; set; } = string.Empty;
}

#endregion

#region Analytics DTOs

/// <summary>
/// Upsell analytics summary.
/// </summary>
public class UpsellAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Total suggestions shown.
    /// </summary>
    public int TotalSuggestions { get; set; }

    /// <summary>
    /// Total suggestions accepted.
    /// </summary>
    public int AcceptedSuggestions { get; set; }

    /// <summary>
    /// Overall acceptance rate.
    /// </summary>
    public decimal AcceptanceRate { get; set; }

    /// <summary>
    /// Total revenue from upsells.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average value per accepted suggestion.
    /// </summary>
    public decimal AverageOrderValueIncrease { get; set; }

    /// <summary>
    /// Breakdown by suggestion type.
    /// </summary>
    public List<SuggestionTypeMetrics> ByType { get; set; } = new();

    /// <summary>
    /// Daily trend data.
    /// </summary>
    public List<DailyUpsellMetrics> DailyTrend { get; set; } = new();
}

/// <summary>
/// Metrics by suggestion type.
/// </summary>
public class SuggestionTypeMetrics
{
    public UpsellRuleType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int Suggestions { get; set; }
    public int Accepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Daily upsell metrics.
/// </summary>
public class DailyUpsellMetrics
{
    public DateTime Date { get; set; }
    public int Suggestions { get; set; }
    public int Accepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Performance report for upsells.
/// </summary>
public class UpsellPerformanceReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Top performing products (most accepted).
    /// </summary>
    public List<TopUpsellProduct> TopProducts { get; set; } = new();

    /// <summary>
    /// Top performing rules.
    /// </summary>
    public List<TopUpsellRule> TopRules { get; set; } = new();

    /// <summary>
    /// Top performing associations.
    /// </summary>
    public List<TopAssociation> TopAssociations { get; set; } = new();

    /// <summary>
    /// Performance by time of day.
    /// </summary>
    public List<TimeOfDayPerformance> ByTimeOfDay { get; set; } = new();

    /// <summary>
    /// Performance by day of week.
    /// </summary>
    public List<DayOfWeekPerformance> ByDayOfWeek { get; set; } = new();
}

/// <summary>
/// Top upsell product metrics.
/// </summary>
public class TopUpsellProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TimesShown { get; set; }
    public int TimesAccepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Top upsell rule metrics.
/// </summary>
public class TopUpsellRule
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public UpsellRuleType Type { get; set; }
    public int TimesTriggered { get; set; }
    public int TimesAccepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Top association metrics.
/// </summary>
public class TopAssociation
{
    public int AssociationId { get; set; }
    public string SourceProductName { get; set; } = string.Empty;
    public string TargetProductName { get; set; } = string.Empty;
    public decimal Lift { get; set; }
    public int TimesShown { get; set; }
    public int TimesAccepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Performance by time of day.
/// </summary>
public class TimeOfDayPerformance
{
    public TimeOfDay TimeOfDay { get; set; }
    public string TimeOfDayName { get; set; } = string.Empty;
    public int Suggestions { get; set; }
    public int Accepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Performance by day of week.
/// </summary>
public class DayOfWeekPerformance
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Suggestions { get; set; }
    public int Accepted { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal Revenue { get; set; }
}

#endregion

#region Tracking DTOs

/// <summary>
/// Request to record a suggestion shown.
/// </summary>
public class RecordSuggestionRequest
{
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public UpsellRuleType SuggestionType { get; set; }
    public decimal ConfidenceScore { get; set; }
    public int? AssociationId { get; set; }
    public int? RuleId { get; set; }
    public int? SourceProductId { get; set; }
    public int? UserId { get; set; }
    public int? CustomerId { get; set; }
    public int? StoreId { get; set; }
}

/// <summary>
/// Request to record suggestion acceptance.
/// </summary>
public class RecordAcceptanceRequest
{
    public int SuggestionLogId { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
}

#endregion
