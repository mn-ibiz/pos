using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Types of email digests.
/// </summary>
public enum DigestType
{
    /// <summary>Daily pending POs digest.</summary>
    DailyPendingPOs = 1,
    /// <summary>Weekly summary.</summary>
    WeeklySummary = 2,
    /// <summary>Low stock alert.</summary>
    LowStockAlert = 3,
    /// <summary>Overdue POs reminder.</summary>
    OverduePOs = 4
}

/// <summary>
/// Result of sending a digest email.
/// </summary>
public class DigestEmailResult
{
    /// <summary>
    /// Whether the email was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of recipients.
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Email addresses that were sent to.
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the digest was sent.
    /// </summary>
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Preview of a digest email.
/// </summary>
public class DigestPreview
{
    /// <summary>
    /// Digest type.
    /// </summary>
    public DigestType Type { get; set; }

    /// <summary>
    /// Email subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML content.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Plain text content.
    /// </summary>
    public string PlainTextContent { get; set; } = string.Empty;

    /// <summary>
    /// Recipients.
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Whether there is content to send.
    /// </summary>
    public bool HasContent { get; set; }

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// Service for sending email digests and summaries.
/// </summary>
public interface IEmailDigestService
{
    /// <summary>
    /// Sends the daily pending POs digest.
    /// </summary>
    Task<DigestEmailResult> SendDailyPendingPOsDigestAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the weekly PO summary.
    /// </summary>
    Task<DigestEmailResult> SendWeeklySummaryAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a low stock alert.
    /// </summary>
    Task<DigestEmailResult> SendLowStockAlertAsync(IEnumerable<Product> lowStockProducts, int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends overdue POs reminder.
    /// </summary>
    Task<DigestEmailResult> SendOverduePOsReminderAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews a digest without sending.
    /// </summary>
    Task<DigestPreview> PreviewDigestAsync(DigestType type, int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the digest settings.
    /// </summary>
    Task<PurchaseOrderSettings?> GetDigestSettingsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves digest settings.
    /// </summary>
    Task<PurchaseOrderSettings> SaveDigestSettingsAsync(PurchaseOrderSettings settings, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if daily digest should be sent now.
    /// </summary>
    Task<bool> ShouldSendDailyDigestAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if weekly summary should be sent now.
    /// </summary>
    Task<bool> ShouldSendWeeklySummaryAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of digest recipients.
    /// </summary>
    Task<List<string>> GetDigestRecipientsAsync(int? storeId = null, CancellationToken cancellationToken = default);
}
