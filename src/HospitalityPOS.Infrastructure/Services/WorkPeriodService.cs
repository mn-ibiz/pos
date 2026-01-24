using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing work periods (shifts) in the POS system.
/// </summary>
public class WorkPeriodService : IWorkPeriodService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkPeriodService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public WorkPeriodService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkPeriod?> GetCurrentWorkPeriodAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .Where(wp => wp.Status == WorkPeriodStatus.Open)
            .OrderByDescending(wp => wp.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsWorkPeriodOpenAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriods
            .AnyAsync(wp => wp.Status == WorkPeriodStatus.Open, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkPeriod> OpenWorkPeriodAsync(decimal openingFloat, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        // Validate no existing open work period
        if (await IsWorkPeriodOpenAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A work period is already open. Please close the current work period before opening a new one.");
        }

        // Validate opening float
        if (openingFloat < 0)
        {
            throw new ArgumentException("Opening float cannot be negative.", nameof(openingFloat));
        }

        var workPeriod = new WorkPeriod
        {
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = userId,
            OpeningFloat = openingFloat,
            Status = WorkPeriodStatus.Open,
            Notes = notes?.Trim()
        };

        await _context.WorkPeriods.AddAsync(workPeriod, cancellationToken).ConfigureAwait(false);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "WorkPeriodOpened",
            EntityType = nameof(WorkPeriod),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                OpeningFloat = openingFloat,
                OpenedAt = workPeriod.OpenedAt,
                Notes = notes
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Work period opened by user {UserId} with opening float {OpeningFloat:C}", userId, openingFloat);

        // Reload with navigation properties
        return (await GetByIdAsync(workPeriod.Id, cancellationToken).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public async Task<WorkPeriod> CloseWorkPeriodAsync(decimal closingCash, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .Where(wp => wp.Status == WorkPeriodStatus.Open)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException("No work period is currently open.");
        }

        // Calculate expected cash
        var expectedCash = await CalculateExpectedCashAsync(workPeriod.Id, cancellationToken).ConfigureAwait(false);

        // Get next Z-report number
        var lastZReport = await _context.WorkPeriods
            .Where(wp => wp.ZReportNumber.HasValue)
            .MaxAsync(wp => (int?)wp.ZReportNumber, cancellationToken)
            .ConfigureAwait(false);

        workPeriod.ClosedAt = DateTime.UtcNow;
        workPeriod.ClosedByUserId = userId;
        workPeriod.ClosingCash = closingCash;
        workPeriod.ExpectedCash = expectedCash;
        workPeriod.Variance = closingCash - expectedCash;
        workPeriod.Status = WorkPeriodStatus.Closed;
        workPeriod.ZReportNumber = (lastZReport ?? 0) + 1;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            workPeriod.Notes = string.IsNullOrEmpty(workPeriod.Notes)
                ? notes.Trim()
                : $"{workPeriod.Notes}\n---\nClosing Notes: {notes.Trim()}";
        }

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "WorkPeriodClosed",
            EntityType = nameof(WorkPeriod),
            EntityId = workPeriod.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                ClosingCash = closingCash,
                ExpectedCash = expectedCash,
                Variance = workPeriod.Variance,
                ZReportNumber = workPeriod.ZReportNumber,
                ClosedAt = workPeriod.ClosedAt,
                Duration = (workPeriod.ClosedAt - workPeriod.OpenedAt)?.TotalHours
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Work period {WorkPeriodId} closed by user {UserId}. Expected: {Expected:C}, Actual: {Actual:C}, Variance: {Variance:C}",
            workPeriod.Id, userId, expectedCash, closingCash, workPeriod.Variance);

        return workPeriod;
    }

    /// <inheritdoc />
    public async Task<WorkPeriod?> GetLastClosedWorkPeriodAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .Where(wp => wp.Status == WorkPeriodStatus.Closed)
            .OrderByDescending(wp => wp.ClosedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkPeriod>> GetWorkPeriodHistoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .Where(wp => wp.OpenedAt >= startDate && wp.OpenedAt <= endDate)
            .OrderByDescending(wp => wp.OpenedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateExpectedCashAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            return 0;
        }

        // Start with opening float
        var expectedCash = workPeriod.OpeningFloat;

        // Add cash payments from receipts in this work period
        // Note: This will be implemented when Payment entity queries are available
        // For now, return opening float as placeholder
        var cashPayments = await _context.Payments
            .Where(p => p.Receipt != null && p.Receipt.WorkPeriodId == workPeriodId)
            .Where(p => p.PaymentMethod != null && p.PaymentMethod.Type == PaymentMethodType.Cash)
            .SumAsync(p => p.Amount, cancellationToken)
            .ConfigureAwait(false);

        expectedCash += cashPayments;

        // Subtract cash payouts
        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId)
            .Where(p => p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, cancellationToken)
            .ConfigureAwait(false);

        expectedCash -= cashPayouts;

        return expectedCash;
    }

    /// <inheritdoc />
    public async Task<WorkPeriod?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<XReport> GenerateXReportAsync(int workPeriodId, int generatedByUserId, string generatedByUserName, CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException($"Work period with ID {workPeriodId} not found.");
        }

        // Get business settings
        var businessName = await GetSystemSettingAsync("BusinessName", "Hospitality POS", cancellationToken);
        var businessAddress = await GetSystemSettingAsync("BusinessAddress", "", cancellationToken);
        var businessPhone = await GetSystemSettingAsync("BusinessPhone", "", cancellationToken);

        // Get X-report sequence number for this work period
        var xReportCount = await _context.AuditLogs
            .CountAsync(a => a.Action == "XReportGenerated" &&
                            a.EntityType == nameof(WorkPeriod) &&
                            a.EntityId == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        // Get all receipts for this work period
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.VoidedByUser)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.Order)
                .ThenInclude(o => o!.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p!.Category)
            .Where(r => r.WorkPeriodId == workPeriodId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();

        // Calculate sales summary
        var grossSales = settledReceipts.Sum(r => r.Subtotal);
        var totalDiscounts = settledReceipts.Sum(r => r.DiscountAmount);
        var netSales = grossSales - totalDiscounts;
        var taxCollected = settledReceipts.Sum(r => r.TaxAmount);
        var grandTotal = settledReceipts.Sum(r => r.TotalAmount);

        // Transaction statistics
        var transactionCount = settledReceipts.Count;
        var averageTransactionValue = transactionCount > 0 ? grandTotal / transactionCount : 0;

        // Sales by category
        var salesByCategory = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => oi.Product?.Category?.Name ?? "Uncategorized")
            .Select(g => new CategorySalesSummary
            {
                CategoryName = g.Key,
                ItemCount = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                TotalAmount = g.Sum(oi => oi.TotalAmount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Sales by payment method
        var salesByPaymentMethod = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => p.PaymentMethod?.Name ?? "Unknown")
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethod = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // Sales by user/cashier
        var salesByUser = settledReceipts
            .GroupBy(r => r.Owner?.FullName ?? "Unknown")
            .Select(g => new UserSalesSummary
            {
                UserName = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(r => r.TotalAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0
            })
            .OrderByDescending(u => u.TotalAmount)
            .ToList();

        // Voids summary
        var voidsSummary = voidedReceipts
            .Select(r => new VoidSummary
            {
                ReceiptNumber = r.ReceiptNumber,
                Amount = r.TotalAmount,
                Reason = r.VoidReason ?? "No reason provided",
                VoidedBy = r.VoidedByUser?.FullName ?? "Unknown",
                VoidedAt = r.VoidedAt ?? DateTime.UtcNow
            })
            .ToList();

        // Calculate expected cash
        var expectedCash = await CalculateExpectedCashAsync(workPeriodId, cancellationToken).ConfigureAwait(false);

        // Hourly sales breakdown
        var hourlySales = settledReceipts
            .GroupBy(r => r.SettledAt?.ToLocalTime().Hour ?? r.CreatedAt.ToLocalTime().Hour)
            .Select(g => new HourlySalesSummary
            {
                Hour = g.Key,
                HourLabel = FormatHourLabel(g.Key),
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(r => r.TotalAmount)
            })
            .OrderBy(h => h.Hour)
            .ToList();

        // Fill in missing hours with zero values for complete chart
        var allHourlySales = Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var existing = hourlySales.FirstOrDefault(h => h.Hour == hour);
                return existing ?? new HourlySalesSummary
                {
                    Hour = hour,
                    HourLabel = FormatHourLabel(hour),
                    TransactionCount = 0,
                    TotalAmount = 0
                };
            })
            .ToList();

        var xReport = new XReport
        {
            // Header
            BusinessName = businessName,
            BusinessAddress = businessAddress,
            BusinessPhone = businessPhone,
            GeneratedAt = DateTime.UtcNow,
            ReportNumber = xReportCount + 1,
            GeneratedBy = generatedByUserName,

            // Work Period Info
            WorkPeriodId = workPeriodId,
            WorkPeriodOpenedAt = workPeriod.OpenedAt,
            WorkPeriodOpenedBy = workPeriod.OpenedByUser?.FullName ?? "Unknown",
            Duration = DateTime.UtcNow - workPeriod.OpenedAt,
            OpeningFloat = workPeriod.OpeningFloat,

            // Sales Summary
            GrossSales = grossSales,
            TotalDiscounts = totalDiscounts,
            NetSales = netSales,
            TaxCollected = taxCollected,
            GrandTotal = grandTotal,

            // Breakdowns
            SalesByCategory = salesByCategory,
            SalesByPaymentMethod = salesByPaymentMethod,
            SalesByUser = salesByUser,
            HourlySales = allHourlySales,

            // Transaction Stats
            TransactionCount = transactionCount,
            AverageTransactionValue = averageTransactionValue,

            // Voids
            VoidCount = voidedReceipts.Count,
            VoidTotal = voidedReceipts.Sum(r => r.TotalAmount),
            Voids = voidsSummary,

            // Cash Position
            ExpectedCash = expectedCash
        };

        // Log X-Report generation
        var auditLog = new AuditLog
        {
            UserId = generatedByUserId,
            Action = "XReportGenerated",
            EntityType = nameof(WorkPeriod),
            EntityId = workPeriodId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                ReportNumber = xReport.ReportNumber,
                GrandTotal = grandTotal,
                TransactionCount = transactionCount,
                VoidCount = voidedReceipts.Count
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "X-Report #{ReportNumber} generated for work period {WorkPeriodId}. Total: {GrandTotal:C}, Transactions: {TransactionCount}",
            xReport.ReportNumber, workPeriodId, grandTotal, transactionCount);

        return xReport;
    }

    /// <inheritdoc />
    public async Task<ZReport> GenerateZReportAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException($"Work period with ID {workPeriodId} not found.");
        }

        if (workPeriod.Status != WorkPeriodStatus.Closed)
        {
            throw new InvalidOperationException("Z-Report can only be generated for closed work periods.");
        }

        // Get business settings
        var businessName = await GetSystemSettingAsync("BusinessName", "Hospitality POS", cancellationToken);
        var businessAddress = await GetSystemSettingAsync("BusinessAddress", "", cancellationToken);
        var businessPhone = await GetSystemSettingAsync("BusinessPhone", "", cancellationToken);

        // Get all receipts for this work period
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.VoidedByUser)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.Order)
                .ThenInclude(o => o!.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p!.Category)
            .Where(r => r.WorkPeriodId == workPeriodId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var pendingReceipts = receipts.Where(r => r.Status == ReceiptStatus.Open || r.Status == ReceiptStatus.Pending).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();

        // Calculate sales summary
        var grossSales = settledReceipts.Sum(r => r.Subtotal);
        var totalDiscounts = settledReceipts.Sum(r => r.DiscountAmount);
        var netSales = grossSales - totalDiscounts;
        var taxCollected = settledReceipts.Sum(r => r.TaxAmount);
        var grandTotal = settledReceipts.Sum(r => r.TotalAmount);

        // Transaction statistics
        var transactionCount = settledReceipts.Count;
        var averageTransactionValue = transactionCount > 0 ? grandTotal / transactionCount : 0;

        // Sales by category
        var salesByCategory = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => oi.Product?.Category?.Name ?? "Uncategorized")
            .Select(g => new CategorySalesSummary
            {
                CategoryName = g.Key,
                ItemCount = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                TotalAmount = g.Sum(oi => oi.TotalAmount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Sales by payment method
        var salesByPaymentMethod = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => p.PaymentMethod?.Name ?? "Unknown")
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethod = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // Sales by user/cashier
        var salesByUser = settledReceipts
            .GroupBy(r => r.Owner?.FullName ?? "Unknown")
            .Select(g => new UserSalesSummary
            {
                UserName = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(r => r.TotalAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0
            })
            .OrderByDescending(u => u.TotalAmount)
            .ToList();

        // Voids summary
        var voidsSummary = voidedReceipts
            .Select(r => new VoidSummary
            {
                ReceiptNumber = r.ReceiptNumber,
                Amount = r.TotalAmount,
                Reason = r.VoidReason ?? "No reason provided",
                VoidedBy = r.VoidedByUser?.FullName ?? "Unknown",
                VoidedAt = r.VoidedAt ?? DateTime.UtcNow
            })
            .ToList();

        // Calculate cash sales (from payments)
        var cashSales = settledReceipts
            .SelectMany(r => r.Payments)
            .Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        // Cash payouts from approved payouts
        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId)
            .Where(p => p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, cancellationToken)
            .ConfigureAwait(false);

        // Top selling items
        var topSellingItems = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => oi.Product?.Name ?? "Unknown")
            .Select(g => new ItemSoldSummary
            {
                ProductName = g.Key,
                QuantitySold = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                TotalValue = g.Sum(oi => oi.TotalAmount)
            })
            .OrderByDescending(i => i.TotalValue)
            .Take(10)
            .ToList();

        var zReport = new ZReport
        {
            // Header
            BusinessName = businessName,
            BusinessAddress = businessAddress,
            BusinessPhone = businessPhone,
            ZReportNumber = workPeriod.ZReportNumber ?? 0,

            // Work Period Info
            WorkPeriodId = workPeriodId,
            WorkPeriodOpenedAt = workPeriod.OpenedAt,
            WorkPeriodOpenedBy = workPeriod.OpenedByUser?.FullName ?? "Unknown",
            WorkPeriodClosedAt = workPeriod.ClosedAt ?? DateTime.UtcNow,
            WorkPeriodClosedBy = workPeriod.ClosedByUser?.FullName ?? "Unknown",
            Duration = (workPeriod.ClosedAt ?? DateTime.UtcNow) - workPeriod.OpenedAt,

            // Sales Summary
            GrossSales = grossSales,
            TotalDiscounts = totalDiscounts,
            NetSales = netSales,
            TaxCollected = taxCollected,
            GrandTotal = grandTotal,

            // Breakdowns
            SalesByCategory = salesByCategory,
            SalesByPaymentMethod = salesByPaymentMethod,
            SalesByUser = salesByUser,

            // Transaction Stats
            TransactionCount = transactionCount,
            AverageTransactionValue = averageTransactionValue,

            // Receipt Summary
            SettledReceiptsCount = settledReceipts.Count,
            SettledReceiptsTotal = settledReceipts.Sum(r => r.TotalAmount),
            PendingReceiptsCount = pendingReceipts.Count,
            PendingReceiptsTotal = pendingReceipts.Sum(r => r.TotalAmount),

            // Voids
            VoidCount = voidedReceipts.Count,
            VoidTotal = voidedReceipts.Sum(r => r.TotalAmount),
            Voids = voidsSummary,

            // Cash Drawer
            OpeningFloat = workPeriod.OpeningFloat,
            CashSales = cashSales,
            CashPayouts = cashPayouts,
            ExpectedCash = workPeriod.ExpectedCash ?? 0,
            ActualCash = workPeriod.ClosingCash ?? 0,
            Variance = workPeriod.Variance ?? 0,

            // Top Selling
            TopSellingItems = topSellingItems,

            Notes = workPeriod.Notes
        };

        _logger.Information(
            "Z-Report #{ZReportNumber} generated for work period {WorkPeriodId}. Total: {GrandTotal:C}, Variance: {Variance:C}",
            zReport.ZReportNumber, workPeriodId, grandTotal, zReport.Variance);

        return zReport;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Receipt>> GetUnsettledReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.Order)
            .Where(r => r.WorkPeriodId == workPeriodId)
            .Where(r => r.Status == ReceiptStatus.Open || r.Status == ReceiptStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetUnsettledReceiptsCountAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .Where(r => r.WorkPeriodId == workPeriodId)
            .Where(r => r.Status == ReceiptStatus.Open || r.Status == ReceiptStatus.Pending)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
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

    private async Task<string> GetSystemSettingAsync(string key, string defaultValue, CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken)
            .ConfigureAwait(false);

        return setting?.SettingValue ?? defaultValue;
    }

    #region Denomination-Aware Methods

    /// <inheritdoc />
    public async Task<List<CashDenominationDto>> GetActiveDenominationsAsync(string currencyCode = "KES", CancellationToken cancellationToken = default)
    {
        return await _context.CashDenominations
            .AsNoTracking()
            .Where(d => d.CurrencyCode == currencyCode && d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Select(d => new CashDenominationDto
            {
                Id = d.Id,
                CurrencyCode = d.CurrencyCode,
                Type = d.Type,
                Value = d.Value,
                DisplayName = d.DisplayName,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkPeriod> OpenWorkPeriodWithDenominationsAsync(
        CashDenominationCountDto openingCount,
        int userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Validate no existing open work period
        if (await IsWorkPeriodOpenAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A work period is already open. Please close the current work period before opening a new one.");
        }

        // Calculate totals from denomination count
        var (totalNotes, totalCoins, grandTotal) = await CalculateDenominationTotalsAsync(openingCount.Denominations, cancellationToken);

        var workPeriod = new WorkPeriod
        {
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = userId,
            OpeningFloat = grandTotal,
            Status = WorkPeriodStatus.Open,
            Notes = notes?.Trim()
        };

        await _context.WorkPeriods.AddAsync(workPeriod, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create the denomination count record
        var cashCount = await CreateCashCountAsync(
            workPeriod.Id,
            CashCountType.Opening,
            openingCount,
            userId,
            totalNotes,
            totalCoins,
            grandTotal,
            cancellationToken);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "WorkPeriodOpenedWithDenominations",
            EntityType = nameof(WorkPeriod),
            EntityId = workPeriod.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                OpeningFloat = grandTotal,
                TotalNotes = totalNotes,
                TotalCoins = totalCoins,
                DenominationCount = openingCount.Denominations.Count,
                OpenedAt = workPeriod.OpenedAt,
                Notes = notes
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Work period opened by user {UserId} with denomination count. Total: {Total:C} (Notes: {Notes:C}, Coins: {Coins:C})",
            userId, grandTotal, totalNotes, totalCoins);

        return (await GetByIdAsync(workPeriod.Id, cancellationToken).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public async Task<WorkPeriod> CloseWorkPeriodWithDenominationsAsync(
        CashDenominationCountDto closingCount,
        int userId,
        string? varianceExplanation = null,
        CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .Where(wp => wp.Status == WorkPeriodStatus.Open)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException("No work period is currently open.");
        }

        // Calculate totals from denomination count
        var (totalNotes, totalCoins, grandTotal) = await CalculateDenominationTotalsAsync(closingCount.Denominations, cancellationToken);

        // Calculate expected cash
        var expectedCash = await CalculateExpectedCashAsync(workPeriod.Id, cancellationToken).ConfigureAwait(false);

        // Get next Z-report number
        var lastZReport = await _context.WorkPeriods
            .Where(wp => wp.ZReportNumber.HasValue)
            .MaxAsync(wp => (int?)wp.ZReportNumber, cancellationToken)
            .ConfigureAwait(false);

        workPeriod.ClosedAt = DateTime.UtcNow;
        workPeriod.ClosedByUserId = userId;
        workPeriod.ClosingCash = grandTotal;
        workPeriod.ExpectedCash = expectedCash;
        workPeriod.Variance = grandTotal - expectedCash;
        workPeriod.Status = WorkPeriodStatus.Closed;
        workPeriod.ZReportNumber = (lastZReport ?? 0) + 1;

        // Add variance explanation to notes
        if (!string.IsNullOrWhiteSpace(varianceExplanation))
        {
            var varianceNote = $"Variance Explanation: {varianceExplanation.Trim()}";
            workPeriod.Notes = string.IsNullOrEmpty(workPeriod.Notes)
                ? varianceNote
                : $"{workPeriod.Notes}\n---\n{varianceNote}";
        }

        // Create the denomination count record
        var cashCount = await CreateCashCountAsync(
            workPeriod.Id,
            CashCountType.Closing,
            closingCount,
            userId,
            totalNotes,
            totalCoins,
            grandTotal,
            cancellationToken);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "WorkPeriodClosedWithDenominations",
            EntityType = nameof(WorkPeriod),
            EntityId = workPeriod.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                ClosingCash = grandTotal,
                TotalNotes = totalNotes,
                TotalCoins = totalCoins,
                ExpectedCash = expectedCash,
                Variance = workPeriod.Variance,
                VarianceExplanation = varianceExplanation,
                ZReportNumber = workPeriod.ZReportNumber,
                ClosedAt = workPeriod.ClosedAt
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Work period {WorkPeriodId} closed by user {UserId} with denominations. Expected: {Expected:C}, Actual: {Actual:C}, Variance: {Variance:C}",
            workPeriod.Id, userId, expectedCash, grandTotal, workPeriod.Variance);

        return workPeriod;
    }

    /// <inheritdoc />
    public async Task<CashCountResultDto> RecordMidShiftCountAsync(
        int workPeriodId,
        CashDenominationCountDto count,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId && wp.Status == WorkPeriodStatus.Open, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException($"Work period {workPeriodId} is not open or does not exist.");
        }

        var (totalNotes, totalCoins, grandTotal) = await CalculateDenominationTotalsAsync(count.Denominations, cancellationToken);

        var cashCount = await CreateCashCountAsync(
            workPeriodId,
            CashCountType.MidShift,
            count,
            userId,
            totalNotes,
            totalCoins,
            grandTotal,
            cancellationToken);

        _logger.Information(
            "Mid-shift cash count recorded for work period {WorkPeriodId}. Total: {Total:C}",
            workPeriodId, grandTotal);

        return await GetCashCountResultAsync(cashCount.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CashCountResultDto?> GetDenominationCountAsync(
        int workPeriodId,
        CashCountType countType,
        CancellationToken cancellationToken = default)
    {
        var cashCount = await _context.CashDenominationCounts
            .AsNoTracking()
            .Include(c => c.CountedByUser)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.Lines)
                .ThenInclude(l => l.Denomination)
            .Where(c => c.WorkPeriodId == workPeriodId && c.CountType == countType)
            .OrderByDescending(c => c.CountedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (cashCount is null)
            return null;

        return MapToCashCountResultDto(cashCount);
    }

    /// <inheritdoc />
    public async Task<List<CashCountResultDto>> GetAllDenominationCountsAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        var cashCounts = await _context.CashDenominationCounts
            .AsNoTracking()
            .Include(c => c.CountedByUser)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.Lines)
                .ThenInclude(l => l.Denomination)
            .Where(c => c.WorkPeriodId == workPeriodId)
            .OrderBy(c => c.CountedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return cashCounts.Select(MapToCashCountResultDto).ToList();
    }

    /// <inheritdoc />
    public async Task<FloatRecommendationDto> GetRecommendedFloatAsync(
        string currencyCode = "KES",
        CancellationToken cancellationToken = default)
    {
        // Get float recommendation from settings or use default
        var recommendedTotal = decimal.Parse(
            await GetSystemSettingAsync("RecommendedFloat", "15000", cancellationToken));

        // Standard recommended float composition for Kenya
        var recommendations = new Dictionary<decimal, int>
        {
            { 1000, 5 },   // 5 x KES 1,000 = 5,000
            { 500, 6 },    // 6 x KES 500 = 3,000
            { 200, 5 },    // 5 x KES 200 = 1,000
            { 100, 20 },   // 20 x KES 100 = 2,000
            { 50, 40 },    // 40 x KES 50 = 2,000
            { 20, 50 },    // 50 x KES 20 = 1,000
            { 10, 50 },    // 50 x KES 10 = 500
            { 5, 100 },    // 100 x KES 5 = 500
        };

        return new FloatRecommendationDto
        {
            TotalAmount = recommendedTotal,
            RecommendedDenominations = recommendations,
            Notes = "Standard float composition for adequate change. Adjust based on typical transaction patterns."
        };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyCashCountAsync(
        int cashCountId,
        int verifierUserId,
        CancellationToken cancellationToken = default)
    {
        var cashCount = await _context.CashDenominationCounts
            .FirstOrDefaultAsync(c => c.Id == cashCountId, cancellationToken)
            .ConfigureAwait(false);

        if (cashCount is null)
            return false;

        cashCount.VerifiedByUserId = verifierUserId;
        cashCount.VerifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Cash count {CashCountId} verified by user {VerifierUserId}",
            cashCountId, verifierUserId);

        return true;
    }

    #endregion

    #region Private Helpers

    private async Task<(decimal TotalNotes, decimal TotalCoins, decimal GrandTotal)> CalculateDenominationTotalsAsync(
        Dictionary<decimal, int> denominations,
        CancellationToken cancellationToken)
    {
        var activeDenominations = await _context.CashDenominations
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal totalNotes = 0;
        decimal totalCoins = 0;

        foreach (var (value, quantity) in denominations)
        {
            var denomination = activeDenominations.FirstOrDefault(d => d.Value == value);
            if (denomination is null) continue;

            var lineTotal = value * quantity;
            if (denomination.Type == DenominationType.Note)
                totalNotes += lineTotal;
            else
                totalCoins += lineTotal;
        }

        return (totalNotes, totalCoins, totalNotes + totalCoins);
    }

    private async Task<CashDenominationCount> CreateCashCountAsync(
        int workPeriodId,
        CashCountType countType,
        CashDenominationCountDto countDto,
        int userId,
        decimal totalNotes,
        decimal totalCoins,
        decimal grandTotal,
        CancellationToken cancellationToken)
    {
        var cashCount = new CashDenominationCount
        {
            WorkPeriodId = workPeriodId,
            CountType = countType,
            CountedByUserId = userId,
            CountedAt = DateTime.UtcNow,
            TotalNotes = totalNotes,
            TotalCoins = totalCoins,
            GrandTotal = grandTotal,
            Notes = countDto.Notes
        };

        await _context.CashDenominationCounts.AddAsync(cashCount, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create lines for each denomination
        var activeDenominations = await _context.CashDenominations
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var (value, quantity) in countDto.Denominations)
        {
            if (quantity <= 0) continue;

            var denomination = activeDenominations.FirstOrDefault(d => d.Value == value);
            if (denomination is null) continue;

            var line = new CashCountLine
            {
                CashDenominationCountId = cashCount.Id,
                DenominationId = denomination.Id,
                Quantity = quantity,
                LineTotal = value * quantity
            };

            await _context.CashCountLines.AddAsync(line, cancellationToken).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return cashCount;
    }

    private async Task<CashCountResultDto> GetCashCountResultAsync(int cashCountId, CancellationToken cancellationToken)
    {
        var cashCount = await _context.CashDenominationCounts
            .AsNoTracking()
            .Include(c => c.CountedByUser)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.Lines)
                .ThenInclude(l => l.Denomination)
            .FirstAsync(c => c.Id == cashCountId, cancellationToken)
            .ConfigureAwait(false);

        return MapToCashCountResultDto(cashCount);
    }

    private static CashCountResultDto MapToCashCountResultDto(CashDenominationCount cashCount)
    {
        return new CashCountResultDto
        {
            Id = cashCount.Id,
            WorkPeriodId = cashCount.WorkPeriodId,
            CountType = cashCount.CountType,
            CountedByUserId = cashCount.CountedByUserId,
            CountedByUserName = cashCount.CountedByUser?.FullName ?? "Unknown",
            CountedAt = cashCount.CountedAt,
            VerifiedByUserId = cashCount.VerifiedByUserId,
            VerifiedByUserName = cashCount.VerifiedByUser?.FullName,
            VerifiedAt = cashCount.VerifiedAt,
            TotalNotes = cashCount.TotalNotes,
            TotalCoins = cashCount.TotalCoins,
            GrandTotal = cashCount.GrandTotal,
            Notes = cashCount.Notes,
            Lines = cashCount.Lines.Select(l => new CashCountLineDto
            {
                DenominationId = l.DenominationId,
                Type = l.Denomination?.Type ?? DenominationType.Note,
                DenominationValue = l.Denomination?.Value ?? 0,
                DisplayName = l.Denomination?.DisplayName ?? "Unknown",
                Quantity = l.Quantity,
                LineTotal = l.LineTotal
            }).OrderBy(l => l.Type).ThenByDescending(l => l.DenominationValue).ToList()
        };
    }

    #endregion
}
