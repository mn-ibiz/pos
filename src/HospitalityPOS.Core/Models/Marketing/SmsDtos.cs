// src/HospitalityPOS.Core/Models/Marketing/SmsDtos.cs
// DTOs for SMS marketing and customer segmentation
// Story 47-1: SMS Marketing to Customers

namespace HospitalityPOS.Core.Models.Marketing;

#region Enums

/// <summary>
/// SMS gateway providers.
/// </summary>
public enum SmsGateway
{
    /// <summary>Africa's Talking (Kenya-focused).</summary>
    AfricasTalking,
    /// <summary>Twilio (global).</summary>
    Twilio,
    /// <summary>Custom/Local gateway.</summary>
    Custom
}

/// <summary>
/// Status of an SMS message.
/// </summary>
public enum SmsStatus
{
    /// <summary>Queued for sending.</summary>
    Pending,
    /// <summary>Sent to gateway.</summary>
    Sent,
    /// <summary>Delivered to recipient.</summary>
    Delivered,
    /// <summary>Failed to deliver.</summary>
    Failed,
    /// <summary>Rejected by gateway.</summary>
    Rejected
}

/// <summary>
/// Status of an SMS campaign.
/// </summary>
public enum CampaignStatus
{
    /// <summary>Campaign being drafted.</summary>
    Draft,
    /// <summary>Scheduled for future delivery.</summary>
    Scheduled,
    /// <summary>Currently sending messages.</summary>
    Sending,
    /// <summary>Paused mid-sending.</summary>
    Paused,
    /// <summary>All messages sent.</summary>
    Completed,
    /// <summary>Campaign cancelled.</summary>
    Cancelled
}

/// <summary>
/// Template category.
/// </summary>
public enum SmsTemplateCategory
{
    /// <summary>Promotional messages.</summary>
    Promotion,
    /// <summary>Loyalty program messages.</summary>
    Loyalty,
    /// <summary>Transactional notifications.</summary>
    Transactional,
    /// <summary>Birthday/special occasion.</summary>
    Special
}

/// <summary>
/// Logic for combining segment criteria.
/// </summary>
public enum SegmentLogic
{
    /// <summary>All criteria must match (AND).</summary>
    And,
    /// <summary>Any criterion can match (OR).</summary>
    Or
}

/// <summary>
/// Type of segment criterion.
/// </summary>
public enum SegmentCriterionType
{
    /// <summary>Purchased anything within X days.</summary>
    PurchasedWithinDays,
    /// <summary>Purchased from category within X days.</summary>
    PurchasedCategory,
    /// <summary>Purchased specific product within X days.</summary>
    PurchasedProduct,
    /// <summary>Spent minimum amount within X days.</summary>
    SpentMinimum,
    /// <summary>By loyalty tier.</summary>
    LoyaltyTier,
    /// <summary>Last visit X days ago.</summary>
    LastVisitDays,
    /// <summary>No visit for X days (lapsed).</summary>
    LapsedDays,
    /// <summary>Has birthday this month.</summary>
    BirthdayThisMonth,
    /// <summary>Points about to expire.</summary>
    PointsExpiring
}

/// <summary>
/// Type of segment (static list vs dynamic query).
/// </summary>
public enum SegmentType
{
    /// <summary>Static list of customer IDs.</summary>
    Static,
    /// <summary>Dynamic query evaluated at runtime.</summary>
    Dynamic
}

/// <summary>
/// Transactional SMS trigger type.
/// </summary>
public enum TransactionalSmsType
{
    /// <summary>Points earned notification.</summary>
    PointsEarned,
    /// <summary>Tier upgrade notification.</summary>
    TierUpgrade,
    /// <summary>Points expiry reminder.</summary>
    PointsExpiring,
    /// <summary>Receipt/purchase confirmation.</summary>
    Receipt,
    /// <summary>Birthday greeting.</summary>
    Birthday,
    /// <summary>Welcome message.</summary>
    Welcome
}

#endregion

#region SMS Configuration

/// <summary>
/// SMS gateway configuration.
/// </summary>
public class SmsConfiguration
{
    public int Id { get; set; }
    public SmsGateway Gateway { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string? ApiSecret { get; set; }
    public string? AccountId { get; set; }
    public string? SenderId { get; set; } // Alphanumeric sender ID
    public string? BaseUrl { get; set; } // For custom gateway
    public decimal CostPerSms { get; set; } = 1m; // KSh per SMS
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool LastTestSuccessful { get; set; }
}

/// <summary>
/// Request to configure SMS gateway.
/// </summary>
public class SmsConfigurationRequest
{
    public int? Id { get; set; }
    public SmsGateway Gateway { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string? ApiSecret { get; set; }
    public string? AccountId { get; set; }
    public string? SenderId { get; set; }
    public decimal CostPerSms { get; set; } = 1m;
}

#endregion

#region SMS Templates

/// <summary>
/// Reusable SMS template.
/// </summary>
public class SmsTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SmsTemplateCategory Category { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public List<string> Placeholders { get; set; } = new(); // Extracted from message
    public int CharacterCount => MessageText.Length;
    public int SmsSegments => (int)Math.Ceiling(MessageText.Length / 160.0);
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int UsageCount { get; set; }

    // Common placeholders: {CustomerName}, {Points}, {TierName}, {StoreName}, {ExpiryDate}
}

/// <summary>
/// Request to create or update a template.
/// </summary>
public class SmsTemplateRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SmsTemplateCategory Category { get; set; }
    public string MessageText { get; set; } = string.Empty;
}

/// <summary>
/// Preview of a template with sample data.
/// </summary>
public class SmsTemplatePreview
{
    public string OriginalMessage { get; set; } = string.Empty;
    public string RenderedMessage { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public int SmsSegments { get; set; }
    public decimal EstimatedCost { get; set; }
}

#endregion

#region Customer Segmentation

/// <summary>
/// Saved customer segment for targeting.
/// </summary>
public class CustomerSegment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SegmentType SegmentType { get; set; } = SegmentType.Dynamic;
    public SegmentFilter FilterCriteria { get; set; } = new();
    public int CachedCount { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create or update a segment.
/// </summary>
public class CustomerSegmentRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SegmentFilter FilterCriteria { get; set; } = new();
    public int CreatedByUserId { get; set; }
}

/// <summary>
/// Filter definition for building segments.
/// </summary>
public class SegmentFilter
{
    public SegmentLogic Logic { get; set; } = SegmentLogic.And;
    public List<SegmentCriterion> Criteria { get; set; } = new();

    // Predefined filters
    public bool IncludeOnlyOptedIn { get; set; } = true;
    public bool IncludeOnlyActive { get; set; } = true;
}

/// <summary>
/// Single filter criterion.
/// </summary>
public class SegmentCriterion
{
    public SegmentCriterionType Type { get; set; }
    public int? DaysBack { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? MinimumSpend { get; set; }
    public string? TierName { get; set; }
}

/// <summary>
/// Result of segment calculation.
/// </summary>
public class SegmentResult
{
    public int MatchingCount { get; set; }
    public int TotalOptedIn { get; set; }
    public decimal PercentOfTotal => TotalOptedIn > 0 ? (MatchingCount * 100m / TotalOptedIn) : 0;
    public List<CustomerSmsInfo> Customers { get; set; } = new();
}

/// <summary>
/// Customer info for SMS targeting.
/// </summary>
public class CustomerSmsInfo
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? TierName { get; set; }
    public decimal? PointsBalance { get; set; }
    public DateOnly? Birthday { get; set; }
    public DateTime? LastVisit { get; set; }
    public decimal TotalSpend { get; set; }
    public bool IsOptedIn { get; set; }
}

#endregion

#region SMS Campaigns

/// <summary>
/// SMS marketing campaign.
/// </summary>
public class SmsCampaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public int CharacterCount => MessageText.Length;

    public string? TargetSegment { get; set; } // Description or segment name
    public int? CustomerSegmentId { get; set; }
    public int TargetCount { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalCost { get; set; }

    public int CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }

    public decimal DeliveryRate => SentCount > 0 ? (DeliveredCount * 100m / SentCount) : 0;
    public decimal FailureRate => SentCount > 0 ? (FailedCount * 100m / SentCount) : 0;
}

/// <summary>
/// Request to create or update a campaign.
/// </summary>
public class SmsCampaignRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public int? CustomerSegmentId { get; set; }
    public SegmentFilter? InlineFilter { get; set; } // If no saved segment
    public DateTime? ScheduledAt { get; set; }
    public int CreatedByUserId { get; set; }
}

/// <summary>
/// Result of campaign operation.
/// </summary>
public class CampaignResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SmsCampaign? Campaign { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static CampaignResult Succeeded(SmsCampaign campaign, string message = "Campaign created successfully")
        => new() { Success = true, Message = message, Campaign = campaign };

    public static CampaignResult Failed(string message)
        => new() { Success = false, Message = message };
}

#endregion

#region SMS Sending

/// <summary>
/// Log entry for sent SMS.
/// </summary>
public class SmsSentLog
{
    public long Id { get; set; }
    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public SmsStatus Status { get; set; } = SmsStatus.Pending;
    public string? GatewayMessageId { get; set; }
    public decimal? Cost { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to send a single SMS.
/// </summary>
public class SendSmsRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int? CampaignId { get; set; }
}

/// <summary>
/// Result of sending an SMS.
/// </summary>
public class SmsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? GatewayMessageId { get; set; }
    public decimal? Cost { get; set; }
    public SmsStatus Status { get; set; }
    public string? ErrorCode { get; set; }

    public static SmsResult Succeeded(string gatewayMessageId, decimal cost)
        => new() { Success = true, Message = "SMS sent", GatewayMessageId = gatewayMessageId, Cost = cost, Status = SmsStatus.Sent };

    public static SmsResult Failed(string message, string? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Status = SmsStatus.Failed };
}

/// <summary>
/// Batch sending progress.
/// </summary>
public class BatchSendProgress
{
    public int CampaignId { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int RemainingCount => TotalRecipients - SentCount - FailedCount;
    public decimal ProgressPercent => TotalRecipients > 0 ? ((SentCount + FailedCount) * 100m / TotalRecipients) : 0;
    public bool IsComplete => RemainingCount == 0;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

#endregion

#region Opt-In/Opt-Out

/// <summary>
/// Customer SMS consent record.
/// </summary>
public class SmsConsent
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsOptedIn { get; set; }
    public DateTime? OptInDate { get; set; }
    public DateTime? OptOutDate { get; set; }
    public string? OptOutReason { get; set; }
}

/// <summary>
/// Opt-out log entry.
/// </summary>
public class SmsOptOutLog
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OptOutKeyword { get; set; } = string.Empty; // STOP, UNSUBSCRIBE, etc.
    public DateTime OptOutDate { get; set; }
    public bool ConfirmationSent { get; set; }
}

#endregion

#region Transactional SMS

/// <summary>
/// Transactional SMS configuration.
/// </summary>
public class TransactionalSmsConfig
{
    public TransactionalSmsType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? TemplateId { get; set; }
    public string? DefaultMessage { get; set; }
    public int? MinIntervalMinutes { get; set; } // Rate limiting
}

/// <summary>
/// Request to send transactional SMS.
/// </summary>
public class TransactionalSmsRequest
{
    public TransactionalSmsType Type { get; set; }
    public int CustomerId { get; set; }
    public Dictionary<string, string> Data { get; set; } = new(); // Placeholder values
}

#endregion

#region Reports

/// <summary>
/// Campaign report.
/// </summary>
public class CampaignReport
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; }
    public DateTime? SentAt { get; set; }

    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }

    public decimal DeliveryRate { get; set; }
    public decimal FailureRate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostPerDelivered => DeliveredCount > 0 ? TotalCost / DeliveredCount : 0;

    public List<SmsSentLog> FailedMessages { get; set; } = new();
}

/// <summary>
/// SMS usage report.
/// </summary>
public class SmsUsageReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly GeneratedDate { get; set; }

    public int TotalCampaigns { get; set; }
    public int TotalMessagesSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public decimal TotalCost { get; set; }

    public decimal OverallDeliveryRate => TotalMessagesSent > 0 ? (TotalDelivered * 100m / TotalMessagesSent) : 0;

    public List<CampaignSummary> CampaignSummaries { get; set; } = new();
    public List<DailySmsSummary> DailyBreakdown { get; set; } = new();
    public int OptOutsThisPeriod { get; set; }
}

/// <summary>
/// Summary of a campaign for reports.
/// </summary>
public class CampaignSummary
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public int Recipients { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>
/// Daily SMS summary.
/// </summary>
public class DailySmsSummary
{
    public DateOnly Date { get; set; }
    public int MessagesSent { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public decimal Cost { get; set; }
}

#endregion

#region Settings

/// <summary>
/// SMS marketing settings.
/// </summary>
public class SmsMarketingSettings
{
    /// <summary>Maximum SMS per day (budget control).</summary>
    public int? MaxSmsPerDay { get; set; }

    /// <summary>Batch size for bulk sending.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Delay between batches in milliseconds.</summary>
    public int BatchDelayMs { get; set; } = 1000;

    /// <summary>Store name for templates.</summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>Auto-send birthday greetings.</summary>
    public bool AutoSendBirthdayGreetings { get; set; }

    /// <summary>Auto-send points expiry reminders.</summary>
    public bool AutoSendPointsExpiryReminders { get; set; }

    /// <summary>Days before expiry to send reminder.</summary>
    public int PointsExpiryReminderDays { get; set; } = 7;

    /// <summary>Send welcome SMS on enrollment.</summary>
    public bool SendWelcomeSms { get; set; } = true;

    /// <summary>Opt-out keywords.</summary>
    public List<string> OptOutKeywords { get; set; } = new() { "STOP", "UNSUBSCRIBE", "QUIT", "CANCEL" };

    /// <summary>Opt-in keywords.</summary>
    public List<string> OptInKeywords { get; set; } = new() { "START", "SUBSCRIBE", "YES" };
}

#endregion

#region Events

/// <summary>
/// Event args for SMS events.
/// </summary>
public class SmsEventArgs : EventArgs
{
    public SmsSentLog Log { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public SmsEventArgs(SmsSentLog log, string eventType)
    {
        Log = log;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for campaign events.
/// </summary>
public class CampaignEventArgs : EventArgs
{
    public SmsCampaign Campaign { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public CampaignEventArgs(SmsCampaign campaign, string eventType)
    {
        Campaign = campaign;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for opt-out events.
/// </summary>
public class OptOutEventArgs : EventArgs
{
    public int CustomerId { get; }
    public string PhoneNumber { get; }
    public string Keyword { get; }
    public DateTime Timestamp { get; }

    public OptOutEventArgs(int customerId, string phoneNumber, string keyword)
    {
        CustomerId = customerId;
        PhoneNumber = phoneNumber;
        Keyword = keyword;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
