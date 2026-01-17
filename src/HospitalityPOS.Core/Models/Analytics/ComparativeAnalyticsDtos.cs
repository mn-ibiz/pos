namespace HospitalityPOS.Core.Models.Analytics;

/// <summary>
/// Period comparison types for quick selection.
/// </summary>
public enum ComparisonPeriodType
{
    /// <summary>
    /// Custom date ranges.
    /// </summary>
    Custom = 0,

    /// <summary>
    /// This week vs last week.
    /// </summary>
    WeekOverWeek = 1,

    /// <summary>
    /// This month vs last month.
    /// </summary>
    MonthOverMonth = 2,

    /// <summary>
    /// This year vs last year.
    /// </summary>
    YearOverYear = 3,

    /// <summary>
    /// Today vs yesterday.
    /// </summary>
    DayOverDay = 4,

    /// <summary>
    /// This quarter vs last quarter.
    /// </summary>
    QuarterOverQuarter = 5
}

/// <summary>
/// Parameters for requesting a period comparison.
/// </summary>
public class PeriodComparisonRequest
{
    /// <summary>
    /// Gets or sets the comparison type.
    /// </summary>
    public ComparisonPeriodType PeriodType { get; set; } = ComparisonPeriodType.WeekOverWeek;

    /// <summary>
    /// Gets or sets the current period start date (for custom comparisons).
    /// </summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the current period end date (for custom comparisons).
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the previous period start date (for custom comparisons).
    /// </summary>
    public DateTime? PreviousPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the previous period end date (for custom comparisons).
    /// </summary>
    public DateTime? PreviousPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the optional store ID for filtering.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the optional category ID for filtering.
    /// </summary>
    public int? CategoryId { get; set; }
}

/// <summary>
/// Growth metrics showing change between two values.
/// </summary>
public class GrowthMetricsDto
{
    /// <summary>
    /// Gets or sets the current period value.
    /// </summary>
    public decimal CurrentPeriodValue { get; set; }

    /// <summary>
    /// Gets or sets the previous period value.
    /// </summary>
    public decimal PreviousPeriodValue { get; set; }

    /// <summary>
    /// Gets the absolute difference between periods.
    /// </summary>
    public decimal AbsoluteChange => CurrentPeriodValue - PreviousPeriodValue;

    /// <summary>
    /// Gets the percentage change between periods.
    /// </summary>
    public decimal PercentageChange => PreviousPeriodValue != 0
        ? Math.Round(((CurrentPeriodValue - PreviousPeriodValue) / PreviousPeriodValue) * 100, 2)
        : CurrentPeriodValue > 0 ? 100 : 0;

    /// <summary>
    /// Gets whether the change is positive.
    /// </summary>
    public bool IsPositive => AbsoluteChange >= 0;

    /// <summary>
    /// Gets the formatted change string (e.g., "+15.5%" or "-8.2%").
    /// </summary>
    public string FormattedChange => $"{(IsPositive ? "+" : "")}{PercentageChange:N1}%";
}

/// <summary>
/// Complete period comparison result.
/// </summary>
public class PeriodComparisonDto
{
    /// <summary>
    /// Gets or sets the current period start date.
    /// </summary>
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the current period end date.
    /// </summary>
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the previous period start date.
    /// </summary>
    public DateTime PreviousPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the previous period end date.
    /// </summary>
    public DateTime PreviousPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the current period label (e.g., "This Week").
    /// </summary>
    public string CurrentPeriodLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous period label (e.g., "Last Week").
    /// </summary>
    public string PreviousPeriodLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sales comparison.
    /// </summary>
    public GrowthMetricsDto Sales { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction count comparison.
    /// </summary>
    public GrowthMetricsDto Transactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the average ticket comparison.
    /// </summary>
    public GrowthMetricsDto AverageTicket { get; set; } = new();

    /// <summary>
    /// Gets or sets the items sold comparison.
    /// </summary>
    public GrowthMetricsDto ItemsSold { get; set; } = new();

    /// <summary>
    /// Gets or sets the unique customers comparison.
    /// </summary>
    public GrowthMetricsDto UniqueCustomers { get; set; } = new();

    /// <summary>
    /// Gets or sets the gross profit comparison.
    /// </summary>
    public GrowthMetricsDto GrossProfit { get; set; } = new();

    /// <summary>
    /// Gets or sets the discounts given comparison.
    /// </summary>
    public GrowthMetricsDto Discounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the tax collected comparison.
    /// </summary>
    public GrowthMetricsDto TaxCollected { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when data was retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Daily sales data point for trend charts.
/// </summary>
public class DailySalesTrendDto
{
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the formatted date label.
    /// </summary>
    public string DateLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the total sales for the day.
    /// </summary>
    public decimal Sales { get; set; }

    /// <summary>
    /// Gets or sets the transaction count for the day.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average ticket for the day.
    /// </summary>
    public decimal AverageTicket { get; set; }

    /// <summary>
    /// Gets or sets whether this is a comparison period data point.
    /// </summary>
    public bool IsComparisonPeriod { get; set; }
}

/// <summary>
/// Sales trend data with comparison overlay.
/// </summary>
public class SalesTrendComparisonDto
{
    /// <summary>
    /// Gets or sets the current period trend data.
    /// </summary>
    public List<DailySalesTrendDto> CurrentPeriod { get; set; } = [];

    /// <summary>
    /// Gets or sets the previous period trend data.
    /// </summary>
    public List<DailySalesTrendDto> PreviousPeriod { get; set; } = [];

    /// <summary>
    /// Gets or sets the moving average data (7-day by default).
    /// </summary>
    public List<MovingAveragePointDto> MovingAverage { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of days for the moving average.
    /// </summary>
    public int MovingAverageDays { get; set; } = 7;
}

/// <summary>
/// Moving average data point.
/// </summary>
public class MovingAveragePointDto
{
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the moving average value.
    /// </summary>
    public decimal Value { get; set; }
}

/// <summary>
/// Category performance comparison between periods.
/// </summary>
public class CategoryComparisonDto
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current period sales.
    /// </summary>
    public decimal CurrentPeriodSales { get; set; }

    /// <summary>
    /// Gets or sets the previous period sales.
    /// </summary>
    public decimal PreviousPeriodSales { get; set; }

    /// <summary>
    /// Gets or sets the sales growth metrics.
    /// </summary>
    public GrowthMetricsDto SalesGrowth { get; set; } = new();

    /// <summary>
    /// Gets or sets the current period contribution percentage.
    /// </summary>
    public decimal CurrentContributionPercent { get; set; }

    /// <summary>
    /// Gets or sets the previous period contribution percentage.
    /// </summary>
    public decimal PreviousContributionPercent { get; set; }

    /// <summary>
    /// Gets or sets the change in contribution percentage.
    /// </summary>
    public decimal ContributionChange { get; set; }

    /// <summary>
    /// Gets or sets the current period quantity sold.
    /// </summary>
    public decimal CurrentQuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the previous period quantity sold.
    /// </summary>
    public decimal PreviousQuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the quantity growth metrics.
    /// </summary>
    public GrowthMetricsDto QuantityGrowth { get; set; } = new();

    /// <summary>
    /// Gets or sets the rank by growth.
    /// </summary>
    public int GrowthRank { get; set; }
}

/// <summary>
/// Product performance comparison between periods.
/// </summary>
public class ProductComparisonDto
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
    /// Gets or sets the product code/SKU.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current period sales.
    /// </summary>
    public decimal CurrentPeriodSales { get; set; }

    /// <summary>
    /// Gets or sets the previous period sales.
    /// </summary>
    public decimal PreviousPeriodSales { get; set; }

    /// <summary>
    /// Gets or sets the sales growth metrics.
    /// </summary>
    public GrowthMetricsDto SalesGrowth { get; set; } = new();

    /// <summary>
    /// Gets or sets the current period quantity sold.
    /// </summary>
    public decimal CurrentQuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the previous period quantity sold.
    /// </summary>
    public decimal PreviousQuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the quantity growth metrics.
    /// </summary>
    public GrowthMetricsDto QuantityGrowth { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the product is new (only in current period).
    /// </summary>
    public bool IsNewProduct { get; set; }

    /// <summary>
    /// Gets or sets whether the product was discontinued (only in previous period).
    /// </summary>
    public bool IsDiscontinued { get; set; }

    /// <summary>
    /// Gets or sets the rank by growth (1 = top gainer).
    /// </summary>
    public int GrowthRank { get; set; }
}

/// <summary>
/// Top gainers and losers summary.
/// </summary>
public class TopMoversDto
{
    /// <summary>
    /// Gets or sets the top gaining products.
    /// </summary>
    public List<ProductComparisonDto> TopGainers { get; set; } = [];

    /// <summary>
    /// Gets or sets the top declining products.
    /// </summary>
    public List<ProductComparisonDto> TopLosers { get; set; } = [];

    /// <summary>
    /// Gets or sets new products in the current period.
    /// </summary>
    public List<ProductComparisonDto> NewProducts { get; set; } = [];

    /// <summary>
    /// Gets or sets products discontinued in the current period.
    /// </summary>
    public List<ProductComparisonDto> DiscontinuedProducts { get; set; } = [];
}

/// <summary>
/// Day-of-week pattern analysis.
/// </summary>
public class DayOfWeekPatternDto
{
    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the day name.
    /// </summary>
    public string DayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short day name (e.g., "Mon").
    /// </summary>
    public string ShortDayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the average sales for this day of week.
    /// </summary>
    public decimal AverageSales { get; set; }

    /// <summary>
    /// Gets or sets the average transactions for this day of week.
    /// </summary>
    public decimal AverageTransactions { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total weekly sales.
    /// </summary>
    public decimal PercentOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the heat intensity (0-1) for visualization.
    /// </summary>
    public decimal HeatIntensity { get; set; }

    /// <summary>
    /// Gets or sets the color code based on intensity (for heat map).
    /// </summary>
    public string ColorCode { get; set; } = string.Empty;
}

/// <summary>
/// Hour-by-day heat map data.
/// </summary>
public class HourlyPatternDto
{
    /// <summary>
    /// Gets or sets the hour (0-23).
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the formatted hour label.
    /// </summary>
    public string HourLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the patterns by day of week.
    /// </summary>
    public List<DayHourValueDto> DayPatterns { get; set; } = [];
}

/// <summary>
/// Single day-hour value for heat map.
/// </summary>
public class DayHourValueDto
{
    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the hour.
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the average sales value.
    /// </summary>
    public decimal AverageSales { get; set; }

    /// <summary>
    /// Gets or sets the heat intensity (0-1).
    /// </summary>
    public decimal HeatIntensity { get; set; }

    /// <summary>
    /// Gets or sets the color code for heat map.
    /// </summary>
    public string ColorCode { get; set; } = string.Empty;
}

/// <summary>
/// Sparkline data for quick trend visualization.
/// </summary>
public class SparklineDataDto
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the formatted current value.
    /// </summary>
    public string FormattedValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trend direction (up, down, flat).
    /// </summary>
    public TrendDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the percentage change.
    /// </summary>
    public decimal PercentageChange { get; set; }

    /// <summary>
    /// Gets or sets the data points for the sparkline.
    /// </summary>
    public List<decimal> DataPoints { get; set; } = [];
}

/// <summary>
/// Trend direction indicator.
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Upward trend.
    /// </summary>
    Up = 1,

    /// <summary>
    /// Downward trend.
    /// </summary>
    Down = -1,

    /// <summary>
    /// Flat/no significant change.
    /// </summary>
    Flat = 0
}

/// <summary>
/// Complete comparative analytics result.
/// </summary>
public class ComparativeAnalyticsDto
{
    /// <summary>
    /// Gets or sets the period comparison summary.
    /// </summary>
    public PeriodComparisonDto PeriodComparison { get; set; } = new();

    /// <summary>
    /// Gets or sets the sales trend with comparison.
    /// </summary>
    public SalesTrendComparisonDto SalesTrend { get; set; } = new();

    /// <summary>
    /// Gets or sets the category comparisons.
    /// </summary>
    public List<CategoryComparisonDto> CategoryComparisons { get; set; } = [];

    /// <summary>
    /// Gets or sets the top movers summary.
    /// </summary>
    public TopMoversDto TopMovers { get; set; } = new();

    /// <summary>
    /// Gets or sets the day-of-week patterns.
    /// </summary>
    public List<DayOfWeekPatternDto> DayOfWeekPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the sparkline data for key metrics.
    /// </summary>
    public List<SparklineDataDto> Sparklines { get; set; } = [];

    /// <summary>
    /// Gets or sets when the analytics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Export data container for Excel export.
/// </summary>
public class ComparativeAnalyticsExportDto
{
    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string ReportTitle { get; set; } = "Comparative Analytics Report";

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the store name (if filtered).
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Gets or sets the complete analytics data.
    /// </summary>
    public ComparativeAnalyticsDto Data { get; set; } = new();
}
