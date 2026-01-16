// src/HospitalityPOS.Core/Interfaces/ISmsMarketingService.cs
// Service interface for SMS marketing campaigns and customer segmentation
// Story 47-1: SMS Marketing to Customers

using HospitalityPOS.Core.Models.Marketing;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for SMS marketing.
/// Handles templates, campaigns, customer segmentation, and marketing reports.
/// Extends the base ISmsService for marketing-specific functionality.
/// </summary>
public interface ISmsMarketingService
{
    #region SMS Templates

    /// <summary>
    /// Creates a new SMS template.
    /// </summary>
    /// <param name="request">Template request.</param>
    /// <returns>Created template.</returns>
    Task<SmsTemplate> CreateTemplateAsync(SmsTemplateRequest request);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="request">Template request.</param>
    /// <returns>Updated template.</returns>
    Task<SmsTemplate> UpdateTemplateAsync(SmsTemplateRequest request);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="templateId">Template ID.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteTemplateAsync(int templateId);

    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    /// <param name="templateId">Template ID.</param>
    /// <returns>Template or null.</returns>
    Task<SmsTemplate?> GetTemplateAsync(int templateId);

    /// <summary>
    /// Gets all active templates.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <returns>List of templates.</returns>
    Task<IReadOnlyList<SmsTemplate>> GetTemplatesAsync(SmsTemplateCategory? category = null);

    /// <summary>
    /// Previews a template with sample data.
    /// </summary>
    /// <param name="templateId">Template ID.</param>
    /// <param name="sampleData">Sample placeholder values.</param>
    /// <returns>Preview.</returns>
    Task<SmsTemplatePreview> PreviewTemplateAsync(int templateId, Dictionary<string, string>? sampleData = null);

    /// <summary>
    /// Renders a message with customer data.
    /// </summary>
    /// <param name="messageText">Message with placeholders.</param>
    /// <param name="customer">Customer info.</param>
    /// <returns>Rendered message.</returns>
    string RenderMessage(string messageText, CustomerSmsInfo customer);

    #endregion

    #region Customer Segmentation

    /// <summary>
    /// Creates a new customer segment.
    /// </summary>
    /// <param name="request">Segment request.</param>
    /// <returns>Created segment.</returns>
    Task<CustomerSegment> CreateSegmentAsync(CustomerSegmentRequest request);

    /// <summary>
    /// Updates an existing segment.
    /// </summary>
    /// <param name="request">Segment request.</param>
    /// <returns>Updated segment.</returns>
    Task<CustomerSegment> UpdateSegmentAsync(CustomerSegmentRequest request);

    /// <summary>
    /// Deletes a segment.
    /// </summary>
    /// <param name="segmentId">Segment ID.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteSegmentAsync(int segmentId);

    /// <summary>
    /// Gets a segment by ID.
    /// </summary>
    /// <param name="segmentId">Segment ID.</param>
    /// <returns>Segment or null.</returns>
    Task<CustomerSegment?> GetSegmentAsync(int segmentId);

    /// <summary>
    /// Gets all active segments.
    /// </summary>
    /// <returns>List of segments.</returns>
    Task<IReadOnlyList<CustomerSegment>> GetSegmentsAsync();

    /// <summary>
    /// Evaluates a segment filter and returns matching customers.
    /// </summary>
    /// <param name="filter">Segment filter.</param>
    /// <returns>Segment result with matching customers.</returns>
    Task<SegmentResult> EvaluateSegmentAsync(SegmentFilter filter);

    /// <summary>
    /// Gets customers who purchased within X days.
    /// </summary>
    /// <param name="days">Number of days.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetCustomersWhoPurchasedWithinDaysAsync(int days);

    /// <summary>
    /// Gets customers who purchased from a category within X days.
    /// </summary>
    /// <param name="categoryId">Category ID.</param>
    /// <param name="days">Number of days.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetCustomersWhoPurchasedCategoryAsync(int categoryId, int days);

    /// <summary>
    /// Gets customers who purchased a specific product within X days.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="days">Number of days.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetCustomersWhoPurchasedProductAsync(int productId, int days);

    /// <summary>
    /// Gets customers who spent at least a minimum amount within X days.
    /// </summary>
    /// <param name="minimumSpend">Minimum spend amount.</param>
    /// <param name="days">Number of days.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetCustomersBySpendAsync(decimal minimumSpend, int days);

    /// <summary>
    /// Gets customers by loyalty tier.
    /// </summary>
    /// <param name="tierName">Tier name.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetCustomersByTierAsync(string tierName);

    /// <summary>
    /// Gets lapsed customers (no visit for X days).
    /// </summary>
    /// <param name="days">Number of days since last visit.</param>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetLapsedCustomersAsync(int days);

    /// <summary>
    /// Gets all opted-in customers.
    /// </summary>
    /// <returns>Segment result.</returns>
    Task<SegmentResult> GetAllOptedInCustomersAsync();

    /// <summary>
    /// Gets segment count preview.
    /// </summary>
    /// <param name="filter">Segment filter.</param>
    /// <returns>Count of matching customers.</returns>
    Task<int> GetSegmentCountAsync(SegmentFilter filter);

    #endregion

    #region SMS Campaigns

    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    /// <param name="request">Campaign request.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> CreateCampaignAsync(SmsCampaignRequest request);

    /// <summary>
    /// Updates an existing campaign.
    /// </summary>
    /// <param name="request">Campaign request.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> UpdateCampaignAsync(SmsCampaignRequest request);

    /// <summary>
    /// Schedules a campaign for future delivery.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <param name="scheduledAt">Scheduled time.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> ScheduleCampaignAsync(int campaignId, DateTime scheduledAt);

    /// <summary>
    /// Starts sending a campaign.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> StartCampaignAsync(int campaignId);

    /// <summary>
    /// Pauses a campaign that is sending.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> PauseCampaignAsync(int campaignId);

    /// <summary>
    /// Resumes a paused campaign.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> ResumeCampaignAsync(int campaignId);

    /// <summary>
    /// Cancels a campaign.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign result.</returns>
    Task<CampaignResult> CancelCampaignAsync(int campaignId);

    /// <summary>
    /// Gets a campaign by ID.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign or null.</returns>
    Task<SmsCampaign?> GetCampaignAsync(int campaignId);

    /// <summary>
    /// Gets campaigns by status.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <returns>List of campaigns.</returns>
    Task<IReadOnlyList<SmsCampaign>> GetCampaignsAsync(CampaignStatus? status = null);

    /// <summary>
    /// Gets scheduled campaigns due for sending.
    /// </summary>
    /// <returns>List of due campaigns.</returns>
    Task<IReadOnlyList<SmsCampaign>> GetDueCampaignsAsync();

    /// <summary>
    /// Gets campaign sending progress.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Progress info.</returns>
    Task<BatchSendProgress?> GetCampaignProgressAsync(int campaignId);

    #endregion

    #region SMS Sending

    /// <summary>
    /// Sends campaign messages to all target customers.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Number of messages sent.</returns>
    Task<int> SendCampaignMessagesAsync(int campaignId);

    /// <summary>
    /// Gets SMS sent log.
    /// </summary>
    /// <param name="campaignId">Optional campaign filter.</param>
    /// <param name="startDate">Optional start date.</param>
    /// <param name="endDate">Optional end date.</param>
    /// <returns>List of log entries.</returns>
    Task<IReadOnlyList<SmsSentLog>> GetSentLogAsync(
        int? campaignId = null, DateOnly? startDate = null, DateOnly? endDate = null);

    #endregion

    #region Transactional SMS

    /// <summary>
    /// Gets transactional SMS configurations.
    /// </summary>
    /// <returns>List of configurations.</returns>
    Task<IReadOnlyList<TransactionalSmsConfig>> GetTransactionalConfigsAsync();

    /// <summary>
    /// Updates transactional SMS configuration.
    /// </summary>
    /// <param name="config">Configuration.</param>
    /// <returns>Updated configuration.</returns>
    Task<TransactionalSmsConfig> UpdateTransactionalConfigAsync(TransactionalSmsConfig config);

    /// <summary>
    /// Sends a transactional SMS.
    /// </summary>
    /// <param name="request">Transactional request.</param>
    /// <returns>Send result.</returns>
    Task<SmsResult> SendTransactionalSmsAsync(TransactionalSmsRequest request);

    #endregion

    #region Opt-In/Opt-Out

    /// <summary>
    /// Gets SMS consent for a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <returns>Consent record.</returns>
    Task<SmsConsent?> GetConsentAsync(int customerId);

    /// <summary>
    /// Sets customer opt-in status.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="optIn">Opt-in status.</param>
    /// <returns>True if updated.</returns>
    Task<bool> SetOptInStatusAsync(int customerId, bool optIn);

    /// <summary>
    /// Processes incoming opt-out message.
    /// </summary>
    /// <param name="phoneNumber">Phone number.</param>
    /// <param name="keyword">Opt-out keyword used.</param>
    /// <returns>True if processed.</returns>
    Task<bool> ProcessOptOutAsync(string phoneNumber, string keyword);

    /// <summary>
    /// Gets opt-out log.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of opt-out entries.</returns>
    Task<IReadOnlyList<SmsOptOutLog>> GetOptOutLogAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Reports

    /// <summary>
    /// Generates campaign report.
    /// </summary>
    /// <param name="campaignId">Campaign ID.</param>
    /// <returns>Campaign report.</returns>
    Task<CampaignReport> GenerateCampaignReportAsync(int campaignId);

    /// <summary>
    /// Generates SMS usage report.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Usage report.</returns>
    Task<SmsUsageReport> GenerateUsageReportAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Settings

    /// <summary>
    /// Gets SMS marketing settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<SmsMarketingSettings> GetSettingsAsync();

    /// <summary>
    /// Updates SMS marketing settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<SmsMarketingSettings> UpdateSettingsAsync(SmsMarketingSettings settings);

    #endregion

    #region Events

    /// <summary>Raised when a campaign starts.</summary>
    event EventHandler<CampaignEventArgs>? CampaignStarted;

    /// <summary>Raised when a campaign completes.</summary>
    event EventHandler<CampaignEventArgs>? CampaignCompleted;

    /// <summary>Raised when a customer opts out.</summary>
    event EventHandler<OptOutEventArgs>? CustomerOptedOut;

    #endregion
}
