using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Type of campaign flow.
/// </summary>
public enum CampaignFlowType
{
    Welcome = 1,
    Birthday = 2,
    Anniversary = 3,
    WinBack = 4,
    TierUpgrade = 5,
    TierDowngrade = 6,
    PointsExpiry = 7,
    PostPurchase = 8,
    ReferralSuccess = 9,
    ChallengeComplete = 10,
    Custom = 11
}

/// <summary>
/// What triggers a campaign flow.
/// </summary>
public enum CampaignFlowTrigger
{
    OnEnrollment = 1,
    OnBirthday = 2,
    OnAnniversary = 3,
    OnInactivity = 4,
    OnTierChange = 5,
    OnPointsExpiry = 6,
    OnPurchase = 7,
    OnReferralComplete = 8,
    OnChallengeComplete = 9,
    Manual = 10
}

/// <summary>
/// Communication channel for campaign messages.
/// </summary>
public enum CampaignChannel
{
    Email = 1,
    SMS = 2,
    Push = 3,
    InApp = 4
}

/// <summary>
/// Status of a member's enrollment in a flow.
/// </summary>
public enum FlowEnrollmentStatus
{
    Active = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

/// <summary>
/// Status of a flow step execution.
/// </summary>
public enum FlowStepExecutionStatus
{
    Scheduled = 1,
    Executing = 2,
    Executed = 3,
    Failed = 4,
    Skipped = 5,
    Cancelled = 6
}

/// <summary>
/// Type of condition for step execution.
/// </summary>
public enum StepConditionType
{
    None = 0,
    NoPurchaseSinceEnrollment = 1,
    NoPurchaseSinceLastStep = 2,
    RewardNotRedeemed = 3,
    MemberStillInactive = 4,
    TierIs = 5,
    TierIsNot = 6,
    PointsAbove = 7,
    PointsBelow = 8,
    HasVisitedStore = 9,
    HasNotVisitedStore = 10
}

#endregion

#region Campaign Flow Entities

/// <summary>
/// A campaign flow definition - automated sequence of marketing actions.
/// </summary>
public class CampaignFlow : BaseEntity
{
    /// <summary>
    /// Flow name (e.g., "Welcome Series").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Flow description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of campaign flow.
    /// </summary>
    public CampaignFlowType Type { get; set; }

    /// <summary>
    /// What triggers this flow.
    /// </summary>
    public CampaignFlowTrigger Trigger { get; set; }

    /// <summary>
    /// Days offset from trigger (negative = before, positive = after).
    /// For example, -7 for birthday means 7 days before birthday.
    /// </summary>
    public int TriggerDaysOffset { get; set; }

    /// <summary>
    /// For inactivity trigger: days of inactivity required.
    /// </summary>
    public int? InactivityDaysThreshold { get; set; }

    /// <summary>
    /// Store ID for store-specific flows (null = all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Minimum tier required for this flow.
    /// </summary>
    public LoyaltyTier? MinimumTier { get; set; }

    /// <summary>
    /// Whether this flow is currently active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Maximum times a member can be enrolled in this flow.
    /// </summary>
    public int MaxEnrollmentsPerMember { get; set; } = 1;

    /// <summary>
    /// Cooldown days before re-enrollment is allowed.
    /// </summary>
    public int CooldownDays { get; set; }

    /// <summary>
    /// Display order for admin listing.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual ICollection<CampaignFlowStep> Steps { get; set; } = new List<CampaignFlowStep>();
    public virtual ICollection<MemberFlowEnrollment> Enrollments { get; set; } = new List<MemberFlowEnrollment>();
}

/// <summary>
/// A step within a campaign flow.
/// </summary>
public class CampaignFlowStep : BaseEntity
{
    /// <summary>
    /// Parent flow ID.
    /// </summary>
    public int FlowId { get; set; }

    /// <summary>
    /// Order of this step in the flow.
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Step name (e.g., "Welcome Email").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Step description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Days delay after trigger/previous step.
    /// </summary>
    public int DelayDays { get; set; }

    /// <summary>
    /// Additional hours delay.
    /// </summary>
    public int DelayHours { get; set; }

    /// <summary>
    /// Preferred hour of day to send (0-23).
    /// </summary>
    public int? PreferredSendHour { get; set; }

    /// <summary>
    /// Communication channel.
    /// </summary>
    public CampaignChannel Channel { get; set; }

    /// <summary>
    /// Email template ID if using templates.
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// SMS template ID if using templates.
    /// </summary>
    public int? SmsTemplateId { get; set; }

    /// <summary>
    /// Email subject (for email channel).
    /// </summary>
    [MaxLength(200)]
    public string? Subject { get; set; }

    /// <summary>
    /// Message content with template variables.
    /// </summary>
    [MaxLength(4000)]
    public string? Content { get; set; }

    /// <summary>
    /// Bonus points to award on this step.
    /// </summary>
    public int? BonusPointsToAward { get; set; }

    /// <summary>
    /// Discount percentage to offer.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Fixed discount amount to offer.
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Days the discount is valid.
    /// </summary>
    public int? DiscountValidityDays { get; set; }

    /// <summary>
    /// Condition type for this step.
    /// </summary>
    public StepConditionType ConditionType { get; set; } = StepConditionType.None;

    /// <summary>
    /// Condition value (tier ID, points amount, etc.).
    /// </summary>
    [MaxLength(200)]
    public string? ConditionValue { get; set; }

    /// <summary>
    /// Whether this step is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // Navigation properties
    public virtual CampaignFlow Flow { get; set; } = null!;
    public virtual ICollection<FlowStepExecution> Executions { get; set; } = new List<FlowStepExecution>();
}

/// <summary>
/// A member's enrollment in a campaign flow.
/// </summary>
public class MemberFlowEnrollment : BaseEntity
{
    /// <summary>
    /// Member enrolled in the flow.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Flow the member is enrolled in.
    /// </summary>
    public int FlowId { get; set; }

    /// <summary>
    /// Current step index (0-based).
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Enrollment status.
    /// </summary>
    public FlowEnrollmentStatus Status { get; set; } = FlowEnrollmentStatus.Active;

    /// <summary>
    /// When member was enrolled.
    /// </summary>
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date that triggered the flow (birthday, etc.).
    /// </summary>
    public DateTime TriggerDate { get; set; }

    /// <summary>
    /// When the next step is scheduled.
    /// </summary>
    public DateTime? NextStepScheduledAt { get; set; }

    /// <summary>
    /// When flow was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When flow was paused.
    /// </summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>
    /// When flow was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Context data JSON (trigger-specific data).
    /// </summary>
    [MaxLength(2000)]
    public string? ContextJson { get; set; }

    /// <summary>
    /// Store where enrollment occurred.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual LoyaltyMember Member { get; set; } = null!;
    public virtual CampaignFlow Flow { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual ICollection<FlowStepExecution> Executions { get; set; } = new List<FlowStepExecution>();
}

/// <summary>
/// Record of a step execution within an enrollment.
/// </summary>
public class FlowStepExecution : BaseEntity
{
    /// <summary>
    /// Parent enrollment ID.
    /// </summary>
    public int EnrollmentId { get; set; }

    /// <summary>
    /// Step that was executed.
    /// </summary>
    public int StepId { get; set; }

    /// <summary>
    /// When execution was scheduled.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// When execution actually occurred.
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    public FlowStepExecutionStatus Status { get; set; } = FlowStepExecutionStatus.Scheduled;

    /// <summary>
    /// Channel used for this execution.
    /// </summary>
    public CampaignChannel Channel { get; set; }

    /// <summary>
    /// External message ID from SMS/Email provider.
    /// </summary>
    [MaxLength(200)]
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Whether message was delivered.
    /// </summary>
    public bool? Delivered { get; set; }

    /// <summary>
    /// Delivery timestamp.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Whether email was opened.
    /// </summary>
    public bool? Opened { get; set; }

    /// <summary>
    /// Email open timestamp.
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// Whether link was clicked.
    /// </summary>
    public bool? Clicked { get; set; }

    /// <summary>
    /// Link click timestamp.
    /// </summary>
    public DateTime? ClickedAt { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Points awarded in this step.
    /// </summary>
    public int? PointsAwarded { get; set; }

    /// <summary>
    /// Discount code generated.
    /// </summary>
    [MaxLength(50)]
    public string? DiscountCode { get; set; }

    /// <summary>
    /// Reason if step was skipped.
    /// </summary>
    [MaxLength(500)]
    public string? SkipReason { get; set; }

    /// <summary>
    /// Rendered subject (after template processing).
    /// </summary>
    [MaxLength(200)]
    public string? RenderedSubject { get; set; }

    /// <summary>
    /// Rendered content (after template processing).
    /// </summary>
    [MaxLength(4000)]
    public string? RenderedContent { get; set; }

    // Navigation properties
    public virtual MemberFlowEnrollment Enrollment { get; set; } = null!;
    public virtual CampaignFlowStep Step { get; set; } = null!;
}

#endregion

#region Email/SMS Templates

/// <summary>
/// Reusable email template for marketing campaigns.
/// </summary>
public class CampaignEmailTemplate : BaseEntity
{
    /// <summary>
    /// Template name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Default subject line.
    /// </summary>
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML body content.
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body (fallback).
    /// </summary>
    [MaxLength(4000)]
    public string? TextBody { get; set; }

    /// <summary>
    /// Template category.
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Store ID for store-specific templates.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Reusable SMS template for marketing campaigns.
/// </summary>
public class CampaignSmsTemplate : BaseEntity
{
    /// <summary>
    /// Template name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// SMS content (max 160 chars per segment).
    /// </summary>
    [Required]
    [MaxLength(480)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Template category.
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Store ID for store-specific templates.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion

#region Configuration

/// <summary>
/// Campaign flow global configuration.
/// </summary>
public class CampaignFlowConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID for store-specific config (null = global).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether campaign flows are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether email channel is enabled.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Whether SMS channel is enabled.
    /// </summary>
    public bool SmsEnabled { get; set; } = true;

    /// <summary>
    /// Default sender email address.
    /// </summary>
    [MaxLength(200)]
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Default sender name.
    /// </summary>
    [MaxLength(100)]
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Default SMS sender ID.
    /// </summary>
    [MaxLength(20)]
    public string? DefaultSmsFrom { get; set; }

    /// <summary>
    /// Maximum messages per member per day.
    /// </summary>
    public int MaxMessagesPerMemberPerDay { get; set; } = 3;

    /// <summary>
    /// Quiet hours start (don't send during these hours).
    /// </summary>
    public int? QuietHoursStart { get; set; } = 21; // 9 PM

    /// <summary>
    /// Quiet hours end.
    /// </summary>
    public int? QuietHoursEnd { get; set; } = 8; // 8 AM

    /// <summary>
    /// Days of inactivity before win-back flow triggers.
    /// </summary>
    public int WinBackInactivityDays { get; set; } = 30;

    /// <summary>
    /// Days before birthday to start birthday flow.
    /// </summary>
    public int BirthdayFlowStartDays { get; set; } = 7;

    /// <summary>
    /// Days before points expiry to notify.
    /// </summary>
    public int PointsExpiryNotifyDays { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed messages.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in minutes (doubles each attempt).
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 15;

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion
