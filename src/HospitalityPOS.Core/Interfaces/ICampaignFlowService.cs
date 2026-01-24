using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for automated marketing campaign flows.
/// </summary>
public interface ICampaignFlowService
{
    #region Flow Management

    /// <summary>
    /// Gets all campaign flows.
    /// </summary>
    Task<List<CampaignFlowDto>> GetAllFlowsAsync(int? storeId = null);

    /// <summary>
    /// Gets a campaign flow by ID.
    /// </summary>
    Task<CampaignFlowDto?> GetFlowAsync(int flowId);

    /// <summary>
    /// Gets flows by type.
    /// </summary>
    Task<List<CampaignFlowDto>> GetFlowsByTypeAsync(CampaignFlowType type, int? storeId = null);

    /// <summary>
    /// Gets flows by trigger.
    /// </summary>
    Task<List<CampaignFlowDto>> GetFlowsByTriggerAsync(CampaignFlowTrigger trigger, int? storeId = null);

    /// <summary>
    /// Creates a new campaign flow.
    /// </summary>
    Task<CampaignFlowDto> CreateFlowAsync(CreateCampaignFlowRequest request);

    /// <summary>
    /// Updates an existing campaign flow.
    /// </summary>
    Task<CampaignFlowDto> UpdateFlowAsync(int flowId, CreateCampaignFlowRequest request);

    /// <summary>
    /// Activates a campaign flow.
    /// </summary>
    Task ActivateFlowAsync(int flowId);

    /// <summary>
    /// Deactivates a campaign flow.
    /// </summary>
    Task DeactivateFlowAsync(int flowId);

    /// <summary>
    /// Deletes a campaign flow (soft delete).
    /// </summary>
    Task DeleteFlowAsync(int flowId);

    #endregion

    #region Step Management

    /// <summary>
    /// Adds a step to a flow.
    /// </summary>
    Task<CampaignFlowStepDto> AddStepAsync(int flowId, CreateCampaignFlowStepRequest request);

    /// <summary>
    /// Updates a flow step.
    /// </summary>
    Task<CampaignFlowStepDto> UpdateStepAsync(int stepId, CreateCampaignFlowStepRequest request);

    /// <summary>
    /// Removes a step from a flow.
    /// </summary>
    Task RemoveStepAsync(int stepId);

    /// <summary>
    /// Reorders steps in a flow.
    /// </summary>
    Task ReorderStepsAsync(int flowId, List<int> stepIds);

    /// <summary>
    /// Enables a step.
    /// </summary>
    Task EnableStepAsync(int stepId);

    /// <summary>
    /// Disables a step.
    /// </summary>
    Task DisableStepAsync(int stepId);

    #endregion

    #region Enrollment

    /// <summary>
    /// Enrolls a member in a flow.
    /// </summary>
    Task<EnrollmentResult> EnrollMemberAsync(EnrollMemberRequest request);

    /// <summary>
    /// Triggers a flow for a member based on trigger type.
    /// </summary>
    Task<EnrollmentResult> TriggerFlowAsync(TriggerFlowRequest request);

    /// <summary>
    /// Pauses a member's enrollment.
    /// </summary>
    Task PauseEnrollmentAsync(int enrollmentId, string? reason = null);

    /// <summary>
    /// Resumes a paused enrollment.
    /// </summary>
    Task ResumeEnrollmentAsync(int enrollmentId);

    /// <summary>
    /// Cancels a member's enrollment.
    /// </summary>
    Task CancelEnrollmentAsync(int enrollmentId, string reason);

    /// <summary>
    /// Gets a member's enrollments.
    /// </summary>
    Task<List<MemberFlowEnrollmentDto>> GetMemberEnrollmentsAsync(int memberId, bool activeOnly = false);

    /// <summary>
    /// Gets active enrollments for a flow.
    /// </summary>
    Task<List<MemberFlowEnrollmentDto>> GetActiveEnrollmentsAsync(int flowId);

    /// <summary>
    /// Gets executions for an enrollment.
    /// </summary>
    Task<List<FlowStepExecutionDto>> GetEnrollmentExecutionsAsync(int enrollmentId);

    #endregion

    #region Execution

    /// <summary>
    /// Processes all scheduled steps (background job).
    /// </summary>
    Task<FlowProcessingResult> ProcessScheduledStepsAsync();

    /// <summary>
    /// Executes a specific step for an enrollment.
    /// </summary>
    Task<StepExecutionResult> ExecuteStepAsync(int enrollmentId, int stepId);

    /// <summary>
    /// Skips a step in an enrollment.
    /// </summary>
    Task SkipStepAsync(int enrollmentId, int stepId, string reason);

    /// <summary>
    /// Retries a failed step execution.
    /// </summary>
    Task<StepExecutionResult> RetryStepAsync(int executionId);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets analytics for a specific flow.
    /// </summary>
    Task<FlowAnalytics> GetFlowAnalyticsAsync(int flowId, DateTime from, DateTime to);

    /// <summary>
    /// Gets overall performance report.
    /// </summary>
    Task<FlowPerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets campaign flow configuration.
    /// </summary>
    Task<CampaignFlowConfigurationDto> GetConfigurationAsync(int? storeId = null);

    /// <summary>
    /// Updates campaign flow configuration.
    /// </summary>
    Task<CampaignFlowConfigurationDto> UpdateConfigurationAsync(CampaignFlowConfigurationDto config);

    #endregion

    #region Templates

    /// <summary>
    /// Gets all email templates.
    /// </summary>
    Task<List<EmailTemplateDto>> GetEmailTemplatesAsync(int? storeId = null);

    /// <summary>
    /// Gets an email template by ID.
    /// </summary>
    Task<EmailTemplateDto?> GetEmailTemplateAsync(int templateId);

    /// <summary>
    /// Creates an email template.
    /// </summary>
    Task<EmailTemplateDto> CreateEmailTemplateAsync(EmailTemplateDto template);

    /// <summary>
    /// Updates an email template.
    /// </summary>
    Task<EmailTemplateDto> UpdateEmailTemplateAsync(EmailTemplateDto template);

    /// <summary>
    /// Deletes an email template.
    /// </summary>
    Task DeleteEmailTemplateAsync(int templateId);

    /// <summary>
    /// Gets all SMS templates.
    /// </summary>
    Task<List<SmsTemplateDto>> GetSmsTemplatesAsync(int? storeId = null);

    /// <summary>
    /// Gets an SMS template by ID.
    /// </summary>
    Task<SmsTemplateDto?> GetSmsTemplateAsync(int templateId);

    /// <summary>
    /// Creates an SMS template.
    /// </summary>
    Task<SmsTemplateDto> CreateSmsTemplateAsync(SmsTemplateDto template);

    /// <summary>
    /// Updates an SMS template.
    /// </summary>
    Task<SmsTemplateDto> UpdateSmsTemplateAsync(SmsTemplateDto template);

    /// <summary>
    /// Deletes an SMS template.
    /// </summary>
    Task DeleteSmsTemplateAsync(int templateId);

    /// <summary>
    /// Previews a template with sample data.
    /// </summary>
    Task<string> PreviewTemplateAsync(string content, int? memberId = null);

    #endregion
}

/// <summary>
/// Service interface for triggering flows from various events.
/// </summary>
public interface ICampaignFlowTriggerService
{
    /// <summary>
    /// Called when a member enrolls in the loyalty program.
    /// </summary>
    Task OnMemberEnrolledAsync(int memberId, int? storeId = null);

    /// <summary>
    /// Called on a member's birthday.
    /// </summary>
    Task OnBirthdayAsync(int memberId);

    /// <summary>
    /// Called on a member's signup anniversary.
    /// </summary>
    Task OnAnniversaryAsync(int memberId);

    /// <summary>
    /// Called when a member makes a purchase.
    /// </summary>
    Task OnPurchaseAsync(int memberId, int receiptId, decimal amount, int? storeId = null);

    /// <summary>
    /// Called when a member's tier changes.
    /// </summary>
    Task OnTierChangedAsync(int memberId, LoyaltyTier oldTier, LoyaltyTier newTier);

    /// <summary>
    /// Called when points are about to expire.
    /// </summary>
    Task OnPointsExpiryApproachingAsync(int memberId, int daysUntilExpiry, int expiringPoints);

    /// <summary>
    /// Called when inactivity is detected.
    /// </summary>
    Task OnInactivityDetectedAsync(int memberId, int daysSinceLastVisit);

    /// <summary>
    /// Called when a referral is completed.
    /// </summary>
    Task OnReferralCompleteAsync(int memberId, int referredMemberId);

    /// <summary>
    /// Called when a challenge is completed.
    /// </summary>
    Task OnChallengeCompleteAsync(int memberId, int challengeId);
}

/// <summary>
/// Interface for flow processor background job.
/// </summary>
public interface ICampaignFlowProcessorJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    FlowProcessingResult? LastRunResult { get; }

    /// <summary>
    /// Triggers an immediate run.
    /// </summary>
    Task TriggerRunAsync();
}

/// <summary>
/// Interface for flow trigger background job.
/// </summary>
public interface ICampaignFlowTriggerJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    TriggerProcessingResult? LastRunResult { get; }

    /// <summary>
    /// Triggers an immediate run.
    /// </summary>
    Task TriggerRunAsync();
}
