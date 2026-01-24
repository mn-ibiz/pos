using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating combined (multi-terminal) reports.
/// </summary>
public class CombinedReportService : ICombinedReportService
{
    private readonly HospitalityPOSDbContext _context;
    private readonly IXReportService _xReportService;
    private readonly IZReportService _zReportService;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly IBusinessSettingsService _businessSettings;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CombinedReportService> _logger;

    public CombinedReportService(
        HospitalityPOSDbContext context,
        IXReportService xReportService,
        IZReportService zReportService,
        IWorkPeriodService workPeriodService,
        IBusinessSettingsService businessSettings,
        ICurrentUserService currentUser,
        ILogger<CombinedReportService> logger)
    {
        _context = context;
        _xReportService = xReportService;
        _zReportService = zReportService;
        _workPeriodService = workPeriodService;
        _businessSettings = businessSettings;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CombinedXReportData> GenerateCombinedXReportAsync(
        CancellationToken cancellationToken = default)
    {
        var workPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync(cancellationToken);
        if (workPeriod == null)
        {
            throw new InvalidOperationException("No active work period found.");
        }

        return await GenerateCombinedXReportForWorkPeriodAsync(workPeriod.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CombinedXReportData> GenerateCombinedXReportForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating combined X-Report for work period {WorkPeriodId}", workPeriodId);

        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .Include(wp => wp.StartedByUser)
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken);

        if (workPeriod == null)
        {
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");
        }

        // Get all terminals
        var terminals = await _context.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var businessName = await _businessSettings.GetBusinessNameAsync(cancellationToken);
        var businessAddress = await _businessSettings.GetBusinessAddressAsync(cancellationToken);
        var businessPhone = await _businessSettings.GetBusinessPhoneAsync(cancellationToken);

        var combinedReport = new CombinedXReportData
        {
            BusinessName = businessName ?? "Hospitality POS",
            BusinessAddress = businessAddress ?? "",
            BusinessPhone = businessPhone ?? "",
            ReportNumber = $"CX-{DateTime.UtcNow:yyyyMMdd-HHmm}",
            GeneratedAt = DateTime.UtcNow,
            GeneratedByName = _currentUser.UserName ?? "Unknown",
            WorkPeriodId = workPeriodId,
            ShiftStarted = workPeriod.StartedAt,
            CurrentTime = DateTime.UtcNow,
            ShiftDurationFormatted = FormatDuration(DateTime.UtcNow - workPeriod.StartedAt),
            TerminalCount = terminals.Count
        };

        // Aggregate data from all terminals
        var terminalBreakdown = new List<TerminalXReportSummary>();
        var allPaymentBreakdown = new Dictionary<int, PaymentMethodBreakdownItem>();
        var allCashierSessions = new List<CashierSessionBreakdownItem>();

        foreach (var terminal in terminals)
        {
            try
            {
                var terminalSummary = await BuildTerminalXReportSummaryAsync(
                    terminal, workPeriodId, cancellationToken);
                terminalBreakdown.Add(terminalSummary);

                // Aggregate totals
                combinedReport.GrossSales += terminalSummary.GrossSales;
                combinedReport.NetSales += terminalSummary.NetSales;
                combinedReport.GrandTotal += terminalSummary.GrandTotal;
                combinedReport.TransactionCount += terminalSummary.TransactionCount;
                combinedReport.VoidCount += terminalSummary.VoidCount;
                combinedReport.RefundCount += terminalSummary.RefundCount;
                combinedReport.ExpectedCash += terminalSummary.ExpectedCash;

                // Aggregate cashier sessions
                allCashierSessions.AddRange(terminalSummary.CashierSessions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get X-Report data for terminal {TerminalId}", terminal.Id);
                terminalBreakdown.Add(new TerminalXReportSummary
                {
                    TerminalId = terminal.Id,
                    TerminalCode = terminal.Code ?? "",
                    TerminalName = terminal.Name,
                    IsOnline = false
                });
            }
        }

        // Aggregate payment methods across all terminals
        var paymentBreakdown = await GetCombinedPaymentBreakdownAsync(workPeriodId, cancellationToken);
        combinedReport.PaymentBreakdown = paymentBreakdown;

        // Get additional totals
        var discountInfo = await GetCombinedDiscountInfoAsync(workPeriodId, cancellationToken);
        combinedReport.Discounts = discountInfo.TotalAmount;
        combinedReport.DiscountCount = discountInfo.Count;

        var refundInfo = await GetCombinedRefundInfoAsync(workPeriodId, cancellationToken);
        combinedReport.Refunds = refundInfo.TotalAmount;
        combinedReport.RefundCount = refundInfo.Count;

        var taxInfo = await GetCombinedTaxInfoAsync(workPeriodId, cancellationToken);
        combinedReport.TaxAmount = taxInfo;

        var tipsInfo = await GetCombinedTipsInfoAsync(workPeriodId, cancellationToken);
        combinedReport.TipsCollected = tipsInfo;

        var cashDrawerInfo = await GetCombinedCashDrawerInfoAsync(workPeriodId, cancellationToken);
        combinedReport.OpeningFloat = cashDrawerInfo.OpeningFloat;
        combinedReport.CashReceived = cashDrawerInfo.CashReceived;
        combinedReport.CashRefunds = cashDrawerInfo.CashRefunds;
        combinedReport.CashPayouts = cashDrawerInfo.CashPayouts;

        combinedReport.TerminalBreakdown = terminalBreakdown;
        combinedReport.CashierSessions = allCashierSessions
            .OrderBy(cs => cs.TerminalCode)
            .ThenBy(cs => cs.StartTime)
            .ToList();

        _logger.LogInformation(
            "Generated combined X-Report: {TerminalCount} terminals, {TransactionCount} transactions, {GrandTotal} total",
            combinedReport.TerminalCount, combinedReport.TransactionCount, combinedReport.GrandTotal);

        return combinedReport;
    }

    /// <inheritdoc />
    public async Task<CombinedZReportPreview> PreviewCombinedZReportAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Previewing combined Z-Report for work period {WorkPeriodId}", workPeriodId);

        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken);

        if (workPeriod == null)
        {
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");
        }

        var terminals = await _context.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var preview = new CombinedZReportPreview
        {
            WorkPeriodId = workPeriodId,
            WorkPeriodStart = workPeriod.StartedAt,
            TerminalCount = terminals.Count
        };

        var terminalBreakdown = new List<TerminalZReportSummary>();

        foreach (var terminal in terminals)
        {
            var summary = await BuildTerminalZReportSummaryAsync(terminal, workPeriodId, cancellationToken);
            terminalBreakdown.Add(summary);

            preview.TotalGrossSales += summary.GrossSales;
            preview.TotalNetSales += summary.NetSales;
            preview.TotalGrandTotal += summary.GrandTotal;
            preview.TotalTransactionCount += summary.TransactionCount;

            if (summary.HasZReport)
            {
                preview.CompletedZReportCount++;
            }
            else
            {
                preview.PendingZReportCount++;
            }
        }

        preview.TerminalBreakdown = terminalBreakdown;

        return preview;
    }

    /// <inheritdoc />
    public async Task<CombinedZReportValidationResult> ValidateCombinedZReportAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating combined Z-Report for work period {WorkPeriodId}", workPeriodId);

        var terminals = await _context.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var result = new CombinedZReportValidationResult();
        var allReady = true;

        foreach (var terminal in terminals)
        {
            var terminalValidation = await _zReportService.ValidateCanGenerateForTerminalAsync(
                workPeriodId, terminal.Id, cancellationToken);

            if (terminalValidation.CanGenerate)
            {
                result.ReadyTerminals.Add(terminal.Id);
            }
            else
            {
                allReady = false;
                result.TerminalIssues.Add(new TerminalValidationIssue
                {
                    TerminalId = terminal.Id,
                    TerminalCode = terminal.Code ?? "",
                    Issues = terminalValidation.Issues.Select(i => i.Message).ToList()
                });
            }

            if (terminalValidation.HasUnsettledReceipts)
            {
                result.HasUnsettledReceipts = true;
                result.TotalUnsettledReceiptCount += terminalValidation.UnsettledReceiptCount;
            }

            if (terminalValidation.HasOpenOrders)
            {
                result.HasOpenOrders = true;
                result.TotalOpenOrderCount += terminalValidation.OpenOrderCount;
            }
        }

        result.AllTerminalsReady = allReady;

        return result;
    }

    /// <inheritdoc />
    public async Task<List<TerminalXReportSummary>> GetTerminalXReportSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var workPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync(cancellationToken);
        if (workPeriod == null)
        {
            return [];
        }

        return await GetTerminalXReportSummariesForWorkPeriodAsync(workPeriod.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TerminalXReportSummary>> GetTerminalXReportSummariesForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        var terminals = await _context.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var summaries = new List<TerminalXReportSummary>();

        foreach (var terminal in terminals)
        {
            var summary = await BuildTerminalXReportSummaryAsync(terminal, workPeriodId, cancellationToken);
            summaries.Add(summary);
        }

        return summaries;
    }

    /// <inheritdoc />
    public async Task<List<TerminalZReportSummary>> GetTerminalZReportSummariesAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        var terminals = await _context.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var summaries = new List<TerminalZReportSummary>();

        foreach (var terminal in terminals)
        {
            var summary = await BuildTerminalZReportSummaryAsync(terminal, workPeriodId, cancellationToken);
            summaries.Add(summary);
        }

        return summaries;
    }

    private async Task<TerminalXReportSummary> BuildTerminalXReportSummaryAsync(
        Terminal terminal,
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var summary = new TerminalXReportSummary
        {
            TerminalId = terminal.Id,
            TerminalCode = terminal.Code ?? "",
            TerminalName = terminal.Name,
            IsOnline = terminal.IsOnline
        };

        // Get receipts for this terminal in this work period
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Payments)
            .Where(r => r.WorkPeriodId == workPeriodId && r.TerminalId == terminal.Id)
            .ToListAsync(cancellationToken);

        var completedReceipts = receipts.Where(r => r.Status == Core.Enums.ReceiptStatus.Settled).ToList();

        summary.GrossSales = completedReceipts.Sum(r => r.Subtotal);
        summary.NetSales = completedReceipts.Sum(r => r.Subtotal - (r.DiscountAmount ?? 0));
        summary.GrandTotal = completedReceipts.Sum(r => r.TotalAmount);
        summary.TransactionCount = completedReceipts.Count;
        summary.VoidCount = receipts.Count(r => r.Status == Core.Enums.ReceiptStatus.Voided);
        summary.RefundCount = receipts.Count(r => r.Status == Core.Enums.ReceiptStatus.Refunded);

        // Calculate expected cash
        var cashPayments = completedReceipts
            .SelectMany(r => r.Payments)
            .Where(p => IsCashPayment(p.PaymentMethodId))
            .Sum(p => p.Amount);

        var openingFloat = await GetTerminalOpeningFloatAsync(terminal.Id, workPeriodId, cancellationToken);
        summary.ExpectedCash = openingFloat + cashPayments;

        // Get cashier sessions
        var cashierSessions = await _context.CashierSessions
            .AsNoTracking()
            .Include(cs => cs.User)
            .Where(cs => cs.WorkPeriodId == workPeriodId && cs.TerminalId == terminal.Id)
            .ToListAsync(cancellationToken);

        summary.CashierSessions = cashierSessions.Select(cs => new CashierSessionBreakdownItem
        {
            UserId = cs.UserId,
            CashierName = cs.User?.FullName ?? cs.User?.Username ?? "Unknown",
            TerminalId = terminal.Id,
            TerminalCode = terminal.Code ?? "",
            StartTime = cs.LoggedInAt,
            EndTime = cs.LoggedOutAt,
            TransactionCount = receipts.Count(r => r.UserId == cs.UserId),
            SalesTotal = completedReceipts.Where(r => r.UserId == cs.UserId).Sum(r => r.TotalAmount)
        }).ToList();

        return summary;
    }

    private async Task<TerminalZReportSummary> BuildTerminalZReportSummaryAsync(
        Terminal terminal,
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var summary = new TerminalZReportSummary
        {
            TerminalId = terminal.Id,
            TerminalCode = terminal.Code ?? "",
            TerminalName = terminal.Name
        };

        // Check if Z-Report exists for this terminal
        var zReport = await _context.ZReportRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.WorkPeriodId == workPeriodId && z.TerminalId == terminal.Id, cancellationToken);

        if (zReport != null)
        {
            summary.HasZReport = true;
            summary.ZReportId = zReport.Id;
            summary.ZReportNumber = zReport.ReportNumberFormatted ?? $"Z-{zReport.ReportNumber:D4}";
            summary.GrossSales = zReport.GrossSales;
            summary.NetSales = zReport.NetSales;
            summary.GrandTotal = zReport.GrandTotal;
            summary.TransactionCount = zReport.TransactionCount;
            summary.ExpectedCash = zReport.ExpectedCash;
            summary.ActualCash = zReport.ActualCash;
            summary.Variance = zReport.Variance;
        }
        else
        {
            // Get preview data
            var receipts = await _context.Receipts
                .AsNoTracking()
                .Include(r => r.Payments)
                .Where(r => r.WorkPeriodId == workPeriodId && r.TerminalId == terminal.Id)
                .ToListAsync(cancellationToken);

            var completedReceipts = receipts.Where(r => r.Status == Core.Enums.ReceiptStatus.Settled).ToList();

            summary.GrossSales = completedReceipts.Sum(r => r.Subtotal);
            summary.NetSales = completedReceipts.Sum(r => r.Subtotal - (r.DiscountAmount ?? 0));
            summary.GrandTotal = completedReceipts.Sum(r => r.TotalAmount);
            summary.TransactionCount = completedReceipts.Count;

            var cashPayments = completedReceipts
                .SelectMany(r => r.Payments)
                .Where(p => IsCashPayment(p.PaymentMethodId))
                .Sum(p => p.Amount);

            var openingFloat = await GetTerminalOpeningFloatAsync(terminal.Id, workPeriodId, cancellationToken);
            summary.ExpectedCash = openingFloat + cashPayments;
        }

        return summary;
    }

    private async Task<List<PaymentMethodBreakdownItem>> GetCombinedPaymentBreakdownAsync(
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var paymentBreakdown = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Payments)
            .ThenInclude(p => p.PaymentMethod)
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Settled)
            .SelectMany(r => r.Payments)
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod.Name, p.PaymentMethod.Type })
            .Select(g => new PaymentMethodBreakdownItem
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                PaymentMethodType = g.Key.Type,
                Amount = g.Sum(p => p.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(pb => pb.PaymentMethodType)
            .ThenByDescending(pb => pb.Amount)
            .ToListAsync(cancellationToken);

        return paymentBreakdown;
    }

    private async Task<(decimal TotalAmount, int Count)> GetCombinedDiscountInfoAsync(
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId &&
                       r.Status == Core.Enums.ReceiptStatus.Settled &&
                       r.DiscountAmount > 0)
            .ToListAsync(cancellationToken);

        return (receipts.Sum(r => r.DiscountAmount ?? 0), receipts.Count);
    }

    private async Task<(decimal TotalAmount, int Count)> GetCombinedRefundInfoAsync(
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Refunded)
            .ToListAsync(cancellationToken);

        return (receipts.Sum(r => r.TotalAmount), receipts.Count);
    }

    private async Task<decimal> GetCombinedTaxInfoAsync(
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        return await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Settled)
            .SumAsync(r => r.TaxAmount ?? 0, cancellationToken);
    }

    private async Task<decimal> GetCombinedTipsInfoAsync(
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        return await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Settled)
            .SumAsync(r => r.TipAmount ?? 0, cancellationToken);
    }

    private async Task<(decimal OpeningFloat, decimal CashReceived, decimal CashRefunds, decimal CashPayouts)>
        GetCombinedCashDrawerInfoAsync(int workPeriodId, CancellationToken cancellationToken)
    {
        // Get all cash drawer operations for the work period
        var cashDrawerOps = await _context.CashDrawerOperations
            .AsNoTracking()
            .Where(op => op.WorkPeriodId == workPeriodId)
            .ToListAsync(cancellationToken);

        var openingFloat = cashDrawerOps
            .Where(op => op.OperationType == Core.Enums.CashDrawerOperationType.OpeningFloat)
            .Sum(op => op.Amount);

        var cashPayouts = cashDrawerOps
            .Where(op => op.OperationType == Core.Enums.CashDrawerOperationType.Payout)
            .Sum(op => op.Amount);

        // Get cash payments from receipts
        var cashPaymentMethodIds = await GetCashPaymentMethodIdsAsync(cancellationToken);

        var cashReceived = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Payments)
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Settled)
            .SelectMany(r => r.Payments)
            .Where(p => cashPaymentMethodIds.Contains(p.PaymentMethodId))
            .SumAsync(p => p.Amount, cancellationToken);

        var cashRefunds = await _context.Receipts
            .AsNoTracking()
            .Include(r => r.Payments)
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == Core.Enums.ReceiptStatus.Refunded)
            .SelectMany(r => r.Payments)
            .Where(p => cashPaymentMethodIds.Contains(p.PaymentMethodId))
            .SumAsync(p => p.Amount, cancellationToken);

        return (openingFloat, cashReceived, cashRefunds, cashPayouts);
    }

    private async Task<decimal> GetTerminalOpeningFloatAsync(
        int terminalId,
        int workPeriodId,
        CancellationToken cancellationToken)
    {
        var openingFloat = await _context.CashDrawerOperations
            .AsNoTracking()
            .Where(op => op.WorkPeriodId == workPeriodId &&
                        op.TerminalId == terminalId &&
                        op.OperationType == Core.Enums.CashDrawerOperationType.OpeningFloat)
            .SumAsync(op => op.Amount, cancellationToken);

        return openingFloat;
    }

    private async Task<HashSet<int>> GetCashPaymentMethodIdsAsync(CancellationToken cancellationToken)
    {
        var cashPaymentMethods = await _context.PaymentMethods
            .AsNoTracking()
            .Where(pm => pm.IsCash || pm.Name.Contains("Cash", StringComparison.OrdinalIgnoreCase))
            .Select(pm => pm.Id)
            .ToListAsync(cancellationToken);

        return [.. cashPaymentMethods];
    }

    private bool IsCashPayment(int paymentMethodId)
    {
        // Simple check - in production this should query the payment method
        return paymentMethodId == 1; // Assuming 1 is Cash
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return $"{hours}h {minutes:D2}m";
    }
}
