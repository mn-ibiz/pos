namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Chain-wide dashboard metrics for HQ monitoring.
/// </summary>
public class ChainDashboardMetricsDto
{
    public decimal TotalSalesToday { get; set; }
    public decimal TotalSalesYesterday { get; set; }
    public decimal TotalSalesThisWeek { get; set; }
    public decimal TotalSalesLastWeek { get; set; }
    public decimal TotalSalesThisMonth { get; set; }
    public decimal TotalSalesLastMonth { get; set; }
    public int TransactionCountToday { get; set; }
    public int TransactionCountThisWeek { get; set; }
    public int TransactionCountThisMonth { get; set; }
    public decimal AverageBasketSizeToday { get; set; }
    public decimal AverageBasketSizeThisWeek { get; set; }
    public decimal AverageBasketSizeThisMonth { get; set; }
    public decimal TodayGrowthPercent { get; set; }
    public decimal WeekGrowthPercent { get; set; }
    public decimal MonthGrowthPercent { get; set; }
    public int TotalStores { get; set; }
    public int OnlineStores { get; set; }
    public int OfflineStores { get; set; }
    public List<StoreSummaryDto> StoreBreakdown { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCategoryDto> TopCategories { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Summary of a single store's performance.
/// </summary>
public class StoreSummaryDto
{
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal SalesToday { get; set; }
    public decimal SalesThisWeek { get; set; }
    public decimal SalesThisMonth { get; set; }
    public int TransactionsToday { get; set; }
    public int TransactionsThisWeek { get; set; }
    public int TransactionsThisMonth { get; set; }
    public decimal AverageBasketToday { get; set; }
    public decimal AverageBasketThisWeek { get; set; }
    public decimal AverageBasketThisMonth { get; set; }
    public decimal GrossMargin { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int PendingSyncCount { get; set; }
}

/// <summary>
/// Store comparison report for ranking stores.
/// </summary>
public class StoreComparisonReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalStores { get; set; }
    public decimal TotalChainSales { get; set; }
    public decimal TotalChainTransactions { get; set; }
    public decimal AverageStoreSales { get; set; }
    public List<StoreRankingDto> Rankings { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Store ranking with comparison metrics.
/// </summary>
public class StoreRankingDto
{
    public int Rank { get; set; }
    public int PreviousRank { get; set; }
    public int RankChange => PreviousRank > 0 ? PreviousRank - Rank : 0;
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal PreviousPeriodSales { get; set; }
    public decimal SalesGrowthPercent { get; set; }
    public int Transactions { get; set; }
    public decimal AverageBasket { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ContributionPercent { get; set; }
    public int ItemsSold { get; set; }
    public decimal ReturnsAmount { get; set; }
    public decimal VoidsAmount { get; set; }
    public decimal DiscountsGiven { get; set; }
}

/// <summary>
/// Product performance report across chain.
/// </summary>
public class ProductPerformanceReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalQuantitySold { get; set; }
    public List<ProductPerformanceDto> Products { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Performance metrics for a single product across the chain.
/// </summary>
public class ProductPerformanceDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossProfit { get; set; }
    public int StoresSellingCount { get; set; }
    public List<ProductStoreBreakdownDto> StoreBreakdown { get; set; } = new();
}

/// <summary>
/// Product performance at a specific store.
/// </summary>
public class ProductStoreBreakdownDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal ContributionPercent { get; set; }
}

/// <summary>
/// Category performance across the chain.
/// </summary>
public class CategoryPerformanceReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<CategoryPerformanceDto> Categories { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Performance metrics for a category.
/// </summary>
public class CategoryPerformanceDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal ContributionPercent { get; set; }
    public List<CategoryStoreBreakdownDto> StoreBreakdown { get; set; } = new();
}

/// <summary>
/// Category performance at a specific store.
/// </summary>
public class CategoryStoreBreakdownDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal ContributionPercent { get; set; }
}

/// <summary>
/// Top selling product summary.
/// </summary>
public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Top selling category summary.
/// </summary>
public class TopCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Daily sales trend data point.
/// </summary>
public class DailySalesTrendDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
    public decimal AverageBasket { get; set; }
}

/// <summary>
/// Hourly sales pattern data point.
/// </summary>
public class HourlySalesPatternDto
{
    public int Hour { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
    public decimal AverageSales { get; set; }
    public int AverageTransactions { get; set; }
}

/// <summary>
/// Sales trend report over time.
/// </summary>
public class SalesTrendReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DailySalesTrendDto> DailyTrends { get; set; } = new();
    public List<HourlySalesPatternDto> HourlyPatterns { get; set; } = new();
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageBasket { get; set; }
    public decimal BestDaySales { get; set; }
    public DateTime? BestDayDate { get; set; }
    public int PeakHour { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Query parameters for chain reports.
/// </summary>
public class ChainReportQueryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<int>? StoreIds { get; set; }
    public List<int>? ZoneIds { get; set; }
    public List<int>? CategoryIds { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
    public int? TopN { get; set; }
}

/// <summary>
/// Inventory status across chain.
/// </summary>
public class ChainInventoryStatusDto
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<StoreInventorySummaryDto> StoreBreakdown { get; set; } = new();
    public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Inventory summary for a store.
/// </summary>
public class StoreInventorySummaryDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal InventoryValue { get; set; }
}

/// <summary>
/// Low stock alert for a product at a store.
/// </summary>
public class LowStockAlertDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsOutOfStock { get; set; }
    public int DaysSinceLastRestock { get; set; }
}

/// <summary>
/// Payment method breakdown across chain.
/// </summary>
public class PaymentMethodBreakdownDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ContributionPercent { get; set; }
}

/// <summary>
/// Financial summary for the chain.
/// </summary>
public class ChainFinancialSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Returns { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public List<PaymentMethodBreakdownDto> PaymentBreakdown { get; set; } = new();
    public List<StoreSummaryDto> StoreBreakdown { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}
