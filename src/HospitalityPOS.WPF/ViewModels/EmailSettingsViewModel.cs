using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the email settings management screen.
/// </summary>
public partial class EmailSettingsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties - SMTP Configuration

    [ObservableProperty]
    private int? _configurationId;

    [ObservableProperty]
    private string _smtpHost = string.Empty;

    [ObservableProperty]
    private int _smtpPort = 587;

    [ObservableProperty]
    private string _smtpUsername = string.Empty;

    [ObservableProperty]
    private string _smtpPassword = string.Empty;

    [ObservableProperty]
    private string _fromEmail = string.Empty;

    [ObservableProperty]
    private string _fromName = string.Empty;

    [ObservableProperty]
    private bool _useSsl = true;

    [ObservableProperty]
    private bool _useStartTls = true;

    [ObservableProperty]
    private int _timeoutSeconds = 30;

    [ObservableProperty]
    private int _maxRetryAttempts = 3;

    [ObservableProperty]
    private bool _isSmtpEnabled = true;

    #endregion

    #region Observable Properties - Recipients

    [ObservableProperty]
    private ObservableCollection<EmailRecipientDto> _recipients = new();

    [ObservableProperty]
    private EmailRecipientDto? _selectedRecipient;

    [ObservableProperty]
    private string _newRecipientEmail = string.Empty;

    [ObservableProperty]
    private string _newRecipientName = string.Empty;

    [ObservableProperty]
    private bool _newRecipientIsCc;

    [ObservableProperty]
    private bool _newRecipientIsBcc;

    [ObservableProperty]
    private bool _newRecipientDailySales = true;

    [ObservableProperty]
    private bool _newRecipientWeeklyReport = true;

    [ObservableProperty]
    private bool _newRecipientLowStock = true;

    [ObservableProperty]
    private bool _newRecipientExpiry = true;

    #endregion

    #region Observable Properties - Schedules

    [ObservableProperty]
    private ObservableCollection<EmailScheduleDto> _schedules = new();

    [ObservableProperty]
    private EmailScheduleDto? _selectedSchedule;

    [ObservableProperty]
    private bool _dailySalesEnabled;

    [ObservableProperty]
    private TimeSpan _dailySalesTime = new TimeSpan(6, 0, 0);

    [ObservableProperty]
    private bool _weeklySalesEnabled;

    [ObservableProperty]
    private TimeSpan _weeklySalesTime = new TimeSpan(8, 0, 0);

    [ObservableProperty]
    private DayOfWeek _weeklySalesDay = DayOfWeek.Monday;

    [ObservableProperty]
    private bool _lowStockAlertEnabled;

    [ObservableProperty]
    private TimeSpan _lowStockAlertTime = new TimeSpan(9, 0, 0);

    [ObservableProperty]
    private bool _expiryAlertEnabled;

    [ObservableProperty]
    private TimeSpan _expiryAlertTime = new TimeSpan(9, 0, 0);

    [ObservableProperty]
    private string _selectedTimezone = "Africa/Nairobi";

    #endregion

    #region Observable Properties - Alert Configurations

    [ObservableProperty]
    private int _lowStockThreshold = 10;

    [ObservableProperty]
    private int _expiryDaysThreshold = 7;

    #endregion

    #region Observable Properties - Email Logs

    [ObservableProperty]
    private ObservableCollection<EmailLogDto> _emailLogs = new();

    [ObservableProperty]
    private EmailLogDto? _selectedLog;

    [ObservableProperty]
    private int _logPageNumber = 1;

    [ObservableProperty]
    private int _logPageSize = 20;

    [ObservableProperty]
    private int _totalLogCount;

    #endregion

    #region Observable Properties - UI State

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    [ObservableProperty]
    private bool _isStatusSuccess;

    [ObservableProperty]
    private string _selectedTab = "Configuration";

    [ObservableProperty]
    private ObservableCollection<string> _availableTimezones = new();

    [ObservableProperty]
    private ObservableCollection<DayOfWeek> _daysOfWeek = new();

    #endregion

    public EmailSettingsViewModel(
        IServiceScopeFactory scopeFactory,
        ILogger logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        Title = "Email Settings";

        // Initialize days of week
        DaysOfWeek = new ObservableCollection<DayOfWeek>(Enum.GetValues<DayOfWeek>());

        // Initialize common timezones
        AvailableTimezones = new ObservableCollection<string>(GetCommonTimezones());
    }

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel and loads data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Load configuration
            var config = await emailService.GetConfigurationAsync();
            if (config != null)
            {
                LoadConfiguration(config);
            }

            // Load recipients
            var recipients = await emailService.GetRecipientsAsync();
            Recipients = new ObservableCollection<EmailRecipientDto>(recipients);

            // Load schedules
            var schedules = await emailService.GetSchedulesAsync();
            Schedules = new ObservableCollection<EmailScheduleDto>(schedules);
            LoadScheduleSettings(schedules);

            // Load alert configs
            var lowStockConfig = await emailService.GetLowStockAlertConfigAsync();
            if (lowStockConfig != null)
            {
                LowStockThreshold = lowStockConfig.Threshold;
            }

            var expiryConfig = await emailService.GetExpiryAlertConfigAsync();
            if (expiryConfig != null)
            {
                ExpiryDaysThreshold = expiryConfig.DaysBeforeExpiry;
            }

            // Load email logs
            await LoadEmailLogsAsync();
        }, "Loading email settings...");
    }

    private void LoadConfiguration(EmailConfigurationDto config)
    {
        ConfigurationId = config.Id;
        SmtpHost = config.SmtpHost;
        SmtpPort = config.SmtpPort;
        SmtpUsername = config.SmtpUsername;
        SmtpPassword = config.SmtpPassword ?? string.Empty;
        FromEmail = config.FromEmail;
        FromName = config.FromName;
        UseSsl = config.UseSsl;
        UseStartTls = config.UseStartTls;
        TimeoutSeconds = config.TimeoutSeconds;
        MaxRetryAttempts = config.MaxRetryAttempts;
        IsSmtpEnabled = config.IsEnabled;
    }

    private void LoadScheduleSettings(List<EmailScheduleDto> schedules)
    {
        foreach (var schedule in schedules)
        {
            switch (schedule.ReportType)
            {
                case EmailReportType.DailySales:
                    DailySalesEnabled = schedule.IsEnabled;
                    DailySalesTime = schedule.SendTime;
                    SelectedTimezone = schedule.Timezone;
                    break;
                case EmailReportType.WeeklyReport:
                    WeeklySalesEnabled = schedule.IsEnabled;
                    WeeklySalesTime = schedule.SendTime;
                    WeeklySalesDay = schedule.DayOfWeek ?? DayOfWeek.Monday;
                    break;
                case EmailReportType.LowStockAlert:
                    LowStockAlertEnabled = schedule.IsEnabled;
                    LowStockAlertTime = schedule.SendTime;
                    break;
                case EmailReportType.ExpiryAlert:
                    ExpiryAlertEnabled = schedule.IsEnabled;
                    ExpiryAlertTime = schedule.SendTime;
                    break;
            }
        }
    }

    private async Task LoadEmailLogsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var logs = await emailService.GetEmailLogsAsync(LogPageNumber, LogPageSize);
        EmailLogs = new ObservableCollection<EmailLogDto>(logs);

        TotalLogCount = await emailService.GetEmailLogCountAsync();
    }

    private static List<string> GetCommonTimezones()
    {
        return new List<string>
        {
            "Africa/Nairobi",
            "Africa/Lagos",
            "Africa/Cairo",
            "Africa/Johannesburg",
            "Europe/London",
            "Europe/Paris",
            "America/New_York",
            "America/Los_Angeles",
            "Asia/Dubai",
            "Asia/Singapore",
            "Asia/Tokyo",
            "Australia/Sydney",
            "UTC"
        };
    }

    #endregion

    #region Tab Navigation

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        SelectedTab = tabName;
        ClearStatusMessage();
    }

    #endregion

    #region SMTP Configuration Commands

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(SmtpHost))
        {
            ShowStatusMessage("Please enter the SMTP host", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(FromEmail))
        {
            ShowStatusMessage("Please enter the sender email address", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var config = new EmailConfigurationDto
            {
                Id = ConfigurationId ?? 0,
                SmtpHost = SmtpHost,
                SmtpPort = SmtpPort,
                SmtpUsername = SmtpUsername,
                SmtpPassword = SmtpPassword,
                FromEmail = FromEmail,
                FromName = FromName,
                UseSsl = UseSsl,
                UseStartTls = UseStartTls,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetryAttempts = MaxRetryAttempts,
                IsEnabled = IsSmtpEnabled
            };

            await emailService.SaveConfigurationAsync(config);
            ConfigurationId = config.Id;

            ShowStatusMessage("Email configuration saved successfully!", true);
        }, "Saving configuration...");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (ConfigurationId == null || ConfigurationId == 0)
        {
            ShowStatusMessage("Please save configuration first before testing", false);
            return;
        }

        IsTesting = true;
        ClearStatusMessage();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var result = await emailService.TestConnectionAsync(ConfigurationId.Value);

            if (result.Success)
            {
                ShowStatusMessage($"Connection successful! Response: {result.ResponseTime}ms", true);
            }
            else
            {
                ShowStatusMessage($"Connection failed: {result.ErrorMessage}", false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing email connection");
            ShowStatusMessage($"Error: {ex.Message}", false);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task SendTestEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(SmtpUsername))
        {
            ShowStatusMessage("Please configure SMTP settings first", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var message = new EmailMessageDto
            {
                ToAddresses = new List<string> { SmtpUsername },
                Subject = "Test Email from Hospitality POS",
                HtmlBody = @"<html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Test Email</h2>
                        <p>This is a test email from Hospitality POS to verify your email configuration.</p>
                        <p>If you received this email, your SMTP settings are configured correctly!</p>
                        <hr/>
                        <p style='color: #666; font-size: 12px;'>Sent at: " + DateTime.Now.ToString("f") + @"</p>
                    </body>
                </html>",
                ReportType = EmailReportType.Custom
            };

            var result = await emailService.SendEmailAsync(message);

            if (result.Success)
            {
                ShowStatusMessage("Test email sent successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Failed to send test email: {result.ErrorMessage}", false);
            }
        }, "Sending test email...");
    }

    #endregion

    #region Recipient Commands

    [RelayCommand]
    private async Task AddRecipientAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRecipientEmail))
        {
            ShowStatusMessage("Please enter an email address", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var recipient = new EmailRecipientDto
            {
                Email = NewRecipientEmail.Trim(),
                Name = NewRecipientName.Trim(),
                IsCc = NewRecipientIsCc,
                IsBcc = NewRecipientIsBcc,
                ReceivesDailySales = NewRecipientDailySales,
                ReceivesWeeklyReport = NewRecipientWeeklyReport,
                ReceivesLowStockAlert = NewRecipientLowStock,
                ReceivesExpiryAlert = NewRecipientExpiry,
                IsActive = true
            };

            await emailService.SaveRecipientAsync(recipient);
            Recipients.Add(recipient);

            // Clear form
            NewRecipientEmail = string.Empty;
            NewRecipientName = string.Empty;
            NewRecipientIsCc = false;
            NewRecipientIsBcc = false;
            NewRecipientDailySales = true;
            NewRecipientWeeklyReport = true;
            NewRecipientLowStock = true;
            NewRecipientExpiry = true;

            ShowStatusMessage("Recipient added successfully!", true);
        }, "Adding recipient...");
    }

    [RelayCommand]
    private async Task UpdateRecipientAsync()
    {
        if (SelectedRecipient == null)
        {
            ShowStatusMessage("Please select a recipient first", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            await emailService.SaveRecipientAsync(SelectedRecipient);

            ShowStatusMessage("Recipient updated successfully!", true);
        }, "Updating recipient...");
    }

    [RelayCommand]
    private async Task DeleteRecipientAsync()
    {
        if (SelectedRecipient == null)
        {
            ShowStatusMessage("Please select a recipient first", false);
            return;
        }

        var confirmed = await DialogService.ShowConfirmAsync(
            "Delete Recipient",
            $"Are you sure you want to delete {SelectedRecipient.Email}?");

        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            await emailService.DeleteRecipientAsync(SelectedRecipient.Id);
            Recipients.Remove(SelectedRecipient);
            SelectedRecipient = null;

            ShowStatusMessage("Recipient deleted successfully!", true);
        }, "Deleting recipient...");
    }

    #endregion

    #region Schedule Commands

    [RelayCommand]
    private async Task SaveSchedulesAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Save daily sales schedule
            await SaveOrUpdateScheduleAsync(emailService, EmailReportType.DailySales, DailySalesEnabled, DailySalesTime, null);

            // Save weekly report schedule
            await SaveOrUpdateScheduleAsync(emailService, EmailReportType.WeeklyReport, WeeklySalesEnabled, WeeklySalesTime, WeeklySalesDay);

            // Save low stock alert schedule
            await SaveOrUpdateScheduleAsync(emailService, EmailReportType.LowStockAlert, LowStockAlertEnabled, LowStockAlertTime, null);

            // Save expiry alert schedule
            await SaveOrUpdateScheduleAsync(emailService, EmailReportType.ExpiryAlert, ExpiryAlertEnabled, ExpiryAlertTime, null);

            // Reload schedules
            var schedules = await emailService.GetSchedulesAsync();
            Schedules = new ObservableCollection<EmailScheduleDto>(schedules);

            ShowStatusMessage("Schedules saved successfully!", true);
        }, "Saving schedules...");
    }

    private async Task SaveOrUpdateScheduleAsync(
        IEmailService emailService,
        EmailReportType reportType,
        bool isEnabled,
        TimeSpan sendTime,
        DayOfWeek? dayOfWeek)
    {
        var existingSchedule = Schedules.FirstOrDefault(s => s.ReportType == reportType);

        var schedule = new EmailScheduleDto
        {
            Id = existingSchedule?.Id ?? 0,
            ReportType = reportType,
            IsEnabled = isEnabled,
            SendTime = sendTime,
            Timezone = SelectedTimezone,
            DayOfWeek = dayOfWeek,
            Frequency = reportType == EmailReportType.WeeklyReport
                ? EmailScheduleFrequency.Weekly
                : EmailScheduleFrequency.Daily
        };

        await emailService.SaveScheduleAsync(schedule);
    }

    #endregion

    #region Alert Configuration Commands

    [RelayCommand]
    private async Task SaveAlertConfigsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var lowStockConfig = new LowStockAlertConfigDto
            {
                Threshold = LowStockThreshold,
                IsEnabled = LowStockAlertEnabled
            };
            await emailService.SaveLowStockAlertConfigAsync(lowStockConfig);

            var expiryConfig = new ExpiryAlertConfigDto
            {
                DaysBeforeExpiry = ExpiryDaysThreshold,
                IsEnabled = ExpiryAlertEnabled
            };
            await emailService.SaveExpiryAlertConfigAsync(expiryConfig);

            ShowStatusMessage("Alert configurations saved successfully!", true);
        }, "Saving alert configurations...");
    }

    #endregion

    #region Email Log Commands

    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        await ExecuteAsync(async () =>
        {
            await LoadEmailLogsAsync();
            ShowStatusMessage("Email logs refreshed!", true);
        }, "Refreshing logs...");
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (LogPageNumber * LogPageSize < TotalLogCount)
        {
            LogPageNumber++;
            await LoadEmailLogsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (LogPageNumber > 1)
        {
            LogPageNumber--;
            await LoadEmailLogsAsync();
        }
    }

    [RelayCommand]
    private async Task RetryEmailAsync()
    {
        if (SelectedLog == null)
        {
            ShowStatusMessage("Please select an email to retry", false);
            return;
        }

        if (SelectedLog.Status != EmailSendStatus.Failed)
        {
            ShowStatusMessage("Only failed emails can be retried", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var result = await emailService.RetryEmailAsync(SelectedLog.Id);

            if (result.Success)
            {
                ShowStatusMessage("Email retry queued successfully!", true);
                await LoadEmailLogsAsync();
            }
            else
            {
                ShowStatusMessage($"Failed to retry email: {result.ErrorMessage}", false);
            }
        }, "Retrying email...");
    }

    #endregion

    #region Manual Report Trigger Commands

    [RelayCommand]
    private async Task TriggerDailySalesReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<EmailTriggerService>();

            var result = await triggerService.TriggerReportAsync(EmailReportType.DailySales);

            if (result.Success)
            {
                ShowStatusMessage("Daily sales report sent successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Failed to send report: {result.ErrorMessage}", false);
            }
        }, "Sending daily sales report...");
    }

    [RelayCommand]
    private async Task TriggerWeeklyReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<EmailTriggerService>();

            var result = await triggerService.TriggerReportAsync(EmailReportType.WeeklyReport);

            if (result.Success)
            {
                ShowStatusMessage("Weekly report sent successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Failed to send report: {result.ErrorMessage}", false);
            }
        }, "Sending weekly report...");
    }

    [RelayCommand]
    private async Task TriggerLowStockAlertAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<EmailTriggerService>();

            var result = await triggerService.TriggerReportAsync(EmailReportType.LowStockAlert);

            if (result.Success)
            {
                ShowStatusMessage("Low stock alert sent successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Failed to send alert: {result.ErrorMessage}", false);
            }
        }, "Sending low stock alert...");
    }

    [RelayCommand]
    private async Task TriggerExpiryAlertAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<EmailTriggerService>();

            var result = await triggerService.TriggerReportAsync(EmailReportType.ExpiryAlert);

            if (result.Success)
            {
                ShowStatusMessage("Expiry alert sent successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Failed to send alert: {result.ErrorMessage}", false);
            }
        }, "Sending expiry alert...");
    }

    #endregion

    #region Helper Methods

    private void ShowStatusMessage(string message, bool isSuccess)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        IsStatusSuccess = isSuccess;
    }

    private void ClearStatusMessage()
    {
        StatusMessage = string.Empty;
        HasStatusMessage = false;
        IsStatusSuccess = false;
    }

    #endregion
}
