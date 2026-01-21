namespace HospitalityPOS.Core.Models.AI;

/// <summary>
/// Sales forecast result.
/// </summary>
public class SalesForecast
{
    public DateTime ForecastDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Predicted values
    public decimal PredictedSales { get; set; }
    public decimal ConfidenceLower { get; set; }
    public decimal ConfidenceUpper { get; set; }
    public decimal ConfidenceLevel { get; set; } = 0.95m; // 95% confidence

    // Comparison to historical
    public decimal? SameDayLastWeek { get; set; }
    public decimal? SameDayLastMonth { get; set; }
    public decimal? SameDayLastYear { get; set; }
    public decimal? AverageForDayOfWeek { get; set; }

    // Factors affecting forecast
    public List<ForecastFactor> Factors { get; set; } = [];

    // Accuracy metrics (if we have actuals)
    public decimal? ActualSales { get; set; }
    public decimal? ForecastError => ActualSales.HasValue
        ? Math.Round((PredictedSales - ActualSales.Value) / ActualSales.Value * 100, 2) : null;

    // Model info
    public string ModelVersion { get; set; } = string.Empty;
    public decimal ModelAccuracy { get; set; }
}

public class ForecastFactor
{
    public string FactorName { get; set; } = string.Empty;
    public string FactorType { get; set; } = string.Empty; // Weather, Event, Seasonality, Trend
    public decimal Impact { get; set; } // Percentage impact on forecast
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Hourly sales forecast.
/// </summary>
public class HourlyForecast
{
    public DateTime Date { get; set; }
    public int Hour { get; set; }
    public string HourDisplay => $"{Hour:D2}:00";
    public decimal PredictedSales { get; set; }
    public int PredictedTransactions { get; set; }
    public decimal? HistoricalAverage { get; set; }
    public decimal VarianceFromAverage => HistoricalAverage.HasValue && HistoricalAverage > 0
        ? Math.Round((PredictedSales - HistoricalAverage.Value) / HistoricalAverage.Value * 100, 2) : 0;
}

/// <summary>
/// Product demand forecast.
/// </summary>
public class ProductDemandForecast
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public int PredictedQuantity { get; set; }
    public int SafetyStock { get; set; }
    public int RecommendedPrepQuantity { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public decimal? HistoricalAverage { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // Up, Down, Stable
}

/// <summary>
/// Inventory recommendation.
/// </summary>
public class InventoryRecommendation
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Current state
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public int DaysOfStockRemaining { get; set; }

    // Recommendation
    public RecommendationType RecommendationType { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public DateTime RecommendedOrderDate { get; set; }
    public decimal EstimatedCost { get; set; }

    // Urgency
    public UrgencyLevel Urgency { get; set; }
    public string UrgencyReason { get; set; } = string.Empty;

    // Additional info
    public decimal AverageDailySales { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime? ProjectedStockoutDate { get; set; }
}

public enum RecommendationType
{
    Reorder,
    IncreaseOrder,
    DecreaseOrder,
    Discontinue,
    NoActionNeeded
}

public enum UrgencyLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Menu optimization recommendation.
/// </summary>
public class MenuRecommendation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public MenuRecommendationType RecommendationType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;

    // Current state
    public decimal CurrentPrice { get; set; }
    public decimal CurrentMargin { get; set; }
    public int CurrentSalesRank { get; set; }

    // Suggested change
    public decimal? SuggestedPrice { get; set; }
    public decimal? SuggestedMargin { get; set; }

    // Impact
    public decimal EstimatedRevenueImpact { get; set; }
    public decimal EstimatedProfitImpact { get; set; }
    public decimal ConfidenceLevel { get; set; }

    public int Priority { get; set; }
}

public enum MenuRecommendationType
{
    IncreasePrice,
    DecreasePrice,
    Promote,
    Reposition,
    Remove,
    AddToBundle,
    SeasonalFeature
}

/// <summary>
/// Staffing recommendation.
/// </summary>
public class StaffingRecommendation
{
    public DateTime Date { get; set; }
    public int Hour { get; set; }
    public string TimeSlot => $"{Hour:D2}:00 - {(Hour + 1) % 24:D2}:00";

    public int CurrentStaff { get; set; }
    public int RecommendedStaff { get; set; }
    public int Variance => RecommendedStaff - CurrentStaff;

    public string Status { get; set; } = string.Empty; // Optimal, Understaffed, Overstaffed
    public string Reason { get; set; } = string.Empty;

    public decimal PredictedSales { get; set; }
    public decimal TargetSPLH { get; set; }
    public decimal CurrentSPLH { get; set; }
    public decimal OptimalSPLH { get; set; }

    public decimal PotentialLaborSavings { get; set; }
    public decimal PotentialRevenueLoss { get; set; }
}

/// <summary>
/// Anomaly alert from the AI system.
/// </summary>
public class AnomalyAlert
{
    public int Id { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public AnomalyType AnomalyType { get; set; }
    public AnomalySeverity Severity { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    // Related entity
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;

    // Metrics
    public decimal? ExpectedValue { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? DeviationPercentage { get; set; }

    // Status
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public bool IsResolved { get; set; }
    public string? Resolution { get; set; }

    // Suggested actions
    public List<string> SuggestedActions { get; set; } = [];
}

public enum AnomalyType
{
    SalesAnomaly,
    VoidAnomaly,
    DiscountAnomaly,
    CashVariance,
    InventoryDiscrepancy,
    PriceOverride,
    UnusualTransaction,
    AfterHoursActivity,
    RefundPattern,
    NoSalePattern
}

public enum AnomalySeverity
{
    Info,
    Warning,
    High,
    Critical
}

/// <summary>
/// Fraud detection alert.
/// </summary>
public class FraudAlert
{
    public int Id { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public FraudType FraudType { get; set; }
    public AnomalySeverity Severity { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Employee involved
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    // Pattern details
    public string PatternDescription { get; set; } = string.Empty;
    public int IncidentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PatternStartDate { get; set; }

    // Comparison to normal
    public decimal? NormalRate { get; set; }
    public decimal? CurrentRate { get; set; }
    public decimal DeviationMultiple => NormalRate.HasValue && NormalRate > 0
        ? Math.Round(CurrentRate ?? 0 / NormalRate.Value, 2) : 0;

    // Evidence
    public List<FraudEvidence> Evidence { get; set; } = [];
}

public enum FraudType
{
    ExcessiveVoids,
    SweethearingDiscount,
    CashSkimming,
    RefundFraud,
    NoSaleAbuse,
    TimeTheft,
    InventoryTheft
}

public class FraudEvidence
{
    public DateTime Timestamp { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

/// <summary>
/// Business insight generated by AI.
/// </summary>
public class BusinessInsight
{
    public int Id { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public InsightType InsightType { get; set; }
    public InsightCategory Category { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string DetailedAnalysis { get; set; } = string.Empty;

    // Impact
    public InsightImpact Impact { get; set; }
    public decimal? EstimatedValue { get; set; }

    // Recommendation
    public string Recommendation { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = [];

    // Validity
    public DateTime? ExpiresAt { get; set; }
    public bool IsActioned { get; set; }

    // Supporting data
    public List<InsightDataPoint> SupportingData { get; set; } = [];
}

public enum InsightType
{
    Opportunity,
    Risk,
    Trend,
    Anomaly,
    Achievement,
    Recommendation
}

public enum InsightCategory
{
    Sales,
    Inventory,
    Customers,
    Employees,
    Operations,
    Financial,
    Menu
}

public enum InsightImpact
{
    Low,
    Medium,
    High,
    Critical
}

public class InsightDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? Comparison { get; set; }
    public string? ComparisonLabel { get; set; }
}

/// <summary>
/// Natural language query response.
/// </summary>
public class NaturalLanguageQueryResponse
{
    public string Query { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public string Answer { get; set; } = string.Empty;
    public string DetailedExplanation { get; set; } = string.Empty;

    // Data used to answer
    public List<QueryDataSource> DataSources { get; set; } = [];

    // Visualizations
    public List<QueryVisualization> Visualizations { get; set; } = [];

    // Follow-up suggestions
    public List<string> SuggestedFollowUps { get; set; } = [];

    // Confidence
    public decimal ConfidenceScore { get; set; }
}

public class QueryDataSource
{
    public string SourceName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime DateRange_Start { get; set; }
    public DateTime DateRange_End { get; set; }
    public int RecordCount { get; set; }
}

public class QueryVisualization
{
    public string ChartType { get; set; } = string.Empty; // Line, Bar, Pie, Table
    public string Title { get; set; } = string.Empty;
    public string XAxisLabel { get; set; } = string.Empty;
    public string YAxisLabel { get; set; } = string.Empty;
    public List<ChartDataPoint> DataPoints { get; set; } = [];
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Series { get; set; }
    public string? Color { get; set; }
}

/// <summary>
/// AI-generated report summary.
/// </summary>
public class AIReportSummary
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Executive summary
    public string ExecutiveSummary { get; set; } = string.Empty;

    // Key highlights
    public List<string> KeyHighlights { get; set; } = [];

    // Key metrics
    public List<SummaryMetric> KeyMetrics { get; set; } = [];

    // Trends
    public List<string> PositiveTrends { get; set; } = [];
    public List<string> ConcerningTrends { get; set; } = [];

    // Recommendations
    public List<string> Recommendations { get; set; } = [];

    // Alerts
    public List<string> Alerts { get; set; } = [];
}

public class SummaryMetric
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public decimal? ChangeFromPrevious { get; set; }
    public string Trend { get; set; } = string.Empty; // Up, Down, Stable
    public string Status { get; set; } = string.Empty; // Good, Warning, Critical
}

/// <summary>
/// Churn risk prediction result.
/// </summary>
public class ChurnRiskAlert
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal ChurnProbability { get; set; }
    public ChurnRiskLevel RiskLevel { get; set; }

    public decimal HistoricalValue { get; set; }
    public decimal PredictedLostValue { get; set; }
    public int DaysSinceLastPurchase { get; set; }

    public List<string> RiskFactors { get; set; } = [];
    public string RecommendedAction { get; set; } = string.Empty;
    public decimal? WinBackOfferValue { get; set; }
}

public enum ChurnRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// What-if scenario analysis result.
/// </summary>
public class WhatIfAnalysisResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Input parameters
    public Dictionary<string, decimal> InputParameters { get; set; } = [];

    // Baseline
    public decimal BaselineRevenue { get; set; }
    public decimal BaselineProfit { get; set; }
    public decimal BaselineMargin { get; set; }

    // Projected
    public decimal ProjectedRevenue { get; set; }
    public decimal ProjectedProfit { get; set; }
    public decimal ProjectedMargin { get; set; }

    // Changes
    public decimal RevenueChange => ProjectedRevenue - BaselineRevenue;
    public decimal RevenueChangePercentage => BaselineRevenue > 0
        ? Math.Round(RevenueChange / BaselineRevenue * 100, 2) : 0;
    public decimal ProfitChange => ProjectedProfit - BaselineProfit;
    public decimal ProfitChangePercentage => BaselineProfit != 0
        ? Math.Round(ProfitChange / Math.Abs(BaselineProfit) * 100, 2) : 0;

    // Confidence
    public decimal ConfidenceLevel { get; set; }

    // Risks and opportunities
    public List<string> Risks { get; set; } = [];
    public List<string> Opportunities { get; set; } = [];

    // Recommendation
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// AI Dashboard summary for quick view.
/// </summary>
public class AIDashboardSummary
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime DataAsOf { get; set; }

    // Today's forecast
    public SalesForecast? TodaysForecast { get; set; }
    public List<HourlyForecast> HourlyForecast { get; set; } = [];

    // Active alerts
    public int CriticalAlertCount { get; set; }
    public int HighAlertCount { get; set; }
    public int WarningAlertCount { get; set; }
    public List<AnomalyAlert> TopAlerts { get; set; } = [];

    // Top insights
    public List<BusinessInsight> TopInsights { get; set; } = [];

    // Urgent recommendations
    public List<InventoryRecommendation> UrgentInventoryRecommendations { get; set; } = [];
    public List<MenuRecommendation> TopMenuRecommendations { get; set; } = [];
    public List<StaffingRecommendation> StaffingIssues { get; set; } = [];

    // Customer health
    public int AtRiskCustomerCount { get; set; }
    public decimal AtRiskRevenue { get; set; }

    // Performance vs forecast
    public decimal? CurrentDaySales { get; set; }
    public decimal? ForecastVariance { get; set; }
    public string PerformanceStatus { get; set; } = string.Empty; // OnTrack, BelowTarget, AboveTarget
}
