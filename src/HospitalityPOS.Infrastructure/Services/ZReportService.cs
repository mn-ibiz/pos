using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SkiaSharp;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for Z Report generation, management, and scheduling.
/// </summary>
public class ZReportService : IZReportService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly ITerminalSessionContext _terminalContext;

    public ZReportService(
        POSDbContext context,
        ILogger logger,
        IWorkPeriodService workPeriodService,
        ITerminalSessionContext terminalContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _terminalContext = terminalContext ?? throw new ArgumentNullException(nameof(terminalContext));
    }

    #region Preview & Validation

    public async Task<ZReportPreview> PreviewZReportAsync(int workPeriodId, CancellationToken ct = default)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");

        var preview = new ZReportPreview
        {
            WorkPeriodId = workPeriodId,
            PeriodStartDateTime = workPeriod.OpenedAt,
            PeriodEndDateTime = workPeriod.ClosedAt,
            OpenedByUserName = workPeriod.OpenedByUser?.FullName ?? "Unknown",
            Duration = (workPeriod.ClosedAt ?? DateTime.UtcNow) - workPeriod.OpenedAt,
            OpeningCash = workPeriod.OpeningFloat
        };

        // Get receipts for the work period
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.Order)
                .ThenInclude(o => o!.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p!.Category)
            .Where(r => r.WorkPeriodId == workPeriodId)
            .ToListAsync(ct);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();
        var refundedReceipts = receipts.Where(r => r.TotalAmount < 0).ToList();

        // Calculate sales summary
        preview.GrossSales = settledReceipts.Sum(r => r.Subtotal);
        preview.TotalDiscounts = settledReceipts.Sum(r => r.DiscountAmount);
        preview.NetSales = preview.GrossSales - preview.TotalDiscounts;
        preview.TotalTax = settledReceipts.Sum(r => r.TaxAmount);
        preview.TotalTips = settledReceipts.Sum(r => r.TipAmount);
        preview.TotalRefunds = Math.Abs(refundedReceipts.Sum(r => r.TotalAmount));
        preview.TotalVoids = voidedReceipts.Sum(r => r.TotalAmount);
        preview.GrandTotal = settledReceipts.Sum(r => r.TotalAmount);

        // Transaction stats
        preview.TransactionCount = settledReceipts.Count;
        preview.VoidCount = voidedReceipts.Count;
        preview.RefundCount = refundedReceipts.Count;
        preview.AverageTransactionValue = preview.TransactionCount > 0
            ? preview.GrandTotal / preview.TransactionCount
            : 0;

        // Cash calculations
        var cashPayments = settledReceipts
            .SelectMany(r => r.Payments)
            .Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId && p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, ct);

        preview.CashReceived = cashPayments;
        preview.CashPaidOut = cashPayouts;
        preview.ExpectedCash = preview.OpeningCash + preview.CashReceived - preview.CashPaidOut;

        // Category breakdown
        preview.CategorySales = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => new { Id = oi.Product?.CategoryId, Name = oi.Product?.Category?.Name ?? "Uncategorized" })
            .Select(g => new CategorySalesSummaryDto
            {
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                QuantitySold = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                GrossAmount = g.Sum(oi => oi.TotalAmount),
                NetAmount = g.Sum(oi => oi.TotalAmount - oi.DiscountAmount)
            })
            .OrderByDescending(c => c.NetAmount)
            .ToList();

        // Payment breakdown
        preview.PaymentSummaries = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => new { Id = p.PaymentMethodId, Name = p.PaymentMethod?.Name ?? "Unknown" })
            .Select(g => new PaymentSummaryDto
            {
                PaymentMethodId = g.Key.Id,
                PaymentMethodName = g.Key.Name,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // User breakdown
        preview.UserSales = settledReceipts
            .GroupBy(r => new { Id = r.OwnerId, Name = r.Owner?.FullName ?? "Unknown" })
            .Select(g => new UserSalesSummaryDto
            {
                UserId = g.Key.Id,
                UserName = g.Key.Name,
                TransactionCount = g.Count(),
                TotalSales = g.Sum(r => r.TotalAmount)
            })
            .OrderByDescending(u => u.TotalSales)
            .ToList();

        // Cashier session breakdown
        preview.CashierSessions = await BuildCashierSessionBreakdownAsync(workPeriodId, null, ct);

        // Validation
        var validation = await ValidateCanGenerateAsync(workPeriodId, ct);
        preview.ValidationIssues = validation.Issues;

        return preview;
    }

    public async Task<ZReportPreview> PreviewZReportForTerminalAsync(int workPeriodId, int terminalId, CancellationToken ct = default)
    {
        var terminal = await _context.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId, ct);

        if (terminal == null)
            throw new InvalidOperationException($"Terminal {terminalId} not found.");

        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.OpenedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");

        var preview = new ZReportPreview
        {
            WorkPeriodId = workPeriodId,
            TerminalId = terminalId,
            TerminalCode = terminal.Code,
            PeriodStartDateTime = workPeriod.OpenedAt,
            PeriodEndDateTime = workPeriod.ClosedAt,
            OpenedByUserName = workPeriod.OpenedByUser?.FullName ?? "Unknown",
            Duration = (workPeriod.ClosedAt ?? DateTime.UtcNow) - workPeriod.OpenedAt,
            OpeningCash = workPeriod.OpeningFloat
        };

        // Get receipts for the work period filtered by terminal
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.Order)
                .ThenInclude(o => o!.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p!.Category)
            .Where(r => r.WorkPeriodId == workPeriodId && r.TerminalId == terminalId)
            .ToListAsync(ct);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();
        var refundedReceipts = receipts.Where(r => r.TotalAmount < 0).ToList();

        // Calculate sales summary
        preview.GrossSales = settledReceipts.Sum(r => r.Subtotal);
        preview.TotalDiscounts = settledReceipts.Sum(r => r.DiscountAmount);
        preview.NetSales = preview.GrossSales - preview.TotalDiscounts;
        preview.TotalTax = settledReceipts.Sum(r => r.TaxAmount);
        preview.TotalTips = settledReceipts.Sum(r => r.TipAmount);
        preview.TotalRefunds = Math.Abs(refundedReceipts.Sum(r => r.TotalAmount));
        preview.TotalVoids = voidedReceipts.Sum(r => r.TotalAmount);
        preview.GrandTotal = settledReceipts.Sum(r => r.TotalAmount);

        // Transaction stats
        preview.TransactionCount = settledReceipts.Count;
        preview.VoidCount = voidedReceipts.Count;
        preview.RefundCount = refundedReceipts.Count;
        preview.AverageTransactionValue = preview.TransactionCount > 0
            ? preview.GrandTotal / preview.TransactionCount
            : 0;

        // Cash calculations
        var cashPayments = settledReceipts
            .SelectMany(r => r.Payments)
            .Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId && p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, ct);

        preview.CashReceived = cashPayments;
        preview.CashPaidOut = cashPayouts;
        preview.ExpectedCash = preview.OpeningCash + preview.CashReceived - preview.CashPaidOut;

        // Category breakdown
        preview.CategorySales = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => new { Id = oi.Product?.CategoryId, Name = oi.Product?.Category?.Name ?? "Uncategorized" })
            .Select(g => new CategorySalesSummaryDto
            {
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                QuantitySold = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                GrossAmount = g.Sum(oi => oi.TotalAmount),
                NetAmount = g.Sum(oi => oi.TotalAmount - oi.DiscountAmount)
            })
            .OrderByDescending(c => c.NetAmount)
            .ToList();

        // Payment breakdown
        preview.PaymentSummaries = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => new { Id = p.PaymentMethodId, Name = p.PaymentMethod?.Name ?? "Unknown" })
            .Select(g => new PaymentSummaryDto
            {
                PaymentMethodId = g.Key.Id,
                PaymentMethodName = g.Key.Name,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // User breakdown
        preview.UserSales = settledReceipts
            .GroupBy(r => new { Id = r.OwnerId, Name = r.Owner?.FullName ?? "Unknown" })
            .Select(g => new UserSalesSummaryDto
            {
                UserId = g.Key.Id,
                UserName = g.Key.Name,
                TransactionCount = g.Count(),
                TotalSales = g.Sum(r => r.TotalAmount)
            })
            .OrderByDescending(u => u.TotalSales)
            .ToList();

        // Cashier session breakdown for this terminal
        preview.CashierSessions = await BuildCashierSessionBreakdownAsync(workPeriodId, terminalId, ct);

        // Validation
        var validation = await ValidateCanGenerateForTerminalAsync(workPeriodId, terminalId, ct);
        preview.ValidationIssues = validation.Issues;

        return preview;
    }

    public async Task<ZReportValidationResult> ValidateCanGenerateAsync(int workPeriodId, CancellationToken ct = default)
    {
        var result = new ZReportValidationResult { CanGenerate = true };

        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
        {
            result.CanGenerate = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "WORK_PERIOD_NOT_FOUND",
                Message = "Work period not found.",
                IsBlocking = true
            });
            return result;
        }

        // Check if Z Report already exists for this work period
        var existingReport = await _context.Set<ZReportRecord>()
            .AnyAsync(z => z.WorkPeriodId == workPeriodId && z.IsFinalized, ct);

        if (existingReport)
        {
            result.CanGenerate = false;
            result.HasPreviousPendingReport = true;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "REPORT_EXISTS",
                Message = "A Z Report has already been generated for this work period.",
                IsBlocking = true
            });
            return result;
        }

        // Check for unsettled receipts
        var unsettledReceipts = await _context.Receipts
            .CountAsync(r => r.WorkPeriodId == workPeriodId &&
                           (r.Status == ReceiptStatus.Open || r.Status == ReceiptStatus.Pending), ct);

        if (unsettledReceipts > 0)
        {
            result.HasUnsettledReceipts = true;
            result.UnsettledReceiptCount = unsettledReceipts;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "UNSETTLED_RECEIPTS",
                Message = $"There are {unsettledReceipts} unsettled receipt(s). Please settle or void them before generating the Z Report.",
                IsBlocking = true,
                Resolution = "Go to the receipts view and settle or void all open receipts."
            });
        }

        // Check for open orders
        var openOrders = await _context.Orders
            .CountAsync(o => o.WorkPeriodId == workPeriodId &&
                            o.Status != OrderStatus.Completed &&
                            o.Status != OrderStatus.Cancelled, ct);

        if (openOrders > 0)
        {
            result.HasOpenOrders = true;
            result.OpenOrderCount = openOrders;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "OPEN_ORDERS",
                Message = $"There are {openOrders} open order(s). Please complete or cancel them before generating the Z Report.",
                IsBlocking = true,
                Resolution = "Go to the orders view and complete or cancel all open orders."
            });
        }

        // Cash count check (warning only if work period still open)
        if (workPeriod.Status == WorkPeriodStatus.Open)
        {
            result.IsCashCounted = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "CASH_NOT_COUNTED",
                Message = "Cash has not been counted yet. You will need to count cash when generating the Z Report.",
                IsBlocking = false,
                Resolution = "Count the cash drawer when generating the report."
            });
        }
        else
        {
            result.IsCashCounted = workPeriod.ClosingCash.HasValue;
        }

        result.CanGenerate = !result.Issues.Any(i => i.IsBlocking);
        return result;
    }

    public async Task<ZReportValidationResult> ValidateCanGenerateForTerminalAsync(int workPeriodId, int terminalId, CancellationToken ct = default)
    {
        var result = new ZReportValidationResult { CanGenerate = true };

        var terminal = await _context.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId, ct);

        if (terminal == null)
        {
            result.CanGenerate = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "TERMINAL_NOT_FOUND",
                Message = "Terminal not found.",
                IsBlocking = true
            });
            return result;
        }

        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
        {
            result.CanGenerate = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "WORK_PERIOD_NOT_FOUND",
                Message = "Work period not found.",
                IsBlocking = true
            });
            return result;
        }

        // Verify work period belongs to this terminal
        if (workPeriod.TerminalId != terminalId)
        {
            result.CanGenerate = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "WORK_PERIOD_TERMINAL_MISMATCH",
                Message = $"Work period {workPeriodId} does not belong to terminal {terminal.Code}.",
                IsBlocking = true
            });
            return result;
        }

        // Check if Z Report already exists for this terminal's work period
        var existingReport = await _context.Set<ZReportRecord>()
            .AnyAsync(z => z.WorkPeriodId == workPeriodId && z.TerminalId == terminalId && z.IsFinalized, ct);

        if (existingReport)
        {
            result.CanGenerate = false;
            result.HasPreviousPendingReport = true;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "REPORT_EXISTS",
                Message = $"A Z Report has already been generated for terminal {terminal.Code} for this work period.",
                IsBlocking = true
            });
            return result;
        }

        // Check for unsettled receipts on this terminal
        var unsettledReceipts = await _context.Receipts
            .CountAsync(r => r.WorkPeriodId == workPeriodId &&
                           r.TerminalId == terminalId &&
                           (r.Status == ReceiptStatus.Open || r.Status == ReceiptStatus.Pending), ct);

        if (unsettledReceipts > 0)
        {
            result.HasUnsettledReceipts = true;
            result.UnsettledReceiptCount = unsettledReceipts;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "UNSETTLED_RECEIPTS",
                Message = $"There are {unsettledReceipts} unsettled receipt(s) on terminal {terminal.Code}. Please settle or void them before generating the Z Report.",
                IsBlocking = true,
                Resolution = "Go to the receipts view and settle or void all open receipts."
            });
        }

        // Check for open orders on this terminal
        var openOrders = await _context.Orders
            .CountAsync(o => o.WorkPeriodId == workPeriodId &&
                            o.TerminalId == terminalId &&
                            o.Status != OrderStatus.Completed &&
                            o.Status != OrderStatus.Cancelled, ct);

        if (openOrders > 0)
        {
            result.HasOpenOrders = true;
            result.OpenOrderCount = openOrders;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "OPEN_ORDERS",
                Message = $"There are {openOrders} open order(s) on terminal {terminal.Code}. Please complete or cancel them before generating the Z Report.",
                IsBlocking = true,
                Resolution = "Go to the orders view and complete or cancel all open orders."
            });
        }

        // Cash count check (warning only if work period still open)
        if (workPeriod.Status == WorkPeriodStatus.Open)
        {
            result.IsCashCounted = false;
            result.Issues.Add(new ZReportValidationIssue
            {
                Code = "CASH_NOT_COUNTED",
                Message = "Cash has not been counted yet. You will need to count cash when generating the Z Report.",
                IsBlocking = false,
                Resolution = "Count the cash drawer when generating the report."
            });
        }
        else
        {
            result.IsCashCounted = workPeriod.ClosingCash.HasValue;
        }

        result.CanGenerate = !result.Issues.Any(i => i.IsBlocking);
        return result;
    }

    #endregion

    #region Generation

    public async Task<ZReportRecord> GenerateZReportAsync(
        int workPeriodId,
        decimal actualCashCounted,
        int generatedByUserId,
        string? varianceExplanation = null,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await ValidateCanGenerateAsync(workPeriodId, ct);
        if (!validation.CanGenerate)
        {
            var blockingIssues = string.Join("; ", validation.Issues.Where(i => i.IsBlocking).Select(i => i.Message));
            throw new InvalidOperationException($"Cannot generate Z Report: {blockingIssues}");
        }

        var workPeriod = await _context.WorkPeriods
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");

        var generatingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == generatedByUserId, ct);

        // If work period is still open, close it
        if (workPeriod.Status == WorkPeriodStatus.Open)
        {
            await _workPeriodService.CloseWorkPeriodAsync(actualCashCounted, generatedByUserId, varianceExplanation, ct);
            // Refresh work period
            workPeriod = await _context.WorkPeriods
                .Include(wp => wp.OpenedByUser)
                .Include(wp => wp.ClosedByUser)
                .FirstAsync(wp => wp.Id == workPeriodId, ct);
        }

        // Get business settings
        var businessName = await GetSystemSettingAsync("BusinessName", "Hospitality POS", ct);
        var businessAddress = await GetSystemSettingAsync("BusinessAddress", "", ct);
        var businessPhone = await GetSystemSettingAsync("BusinessPhone", "", ct);
        var taxId = await GetSystemSettingAsync("TaxId", "", ct);

        // Get next report number
        var reportNumber = await GetNextReportNumberAsync(null, ct);

        // Get terminal info from context or work period
        var terminalId = _terminalContext.CurrentTerminalId ?? workPeriod.TerminalId;
        var terminalCode = _terminalContext.CurrentTerminalCode ?? workPeriod.TerminalCode;

        // Generate formatted report number
        string? formattedReportNumber = null;
        if (terminalId.HasValue)
        {
            formattedReportNumber = await GenerateFormattedReportNumberAsync(terminalId.Value, reportNumber, ct);
        }

        // Calculate all report data
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
            .ToListAsync(ct);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();

        // Calculate expected cash
        var cashPayments = settledReceipts
            .SelectMany(r => r.Payments)
            .Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId && p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, ct);

        var expectedCash = workPeriod.OpeningFloat + cashPayments - cashPayouts;
        var cashVariance = actualCashCounted - expectedCash;

        // Check variance threshold
        var threshold = await GetVarianceThresholdAsync(null, ct);
        var varianceRequiresApproval = threshold != null &&
            (Math.Abs(cashVariance) >= threshold.AmountThreshold ||
             (threshold.PercentageThreshold.HasValue && expectedCash > 0 &&
              Math.Abs(cashVariance / expectedCash * 100) >= threshold.PercentageThreshold.Value));

        // Build cashier sessions breakdown
        var cashierSessions = await BuildCashierSessionBreakdownAsync(workPeriodId, terminalId, ct);
        var cashierSessionsJson = JsonSerializer.Serialize(cashierSessions);

        // Create Z Report record
        var zReportRecord = new ZReportRecord
        {
            ReportNumber = reportNumber,
            ReportNumberFormatted = formattedReportNumber,
            TerminalId = terminalId,
            TerminalCode = terminalCode,
            WorkPeriodId = workPeriodId,
            ReportDateTime = DateTime.UtcNow,
            PeriodStartDateTime = workPeriod.OpenedAt,
            PeriodEndDateTime = workPeriod.ClosedAt ?? DateTime.UtcNow,
            GeneratedByUserId = generatedByUserId,
            GeneratedByUserName = generatingUser?.FullName ?? "Unknown",

            // Sales Summary
            GrossSales = settledReceipts.Sum(r => r.Subtotal),
            TotalDiscounts = settledReceipts.Sum(r => r.DiscountAmount),
            NetSales = settledReceipts.Sum(r => r.Subtotal - r.DiscountAmount),
            TotalTax = settledReceipts.Sum(r => r.TaxAmount),
            TotalTips = settledReceipts.Sum(r => r.TipAmount),
            TotalVoids = voidedReceipts.Sum(r => r.TotalAmount),
            TotalRefunds = receipts.Where(r => r.TotalAmount < 0).Sum(r => Math.Abs(r.TotalAmount)),
            GrandTotal = settledReceipts.Sum(r => r.TotalAmount),

            // Cash Reconciliation
            OpeningCash = workPeriod.OpeningFloat,
            CashReceived = cashPayments,
            CashPaidOut = cashPayouts,
            ExpectedCash = expectedCash,
            ActualCash = actualCashCounted,
            CashVariance = cashVariance,
            VarianceExplanation = varianceExplanation,
            VarianceRequiresApproval = varianceRequiresApproval,

            // Transaction Statistics
            TransactionCount = settledReceipts.Count,
            CustomerCount = settledReceipts.Count, // Approximation
            AverageTransactionValue = settledReceipts.Count > 0
                ? settledReceipts.Sum(r => r.TotalAmount) / settledReceipts.Count
                : 0,
            VoidCount = voidedReceipts.Count,
            RefundCount = receipts.Count(r => r.TotalAmount < 0),
            DiscountCount = settledReceipts.Count(r => r.DiscountAmount > 0),

            // Business Info
            BusinessName = businessName,
            BusinessAddress = businessAddress,
            BusinessPhone = businessPhone,
            TaxId = taxId,

            // Finalization
            IsFinalized = true,
            FinalizedAt = DateTime.UtcNow
        };

        // Compute hash
        zReportRecord.ReportHash = zReportRecord.ComputeHash();

        // Store full report data as JSON
        var fullReportData = await BuildFullReportDataAsync(workPeriodId, settledReceipts, voidedReceipts, ct);
        zReportRecord.ReportDataJson = JsonSerializer.Serialize(fullReportData);
        zReportRecord.CashierSessionsJson = cashierSessionsJson;

        await _context.Set<ZReportRecord>().AddAsync(zReportRecord, ct);
        await _context.SaveChangesAsync(ct);

        // Add category sales breakdown
        var categorySales = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => new { Id = oi.Product?.CategoryId, Name = oi.Product?.Category?.Name ?? "Uncategorized" })
            .Select(g => new ZReportCategorySales
            {
                ZReportRecordId = zReportRecord.Id,
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                QuantitySold = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                GrossAmount = g.Sum(oi => oi.TotalAmount),
                DiscountAmount = g.Sum(oi => oi.DiscountAmount),
                NetAmount = g.Sum(oi => oi.TotalAmount - oi.DiscountAmount),
                TaxAmount = g.Sum(oi => oi.TaxAmount),
                CostAmount = g.Sum(oi => oi.Quantity * (oi.Product?.CostPrice ?? 0)),
                PercentageOfSales = zReportRecord.GrandTotal > 0
                    ? g.Sum(oi => oi.TotalAmount) / zReportRecord.GrandTotal * 100
                    : 0
            })
            .ToList();

        foreach (var cs in categorySales)
        {
            cs.GrossProfit = cs.NetAmount - cs.CostAmount;
        }

        await _context.Set<ZReportCategorySales>().AddRangeAsync(categorySales, ct);

        // Add payment breakdown
        var paymentSummaries = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => new
            {
                Id = p.PaymentMethodId,
                Name = p.PaymentMethod?.Name ?? "Unknown",
                Type = p.PaymentMethod?.Type.ToString() ?? "Unknown"
            })
            .Select(g => new ZReportPaymentSummary
            {
                ZReportRecordId = zReportRecord.Id,
                PaymentMethodId = g.Key.Id,
                PaymentMethodName = g.Key.Name,
                PaymentMethodType = g.Key.Type,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                RefundAmount = g.Where(p => p.Amount < 0).Sum(p => Math.Abs(p.Amount)),
                NetAmount = g.Sum(p => p.Amount),
                TipAmount = g.Sum(p => p.TipAmount),
                PercentageOfSales = zReportRecord.GrandTotal > 0
                    ? g.Sum(p => p.Amount) / zReportRecord.GrandTotal * 100
                    : 0
            })
            .ToList();

        await _context.Set<ZReportPaymentSummary>().AddRangeAsync(paymentSummaries, ct);

        // Add hourly breakdown
        var hourlySales = settledReceipts
            .GroupBy(r => (r.SettledAt ?? r.CreatedAt).ToLocalTime().Hour)
            .Select(g => new ZReportHourlySales
            {
                ZReportRecordId = zReportRecord.Id,
                Hour = g.Key,
                HourLabel = FormatHourLabel(g.Key),
                TransactionCount = g.Count(),
                CustomerCount = g.Count(),
                SalesAmount = g.Sum(r => r.TotalAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0
            })
            .ToList();

        // Mark peak hours
        var peakSales = hourlySales.Any() ? hourlySales.Max(h => h.SalesAmount) : 0;
        foreach (var hs in hourlySales)
        {
            hs.IsPeakHour = peakSales > 0 && hs.SalesAmount >= peakSales * 0.8m;
        }

        await _context.Set<ZReportHourlySales>().AddRangeAsync(hourlySales, ct);

        // Add user sales breakdown
        var userSales = settledReceipts
            .GroupBy(r => new { Id = r.OwnerId, Name = r.Owner?.FullName ?? "Unknown" })
            .Select(g => new ZReportUserSales
            {
                ZReportRecordId = zReportRecord.Id,
                UserId = g.Key.Id,
                UserName = g.Key.Name,
                TransactionCount = g.Count(),
                GrossSales = g.Sum(r => r.Subtotal),
                DiscountAmount = g.Sum(r => r.DiscountAmount),
                NetSales = g.Sum(r => r.TotalAmount),
                TipAmount = g.Sum(r => r.TipAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0,
                VoidCount = voidedReceipts.Count(r => r.OwnerId == g.Key.Id),
                VoidAmount = voidedReceipts.Where(r => r.OwnerId == g.Key.Id).Sum(r => r.TotalAmount),
                RefundCount = receipts.Count(r => r.OwnerId == g.Key.Id && r.TotalAmount < 0),
                RefundAmount = Math.Abs(receipts.Where(r => r.OwnerId == g.Key.Id && r.TotalAmount < 0).Sum(r => r.TotalAmount))
            })
            .ToList();

        await _context.Set<ZReportUserSales>().AddRangeAsync(userSales, ct);

        await _context.SaveChangesAsync(ct);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = generatedByUserId,
            Action = "ZReportGenerated",
            EntityType = nameof(ZReportRecord),
            EntityId = zReportRecord.Id,
            NewValues = JsonSerializer.Serialize(new
            {
                zReportRecord.ReportNumber,
                zReportRecord.GrandTotal,
                zReportRecord.TransactionCount,
                zReportRecord.CashVariance,
                zReportRecord.ReportHash
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, ct);
        await _context.SaveChangesAsync(ct);

        _logger.Information(
            "Z-Report #{ReportNumber} generated for work period {WorkPeriodId}. Total: {GrandTotal:C}, Variance: {Variance:C}",
            reportNumber, workPeriodId, zReportRecord.GrandTotal, cashVariance);

        return zReportRecord;
    }

    public async Task<ZReportRecord> GenerateZReportForTerminalAsync(
        int workPeriodId,
        int terminalId,
        decimal actualCashCounted,
        int generatedByUserId,
        string? varianceExplanation = null,
        CancellationToken ct = default)
    {
        // Validate for specific terminal
        var validation = await ValidateCanGenerateForTerminalAsync(workPeriodId, terminalId, ct);
        if (!validation.CanGenerate)
        {
            var blockingIssues = string.Join("; ", validation.Issues.Where(i => i.IsBlocking).Select(i => i.Message));
            throw new InvalidOperationException($"Cannot generate Z Report for terminal: {blockingIssues}");
        }

        var terminal = await _context.Terminals
            .AsNoTracking()
            .FirstAsync(t => t.Id == terminalId, ct);

        var workPeriod = await _context.WorkPeriods
            .Include(wp => wp.OpenedByUser)
            .Include(wp => wp.ClosedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, ct);

        if (workPeriod == null)
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");

        var generatingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == generatedByUserId, ct);

        // If work period is still open, close it
        if (workPeriod.Status == WorkPeriodStatus.Open)
        {
            await _workPeriodService.CloseWorkPeriodAsync(actualCashCounted, generatedByUserId, varianceExplanation, ct);
            workPeriod = await _context.WorkPeriods
                .Include(wp => wp.OpenedByUser)
                .Include(wp => wp.ClosedByUser)
                .FirstAsync(wp => wp.Id == workPeriodId, ct);
        }

        // Get business settings
        var businessName = await GetSystemSettingAsync("BusinessName", "Hospitality POS", ct);
        var businessAddress = await GetSystemSettingAsync("BusinessAddress", "", ct);
        var businessPhone = await GetSystemSettingAsync("BusinessPhone", "", ct);
        var taxId = await GetSystemSettingAsync("TaxId", "", ct);

        // Get next report number
        var reportNumber = await GetNextReportNumberAsync(null, ct);
        var formattedReportNumber = await GenerateFormattedReportNumberAsync(terminalId, reportNumber, ct);

        // Get receipts filtered by terminal
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
            .Where(r => r.WorkPeriodId == workPeriodId && r.TerminalId == terminalId)
            .ToListAsync(ct);

        var settledReceipts = receipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
        var voidedReceipts = receipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();

        // Calculate expected cash
        var cashPayments = settledReceipts
            .SelectMany(r => r.Payments)
            .Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        var cashPayouts = await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId && p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, ct);

        var expectedCash = workPeriod.OpeningFloat + cashPayments - cashPayouts;
        var cashVariance = actualCashCounted - expectedCash;

        // Check variance threshold
        var threshold = await GetVarianceThresholdAsync(null, ct);
        var varianceRequiresApproval = threshold != null &&
            (Math.Abs(cashVariance) >= threshold.AmountThreshold ||
             (threshold.PercentageThreshold.HasValue && expectedCash > 0 &&
              Math.Abs(cashVariance / expectedCash * 100) >= threshold.PercentageThreshold.Value));

        // Build cashier sessions breakdown for this terminal
        var cashierSessions = await BuildCashierSessionBreakdownAsync(workPeriodId, terminalId, ct);
        var cashierSessionsJson = JsonSerializer.Serialize(cashierSessions);

        // Create Z Report record
        var zReportRecord = new ZReportRecord
        {
            ReportNumber = reportNumber,
            ReportNumberFormatted = formattedReportNumber,
            TerminalId = terminalId,
            TerminalCode = terminal.Code,
            WorkPeriodId = workPeriodId,
            ReportDateTime = DateTime.UtcNow,
            PeriodStartDateTime = workPeriod.OpenedAt,
            PeriodEndDateTime = workPeriod.ClosedAt ?? DateTime.UtcNow,
            GeneratedByUserId = generatedByUserId,
            GeneratedByUserName = generatingUser?.FullName ?? "Unknown",

            // Sales Summary
            GrossSales = settledReceipts.Sum(r => r.Subtotal),
            TotalDiscounts = settledReceipts.Sum(r => r.DiscountAmount),
            NetSales = settledReceipts.Sum(r => r.Subtotal - r.DiscountAmount),
            TotalTax = settledReceipts.Sum(r => r.TaxAmount),
            TotalTips = settledReceipts.Sum(r => r.TipAmount),
            TotalVoids = voidedReceipts.Sum(r => r.TotalAmount),
            TotalRefunds = receipts.Where(r => r.TotalAmount < 0).Sum(r => Math.Abs(r.TotalAmount)),
            GrandTotal = settledReceipts.Sum(r => r.TotalAmount),

            // Cash Reconciliation
            OpeningCash = workPeriod.OpeningFloat,
            CashReceived = cashPayments,
            CashPaidOut = cashPayouts,
            ExpectedCash = expectedCash,
            ActualCash = actualCashCounted,
            CashVariance = cashVariance,
            VarianceExplanation = varianceExplanation,
            VarianceRequiresApproval = varianceRequiresApproval,

            // Transaction Statistics
            TransactionCount = settledReceipts.Count,
            CustomerCount = settledReceipts.Count,
            AverageTransactionValue = settledReceipts.Count > 0
                ? settledReceipts.Sum(r => r.TotalAmount) / settledReceipts.Count
                : 0,
            VoidCount = voidedReceipts.Count,
            RefundCount = receipts.Count(r => r.TotalAmount < 0),
            DiscountCount = settledReceipts.Count(r => r.DiscountAmount > 0),

            // Business Info
            BusinessName = businessName,
            BusinessAddress = businessAddress,
            BusinessPhone = businessPhone,
            TaxId = taxId,

            // Finalization
            IsFinalized = true,
            FinalizedAt = DateTime.UtcNow
        };

        // Compute hash
        zReportRecord.ReportHash = zReportRecord.ComputeHash();

        // Store full report data as JSON
        var fullReportData = await BuildFullReportDataAsync(workPeriodId, settledReceipts, voidedReceipts, ct);
        zReportRecord.ReportDataJson = JsonSerializer.Serialize(fullReportData);
        zReportRecord.CashierSessionsJson = cashierSessionsJson;

        await _context.Set<ZReportRecord>().AddAsync(zReportRecord, ct);
        await _context.SaveChangesAsync(ct);

        // Add category sales breakdown
        var categorySales = settledReceipts
            .SelectMany(r => r.Order?.OrderItems ?? [])
            .GroupBy(oi => new { Id = oi.Product?.CategoryId, Name = oi.Product?.Category?.Name ?? "Uncategorized" })
            .Select(g => new ZReportCategorySales
            {
                ZReportRecordId = zReportRecord.Id,
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                QuantitySold = (int)Math.Round(g.Sum(oi => oi.Quantity)),
                GrossAmount = g.Sum(oi => oi.TotalAmount),
                DiscountAmount = g.Sum(oi => oi.DiscountAmount),
                NetAmount = g.Sum(oi => oi.TotalAmount - oi.DiscountAmount),
                TaxAmount = g.Sum(oi => oi.TaxAmount),
                CostAmount = g.Sum(oi => oi.Quantity * (oi.Product?.CostPrice ?? 0)),
                PercentageOfSales = zReportRecord.GrandTotal > 0
                    ? g.Sum(oi => oi.TotalAmount) / zReportRecord.GrandTotal * 100
                    : 0
            })
            .ToList();

        foreach (var cs in categorySales)
        {
            cs.GrossProfit = cs.NetAmount - cs.CostAmount;
        }

        await _context.Set<ZReportCategorySales>().AddRangeAsync(categorySales, ct);

        // Add payment breakdown
        var paymentSummaries = settledReceipts
            .SelectMany(r => r.Payments)
            .GroupBy(p => new
            {
                Id = p.PaymentMethodId,
                Name = p.PaymentMethod?.Name ?? "Unknown",
                Type = p.PaymentMethod?.Type.ToString() ?? "Unknown"
            })
            .Select(g => new ZReportPaymentSummary
            {
                ZReportRecordId = zReportRecord.Id,
                PaymentMethodId = g.Key.Id,
                PaymentMethodName = g.Key.Name,
                PaymentMethodType = g.Key.Type,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                RefundAmount = g.Where(p => p.Amount < 0).Sum(p => Math.Abs(p.Amount)),
                NetAmount = g.Sum(p => p.Amount),
                TipAmount = g.Sum(p => p.TipAmount),
                PercentageOfSales = zReportRecord.GrandTotal > 0
                    ? g.Sum(p => p.Amount) / zReportRecord.GrandTotal * 100
                    : 0
            })
            .ToList();

        await _context.Set<ZReportPaymentSummary>().AddRangeAsync(paymentSummaries, ct);

        // Add user sales breakdown
        var userSales = settledReceipts
            .GroupBy(r => new { Id = r.OwnerId, Name = r.Owner?.FullName ?? "Unknown" })
            .Select(g => new ZReportUserSales
            {
                ZReportRecordId = zReportRecord.Id,
                UserId = g.Key.Id,
                UserName = g.Key.Name,
                TransactionCount = g.Count(),
                GrossSales = g.Sum(r => r.Subtotal),
                DiscountAmount = g.Sum(r => r.DiscountAmount),
                NetSales = g.Sum(r => r.TotalAmount),
                TipAmount = g.Sum(r => r.TipAmount),
                AverageTransaction = g.Count() > 0 ? g.Sum(r => r.TotalAmount) / g.Count() : 0,
                VoidCount = voidedReceipts.Count(r => r.OwnerId == g.Key.Id),
                VoidAmount = voidedReceipts.Where(r => r.OwnerId == g.Key.Id).Sum(r => r.TotalAmount),
                RefundCount = receipts.Count(r => r.OwnerId == g.Key.Id && r.TotalAmount < 0),
                RefundAmount = Math.Abs(receipts.Where(r => r.OwnerId == g.Key.Id && r.TotalAmount < 0).Sum(r => r.TotalAmount))
            })
            .ToList();

        await _context.Set<ZReportUserSales>().AddRangeAsync(userSales, ct);
        await _context.SaveChangesAsync(ct);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = generatedByUserId,
            Action = "ZReportGenerated",
            EntityType = nameof(ZReportRecord),
            EntityId = zReportRecord.Id,
            NewValues = JsonSerializer.Serialize(new
            {
                zReportRecord.ReportNumber,
                zReportRecord.ReportNumberFormatted,
                TerminalCode = terminal.Code,
                zReportRecord.GrandTotal,
                zReportRecord.TransactionCount,
                zReportRecord.CashVariance,
                zReportRecord.ReportHash
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, ct);
        await _context.SaveChangesAsync(ct);

        _logger.Information(
            "Z-Report #{ReportNumber} ({FormattedNumber}) generated for terminal {TerminalCode}, work period {WorkPeriodId}. Total: {GrandTotal:C}, Variance: {Variance:C}",
            reportNumber, formattedReportNumber, terminal.Code, workPeriodId, zReportRecord.GrandTotal, cashVariance);

        return zReportRecord;
    }

    public async Task<ZReportRecord> GenerateConsolidatedZReportAsync(
        int storeId,
        DateTime reportDate,
        int generatedByUserId,
        CancellationToken ct = default)
    {
        var startOfDay = reportDate.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        // Get all individual Z Reports for the date
        var individualReports = await _context.Set<ZReportRecord>()
            .Where(z => z.StoreId == storeId &&
                       z.ReportDateTime >= startOfDay &&
                       z.ReportDateTime <= endOfDay &&
                       z.IsFinalized &&
                       !z.IsConsolidated)
            .ToListAsync(ct);

        if (!individualReports.Any())
            throw new InvalidOperationException("No individual Z Reports found for consolidation.");

        var generatingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == generatedByUserId, ct);

        var businessName = await GetSystemSettingAsync("BusinessName", "Hospitality POS", ct);
        var businessAddress = await GetSystemSettingAsync("BusinessAddress", "", ct);

        var consolidatedReport = new ZReportRecord
        {
            ReportNumber = await GetNextReportNumberAsync(storeId, ct),
            StoreId = storeId,
            ReportDateTime = DateTime.UtcNow,
            PeriodStartDateTime = individualReports.Min(r => r.PeriodStartDateTime),
            PeriodEndDateTime = individualReports.Max(r => r.PeriodEndDateTime),
            GeneratedByUserId = generatedByUserId,
            GeneratedByUserName = generatingUser?.FullName ?? "Unknown",

            // Consolidated totals
            GrossSales = individualReports.Sum(r => r.GrossSales),
            NetSales = individualReports.Sum(r => r.NetSales),
            TotalRefunds = individualReports.Sum(r => r.TotalRefunds),
            TotalVoids = individualReports.Sum(r => r.TotalVoids),
            TotalDiscounts = individualReports.Sum(r => r.TotalDiscounts),
            TotalTax = individualReports.Sum(r => r.TotalTax),
            TotalTips = individualReports.Sum(r => r.TotalTips),
            GrandTotal = individualReports.Sum(r => r.GrandTotal),

            OpeningCash = individualReports.Sum(r => r.OpeningCash),
            CashReceived = individualReports.Sum(r => r.CashReceived),
            CashPaidOut = individualReports.Sum(r => r.CashPaidOut),
            ExpectedCash = individualReports.Sum(r => r.ExpectedCash),
            ActualCash = individualReports.Sum(r => r.ActualCash),
            CashVariance = individualReports.Sum(r => r.CashVariance),

            TransactionCount = individualReports.Sum(r => r.TransactionCount),
            CustomerCount = individualReports.Sum(r => r.CustomerCount),
            AverageTransactionValue = individualReports.Sum(r => r.TransactionCount) > 0
                ? individualReports.Sum(r => r.GrandTotal) / individualReports.Sum(r => r.TransactionCount)
                : 0,
            VoidCount = individualReports.Sum(r => r.VoidCount),
            RefundCount = individualReports.Sum(r => r.RefundCount),

            BusinessName = businessName,
            BusinessAddress = businessAddress,

            IsConsolidated = true,
            ConsolidatedFromReportIds = string.Join(",", individualReports.Select(r => r.Id)),
            IsFinalized = true,
            FinalizedAt = DateTime.UtcNow
        };

        consolidatedReport.ReportHash = consolidatedReport.ComputeHash();

        await _context.Set<ZReportRecord>().AddAsync(consolidatedReport, ct);
        await _context.SaveChangesAsync(ct);

        _logger.Information(
            "Consolidated Z-Report #{ReportNumber} generated for store {StoreId} on {Date}. Total: {GrandTotal:C}",
            consolidatedReport.ReportNumber, storeId, reportDate.ToShortDateString(), consolidatedReport.GrandTotal);

        return consolidatedReport;
    }

    public async Task<ZReport> ReconstructReportModelAsync(int reportId, CancellationToken ct = default)
    {
        var record = await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Include(z => z.CategorySales)
            .Include(z => z.PaymentSummaries)
            .Include(z => z.UserSales)
            .Include(z => z.HourlySales)
            .FirstOrDefaultAsync(z => z.Id == reportId, ct);

        if (record == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        return new ZReport
        {
            BusinessName = record.BusinessName,
            BusinessAddress = record.BusinessAddress,
            BusinessPhone = record.BusinessPhone ?? "",
            ZReportNumber = record.ReportNumber,
            WorkPeriodId = record.WorkPeriodId,
            WorkPeriodOpenedAt = record.PeriodStartDateTime,
            WorkPeriodClosedAt = record.PeriodEndDateTime,
            WorkPeriodOpenedBy = "N/A", // Would need to query work period
            WorkPeriodClosedBy = record.GeneratedByUserName,
            Duration = record.PeriodEndDateTime - record.PeriodStartDateTime,
            GrossSales = record.GrossSales,
            TotalDiscounts = record.TotalDiscounts,
            NetSales = record.NetSales,
            TaxCollected = record.TotalTax,
            GrandTotal = record.GrandTotal,
            SalesByCategory = record.CategorySales.Select(c => new CategorySalesSummary
            {
                CategoryName = c.CategoryName,
                ItemCount = c.QuantitySold,
                TotalAmount = c.NetAmount
            }).ToList(),
            SalesByPaymentMethod = record.PaymentSummaries.Select(p => new PaymentMethodSummary
            {
                PaymentMethod = p.PaymentMethodName,
                TransactionCount = p.TransactionCount,
                TotalAmount = p.TotalAmount
            }).ToList(),
            SalesByUser = record.UserSales.Select(u => new UserSalesSummary
            {
                UserName = u.UserName,
                TransactionCount = u.TransactionCount,
                TotalAmount = u.NetSales,
                AverageTransaction = u.AverageTransaction
            }).ToList(),
            TransactionCount = record.TransactionCount,
            AverageTransactionValue = record.AverageTransactionValue,
            VoidCount = record.VoidCount,
            VoidTotal = record.TotalVoids,
            OpeningFloat = record.OpeningCash,
            CashSales = record.CashReceived,
            CashPayouts = record.CashPaidOut,
            ExpectedCash = record.ExpectedCash,
            ActualCash = record.ActualCash,
            Variance = record.CashVariance
        };
    }

    #endregion

    #region Retrieval

    public async Task<ZReportRecord?> GetZReportAsync(int reportId, CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Include(z => z.CategorySales)
            .Include(z => z.PaymentSummaries)
            .Include(z => z.UserSales)
            .Include(z => z.HourlySales)
            .FirstOrDefaultAsync(z => z.Id == reportId, ct);
    }

    public async Task<ZReportRecord?> GetZReportByNumberAsync(int reportNumber, int? storeId = null, CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Include(z => z.CategorySales)
            .Include(z => z.PaymentSummaries)
            .FirstOrDefaultAsync(z => z.ReportNumber == reportNumber &&
                                      (storeId == null || z.StoreId == storeId), ct);
    }

    public async Task<ZReportRecord?> GetZReportByWorkPeriodAsync(int workPeriodId, CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.WorkPeriodId == workPeriodId && z.IsFinalized, ct);
    }

    public async Task<IReadOnlyList<ZReportRecord>> GetZReportsAsync(ZReportFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildFilterQuery(filter);

        return await query
            .OrderByDescending(z => z.ReportDateTime)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ZReportSummaryDto>> GetZReportSummariesAsync(ZReportFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildFilterQuery(filter);

        return await query
            .OrderByDescending(z => z.ReportDateTime)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(z => new ZReportSummaryDto
            {
                Id = z.Id,
                ReportNumber = z.ReportNumber,
                ReportNumberFormatted = z.ReportNumberFormatted,
                TerminalId = z.TerminalId,
                TerminalCode = z.TerminalCode,
                ReportDateTime = z.ReportDateTime,
                PeriodStartDateTime = z.PeriodStartDateTime,
                PeriodEndDateTime = z.PeriodEndDateTime,
                GeneratedByUserName = z.GeneratedByUserName,
                GrandTotal = z.GrandTotal,
                TransactionCount = z.TransactionCount,
                CashVariance = z.CashVariance,
                VarianceStatus = z.CashVariance < 0 ? "SHORT" : (z.CashVariance > 0 ? "OVER" : "EXACT"),
                IsConsolidated = z.IsConsolidated
            })
            .ToListAsync(ct);
    }

    public async Task<ZReportRecord?> GetMostRecentZReportAsync(int? storeId = null, int? terminalId = null, CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Where(z => z.IsFinalized)
            .Where(z => storeId == null || z.StoreId == storeId)
            .Where(z => terminalId == null || z.TerminalId == terminalId)
            .OrderByDescending(z => z.ReportDateTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> GetNextReportNumberAsync(int? storeId = null, CancellationToken ct = default)
    {
        var lastNumber = await _context.Set<ZReportRecord>()
            .Where(z => storeId == null || z.StoreId == storeId)
            .MaxAsync(z => (int?)z.ReportNumber, ct);

        return (lastNumber ?? 0) + 1;
    }

    public async Task<int> GetZReportCountAsync(ZReportFilterDto filter, CancellationToken ct = default)
    {
        return await BuildFilterQuery(filter).CountAsync(ct);
    }

    private IQueryable<ZReportRecord> BuildFilterQuery(ZReportFilterDto filter)
    {
        var query = _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Where(z => z.IsFinalized);

        if (filter.StartDate.HasValue)
            query = query.Where(z => z.ReportDateTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(z => z.ReportDateTime <= filter.EndDate.Value);

        if (filter.StoreId.HasValue)
            query = query.Where(z => z.StoreId == filter.StoreId.Value);

        if (filter.TerminalId.HasValue)
            query = query.Where(z => z.TerminalId == filter.TerminalId.Value);

        if (filter.GeneratedByUserId.HasValue)
            query = query.Where(z => z.GeneratedByUserId == filter.GeneratedByUserId.Value);

        if (filter.HasVariance.HasValue)
        {
            if (filter.HasVariance.Value)
                query = query.Where(z => z.CashVariance != 0);
            else
                query = query.Where(z => z.CashVariance == 0);
        }

        if (filter.IsConsolidated.HasValue)
            query = query.Where(z => z.IsConsolidated == filter.IsConsolidated.Value);

        return query;
    }

    #endregion

    #region Export

    public async Task<byte[]> ExportToPdfAsync(int reportId, CancellationToken ct = default)
    {
        var report = await GetZReportAsync(reportId, ct);
        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        // Generate PDF using SkiaSharp
        const int pageWidth = 595;  // A4 width in points
        const int pageHeight = 842; // A4 height in points
        const int margin = 40;
        const int contentWidth = pageWidth - (2 * margin);

        using var surface = SKSurface.Create(new SKImageInfo(pageWidth, pageHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var headerPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 11,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
        };

        using var linePaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true
        };

        var y = margin + 30;

        // Title
        canvas.DrawText($"Z REPORT #{report.ReportNumber}", margin, y, titlePaint);
        y += 35;

        // Date and period
        canvas.DrawText($"Generated: {report.ReportDateTime:yyyy-MM-dd HH:mm}", margin, y, textPaint);
        y += 18;
        canvas.DrawText($"Period: {report.PeriodStartDateTime:yyyy-MM-dd HH:mm} - {report.PeriodEndDateTime:yyyy-MM-dd HH:mm}", margin, y, textPaint);
        y += 25;

        // Line separator
        canvas.DrawLine(margin, y, pageWidth - margin, y, linePaint);
        y += 20;

        // Sales Summary Section
        canvas.DrawText("SALES SUMMARY", margin, y, headerPaint);
        y += 20;

        var salesData = new[]
        {
            ("Gross Sales", report.GrossSales),
            ("Total Discounts", report.TotalDiscounts),
            ("Net Sales", report.NetSales),
            ("Tax Collected", report.TotalTax),
            ("Tips", report.TotalTips),
            ("Grand Total", report.GrandTotal)
        };

        foreach (var (label, value) in salesData)
        {
            canvas.DrawText(label, margin, y, textPaint);
            canvas.DrawText($"KES {value:N2}", pageWidth - margin - 100, y, textPaint);
            y += 16;
        }
        y += 10;

        // Line separator
        canvas.DrawLine(margin, y, pageWidth - margin, y, linePaint);
        y += 20;

        // Cash Reconciliation Section
        canvas.DrawText("CASH RECONCILIATION", margin, y, headerPaint);
        y += 20;

        var cashData = new[]
        {
            ("Opening Cash", report.OpeningCash),
            ("Cash Received", report.CashReceived),
            ("Cash Paid Out", report.CashPaidOut),
            ("Expected Cash", report.ExpectedCash),
            ("Actual Cash", report.ActualCash),
            ("Variance", report.CashVariance)
        };

        foreach (var (label, value) in cashData)
        {
            canvas.DrawText(label, margin, y, textPaint);
            var valueText = $"KES {value:N2}";
            if (label == "Variance" && value != 0)
            {
                using var variancePaint = new SKPaint
                {
                    Color = value > 0 ? SKColors.Green : SKColors.Red,
                    TextSize = 11,
                    IsAntialias = true
                };
                canvas.DrawText(valueText, pageWidth - margin - 100, y, variancePaint);
            }
            else
            {
                canvas.DrawText(valueText, pageWidth - margin - 100, y, textPaint);
            }
            y += 16;
        }
        y += 10;

        // Line separator
        canvas.DrawLine(margin, y, pageWidth - margin, y, linePaint);
        y += 20;

        // Transaction Counts
        canvas.DrawText("TRANSACTION COUNTS", margin, y, headerPaint);
        y += 20;

        canvas.DrawText($"Total Receipts: {report.TotalReceipts}", margin, y, textPaint);
        y += 16;
        canvas.DrawText($"Paid Receipts: {report.PaidReceipts}", margin, y, textPaint);
        y += 16;
        canvas.DrawText($"Voided Receipts: {report.VoidedReceipts}", margin, y, textPaint);
        y += 16;
        canvas.DrawText($"Total Items Sold: {report.TotalItemsSold}", margin, y, textPaint);
        y += 25;

        // Footer
        canvas.DrawLine(margin, pageHeight - 60, pageWidth - margin, pageHeight - 60, linePaint);
        using var footerPaint = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 9,
            IsAntialias = true
        };
        canvas.DrawText($"Generated by HospitalityPOS - {DateTime.Now:yyyy-MM-dd HH:mm:ss}", margin, pageHeight - 40, footerPaint);

        // Export to PDF bytes
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ExportToExcelAsync(int reportId, CancellationToken ct = default)
    {
        var report = await GetZReportAsync(reportId, ct);
        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        var csv = new StringBuilder();
        csv.AppendLine("Z REPORT EXPORT");
        csv.AppendLine($"Report Number,{report.ReportNumber}");
        csv.AppendLine($"Report Date,{report.ReportDateTime:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Period Start,{report.PeriodStartDateTime:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Period End,{report.PeriodEndDateTime:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();
        csv.AppendLine("SALES SUMMARY");
        csv.AppendLine($"Gross Sales,{report.GrossSales:F2}");
        csv.AppendLine($"Discounts,{report.TotalDiscounts:F2}");
        csv.AppendLine($"Net Sales,{report.NetSales:F2}");
        csv.AppendLine($"Tax Collected,{report.TotalTax:F2}");
        csv.AppendLine($"Tips,{report.TotalTips:F2}");
        csv.AppendLine($"Grand Total,{report.GrandTotal:F2}");
        csv.AppendLine();
        csv.AppendLine("CASH RECONCILIATION");
        csv.AppendLine($"Opening Cash,{report.OpeningCash:F2}");
        csv.AppendLine($"Cash Received,{report.CashReceived:F2}");
        csv.AppendLine($"Cash Paid Out,{report.CashPaidOut:F2}");
        csv.AppendLine($"Expected Cash,{report.ExpectedCash:F2}");
        csv.AppendLine($"Actual Cash,{report.ActualCash:F2}");
        csv.AppendLine($"Variance,{report.CashVariance:F2}");
        csv.AppendLine();
        csv.AppendLine("CATEGORY SALES");
        csv.AppendLine("Category,Quantity,Gross,Net");
        foreach (var cat in report.CategorySales)
        {
            csv.AppendLine($"{cat.CategoryName},{cat.QuantitySold},{cat.GrossAmount:F2},{cat.NetAmount:F2}");
        }
        csv.AppendLine();
        csv.AppendLine("PAYMENT METHODS");
        csv.AppendLine("Method,Count,Amount");
        foreach (var pay in report.PaymentSummaries)
        {
            csv.AppendLine($"{pay.PaymentMethodName},{pay.TransactionCount},{pay.TotalAmount:F2}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportToCsvAsync(int reportId, CancellationToken ct = default)
    {
        return await ExportToExcelAsync(reportId, ct);
    }

    public async Task<string> GenerateHtmlReportAsync(int reportId, CancellationToken ct = default)
    {
        var report = await GetZReportAsync(reportId, ct);
        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine("<title>Z Report #" + report.ReportNumber + "</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: 'Courier New', monospace; max-width: 400px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { text-align: center; border-bottom: 2px dashed #000; padding-bottom: 10px; }");
        html.AppendLine(".section { margin: 15px 0; }");
        html.AppendLine(".section-title { font-weight: bold; border-bottom: 1px solid #000; margin-bottom: 5px; }");
        html.AppendLine(".row { display: flex; justify-content: space-between; }");
        html.AppendLine(".total { font-weight: bold; font-size: 1.2em; border-top: 2px solid #000; margin-top: 10px; padding-top: 5px; }");
        html.AppendLine(".variance-short { color: red; }");
        html.AppendLine(".variance-over { color: green; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; }");
        html.AppendLine("th, td { text-align: left; padding: 2px 0; }");
        html.AppendLine("th:last-child, td:last-child { text-align: right; }");
        html.AppendLine("</style>");
        html.AppendLine("</head><body>");

        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine($"<h2>{report.BusinessName}</h2>");
        if (!string.IsNullOrEmpty(report.BusinessAddress))
            html.AppendLine($"<p>{report.BusinessAddress}</p>");
        html.AppendLine($"<h3>Z REPORT #{report.ReportNumber}</h3>");
        html.AppendLine($"<p>Generated: {report.ReportDateTime:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("</div>");

        // Period Info
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>PERIOD INFORMATION</div>");
        html.AppendLine($"<div class='row'><span>Start:</span><span>{report.PeriodStartDateTime:yyyy-MM-dd HH:mm}</span></div>");
        html.AppendLine($"<div class='row'><span>End:</span><span>{report.PeriodEndDateTime:yyyy-MM-dd HH:mm}</span></div>");
        html.AppendLine($"<div class='row'><span>Duration:</span><span>{(report.PeriodEndDateTime - report.PeriodStartDateTime):hh\\:mm}</span></div>");
        html.AppendLine($"<div class='row'><span>Generated By:</span><span>{report.GeneratedByUserName}</span></div>");
        html.AppendLine("</div>");

        // Sales Summary
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>SALES SUMMARY</div>");
        html.AppendLine($"<div class='row'><span>Gross Sales:</span><span>{report.GrossSales:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Discounts:</span><span>-{report.TotalDiscounts:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Net Sales:</span><span>{report.NetSales:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Tax:</span><span>{report.TotalTax:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Tips:</span><span>{report.TotalTips:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Refunds:</span><span>-{report.TotalRefunds:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Voids:</span><span>-{report.TotalVoids:N2}</span></div>");
        html.AppendLine($"<div class='row total'><span>GRAND TOTAL:</span><span>{report.GrandTotal:N2}</span></div>");
        html.AppendLine("</div>");

        // Transaction Stats
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>TRANSACTION STATISTICS</div>");
        html.AppendLine($"<div class='row'><span>Transactions:</span><span>{report.TransactionCount}</span></div>");
        html.AppendLine($"<div class='row'><span>Average:</span><span>{report.AverageTransactionValue:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Voids:</span><span>{report.VoidCount}</span></div>");
        html.AppendLine($"<div class='row'><span>Refunds:</span><span>{report.RefundCount}</span></div>");
        html.AppendLine("</div>");

        // Cash Reconciliation
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>CASH RECONCILIATION</div>");
        html.AppendLine($"<div class='row'><span>Opening Cash:</span><span>{report.OpeningCash:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Cash Received:</span><span>+{report.CashReceived:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Cash Paid Out:</span><span>-{report.CashPaidOut:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Expected Cash:</span><span>{report.ExpectedCash:N2}</span></div>");
        html.AppendLine($"<div class='row'><span>Actual Cash:</span><span>{report.ActualCash:N2}</span></div>");
        var varianceClass = report.CashVariance < 0 ? "variance-short" : (report.CashVariance > 0 ? "variance-over" : "");
        html.AppendLine($"<div class='row {varianceClass}'><span>Variance:</span><span>{report.CashVariance:N2} ({report.VarianceStatus})</span></div>");
        html.AppendLine("</div>");

        // Category Sales
        if (report.CategorySales.Any())
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>SALES BY CATEGORY</div>");
            html.AppendLine("<table><thead><tr><th>Category</th><th>Qty</th><th>Amount</th></tr></thead><tbody>");
            foreach (var cat in report.CategorySales.OrderByDescending(c => c.NetAmount))
            {
                html.AppendLine($"<tr><td>{cat.CategoryName}</td><td>{cat.QuantitySold}</td><td>{cat.NetAmount:N2}</td></tr>");
            }
            html.AppendLine("</tbody></table></div>");
        }

        // Payment Methods
        if (report.PaymentSummaries.Any())
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>PAYMENT METHODS</div>");
            html.AppendLine("<table><thead><tr><th>Method</th><th>#</th><th>Amount</th></tr></thead><tbody>");
            foreach (var pay in report.PaymentSummaries.OrderByDescending(p => p.TotalAmount))
            {
                html.AppendLine($"<tr><td>{pay.PaymentMethodName}</td><td>{pay.TransactionCount}</td><td>{pay.TotalAmount:N2}</td></tr>");
            }
            html.AppendLine("</tbody></table></div>");
        }

        // User Sales
        if (report.UserSales.Any())
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>SALES BY USER</div>");
            html.AppendLine("<table><thead><tr><th>User</th><th>#</th><th>Amount</th></tr></thead><tbody>");
            foreach (var user in report.UserSales.OrderByDescending(u => u.NetSales))
            {
                html.AppendLine($"<tr><td>{user.UserName}</td><td>{user.TransactionCount}</td><td>{user.NetSales:N2}</td></tr>");
            }
            html.AppendLine("</tbody></table></div>");
        }

        // Footer
        html.AppendLine("<div class='section' style='text-align: center; border-top: 2px dashed #000; margin-top: 20px; padding-top: 10px;'>");
        html.AppendLine($"<p>Report Hash: {report.ReportHash[..16]}...</p>");
        html.AppendLine("<p>*** END OF Z REPORT ***</p>");
        html.AppendLine("</div>");

        html.AppendLine("</body></html>");

        return html.ToString();
    }

    public async Task<string> GenerateReceiptFormatAsync(int reportId, CancellationToken ct = default)
    {
        var report = await GetZReportAsync(reportId, ct);
        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        var sb = new StringBuilder();
        var width = 40;

        void AddLine(string text = "") => sb.AppendLine(text.PadRight(width));
        void AddCentered(string text) => sb.AppendLine(text.PadLeft((width + text.Length) / 2).PadRight(width));
        void AddRow(string left, string right) => sb.AppendLine($"{left}{right.PadLeft(width - left.Length)}");
        void AddDivider(char c = '-') => sb.AppendLine(new string(c, width));

        AddCentered(report.BusinessName);
        if (!string.IsNullOrEmpty(report.BusinessAddress))
            AddCentered(report.BusinessAddress);
        AddLine();
        AddDivider('=');
        AddCentered($"Z REPORT #{report.ReportNumber}");
        AddDivider('=');
        AddRow("Date:", report.ReportDateTime.ToString("yyyy-MM-dd HH:mm"));
        AddRow("Period:", $"{report.PeriodStartDateTime:HH:mm} - {report.PeriodEndDateTime:HH:mm}");
        AddRow("By:", report.GeneratedByUserName);
        AddDivider();

        AddCentered("SALES SUMMARY");
        AddDivider();
        AddRow("Gross Sales:", report.GrossSales.ToString("N2"));
        AddRow("Discounts:", $"-{report.TotalDiscounts:N2}");
        AddRow("Net Sales:", report.NetSales.ToString("N2"));
        AddRow("Tax:", report.TotalTax.ToString("N2"));
        AddRow("Tips:", report.TotalTips.ToString("N2"));
        AddDivider('=');
        AddRow("GRAND TOTAL:", report.GrandTotal.ToString("N2"));
        AddDivider('=');

        AddLine();
        AddCentered("CASH RECONCILIATION");
        AddDivider();
        AddRow("Opening:", report.OpeningCash.ToString("N2"));
        AddRow("Cash In:", $"+{report.CashReceived:N2}");
        AddRow("Cash Out:", $"-{report.CashPaidOut:N2}");
        AddRow("Expected:", report.ExpectedCash.ToString("N2"));
        AddRow("Actual:", report.ActualCash.ToString("N2"));
        AddRow("Variance:", $"{report.CashVariance:N2} {report.VarianceStatus}");
        AddDivider();

        AddLine();
        AddCentered("TRANSACTIONS");
        AddDivider();
        AddRow("Count:", report.TransactionCount.ToString());
        AddRow("Average:", report.AverageTransactionValue.ToString("N2"));
        AddRow("Voids:", report.VoidCount.ToString());
        AddRow("Refunds:", report.RefundCount.ToString());
        AddDivider();

        AddLine();
        AddDivider('=');
        AddCentered("*** END OF Z REPORT ***");
        AddLine();

        return sb.ToString();
    }

    #endregion

    #region Printing & Email

    public async Task PrintZReportAsync(int reportId, string? printerName = null, CancellationToken ct = default)
    {
        var receiptText = await GenerateReceiptFormatAsync(reportId, ct);
        // In real implementation, send to receipt printer
        _logger.Information("Z Report {ReportId} sent to printer {PrinterName}", reportId, printerName ?? "default");
    }

    public async Task EmailZReportAsync(
        int reportId,
        string[] recipients,
        string? subject = null,
        string? message = null,
        CancellationToken ct = default)
    {
        var report = await GetZReportAsync(reportId, ct);
        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        // In real implementation, use email service
        _logger.Information(
            "Z Report #{ReportNumber} emailed to {Recipients}",
            report.ReportNumber,
            string.Join(", ", recipients));
    }

    #endregion

    #region Scheduling

    public async Task<ZReportSchedule?> GetScheduleAsync(int? storeId = null, int? terminalId = null, CancellationToken ct = default)
    {
        return await _context.Set<ZReportSchedule>()
            .FirstOrDefaultAsync(s =>
                (storeId == null || s.StoreId == storeId) &&
                (terminalId == null || s.TerminalId == terminalId), ct);
    }

    public async Task<ZReportSchedule> UpdateScheduleAsync(int? storeId, ZReportScheduleDto schedule, CancellationToken ct = default)
    {
        var existing = await GetScheduleAsync(storeId, null, ct);

        if (existing == null)
        {
            existing = new ZReportSchedule
            {
                StoreId = storeId
            };
            await _context.Set<ZReportSchedule>().AddAsync(existing, ct);
        }

        existing.ScheduledTime = schedule.ScheduledTime;
        existing.IsEnabled = schedule.IsEnabled;
        existing.SendEmailNotification = schedule.SendEmailNotification;
        existing.NotificationEmails = string.Join(",", schedule.NotificationEmails);
        existing.MinutesWarningBefore = schedule.MinutesWarningBefore;
        existing.AutoCloseWorkPeriod = schedule.AutoCloseWorkPeriod;
        existing.EmailReportAfterGeneration = schedule.EmailReportAfterGeneration;
        existing.ReportRecipientEmails = string.Join(",", schedule.ReportRecipientEmails);

        // Set active days
        existing.ActiveDaysOfWeek = 0;
        foreach (var day in schedule.ActiveDays)
        {
            existing.SetActiveOnDay(day, true);
        }

        // Calculate next execution
        existing.NextExecutionAt = CalculateNextExecution(existing);

        await _context.SaveChangesAsync(ct);

        _logger.Information("Z Report schedule updated for store {StoreId}", storeId);

        return existing;
    }

    public async Task<int> TriggerScheduledZReportsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var schedules = await _context.Set<ZReportSchedule>()
            .Where(s => s.IsEnabled && s.NextExecutionAt <= now)
            .ToListAsync(ct);

        var count = 0;
        foreach (var schedule in schedules)
        {
            try
            {
                // Find open work period
                var workPeriod = await _context.WorkPeriods
                    .Where(wp => wp.Status == WorkPeriodStatus.Open)
                    .FirstOrDefaultAsync(ct);

                if (workPeriod != null)
                {
                    if (schedule.AutoCloseWorkPeriod)
                    {
                        // Use estimated cash as actual (in real scenario, this would need manual input)
                        var expectedCash = await _workPeriodService.CalculateExpectedCashAsync(workPeriod.Id, ct);
                        await GenerateZReportAsync(
                            workPeriod.Id,
                            expectedCash,
                            1, // System user
                            "Auto-generated by scheduled Z Report",
                            ct);
                        count++;

                        schedule.LastExecutionResult = "Success";
                    }
                    else
                    {
                        schedule.LastExecutionResult = "Skipped - work period still open and AutoCloseWorkPeriod is disabled";
                    }
                }
                else
                {
                    schedule.LastExecutionResult = "Skipped - no open work period";
                }
            }
            catch (Exception ex)
            {
                schedule.LastExecutionResult = $"Error: {ex.Message}";
                _logger.Error(ex, "Failed to execute scheduled Z Report for schedule {ScheduleId}", schedule.Id);
            }

            schedule.LastExecutedAt = now;
            schedule.NextExecutionAt = CalculateNextExecution(schedule);
        }

        await _context.SaveChangesAsync(ct);
        return count;
    }

    public async Task SendScheduleWarningNotificationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var schedules = await _context.Set<ZReportSchedule>()
            .Where(s => s.IsEnabled &&
                       s.SendEmailNotification &&
                       s.NextExecutionAt.HasValue &&
                       s.NextExecutionAt.Value <= now.AddMinutes(s.MinutesWarningBefore))
            .ToListAsync(ct);

        foreach (var schedule in schedules)
        {
            var emails = schedule.GetNotificationEmails();
            if (emails.Any())
            {
                // In real implementation, send warning emails
                _logger.Information(
                    "Z Report warning notification sent for schedule {ScheduleId} to {Recipients}",
                    schedule.Id,
                    string.Join(", ", emails));
            }
        }
    }

    public async Task<IReadOnlyList<ZReportSchedule>> GetActiveSchedulesAsync(CancellationToken ct = default)
    {
        return await _context.Set<ZReportSchedule>()
            .Where(s => s.IsEnabled)
            .ToListAsync(ct);
    }

    public async Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default)
    {
        var schedule = await _context.Set<ZReportSchedule>()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule != null)
        {
            _context.Set<ZReportSchedule>().Remove(schedule);
            await _context.SaveChangesAsync(ct);
        }
    }

    private DateTime? CalculateNextExecution(ZReportSchedule schedule)
    {
        if (!schedule.IsEnabled)
            return null;

        var now = DateTime.UtcNow;
        var today = now.Date;
        var scheduledToday = today.Add(schedule.ScheduledTime);

        // Start checking from today
        for (int i = 0; i < 8; i++) // Check up to a week ahead
        {
            var checkDate = today.AddDays(i);
            var scheduledTime = checkDate.Add(schedule.ScheduledTime);

            if (scheduledTime > now && schedule.IsActiveOnDay(checkDate.DayOfWeek))
            {
                return scheduledTime;
            }
        }

        return null;
    }

    #endregion

    #region Variance Management

    public async Task<ZReportVarianceThreshold?> GetVarianceThresholdAsync(int? storeId = null, CancellationToken ct = default)
    {
        return await _context.Set<ZReportVarianceThreshold>()
            .FirstOrDefaultAsync(t => t.IsActive &&
                                      (t.StoreId == storeId || (t.StoreId == null && storeId != null)), ct);
    }

    public async Task<ZReportVarianceThreshold> UpdateVarianceThresholdAsync(ZReportVarianceThreshold threshold, CancellationToken ct = default)
    {
        var existing = await _context.Set<ZReportVarianceThreshold>()
            .FirstOrDefaultAsync(t => t.Id == threshold.Id, ct);

        if (existing == null)
        {
            await _context.Set<ZReportVarianceThreshold>().AddAsync(threshold, ct);
        }
        else
        {
            existing.AmountThreshold = threshold.AmountThreshold;
            existing.PercentageThreshold = threshold.PercentageThreshold;
            existing.RequireExplanation = threshold.RequireExplanation;
            existing.RequireManagerApproval = threshold.RequireManagerApproval;
            existing.FlagForInvestigation = threshold.FlagForInvestigation;
            existing.IsActive = threshold.IsActive;
        }

        await _context.SaveChangesAsync(ct);
        return threshold;
    }

    public async Task ApproveVarianceAsync(int reportId, int approverUserId, CancellationToken ct = default)
    {
        var report = await _context.Set<ZReportRecord>()
            .FirstOrDefaultAsync(z => z.Id == reportId, ct);

        if (report == null)
            throw new InvalidOperationException($"Z Report {reportId} not found.");

        report.VarianceApprovedByUserId = approverUserId;
        report.VarianceApprovedAt = DateTime.UtcNow;
        report.VarianceRequiresApproval = false;

        await _context.SaveChangesAsync(ct);

        _logger.Information("Variance for Z Report {ReportId} approved by user {UserId}", reportId, approverUserId);
    }

    public async Task<IReadOnlyList<ZReportRecord>> GetReportsWithUnapprovedVariancesAsync(int? storeId = null, CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .Where(z => z.IsFinalized &&
                       z.VarianceRequiresApproval &&
                       !z.VarianceApprovedAt.HasValue)
            .Where(z => storeId == null || z.StoreId == storeId)
            .OrderByDescending(z => z.ReportDateTime)
            .ToListAsync(ct);
    }

    #endregion

    #region Integrity & Audit

    public async Task<bool> VerifyReportIntegrityAsync(int reportId, CancellationToken ct = default)
    {
        var report = await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == reportId, ct);

        if (report == null)
            return false;

        return report.VerifyIntegrity();
    }

    public async Task<IReadOnlyList<int>> VerifyReportIntegrityBatchAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var reports = await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Where(z => z.ReportDateTime >= startDate && z.ReportDateTime <= endDate)
            .ToListAsync(ct);

        return reports
            .Where(r => !r.VerifyIntegrity())
            .Select(r => r.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetReportAuditLogAsync(int reportId, CancellationToken ct = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == nameof(ZReportRecord) && a.EntityId == reportId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> CheckForSequenceGapsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        var reportNumbers = await _context.Set<ZReportRecord>()
            .Where(z => z.ReportDateTime >= startDate && z.ReportDateTime <= endDate)
            .Where(z => storeId == null || z.StoreId == storeId)
            .Select(z => z.ReportNumber)
            .OrderBy(n => n)
            .ToListAsync(ct);

        if (reportNumbers.Count < 2)
            return [];

        var gaps = new List<int>();
        for (int i = 1; i < reportNumbers.Count; i++)
        {
            var expected = reportNumbers[i - 1] + 1;
            while (expected < reportNumbers[i])
            {
                gaps.Add(expected);
                expected++;
            }
        }

        return gaps;
    }

    #endregion

    #region Statistics & Analytics

    public async Task<VarianceStatistics> GetVarianceStatisticsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        var reports = await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Where(z => z.IsFinalized &&
                       z.ReportDateTime >= startDate &&
                       z.ReportDateTime <= endDate)
            .Where(z => storeId == null || z.StoreId == storeId)
            .ToListAsync(ct);

        if (!reports.Any())
        {
            return new VarianceStatistics();
        }

        return new VarianceStatistics
        {
            TotalReports = reports.Count,
            ReportsWithVariance = reports.Count(r => r.CashVariance != 0),
            TotalVarianceAmount = reports.Sum(r => r.CashVariance),
            AverageVariance = reports.Average(r => r.CashVariance),
            MaxShortage = reports.Where(r => r.CashVariance < 0).Select(r => r.CashVariance).DefaultIfEmpty(0).Min(),
            MaxOverage = reports.Where(r => r.CashVariance > 0).Select(r => r.CashVariance).DefaultIfEmpty(0).Max(),
            ShortageCount = reports.Count(r => r.CashVariance < 0),
            OverageCount = reports.Count(r => r.CashVariance > 0),
            ExactCount = reports.Count(r => r.CashVariance == 0),
            VariancePercentage = reports.Sum(r => r.ExpectedCash) > 0
                ? reports.Sum(r => Math.Abs(r.CashVariance)) / reports.Sum(r => r.ExpectedCash) * 100
                : 0
        };
    }

    public async Task<IReadOnlyList<DailySalesTotalDto>> GetDailySalesTotalsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        return await _context.Set<ZReportRecord>()
            .AsNoTracking()
            .Where(z => z.IsFinalized &&
                       z.ReportDateTime >= startDate &&
                       z.ReportDateTime <= endDate &&
                       !z.IsConsolidated)
            .Where(z => storeId == null || z.StoreId == storeId)
            .GroupBy(z => z.ReportDateTime.Date)
            .Select(g => new DailySalesTotalDto
            {
                Date = g.Key,
                ReportCount = g.Count(),
                GrossSales = g.Sum(z => z.GrossSales),
                NetSales = g.Sum(z => z.NetSales),
                TaxCollected = g.Sum(z => z.TotalTax),
                TipsCollected = g.Sum(z => z.TotalTips),
                TransactionCount = g.Sum(z => z.TransactionCount),
                AverageTransactionValue = g.Sum(z => z.TransactionCount) > 0
                    ? g.Sum(z => z.GrandTotal) / g.Sum(z => z.TransactionCount)
                    : 0,
                CashVariance = g.Sum(z => z.CashVariance)
            })
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
    }

    #endregion

    #region Private Helpers

    private async Task<object> BuildFullReportDataAsync(
        int workPeriodId,
        List<Receipt> settledReceipts,
        List<Receipt> voidedReceipts,
        CancellationToken ct)
    {
        // Build comprehensive report data object for JSON storage
        return new
        {
            GeneratedAt = DateTime.UtcNow,
            WorkPeriodId = workPeriodId,
            Receipts = settledReceipts.Select(r => new
            {
                r.Id,
                r.ReceiptNumber,
                r.Subtotal,
                r.DiscountAmount,
                r.TaxAmount,
                r.TipAmount,
                r.TotalAmount,
                r.Status,
                Owner = r.Owner?.FullName,
                r.CreatedAt,
                r.SettledAt
            }).ToList(),
            Voids = voidedReceipts.Select(v => new
            {
                v.Id,
                v.ReceiptNumber,
                v.TotalAmount,
                v.VoidReason,
                VoidedBy = v.VoidedByUser?.FullName,
                v.VoidedAt
            }).ToList()
        };
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

    private async Task<string> GetSystemSettingAsync(string key, string defaultValue, CancellationToken ct)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        return setting?.SettingValue ?? defaultValue;
    }

    public async Task<string> GenerateFormattedReportNumberAsync(int terminalId, int sequentialNumber, CancellationToken ct = default)
    {
        var terminal = await _context.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId, ct);

        // Extract numeric part from terminal code (e.g., "REG-001" -> "001")
        var terminalNumericPart = "000";
        if (terminal != null && terminal.Code != null)
        {
            var numericPart = new string(terminal.Code.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(numericPart))
            {
                terminalNumericPart = numericPart.PadLeft(3, '0');
            }
        }

        // Format: Z-YYYY-TID-NNNN (e.g., Z-2024-001-0042)
        return $"Z-{DateTime.UtcNow.Year}-{terminalNumericPart}-{sequentialNumber:D4}";
    }

    private async Task<List<CashierSessionBreakdown>> BuildCashierSessionBreakdownAsync(
        int workPeriodId,
        int? terminalId,
        CancellationToken ct)
    {
        // Get work period sessions
        var sessionsQuery = _context.Set<WorkPeriodSession>()
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.WorkPeriodId == workPeriodId);

        var sessions = await sessionsQuery.ToListAsync(ct);

        // Get receipts for the work period (optionally filtered by terminal)
        var receiptsQuery = _context.Receipts
            .AsNoTracking()
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Where(r => r.WorkPeriodId == workPeriodId);

        if (terminalId.HasValue)
        {
            receiptsQuery = receiptsQuery.Where(r => r.TerminalId == terminalId);
        }

        var receipts = await receiptsQuery.ToListAsync(ct);

        // Group receipts by owner (cashier)
        var receiptsByOwner = receipts
            .GroupBy(r => r.OwnerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var breakdowns = new List<CashierSessionBreakdown>();

        foreach (var session in sessions)
        {
            var userReceipts = receiptsByOwner.GetValueOrDefault(session.UserId, []);

            // Filter receipts within session timeframe
            var sessionReceipts = userReceipts
                .Where(r => r.CreatedAt >= session.LoggedInAt &&
                           (session.LoggedOutAt == null || r.CreatedAt <= session.LoggedOutAt))
                .ToList();

            var settledReceipts = sessionReceipts.Where(r => r.Status == ReceiptStatus.Settled).ToList();
            var voidedReceipts = sessionReceipts.Where(r => r.Status == ReceiptStatus.Voided).ToList();
            var refundedReceipts = sessionReceipts.Where(r => r.TotalAmount < 0).ToList();

            var payments = settledReceipts.SelectMany(r => r.Payments).ToList();
            var cashPayments = payments.Where(p => p.PaymentMethod?.Type == PaymentMethodType.Cash).Sum(p => p.Amount);
            var cardPayments = payments.Where(p => p.PaymentMethod?.Type == PaymentMethodType.Card).Sum(p => p.Amount);
            var otherPayments = payments.Where(p => p.PaymentMethod?.Type != PaymentMethodType.Cash &&
                                                    p.PaymentMethod?.Type != PaymentMethodType.Card).Sum(p => p.Amount);

            breakdowns.Add(new CashierSessionBreakdown
            {
                UserId = session.UserId,
                UserName = session.User?.FullName ?? "Unknown",
                SessionId = session.Id,
                SessionStart = session.LoggedInAt,
                SessionEnd = session.LoggedOutAt,
                TransactionCount = settledReceipts.Count,
                GrossSales = settledReceipts.Sum(r => r.Subtotal),
                NetSales = settledReceipts.Sum(r => r.TotalAmount),
                CashPayments = cashPayments,
                CardPayments = cardPayments,
                OtherPayments = otherPayments,
                VoidCount = voidedReceipts.Count,
                VoidAmount = voidedReceipts.Sum(r => r.TotalAmount),
                RefundCount = refundedReceipts.Count,
                RefundAmount = Math.Abs(refundedReceipts.Sum(r => r.TotalAmount))
            });
        }

        return breakdowns.OrderBy(b => b.SessionStart).ToList();
    }

    #endregion
}
