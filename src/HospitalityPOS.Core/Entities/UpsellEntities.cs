using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Types of product associations from market basket analysis.
/// </summary>
public enum AssociationType
{
    /// <summary>Products frequently bought together in same transaction.</summary>
    FrequentlyBoughtTogether = 1,

    /// <summary>Products bought in sequence within same session.</summary>
    SequentialPurchase = 2,

    /// <summary>Products from same category bought by same customers.</summary>
    CategoryAffinity = 3,

    /// <summary>Products at similar price points bought together.</summary>
    PriceRangeAffinity = 4
}

/// <summary>
/// Types of upsell/cross-sell rules.
/// </summary>
public enum UpsellRuleType
{
    /// <summary>Add this item to the order.</summary>
    Addon = 1,

    /// <summary>Upgrade to larger size.</summary>
    SizeUpgrade = 2,

    /// <summary>Upgrade to combo/meal deal.</summary>
    ComboUpgrade = 3,

    /// <summary>Upgrade to premium version.</summary>
    Premium = 4,

    /// <summary>Complementary product that goes well with.</summary>
    Complementary = 5,

    /// <summary>Trending/popular item right now.</summary>
    Trending = 6,

    /// <summary>Based on customer's purchase history.</summary>
    Personalized = 7
}

/// <summary>
/// Type of product recommendation.
/// </summary>
public enum RecommendationType
{
    /// <summary>Frequently bought with items in cart.</summary>
    FrequentlyBought = 1,

    /// <summary>Customer's personal favorite.</summary>
    PersonalFavorite = 2,

    /// <summary>Currently trending/popular.</summary>
    Trending = 3,

    /// <summary>Recently added to menu.</summary>
    NewArrival = 4,

    /// <summary>Seasonal recommendation.</summary>
    SeasonalPick = 5,

    /// <summary>Staff recommended item.</summary>
    StaffPick = 6
}

/// <summary>
/// Time of day for suggestion filtering.
/// </summary>
public enum TimeOfDay
{
    Breakfast = 1,
    Lunch = 2,
    Afternoon = 3,
    Dinner = 4,
    LateNight = 5
}

#endregion

#region Product Association

/// <summary>
/// Product association discovered through market basket analysis.
/// Represents the relationship between products that are frequently purchased together.
/// </summary>
public class ProductAssociation : BaseEntity
{
    /// <summary>
    /// The source product ID (if customer buys this...).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The associated/suggested product ID (...suggest this).
    /// </summary>
    public int AssociatedProductId { get; set; }

    /// <summary>
    /// Type of association discovered.
    /// </summary>
    public AssociationType Type { get; set; }

    /// <summary>
    /// Support: Percentage of transactions containing both products (0-1).
    /// </summary>
    public decimal Support { get; set; }

    /// <summary>
    /// Confidence: Probability of buying associated product given source product (0-1).
    /// P(Associated | Product)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Lift: Improvement over random selection (>1 indicates positive association).
    /// Lift = Confidence / P(Associated)
    /// </summary>
    public decimal Lift { get; set; }

    /// <summary>
    /// Number of transactions used to calculate this association.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// When this association was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Analysis period start date.
    /// </summary>
    public DateTime AnalysisStartDate { get; set; }

    /// <summary>
    /// Analysis period end date.
    /// </summary>
    public DateTime AnalysisEndDate { get; set; }

    /// <summary>
    /// Store ID for store-specific associations, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Product? AssociatedProduct { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Upsell Rules

/// <summary>
/// Manual upsell/cross-sell rule created by admin.
/// </summary>
public class UpsellRule : BaseEntity
{
    /// <summary>
    /// Rule name for admin reference.
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
    /// Source product ID that triggers this rule (null for category-based).
    /// </summary>
    public int? SourceProductId { get; set; }

    /// <summary>
    /// Source category ID that triggers this rule (null for product-based).
    /// </summary>
    public int? SourceCategoryId { get; set; }

    /// <summary>
    /// Target product to suggest.
    /// </summary>
    public int TargetProductId { get; set; }

    /// <summary>
    /// Type of upsell suggestion.
    /// </summary>
    public UpsellRuleType Type { get; set; }

    /// <summary>
    /// Custom suggestion text shown to staff.
    /// Supports variables: {{ProductName}}, {{Price}}, {{Savings}}
    /// </summary>
    [MaxLength(200)]
    public string? SuggestionText { get; set; }

    /// <summary>
    /// Savings amount for combo/upgrade suggestions.
    /// </summary>
    public decimal? SavingsAmount { get; set; }

    /// <summary>
    /// Priority for sorting (higher = shown first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional start date for time-limited rules.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional end date for time-limited rules.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum suggestions per day (rate limiting).
    /// </summary>
    public int? MaxSuggestionsPerDay { get; set; }

    /// <summary>
    /// Count of suggestions made today (reset daily).
    /// </summary>
    public int TodaySuggestionCount { get; set; }

    /// <summary>
    /// Last date suggestion count was reset.
    /// </summary>
    public DateTime? LastCountResetDate { get; set; }

    /// <summary>
    /// Time of day filter (null for all day).
    /// </summary>
    public TimeOfDay? TimeOfDayFilter { get; set; }

    /// <summary>
    /// Store ID for store-specific rules, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Product? SourceProduct { get; set; }
    public virtual Category? SourceCategory { get; set; }
    public virtual Product? TargetProduct { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Suggestion Logging

/// <summary>
/// Log of upsell suggestions shown and their outcomes.
/// </summary>
public class UpsellSuggestionLog : BaseEntity
{
    /// <summary>
    /// Receipt this suggestion was made for.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Product that was suggested.
    /// </summary>
    public int SuggestedProductId { get; set; }

    /// <summary>
    /// Product association that triggered this (if association-based).
    /// </summary>
    public int? AssociationId { get; set; }

    /// <summary>
    /// Upsell rule that triggered this (if rule-based).
    /// </summary>
    public int? RuleId { get; set; }

    /// <summary>
    /// Type of suggestion made.
    /// </summary>
    public UpsellRuleType SuggestionType { get; set; }

    /// <summary>
    /// Confidence score of this suggestion (0-1).
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Whether the suggestion was accepted by customer.
    /// </summary>
    public bool? WasAccepted { get; set; }

    /// <summary>
    /// When the suggestion was made.
    /// </summary>
    public DateTime SuggestedAt { get; set; }

    /// <summary>
    /// When the outcome (accept/reject) was recorded.
    /// </summary>
    public DateTime? OutcomeRecordedAt { get; set; }

    /// <summary>
    /// Quantity accepted (if accepted).
    /// </summary>
    public int? AcceptedQuantity { get; set; }

    /// <summary>
    /// Value of accepted items.
    /// </summary>
    public decimal? AcceptedValue { get; set; }

    /// <summary>
    /// User who was shown this suggestion.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Customer ID if customer was identified.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Source product(s) that triggered this suggestion.
    /// </summary>
    [MaxLength(200)]
    public string? TriggerProductIds { get; set; }

    /// <summary>
    /// Store where suggestion was made.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Receipt? Receipt { get; set; }
    public virtual Product? SuggestedProduct { get; set; }
    public virtual ProductAssociation? Association { get; set; }
    public virtual UpsellRule? Rule { get; set; }
    public virtual User? User { get; set; }
    public virtual LoyaltyMember? Customer { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Customer Preferences

/// <summary>
/// Customer product preferences calculated from purchase history.
/// </summary>
public class CustomerPreference : BaseEntity
{
    /// <summary>
    /// Loyalty member ID.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Product the customer has purchased.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Number of times this product was purchased.
    /// </summary>
    public int PurchaseCount { get; set; }

    /// <summary>
    /// Total amount spent on this product.
    /// </summary>
    public decimal TotalSpent { get; set; }

    /// <summary>
    /// Average quantity per purchase.
    /// </summary>
    public decimal AverageQuantity { get; set; }

    /// <summary>
    /// Date of most recent purchase.
    /// </summary>
    public DateTime LastPurchased { get; set; }

    /// <summary>
    /// First purchase date.
    /// </summary>
    public DateTime FirstPurchased { get; set; }

    /// <summary>
    /// Calculated preference score (0-1, normalized).
    /// Based on recency, frequency, and value.
    /// </summary>
    public decimal PreferenceScore { get; set; }

    /// <summary>
    /// When this preference was last calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Store ID for store-specific preferences, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual LoyaltyMember? Customer { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Configuration

/// <summary>
/// Configuration settings for the upsell recommendation system.
/// </summary>
public class UpsellConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID for store-specific config, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether upsell suggestions are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Maximum suggestions to show at once.
    /// </summary>
    public int MaxSuggestions { get; set; } = 3;

    /// <summary>
    /// Minimum confidence score to show suggestion (0-1).
    /// </summary>
    public decimal MinConfidenceScore { get; set; } = 0.3m;

    /// <summary>
    /// Minimum support threshold for associations (0-1).
    /// </summary>
    public decimal MinSupport { get; set; } = 0.01m;

    /// <summary>
    /// Minimum confidence for associations (0-1).
    /// </summary>
    public decimal MinAssociationConfidence { get; set; } = 0.25m;

    /// <summary>
    /// Minimum lift for associations (>1).
    /// </summary>
    public decimal MinLift { get; set; } = 1.2m;

    /// <summary>
    /// Days of transaction history to analyze.
    /// </summary>
    public int AnalysisDays { get; set; } = 90;

    /// <summary>
    /// Whether to include personalized suggestions.
    /// </summary>
    public bool IncludePersonalized { get; set; } = true;

    /// <summary>
    /// Whether to include trending suggestions.
    /// </summary>
    public bool IncludeTrending { get; set; } = true;

    /// <summary>
    /// Maximum one suggestion per category (diversity).
    /// </summary>
    public bool EnforceCategoryDiversity { get; set; } = true;

    /// <summary>
    /// Exclude items purchased by customer in last N days.
    /// </summary>
    public int ExcludeRecentPurchaseDays { get; set; } = 7;

    /// <summary>
    /// Weight for rule-based suggestions in ranking.
    /// </summary>
    public decimal RuleWeight { get; set; } = 1.5m;

    /// <summary>
    /// Weight for association-based suggestions.
    /// </summary>
    public decimal AssociationWeight { get; set; } = 1.0m;

    /// <summary>
    /// Weight for personalized suggestions.
    /// </summary>
    public decimal PersonalizedWeight { get; set; } = 1.2m;

    /// <summary>
    /// Weight for trending suggestions.
    /// </summary>
    public decimal TrendingWeight { get; set; } = 0.8m;

    /// <summary>
    /// Days to consider for trending calculation.
    /// </summary>
    public int TrendingDays { get; set; } = 7;

    /// <summary>
    /// Whether to show savings amount for upgrades.
    /// </summary>
    public bool ShowSavingsAmount { get; set; } = true;

    /// <summary>
    /// Default suggestion text template.
    /// </summary>
    [MaxLength(200)]
    public string DefaultSuggestionText { get; set; } = "Customers also bought {{ProductName}}";

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion

#region Analytics Aggregates

/// <summary>
/// Daily aggregated upsell performance metrics.
/// </summary>
public class UpsellDailyMetrics : BaseEntity
{
    /// <summary>
    /// Date of the metrics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID for store-specific metrics, null for global.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Total number of suggestions shown.
    /// </summary>
    public int TotalSuggestions { get; set; }

    /// <summary>
    /// Number of suggestions accepted.
    /// </summary>
    public int AcceptedSuggestions { get; set; }

    /// <summary>
    /// Number of suggestions rejected.
    /// </summary>
    public int RejectedSuggestions { get; set; }

    /// <summary>
    /// Acceptance rate (accepted / total).
    /// </summary>
    public decimal AcceptanceRate { get; set; }

    /// <summary>
    /// Total revenue from accepted suggestions.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average value per accepted suggestion.
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    /// Revenue from rule-based suggestions.
    /// </summary>
    public decimal RuleBasedRevenue { get; set; }

    /// <summary>
    /// Revenue from association-based suggestions.
    /// </summary>
    public decimal AssociationBasedRevenue { get; set; }

    /// <summary>
    /// Revenue from personalized suggestions.
    /// </summary>
    public decimal PersonalizedRevenue { get; set; }

    /// <summary>
    /// Number of unique products suggested.
    /// </summary>
    public int UniqueProductsSuggested { get; set; }

    /// <summary>
    /// Number of unique products accepted.
    /// </summary>
    public int UniqueProductsAccepted { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion
