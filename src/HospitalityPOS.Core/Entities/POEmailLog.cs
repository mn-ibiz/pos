namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of an email delivery.
/// </summary>
public enum EmailDeliveryStatus
{
    /// <summary>
    /// Email is queued for sending.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Email has been sent to the mail server.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Email has been delivered to recipient's mailbox.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Email sending failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Email bounced back (invalid address, mailbox full, etc.).
    /// </summary>
    Bounced = 4,

    /// <summary>
    /// Email was opened by recipient.
    /// </summary>
    Opened = 5
}

/// <summary>
/// Type of email sent for purchase orders.
/// </summary>
public enum POEmailType
{
    /// <summary>
    /// Initial PO submission email.
    /// </summary>
    PurchaseOrder = 0,

    /// <summary>
    /// PO update/amendment email.
    /// </summary>
    Amendment = 1,

    /// <summary>
    /// PO cancellation notification.
    /// </summary>
    Cancellation = 2,

    /// <summary>
    /// Delivery reminder email.
    /// </summary>
    DeliveryReminder = 3,

    /// <summary>
    /// Payment confirmation email.
    /// </summary>
    PaymentConfirmation = 4,

    /// <summary>
    /// General follow-up email.
    /// </summary>
    FollowUp = 5
}

/// <summary>
/// Tracks emails sent for Purchase Orders.
/// Provides audit trail and delivery status tracking.
/// </summary>
public class POEmailLog : BaseEntity
{
    /// <summary>
    /// Purchase Order this email was sent for.
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Type of email sent.
    /// </summary>
    public POEmailType EmailType { get; set; } = POEmailType.PurchaseOrder;

    /// <summary>
    /// Primary recipients (To addresses), comma-separated.
    /// </summary>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// CC recipients, comma-separated.
    /// </summary>
    public string? CcRecipients { get; set; }

    /// <summary>
    /// BCC recipients, comma-separated.
    /// </summary>
    public string? BccRecipients { get; set; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content (HTML).
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Whether the email includes PDF attachment.
    /// </summary>
    public bool HasPdfAttachment { get; set; } = true;

    /// <summary>
    /// List of attachment file names.
    /// </summary>
    public string? AttachmentNames { get; set; }

    /// <summary>
    /// When the email was queued/initiated.
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// When the email was actually sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// User who initiated the email send.
    /// </summary>
    public int SentByUserId { get; set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.Queued;

    /// <summary>
    /// Error message if sending failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Message ID returned by email provider.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Custom message added by user when sending.
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    /// When the email was opened (if tracked).
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// IP address from which email was opened.
    /// </summary>
    public string? OpenedFromIp { get; set; }

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual User SentByUser { get; set; } = null!;
}
