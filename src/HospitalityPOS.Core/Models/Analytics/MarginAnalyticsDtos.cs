namespace HospitalityPOS.Core.Models.Analytics;

/// <summary>
/// Represents the health status of a margin.
/// </summary>
public enum MarginHealth
{
    /// <summary>Low margin (below 15%).</summary>
    Low,
    /// <summary>Medium margin (15-30%).</summary>
    Medium,
    /// <summary>Good margin (above 30%).</summary>
    Good
}

/// <summary>
/// Request for margin analysis reports.
/// </summary>
public class MarginReportRequest
{
    /// <summary>Start date for report period.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End date for report period.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Optional category filter.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Optional supplier filter.</summary>
    public int? SupplierId { get; set; }

    /// <summary>Optional store filter.</summary>
    public int? StoreId { get; set; }

    /// <summary>Minimum margin threshold for alerts (default 15%).</summary>
    public decimal MinimumMarginThreshold { get; set; } = 15.0m;

    /// <summary>Include only products with sales in the period.</summary>
    public bool OnlyWithSales { get; set; } = true;
}

/// <summary>
/// Margin data for a single product.
/// </summary>
public class ProductMarginDto
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product code.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Category ID.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Current selling price.</summary>
    public decimal SellingPrice { get; set; }

    /// <summary>Current cost price.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Absolute margin (selling price - cost price).</summary>
    public decimal Margin => SellingPrice - CostPrice;

    /// <summary>Margin as percentage of selling price.</summary>
    public decimal MarginPercent => SellingPrice > 0
        ? Math.Round((Margin / SellingPrice) * 100, 2)
        : 0;

    /// <summary>Health indicator based on margin percentage.</summary>
    public MarginHealth Health => MarginPercent switch
    {
        < 15 => MarginHealth.Low,
        < 30 => MarginHealth.Medium,
        _ => MarginHealth.Good
    };

    /// <summary>Color code for UI display.</summary>
    public string HealthColor => Health switch
    {
        MarginHealth.Low => "#EF4444",    // Red
        MarginHealth.Medium => "#F59E0B", // Yellow/Amber
        _ => "#22C55E"                     // Green
    };

    /// <summary>Units sold in the period.</summary>
    public decimal UnitsSold { get; set; }

    /// <summary>Total revenue from this product.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Total cost for items sold.</summary>
    public decimal TotalCost => UnitsSold * CostPrice;

    /// <summary>Total profit contribution from this product.</summary>
    public decimal TotalProfit => TotalRevenue - TotalCost;

    /// <summary>Percentage contribution to overall profit.</summary>
    public decimal ProfitContributionPercent { get; set; }

    /// <summary>Whether margin is below the configured threshold.</summary>
    public bool IsBelowThreshold { get; set; }

    /// <summary>Formatted margin display.</summary>
    public string FormattedMargin => $"KSh {Margin:N0} ({MarginPercent:N1}%)";
}

/// <summary>
/// Aggregated margin data for a category.
/// </summary>
public class CategoryMarginDto
{
    /// <summary>Category ID.</summary>
    public int CategoryId { get; set; }

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Number of products in category.</summary>
    public int ProductCount { get; set; }

    /// <summary>Number of products with cost prices defined.</summary>
    public int ProductsWithCostPrice { get; set; }

    /// <summary>Total revenue for the category.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Total cost for the category.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Total profit for the category.</summary>
    public decimal TotalProfit => TotalRevenue - TotalCost;

    /// <summary>Average margin percentage across all products.</summary>
    public decimal AverageMarginPercent { get; set; }

    /// <summary>Weighted margin percentage (based on sales).</summary>
    public decimal WeightedMarginPercent => TotalRevenue > 0
        ? Math.Round((TotalProfit / TotalRevenue) * 100, 2)
        : 0;

    /// <summary>Health indicator based on weighted margin.</summary>
    public MarginHealth Health => WeightedMarginPercent switch
    {
        < 15 => MarginHealth.Low,
        < 30 => MarginHealth.Medium,
        _ => MarginHealth.Good
    };

    /// <summary>Color code for UI display.</summary>
    public string HealthColor => Health switch
    {
        MarginHealth.Low => "#EF4444",
        MarginHealth.Medium => "#F59E0B",
        _ => "#22C55E"
    };

    /// <summary>Percentage contribution to overall profit.</summary>
    public decimal ProfitContributionPercent { get; set; }

    /// <summary>Ranking by profitability (1 = highest).</summary>
    public int ProfitabilityRank { get; set; }

    /// <summary>Number of low-margin products in category.</summary>
    public int LowMarginProductCount { get; set; }
}

/// <summary>
/// Gross profit summary for a period.
/// </summary>
public class GrossProfitSummaryDto
{
    /// <summary>Report period start date.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Report period end date.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Total revenue for the period.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Total cost of goods sold.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Gross profit amount.</summary>
    public decimal GrossProfit => TotalRevenue - TotalCost;

    /// <summary>Gross profit percentage.</summary>
    public decimal GrossProfitPercent => TotalRevenue > 0
        ? Math.Round((GrossProfit / TotalRevenue) * 100, 2)
        : 0;

    /// <summary>Total number of transactions.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Total units sold.</summary>
    public decimal TotalUnitsSold { get; set; }

    /// <summary>Average profit per transaction.</summary>
    public decimal AverageProfitPerTransaction => TransactionCount > 0
        ? Math.Round(GrossProfit / TransactionCount, 2)
        : 0;

    /// <summary>Average margin percentage.</summary>
    public decimal AverageMarginPercent { get; set; }

    /// <summary>Formatted gross profit display.</summary>
    public string FormattedGrossProfit => $"KSh {GrossProfit:N0} ({GrossProfitPercent:N1}%)";
}

/// <summary>
/// Low margin alert for a product.
/// </summary>
public class LowMarginAlertDto
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product code.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Current margin percentage.</summary>
    public decimal CurrentMarginPercent { get; set; }

    /// <summary>Configured threshold.</summary>
    public decimal ThresholdPercent { get; set; }

    /// <summary>Gap below threshold.</summary>
    public decimal GapPercent => ThresholdPercent - CurrentMarginPercent;

    /// <summary>Selling price.</summary>
    public decimal SellingPrice { get; set; }

    /// <summary>Cost price.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Suggested selling price to meet threshold.</summary>
    public decimal SuggestedPrice => ThresholdPercent > 0 && ThresholdPercent < 100
        ? Math.Round(CostPrice / (1 - (ThresholdPercent / 100)), 2)
        : CostPrice;

    /// <summary>Price increase needed.</summary>
    public decimal PriceIncreaseNeeded => SuggestedPrice - SellingPrice;

    /// <summary>Alert severity based on how far below threshold.</summary>
    public AlertSeverity Severity => GapPercent switch
    {
        > 10 => AlertSeverity.Critical,
        > 5 => AlertSeverity.High,
        _ => AlertSeverity.Medium
    };

    /// <summary>Units sold in recent period.</summary>
    public decimal RecentUnitsSold { get; set; }

    /// <summary>Potential profit loss due to low margin.</summary>
    public decimal PotentialProfitLoss => RecentUnitsSold * PriceIncreaseNeeded;
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Medium severity - slightly below threshold.</summary>
    Medium,
    /// <summary>High severity - significantly below threshold.</summary>
    High,
    /// <summary>Critical severity - very low margin.</summary>
    Critical
}

/// <summary>
/// Margin trend data point.
/// </summary>
public class MarginTrendPointDto
{
    /// <summary>Date of the data point.</summary>
    public DateTime Date { get; set; }

    /// <summary>Average margin percentage for the period.</summary>
    public decimal AverageMarginPercent { get; set; }

    /// <summary>Total revenue for the period.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Total cost for the period.</summary>
    public decimal Cost { get; set; }

    /// <summary>Gross profit for the period.</summary>
    public decimal GrossProfit => Revenue - Cost;

    /// <summary>Gross profit percentage.</summary>
    public decimal GrossProfitPercent => Revenue > 0
        ? Math.Round((GrossProfit / Revenue) * 100, 2)
        : 0;
}

/// <summary>
/// Margin trend analysis for a product.
/// </summary>
public class ProductMarginTrendDto
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Current margin percentage.</summary>
    public decimal CurrentMarginPercent { get; set; }

    /// <summary>Previous period margin percentage.</summary>
    public decimal PreviousMarginPercent { get; set; }

    /// <summary>Change in margin percentage.</summary>
    public decimal MarginChange => CurrentMarginPercent - PreviousMarginPercent;

    /// <summary>Trend direction.</summary>
    public TrendDirection Trend => MarginChange switch
    {
        > 0.5m => TrendDirection.Up,
        < -0.5m => TrendDirection.Down,
        _ => TrendDirection.Stable
    };

    /// <summary>Current cost price.</summary>
    public decimal CurrentCostPrice { get; set; }

    /// <summary>Previous cost price.</summary>
    public decimal PreviousCostPrice { get; set; }

    /// <summary>Cost price change.</summary>
    public decimal CostPriceChange => CurrentCostPrice - PreviousCostPrice;

    /// <summary>Whether this is a cost increase alert.</summary>
    public bool IsCostIncreaseAlert => CostPriceChange > 0 && MarginChange < -2;

    /// <summary>Historical trend data points.</summary>
    public List<MarginTrendPointDto> TrendData { get; set; } = [];
}

/// <summary>
/// Cost price history record.
/// </summary>
public class CostPriceHistoryDto
{
    /// <summary>History record ID.</summary>
    public int Id { get; set; }

    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Cost price at this point in time.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Effective date of the cost price.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>Supplier ID (if from GRN).</summary>
    public int? SupplierId { get; set; }

    /// <summary>Supplier name.</summary>
    public string? SupplierName { get; set; }

    /// <summary>GRN ID (if from goods receipt).</summary>
    public int? GoodsReceivedId { get; set; }

    /// <summary>GRN number.</summary>
    public string? GRNNumber { get; set; }

    /// <summary>Previous cost price (for comparison).</summary>
    public decimal? PreviousCostPrice { get; set; }

    /// <summary>Change from previous cost.</summary>
    public decimal? CostChange => PreviousCostPrice.HasValue
        ? CostPrice - PreviousCostPrice.Value
        : null;

    /// <summary>Percentage change from previous.</summary>
    public decimal? CostChangePercent => PreviousCostPrice.HasValue && PreviousCostPrice.Value > 0
        ? Math.Round(((CostPrice - PreviousCostPrice.Value) / PreviousCostPrice.Value) * 100, 2)
        : null;
}

/// <summary>
/// Complete margin analytics report.
/// </summary>
public class MarginAnalyticsReportDto
{
    /// <summary>Report request parameters.</summary>
    public MarginReportRequest Request { get; set; } = new();

    /// <summary>Gross profit summary.</summary>
    public GrossProfitSummaryDto GrossProfitSummary { get; set; } = new();

    /// <summary>Category margin summaries.</summary>
    public List<CategoryMarginDto> CategoryMargins { get; set; } = [];

    /// <summary>Product margins.</summary>
    public List<ProductMarginDto> ProductMargins { get; set; } = [];

    /// <summary>Low margin alerts.</summary>
    public List<LowMarginAlertDto> LowMarginAlerts { get; set; } = [];

    /// <summary>Margin trend over time.</summary>
    public List<MarginTrendPointDto> MarginTrend { get; set; } = [];

    /// <summary>Products with declining margins.</summary>
    public List<ProductMarginTrendDto> DecliningMarginProducts { get; set; } = [];

    /// <summary>Report generation timestamp.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Total number of products analyzed.</summary>
    public int TotalProductsAnalyzed { get; set; }

    /// <summary>Number of products with cost prices.</summary>
    public int ProductsWithCostPrice { get; set; }

    /// <summary>Number of low margin products.</summary>
    public int LowMarginProductCount { get; set; }

    /// <summary>Coverage percentage (products with cost price).</summary>
    public decimal CostPriceCoverage => TotalProductsAnalyzed > 0
        ? Math.Round((decimal)ProductsWithCostPrice / TotalProductsAnalyzed * 100, 1)
        : 0;
}
