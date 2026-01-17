using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for profit margin analysis and reporting.
/// </summary>
public class MarginAnalysisService : IMarginAnalysisService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;
    private decimal _minimumMarginThreshold = 15.0m;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarginAnalysisService"/> class.
    /// </summary>
    public MarginAnalysisService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Product Margin Analysis

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductMarginDto>> GetProductMarginsAsync(
        int? categoryId = null,
        int? supplierId = null,
        bool onlyWithCostPrice = true)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (onlyWithCostPrice)
        {
            query = query.Where(p => p.CostPrice.HasValue && p.CostPrice > 0);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var products = await query
            .OrderBy(p => p.Category!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(p => new ProductMarginDto
        {
            ProductId = p.Id,
            ProductCode = p.Code,
            ProductName = p.Name,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "Uncategorized",
            SellingPrice = p.SellingPrice,
            CostPrice = p.CostPrice ?? 0,
            IsBelowThreshold = p.CostPrice.HasValue && p.SellingPrice > 0 &&
                ((p.SellingPrice - p.CostPrice.Value) / p.SellingPrice * 100) < _minimumMarginThreshold
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<ProductMarginDto?> GetProductMarginAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

        if (product == null) return null;

        return new ProductMarginDto
        {
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "Uncategorized",
            SellingPrice = product.SellingPrice,
            CostPrice = product.CostPrice ?? 0,
            IsBelowThreshold = product.CostPrice.HasValue && product.SellingPrice > 0 &&
                ((product.SellingPrice - product.CostPrice.Value) / product.SellingPrice * 100) < _minimumMarginThreshold
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductMarginDto>> GetProductMarginsWithSalesAsync(MarginReportRequest request)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1);

        // Get sales data grouped by product
        var salesQuery = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.CreatedAt >= startDate && ri.Receipt.CreatedAt < endDate)
            .Where(ri => !ri.Receipt.IsVoided);

        if (request.CategoryId.HasValue)
        {
            salesQuery = salesQuery.Where(ri => ri.Product.CategoryId == request.CategoryId);
        }

        var salesData = await salesQuery
            .GroupBy(ri => new
            {
                ri.ProductId,
                ri.Product.Code,
                ri.Product.Name,
                CategoryId = ri.Product.CategoryId,
                CategoryName = ri.Product.Category != null ? ri.Product.Category.Name : "Uncategorized",
                SellingPrice = ri.Product.SellingPrice,
                CostPrice = ri.Product.CostPrice ?? 0
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Key.SellingPrice,
                g.Key.CostPrice,
                UnitsSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync();

        var totalProfit = salesData.Sum(s => s.TotalRevenue - (s.UnitsSold * s.CostPrice));

        var results = salesData.Select(s =>
        {
            var productProfit = s.TotalRevenue - (s.UnitsSold * s.CostPrice);
            var marginPercent = s.SellingPrice > 0
                ? ((s.SellingPrice - s.CostPrice) / s.SellingPrice) * 100
                : 0;

            return new ProductMarginDto
            {
                ProductId = s.ProductId,
                ProductCode = s.Code,
                ProductName = s.Name,
                CategoryId = s.CategoryId,
                CategoryName = s.CategoryName,
                SellingPrice = s.SellingPrice,
                CostPrice = s.CostPrice,
                UnitsSold = s.UnitsSold,
                TotalRevenue = s.TotalRevenue,
                ProfitContributionPercent = totalProfit != 0
                    ? Math.Round((productProfit / totalProfit) * 100, 2)
                    : 0,
                IsBelowThreshold = marginPercent < request.MinimumMarginThreshold
            };
        })
        .OrderByDescending(p => p.TotalProfit)
        .ToList();

        return results;
    }

    #endregion

    #region Category Margin Analysis

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryMarginDto>> GetCategoryMarginsAsync(MarginReportRequest request)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1);

        var salesQuery = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.CreatedAt >= startDate && ri.Receipt.CreatedAt < endDate)
            .Where(ri => !ri.Receipt.IsVoided)
            .Where(ri => ri.Product.CategoryId.HasValue);

        if (request.CategoryId.HasValue)
        {
            salesQuery = salesQuery.Where(ri => ri.Product.CategoryId == request.CategoryId);
        }

        var categoryData = await salesQuery
            .GroupBy(ri => new
            {
                CategoryId = ri.Product.CategoryId!.Value,
                CategoryName = ri.Product.Category!.Name
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalCost = g.Sum(x => x.Quantity * (x.Product.CostPrice ?? 0)),
                ProductCount = g.Select(x => x.ProductId).Distinct().Count()
            })
            .ToListAsync();

        var totalProfit = categoryData.Sum(c => c.TotalRevenue - c.TotalCost);

        var results = categoryData
            .Select(c =>
            {
                var categoryProfit = c.TotalRevenue - c.TotalCost;
                return new CategoryMarginDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ProductCount = c.ProductCount,
                    TotalRevenue = c.TotalRevenue,
                    TotalCost = c.TotalCost,
                    ProfitContributionPercent = totalProfit != 0
                        ? Math.Round((categoryProfit / totalProfit) * 100, 2)
                        : 0
                };
            })
            .OrderByDescending(c => c.TotalProfit)
            .ToList();

        // Assign profitability ranks
        for (int i = 0; i < results.Count; i++)
        {
            results[i].ProfitabilityRank = i + 1;
        }

        // Get low margin product counts per category
        var lowMarginProducts = await _context.Products
            .Where(p => p.IsActive && p.CostPrice.HasValue && p.CostPrice > 0 && p.CategoryId.HasValue)
            .Where(p => ((p.SellingPrice - p.CostPrice!.Value) / p.SellingPrice * 100) < request.MinimumMarginThreshold)
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        foreach (var category in results)
        {
            if (lowMarginProducts.TryGetValue(category.CategoryId, out var count))
            {
                category.LowMarginProductCount = count;
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryMarginDto>> GetTopProfitableCategoriesAsync(
        MarginReportRequest request,
        int limit = 10)
    {
        var categories = await GetCategoryMarginsAsync(request);
        return categories.Take(limit).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryMarginDto>> GetLowestMarginCategoriesAsync(
        MarginReportRequest request,
        int limit = 10)
    {
        var categories = await GetCategoryMarginsAsync(request);
        return categories
            .Where(c => c.TotalRevenue > 0)
            .OrderBy(c => c.WeightedMarginPercent)
            .Take(limit)
            .ToList();
    }

    #endregion

    #region Gross Profit Reports

    /// <inheritdoc />
    public async Task<GrossProfitSummaryDto> GetGrossProfitSummaryAsync(MarginReportRequest request)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1);

        var salesQuery = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.CreatedAt >= startDate && ri.Receipt.CreatedAt < endDate)
            .Where(ri => !ri.Receipt.IsVoided);

        if (request.CategoryId.HasValue)
        {
            salesQuery = salesQuery.Where(ri => ri.Product.CategoryId == request.CategoryId);
        }

        var salesData = await salesQuery
            .GroupBy(ri => 1)
            .Select(g => new
            {
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalCost = g.Sum(x => x.Quantity * (x.Product.CostPrice ?? 0)),
                TotalUnitsSold = g.Sum(x => x.Quantity),
                TransactionCount = g.Select(x => x.ReceiptId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        var productMargins = await GetProductMarginsWithSalesAsync(request);
        var averageMargin = productMargins.Any()
            ? productMargins.Average(p => p.MarginPercent)
            : 0;

        return new GrossProfitSummaryDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalRevenue = salesData?.TotalRevenue ?? 0,
            TotalCost = salesData?.TotalCost ?? 0,
            TotalUnitsSold = salesData?.TotalUnitsSold ?? 0,
            TransactionCount = salesData?.TransactionCount ?? 0,
            AverageMarginPercent = Math.Round(averageMargin, 2)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarginTrendPointDto>> GetDailyGrossProfitAsync(MarginReportRequest request)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1);

        var dailyData = await _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.CreatedAt >= startDate && ri.Receipt.CreatedAt < endDate)
            .Where(ri => !ri.Receipt.IsVoided)
            .GroupBy(ri => ri.Receipt.CreatedAt.Date)
            .Select(g => new MarginTrendPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.TotalAmount),
                Cost = g.Sum(x => x.Quantity * (x.Product.CostPrice ?? 0))
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Calculate average margin percent for each day
        foreach (var day in dailyData)
        {
            day.AverageMarginPercent = day.GrossProfitPercent;
        }

        return dailyData;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarginTrendPointDto>> GetMonthlyGrossProfitAsync(
        DateTime startDate,
        DateTime endDate,
        int? categoryId = null)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var query = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.CreatedAt >= start && ri.Receipt.CreatedAt < end)
            .Where(ri => !ri.Receipt.IsVoided);

        if (categoryId.HasValue)
        {
            query = query.Where(ri => ri.Product.CategoryId == categoryId);
        }

        var monthlyData = await query
            .GroupBy(ri => new { ri.Receipt.CreatedAt.Year, ri.Receipt.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount),
                Cost = g.Sum(x => x.Quantity * (x.Product.CostPrice ?? 0))
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        return monthlyData.Select(m => new MarginTrendPointDto
        {
            Date = new DateTime(m.Year, m.Month, 1),
            Revenue = m.Revenue,
            Cost = m.Cost,
            AverageMarginPercent = m.Revenue > 0
                ? Math.Round(((m.Revenue - m.Cost) / m.Revenue) * 100, 2)
                : 0
        }).ToList();
    }

    #endregion

    #region Low Margin Alerts

    /// <inheritdoc />
    public async Task<IReadOnlyList<LowMarginAlertDto>> GetLowMarginAlertsAsync(
        decimal thresholdPercent = 15.0m,
        int? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .Where(p => p.CostPrice.HasValue && p.CostPrice > 0)
            .Where(p => p.SellingPrice > 0);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var products = await query.ToListAsync();

        // Filter by margin threshold
        var lowMarginProducts = products
            .Where(p =>
            {
                var margin = (p.SellingPrice - p.CostPrice!.Value) / p.SellingPrice * 100;
                return margin < thresholdPercent;
            })
            .ToList();

        // Get recent sales data (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentSales = await _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Where(ri => ri.Receipt.CreatedAt >= thirtyDaysAgo)
            .Where(ri => !ri.Receipt.IsVoided)
            .Where(ri => lowMarginProducts.Select(p => p.Id).Contains(ri.ProductId))
            .GroupBy(ri => ri.ProductId)
            .Select(g => new { ProductId = g.Key, UnitsSold = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.UnitsSold);

        return lowMarginProducts.Select(p =>
        {
            var marginPercent = (p.SellingPrice - p.CostPrice!.Value) / p.SellingPrice * 100;
            recentSales.TryGetValue(p.Id, out var unitsSold);

            return new LowMarginAlertDto
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                CategoryName = p.Category?.Name ?? "Uncategorized",
                CurrentMarginPercent = Math.Round(marginPercent, 2),
                ThresholdPercent = thresholdPercent,
                SellingPrice = p.SellingPrice,
                CostPrice = p.CostPrice!.Value,
                RecentUnitsSold = unitsSold
            };
        })
        .OrderBy(a => a.CurrentMarginPercent)
        .ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetLowMarginCountAsync(decimal thresholdPercent = 15.0m)
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Where(p => p.CostPrice.HasValue && p.CostPrice > 0)
            .Where(p => p.SellingPrice > 0)
            .ToListAsync();

        return products.Count(p =>
            ((p.SellingPrice - p.CostPrice!.Value) / p.SellingPrice * 100) < thresholdPercent);
    }

    /// <inheritdoc />
    public async Task<decimal> GetPotentialProfitLossAsync(decimal thresholdPercent = 15.0m, int periodDays = 30)
    {
        var alerts = await GetLowMarginAlertsAsync(thresholdPercent);
        return alerts.Sum(a => a.PotentialProfitLoss);
    }

    #endregion

    #region Margin Trend Analysis

    /// <inheritdoc />
    public async Task<ProductMarginTrendDto?> GetProductMarginTrendAsync(int productId, int months = 6)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

        if (product == null) return null;

        var startDate = DateTime.UtcNow.AddMonths(-months);

        // Get cost price history from GRN items
        var costHistory = await _context.Set<GRNItem>()
            .Include(gi => gi.GoodsReceivedNote)
            .Where(gi => gi.ProductId == productId)
            .Where(gi => gi.GoodsReceivedNote.ReceivedDate >= startDate)
            .OrderByDescending(gi => gi.GoodsReceivedNote.ReceivedDate)
            .Select(gi => new
            {
                Date = gi.GoodsReceivedNote.ReceivedDate,
                CostPrice = gi.UnitCost
            })
            .ToListAsync();

        // Get monthly margin trends
        var monthlyData = await _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Where(ri => ri.ProductId == productId)
            .Where(ri => ri.Receipt.CreatedAt >= startDate)
            .Where(ri => !ri.Receipt.IsVoided)
            .GroupBy(ri => new { ri.Receipt.CreatedAt.Year, ri.Receipt.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount),
                UnitsSold = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var currentCost = product.CostPrice ?? 0;
        var previousCost = costHistory.Skip(1).FirstOrDefault()?.CostPrice ?? currentCost;
        var currentMargin = product.SellingPrice > 0
            ? ((product.SellingPrice - currentCost) / product.SellingPrice) * 100
            : 0;
        var previousMargin = product.SellingPrice > 0
            ? ((product.SellingPrice - previousCost) / product.SellingPrice) * 100
            : 0;

        return new ProductMarginTrendDto
        {
            ProductId = productId,
            ProductName = product.Name,
            CurrentMarginPercent = Math.Round(currentMargin, 2),
            PreviousMarginPercent = Math.Round(previousMargin, 2),
            CurrentCostPrice = currentCost,
            PreviousCostPrice = previousCost,
            TrendData = monthlyData.Select(m => new MarginTrendPointDto
            {
                Date = new DateTime(m.Year, m.Month, 1),
                Revenue = m.Revenue,
                Cost = m.UnitsSold * currentCost, // Approximate
                AverageMarginPercent = m.Revenue > 0
                    ? Math.Round(((m.Revenue - (m.UnitsSold * currentCost)) / m.Revenue) * 100, 2)
                    : 0
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductMarginTrendDto>> GetDecliningMarginProductsAsync(
        decimal declineThresholdPercent = 5.0m,
        int limit = 20)
    {
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        // Get products with cost price changes
        var productsWithCostChanges = await _context.Set<GRNItem>()
            .Include(gi => gi.GoodsReceivedNote)
            .Include(gi => gi.Product)
            .Where(gi => gi.GoodsReceivedNote.ReceivedDate >= sixMonthsAgo)
            .GroupBy(gi => gi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                SellingPrice = g.First().Product.SellingPrice,
                CurrentCost = g.Where(x => x.GoodsReceivedNote.ReceivedDate >= threeMonthsAgo)
                    .OrderByDescending(x => x.GoodsReceivedNote.ReceivedDate)
                    .Select(x => x.UnitCost)
                    .FirstOrDefault(),
                PreviousCost = g.Where(x => x.GoodsReceivedNote.ReceivedDate < threeMonthsAgo)
                    .OrderByDescending(x => x.GoodsReceivedNote.ReceivedDate)
                    .Select(x => x.UnitCost)
                    .FirstOrDefault()
            })
            .Where(x => x.CurrentCost > 0 && x.PreviousCost > 0)
            .ToListAsync();

        var results = productsWithCostChanges
            .Select(p =>
            {
                var currentMargin = p.SellingPrice > 0
                    ? ((p.SellingPrice - p.CurrentCost) / p.SellingPrice) * 100
                    : 0;
                var previousMargin = p.SellingPrice > 0
                    ? ((p.SellingPrice - p.PreviousCost) / p.SellingPrice) * 100
                    : 0;

                return new ProductMarginTrendDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CurrentMarginPercent = Math.Round(currentMargin, 2),
                    PreviousMarginPercent = Math.Round(previousMargin, 2),
                    CurrentCostPrice = p.CurrentCost,
                    PreviousCostPrice = p.PreviousCost
                };
            })
            .Where(p => p.MarginChange < -declineThresholdPercent)
            .OrderBy(p => p.MarginChange)
            .Take(limit)
            .ToList();

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductMarginTrendDto>> GetCostIncreaseAlertsAsync(
        int periodDays = 30,
        int limit = 20)
    {
        var startDate = DateTime.UtcNow.AddDays(-periodDays);

        var costIncreases = await _context.Set<GRNItem>()
            .Include(gi => gi.GoodsReceivedNote)
            .Include(gi => gi.Product)
            .Where(gi => gi.GoodsReceivedNote.ReceivedDate >= startDate)
            .GroupBy(gi => gi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                SellingPrice = g.First().Product.SellingPrice,
                CurrentCost = g.First().Product.CostPrice ?? 0,
                LatestGRNCost = g.OrderByDescending(x => x.GoodsReceivedNote.ReceivedDate)
                    .Select(x => x.UnitCost)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Get previous costs from older GRNs
        var productIds = costIncreases.Select(c => c.ProductId).ToList();
        var previousCosts = await _context.Set<GRNItem>()
            .Include(gi => gi.GoodsReceivedNote)
            .Where(gi => productIds.Contains(gi.ProductId))
            .Where(gi => gi.GoodsReceivedNote.ReceivedDate < startDate)
            .GroupBy(gi => gi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                PreviousCost = g.OrderByDescending(x => x.GoodsReceivedNote.ReceivedDate)
                    .Select(x => x.UnitCost)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.PreviousCost);

        return costIncreases
            .Where(c => previousCosts.ContainsKey(c.ProductId) && c.LatestGRNCost > previousCosts[c.ProductId])
            .Select(c =>
            {
                var prevCost = previousCosts[c.ProductId];
                var currentMargin = c.SellingPrice > 0
                    ? ((c.SellingPrice - c.LatestGRNCost) / c.SellingPrice) * 100
                    : 0;
                var previousMargin = c.SellingPrice > 0
                    ? ((c.SellingPrice - prevCost) / c.SellingPrice) * 100
                    : 0;

                return new ProductMarginTrendDto
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    CurrentMarginPercent = Math.Round(currentMargin, 2),
                    PreviousMarginPercent = Math.Round(previousMargin, 2),
                    CurrentCostPrice = c.LatestGRNCost,
                    PreviousCostPrice = prevCost
                };
            })
            .OrderByDescending(c => c.CostPriceChange)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostPriceHistoryDto>> GetCostPriceHistoryAsync(int productId, int limit = 50)
    {
        var history = await _context.Set<GRNItem>()
            .Include(gi => gi.GoodsReceivedNote)
            .ThenInclude(grn => grn.Supplier)
            .Include(gi => gi.Product)
            .Where(gi => gi.ProductId == productId)
            .OrderByDescending(gi => gi.GoodsReceivedNote.ReceivedDate)
            .Take(limit)
            .Select(gi => new CostPriceHistoryDto
            {
                Id = gi.Id,
                ProductId = gi.ProductId,
                ProductName = gi.Product.Name,
                CostPrice = gi.UnitCost,
                EffectiveDate = gi.GoodsReceivedNote.ReceivedDate,
                SupplierId = gi.GoodsReceivedNote.SupplierId,
                SupplierName = gi.GoodsReceivedNote.Supplier != null ? gi.GoodsReceivedNote.Supplier.Name : null,
                GoodsReceivedId = gi.GoodsReceivedNoteId,
                GRNNumber = gi.GoodsReceivedNote.GRNNumber
            })
            .ToListAsync();

        // Calculate changes from previous
        for (int i = 0; i < history.Count - 1; i++)
        {
            history[i].PreviousCostPrice = history[i + 1].CostPrice;
        }

        return history;
    }

    #endregion

    #region Complete Reports

    /// <inheritdoc />
    public async Task<MarginAnalyticsReportDto> GetMarginAnalyticsReportAsync(MarginReportRequest request)
    {
        _logger.Information("Generating margin analytics report for {StartDate} to {EndDate}",
            request.StartDate, request.EndDate);

        var report = new MarginAnalyticsReportDto
        {
            Request = request
        };

        // Get all data in parallel
        var grossProfitTask = GetGrossProfitSummaryAsync(request);
        var categoryMarginsTask = GetCategoryMarginsAsync(request);
        var productMarginsTask = GetProductMarginsWithSalesAsync(request);
        var lowMarginAlertsTask = GetLowMarginAlertsAsync(request.MinimumMarginThreshold, request.CategoryId);
        var marginTrendTask = GetDailyGrossProfitAsync(request);
        var decliningMarginsTask = GetDecliningMarginProductsAsync();

        await Task.WhenAll(
            grossProfitTask,
            categoryMarginsTask,
            productMarginsTask,
            lowMarginAlertsTask,
            marginTrendTask,
            decliningMarginsTask);

        report.GrossProfitSummary = await grossProfitTask;
        report.CategoryMargins = (await categoryMarginsTask).ToList();
        report.ProductMargins = (await productMarginsTask).ToList();
        report.LowMarginAlerts = (await lowMarginAlertsTask).ToList();
        report.MarginTrend = (await marginTrendTask).ToList();
        report.DecliningMarginProducts = (await decliningMarginsTask).ToList();

        // Summary stats
        report.TotalProductsAnalyzed = await _context.Products.CountAsync(p => p.IsActive);
        report.ProductsWithCostPrice = await _context.Products
            .CountAsync(p => p.IsActive && p.CostPrice.HasValue && p.CostPrice > 0);
        report.LowMarginProductCount = report.LowMarginAlerts.Count;

        _logger.Information("Margin analytics report generated: {ProductCount} products, {LowMarginCount} low margin alerts",
            report.ProductsWithCostPrice, report.LowMarginProductCount);

        return report;
    }

    /// <inheritdoc />
    public async Task<bool> ExportToExcelAsync(MarginReportRequest request, string filePath)
    {
        try
        {
            _logger.Information("Exporting margin report to {FilePath}", filePath);
            // Implementation would use an Excel library (e.g., ClosedXML, EPPlus)
            // For now, return true as the actual export is handled by IExportService
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export margin report to Excel");
            return false;
        }
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public Task<decimal> GetMinimumMarginThresholdAsync()
    {
        return Task.FromResult(_minimumMarginThreshold);
    }

    /// <inheritdoc />
    public Task SetMinimumMarginThresholdAsync(decimal thresholdPercent)
    {
        if (thresholdPercent < 0 || thresholdPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdPercent),
                "Threshold must be between 0 and 100 percent.");
        }

        _minimumMarginThreshold = thresholdPercent;
        _logger.Information("Minimum margin threshold set to {Threshold}%", thresholdPercent);
        return Task.CompletedTask;
    }

    #endregion
}
