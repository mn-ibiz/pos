# feat: Implement Email Notification Digest for POs

**Labels:** `enhancement` `backend` `notifications` `email` `priority-medium`

## Overview

Implement an email digest system that sends daily/weekly summaries of pending POs, low stock alerts, and PO activity to managers. This ensures managers stay informed even when not actively using the application.

## Background

The system already has email capability (see `EmailPurchaseOrderAsync` in PurchaseOrdersViewModel). This feature extends it to:
1. Send scheduled digest emails
2. Summarize pending PO actions
3. Alert on critical inventory situations
4. Provide activity reports

## Requirements

### Email Digest Types

| Digest Type | Frequency | Content |
|------------|-----------|---------|
| Daily Pending POs | Daily at configured time | List of draft POs awaiting review |
| Low Stock Alert | Real-time or daily | Products below critical threshold |
| Weekly Summary | Weekly (configurable day) | Approvals, rejections, totals, trends |
| Overdue POs | Daily | POs past expected delivery date |

### Digest Email Templates

#### Daily Pending POs Email

```html
Subject: [QuickTech POS] 12 Purchase Orders Pending Review

--------------------------------------------------
DAILY PO REVIEW DIGEST
January 23, 2025
--------------------------------------------------

You have 12 purchase orders awaiting review.

CRITICAL PRIORITY (3)
---------------------
* PO-20250123-001 | ABC Distributors | $2,450.00
  - 3 items at zero stock
  - Created 2 hours ago

* PO-20250123-002 | XYZ Supplies | $1,890.00
  - 5 items below critical level
  - Created 4 hours ago

* PO-20250122-015 | ABC Distributors | $980.00
  - 2 items at zero stock
  - Created 1 day ago (OVERDUE FOR REVIEW)

HIGH PRIORITY (5)
-----------------
[List continues...]

SUMMARY
-------
Total pending value: $15,420.00
Oldest pending PO: 3 days ago

[Review POs Now] -> Link to app/web portal

--------------------------------------------------
This is an automated message from QuickTech POS.
To change notification settings, visit Settings > Notifications.
```

#### Weekly Summary Email

```html
Subject: [QuickTech POS] Weekly Purchasing Summary - Week of Jan 17

--------------------------------------------------
WEEKLY PURCHASING SUMMARY
Week of January 17-23, 2025
--------------------------------------------------

ACTIVITY OVERVIEW
-----------------
POs Created:      23
POs Approved:     18
POs Rejected:      2
POs Pending:       3

Total Ordered:    $45,670.00
Total Received:   $38,450.00

TOP SUPPLIERS THIS WEEK
-----------------------
1. ABC Distributors    - $18,500 (8 POs)
2. XYZ Supplies        - $12,300 (5 POs)
3. Quality Parts Inc   - $8,200 (3 POs)

INVENTORY ALERTS
----------------
* 5 products reached zero stock this week
* 12 products are currently below reorder point
* Average days of stock remaining: 14 days

TRENDS
------
* PO volume up 15% vs last week
* Average PO value: $1,985 (down 8%)

[View Full Report] -> Link to reporting section

--------------------------------------------------
```

#### Low Stock Alert Email

```html
Subject: [ALERT] 5 Products at Critical Stock Level

--------------------------------------------------
LOW STOCK ALERT
January 23, 2025 2:45 PM
--------------------------------------------------

The following products have reached critical stock levels:

ZERO STOCK (Immediate Action Required)
--------------------------------------
* Widget A (SKU: WGT-001)
  Current Stock: 0 | Daily Sales: 5 | Supplier: ABC Distributors
  [Create PO]

* Gadget B (SKU: GDG-002)
  Current Stock: 0 | Daily Sales: 3 | Supplier: XYZ Supplies
  [Create PO]

CRITICAL LOW (< 2 Days Stock)
-----------------------------
* Component C (SKU: CMP-003)
  Current Stock: 8 | Daily Sales: 5 | Days Left: 1.6
  Supplier: ABC Distributors
  [Create PO]

RECOMMENDED ACTION
------------------
Create purchase orders immediately to avoid stockouts.

[Review Low Stock Items] -> Link to inventory view

--------------------------------------------------
```

### Service Interface

```csharp
public interface IEmailDigestService
{
    /// <summary>
    /// Send daily pending POs digest to configured recipients
    /// </summary>
    Task SendDailyPendingPOsDigestAsync();

    /// <summary>
    /// Send weekly summary to configured recipients
    /// </summary>
    Task SendWeeklySummaryAsync();

    /// <summary>
    /// Send low stock alert (can be triggered real-time or batched)
    /// </summary>
    Task SendLowStockAlertAsync(List<Product> criticalProducts);

    /// <summary>
    /// Send overdue POs reminder
    /// </summary>
    Task SendOverduePOsReminderAsync();

    /// <summary>
    /// Get digest preview without sending (for testing/preview)
    /// </summary>
    Task<string> PreviewDigestAsync(DigestType type);

    /// <summary>
    /// Configure digest settings
    /// </summary>
    Task SaveDigestSettingsAsync(DigestSettings settings);
    Task<DigestSettings> GetDigestSettingsAsync();
}

public enum DigestType
{
    DailyPendingPOs,
    WeeklySummary,
    LowStockAlert,
    OverduePOs
}

public class DigestSettings
{
    public bool EnableDailyDigest { get; set; } = true;
    public TimeSpan DailyDigestTime { get; set; } = new TimeSpan(8, 0, 0);
    public bool EnableWeeklySummary { get; set; } = true;
    public DayOfWeek WeeklySummaryDay { get; set; } = DayOfWeek.Monday;
    public bool EnableLowStockAlerts { get; set; } = true;
    public bool BatchLowStockAlerts { get; set; } = true; // vs real-time
    public List<string> RecipientEmails { get; set; } = new();
    public List<int> RecipientUserIds { get; set; } = new();
}
```

### Background Job for Scheduled Digests

```csharp
public class EmailDigestBackgroundJob : BackgroundService
{
    private Timer? _dailyTimer;
    private Timer? _weeklyTimer;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ScheduleDailyDigest();
        ScheduleWeeklyDigest();
        return Task.CompletedTask;
    }

    private void ScheduleDailyDigest()
    {
        var settings = _settingsService.GetDigestSettingsAsync().Result;
        if (!settings.EnableDailyDigest) return;

        var now = DateTime.Now;
        var scheduledTime = now.Date.Add(settings.DailyDigestTime);
        if (scheduledTime <= now)
            scheduledTime = scheduledTime.AddDays(1);

        var delay = scheduledTime - now;

        _dailyTimer = new Timer(
            async _ => await SendDailyDigestAsync(),
            null,
            delay,
            TimeSpan.FromDays(1));
    }

    private async Task SendDailyDigestAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var digestService = scope.ServiceProvider
            .GetRequiredService<IEmailDigestService>();

        await digestService.SendDailyPendingPOsDigestAsync();
        await digestService.SendOverduePOsReminderAsync();
    }
}
```

### Email Template System

Use a templating engine like RazorLight or Scriban:

```csharp
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ITemplateEngine _templateEngine;

    public async Task<string> RenderTemplateAsync(
        string templateName,
        object model)
    {
        var templatePath = $"Templates/Email/{templateName}.cshtml";
        return await _templateEngine.RenderAsync(templatePath, model);
    }
}

// Template model
public class DailyDigestModel
{
    public DateTime Date { get; set; }
    public int TotalPendingCount { get; set; }
    public decimal TotalPendingValue { get; set; }
    public List<PendingPOSummary> CriticalPOs { get; set; }
    public List<PendingPOSummary> HighPriorityPOs { get; set; }
    public List<PendingPOSummary> OtherPOs { get; set; }
    public int OldestPendingDays { get; set; }
    public string ReviewUrl { get; set; }
}
```

### Settings UI

Add to Settings view:

```
+----------------------------------------------------------+
| EMAIL DIGEST SETTINGS                                     |
+----------------------------------------------------------+
|                                                          |
| [x] Enable daily PO digest                               |
|     Send time: [08:00 AM]                                |
|                                                          |
| [x] Enable weekly summary                                |
|     Send on: [Monday v] at [08:00 AM]                    |
|                                                          |
| [x] Enable low stock alerts                              |
|     ( ) Send immediately                                 |
|     (x) Batch with daily digest                          |
|                                                          |
| Recipients:                                              |
| +------------------------------------------------------+ |
| | manager@company.com                            [x]   | |
| | admin@company.com                              [x]   | |
| | purchasing@company.com                         [ ]   | |
| +------------------------------------------------------+ |
| [Add Email...]                                           |
|                                                          |
| [Send Test Email]                   [Save Settings]      |
+----------------------------------------------------------+
```

## Acceptance Criteria

### Daily Digest
- [ ] Sends at configured time each day
- [ ] Includes all draft POs awaiting review
- [ ] Groups POs by priority (Critical, High, Medium, Low)
- [ ] Highlights overdue-for-review POs (pending > 24 hours)
- [ ] Includes summary statistics
- [ ] Link to review dashboard (if web portal exists)
- [ ] Skips sending if no pending POs

### Weekly Summary
- [ ] Sends on configured day of week
- [ ] Includes PO activity counts (created, approved, rejected)
- [ ] Includes total order value
- [ ] Lists top suppliers by order volume
- [ ] Includes inventory health summary
- [ ] Shows week-over-week trends

### Low Stock Alerts
- [ ] Triggered when products hit critical level
- [ ] Can be real-time or batched with daily digest
- [ ] Lists all zero-stock products
- [ ] Lists products with < 2 days stock remaining
- [ ] Includes recommended action

### Overdue POs
- [ ] Sends daily if any POs past expected delivery date
- [ ] Groups by days overdue
- [ ] Includes supplier contact info
- [ ] Suggests follow-up actions

### Configuration
- [ ] Enable/disable each digest type
- [ ] Configure send times
- [ ] Configure recipient list (emails + system users)
- [ ] Test email functionality
- [ ] Preview digest before sending

### Email Quality
- [ ] HTML email with responsive design
- [ ] Plain text fallback
- [ ] Company branding (logo, colors)
- [ ] Unsubscribe link (per digest type)
- [ ] "View in browser" option

### Logging
- [ ] Log when digests are sent
- [ ] Log recipient list
- [ ] Log any send failures
- [ ] Track email open rates (if tracking enabled)

## Technical Notes

### Email Template Structure

Create templates in `Resources/EmailTemplates/`:
```
EmailTemplates/
  DailyDigest.html
  DailyDigest.txt
  WeeklySummary.html
  WeeklySummary.txt
  LowStockAlert.html
  LowStockAlert.txt
  OverduePOs.html
  OverduePOs.txt
  _Layout.html  // Base template with header/footer
```

### Sample HTML Template

```html
<!-- DailyDigest.html -->
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Daily PO Digest</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; }
        .section { margin: 20px 0; padding: 15px; background: #f8fafc; border-radius: 8px; }
        .priority-critical { border-left: 4px solid #dc2626; }
        .priority-high { border-left: 4px solid #f59e0b; }
        .po-item { padding: 10px 0; border-bottom: 1px solid #e2e8f0; }
        .btn { display: inline-block; background: #2563eb; color: white;
               padding: 12px 24px; text-decoration: none; border-radius: 6px; }
        .summary { display: flex; justify-content: space-between; }
        .stat { text-align: center; }
        .stat-value { font-size: 24px; font-weight: bold; color: #2563eb; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Daily PO Review Digest</h1>
            <p>{{Date | date: "MMMM d, yyyy"}}</p>
        </div>

        <div class="summary">
            <div class="stat">
                <div class="stat-value">{{TotalPendingCount}}</div>
                <div>Pending POs</div>
            </div>
            <div class="stat">
                <div class="stat-value">{{TotalPendingValue | currency}}</div>
                <div>Total Value</div>
            </div>
        </div>

        {{#if CriticalPOs}}
        <div class="section priority-critical">
            <h2>Critical Priority ({{CriticalPOs.Count}})</h2>
            {{#each CriticalPOs}}
            <div class="po-item">
                <strong>{{PONumber}}</strong> | {{SupplierName}} | {{TotalAmount | currency}}
                <br><small>{{ItemCount}} items - Created {{TimeAgo}}</small>
            </div>
            {{/each}}
        </div>
        {{/if}}

        <!-- More sections... -->

        <div style="text-align: center; margin-top: 30px;">
            <a href="{{ReviewUrl}}" class="btn">Review POs Now</a>
        </div>

        <div style="text-align: center; margin-top: 30px; color: #64748b; font-size: 12px;">
            <p>This is an automated message from QuickTech POS.</p>
            <p><a href="{{UnsubscribeUrl}}">Manage notification settings</a></p>
        </div>
    </div>
</body>
</html>
```

### Sending Email

Use existing email infrastructure or add MailKit:

```csharp
public class EmailService : IEmailService
{
    public async Task SendEmailAsync(
        List<string> recipients,
        string subject,
        string htmlBody,
        string? textBody = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:FromName"],
            _config["Email:FromAddress"]));

        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody ?? StripHtml(htmlBody)
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _config["Email:SmtpHost"],
            _config.GetValue<int>("Email:SmtpPort"),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _config["Email:Username"],
            _config["Email:Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

## Test Cases

1. **Daily digest at scheduled time** - Email sent with correct content
2. **No pending POs** - Email not sent (or sends "all clear" message)
3. **Weekly summary accuracy** - Numbers match actual activity
4. **Critical alert immediate** - Email sent within minutes
5. **Batched alerts** - Multiple alerts combined in daily digest
6. **Multiple recipients** - All recipients receive email
7. **Test email** - Sends sample to single address
8. **HTML rendering** - Email displays correctly in major clients
9. **Plain text fallback** - Readable in text-only clients
10. **Unsubscribe** - User can opt out of specific digest types

## Dependencies
- Issue #003: System Configuration (stores digest settings)
- Issue #005: Notification System (uses same notification data)

## Blocked By
- None

## Blocks
- None

## Estimated Complexity
**Medium** - Email templating with scheduling logic
