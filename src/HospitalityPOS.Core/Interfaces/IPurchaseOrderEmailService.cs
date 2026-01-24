using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for Purchase Order email operations.
/// Handles sending, tracking, and previewing PO emails to suppliers.
/// </summary>
public interface IPurchaseOrderEmailService
{
    /// <summary>
    /// Sends a Purchase Order email to the supplier.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID to send.</param>
    /// <param name="sentByUserId">User initiating the send.</param>
    /// <param name="additionalRecipients">Additional email addresses to include.</param>
    /// <param name="customMessage">Custom message to include in the email body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Email log entry with send status.</returns>
    Task<POEmailLog> SendPurchaseOrderAsync(
        int purchaseOrderId,
        int sentByUserId,
        string[]? additionalRecipients = null,
        string? customMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends a Purchase Order email.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID to resend.</param>
    /// <param name="sentByUserId">User initiating the resend.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Email log entry with send status.</returns>
    Task<POEmailLog> ResendPurchaseOrderAsync(
        int purchaseOrderId,
        int sentByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PO amendment notification email.
    /// </summary>
    Task<POEmailLog> SendAmendmentNotificationAsync(
        int purchaseOrderId,
        int sentByUserId,
        string changeDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PO cancellation notification email.
    /// </summary>
    Task<POEmailLog> SendCancellationNotificationAsync(
        int purchaseOrderId,
        int sentByUserId,
        string cancellationReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a delivery reminder email for an overdue PO.
    /// </summary>
    Task<POEmailLog> SendDeliveryReminderAsync(
        int purchaseOrderId,
        int sentByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the email history for a Purchase Order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of email log entries.</returns>
    Task<IReadOnlyList<POEmailLog>> GetEmailHistoryAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the email log by ID.
    /// </summary>
    Task<POEmailLog?> GetEmailLogByIdAsync(int logId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews the PO email without sending.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="customMessage">Optional custom message to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preview of the email content.</returns>
    Task<POEmailPreview> PreviewPOEmailAsync(
        int purchaseOrderId,
        string? customMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the PDF content for a Purchase Order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF byte array.</returns>
    Task<byte[]> GeneratePOPdfAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries sending a failed email.
    /// </summary>
    /// <param name="emailLogId">The email log ID to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated email log entry.</returns>
    Task<POEmailLog> RetryFailedEmailAsync(
        int emailLogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier contacts who should receive PO emails.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of email addresses.</returns>
    Task<IReadOnlyList<string>> GetPOEmailRecipientsAsync(
        int supplierId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a supplier has valid email configuration for receiving POs.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any issues.</returns>
    Task<EmailValidationResult> ValidateSupplierEmailConfigAsync(
        int supplierId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Preview model for PO email content.
/// </summary>
public class POEmailPreview
{
    /// <summary>
    /// Email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML body content.
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body content.
    /// </summary>
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Primary recipients.
    /// </summary>
    public List<string> Recipients { get; set; } = [];

    /// <summary>
    /// CC recipients.
    /// </summary>
    public List<string> CcRecipients { get; set; } = [];

    /// <summary>
    /// Attachment file names.
    /// </summary>
    public List<string> Attachments { get; set; } = [];

    /// <summary>
    /// Purchase Order details for reference.
    /// </summary>
    public string PONumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Result of email configuration validation.
/// </summary>
public class EmailValidationResult
{
    /// <summary>
    /// Whether the configuration is valid for sending.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation issues found.
    /// </summary>
    public List<string> Issues { get; set; } = [];

    /// <summary>
    /// List of warnings (email can still be sent).
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Primary recipient if found.
    /// </summary>
    public string? PrimaryEmail { get; set; }

    /// <summary>
    /// Number of contacts that will receive the email.
    /// </summary>
    public int RecipientCount { get; set; }
}
