using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Types of automated reports that can be sent via email.
/// </summary>
public enum EmailReportType
{
    /// <summary>Daily sales summary report.</summary>
    DailySales = 1,
    /// <summary>Weekly performance report.</summary>
    WeeklyReport = 2,
    /// <summary>Low stock alert notification.</summary>
    LowStockAlert = 3,
    /// <summary>Product expiry alert notification.</summary>
    ExpiryAlert = 4,
    /// <summary>Monthly summary report.</summary>
    MonthlySummary = 5,
    /// <summary>Custom report.</summary>
    Custom = 99
}

/// <summary>
/// Status of an email send attempt.
/// </summary>
public enum EmailSendStatus
{
    /// <summary>Email is pending to be sent.</summary>
    Pending = 0,
    /// <summary>Email was sent successfully.</summary>
    Sent = 1,
    /// <summary>Email sending failed.</summary>
    Failed = 2,
    /// <summary>Email is scheduled for retry.</summary>
    Retry = 3,
    /// <summary>Email was cancelled.</summary>
    Cancelled = 4
}

/// <summary>
/// Frequency for scheduled email reports.
/// </summary>
public enum EmailScheduleFrequency
{
    /// <summary>Send immediately (for alerts).</summary>
    Immediate = 0,
    /// <summary>Daily digest.</summary>
    Daily = 1,
    /// <summary>Weekly report.</summary>
    Weekly = 2,
    /// <summary>Monthly report.</summary>
    Monthly = 3
}

#endregion

#region Entities

/// <summary>
/// SMTP email server configuration for sending automated emails.
/// </summary>
public class EmailConfiguration : BaseEntity
{
    /// <summary>Store ID if store-specific, null for global configuration.</summary>
    public int? StoreId { get; set; }

    /// <summary>SMTP server hostname.</summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>SMTP server port (default 587 for TLS).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>SMTP authentication username.</summary>
    public string? SmtpUsername { get; set; }

    /// <summary>Encrypted SMTP password.</summary>
    public string? SmtpPasswordEncrypted { get; set; }

    /// <summary>Whether to use SSL/TLS encryption.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Whether to use STARTTLS.</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Sender email address.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Sender display name.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>Reply-to email address (optional).</summary>
    public string? ReplyToAddress { get; set; }

    /// <summary>Connection timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum number of retry attempts for failed emails.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Delay between retry attempts in minutes.</summary>
    public int RetryDelayMinutes { get; set; } = 15;

    /// <summary>Last successful connection test date.</summary>
    public DateTime? LastConnectionTest { get; set; }

    /// <summary>Whether connection test was successful.</summary>
    public bool? ConnectionTestSuccessful { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Email recipient configuration for automated reports.
/// </summary>
public class EmailRecipient : BaseEntity
{
    /// <summary>Recipient email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Recipient display name.</summary>
    public string? Name { get; set; }

    /// <summary>Store ID if store-specific, null for all stores.</summary>
    public int? StoreId { get; set; }

    /// <summary>User ID if linked to a system user.</summary>
    public int? UserId { get; set; }

    /// <summary>Whether recipient receives daily sales reports.</summary>
    public bool ReceiveDailySales { get; set; } = true;

    /// <summary>Whether recipient receives weekly reports.</summary>
    public bool ReceiveWeeklyReport { get; set; } = true;

    /// <summary>Whether recipient receives low stock alerts.</summary>
    public bool ReceiveLowStockAlerts { get; set; } = true;

    /// <summary>Whether recipient receives expiry alerts.</summary>
    public bool ReceiveExpiryAlerts { get; set; } = true;

    /// <summary>Whether recipient receives monthly reports.</summary>
    public bool ReceiveMonthlyReport { get; set; } = false;

    /// <summary>Whether to include this recipient in CC instead of TO.</summary>
    public bool IsCc { get; set; }

    /// <summary>Whether to include this recipient in BCC.</summary>
    public bool IsBcc { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual User? User { get; set; }
}

/// <summary>
/// Schedule configuration for automated email reports.
/// </summary>
public class EmailSchedule : BaseEntity
{
    /// <summary>Store ID if store-specific, null for global schedule.</summary>
    public int? StoreId { get; set; }

    /// <summary>Type of report to send.</summary>
    public EmailReportType ReportType { get; set; }

    /// <summary>Whether this schedule is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Time of day to send the report (UTC).</summary>
    public TimeOnly SendTime { get; set; } = new TimeOnly(20, 0); // 8 PM default

    /// <summary>Day of week for weekly reports (0=Sunday, 1=Monday, etc.).</summary>
    public int? DayOfWeek { get; set; } = 1; // Monday default

    /// <summary>Day of month for monthly reports (1-28).</summary>
    public int? DayOfMonth { get; set; } = 1; // 1st of month default

    /// <summary>Time zone for schedule (IANA format, e.g., "Africa/Nairobi").</summary>
    public string TimeZone { get; set; } = "Africa/Nairobi";

    /// <summary>Frequency for alert digests.</summary>
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;

    /// <summary>Last time this schedule was executed.</summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>Next scheduled execution time.</summary>
    public DateTime? NextScheduledAt { get; set; }

    /// <summary>Custom subject line template (supports placeholders).</summary>
    public string? CustomSubject { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Log of all email send attempts.
/// </summary>
public class EmailLog : BaseEntity
{
    /// <summary>Store ID if store-specific.</summary>
    public int? StoreId { get; set; }

    /// <summary>Type of report sent.</summary>
    public EmailReportType ReportType { get; set; }

    /// <summary>Comma-separated list of recipients.</summary>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>Email subject.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Status of the send attempt.</summary>
    public EmailSendStatus Status { get; set; } = EmailSendStatus.Pending;

    /// <summary>Error message if send failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Number of retry attempts made.</summary>
    public int RetryCount { get; set; }

    /// <summary>Scheduled time for sending.</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Actual time when sent (or attempted).</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Time spent generating the report content (ms).</summary>
    public int? GenerationTimeMs { get; set; }

    /// <summary>Size of the email body in bytes.</summary>
    public int? BodySizeBytes { get; set; }

    /// <summary>Whether email included attachment.</summary>
    public bool HasAttachment { get; set; }

    /// <summary>Attachment filename if present.</summary>
    public string? AttachmentName { get; set; }

    /// <summary>Reference to the schedule that triggered this email.</summary>
    public int? EmailScheduleId { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
    public virtual EmailSchedule? EmailSchedule { get; set; }
}

/// <summary>
/// Email template for different report types.
/// </summary>
public class EmailTemplate : BaseEntity
{
    /// <summary>Template name/identifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Type of report this template is for.</summary>
    public EmailReportType ReportType { get; set; }

    /// <summary>Subject line template (supports placeholders like {{Date}}, {{StoreName}}).</summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>HTML body template.</summary>
    public string HtmlBodyTemplate { get; set; } = string.Empty;

    /// <summary>Plain text body template (fallback).</summary>
    public string? PlainTextTemplate { get; set; }

    /// <summary>Whether this is the default template for the report type.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Store ID if store-specific, null for global template.</summary>
    public int? StoreId { get; set; }

    // Navigation
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Configuration for low stock alert emails.
/// </summary>
public class LowStockAlertConfig : BaseEntity
{
    /// <summary>Store ID if store-specific, null for global config.</summary>
    public int? StoreId { get; set; }

    /// <summary>Whether low stock alerts are enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Send immediately or as daily digest.</summary>
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;

    /// <summary>Percentage below reorder level to trigger alert (0-100).</summary>
    public int ThresholdPercent { get; set; } = 100; // At or below reorder level

    /// <summary>Minimum number of items to include in alert.</summary>
    public int MinimumItemsForAlert { get; set; } = 1;

    /// <summary>Maximum items to include in single email.</summary>
    public int MaxItemsPerEmail { get; set; } = 50;

    // Navigation
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Configuration for expiry alert emails.
/// </summary>
public class ExpiryAlertConfig : BaseEntity
{
    /// <summary>Store ID if store-specific, null for global config.</summary>
    public int? StoreId { get; set; }

    /// <summary>Whether expiry alerts are enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Send immediately or as daily digest.</summary>
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;

    /// <summary>Days before expiry to start alerting (default 7 days).</summary>
    public int AlertThresholdDays { get; set; } = 7;

    /// <summary>Additional threshold for urgent alerts (default 3 days).</summary>
    public int UrgentThresholdDays { get; set; } = 3;

    /// <summary>Maximum items to include in single email.</summary>
    public int MaxItemsPerEmail { get; set; } = 50;

    // Navigation
    public virtual Store? Store { get; set; }
}

#endregion
