using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Background service for scheduling and sending automated email reports.
/// </summary>
public class EmailSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public EmailSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Email Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledEmailsAsync(stoppingToken);
                await ProcessPendingRetriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in email scheduler service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.Information("Email Scheduler Service stopped");
    }

    private async Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var reportService = scope.ServiceProvider.GetRequiredService<IEmailReportService>();

        // Get due schedules
        var dueSchedules = await emailService.GetDueSchedulesAsync(cancellationToken);

        foreach (var schedule in dueSchedules)
        {
            try
            {
                _logger.Information("Processing scheduled email: {ReportType} for store {StoreId}",
                    schedule.ReportType, schedule.StoreId);

                // Generate the report email
                var message = await reportService.GenerateReportEmailAsync(
                    schedule.ReportType,
                    schedule.StoreId,
                    cancellationToken);

                if (message != null)
                {
                    // Send the email
                    var result = await emailService.SendEmailAsync(message, cancellationToken);

                    if (result.Success)
                    {
                        _logger.Information("Scheduled email sent successfully: {ReportType}",
                            schedule.ReportType);
                    }
                    else
                    {
                        _logger.Warning("Failed to send scheduled email: {ReportType} - {Error}",
                            schedule.ReportType, result.ErrorMessage);
                    }
                }
                else
                {
                    _logger.Information("No content to send for scheduled email: {ReportType}",
                        schedule.ReportType);
                }

                // Update schedule executed time
                await emailService.UpdateScheduleExecutedAsync(
                    schedule.Id,
                    DateTime.UtcNow,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error processing scheduled email: {ReportType}", schedule.ReportType);
            }
        }
    }

    private async Task ProcessPendingRetriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Get pending retries
        var pendingRetries = await emailService.GetPendingRetriesAsync(cancellationToken);

        foreach (var log in pendingRetries)
        {
            try
            {
                _logger.Information("Retrying failed email: {LogId} - {Subject}",
                    log.Id, log.Subject);

                var result = await emailService.RetryEmailAsync(log.Id, cancellationToken);

                if (result.Success)
                {
                    _logger.Information("Email retry queued: {LogId}", log.Id);
                }
                else
                {
                    _logger.Warning("Email retry failed: {LogId} - {Error}",
                        log.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrying email: {LogId}", log.Id);
            }
        }
    }
}

/// <summary>
/// Service for triggering email reports on-demand.
/// </summary>
public class EmailTriggerService
{
    private readonly IEmailService _emailService;
    private readonly IEmailReportService _reportService;
    private readonly ILogger _logger;

    public EmailTriggerService(
        IEmailService emailService,
        IEmailReportService reportService,
        ILogger logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggers an immediate send of a specific report type.
    /// </summary>
    /// <param name="reportType">Type of report to send.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    public async Task<EmailSendResultDto> TriggerReportAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Triggering manual email report: {ReportType} for store {StoreId}",
            reportType, storeId);

        var message = await _reportService.GenerateReportEmailAsync(reportType, storeId, cancellationToken);

        if (message == null)
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "No content to send or no recipients configured"
            };
        }

        return await _emailService.SendEmailAsync(message, cancellationToken);
    }

    /// <summary>
    /// Triggers a daily sales report for a specific date.
    /// </summary>
    /// <param name="date">Report date.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    public async Task<EmailSendResultDto> TriggerDailySalesReportAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var recipients = await _emailService.GetRecipientsForReportAsync(
            EmailReportType.DailySales, storeId, cancellationToken);

        if (!recipients.Any())
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "No recipients configured for daily sales report"
            };
        }

        var data = await _reportService.GenerateDailySalesDataAsync(date, storeId, cancellationToken);

        var message = new EmailMessageDto
        {
            ToAddresses = recipients.Where(r => !r.IsCc && !r.IsBcc).Select(r => r.Email).ToList(),
            CcAddresses = recipients.Where(r => r.IsCc).Select(r => r.Email).ToList(),
            BccAddresses = recipients.Where(r => r.IsBcc).Select(r => r.Email).ToList(),
            Subject = _reportService.GetDailySalesSubject(data),
            HtmlBody = _reportService.RenderDailySalesEmail(data),
            ReportType = EmailReportType.DailySales,
            StoreId = storeId
        };

        return await _emailService.SendEmailAsync(message, cancellationToken);
    }

    /// <summary>
    /// Triggers a weekly report for a specific week.
    /// </summary>
    /// <param name="weekEndDate">Last day of the week.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    public async Task<EmailSendResultDto> TriggerWeeklyReportAsync(
        DateTime weekEndDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var recipients = await _emailService.GetRecipientsForReportAsync(
            EmailReportType.WeeklyReport, storeId, cancellationToken);

        if (!recipients.Any())
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "No recipients configured for weekly report"
            };
        }

        var data = await _reportService.GenerateWeeklyReportDataAsync(weekEndDate, storeId, cancellationToken);
        var (excelContent, excelName) = await _reportService.GenerateWeeklyReportExcelAsync(data, cancellationToken);

        var message = new EmailMessageDto
        {
            ToAddresses = recipients.Where(r => !r.IsCc && !r.IsBcc).Select(r => r.Email).ToList(),
            CcAddresses = recipients.Where(r => r.IsCc).Select(r => r.Email).ToList(),
            BccAddresses = recipients.Where(r => r.IsBcc).Select(r => r.Email).ToList(),
            Subject = _reportService.GetWeeklyReportSubject(data),
            HtmlBody = _reportService.RenderWeeklyReportEmail(data),
            Attachment = new EmailAttachmentDto
            {
                FileName = excelName,
                Content = excelContent,
                ContentType = "text/csv"
            },
            ReportType = EmailReportType.WeeklyReport,
            StoreId = storeId
        };

        return await _emailService.SendEmailAsync(message, cancellationToken);
    }

    /// <summary>
    /// Triggers a low stock alert immediately.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    public async Task<EmailSendResultDto> TriggerLowStockAlertAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var recipients = await _emailService.GetRecipientsForReportAsync(
            EmailReportType.LowStockAlert, storeId, cancellationToken);

        if (!recipients.Any())
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "No recipients configured for low stock alerts"
            };
        }

        var data = await _reportService.GenerateLowStockAlertDataAsync(storeId, cancellationToken);

        if (data.TotalLowStockItems == 0)
        {
            return new EmailSendResultDto
            {
                Success = true,
                ErrorMessage = "No low stock items to report"
            };
        }

        var message = new EmailMessageDto
        {
            ToAddresses = recipients.Where(r => !r.IsCc && !r.IsBcc).Select(r => r.Email).ToList(),
            CcAddresses = recipients.Where(r => r.IsCc).Select(r => r.Email).ToList(),
            BccAddresses = recipients.Where(r => r.IsBcc).Select(r => r.Email).ToList(),
            Subject = _reportService.GetLowStockAlertSubject(data),
            HtmlBody = _reportService.RenderLowStockAlertEmail(data),
            ReportType = EmailReportType.LowStockAlert,
            StoreId = storeId
        };

        return await _emailService.SendEmailAsync(message, cancellationToken);
    }

    /// <summary>
    /// Triggers an expiry alert immediately.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    public async Task<EmailSendResultDto> TriggerExpiryAlertAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var recipients = await _emailService.GetRecipientsForReportAsync(
            EmailReportType.ExpiryAlert, storeId, cancellationToken);

        if (!recipients.Any())
        {
            return new EmailSendResultDto
            {
                Success = false,
                ErrorMessage = "No recipients configured for expiry alerts"
            };
        }

        var data = await _reportService.GenerateExpiryAlertDataAsync(storeId, cancellationToken);

        if (data.TotalExpiringItems == 0)
        {
            return new EmailSendResultDto
            {
                Success = true,
                ErrorMessage = "No expiring items to report"
            };
        }

        var message = new EmailMessageDto
        {
            ToAddresses = recipients.Where(r => !r.IsCc && !r.IsBcc).Select(r => r.Email).ToList(),
            CcAddresses = recipients.Where(r => r.IsCc).Select(r => r.Email).ToList(),
            BccAddresses = recipients.Where(r => r.IsBcc).Select(r => r.Email).ToList(),
            Subject = _reportService.GetExpiryAlertSubject(data),
            HtmlBody = _reportService.RenderExpiryAlertEmail(data),
            ReportType = EmailReportType.ExpiryAlert,
            StoreId = storeId
        };

        return await _emailService.SendEmailAsync(message, cancellationToken);
    }
}
