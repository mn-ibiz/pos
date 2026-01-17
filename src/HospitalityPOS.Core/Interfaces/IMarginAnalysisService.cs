using HospitalityPOS.Core.Models.Analytics;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for profit margin analysis and reporting.
/// Provides margin calculations, trend analysis, and profitability reports.
/// </summary>
public interface IMarginAnalysisService
{
    #region Product Margin Analysis

    /// <summary>
    /// Gets margin data for all products.
    /// </summary>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="supplierId">Optional supplier filter.</param>
    /// <param name="onlyWithCostPrice">Only include products with cost price defined.</param>
    /// <returns>List of product margins.</returns>
    Task<IReadOnlyList<ProductMarginDto>> GetProductMarginsAsync(
        int? categoryId = null,
        int? supplierId = null,
        bool onlyWithCostPrice = true);

    /// <summary>
    /// Gets margin data for a specific product.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <returns>Product margin data or null if not found.</returns>
    Task<ProductMarginDto?> GetProductMarginAsync(int productId);

    /// <summary>
    /// Gets product margins with sales data for a period.
    /// </summary>
    /// <param name="request">Report request with date range and filters.</param>
    /// <returns>List of product margins with sales data.</returns>
    Task<IReadOnlyList<ProductMarginDto>> GetProductMarginsWithSalesAsync(MarginReportRequest request);

    #endregion

    #region Category Margin Analysis

    /// <summary>
    /// Gets aggregated margin data by category.
    /// </summary>
    /// <param name="request">Report request with date range and filters.</param>
    /// <returns>List of category margins.</returns>
    Task<IReadOnlyList<CategoryMarginDto>> GetCategoryMarginsAsync(MarginReportRequest request);

    /// <summary>
    /// Gets the top profitable categories.
    /// </summary>
    /// <param name="request">Report request with date range.</param>
    /// <param name="limit">Maximum number of categories to return.</param>
    /// <returns>List of categories ranked by profitability.</returns>
    Task<IReadOnlyList<CategoryMarginDto>> GetTopProfitableCategoriesAsync(
        MarginReportRequest request,
        int limit = 10);

    /// <summary>
    /// Gets categories with lowest margins.
    /// </summary>
    /// <param name="request">Report request with date range.</param>
    /// <param name="limit">Maximum number of categories to return.</param>
    /// <returns>List of categories with lowest margins.</returns>
    Task<IReadOnlyList<CategoryMarginDto>> GetLowestMarginCategoriesAsync(
        MarginReportRequest request,
        int limit = 10);

    #endregion

    #region Gross Profit Reports

    /// <summary>
    /// Gets gross profit summary for a period.
    /// </summary>
    /// <param name="request">Report request with date range and filters.</param>
    /// <returns>Gross profit summary.</returns>
    Task<GrossProfitSummaryDto> GetGrossProfitSummaryAsync(MarginReportRequest request);

    /// <summary>
    /// Gets daily gross profit data for charting.
    /// </summary>
    /// <param name="request">Report request with date range.</param>
    /// <returns>List of daily gross profit data points.</returns>
    Task<IReadOnlyList<MarginTrendPointDto>> GetDailyGrossProfitAsync(MarginReportRequest request);

    /// <summary>
    /// Gets monthly gross profit data for trend analysis.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>List of monthly gross profit data points.</returns>
    Task<IReadOnlyList<MarginTrendPointDto>> GetMonthlyGrossProfitAsync(
        DateTime startDate,
        DateTime endDate,
        int? categoryId = null);

    #endregion

    #region Low Margin Alerts

    /// <summary>
    /// Gets products with margins below the threshold.
    /// </summary>
    /// <param name="thresholdPercent">Minimum margin threshold (default 15%).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>List of low margin alerts.</returns>
    Task<IReadOnlyList<LowMarginAlertDto>> GetLowMarginAlertsAsync(
        decimal thresholdPercent = 15.0m,
        int? categoryId = null);

    /// <summary>
    /// Gets count of products below margin threshold.
    /// </summary>
    /// <param name="thresholdPercent">Minimum margin threshold.</param>
    /// <returns>Count of low margin products.</returns>
    Task<int> GetLowMarginCountAsync(decimal thresholdPercent = 15.0m);

    /// <summary>
    /// Gets potential profit loss from low margin products.
    /// </summary>
    /// <param name="thresholdPercent">Minimum margin threshold.</param>
    /// <param name="periodDays">Number of days to analyze.</param>
    /// <returns>Total potential profit loss.</returns>
    Task<decimal> GetPotentialProfitLossAsync(decimal thresholdPercent = 15.0m, int periodDays = 30);

    #endregion

    #region Margin Trend Analysis

    /// <summary>
    /// Gets margin trend data for a product.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="months">Number of months to analyze.</param>
    /// <returns>Product margin trend data.</returns>
    Task<ProductMarginTrendDto?> GetProductMarginTrendAsync(int productId, int months = 6);

    /// <summary>
    /// Gets products with declining margins.
    /// </summary>
    /// <param name="declineThresholdPercent">Minimum decline to flag (default 5%).</param>
    /// <param name="limit">Maximum number of products to return.</param>
    /// <returns>List of products with declining margins.</returns>
    Task<IReadOnlyList<ProductMarginTrendDto>> GetDecliningMarginProductsAsync(
        decimal declineThresholdPercent = 5.0m,
        int limit = 20);

    /// <summary>
    /// Gets products with cost price increases.
    /// </summary>
    /// <param name="periodDays">Number of days to analyze.</param>
    /// <param name="limit">Maximum number of products to return.</param>
    /// <returns>List of products with recent cost increases.</returns>
    Task<IReadOnlyList<ProductMarginTrendDto>> GetCostIncreaseAlertsAsync(
        int periodDays = 30,
        int limit = 20);

    /// <summary>
    /// Gets cost price history for a product.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <returns>List of cost price history records.</returns>
    Task<IReadOnlyList<CostPriceHistoryDto>> GetCostPriceHistoryAsync(int productId, int limit = 50);

    #endregion

    #region Complete Reports

    /// <summary>
    /// Gets complete margin analytics report.
    /// </summary>
    /// <param name="request">Report request with all filters.</param>
    /// <returns>Complete margin analytics report.</returns>
    Task<MarginAnalyticsReportDto> GetMarginAnalyticsReportAsync(MarginReportRequest request);

    /// <summary>
    /// Exports margin report to Excel.
    /// </summary>
    /// <param name="request">Report request.</param>
    /// <param name="filePath">Output file path.</param>
    /// <returns>True if export successful.</returns>
    Task<bool> ExportToExcelAsync(MarginReportRequest request, string filePath);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the configured minimum margin threshold.
    /// </summary>
    /// <returns>Minimum margin threshold percentage.</returns>
    Task<decimal> GetMinimumMarginThresholdAsync();

    /// <summary>
    /// Sets the minimum margin threshold.
    /// </summary>
    /// <param name="thresholdPercent">Threshold percentage.</param>
    Task SetMinimumMarginThresholdAsync(decimal thresholdPercent);

    #endregion
}
