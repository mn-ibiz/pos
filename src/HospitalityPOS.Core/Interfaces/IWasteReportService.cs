using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for waste reporting and analysis.
/// </summary>
public interface IWasteReportService
{
    #region Waste Summary Reports

    /// <summary>
    /// Gets waste summary report based on query parameters.
    /// </summary>
    /// <param name="query">Query parameters for the report.</param>
    /// <returns>Waste summary report.</returns>
    Task<WasteSummaryReportDto> GetWasteSummaryAsync(WasteReportQueryDto query);

    /// <summary>
    /// Gets waste breakdown by category.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of waste by category.</returns>
    Task<List<WasteByCategoryDto>> GetWasteByCategoryAsync(int? storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets waste breakdown by supplier.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of waste by supplier.</returns>
    Task<List<WasteBySupplierDto>> GetWasteBySupplierAsync(int? storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets waste breakdown by reason.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of waste by reason.</returns>
    Task<List<WasteByReasonDto>> GetWasteByReasonAsync(int? storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets waste breakdown by product.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="topCount">Number of top products to return.</param>
    /// <returns>List of waste by product.</returns>
    Task<List<WasteByProductDto>> GetWasteByProductAsync(int? storeId, DateTime fromDate, DateTime toDate, int topCount = 20);

    #endregion

    #region Trend Analysis

    /// <summary>
    /// Gets waste trend data for charting.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="groupBy">Grouping period (day, week, month).</param>
    /// <returns>Trend data points.</returns>
    Task<List<WasteTrendDataDto>> GetWasteTrendsAsync(int? storeId, DateTime fromDate, DateTime toDate, string groupBy = "day");

    /// <summary>
    /// Gets waste comparison between periods.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="currentPeriodStart">Current period start.</param>
    /// <param name="currentPeriodEnd">Current period end.</param>
    /// <param name="previousPeriodStart">Previous period start.</param>
    /// <param name="previousPeriodEnd">Previous period end.</param>
    /// <returns>Period comparison data.</returns>
    Task<WasteComparisonDto> GetWasteComparisonAsync(
        int? storeId,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        DateTime previousPeriodStart,
        DateTime previousPeriodEnd);

    /// <summary>
    /// Gets waste analysis with insights.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Waste analysis with insights.</returns>
    Task<WasteAnalysisDto> GetWasteAnalysisAsync(int? storeId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets waste dashboard data.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Dashboard data.</returns>
    Task<WasteDashboardDto> GetWasteDashboardAsync(int? storeId);

    /// <summary>
    /// Gets period summary for dashboard.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="periodDays">Number of days in period.</param>
    /// <returns>Period summary.</returns>
    Task<WastePeriodSummaryDto> GetPeriodSummaryAsync(int? storeId, int periodDays = 30);

    #endregion

    #region Export

    /// <summary>
    /// Exports waste data for reporting.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>Export data.</returns>
    Task<WasteExportDto> ExportWasteDataAsync(WasteReportQueryDto query);

    /// <summary>
    /// Gets waste export data as CSV content.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>CSV content as string.</returns>
    Task<string> ExportWasteDataAsCsvAsync(WasteReportQueryDto query);

    #endregion

    #region Store Comparison

    /// <summary>
    /// Gets waste comparison across stores.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Store-by-store waste comparison.</returns>
    Task<List<WasteByStoreDto>> GetWasteByStoreAsync(DateTime fromDate, DateTime toDate);

    #endregion
}
