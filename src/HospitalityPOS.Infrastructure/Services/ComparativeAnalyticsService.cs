using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for comparative analytics and period-over-period analysis.
/// </summary>
public class ComparativeAnalyticsService : IComparativeAnalyticsService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComparativeAnalyticsService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public ComparativeAnalyticsService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Period Comparison

    /// <inheritdoc />
    public async Task<PeriodComparisonDto> GetPeriodComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodDates(request);

        _logger.Information(
            "Generating period comparison: Current {CurrentStart:d} to {CurrentEnd:d}, Previous {PreviousStart:d} to {PreviousEnd:d}",
            currentStart, currentEnd, previousStart, previousEnd);

        // Get current period metrics
        var currentMetrics = await GetPeriodMetricsAsync(currentStart, currentEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        // Get previous period metrics
        var previousMetrics = await GetPeriodMetricsAsync(previousStart, previousEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        return new PeriodComparisonDto
        {
            CurrentPeriodStart = currentStart,
            CurrentPeriodEnd = currentEnd,
            PreviousPeriodStart = previousStart,
            PreviousPeriodEnd = previousEnd,
            CurrentPeriodLabel = GetPeriodLabel(request.PeriodType, true),
            PreviousPeriodLabel = GetPeriodLabel(request.PeriodType, false),
            Sales = CreateGrowthMetrics(currentMetrics.TotalSales, previousMetrics.TotalSales),
            Transactions = CreateGrowthMetrics(currentMetrics.TransactionCount, previousMetrics.TransactionCount),
            AverageTicket = CreateGrowthMetrics(currentMetrics.AverageTicket, previousMetrics.AverageTicket),
            ItemsSold = CreateGrowthMetrics(currentMetrics.ItemsSold, previousMetrics.ItemsSold),
            UniqueCustomers = CreateGrowthMetrics(currentMetrics.UniqueCustomers, previousMetrics.UniqueCustomers),
            GrossProfit = CreateGrowthMetrics(currentMetrics.GrossProfit, previousMetrics.GrossProfit),
            Discounts = CreateGrowthMetrics(currentMetrics.Discounts, previousMetrics.Discounts),
            TaxCollected = CreateGrowthMetrics(currentMetrics.TaxCollected, previousMetrics.TaxCollected),
            RetrievedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<GrowthMetricsDto> CalculateGrowthMetricsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var currentMetrics = await GetPeriodMetricsAsync(currentStart, currentEnd, storeId, cancellationToken)
            .ConfigureAwait(false);
        var previousMetrics = await GetPeriodMetricsAsync(previousStart, previousEnd, storeId, cancellationToken)
            .ConfigureAwait(false);

        return CreateGrowthMetrics(currentMetrics.TotalSales, previousMetrics.TotalSales);
    }

    #endregion

    #region Trend Analysis

    /// <inheritdoc />
    public async Task<SalesTrendComparisonDto> GetSalesTrendComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodDates(request);

        var currentTrend = await GetDailySalesTrendAsync(currentStart, currentEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        var previousTrend = await GetDailySalesTrendAsync(previousStart, previousEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        // Mark comparison period data
        foreach (var point in previousTrend)
        {
            point.IsComparisonPeriod = true;
        }

        var movingAverage = await GetMovingAverageAsync(currentStart, currentEnd, 7, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        return new SalesTrendComparisonDto
        {
            CurrentPeriod = currentTrend,
            PreviousPeriod = previousTrend,
            MovingAverage = movingAverage,
            MovingAverageDays = 7
        };
    }

    /// <inheritdoc />
    public async Task<List<DailySalesTrendDto>> GetDailySalesTrendAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startDate && r.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var dailyData = await query
            .GroupBy(r => r.SettledAt!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Sales = g.Sum(r => r.TotalAmount),
                TransactionCount = g.Count(),
                AverageTicket = g.Average(r => r.TotalAmount)
            })
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return dailyData.Select(d => new DailySalesTrendDto
        {
            Date = d.Date,
            DateLabel = d.Date.ToString("MMM dd"),
            DayOfWeek = d.Date.DayOfWeek,
            Sales = d.Sales,
            TransactionCount = d.TransactionCount,
            AverageTicket = d.AverageTicket
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<MovingAveragePointDto>> GetMovingAverageAsync(
        DateTime startDate,
        DateTime endDate,
        int days = 7,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        // Get data including days before start for proper moving average calculation
        var extendedStart = startDate.AddDays(-days);

        var dailySales = await GetDailySalesTrendAsync(extendedStart, endDate, storeId, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<MovingAveragePointDto>();
        var salesDict = dailySales.ToDictionary(d => d.Date, d => d.Sales);

        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            var maValues = new List<decimal>();
            for (var i = 0; i < days; i++)
            {
                var checkDate = date.AddDays(-i);
                if (salesDict.TryGetValue(checkDate, out var sales))
                {
                    maValues.Add(sales);
                }
            }

            if (maValues.Count > 0)
            {
                result.Add(new MovingAveragePointDto
                {
                    Date = date,
                    Value = Math.Round(maValues.Average(), 2)
                });
            }
        }

        return result;
    }

    #endregion

    #region Category Comparison

    /// <inheritdoc />
    public async Task<List<CategoryComparisonDto>> GetCategoryComparisonAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodDates(request);

        // Current period category sales
        var currentCategorySales = await GetCategorySalesAsync(currentStart, currentEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        // Previous period category sales
        var previousCategorySales = await GetCategorySalesAsync(previousStart, previousEnd, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        var currentTotal = currentCategorySales.Sum(c => c.Sales);
        var previousTotal = previousCategorySales.Sum(c => c.Sales);

        // Merge and compare
        var allCategoryIds = currentCategorySales.Select(c => c.CategoryId)
            .Union(previousCategorySales.Select(c => c.CategoryId))
            .Distinct();

        var comparisons = new List<CategoryComparisonDto>();

        foreach (var categoryId in allCategoryIds)
        {
            var current = currentCategorySales.FirstOrDefault(c => c.CategoryId == categoryId);
            var previous = previousCategorySales.FirstOrDefault(c => c.CategoryId == categoryId);

            var currentSales = current?.Sales ?? 0;
            var previousSales = previous?.Sales ?? 0;
            var currentQty = current?.QuantitySold ?? 0;
            var previousQty = previous?.QuantitySold ?? 0;

            var currentContrib = currentTotal > 0 ? (currentSales / currentTotal) * 100 : 0;
            var previousContrib = previousTotal > 0 ? (previousSales / previousTotal) * 100 : 0;

            comparisons.Add(new CategoryComparisonDto
            {
                CategoryId = categoryId,
                CategoryName = current?.CategoryName ?? previous?.CategoryName ?? "Unknown",
                CurrentPeriodSales = currentSales,
                PreviousPeriodSales = previousSales,
                SalesGrowth = CreateGrowthMetrics(currentSales, previousSales),
                CurrentContributionPercent = Math.Round(currentContrib, 2),
                PreviousContributionPercent = Math.Round(previousContrib, 2),
                ContributionChange = Math.Round(currentContrib - previousContrib, 2),
                CurrentQuantitySold = currentQty,
                PreviousQuantitySold = previousQty,
                QuantityGrowth = CreateGrowthMetrics(currentQty, previousQty)
            });
        }

        // Rank by growth
        var ranked = comparisons
            .OrderByDescending(c => c.SalesGrowth.PercentageChange)
            .Select((c, i) => { c.GrowthRank = i + 1; return c; })
            .ToList();

        return ranked;
    }

    /// <inheritdoc />
    public async Task<List<CategoryComparisonDto>> GetFastestGrowingCategoriesAsync(
        PeriodComparisonRequest request,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var comparisons = await GetCategoryComparisonAsync(request, cancellationToken).ConfigureAwait(false);
        return comparisons
            .Where(c => c.SalesGrowth.IsPositive && c.PreviousPeriodSales > 0)
            .OrderByDescending(c => c.SalesGrowth.PercentageChange)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<CategoryComparisonDto>> GetDecliningCategoriesAsync(
        PeriodComparisonRequest request,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var comparisons = await GetCategoryComparisonAsync(request, cancellationToken).ConfigureAwait(false);
        return comparisons
            .Where(c => !c.SalesGrowth.IsPositive && c.PreviousPeriodSales > 0)
            .OrderBy(c => c.SalesGrowth.PercentageChange)
            .Take(count)
            .ToList();
    }

    #endregion

    #region Product Comparison

    /// <inheritdoc />
    public async Task<List<ProductComparisonDto>> GetProductComparisonAsync(
        PeriodComparisonRequest request,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodDates(request);

        // Current period product sales
        var currentProductSales = await GetProductSalesAsync(currentStart, currentEnd, request.StoreId, request.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        // Previous period product sales
        var previousProductSales = await GetProductSalesAsync(previousStart, previousEnd, request.StoreId, request.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        // Merge and compare
        var allProductIds = currentProductSales.Select(p => p.ProductId)
            .Union(previousProductSales.Select(p => p.ProductId))
            .Distinct();

        var comparisons = new List<ProductComparisonDto>();

        foreach (var productId in allProductIds)
        {
            var current = currentProductSales.FirstOrDefault(p => p.ProductId == productId);
            var previous = previousProductSales.FirstOrDefault(p => p.ProductId == productId);

            var currentSales = current?.Sales ?? 0;
            var previousSales = previous?.Sales ?? 0;
            var currentQty = current?.QuantitySold ?? 0;
            var previousQty = previous?.QuantitySold ?? 0;

            comparisons.Add(new ProductComparisonDto
            {
                ProductId = productId,
                ProductName = current?.ProductName ?? previous?.ProductName ?? "Unknown",
                ProductCode = current?.ProductCode ?? previous?.ProductCode ?? "",
                CategoryName = current?.CategoryName ?? previous?.CategoryName ?? "",
                CurrentPeriodSales = currentSales,
                PreviousPeriodSales = previousSales,
                SalesGrowth = CreateGrowthMetrics(currentSales, previousSales),
                CurrentQuantitySold = currentQty,
                PreviousQuantitySold = previousQty,
                QuantityGrowth = CreateGrowthMetrics(currentQty, previousQty),
                IsNewProduct = current != null && previous == null,
                IsDiscontinued = current == null && previous != null
            });
        }

        // Rank by absolute sales growth for relevance
        var ranked = comparisons
            .OrderByDescending(p => Math.Abs(p.SalesGrowth.AbsoluteChange))
            .Take(count)
            .Select((p, i) => { p.GrowthRank = i + 1; return p; })
            .ToList();

        return ranked;
    }

    /// <inheritdoc />
    public async Task<TopMoversDto> GetTopMoversAsync(
        PeriodComparisonRequest request,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var allProducts = await GetProductComparisonAsync(request, 1000, cancellationToken).ConfigureAwait(false);

        return new TopMoversDto
        {
            TopGainers = allProducts
                .Where(p => p.SalesGrowth.IsPositive && !p.IsNewProduct && p.PreviousPeriodSales > 0)
                .OrderByDescending(p => p.SalesGrowth.PercentageChange)
                .Take(count)
                .ToList(),

            TopLosers = allProducts
                .Where(p => !p.SalesGrowth.IsPositive && !p.IsDiscontinued && p.PreviousPeriodSales > 0)
                .OrderBy(p => p.SalesGrowth.PercentageChange)
                .Take(count)
                .ToList(),

            NewProducts = allProducts
                .Where(p => p.IsNewProduct)
                .OrderByDescending(p => p.CurrentPeriodSales)
                .Take(count)
                .ToList(),

            DiscontinuedProducts = allProducts
                .Where(p => p.IsDiscontinued)
                .OrderByDescending(p => p.PreviousPeriodSales)
                .Take(count)
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<List<ProductComparisonDto>> GetNewProductsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var allProducts = await GetProductComparisonAsync(request, 1000, cancellationToken).ConfigureAwait(false);
        return allProducts.Where(p => p.IsNewProduct).OrderByDescending(p => p.CurrentPeriodSales).ToList();
    }

    #endregion

    #region Pattern Analysis

    /// <inheritdoc />
    public async Task<List<DayOfWeekPatternDto>> GetDayOfWeekPatternsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startDate && r.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var dailyData = await query
            .GroupBy(r => r.SettledAt!.Value.DayOfWeek)
            .Select(g => new
            {
                DayOfWeek = g.Key,
                TotalSales = g.Sum(r => r.TotalAmount),
                TransactionCount = g.Count(),
                DayCount = g.Select(r => r.SettledAt!.Value.Date).Distinct().Count()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalWeeklySales = dailyData.Sum(d => d.TotalSales / Math.Max(1, d.DayCount));
        var maxAvgSales = dailyData.Any() ? dailyData.Max(d => d.TotalSales / Math.Max(1, d.DayCount)) : 0;

        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        var shortNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        return Enum.GetValues<DayOfWeek>().Select(dow =>
        {
            var data = dailyData.FirstOrDefault(d => d.DayOfWeek == dow);
            var avgSales = data != null ? data.TotalSales / Math.Max(1, data.DayCount) : 0;
            var avgTxns = data != null ? (decimal)data.TransactionCount / Math.Max(1, data.DayCount) : 0;
            var percentOfWeek = totalWeeklySales > 0 ? (avgSales / totalWeeklySales) * 100 : 0;
            var intensity = maxAvgSales > 0 ? avgSales / maxAvgSales : 0;

            return new DayOfWeekPatternDto
            {
                DayOfWeek = dow,
                DayName = dayNames[(int)dow],
                ShortDayName = shortNames[(int)dow],
                AverageSales = Math.Round(avgSales, 2),
                AverageTransactions = Math.Round(avgTxns, 2),
                PercentOfWeek = Math.Round(percentOfWeek, 1),
                HeatIntensity = Math.Round(intensity, 2),
                ColorCode = GetHeatColor(intensity)
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<HourlyPatternDto>> GetHourlyPatternsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startDate && r.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var hourlyData = await query
            .GroupBy(r => new { r.SettledAt!.Value.DayOfWeek, r.SettledAt!.Value.Hour })
            .Select(g => new
            {
                DayOfWeek = g.Key.DayOfWeek,
                Hour = g.Key.Hour,
                TotalSales = g.Sum(r => r.TotalAmount),
                DayCount = g.Select(r => r.SettledAt!.Value.Date).Distinct().Count()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var maxAvgSales = hourlyData.Any() ? hourlyData.Max(h => h.TotalSales / Math.Max(1, h.DayCount)) : 0;

        var result = new List<HourlyPatternDto>();

        for (int hour = 0; hour < 24; hour++)
        {
            var hourLabel = hour == 0 ? "12 AM" :
                           hour < 12 ? $"{hour} AM" :
                           hour == 12 ? "12 PM" : $"{hour - 12} PM";

            var dayPatterns = new List<DayHourValueDto>();

            foreach (var dow in Enum.GetValues<DayOfWeek>())
            {
                var data = hourlyData.FirstOrDefault(h => h.DayOfWeek == dow && h.Hour == hour);
                var avgSales = data != null ? data.TotalSales / Math.Max(1, data.DayCount) : 0;
                var intensity = maxAvgSales > 0 ? avgSales / maxAvgSales : 0;

                dayPatterns.Add(new DayHourValueDto
                {
                    DayOfWeek = dow,
                    Hour = hour,
                    AverageSales = Math.Round(avgSales, 2),
                    HeatIntensity = Math.Round(intensity, 2),
                    ColorCode = GetHeatColor(intensity)
                });
            }

            result.Add(new HourlyPatternDto
            {
                Hour = hour,
                HourLabel = hourLabel,
                DayPatterns = dayPatterns
            });
        }

        return result;
    }

    #endregion

    #region Sparklines

    /// <inheritdoc />
    public async Task<List<SparklineDataDto>> GetSparklineDataAsync(
        int days = 14,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.Today.AddDays(1);
        var startDate = DateTime.Today.AddDays(-days);

        var dailyTrend = await GetDailySalesTrendAsync(startDate, endDate, storeId, cancellationToken)
            .ConfigureAwait(false);

        var salesPoints = dailyTrend.Select(d => d.Sales).ToList();
        var txnPoints = dailyTrend.Select(d => (decimal)d.TransactionCount).ToList();
        var avgTicketPoints = dailyTrend.Select(d => d.AverageTicket).ToList();

        return new List<SparklineDataDto>
        {
            CreateSparkline("Sales", salesPoints, "KSh"),
            CreateSparkline("Transactions", txnPoints, ""),
            CreateSparkline("Avg Ticket", avgTicketPoints, "KSh")
        };
    }

    #endregion

    #region Complete Analytics

    /// <inheritdoc />
    public async Task<ComparativeAnalyticsDto> GetComparativeAnalyticsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating complete comparative analytics for period type {PeriodType}", request.PeriodType);

        var (currentStart, currentEnd, _, _) = GetPeriodDates(request);

        var periodComparison = await GetPeriodComparisonAsync(request, cancellationToken).ConfigureAwait(false);
        var salesTrend = await GetSalesTrendComparisonAsync(request, cancellationToken).ConfigureAwait(false);
        var categoryComparisons = await GetCategoryComparisonAsync(request, cancellationToken).ConfigureAwait(false);
        var topMovers = await GetTopMoversAsync(request, 10, cancellationToken).ConfigureAwait(false);
        var dayOfWeekPatterns = await GetDayOfWeekPatternsAsync(currentStart, currentEnd, request.StoreId, cancellationToken).ConfigureAwait(false);
        var sparklines = await GetSparklineDataAsync(14, request.StoreId, cancellationToken).ConfigureAwait(false);

        return new ComparativeAnalyticsDto
        {
            PeriodComparison = periodComparison,
            SalesTrend = salesTrend,
            CategoryComparisons = categoryComparisons,
            TopMovers = topMovers,
            DayOfWeekPatterns = dayOfWeekPatterns,
            Sparklines = sparklines,
            GeneratedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Export

    /// <inheritdoc />
    public async Task<ComparativeAnalyticsExportDto> ExportAnalyticsAsync(
        PeriodComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var data = await GetComparativeAnalyticsAsync(request, cancellationToken).ConfigureAwait(false);

        string? storeName = null;
        if (request.StoreId.HasValue)
        {
            var store = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "StoreName", cancellationToken)
                .ConfigureAwait(false);
            storeName = store?.Value;
        }

        return new ComparativeAnalyticsExportDto
        {
            ReportTitle = $"Comparative Analytics: {data.PeriodComparison.CurrentPeriodLabel} vs {data.PeriodComparison.PreviousPeriodLabel}",
            GeneratedAt = DateTime.UtcNow,
            StoreName = storeName,
            Data = data
        };
    }

    #endregion

    #region Utility Methods

    /// <inheritdoc />
    public (DateTime CurrentStart, DateTime CurrentEnd, DateTime PreviousStart, DateTime PreviousEnd)
        ResolvePeriodDates(ComparisonPeriodType periodType)
    {
        var today = DateTime.Today;

        return periodType switch
        {
            ComparisonPeriodType.DayOverDay => (
                today,
                today.AddDays(1),
                today.AddDays(-1),
                today
            ),

            ComparisonPeriodType.WeekOverWeek => (
                GetStartOfWeek(today),
                GetStartOfWeek(today).AddDays(7),
                GetStartOfWeek(today.AddDays(-7)),
                GetStartOfWeek(today.AddDays(-7)).AddDays(7)
            ),

            ComparisonPeriodType.MonthOverMonth => (
                new DateTime(today.Year, today.Month, 1),
                new DateTime(today.Year, today.Month, 1).AddMonths(1),
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1)
            ),

            ComparisonPeriodType.YearOverYear => (
                new DateTime(today.Year, 1, 1),
                new DateTime(today.Year + 1, 1, 1),
                new DateTime(today.Year - 1, 1, 1),
                new DateTime(today.Year, 1, 1)
            ),

            ComparisonPeriodType.QuarterOverQuarter => GetQuarterDates(today),

            _ => (today, today.AddDays(1), today.AddDays(-1), today)
        };
    }

    /// <inheritdoc />
    public string GetPeriodLabel(ComparisonPeriodType periodType, bool isCurrent)
    {
        return periodType switch
        {
            ComparisonPeriodType.DayOverDay => isCurrent ? "Today" : "Yesterday",
            ComparisonPeriodType.WeekOverWeek => isCurrent ? "This Week" : "Last Week",
            ComparisonPeriodType.MonthOverMonth => isCurrent ? "This Month" : "Last Month",
            ComparisonPeriodType.YearOverYear => isCurrent ? "This Year" : "Last Year",
            ComparisonPeriodType.QuarterOverQuarter => isCurrent ? "This Quarter" : "Last Quarter",
            ComparisonPeriodType.Custom => isCurrent ? "Current Period" : "Previous Period",
            _ => isCurrent ? "Current" : "Previous"
        };
    }

    #endregion

    #region Private Helper Methods

    private (DateTime CurrentStart, DateTime CurrentEnd, DateTime PreviousStart, DateTime PreviousEnd) GetPeriodDates(PeriodComparisonRequest request)
    {
        if (request.PeriodType == ComparisonPeriodType.Custom &&
            request.CurrentPeriodStart.HasValue &&
            request.CurrentPeriodEnd.HasValue &&
            request.PreviousPeriodStart.HasValue &&
            request.PreviousPeriodEnd.HasValue)
        {
            return (
                request.CurrentPeriodStart.Value,
                request.CurrentPeriodEnd.Value,
                request.PreviousPeriodStart.Value,
                request.PreviousPeriodEnd.Value
            );
        }

        return ResolvePeriodDates(request.PeriodType);
    }

    private async Task<PeriodMetrics> GetPeriodMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId,
        CancellationToken cancellationToken)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startDate && r.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var receipts = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PeriodMetrics
        {
            TotalSales = receipts.Sum(r => r.TotalAmount),
            TransactionCount = receipts.Count,
            AverageTicket = receipts.Count > 0 ? receipts.Average(r => r.TotalAmount) : 0,
            ItemsSold = receipts.Sum(r => r.Items?.Sum(i => i.Quantity) ?? 0),
            UniqueCustomers = receipts.Where(r => r.CustomerId.HasValue).Select(r => r.CustomerId).Distinct().Count(),
            GrossProfit = receipts.Sum(r => r.Subtotal),
            Discounts = receipts.Sum(r => r.DiscountAmount),
            TaxCollected = receipts.Sum(r => r.TaxAmount)
        };
    }

    private async Task<List<CategorySalesData>> GetCategorySalesAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId,
        CancellationToken cancellationToken)
    {
        var query = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
                .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= startDate && ri.Receipt.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId.Value);
        }

        return await query
            .GroupBy(ri => new { ri.Product.CategoryId, ri.Product.Category.Name })
            .Select(g => new CategorySalesData
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Sales = g.Sum(ri => ri.TotalPrice),
                QuantitySold = g.Sum(ri => ri.Quantity)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<ProductSalesData>> GetProductSalesAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var query = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
                .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= startDate && ri.Receipt.SettledAt < endDate);

        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(ri => ri.Product.CategoryId == categoryId.Value);
        }

        return await query
            .GroupBy(ri => new { ri.ProductId, ri.Product.Name, ri.Product.ProductCode, CategoryName = ri.Product.Category.Name })
            .Select(g => new ProductSalesData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                ProductCode = g.Key.ProductCode,
                CategoryName = g.Key.CategoryName,
                Sales = g.Sum(ri => ri.TotalPrice),
                QuantitySold = g.Sum(ri => ri.Quantity)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static GrowthMetricsDto CreateGrowthMetrics(decimal current, decimal previous)
    {
        return new GrowthMetricsDto
        {
            CurrentPeriodValue = current,
            PreviousPeriodValue = previous
        };
    }

    private static SparklineDataDto CreateSparkline(string name, List<decimal> dataPoints, string prefix)
    {
        var currentValue = dataPoints.LastOrDefault();
        var previousValue = dataPoints.Count > 1 ? dataPoints[^2] : 0;
        var percentChange = previousValue != 0
            ? ((currentValue - previousValue) / previousValue) * 100
            : currentValue > 0 ? 100 : 0;

        var direction = percentChange > 1 ? TrendDirection.Up :
                       percentChange < -1 ? TrendDirection.Down : TrendDirection.Flat;

        return new SparklineDataDto
        {
            MetricName = name,
            CurrentValue = currentValue,
            FormattedValue = prefix == "KSh" ? $"KSh {currentValue:N0}" : $"{currentValue:N0}",
            Direction = direction,
            PercentageChange = Math.Round(percentChange, 1),
            DataPoints = dataPoints
        };
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static (DateTime CurrentStart, DateTime CurrentEnd, DateTime PreviousStart, DateTime PreviousEnd) GetQuarterDates(DateTime today)
    {
        var currentQuarter = (today.Month - 1) / 3 + 1;
        var currentQuarterStart = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
        var currentQuarterEnd = currentQuarterStart.AddMonths(3);

        var previousQuarterStart = currentQuarterStart.AddMonths(-3);
        var previousQuarterEnd = currentQuarterStart;

        return (currentQuarterStart, currentQuarterEnd, previousQuarterStart, previousQuarterEnd);
    }

    private static string GetHeatColor(decimal intensity)
    {
        // Generate color from blue (cold) through yellow to red (hot)
        if (intensity <= 0) return "#E3F2FD"; // Light blue for zero
        if (intensity < 0.25m) return "#90CAF9"; // Blue
        if (intensity < 0.5m) return "#FFF176"; // Yellow
        if (intensity < 0.75m) return "#FFB74D"; // Orange
        return "#EF5350"; // Red
    }

    #endregion

    #region Private Helper Classes

    private class PeriodMetrics
    {
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTicket { get; set; }
        public decimal ItemsSold { get; set; }
        public int UniqueCustomers { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal Discounts { get; set; }
        public decimal TaxCollected { get; set; }
    }

    private class CategorySalesData
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal QuantitySold { get; set; }
    }

    private class ProductSalesData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal QuantitySold { get; set; }
    }

    #endregion
}
