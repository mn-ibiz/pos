using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for EmailService.
/// </summary>
public class EmailServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger> _mockLogger;
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger>();

        _mockConfiguration.Setup(c => c["Encryption:Key"])
            .Returns("TestEncryptionKey12345678901234");

        _service = new EmailService(_context, _mockConfiguration.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(null!, _mockConfiguration.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(_context, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(_context, _mockConfiguration.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.ke", true)]
    [InlineData("user+tag@example.org", true)]
    [InlineData("invalid", false)]
    [InlineData("invalid@", false)]
    [InlineData("@invalid.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ValidateEmailFormat_ReturnsExpectedResult(string? email, bool expected)
    {
        // Act
        var result = _service.ValidateEmailFormat(email!);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Encryption Tests

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalValue()
    {
        // Arrange
        var originalValue = "TestPassword123!";

        // Act
        var encrypted = _service.Encrypt(originalValue);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(originalValue);
        decrypted.Should().Be(originalValue);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _service.Encrypt(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _service.Decrypt(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task GetConfigurationAsync_NoConfig_ReturnsNull()
    {
        // Act
        var result = await _service.GetConfigurationAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveConfigurationAsync_NewConfig_CreatesConfiguration()
    {
        // Arrange
        var dto = new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUsername = "user@test.com",
            SmtpPassword = "password123",
            FromAddress = "noreply@test.com",
            FromName = "Test System",
            UseSsl = true
        };

        // Act
        var result = await _service.SaveConfigurationAsync(null, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.SmtpHost.Should().Be("smtp.test.com");
        result.SmtpPort.Should().Be(587);
        result.SmtpUsername.Should().Be("user@test.com");
        result.HasPassword.Should().BeTrue();
        result.FromAddress.Should().Be("noreply@test.com");
        result.FromName.Should().Be("Test System");
        result.UseSsl.Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigurationAsync_AfterSave_ReturnsConfig()
    {
        // Arrange
        var dto = new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromAddress = "noreply@test.com",
            FromName = "Test System"
        };
        await _service.SaveConfigurationAsync(null, dto);

        // Act
        var result = await _service.GetConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result!.SmtpHost.Should().Be("smtp.test.com");
    }

    [Fact]
    public async Task SaveConfigurationAsync_UpdateExisting_UpdatesConfiguration()
    {
        // Arrange
        var createDto = new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromAddress = "noreply@test.com",
            FromName = "Test System"
        };
        var created = await _service.SaveConfigurationAsync(null, createDto);

        var updateDto = new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.updated.com",
            SmtpPort = 465,
            FromAddress = "updated@test.com",
            FromName = "Updated System"
        };

        // Act
        var result = await _service.SaveConfigurationAsync(created.Id, updateDto);

        // Assert
        result.SmtpHost.Should().Be("smtp.updated.com");
        result.SmtpPort.Should().Be(465);
        result.FromAddress.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task SaveConfigurationAsync_DuplicateGlobalConfig_ThrowsException()
    {
        // Arrange
        var dto = new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromAddress = "noreply@test.com",
            FromName = "Test System"
        };
        await _service.SaveConfigurationAsync(null, dto);

        // Act & Assert
        var act = () => _service.SaveConfigurationAsync(null, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task IsConfiguredAsync_NotConfigured_ReturnsFalse()
    {
        // Act
        var result = await _service.IsConfiguredAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Recipient Tests

    [Fact]
    public async Task GetRecipientsAsync_NoRecipients_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetRecipientsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveRecipientAsync_NewRecipient_CreatesRecipient()
    {
        // Arrange
        var dto = new SaveEmailRecipientDto
        {
            Email = "test@example.com",
            Name = "Test User",
            ReceiveDailySales = true,
            ReceiveWeeklyReport = true,
            ReceiveLowStockAlerts = false,
            ReceiveExpiryAlerts = false
        };

        // Act
        var result = await _service.SaveRecipientAsync(null, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.ReceiveDailySales.Should().BeTrue();
        result.ReceiveWeeklyReport.Should().BeTrue();
        result.ReceiveLowStockAlerts.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRecipientAsync_InvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var dto = new SaveEmailRecipientDto
        {
            Email = "invalid-email",
            Name = "Test User"
        };

        // Act & Assert
        var act = () => _service.SaveRecipientAsync(null, dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("Email");
    }

    [Fact]
    public async Task SaveRecipientAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var dto = new SaveEmailRecipientDto
        {
            Email = "test@example.com",
            Name = "Test User"
        };
        await _service.SaveRecipientAsync(null, dto);

        // Act & Assert
        var act = () => _service.SaveRecipientAsync(null, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task GetRecipientsForReportAsync_FiltersCorrectly()
    {
        // Arrange
        await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "daily@example.com",
            ReceiveDailySales = true,
            ReceiveWeeklyReport = false
        });
        await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "weekly@example.com",
            ReceiveDailySales = false,
            ReceiveWeeklyReport = true
        });
        await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "both@example.com",
            ReceiveDailySales = true,
            ReceiveWeeklyReport = true
        });

        // Act
        var dailyRecipients = await _service.GetRecipientsForReportAsync(EmailReportType.DailySales);
        var weeklyRecipients = await _service.GetRecipientsForReportAsync(EmailReportType.WeeklyReport);

        // Assert
        dailyRecipients.Should().HaveCount(2);
        dailyRecipients.Should().Contain(r => r.Email == "daily@example.com");
        dailyRecipients.Should().Contain(r => r.Email == "both@example.com");

        weeklyRecipients.Should().HaveCount(2);
        weeklyRecipients.Should().Contain(r => r.Email == "weekly@example.com");
        weeklyRecipients.Should().Contain(r => r.Email == "both@example.com");
    }

    [Fact]
    public async Task DeleteRecipientAsync_ExistingRecipient_SoftDeletes()
    {
        // Arrange
        var created = await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "test@example.com"
        });

        // Act
        var result = await _service.DeleteRecipientAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var recipients = await _service.GetRecipientsAsync();
        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRecipientAsync_NonExistent_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteRecipientAsync(9999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Schedule Tests

    [Fact]
    public async Task GetSchedulesAsync_NoSchedules_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetSchedulesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveScheduleAsync_NewSchedule_CreatesSchedule()
    {
        // Arrange
        var dto = new SaveEmailScheduleDto
        {
            ReportType = EmailReportType.DailySales,
            IsEnabled = true,
            SendTime = new TimeOnly(20, 0),
            TimeZone = "Africa/Nairobi"
        };

        // Act
        var result = await _service.SaveScheduleAsync(null, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ReportType.Should().Be(EmailReportType.DailySales);
        result.IsEnabled.Should().BeTrue();
        result.SendTime.Should().Be(new TimeOnly(20, 0));
    }

    [Fact]
    public async Task SaveScheduleAsync_DuplicateSchedule_ThrowsException()
    {
        // Arrange
        var dto = new SaveEmailScheduleDto
        {
            ReportType = EmailReportType.DailySales,
            IsEnabled = true,
            SendTime = new TimeOnly(20, 0)
        };
        await _service.SaveScheduleAsync(null, dto);

        // Act & Assert
        var act = () => _service.SaveScheduleAsync(null, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task SetScheduleEnabledAsync_TogglesEnabled()
    {
        // Arrange
        var schedule = await _service.SaveScheduleAsync(null, new SaveEmailScheduleDto
        {
            ReportType = EmailReportType.DailySales,
            IsEnabled = true
        });

        // Act
        var disabled = await _service.SetScheduleEnabledAsync(schedule.Id, false);
        var enabled = await _service.SetScheduleEnabledAsync(schedule.Id, true);

        // Assert
        disabled.IsEnabled.Should().BeFalse();
        enabled.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteScheduleAsync_ExistingSchedule_SoftDeletes()
    {
        // Arrange
        var created = await _service.SaveScheduleAsync(null, new SaveEmailScheduleDto
        {
            ReportType = EmailReportType.DailySales
        });

        // Act
        var result = await _service.DeleteScheduleAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var schedules = await _service.GetSchedulesAsync();
        schedules.Should().BeEmpty();
    }

    #endregion

    #region Email Log Tests

    [Fact]
    public async Task GetEmailLogsAsync_NoLogs_ReturnsEmptyResult()
    {
        // Act
        var result = await _service.GetEmailLogsAsync(new EmailLogQueryDto());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateEmailLogAsync_CreatesLog()
    {
        // Arrange
        var message = new EmailMessageDto
        {
            ToAddresses = new List<string> { "test@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<html><body>Test</body></html>",
            ReportType = EmailReportType.DailySales
        };

        // Act
        var logId = await _service.CreateEmailLogAsync(message, EmailSendStatus.Pending);

        // Assert
        logId.Should().BeGreaterThan(0);

        var log = await _service.GetEmailLogAsync(logId);
        log.Should().NotBeNull();
        log!.Subject.Should().Be("Test Subject");
        log.Status.Should().Be(EmailSendStatus.Pending);
    }

    [Fact]
    public async Task UpdateEmailLogStatusAsync_UpdatesStatus()
    {
        // Arrange
        var message = new EmailMessageDto
        {
            ToAddresses = new List<string> { "test@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<html><body>Test</body></html>",
            ReportType = EmailReportType.DailySales
        };
        var logId = await _service.CreateEmailLogAsync(message, EmailSendStatus.Pending);

        // Act
        await _service.UpdateEmailLogStatusAsync(logId, EmailSendStatus.Sent);

        // Assert
        var log = await _service.GetEmailLogAsync(logId);
        log!.Status.Should().Be(EmailSendStatus.Sent);
        log.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEmailLogsAsync_WithFilters_FiltersCorrectly()
    {
        // Arrange
        await _service.CreateEmailLogAsync(new EmailMessageDto
        {
            ToAddresses = new List<string> { "test1@example.com" },
            Subject = "Daily Report",
            HtmlBody = "<html></html>",
            ReportType = EmailReportType.DailySales
        }, EmailSendStatus.Sent);

        await _service.CreateEmailLogAsync(new EmailMessageDto
        {
            ToAddresses = new List<string> { "test2@example.com" },
            Subject = "Weekly Report",
            HtmlBody = "<html></html>",
            ReportType = EmailReportType.WeeklyReport
        }, EmailSendStatus.Failed);

        // Act
        var dailyLogs = await _service.GetEmailLogsAsync(new EmailLogQueryDto
        {
            ReportType = EmailReportType.DailySales
        });
        var failedLogs = await _service.GetEmailLogsAsync(new EmailLogQueryDto
        {
            Status = EmailSendStatus.Failed
        });

        // Assert
        dailyLogs.TotalCount.Should().Be(1);
        dailyLogs.Items[0].ReportType.Should().Be(EmailReportType.DailySales);

        failedLogs.TotalCount.Should().Be(1);
        failedLogs.Items[0].Status.Should().Be(EmailSendStatus.Failed);
    }

    #endregion

    #region Alert Configuration Tests

    [Fact]
    public async Task GetLowStockAlertConfigAsync_NoConfig_ReturnsNull()
    {
        // Act
        var result = await _service.GetLowStockAlertConfigAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveLowStockAlertConfigAsync_CreatesConfig()
    {
        // Arrange
        var dto = new SaveLowStockAlertConfigDto
        {
            IsEnabled = true,
            AlertFrequency = EmailScheduleFrequency.Daily,
            ThresholdPercent = 50,
            MinimumItemsForAlert = 5,
            MaxItemsPerEmail = 100
        };

        // Act
        var result = await _service.SaveLowStockAlertConfigAsync(null, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.IsEnabled.Should().BeTrue();
        result.ThresholdPercent.Should().Be(50);
    }

    [Fact]
    public async Task GetExpiryAlertConfigAsync_NoConfig_ReturnsNull()
    {
        // Act
        var result = await _service.GetExpiryAlertConfigAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveExpiryAlertConfigAsync_CreatesConfig()
    {
        // Arrange
        var dto = new SaveExpiryAlertConfigDto
        {
            IsEnabled = true,
            AlertFrequency = EmailScheduleFrequency.Daily,
            AlertThresholdDays = 14,
            UrgentThresholdDays = 3,
            MaxItemsPerEmail = 50
        };

        // Act
        var result = await _service.SaveExpiryAlertConfigAsync(null, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.IsEnabled.Should().BeTrue();
        result.AlertThresholdDays.Should().Be(14);
        result.UrgentThresholdDays.Should().Be(3);
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboardAsync_NoData_ReturnsEmptyDashboard()
    {
        // Act
        var result = await _service.GetDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsConfigured.Should().BeFalse();
        result.ConnectionHealthy.Should().BeFalse();
        result.TotalRecipients.Should().Be(0);
        result.ActiveSchedules.Should().Be(0);
        result.EmailsSentToday.Should().Be(0);
        result.EmailsFailedToday.Should().Be(0);
        result.PendingEmails.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardAsync_WithData_ReturnsDashboardStats()
    {
        // Arrange
        await _service.SaveConfigurationAsync(null, new SaveEmailConfigurationDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromAddress = "noreply@test.com",
            FromName = "Test"
        });

        await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "recipient1@test.com"
        });
        await _service.SaveRecipientAsync(null, new SaveEmailRecipientDto
        {
            Email = "recipient2@test.com"
        });

        await _service.SaveScheduleAsync(null, new SaveEmailScheduleDto
        {
            ReportType = EmailReportType.DailySales,
            IsEnabled = true
        });

        // Act
        var result = await _service.GetDashboardAsync();

        // Assert
        result.IsConfigured.Should().BeTrue();
        result.TotalRecipients.Should().Be(2);
        result.ActiveSchedules.Should().Be(1);
    }

    #endregion
}
