namespace HospitalityPOS.Core.Models.Dashboard;

/// <summary>
/// Today's sales summary for dashboard display.
/// </summary>
public class TodaySalesSummaryDto
{
    /// <summary>
    /// Gets or sets total sales amount for today.
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Gets or sets the transaction count for today.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTicket { get; set; }

    /// <summary>
    /// Gets or sets the gross sales (before discounts).
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total discounts applied.
    /// </summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>
    /// Gets or sets the tax collected.
    /// </summary>
    public decimal TaxCollected { get; set; }

    /// <summary>
    /// Gets or sets the number of items sold.
    /// </summary>
    public int ItemsSold { get; set; }

    /// <summary>
    /// Gets or sets the data timestamp.
    /// </summary>
    public DateTime AsOf { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Hourly sales data for chart display.
/// </summary>
public class HourlySalesDto
{
    /// <summary>
    /// Gets or sets the hour (0-23).
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the formatted hour label (e.g., "9 AM").
    /// </summary>
    public string HourLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sales amount for the hour.
    /// </summary>
    public decimal Sales { get; set; }

    /// <summary>
    /// Gets or sets the transaction count for the hour.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current hour.
    /// </summary>
    public bool IsCurrentHour { get; set; }
}

/// <summary>
/// Top selling product for dashboard display.
/// </summary>
public class TopSellingProductDto
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity sold today.
    /// </summary>
    public decimal QuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the revenue generated today.
    /// </summary>
    public decimal Revenue { get; set; }

    /// <summary>
    /// Gets or sets the rank (1 = top seller).
    /// </summary>
    public int Rank { get; set; }
}

/// <summary>
/// Payment method breakdown for dashboard display.
/// </summary>
public class PaymentMethodBreakdownDto
{
    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payment method name.
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount for this payment method.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction count for this payment method.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total sales.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets the color code for chart display (e.g., "#4CAF50").
    /// </summary>
    public string ColorCode { get; set; } = string.Empty;
}

/// <summary>
/// Comparison metrics (vs yesterday and last week).
/// </summary>
public class ComparisonMetricsDto
{
    /// <summary>
    /// Gets or sets yesterday's sales at the same time.
    /// </summary>
    public decimal YesterdaySales { get; set; }

    /// <summary>
    /// Gets or sets the percentage change vs yesterday.
    /// </summary>
    public decimal VsYesterdayPercent { get; set; }

    /// <summary>
    /// Gets or sets whether today is performing better than yesterday.
    /// </summary>
    public bool IsBetterThanYesterday { get; set; }

    /// <summary>
    /// Gets or sets last week same day's sales at the same time.
    /// </summary>
    public decimal LastWeekSameDaySales { get; set; }

    /// <summary>
    /// Gets or sets the percentage change vs last week same day.
    /// </summary>
    public decimal VsLastWeekPercent { get; set; }

    /// <summary>
    /// Gets or sets whether today is performing better than last week.
    /// </summary>
    public bool IsBetterThanLastWeek { get; set; }

    /// <summary>
    /// Gets or sets yesterday's transaction count at the same time.
    /// </summary>
    public int YesterdayTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the percentage change in transactions vs yesterday.
    /// </summary>
    public decimal TransactionVsYesterdayPercent { get; set; }

    /// <summary>
    /// Gets or sets yesterday's average ticket at the same time.
    /// </summary>
    public decimal YesterdayAverageTicket { get; set; }

    /// <summary>
    /// Gets or sets the percentage change in average ticket vs yesterday.
    /// </summary>
    public decimal AvgTicketVsYesterdayPercent { get; set; }
}

/// <summary>
/// Low stock alert data for dashboard.
/// </summary>
public class LowStockAlertDto
{
    /// <summary>
    /// Gets or sets the total count of low stock products.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the count of out-of-stock products.
    /// </summary>
    public int OutOfStockCount { get; set; }

    /// <summary>
    /// Gets or sets the list of low stock items.
    /// </summary>
    public List<LowStockItemDto> Items { get; set; } = [];
}

/// <summary>
/// Individual low stock item.
/// </summary>
public class LowStockItemDto
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal MinimumStock { get; set; }

    /// <summary>
    /// Gets or sets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock { get; set; }
}

/// <summary>
/// Expiry alert data for dashboard.
/// </summary>
public class ExpiryAlertDto
{
    /// <summary>
    /// Gets or sets the total count of expiring products.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the count of already expired products.
    /// </summary>
    public int ExpiredCount { get; set; }

    /// <summary>
    /// Gets or sets whether batch tracking is enabled.
    /// </summary>
    public bool IsBatchTrackingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the list of expiring items.
    /// </summary>
    public List<ExpiringItemDto> Items { get; set; } = [];
}

/// <summary>
/// Individual expiring item.
/// </summary>
public class ExpiringItemDto
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public int BatchId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch number.
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiry date.
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the days until expiry (negative if expired).
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Gets or sets the quantity in this batch.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets whether the batch is already expired.
    /// </summary>
    public bool IsExpired { get; set; }
}

/// <summary>
/// Sync status for offline mode indication.
/// </summary>
public class SyncStatusDto
{
    /// <summary>
    /// Gets or sets the count of pending sync items.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Gets or sets whether the system is currently online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the last successful sync time.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Gets or sets whether sync is in progress.
    /// </summary>
    public bool IsSyncing { get; set; }
}

/// <summary>
/// Complete dashboard data container.
/// </summary>
public class DashboardDataDto
{
    /// <summary>
    /// Gets or sets today's sales summary.
    /// </summary>
    public TodaySalesSummaryDto SalesSummary { get; set; } = new();

    /// <summary>
    /// Gets or sets the hourly sales breakdown.
    /// </summary>
    public List<HourlySalesDto> HourlySales { get; set; } = [];

    /// <summary>
    /// Gets or sets the top selling products.
    /// </summary>
    public List<TopSellingProductDto> TopProducts { get; set; } = [];

    /// <summary>
    /// Gets or sets the payment method breakdown.
    /// </summary>
    public List<PaymentMethodBreakdownDto> PaymentBreakdown { get; set; } = [];

    /// <summary>
    /// Gets or sets the comparison metrics.
    /// </summary>
    public ComparisonMetricsDto Comparison { get; set; } = new();

    /// <summary>
    /// Gets or sets the low stock alerts.
    /// </summary>
    public LowStockAlertDto LowStockAlerts { get; set; } = new();

    /// <summary>
    /// Gets or sets the expiry alerts.
    /// </summary>
    public ExpiryAlertDto ExpiryAlerts { get; set; } = new();

    /// <summary>
    /// Gets or sets the sync status.
    /// </summary>
    public SyncStatusDto SyncStatus { get; set; } = new();

    /// <summary>
    /// Gets or sets when the data was retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the selected store ID (null = all stores).
    /// </summary>
    public int? StoreId { get; set; }
}

/// <summary>
/// Branch summary for multi-branch comparison.
/// </summary>
public class BranchSummaryDto
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets today's total sales.
    /// </summary>
    public decimal TodaySales { get; set; }

    /// <summary>
    /// Gets or sets today's transaction count.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the percentage change vs yesterday.
    /// </summary>
    public decimal VsYesterdayPercent { get; set; }

    /// <summary>
    /// Gets or sets whether the store is currently online.
    /// </summary>
    public bool IsOnline { get; set; }
}
