using HospitalityPOS.Core.Models.Analytics;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for comparative analytics and period-over-period analysis.
/// Provides trend analysis, growth calculations, and performance comparisons.
/// </summary>
public interface IComparativeAnalyticsService
{
    #region Period Comparison

    /// <summary>
    /// Gets a complete period comparison with all key metrics.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Period comparison with growth metrics.</returns>
    Task<PeriodComparisonDto> GetPeriodComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets growth metrics for a specific metric type.
    /// </summary>
    /// <param name="currentStart">Current period start date.</param>
    /// <param name="currentEnd">Current period end date.</param>
    /// <param name="previousStart">Previous period start date.</param>
    /// <param name="previousEnd">Previous period end date.</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Growth metrics for the periods.</returns>
    Task<GrowthMetricsDto> CalculateGrowthMetricsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Trend Analysis

    /// <summary>
    /// Gets sales trend data with comparison period overlay.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sales trend comparison data.</returns>
    Task<SalesTrendComparisonDto> GetSalesTrendComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily sales trend data for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of daily sales trend data.</returns>
    Task<List<DailySalesTrendDto>> GetDailySalesTrendAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates moving average for sales data.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="days">Number of days for moving average (default 7).</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Moving average data points.</returns>
    Task<List<MovingAveragePointDto>> GetMovingAverageAsync(
        DateTime startDate,
        DateTime endDate,
        int days = 7,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Category Comparison

    /// <summary>
    /// Gets category performance comparison between periods.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of category comparisons.</returns>
    Task<List<CategoryComparisonDto>> GetCategoryComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the fastest growing categories.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="count">Number of categories to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top growing categories.</returns>
    Task<List<CategoryComparisonDto>> GetFastestGrowingCategoriesAsync(
        PeriodComparisonRequest request,
        int count = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the declining categories.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="count">Number of categories to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top declining categories.</returns>
    Task<List<CategoryComparisonDto>> GetDecliningCategoriesAsync(
        PeriodComparisonRequest request,
        int count = 5,
        CancellationToken cancellationToken = default);

    #endregion

    #region Product Comparison

    /// <summary>
    /// Gets product performance comparison between periods.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="count">Maximum number of products to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of product comparisons.</returns>
    Task<List<ProductComparisonDto>> GetProductComparisonAsync(
        PeriodComparisonRequest request,
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top gainers and losers summary.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="count">Number of products per category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top movers summary.</returns>
    Task<TopMoversDto> GetTopMoversAsync(
        PeriodComparisonRequest request,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products that are new in the current period.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New products list.</returns>
    Task<List<ProductComparisonDto>> GetNewProductsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Pattern Analysis

    /// <summary>
    /// Gets day-of-week sales patterns.
    /// </summary>
    /// <param name="startDate">Start date for pattern analysis.</param>
    /// <param name="endDate">End date for pattern analysis.</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Day-of-week pattern data.</returns>
    Task<List<DayOfWeekPatternDto>> GetDayOfWeekPatternsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hourly sales patterns by day of week for heat map.
    /// </summary>
    /// <param name="startDate">Start date for pattern analysis.</param>
    /// <param name="endDate">End date for pattern analysis.</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Hourly pattern data for heat map.</returns>
    Task<List<HourlyPatternDto>> GetHourlyPatternsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Sparklines and Indicators

    /// <summary>
    /// Gets sparkline data for key metrics.
    /// </summary>
    /// <param name="days">Number of days to include in sparkline.</param>
    /// <param name="storeId">Optional store ID for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sparkline data for metrics.</returns>
    Task<List<SparklineDataDto>> GetSparklineDataAsync(
        int days = 14,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Complete Analytics

    /// <summary>
    /// Gets complete comparative analytics in a single call.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete comparative analytics data.</returns>
    Task<ComparativeAnalyticsDto> GetComparativeAnalyticsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports comparative analytics data for Excel export.
    /// </summary>
    /// <param name="request">The comparison request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Export data container.</returns>
    Task<ComparativeAnalyticsExportDto> ExportAnalyticsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Resolves period dates based on period type.
    /// </summary>
    /// <param name="periodType">The comparison period type.</param>
    /// <returns>Tuple with current and previous period dates.</returns>
    (DateTime CurrentStart, DateTime CurrentEnd, DateTime PreviousStart, DateTime PreviousEnd)
        ResolvePeriodDates(ComparisonPeriodType periodType);

    /// <summary>
    /// Gets the label for a comparison period.
    /// </summary>
    /// <param name="periodType">The comparison period type.</param>
    /// <param name="isCurrent">Whether this is the current period.</param>
    /// <returns>Period label string.</returns>
    string GetPeriodLabel(ComparisonPeriodType periodType, bool isCurrent);

    #endregion
}
