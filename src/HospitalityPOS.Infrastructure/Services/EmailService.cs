using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for email configuration and sending operations using MailKit.
/// </summary>
public partial class EmailService : IEmailService
{
    private readonly POSDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly string _encryptionKey;

    // Email validation regex
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public EmailService(
        POSDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get encryption key from configuration or use default for development
        _encryptionKey = _configuration["Encryption:Key"] ?? "HospitalityPOS_Default_Key_2025!";
    }

    #region Configuration Management

    public async Task<EmailConfigurationDto?> GetConfigurationAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.Set<EmailConfiguration>()
            .Include(c => c.Store)
            .Where(c => c.IsActive && c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken);

        // Fall back to global config if store-specific not found
        if (config == null && storeId.HasValue)
        {
            config = await _context.Set<EmailConfiguration>()
                .Include(c => c.Store)
                .Where(c => c.IsActive && c.StoreId == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return config == null ? null : MapToConfigurationDto(config);
    }

    public async Task<EmailConfigurationDto> SaveConfigurationAsync(
        int? configId,
        SaveEmailConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        EmailConfiguration config;

        if (configId.HasValue)
        {
            config = await _context.Set<EmailConfiguration>()
                .FirstOrDefaultAsync(c => c.Id == configId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Email configuration {configId} not found");
        }
        else
        {
            // Check for existing config for this store
            var existing = await _context.Set<EmailConfiguration>()
                .FirstOrDefaultAsync(c => c.StoreId == dto.StoreId && c.IsActive, cancellationToken);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    dto.StoreId.HasValue
                        ? $"Email configuration already exists for store {dto.StoreId}"
                        : "Global email configuration already exists");
            }

            config = new EmailConfiguration();
            _context.Set<EmailConfiguration>().Add(config);
        }

        config.StoreId = dto.StoreId;
        config.SmtpHost = dto.SmtpHost;
        config.SmtpPort = dto.SmtpPort;
        config.SmtpUsername = dto.SmtpUsername;
        config.UseSsl = dto.UseSsl;
        config.UseStartTls = dto.UseStartTls;
        config.FromAddress = dto.FromAddress;
        config.FromName = dto.FromName;
        config.ReplyToAddress = dto.ReplyToAddress;
        config.TimeoutSeconds = dto.TimeoutSeconds;
        config.MaxRetryAttempts = dto.MaxRetryAttempts;
        config.RetryDelayMinutes = dto.RetryDelayMinutes;

        // Only update password if provided
        if (!string.IsNullOrEmpty(dto.SmtpPassword))
        {
            config.SmtpPasswordEncrypted = Encrypt(dto.SmtpPassword);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.Information("Email configuration saved: {ConfigId}", config.Id);

        return MapToConfigurationDto(config);
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        int configId,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.Set<EmailConfiguration>()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken)
            ?? throw new InvalidOperationException($"Email configuration {configId} not found");

        var result = await TestSmtpConnectionAsync(
            config.SmtpHost,
            config.SmtpPort,
            config.SmtpUsername,
            config.SmtpPasswordEncrypted != null ? Decrypt(config.SmtpPasswordEncrypted) : null,
            config.UseSsl,
            config.UseStartTls,
            config.TimeoutSeconds,
            cancellationToken);

        // Update config with test result
        config.LastConnectionTest = DateTime.UtcNow;
        config.ConnectionTestSuccessful = result.Success;
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        SaveEmailConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        return await TestSmtpConnectionAsync(
            dto.SmtpHost,
            dto.SmtpPort,
            dto.SmtpUsername,
            dto.SmtpPassword,
            dto.UseSsl,
            dto.UseStartTls,
            dto.TimeoutSeconds,
            cancellationToken);
    }

    public async Task<bool> IsConfiguredAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(storeId, cancellationToken);
        return config != null && config.ConnectionTestSuccessful == true;
    }

    private async Task<ConnectionTestResultDto> TestSmtpConnectionAsync(
        string host,
        int port,
        string? username,
        string? password,
        bool useSsl,
        bool useStartTls,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            using var client = new SmtpClient();
            client.Timeout = timeoutSeconds * 1000;

            SecureSocketOptions options = useSsl ? SecureSocketOptions.SslOnConnect :
                useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(host, port, options, cancellationToken);
            var banner = "Connected successfully"; // Server greeting not exposed in MailKit 4.x

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);

            var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.Information("SMTP connection test successful: {Host}:{Port}", host, port);

            return new ConnectionTestResultDto
            {
                Success = true,
                Message = "Connection successful",
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = elapsed,
                ServerBanner = banner
            };
        }
        catch (Exception ex)
        {
            var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.Warning(ex, "SMTP connection test failed: {Host}:{Port}", host, port);

            return new ConnectionTestResultDto
            {
                Success = false,
                Message = ex.Message,
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = elapsed
            };
        }
    }

    #endregion

    #region Recipient Management

    public async Task<List<EmailRecipientDto>> GetRecipientsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<EmailRecipient>()
            .Include(r => r.Store)
            .Include(r => r.User)
            .Where(r => r.IsActive);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId || r.StoreId == null);
        }

        var recipients = await query.OrderBy(r => r.Email).ToListAsync(cancellationToken);
        return recipients.Select(MapToRecipientDto).ToList();
    }

    public async Task<List<EmailRecipientDto>> GetRecipientsForReportAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<EmailRecipient>()
            .Include(r => r.Store)
            .Include(r => r.User)
            .Where(r => r.IsActive);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId || r.StoreId == null);
        }

        // Filter by report type subscription
        query = reportType switch
        {
            EmailReportType.DailySales => query.Where(r => r.ReceiveDailySales),
            EmailReportType.WeeklyReport => query.Where(r => r.ReceiveWeeklyReport),
            EmailReportType.LowStockAlert => query.Where(r => r.ReceiveLowStockAlerts),
            EmailReportType.ExpiryAlert => query.Where(r => r.ReceiveExpiryAlerts),
            EmailReportType.MonthlySummary => query.Where(r => r.ReceiveMonthlyReport),
            _ => query
        };

        var recipients = await query.OrderBy(r => r.Email).ToListAsync(cancellationToken);
        return recipients.Select(MapToRecipientDto).ToList();
    }

    public async Task<EmailRecipientDto?> GetRecipientAsync(
        int recipientId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await _context.Set<EmailRecipient>()
            .Include(r => r.Store)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == recipientId, cancellationToken);

        return recipient == null ? null : MapToRecipientDto(recipient);
    }

    public async Task<EmailRecipientDto> SaveRecipientAsync(
        int? recipientId,
        SaveEmailRecipientDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateEmailFormat(dto.Email))
        {
            throw new ArgumentException("Invalid email format", nameof(dto.Email));
        }

        EmailRecipient recipient;

        if (recipientId.HasValue)
        {
            recipient = await _context.Set<EmailRecipient>()
                .FirstOrDefaultAsync(r => r.Id == recipientId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Recipient {recipientId} not found");
        }
        else
        {
            // Check for duplicate email
            var existing = await _context.Set<EmailRecipient>()
                .AnyAsync(r => r.Email == dto.Email && r.IsActive, cancellationToken);

            if (existing)
            {
                throw new InvalidOperationException($"Recipient with email {dto.Email} already exists");
            }

            recipient = new EmailRecipient();
            _context.Set<EmailRecipient>().Add(recipient);
        }

        recipient.Email = dto.Email;
        recipient.Name = dto.Name;
        recipient.StoreId = dto.StoreId;
        recipient.UserId = dto.UserId;
        recipient.ReceiveDailySales = dto.ReceiveDailySales;
        recipient.ReceiveWeeklyReport = dto.ReceiveWeeklyReport;
        recipient.ReceiveLowStockAlerts = dto.ReceiveLowStockAlerts;
        recipient.ReceiveExpiryAlerts = dto.ReceiveExpiryAlerts;
        recipient.ReceiveMonthlyReport = dto.ReceiveMonthlyReport;
        recipient.IsCc = dto.IsCc;
        recipient.IsBcc = dto.IsBcc;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.Information("Email recipient saved: {RecipientId} - {Email}", recipient.Id, recipient.Email);

        return MapToRecipientDto(recipient);
    }

    public async Task<bool> DeleteRecipientAsync(
        int recipientId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await _context.Set<EmailRecipient>()
            .FirstOrDefaultAsync(r => r.Id == recipientId, cancellationToken);

        if (recipient == null) return false;

        recipient.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Email recipient deleted: {RecipientId}", recipientId);
        return true;
    }

    public bool ValidateEmailFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex().IsMatch(email);
    }

    #endregion

    #region Schedule Management

    public async Task<List<EmailScheduleDto>> GetSchedulesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<EmailSchedule>()
            .Include(s => s.Store)
            .Where(s => s.IsActive);

        if (storeId.HasValue)
        {
            query = query.Where(s => s.StoreId == storeId || s.StoreId == null);
        }

        var schedules = await query.OrderBy(s => s.ReportType).ToListAsync(cancellationToken);
        return schedules.Select(MapToScheduleDto).ToList();
    }

    public async Task<EmailScheduleDto?> GetScheduleAsync(
        int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Set<EmailSchedule>()
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        return schedule == null ? null : MapToScheduleDto(schedule);
    }

    public async Task<EmailScheduleDto?> GetScheduleForReportAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Set<EmailSchedule>()
            .Include(s => s.Store)
            .Where(s => s.IsActive && s.ReportType == reportType && s.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken);

        // Fall back to global schedule
        if (schedule == null && storeId.HasValue)
        {
            schedule = await _context.Set<EmailSchedule>()
                .Include(s => s.Store)
                .Where(s => s.IsActive && s.ReportType == reportType && s.StoreId == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return schedule == null ? null : MapToScheduleDto(schedule);
    }

    public async Task<EmailScheduleDto> SaveScheduleAsync(
        int? scheduleId,
        SaveEmailScheduleDto dto,
        CancellationToken cancellationToken = default)
    {
        EmailSchedule schedule;

        if (scheduleId.HasValue)
        {
            schedule = await _context.Set<EmailSchedule>()
                .FirstOrDefaultAsync(s => s.Id == scheduleId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");
        }
        else
        {
            // Check for existing schedule for this report type and store
            var existing = await _context.Set<EmailSchedule>()
                .AnyAsync(s => s.ReportType == dto.ReportType && s.StoreId == dto.StoreId && s.IsActive, cancellationToken);

            if (existing)
            {
                throw new InvalidOperationException(
                    $"Schedule for {dto.ReportType} already exists" +
                    (dto.StoreId.HasValue ? $" for store {dto.StoreId}" : " (global)"));
            }

            schedule = new EmailSchedule();
            _context.Set<EmailSchedule>().Add(schedule);
        }

        schedule.StoreId = dto.StoreId;
        schedule.ReportType = dto.ReportType;
        schedule.IsEnabled = dto.IsEnabled;
        schedule.SendTime = dto.SendTime;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.DayOfMonth = dto.DayOfMonth;
        schedule.TimeZone = dto.TimeZone;
        schedule.AlertFrequency = dto.AlertFrequency;
        schedule.CustomSubject = dto.CustomSubject;

        // Calculate next scheduled time
        schedule.NextScheduledAt = CalculateNextScheduledTime(schedule);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.Information("Email schedule saved: {ScheduleId} - {ReportType}", schedule.Id, schedule.ReportType);

        return MapToScheduleDto(schedule);
    }

    public async Task<EmailScheduleDto> SetScheduleEnabledAsync(
        int scheduleId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Set<EmailSchedule>()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        schedule.IsEnabled = isEnabled;
        if (isEnabled)
        {
            schedule.NextScheduledAt = CalculateNextScheduledTime(schedule);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapToScheduleDto(schedule);
    }

    public async Task<bool> DeleteScheduleAsync(
        int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Set<EmailSchedule>()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        if (schedule == null) return false;

        schedule.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Email schedule deleted: {ScheduleId}", scheduleId);
        return true;
    }

    public async Task<List<EmailScheduleDto>> GetDueSchedulesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var schedules = await _context.Set<EmailSchedule>()
            .Include(s => s.Store)
            .Where(s => s.IsActive && s.IsEnabled && s.NextScheduledAt <= now)
            .ToListAsync(cancellationToken);

        return schedules.Select(MapToScheduleDto).ToList();
    }

    public async Task UpdateScheduleExecutedAsync(
        int scheduleId,
        DateTime executedAt,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Set<EmailSchedule>()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        schedule.LastExecutedAt = executedAt;
        schedule.NextScheduledAt = CalculateNextScheduledTime(schedule);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private DateTime? CalculateNextScheduledTime(EmailSchedule schedule)
    {
        if (!schedule.IsEnabled) return null;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);
            var nowInZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var todayInZone = DateOnly.FromDateTime(nowInZone);
            var sendTimeToday = todayInZone.ToDateTime(schedule.SendTime);

            DateTime nextInZone;

            switch (schedule.ReportType)
            {
                case EmailReportType.DailySales:
                case EmailReportType.LowStockAlert when schedule.AlertFrequency == EmailScheduleFrequency.Daily:
                case EmailReportType.ExpiryAlert when schedule.AlertFrequency == EmailScheduleFrequency.Daily:
                    nextInZone = sendTimeToday <= nowInZone ? sendTimeToday.AddDays(1) : sendTimeToday;
                    break;

                case EmailReportType.WeeklyReport:
                    var targetDayOfWeek = (DayOfWeek)(schedule.DayOfWeek ?? 1);
                    var daysUntilTarget = ((int)targetDayOfWeek - (int)nowInZone.DayOfWeek + 7) % 7;
                    if (daysUntilTarget == 0 && sendTimeToday <= nowInZone) daysUntilTarget = 7;
                    nextInZone = todayInZone.AddDays(daysUntilTarget).ToDateTime(schedule.SendTime);
                    break;

                case EmailReportType.MonthlySummary:
                    var targetDay = Math.Min(schedule.DayOfMonth ?? 1, DateTime.DaysInMonth(nowInZone.Year, nowInZone.Month));
                    var thisMonth = new DateOnly(nowInZone.Year, nowInZone.Month, targetDay).ToDateTime(schedule.SendTime);
                    if (thisMonth <= nowInZone)
                    {
                        var nextMonth = nowInZone.AddMonths(1);
                        targetDay = Math.Min(schedule.DayOfMonth ?? 1, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                        nextInZone = new DateOnly(nextMonth.Year, nextMonth.Month, targetDay).ToDateTime(schedule.SendTime);
                    }
                    else
                    {
                        nextInZone = thisMonth;
                    }
                    break;

                default:
                    return null;
            }

            return TimeZoneInfo.ConvertTimeToUtc(nextInZone, timeZone);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to calculate next schedule time for {ScheduleId}", schedule.Id);
            return null;
        }
    }

    #endregion

    #region Email Sending

    public async Task<EmailSendResultDto> SendEmailAsync(
        EmailMessageDto message,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(message.StoreId, cancellationToken);
        if (config == null)
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "Email not configured"
            };
        }

        // Create log entry
        var logId = await CreateEmailLogAsync(message, EmailSendStatus.Pending, cancellationToken);

        try
        {
            var mimeMessage = CreateMimeMessage(config, message);

            using var client = new SmtpClient();
            client.Timeout = config.TimeoutSeconds * 1000;

            SecureSocketOptions options = config.UseSsl ? SecureSocketOptions.SslOnConnect :
                config.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, options, cancellationToken);

            if (!string.IsNullOrEmpty(config.SmtpUsername) && config.HasPassword)
            {
                // Get decrypted password from database
                var fullConfig = await _context.Set<EmailConfiguration>()
                    .FirstAsync(c => c.Id == config.Id, cancellationToken);
                var password = fullConfig.SmtpPasswordEncrypted != null
                    ? Decrypt(fullConfig.SmtpPasswordEncrypted)
                    : null;

                if (!string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(config.SmtpUsername, password, cancellationToken);
                }
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            await UpdateEmailLogStatusAsync(logId, EmailSendStatus.Sent, null, cancellationToken);
            _logger.Information("Email sent successfully: {Subject} to {Recipients}",
                message.Subject, string.Join(", ", message.ToAddresses));

            return new EmailSendResultDto
            {
                Success = true,
                EmailLogId = logId,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            await UpdateEmailLogStatusAsync(logId, EmailSendStatus.Failed, ex.Message, cancellationToken);
            _logger.Error(ex, "Failed to send email: {Subject}", message.Subject);

            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                EmailLogId = logId
            };
        }
    }

    public async Task<EmailSendResultDto> SendTestEmailAsync(
        int configId,
        string toAddress,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateEmailFormat(toAddress))
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "Invalid email address format"
            };
        }

        var config = await _context.Set<EmailConfiguration>()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);

        if (config == null)
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "Email configuration not found"
            };
        }

        var message = new EmailMessageDto
        {
            ToAddresses = new List<string> { toAddress },
            Subject = "Hospitality POS - Test Email",
            HtmlBody = @"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Test Email</h2>
                    <p>This is a test email from Hospitality POS System.</p>
                    <p>If you received this email, your email configuration is working correctly.</p>
                    <hr/>
                    <p style='color: #666; font-size: 12px;'>
                        Sent at: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"
                    </p>
                </body>
                </html>",
            PlainTextBody = "This is a test email from Hospitality POS System.\n\nIf you received this email, your email configuration is working correctly.",
            ReportType = EmailReportType.Custom,
            StoreId = config.StoreId
        };

        return await SendEmailAsync(message, cancellationToken);
    }

    public async Task<EmailSendResultDto> RetryEmailAsync(
        int emailLogId,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.Set<EmailLog>()
            .FirstOrDefaultAsync(l => l.Id == emailLogId, cancellationToken);

        if (log == null)
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "Email log not found"
            };
        }

        if (log.Status != EmailSendStatus.Failed && log.Status != EmailSendStatus.Retry)
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = $"Cannot retry email with status {log.Status}"
            };
        }

        var config = await GetConfigurationAsync(log.StoreId, cancellationToken);
        if (config == null)
        {
            log.RetryCount++;
            log.Status = EmailSendStatus.Failed;
            log.ErrorMessage = "Email not configured";
            await _context.SaveChangesAsync(cancellationToken);

            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "Email not configured"
            };
        }

        // Check retry limit
        var maxRetries = config.MaxRetryAttempts;
        if (log.RetryCount >= maxRetries)
        {
            log.Status = EmailSendStatus.Failed;
            log.ErrorMessage = $"Max retry attempts ({maxRetries}) exceeded";
            await _context.SaveChangesAsync(cancellationToken);

            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = log.ErrorMessage
            };
        }

        log.RetryCount++;
        log.Status = EmailSendStatus.Retry;
        await _context.SaveChangesAsync(cancellationToken);

        // We don't have the original message, so we can't retry it directly
        // The scheduler should pick up retry status and resend
        return new EmailSendResultDto
        {
            Success = true,
            EmailLogId = emailLogId,
            ErrorMessage = "Queued for retry"
        };
    }

    private MimeMessage CreateMimeMessage(EmailConfigurationDto config, EmailMessageDto message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromAddress));

        foreach (var to in message.ToAddresses)
        {
            mimeMessage.To.Add(MailboxAddress.Parse(to));
        }

        foreach (var cc in message.CcAddresses)
        {
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
        }

        foreach (var bcc in message.BccAddresses)
        {
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        if (!string.IsNullOrEmpty(config.ReplyToAddress))
        {
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(config.ReplyToAddress));
        }

        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };

        if (message.Attachment != null)
        {
            builder.Attachments.Add(
                message.Attachment.FileName,
                message.Attachment.Content,
                ContentType.Parse(message.Attachment.ContentType));
        }

        mimeMessage.Body = builder.ToMessageBody();

        return mimeMessage;
    }

    #endregion

    #region Email Logging

    public async Task<EmailLogResultDto> GetEmailLogsAsync(
        EmailLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Set<EmailLog>()
            .Include(l => l.Store)
            .AsQueryable();

        if (query.StoreId.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.StoreId == query.StoreId);
        }

        if (query.ReportType.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.ReportType == query.ReportType);
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.Status == query.Status);
        }

        if (query.FromDate.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.CreatedAt >= query.FromDate);
        }

        if (query.ToDate.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.CreatedAt <= query.ToDate);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            dbQuery = dbQuery.Where(l =>
                l.Subject.Contains(query.SearchTerm) ||
                l.Recipients.Contains(query.SearchTerm));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var logs = await dbQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new EmailLogResultDto
        {
            Items = logs.Select(MapToLogDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<EmailLogDto?> GetEmailLogAsync(
        int logId,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.Set<EmailLog>()
            .Include(l => l.Store)
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);

        return log == null ? null : MapToLogDto(log);
    }

    public async Task<int> CreateEmailLogAsync(
        EmailMessageDto message,
        EmailSendStatus status,
        CancellationToken cancellationToken = default)
    {
        var log = new EmailLog
        {
            StoreId = message.StoreId,
            ReportType = message.ReportType,
            Recipients = string.Join(", ", message.ToAddresses.Concat(message.CcAddresses).Concat(message.BccAddresses)),
            Subject = message.Subject,
            Status = status,
            HasAttachment = message.Attachment != null,
            AttachmentName = message.Attachment?.FileName,
            BodySizeBytes = Encoding.UTF8.GetByteCount(message.HtmlBody ?? "")
        };

        _context.Set<EmailLog>().Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }

    public async Task UpdateEmailLogStatusAsync(
        int logId,
        EmailSendStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.Set<EmailLog>()
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);

        if (log != null)
        {
            log.Status = status;
            log.ErrorMessage = errorMessage;
            if (status == EmailSendStatus.Sent)
            {
                log.SentAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<EmailLogDto>> GetPendingRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        var logs = await _context.Set<EmailLog>()
            .Include(l => l.Store)
            .Where(l => l.Status == EmailSendStatus.Retry ||
                       (l.Status == EmailSendStatus.Failed && l.RetryCount < 3))
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToLogDto).ToList();
    }

    #endregion

    #region Alert Configuration

    public async Task<LowStockAlertConfigDto?> GetLowStockAlertConfigAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.Set<LowStockAlertConfig>()
            .Include(c => c.Store)
            .Where(c => c.IsActive && c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null && storeId.HasValue)
        {
            config = await _context.Set<LowStockAlertConfig>()
                .Include(c => c.Store)
                .Where(c => c.IsActive && c.StoreId == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return config == null ? null : new LowStockAlertConfigDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            StoreName = config.Store?.Name,
            IsEnabled = config.IsEnabled,
            AlertFrequency = config.AlertFrequency,
            ThresholdPercent = config.ThresholdPercent,
            MinimumItemsForAlert = config.MinimumItemsForAlert,
            MaxItemsPerEmail = config.MaxItemsPerEmail,
            IsActive = config.IsActive
        };
    }

    public async Task<LowStockAlertConfigDto> SaveLowStockAlertConfigAsync(
        int? configId,
        SaveLowStockAlertConfigDto dto,
        CancellationToken cancellationToken = default)
    {
        LowStockAlertConfig config;

        if (configId.HasValue)
        {
            config = await _context.Set<LowStockAlertConfig>()
                .FirstOrDefaultAsync(c => c.Id == configId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Low stock alert config {configId} not found");
        }
        else
        {
            config = new LowStockAlertConfig();
            _context.Set<LowStockAlertConfig>().Add(config);
        }

        config.StoreId = dto.StoreId;
        config.IsEnabled = dto.IsEnabled;
        config.AlertFrequency = dto.AlertFrequency;
        config.ThresholdPercent = dto.ThresholdPercent;
        config.MinimumItemsForAlert = dto.MinimumItemsForAlert;
        config.MaxItemsPerEmail = dto.MaxItemsPerEmail;

        await _context.SaveChangesAsync(cancellationToken);

        return new LowStockAlertConfigDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            IsEnabled = config.IsEnabled,
            AlertFrequency = config.AlertFrequency,
            ThresholdPercent = config.ThresholdPercent,
            MinimumItemsForAlert = config.MinimumItemsForAlert,
            MaxItemsPerEmail = config.MaxItemsPerEmail,
            IsActive = config.IsActive
        };
    }

    public async Task<ExpiryAlertConfigDto?> GetExpiryAlertConfigAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.Set<ExpiryAlertConfig>()
            .Include(c => c.Store)
            .Where(c => c.IsActive && c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null && storeId.HasValue)
        {
            config = await _context.Set<ExpiryAlertConfig>()
                .Include(c => c.Store)
                .Where(c => c.IsActive && c.StoreId == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return config == null ? null : new ExpiryAlertConfigDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            StoreName = config.Store?.Name,
            IsEnabled = config.IsEnabled,
            AlertFrequency = config.AlertFrequency,
            AlertThresholdDays = config.AlertThresholdDays,
            UrgentThresholdDays = config.UrgentThresholdDays,
            MaxItemsPerEmail = config.MaxItemsPerEmail,
            IsActive = config.IsActive
        };
    }

    public async Task<ExpiryAlertConfigDto> SaveExpiryAlertConfigAsync(
        int? configId,
        SaveExpiryAlertConfigDto dto,
        CancellationToken cancellationToken = default)
    {
        ExpiryAlertConfig config;

        if (configId.HasValue)
        {
            config = await _context.Set<ExpiryAlertConfig>()
                .FirstOrDefaultAsync(c => c.Id == configId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Expiry alert config {configId} not found");
        }
        else
        {
            config = new ExpiryAlertConfig();
            _context.Set<ExpiryAlertConfig>().Add(config);
        }

        config.StoreId = dto.StoreId;
        config.IsEnabled = dto.IsEnabled;
        config.AlertFrequency = dto.AlertFrequency;
        config.AlertThresholdDays = dto.AlertThresholdDays;
        config.UrgentThresholdDays = dto.UrgentThresholdDays;
        config.MaxItemsPerEmail = dto.MaxItemsPerEmail;

        await _context.SaveChangesAsync(cancellationToken);

        return new ExpiryAlertConfigDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            IsEnabled = config.IsEnabled,
            AlertFrequency = config.AlertFrequency,
            AlertThresholdDays = config.AlertThresholdDays,
            UrgentThresholdDays = config.UrgentThresholdDays,
            MaxItemsPerEmail = config.MaxItemsPerEmail,
            IsActive = config.IsActive
        };
    }

    #endregion

    #region Dashboard

    public async Task<EmailDashboardDto> GetDashboardAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(storeId, cancellationToken);
        var today = DateTime.UtcNow.Date;

        var recipientCount = await _context.Set<EmailRecipient>()
            .CountAsync(r => r.IsActive && (r.StoreId == storeId || r.StoreId == null), cancellationToken);

        var activeSchedules = await _context.Set<EmailSchedule>()
            .CountAsync(s => s.IsActive && s.IsEnabled && (s.StoreId == storeId || s.StoreId == null), cancellationToken);

        var todayLogs = await _context.Set<EmailLog>()
            .Where(l => l.CreatedAt >= today && (l.StoreId == storeId || l.StoreId == null))
            .ToListAsync(cancellationToken);

        var pendingCount = await _context.Set<EmailLog>()
            .CountAsync(l => (l.Status == EmailSendStatus.Pending || l.Status == EmailSendStatus.Retry) &&
                            (l.StoreId == storeId || l.StoreId == null), cancellationToken);

        var lastSent = await _context.Set<EmailLog>()
            .Include(l => l.Store)
            .Where(l => l.Status == EmailSendStatus.Sent && (l.StoreId == storeId || l.StoreId == null))
            .OrderByDescending(l => l.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastFailed = await _context.Set<EmailLog>()
            .Include(l => l.Store)
            .Where(l => l.Status == EmailSendStatus.Failed && (l.StoreId == storeId || l.StoreId == null))
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new EmailDashboardDto
        {
            IsConfigured = config != null,
            ConnectionHealthy = config?.ConnectionTestSuccessful == true,
            LastConnectionTest = config?.LastConnectionTest,
            TotalRecipients = recipientCount,
            ActiveSchedules = activeSchedules,
            EmailsSentToday = todayLogs.Count(l => l.Status == EmailSendStatus.Sent),
            EmailsFailedToday = todayLogs.Count(l => l.Status == EmailSendStatus.Failed),
            PendingEmails = pendingCount,
            LastSentEmail = lastSent != null ? MapToLogDto(lastSent) : null,
            LastFailedEmail = lastFailed != null ? MapToLogDto(lastFailed) : null
        };
    }

    #endregion

    #region Encryption

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        using var aes = Aes.Create();
        var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        aes.Key = key;

        // Extract IV from cipher text
        var iv = new byte[16];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 16, cipherBytes.Length - 16);

        return Encoding.UTF8.GetString(plainBytes);
    }

    #endregion

    #region Mapping Helpers

    private static EmailConfigurationDto MapToConfigurationDto(EmailConfiguration config)
    {
        return new EmailConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            StoreName = config.Store?.Name,
            SmtpHost = config.SmtpHost,
            SmtpPort = config.SmtpPort,
            SmtpUsername = config.SmtpUsername,
            HasPassword = !string.IsNullOrEmpty(config.SmtpPasswordEncrypted),
            UseSsl = config.UseSsl,
            UseStartTls = config.UseStartTls,
            FromAddress = config.FromAddress,
            FromName = config.FromName,
            ReplyToAddress = config.ReplyToAddress,
            TimeoutSeconds = config.TimeoutSeconds,
            MaxRetryAttempts = config.MaxRetryAttempts,
            RetryDelayMinutes = config.RetryDelayMinutes,
            LastConnectionTest = config.LastConnectionTest,
            ConnectionTestSuccessful = config.ConnectionTestSuccessful,
            IsActive = config.IsActive
        };
    }

    private static EmailRecipientDto MapToRecipientDto(EmailRecipient recipient)
    {
        var reportTypes = new List<string>();
        if (recipient.ReceiveDailySales) reportTypes.Add("Daily Sales");
        if (recipient.ReceiveWeeklyReport) reportTypes.Add("Weekly Report");
        if (recipient.ReceiveLowStockAlerts) reportTypes.Add("Low Stock");
        if (recipient.ReceiveExpiryAlerts) reportTypes.Add("Expiry");
        if (recipient.ReceiveMonthlyReport) reportTypes.Add("Monthly");

        return new EmailRecipientDto
        {
            Id = recipient.Id,
            Email = recipient.Email,
            Name = recipient.Name,
            StoreId = recipient.StoreId,
            StoreName = recipient.Store?.Name,
            UserId = recipient.UserId,
            UserName = recipient.User?.Username,
            ReceiveDailySales = recipient.ReceiveDailySales,
            ReceiveWeeklyReport = recipient.ReceiveWeeklyReport,
            ReceiveLowStockAlerts = recipient.ReceiveLowStockAlerts,
            ReceiveExpiryAlerts = recipient.ReceiveExpiryAlerts,
            ReceiveMonthlyReport = recipient.ReceiveMonthlyReport,
            IsCc = recipient.IsCc,
            IsBcc = recipient.IsBcc,
            IsActive = recipient.IsActive,
            ReportTypesSummary = string.Join(", ", reportTypes)
        };
    }

    private static EmailScheduleDto MapToScheduleDto(EmailSchedule schedule)
    {
        var dayOfWeekNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        return new EmailScheduleDto
        {
            Id = schedule.Id,
            StoreId = schedule.StoreId,
            StoreName = schedule.Store?.Name,
            ReportType = schedule.ReportType,
            ReportTypeName = schedule.ReportType.ToString(),
            IsEnabled = schedule.IsEnabled,
            SendTime = schedule.SendTime,
            SendTimeDisplay = schedule.SendTime.ToString("hh:mm tt"),
            DayOfWeek = schedule.DayOfWeek,
            DayOfWeekName = schedule.DayOfWeek.HasValue && schedule.DayOfWeek.Value >= 0 && schedule.DayOfWeek.Value <= 6
                ? dayOfWeekNames[schedule.DayOfWeek.Value]
                : null,
            DayOfMonth = schedule.DayOfMonth,
            TimeZone = schedule.TimeZone,
            AlertFrequency = schedule.AlertFrequency,
            LastExecutedAt = schedule.LastExecutedAt,
            NextScheduledAt = schedule.NextScheduledAt,
            CustomSubject = schedule.CustomSubject,
            IsActive = schedule.IsActive
        };
    }

    private static EmailLogDto MapToLogDto(EmailLog log)
    {
        return new EmailLogDto
        {
            Id = log.Id,
            StoreId = log.StoreId,
            StoreName = log.Store?.Name,
            ReportType = log.ReportType,
            ReportTypeName = log.ReportType.ToString(),
            Recipients = log.Recipients,
            Subject = log.Subject,
            Status = log.Status,
            StatusName = log.Status.ToString(),
            ErrorMessage = log.ErrorMessage,
            RetryCount = log.RetryCount,
            ScheduledAt = log.ScheduledAt,
            SentAt = log.SentAt,
            GenerationTimeMs = log.GenerationTimeMs,
            HasAttachment = log.HasAttachment,
            AttachmentName = log.AttachmentName,
            CreatedAt = log.CreatedAt
        };
    }

    #endregion
}
