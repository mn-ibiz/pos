using HospitalityPOS.Core.Models.Analytics;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for customer analytics including RFM analysis, CLV, and behavior analysis.
/// </summary>
public interface ICustomerAnalyticsService
{
    /// <summary>
    /// Generates a comprehensive customer analytics report with RFM analysis.
    /// </summary>
    Task<CustomerAnalyticsReport> GenerateCustomerAnalyticsReportAsync(
        RFMAnalysisParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates RFM scores for all customers.
    /// </summary>
    Task<List<CustomerRFMScore>> CalculateRFMScoresAsync(
        RFMAnalysisParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets RFM score for a specific customer.
    /// </summary>
    Task<CustomerRFMScore?> GetCustomerRFMScoreAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by RFM segment.
    /// </summary>
    Task<List<CustomerRFMScore>> GetCustomersBySegmentAsync(
        RFMSegment segment,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a customer lifetime value report.
    /// </summary>
    Task<CustomerLifetimeValueReport> GenerateCLVReportAsync(
        CLVParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates CLV for a specific customer.
    /// </summary>
    Task<CustomerCLVDetail> CalculateCustomerCLVAsync(
        int customerId,
        CLVParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a purchase frequency analysis report.
    /// </summary>
    Task<PurchaseFrequencyReport> GeneratePurchaseFrequencyReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a basket analysis report.
    /// </summary>
    Task<BasketAnalysisReport> GenerateBasketAnalysisReportAsync(
        BasketAnalysisParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new vs returning customers report.
    /// </summary>
    Task<NewVsReturningReport> GenerateNewVsReturningReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts churn risk for customers.
    /// </summary>
    Task<List<ChurnPrediction>> PredictChurnAsync(
        int? storeId = null,
        decimal minRiskScore = 0.5m,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets at-risk customers who need attention.
    /// </summary>
    Task<List<CustomerRFMScore>> GetAtRiskCustomersAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets high-value customers.
    /// </summary>
    Task<List<CustomerRFMScore>> GetHighValueCustomersAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets segment summary statistics.
    /// </summary>
    Task<List<RFMSegmentSummary>> GetSegmentSummariesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer retention metrics.
    /// </summary>
    Task<CustomerRetentionMetrics> GetRetentionMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Churn prediction result.
/// </summary>
public class ChurnPrediction
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal ChurnProbability { get; set; }
    public ChurnRiskLevel RiskLevel { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal PotentialLostValue { get; set; }
    public int DaysSinceLastPurchase { get; set; }
    public int DeclineInFrequency { get; set; }
    public List<string> RiskFactors { get; set; } = [];
    public string RecommendedAction { get; set; } = string.Empty;
    public decimal? SuggestedOfferValue { get; set; }
}

/// <summary>
/// Customer retention metrics.
/// </summary>
public class CustomerRetentionMetrics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Customer counts
    public int StartingCustomers { get; set; }
    public int EndingCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int LostCustomers { get; set; }

    // Rates
    public decimal RetentionRate => StartingCustomers > 0
        ? Math.Round((decimal)(StartingCustomers - LostCustomers) / StartingCustomers * 100, 2) : 0;
    public decimal ChurnRate => 100 - RetentionRate;
    public decimal GrowthRate => StartingCustomers > 0
        ? Math.Round((decimal)(EndingCustomers - StartingCustomers) / StartingCustomers * 100, 2) : 0;

    // Revenue impact
    public decimal RevenueFromRetained { get; set; }
    public decimal RevenueFromNew { get; set; }
    public decimal LostRevenue { get; set; }

    // Cohort data
    public List<RetentionCohort> Cohorts { get; set; } = [];
}

public class RetentionCohort
{
    public DateTime CohortMonth { get; set; }
    public int InitialCustomers { get; set; }
    public List<CohortRetentionPoint> RetentionPoints { get; set; } = [];
}

public class CohortRetentionPoint
{
    public int MonthNumber { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal RetentionPercentage { get; set; }
}
