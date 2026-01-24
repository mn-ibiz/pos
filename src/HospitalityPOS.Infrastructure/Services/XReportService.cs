using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating X-Reports (mid-shift examination reports).
/// </summary>
public class XReportService : IXReportService
{
    private readonly POSDbContext _context;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly IWorkPeriodSessionService _sessionService;
    private readonly ITerminalSessionContext _terminalSession;
    private readonly ISessionService _userSession;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="XReportService"/> class.
    /// </summary>
    public XReportService(
        POSDbContext context,
        IWorkPeriodService workPeriodService,
        IWorkPeriodSessionService sessionService,
        ITerminalSessionContext terminalSession,
        ISessionService userSession,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _terminalSession = terminalSession ?? throw new ArgumentNullException(nameof(terminalSession));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<XReportData> GenerateXReportAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        // Get current work period for terminal
        var workPeriod = await GetCurrentWorkPeriodForTerminalAsync(terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod == null)
        {
            throw new InvalidOperationException($"No open work period found for terminal {terminalId}");
        }

        return await GenerateXReportForWorkPeriodAsync(workPeriod.Id, terminalId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<XReportData> GenerateXReportForWorkPeriodAsync(
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod == null)
        {
            throw new InvalidOperationException($"Work period {workPeriodId} not found");
        }

        var terminal = await _context.Set<Terminal>()
            .AsNoTracking()
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal == null)
        {
            throw new InvalidOperationException($"Terminal {terminalId} not found");
        }

        var reportNumber = await GenerateReportNumberAsync(terminalId, cancellationToken)
            .ConfigureAwait(false);

        var report = new XReportData
        {
            // Terminal Info
            TerminalId = terminalId,
            TerminalCode = terminal.Code,
            TerminalName = terminal.Name,
            ReportNumber = reportNumber,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = _userSession.CurrentUserId,
            GeneratedByName = _userSession.CurrentUserDisplayName,

            // Shift Info
            WorkPeriodId = workPeriodId,
            ShiftStarted = workPeriod.StartedAt,
            CurrentTime = DateTime.UtcNow,
            ShiftDuration = DateTime.UtcNow - workPeriod.StartedAt
        };

        // Load business info
        await LoadBusinessInfoAsync(report, terminal, cancellationToken).ConfigureAwait(false);

        // Load cashier sessions
        await LoadCashierSessionsAsync(report, workPeriodId, terminalId, cancellationToken).ConfigureAwait(false);

        // Load sales summary
        await LoadSalesSummaryAsync(report, workPeriodId, terminalId, cancellationToken).ConfigureAwait(false);

        // Load payment breakdown
        await LoadPaymentBreakdownAsync(report, workPeriodId, terminalId, cancellationToken).ConfigureAwait(false);

        // Load cash drawer info
        await LoadCashDrawerInfoAsync(report, workPeriodId, terminalId, cancellationToken).ConfigureAwait(false);

        // Load statistics
        await LoadStatisticsAsync(report, workPeriodId, terminalId, cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Generated X-Report {ReportNumber} for terminal {TerminalCode} - Net Sales: {NetSales:C}",
            reportNumber, terminal.Code, report.NetSales);

        return report;
    }

    /// <inheritdoc />
    public async Task<XReportData> GenerateXReportForCashierAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Get current work period for terminal
        var workPeriod = await GetCurrentWorkPeriodForTerminalAsync(terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod == null)
        {
            throw new InvalidOperationException($"No open work period found for terminal {terminalId}");
        }

        var report = await GenerateXReportForWorkPeriodAsync(workPeriod.Id, terminalId, cancellationToken)
            .ConfigureAwait(false);

        // Filter to only the specified cashier
        report.CashierSessions = report.CashierSessions
            .Where(s => s.UserId == userId)
            .ToList();

        return report;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<XReportRecord>> GetXReportHistoryAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<XReportEntity>()
            .AsNoTracking()
            .Where(x => x.WorkPeriodId == workPeriodId)
            .OrderByDescending(x => x.GeneratedAt)
            .Select(x => new XReportRecord
            {
                Id = x.Id,
                WorkPeriodId = x.WorkPeriodId,
                TerminalId = x.TerminalId,
                TerminalCode = x.TerminalCode,
                ReportNumber = x.ReportNumber,
                GeneratedAt = x.GeneratedAt,
                GeneratedByUserId = x.GeneratedByUserId,
                GeneratedByName = x.GeneratedByUser != null ? x.GeneratedByUser.FullName : "Unknown",
                GrossSales = x.GrossSales,
                NetSales = x.NetSales,
                TaxAmount = x.TaxAmount,
                TotalPayments = x.TotalPayments,
                ExpectedCash = x.ExpectedCash,
                TransactionCount = x.TransactionCount
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<XReportRecord>> GetXReportHistoryByTerminalAsync(
        int terminalId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<XReportEntity>()
            .AsNoTracking()
            .Where(x => x.TerminalId == terminalId &&
                        x.GeneratedAt >= startDate &&
                        x.GeneratedAt <= endDate)
            .OrderByDescending(x => x.GeneratedAt)
            .Select(x => new XReportRecord
            {
                Id = x.Id,
                WorkPeriodId = x.WorkPeriodId,
                TerminalId = x.TerminalId,
                TerminalCode = x.TerminalCode,
                ReportNumber = x.ReportNumber,
                GeneratedAt = x.GeneratedAt,
                GeneratedByUserId = x.GeneratedByUserId,
                GeneratedByName = x.GeneratedByUser != null ? x.GeneratedByUser.FullName : "Unknown",
                GrossSales = x.GrossSales,
                NetSales = x.NetSales,
                TaxAmount = x.TaxAmount,
                TotalPayments = x.TotalPayments,
                ExpectedCash = x.ExpectedCash,
                TransactionCount = x.TransactionCount
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<XReportRecord> SaveXReportAsync(
        XReportData report,
        CancellationToken cancellationToken = default)
    {
        var entity = new XReportEntity
        {
            WorkPeriodId = report.WorkPeriodId,
            TerminalId = report.TerminalId,
            TerminalCode = report.TerminalCode,
            ReportNumber = report.ReportNumber,
            GeneratedAt = report.GeneratedAt,
            GeneratedByUserId = report.GeneratedByUserId,
            GrossSales = report.GrossSales,
            NetSales = report.NetSales,
            TaxAmount = report.TaxAmount,
            TotalPayments = report.TotalPayments,
            ExpectedCash = report.ExpectedCash,
            TransactionCount = report.TransactionCount,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _context.Set<XReportEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new XReportRecord
        {
            Id = entity.Id,
            WorkPeriodId = entity.WorkPeriodId,
            TerminalId = entity.TerminalId,
            TerminalCode = entity.TerminalCode,
            ReportNumber = entity.ReportNumber,
            GeneratedAt = entity.GeneratedAt,
            GeneratedByUserId = entity.GeneratedByUserId,
            GeneratedByName = report.GeneratedByName,
            GrossSales = entity.GrossSales,
            NetSales = entity.NetSales,
            TaxAmount = entity.TaxAmount,
            TotalPayments = entity.TotalPayments,
            ExpectedCash = entity.ExpectedCash,
            TransactionCount = entity.TransactionCount
        };
    }

    /// <inheritdoc />
    public async Task<string> GenerateReportNumberAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var year = today.Year;
        var prefix = $"X-{year}-{terminalId:D3}-";

        // Get count of X-Reports for this terminal today
        var startOfDay = today.Date;
        var endOfDay = startOfDay.AddDays(1);

        var count = await _context.Set<XReportEntity>()
            .CountAsync(x => x.TerminalId == terminalId &&
                            x.GeneratedAt >= startOfDay &&
                            x.GeneratedAt < endOfDay,
                        cancellationToken)
            .ConfigureAwait(false);

        return $"{prefix}{(count + 1):D4}";
    }

    /// <inheritdoc />
    public Task<byte[]> ExportXReportAsync(
        XReportData report,
        ReportExportFormat format,
        CancellationToken cancellationToken = default)
    {
        // Export functionality will be implemented in a separate service
        // For now, return a placeholder
        _logger.Information("Export X-Report {ReportNumber} to {Format}", report.ReportNumber, format);
        return Task.FromResult(Array.Empty<byte>());
    }

    #region Private Helper Methods

    private async Task<WorkPeriod?> GetCurrentWorkPeriodForTerminalAsync(
        int terminalId,
        CancellationToken cancellationToken)
    {
        return await _context.WorkPeriods
            .AsNoTracking()
            .Where(w => (w.TerminalId == terminalId || w.TerminalId == null) &&
                        w.Status == WorkPeriodStatus.Open)
            .OrderByDescending(w => w.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task LoadBusinessInfoAsync(
        XReportData report,
        Terminal terminal,
        CancellationToken cancellationToken)
    {
        var store = terminal.Store ?? await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == terminal.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (store != null)
        {
            report.BusinessName = store.Name;
            report.BusinessAddress = store.Address ?? string.Empty;
            report.BusinessPhone = store.Phone ?? string.Empty;
            report.TaxId = store.TaxId ?? string.Empty;
        }
    }

    private async Task LoadCashierSessionsAsync(
        XReportData report,
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken)
    {
        var sessions = await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.WorkPeriodId == workPeriodId && s.TerminalId == terminalId)
            .OrderBy(s => s.LoginAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        report.CashierSessions = sessions.Select(s => new CashierSessionSummary
        {
            SessionId = s.Id,
            UserId = s.UserId,
            CashierName = s.User?.FullName ?? s.User?.Username ?? "Unknown",
            StartTime = s.LoginAt,
            EndTime = s.LogoutAt,
            IsCurrent = s.LogoutAt == null,
            SalesTotal = s.SalesTotal,
            TransactionCount = s.TransactionCount,
            CashReceived = s.CashReceived,
            CardTotal = s.CardTotal,
            MpesaTotal = s.MpesaTotal,
            RefundTotal = s.RefundTotal,
            VoidTotal = s.VoidTotal,
            DiscountTotal = s.DiscountTotal,
            Duration = s.Duration
        }).ToList();
    }

    private async Task LoadSalesSummaryAsync(
        XReportData report,
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken)
    {
        // Get settled receipts for this work period and terminal
        var receipts = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId &&
                        (r.TerminalId == terminalId || r.TerminalId == null) &&
                        r.Status == ReceiptStatus.Settled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get voided receipts
        var voidedReceipts = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.WorkPeriodId == workPeriodId &&
                        (r.TerminalId == terminalId || r.TerminalId == null) &&
                        r.IsVoided)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        report.GrossSales = receipts.Sum(r => r.Subtotal);
        report.Discounts = receipts.Sum(r => r.DiscountAmount);
        report.TaxAmount = receipts.Sum(r => r.TaxAmount);
        report.Voids = voidedReceipts.Sum(r => r.TotalAmount);

        // Calculate refunds from sessions
        report.Refunds = report.CashierSessions.Sum(s => s.RefundTotal);

        report.NetSales = report.GrossSales - report.Discounts - report.Refunds;
        report.GrandTotal = report.NetSales + report.TaxAmount;
    }

    private async Task LoadPaymentBreakdownAsync(
        XReportData report,
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken)
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.PaymentMethod)
            .Include(p => p.Receipt)
            .Where(p => p.Receipt != null &&
                        p.Receipt.WorkPeriodId == workPeriodId &&
                        (p.TerminalId == terminalId || p.TerminalId == null) &&
                        p.Receipt.Status == ReceiptStatus.Settled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var breakdown = payments
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod?.Name, p.PaymentMethod?.Code })
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name ?? "Unknown",
                PaymentMethodCode = g.Key.Code ?? "UNKNOWN",
                Amount = g.Sum(p => p.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(p => p.PaymentMethodName)
            .ToList();

        report.TotalPayments = breakdown.Sum(b => b.Amount);

        // Calculate percentages
        foreach (var item in breakdown)
        {
            item.Percentage = report.TotalPayments > 0
                ? Math.Round(item.Amount / report.TotalPayments * 100, 1)
                : 0;
        }

        report.PaymentBreakdown = breakdown;
    }

    private async Task LoadCashDrawerInfoAsync(
        XReportData report,
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken)
    {
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        report.OpeningFloat = workPeriod?.OpeningFloat ?? 0;

        // Calculate cash from sessions
        report.CashReceived = report.CashierSessions.Sum(s => s.CashReceived);

        // Get cash payouts
        var cashPayouts = await _context.Set<CashPayout>()
            .AsNoTracking()
            .Where(cp => cp.WorkPeriodId == workPeriodId &&
                         (cp.TerminalId == terminalId || cp.TerminalId == null))
            .SumAsync(cp => cp.Amount, cancellationToken)
            .ConfigureAwait(false);

        report.CashPayouts = cashPayouts;

        // Cash refunds (from sessions)
        report.CashRefunds = report.CashierSessions
            .Where(s => s.RefundTotal > 0)
            .Sum(s => s.RefundTotal);

        // Expected cash = Opening + Received - Refunds - Payouts
        report.ExpectedCash = report.OpeningFloat + report.CashReceived - report.CashRefunds - report.CashPayouts;
    }

    private async Task LoadStatisticsAsync(
        XReportData report,
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken)
    {
        // Transaction count from receipts
        report.TransactionCount = await _context.Receipts
            .CountAsync(r => r.WorkPeriodId == workPeriodId &&
                            (r.TerminalId == terminalId || r.TerminalId == null) &&
                            r.Status == ReceiptStatus.Settled,
                        cancellationToken)
            .ConfigureAwait(false);

        // Average transaction
        report.AverageTransaction = report.TransactionCount > 0
            ? report.NetSales / report.TransactionCount
            : 0;

        // Void count
        report.VoidCount = await _context.Receipts
            .CountAsync(r => r.WorkPeriodId == workPeriodId &&
                            (r.TerminalId == terminalId || r.TerminalId == null) &&
                            r.IsVoided,
                        cancellationToken)
            .ConfigureAwait(false);

        // Discount count (receipts with discounts)
        report.DiscountCount = await _context.Receipts
            .CountAsync(r => r.WorkPeriodId == workPeriodId &&
                            (r.TerminalId == terminalId || r.TerminalId == null) &&
                            r.DiscountAmount > 0,
                        cancellationToken)
            .ConfigureAwait(false);

        // Customer count (approximate - unique table numbers or orders)
        report.CustomerCount = report.TransactionCount; // Simplified
    }

    #endregion
}

/// <summary>
/// Entity for persisting X-Report records.
/// </summary>
public class XReportEntity : BaseEntity
{
    public int WorkPeriodId { get; set; }
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string ReportNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int GeneratedByUserId { get; set; }

    // Snapshot values
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ExpectedCash { get; set; }
    public int TransactionCount { get; set; }

    // Navigation
    public virtual WorkPeriod WorkPeriod { get; set; } = null!;
    public virtual Terminal Terminal { get; set; } = null!;
    public virtual User GeneratedByUser { get; set; } = null!;
}
