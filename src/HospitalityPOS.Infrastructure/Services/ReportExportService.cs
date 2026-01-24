using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for exporting X-Reports and Z-Reports to various formats.
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly IExportService _exportService;
    private readonly ILogger<ReportExportService> _logger;

    public ReportExportService(
        IExportService exportService,
        ILogger<ReportExportService> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> ExportXReportAsync(
        XReportData report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        var fileName = filePath ?? GetExportFilePath(GetXReportFileName(report, format));
        if (string.IsNullOrEmpty(fileName)) return null;

        _logger.LogInformation("Exporting X-Report {ReportNumber} to {Format}: {Path}",
            report.ReportNumber, format, fileName);

        try
        {
            switch (format)
            {
                case ExportFormat.Csv:
                    await ExportXReportToCsvAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Excel:
                    await ExportXReportToExcelAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Pdf:
                    await ExportXReportToPdfAsync(report, fileName, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            _logger.LogInformation("X-Report exported successfully: {Path}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export X-Report to {Format}", format);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ExportZReportAsync(
        ZReport report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        var fileName = filePath ?? GetExportFilePath(GetZReportFileName(report, format));
        if (string.IsNullOrEmpty(fileName)) return null;

        _logger.LogInformation("Exporting Z-Report {ReportNumber} to {Format}: {Path}",
            report.ZReportNumber, format, fileName);

        try
        {
            switch (format)
            {
                case ExportFormat.Csv:
                    await ExportZReportToCsvAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Excel:
                    await ExportZReportToExcelAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Pdf:
                    await ExportZReportToPdfAsync(report, fileName, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            _logger.LogInformation("Z-Report exported successfully: {Path}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Z-Report to {Format}", format);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ExportCombinedXReportAsync(
        CombinedXReportData report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        var defaultFileName = $"CombinedXReport_{report.ReportNumber}_{DateTime.Now:yyyyMMdd_HHmm}.{GetExtension(format)}";
        var fileName = filePath ?? GetExportFilePath(defaultFileName);
        if (string.IsNullOrEmpty(fileName)) return null;

        _logger.LogInformation("Exporting Combined X-Report to {Format}: {Path}", format, fileName);

        try
        {
            switch (format)
            {
                case ExportFormat.Csv:
                    await ExportCombinedXReportToCsvAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Excel:
                    await ExportCombinedXReportToExcelAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Pdf:
                    await ExportCombinedXReportToPdfAsync(report, fileName, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Combined X-Report to {Format}", format);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ExportCombinedZReportAsync(
        CombinedZReportPreview report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        var defaultFileName = $"CombinedZReport_{DateTime.Now:yyyyMMdd_HHmm}.{GetExtension(format)}";
        var fileName = filePath ?? GetExportFilePath(defaultFileName);
        if (string.IsNullOrEmpty(fileName)) return null;

        _logger.LogInformation("Exporting Combined Z-Report to {Format}: {Path}", format, fileName);

        try
        {
            switch (format)
            {
                case ExportFormat.Csv:
                    await ExportCombinedZReportToCsvAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Excel:
                    await ExportCombinedZReportToExcelAsync(report, fileName, cancellationToken);
                    break;
                case ExportFormat.Pdf:
                    await ExportCombinedZReportToPdfAsync(report, fileName, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Combined Z-Report to {Format}", format);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetXReportFileName(XReportData report, ExportFormat format)
    {
        var terminalCode = !string.IsNullOrEmpty(report.TerminalCode) ? $"_{report.TerminalCode}" : "";
        return $"XReport{terminalCode}_{report.GeneratedAt:yyyyMMdd_HHmm}.{GetExtension(format)}";
    }

    /// <inheritdoc />
    public string GetZReportFileName(ZReport report, ExportFormat format)
    {
        var terminalCode = !string.IsNullOrEmpty(report.TerminalCode) ? $"_{report.TerminalCode}" : "";
        return $"ZReport{terminalCode}_{report.WorkPeriodClosedAt:yyyyMMdd}.{GetExtension(format)}";
    }

    #region X-Report Export Methods

    private async Task ExportXReportToCsvAsync(XReportData report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildXReportDataTable(report);
        await _exportService.ExportToCsvAsync(dataTable, filePath, ct);
    }

    private async Task ExportXReportToExcelAsync(XReportData report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildXReportDataTable(report);
        await _exportService.ExportToExcelAsync(dataTable, filePath, "X-Report", ct);
    }

    private Task ExportXReportToPdfAsync(XReportData report, string filePath, CancellationToken ct)
    {
        // Generate HTML content for PDF
        var htmlContent = GenerateXReportHtml(report);
        return SaveHtmlAsPdfAsync(htmlContent, filePath, ct);
    }

    private DataTable BuildXReportDataTable(XReportData report)
    {
        var dt = new DataTable("X-Report");

        // Summary section
        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Value", typeof(string));

        // Header info
        dt.Rows.Add("Header", "Business Name", report.BusinessName);
        dt.Rows.Add("Header", "Terminal", $"{report.TerminalCode} - {report.TerminalName}");
        dt.Rows.Add("Header", "Report Number", report.ReportNumber);
        dt.Rows.Add("Header", "Generated At", report.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        dt.Rows.Add("Header", "Generated By", report.GeneratedByName);

        // Shift info
        dt.Rows.Add("Shift", "Started", report.ShiftStarted.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        dt.Rows.Add("Shift", "Duration", report.ShiftDurationFormatted);

        // Sales summary
        dt.Rows.Add("Sales", "Gross Sales", $"KSh {report.GrossSales:N2}");
        dt.Rows.Add("Sales", "Discounts", $"-KSh {report.Discounts:N2}");
        dt.Rows.Add("Sales", "Refunds", $"-KSh {report.Refunds:N2}");
        dt.Rows.Add("Sales", "Net Sales", $"KSh {report.NetSales:N2}");
        dt.Rows.Add("Sales", "Tax", $"KSh {report.TaxAmount:N2}");
        dt.Rows.Add("Sales", "Tips", $"KSh {report.TipsCollected:N2}");
        dt.Rows.Add("Sales", "Grand Total", $"KSh {report.GrandTotal:N2}");

        // Payment breakdown
        foreach (var payment in report.PaymentBreakdown)
        {
            dt.Rows.Add("Payments", $"{payment.PaymentMethodName} ({payment.TransactionCount})", $"KSh {payment.Amount:N2}");
        }

        // Statistics
        dt.Rows.Add("Statistics", "Transaction Count", report.TransactionCount.ToString());
        dt.Rows.Add("Statistics", "Average Transaction", $"KSh {report.AverageTransaction:N2}");
        dt.Rows.Add("Statistics", "Void Count", report.VoidCount.ToString());
        dt.Rows.Add("Statistics", "Refund Count", report.RefundCount.ToString());

        // Cash drawer
        dt.Rows.Add("Cash Drawer", "Opening Float", $"KSh {report.OpeningFloat:N2}");
        dt.Rows.Add("Cash Drawer", "Cash Received", $"KSh {report.CashReceived:N2}");
        dt.Rows.Add("Cash Drawer", "Cash Refunds", $"-KSh {report.CashRefunds:N2}");
        dt.Rows.Add("Cash Drawer", "Cash Payouts", $"-KSh {report.CashPayouts:N2}");
        dt.Rows.Add("Cash Drawer", "Expected Cash", $"KSh {report.ExpectedCash:N2}");

        return dt;
    }

    private string GenerateXReportHtml(XReportData report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='UTF-8'><title>X-Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #2c3e50; text-align: center; }");
        sb.AppendLine("h2 { color: #27ae60; border-bottom: 2px solid #27ae60; padding-bottom: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        sb.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #27ae60; color: white; }");
        sb.AppendLine(".total { font-weight: bold; font-size: 1.2em; }");
        sb.AppendLine(".amount { text-align: right; font-family: Consolas, monospace; }");
        sb.AppendLine(".header-info { text-align: center; margin-bottom: 20px; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine($"<div class='header-info'>");
        sb.AppendLine($"<h1>{report.BusinessName}</h1>");
        sb.AppendLine($"<h2>X-REPORT</h2>");
        sb.AppendLine($"<p>Terminal: {report.TerminalCode} ({report.TerminalName})</p>");
        sb.AppendLine($"<p>Report #: {report.ReportNumber}</p>");
        sb.AppendLine($"<p>Generated: {report.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm} by {report.GeneratedByName}</p>");
        sb.AppendLine($"</div>");

        // Sales Summary
        sb.AppendLine("<h2>Sales Summary</h2>");
        sb.AppendLine("<table><tr><th>Item</th><th class='amount'>Amount</th></tr>");
        sb.AppendLine($"<tr><td>Gross Sales</td><td class='amount'>KSh {report.GrossSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Discounts</td><td class='amount'>-KSh {report.Discounts:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Refunds</td><td class='amount'>-KSh {report.Refunds:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Net Sales</td><td class='amount'>KSh {report.NetSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Tax</td><td class='amount'>KSh {report.TaxAmount:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>GRAND TOTAL</td><td class='amount'>KSh {report.GrandTotal:N2}</td></tr>");
        sb.AppendLine("</table>");

        // Payment Breakdown
        sb.AppendLine("<h2>Payment Breakdown</h2>");
        sb.AppendLine("<table><tr><th>Payment Method</th><th>Count</th><th class='amount'>Amount</th></tr>");
        foreach (var payment in report.PaymentBreakdown)
        {
            sb.AppendLine($"<tr><td>{payment.PaymentMethodName}</td><td>{payment.TransactionCount}</td><td class='amount'>KSh {payment.Amount:N2}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Cash Drawer
        sb.AppendLine("<h2>Cash Drawer</h2>");
        sb.AppendLine("<table><tr><th>Item</th><th class='amount'>Amount</th></tr>");
        sb.AppendLine($"<tr><td>Opening Float</td><td class='amount'>KSh {report.OpeningFloat:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Cash Received</td><td class='amount'>KSh {report.CashReceived:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Cash Refunds</td><td class='amount'>-KSh {report.CashRefunds:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Cash Payouts</td><td class='amount'>-KSh {report.CashPayouts:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>EXPECTED CASH</td><td class='amount'>KSh {report.ExpectedCash:N2}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<p style='text-align: center; margin-top: 30px;'>*** End of X-Report ***</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    #endregion

    #region Z-Report Export Methods

    private async Task ExportZReportToCsvAsync(ZReport report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildZReportDataTable(report);
        await _exportService.ExportToCsvAsync(dataTable, filePath, ct);
    }

    private async Task ExportZReportToExcelAsync(ZReport report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildZReportDataTable(report);
        await _exportService.ExportToExcelAsync(dataTable, filePath, "Z-Report", ct);
    }

    private Task ExportZReportToPdfAsync(ZReport report, string filePath, CancellationToken ct)
    {
        var htmlContent = GenerateZReportHtml(report);
        return SaveHtmlAsPdfAsync(htmlContent, filePath, ct);
    }

    private DataTable BuildZReportDataTable(ZReport report)
    {
        var dt = new DataTable("Z-Report");

        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Value", typeof(string));

        // Header info
        dt.Rows.Add("Header", "Business Name", report.BusinessName);
        dt.Rows.Add("Header", "Terminal", $"{report.TerminalCode} - {report.TerminalName}");
        dt.Rows.Add("Header", "Z-Report Number", report.ReportNumberFormatted);
        dt.Rows.Add("Header", "Period Opened", report.WorkPeriodOpenedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        dt.Rows.Add("Header", "Period Closed", report.WorkPeriodClosedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        dt.Rows.Add("Header", "Duration", $"{(int)report.Duration.TotalHours}h {report.Duration.Minutes}m");

        // Sales summary
        dt.Rows.Add("Sales", "Gross Sales", $"KSh {report.GrossSales:N2}");
        dt.Rows.Add("Sales", "Discounts", $"-KSh {report.TotalDiscounts:N2}");
        dt.Rows.Add("Sales", "Net Sales", $"KSh {report.NetSales:N2}");
        dt.Rows.Add("Sales", "Tax", $"KSh {report.TaxCollected:N2}");
        dt.Rows.Add("Sales", "Grand Total", $"KSh {report.GrandTotal:N2}");

        // Payment methods
        foreach (var pm in report.SalesByPaymentMethod)
        {
            dt.Rows.Add("Payments", $"{pm.PaymentMethod} ({pm.TransactionCount})", $"KSh {pm.TotalAmount:N2}");
        }

        // Cash drawer
        dt.Rows.Add("Cash Drawer", "Opening Float", $"KSh {report.OpeningFloat:N2}");
        dt.Rows.Add("Cash Drawer", "Cash Sales", $"KSh {report.CashSales:N2}");
        dt.Rows.Add("Cash Drawer", "Cash Payouts", $"-KSh {report.CashPayouts:N2}");
        dt.Rows.Add("Cash Drawer", "Expected Cash", $"KSh {report.ExpectedCash:N2}");
        dt.Rows.Add("Cash Drawer", "Actual Cash", $"KSh {report.ActualCash:N2}");
        dt.Rows.Add("Cash Drawer", "Variance", $"KSh {report.Variance:N2}");
        dt.Rows.Add("Cash Drawer", "Status", report.VarianceStatus);

        // Statistics
        dt.Rows.Add("Statistics", "Settled Receipts", $"{report.SettledReceiptsCount} (KSh {report.SettledReceiptsTotal:N2})");
        dt.Rows.Add("Statistics", "Pending Receipts", $"{report.PendingReceiptsCount} (KSh {report.PendingReceiptsTotal:N2})");
        dt.Rows.Add("Statistics", "Voids", $"{report.VoidCount} (KSh {report.VoidTotal:N2})");

        return dt;
    }

    private string GenerateZReportHtml(ZReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='UTF-8'><title>Z-Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #2c3e50; text-align: center; }");
        sb.AppendLine("h2 { color: #f39c12; border-bottom: 2px solid #f39c12; padding-bottom: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        sb.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #f39c12; color: white; }");
        sb.AppendLine(".total { font-weight: bold; font-size: 1.2em; }");
        sb.AppendLine(".amount { text-align: right; font-family: Consolas, monospace; }");
        sb.AppendLine(".header-info { text-align: center; margin-bottom: 20px; }");
        sb.AppendLine(".variance-exact { color: #27ae60; font-weight: bold; }");
        sb.AppendLine(".variance-over { color: #3498db; font-weight: bold; }");
        sb.AppendLine(".variance-short { color: #e74c3c; font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine($"<div class='header-info'>");
        sb.AppendLine($"<h1>{report.BusinessName}</h1>");
        sb.AppendLine($"<h2 style='color: #f39c12;'>*** Z-REPORT ***</h2>");
        sb.AppendLine($"<p>Terminal: {report.TerminalCode} ({report.TerminalName})</p>");
        sb.AppendLine($"<p>Z-Report #: {report.ReportNumberFormatted}</p>");
        sb.AppendLine($"<p>Period: {report.WorkPeriodOpenedAt.ToLocalTime():yyyy-MM-dd HH:mm} - {report.WorkPeriodClosedAt.ToLocalTime():yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine($"</div>");

        // Sales Summary
        sb.AppendLine("<h2>Sales Summary</h2>");
        sb.AppendLine("<table><tr><th>Item</th><th class='amount'>Amount</th></tr>");
        sb.AppendLine($"<tr><td>Gross Sales</td><td class='amount'>KSh {report.GrossSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Discounts</td><td class='amount'>-KSh {report.TotalDiscounts:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Net Sales</td><td class='amount'>KSh {report.NetSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Tax Collected</td><td class='amount'>KSh {report.TaxCollected:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>GRAND TOTAL</td><td class='amount'>KSh {report.GrandTotal:N2}</td></tr>");
        sb.AppendLine("</table>");

        // Payment Methods
        sb.AppendLine("<h2>Payment Methods</h2>");
        sb.AppendLine("<table><tr><th>Payment Method</th><th>Count</th><th class='amount'>Amount</th></tr>");
        foreach (var pm in report.SalesByPaymentMethod)
        {
            sb.AppendLine($"<tr><td>{pm.PaymentMethod}</td><td>{pm.TransactionCount}</td><td class='amount'>KSh {pm.TotalAmount:N2}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Cash Drawer
        var varianceClass = report.IsShort ? "variance-short" : (report.IsOver ? "variance-over" : "variance-exact");
        sb.AppendLine("<h2>Cash Drawer</h2>");
        sb.AppendLine("<table><tr><th>Item</th><th class='amount'>Amount</th></tr>");
        sb.AppendLine($"<tr><td>Opening Float</td><td class='amount'>KSh {report.OpeningFloat:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Cash Sales</td><td class='amount'>KSh {report.CashSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Cash Payouts</td><td class='amount'>-KSh {report.CashPayouts:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>EXPECTED CASH</td><td class='amount'>KSh {report.ExpectedCash:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Actual Cash</td><td class='amount'>KSh {report.ActualCash:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>VARIANCE</td><td class='amount {varianceClass}'>KSh {report.Variance:N2} ({report.VarianceStatus})</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<p style='text-align: center; margin-top: 30px; font-weight: bold; color: #f39c12;'>*** END OF Z-REPORT ***</p>");
        sb.AppendLine("<p style='text-align: center;'>This is an official document</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    #endregion

    #region Combined Report Export Methods

    private async Task ExportCombinedXReportToCsvAsync(CombinedXReportData report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildCombinedXReportDataTable(report);
        await _exportService.ExportToCsvAsync(dataTable, filePath, ct);
    }

    private async Task ExportCombinedXReportToExcelAsync(CombinedXReportData report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildCombinedXReportDataTable(report);
        await _exportService.ExportToExcelAsync(dataTable, filePath, "Combined X-Report", ct);
    }

    private Task ExportCombinedXReportToPdfAsync(CombinedXReportData report, string filePath, CancellationToken ct)
    {
        var htmlContent = GenerateCombinedXReportHtml(report);
        return SaveHtmlAsPdfAsync(htmlContent, filePath, ct);
    }

    private DataTable BuildCombinedXReportDataTable(CombinedXReportData report)
    {
        var dt = new DataTable("Combined X-Report");

        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Value", typeof(string));

        // Header
        dt.Rows.Add("Header", "Business Name", report.BusinessName);
        dt.Rows.Add("Header", "Report Number", report.ReportNumber);
        dt.Rows.Add("Header", "Terminals", report.TerminalCount.ToString());
        dt.Rows.Add("Header", "Generated At", report.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

        // Combined Sales
        dt.Rows.Add("Sales", "Gross Sales", $"KSh {report.GrossSales:N2}");
        dt.Rows.Add("Sales", "Discounts", $"-KSh {report.Discounts:N2}");
        dt.Rows.Add("Sales", "Refunds", $"-KSh {report.Refunds:N2}");
        dt.Rows.Add("Sales", "Net Sales", $"KSh {report.NetSales:N2}");
        dt.Rows.Add("Sales", "Tax", $"KSh {report.TaxAmount:N2}");
        dt.Rows.Add("Sales", "Grand Total", $"KSh {report.GrandTotal:N2}");

        // Terminal breakdown
        foreach (var terminal in report.TerminalBreakdown)
        {
            dt.Rows.Add($"Terminal {terminal.TerminalCode}", "Net Sales", $"KSh {terminal.NetSales:N2}");
            dt.Rows.Add($"Terminal {terminal.TerminalCode}", "Transactions", terminal.TransactionCount.ToString());
        }

        // Payment breakdown
        foreach (var pm in report.PaymentBreakdown)
        {
            dt.Rows.Add("Payments", $"{pm.PaymentMethodName} ({pm.TransactionCount})", $"KSh {pm.Amount:N2}");
        }

        return dt;
    }

    private string GenerateCombinedXReportHtml(CombinedXReportData report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='UTF-8'><title>Combined X-Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #2c3e50; text-align: center; }");
        sb.AppendLine("h2 { color: #27ae60; border-bottom: 2px solid #27ae60; padding-bottom: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        sb.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #27ae60; color: white; }");
        sb.AppendLine(".total { font-weight: bold; }");
        sb.AppendLine(".amount { text-align: right; font-family: Consolas, monospace; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>{report.BusinessName}</h1>");
        sb.AppendLine($"<h2 style='text-align: center;'>COMBINED X-REPORT - {report.TerminalCount} Terminals</h2>");
        sb.AppendLine($"<p style='text-align: center;'>Report #: {report.ReportNumber} | Generated: {report.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm}</p>");

        // Combined totals
        sb.AppendLine("<h2>Combined Sales Summary</h2>");
        sb.AppendLine("<table><tr><th>Item</th><th class='amount'>Amount</th></tr>");
        sb.AppendLine($"<tr><td>Gross Sales</td><td class='amount'>KSh {report.GrossSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Net Sales</td><td class='amount'>KSh {report.NetSales:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>GRAND TOTAL</td><td class='amount'>KSh {report.GrandTotal:N2}</td></tr>");
        sb.AppendLine("</table>");

        // Terminal breakdown
        sb.AppendLine("<h2>Terminal Breakdown</h2>");
        sb.AppendLine("<table><tr><th>Terminal</th><th>Transactions</th><th class='amount'>Net Sales</th><th class='amount'>Grand Total</th></tr>");
        foreach (var t in report.TerminalBreakdown)
        {
            sb.AppendLine($"<tr><td>{t.TerminalCode} - {t.TerminalName}</td><td>{t.TransactionCount}</td><td class='amount'>KSh {t.NetSales:N2}</td><td class='amount'>KSh {t.GrandTotal:N2}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private async Task ExportCombinedZReportToCsvAsync(CombinedZReportPreview report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildCombinedZReportDataTable(report);
        await _exportService.ExportToCsvAsync(dataTable, filePath, ct);
    }

    private async Task ExportCombinedZReportToExcelAsync(CombinedZReportPreview report, string filePath, CancellationToken ct)
    {
        var dataTable = BuildCombinedZReportDataTable(report);
        await _exportService.ExportToExcelAsync(dataTable, filePath, "Combined Z-Report", ct);
    }

    private Task ExportCombinedZReportToPdfAsync(CombinedZReportPreview report, string filePath, CancellationToken ct)
    {
        var htmlContent = GenerateCombinedZReportHtml(report);
        return SaveHtmlAsPdfAsync(htmlContent, filePath, ct);
    }

    private DataTable BuildCombinedZReportDataTable(CombinedZReportPreview report)
    {
        var dt = new DataTable("Combined Z-Report");

        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Value", typeof(string));

        dt.Rows.Add("Header", "Work Period", report.WorkPeriodStart.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        dt.Rows.Add("Header", "Terminals", report.TerminalCount.ToString());
        dt.Rows.Add("Header", "Completed Z-Reports", report.CompletedZReportCount.ToString());
        dt.Rows.Add("Header", "Pending Z-Reports", report.PendingZReportCount.ToString());

        dt.Rows.Add("Sales", "Gross Sales", $"KSh {report.TotalGrossSales:N2}");
        dt.Rows.Add("Sales", "Net Sales", $"KSh {report.TotalNetSales:N2}");
        dt.Rows.Add("Sales", "Grand Total", $"KSh {report.TotalGrandTotal:N2}");
        dt.Rows.Add("Sales", "Transactions", report.TotalTransactionCount.ToString());

        foreach (var t in report.TerminalBreakdown)
        {
            var status = t.HasZReport ? "Completed" : "Pending";
            dt.Rows.Add($"Terminal {t.TerminalCode}", "Status", status);
            dt.Rows.Add($"Terminal {t.TerminalCode}", "Net Sales", $"KSh {t.NetSales:N2}");
            dt.Rows.Add($"Terminal {t.TerminalCode}", "Transactions", t.TransactionCount.ToString());
        }

        return dt;
    }

    private string GenerateCombinedZReportHtml(CombinedZReportPreview report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'><title>Combined Z-Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}h1{text-align:center;}h2{color:#f39c12;}table{width:100%;border-collapse:collapse;margin-bottom:20px;}th,td{padding:8px;border-bottom:1px solid #ddd;}th{background:#f39c12;color:white;}.amount{text-align:right;font-family:Consolas;}</style></head><body>");
        sb.AppendLine($"<h1>COMBINED Z-REPORT</h1>");
        sb.AppendLine($"<p style='text-align:center;'>Work Period: {report.WorkPeriodStart.ToLocalTime():yyyy-MM-dd HH:mm} | Terminals: {report.TerminalCount}</p>");

        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine("<table><tr><th>Metric</th><th class='amount'>Value</th></tr>");
        sb.AppendLine($"<tr><td>Total Gross Sales</td><td class='amount'>KSh {report.TotalGrossSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Total Net Sales</td><td class='amount'>KSh {report.TotalNetSales:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Total Grand Total</td><td class='amount'>KSh {report.TotalGrandTotal:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Total Transactions</td><td class='amount'>{report.TotalTransactionCount}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Terminal Status</h2>");
        sb.AppendLine("<table><tr><th>Terminal</th><th>Status</th><th class='amount'>Net Sales</th></tr>");
        foreach (var t in report.TerminalBreakdown)
        {
            var status = t.HasZReport ? "Completed" : "Pending";
            sb.AppendLine($"<tr><td>{t.TerminalCode} - {t.TerminalName}</td><td>{status}</td><td class='amount'>KSh {t.NetSales:N2}</td></tr>");
        }
        sb.AppendLine("</table></body></html>");

        return sb.ToString();
    }

    #endregion

    #region Helper Methods

    private string GetExportFilePath(string defaultFileName)
    {
        var exportDir = _exportService.GetDefaultExportDirectory();
        return Path.Combine(exportDir, defaultFileName);
    }

    private static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.Csv => "csv",
        ExportFormat.Excel => "xml",
        ExportFormat.Pdf => "html", // HTML that can be printed to PDF
        _ => "txt"
    };

    private async Task SaveHtmlAsPdfAsync(string htmlContent, string filePath, CancellationToken ct)
    {
        // Save as HTML file (can be printed to PDF from browser or viewer)
        // In a production system, you'd use a library like PuppeteerSharp or wkhtmltopdf
        var htmlPath = Path.ChangeExtension(filePath, ".html");
        await File.WriteAllTextAsync(htmlPath, htmlContent, ct);
        _logger.LogInformation("HTML report saved: {Path}. Open in browser to print as PDF.", htmlPath);
    }

    #endregion
}
