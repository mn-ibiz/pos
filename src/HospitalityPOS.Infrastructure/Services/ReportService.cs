using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating sales and business reports.
/// </summary>
public class ReportService : IReportService
{
    private readonly POSDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="sessionService">The session service.</param>
    /// <param name="logger">The logger.</param>
    public ReportService(
        POSDbContext context,
        ISessionService sessionService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DailySalesSummary> GenerateDailySummaryAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating daily sales summary for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var settledReceipts = await GetSettledReceiptsQuery(parameters)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var voidedReceipts = await GetVoidedReceiptsQuery(parameters)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summary = new DailySalesSummary
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GrossSales = settledReceipts.Sum(r => r.Subtotal + r.DiscountAmount),
            Discounts = settledReceipts.Sum(r => r.DiscountAmount),
            NetSales = settledReceipts.Sum(r => r.Subtotal),
            TaxCollected = settledReceipts.Sum(r => r.TaxAmount),
            TotalRevenue = settledReceipts.Sum(r => r.TotalAmount),
            TransactionCount = settledReceipts.Count,
            VoidedCount = voidedReceipts.Count,
            VoidedAmount = voidedReceipts.Sum(r => r.TotalAmount),
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        if (settledReceipts.Count > 0)
        {
            summary.AverageTransaction = summary.TotalRevenue / settledReceipts.Count;
            summary.LargestTransaction = settledReceipts.Max(r => r.TotalAmount);
            summary.SmallestTransaction = settledReceipts.Min(r => r.TotalAmount);
        }

        _logger.Information("Daily summary generated: {TransactionCount} transactions, {TotalRevenue:C} revenue",
            summary.TransactionCount, summary.TotalRevenue);

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<ProductSalesReport>> GenerateProductSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating product sales report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var query = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
                .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= parameters.StartDate)
            .Where(ri => ri.Receipt.SettledAt < parameters.EndDate);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(ri => ri.Product.CategoryId == parameters.CategoryId.Value);
        }

        var productSales = await query
            .GroupBy(ri => new
            {
                ri.ProductId,
                ri.Product.Code,
                ri.Product.Name,
                CategoryName = ri.Product.Category != null ? ri.Product.Category.Name : "Uncategorized",
                ri.Product.CostPrice
            })
            .Select(g => new ProductSalesReport
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.Code,
                ProductName = g.Key.Name,
                CategoryName = g.Key.CategoryName,
                QuantitySold = g.Sum(ri => ri.Quantity),
                GrossSales = g.Sum(ri => ri.Quantity * ri.UnitPrice),
                Discounts = g.Sum(ri => ri.DiscountAmount),
                NetSales = g.Sum(ri => ri.TotalAmount - ri.TaxAmount),
                CostOfGoodsSold = g.Sum(ri => ri.Quantity) * g.Key.CostPrice
            })
            .OrderByDescending(p => p.NetSales)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Calculate percentages and profit
        var totalNetSales = productSales.Sum(p => p.NetSales);
        foreach (var product in productSales)
        {
            product.Percentage = totalNetSales > 0
                ? Math.Round(product.NetSales / totalNetSales * 100, 2)
                : 0;
            product.GrossProfit = product.NetSales - product.CostOfGoodsSold;
        }

        _logger.Information("Product sales report generated: {ProductCount} products",
            productSales.Count);

        return productSales;
    }

    /// <inheritdoc />
    public async Task<List<CategorySalesReport>> GenerateCategorySalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating category sales report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var categorySales = await _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
                .ThenInclude(p => p.Category)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.Receipt.SettledAt >= parameters.StartDate)
            .Where(ri => ri.Receipt.SettledAt < parameters.EndDate)
            .GroupBy(ri => new
            {
                CategoryId = ri.Product.Category != null ? ri.Product.Category.Id : 0,
                CategoryName = ri.Product.Category != null ? ri.Product.Category.Name : "Uncategorized"
            })
            .Select(g => new CategorySalesReport
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                ItemCount = g.Select(ri => ri.ProductId).Distinct().Count(),
                QuantitySold = g.Sum(ri => ri.Quantity),
                GrossSales = g.Sum(ri => ri.Quantity * ri.UnitPrice),
                Discounts = g.Sum(ri => ri.DiscountAmount),
                NetSales = g.Sum(ri => ri.TotalAmount - ri.TaxAmount)
            })
            .OrderByDescending(c => c.NetSales)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Calculate percentages
        var totalNetSales = categorySales.Sum(c => c.NetSales);
        foreach (var category in categorySales)
        {
            category.Percentage = totalNetSales > 0
                ? Math.Round(category.NetSales / totalNetSales * 100, 2)
                : 0;
        }

        _logger.Information("Category sales report generated: {CategoryCount} categories",
            categorySales.Count);

        return categorySales;
    }

    /// <inheritdoc />
    public async Task<List<CashierSalesReport>> GenerateCashierSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating cashier sales report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        // Get settled receipts grouped by owner
        var settledByOwner = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= parameters.StartDate)
            .Where(r => r.SettledAt < parameters.EndDate)
            .GroupBy(r => new { r.OwnerId, r.Owner.FullName })
            .Select(g => new
            {
                UserId = g.Key.OwnerId,
                CashierName = g.Key.FullName,
                TransactionCount = g.Count(),
                TotalSales = g.Sum(r => r.TotalAmount),
                TotalDiscounts = g.Sum(r => r.DiscountAmount)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get voids by owner
        var voidsByOwner = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Voided)
            .Where(r => r.VoidedAt >= parameters.StartDate)
            .Where(r => r.VoidedAt < parameters.EndDate)
            .GroupBy(r => r.OwnerId)
            .Select(g => new
            {
                OwnerId = g.Key,
                VoidCount = g.Count(),
                VoidAmount = g.Sum(r => r.TotalAmount)
            })
            .ToDictionaryAsync(g => g.OwnerId, cancellationToken)
            .ConfigureAwait(false);

        var cashierSales = settledByOwner.Select(s =>
        {
            var voidInfo = voidsByOwner.GetValueOrDefault(s.UserId);
            return new CashierSalesReport
            {
                UserId = s.UserId,
                CashierName = s.CashierName,
                TransactionCount = s.TransactionCount,
                TotalSales = s.TotalSales,
                AverageTransaction = s.TransactionCount > 0 ? s.TotalSales / s.TransactionCount : 0,
                TotalDiscounts = s.TotalDiscounts,
                VoidCount = voidInfo?.VoidCount ?? 0,
                VoidAmount = voidInfo?.VoidAmount ?? 0
            };
        })
        .OrderByDescending(c => c.TotalSales)
        .ToList();

        _logger.Information("Cashier sales report generated: {CashierCount} cashiers",
            cashierSales.Count);

        return cashierSales;
    }

    /// <inheritdoc />
    public async Task<List<PaymentMethodSalesReport>> GeneratePaymentMethodSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating payment method sales report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var paymentMethodSales = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Receipt)
            .Include(p => p.PaymentMethod)
            .Where(p => p.Receipt.Status == ReceiptStatus.Settled)
            .Where(p => p.Receipt.SettledAt >= parameters.StartDate)
            .Where(p => p.Receipt.SettledAt < parameters.EndDate)
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod.Name })
            .Select(g => new PaymentMethodSalesReport
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(pm => pm.TotalAmount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Calculate percentages
        var totalAmount = paymentMethodSales.Sum(pm => pm.TotalAmount);
        foreach (var pm in paymentMethodSales)
        {
            pm.Percentage = totalAmount > 0
                ? Math.Round(pm.TotalAmount / totalAmount * 100, 2)
                : 0;
        }

        _logger.Information("Payment method sales report generated: {MethodCount} methods",
            paymentMethodSales.Count);

        return paymentMethodSales;
    }

    /// <inheritdoc />
    public async Task<List<HourlySalesReport>> GenerateHourlySalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating hourly sales report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var receipts = await GetSettledReceiptsQuery(parameters)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hourlySales = receipts
            .GroupBy(r => r.SettledAt!.Value.Hour)
            .Select(g => new HourlySalesReport
            {
                Hour = g.Key,
                HourDisplay = $"{g.Key:D2}:00 - {(g.Key + 1) % 24:D2}:00",
                TransactionCount = g.Count(),
                TotalSales = g.Sum(r => r.TotalAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0
            })
            .OrderBy(h => h.Hour)
            .ToList();

        // Calculate percentages
        var totalSales = hourlySales.Sum(h => h.TotalSales);
        foreach (var hourly in hourlySales)
        {
            hourly.Percentage = totalSales > 0
                ? Math.Round(hourly.TotalSales / totalSales * 100, 2)
                : 0;
        }

        _logger.Information("Hourly sales report generated: {HourCount} hours with sales",
            hourlySales.Count);

        return hourlySales;
    }

    /// <inheritdoc />
    public async Task<SalesReportResult> GenerateSalesReportAsync(
        SalesReportType reportType,
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating {ReportType} report for {StartDate} to {EndDate}",
            reportType, parameters.StartDate, parameters.EndDate);

        var result = new SalesReportResult
        {
            Parameters = parameters,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        // Always include summary
        result.Summary = await GenerateDailySummaryAsync(parameters, cancellationToken)
            .ConfigureAwait(false);

        // Populate specific report based on type
        switch (reportType)
        {
            case SalesReportType.DailySummary:
                // Summary already populated
                break;

            case SalesReportType.ByProduct:
                result.ProductSales = await GenerateProductSalesAsync(parameters, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SalesReportType.ByCategory:
                result.CategorySales = await GenerateCategorySalesAsync(parameters, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SalesReportType.ByCashier:
                result.CashierSales = await GenerateCashierSalesAsync(parameters, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SalesReportType.ByPaymentMethod:
                result.PaymentMethodSales = await GeneratePaymentMethodSalesAsync(parameters, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case SalesReportType.HourlySales:
                result.HourlySales = await GenerateHourlySalesAsync(parameters, cancellationToken)
                    .ConfigureAwait(false);
                break;

            default:
                _logger.Warning("Unknown report type: {ReportType}", reportType);
                break;
        }

        _logger.Information("{ReportType} report generated successfully", reportType);

        return result;
    }

    /// <inheritdoc />
    public async Task<VoidReportResult> GenerateVoidReportAsync(
        ExceptionReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating void report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var query = _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.VoidedBy)
            .Include(r => r.VoidAuthorizedBy)
            .Include(r => r.VoidReason)
            .Where(r => r.Status == ReceiptStatus.Voided)
            .Where(r => r.VoidedAt >= parameters.StartDate)
            .Where(r => r.VoidedAt < parameters.EndDate);

        if (parameters.UserId.HasValue)
        {
            query = query.Where(r => r.VoidedById == parameters.UserId.Value);
        }

        if (parameters.VoidReasonId.HasValue)
        {
            query = query.Where(r => r.VoidReasonId == parameters.VoidReasonId.Value);
        }

        if (parameters.WorkPeriodId.HasValue)
        {
            query = query.Where(r => r.WorkPeriodId == parameters.WorkPeriodId.Value);
        }

        var voidedReceipts = await query
            .OrderByDescending(r => r.VoidedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = voidedReceipts.Select(r => new VoidReportItem
        {
            ReceiptId = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            VoidedAmount = r.TotalAmount,
            VoidedAt = r.VoidedAt ?? DateTime.MinValue,
            VoidedByUserId = r.VoidedById ?? 0,
            VoidedBy = r.VoidedBy?.FullName ?? "Unknown",
            AuthorizedByUserId = r.VoidAuthorizedById,
            AuthorizedBy = r.VoidAuthorizedBy != null
                ? r.VoidAuthorizedBy.FullName
                : (r.VoidedById == r.VoidAuthorizedById ? "(Self)" : "Unknown"),
            VoidReasonId = r.VoidReasonId,
            Reason = r.VoidReason?.Name ?? "Unknown",
            Notes = r.VoidNotes
        }).ToList();

        // Group by reason
        var byReason = items
            .GroupBy(v => v.Reason)
            .Select(g => new VoidByReasonSummary
            {
                ReasonName = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(v => v.VoidedAmount)
            })
            .ToList();

        var totalAmount = byReason.Sum(r => r.TotalAmount);
        foreach (var reason in byReason)
        {
            reason.Percentage = totalAmount > 0
                ? Math.Round(reason.TotalAmount / totalAmount * 100, 2)
                : 0;
        }

        var result = new VoidReportResult
        {
            Parameters = parameters,
            Items = items,
            ByReason = byReason,
            TotalCount = items.Count,
            TotalAmount = totalAmount,
            AverageAmount = items.Count > 0 ? totalAmount / items.Count : 0,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Void report generated: {VoidCount} voids, {TotalAmount:C} total",
            result.TotalCount, result.TotalAmount);

        return result;
    }

    /// <inheritdoc />
    public async Task<DiscountReportResult> GenerateDiscountReportAsync(
        ExceptionReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating discount report for {StartDate} to {EndDate}",
            parameters.StartDate, parameters.EndDate);

        var items = new List<DiscountReportItem>();

        // Get receipt-level discounts
        var receiptDiscountQuery = _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.DiscountAmount > 0)
            .Where(r => r.SettledAt >= parameters.StartDate)
            .Where(r => r.SettledAt < parameters.EndDate);

        if (parameters.UserId.HasValue)
        {
            receiptDiscountQuery = receiptDiscountQuery.Where(r => r.OwnerId == parameters.UserId.Value);
        }

        if (parameters.WorkPeriodId.HasValue)
        {
            receiptDiscountQuery = receiptDiscountQuery.Where(r => r.WorkPeriodId == parameters.WorkPeriodId.Value);
        }

        var receiptDiscounts = await receiptDiscountQuery
            .Select(r => new DiscountReportItem
            {
                ReceiptId = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                ItemDescription = "Order Discount",
                OriginalAmount = r.Subtotal + r.DiscountAmount,
                DiscountAmount = r.DiscountAmount,
                DiscountType = "Order",
                AppliedByUserId = r.OwnerId,
                AppliedBy = r.Owner.FullName,
                AppliedAt = r.SettledAt ?? r.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        items.AddRange(receiptDiscounts);

        // Get item-level discounts
        var itemDiscountQuery = _context.ReceiptItems
            .AsNoTracking()
            .Include(ri => ri.Receipt)
                .ThenInclude(r => r.Owner)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.Status == ReceiptStatus.Settled)
            .Where(ri => ri.DiscountAmount > 0)
            .Where(ri => ri.Receipt.SettledAt >= parameters.StartDate)
            .Where(ri => ri.Receipt.SettledAt < parameters.EndDate);

        if (parameters.UserId.HasValue)
        {
            itemDiscountQuery = itemDiscountQuery.Where(ri => ri.Receipt.OwnerId == parameters.UserId.Value);
        }

        if (parameters.WorkPeriodId.HasValue)
        {
            itemDiscountQuery = itemDiscountQuery.Where(ri => ri.Receipt.WorkPeriodId == parameters.WorkPeriodId.Value);
        }

        var itemDiscounts = await itemDiscountQuery
            .Select(ri => new DiscountReportItem
            {
                ReceiptId = ri.ReceiptId,
                ReceiptNumber = ri.Receipt.ReceiptNumber,
                ItemDescription = $"{ri.Product.Name} x{ri.Quantity}",
                OriginalAmount = ri.Quantity * ri.UnitPrice,
                DiscountAmount = ri.DiscountAmount,
                DiscountType = "Item",
                AppliedByUserId = ri.Receipt.OwnerId,
                AppliedBy = ri.Receipt.Owner.FullName,
                AppliedAt = ri.Receipt.SettledAt ?? ri.Receipt.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        items.AddRange(itemDiscounts);

        // Sort by timestamp descending
        items = items.OrderByDescending(d => d.AppliedAt).ToList();

        // Group by type
        var byType = items
            .GroupBy(d => d.DiscountType)
            .Select(g => new DiscountByTypeSummary
            {
                DiscountType = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(d => d.DiscountAmount)
            })
            .ToList();

        // Group by user
        var byUser = items
            .GroupBy(d => new { d.AppliedByUserId, d.AppliedBy })
            .Select(g => new DiscountByUserSummary
            {
                UserId = g.Key.AppliedByUserId,
                UserName = g.Key.AppliedBy,
                Count = g.Count(),
                TotalAmount = g.Sum(d => d.DiscountAmount)
            })
            .OrderByDescending(u => u.TotalAmount)
            .ToList();

        // Calculate percentages
        var totalDiscounts = items.Sum(d => d.DiscountAmount);
        foreach (var type in byType)
        {
            type.Percentage = totalDiscounts > 0
                ? Math.Round(type.TotalAmount / totalDiscounts * 100, 2)
                : 0;
        }
        foreach (var user in byUser)
        {
            user.Percentage = totalDiscounts > 0
                ? Math.Round(user.TotalAmount / totalDiscounts * 100, 2)
                : 0;
        }

        // Get total sales for discount rate calculation
        var totalSales = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= parameters.StartDate)
            .Where(r => r.SettledAt < parameters.EndDate)
            .SumAsync(r => r.Subtotal, cancellationToken)
            .ConfigureAwait(false);

        var result = new DiscountReportResult
        {
            Parameters = parameters,
            Items = items,
            ByType = byType,
            ByUser = byUser,
            TotalDiscounts = totalDiscounts,
            DiscountTransactionCount = items.Select(d => d.ReceiptNumber).Distinct().Count(),
            AverageDiscount = items.Count > 0 ? totalDiscounts / items.Count : 0,
            DiscountRate = totalSales > 0 ? Math.Round(totalDiscounts / totalSales * 100, 2) : 0,
            TotalSales = totalSales,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Discount report generated: {DiscountCount} discounts, {TotalDiscounts:C} total ({DiscountRate}% of sales)",
            items.Count, result.TotalDiscounts, result.DiscountRate);

        return result;
    }

    /// <inheritdoc />
    public async Task<CurrentStockReportResult> GenerateCurrentStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating current stock report");

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Category != null ? p.Category.Name : "ZZZ")
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<CurrentStockItem>();
        var inStockCount = 0;
        var outOfStockCount = 0;
        var lowStockCount = 0;
        decimal totalStockValue = 0;
        decimal totalRetailValue = 0;

        foreach (var product in products)
        {
            var currentStock = product.Inventory?.CurrentStock ?? 0;
            var costPrice = product.CostPrice ?? 0;
            var sellingPrice = product.SellingPrice;

            // Skip out of stock items if not including them
            if (!parameters.IncludeOutOfStock && currentStock <= 0)
            {
                continue;
            }

            // Determine status
            string status;
            if (currentStock <= 0)
            {
                status = "OUT";
                outOfStockCount++;
            }
            else if (product.MinStockLevel.HasValue && currentStock <= product.MinStockLevel.Value)
            {
                status = "LOW";
                lowStockCount++;
                inStockCount++;
            }
            else
            {
                status = "OK";
                inStockCount++;
            }

            var stockValue = currentStock * costPrice;
            var retailValue = currentStock * sellingPrice;
            totalStockValue += stockValue;
            totalRetailValue += retailValue;

            items.Add(new CurrentStockItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                CategoryName = product.Category?.Name ?? "Uncategorized",
                CurrentStock = currentStock,
                StockUnit = product.UnitOfMeasure,
                CostPrice = costPrice,
                SellingPrice = sellingPrice,
                StockValue = stockValue,
                RetailValue = retailValue,
                MinStock = product.MinStockLevel ?? 0,
                MaxStock = product.MaxStockLevel ?? 0,
                Status = status
            });
        }

        var result = new CurrentStockReportResult
        {
            Parameters = parameters,
            Items = items,
            TotalSkuCount = items.Count,
            ItemsInStock = inStockCount,
            OutOfStockCount = outOfStockCount,
            LowStockCount = lowStockCount,
            TotalStockValue = totalStockValue,
            TotalRetailValue = totalRetailValue,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Current stock report generated: {TotalSku} SKUs, {InStock} in stock, {OutOfStock} out of stock",
            result.TotalSkuCount, result.ItemsInStock, result.OutOfStockCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<LowStockReportResult> GenerateLowStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating low stock report");

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive)
            .Where(p => p.TrackInventory)
            .Where(p => p.MinStockLevel.HasValue)
            .Where(p => p.Inventory != null && p.Inventory.CurrentStock <= p.MinStockLevel);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Inventory!.CurrentStock)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<LowStockItem>();
        var criticalCount = 0;
        var lowCount = 0;
        decimal totalReorderValue = 0;

        foreach (var product in products)
        {
            var currentStock = product.Inventory?.CurrentStock ?? 0;
            var minStock = product.MinStockLevel ?? 0;
            var maxStock = product.MaxStockLevel ?? minStock * 2;
            var costPrice = product.CostPrice ?? 0;

            var isCritical = currentStock <= 0;
            if (isCritical)
            {
                criticalCount++;
            }
            else
            {
                lowCount++;
            }

            // Calculate reorder quantity to reach max stock level
            var reorderQty = Math.Max(0, maxStock - currentStock);
            var reorderValue = reorderQty * costPrice;
            totalReorderValue += reorderValue;

            items.Add(new LowStockItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                CategoryName = product.Category?.Name ?? "Uncategorized",
                CurrentStock = currentStock,
                MinStock = minStock,
                MaxStock = maxStock,
                ReorderQty = reorderQty,
                CostPrice = costPrice,
                ReorderValue = reorderValue,
                Status = isCritical ? "CRITICAL" : "LOW"
            });
        }

        var result = new LowStockReportResult
        {
            Parameters = parameters,
            Items = items,
            CriticalCount = criticalCount,
            LowStockCount = lowCount,
            TotalReorderValue = totalReorderValue,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Low stock report generated: {CriticalCount} critical, {LowCount} low stock items",
            result.CriticalCount, result.LowStockCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<StockMovementReportResult> GenerateStockMovementReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating stock movement report for {StartDate} to {EndDate}, Page {PageNumber}",
            parameters.StartDate, parameters.EndDate, parameters.PageNumber);

        var query = _context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .Where(sm => sm.CreatedAt >= parameters.StartDate)
            .Where(sm => sm.CreatedAt < parameters.EndDate);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(sm => sm.Product.CategoryId == parameters.CategoryId.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Calculate totals from the full query (not paginated) using aggregation
        var totalsQuery = query.GroupBy(_ => 1).Select(g => new
        {
            TotalReceived = g.Where(sm =>
                sm.MovementType == MovementType.Purchase ||
                sm.MovementType == MovementType.PurchaseReceive ||
                sm.MovementType == MovementType.Return ||
                sm.MovementType == MovementType.Void).Sum(sm => sm.Quantity),
            TotalSold = g.Where(sm =>
                sm.MovementType == MovementType.Sale ||
                sm.MovementType == MovementType.Waste).Sum(sm => sm.Quantity),
            TotalAdjusted = g.Where(sm =>
                sm.MovementType == MovementType.Adjustment ||
                sm.MovementType == MovementType.StockTake).Sum(sm => sm.Quantity)
        });

        var totals = await totalsQuery.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        // Apply pagination or load all based on EnablePagination flag
        var movements = parameters.EnablePagination
            ? await query
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip(parameters.Skip)
                .Take(parameters.Take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await query
                .OrderByDescending(sm => sm.CreatedAt)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var items = movements.Select(sm => new StockMovementItem
        {
            MovementId = sm.Id,
            ProductId = sm.ProductId,
            ProductName = sm.Product.Name,
            Date = sm.CreatedAt,
            MovementType = GetMovementTypeDisplayName(sm.MovementType),
            Quantity = GetSignedQuantity(sm.MovementType, sm.Quantity),
            PreviousStock = sm.PreviousStock,
            NewStock = sm.NewStock,
            Reference = BuildMovementReference(sm),
            UserName = sm.User?.FullName ?? "System",
            Notes = sm.Notes ?? sm.Reason
        }).ToList();

        var totalReceived = totals?.TotalReceived ?? 0;
        var totalSold = totals?.TotalSold ?? 0;
        var totalAdjusted = totals?.TotalAdjusted ?? 0;

        var result = new StockMovementReportResult
        {
            Parameters = parameters,
            Items = items,
            TotalReceived = totalReceived,
            TotalSold = totalSold,
            TotalAdjusted = totalAdjusted,
            NetMovement = totalReceived - totalSold + totalAdjusted,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };

        _logger.Information("Stock movement report generated: {MovementCount} movements (page {Page}/{TotalPages}), Net: {NetMovement}",
            items.Count, result.PageNumber, result.TotalPages, result.NetMovement);

        return result;
    }

    /// <inheritdoc />
    public async Task<StockValuationReportResult> GenerateStockValuationReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating stock valuation report");

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive)
            .Where(p => p.TrackInventory)
            .Where(p => p.Inventory != null && p.Inventory.CurrentStock > 0);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);
        }

        var products = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Group by category
        var categoryGroups = products
            .GroupBy(p => new { CategoryId = p.CategoryId ?? 0, CategoryName = p.Category?.Name ?? "Uncategorized" })
            .Select(g =>
            {
                var costValue = g.Sum(p => (p.Inventory?.CurrentStock ?? 0) * (p.CostPrice ?? 0));
                var retailValue = g.Sum(p => (p.Inventory?.CurrentStock ?? 0) * p.SellingPrice);
                return new CategoryValuation
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ItemCount = g.Count(),
                    TotalUnits = g.Sum(p => p.Inventory?.CurrentStock ?? 0),
                    CostValue = costValue,
                    RetailValue = retailValue
                };
            })
            .OrderByDescending(c => c.CostValue)
            .ToList();

        var totalCostValue = categoryGroups.Sum(c => c.CostValue);
        var totalRetailValue = categoryGroups.Sum(c => c.RetailValue);

        var result = new StockValuationReportResult
        {
            Parameters = parameters,
            AsOfDate = DateTime.UtcNow,
            Categories = categoryGroups,
            TotalCostValue = totalCostValue,
            TotalRetailValue = totalRetailValue,
            PotentialProfit = totalRetailValue - totalCostValue,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Stock valuation report generated: {CategoryCount} categories, Total Cost: {TotalCost:C}, Retail: {TotalRetail:C}",
            categoryGroups.Count, result.TotalCostValue, result.TotalRetailValue);

        return result;
    }

    /// <inheritdoc />
    public async Task<DeadStockReportResult> GenerateDeadStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating dead stock report with threshold of {Days} days",
            parameters.DeadStockDaysThreshold);

        var cutoffDate = DateTime.UtcNow.AddDays(-parameters.DeadStockDaysThreshold);

        // Get all products with stock
        var productsQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive)
            .Where(p => p.TrackInventory)
            .Where(p => p.Inventory != null && p.Inventory.CurrentStock > 0);

        if (parameters.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == parameters.CategoryId.Value);
        }

        var products = await productsQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Get last movement date for each product
        var productIds = products.Select(p => p.Id).ToList();
        var lastMovements = await _context.StockMovements
            .AsNoTracking()
            .Where(sm => productIds.Contains(sm.ProductId))
            .GroupBy(sm => sm.ProductId)
            .Select(g => new { ProductId = g.Key, LastMovement = g.Max(sm => sm.CreatedAt) })
            .ToDictionaryAsync(x => x.ProductId, x => x.LastMovement, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<DeadStockItem>();
        decimal totalValue = 0;

        foreach (var product in products)
        {
            var lastMovementDate = lastMovements.GetValueOrDefault(product.Id);
            var currentStock = product.Inventory?.CurrentStock ?? 0;
            var costPrice = product.CostPrice ?? 0;

            // Check if last movement is before cutoff (or no movement at all)
            if (!lastMovementDate.HasValue || lastMovementDate.Value < cutoffDate)
            {
                var daysSinceMovement = lastMovementDate.HasValue
                    ? (int)(DateTime.UtcNow - lastMovementDate.Value).TotalDays
                    : int.MaxValue;

                var stockValue = currentStock * costPrice;
                totalValue += stockValue;

                items.Add(new DeadStockItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    CategoryName = product.Category?.Name ?? "Uncategorized",
                    CurrentStock = currentStock,
                    StockValue = stockValue,
                    LastMovementDate = lastMovementDate,
                    DaysSinceMovement = daysSinceMovement
                });
            }
        }

        // Sort by days since movement descending (oldest first)
        items = items.OrderByDescending(i => i.DaysSinceMovement).ToList();

        var result = new DeadStockReportResult
        {
            Parameters = parameters,
            Items = items,
            TotalCount = items.Count,
            TotalValue = totalValue,
            DaysThreshold = parameters.DeadStockDaysThreshold,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _sessionService.CurrentUserDisplayName
        };

        _logger.Information("Dead stock report generated: {Count} items, Total Value: {TotalValue:C}",
            result.TotalCount, result.TotalValue);

        return result;
    }

    /// <inheritdoc />
    public async Task<UserActivityReportResult> GenerateUserActivityReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating user activity report for {FromDate} to {ToDate}",
            parameters.FromDate, parameters.ToDate);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd);

        // Filter for authentication/login related actions
        query = query.Where(a =>
            a.Action == "Login" ||
            a.Action == "Logout" ||
            a.Action == "LoginFailed" ||
            a.Action == "PasswordChanged" ||
            a.Action == "PasswordReset");

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        if (!string.IsNullOrEmpty(parameters.Action))
        {
            query = query.Where(a => a.Action == parameters.Action);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(parameters.MaxRecords)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = logs.Select(a => new UserActivityItem
        {
            AuditLogId = a.Id,
            Timestamp = a.CreatedAt,
            UserId = a.UserId,
            UserName = a.User?.FullName ?? "System",
            Action = a.Action,
            ActionDisplayName = GetActionDisplayName(a.Action),
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            MachineName = a.MachineName,
            IpAddress = a.IpAddress
        }).ToList();

        var result = new UserActivityReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalActions = items.Count,
            LoginCount = items.Count(i => i.Action == "Login"),
            LogoutCount = items.Count(i => i.Action == "Logout"),
            FailedLoginCount = items.Count(i => i.Action == "LoginFailed"),
            Items = items
        };

        _logger.Information("User activity report generated: {Count} actions, {Logins} logins, {Logouts} logouts",
            result.TotalActions, result.LoginCount, result.LogoutCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<TransactionLogReportResult> GenerateTransactionLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating transaction log report for {FromDate} to {ToDate}",
            parameters.FromDate, parameters.ToDate);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd);

        // Filter for transaction-related actions
        query = query.Where(a =>
            a.EntityType == "Order" ||
            a.EntityType == "Receipt" ||
            a.EntityType == "Payment" ||
            a.Action.Contains("Order") ||
            a.Action.Contains("Receipt") ||
            a.Action.Contains("Payment") ||
            a.Action.Contains("Settled"));

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        if (!string.IsNullOrEmpty(parameters.Action))
        {
            query = query.Where(a => a.Action == parameters.Action);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(parameters.MaxRecords)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = logs.Select(a => new TransactionLogItem
        {
            AuditLogId = a.Id,
            Timestamp = a.CreatedAt,
            UserName = a.User?.FullName ?? "System",
            Action = a.Action,
            ActionDisplayName = GetActionDisplayName(a.Action),
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            Amount = TryExtractAmount(a.NewValues)
        }).ToList();

        var orderCount = items.Count(i => i.Action.Contains("Order") || i.EntityType == "Order");
        var settlementCount = items.Count(i => i.Action.Contains("Settled") || i.Action.Contains("Settlement"));

        var result = new TransactionLogReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalTransactions = items.Count,
            OrderCount = orderCount,
            SettlementCount = settlementCount,
            TotalSalesValue = items.Where(i => i.Amount.HasValue).Sum(i => i.Amount ?? 0),
            Items = items
        };

        _logger.Information("Transaction log report generated: {Count} transactions, {Orders} orders, {Settlements} settlements",
            result.TotalTransactions, result.OrderCount, result.SettlementCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<VoidRefundLogReportResult> GenerateVoidRefundLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating void/refund log report for {FromDate} to {ToDate}",
            parameters.FromDate, parameters.ToDate);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd)
            .Where(a => a.Action.Contains("Void") || a.Action == "ReceiptVoided");

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(parameters.MaxRecords)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = logs.Select(a => new VoidRefundLogItem
        {
            AuditLogId = a.Id,
            Timestamp = a.CreatedAt,
            RequestedByUser = a.User?.FullName ?? "Unknown",
            AuthorizedByUser = TryExtractAuthorizedBy(a.NewValues),
            ReceiptNumber = TryExtractReceiptNumber(a.NewValues) ?? $"Receipt-{a.EntityId}",
            VoidReason = TryExtractVoidReason(a.NewValues),
            VoidedAmount = TryExtractAmount(a.NewValues) ?? 0,
            NewValues = a.NewValues
        }).ToList();

        var result = new VoidRefundLogReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalVoids = items.Count,
            TotalVoidValue = items.Sum(i => i.VoidedAmount),
            Items = items
        };

        _logger.Information("Void/refund log report generated: {Count} voids, {TotalValue:C} total value",
            result.TotalVoids, result.TotalVoidValue);

        return result;
    }

    /// <inheritdoc />
    public async Task<PriceChangeLogReportResult> GeneratePriceChangeLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating price change log report for {FromDate} to {ToDate}",
            parameters.FromDate, parameters.ToDate);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd)
            .Where(a => a.EntityType == "Product")
            .Where(a => a.Action == "ProductUpdated" || a.Action == "ProductPriceChanged");

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(parameters.MaxRecords)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<PriceChangeLogItem>();
        var productIdsAffected = new HashSet<int>();

        foreach (var log in logs)
        {
            var oldPrice = TryExtractPrice(log.OldValues, "SellingPrice");
            var newPrice = TryExtractPrice(log.NewValues, "SellingPrice");

            // Only include if price actually changed
            if (oldPrice.HasValue && newPrice.HasValue && oldPrice != newPrice)
            {
                if (log.EntityId.HasValue)
                {
                    productIdsAffected.Add(log.EntityId.Value);
                }

                var priceDiff = newPrice.Value - oldPrice.Value;
                var changePercent = oldPrice.Value > 0 ? (priceDiff / oldPrice.Value) * 100 : 0;

                items.Add(new PriceChangeLogItem
                {
                    AuditLogId = log.Id,
                    Timestamp = log.CreatedAt,
                    UserName = log.User?.FullName ?? "Unknown",
                    ProductId = log.EntityId ?? 0,
                    ProductName = TryExtractProductName(log.NewValues) ?? $"Product-{log.EntityId}",
                    ProductCode = TryExtractProductCode(log.NewValues),
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PriceDifference = priceDiff,
                    ChangePercentage = Math.Round(changePercent, 2),
                    OldValues = log.OldValues,
                    NewValues = log.NewValues
                });
            }
        }

        var result = new PriceChangeLogReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalPriceChanges = items.Count,
            ProductsAffected = productIdsAffected.Count,
            Items = items
        };

        _logger.Information("Price change log report generated: {Count} price changes, {Products} products affected",
            result.TotalPriceChanges, result.ProductsAffected);

        return result;
    }

    /// <inheritdoc />
    public async Task<PermissionOverrideLogReportResult> GeneratePermissionOverrideLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating permission override log report for {FromDate} to {ToDate}",
            parameters.FromDate, parameters.ToDate);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd)
            .Where(a => a.Action.Contains("Override") || a.Action == "PermissionOverride");

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(parameters.MaxRecords)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = logs.Select(a => new PermissionOverrideLogItem
        {
            AuditLogId = a.Id,
            Timestamp = a.CreatedAt,
            RequestedByUser = TryExtractRequestedBy(a.NewValues) ?? "Unknown",
            AuthorizedByUser = a.User?.FullName ?? "Unknown",
            Permission = TryExtractPermission(a.NewValues) ?? "Unknown",
            PermissionDisplayName = GetPermissionDisplayName(TryExtractPermission(a.NewValues)),
            ActionType = TryExtractActionType(a.NewValues),
            EntityReference = a.EntityId.HasValue ? $"{a.EntityType}-{a.EntityId}" : null,
            Reason = TryExtractReason(a.NewValues),
            NewValues = a.NewValues
        }).ToList();

        // Group by permission type
        var byType = items
            .GroupBy(i => i.Permission)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new PermissionOverrideLogReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalOverrides = items.Count,
            OverridesByType = byType,
            Items = items
        };

        _logger.Information("Permission override log report generated: {Count} overrides",
            result.TotalOverrides);

        return result;
    }

    /// <inheritdoc />
    public async Task<AuditTrailReportResult> GenerateAuditTrailReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating audit trail report for {FromDate} to {ToDate}, Page {PageNumber}",
            parameters.FromDate, parameters.ToDate, parameters.PageNumber);

        var toDateEnd = parameters.ToDate.Date.AddDays(1);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= parameters.FromDate.Date)
            .Where(a => a.CreatedAt < toDateEnd);

        if (parameters.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == parameters.UserId.Value);
        }

        if (!string.IsNullOrEmpty(parameters.Action))
        {
            query = query.Where(a => a.Action == parameters.Action);
        }

        if (!string.IsNullOrEmpty(parameters.EntityType))
        {
            query = query.Where(a => a.EntityType == parameters.EntityType);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Get unique users count from the full query (not paginated)
        var uniqueUsers = await query
            .Where(a => a.UserId.HasValue)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        // Apply pagination
        var logs = parameters.UsePagination
            ? await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(parameters.Skip)
                .Take(parameters.Take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await query
                .OrderByDescending(a => a.CreatedAt)
#pragma warning disable CS0618 // Type or member is obsolete
                .Take(parameters.MaxRecords)
#pragma warning restore CS0618
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var items = logs.Select(a => new AuditTrailItem
        {
            AuditLogId = a.Id,
            Timestamp = a.CreatedAt,
            UserId = a.UserId,
            UserName = a.User?.FullName ?? "System",
            Action = a.Action,
            ActionDisplayName = GetActionDisplayName(a.Action),
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            MachineName = a.MachineName
        }).ToList();

        var result = new AuditTrailReportResult
        {
            FromDate = parameters.FromDate,
            ToDate = parameters.ToDate,
            TotalActions = totalCount,
            UniqueUsers = uniqueUsers,
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };

        _logger.Information("Audit trail report generated: {Count} actions (page {Page}/{TotalPages}), {Users} unique users",
            items.Count, result.PageNumber, result.TotalPages, result.UniqueUsers);

        return result;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctAuditActionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType != null)
            .Select(a => a.EntityType!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #region Private Helper Methods

    private static string GetActionDisplayName(string action)
    {
        return action switch
        {
            "Login" => "User Login",
            "Logout" => "User Logout",
            "LoginFailed" => "Failed Login",
            "PasswordChanged" => "Password Changed",
            "PasswordReset" => "Password Reset",
            "OrderCreated" => "Order Created",
            "OrderUpdated" => "Order Updated",
            "ReceiptSettled" => "Receipt Settled",
            "ReceiptVoided" => "Receipt Voided",
            "ProductCreated" => "Product Created",
            "ProductUpdated" => "Product Updated",
            "ProductActivated" => "Product Activated",
            "ProductDeactivated" => "Product Deactivated",
            "ProductDeleted" => "Product Deleted",
            "CategoryCreated" => "Category Created",
            "CategoryUpdated" => "Category Updated",
            "CategoryDeleted" => "Category Deleted",
            "CategoryActivated" => "Category Activated",
            "CategoryDeactivated" => "Category Deactivated",
            "RoleCreated" => "Role Created",
            "RoleUpdated" => "Role Updated",
            "RoleDeleted" => "Role Deleted",
            "UserCreated" => "User Created",
            "UserUpdated" => "User Updated",
            "UserActivated" => "User Activated",
            "UserDeactivated" => "User Deactivated",
            "PermissionOverride" => "Permission Override",
            "WorkPeriodOpened" => "Work Period Opened",
            "WorkPeriodClosed" => "Work Period Closed",
            "PaymentMethodCreated" => "Payment Method Created",
            "PaymentMethodUpdated" => "Payment Method Updated",
            "StockAdjustment" => "Stock Adjustment",
            "StockReceived" => "Stock Received",
            "ReceiptMerged" => "Receipts Merged",
            "ReceiptSplit" => "Receipt Split",
            "OwnershipTransferred" => "Ownership Transferred",
            _ => action
        };
    }

    private static string GetPermissionDisplayName(string? permission)
    {
        if (string.IsNullOrEmpty(permission))
            return "Unknown Permission";

        // Add spaces before capital letters and title case
        var result = System.Text.RegularExpressions.Regex.Replace(
            permission,
            "([A-Z])",
            " $1"
        ).Trim();

        return result;
    }

    private static decimal? TryExtractAmount(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            // Simple extraction for common amount fields
            foreach (var field in new[] { "TotalAmount", "Amount", "VoidedAmount", "SettledAmount" })
            {
                var pattern = $"\"{field}\"\\s*:\\s*([\\d.]+)";
                var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out var amount))
                {
                    return amount;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static decimal? TryExtractPrice(string? json, string fieldName)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            var pattern = $"\"{fieldName}\"\\s*:\\s*([\\d.]+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var price))
            {
                return price;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static string? TryExtractAuthorizedBy(string? json)
    {
        return TryExtractStringField(json, "AuthorizedBy") ?? TryExtractStringField(json, "VoidAuthorizedBy");
    }

    private static string? TryExtractReceiptNumber(string? json)
    {
        return TryExtractStringField(json, "ReceiptNumber");
    }

    private static string? TryExtractVoidReason(string? json)
    {
        return TryExtractStringField(json, "VoidReason") ?? TryExtractStringField(json, "Reason");
    }

    private static string? TryExtractProductName(string? json)
    {
        return TryExtractStringField(json, "Name");
    }

    private static string? TryExtractProductCode(string? json)
    {
        return TryExtractStringField(json, "Code");
    }

    private static string? TryExtractRequestedBy(string? json)
    {
        return TryExtractStringField(json, "RequestedBy") ?? TryExtractStringField(json, "RequestingUser");
    }

    private static string? TryExtractPermission(string? json)
    {
        return TryExtractStringField(json, "Permission") ?? TryExtractStringField(json, "PermissionName");
    }

    private static string? TryExtractActionType(string? json)
    {
        return TryExtractStringField(json, "ActionType") ?? TryExtractStringField(json, "Action");
    }

    private static string? TryExtractReason(string? json)
    {
        return TryExtractStringField(json, "Reason");
    }

    private static string? TryExtractStringField(string? json, string fieldName)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            var pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static string GetMovementTypeDisplayName(MovementType movementType)
    {
        return movementType switch
        {
            MovementType.Sale => "Sale",
            MovementType.Purchase => "Purchase",
            MovementType.PurchaseReceive => "Goods Received",
            MovementType.Adjustment => "Adjustment",
            MovementType.Void => "Void Return",
            MovementType.StockTake => "Stock Take",
            MovementType.Transfer => "Transfer",
            MovementType.Return => "Return",
            MovementType.Waste => "Waste/Damage",
            _ => movementType.ToString()
        };
    }

    private static decimal GetSignedQuantity(MovementType movementType, decimal quantity)
    {
        // Return negative for outgoing movements
        return movementType switch
        {
            MovementType.Sale or MovementType.Waste or MovementType.Transfer => -Math.Abs(quantity),
            _ => Math.Abs(quantity)
        };
    }

    private static string BuildMovementReference(StockMovement movement)
    {
        if (string.IsNullOrEmpty(movement.ReferenceType))
        {
            return string.Empty;
        }

        return movement.ReferenceId.HasValue
            ? $"{movement.ReferenceType}-{movement.ReferenceId}"
            : movement.ReferenceType;
    }

    private IQueryable<Receipt> GetSettledReceiptsQuery(SalesReportParameters parameters)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= parameters.StartDate)
            .Where(r => r.SettledAt < parameters.EndDate);

        if (parameters.WorkPeriodId.HasValue)
        {
            query = query.Where(r => r.WorkPeriodId == parameters.WorkPeriodId.Value);
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(r => r.OwnerId == parameters.UserId.Value);
        }

        return query;
    }

    private IQueryable<Receipt> GetVoidedReceiptsQuery(SalesReportParameters parameters)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Voided)
            .Where(r => r.VoidedAt >= parameters.StartDate)
            .Where(r => r.VoidedAt < parameters.EndDate);

        if (parameters.WorkPeriodId.HasValue)
        {
            query = query.Where(r => r.WorkPeriodId == parameters.WorkPeriodId.Value);
        }

        return query;
    }

    #endregion
}
