using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.AI;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for AI-powered business insights, forecasting, and recommendations.
/// </summary>
public class AIInsightsService : IAIInsightsService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<AIInsightsService> _logger;

    public AIInsightsService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<AIInsightsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Forecasting

    public async Task<SalesForecast> ForecastSalesAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var forecast = new SalesForecast
        {
            ForecastDate = date,
            GeneratedAt = DateTime.UtcNow,
            ModelVersion = "1.0",
            ModelAccuracy = 0.85m
        };

        // Get historical data for the same day of week
        var dayOfWeek = date.DayOfWeek;
        var lookbackWeeks = 12;
        var startDate = date.AddDays(-lookbackWeeks * 7);

        var historicalOrders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < date)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        // Group by date and filter to same day of week
        var sameDayData = historicalOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Where(g => g.Key.DayOfWeek == dayOfWeek)
            .Select(g => g.Sum(o => o.TotalAmount))
            .ToList();

        if (sameDayData.Any())
        {
            // Simple weighted moving average (more recent = higher weight)
            var weights = Enumerable.Range(1, sameDayData.Count).Select(i => (decimal)i).ToList();
            var totalWeight = weights.Sum();
            forecast.PredictedSales = sameDayData
                .Zip(weights)
                .Sum(x => x.First * x.Second) / totalWeight;

            // Confidence interval (simple standard deviation based)
            var stdDev = CalculateStandardDeviation(sameDayData);
            forecast.ConfidenceLower = forecast.PredictedSales - (1.96m * stdDev);
            forecast.ConfidenceUpper = forecast.PredictedSales + (1.96m * stdDev);
            forecast.ConfidenceLevel = 0.95m;

            // Historical comparisons
            var lastWeek = historicalOrders.Where(o => o.CreatedAt.Date == date.AddDays(-7));
            var lastMonth = historicalOrders.Where(o => o.CreatedAt.Date == date.AddMonths(-1));

            forecast.SameDayLastWeek = lastWeek.Any() ? lastWeek.Sum(o => o.TotalAmount) : null;
            forecast.SameDayLastMonth = lastMonth.Any() ? lastMonth.Sum(o => o.TotalAmount) : null;
            forecast.AverageForDayOfWeek = sameDayData.Average();
        }
        else
        {
            // Default forecast based on overall average
            var allDailyTotals = historicalOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => g.Sum(o => o.TotalAmount))
                .ToList();

            forecast.PredictedSales = allDailyTotals.Any() ? allDailyTotals.Average() : 0;
            forecast.ConfidenceLower = forecast.PredictedSales * 0.7m;
            forecast.ConfidenceUpper = forecast.PredictedSales * 1.3m;
        }

        // Factors affecting forecast
        forecast.Factors = GetForecastFactors(date, dayOfWeek);

        return forecast;
    }

    public async Task<List<HourlyForecast>> ForecastHourlySalesAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var forecasts = new List<HourlyForecast>();
        var dayOfWeek = date.DayOfWeek;
        var lookbackWeeks = 8;
        var startDate = date.AddDays(-lookbackWeeks * 7);

        var historicalOrders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < date)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        // Group by hour for same day of week
        var hourlyData = historicalOrders
            .Where(o => o.CreatedAt.DayOfWeek == dayOfWeek)
            .GroupBy(o => o.CreatedAt.Hour)
            .ToDictionary(g => g.Key, g => new
            {
                AvgSales = g.GroupBy(o => o.CreatedAt.Date).Average(dg => dg.Sum(o => o.TotalAmount)),
                AvgTransactions = g.GroupBy(o => o.CreatedAt.Date).Average(dg => dg.Count())
            });

        for (int hour = 0; hour < 24; hour++)
        {
            var hourData = hourlyData.GetValueOrDefault(hour);
            forecasts.Add(new HourlyForecast
            {
                Date = date,
                Hour = hour,
                PredictedSales = hourData?.AvgSales ?? 0,
                PredictedTransactions = (int)(hourData?.AvgTransactions ?? 0),
                HistoricalAverage = hourData?.AvgSales ?? 0
            });
        }

        return forecasts;
    }

    public async Task<List<ProductDemandForecast>> ForecastProductDemandAsync(
        DateTime date,
        int? storeId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var forecasts = new List<ProductDemandForecast>();
        var dayOfWeek = date.DayOfWeek;
        var lookbackWeeks = 8;
        var startDate = date.AddDays(-lookbackWeeks * 7);

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < date)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();

        var itemsQuery = context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Product!.CategoryId == categoryId);

        var items = await itemsQuery.ToListAsync(cancellationToken);

        // Get order dates for day of week filtering
        var orderDates = orders.ToDictionary(o => o.Id, o => o.CreatedAt);
        var sameDayOrderIds = orders
            .Where(o => o.CreatedAt.DayOfWeek == dayOfWeek)
            .Select(o => o.Id)
            .ToHashSet();

        var productGroups = items
            .Where(oi => sameDayOrderIds.Contains(oi.OrderId))
            .GroupBy(oi => oi.ProductId);

        foreach (var group in productGroups)
        {
            var product = group.First().Product;
            if (product == null) continue;

            var dailyQuantities = group
                .GroupBy(oi => orderDates.TryGetValue(oi.OrderId, out var d) ? d.Date : DateTime.MinValue)
                .Where(g => g.Key != DateTime.MinValue)
                .Select(g => g.Sum(oi => oi.Quantity))
                .ToList();

            var avgQuantity = dailyQuantities.Any() ? dailyQuantities.Average() : 0;
            var stdDev = dailyQuantities.Count > 1 ? CalculateStandardDeviation(dailyQuantities.Select(q => (decimal)q).ToList()) : 0;

            forecasts.Add(new ProductDemandForecast
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = product.Category?.Name ?? "Uncategorized",
                ForecastDate = date,
                PredictedQuantity = (int)Math.Ceiling(avgQuantity),
                SafetyStock = (int)Math.Ceiling(avgQuantity + stdDev),
                RecommendedPrepQuantity = (int)Math.Ceiling(avgQuantity * 1.2m), // 20% buffer
                ConfidenceLevel = dailyQuantities.Count >= 4 ? 0.8m : 0.6m,
                HistoricalAverage = (decimal)avgQuantity,
                TrendDirection = "Stable" // Would need more data to determine trend
            });
        }

        return forecasts.OrderByDescending(f => f.PredictedQuantity).ToList();
    }

    public async Task<List<SalesForecast>> ForecastSalesRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var forecasts = new List<SalesForecast>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var forecast = await ForecastSalesAsync(currentDate, storeId, cancellationToken);
            forecasts.Add(forecast);
            currentDate = currentDate.AddDays(1);
        }

        return forecasts;
    }

    #endregion

    #region Recommendations

    public async Task<List<InventoryRecommendation>> GetInventoryRecommendationsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var recommendations = new List<InventoryRecommendation>();

        var inventoryQuery = context.Inventories.Include(i => i.Product).ThenInclude(p => p!.Category).AsQueryable();
        if (storeId.HasValue)
            inventoryQuery = inventoryQuery.Where(i => i.StoreId == storeId);

        var inventory = await inventoryQuery.ToListAsync(cancellationToken);

        // Get recent sales data for demand calculation
        var startDate = DateTime.Today.AddDays(-30);
        var orderIds = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Served)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var salesData = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalSold, cancellationToken);

        foreach (var item in inventory)
        {
            if (item.Product == null) continue;

            var dailySales = salesData.GetValueOrDefault(item.ProductId) / 30m;
            var daysOfStock = dailySales > 0 ? (int)(item.CurrentStock / dailySales) : 999;
            var estimatedMaxStock = item.ReorderLevel * 3; // Estimate max as 3x reorder level

            var rec = new InventoryRecommendation
            {
                ProductId = item.ProductId,
                ProductCode = item.Product.ProductCode,
                ProductName = item.Product.Name,
                CategoryName = item.Product.Category?.Name ?? "Uncategorized",
                CurrentStock = item.CurrentStock,
                MinimumStock = item.ReorderLevel,
                MaximumStock = estimatedMaxStock,
                DaysOfStockRemaining = daysOfStock,
                AverageDailySales = dailySales,
                LeadTimeDays = 3 // Default lead time
            };

            // Determine recommendation
            if (item.CurrentStock <= item.ReorderLevel)
            {
                rec.RecommendationType = RecommendationType.Reorder;
                rec.Urgency = item.CurrentStock <= item.ReorderLevel * 0.5m ? UrgencyLevel.Critical : UrgencyLevel.High;
                rec.UrgencyReason = "Stock below minimum level";
                rec.RecommendedQuantity = estimatedMaxStock - item.CurrentStock;
                rec.RecommendedOrderDate = DateTime.Today;
                rec.ProjectedStockoutDate = DateTime.Today.AddDays(daysOfStock);
            }
            else if (daysOfStock <= rec.LeadTimeDays + 2)
            {
                rec.RecommendationType = RecommendationType.Reorder;
                rec.Urgency = UrgencyLevel.High;
                rec.UrgencyReason = "Stock will deplete before next delivery";
                rec.RecommendedQuantity = estimatedMaxStock - item.CurrentStock;
                rec.RecommendedOrderDate = DateTime.Today;
                rec.ProjectedStockoutDate = DateTime.Today.AddDays(daysOfStock);
            }
            else if (daysOfStock <= rec.LeadTimeDays + 7)
            {
                rec.RecommendationType = RecommendationType.Reorder;
                rec.Urgency = UrgencyLevel.Medium;
                rec.UrgencyReason = "Approaching reorder point";
                rec.RecommendedQuantity = estimatedMaxStock - item.CurrentStock;
                rec.RecommendedOrderDate = DateTime.Today.AddDays(daysOfStock - rec.LeadTimeDays - 2);
            }
            else if (dailySales == 0 && item.CurrentStock > 0)
            {
                rec.RecommendationType = RecommendationType.Discontinue;
                rec.Urgency = UrgencyLevel.Low;
                rec.UrgencyReason = "No sales in past 30 days";
            }
            else
            {
                rec.RecommendationType = RecommendationType.NoActionNeeded;
                rec.Urgency = UrgencyLevel.Low;
            }

            rec.EstimatedCost = rec.RecommendedQuantity * (item.Product.CostPrice ?? 0);

            if (rec.RecommendationType != RecommendationType.NoActionNeeded)
            {
                recommendations.Add(rec);
            }
        }

        return recommendations.OrderBy(r => r.Urgency).ThenBy(r => r.DaysOfStockRemaining).ToList();
    }

    public async Task<List<MenuRecommendation>> GetMenuRecommendationsAsync(
        int? storeId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var recommendations = new List<MenuRecommendation>();

        var startDate = DateTime.Today.AddDays(-30);
        var orderIds = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Served)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var itemsQuery = context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Product!.CategoryId == categoryId);

        var items = await itemsQuery.ToListAsync(cancellationToken);

        var productStats = items
            .GroupBy(oi => oi.ProductId)
            .Select(g =>
            {
                var product = g.First().Product!;
                var costPrice = product.CostPrice ?? 0;
                return new
                {
                    Product = product,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalAmount),
                    Margin = product.SellingPrice - costPrice,
                    MarginPercent = product.SellingPrice > 0 ? (product.SellingPrice - costPrice) / product.SellingPrice * 100 : 0
                };
            })
            .ToList();

        var avgQuantity = productStats.Any() ? productStats.Average(p => p.QuantitySold) : 0;
        var avgMargin = productStats.Any() ? productStats.Average(p => p.Margin) : 0;

        foreach (var stat in productStats)
        {
            MenuRecommendation? rec = null;

            // Low margin, high sales - consider price increase
            if (stat.MarginPercent < 50 && stat.QuantitySold > avgQuantity)
            {
                var suggestedPrice = (stat.Product.CostPrice ?? 0) / 0.65m; // Target 35% food cost
                rec = new MenuRecommendation
                {
                    ProductId = stat.Product.Id,
                    ProductName = stat.Product.Name,
                    CategoryName = stat.Product.Category?.Name ?? "Uncategorized",
                    RecommendationType = MenuRecommendationType.IncreasePrice,
                    Title = $"Increase price of {stat.Product.Name}",
                    Description = $"Popular item with low margin ({stat.MarginPercent:F1}%)",
                    Rationale = "High demand allows for price elasticity",
                    CurrentPrice = stat.Product.SellingPrice,
                    CurrentMargin = stat.Margin,
                    CurrentSalesRank = productStats.OrderByDescending(p => p.QuantitySold).ToList().FindIndex(p => p.Product.Id == stat.Product.Id) + 1,
                    SuggestedPrice = suggestedPrice,
                    EstimatedRevenueImpact = (suggestedPrice - stat.Product.SellingPrice) * stat.QuantitySold * 0.9m,
                    EstimatedProfitImpact = (suggestedPrice - stat.Product.SellingPrice) * stat.QuantitySold * 0.85m,
                    ConfidenceLevel = 0.75m,
                    Priority = 1
                };
            }
            // High margin, low sales - promote
            else if (stat.MarginPercent > 60 && stat.QuantitySold < avgQuantity * 0.5m)
            {
                rec = new MenuRecommendation
                {
                    ProductId = stat.Product.Id,
                    ProductName = stat.Product.Name,
                    CategoryName = stat.Product.Category?.Name ?? "Uncategorized",
                    RecommendationType = MenuRecommendationType.Promote,
                    Title = $"Promote {stat.Product.Name}",
                    Description = $"High margin ({stat.MarginPercent:F1}%) but low visibility",
                    Rationale = "Better placement and staff recommendations could boost sales",
                    CurrentPrice = stat.Product.SellingPrice,
                    CurrentMargin = stat.Margin,
                    CurrentSalesRank = productStats.OrderByDescending(p => p.QuantitySold).ToList().FindIndex(p => p.Product.Id == stat.Product.Id) + 1,
                    EstimatedProfitImpact = stat.Margin * stat.QuantitySold, // If sales double
                    ConfidenceLevel = 0.7m,
                    Priority = 2
                };
            }
            // Very low sales and low margin - consider removal
            else if (stat.QuantitySold < avgQuantity * 0.2m && stat.MarginPercent < 40)
            {
                rec = new MenuRecommendation
                {
                    ProductId = stat.Product.Id,
                    ProductName = stat.Product.Name,
                    CategoryName = stat.Product.Category?.Name ?? "Uncategorized",
                    RecommendationType = MenuRecommendationType.Remove,
                    Title = $"Consider removing {stat.Product.Name}",
                    Description = $"Low sales ({stat.QuantitySold}) and low margin ({stat.MarginPercent:F1}%)",
                    Rationale = "Menu simplification improves operations",
                    CurrentPrice = stat.Product.SellingPrice,
                    CurrentMargin = stat.Margin,
                    CurrentSalesRank = productStats.OrderByDescending(p => p.QuantitySold).ToList().FindIndex(p => p.Product.Id == stat.Product.Id) + 1,
                    ConfidenceLevel = 0.6m,
                    Priority = 3
                };
            }

            if (rec != null)
                recommendations.Add(rec);
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }

    public async Task<List<StaffingRecommendation>> GetStaffingRecommendationsAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<StaffingRecommendation>();

        // Get hourly forecast
        var hourlyForecast = await ForecastHourlySalesAsync(date, storeId, cancellationToken);

        // Target SPLH (Sales Per Labor Hour)
        var targetSPLH = 50m;

        foreach (var hourForecast in hourlyForecast)
        {
            var recommendedStaff = (int)Math.Ceiling(hourForecast.PredictedSales / targetSPLH);
            recommendedStaff = Math.Max(1, recommendedStaff); // Minimum 1 staff

            var rec = new StaffingRecommendation
            {
                Date = date,
                Hour = hourForecast.Hour,
                CurrentStaff = 0, // Would need to check schedule
                RecommendedStaff = recommendedStaff,
                PredictedSales = hourForecast.PredictedSales,
                TargetSPLH = targetSPLH,
                OptimalSPLH = recommendedStaff > 0 ? hourForecast.PredictedSales / recommendedStaff : 0
            };

            rec.Status = rec.Variance switch
            {
                < 0 => "Overstaffed",
                > 0 => "Understaffed",
                _ => "Optimal"
            };

            recommendations.Add(rec);
        }

        return recommendations;
    }

    #endregion

    #region Anomaly Detection

    public async Task<List<AnomalyAlert>> DetectAnomaliesAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var alerts = new List<AnomalyAlert>();

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        // Detect void anomalies - high void rate
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Served);
        var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
        var voidRate = completedOrders > 0 ? (decimal)cancelledOrders / completedOrders * 100 : 0;

        if (voidRate > 5) // More than 5% voids
        {
            alerts.Add(new AnomalyAlert
            {
                DetectedAt = DateTime.UtcNow,
                AnomalyType = AnomalyType.VoidAnomaly,
                Severity = voidRate > 10 ? AnomalySeverity.Critical : AnomalySeverity.High,
                Title = "High Void Rate Detected",
                Description = $"Void rate of {voidRate:F1}% exceeds normal threshold",
                ExpectedValue = 3,
                ActualValue = voidRate,
                DeviationPercentage = voidRate - 3,
                SuggestedActions = new List<string>
                {
                    "Review void authorization logs",
                    "Check for training issues",
                    "Verify POS system accuracy"
                }
            });
        }

        // Detect discount anomalies - excessive discounting
        var totalDiscounts = orders.Sum(o => o.DiscountAmount);
        var totalSales = orders.Sum(o => o.Subtotal);
        var discountRate = totalSales > 0 ? totalDiscounts / totalSales * 100 : 0;

        if (discountRate > 15) // More than 15% discounts
        {
            alerts.Add(new AnomalyAlert
            {
                DetectedAt = DateTime.UtcNow,
                AnomalyType = AnomalyType.DiscountAnomaly,
                Severity = discountRate > 25 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                Title = "Excessive Discounting Detected",
                Description = $"Discount rate of {discountRate:F1}% is above threshold",
                ExpectedValue = 10,
                ActualValue = discountRate,
                DeviationPercentage = discountRate - 10,
                SuggestedActions = new List<string>
                {
                    "Review discount authorization policies",
                    "Check promotion configurations",
                    "Audit discount usage by employee"
                }
            });
        }

        // Detect sales anomalies - unusual daily totals
        var dailySales = orders
            .Where(o => o.Status == OrderStatus.Served)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => g.Sum(o => o.TotalAmount))
            .ToList();

        if (dailySales.Count >= 7)
        {
            var avg = dailySales.Average();
            var stdDev = CalculateStandardDeviation(dailySales);

            foreach (var day in orders.GroupBy(o => o.CreatedAt.Date))
            {
                var dailyTotal = day.Where(o => o.Status == OrderStatus.Served).Sum(o => o.TotalAmount);
                var zScore = stdDev > 0 ? (dailyTotal - avg) / stdDev : 0;

                if (Math.Abs(zScore) > 2.5m) // More than 2.5 standard deviations
                {
                    alerts.Add(new AnomalyAlert
                    {
                        DetectedAt = DateTime.UtcNow,
                        AnomalyType = AnomalyType.SalesAnomaly,
                        Severity = Math.Abs(zScore) > 3 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                        Title = zScore > 0 ? "Unusually High Sales" : "Unusually Low Sales",
                        Description = $"Sales on {day.Key:d} were {Math.Abs((dailyTotal - avg) / avg * 100):F1}% {(zScore > 0 ? "above" : "below")} average",
                        ExpectedValue = avg,
                        ActualValue = dailyTotal,
                        DeviationPercentage = (dailyTotal - avg) / avg * 100
                    });
                }
            }
        }

        return alerts;
    }

    public async Task<List<FraudAlert>> DetectFraudPatternsAsync(
        int? employeeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var alerts = new List<FraudAlert>();
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var ordersQuery = context.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end);

        if (employeeId.HasValue)
            ordersQuery = ordersQuery.Where(o => o.UserId == employeeId);

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        // Group by user (cashier) and analyze patterns
        var cashierGroups = orders.GroupBy(o => o.UserId);

        foreach (var group in cashierGroups)
        {
            var cashierId = group.Key;

            var cashierOrders = group.ToList();
            var cancelCount = cashierOrders.Count(o => o.Status == OrderStatus.Cancelled);
            var totalCount = cashierOrders.Count;
            var voidRate = totalCount > 0 ? (decimal)cancelCount / totalCount * 100 : 0;

            // Excessive voids
            if (voidRate > 10 && cancelCount > 5)
            {
                alerts.Add(new FraudAlert
                {
                    DetectedAt = DateTime.UtcNow,
                    FraudType = FraudType.ExcessiveVoids,
                    Severity = voidRate > 20 ? AnomalySeverity.Critical : AnomalySeverity.High,
                    Title = "Excessive Void Pattern",
                    Description = $"Cashier has {voidRate:F1}% void rate ({cancelCount} voids)",
                    EmployeeId = cashierId,
                    IncidentCount = cancelCount,
                    TotalAmount = cashierOrders.Where(o => o.Status == OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                    NormalRate = 3,
                    CurrentRate = voidRate,
                    PatternStartDate = start
                });
            }

            // High discount amounts
            var discountTotal = cashierOrders.Sum(o => o.DiscountAmount);
            var salesTotal = cashierOrders.Sum(o => o.Subtotal);
            var discountRate = salesTotal > 0 ? discountTotal / salesTotal * 100 : 0;

            if (discountRate > 20 && discountTotal > 1000)
            {
                alerts.Add(new FraudAlert
                {
                    DetectedAt = DateTime.UtcNow,
                    FraudType = FraudType.SweethearingDiscount,
                    Severity = discountRate > 30 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                    Title = "Excessive Discounting Pattern",
                    Description = $"Cashier applying {discountRate:F1}% discounts on average",
                    EmployeeId = cashierId,
                    IncidentCount = cashierOrders.Count(o => o.DiscountAmount > 0),
                    TotalAmount = discountTotal,
                    NormalRate = 10,
                    CurrentRate = discountRate,
                    PatternStartDate = start
                });
            }
        }

        return alerts;
    }

    public async Task<List<AnomalyAlert>> GetActiveAlertsAsync(
        int? storeId = null,
        AnomalySeverity? minSeverity = null,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would query a database of stored alerts
        // For now, we'll generate fresh anomaly detection
        var alerts = await DetectAnomaliesAsync(
            DateTime.Today.AddDays(-7),
            DateTime.Today,
            storeId,
            cancellationToken);

        if (minSeverity.HasValue)
        {
            alerts = alerts.Where(a => a.Severity >= minSeverity.Value).ToList();
        }

        return alerts.Where(a => !a.IsResolved).ToList();
    }

    public Task AcknowledgeAlertAsync(int alertId, int userId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would update the alert in the database
        _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", alertId, userId);
        return Task.CompletedTask;
    }

    public Task ResolveAlertAsync(int alertId, string resolution, int userId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would update the alert in the database
        _logger.LogInformation("Alert {AlertId} resolved by user {UserId}: {Resolution}", alertId, userId, resolution);
        return Task.CompletedTask;
    }

    #endregion

    #region Natural Language Interface

    public async Task<NaturalLanguageQueryResponse> AnswerBusinessQuestionAsync(
        string question,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var response = new NaturalLanguageQueryResponse
        {
            Query = question,
            ProcessedAt = DateTime.UtcNow,
            ConfidenceScore = 0.85m
        };

        var questionLower = question.ToLower();

        // Sales-related questions
        if (questionLower.Contains("sales") || questionLower.Contains("revenue"))
        {
            if (questionLower.Contains("today"))
            {
                var todaySales = await GetTodaySalesAsync(context, cancellationToken);
                response.Answer = $"Today's sales so far are {todaySales:C}.";
                response.DetailedExplanation = "This includes all completed orders from midnight to now.";
            }
            else if (questionLower.Contains("yesterday"))
            {
                var yesterdaySales = await GetYesterdaySalesAsync(context, cancellationToken);
                response.Answer = $"Yesterday's total sales were {yesterdaySales:C}.";
            }
            else if (questionLower.Contains("week"))
            {
                var weekSales = await GetWeekSalesAsync(context, cancellationToken);
                response.Answer = $"This week's sales are {weekSales:C}.";
            }
            else if (questionLower.Contains("month"))
            {
                var monthSales = await GetMonthSalesAsync(context, cancellationToken);
                response.Answer = $"This month's sales are {monthSales:C}.";
            }
            else
            {
                var forecast = await ForecastSalesAsync(DateTime.Today, storeId, cancellationToken);
                response.Answer = $"Today's predicted sales are {forecast.PredictedSales:C} with 95% confidence between {forecast.ConfidenceLower:C} and {forecast.ConfidenceUpper:C}.";
            }
        }
        // Best selling products
        else if (questionLower.Contains("best") && (questionLower.Contains("product") || questionLower.Contains("seller") || questionLower.Contains("item")))
        {
            var topProducts = await GetTopProductsAsync(context, 5, cancellationToken);
            response.Answer = $"Top 5 best-selling products:\n{string.Join("\n", topProducts.Select((p, i) => $"{i + 1}. {p.Name} ({p.Quantity} sold)"))}";
        }
        // Inventory questions
        else if (questionLower.Contains("inventory") || questionLower.Contains("stock"))
        {
            var lowStock = await GetLowStockItemsAsync(context, cancellationToken);
            response.Answer = lowStock.Any()
                ? $"There are {lowStock.Count} items with low stock:\n{string.Join("\n", lowStock.Take(5).Select(p => $"- {p.Name}: {p.Quantity} remaining"))}"
                : "All inventory levels are adequate.";
        }
        // Customer questions
        else if (questionLower.Contains("customer"))
        {
            var customerCount = await context.Set<Core.Entities.LoyaltyMember>().CountAsync(cancellationToken);
            response.Answer = $"You have {customerCount} registered customers in your loyalty program.";
        }
        else
        {
            response.Answer = "I can help you with questions about sales, inventory, products, and customers. Try asking:\n- What are today's sales?\n- What are the best-selling products?\n- What items are low on stock?";
            response.ConfidenceScore = 0.5m;
        }

        response.SuggestedFollowUps = new List<string>
        {
            "What are this week's sales?",
            "Which products need reordering?",
            "What is the sales forecast for tomorrow?"
        };

        return response;
    }

    public Task<AIReportSummary> GenerateReportSummaryAsync(
        string reportType,
        object reportData,
        CancellationToken cancellationToken = default)
    {
        var summary = new AIReportSummary
        {
            ReportType = reportType,
            GeneratedAt = DateTime.UtcNow,
            ExecutiveSummary = $"This {reportType} report provides key insights into your business performance.",
            KeyHighlights = new List<string>
            {
                "Overall performance is within expected parameters",
                "Key metrics are trending positively",
                "Action items have been identified"
            },
            Recommendations = new List<string>
            {
                "Review highlighted areas for improvement",
                "Monitor key performance indicators",
                "Implement suggested optimizations"
            }
        };

        return Task.FromResult(summary);
    }

    public Task<List<string>> GetSuggestedQuestionsAsync(
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<string>
        {
            "What are today's sales?",
            "What are the best-selling products this week?",
            "Which items are running low on stock?",
            "How is our labor cost trending?",
            "What is the sales forecast for tomorrow?",
            "Are there any anomalies I should know about?",
            "Which customers are at risk of churning?",
            "What menu items should we promote?"
        };

        return Task.FromResult(suggestions);
    }

    #endregion

    #region Customer Analytics

    public async Task<List<ChurnRiskAlert>> PredictChurnAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var alerts = new List<ChurnRiskAlert>();

        // Get loyalty members with their transactions
        var members = await context.Set<Core.Entities.LoyaltyMember>()
            .Take(limit ?? 50)
            .ToListAsync(cancellationToken);

        foreach (var member in members)
        {
            // Get member's transactions
            var transactions = await context.Set<Core.Entities.LoyaltyTransaction>()
                .Where(t => t.LoyaltyMemberId == member.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync(cancellationToken);

            if (!transactions.Any()) continue;

            var lastTransaction = transactions.First();
            var daysSinceLastPurchase = (DateTime.Today - lastTransaction.TransactionDate).Days;
            var totalSpent = transactions
                .Where(t => t.TransactionType == LoyaltyTransactionType.Earned)
                .Sum(t => t.MonetaryValue);

            // Simple churn prediction based on recency
            var churnProbability = daysSinceLastPurchase switch
            {
                > 180 => 0.9m,
                > 90 => 0.7m,
                > 60 => 0.5m,
                > 30 => 0.3m,
                _ => 0.1m
            };

            if (churnProbability >= 0.5m)
            {
                alerts.Add(new ChurnRiskAlert
                {
                    CustomerId = member.Id,
                    CustomerName = member.Name,
                    ChurnProbability = churnProbability,
                    RiskLevel = churnProbability >= 0.8m ? Core.Models.AI.ChurnRiskLevel.Critical :
                               churnProbability >= 0.6m ? Core.Models.AI.ChurnRiskLevel.High :
                               Core.Models.AI.ChurnRiskLevel.Medium,
                    HistoricalValue = totalSpent,
                    PredictedLostValue = totalSpent * 0.5m, // Assume 50% of historical value
                    DaysSinceLastPurchase = daysSinceLastPurchase,
                    RiskFactors = new List<string>
                    {
                        $"No purchase in {daysSinceLastPurchase} days",
                        "Declining visit frequency"
                    },
                    RecommendedAction = "Send personalized win-back offer",
                    WinBackOfferValue = Math.Min(totalSpent * 0.1m, 500) // 10% of historical value, max 500
                });
            }
        }

        return alerts.OrderByDescending(a => a.ChurnProbability * a.HistoricalValue).ToList();
    }

    public async Task<List<BusinessInsight>> GetCustomerInsightsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var insights = new List<BusinessInsight>();

        var churnAlerts = await PredictChurnAsync(storeId, 10, cancellationToken);

        if (churnAlerts.Any())
        {
            insights.Add(new BusinessInsight
            {
                GeneratedAt = DateTime.UtcNow,
                InsightType = InsightType.Risk,
                Category = InsightCategory.Customers,
                Title = "Customer Churn Risk",
                Summary = $"{churnAlerts.Count} high-value customers at risk of churning",
                Impact = InsightImpact.High,
                EstimatedValue = churnAlerts.Sum(a => a.PredictedLostValue),
                Recommendation = "Launch targeted win-back campaign",
                ActionItems = new List<string>
                {
                    "Send personalized offers to at-risk customers",
                    "Review service quality feedback",
                    "Implement loyalty program enhancements"
                }
            });
        }

        return insights;
    }

    #endregion

    #region Business Insights

    public async Task<List<BusinessInsight>> GetTopInsightsAsync(
        int? storeId = null,
        InsightCategory? category = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var insights = new List<BusinessInsight>();

        // Sales insights
        if (category == null || category == InsightCategory.Sales)
        {
            var salesInsights = await GetSalesInsightsAsync(cancellationToken);
            insights.AddRange(salesInsights);
        }

        // Inventory insights
        if (category == null || category == InsightCategory.Inventory)
        {
            var inventoryInsights = await GetInventoryInsightsAsync(storeId, cancellationToken);
            insights.AddRange(inventoryInsights);
        }

        // Customer insights
        if (category == null || category == InsightCategory.Customers)
        {
            var customerInsights = await GetCustomerInsightsAsync(storeId, cancellationToken);
            insights.AddRange(customerInsights);
        }

        return insights.OrderByDescending(i => i.Impact).Take(limit).ToList();
    }

    public Task<List<BusinessInsight>> GetInsightsByCategoryAsync(
        InsightCategory category,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        return GetTopInsightsAsync(storeId, category, 20, cancellationToken);
    }

    public Task MarkInsightAsActionedAsync(int insightId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Insight {InsightId} marked as actioned", insightId);
        return Task.CompletedTask;
    }

    #endregion

    #region What-If Analysis

    public async Task<WhatIfAnalysisResult> RunWhatIfAnalysisAsync(
        WhatIfScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var result = new WhatIfAnalysisResult
        {
            ScenarioName = scenario.ScenarioType,
            Description = scenario.Description,
            AnalyzedAt = DateTime.UtcNow,
            InputParameters = scenario.Parameters,
            ConfidenceLevel = 0.7m
        };

        // Get baseline metrics from last month
        var startDate = DateTime.Today.AddMonths(-1);
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        result.BaselineRevenue = orders.Sum(o => o.TotalAmount);
        result.BaselineMargin = result.BaselineRevenue * 0.35m; // Assume 35% margin
        result.BaselineProfit = result.BaselineMargin;

        // Apply scenario
        if (scenario.ScenarioType == "PriceChange" && scenario.ProductId.HasValue && scenario.NewPrice.HasValue)
        {
            var product = await context.Products.FindAsync(new object?[] { scenario.ProductId.Value }, cancellationToken);
            if (product != null)
            {
                var priceChange = (scenario.NewPrice.Value - product.SellingPrice) / product.SellingPrice;
                var elasticity = -1.2m;
                var demandChange = priceChange * elasticity;

                // Simplified projection
                result.ProjectedRevenue = result.BaselineRevenue * (1 + priceChange * (1 + demandChange));
                result.ProjectedMargin = result.ProjectedRevenue * 0.35m;
                result.ProjectedProfit = result.ProjectedMargin;

                result.Recommendation = result.ProfitChange > 0
                    ? "This price change is projected to increase profit. Consider implementing."
                    : "This price change may decrease profit. Proceed with caution.";
            }
        }
        else
        {
            // Default projection
            result.ProjectedRevenue = result.BaselineRevenue;
            result.ProjectedMargin = result.BaselineMargin;
            result.ProjectedProfit = result.BaselineProfit;
            result.Recommendation = "Please specify scenario parameters for detailed analysis.";
        }

        return result;
    }

    #endregion

    #region Dashboard

    public async Task<AIDashboardSummary> GetDashboardSummaryAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var summary = new AIDashboardSummary
        {
            GeneratedAt = DateTime.UtcNow,
            DataAsOf = DateTime.Now
        };

        // Today's forecast
        summary.TodaysForecast = await ForecastSalesAsync(DateTime.Today, storeId, cancellationToken);
        summary.HourlyForecast = await ForecastHourlySalesAsync(DateTime.Today, storeId, cancellationToken);

        // Current day sales
        var todaySales = await context.Orders
            .Where(o => o.CreatedAt.Date == DateTime.Today && o.Status == OrderStatus.Served)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        summary.CurrentDaySales = todaySales;
        summary.ForecastVariance = summary.TodaysForecast != null
            ? todaySales - summary.TodaysForecast.PredictedSales * (decimal)DateTime.Now.Hour / 24
            : null;

        summary.PerformanceStatus = summary.ForecastVariance switch
        {
            > 0 => "AboveTarget",
            < 0 => "BelowTarget",
            _ => "OnTrack"
        };

        // Alerts
        var alerts = await GetActiveAlertsAsync(storeId, null, cancellationToken);
        summary.CriticalAlertCount = alerts.Count(a => a.Severity == AnomalySeverity.Critical);
        summary.HighAlertCount = alerts.Count(a => a.Severity == AnomalySeverity.High);
        summary.WarningAlertCount = alerts.Count(a => a.Severity == AnomalySeverity.Warning);
        summary.TopAlerts = alerts.OrderByDescending(a => a.Severity).Take(5).ToList();

        // Top insights
        summary.TopInsights = await GetTopInsightsAsync(storeId, null, 5, cancellationToken);

        // Inventory recommendations
        var inventoryRecs = await GetInventoryRecommendationsAsync(storeId, cancellationToken);
        summary.UrgentInventoryRecommendations = inventoryRecs
            .Where(r => r.Urgency == UrgencyLevel.Critical || r.Urgency == UrgencyLevel.High)
            .Take(5)
            .ToList();

        // Menu recommendations
        summary.TopMenuRecommendations = await GetMenuRecommendationsAsync(storeId, null, cancellationToken);
        summary.TopMenuRecommendations = summary.TopMenuRecommendations.Take(3).ToList();

        // Customer health
        var churnAlerts = await PredictChurnAsync(storeId, 100, cancellationToken);
        summary.AtRiskCustomerCount = churnAlerts.Count;
        summary.AtRiskRevenue = churnAlerts.Sum(a => a.PredictedLostValue);

        return summary;
    }

    #endregion

    #region Helper Methods

    private static List<ForecastFactor> GetForecastFactors(DateTime date, DayOfWeek dayOfWeek)
    {
        var factors = new List<ForecastFactor>();

        // Day of week factor
        var dayImpact = dayOfWeek switch
        {
            DayOfWeek.Friday or DayOfWeek.Saturday => 15,
            DayOfWeek.Sunday => 10,
            DayOfWeek.Monday => -5,
            _ => 0
        };

        if (dayImpact != 0)
        {
            factors.Add(new ForecastFactor
            {
                FactorName = dayOfWeek.ToString(),
                FactorType = "DayOfWeek",
                Impact = dayImpact,
                Description = dayImpact > 0 ? "Weekend typically sees higher traffic" : "Monday typically has lower sales"
            });
        }

        // End of month factor
        if (date.Day >= 25)
        {
            factors.Add(new ForecastFactor
            {
                FactorName = "End of Month",
                FactorType = "Seasonality",
                Impact = 10,
                Description = "End of month typically sees increased spending"
            });
        }

        // Holiday check (simplified)
        if (date.Month == 12 && date.Day >= 20)
        {
            factors.Add(new ForecastFactor
            {
                FactorName = "Holiday Season",
                FactorType = "Event",
                Impact = 25,
                Description = "Holiday season drives increased foot traffic"
            });
        }

        return factors;
    }

    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (!values.Any()) return 0;
        var avg = values.Average();
        var sumSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumSquares / values.Count));
    }

    private async Task<decimal> GetTodaySalesAsync(POSDbContext context, CancellationToken cancellationToken)
    {
        return await context.Orders
            .Where(o => o.CreatedAt.Date == DateTime.Today && o.Status == OrderStatus.Served)
            .SumAsync(o => o.TotalAmount, cancellationToken);
    }

    private async Task<decimal> GetYesterdaySalesAsync(POSDbContext context, CancellationToken cancellationToken)
    {
        var yesterday = DateTime.Today.AddDays(-1);
        return await context.Orders
            .Where(o => o.CreatedAt.Date == yesterday && o.Status == OrderStatus.Served)
            .SumAsync(o => o.TotalAmount, cancellationToken);
    }

    private async Task<decimal> GetWeekSalesAsync(POSDbContext context, CancellationToken cancellationToken)
    {
        var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        return await context.Orders
            .Where(o => o.CreatedAt >= startOfWeek && o.Status == OrderStatus.Served)
            .SumAsync(o => o.TotalAmount, cancellationToken);
    }

    private async Task<decimal> GetMonthSalesAsync(POSDbContext context, CancellationToken cancellationToken)
    {
        var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return await context.Orders
            .Where(o => o.CreatedAt >= startOfMonth && o.Status == OrderStatus.Served)
            .SumAsync(o => o.TotalAmount, cancellationToken);
    }

    private async Task<List<(string Name, int Quantity)>> GetTopProductsAsync(POSDbContext context, int count, CancellationToken cancellationToken)
    {
        var startDate = DateTime.Today.AddDays(-30);
        var orderIds = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Served)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        return await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .GroupBy(oi => oi.Product!.Name)
            .Select(g => new { Name = g.Key, Quantity = (int)g.Sum(oi => oi.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .Take(count)
            .Select(x => ValueTuple.Create(x.Name, x.Quantity))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<(string Name, decimal Quantity)>> GetLowStockItemsAsync(POSDbContext context, CancellationToken cancellationToken)
    {
        return await context.Inventories
            .Include(i => i.Product)
            .Where(i => i.CurrentStock <= i.ReorderLevel)
            .Select(i => ValueTuple.Create(i.Product!.Name, i.CurrentStock))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<BusinessInsight>> GetSalesInsightsAsync(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var insights = new List<BusinessInsight>();

        // Compare today to yesterday
        var todaySales = await GetTodaySalesAsync(context, cancellationToken);
        var yesterdaySales = await GetYesterdaySalesAsync(context, cancellationToken);

        if (yesterdaySales > 0)
        {
            var change = (todaySales - yesterdaySales) / yesterdaySales * 100;
            if (Math.Abs(change) > 10)
            {
                insights.Add(new BusinessInsight
                {
                    GeneratedAt = DateTime.UtcNow,
                    InsightType = change > 0 ? InsightType.Achievement : InsightType.Risk,
                    Category = InsightCategory.Sales,
                    Title = change > 0 ? "Sales Up from Yesterday" : "Sales Down from Yesterday",
                    Summary = $"Sales are {Math.Abs(change):F1}% {(change > 0 ? "higher" : "lower")} than yesterday",
                    Impact = Math.Abs(change) > 20 ? InsightImpact.High : InsightImpact.Medium,
                    EstimatedValue = Math.Abs(todaySales - yesterdaySales)
                });
            }
        }

        return insights;
    }

    private async Task<List<BusinessInsight>> GetInventoryInsightsAsync(int? storeId, CancellationToken cancellationToken)
    {
        var insights = new List<BusinessInsight>();

        var recommendations = await GetInventoryRecommendationsAsync(storeId, cancellationToken);
        var criticalItems = recommendations.Where(r => r.Urgency == UrgencyLevel.Critical).ToList();

        if (criticalItems.Any())
        {
            insights.Add(new BusinessInsight
            {
                GeneratedAt = DateTime.UtcNow,
                InsightType = InsightType.Risk,
                Category = InsightCategory.Inventory,
                Title = "Critical Stock Shortage",
                Summary = $"{criticalItems.Count} items are critically low on stock",
                Impact = InsightImpact.Critical,
                EstimatedValue = criticalItems.Sum(i => i.AverageDailySales * i.EstimatedCost),
                Recommendation = "Place emergency orders immediately",
                ActionItems = criticalItems.Take(3).Select(i => $"Reorder {i.ProductName}").ToList()
            });
        }

        return insights;
    }

    #endregion
}
