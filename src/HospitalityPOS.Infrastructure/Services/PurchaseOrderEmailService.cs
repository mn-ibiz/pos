using System.Text;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for sending Purchase Order emails to suppliers.
/// </summary>
public class PurchaseOrderEmailService : IPurchaseOrderEmailService
{
    private readonly POSDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    public PurchaseOrderEmailService(
        POSDbContext context,
        IEmailService emailService,
        ILogger logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<POEmailLog> SendPurchaseOrderAsync(
        int purchaseOrderId,
        int sentByUserId,
        string[]? additionalRecipients = null,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        // Get recipients
        var recipients = await GetPOEmailRecipientsAsync(po.SupplierId, cancellationToken);
        var allRecipients = recipients.ToList();

        if (additionalRecipients?.Length > 0)
        {
            allRecipients.AddRange(additionalRecipients);
        }

        if (allRecipients.Count == 0)
        {
            throw new InvalidOperationException(
                $"No email recipients found for supplier {po.Supplier.Name}. " +
                "Please configure supplier email addresses.");
        }

        // Get CC addresses
        var ccAddresses = new List<string>();
        if (!string.IsNullOrWhiteSpace(po.Supplier.EmailCcAddresses))
        {
            ccAddresses.AddRange(po.Supplier.EmailCcAddresses
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()));
        }

        // Generate email content
        var preview = await GenerateEmailContentAsync(po, customMessage, cancellationToken);

        // Create log entry
        var emailLog = new POEmailLog
        {
            PurchaseOrderId = purchaseOrderId,
            EmailType = POEmailType.PurchaseOrder,
            Recipients = string.Join(", ", allRecipients),
            CcRecipients = ccAddresses.Count > 0 ? string.Join(", ", ccAddresses) : null,
            Subject = preview.Subject,
            Body = preview.HtmlBody,
            HasPdfAttachment = true,
            AttachmentNames = $"PO-{po.PONumber}.pdf",
            QueuedAt = DateTime.UtcNow,
            SentByUserId = sentByUserId,
            Status = EmailDeliveryStatus.Queued,
            CustomMessage = customMessage
        };

        _context.Set<POEmailLog>().Add(emailLog);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Generate PDF
            var pdfContent = await GeneratePOPdfAsync(purchaseOrderId, cancellationToken);

            // Send email
            var message = new EmailMessageDto
            {
                ToAddresses = allRecipients,
                CcAddresses = ccAddresses,
                Subject = preview.Subject,
                HtmlBody = preview.HtmlBody,
                PlainTextBody = preview.PlainTextBody,
                Attachment = new EmailAttachmentDto
                {
                    FileName = $"PO-{po.PONumber}.pdf",
                    Content = pdfContent,
                    ContentType = "application/pdf"
                },
                StoreId = po.StoreId
            };

            var result = await _emailService.SendEmailAsync(message, cancellationToken);

            if (result.Success)
            {
                emailLog.Status = EmailDeliveryStatus.Sent;
                emailLog.SentAt = DateTime.UtcNow;
                emailLog.MessageId = result.EmailLogId?.ToString();

                // Update PO tracking
                po.LastEmailedAt = DateTime.UtcNow;
                po.EmailCount++;

                _logger.Information("Purchase Order {PONumber} emailed to {Recipients}",
                    po.PONumber, string.Join(", ", allRecipients));
            }
            else
            {
                emailLog.Status = EmailDeliveryStatus.Failed;
                emailLog.ErrorMessage = result.ErrorMessage;

                _logger.Warning("Failed to email Purchase Order {PONumber}: {Error}",
                    po.PONumber, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailDeliveryStatus.Failed;
            emailLog.ErrorMessage = ex.Message;

            _logger.Error(ex, "Exception sending Purchase Order {PONumber} email", po.PONumber);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return emailLog;
    }

    public async Task<POEmailLog> ResendPurchaseOrderAsync(
        int purchaseOrderId,
        int sentByUserId,
        CancellationToken cancellationToken = default)
    {
        return await SendPurchaseOrderAsync(purchaseOrderId, sentByUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<POEmailLog> SendAmendmentNotificationAsync(
        int purchaseOrderId,
        int sentByUserId,
        string changeDescription,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        var recipients = await GetPOEmailRecipientsAsync(po.SupplierId, cancellationToken);
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("No email recipients found for supplier.");
        }

        var subject = $"AMENDED: Purchase Order #{po.PONumber}";
        var body = GenerateAmendmentEmailBody(po, changeDescription);

        var emailLog = await CreateAndSendEmailAsync(
            po, POEmailType.Amendment, recipients.ToList(), subject, body,
            sentByUserId, null, cancellationToken);

        return emailLog;
    }

    public async Task<POEmailLog> SendCancellationNotificationAsync(
        int purchaseOrderId,
        int sentByUserId,
        string cancellationReason,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        var recipients = await GetPOEmailRecipientsAsync(po.SupplierId, cancellationToken);
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("No email recipients found for supplier.");
        }

        var subject = $"CANCELLED: Purchase Order #{po.PONumber}";
        var body = GenerateCancellationEmailBody(po, cancellationReason);

        var emailLog = await CreateAndSendEmailAsync(
            po, POEmailType.Cancellation, recipients.ToList(), subject, body,
            sentByUserId, null, cancellationToken);

        return emailLog;
    }

    public async Task<POEmailLog> SendDeliveryReminderAsync(
        int purchaseOrderId,
        int sentByUserId,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        var recipients = await GetPOEmailRecipientsAsync(po.SupplierId, cancellationToken);
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("No email recipients found for supplier.");
        }

        var subject = $"REMINDER: Delivery Due for Purchase Order #{po.PONumber}";
        var body = GenerateDeliveryReminderBody(po);

        var emailLog = await CreateAndSendEmailAsync(
            po, POEmailType.DeliveryReminder, recipients.ToList(), subject, body,
            sentByUserId, null, cancellationToken);

        return emailLog;
    }

    public async Task<IReadOnlyList<POEmailLog>> GetEmailHistoryAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<POEmailLog>()
            .Include(l => l.SentByUser)
            .Where(l => l.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(l => l.QueuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<POEmailLog?> GetEmailLogByIdAsync(int logId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<POEmailLog>()
            .Include(l => l.PurchaseOrder)
            .Include(l => l.SentByUser)
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);
    }

    public async Task<POEmailPreview> PreviewPOEmailAsync(
        int purchaseOrderId,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        return await GenerateEmailContentAsync(po, customMessage, cancellationToken);
    }

    public async Task<byte[]> GeneratePOPdfAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var po = await GetPurchaseOrderWithDetailsAsync(purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found");

        var html = GeneratePOHtml(po);

        // For now, return HTML as UTF-8 bytes (in production, use a PDF library like QuestPDF or iText)
        // TODO: Integrate proper PDF generation library
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<POEmailLog> RetryFailedEmailAsync(
        int emailLogId,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.Set<POEmailLog>()
            .Include(l => l.PurchaseOrder)
            .FirstOrDefaultAsync(l => l.Id == emailLogId, cancellationToken)
            ?? throw new InvalidOperationException($"Email log {emailLogId} not found");

        if (log.Status != EmailDeliveryStatus.Failed)
        {
            throw new InvalidOperationException("Can only retry failed emails.");
        }

        log.RetryCount++;
        log.Status = EmailDeliveryStatus.Queued;
        log.ErrorMessage = null;

        try
        {
            var recipients = log.Recipients.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();
            var ccRecipients = string.IsNullOrEmpty(log.CcRecipients)
                ? new List<string>()
                : log.CcRecipients.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                CcAddresses = ccRecipients,
                Subject = log.Subject,
                HtmlBody = log.Body ?? "",
                StoreId = log.PurchaseOrder?.StoreId
            };

            if (log.HasPdfAttachment)
            {
                var pdfContent = await GeneratePOPdfAsync(log.PurchaseOrderId, cancellationToken);
                message.Attachment = new EmailAttachmentDto
                {
                    FileName = log.AttachmentNames ?? "PurchaseOrder.pdf",
                    Content = pdfContent,
                    ContentType = "application/pdf"
                };
            }

            var result = await _emailService.SendEmailAsync(message, cancellationToken);

            if (result.Success)
            {
                log.Status = EmailDeliveryStatus.Sent;
                log.SentAt = DateTime.UtcNow;
            }
            else
            {
                log.Status = EmailDeliveryStatus.Failed;
                log.ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            log.Status = EmailDeliveryStatus.Failed;
            log.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<IReadOnlyList<string>> GetPOEmailRecipientsAsync(
        int supplierId,
        CancellationToken cancellationToken = default)
    {
        var recipients = new List<string>();

        var supplier = await _context.Suppliers
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);

        if (supplier == null) return recipients;

        // Get contacts who receive PO emails
        var contactEmails = supplier.Contacts
            .Where(c => c.ReceivesPOEmails && !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => c.Email!)
            .ToList();

        if (contactEmails.Count > 0)
        {
            recipients.AddRange(contactEmails);
        }
        else
        {
            // Fall back to supplier email
            var primaryEmail = supplier.GetPOEmail();
            if (!string.IsNullOrWhiteSpace(primaryEmail))
            {
                recipients.Add(primaryEmail);
            }
        }

        return recipients.Distinct().ToList();
    }

    public async Task<EmailValidationResult> ValidateSupplierEmailConfigAsync(
        int supplierId,
        CancellationToken cancellationToken = default)
    {
        var result = new EmailValidationResult();

        var supplier = await _context.Suppliers
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);

        if (supplier == null)
        {
            result.Issues.Add("Supplier not found.");
            return result;
        }

        var recipients = await GetPOEmailRecipientsAsync(supplierId, cancellationToken);

        if (recipients.Count == 0)
        {
            result.Issues.Add("No valid email addresses configured for this supplier.");
            result.Issues.Add("Please add an email address to the supplier or create contacts with 'Receives PO Emails' enabled.");
        }
        else
        {
            result.IsValid = true;
            result.PrimaryEmail = recipients.First();
            result.RecipientCount = recipients.Count;

            // Check for potential issues
            if (string.IsNullOrWhiteSpace(supplier.Email) &&
                string.IsNullOrWhiteSpace(supplier.OrderEmail))
            {
                result.Warnings.Add("Supplier has no primary email configured. Using contact emails only.");
            }

            if (supplier.Contacts.Count == 0)
            {
                result.Warnings.Add("No contacts configured for this supplier.");
            }
            else if (!supplier.Contacts.Any(c => c.IsPrimaryContact))
            {
                result.Warnings.Add("No primary contact designated for this supplier.");
            }
        }

        return result;
    }

    #region Private Methods

    private async Task<PurchaseOrder?> GetPurchaseOrderWithDetailsAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
                .ThenInclude(s => s.Contacts)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Store)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);
    }

    private async Task<POEmailPreview> GenerateEmailContentAsync(
        PurchaseOrder po,
        string? customMessage,
        CancellationToken cancellationToken)
    {
        var recipients = await GetPOEmailRecipientsAsync(po.SupplierId, cancellationToken);
        var ccAddresses = new List<string>();

        if (!string.IsNullOrWhiteSpace(po.Supplier.EmailCcAddresses))
        {
            ccAddresses.AddRange(po.Supplier.EmailCcAddresses
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()));
        }

        var contactName = po.Supplier.Contacts
            .FirstOrDefault(c => c.IsPrimaryContact)?.Name
            ?? po.Supplier.ContactPerson
            ?? "Team";

        var subject = $"Purchase Order #{po.PONumber} from Your Company";
        var htmlBody = GeneratePOEmailHtml(po, contactName, customMessage);
        var plainTextBody = GeneratePOEmailPlainText(po, contactName, customMessage);

        return new POEmailPreview
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            Recipients = recipients.ToList(),
            CcRecipients = ccAddresses,
            Attachments = [$"PO-{po.PONumber}.pdf"],
            PONumber = po.PONumber,
            SupplierName = po.Supplier.Name,
            TotalAmount = po.TotalAmount,
            ItemCount = po.PurchaseOrderItems.Count
        };
    }

    private async Task<POEmailLog> CreateAndSendEmailAsync(
        PurchaseOrder po,
        POEmailType emailType,
        List<string> recipients,
        string subject,
        string body,
        int sentByUserId,
        byte[]? attachment,
        CancellationToken cancellationToken)
    {
        var emailLog = new POEmailLog
        {
            PurchaseOrderId = po.Id,
            EmailType = emailType,
            Recipients = string.Join(", ", recipients),
            Subject = subject,
            Body = body,
            HasPdfAttachment = attachment != null,
            QueuedAt = DateTime.UtcNow,
            SentByUserId = sentByUserId,
            Status = EmailDeliveryStatus.Queued
        };

        _context.Set<POEmailLog>().Add(emailLog);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var message = new EmailMessageDto
            {
                ToAddresses = recipients,
                Subject = subject,
                HtmlBody = body,
                StoreId = po.StoreId
            };

            if (attachment != null)
            {
                message.Attachment = new EmailAttachmentDto
                {
                    FileName = $"PO-{po.PONumber}.pdf",
                    Content = attachment,
                    ContentType = "application/pdf"
                };
            }

            var result = await _emailService.SendEmailAsync(message, cancellationToken);

            emailLog.Status = result.Success ? EmailDeliveryStatus.Sent : EmailDeliveryStatus.Failed;
            emailLog.SentAt = result.Success ? DateTime.UtcNow : null;
            emailLog.ErrorMessage = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailDeliveryStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return emailLog;
    }

    private string GeneratePOEmailHtml(PurchaseOrder po, string contactName, string? customMessage)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
        html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: #2d2d44; color: white; padding: 20px; text-align: center; }");
        html.AppendLine(".content { padding: 20px; background: #f9f9f9; }");
        html.AppendLine(".summary-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #2d2d44; }");
        html.AppendLine(".footer { padding: 20px; text-align: center; font-size: 12px; color: #666; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
        html.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }");
        html.AppendLine("th { background: #f0f0f0; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='container'>");

        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine($"<h1>Purchase Order #{po.PONumber}</h1>");
        html.AppendLine("</div>");

        // Content
        html.AppendLine("<div class='content'>");
        html.AppendLine($"<p>Dear {contactName},</p>");
        html.AppendLine($"<p>Please find attached Purchase Order #{po.PONumber} dated {po.OrderDate:dd MMMM yyyy}.</p>");

        // Order Summary
        html.AppendLine("<div class='summary-box'>");
        html.AppendLine("<h3>ORDER SUMMARY</h3>");
        html.AppendLine("<table>");
        html.AppendLine($"<tr><td>PO Number:</td><td><strong>{po.PONumber}</strong></td></tr>");
        html.AppendLine($"<tr><td>Date:</td><td>{po.OrderDate:dd/MM/yyyy}</td></tr>");
        if (po.ExpectedDate.HasValue)
        {
            html.AppendLine($"<tr><td>Expected Delivery:</td><td>{po.ExpectedDate:dd/MM/yyyy}</td></tr>");
        }
        html.AppendLine($"<tr><td>Total Items:</td><td>{po.PurchaseOrderItems.Count}</td></tr>");
        html.AppendLine($"<tr><td>Order Total:</td><td class='amount'><strong>KES {po.TotalAmount:N2}</strong></td></tr>");
        html.AppendLine("</table></div>");

        // Custom message
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            html.AppendLine($"<p><strong>Note:</strong> {customMessage}</p>");
        }

        html.AppendLine("<p>Please confirm receipt of this order and expected delivery date.</p>");

        html.AppendLine("<p>Best regards,<br/>");
        html.AppendLine($"{po.CreatedByUser?.FullName ?? "Procurement Team"}</p>");
        html.AppendLine("</div>");

        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>This is an automated message from our POS System.</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div></body></html>");
        return html.ToString();
    }

    private string GeneratePOEmailPlainText(PurchaseOrder po, string contactName, string? customMessage)
    {
        var text = new StringBuilder();
        text.AppendLine($"PURCHASE ORDER #{po.PONumber}");
        text.AppendLine(new string('=', 50));
        text.AppendLine();
        text.AppendLine($"Dear {contactName},");
        text.AppendLine();
        text.AppendLine($"Please find attached Purchase Order #{po.PONumber} dated {po.OrderDate:dd MMMM yyyy}.");
        text.AppendLine();
        text.AppendLine("ORDER SUMMARY");
        text.AppendLine(new string('-', 30));
        text.AppendLine($"PO Number: {po.PONumber}");
        text.AppendLine($"Date: {po.OrderDate:dd/MM/yyyy}");
        if (po.ExpectedDate.HasValue)
        {
            text.AppendLine($"Expected Delivery: {po.ExpectedDate:dd/MM/yyyy}");
        }
        text.AppendLine($"Total Items: {po.PurchaseOrderItems.Count}");
        text.AppendLine($"Order Total: KES {po.TotalAmount:N2}");
        text.AppendLine();

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            text.AppendLine($"Note: {customMessage}");
            text.AppendLine();
        }

        text.AppendLine("Please confirm receipt of this order and expected delivery date.");
        text.AppendLine();
        text.AppendLine($"Best regards,");
        text.AppendLine(po.CreatedByUser?.FullName ?? "Procurement Team");

        return text.ToString();
    }

    private string GenerateAmendmentEmailBody(PurchaseOrder po, string changeDescription)
    {
        var html = new StringBuilder();
        html.AppendLine("<p>Dear Supplier,</p>");
        html.AppendLine($"<p>Purchase Order #{po.PONumber} has been <strong>amended</strong>.</p>");
        html.AppendLine($"<p><strong>Changes made:</strong> {changeDescription}</p>");
        html.AppendLine("<p>Please review the updated PO attached and confirm.</p>");
        return html.ToString();
    }

    private string GenerateCancellationEmailBody(PurchaseOrder po, string cancellationReason)
    {
        var html = new StringBuilder();
        html.AppendLine("<p>Dear Supplier,</p>");
        html.AppendLine($"<p>Purchase Order #{po.PONumber} has been <strong>cancelled</strong>.</p>");
        html.AppendLine($"<p><strong>Reason:</strong> {cancellationReason}</p>");
        html.AppendLine("<p>Please disregard any previous communications regarding this order.</p>");
        return html.ToString();
    }

    private string GenerateDeliveryReminderBody(PurchaseOrder po)
    {
        var html = new StringBuilder();
        html.AppendLine("<p>Dear Supplier,</p>");
        html.AppendLine($"<p>This is a reminder that delivery for Purchase Order #{po.PONumber} is due.</p>");
        html.AppendLine($"<p><strong>Expected Delivery Date:</strong> {po.ExpectedDate:dd MMMM yyyy}</p>");
        html.AppendLine("<p>Please provide an update on the delivery status.</p>");
        return html.ToString();
    }

    private string GeneratePOHtml(PurchaseOrder po)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; border-bottom: 2px solid #000; padding-bottom: 20px; }");
        html.AppendLine(".header h1 { margin: 0; }");
        html.AppendLine(".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin: 20px 0; }");
        html.AppendLine(".section { margin: 20px 0; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background: #f0f0f0; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine(".total-row { font-weight: bold; background: #f8f8f8; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>PURCHASE ORDER</h1>");
        html.AppendLine($"<h2>{po.PONumber}</h2>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='info-grid'>");
        html.AppendLine("<div>");
        html.AppendLine("<h3>Order To:</h3>");
        html.AppendLine($"<p><strong>{po.Supplier.Name}</strong><br/>");
        if (!string.IsNullOrEmpty(po.Supplier.Address))
        {
            html.AppendLine($"{po.Supplier.Address}<br/>");
        }
        if (!string.IsNullOrEmpty(po.Supplier.City))
        {
            html.AppendLine($"{po.Supplier.City}, {po.Supplier.Country}<br/>");
        }
        html.AppendLine($"Phone: {po.Supplier.Phone}<br/>");
        html.AppendLine($"Email: {po.Supplier.Email}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div>");
        html.AppendLine("<h3>Order Details:</h3>");
        html.AppendLine($"<p>PO Number: <strong>{po.PONumber}</strong><br/>");
        html.AppendLine($"Order Date: {po.OrderDate:dd/MM/yyyy}<br/>");
        html.AppendLine($"Expected Date: {po.ExpectedDate:dd/MM/yyyy}<br/>");
        html.AppendLine($"Status: {po.Status}</p>");
        html.AppendLine("</div></div>");

        html.AppendLine("<div class='section'>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>#</th><th>Item</th><th>SKU</th><th class='amount'>Qty</th><th class='amount'>Unit Price</th><th class='amount'>Total</th></tr>");

        var lineNo = 1;
        foreach (var item in po.PurchaseOrderItems)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{lineNo++}</td>");
            html.AppendLine($"<td>{item.Product?.Name ?? "N/A"}</td>");
            html.AppendLine($"<td>{item.Product?.SKU ?? "N/A"}</td>");
            html.AppendLine($"<td class='amount'>{item.Quantity:N0}</td>");
            html.AppendLine($"<td class='amount'>{item.UnitPrice:N2}</td>");
            html.AppendLine($"<td class='amount'>{item.TotalPrice:N2}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine($"<tr class='total-row'><td colspan='5'>Subtotal</td><td class='amount'>KES {po.SubTotal:N2}</td></tr>");
        html.AppendLine($"<tr class='total-row'><td colspan='5'>Tax</td><td class='amount'>KES {po.TaxAmount:N2}</td></tr>");
        html.AppendLine($"<tr class='total-row'><td colspan='5'><strong>TOTAL</strong></td><td class='amount'><strong>KES {po.TotalAmount:N2}</strong></td></tr>");
        html.AppendLine("</table></div>");

        if (!string.IsNullOrEmpty(po.Notes))
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<h3>Notes:</h3><p>{po.Notes}</p>");
            html.AppendLine("</div>");
        }

        html.AppendLine($"<p style='margin-top: 30px;'>Generated: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    #endregion
}
