using HospitalityPOS.Core.Models.Dashboard;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for real-time dashboard data retrieval.
/// Provides optimized queries for live dashboard updates.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets today's sales summary with live totals.
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Today's sales summary.</returns>
    Task<TodaySalesSummaryDto> GetTodaySalesSummaryAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hourly sales breakdown for today.
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of hourly sales data.</returns>
    Task<List<HourlySalesDto>> GetHourlySalesBreakdownAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top selling products for today.
    /// </summary>
    /// <param name="count">Number of products to return (default 10).</param>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of top selling products.</returns>
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count = 10,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment method breakdown for today.
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of payment method totals.</returns>
    Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdownAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comparison metrics (vs yesterday and last week same day).
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison metrics.</returns>
    Task<ComparisonMetricsDto> GetComparisonMetricsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets low stock alerts count and details.
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="maxItems">Maximum number of alert items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Low stock alert data.</returns>
    Task<LowStockAlertDto> GetLowStockAlertsAsync(
        int? storeId = null,
        int maxItems = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expiring product alerts (if batch tracking enabled).
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="daysThreshold">Days threshold for expiry warning (default 30).</param>
    /// <param name="maxItems">Maximum number of alert items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expiry alert data.</returns>
    Task<ExpiryAlertDto> GetExpiryAlertsAsync(
        int? storeId = null,
        int daysThreshold = 30,
        int maxItems = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending sync items count (for offline mode).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync status data.</returns>
    Task<SyncStatusDto> GetSyncStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complete dashboard data in a single optimized call.
    /// </summary>
    /// <param name="storeId">Optional store ID for multi-branch filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete dashboard data.</returns>
    Task<DashboardDataDto> GetDashboardDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets branch summary data for multi-branch comparison.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of branch summaries.</returns>
    Task<List<BranchSummaryDto>> GetBranchSummariesAsync(
        CancellationToken cancellationToken = default);
}
