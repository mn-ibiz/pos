// tests/HospitalityPOS.Business.Tests/Services/SmsMarketingServiceTests.cs
// Unit tests for SmsMarketingService
// Story 47-1: SMS Marketing to Customers

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Marketing;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class SmsMarketingServiceTests
{
    private readonly ISmsMarketingService _service;

    public SmsMarketingServiceTests()
    {
        _service = new SmsMarketingService();
    }

    #region Template Tests

    [Fact]
    public async Task GetTemplatesAsync_ReturnsDefaultTemplates()
    {
        // Act
        var templates = await _service.GetTemplatesAsync();

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.Name.Contains("Promotion"));
        templates.Should().Contain(t => t.Name.Contains("Points"));
        templates.Should().Contain(t => t.Name.Contains("Birthday"));
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        // Act
        var templates = await _service.GetTemplatesAsync(SmsTemplateCategory.Loyalty);

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(t => t.Category == SmsTemplateCategory.Loyalty);
    }

    [Fact]
    public async Task CreateTemplateAsync_CreatesNewTemplate()
    {
        // Arrange
        var request = new SmsTemplateRequest
        {
            Name = "Test Template",
            Category = SmsTemplateCategory.Promotion,
            MessageText = "Hi {CustomerName}! Special offer at {StoreName}."
        };

        // Act
        var template = await _service.CreateTemplateAsync(request);

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().BeGreaterThan(0);
        template.Name.Should().Be("Test Template");
        template.Placeholders.Should().Contain("CustomerName");
        template.Placeholders.Should().Contain("StoreName");
        template.CharacterCount.Should().Be(request.MessageText.Length);
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesExistingTemplate()
    {
        // Arrange
        var templates = await _service.GetTemplatesAsync();
        var template = templates.First();
        var request = new SmsTemplateRequest
        {
            Id = template.Id,
            Name = "Updated Template",
            Category = SmsTemplateCategory.Special,
            MessageText = "Updated message for {CustomerName}."
        };

        // Act
        var updated = await _service.UpdateTemplateAsync(request);

        // Assert
        updated.Name.Should().Be("Updated Template");
        updated.Category.Should().Be(SmsTemplateCategory.Special);
        updated.Placeholders.Should().Contain("CustomerName");
    }

    [Fact]
    public async Task PreviewTemplateAsync_ReturnsPreviewWithSampleData()
    {
        // Arrange
        var templates = await _service.GetTemplatesAsync();
        var template = templates.First(t => t.Placeholders.Contains("CustomerName"));

        // Act
        var preview = await _service.PreviewTemplateAsync(template.Id);

        // Assert
        preview.Should().NotBeNull();
        preview.RenderedMessage.Should().NotContain("{CustomerName}");
        preview.RenderedMessage.Should().Contain("John Kamau"); // Sample data
        preview.CharacterCount.Should().BeGreaterThan(0);
        preview.SmsSegments.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void RenderMessage_SubstitutesPlaceholders()
    {
        // Arrange
        var customer = new CustomerSmsInfo
        {
            Name = "Test Customer",
            PointsBalance = 1500,
            TierName = "Gold",
            PhoneNumber = "254712345678"
        };
        var message = "Hi {CustomerName}, you have {PointsBalance} points as a {TierName} member!";

        // Act
        var rendered = _service.RenderMessage(message, customer);

        // Assert
        rendered.Should().Contain("Test Customer");
        rendered.Should().Contain("1,500");
        rendered.Should().Contain("Gold");
        rendered.Should().NotContain("{");
    }

    #endregion

    #region Customer Segmentation Tests

    [Fact]
    public async Task GetAllOptedInCustomersAsync_ReturnsOptedInOnly()
    {
        // Act
        var result = await _service.GetAllOptedInCustomersAsync();

        // Assert
        result.MatchingCount.Should().BeGreaterThan(0);
        result.Customers.Should().OnlyContain(c => c.IsOptedIn);
    }

    [Fact]
    public async Task GetCustomersByTierAsync_FiltersByTier()
    {
        // Act
        var result = await _service.GetCustomersByTierAsync("Gold");

        // Assert
        result.MatchingCount.Should().BeGreaterThan(0);
        result.Customers.Should().OnlyContain(c => c.TierName == "Gold");
    }

    [Fact]
    public async Task GetLapsedCustomersAsync_ReturnsCustomersWithNoRecentVisit()
    {
        // Act
        var result = await _service.GetLapsedCustomersAsync(30);

        // Assert
        result.Customers.Should().OnlyContain(c =>
            !c.LastVisit.HasValue || (DateTime.Today - c.LastVisit.Value).TotalDays > 30);
    }

    [Fact]
    public async Task GetCustomersBySpendAsync_FiltersByMinimumSpend()
    {
        // Act
        var result = await _service.GetCustomersBySpendAsync(50000, 365);

        // Assert
        result.Customers.Should().OnlyContain(c => c.TotalSpend >= 50000);
    }

    [Fact]
    public async Task EvaluateSegmentAsync_WithAndLogic_AppliesAllCriteria()
    {
        // Arrange
        var filter = new SegmentFilter
        {
            Logic = SegmentLogic.And,
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.LoyaltyTier, TierName = "Gold" },
                new() { Type = SegmentCriterionType.LastVisitDays, DaysBack = 30 }
            }
        };

        // Act
        var result = await _service.EvaluateSegmentAsync(filter);

        // Assert
        result.Customers.Should().OnlyContain(c =>
            c.TierName == "Gold" &&
            c.LastVisit.HasValue &&
            (DateTime.Today - c.LastVisit.Value).TotalDays <= 30);
    }

    [Fact]
    public async Task CreateSegmentAsync_CreatesAndCalculatesCount()
    {
        // Arrange
        var request = new CustomerSegmentRequest
        {
            Name = "Test Segment",
            Description = "Gold tier customers",
            FilterCriteria = new SegmentFilter
            {
                Criteria = new List<SegmentCriterion>
                {
                    new() { Type = SegmentCriterionType.LoyaltyTier, TierName = "Gold" }
                }
            },
            CreatedByUserId = 1
        };

        // Act
        var segment = await _service.CreateSegmentAsync(request);

        // Assert
        segment.Should().NotBeNull();
        segment.Id.Should().BeGreaterThan(0);
        segment.Name.Should().Be("Test Segment");
        segment.CachedCount.Should().BeGreaterThan(0);
        segment.LastCalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetSegmentCountAsync_ReturnsMatchingCount()
    {
        // Arrange
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.LoyaltyTier, TierName = "Silver" }
            }
        };

        // Act
        var count = await _service.GetSegmentCountAsync(filter);

        // Assert
        count.Should().BeGreaterThan(0);
    }

    #endregion

    #region Campaign Tests

    [Fact]
    public async Task CreateCampaignAsync_CreatesNewCampaign()
    {
        // Arrange
        var request = new SmsCampaignRequest
        {
            Name = "Test Campaign",
            MessageText = "Hi {CustomerName}! Check out our special offers at {StoreName}.",
            CreatedByUserId = 1
        };

        // Act
        var result = await _service.CreateCampaignAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Campaign.Should().NotBeNull();
        result.Campaign!.Name.Should().Be("Test Campaign");
        result.Campaign.Status.Should().Be(CampaignStatus.Draft);
        result.Campaign.TargetCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCampaignAsync_WithSegment_UsesSegmentForTargeting()
    {
        // Arrange
        var segmentRequest = new CustomerSegmentRequest
        {
            Name = "Gold Customers",
            FilterCriteria = new SegmentFilter
            {
                Criteria = new List<SegmentCriterion>
                {
                    new() { Type = SegmentCriterionType.LoyaltyTier, TierName = "Gold" }
                }
            },
            CreatedByUserId = 1
        };
        var segment = await _service.CreateSegmentAsync(segmentRequest);

        var campaignRequest = new SmsCampaignRequest
        {
            Name = "Gold Customer Campaign",
            MessageText = "Exclusive offer for Gold members!",
            CustomerSegmentId = segment.Id,
            CreatedByUserId = 1
        };

        // Act
        var result = await _service.CreateCampaignAsync(campaignRequest);

        // Assert
        result.Success.Should().BeTrue();
        result.Campaign!.CustomerSegmentId.Should().Be(segment.Id);
        result.Campaign.TargetCount.Should().Be(segment.CachedCount);
    }

    [Fact]
    public async Task ScheduleCampaignAsync_SchedulesDraftCampaign()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Scheduled Campaign",
            MessageText = "Test message",
            CreatedByUserId = 1
        });

        var scheduledTime = DateTime.UtcNow.AddHours(1);

        // Act
        var result = await _service.ScheduleCampaignAsync(createResult.Campaign!.Id, scheduledTime);

        // Assert
        result.Success.Should().BeTrue();
        result.Campaign!.Status.Should().Be(CampaignStatus.Scheduled);
        result.Campaign.ScheduledAt.Should().Be(scheduledTime);
    }

    [Fact]
    public async Task ScheduleCampaignAsync_RejectsPastTime()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Test Campaign",
            MessageText = "Test message",
            CreatedByUserId = 1
        });

        var pastTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = await _service.ScheduleCampaignAsync(createResult.Campaign!.Id, pastTime);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("future");
    }

    [Fact]
    public async Task StartCampaignAsync_SendsMessagesAndCompletes()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Send Now Campaign",
            MessageText = "Hi {CustomerName}! Special offer for you.",
            CreatedByUserId = 1
        });

        // Act
        var result = await _service.StartCampaignAsync(createResult.Campaign!.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Campaign!.Status.Should().Be(CampaignStatus.Completed);
        result.Campaign.SentCount.Should().BeGreaterThan(0);
        result.Campaign.TotalCost.Should().BeGreaterThan(0);
        result.Campaign.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelCampaignAsync_CancelsDraftCampaign()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "To Cancel",
            MessageText = "Test",
            CreatedByUserId = 1
        });

        // Act
        var result = await _service.CancelCampaignAsync(createResult.Campaign!.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Campaign!.Status.Should().Be(CampaignStatus.Cancelled);
    }

    [Fact]
    public async Task GetCampaignsAsync_FiltersByStatus()
    {
        // Arrange
        await _service.CreateCampaignAsync(new SmsCampaignRequest { Name = "Draft1", MessageText = "Test", CreatedByUserId = 1 });
        await _service.CreateCampaignAsync(new SmsCampaignRequest { Name = "Draft2", MessageText = "Test", CreatedByUserId = 1 });

        // Act
        var drafts = await _service.GetCampaignsAsync(CampaignStatus.Draft);

        // Assert
        drafts.Should().NotBeEmpty();
        drafts.Should().OnlyContain(c => c.Status == CampaignStatus.Draft);
    }

    [Fact]
    public async Task GetCampaignProgressAsync_ReturnsProgress()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Progress Test",
            MessageText = "Test",
            CreatedByUserId = 1
        });
        await _service.StartCampaignAsync(createResult.Campaign!.Id);

        // Act
        var progress = await _service.GetCampaignProgressAsync(createResult.Campaign.Id);

        // Assert
        progress.Should().NotBeNull();
        progress!.CampaignId.Should().Be(createResult.Campaign.Id);
        progress.IsComplete.Should().BeTrue();
    }

    #endregion

    #region Transactional SMS Tests

    [Fact]
    public async Task GetTransactionalConfigsAsync_ReturnsDefaultConfigs()
    {
        // Act
        var configs = await _service.GetTransactionalConfigsAsync();

        // Assert
        configs.Should().NotBeEmpty();
        configs.Should().Contain(c => c.Type == TransactionalSmsType.Welcome);
        configs.Should().Contain(c => c.Type == TransactionalSmsType.TierUpgrade);
    }

    [Fact]
    public async Task UpdateTransactionalConfigAsync_UpdatesConfig()
    {
        // Arrange
        var config = new TransactionalSmsConfig
        {
            Type = TransactionalSmsType.PointsEarned,
            Name = "Points Earned",
            IsEnabled = true,
            MinIntervalMinutes = 60
        };

        // Act
        var updated = await _service.UpdateTransactionalConfigAsync(config);

        // Assert
        updated.IsEnabled.Should().BeTrue();
        updated.MinIntervalMinutes.Should().Be(60);
    }

    [Fact]
    public async Task SendTransactionalSmsAsync_SendsToOptedInCustomer()
    {
        // Arrange
        await _service.UpdateTransactionalConfigAsync(new TransactionalSmsConfig
        {
            Type = TransactionalSmsType.TierUpgrade,
            IsEnabled = true
        });

        var request = new TransactionalSmsRequest
        {
            Type = TransactionalSmsType.TierUpgrade,
            CustomerId = 1, // Opted-in customer
            Data = new Dictionary<string, string> { { "TierName", "Platinum" } }
        };

        // Act
        var result = await _service.SendTransactionalSmsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.GatewayMessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendTransactionalSmsAsync_FailsForOptedOutCustomer()
    {
        // Arrange
        await _service.UpdateTransactionalConfigAsync(new TransactionalSmsConfig
        {
            Type = TransactionalSmsType.Welcome,
            IsEnabled = true
        });

        var request = new TransactionalSmsRequest
        {
            Type = TransactionalSmsType.Welcome,
            CustomerId = 5 // Opted-out customer
        };

        // Act
        var result = await _service.SendTransactionalSmsAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("opted out");
    }

    [Fact]
    public async Task SendTransactionalSmsAsync_HandlesValuesWithBracesWithoutDoubleRendering()
    {
        // Arrange - test that values containing braces don't get double-rendered
        await _service.UpdateTransactionalConfigAsync(new TransactionalSmsConfig
        {
            Type = TransactionalSmsType.PointsEarned,
            IsEnabled = true,
            DefaultMessage = "You earned {PointsEarned} points! Balance: {PointsBalance}"
        });

        var request = new TransactionalSmsRequest
        {
            Type = TransactionalSmsType.PointsEarned,
            CustomerId = 1,
            Data = new Dictionary<string, string>
            {
                // Value contains braces that could be misinterpreted as placeholders
                { "PointsEarned", "100 {bonus}" }
            }
        };

        // Act
        var result = await _service.SendTransactionalSmsAsync(request);

        // Assert - should succeed without errors from double-rendering
        result.Success.Should().BeTrue();

        // Verify the message was sent correctly via the sent log
        var logs = await _service.GetSentLogAsync();
        var lastLog = logs.LastOrDefault();
        lastLog.Should().NotBeNull();
        // The value "100 {bonus}" should appear as-is, not be treated as a placeholder
        lastLog!.MessageText.Should().Contain("100 {bonus}");
    }

    [Fact]
    public async Task SendTransactionalSmsAsync_RequestDataOverridesCustomerData()
    {
        // Arrange - verify request data takes precedence over customer defaults
        await _service.UpdateTransactionalConfigAsync(new TransactionalSmsConfig
        {
            Type = TransactionalSmsType.TierUpgrade,
            IsEnabled = true,
            DefaultMessage = "Welcome {CustomerName}! You are now {TierName}!"
        });

        var request = new TransactionalSmsRequest
        {
            Type = TransactionalSmsType.TierUpgrade,
            CustomerId = 1,
            Data = new Dictionary<string, string>
            {
                // Override the tier name from customer data
                { "TierName", "Diamond Elite" }
            }
        };

        // Act
        var result = await _service.SendTransactionalSmsAsync(request);

        // Assert
        result.Success.Should().BeTrue();

        var logs = await _service.GetSentLogAsync();
        var lastLog = logs.LastOrDefault();
        lastLog.Should().NotBeNull();
        // Request data "Diamond Elite" should override customer's default tier
        lastLog!.MessageText.Should().Contain("Diamond Elite");
    }

    #endregion

    #region Opt-In/Opt-Out Tests

    [Fact]
    public async Task GetConsentAsync_ReturnsConsentStatus()
    {
        // Act
        var consent = await _service.GetConsentAsync(1);

        // Assert
        consent.Should().NotBeNull();
        consent!.CustomerId.Should().Be(1);
        consent.IsOptedIn.Should().BeTrue();
    }

    [Fact]
    public async Task SetOptInStatusAsync_UpdatesStatus()
    {
        // Arrange
        var consent = await _service.GetConsentAsync(1);
        consent!.IsOptedIn.Should().BeTrue();

        // Act
        var result = await _service.SetOptInStatusAsync(1, false);

        // Assert
        result.Should().BeTrue();
        var updated = await _service.GetConsentAsync(1);
        updated!.IsOptedIn.Should().BeFalse();

        // Cleanup - restore opt-in
        await _service.SetOptInStatusAsync(1, true);
    }

    [Fact]
    public async Task ProcessOptOutAsync_OptsOutCustomer()
    {
        // Arrange
        var customer = (await _service.GetAllOptedInCustomersAsync()).Customers.First();

        // Act
        var result = await _service.ProcessOptOutAsync(customer.PhoneNumber, "STOP");

        // Assert
        result.Should().BeTrue();

        var consent = await _service.GetConsentAsync(customer.CustomerId);
        consent!.IsOptedIn.Should().BeFalse();

        // Cleanup
        await _service.SetOptInStatusAsync(customer.CustomerId, true);
    }

    [Fact]
    public async Task GetOptOutLogAsync_ReturnsOptOutHistory()
    {
        // Arrange
        var customer = (await _service.GetAllOptedInCustomersAsync()).Customers.First();
        await _service.ProcessOptOutAsync(customer.PhoneNumber, "UNSUBSCRIBE");

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var logs = await _service.GetOptOutLogAsync(startDate, endDate);

        // Assert
        logs.Should().NotBeEmpty();
        logs.Should().Contain(l => l.PhoneNumber == customer.PhoneNumber);

        // Cleanup
        await _service.SetOptInStatusAsync(customer.CustomerId, true);
    }

    #endregion

    #region Report Tests

    [Fact]
    public async Task GenerateCampaignReportAsync_ReturnsDetailedReport()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Report Test Campaign",
            MessageText = "Test message for {CustomerName}",
            CreatedByUserId = 1
        });
        await _service.StartCampaignAsync(createResult.Campaign!.Id);

        // Act
        var report = await _service.GenerateCampaignReportAsync(createResult.Campaign.Id);

        // Assert
        report.Should().NotBeNull();
        report.CampaignId.Should().Be(createResult.Campaign.Id);
        report.SentCount.Should().BeGreaterThan(0);
        report.TotalCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateUsageReportAsync_ReturnsUsageSummary()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Usage Report Test",
            MessageText = "Test",
            CreatedByUserId = 1
        });
        await _service.StartCampaignAsync(createResult.Campaign!.Id);

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var report = await _service.GenerateUsageReportAsync(startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.TotalMessagesSent.Should().BeGreaterThan(0);
        report.TotalCost.Should().BeGreaterThan(0);
        report.CampaignSummaries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSentLogAsync_ReturnsSentMessages()
    {
        // Arrange
        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Log Test",
            MessageText = "Test",
            CreatedByUserId = 1
        });
        await _service.StartCampaignAsync(createResult.Campaign!.Id);

        // Act
        var logs = await _service.GetSentLogAsync(createResult.Campaign.Id);

        // Assert
        logs.Should().NotBeEmpty();
        logs.Should().OnlyContain(l => l.CampaignId == createResult.Campaign.Id);
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.BatchSize.Should().Be(100);
        settings.OptOutKeywords.Should().Contain("STOP");
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesSettings()
    {
        // Arrange
        var newSettings = new SmsMarketingSettings
        {
            BatchSize = 50,
            BatchDelayMs = 2000,
            StoreName = "Test Store",
            AutoSendBirthdayGreetings = true,
            MaxSmsPerDay = 1000
        };

        // Act
        var updated = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        updated.BatchSize.Should().Be(50);
        updated.StoreName.Should().Be("Test Store");
        updated.AutoSendBirthdayGreetings.Should().BeTrue();
        updated.MaxSmsPerDay.Should().Be(1000);
    }

    #endregion

    #region Events Tests

    [Fact]
    public async Task StartCampaignAsync_RaisesCampaignEvents()
    {
        // Arrange
        CampaignEventArgs? startedArgs = null;
        CampaignEventArgs? completedArgs = null;
        _service.CampaignStarted += (sender, args) => startedArgs = args;
        _service.CampaignCompleted += (sender, args) => completedArgs = args;

        var createResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "Event Test",
            MessageText = "Test",
            CreatedByUserId = 1
        });

        // Act
        await _service.StartCampaignAsync(createResult.Campaign!.Id);

        // Assert
        startedArgs.Should().NotBeNull();
        startedArgs!.EventType.Should().Be("Started");
        completedArgs.Should().NotBeNull();
        completedArgs!.EventType.Should().Be("Completed");
    }

    [Fact]
    public async Task ProcessOptOutAsync_RaisesOptOutEvent()
    {
        // Arrange
        OptOutEventArgs? eventArgs = null;
        _service.CustomerOptedOut += (sender, args) => eventArgs = args;

        var customer = (await _service.GetAllOptedInCustomersAsync()).Customers.First();

        // Act
        await _service.ProcessOptOutAsync(customer.PhoneNumber, "STOP");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.PhoneNumber.Should().Be(customer.PhoneNumber);
        eventArgs.Keyword.Should().Be("STOP");

        // Cleanup
        await _service.SetOptInStatusAsync(customer.CustomerId, true);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullCampaignWorkflow_EndToEnd()
    {
        // Step 1: Create a template
        var template = await _service.CreateTemplateAsync(new SmsTemplateRequest
        {
            Name = "E2E Test Template",
            Category = SmsTemplateCategory.Promotion,
            MessageText = "Hi {CustomerName}! Get 10% off at {StoreName} this weekend!"
        });
        template.Should().NotBeNull();

        // Step 2: Create a segment
        var segment = await _service.CreateSegmentAsync(new CustomerSegmentRequest
        {
            Name = "E2E Test Segment",
            FilterCriteria = new SegmentFilter
            {
                Criteria = new List<SegmentCriterion>
                {
                    new() { Type = SegmentCriterionType.LoyaltyTier, TierName = "Gold" }
                }
            },
            CreatedByUserId = 1
        });
        segment.Should().NotBeNull();
        segment.CachedCount.Should().BeGreaterThan(0);

        // Step 3: Preview the template
        var preview = await _service.PreviewTemplateAsync(template.Id);
        preview.RenderedMessage.Should().NotContain("{CustomerName}");

        // Step 4: Create a campaign
        var campaignResult = await _service.CreateCampaignAsync(new SmsCampaignRequest
        {
            Name = "E2E Test Campaign",
            TemplateId = template.Id,
            MessageText = template.MessageText,
            CustomerSegmentId = segment.Id,
            CreatedByUserId = 1
        });
        campaignResult.Success.Should().BeTrue();

        // Step 5: Start the campaign
        var startResult = await _service.StartCampaignAsync(campaignResult.Campaign!.Id);
        startResult.Success.Should().BeTrue();
        startResult.Campaign!.Status.Should().Be(CampaignStatus.Completed);

        // Step 6: Check sent logs
        var logs = await _service.GetSentLogAsync(campaignResult.Campaign.Id);
        logs.Should().NotBeEmpty();
        logs.Count.Should().Be(segment.CachedCount);

        // Step 7: Generate report
        var report = await _service.GenerateCampaignReportAsync(campaignResult.Campaign.Id);
        report.SentCount.Should().Be(logs.Count);
        report.TotalCost.Should().Be(logs.Count); // KSh 1 per SMS
    }

    #endregion
}
