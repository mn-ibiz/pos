using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for sending email digests and summaries.
/// </summary>
public class EmailDigestService : IEmailDigestService
{
    private readonly POSDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPurchaseOrderSettingsService _settingsService;
    private readonly ISystemConfigurationService _configService;
    private readonly ILogger _logger;

    public EmailDigestService(
        POSDbContext context,
        IEmailService emailService,
        IPurchaseOrderSettingsService settingsService,
        ISystemConfigurationService configService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DigestEmailResult> SendDailyPendingPOsDigestAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var result = new DigestEmailResult { SentAt = DateTime.UtcNow };

        try
        {
            var recipients = await GetDigestRecipientsAsync(storeId, cancellationToken).ConfigureAwait(false);
            if (!recipients.Any())
            {
                result.ErrorMessage = "No recipients configured for digest emails.";
                return result;
            }

            var preview = await PreviewDigestAsync(DigestType.DailyPendingPOs, storeId, cancellationToken).ConfigureAwait(false);
            if (!preview.HasContent)
            {
                result.Success = true;
                result.ErrorMessage = "No pending POs to report.";
                return result;
            }

            var config = await _configService.GetConfigurationAsync().ConfigureAwait(false);
            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                Subject = preview.Subject,
                HtmlBody = preview.HtmlContent,
                PlainTextBody = preview.PlainTextContent,
                ReportType = EmailReportType.Custom
            };

            var sendResult = await _emailService.SendEmailAsync(message).ConfigureAwait(false);
            result.Success = sendResult.Success;
            result.RecipientCount = recipients.Count;
            result.Recipients = recipients;

            if (!sendResult.Success)
            {
                result.ErrorMessage = sendResult.ErrorMessage;
            }

            _logger.Information("Daily pending POs digest sent to {Count} recipients", result.RecipientCount);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Failed to send daily pending POs digest");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<DigestEmailResult> SendWeeklySummaryAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var result = new DigestEmailResult { SentAt = DateTime.UtcNow };

        try
        {
            var recipients = await GetDigestRecipientsAsync(storeId, cancellationToken).ConfigureAwait(false);
            if (!recipients.Any())
            {
                result.ErrorMessage = "No recipients configured for digest emails.";
                return result;
            }

            var preview = await PreviewDigestAsync(DigestType.WeeklySummary, storeId, cancellationToken).ConfigureAwait(false);

            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                Subject = preview.Subject,
                HtmlBody = preview.HtmlContent,
                PlainTextBody = preview.PlainTextContent,
                ReportType = EmailReportType.Custom
            };

            var sendResult = await _emailService.SendEmailAsync(message).ConfigureAwait(false);
            result.Success = sendResult.Success;
            result.RecipientCount = recipients.Count;
            result.Recipients = recipients;

            if (!sendResult.Success)
            {
                result.ErrorMessage = sendResult.ErrorMessage;
            }

            _logger.Information("Weekly summary digest sent to {Count} recipients", result.RecipientCount);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Failed to send weekly summary digest");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<DigestEmailResult> SendLowStockAlertAsync(IEnumerable<Product> lowStockProducts, int? storeId = null, CancellationToken cancellationToken = default)
    {
        var result = new DigestEmailResult { SentAt = DateTime.UtcNow };
        var products = lowStockProducts.ToList();

        if (!products.Any())
        {
            result.Success = true;
            result.ErrorMessage = "No low stock products to report.";
            return result;
        }

        try
        {
            var recipients = await GetDigestRecipientsAsync(storeId, cancellationToken).ConfigureAwait(false);
            if (!recipients.Any())
            {
                result.ErrorMessage = "No recipients configured for digest emails.";
                return result;
            }

            var config = await _configService.GetConfigurationAsync().ConfigureAwait(false);
            var html = GenerateLowStockAlertHtml(products, config);
            var plainText = GenerateLowStockAlertPlainText(products, config);

            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                Subject = $"[ALERT] {products.Count} Products with Low Stock - {config?.BusinessName ?? "POS"}",
                HtmlBody = html,
                PlainTextBody = plainText,
                ReportType = EmailReportType.Custom
            };

            var sendResult = await _emailService.SendEmailAsync(message).ConfigureAwait(false);
            result.Success = sendResult.Success;
            result.RecipientCount = recipients.Count;
            result.Recipients = recipients;

            if (!sendResult.Success)
            {
                result.ErrorMessage = sendResult.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Failed to send low stock alert");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<DigestEmailResult> SendOverduePOsReminderAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var result = new DigestEmailResult { SentAt = DateTime.UtcNow };

        try
        {
            var recipients = await GetDigestRecipientsAsync(storeId, cancellationToken).ConfigureAwait(false);
            if (!recipients.Any())
            {
                result.ErrorMessage = "No recipients configured for digest emails.";
                return result;
            }

            var preview = await PreviewDigestAsync(DigestType.OverduePOs, storeId, cancellationToken).ConfigureAwait(false);
            if (!preview.HasContent)
            {
                result.Success = true;
                result.ErrorMessage = "No overdue POs to report.";
                return result;
            }

            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                Subject = preview.Subject,
                HtmlBody = preview.HtmlContent,
                PlainTextBody = preview.PlainTextContent,
                ReportType = EmailReportType.Custom
            };

            var sendResult = await _emailService.SendEmailAsync(message).ConfigureAwait(false);
            result.Success = sendResult.Success;
            result.RecipientCount = recipients.Count;
            result.Recipients = recipients;

            if (!sendResult.Success)
            {
                result.ErrorMessage = sendResult.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Failed to send overdue POs reminder");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<DigestPreview> PreviewDigestAsync(DigestType type, int? storeId = null, CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigurationAsync().ConfigureAwait(false);
        var preview = new DigestPreview
        {
            Type = type,
            Recipients = await GetDigestRecipientsAsync(storeId, cancellationToken).ConfigureAwait(false)
        };

        switch (type)
        {
            case DigestType.DailyPendingPOs:
                await GenerateDailyPendingPOsPreviewAsync(preview, config, storeId, cancellationToken).ConfigureAwait(false);
                break;
            case DigestType.WeeklySummary:
                await GenerateWeeklySummaryPreviewAsync(preview, config, storeId, cancellationToken).ConfigureAwait(false);
                break;
            case DigestType.OverduePOs:
                await GenerateOverduePOsPreviewAsync(preview, config, storeId, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        return preview;
    }

    private async Task GenerateDailyPendingPOsPreviewAsync(DigestPreview preview, SystemConfiguration? config, int? storeId, CancellationToken cancellationToken)
    {
        var pendingPOs = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
            .Where(po => po.Status == PurchaseOrderStatus.Draft && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .OrderBy(po => po.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pendingSuggestions = await _context.ReorderSuggestions
            .Include(s => s.Product)
            .Include(s => s.Supplier)
            .Where(s => s.Status == "Pending" && !s.IsDeleted)
            .Where(s => storeId == null || s.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        preview.HasContent = pendingPOs.Any() || pendingSuggestions.Any();
        preview.Subject = $"Daily PO Digest - {pendingPOs.Count} Pending POs, {pendingSuggestions.Count} Suggestions - {config?.BusinessName ?? "POS"}";
        preview.Statistics["PendingPOCount"] = pendingPOs.Count;
        preview.Statistics["PendingSuggestionCount"] = pendingSuggestions.Count;
        preview.Statistics["TotalPendingValue"] = pendingPOs.Sum(po => po.TotalAmount);

        preview.HtmlContent = GenerateDailyDigestHtml(pendingPOs, pendingSuggestions, config);
        preview.PlainTextContent = GenerateDailyDigestPlainText(pendingPOs, pendingSuggestions, config);
    }

    private async Task GenerateWeeklySummaryPreviewAsync(DigestPreview preview, SystemConfiguration? config, int? storeId, CancellationToken cancellationToken)
    {
        var weekStart = DateTime.UtcNow.AddDays(-7);

        var createdPOs = await _context.PurchaseOrders
            .Where(po => po.CreatedAt >= weekStart && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var sentPOs = await _context.PurchaseOrders
            .Where(po => po.Status == PurchaseOrderStatus.Sent && po.UpdatedAt >= weekStart && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var completedPOs = await _context.PurchaseOrders
            .Where(po => po.Status == PurchaseOrderStatus.Complete && po.UpdatedAt >= weekStart && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalValue = await _context.PurchaseOrders
            .Where(po => po.CreatedAt >= weekStart && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .SumAsync(po => po.TotalAmount, cancellationToken)
            .ConfigureAwait(false);

        var suggestionsGenerated = await _context.ReorderSuggestions
            .Where(s => s.CreatedAt >= weekStart && !s.IsDeleted)
            .Where(s => storeId == null || s.StoreId == storeId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        preview.HasContent = true;
        preview.Subject = $"Weekly PO Summary - {config?.BusinessName ?? "POS"}";
        preview.Statistics["CreatedPOs"] = createdPOs;
        preview.Statistics["SentPOs"] = sentPOs;
        preview.Statistics["CompletedPOs"] = completedPOs;
        preview.Statistics["TotalValue"] = totalValue;
        preview.Statistics["SuggestionsGenerated"] = suggestionsGenerated;

        preview.HtmlContent = GenerateWeeklySummaryHtml(preview.Statistics, config);
        preview.PlainTextContent = GenerateWeeklySummaryPlainText(preview.Statistics, config);
    }

    private async Task GenerateOverduePOsPreviewAsync(DigestPreview preview, SystemConfiguration? config, int? storeId, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var overduePOs = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Where(po => po.ExpectedDate.HasValue && po.ExpectedDate.Value.Date < today)
            .Where(po => po.Status != PurchaseOrderStatus.Complete && po.Status != PurchaseOrderStatus.Cancelled && !po.IsDeleted)
            .Where(po => storeId == null || po.StoreId == storeId)
            .OrderBy(po => po.ExpectedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        preview.HasContent = overduePOs.Any();
        preview.Subject = $"[URGENT] {overduePOs.Count} Overdue Purchase Orders - {config?.BusinessName ?? "POS"}";
        preview.Statistics["OverdueCount"] = overduePOs.Count;
        preview.Statistics["TotalOverdueValue"] = overduePOs.Sum(po => po.TotalAmount);

        preview.HtmlContent = GenerateOverduePOsHtml(overduePOs, config);
        preview.PlainTextContent = GenerateOverduePOsPlainText(overduePOs, config);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderSettings?> GetDigestSettingsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        return await _settingsService.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderSettings> SaveDigestSettingsAsync(PurchaseOrderSettings settings, int userId, CancellationToken cancellationToken = default)
    {
        return await _settingsService.SaveSettingsAsync(settings, userId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ShouldSendDailyDigestAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings == null || !settings.SendDailyPendingPODigest)
            return false;

        if (string.IsNullOrEmpty(settings.DailyDigestTime))
            return false;

        var parts = settings.DailyDigestTime.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
            return false;

        var now = DateTime.Now;
        return now.Hour == hour && now.Minute >= minute && now.Minute < minute + 15;
    }

    /// <inheritdoc />
    public async Task<bool> ShouldSendWeeklySummaryAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings == null || !settings.SendWeeklySummary)
            return false;

        var now = DateTime.Now;
        return (int)now.DayOfWeek == settings.WeeklySummaryDay && now.Hour == 8; // 8 AM on configured day
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDigestRecipientsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings == null || string.IsNullOrWhiteSpace(settings.DigestRecipientEmails))
            return new List<string>();

        return settings.DigestRecipientEmails
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();
    }

    #region HTML Generation

    private static string GenerateDailyDigestHtml(List<PurchaseOrder> pendingPOs, List<ReorderSuggestion> suggestions, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #2196F3; color: white; padding: 20px; text-align: center; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
        sb.AppendLine("th { background: #f5f5f5; }");
        sb.AppendLine(".urgent { background: #ffebee; }");
        sb.AppendLine(".total { font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>Daily Purchase Order Digest</h1>");
        sb.AppendLine($"<p>{config?.BusinessName ?? "POS"} - {DateTime.Now:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine("</div>");

        if (pendingPOs.Any())
        {
            sb.AppendLine("<h2>Pending Purchase Orders</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>PO Number</th><th>Supplier</th><th>Items</th><th>Total</th><th>Created</th></tr>");
            foreach (var po in pendingPOs)
            {
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{po.PONumber}</td>");
                sb.AppendLine($"<td>{po.Supplier?.Name ?? "Unknown"}</td>");
                sb.AppendLine($"<td>{po.PurchaseOrderItems.Count}</td>");
                sb.AppendLine($"<td>{config?.CurrencySymbol ?? "KSh"} {po.TotalAmount:N2}</td>");
                sb.AppendLine($"<td>{po.CreatedAt:yyyy-MM-dd}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine($"<tr class='total'><td colspan='3'>Total</td><td colspan='2'>{config?.CurrencySymbol ?? "KSh"} {pendingPOs.Sum(p => p.TotalAmount):N2}</td></tr>");
            sb.AppendLine("</table>");
        }

        if (suggestions.Any())
        {
            sb.AppendLine("<h2>Pending Reorder Suggestions</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Product</th><th>Supplier</th><th>Current Stock</th><th>Suggested Qty</th><th>Est. Cost</th></tr>");
            foreach (var s in suggestions.Take(20))
            {
                var rowClass = s.Priority is "Critical" or "High" ? "class='urgent'" : "";
                sb.AppendLine($"<tr {rowClass}>");
                sb.AppendLine($"<td>{s.Product?.Name ?? "Unknown"}</td>");
                sb.AppendLine($"<td>{s.Supplier?.Name ?? "Not assigned"}</td>");
                sb.AppendLine($"<td>{s.CurrentStock:N0}</td>");
                sb.AppendLine($"<td>{s.SuggestedQuantity:N0}</td>");
                sb.AppendLine($"<td>{config?.CurrencySymbol ?? "KSh"} {s.EstimatedCost:N2}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine($"<p><em>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</em></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GenerateDailyDigestPlainText(List<PurchaseOrder> pendingPOs, List<ReorderSuggestion> suggestions, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"DAILY PURCHASE ORDER DIGEST - {config?.BusinessName ?? "POS"}");
        sb.AppendLine($"Date: {DateTime.Now:dddd, MMMM dd, yyyy}");
        sb.AppendLine(new string('=', 50));

        if (pendingPOs.Any())
        {
            sb.AppendLine($"\nPENDING PURCHASE ORDERS ({pendingPOs.Count})");
            sb.AppendLine(new string('-', 50));
            foreach (var po in pendingPOs)
            {
                sb.AppendLine($"{po.PONumber} - {po.Supplier?.Name ?? "Unknown"} - {config?.CurrencySymbol ?? "KSh"} {po.TotalAmount:N2}");
            }
            sb.AppendLine($"Total: {config?.CurrencySymbol ?? "KSh"} {pendingPOs.Sum(p => p.TotalAmount):N2}");
        }

        if (suggestions.Any())
        {
            sb.AppendLine($"\nPENDING SUGGESTIONS ({suggestions.Count})");
            sb.AppendLine(new string('-', 50));
            foreach (var s in suggestions.Take(20))
            {
                sb.AppendLine($"{s.Product?.Name ?? "Unknown"}: {s.SuggestedQuantity:N0} units @ {config?.CurrencySymbol ?? "KSh"} {s.EstimatedCost:N2}");
            }
        }

        sb.AppendLine($"\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        return sb.ToString();
    }

    private static string GenerateWeeklySummaryHtml(Dictionary<string, object> stats, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #4CAF50; color: white; padding: 20px; text-align: center; }");
        sb.AppendLine(".stat-box { display: inline-block; margin: 10px; padding: 20px; border: 1px solid #ddd; text-align: center; min-width: 150px; }");
        sb.AppendLine(".stat-value { font-size: 2em; font-weight: bold; color: #2196F3; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>Weekly PO Summary</h1>");
        sb.AppendLine($"<p>{config?.BusinessName ?? "POS"} - Week Ending {DateTime.Now:MMMM dd, yyyy}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div style='text-align: center;'>");
        sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{stats["CreatedPOs"]}</div><div>POs Created</div></div>");
        sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{stats["SentPOs"]}</div><div>POs Sent</div></div>");
        sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{stats["CompletedPOs"]}</div><div>POs Completed</div></div>");
        sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{config?.CurrencySymbol ?? "KSh"} {(decimal)stats["TotalValue"]:N0}</div><div>Total Value</div></div>");
        sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{stats["SuggestionsGenerated"]}</div><div>Suggestions Generated</div></div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<p style='text-align: center; color: #666;'><em>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</em></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GenerateWeeklySummaryPlainText(Dictionary<string, object> stats, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"WEEKLY PO SUMMARY - {config?.BusinessName ?? "POS"}");
        sb.AppendLine($"Week Ending: {DateTime.Now:MMMM dd, yyyy}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"POs Created: {stats["CreatedPOs"]}");
        sb.AppendLine($"POs Sent: {stats["SentPOs"]}");
        sb.AppendLine($"POs Completed: {stats["CompletedPOs"]}");
        sb.AppendLine($"Total Value: {config?.CurrencySymbol ?? "KSh"} {(decimal)stats["TotalValue"]:N2}");
        sb.AppendLine($"Suggestions Generated: {stats["SuggestionsGenerated"]}");
        sb.AppendLine($"\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        return sb.ToString();
    }

    private static string GenerateOverduePOsHtml(List<PurchaseOrder> overduePOs, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #F44336; color: white; padding: 20px; text-align: center; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
        sb.AppendLine("th { background: #ffebee; }");
        sb.AppendLine(".days-overdue { color: #F44336; font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>Overdue Purchase Orders Alert</h1>");
        sb.AppendLine($"<p>{overduePOs.Count} POs require attention</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>PO Number</th><th>Supplier</th><th>Expected Date</th><th>Days Overdue</th><th>Total</th></tr>");
        foreach (var po in overduePOs)
        {
            var daysOverdue = (DateTime.Today - po.ExpectedDate!.Value.Date).Days;
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{po.PONumber}</td>");
            sb.AppendLine($"<td>{po.Supplier?.Name ?? "Unknown"}</td>");
            sb.AppendLine($"<td>{po.ExpectedDate:yyyy-MM-dd}</td>");
            sb.AppendLine($"<td class='days-overdue'>{daysOverdue} days</td>");
            sb.AppendLine($"<td>{config?.CurrencySymbol ?? "KSh"} {po.TotalAmount:N2}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine($"<p><em>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</em></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GenerateOverduePOsPlainText(List<PurchaseOrder> overduePOs, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"OVERDUE PURCHASE ORDERS ALERT - {config?.BusinessName ?? "POS"}");
        sb.AppendLine($"{overduePOs.Count} POs require attention");
        sb.AppendLine(new string('=', 50));
        foreach (var po in overduePOs)
        {
            var daysOverdue = (DateTime.Today - po.ExpectedDate!.Value.Date).Days;
            sb.AppendLine($"{po.PONumber} - {po.Supplier?.Name ?? "Unknown"} - {daysOverdue} days overdue - {config?.CurrencySymbol ?? "KSh"} {po.TotalAmount:N2}");
        }
        sb.AppendLine($"\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        return sb.ToString();
    }

    private static string GenerateLowStockAlertHtml(List<Product> products, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #FF9800; color: white; padding: 20px; text-align: center; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
        sb.AppendLine("th { background: #fff3e0; }");
        sb.AppendLine(".critical { background: #ffebee; color: #c62828; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>Low Stock Alert</h1>");
        sb.AppendLine($"<p>{products.Count} products require attention</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Product</th><th>SKU</th><th>Current Stock</th><th>Reorder Level</th></tr>");
        foreach (var p in products)
        {
            var isCritical = p.StockQuantity <= (p.ReorderLevel / 2);
            var rowClass = isCritical ? "class='critical'" : "";
            sb.AppendLine($"<tr {rowClass}>");
            sb.AppendLine($"<td>{p.Name}</td>");
            sb.AppendLine($"<td>{p.SKU}</td>");
            sb.AppendLine($"<td>{p.StockQuantity:N0}</td>");
            sb.AppendLine($"<td>{p.ReorderLevel:N0}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine($"<p><em>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</em></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GenerateLowStockAlertPlainText(List<Product> products, SystemConfiguration? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"LOW STOCK ALERT - {config?.BusinessName ?? "POS"}");
        sb.AppendLine($"{products.Count} products require attention");
        sb.AppendLine(new string('=', 50));
        foreach (var p in products)
        {
            var status = p.StockQuantity <= (p.ReorderLevel / 2) ? "[CRITICAL]" : "";
            sb.AppendLine($"{status} {p.Name} ({p.SKU}): {p.StockQuantity:N0} / {p.ReorderLevel:N0}");
        }
        sb.AppendLine($"\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        return sb.ToString();
    }

    #endregion
}
