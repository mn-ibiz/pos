using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Email Configuration DTOs

/// <summary>
/// DTO for email configuration display and editing.
/// </summary>
public class EmailConfigurationDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public bool HasPassword { get; set; }
    public bool UseSsl { get; set; } = true;
    public bool UseStartTls { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyToAddress { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 15;
    public DateTime? LastConnectionTest { get; set; }
    public bool? ConnectionTestSuccessful { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating or updating email configuration.
/// </summary>
public class SaveEmailConfigurationDto
{
    public int? StoreId { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; } // Plain text, will be encrypted
    public bool UseSsl { get; set; } = true;
    public bool UseStartTls { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyToAddress { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 15;
}

/// <summary>
/// Result of connection test.
/// </summary>
public class ConnectionTestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ServerBanner { get; set; }
}

#endregion

#region Email Recipient DTOs

/// <summary>
/// DTO for email recipient display.
/// </summary>
public class EmailRecipientDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public bool ReceiveDailySales { get; set; }
    public bool ReceiveWeeklyReport { get; set; }
    public bool ReceiveLowStockAlerts { get; set; }
    public bool ReceiveExpiryAlerts { get; set; }
    public bool ReceiveMonthlyReport { get; set; }
    public bool IsCc { get; set; }
    public bool IsBcc { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Comma-separated list of report types this recipient receives.
    /// </summary>
    public string ReportTypesSummary { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating or updating an email recipient.
/// </summary>
public class SaveEmailRecipientDto
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? StoreId { get; set; }
    public int? UserId { get; set; }
    public bool ReceiveDailySales { get; set; } = true;
    public bool ReceiveWeeklyReport { get; set; } = true;
    public bool ReceiveLowStockAlerts { get; set; } = true;
    public bool ReceiveExpiryAlerts { get; set; } = true;
    public bool ReceiveMonthlyReport { get; set; }
    public bool IsCc { get; set; }
    public bool IsBcc { get; set; }
}

#endregion

#region Email Schedule DTOs

/// <summary>
/// DTO for email schedule display.
/// </summary>
public class EmailScheduleDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public EmailReportType ReportType { get; set; }
    public string ReportTypeName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public TimeOnly SendTime { get; set; }
    public string SendTimeDisplay { get; set; } = string.Empty;
    public int? DayOfWeek { get; set; }
    public string? DayOfWeekName { get; set; }
    public int? DayOfMonth { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public EmailScheduleFrequency AlertFrequency { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextScheduledAt { get; set; }
    public string? CustomSubject { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating or updating an email schedule.
/// </summary>
public class SaveEmailScheduleDto
{
    public int? StoreId { get; set; }
    public EmailReportType ReportType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public TimeOnly SendTime { get; set; } = new TimeOnly(20, 0);
    public int? DayOfWeek { get; set; } = 1;
    public int? DayOfMonth { get; set; } = 1;
    public string TimeZone { get; set; } = "Africa/Nairobi";
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;
    public string? CustomSubject { get; set; }
}

#endregion

#region Email Log DTOs

/// <summary>
/// DTO for email log display.
/// </summary>
public class EmailLogDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public EmailReportType ReportType { get; set; }
    public string ReportTypeName { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailSendStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int? GenerationTimeMs { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Query parameters for email logs.
/// </summary>
public class EmailLogQueryDto
{
    public int? StoreId { get; set; }
    public EmailReportType? ReportType { get; set; }
    public EmailSendStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated result for email logs.
/// </summary>
public class EmailLogResultDto
{
    public List<EmailLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

#endregion

#region Email Message DTOs

/// <summary>
/// Represents an email message to be sent.
/// </summary>
public class EmailMessageDto
{
    public List<string> ToAddresses { get; set; } = new();
    public List<string> CcAddresses { get; set; } = new();
    public List<string> BccAddresses { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public EmailAttachmentDto? Attachment { get; set; }
    public EmailReportType ReportType { get; set; }
    public int? StoreId { get; set; }
}

/// <summary>
/// Email attachment data.
/// </summary>
public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Result of sending an email.
/// </summary>
public class EmailSendResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? EmailLogId { get; set; }
    public DateTime? SentAt { get; set; }
}

#endregion

#region Report Data DTOs

/// <summary>
/// Data for daily sales summary email.
/// </summary>
public class DailySalesEmailDataDto
{
    public DateTime Date { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal PreviousDaySales { get; set; }
    public decimal SalesChangePercent { get; set; }
    public bool SalesIncreased => SalesChangePercent >= 0;
    public List<EmailTopProductDto> TopProducts { get; set; } = new();
    public List<PaymentBreakdownDto> PaymentBreakdown { get; set; } = new();
    public string CurrencySymbol { get; set; } = "KSh";
}

/// <summary>
/// Data for weekly performance report email.
/// </summary>
public class WeeklyReportEmailDataDto
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
    public decimal PreviousWeekSales { get; set; }
    public decimal SalesChangePercent { get; set; }
    public List<DailySalesDto> DailySales { get; set; } = new();
    public List<EmailCategoryPerformanceDto> CategoryPerformance { get; set; } = new();
    public string BestDay { get; set; } = string.Empty;
    public decimal BestDaySales { get; set; }
    public string WorstDay { get; set; } = string.Empty;
    public decimal WorstDaySales { get; set; }
    public string CurrencySymbol { get; set; } = "KSh";
}

/// <summary>
/// Top selling product summary for email reports.
/// </summary>
public class EmailTopProductDto
{
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Payment method breakdown.
/// </summary>
public class PaymentBreakdownDto
{
    public string MethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Daily sales for weekly report.
/// </summary>
public class DailySalesDto
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
}

/// <summary>
/// Category performance summary for email reports.
/// </summary>
public class EmailCategoryPerformanceDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int ItemsSold { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Low stock item for alert email.
/// </summary>
public class LowStockItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal StockDeficit => ReorderLevel - CurrentStock;
    public string? SupplierName { get; set; }
}

/// <summary>
/// Data for low stock alert email.
/// </summary>
public class LowStockAlertEmailDataDto
{
    public DateTime GeneratedAt { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int TotalLowStockItems { get; set; }
    public int CriticalItems { get; set; }
    public List<LowStockItemDto> Items { get; set; } = new();
}

/// <summary>
/// Expiring item for alert email.
/// </summary>
public class ExpiringItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
}

/// <summary>
/// Data for expiry alert email.
/// </summary>
public class ExpiryAlertEmailDataDto
{
    public DateTime GeneratedAt { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int AlertThresholdDays { get; set; }
    public int TotalExpiringItems { get; set; }
    public int UrgentItems { get; set; }
    public List<ExpiringItemDto> Items { get; set; } = new();
}

#endregion

#region Alert Config DTOs

/// <summary>
/// DTO for low stock alert configuration.
/// </summary>
public class LowStockAlertConfigDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsEnabled { get; set; }
    public EmailScheduleFrequency AlertFrequency { get; set; }
    public int ThresholdPercent { get; set; }
    public int MinimumItemsForAlert { get; set; }
    public int MaxItemsPerEmail { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for saving low stock alert configuration.
/// </summary>
public class SaveLowStockAlertConfigDto
{
    public int? StoreId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;
    public int ThresholdPercent { get; set; } = 100;
    public int MinimumItemsForAlert { get; set; } = 1;
    public int MaxItemsPerEmail { get; set; } = 50;
}

/// <summary>
/// DTO for expiry alert configuration.
/// </summary>
public class ExpiryAlertConfigDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsEnabled { get; set; }
    public EmailScheduleFrequency AlertFrequency { get; set; }
    public int AlertThresholdDays { get; set; }
    public int UrgentThresholdDays { get; set; }
    public int MaxItemsPerEmail { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for saving expiry alert configuration.
/// </summary>
public class SaveExpiryAlertConfigDto
{
    public int? StoreId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public EmailScheduleFrequency AlertFrequency { get; set; } = EmailScheduleFrequency.Daily;
    public int AlertThresholdDays { get; set; } = 7;
    public int UrgentThresholdDays { get; set; } = 3;
    public int MaxItemsPerEmail { get; set; } = 50;
}

#endregion

#region Dashboard DTOs

/// <summary>
/// Summary of email system status for dashboard.
/// </summary>
public class EmailDashboardDto
{
    public bool IsConfigured { get; set; }
    public bool ConnectionHealthy { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public int TotalRecipients { get; set; }
    public int ActiveSchedules { get; set; }
    public int EmailsSentToday { get; set; }
    public int EmailsFailedToday { get; set; }
    public int PendingEmails { get; set; }
    public EmailLogDto? LastSentEmail { get; set; }
    public EmailLogDto? LastFailedEmail { get; set; }
}

#endregion
