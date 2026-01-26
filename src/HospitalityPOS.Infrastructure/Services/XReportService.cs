using System.Text;
using ClosedXML.Excel;
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
        _logger.Information("Exporting X-Report {ReportNumber} to {Format}", report.ReportNumber, format);

        return format switch
        {
            ReportExportFormat.Excel => Task.FromResult(ExportToExcel(report)),
            ReportExportFormat.Csv => Task.FromResult(ExportToCsv(report)),
            ReportExportFormat.Pdf => Task.FromResult(ExportToPdf(report)),
            ReportExportFormat.ThermalPrint => Task.FromResult(ExportToThermalPrint(report)),
            _ => Task.FromResult(Array.Empty<byte>())
        };
    }

    private byte[] ExportToExcel(XReportData report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("X-Report");

        var row = 1;

        // Header
        ws.Cell(row, 1).Value = report.BusinessName;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = $"X-Report: {report.ReportNumber}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = $"Terminal: {report.TerminalName} ({report.TerminalCode})";
        row++;

        ws.Cell(row, 1).Value = $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}";
        ws.Cell(row, 3).Value = $"By: {report.GeneratedByName}";
        row++;

        ws.Cell(row, 1).Value = $"Shift Started: {report.ShiftStarted:yyyy-MM-dd HH:mm}";
        ws.Cell(row, 3).Value = $"Duration: {report.ShiftDurationFormatted}";
        row += 2;

        // Sales Summary Section
        ws.Cell(row, 1).Value = "SALES SUMMARY";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = "Gross Sales";
        ws.Cell(row, 2).Value = report.GrossSales;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Discounts";
        ws.Cell(row, 2).Value = -report.Discounts;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Refunds";
        ws.Cell(row, 2).Value = -report.Refunds;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Voids";
        ws.Cell(row, 2).Value = -report.Voids;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Net Sales";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = report.NetSales;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 2).Style.Font.Bold = true;
        row++;

        ws.Cell(row, 1).Value = "Tax Amount";
        ws.Cell(row, 2).Value = report.TaxAmount;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Tips Collected";
        ws.Cell(row, 2).Value = report.TipsCollected;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Grand Total";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = report.GrandTotal;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        // Payment Breakdown Section
        ws.Cell(row, 1).Value = "PAYMENT BREAKDOWN";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = "Payment Method";
        ws.Cell(row, 2).Value = "Amount";
        ws.Cell(row, 3).Value = "Count";
        ws.Cell(row, 4).Value = "%";
        ws.Range(row, 1, row, 4).Style.Font.Bold = true;
        row++;

        foreach (var payment in report.PaymentBreakdown)
        {
            ws.Cell(row, 1).Value = payment.PaymentMethodName;
            ws.Cell(row, 2).Value = payment.Amount;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Value = payment.TransactionCount;
            ws.Cell(row, 4).Value = payment.Percentage / 100;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            row++;
        }

        ws.Cell(row, 1).Value = "Total Payments";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = report.TotalPayments;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        // Cash Drawer Section
        ws.Cell(row, 1).Value = "CASH DRAWER";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = "Opening Float";
        ws.Cell(row, 2).Value = report.OpeningFloat;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Cash Received";
        ws.Cell(row, 2).Value = report.CashReceived;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Cash Refunds";
        ws.Cell(row, 2).Value = -report.CashRefunds;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Cash Payouts";
        ws.Cell(row, 2).Value = -report.CashPayouts;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Expected Cash";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = report.ExpectedCash;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        // Statistics Section
        ws.Cell(row, 1).Value = "STATISTICS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, 4).Merge();
        row++;

        ws.Cell(row, 1).Value = "Transaction Count";
        ws.Cell(row, 2).Value = report.TransactionCount;
        row++;

        ws.Cell(row, 1).Value = "Average Transaction";
        ws.Cell(row, 2).Value = report.AverageTransaction;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;

        ws.Cell(row, 1).Value = "Customer Count";
        ws.Cell(row, 2).Value = report.CustomerCount;
        row++;

        ws.Cell(row, 1).Value = "Voids";
        ws.Cell(row, 2).Value = report.VoidCount;
        row++;

        ws.Cell(row, 1).Value = "Refunds";
        ws.Cell(row, 2).Value = report.RefundCount;
        row++;

        ws.Cell(row, 1).Value = "Discounts Applied";
        ws.Cell(row, 2).Value = report.DiscountCount;
        row++;

        ws.Cell(row, 1).Value = "Drawer Opens";
        ws.Cell(row, 2).Value = report.DrawerOpenCount;
        row += 2;

        // Cashier Sessions Section
        if (report.CashierSessions.Count > 0)
        {
            ws.Cell(row, 1).Value = "CASHIER SESSIONS";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Range(row, 1, row, 6).Merge();
            row++;

            ws.Cell(row, 1).Value = "Cashier";
            ws.Cell(row, 2).Value = "Start Time";
            ws.Cell(row, 3).Value = "Sales";
            ws.Cell(row, 4).Value = "Transactions";
            ws.Cell(row, 5).Value = "Cash";
            ws.Cell(row, 6).Value = "Duration";
            ws.Range(row, 1, row, 6).Style.Font.Bold = true;
            row++;

            foreach (var session in report.CashierSessions)
            {
                ws.Cell(row, 1).Value = session.CashierName;
                ws.Cell(row, 2).Value = session.StartTime;
                ws.Cell(row, 2).Style.NumberFormat.Format = "HH:mm";
                ws.Cell(row, 3).Value = session.SalesTotal;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = session.TransactionCount;
                ws.Cell(row, 5).Value = session.CashReceived;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Value = session.DurationFormatted;
                row++;
            }
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] ExportToCsv(XReportData report)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"X-Report: {report.ReportNumber}");
        sb.AppendLine($"Business: {report.BusinessName}");
        sb.AppendLine($"Terminal: {report.TerminalName} ({report.TerminalCode})");
        sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Generated By: {report.GeneratedByName}");
        sb.AppendLine($"Shift Started: {report.ShiftStarted:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Duration: {report.ShiftDurationFormatted}");
        sb.AppendLine();

        // Sales Summary
        sb.AppendLine("SALES SUMMARY");
        sb.AppendLine("Item,Amount");
        sb.AppendLine($"Gross Sales,{report.GrossSales:F2}");
        sb.AppendLine($"Discounts,{-report.Discounts:F2}");
        sb.AppendLine($"Refunds,{-report.Refunds:F2}");
        sb.AppendLine($"Voids,{-report.Voids:F2}");
        sb.AppendLine($"Net Sales,{report.NetSales:F2}");
        sb.AppendLine($"Tax Amount,{report.TaxAmount:F2}");
        sb.AppendLine($"Tips Collected,{report.TipsCollected:F2}");
        sb.AppendLine($"Grand Total,{report.GrandTotal:F2}");
        sb.AppendLine();

        // Payment Breakdown
        sb.AppendLine("PAYMENT BREAKDOWN");
        sb.AppendLine("Payment Method,Amount,Count,Percentage");
        foreach (var payment in report.PaymentBreakdown)
        {
            sb.AppendLine($"{payment.PaymentMethodName},{payment.Amount:F2},{payment.TransactionCount},{payment.Percentage:F1}%");
        }
        sb.AppendLine($"Total Payments,{report.TotalPayments:F2},,");
        sb.AppendLine();

        // Cash Drawer
        sb.AppendLine("CASH DRAWER");
        sb.AppendLine("Item,Amount");
        sb.AppendLine($"Opening Float,{report.OpeningFloat:F2}");
        sb.AppendLine($"Cash Received,{report.CashReceived:F2}");
        sb.AppendLine($"Cash Refunds,{-report.CashRefunds:F2}");
        sb.AppendLine($"Cash Payouts,{-report.CashPayouts:F2}");
        sb.AppendLine($"Expected Cash,{report.ExpectedCash:F2}");
        sb.AppendLine();

        // Statistics
        sb.AppendLine("STATISTICS");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Transaction Count,{report.TransactionCount}");
        sb.AppendLine($"Average Transaction,{report.AverageTransaction:F2}");
        sb.AppendLine($"Customer Count,{report.CustomerCount}");
        sb.AppendLine($"Void Count,{report.VoidCount}");
        sb.AppendLine($"Refund Count,{report.RefundCount}");
        sb.AppendLine($"Discount Count,{report.DiscountCount}");
        sb.AppendLine($"Drawer Open Count,{report.DrawerOpenCount}");
        sb.AppendLine();

        // Cashier Sessions
        if (report.CashierSessions.Count > 0)
        {
            sb.AppendLine("CASHIER SESSIONS");
            sb.AppendLine("Cashier,Start Time,Sales,Transactions,Cash,Duration");
            foreach (var session in report.CashierSessions)
            {
                sb.AppendLine($"{session.CashierName},{session.StartTime:HH:mm},{session.SalesTotal:F2},{session.TransactionCount},{session.CashReceived:F2},{session.DurationFormatted}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] ExportToPdf(XReportData report)
    {
        // PDF generation requires a PDF library (e.g., QuestPDF, iTextSharp)
        // For now, generate a basic HTML-like text that could be converted
        var sb = new StringBuilder();
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine(report.BusinessName.PadLeft(25 + report.BusinessName.Length / 2));
        sb.AppendLine($"X-Report: {report.ReportNumber}".PadLeft(25 + 10));
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"Terminal: {report.TerminalName} ({report.TerminalCode})");
        sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"By: {report.GeneratedByName}");
        sb.AppendLine($"Shift: {report.ShiftStarted:HH:mm} - Duration: {report.ShiftDurationFormatted}");
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine();
        sb.AppendLine("SALES SUMMARY");
        sb.AppendLine($"  Gross Sales:      {report.GrossSales,15:N2}");
        sb.AppendLine($"  Discounts:        {-report.Discounts,15:N2}");
        sb.AppendLine($"  Refunds:          {-report.Refunds,15:N2}");
        sb.AppendLine($"  Net Sales:        {report.NetSales,15:N2}");
        sb.AppendLine($"  Tax:              {report.TaxAmount,15:N2}");
        sb.AppendLine($"  GRAND TOTAL:      {report.GrandTotal,15:N2}");
        sb.AppendLine();
        sb.AppendLine("PAYMENTS");
        foreach (var p in report.PaymentBreakdown)
        {
            sb.AppendLine($"  {p.PaymentMethodName,-15} {p.Amount,12:N2} ({p.TransactionCount})");
        }
        sb.AppendLine($"  {"TOTAL",-15} {report.TotalPayments,12:N2}");
        sb.AppendLine();
        sb.AppendLine("CASH DRAWER");
        sb.AppendLine($"  Opening Float:    {report.OpeningFloat,15:N2}");
        sb.AppendLine($"  Cash In:          {report.CashReceived,15:N2}");
        sb.AppendLine($"  Cash Out:         {-(report.CashRefunds + report.CashPayouts),15:N2}");
        sb.AppendLine($"  Expected:         {report.ExpectedCash,15:N2}");
        sb.AppendLine();
        sb.AppendLine($"Transactions: {report.TransactionCount}  |  Avg: {report.AverageTransaction:N2}");
        sb.AppendLine("=".PadRight(50, '='));

        _logger.Warning("PDF export not fully implemented - returning text format. Consider adding QuestPDF library.");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] ExportToThermalPrint(XReportData report)
    {
        // Generate ESC/POS commands for thermal printing
        // This is a simplified version - actual implementation would use EscPosPrintDocument
        var sb = new StringBuilder();
        sb.AppendLine(report.BusinessName);
        sb.AppendLine($"X-Report: {report.ReportNumber}");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Terminal: {report.TerminalCode}");
        sb.AppendLine($"Generated: {report.GeneratedAt:dd/MM/yy HH:mm}");
        sb.AppendLine($"By: {report.GeneratedByName}");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("SALES SUMMARY");
        sb.AppendLine($"Gross:      {report.GrossSales,10:N2}");
        sb.AppendLine($"Discounts:  {-report.Discounts,10:N2}");
        sb.AppendLine($"Net Sales:  {report.NetSales,10:N2}");
        sb.AppendLine($"Tax:        {report.TaxAmount,10:N2}");
        sb.AppendLine($"TOTAL:      {report.GrandTotal,10:N2}");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("PAYMENTS");
        foreach (var p in report.PaymentBreakdown)
        {
            sb.AppendLine($"{p.PaymentMethodName,-12}{p.Amount,10:N2}");
        }
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("CASH DRAWER");
        sb.AppendLine($"Opening:    {report.OpeningFloat,10:N2}");
        sb.AppendLine($"Cash In:    {report.CashReceived,10:N2}");
        sb.AppendLine($"Expected:   {report.ExpectedCash,10:N2}");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Trans: {report.TransactionCount}  Avg: {report.AverageTransaction:N2}");
        sb.AppendLine();
        sb.AppendLine();

        return Encoding.UTF8.GetBytes(sb.ToString());
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
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod?.Name, p.PaymentMethod?.Code, p.PaymentMethod?.Type })
            .Select(g => new Core.DTOs.PaymentMethodSummary
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name ?? "Unknown",
                PaymentMethodCode = g.Key.Code ?? "UNKNOWN",
                PaymentMethodType = g.Key.Type ?? PaymentMethodType.Cash,
                Amount = g.Sum(p => p.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(p => p.PaymentMethodType)
            .ThenBy(p => p.PaymentMethodName)
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

        // Build PaymentTypeBreakdown (grouped by type)
        var typeBreakdown = breakdown
            .GroupBy(p => p.PaymentMethodType)
            .Select(g => new Core.DTOs.PaymentTypeBreakdown
            {
                PaymentType = g.Key,
                PaymentTypeName = Core.DTOs.PaymentTypeBreakdown.GetPaymentTypeName(g.Key),
                TotalAmount = g.Sum(m => m.Amount),
                TotalTransactionCount = g.Sum(m => m.TransactionCount),
                Methods = g.ToList()
            })
            .OrderBy(t => t.PaymentType)
            .ToList();

        // Calculate type percentages
        foreach (var item in typeBreakdown)
        {
            item.Percentage = report.TotalPayments > 0
                ? Math.Round(item.TotalAmount / report.TotalPayments * 100, 1)
                : 0;
        }

        report.PaymentTypeBreakdown = typeBreakdown;
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
