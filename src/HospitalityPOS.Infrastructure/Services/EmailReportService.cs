using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating email report content and attachments.
/// </summary>
public partial class EmailReportService : IEmailReportService
{
    private readonly POSDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    private const string CURRENCY_SYMBOL = "KSh";

    [GeneratedRegex(@"\{\{(\w+(?:\.\w+)*)\}\}")]
    private static partial Regex TemplateRegex();

    public EmailReportService(
        POSDbContext context,
        IEmailService emailService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Daily Sales Report

    public async Task<DailySalesEmailDataDto> GenerateDailySalesDataAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        var previousDay = startOfDay.AddDays(-1);

        // Get store name
        var storeName = storeId.HasValue
            ? (await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken))?.Name ?? "Store"
            : "All Stores";

        // Get today's receipts
        var receiptsQuery = _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Where(r => r.IsActive &&
                       r.CreatedAt >= startOfDay &&
                       r.CreatedAt < endOfDay &&
                       r.Status == ReceiptStatus.Settled);

        if (storeId.HasValue)
        {
            receiptsQuery = receiptsQuery.Where(r => r.StoreId == storeId);
        }

        var receipts = await receiptsQuery.ToListAsync(cancellationToken);

        // Get previous day's total for comparison
        var previousDayQuery = _context.Receipts
            .Where(r => r.IsActive &&
                       r.CreatedAt >= previousDay &&
                       r.CreatedAt < startOfDay &&
                       r.Status == ReceiptStatus.Settled);

        if (storeId.HasValue)
        {
            previousDayQuery = previousDayQuery.Where(r => r.StoreId == storeId);
        }

        var previousDaySales = await previousDayQuery.SumAsync(r => r.TotalAmount, cancellationToken);

        // Calculate metrics
        var totalSales = receipts.Sum(r => r.TotalAmount);
        var transactionCount = receipts.Count;
        var averageTransaction = transactionCount > 0 ? totalSales / transactionCount : 0;

        // Calculate sales change
        var salesChangePercent = previousDaySales > 0
            ? ((totalSales - previousDaySales) / previousDaySales) * 100
            : (totalSales > 0 ? 100 : 0);

        // Top 5 products
        var topProducts = receipts
            .SelectMany(r => r.ReceiptItems)
            .GroupBy(ri => new { ri.ProductId, ri.Product?.Name })
            .Select(g => new TopProductDto
            {
                Name = g.Key.Name ?? "Unknown",
                QuantitySold = (int)g.Sum(ri => ri.Quantity),
                Revenue = g.Sum(ri => ri.TotalPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        // Payment breakdown
        var paymentBreakdown = receipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => p.PaymentMethod?.Name ?? "Unknown")
            .Select(g => new PaymentBreakdownDto
            {
                MethodName = g.Key,
                Amount = g.Sum(p => p.Amount),
                TransactionCount = g.Count()
            })
            .ToList();

        var totalPayments = paymentBreakdown.Sum(p => p.Amount);
        foreach (var payment in paymentBreakdown)
        {
            payment.Percentage = totalPayments > 0
                ? Math.Round((payment.Amount / totalPayments) * 100, 1)
                : 0;
        }

        return new DailySalesEmailDataDto
        {
            Date = date,
            StoreName = storeName,
            TotalSales = totalSales,
            TransactionCount = transactionCount,
            AverageTransactionValue = averageTransaction,
            PreviousDaySales = previousDaySales,
            SalesChangePercent = Math.Round(salesChangePercent, 1),
            TopProducts = topProducts,
            PaymentBreakdown = paymentBreakdown.OrderByDescending(p => p.Amount).ToList(),
            CurrencySymbol = CURRENCY_SYMBOL
        };
    }

    public string RenderDailySalesEmail(DailySalesEmailDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine($"<div class=\"header\"><h1>Daily Sales Summary</h1>");
        sb.AppendLine($"<p class=\"subtitle\">{data.Date:dddd, MMMM d, yyyy}</p></div>");

        // Store name
        sb.AppendLine($"<h2>{data.StoreName}</h2>");

        // Key metrics
        sb.AppendLine("<div class=\"metrics\">");
        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Total Sales</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.CurrencySymbol} {data.TotalSales:N0}</div>");
        var changeClass = data.SalesIncreased ? "positive" : "negative";
        var changeSign = data.SalesIncreased ? "+" : "";
        sb.AppendLine($"<div class=\"metric-change {changeClass}\">{changeSign}{data.SalesChangePercent}% vs yesterday</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Transactions</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.TransactionCount}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Avg. Transaction</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.CurrencySymbol} {data.AverageTransactionValue:N0}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Top Products
        if (data.TopProducts.Any())
        {
            sb.AppendLine("<h3>Top 5 Products</h3>");
            sb.AppendLine("<table class=\"data-table\">");
            sb.AppendLine("<tr><th>Product</th><th>Qty Sold</th><th>Revenue</th></tr>");
            foreach (var product in data.TopProducts)
            {
                sb.AppendLine($"<tr><td>{product.Name}</td><td>{product.QuantitySold}</td><td>{data.CurrencySymbol} {product.Revenue:N0}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Payment Breakdown
        if (data.PaymentBreakdown.Any())
        {
            sb.AppendLine("<h3>Payment Methods</h3>");
            sb.AppendLine("<table class=\"data-table\">");
            sb.AppendLine("<tr><th>Method</th><th>Amount</th><th>Count</th><th>%</th></tr>");
            foreach (var payment in data.PaymentBreakdown)
            {
                sb.AppendLine($"<tr><td>{payment.MethodName}</td><td>{data.CurrencySymbol} {payment.Amount:N0}</td><td>{payment.TransactionCount}</td><td>{payment.Percentage}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine(GetFooter());
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    public string GetDailySalesSubject(DailySalesEmailDataDto data, string? customSubject = null)
    {
        if (!string.IsNullOrEmpty(customSubject))
        {
            return RenderTemplate(customSubject, new Dictionary<string, object?>
            {
                { "Date", data.Date.ToString("MMM d, yyyy") },
                { "StoreName", data.StoreName },
                { "TotalSales", $"{data.CurrencySymbol} {data.TotalSales:N0}" }
            });
        }

        return $"Daily Sales Summary - {data.StoreName} - {data.Date:MMM d, yyyy}";
    }

    #endregion

    #region Weekly Report

    public async Task<WeeklyReportEmailDataDto> GenerateWeeklyReportDataAsync(
        DateTime weekEndDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var endOfWeek = weekEndDate.Date.AddDays(1);
        var startOfWeek = endOfWeek.AddDays(-7);
        var previousWeekStart = startOfWeek.AddDays(-7);

        // Get store name
        var storeName = storeId.HasValue
            ? (await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken))?.Name ?? "Store"
            : "All Stores";

        // Get this week's receipts
        var receiptsQuery = _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
                    .ThenInclude(p => p!.Category)
            .Where(r => r.IsActive &&
                       r.CreatedAt >= startOfWeek &&
                       r.CreatedAt < endOfWeek &&
                       r.Status == ReceiptStatus.Settled);

        if (storeId.HasValue)
        {
            receiptsQuery = receiptsQuery.Where(r => r.StoreId == storeId);
        }

        var receipts = await receiptsQuery.ToListAsync(cancellationToken);

        // Get previous week's total
        var previousWeekQuery = _context.Receipts
            .Where(r => r.IsActive &&
                       r.CreatedAt >= previousWeekStart &&
                       r.CreatedAt < startOfWeek &&
                       r.Status == ReceiptStatus.Settled);

        if (storeId.HasValue)
        {
            previousWeekQuery = previousWeekQuery.Where(r => r.StoreId == storeId);
        }

        var previousWeekSales = await previousWeekQuery.SumAsync(r => r.TotalAmount, cancellationToken);

        // Calculate metrics
        var totalSales = receipts.Sum(r => r.TotalAmount);
        var transactionCount = receipts.Count;
        var salesChangePercent = previousWeekSales > 0
            ? ((totalSales - previousWeekSales) / previousWeekSales) * 100
            : (totalSales > 0 ? 100 : 0);

        // Daily breakdown
        var dailySales = Enumerable.Range(0, 7)
            .Select(i => startOfWeek.AddDays(i))
            .Select(date => new DailySalesDto
            {
                Date = date,
                DayName = date.ToString("dddd"),
                Sales = receipts.Where(r => r.CreatedAt.Date == date.Date).Sum(r => r.TotalAmount),
                Transactions = receipts.Count(r => r.CreatedAt.Date == date.Date)
            })
            .ToList();

        var bestDay = dailySales.OrderByDescending(d => d.Sales).First();
        var worstDay = dailySales.OrderBy(d => d.Sales).First();

        // Category performance
        var categoryPerformance = receipts
            .SelectMany(r => r.ReceiptItems)
            .Where(ri => ri.Product?.Category != null)
            .GroupBy(ri => ri.Product!.Category!.Name)
            .Select(g => new CategoryPerformanceDto
            {
                CategoryName = g.Key,
                Sales = g.Sum(ri => ri.TotalPrice),
                ItemsSold = (int)g.Sum(ri => ri.Quantity)
            })
            .OrderByDescending(c => c.Sales)
            .Take(10)
            .ToList();

        var totalCategorySales = categoryPerformance.Sum(c => c.Sales);
        foreach (var category in categoryPerformance)
        {
            category.Percentage = totalCategorySales > 0
                ? Math.Round((category.Sales / totalCategorySales) * 100, 1)
                : 0;
        }

        return new WeeklyReportEmailDataDto
        {
            WeekStartDate = startOfWeek,
            WeekEndDate = weekEndDate.Date,
            StoreName = storeName,
            TotalSales = totalSales,
            TransactionCount = transactionCount,
            PreviousWeekSales = previousWeekSales,
            SalesChangePercent = Math.Round(salesChangePercent, 1),
            DailySales = dailySales,
            CategoryPerformance = categoryPerformance,
            BestDay = bestDay.DayName,
            BestDaySales = bestDay.Sales,
            WorstDay = worstDay.DayName,
            WorstDaySales = worstDay.Sales,
            CurrencySymbol = CURRENCY_SYMBOL
        };
    }

    public string RenderWeeklyReportEmail(WeeklyReportEmailDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine("<div class=\"header\"><h1>Weekly Performance Report</h1>");
        sb.AppendLine($"<p class=\"subtitle\">{data.WeekStartDate:MMM d} - {data.WeekEndDate:MMM d, yyyy}</p></div>");

        // Store name
        sb.AppendLine($"<h2>{data.StoreName}</h2>");

        // Key metrics
        sb.AppendLine("<div class=\"metrics\">");
        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Total Sales</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.CurrencySymbol} {data.TotalSales:N0}</div>");
        var changeClass = data.SalesChangePercent >= 0 ? "positive" : "negative";
        var changeSign = data.SalesChangePercent >= 0 ? "+" : "";
        sb.AppendLine($"<div class=\"metric-change {changeClass}\">{changeSign}{data.SalesChangePercent}% vs last week</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Transactions</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.TransactionCount}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"metric-card\">");
        sb.AppendLine("<div class=\"metric-label\">Best Day</div>");
        sb.AppendLine($"<div class=\"metric-value\">{data.BestDay}</div>");
        sb.AppendLine($"<div class=\"metric-sub\">{data.CurrencySymbol} {data.BestDaySales:N0}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Daily breakdown
        sb.AppendLine("<h3>Daily Performance</h3>");
        sb.AppendLine("<table class=\"data-table\">");
        sb.AppendLine("<tr><th>Day</th><th>Sales</th><th>Transactions</th></tr>");
        foreach (var day in data.DailySales)
        {
            var rowClass = day.DayName == data.BestDay ? "highlight-row" : "";
            sb.AppendLine($"<tr class=\"{rowClass}\"><td>{day.DayName}</td><td>{data.CurrencySymbol} {day.Sales:N0}</td><td>{day.Transactions}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Category performance
        if (data.CategoryPerformance.Any())
        {
            sb.AppendLine("<h3>Category Performance</h3>");
            sb.AppendLine("<table class=\"data-table\">");
            sb.AppendLine("<tr><th>Category</th><th>Sales</th><th>Items</th><th>%</th></tr>");
            foreach (var category in data.CategoryPerformance)
            {
                sb.AppendLine($"<tr><td>{category.CategoryName}</td><td>{data.CurrencySymbol} {category.Sales:N0}</td><td>{category.ItemsSold}</td><td>{category.Percentage}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<p class=\"note\">See attached Excel file for detailed breakdown.</p>");
        sb.AppendLine(GetFooter());
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    public string GetWeeklyReportSubject(WeeklyReportEmailDataDto data, string? customSubject = null)
    {
        if (!string.IsNullOrEmpty(customSubject))
        {
            return RenderTemplate(customSubject, new Dictionary<string, object?>
            {
                { "WeekStart", data.WeekStartDate.ToString("MMM d") },
                { "WeekEnd", data.WeekEndDate.ToString("MMM d, yyyy") },
                { "StoreName", data.StoreName },
                { "TotalSales", $"{data.CurrencySymbol} {data.TotalSales:N0}" }
            });
        }

        return $"Weekly Performance Report - {data.StoreName} - {data.WeekStartDate:MMM d} to {data.WeekEndDate:MMM d}";
    }

    public Task<(byte[] Content, string FileName)> GenerateWeeklyReportExcelAsync(
        WeeklyReportEmailDataDto data,
        CancellationToken cancellationToken = default)
    {
        // Generate CSV as a simple alternative to Excel
        // In production, you would use a library like ClosedXML or EPPlus
        var sb = new StringBuilder();

        // Summary sheet
        sb.AppendLine("Weekly Performance Report");
        sb.AppendLine($"Store,{EscapeCsv(data.StoreName)}");
        sb.AppendLine($"Period,{data.WeekStartDate:yyyy-MM-dd} to {data.WeekEndDate:yyyy-MM-dd}");
        sb.AppendLine($"Total Sales,{data.TotalSales}");
        sb.AppendLine($"Total Transactions,{data.TransactionCount}");
        sb.AppendLine($"Change vs Last Week,{data.SalesChangePercent}%");
        sb.AppendLine();

        // Daily breakdown
        sb.AppendLine("Daily Breakdown");
        sb.AppendLine("Date,Day,Sales,Transactions");
        foreach (var day in data.DailySales)
        {
            sb.AppendLine($"{day.Date:yyyy-MM-dd},{day.DayName},{day.Sales},{day.Transactions}");
        }
        sb.AppendLine();

        // Category breakdown
        sb.AppendLine("Category Performance");
        sb.AppendLine("Category,Sales,Items Sold,Percentage");
        foreach (var cat in data.CategoryPerformance)
        {
            sb.AppendLine($"{EscapeCsv(cat.CategoryName)},{cat.Sales},{cat.ItemsSold},{cat.Percentage}%");
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"Weekly_Report_{data.WeekStartDate:yyyy-MM-dd}_{data.WeekEndDate:yyyy-MM-dd}.csv";

        return Task.FromResult((content, fileName));
    }

    #endregion

    #region Low Stock Alert

    public async Task<LowStockAlertEmailDataDto> GenerateLowStockAlertDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var storeName = storeId.HasValue
            ? (await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken))?.Name ?? "Store"
            : "All Stores";

        // Get low stock alert config
        var alertConfig = await _emailService.GetLowStockAlertConfigAsync(storeId, cancellationToken);
        var thresholdPercent = alertConfig?.ThresholdPercent ?? 100;
        var maxItems = alertConfig?.MaxItemsPerEmail ?? 50;

        // Get low stock items
        var query = _context.Inventories
            .Include(i => i.Product)
                .ThenInclude(p => p!.Category)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Supplier)
            .Where(i => i.IsActive &&
                       i.Product != null &&
                       i.Product.IsActive &&
                       i.Product.ReorderLevel > 0);

        // Filter by threshold percent
        query = query.Where(i =>
            (i.Quantity / i.Product!.ReorderLevel * 100) <= thresholdPercent);

        if (storeId.HasValue)
        {
            query = query.Where(i => i.StoreId == storeId);
        }

        var inventories = await query
            .OrderBy(i => i.Quantity / i.Product!.ReorderLevel)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        var items = inventories.Select(i => new LowStockItemDto
        {
            ProductName = i.Product!.Name,
            SKU = i.Product.SKU ?? "",
            CategoryName = i.Product.Category?.Name ?? "Uncategorized",
            CurrentStock = i.Quantity,
            ReorderLevel = i.Product.ReorderLevel,
            SupplierName = i.Product.Supplier?.Name
        }).ToList();

        var criticalCount = items.Count(i => i.CurrentStock <= 0);

        return new LowStockAlertEmailDataDto
        {
            GeneratedAt = DateTime.UtcNow,
            StoreName = storeName,
            TotalLowStockItems = items.Count,
            CriticalItems = criticalCount,
            Items = items
        };
    }

    public string RenderLowStockAlertEmail(LowStockAlertEmailDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine(".urgent { background-color: #fee2e2; }");
        sb.AppendLine(".critical-badge { background-color: #dc2626; color: white; padding: 2px 8px; border-radius: 4px; font-size: 12px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine("<div class=\"header alert-header\"><h1>⚠️ Low Stock Alert</h1>");
        sb.AppendLine($"<p class=\"subtitle\">{data.GeneratedAt:MMMM d, yyyy h:mm tt}</p></div>");

        // Store name
        sb.AppendLine($"<h2>{data.StoreName}</h2>");

        // Summary
        sb.AppendLine("<div class=\"alert-summary\">");
        sb.AppendLine($"<p><strong>{data.TotalLowStockItems}</strong> products are below reorder level.</p>");
        if (data.CriticalItems > 0)
        {
            sb.AppendLine($"<p class=\"urgent-text\"><strong>{data.CriticalItems}</strong> products are OUT OF STOCK!</p>");
        }
        sb.AppendLine("</div>");

        // Items table
        if (data.Items.Any())
        {
            sb.AppendLine("<table class=\"data-table\">");
            sb.AppendLine("<tr><th>Product</th><th>SKU</th><th>Category</th><th>Stock</th><th>Reorder Level</th><th>Deficit</th></tr>");
            foreach (var item in data.Items)
            {
                var rowClass = item.CurrentStock <= 0 ? "urgent" : "";
                var criticalBadge = item.CurrentStock <= 0 ? " <span class=\"critical-badge\">OUT</span>" : "";
                sb.AppendLine($"<tr class=\"{rowClass}\"><td>{item.ProductName}{criticalBadge}</td><td>{item.SKU}</td><td>{item.CategoryName}</td><td>{item.CurrentStock:N0}</td><td>{item.ReorderLevel:N0}</td><td>{item.StockDeficit:N0}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<p class=\"action-note\">Please review and create purchase orders for these items.</p>");
        sb.AppendLine(GetFooter());
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    public string GetLowStockAlertSubject(LowStockAlertEmailDataDto data, string? customSubject = null)
    {
        if (!string.IsNullOrEmpty(customSubject))
        {
            return RenderTemplate(customSubject, new Dictionary<string, object?>
            {
                { "StoreName", data.StoreName },
                { "ItemCount", data.TotalLowStockItems },
                { "CriticalCount", data.CriticalItems }
            });
        }

        var urgency = data.CriticalItems > 0 ? "🚨 URGENT: " : "";
        return $"{urgency}Low Stock Alert - {data.TotalLowStockItems} items - {data.StoreName}";
    }

    #endregion

    #region Expiry Alert

    public async Task<ExpiryAlertEmailDataDto> GenerateExpiryAlertDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var storeName = storeId.HasValue
            ? (await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken))?.Name ?? "Store"
            : "All Stores";

        // Get expiry alert config
        var alertConfig = await _emailService.GetExpiryAlertConfigAsync(storeId, cancellationToken);
        var alertThresholdDays = alertConfig?.AlertThresholdDays ?? 7;
        var urgentThresholdDays = alertConfig?.UrgentThresholdDays ?? 3;
        var maxItems = alertConfig?.MaxItemsPerEmail ?? 50;

        var today = DateTime.UtcNow.Date;
        var alertDate = today.AddDays(alertThresholdDays);

        // Get expiring batches
        var query = _context.ProductBatches
            .Include(b => b.Product)
            .Where(b => b.IsActive &&
                       b.ExpiryDate.HasValue &&
                       b.ExpiryDate <= alertDate &&
                       b.CurrentQuantity > 0 &&
                       b.Status == BatchStatus.Active);

        if (storeId.HasValue)
        {
            query = query.Where(b => b.StoreId == storeId);
        }

        var batches = await query
            .OrderBy(b => b.ExpiryDate)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        var items = batches.Select(b =>
        {
            var daysUntilExpiry = (b.ExpiryDate!.Value.Date - today).Days;
            return new ExpiringItemDto
            {
                ProductName = b.Product?.Name ?? "Unknown",
                SKU = b.Product?.SKU ?? "",
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate.Value,
                DaysUntilExpiry = daysUntilExpiry,
                Quantity = b.CurrentQuantity,
                UnitOfMeasure = b.Product?.UnitOfMeasure ?? "units",
                IsUrgent = daysUntilExpiry <= urgentThresholdDays
            };
        }).ToList();

        var urgentCount = items.Count(i => i.IsUrgent);

        return new ExpiryAlertEmailDataDto
        {
            GeneratedAt = DateTime.UtcNow,
            StoreName = storeName,
            AlertThresholdDays = alertThresholdDays,
            TotalExpiringItems = items.Count,
            UrgentItems = urgentCount,
            Items = items
        };
    }

    public string RenderExpiryAlertEmail(ExpiryAlertEmailDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine(".urgent { background-color: #fef3c7; }");
        sb.AppendLine(".expired { background-color: #fee2e2; }");
        sb.AppendLine(".urgent-badge { background-color: #f59e0b; color: white; padding: 2px 8px; border-radius: 4px; font-size: 12px; }");
        sb.AppendLine(".expired-badge { background-color: #dc2626; color: white; padding: 2px 8px; border-radius: 4px; font-size: 12px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine("<div class=\"header alert-header\"><h1>📅 Product Expiry Alert</h1>");
        sb.AppendLine($"<p class=\"subtitle\">{data.GeneratedAt:MMMM d, yyyy h:mm tt}</p></div>");

        // Store name
        sb.AppendLine($"<h2>{data.StoreName}</h2>");

        // Summary
        sb.AppendLine("<div class=\"alert-summary\">");
        sb.AppendLine($"<p><strong>{data.TotalExpiringItems}</strong> batches expiring within {data.AlertThresholdDays} days.</p>");
        if (data.UrgentItems > 0)
        {
            sb.AppendLine($"<p class=\"urgent-text\"><strong>{data.UrgentItems}</strong> batches require immediate attention!</p>");
        }
        sb.AppendLine("</div>");

        // Items table
        if (data.Items.Any())
        {
            sb.AppendLine("<table class=\"data-table\">");
            sb.AppendLine("<tr><th>Product</th><th>Batch</th><th>Expiry Date</th><th>Days Left</th><th>Quantity</th></tr>");
            foreach (var item in data.Items)
            {
                var rowClass = item.DaysUntilExpiry <= 0 ? "expired" : (item.IsUrgent ? "urgent" : "");
                var badge = item.DaysUntilExpiry <= 0
                    ? " <span class=\"expired-badge\">EXPIRED</span>"
                    : (item.IsUrgent ? " <span class=\"urgent-badge\">URGENT</span>" : "");
                var daysDisplay = item.DaysUntilExpiry <= 0 ? "EXPIRED" : $"{item.DaysUntilExpiry} days";
                sb.AppendLine($"<tr class=\"{rowClass}\"><td>{item.ProductName}{badge}</td><td>{item.BatchNumber}</td><td>{item.ExpiryDate:yyyy-MM-dd}</td><td>{daysDisplay}</td><td>{item.Quantity:N0} {item.UnitOfMeasure}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<p class=\"action-note\">Please review and take appropriate action (discount, return to supplier, or dispose).</p>");
        sb.AppendLine(GetFooter());
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    public string GetExpiryAlertSubject(ExpiryAlertEmailDataDto data, string? customSubject = null)
    {
        if (!string.IsNullOrEmpty(customSubject))
        {
            return RenderTemplate(customSubject, new Dictionary<string, object?>
            {
                { "StoreName", data.StoreName },
                { "ItemCount", data.TotalExpiringItems },
                { "UrgentCount", data.UrgentItems },
                { "ThresholdDays", data.AlertThresholdDays }
            });
        }

        var urgency = data.UrgentItems > 0 ? "🚨 " : "";
        return $"{urgency}Expiry Alert - {data.TotalExpiringItems} batches - {data.StoreName}";
    }

    #endregion

    #region Template Management

    public async Task<string?> GetTemplateAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .Where(t => t.IsActive && t.ReportType == reportType && t.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null && storeId.HasValue)
        {
            template = await _context.EmailTemplates
                .Where(t => t.IsActive && t.ReportType == reportType && t.StoreId == null && t.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return template?.HtmlBodyTemplate;
    }

    public string RenderTemplate(string template, Dictionary<string, object?> data)
    {
        return TemplateRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (data.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? "";
            }
            return match.Value;
        });
    }

    #endregion

    #region Full Report Generation

    public async Task<EmailMessageDto?> GenerateReportEmailAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        // Get recipients for this report type
        var recipients = await _emailService.GetRecipientsForReportAsync(reportType, storeId, cancellationToken);
        if (!recipients.Any())
        {
            _logger.Warning("No recipients found for report type {ReportType}", reportType);
            return null;
        }

        var toAddresses = recipients.Where(r => !r.IsCc && !r.IsBcc).Select(r => r.Email).ToList();
        var ccAddresses = recipients.Where(r => r.IsCc).Select(r => r.Email).ToList();
        var bccAddresses = recipients.Where(r => r.IsBcc).Select(r => r.Email).ToList();

        // Get schedule for custom subject
        var schedule = await _emailService.GetScheduleForReportAsync(reportType, storeId, cancellationToken);
        var customSubject = schedule?.CustomSubject;

        EmailMessageDto message;

        switch (reportType)
        {
            case EmailReportType.DailySales:
                var dailyData = await GenerateDailySalesDataAsync(DateTime.UtcNow.Date.AddDays(-1), storeId, cancellationToken);
                message = new EmailMessageDto
                {
                    ToAddresses = toAddresses,
                    CcAddresses = ccAddresses,
                    BccAddresses = bccAddresses,
                    Subject = GetDailySalesSubject(dailyData, customSubject),
                    HtmlBody = RenderDailySalesEmail(dailyData),
                    ReportType = reportType,
                    StoreId = storeId
                };
                break;

            case EmailReportType.WeeklyReport:
                var weeklyData = await GenerateWeeklyReportDataAsync(DateTime.UtcNow.Date.AddDays(-1), storeId, cancellationToken);
                var (excelContent, excelName) = await GenerateWeeklyReportExcelAsync(weeklyData, cancellationToken);
                message = new EmailMessageDto
                {
                    ToAddresses = toAddresses,
                    CcAddresses = ccAddresses,
                    BccAddresses = bccAddresses,
                    Subject = GetWeeklyReportSubject(weeklyData, customSubject),
                    HtmlBody = RenderWeeklyReportEmail(weeklyData),
                    Attachment = new EmailAttachmentDto
                    {
                        FileName = excelName,
                        Content = excelContent,
                        ContentType = "text/csv"
                    },
                    ReportType = reportType,
                    StoreId = storeId
                };
                break;

            case EmailReportType.LowStockAlert:
                var lowStockData = await GenerateLowStockAlertDataAsync(storeId, cancellationToken);
                if (lowStockData.TotalLowStockItems == 0)
                {
                    _logger.Information("No low stock items to report");
                    return null;
                }
                message = new EmailMessageDto
                {
                    ToAddresses = toAddresses,
                    CcAddresses = ccAddresses,
                    BccAddresses = bccAddresses,
                    Subject = GetLowStockAlertSubject(lowStockData, customSubject),
                    HtmlBody = RenderLowStockAlertEmail(lowStockData),
                    ReportType = reportType,
                    StoreId = storeId
                };
                break;

            case EmailReportType.ExpiryAlert:
                var expiryData = await GenerateExpiryAlertDataAsync(storeId, cancellationToken);
                if (expiryData.TotalExpiringItems == 0)
                {
                    _logger.Information("No expiring items to report");
                    return null;
                }
                message = new EmailMessageDto
                {
                    ToAddresses = toAddresses,
                    CcAddresses = ccAddresses,
                    BccAddresses = bccAddresses,
                    Subject = GetExpiryAlertSubject(expiryData, customSubject),
                    HtmlBody = RenderExpiryAlertEmail(expiryData),
                    ReportType = reportType,
                    StoreId = storeId
                };
                break;

            default:
                _logger.Warning("Unsupported report type: {ReportType}", reportType);
                return null;
        }

        return message;
    }

    #endregion

    #region Helper Methods

    private static string GetCommonStyles()
    {
        return @"
            body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }
            .container { max-width: 800px; margin: 0 auto; background-color: #ffffff; }
            .header { background-color: #2563eb; color: white; padding: 30px; text-align: center; }
            .header h1 { margin: 0 0 10px 0; font-size: 28px; }
            .header .subtitle { margin: 0; opacity: 0.9; font-size: 16px; }
            .alert-header { background-color: #f59e0b; }
            h2 { color: #1f2937; padding: 20px 30px 0 30px; margin: 0; font-size: 20px; }
            h3 { color: #374151; padding: 20px 30px 10px 30px; margin: 0; font-size: 16px; border-top: 1px solid #e5e7eb; }
            .metrics { display: flex; flex-wrap: wrap; padding: 20px 20px; gap: 10px; }
            .metric-card { flex: 1; min-width: 150px; background-color: #f9fafb; border-radius: 8px; padding: 20px; text-align: center; }
            .metric-label { color: #6b7280; font-size: 14px; margin-bottom: 8px; }
            .metric-value { color: #111827; font-size: 28px; font-weight: 700; }
            .metric-change { font-size: 14px; margin-top: 8px; }
            .metric-sub { color: #6b7280; font-size: 14px; margin-top: 4px; }
            .positive { color: #059669; }
            .negative { color: #dc2626; }
            .data-table { width: calc(100% - 60px); margin: 0 30px 20px 30px; border-collapse: collapse; font-size: 14px; }
            .data-table th { background-color: #f9fafb; color: #374151; text-align: left; padding: 12px; border-bottom: 2px solid #e5e7eb; }
            .data-table td { padding: 12px; border-bottom: 1px solid #e5e7eb; color: #4b5563; }
            .data-table tr:hover { background-color: #f9fafb; }
            .highlight-row { background-color: #ecfdf5 !important; }
            .alert-summary { padding: 20px 30px; background-color: #fef3c7; margin: 0; }
            .alert-summary p { margin: 5px 0; }
            .urgent-text { color: #dc2626; font-weight: 600; }
            .action-note { padding: 15px 30px; color: #6b7280; font-style: italic; }
            .note { padding: 0 30px 20px 30px; color: #6b7280; font-size: 14px; }
            .footer { background-color: #f9fafb; padding: 20px 30px; text-align: center; color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }
            .footer p { margin: 5px 0; }
            @media only screen and (max-width: 600px) {
                .metrics { flex-direction: column; }
                .metric-card { min-width: auto; }
                .data-table { font-size: 12px; }
                .data-table th, .data-table td { padding: 8px; }
            }
        ";
    }

    private static string GetFooter()
    {
        return $@"
            <div class=""footer"">
                <p>This is an automated email from Hospitality POS System.</p>
                <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
            </div>
        ";
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion
}
