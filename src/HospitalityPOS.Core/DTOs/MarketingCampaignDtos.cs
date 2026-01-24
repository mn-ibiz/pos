using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Campaign Flow DTOs

/// <summary>
/// Campaign flow display information.
/// </summary>
public class CampaignFlowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignFlowType Type { get; set; }
    public string TypeName => Type.ToString();
    public CampaignFlowTrigger Trigger { get; set; }
    public string TriggerName => Trigger.ToString();
    public int TriggerDaysOffset { get; set; }
    public int? InactivityDaysThreshold { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public LoyaltyTier? MinimumTier { get; set; }
    public bool IsEnabled { get; set; }
    public int MaxEnrollmentsPerMember { get; set; }
    public int CooldownDays { get; set; }
    public int DisplayOrder { get; set; }
    public int StepCount { get; set; }
    public int ActiveEnrollments { get; set; }
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public List<CampaignFlowStepDto> Steps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Campaign flow step display information.
/// </summary>
public class CampaignFlowStepDto
{
    public int Id { get; set; }
    public int FlowId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
    public int? PreferredSendHour { get; set; }
    public CampaignChannel Channel { get; set; }
    public string ChannelName => Channel.ToString();
    public int? EmailTemplateId { get; set; }
    public string? EmailTemplateName { get; set; }
    public int? SmsTemplateId { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public int? BonusPointsToAward { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? DiscountValidityDays { get; set; }
    public StepConditionType ConditionType { get; set; }
    public string? ConditionValue { get; set; }
    public bool IsEnabled { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string TotalDelayDisplay => DelayDays > 0 || DelayHours > 0
        ? $"{(DelayDays > 0 ? $"{DelayDays}d " : "")}{(DelayHours > 0 ? $"{DelayHours}h" : "")}".Trim()
        : "Immediate";
}

/// <summary>
/// Member flow enrollment information.
/// </summary>
public class MemberFlowEnrollmentDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberPhone { get; set; }
    public string? MemberEmail { get; set; }
    public int FlowId { get; set; }
    public string FlowName { get; set; } = string.Empty;
    public CampaignFlowType FlowType { get; set; }
    public int CurrentStepIndex { get; set; }
    public int TotalSteps { get; set; }
    public FlowEnrollmentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime EnrolledAt { get; set; }
    public DateTime TriggerDate { get; set; }
    public DateTime? NextStepScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public decimal ProgressPercentage => TotalSteps > 0 ? (decimal)CurrentStepIndex / TotalSteps * 100 : 0;
    public List<FlowStepExecutionDto> Executions { get; set; } = new();
}

/// <summary>
/// Flow step execution record.
/// </summary>
public class FlowStepExecutionDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public FlowStepExecutionStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public CampaignChannel Channel { get; set; }
    public string ChannelName => Channel.ToString();
    public string? ExternalMessageId { get; set; }
    public bool? Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool? Opened { get; set; }
    public DateTime? OpenedAt { get; set; }
    public bool? Clicked { get; set; }
    public DateTime? ClickedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int? PointsAwarded { get; set; }
    public string? DiscountCode { get; set; }
    public string? SkipReason { get; set; }
    public string? RenderedSubject { get; set; }
}

#endregion

#region Template DTOs

/// <summary>
/// Email template display information.
/// </summary>
public class EmailTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? Category { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// SMS template display information.
/// </summary>
public class SmsTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public int CharacterCount => Content?.Length ?? 0;
    public int SegmentCount => CharacterCount <= 160 ? 1 : (int)Math.Ceiling(CharacterCount / 153.0);
    public string? Category { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region Request DTOs

/// <summary>
/// Request to create a campaign flow.
/// </summary>
public class CreateCampaignFlowRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignFlowType Type { get; set; }
    public CampaignFlowTrigger Trigger { get; set; }
    public int TriggerDaysOffset { get; set; }
    public int? InactivityDaysThreshold { get; set; }
    public int? StoreId { get; set; }
    public LoyaltyTier? MinimumTier { get; set; }
    public int MaxEnrollmentsPerMember { get; set; } = 1;
    public int CooldownDays { get; set; }
    public List<CreateCampaignFlowStepRequest> Steps { get; set; } = new();
}

/// <summary>
/// Request to create a campaign flow step.
/// </summary>
public class CreateCampaignFlowStepRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
    public int? PreferredSendHour { get; set; }
    public CampaignChannel Channel { get; set; }
    public int? EmailTemplateId { get; set; }
    public int? SmsTemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public int? BonusPointsToAward { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? DiscountValidityDays { get; set; }
    public StepConditionType ConditionType { get; set; } = StepConditionType.None;
    public string? ConditionValue { get; set; }
}

/// <summary>
/// Request to trigger a flow for a member.
/// </summary>
public class TriggerFlowRequest
{
    public int MemberId { get; set; }
    public CampaignFlowTrigger Trigger { get; set; }
    public DateTime? TriggerDate { get; set; }
    public int? StoreId { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Request to enroll a member in a flow.
/// </summary>
public class EnrollMemberRequest
{
    public int MemberId { get; set; }
    public int FlowId { get; set; }
    public DateTime? TriggerDate { get; set; }
    public int? StoreId { get; set; }
}

#endregion

#region Result DTOs

/// <summary>
/// Result of enrolling a member in a flow.
/// </summary>
public class EnrollmentResult
{
    public bool Success { get; set; }
    public MemberFlowEnrollmentDto? Enrollment { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of executing a flow step.
/// </summary>
public class StepExecutionResult
{
    public bool Success { get; set; }
    public FlowStepExecutionDto? Execution { get; set; }
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public int? PointsAwarded { get; set; }
    public string? DiscountCode { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of processing scheduled steps.
/// </summary>
public class FlowProcessingResult
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int StepsProcessed { get; set; }
    public int StepsExecuted { get; set; }
    public int StepsSkipped { get; set; }
    public int StepsFailed { get; set; }
    public int EnrollmentsCompleted { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;
}

/// <summary>
/// Result of daily trigger processing.
/// </summary>
public class TriggerProcessingResult
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int BirthdayFlowsTriggered { get; set; }
    public int AnniversaryFlowsTriggered { get; set; }
    public int WinBackFlowsTriggered { get; set; }
    public int PointsExpiryFlowsTriggered { get; set; }
    public int TotalEnrollments { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;
}

#endregion

#region Analytics DTOs

/// <summary>
/// Analytics for a specific flow.
/// </summary>
public class FlowAnalytics
{
    public int FlowId { get; set; }
    public string FlowName { get; set; } = string.Empty;
    public CampaignFlowType FlowType { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalEnrollments { get; set; }
    public int ActiveEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public int CancelledEnrollments { get; set; }
    public decimal CompletionRate => TotalEnrollments > 0 ? (decimal)CompletedEnrollments / TotalEnrollments * 100 : 0;
    public int TotalMessagesent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public decimal DeliveryRate => TotalMessagesent > 0 ? (decimal)TotalDelivered / TotalMessagesent * 100 : 0;
    public decimal OpenRate => TotalDelivered > 0 ? (decimal)TotalOpened / TotalDelivered * 100 : 0;
    public decimal ClickRate => TotalOpened > 0 ? (decimal)TotalClicked / TotalOpened * 100 : 0;
    public int TotalPointsAwarded { get; set; }
    public int TotalDiscountCodesIssued { get; set; }
    public decimal TotalDiscountRedeemed { get; set; }
    public List<StepAnalytics> StepAnalytics { get; set; } = new();
}

/// <summary>
/// Analytics for a specific step.
/// </summary>
public class StepAnalytics
{
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public CampaignChannel Channel { get; set; }
    public int Executions { get; set; }
    public int Delivered { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public decimal DeliveryRate => Executions > 0 ? (decimal)Delivered / Executions * 100 : 0;
    public decimal OpenRate => Delivered > 0 ? (decimal)Opened / Delivered * 100 : 0;
    public decimal ClickRate => Opened > 0 ? (decimal)Clicked / Opened * 100 : 0;
}

/// <summary>
/// Overall flow performance report.
/// </summary>
public class FlowPerformanceReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalActiveFlows { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalMessagesent { get; set; }
    public int TotalDelivered { get; set; }
    public decimal OverallDeliveryRate { get; set; }
    public decimal OverallOpenRate { get; set; }
    public decimal OverallClickRate { get; set; }
    public Dictionary<CampaignFlowType, int> EnrollmentsByType { get; set; } = new();
    public Dictionary<CampaignChannel, int> MessagesByChannel { get; set; } = new();
    public List<FlowAnalytics> TopPerformingFlows { get; set; } = new();
}

#endregion

#region Configuration DTOs

/// <summary>
/// Campaign flow configuration.
/// </summary>
public class CampaignFlowConfigurationDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public bool IsEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public string? DefaultFromEmail { get; set; }
    public string? DefaultFromName { get; set; }
    public string? DefaultSmsFrom { get; set; }
    public int MaxMessagesPerMemberPerDay { get; set; }
    public int? QuietHoursStart { get; set; }
    public int? QuietHoursEnd { get; set; }
    public int WinBackInactivityDays { get; set; }
    public int BirthdayFlowStartDays { get; set; }
    public int PointsExpiryNotifyDays { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelayMinutes { get; set; }
}

#endregion

#region Template Variables

/// <summary>
/// Available template variables for message personalization.
/// </summary>
public static class TemplateVariables
{
    public const string CustomerName = "{CustomerName}";
    public const string FirstName = "{FirstName}";
    public const string LastName = "{LastName}";
    public const string StoreName = "{StoreName}";
    public const string PointsBalance = "{PointsBalance}";
    public const string TierName = "{TierName}";
    public const string TierMultiplier = "{TierMultiplier}";
    public const string WelcomeBonus = "{WelcomeBonus}";
    public const string BirthdayDiscount = "{BirthdayDiscount}";
    public const string DiscountCode = "{DiscountCode}";
    public const string DiscountPercent = "{DiscountPercent}";
    public const string DiscountAmount = "{DiscountAmount}";
    public const string ExpiringPoints = "{ExpiringPoints}";
    public const string ExpiryDate = "{ExpiryDate}";
    public const string DaysSinceVisit = "{DaysSinceVisit}";
    public const string ReferralCode = "{ReferralCode}";
    public const string MembershipNumber = "{MembershipNumber}";
    public const string BonusPoints = "{BonusPoints}";
    public const string NewTierName = "{NewTierName}";
    public const string OldTierName = "{OldTierName}";
    public const string ValidityDays = "{ValidityDays}";

    /// <summary>
    /// Gets all available template variables.
    /// </summary>
    public static Dictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>
        {
            { CustomerName, "Member's full name" },
            { FirstName, "Member's first name" },
            { LastName, "Member's last name" },
            { StoreName, "Business/store name" },
            { PointsBalance, "Current points balance" },
            { TierName, "Current loyalty tier" },
            { TierMultiplier, "Points multiplier for tier" },
            { WelcomeBonus, "Welcome bonus points" },
            { BirthdayDiscount, "Birthday discount percentage" },
            { DiscountCode, "Generated discount code" },
            { DiscountPercent, "Discount percentage" },
            { DiscountAmount, "Discount amount" },
            { ExpiringPoints, "Points about to expire" },
            { ExpiryDate, "Points expiry date" },
            { DaysSinceVisit, "Days since last visit" },
            { ReferralCode, "Member's referral code" },
            { MembershipNumber, "Membership ID" },
            { BonusPoints, "Bonus points being awarded" },
            { NewTierName, "New tier after upgrade" },
            { OldTierName, "Previous tier before change" },
            { ValidityDays, "Days discount is valid" }
        };
    }
}

#endregion
