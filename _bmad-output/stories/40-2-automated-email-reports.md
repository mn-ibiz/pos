# Story 40.2: Automated Email Reports

Status: review

## Story

As a **business owner**,
I want **daily and weekly sales reports automatically emailed to me**,
so that **I can stay informed about business performance without logging into the system**.

## Business Context

**HIGH PRIORITY - MANAGEMENT CONVENIENCE**

Business owners and managers need:
- Daily summaries without manual effort
- Remote visibility into store performance
- Alerts for critical issues (low stock, expiry)
- Scheduled reports for planning

**Business Value:** Reduces time spent on manual reporting, ensures stakeholders stay informed.

## Acceptance Criteria

### AC1: Email Configuration
- [x] Configure SMTP server settings (host, port, credentials)
- [x] Support SSL/TLS encryption
- [x] Test email connection button
- [x] Store credentials securely (encrypted)

### AC2: Recipient Management
- [x] Add multiple email recipients
- [x] Assign report types to each recipient
- [x] Option to CC/BCC additional addresses
- [x] Recipient validation (valid email format)

### AC3: Daily Sales Summary Email
- [x] Automated email sent at configured time (default: 8 PM)
- [x] Contains: Total sales, transaction count, top products
- [x] Payment method breakdown
- [x] Comparison to previous day
- [x] HTML formatted for readability

### AC4: Weekly Performance Report
- [x] Automated email sent weekly (default: Monday 8 AM)
- [x] Week-over-week comparison
- [x] Best/worst performing days
- [x] Category performance summary
- [x] Attached Excel file with detailed data

### AC5: Low Stock Alert Emails
- [x] Email when products fall below reorder level
- [x] Configurable: immediate or daily digest
- [x] List of affected products with current stock
- [x] Option to disable per product

### AC6: Expiry Alert Emails
- [x] Email for products expiring within threshold
- [x] Configurable threshold (7, 14, 30 days)
- [x] List of expiring products with dates
- [x] Daily digest option

### AC7: Customizable Schedule
- [x] Configure send time for each report type
- [x] Enable/disable individual report types
- [x] Day of week selection for weekly reports
- [x] Time zone handling

### AC8: Email History & Logs
- [x] Log all sent emails with timestamp
- [x] Track delivery status (if SMTP provides)
- [x] Retry failed emails
- [x] View email history in admin

## Tasks / Subtasks

- [x] **Task 1: Email Infrastructure** (AC: 1)
  - [x] 1.1 Create EmailConfiguration table
  - [x] 1.2 Create IEmailService interface
  - [x] 1.3 Implement SmtpEmailService using MailKit
  - [x] 1.4 Encrypt SMTP credentials at rest
  - [x] 1.5 Implement connection test method
  - [x] 1.6 Unit tests with mock SMTP

- [x] **Task 2: Email Settings UI** (AC: 1, 2, 7)
  - [x] 2.1 Create EmailSettingsView.xaml
  - [x] 2.2 SMTP configuration form
  - [x] 2.3 Recipient management grid
  - [x] 2.4 Report schedule configuration
  - [x] 2.5 Test email button

- [x] **Task 3: Report Email Templates** (AC: 3, 4)
  - [x] 3.1 Create HTML template for daily summary
  - [x] 3.2 Create HTML template for weekly report
  - [x] 3.3 Implement template rendering with data
  - [x] 3.4 Generate Excel attachment for weekly
  - [x] 3.5 Responsive HTML design (mobile-friendly)

- [x] **Task 4: Alert Email Templates** (AC: 5, 6)
  - [x] 4.1 Create HTML template for low stock alerts
  - [x] 4.2 Create HTML template for expiry alerts
  - [x] 4.3 Support immediate vs digest modes
  - [x] 4.4 Product list formatting

- [x] **Task 5: Email Scheduler Service** (AC: 3, 4, 5, 6, 7)
  - [x] 5.1 Create EmailSchedulerService as HostedService
  - [x] 5.2 Implement daily report scheduling
  - [x] 5.3 Implement weekly report scheduling
  - [x] 5.4 Implement alert digest scheduling
  - [x] 5.5 Handle time zones correctly
  - [x] 5.6 Integration tests

- [x] **Task 6: Email Logging** (AC: 8)
  - [x] 6.1 Create EmailLog table
  - [x] 6.2 Log all email send attempts
  - [x] 6.3 Track success/failure status
  - [x] 6.4 Implement retry for failed emails
  - [x] 6.5 Email history view in admin

## Dev Notes

### Email Template Example (Daily Summary)

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .metric { font-size: 24px; font-weight: bold; }
        .positive { color: green; }
        .negative { color: red; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    </style>
</head>
<body>
    <h1>Daily Sales Summary - {{Date}}</h1>
    <h2>{{StoreName}}</h2>

    <table>
        <tr>
            <td>Total Sales</td>
            <td class="metric">KSh {{TotalSales}}</td>
            <td class="{{ComparisonClass}}">{{ComparisonPercent}}% vs yesterday</td>
        </tr>
        <tr>
            <td>Transactions</td>
            <td class="metric">{{TransactionCount}}</td>
            <td></td>
        </tr>
    </table>

    <h3>Top 5 Products</h3>
    <table>
        <tr><th>Product</th><th>Qty Sold</th><th>Revenue</th></tr>
        {{#TopProducts}}
        <tr><td>{{Name}}</td><td>{{Quantity}}</td><td>KSh {{Revenue}}</td></tr>
        {{/TopProducts}}
    </table>
</body>
</html>
```

### Database Schema

```sql
CREATE TABLE EmailConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SmtpHost NVARCHAR(100) NOT NULL,
    SmtpPort INT NOT NULL DEFAULT 587,
    SmtpUsername NVARCHAR(100),
    SmtpPassword NVARCHAR(500), -- Encrypted
    UseSsl BIT DEFAULT 1,
    FromAddress NVARCHAR(100) NOT NULL,
    FromName NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE EmailRecipients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL,
    Name NVARCHAR(100),
    ReportTypes NVARCHAR(200), -- CSV: DailySales,WeeklyReport,LowStock,Expiry
    IsActive BIT DEFAULT 1
);

CREATE TABLE EmailSchedules (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReportType NVARCHAR(50) NOT NULL, -- DailySales, WeeklyReport, LowStockDigest, ExpiryDigest
    IsEnabled BIT DEFAULT 1,
    SendTime TIME NOT NULL, -- e.g., 20:00:00
    DayOfWeek INT, -- For weekly: 1=Monday, 7=Sunday
    LastSentAt DATETIME2
);

CREATE TABLE EmailLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReportType NVARCHAR(50),
    Recipients NVARCHAR(500),
    Subject NVARCHAR(200),
    Status NVARCHAR(20), -- Sent, Failed, Pending
    ErrorMessage NVARCHAR(500),
    RetryCount INT DEFAULT 0,
    SentAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### Using MailKit for SMTP

```csharp
public class SmtpEmailService : IEmailService
{
    public async Task SendEmailAsync(EmailMessage message)
    {
        var config = await GetConfigurationAsync();

        using var client = new SmtpClient();
        await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl);
        await client.AuthenticateAsync(config.SmtpUsername, Decrypt(config.SmtpPassword));

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        mimeMessage.To.AddRange(message.Recipients.Select(r => new MailboxAddress(r.Name, r.Email)));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (message.Attachment != null)
            builder.Attachments.Add(message.AttachmentName, message.Attachment);

        mimeMessage.Body = builder.ToMessageBody();

        await client.SendAsync(mimeMessage);
        await client.DisconnectAsync(true);
    }
}
```

### Architecture Compliance

- **Layer:** Infrastructure (EmailService), Business (ReportEmailService)
- **Pattern:** Background service for scheduling
- **Security:** Encrypt SMTP credentials
- **NuGet:** MailKit, MimeKit

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#3.10-Automated-Email-Reports]
- [Source: _bmad-output/architecture.md#Reporting]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.5 (claude-opus-4-5-20251101)

### Debug Log References

None

### Completion Notes List

1. Implemented complete email infrastructure using MailKit/MimeKit for SMTP communication
2. Created comprehensive entity model with EmailConfiguration, EmailRecipient, EmailSchedule, EmailLog, EmailTemplate, LowStockAlertConfig, ExpiryAlertConfig
3. Implemented AES encryption for SMTP password storage
4. Created IEmailService with full CRUD operations for configuration, recipients, schedules
5. Implemented EmailReportService with HTML template rendering for all report types
6. Created background EmailSchedulerService (HostedService) for automated email scheduling
7. Implemented EmailTriggerService for on-demand report generation
8. Created WPF UI with tabbed interface for Configuration, Recipients, Schedules, Alerts, and Logs
9. Comprehensive unit tests for EmailService

### File List

#### New Files Created

- `src/HospitalityPOS.Core/Entities/EmailEntities.cs` - Email entity classes and enums
- `src/HospitalityPOS.Core/DTOs/EmailDtos.cs` - Data transfer objects for email operations
- `src/HospitalityPOS.Core/Interfaces/IEmailService.cs` - Email service interface
- `src/HospitalityPOS.Core/Interfaces/IEmailReportService.cs` - Report generation service interface
- `src/HospitalityPOS.Infrastructure/Services/EmailService.cs` - SMTP email service implementation
- `src/HospitalityPOS.Infrastructure/Services/EmailReportService.cs` - Report generation and HTML rendering
- `src/HospitalityPOS.Infrastructure/Services/EmailSchedulerService.cs` - Background scheduler and trigger services
- `src/HospitalityPOS.Infrastructure/Data/Configurations/EmailConfiguration.cs` - EF Core entity configurations
- `src/HospitalityPOS.WPF/ViewModels/EmailSettingsViewModel.cs` - Email settings ViewModel
- `src/HospitalityPOS.WPF/Views/EmailSettingsView.xaml` - Email settings UI
- `src/HospitalityPOS.WPF/Views/EmailSettingsView.xaml.cs` - Email settings code-behind
- `tests/HospitalityPOS.Business.Tests/Services/EmailServiceTests.cs` - Unit tests

#### Modified Files

- `src/HospitalityPOS.Infrastructure/HospitalityPOS.Infrastructure.csproj` - Added MailKit, MimeKit packages
- `src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs` - Added Email DbSets
- `src/HospitalityPOS.WPF/App.xaml.cs` - Registered email services and ViewModel
- `src/HospitalityPOS.WPF/ViewModels/ViewModelLocator.cs` - Added EmailSettingsViewModel
- `src/HospitalityPOS.WPF/Converters/BoolToVisibilityConverters.cs` - Added StringEqualsToVisibilityConverter
