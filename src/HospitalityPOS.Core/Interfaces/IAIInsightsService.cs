using HospitalityPOS.Core.Models.AI;
using HospitalityPOS.Core.Models.Analytics;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for AI-powered business insights, forecasting, and recommendations.
/// </summary>
public interface IAIInsightsService
{
    #region Forecasting

    /// <summary>
    /// Forecasts sales for a specific date.
    /// </summary>
    Task<SalesForecast> ForecastSalesAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hourly sales forecast for a specific date.
    /// </summary>
    Task<List<HourlyForecast>> ForecastHourlySalesAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forecasts product demand for a specific date.
    /// </summary>
    Task<List<ProductDemandForecast>> ForecastProductDemandAsync(
        DateTime date,
        int? storeId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sales forecast for a date range.
    /// </summary>
    Task<List<SalesForecast>> ForecastSalesRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Recommendations

    /// <summary>
    /// Gets inventory recommendations.
    /// </summary>
    Task<List<InventoryRecommendation>> GetInventoryRecommendationsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets menu optimization recommendations.
    /// </summary>
    Task<List<MenuRecommendation>> GetMenuRecommendationsAsync(
        int? storeId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets staffing recommendations for a specific date.
    /// </summary>
    Task<List<StaffingRecommendation>> GetStaffingRecommendationsAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Anomaly Detection

    /// <summary>
    /// Detects anomalies in the specified date range.
    /// </summary>
    Task<List<AnomalyAlert>> DetectAnomaliesAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects fraud patterns.
    /// </summary>
    Task<List<FraudAlert>> DetectFraudPatternsAsync(
        int? employeeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    Task<List<AnomalyAlert>> GetActiveAlertsAsync(
        int? storeId = null,
        AnomalySeverity? minSeverity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task AcknowledgeAlertAsync(
        int alertId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert.
    /// </summary>
    Task ResolveAlertAsync(
        int alertId,
        string resolution,
        int userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Natural Language Interface

    /// <summary>
    /// Answers a business question in natural language.
    /// </summary>
    Task<NaturalLanguageQueryResponse> AnswerBusinessQuestionAsync(
        string question,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an AI summary for a report.
    /// </summary>
    Task<AIReportSummary> GenerateReportSummaryAsync(
        string reportType,
        object reportData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested questions based on context.
    /// </summary>
    Task<List<string>> GetSuggestedQuestionsAsync(
        string? context = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Customer Analytics

    /// <summary>
    /// Predicts customer churn.
    /// </summary>
    Task<List<ChurnRiskAlert>> PredictChurnAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer insights.
    /// </summary>
    Task<List<BusinessInsight>> GetCustomerInsightsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Business Insights

    /// <summary>
    /// Gets top business insights.
    /// </summary>
    Task<List<BusinessInsight>> GetTopInsightsAsync(
        int? storeId = null,
        InsightCategory? category = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets insights for a specific category.
    /// </summary>
    Task<List<BusinessInsight>> GetInsightsByCategoryAsync(
        InsightCategory category,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an insight as actioned.
    /// </summary>
    Task MarkInsightAsActionedAsync(
        int insightId,
        CancellationToken cancellationToken = default);

    #endregion

    #region What-If Analysis

    /// <summary>
    /// Runs a what-if scenario analysis.
    /// </summary>
    Task<WhatIfAnalysisResult> RunWhatIfAnalysisAsync(
        WhatIfScenario scenario,
        CancellationToken cancellationToken = default);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets the AI dashboard summary.
    /// </summary>
    Task<AIDashboardSummary> GetDashboardSummaryAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// What-if scenario definition.
/// </summary>
public class WhatIfScenario
{
    public string ScenarioType { get; set; } = string.Empty; // PriceChange, MenuChange, StaffingChange
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, decimal> Parameters { get; set; } = [];

    // For price changes
    public int? ProductId { get; set; }
    public decimal? NewPrice { get; set; }

    // For category-wide changes
    public int? CategoryId { get; set; }
    public decimal? PriceChangePercentage { get; set; }

    // For staffing changes
    public decimal? LaborHoursChange { get; set; }
    public decimal? LaborCostChange { get; set; }

    // Analysis period
    public int ProjectionMonths { get; set; } = 1;
}
