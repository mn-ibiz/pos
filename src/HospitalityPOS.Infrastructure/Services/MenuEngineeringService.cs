using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for menu engineering and product performance analysis.
/// </summary>
public class MenuEngineeringService : IMenuEngineeringService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<MenuEngineeringService> _logger;

    public MenuEngineeringService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<MenuEngineeringService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<MenuEngineeringReport> GenerateMenuEngineeringReportAsync(
        MenuEngineeringParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new MenuEngineeringReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get served orders in date range
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();

        // Get order items with product details
        var baseQuery = context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .AsQueryable();

        if (parameters.CategoryId.HasValue)
            baseQuery = baseQuery.Where(oi => oi.Product!.CategoryId == parameters.CategoryId);

        var orderItems = await baseQuery.ToListAsync(cancellationToken);

        // Filter and group by product
        var productGroups = orderItems
            .GroupBy(oi => oi.ProductId)
            .Where(g => g.Sum(oi => oi.Quantity) >= parameters.MinimumSalesThreshold)
            .ToList();

        // Calculate totals
        report.TotalItems = productGroups.Count;
        report.TotalItemsSold = (int)productGroups.Sum(g => g.Sum(oi => oi.Quantity));
        report.TotalRevenue = productGroups.Sum(g => g.Sum(oi => oi.TotalAmount));
        report.TotalCost = productGroups.Sum(g => g.Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity));

        // Calculate thresholds
        var avgPopularity = report.TotalItems > 0 ? (decimal)report.TotalItemsSold / report.TotalItems : 0;
        var avgContributionMargin = productGroups.Any()
            ? productGroups.Average(g =>
            {
                var product = g.First().Product;
                return (product?.SellingPrice ?? 0) - (product?.CostPrice ?? 0);
            })
            : 0;

        report.PopularityThreshold = parameters.PopularityThresholdMethod switch
        {
            ThresholdMethod.Custom => parameters.CustomPopularityThreshold ?? avgPopularity,
            ThresholdMethod.Median => CalculateMedian(productGroups.Select(g => g.Sum(oi => oi.Quantity))),
            _ => avgPopularity
        };

        report.ProfitabilityThreshold = parameters.ProfitabilityThresholdMethod switch
        {
            ThresholdMethod.Custom => parameters.CustomProfitabilityThreshold ?? avgContributionMargin,
            ThresholdMethod.Median => CalculateMedian(productGroups.Select(g =>
            {
                var product = g.First().Product;
                return (product?.SellingPrice ?? 0) - (product?.CostPrice ?? 0);
            })),
            _ => avgContributionMargin
        };

        // Classify each product
        foreach (var group in productGroups)
        {
            var product = group.First().Product;
            if (product == null) continue;

            var quantitySold = group.Sum(oi => oi.Quantity);
            var totalRevenue = group.Sum(oi => oi.TotalAmount);
            var totalCost = group.Sum(oi => (product.CostPrice ?? 0) * oi.Quantity);
            var contributionMargin = product.SellingPrice - (product.CostPrice ?? 0);

            var popularityIndex = avgPopularity > 0 ? quantitySold / avgPopularity : 0;
            var profitabilityIndex = avgContributionMargin > 0 ? contributionMargin / avgContributionMargin : 0;

            var isHighPopularity = quantitySold >= report.PopularityThreshold;
            var isHighProfitability = contributionMargin >= report.ProfitabilityThreshold;

            var classification = (isHighPopularity, isHighProfitability) switch
            {
                (true, true) => MenuItemClassification.Star,
                (true, false) => MenuItemClassification.Plow,
                (false, true) => MenuItemClassification.Puzzle,
                _ => MenuItemClassification.Dog
            };

            var item = new MenuEngineeringItem
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.Category?.Name ?? "Uncategorized",
                QuantitySold = (int)quantitySold,
                SellingPrice = product.SellingPrice,
                TotalRevenue = totalRevenue,
                FoodCost = product.CostPrice ?? 0,
                TotalCost = totalCost,
                PopularityIndex = popularityIndex,
                ProfitabilityIndex = profitabilityIndex,
                IsHighPopularity = isHighPopularity,
                IsHighProfitability = isHighProfitability,
                Classification = classification,
                MenuMixPercentage = report.TotalItemsSold > 0
                    ? Math.Round(quantitySold / report.TotalItemsSold * 100, 2) : 0
            };

            (item.Recommendation, item.RecommendationDetail) = GetClassificationRecommendation(classification, item);
            report.Items.Add(item);
        }

        // Category breakdown
        var categoryGroups = report.Items.GroupBy(i => i.CategoryId);
        foreach (var catGroup in categoryGroups)
        {
            var summary = new MenuEngineeringCategorySummary
            {
                CategoryId = catGroup.Key,
                CategoryName = catGroup.First().CategoryName,
                TotalItems = catGroup.Count(),
                TotalSold = catGroup.Sum(i => i.QuantitySold),
                TotalRevenue = catGroup.Sum(i => i.TotalRevenue),
                TotalContribution = catGroup.Sum(i => i.TotalContributionMargin),
                CategoryMixPercentage = report.TotalRevenue > 0
                    ? Math.Round(catGroup.Sum(i => i.TotalRevenue) / report.TotalRevenue * 100, 2) : 0,
                StarCount = catGroup.Count(i => i.Classification == MenuItemClassification.Star),
                PlowCount = catGroup.Count(i => i.Classification == MenuItemClassification.Plow),
                PuzzleCount = catGroup.Count(i => i.Classification == MenuItemClassification.Puzzle),
                DogCount = catGroup.Count(i => i.Classification == MenuItemClassification.Dog)
            };

            summary.HealthScore = Math.Round(
                (summary.StarCount * 100m + summary.PlowCount * 60m + summary.PuzzleCount * 40m) /
                Math.Max(1, summary.TotalItems), 2);

            report.CategoryBreakdown.Add(summary);
        }

        if (parameters.IncludeRecommendations)
        {
            report.Recommendations = GenerateRecommendations(report.Items);
        }

        _logger.LogInformation("Generated Menu Engineering Report: {TotalItems} items, {StarCount} Stars, {DogCount} Dogs",
            report.TotalItems, report.StarCount, report.DogCount);

        return report;
    }

    public async Task<ProductMixReport> GenerateProductMixReportAsync(
        ProductMixParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new ProductMixReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        report.TotalTransactions = orders.Count;

        var baseQuery = context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .AsQueryable();

        if (parameters.CategoryId.HasValue)
            baseQuery = baseQuery.Where(oi => oi.Product!.CategoryId == parameters.CategoryId);

        var orderItems = await baseQuery.ToListAsync(cancellationToken);

        report.TotalItemsSold = (int)orderItems.Sum(oi => oi.Quantity);
        report.TotalRevenue = orderItems.Sum(oi => oi.TotalAmount);

        // Group by product
        var productGroups = orderItems
            .GroupBy(oi => oi.ProductId)
            .Where(g => parameters.IncludeZeroSales || g.Sum(oi => oi.Quantity) > 0)
            .Select(g =>
            {
                var product = g.First().Product;
                return new ProductMixItem
                {
                    ProductId = g.Key,
                    ProductCode = product?.ProductCode ?? "",
                    ProductName = product?.Name ?? "Unknown",
                    CategoryName = product?.Category?.Name ?? "Uncategorized",
                    QuantitySold = (int)g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalAmount),
                    Cost = g.Sum(oi => (product?.CostPrice ?? 0) * oi.Quantity),
                    Price = product?.SellingPrice ?? 0,
                    RevenuePercentage = report.TotalRevenue > 0
                        ? Math.Round(g.Sum(oi => oi.TotalAmount) / report.TotalRevenue * 100, 2) : 0,
                    QuantityPercentage = report.TotalItemsSold > 0
                        ? Math.Round(g.Sum(oi => oi.Quantity) / report.TotalItemsSold * 100, 2) : 0
                };
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        // Calculate ranks
        decimal cumulativeRevenue = 0;
        for (int i = 0; i < productGroups.Count; i++)
        {
            productGroups[i].Rank = i + 1;
            cumulativeRevenue += productGroups[i].RevenuePercentage;
            productGroups[i].CumulativeRevenuePercentage = cumulativeRevenue;
        }

        report.AllItems = productGroups;

        if (parameters.GroupByCategory)
        {
            var categoryGroupsList = productGroups.GroupBy(p => p.CategoryName);
            foreach (var catGroup in categoryGroupsList)
            {
                report.Categories.Add(new ProductMixCategoryGroup
                {
                    CategoryName = catGroup.Key,
                    Revenue = catGroup.Sum(p => p.Revenue),
                    RevenuePercentage = report.TotalRevenue > 0
                        ? Math.Round(catGroup.Sum(p => p.Revenue) / report.TotalRevenue * 100, 2) : 0,
                    QuantitySold = catGroup.Sum(p => p.QuantitySold),
                    QuantityPercentage = report.TotalItemsSold > 0
                        ? Math.Round((decimal)catGroup.Sum(p => p.QuantitySold) / report.TotalItemsSold * 100, 2) : 0,
                    AveragePrice = catGroup.Average(p => p.Price),
                    GrossMargin = catGroup.Sum(p => p.GrossMargin),
                    Items = catGroup.ToList()
                });
            }
        }

        report.TopByRevenue = productGroups.Take(parameters.TopItemsCount).ToList();
        report.TopByQuantity = productGroups.OrderByDescending(p => p.QuantitySold).Take(parameters.TopItemsCount).ToList();
        report.TopByMargin = productGroups.OrderByDescending(p => p.MarginPercentage).Take(parameters.TopItemsCount).ToList();

        return report;
    }

    public async Task<ModifierAnalysisReport> GenerateModifierAnalysisReportAsync(
        ModifierAnalysisParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new ModifierAnalysisReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.TotalOrders = orders.Count;

        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ToListAsync(cancellationToken);

        var orderItemsWithModifiers = orderItems.Where(oi => !string.IsNullOrEmpty(oi.Notes)).ToList();
        report.TotalOrdersWithModifiers = orderItemsWithModifiers.Select(oi => oi.OrderId).Distinct().Count();

        return report;
    }

    public async Task<List<MenuEngineeringRecommendation>> GetRecommendationsAsync(
        int? storeId,
        int? categoryId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var report = await GenerateMenuEngineeringReportAsync(new MenuEngineeringParameters
        {
            StartDate = startDate,
            EndDate = endDate,
            StoreId = storeId,
            CategoryId = categoryId,
            IncludeRecommendations = true
        }, cancellationToken);

        return report.Recommendations;
    }

    public async Task<MenuEngineeringItem> CalculateProductClassificationAsync(
        int productId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var product = await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new ArgumentException($"Product {productId} not found");

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();

        var productItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId) && oi.ProductId == productId)
            .ToListAsync(cancellationToken);

        var quantitySold = productItems.Sum(oi => oi.Quantity);
        var totalRevenue = productItems.Sum(oi => oi.TotalAmount);
        var contributionMargin = product.SellingPrice - product.CostPrice;

        // Get averages for classification
        var allItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ToListAsync(cancellationToken);

        var prodGroups = allItems.GroupBy(oi => oi.ProductId);
        var avgPopularity = prodGroups.Any() ? prodGroups.Average(g => g.Sum(oi => oi.Quantity)) : 0;
        var avgContributionMargin = prodGroups.Any()
            ? prodGroups.Average(g =>
            {
                var p = g.First().Product;
                return (p?.SellingPrice ?? 0) - (p?.CostPrice ?? 0);
            })
            : 0;

        var isHighPopularity = quantitySold >= (decimal)avgPopularity;
        var isHighProfitability = contributionMargin >= avgContributionMargin;

        var classification = (isHighPopularity, isHighProfitability) switch
        {
            (true, true) => MenuItemClassification.Star,
            (true, false) => MenuItemClassification.Plow,
            (false, true) => MenuItemClassification.Puzzle,
            _ => MenuItemClassification.Dog
        };

        return new MenuEngineeringItem
        {
            ProductId = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.Name,
            CategoryId = product.CategoryId ?? 0,
            CategoryName = product.Category?.Name ?? "Uncategorized",
            QuantitySold = (int)quantitySold,
            SellingPrice = product.SellingPrice,
            TotalRevenue = totalRevenue,
            FoodCost = product.CostPrice ?? 0,
            Classification = classification,
            IsHighPopularity = isHighPopularity,
            IsHighProfitability = isHighProfitability
        };
    }

    public async Task<List<MenuEngineeringCategorySummary>> GetCategoryPerformanceAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var report = await GenerateMenuEngineeringReportAsync(new MenuEngineeringParameters
        {
            StartDate = startDate,
            EndDate = endDate,
            StoreId = storeId
        }, cancellationToken);

        return report.CategoryBreakdown;
    }

    public async Task<PriceChangeSimulation> SimulatePriceChangeAsync(
        int productId,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new ArgumentException($"Product {productId} not found");

        var endDate = DateTime.Today;
        var startDate = endDate.AddMonths(-3);

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => oi.ProductId == productId && orderIds.Contains(oi.OrderId))
            .ToListAsync(cancellationToken);

        var currentMonthlySales = orderItems.Sum(oi => oi.Quantity) / 3;

        var priceChange = (newPrice - product.SellingPrice) / product.SellingPrice;
        var elasticity = -1.2m;
        var salesChange = priceChange * elasticity;
        var projectedSales = (int)(currentMonthlySales * (1 + salesChange));

        return new PriceChangeSimulation
        {
            ProductId = productId,
            ProductName = product.Name,
            CurrentPrice = product.SellingPrice,
            CurrentCost = product.CostPrice ?? 0,
            CurrentMonthlySales = (int)currentMonthlySales,
            ProposedPrice = newPrice,
            EstimatedElasticity = elasticity,
            ProjectedMonthlySales = projectedSales,
            ConfidenceLevel = 0.7m,
            Recommendation = salesChange switch
            {
                < -0.15m => "High risk - significant sales decline expected",
                < -0.05m => "Moderate risk - some sales decline expected",
                < 0.05m => "Low risk - minimal impact expected",
                _ => "Positive impact - consider implementation"
            }
        };
    }

    public async Task<TrendingProductsReport> GetTrendingProductsAsync(
        int? storeId,
        int? categoryId,
        int days = 30,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new TrendingProductsReport
        {
            GeneratedAt = DateTime.UtcNow,
            AnalysisDays = days
        };

        var currentEnd = DateTime.Today;
        var currentStart = currentEnd.AddDays(-days);
        var previousEnd = currentStart.AddDays(-1);
        var previousStart = previousEnd.AddDays(-days);

        var currentPeriod = await GetPeriodSalesAsync(context, categoryId, currentStart, currentEnd, cancellationToken);
        var previousPeriod = await GetPeriodSalesAsync(context, categoryId, previousStart, previousEnd, cancellationToken);

        foreach (var current in currentPeriod)
        {
            var previous = previousPeriod.FirstOrDefault(p => p.ProductId == current.ProductId);
            var hasPrevious = previous.ProductId != 0;

            var trending = new TrendingProduct
            {
                ProductId = current.ProductId,
                ProductName = current.ProductName,
                CategoryName = current.CategoryName,
                CurrentPeriodSales = current.Quantity,
                CurrentPeriodRevenue = current.Revenue,
                PreviousPeriodSales = hasPrevious ? previous.Quantity : 0,
                PreviousPeriodRevenue = hasPrevious ? previous.Revenue : 0
            };

            if (hasPrevious && previous.Quantity > 0)
            {
                trending.SalesChangePercentage = Math.Round(
                    (decimal)(current.Quantity - previous.Quantity) / previous.Quantity * 100, 2);
                trending.TrendDirection = trending.SalesChangePercentage switch
                {
                    > 20 => "Up",
                    < -20 => "Down",
                    _ => "Stable"
                };
            }
            else if (!hasPrevious)
            {
                trending.TrendDirection = "New";
                report.NewProducts.Add(trending);
                continue;
            }

            if (trending.SalesChangePercentage > 10)
                report.TrendingUp.Add(trending);
            else if (trending.SalesChangePercentage < -10)
                report.TrendingDown.Add(trending);

            if (trending.SalesChangePercentage < -30)
                report.Declining.Add(trending);
        }

        report.TrendingUp = report.TrendingUp.OrderByDescending(t => t.SalesChangePercentage).Take(topCount).ToList();
        report.TrendingDown = report.TrendingDown.OrderBy(t => t.SalesChangePercentage).Take(topCount).ToList();
        report.Declining = report.Declining.OrderBy(t => t.SalesChangePercentage).Take(topCount).ToList();

        return report;
    }

    private async Task<List<(int ProductId, string ProductName, string CategoryName, int Quantity, decimal Revenue)>> GetPeriodSalesAsync(
        POSDbContext context,
        int? categoryId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();

        var baseQuery = context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            baseQuery = baseQuery.Where(oi => oi.Product!.CategoryId == categoryId);

        var items = await baseQuery.ToListAsync(cancellationToken);

        return items
            .GroupBy(oi => oi.ProductId)
            .Select(g => (
                ProductId: g.Key,
                ProductName: g.First().Product?.Name ?? "Unknown",
                CategoryName: g.First().Product?.Category?.Name ?? "Uncategorized",
                Quantity: (int)g.Sum(oi => oi.Quantity),
                Revenue: g.Sum(oi => oi.TotalAmount)
            ))
            .ToList();
    }

    private static decimal CalculateMedian(IEnumerable<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (!sorted.Any()) return 0;

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    private static (string Recommendation, string Detail) GetClassificationRecommendation(
        MenuItemClassification classification, MenuEngineeringItem item)
    {
        return classification switch
        {
            MenuItemClassification.Star => ("Maintain", $"Top performer. Maintain quality and visibility."),
            MenuItemClassification.Plow => ("Increase Margin", $"High popularity but low margin ({item.ContributionMarginPercentage:F1}%). Consider price increase."),
            MenuItemClassification.Puzzle => ("Promote", $"Good margin ({item.ContributionMarginPercentage:F1}%) but low sales. Increase visibility."),
            MenuItemClassification.Dog => ("Review", $"Low sales and low margin. Consider removal or redesign."),
            _ => ("Review", "Analyze performance.")
        };
    }

    private static List<MenuEngineeringRecommendation> GenerateRecommendations(List<MenuEngineeringItem> items)
    {
        var recommendations = new List<MenuEngineeringRecommendation>();

        var worstDogs = items.Where(i => i.Classification == MenuItemClassification.Dog)
            .OrderBy(i => i.TotalContributionMargin).Take(3);

        foreach (var dog in worstDogs)
        {
            recommendations.Add(new MenuEngineeringRecommendation
            {
                ProductId = dog.ProductId,
                ProductName = dog.ProductName,
                CurrentClassification = dog.Classification,
                RecommendationType = "Remove",
                Title = $"Consider removing {dog.ProductName}",
                Description = $"Low popularity ({dog.QuantitySold} sold) and low margin",
                Priority = 1
            });
        }

        var priceablePlows = items.Where(i => i.Classification == MenuItemClassification.Plow)
            .OrderByDescending(i => i.QuantitySold).Take(3);

        foreach (var plow in priceablePlows)
        {
            recommendations.Add(new MenuEngineeringRecommendation
            {
                ProductId = plow.ProductId,
                ProductName = plow.ProductName,
                CurrentClassification = plow.Classification,
                RecommendationType = "Price",
                Title = $"Increase price of {plow.ProductName}",
                Description = $"Popular item ({plow.QuantitySold} sold) with room for price increase",
                Priority = 2
            });
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }
}
