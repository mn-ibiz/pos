// src/HospitalityPOS.Infrastructure/Services/SmsMarketingService.cs
// Implementation of SMS marketing service
// Story 47-1: SMS Marketing to Customers

using System.Text.RegularExpressions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Marketing;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for SMS marketing campaigns and customer segmentation.
/// </summary>
public class SmsMarketingService : ISmsMarketingService
{
    // In-memory storage for demo
    private readonly List<SmsTemplate> _templates = new();
    private readonly List<CustomerSegment> _segments = new();
    private readonly List<SmsCampaign> _campaigns = new();
    private readonly List<SmsSentLog> _sentLogs = new();
    private readonly List<SmsOptOutLog> _optOutLogs = new();
    private readonly List<TransactionalSmsConfig> _transactionalConfigs = new();
    private SmsMarketingSettings _settings = new();
    private int _nextTemplateId = 1;
    private int _nextSegmentId = 1;
    private int _nextCampaignId = 1;
    private long _nextLogId = 1;
    private int _nextOptOutLogId = 1;

    // Simulated customer data
    private readonly List<CustomerSmsInfo> _customers = new();

    public SmsMarketingService()
    {
        InitializeDefaultTemplates();
        InitializeTransactionalConfigs();
        InitializeSampleCustomers();
    }

    private void InitializeDefaultTemplates()
    {
        var defaults = new[]
        {
            new SmsTemplateRequest
            {
                Name = "Promotion - Weekend Special",
                Category = SmsTemplateCategory.Promotion,
                MessageText = "Hi {CustomerName}! This weekend only: Get 20% off all purchases at {StoreName}. Show this SMS at checkout. Valid until Sunday."
            },
            new SmsTemplateRequest
            {
                Name = "Points Reminder",
                Category = SmsTemplateCategory.Loyalty,
                MessageText = "Hi {CustomerName}, you have {PointsBalance} points! Redeem them on your next visit to {StoreName}."
            },
            new SmsTemplateRequest
            {
                Name = "Birthday Greeting",
                Category = SmsTemplateCategory.Special,
                MessageText = "Happy Birthday {CustomerName}! Enjoy 50 bonus points on your birthday visit to {StoreName}. Valid today only!"
            },
            new SmsTemplateRequest
            {
                Name = "Tier Upgrade",
                Category = SmsTemplateCategory.Loyalty,
                MessageText = "Congratulations {CustomerName}! You've been upgraded to {TierName} member! Enjoy {Multiplier}x points on every purchase."
            },
            new SmsTemplateRequest
            {
                Name = "Points Expiry Warning",
                Category = SmsTemplateCategory.Transactional,
                MessageText = "Hi {CustomerName}, {ExpiringPoints} points will expire on {ExpiryDate}. Visit {StoreName} to use them!"
            },
            new SmsTemplateRequest
            {
                Name = "Welcome Message",
                Category = SmsTemplateCategory.Transactional,
                MessageText = "Welcome to {StoreName} Rewards, {CustomerName}! You've earned {PointsBalance} bonus points. Start earning more on every purchase!"
            }
        };

        foreach (var request in defaults)
        {
            CreateTemplateAsync(request).GetAwaiter().GetResult();
        }
    }

    private void InitializeTransactionalConfigs()
    {
        _transactionalConfigs.AddRange(new[]
        {
            new TransactionalSmsConfig { Type = TransactionalSmsType.Welcome, Name = "Welcome SMS", IsEnabled = true },
            new TransactionalSmsConfig { Type = TransactionalSmsType.PointsEarned, Name = "Points Earned", IsEnabled = false },
            new TransactionalSmsConfig { Type = TransactionalSmsType.TierUpgrade, Name = "Tier Upgrade", IsEnabled = true },
            new TransactionalSmsConfig { Type = TransactionalSmsType.PointsExpiring, Name = "Points Expiring", IsEnabled = true, MinIntervalMinutes = 1440 },
            new TransactionalSmsConfig { Type = TransactionalSmsType.Birthday, Name = "Birthday Greeting", IsEnabled = true },
            new TransactionalSmsConfig { Type = TransactionalSmsType.Receipt, Name = "Receipt SMS", IsEnabled = false }
        });
    }

    private void InitializeSampleCustomers()
    {
        _customers.AddRange(new[]
        {
            new CustomerSmsInfo { CustomerId = 1, Name = "John Kamau", PhoneNumber = "254712345678", TierName = "Gold", PointsBalance = 5000, IsOptedIn = true, LastVisit = DateTime.Today.AddDays(-5), TotalSpend = 75000 },
            new CustomerSmsInfo { CustomerId = 2, Name = "Mary Wanjiku", PhoneNumber = "254723456789", TierName = "Silver", PointsBalance = 2500, IsOptedIn = true, LastVisit = DateTime.Today.AddDays(-10), TotalSpend = 35000 },
            new CustomerSmsInfo { CustomerId = 3, Name = "Peter Ochieng", PhoneNumber = "254734567890", TierName = "Bronze", PointsBalance = 800, IsOptedIn = true, LastVisit = DateTime.Today.AddDays(-30), TotalSpend = 15000 },
            new CustomerSmsInfo { CustomerId = 4, Name = "Grace Njeri", PhoneNumber = "254745678901", TierName = "Gold", PointsBalance = 7500, IsOptedIn = true, Birthday = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), LastVisit = DateTime.Today.AddDays(-2), TotalSpend = 95000 },
            new CustomerSmsInfo { CustomerId = 5, Name = "David Mwangi", PhoneNumber = "254756789012", TierName = "Silver", PointsBalance = 1200, IsOptedIn = false, LastVisit = DateTime.Today.AddDays(-60), TotalSpend = 25000 },
            new CustomerSmsInfo { CustomerId = 6, Name = "Sarah Akinyi", PhoneNumber = "254767890123", TierName = "Bronze", PointsBalance = 300, IsOptedIn = true, LastVisit = DateTime.Today.AddDays(-90), TotalSpend = 8000 },
            new CustomerSmsInfo { CustomerId = 7, Name = "James Kiprop", PhoneNumber = "254778901234", TierName = "Gold", PointsBalance = 12000, IsOptedIn = true, LastVisit = DateTime.Today.AddDays(-1), TotalSpend = 150000 },
            new CustomerSmsInfo { CustomerId = 8, Name = "Lucy Wambui", PhoneNumber = "254789012345", TierName = "Silver", PointsBalance = 3000, IsOptedIn = true, Birthday = DateOnly.FromDateTime(DateTime.Today), LastVisit = DateTime.Today.AddDays(-7), TotalSpend = 42000 }
        });

        _settings.StoreName = "QuickMart";
    }

    #region SMS Templates

    public Task<SmsTemplate> CreateTemplateAsync(SmsTemplateRequest request)
    {
        var template = new SmsTemplate
        {
            Id = _nextTemplateId++,
            Name = request.Name,
            Category = request.Category,
            MessageText = request.MessageText,
            Placeholders = ExtractPlaceholders(request.MessageText),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _templates.Add(template);
        return Task.FromResult(template);
    }

    public Task<SmsTemplate> UpdateTemplateAsync(SmsTemplateRequest request)
    {
        var template = _templates.FirstOrDefault(t => t.Id == request.Id)
            ?? throw new InvalidOperationException($"Template {request.Id} not found");

        template.Name = request.Name;
        template.Category = request.Category;
        template.MessageText = request.MessageText;
        template.Placeholders = ExtractPlaceholders(request.MessageText);

        return Task.FromResult(template);
    }

    public Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = _templates.FirstOrDefault(t => t.Id == templateId);
        if (template == null) return Task.FromResult(false);

        template.IsActive = false;
        return Task.FromResult(true);
    }

    public Task<SmsTemplate?> GetTemplateAsync(int templateId)
    {
        var template = _templates.FirstOrDefault(t => t.Id == templateId);
        return Task.FromResult(template);
    }

    public Task<IReadOnlyList<SmsTemplate>> GetTemplatesAsync(SmsTemplateCategory? category = null)
    {
        var query = _templates.Where(t => t.IsActive);
        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        return Task.FromResult<IReadOnlyList<SmsTemplate>>(query.ToList());
    }

    public async Task<SmsTemplatePreview> PreviewTemplateAsync(int templateId, Dictionary<string, string>? sampleData = null)
    {
        var template = await GetTemplateAsync(templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        var data = sampleData ?? GetDefaultSampleData();
        var rendered = RenderMessageWithData(template.MessageText, data);

        return new SmsTemplatePreview
        {
            OriginalMessage = template.MessageText,
            RenderedMessage = rendered,
            CharacterCount = rendered.Length,
            SmsSegments = (int)Math.Ceiling(rendered.Length / 160.0),
            EstimatedCost = (decimal)Math.Ceiling(rendered.Length / 160.0) * 1m // KSh 1 per segment
        };
    }

    public string RenderMessage(string messageText, CustomerSmsInfo customer)
    {
        var data = new Dictionary<string, string>
        {
            { "CustomerName", customer.Name },
            { "PointsBalance", customer.PointsBalance?.ToString("N0") ?? "0" },
            { "TierName", customer.TierName ?? "Member" },
            { "StoreName", _settings.StoreName },
            { "PhoneNumber", customer.PhoneNumber }
        };

        return RenderMessageWithData(messageText, data);
    }

    private static List<string> ExtractPlaceholders(string message)
    {
        var matches = Regex.Matches(message, @"\{(\w+)\}");
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    private Dictionary<string, string> GetDefaultSampleData()
    {
        return new Dictionary<string, string>
        {
            { "CustomerName", "John Kamau" },
            { "PointsBalance", "5,000" },
            { "TierName", "Gold" },
            { "StoreName", _settings.StoreName },
            { "Multiplier", "2" },
            { "ExpiringPoints", "500" },
            { "ExpiryDate", "January 31, 2026" }
        };
    }

    private static string RenderMessageWithData(string message, Dictionary<string, string> data)
    {
        foreach (var (key, value) in data)
        {
            message = message.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }
        return message;
    }

    #endregion

    #region Customer Segmentation

    public Task<CustomerSegment> CreateSegmentAsync(CustomerSegmentRequest request)
    {
        var segment = new CustomerSegment
        {
            Id = _nextSegmentId++,
            Name = request.Name,
            Description = request.Description,
            FilterCriteria = request.FilterCriteria,
            CreatedByUserId = request.CreatedByUserId,
            CreatedByName = $"User {request.CreatedByUserId}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Calculate initial count
        var result = EvaluateSegmentAsync(request.FilterCriteria).GetAwaiter().GetResult();
        segment.CachedCount = result.MatchingCount;
        segment.LastCalculatedAt = DateTime.UtcNow;

        _segments.Add(segment);
        return Task.FromResult(segment);
    }

    public Task<CustomerSegment> UpdateSegmentAsync(CustomerSegmentRequest request)
    {
        var segment = _segments.FirstOrDefault(s => s.Id == request.Id)
            ?? throw new InvalidOperationException($"Segment {request.Id} not found");

        segment.Name = request.Name;
        segment.Description = request.Description;
        segment.FilterCriteria = request.FilterCriteria;

        // Recalculate count
        var result = EvaluateSegmentAsync(request.FilterCriteria).GetAwaiter().GetResult();
        segment.CachedCount = result.MatchingCount;
        segment.LastCalculatedAt = DateTime.UtcNow;

        return Task.FromResult(segment);
    }

    public Task<bool> DeleteSegmentAsync(int segmentId)
    {
        var segment = _segments.FirstOrDefault(s => s.Id == segmentId);
        if (segment == null) return Task.FromResult(false);

        segment.IsActive = false;
        return Task.FromResult(true);
    }

    public Task<CustomerSegment?> GetSegmentAsync(int segmentId)
    {
        var segment = _segments.FirstOrDefault(s => s.Id == segmentId);
        return Task.FromResult(segment);
    }

    public Task<IReadOnlyList<CustomerSegment>> GetSegmentsAsync()
    {
        return Task.FromResult<IReadOnlyList<CustomerSegment>>(_segments.Where(s => s.IsActive).ToList());
    }

    public Task<SegmentResult> EvaluateSegmentAsync(SegmentFilter filter)
    {
        var query = _customers.AsEnumerable();

        if (filter.IncludeOnlyOptedIn)
            query = query.Where(c => c.IsOptedIn);

        if (filter.Criteria.Count == 0)
        {
            var customers = query.ToList();
            return Task.FromResult(new SegmentResult
            {
                MatchingCount = customers.Count,
                TotalOptedIn = _customers.Count(c => c.IsOptedIn),
                Customers = customers
            });
        }

        // Apply criteria based on logic
        if (filter.Logic == SegmentLogic.And)
        {
            foreach (var criterion in filter.Criteria)
            {
                query = ApplyCriterion(query, criterion);
            }
        }
        else // OR logic
        {
            var matchingIds = new HashSet<int>();
            foreach (var criterion in filter.Criteria)
            {
                var matches = ApplyCriterion(_customers.Where(c => c.IsOptedIn), criterion);
                foreach (var customer in matches)
                {
                    matchingIds.Add(customer.CustomerId);
                }
            }
            query = query.Where(c => matchingIds.Contains(c.CustomerId));
        }

        var result = query.ToList();
        return Task.FromResult(new SegmentResult
        {
            MatchingCount = result.Count,
            TotalOptedIn = _customers.Count(c => c.IsOptedIn),
            Customers = result
        });
    }

    private static IEnumerable<CustomerSmsInfo> ApplyCriterion(IEnumerable<CustomerSmsInfo> query, SegmentCriterion criterion)
    {
        return criterion.Type switch
        {
            SegmentCriterionType.LoyaltyTier when criterion.TierName != null =>
                query.Where(c => c.TierName == criterion.TierName),

            SegmentCriterionType.LastVisitDays when criterion.DaysBack.HasValue =>
                query.Where(c => c.LastVisit.HasValue &&
                    (DateTime.Today - c.LastVisit.Value).TotalDays <= criterion.DaysBack.Value),

            SegmentCriterionType.LapsedDays when criterion.DaysBack.HasValue =>
                query.Where(c => !c.LastVisit.HasValue ||
                    (DateTime.Today - c.LastVisit.Value).TotalDays > criterion.DaysBack.Value),

            SegmentCriterionType.SpentMinimum when criterion.MinimumSpend.HasValue && criterion.DaysBack.HasValue =>
                query.Where(c => c.TotalSpend >= criterion.MinimumSpend.Value),

            SegmentCriterionType.BirthdayThisMonth =>
                query.Where(c => c.Birthday.HasValue && c.Birthday.Value.Month == DateTime.Today.Month),

            SegmentCriterionType.PurchasedWithinDays when criterion.DaysBack.HasValue =>
                query.Where(c => c.LastVisit.HasValue &&
                    (DateTime.Today - c.LastVisit.Value).TotalDays <= criterion.DaysBack.Value),

            _ => query
        };
    }

    public Task<SegmentResult> GetCustomersWhoPurchasedWithinDaysAsync(int days)
    {
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.PurchasedWithinDays, DaysBack = days }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetCustomersWhoPurchasedCategoryAsync(int categoryId, int days)
    {
        // Simulated - in real implementation would query sales data
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.PurchasedCategory, CategoryId = categoryId, DaysBack = days }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetCustomersWhoPurchasedProductAsync(int productId, int days)
    {
        // Simulated - in real implementation would query sales data
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.PurchasedProduct, ProductId = productId, DaysBack = days }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetCustomersBySpendAsync(decimal minimumSpend, int days)
    {
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.SpentMinimum, MinimumSpend = minimumSpend, DaysBack = days }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetCustomersByTierAsync(string tierName)
    {
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.LoyaltyTier, TierName = tierName }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetLapsedCustomersAsync(int days)
    {
        var filter = new SegmentFilter
        {
            Criteria = new List<SegmentCriterion>
            {
                new() { Type = SegmentCriterionType.LapsedDays, DaysBack = days }
            }
        };
        return EvaluateSegmentAsync(filter);
    }

    public Task<SegmentResult> GetAllOptedInCustomersAsync()
    {
        return EvaluateSegmentAsync(new SegmentFilter());
    }

    public async Task<int> GetSegmentCountAsync(SegmentFilter filter)
    {
        var result = await EvaluateSegmentAsync(filter);
        return result.MatchingCount;
    }

    #endregion

    #region SMS Campaigns

    public async Task<CampaignResult> CreateCampaignAsync(SmsCampaignRequest request)
    {
        // Get target customers
        SegmentResult segmentResult;
        if (request.CustomerSegmentId.HasValue)
        {
            var segment = await GetSegmentAsync(request.CustomerSegmentId.Value);
            if (segment == null)
                return CampaignResult.Failed($"Segment {request.CustomerSegmentId} not found");
            segmentResult = await EvaluateSegmentAsync(segment.FilterCriteria);
        }
        else if (request.InlineFilter != null)
        {
            segmentResult = await EvaluateSegmentAsync(request.InlineFilter);
        }
        else
        {
            segmentResult = await GetAllOptedInCustomersAsync();
        }

        var campaign = new SmsCampaign
        {
            Id = _nextCampaignId++,
            Name = request.Name,
            TemplateId = request.TemplateId,
            MessageText = request.MessageText,
            CustomerSegmentId = request.CustomerSegmentId,
            TargetCount = segmentResult.MatchingCount,
            TargetSegment = request.CustomerSegmentId.HasValue
                ? _segments.FirstOrDefault(s => s.Id == request.CustomerSegmentId)?.Name ?? "Custom"
                : "All Customers",
            Status = request.ScheduledAt.HasValue ? CampaignStatus.Scheduled : CampaignStatus.Draft,
            ScheduledAt = request.ScheduledAt,
            CreatedByUserId = request.CreatedByUserId,
            CreatedByName = $"User {request.CreatedByUserId}",
            CreatedAt = DateTime.UtcNow
        };

        _campaigns.Add(campaign);
        return CampaignResult.Succeeded(campaign);
    }

    public async Task<CampaignResult> UpdateCampaignAsync(SmsCampaignRequest request)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == request.Id);
        if (campaign == null)
            return CampaignResult.Failed($"Campaign {request.Id} not found");

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
            return CampaignResult.Failed("Can only update draft or scheduled campaigns");

        // Recalculate target count
        SegmentResult segmentResult;
        if (request.CustomerSegmentId.HasValue)
        {
            var segment = await GetSegmentAsync(request.CustomerSegmentId.Value);
            segmentResult = segment != null
                ? await EvaluateSegmentAsync(segment.FilterCriteria)
                : await GetAllOptedInCustomersAsync();
        }
        else if (request.InlineFilter != null)
        {
            segmentResult = await EvaluateSegmentAsync(request.InlineFilter);
        }
        else
        {
            segmentResult = await GetAllOptedInCustomersAsync();
        }

        campaign.Name = request.Name;
        campaign.TemplateId = request.TemplateId;
        campaign.MessageText = request.MessageText;
        campaign.CustomerSegmentId = request.CustomerSegmentId;
        campaign.TargetCount = segmentResult.MatchingCount;

        return CampaignResult.Succeeded(campaign, "Campaign updated");
    }

    public Task<CampaignResult> ScheduleCampaignAsync(int campaignId, DateTime scheduledAt)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null)
            return Task.FromResult(CampaignResult.Failed($"Campaign {campaignId} not found"));

        if (campaign.Status != CampaignStatus.Draft)
            return Task.FromResult(CampaignResult.Failed("Can only schedule draft campaigns"));

        if (scheduledAt <= DateTime.UtcNow)
            return Task.FromResult(CampaignResult.Failed("Scheduled time must be in the future"));

        campaign.ScheduledAt = scheduledAt;
        campaign.Status = CampaignStatus.Scheduled;

        return Task.FromResult(CampaignResult.Succeeded(campaign, "Campaign scheduled"));
    }

    public async Task<CampaignResult> StartCampaignAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null)
            return CampaignResult.Failed($"Campaign {campaignId} not found");

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
            return CampaignResult.Failed("Can only start draft or scheduled campaigns");

        campaign.Status = CampaignStatus.Sending;
        campaign.StartedAt = DateTime.UtcNow;

        OnCampaignStarted(new CampaignEventArgs(campaign, "Started"));

        // Send messages
        var sentCount = await SendCampaignMessagesAsync(campaignId);

        campaign.Status = CampaignStatus.Completed;
        campaign.CompletedAt = DateTime.UtcNow;

        OnCampaignCompleted(new CampaignEventArgs(campaign, "Completed"));

        return CampaignResult.Succeeded(campaign, $"Campaign completed. Sent {sentCount} messages.");
    }

    public Task<CampaignResult> PauseCampaignAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null)
            return Task.FromResult(CampaignResult.Failed($"Campaign {campaignId} not found"));

        if (campaign.Status != CampaignStatus.Sending)
            return Task.FromResult(CampaignResult.Failed("Can only pause sending campaigns"));

        campaign.Status = CampaignStatus.Paused;
        return Task.FromResult(CampaignResult.Succeeded(campaign, "Campaign paused"));
    }

    public async Task<CampaignResult> ResumeCampaignAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null)
            return CampaignResult.Failed($"Campaign {campaignId} not found");

        if (campaign.Status != CampaignStatus.Paused)
            return CampaignResult.Failed("Can only resume paused campaigns");

        campaign.Status = CampaignStatus.Sending;
        await SendCampaignMessagesAsync(campaignId);

        campaign.Status = CampaignStatus.Completed;
        campaign.CompletedAt = DateTime.UtcNow;

        return CampaignResult.Succeeded(campaign, "Campaign resumed and completed");
    }

    public Task<CampaignResult> CancelCampaignAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null)
            return Task.FromResult(CampaignResult.Failed($"Campaign {campaignId} not found"));

        if (campaign.Status == CampaignStatus.Completed || campaign.Status == CampaignStatus.Cancelled)
            return Task.FromResult(CampaignResult.Failed("Cannot cancel completed or already cancelled campaigns"));

        campaign.Status = CampaignStatus.Cancelled;
        return Task.FromResult(CampaignResult.Succeeded(campaign, "Campaign cancelled"));
    }

    public Task<SmsCampaign?> GetCampaignAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        return Task.FromResult(campaign);
    }

    public Task<IReadOnlyList<SmsCampaign>> GetCampaignsAsync(CampaignStatus? status = null)
    {
        var query = _campaigns.AsEnumerable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return Task.FromResult<IReadOnlyList<SmsCampaign>>(query.OrderByDescending(c => c.CreatedAt).ToList());
    }

    public Task<IReadOnlyList<SmsCampaign>> GetDueCampaignsAsync()
    {
        var due = _campaigns
            .Where(c => c.Status == CampaignStatus.Scheduled && c.ScheduledAt <= DateTime.UtcNow)
            .ToList();
        return Task.FromResult<IReadOnlyList<SmsCampaign>>(due);
    }

    public Task<BatchSendProgress?> GetCampaignProgressAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null) return Task.FromResult<BatchSendProgress?>(null);

        return Task.FromResult<BatchSendProgress?>(new BatchSendProgress
        {
            CampaignId = campaignId,
            TotalRecipients = campaign.TargetCount,
            SentCount = campaign.SentCount,
            FailedCount = campaign.FailedCount,
            StartedAt = campaign.StartedAt ?? DateTime.UtcNow,
            CompletedAt = campaign.CompletedAt
        });
    }

    #endregion

    #region SMS Sending

    public async Task<int> SendCampaignMessagesAsync(int campaignId)
    {
        var campaign = _campaigns.FirstOrDefault(c => c.Id == campaignId);
        if (campaign == null) return 0;

        // Get target customers
        SegmentResult segmentResult;
        if (campaign.CustomerSegmentId.HasValue)
        {
            var segment = await GetSegmentAsync(campaign.CustomerSegmentId.Value);
            segmentResult = segment != null
                ? await EvaluateSegmentAsync(segment.FilterCriteria)
                : await GetAllOptedInCustomersAsync();
        }
        else
        {
            segmentResult = await GetAllOptedInCustomersAsync();
        }

        var sentCount = 0;
        foreach (var customer in segmentResult.Customers)
        {
            var renderedMessage = RenderMessage(campaign.MessageText, customer);

            // Simulated send
            var log = new SmsSentLog
            {
                Id = _nextLogId++,
                CampaignId = campaignId,
                CampaignName = campaign.Name,
                CustomerId = customer.CustomerId,
                CustomerName = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                MessageText = renderedMessage,
                Status = SmsStatus.Sent,
                GatewayMessageId = Guid.NewGuid().ToString("N")[..20],
                Cost = 1m, // KSh 1 per SMS
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _sentLogs.Add(log);
            sentCount++;

            campaign.SentCount++;
            campaign.TotalCost += 1m;
        }

        return sentCount;
    }

    public Task<IReadOnlyList<SmsSentLog>> GetSentLogAsync(
        int? campaignId = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _sentLogs.AsEnumerable();

        if (campaignId.HasValue)
            query = query.Where(l => l.CampaignId == campaignId.Value);

        if (startDate.HasValue)
            query = query.Where(l => DateOnly.FromDateTime(l.CreatedAt) >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => DateOnly.FromDateTime(l.CreatedAt) <= endDate.Value);

        return Task.FromResult<IReadOnlyList<SmsSentLog>>(query.OrderByDescending(l => l.CreatedAt).ToList());
    }

    #endregion

    #region Transactional SMS

    public Task<IReadOnlyList<TransactionalSmsConfig>> GetTransactionalConfigsAsync()
    {
        return Task.FromResult<IReadOnlyList<TransactionalSmsConfig>>(_transactionalConfigs.ToList());
    }

    public Task<TransactionalSmsConfig> UpdateTransactionalConfigAsync(TransactionalSmsConfig config)
    {
        var existing = _transactionalConfigs.FirstOrDefault(c => c.Type == config.Type);
        if (existing != null)
        {
            existing.IsEnabled = config.IsEnabled;
            existing.TemplateId = config.TemplateId;
            existing.DefaultMessage = config.DefaultMessage;
            existing.MinIntervalMinutes = config.MinIntervalMinutes;
            return Task.FromResult(existing);
        }

        _transactionalConfigs.Add(config);
        return Task.FromResult(config);
    }

    public async Task<SmsResult> SendTransactionalSmsAsync(TransactionalSmsRequest request)
    {
        var config = _transactionalConfigs.FirstOrDefault(c => c.Type == request.Type);
        if (config == null || !config.IsEnabled)
            return SmsResult.Failed($"Transactional SMS type {request.Type} is not enabled");

        var customer = _customers.FirstOrDefault(c => c.CustomerId == request.CustomerId);
        if (customer == null)
            return SmsResult.Failed($"Customer {request.CustomerId} not found");

        if (!customer.IsOptedIn)
            return SmsResult.Failed("Customer has opted out of SMS");

        // Get template or default message
        string message;
        if (config.TemplateId.HasValue)
        {
            var template = await GetTemplateAsync(config.TemplateId.Value);
            message = template?.MessageText ?? config.DefaultMessage ?? "";
        }
        else
        {
            message = config.DefaultMessage ?? GetDefaultTransactionalMessage(request.Type);
        }

        // Merge data
        foreach (var (key, value) in request.Data)
        {
            message = message.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }
        message = RenderMessage(message, customer);

        // Create log entry
        var log = new SmsSentLog
        {
            Id = _nextLogId++,
            CustomerId = customer.CustomerId,
            CustomerName = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            MessageText = message,
            Status = SmsStatus.Sent,
            GatewayMessageId = Guid.NewGuid().ToString("N")[..20],
            Cost = 1m,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _sentLogs.Add(log);

        return SmsResult.Succeeded(log.GatewayMessageId!, 1m);
    }

    private static string GetDefaultTransactionalMessage(TransactionalSmsType type)
    {
        return type switch
        {
            TransactionalSmsType.Welcome => "Welcome to {StoreName} Rewards, {CustomerName}!",
            TransactionalSmsType.PointsEarned => "You earned {PointsEarned} points! New balance: {PointsBalance}",
            TransactionalSmsType.TierUpgrade => "Congratulations {CustomerName}! You've been upgraded to {TierName}!",
            TransactionalSmsType.PointsExpiring => "Hi {CustomerName}, {ExpiringPoints} points will expire soon.",
            TransactionalSmsType.Birthday => "Happy Birthday {CustomerName}!",
            TransactionalSmsType.Receipt => "Thank you for shopping at {StoreName}!",
            _ => "Thank you for being a valued customer!"
        };
    }

    #endregion

    #region Opt-In/Opt-Out

    public Task<SmsConsent?> GetConsentAsync(int customerId)
    {
        var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
        if (customer == null) return Task.FromResult<SmsConsent?>(null);

        return Task.FromResult<SmsConsent?>(new SmsConsent
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            IsOptedIn = customer.IsOptedIn
        });
    }

    public Task<bool> SetOptInStatusAsync(int customerId, bool optIn)
    {
        var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
        if (customer == null) return Task.FromResult(false);

        customer.IsOptedIn = optIn;
        return Task.FromResult(true);
    }

    public Task<bool> ProcessOptOutAsync(string phoneNumber, string keyword)
    {
        var customer = _customers.FirstOrDefault(c => c.PhoneNumber == phoneNumber);
        if (customer == null) return Task.FromResult(false);

        customer.IsOptedIn = false;

        var log = new SmsOptOutLog
        {
            Id = _nextOptOutLogId++,
            CustomerId = customer.CustomerId,
            CustomerName = customer.Name,
            PhoneNumber = phoneNumber,
            OptOutKeyword = keyword,
            OptOutDate = DateTime.UtcNow,
            ConfirmationSent = true
        };

        _optOutLogs.Add(log);
        OnCustomerOptedOut(new OptOutEventArgs(customer.CustomerId, phoneNumber, keyword));

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<SmsOptOutLog>> GetOptOutLogAsync(DateOnly startDate, DateOnly endDate)
    {
        var logs = _optOutLogs
            .Where(l => DateOnly.FromDateTime(l.OptOutDate) >= startDate &&
                        DateOnly.FromDateTime(l.OptOutDate) <= endDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<SmsOptOutLog>>(logs);
    }

    #endregion

    #region Reports

    public async Task<CampaignReport> GenerateCampaignReportAsync(int campaignId)
    {
        var campaign = await GetCampaignAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var logs = await GetSentLogAsync(campaignId);
        var failedLogs = logs.Where(l => l.Status == SmsStatus.Failed).ToList();

        return new CampaignReport
        {
            CampaignId = campaignId,
            CampaignName = campaign.Name,
            Status = campaign.Status,
            SentAt = campaign.StartedAt,
            TotalRecipients = campaign.TargetCount,
            SentCount = logs.Count(l => l.Status == SmsStatus.Sent || l.Status == SmsStatus.Delivered),
            DeliveredCount = logs.Count(l => l.Status == SmsStatus.Delivered),
            FailedCount = logs.Count(l => l.Status == SmsStatus.Failed),
            PendingCount = logs.Count(l => l.Status == SmsStatus.Pending),
            DeliveryRate = campaign.DeliveryRate,
            FailureRate = campaign.FailureRate,
            TotalCost = campaign.TotalCost,
            FailedMessages = failedLogs
        };
    }

    public async Task<SmsUsageReport> GenerateUsageReportAsync(DateOnly startDate, DateOnly endDate)
    {
        var logs = await GetSentLogAsync(startDate: startDate, endDate: endDate);
        var campaigns = _campaigns.Where(c =>
            c.StartedAt.HasValue &&
            DateOnly.FromDateTime(c.StartedAt.Value) >= startDate &&
            DateOnly.FromDateTime(c.StartedAt.Value) <= endDate).ToList();

        var optOuts = await GetOptOutLogAsync(startDate, endDate);

        // Daily breakdown
        var dailyBreakdown = logs
            .GroupBy(l => DateOnly.FromDateTime(l.CreatedAt))
            .Select(g => new DailySmsSummary
            {
                Date = g.Key,
                MessagesSent = g.Count(),
                Delivered = g.Count(l => l.Status == SmsStatus.Delivered),
                Failed = g.Count(l => l.Status == SmsStatus.Failed),
                Cost = g.Sum(l => l.Cost ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SmsUsageReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedDate = DateOnly.FromDateTime(DateTime.Today),
            TotalCampaigns = campaigns.Count,
            TotalMessagesSent = logs.Count,
            TotalDelivered = logs.Count(l => l.Status == SmsStatus.Delivered),
            TotalFailed = logs.Count(l => l.Status == SmsStatus.Failed),
            TotalCost = logs.Sum(l => l.Cost ?? 0),
            CampaignSummaries = campaigns.Select(c => new CampaignSummary
            {
                CampaignId = c.Id,
                CampaignName = c.Name,
                SentAt = c.StartedAt,
                Recipients = c.TargetCount,
                DeliveryRate = c.DeliveryRate,
                Cost = c.TotalCost
            }).ToList(),
            DailyBreakdown = dailyBreakdown,
            OptOutsThisPeriod = optOuts.Count
        };
    }

    #endregion

    #region Settings

    public Task<SmsMarketingSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<SmsMarketingSettings> UpdateSettingsAsync(SmsMarketingSettings settings)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion

    #region Events

    public event EventHandler<CampaignEventArgs>? CampaignStarted;
    public event EventHandler<CampaignEventArgs>? CampaignCompleted;
    public event EventHandler<OptOutEventArgs>? CustomerOptedOut;

    protected virtual void OnCampaignStarted(CampaignEventArgs e) => CampaignStarted?.Invoke(this, e);
    protected virtual void OnCampaignCompleted(CampaignEventArgs e) => CampaignCompleted?.Invoke(this, e);
    protected virtual void OnCustomerOptedOut(OptOutEventArgs e) => CustomerOptedOut?.Invoke(this, e);

    #endregion
}
