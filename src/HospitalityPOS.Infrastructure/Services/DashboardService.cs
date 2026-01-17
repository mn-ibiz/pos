using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Dashboard;
using HospitalityPOS.Infrastructure.Data;
using SyncQueueItemStatus = HospitalityPOS.Core.Entities.SyncQueueItemStatus;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for retrieving real-time dashboard data.
/// Provides optimized queries for live dashboard updates.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public DashboardService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TodaySalesSummaryDto> GetTodaySalesSummaryAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting today's sales summary for store {StoreId}", storeId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= today && r.SettledAt < tomorrow);

        // Apply store filter if specified (multi-branch support)
        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var receipts = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var summary = new TodaySalesSummaryDto
        {
            TotalSales = receipts.Sum(r => r.TotalAmount),
            TransactionCount = receipts.Count,
            GrossSales = receipts.Sum(r => r.Subtotal + r.DiscountAmount),
            TotalDiscounts = receipts.Sum(r => r.DiscountAmount),
            TaxCollected = receipts.Sum(r => r.TaxAmount),
            AsOf = DateTime.UtcNow
        };

        summary.AverageTicket = summary.TransactionCount > 0
            ? summary.TotalSales / summary.TransactionCount
            : 0;

        // Get items sold count
        var itemsQuery = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= today && ri.Receipt.SettledAt < tomorrow);

        // Apply store filter for items
        if (storeId.HasValue)
        {
            itemsQuery = itemsQuery.Where(ri => ri.Receipt.StoreId == storeId.Value);
        }

        summary.ItemsSold = (int)await itemsQuery.SumAsync(ri => ri.Quantity, cancellationToken).ConfigureAwait(false);

        _logger.Debug("Today's sales: {TotalSales:C}, {TransactionCount} transactions",
            summary.TotalSales, summary.TransactionCount);

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<HourlySalesDto>> GetHourlySalesBreakdownAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting hourly sales breakdown for store {StoreId}", storeId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var currentHour = DateTime.UtcNow.Hour;

        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= today && r.SettledAt < tomorrow);

        // Apply store filter if specified
        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        var receipts = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Group by hour
        var hourlyData = receipts
            .GroupBy(r => r.SettledAt!.Value.Hour)
            .ToDictionary(g => g.Key, g => new
            {
                Sales = g.Sum(r => r.TotalAmount),
                Count = g.Count()
            });

        // Create entries for all 24 hours
        var result = Enumerable.Range(0, 24)
            .Select(hour => new HourlySalesDto
            {
                Hour = hour,
                HourLabel = FormatHourLabel(hour),
                Sales = hourlyData.TryGetValue(hour, out var data) ? data.Sales : 0,
                TransactionCount = hourlyData.TryGetValue(hour, out var dataCount) ? dataCount.Count : 0,
                IsCurrentHour = hour == currentHour
            })
            .ToList();

        return result;
    }

    /// <inheritdoc />
    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count = 10,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting top {Count} selling products for store {StoreId}", count, storeId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
                .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= today && ri.Receipt.SettledAt < tomorrow);

        // Apply store filter if specified
        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId.Value);
        }

        var topProducts = await query
            .GroupBy(ri => new
            {
                ri.ProductId,
                ri.Product.Name,
                ri.Product.Code,
                CategoryName = ri.Product.Category != null ? ri.Product.Category.Name : "Uncategorized"
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Code,
                g.Key.CategoryName,
                QuantitySold = g.Sum(ri => ri.Quantity),
                Revenue = g.Sum(ri => ri.TotalAmount)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return topProducts
            .Select((p, index) => new TopSellingProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.Name,
                ProductCode = p.Code,
                CategoryName = p.CategoryName,
                QuantitySold = p.QuantitySold,
                Revenue = p.Revenue,
                Rank = index + 1
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdownAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting payment method breakdown for store {StoreId}", storeId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.Payments
            .AsNoTracking()
            .Include(p => p.Receipt)
            .Include(p => p.PaymentMethod)
            .Where(p => p.Receipt.Status == ReceiptStatus.Settled)
            .Where(p => p.PaidAt >= today && p.PaidAt < tomorrow);

        // Apply store filter if specified
        if (storeId.HasValue)
        {
            query = query.Where(p => p.Receipt.StoreId == storeId.Value);
        }

        var breakdown = await query
            .GroupBy(p => new
            {
                p.PaymentMethodId,
                p.PaymentMethod.Name
            })
            .Select(g => new
            {
                g.Key.PaymentMethodId,
                g.Key.Name,
                Amount = g.Sum(p => p.Amount),
                Count = g.Count()
            })
            .OrderByDescending(p => p.Amount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalAmount = breakdown.Sum(b => b.Amount);

        // Define colors for common payment methods
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cash", "#4CAF50" },
            { "M-Pesa", "#8BC34A" },
            { "MPESA", "#8BC34A" },
            { "Card", "#2196F3" },
            { "Credit Card", "#2196F3" },
            { "Debit Card", "#03A9F4" },
            { "Airtel Money", "#F44336" },
            { "Room Charge", "#9C27B0" },
            { "Credit", "#FF9800" }
        };

        var defaultColors = new[] { "#607D8B", "#795548", "#9E9E9E", "#CDDC39" };

        return breakdown
            .Select((b, index) => new PaymentMethodBreakdownDto
            {
                PaymentMethodId = b.PaymentMethodId,
                PaymentMethodName = b.Name,
                Amount = b.Amount,
                TransactionCount = b.Count,
                Percentage = totalAmount > 0 ? Math.Round(b.Amount / totalAmount * 100, 1) : 0,
                ColorCode = colors.TryGetValue(b.Name, out var color)
                    ? color
                    : defaultColors[index % defaultColors.Length]
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ComparisonMetricsDto> GetComparisonMetricsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting comparison metrics for store {StoreId}", storeId);

        var now = DateTime.UtcNow;
        var today = now.Date;
        var currentTime = now.TimeOfDay;

        // Today's data up to current time
        var todaySales = await GetSalesUpToTimeAsync(today, currentTime, cancellationToken);

        // Yesterday's data up to same time
        var yesterday = today.AddDays(-1);
        var yesterdaySales = await GetSalesUpToTimeAsync(yesterday, currentTime, cancellationToken);

        // Last week same day up to same time
        var lastWeekSameDay = today.AddDays(-7);
        var lastWeekSales = await GetSalesUpToTimeAsync(lastWeekSameDay, currentTime, cancellationToken);

        return new ComparisonMetricsDto
        {
            YesterdaySales = yesterdaySales.TotalSales,
            VsYesterdayPercent = CalculatePercentChange(todaySales.TotalSales, yesterdaySales.TotalSales),
            IsBetterThanYesterday = todaySales.TotalSales >= yesterdaySales.TotalSales,

            LastWeekSameDaySales = lastWeekSales.TotalSales,
            VsLastWeekPercent = CalculatePercentChange(todaySales.TotalSales, lastWeekSales.TotalSales),
            IsBetterThanLastWeek = todaySales.TotalSales >= lastWeekSales.TotalSales,

            YesterdayTransactionCount = yesterdaySales.TransactionCount,
            TransactionVsYesterdayPercent = CalculatePercentChange(todaySales.TransactionCount, yesterdaySales.TransactionCount),

            YesterdayAverageTicket = yesterdaySales.AverageTicket,
            AvgTicketVsYesterdayPercent = CalculatePercentChange(todaySales.AverageTicket, yesterdaySales.AverageTicket)
        };
    }

    /// <inheritdoc />
    public async Task<LowStockAlertDto> GetLowStockAlertsAsync(
        int? storeId = null,
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting low stock alerts for store {StoreId}", storeId);

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Inventory)
            .Where(p => p.IsActive)
            .Where(p => p.TrackInventory)
            .Where(p => p.Inventory != null);

        var products = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var lowStockProducts = products
            .Where(p => p.Inventory!.CurrentStock <= p.MinimumStockLevel)
            .OrderBy(p => p.Inventory!.CurrentStock)
            .ToList();

        var outOfStockCount = lowStockProducts.Count(p => p.Inventory!.CurrentStock <= 0);

        return new LowStockAlertDto
        {
            TotalCount = lowStockProducts.Count,
            OutOfStockCount = outOfStockCount,
            Items = lowStockProducts
                .Take(maxItems)
                .Select(p => new LowStockItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductCode = p.Code,
                    CurrentStock = p.Inventory!.CurrentStock,
                    MinimumStock = p.MinimumStockLevel,
                    IsOutOfStock = p.Inventory.CurrentStock <= 0
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<ExpiryAlertDto> GetExpiryAlertsAsync(
        int? storeId = null,
        int daysThreshold = 30,
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting expiry alerts for store {StoreId}, threshold {Days} days", storeId, daysThreshold);

        // Check if batch tracking is enabled by checking if there are any batch configurations
        var hasBatchTracking = await _context.ProductBatchConfigurations
            .AsNoTracking()
            .AnyAsync(c => c.RequiresBatchTracking, cancellationToken)
            .ConfigureAwait(false);

        if (!hasBatchTracking)
        {
            return new ExpiryAlertDto
            {
                TotalCount = 0,
                ExpiredCount = 0,
                IsBatchTrackingEnabled = false,
                Items = []
            };
        }

        var today = DateTime.UtcNow.Date;
        var thresholdDate = today.AddDays(daysThreshold);

        var query = _context.ProductBatches
            .AsNoTracking()
            .Include(b => b.Product)
            .Where(b => b.CurrentQuantity > 0)
            .Where(b => b.ExpiryDate.HasValue)
            .Where(b => b.ExpiryDate <= thresholdDate);

        if (storeId.HasValue)
        {
            query = query.Where(b => b.StoreId == storeId.Value);
        }

        var expiringBatches = await query
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var expiredCount = expiringBatches.Count(b => b.ExpiryDate < today);

        return new ExpiryAlertDto
        {
            TotalCount = expiringBatches.Count,
            ExpiredCount = expiredCount,
            IsBatchTrackingEnabled = true,
            Items = expiringBatches
                .Take(maxItems)
                .Select(b => new ExpiringItemDto
                {
                    BatchId = b.Id,
                    ProductName = b.Product.Name,
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate!.Value,
                    DaysUntilExpiry = (b.ExpiryDate!.Value - today).Days,
                    Quantity = b.CurrentQuantity,
                    IsExpired = b.ExpiryDate < today
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<SyncStatusDto> GetSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting sync status");

        // Check for pending sync items
        var pendingCount = await _context.SyncQueues
            .AsNoTracking()
            .CountAsync(s => s.Status == SyncQueueItemStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        // Get last successful sync
        var lastSync = await _context.SyncLogs
            .AsNoTracking()
            .Where(l => l.IsSuccess)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Check if any sync is in progress
        var isSyncing = await _context.SyncQueues
            .AsNoTracking()
            .AnyAsync(s => s.Status == SyncQueueItemStatus.InProgress, cancellationToken)
            .ConfigureAwait(false);

        return new SyncStatusDto
        {
            PendingCount = pendingCount,
            IsOnline = pendingCount == 0 && !isSyncing,
            LastSyncTime = lastSync?.Timestamp,
            IsSyncing = isSyncing
        };
    }

    /// <inheritdoc />
    public async Task<DashboardDataDto> GetDashboardDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Getting complete dashboard data for store {StoreId}", storeId);

        // Execute all queries in parallel for better performance
        var salesSummaryTask = GetTodaySalesSummaryAsync(storeId, cancellationToken);
        var hourlySalesTask = GetHourlySalesBreakdownAsync(storeId, cancellationToken);
        var topProductsTask = GetTopSellingProductsAsync(10, storeId, cancellationToken);
        var paymentBreakdownTask = GetPaymentMethodBreakdownAsync(storeId, cancellationToken);
        var comparisonTask = GetComparisonMetricsAsync(storeId, cancellationToken);
        var lowStockTask = GetLowStockAlertsAsync(storeId, 10, cancellationToken);
        var expiryTask = GetExpiryAlertsAsync(storeId, 30, 10, cancellationToken);
        var syncTask = GetSyncStatusAsync(cancellationToken);

        await Task.WhenAll(
            salesSummaryTask,
            hourlySalesTask,
            topProductsTask,
            paymentBreakdownTask,
            comparisonTask,
            lowStockTask,
            expiryTask,
            syncTask
        ).ConfigureAwait(false);

        return new DashboardDataDto
        {
            SalesSummary = await salesSummaryTask,
            HourlySales = await hourlySalesTask,
            TopProducts = await topProductsTask,
            PaymentBreakdown = await paymentBreakdownTask,
            Comparison = await comparisonTask,
            LowStockAlerts = await lowStockTask,
            ExpiryAlerts = await expiryTask,
            SyncStatus = await syncTask,
            RetrievedAt = DateTime.UtcNow,
            StoreId = storeId
        };
    }

    /// <inheritdoc />
    public async Task<List<BranchSummaryDto>> GetBranchSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Getting branch summaries");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);

        var stores = await _context.Stores
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<BranchSummaryDto>();

        foreach (var store in stores)
        {
            // Get today's sales for this store
            var todayReceipts = await _context.Receipts
                .AsNoTracking()
                .Where(r => r.Status == ReceiptStatus.Settled)
                .Where(r => r.StoreId == store.Id)
                .Where(r => r.SettledAt >= today && r.SettledAt < tomorrow)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var todaySales = todayReceipts.Sum(r => r.TotalAmount);
            var todayCount = todayReceipts.Count;

            // Get yesterday's sales for comparison
            var yesterdayReceipts = await _context.Receipts
                .AsNoTracking()
                .Where(r => r.Status == ReceiptStatus.Settled)
                .Where(r => r.StoreId == store.Id)
                .Where(r => r.SettledAt >= yesterday && r.SettledAt < today)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var yesterdaySales = yesterdayReceipts.Sum(r => r.TotalAmount);

            result.Add(new BranchSummaryDto
            {
                StoreId = store.Id,
                StoreName = store.Name,
                TodaySales = todaySales,
                TransactionCount = todayCount,
                VsYesterdayPercent = CalculatePercentChange(todaySales, yesterdaySales),
                IsOnline = true // Could be determined by connectivity check in future
            });
        }

        return result.OrderByDescending(b => b.TodaySales).ToList();
    }

    #region Private Helper Methods

    private async Task<(decimal TotalSales, int TransactionCount, decimal AverageTicket)> GetSalesUpToTimeAsync(
        DateTime date,
        TimeSpan upToTime,
        CancellationToken cancellationToken)
    {
        var startOfDay = date;
        var endTime = date.Add(upToTime);

        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startOfDay && r.SettledAt <= endTime);

        var receipts = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalSales = receipts.Sum(r => r.TotalAmount);
        var count = receipts.Count;
        var avgTicket = count > 0 ? totalSales / count : 0;

        return (totalSales, count, avgTicket);
    }

    private static decimal CalculatePercentChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }
        return Math.Round((current - previous) / previous * 100, 1);
    }

    private static string FormatHourLabel(int hour)
    {
        return hour switch
        {
            0 => "12 AM",
            12 => "12 PM",
            _ when hour < 12 => $"{hour} AM",
            _ => $"{hour - 12} PM"
        };
    }

    #endregion
}
