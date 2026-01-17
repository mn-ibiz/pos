using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for consolidated chain-wide reporting and analytics.
/// </summary>
public interface IChainReportingService
{
    // ================== Dashboard ==================

    /// <summary>
    /// Gets the chain-wide dashboard metrics.
    /// </summary>
    Task<ChainDashboardMetricsDto> GetChainDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of all stores with their current status.
    /// </summary>
    Task<IEnumerable<StoreSummaryDto>> GetAllStoresSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed summary for a specific store.
    /// </summary>
    Task<StoreSummaryDto?> GetStoreSummaryAsync(int storeId, CancellationToken cancellationToken = default);

    // ================== Store Comparison ==================

    /// <summary>
    /// Gets store comparison report with rankings.
    /// </summary>
    Task<StoreComparisonReportDto> GetStoreComparisonReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top performing stores for a given period.
    /// </summary>
    Task<IEnumerable<StoreRankingDto>> GetTopPerformingStoresAsync(int topN = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets underperforming stores that need attention.
    /// </summary>
    Task<IEnumerable<StoreRankingDto>> GetUnderperformingStoresAsync(int bottomN = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    // ================== Product Performance ==================

    /// <summary>
    /// Gets product performance report across all stores.
    /// </summary>
    Task<ProductPerformanceReportDto> GetProductPerformanceReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top selling products chain-wide.
    /// </summary>
    Task<IEnumerable<ProductPerformanceDto>> GetTopSellingProductsAsync(int topN = 20, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product performance by store breakdown.
    /// </summary>
    Task<ProductPerformanceDto?> GetProductPerformanceByStoreAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    // ================== Category Performance ==================

    /// <summary>
    /// Gets category performance report across all stores.
    /// </summary>
    Task<CategoryPerformanceReportDto> GetCategoryPerformanceReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category performance by store breakdown.
    /// </summary>
    Task<CategoryPerformanceDto?> GetCategoryPerformanceByStoreAsync(int categoryId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    // ================== Sales Trends ==================

    /// <summary>
    /// Gets sales trend report over a time period.
    /// </summary>
    Task<SalesTrendReportDto> GetSalesTrendReportAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily sales trends.
    /// </summary>
    Task<IEnumerable<DailySalesTrendDto>> GetDailySalesTrendsAsync(DateTime fromDate, DateTime toDate, List<int>? storeIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hourly sales patterns (average across period).
    /// </summary>
    Task<IEnumerable<HourlySalesPatternDto>> GetHourlySalesPatternsAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default);

    // ================== Financial Summary ==================

    /// <summary>
    /// Gets chain-wide financial summary.
    /// </summary>
    Task<ChainFinancialSummaryDto> GetChainFinancialSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment method breakdown across the chain.
    /// </summary>
    Task<IEnumerable<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdownAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default);

    // ================== Inventory ==================

    /// <summary>
    /// Gets chain-wide inventory status.
    /// </summary>
    Task<ChainInventoryStatusDto> GetChainInventoryStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets low stock alerts across all stores.
    /// </summary>
    Task<IEnumerable<LowStockAlertDto>> GetLowStockAlertsAsync(CancellationToken cancellationToken = default);
}
