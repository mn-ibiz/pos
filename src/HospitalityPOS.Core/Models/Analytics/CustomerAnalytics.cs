namespace HospitalityPOS.Core.Models.Analytics;

/// <summary>
/// RFM customer segment classification.
/// </summary>
public enum RFMSegment
{
    /// <summary>Best customers - recent, frequent, high spenders.</summary>
    Champion,
    /// <summary>Loyal customers - consistent buyers.</summary>
    Loyal,
    /// <summary>Potential loyalists - recent with average frequency.</summary>
    PotentialLoyalist,
    /// <summary>New customers - just started buying.</summary>
    NewCustomer,
    /// <summary>Promising - recent but low frequency.</summary>
    Promising,
    /// <summary>Needs attention - above average recency/frequency but declining.</summary>
    NeedsAttention,
    /// <summary>About to sleep - below average on all metrics.</summary>
    AboutToSleep,
    /// <summary>At risk - were good customers, now declining.</summary>
    AtRisk,
    /// <summary>Cannot lose - high value but slipping away.</summary>
    CantLoseThem,
    /// <summary>Hibernating - last purchase long ago.</summary>
    Hibernating,
    /// <summary>Lost - no recent activity, likely gone.</summary>
    Lost
}

/// <summary>
/// Individual customer RFM score.
/// </summary>
public class CustomerRFMScore
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Purchase data
    public DateTime FirstPurchaseDate { get; set; }
    public DateTime LastPurchaseDate { get; set; }
    public int DaysSinceLastPurchase { get; set; }
    public int TotalPurchases { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue => TotalPurchases > 0 ? Math.Round(TotalSpent / TotalPurchases, 2) : 0;

    // RFM Scores (1-5, 5 being best)
    public int RecencyScore { get; set; }
    public int FrequencyScore { get; set; }
    public int MonetaryScore { get; set; }

    // Combined RFM Score (e.g., 555 = best, 111 = worst)
    public int RFMScore => RecencyScore * 100 + FrequencyScore * 10 + MonetaryScore;
    public string RFMScoreDisplay => $"{RecencyScore}{FrequencyScore}{MonetaryScore}";

    // Segment
    public RFMSegment Segment { get; set; }
    public string SegmentName => GetSegmentName(Segment);
    public string SegmentColor => GetSegmentColor(Segment);
    public string SegmentDescription => GetSegmentDescription(Segment);

    // Churn prediction
    public decimal ChurnRiskScore { get; set; }
    public ChurnRiskLevel ChurnRiskLevel => ChurnRiskScore switch
    {
        <= 30 => ChurnRiskLevel.Low,
        <= 60 => ChurnRiskLevel.Medium,
        <= 80 => ChurnRiskLevel.High,
        _ => ChurnRiskLevel.Critical
    };

    // Customer Lifetime Value
    public decimal PredictedLifetimeValue { get; set; }
    public decimal LifetimeValueToDate => TotalSpent;

    // Engagement metrics
    public decimal VisitFrequencyDays { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTier { get; set; }

    // Recommended action
    public string RecommendedAction { get; set; } = string.Empty;

    private static string GetSegmentName(RFMSegment segment) => segment switch
    {
        RFMSegment.Champion => "Champion",
        RFMSegment.Loyal => "Loyal Customer",
        RFMSegment.PotentialLoyalist => "Potential Loyalist",
        RFMSegment.NewCustomer => "New Customer",
        RFMSegment.Promising => "Promising",
        RFMSegment.NeedsAttention => "Needs Attention",
        RFMSegment.AboutToSleep => "About to Sleep",
        RFMSegment.AtRisk => "At Risk",
        RFMSegment.CantLoseThem => "Can't Lose Them",
        RFMSegment.Hibernating => "Hibernating",
        RFMSegment.Lost => "Lost",
        _ => "Unknown"
    };

    private static string GetSegmentColor(RFMSegment segment) => segment switch
    {
        RFMSegment.Champion => "#10B981",
        RFMSegment.Loyal => "#059669",
        RFMSegment.PotentialLoyalist => "#6366F1",
        RFMSegment.NewCustomer => "#8B5CF6",
        RFMSegment.Promising => "#3B82F6",
        RFMSegment.NeedsAttention => "#F59E0B",
        RFMSegment.AboutToSleep => "#F97316",
        RFMSegment.AtRisk => "#EF4444",
        RFMSegment.CantLoseThem => "#DC2626",
        RFMSegment.Hibernating => "#9CA3AF",
        RFMSegment.Lost => "#6B7280",
        _ => "#374151"
    };

    private static string GetSegmentDescription(RFMSegment segment) => segment switch
    {
        RFMSegment.Champion => "Best customers. Bought recently, buy often, and spend the most.",
        RFMSegment.Loyal => "Responsive to promotions. Consistent customers.",
        RFMSegment.PotentialLoyalist => "Recent customers with average frequency. Offer membership/loyalty program.",
        RFMSegment.NewCustomer => "Bought recently but not frequently. Start building relationship.",
        RFMSegment.Promising => "Recent shoppers but haven't spent much. Create brand awareness.",
        RFMSegment.NeedsAttention => "Above average recency, frequency & monetary but declining.",
        RFMSegment.AboutToSleep => "Below average recency, frequency & monetary. Reactivate.",
        RFMSegment.AtRisk => "Spent big & purchased often but long time ago. Win back.",
        RFMSegment.CantLoseThem => "High value customers slipping away. Urgent retention needed.",
        RFMSegment.Hibernating => "Last purchase was long ago. Low spenders. May be lost.",
        RFMSegment.Lost => "Lowest recency, frequency & monetary scores. Probably lost.",
        _ => "Unknown segment"
    };
}

public enum ChurnRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Customer Analytics Report with RFM analysis.
/// </summary>
public class CustomerAnalyticsReport
{
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public DateTime DataStartDate { get; set; }
    public DateTime DataEndDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;

    // Overall metrics
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ChurningCustomers { get; set; }
    public decimal CustomerRetentionRate => TotalCustomers > 0
        ? Math.Round((decimal)ActiveCustomers / TotalCustomers * 100, 2) : 0;

    // Revenue metrics
    public decimal TotalRevenue { get; set; }
    public decimal AverageCustomerValue => ActiveCustomers > 0
        ? Math.Round(TotalRevenue / ActiveCustomers, 2) : 0;
    public decimal AverageOrderValue { get; set; }
    public decimal AverageOrdersPerCustomer { get; set; }

    // Segment distribution
    public Dictionary<RFMSegment, int> SegmentCounts { get; set; } = [];
    public Dictionary<RFMSegment, decimal> SegmentRevenue { get; set; } = [];

    // Segment summaries
    public List<RFMSegmentSummary> SegmentSummaries { get; set; } = [];

    // Individual customers
    public List<CustomerRFMScore> Customers { get; set; } = [];

    // At-risk customers
    public List<CustomerRFMScore> AtRiskCustomers { get; set; } = [];
    public decimal AtRiskRevenue => AtRiskCustomers.Sum(c => c.TotalSpent);

    // High value customers
    public List<CustomerRFMScore> HighValueCustomers { get; set; } = [];

    // Churn analysis
    public ChurnAnalysisSummary ChurnAnalysis { get; set; } = new();

    // Recommendations
    public List<CustomerAnalyticsRecommendation> Recommendations { get; set; } = [];
}

/// <summary>
/// Summary for each RFM segment.
/// </summary>
public class RFMSegmentSummary
{
    public RFMSegment Segment { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentColor { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal CustomerPercentage { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenuePercentage { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public decimal AverageChurnRisk { get; set; }
    public string RecommendedStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Churn analysis summary.
/// </summary>
public class ChurnAnalysisSummary
{
    public int TotalAtRisk { get; set; }
    public decimal AtRiskPercentage { get; set; }
    public decimal PotentialRevenueAtRisk { get; set; }

    public int LowRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int CriticalRiskCount { get; set; }

    public List<ChurnRiskFactor> TopRiskFactors { get; set; } = [];
}

public class ChurnRiskFactor
{
    public string Factor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ImpactScore { get; set; }
    public int AffectedCustomers { get; set; }
}

/// <summary>
/// Customer analytics recommendation.
/// </summary>
public class CustomerAnalyticsRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RFMSegment TargetSegment { get; set; }
    public int TargetCustomerCount { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
    public decimal EstimatedImpact { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Customer Lifetime Value (CLV) Report.
/// </summary>
public class CustomerLifetimeValueReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Summary metrics
    public decimal AverageCLV { get; set; }
    public decimal MedianCLV { get; set; }
    public decimal TotalPredictedCLV { get; set; }
    public decimal AverageCustomerAcquisitionCost { get; set; }
    public decimal CLVToCAC_Ratio => AverageCustomerAcquisitionCost > 0
        ? Math.Round(AverageCLV / AverageCustomerAcquisitionCost, 2) : 0;

    // Distribution
    public List<CLVBracket> CLVDistribution { get; set; } = [];

    // Top customers by CLV
    public List<CustomerCLVDetail> TopCustomers { get; set; } = [];

    // CLV by acquisition source
    public List<CLVBySource> ByAcquisitionSource { get; set; } = [];

    // CLV trends
    public List<CLVTrendPoint> CLVTrend { get; set; } = [];
}

public class CustomerCLVDetail
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal HistoricalValue { get; set; }
    public decimal PredictedFutureValue { get; set; }
    public decimal TotalCLV => HistoricalValue + PredictedFutureValue;
    public int PurchaseCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int DaysSinceFirstPurchase { get; set; }
    public decimal ValuePerDay => DaysSinceFirstPurchase > 0
        ? Math.Round(HistoricalValue / DaysSinceFirstPurchase, 2) : 0;
}

public class CLVBracket
{
    public string BracketName { get; set; } = string.Empty;
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int CustomerCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal PercentageOfCustomers { get; set; }
    public decimal PercentageOfValue { get; set; }
}

public class CLVBySource
{
    public string AcquisitionSource { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal AverageCLV { get; set; }
    public decimal TotalCLV { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal ROI => AcquisitionCost > 0 ? Math.Round((TotalCLV - AcquisitionCost) / AcquisitionCost * 100, 2) : 0;
}

public class CLVTrendPoint
{
    public DateTime Date { get; set; }
    public decimal AverageCLV { get; set; }
    public int NewCustomers { get; set; }
    public decimal NewCustomerCLV { get; set; }
}

/// <summary>
/// Purchase frequency analysis.
/// </summary>
public class PurchaseFrequencyReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public decimal AverageDaysBetweenPurchases { get; set; }
    public decimal MedianDaysBetweenPurchases { get; set; }
    public decimal AveragePurchasesPerMonth { get; set; }

    // Frequency distribution
    public List<FrequencyBracket> FrequencyDistribution { get; set; } = [];

    // By customer segment
    public List<FrequencyBySegment> BySegment { get; set; } = [];

    // Day of week analysis
    public List<DayOfWeekFrequency> DayOfWeekPattern { get; set; } = [];

    // Time of day analysis
    public List<TimeOfDayFrequency> TimeOfDayPattern { get; set; } = [];
}

public class FrequencyBracket
{
    public string BracketName { get; set; } = string.Empty;
    public int MinPurchases { get; set; }
    public int MaxPurchases { get; set; }
    public int CustomerCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageSpend { get; set; }
}

public class FrequencyBySegment
{
    public RFMSegment Segment { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public decimal AveragePurchaseFrequency { get; set; }
    public decimal AverageDaysBetweenPurchases { get; set; }
    public int CustomerCount { get; set; }
}

public class DayOfWeekFrequency
{
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class TimeOfDayFrequency
{
    public int Hour { get; set; }
    public string TimeRange => $"{Hour:D2}:00 - {(Hour + 1) % 24:D2}:00";
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>
/// Basket analysis report - items frequently bought together.
/// </summary>
public class BasketAnalysisReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public int TotalTransactions { get; set; }
    public decimal AverageBasketSize { get; set; }
    public decimal AverageBasketValue { get; set; }

    // Item associations
    public List<ItemAssociation> TopAssociations { get; set; } = [];

    // Frequently bought together pairs
    public List<ProductPair> FrequentPairs { get; set; } = [];

    // Cross-sell opportunities
    public List<CrossSellOpportunity> CrossSellOpportunities { get; set; } = [];
}

public class ItemAssociation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int AssociatedProductId { get; set; }
    public string AssociatedProductName { get; set; } = string.Empty;
    public decimal Support { get; set; }
    public decimal Confidence { get; set; }
    public decimal Lift { get; set; }
    public int CoOccurrenceCount { get; set; }
}

public class ProductPair
{
    public int Product1Id { get; set; }
    public string Product1Name { get; set; } = string.Empty;
    public int Product2Id { get; set; }
    public string Product2Name { get; set; } = string.Empty;
    public int TimesTogetherCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal CombinedRevenue { get; set; }
}

public class CrossSellOpportunity
{
    public int TriggerProductId { get; set; }
    public string TriggerProductName { get; set; } = string.Empty;
    public int RecommendedProductId { get; set; }
    public string RecommendedProductName { get; set; } = string.Empty;
    public decimal ConversionProbability { get; set; }
    public decimal PotentialRevenueIncrease { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// New vs Returning customers report.
/// </summary>
public class NewVsReturningReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // New customers
    public int NewCustomerCount { get; set; }
    public decimal NewCustomerRevenue { get; set; }
    public decimal NewCustomerAverageOrder { get; set; }

    // Returning customers
    public int ReturningCustomerCount { get; set; }
    public decimal ReturningCustomerRevenue { get; set; }
    public decimal ReturningCustomerAverageOrder { get; set; }

    // Ratios
    public decimal NewCustomerPercentage => (NewCustomerCount + ReturningCustomerCount) > 0
        ? Math.Round((decimal)NewCustomerCount / (NewCustomerCount + ReturningCustomerCount) * 100, 2) : 0;
    public decimal ReturningCustomerPercentage => 100 - NewCustomerPercentage;
    public decimal NewCustomerRevenuePercentage => TotalRevenue > 0
        ? Math.Round(NewCustomerRevenue / TotalRevenue * 100, 2) : 0;

    public decimal TotalRevenue => NewCustomerRevenue + ReturningCustomerRevenue;

    // Trend over time
    public List<NewVsReturningTrend> Trend { get; set; } = [];

    // Conversion tracking
    public int NewCustomersConverted { get; set; }
    public decimal ConversionRate => NewCustomerCount > 0
        ? Math.Round((decimal)NewCustomersConverted / NewCustomerCount * 100, 2) : 0;
}

public class NewVsReturningTrend
{
    public DateTime Date { get; set; }
    public int NewCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    public decimal NewCustomerRevenue { get; set; }
    public decimal ReturningCustomerRevenue { get; set; }
}

/// <summary>
/// Parameters for RFM analysis.
/// </summary>
public class RFMAnalysisParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }

    // Recency scoring (days since last purchase)
    public int RecencyScore5Threshold { get; set; } = 30;   // Within 30 days = score 5
    public int RecencyScore4Threshold { get; set; } = 60;   // 31-60 days = score 4
    public int RecencyScore3Threshold { get; set; } = 90;   // 61-90 days = score 3
    public int RecencyScore2Threshold { get; set; } = 180;  // 91-180 days = score 2
    // > 180 days = score 1

    // Frequency scoring (number of purchases)
    public int FrequencyScore5Threshold { get; set; } = 20;
    public int FrequencyScore4Threshold { get; set; } = 10;
    public int FrequencyScore3Threshold { get; set; } = 5;
    public int FrequencyScore2Threshold { get; set; } = 2;
    // 1 purchase = score 1

    // Monetary scoring (total spend) - will be calculated as percentiles
    public bool UsePercentileMonetaryScoring { get; set; } = true;

    // Options
    public bool IncludeChurnPrediction { get; set; } = true;
    public bool IncludeCLV { get; set; } = true;
    public bool IncludeRecommendations { get; set; } = true;
    public int TopAtRiskCount { get; set; } = 50;
    public int TopHighValueCount { get; set; } = 50;
}

/// <summary>
/// Parameters for CLV calculation.
/// </summary>
public class CLVParameters
{
    public int? StoreId { get; set; }
    public decimal DiscountRate { get; set; } = 0.10m; // 10% annual discount rate
    public int ForecastMonths { get; set; } = 24;
    public decimal AverageGrossMargin { get; set; } = 0.60m; // 60% margin
    public bool IncludeChurnProbability { get; set; } = true;
    public int TopCustomersCount { get; set; } = 100;
}

/// <summary>
/// Parameters for basket analysis.
/// </summary>
public class BasketAnalysisParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public decimal MinimumSupport { get; set; } = 0.01m; // 1% minimum support
    public decimal MinimumConfidence { get; set; } = 0.1m; // 10% minimum confidence
    public int TopPairsCount { get; set; } = 20;
    public int TopAssociationsCount { get; set; } = 50;
}
