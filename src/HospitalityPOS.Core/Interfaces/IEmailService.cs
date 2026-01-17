using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for email configuration and sending operations.
/// </summary>
public interface IEmailService
{
    #region Configuration Management

    /// <summary>
    /// Gets the email configuration for a store or global config if no store specified.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Email configuration DTO or null if not configured.</returns>
    Task<EmailConfigurationDto?> GetConfigurationAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves email configuration.
    /// </summary>
    /// <param name="configId">Configuration ID for update, null for create.</param>
    /// <param name="dto">Configuration data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved configuration.</returns>
    Task<EmailConfigurationDto> SaveConfigurationAsync(
        int? configId,
        SaveEmailConfigurationDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the SMTP connection with current configuration.
    /// </summary>
    /// <param name="configId">Configuration ID to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection test result.</returns>
    Task<ConnectionTestResultDto> TestConnectionAsync(
        int configId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests SMTP connection with provided settings without saving.
    /// </summary>
    /// <param name="dto">Configuration to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection test result.</returns>
    Task<ConnectionTestResultDto> TestConnectionAsync(
        SaveEmailConfigurationDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email is configured and connection is healthy.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if email is properly configured and working.</returns>
    Task<bool> IsConfiguredAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Recipient Management

    /// <summary>
    /// Gets all email recipients, optionally filtered by store.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recipients.</returns>
    Task<List<EmailRecipientDto>> GetRecipientsAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recipients for a specific report type.
    /// </summary>
    /// <param name="reportType">Report type.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recipients subscribed to this report type.</returns>
    Task<List<EmailRecipientDto>> GetRecipientsForReportAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recipient by ID.
    /// </summary>
    /// <param name="recipientId">Recipient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recipient DTO or null.</returns>
    Task<EmailRecipientDto?> GetRecipientAsync(
        int recipientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an email recipient.
    /// </summary>
    /// <param name="recipientId">Recipient ID for update, null for create.</param>
    /// <param name="dto">Recipient data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved recipient.</returns>
    Task<EmailRecipientDto> SaveRecipientAsync(
        int? recipientId,
        SaveEmailRecipientDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an email recipient (soft delete).
    /// </summary>
    /// <param name="recipientId">Recipient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteRecipientAsync(
        int recipientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    /// <param name="email">Email address to validate.</param>
    /// <returns>True if valid format.</returns>
    bool ValidateEmailFormat(string email);

    #endregion

    #region Schedule Management

    /// <summary>
    /// Gets all email schedules, optionally filtered by store.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of schedules.</returns>
    Task<List<EmailScheduleDto>> GetSchedulesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schedule by ID.
    /// </summary>
    /// <param name="scheduleId">Schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Schedule DTO or null.</returns>
    Task<EmailScheduleDto?> GetScheduleAsync(
        int scheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the schedule for a specific report type.
    /// </summary>
    /// <param name="reportType">Report type.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Schedule DTO or null.</returns>
    Task<EmailScheduleDto?> GetScheduleForReportAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an email schedule.
    /// </summary>
    /// <param name="scheduleId">Schedule ID for update, null for create.</param>
    /// <param name="dto">Schedule data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved schedule.</returns>
    Task<EmailScheduleDto> SaveScheduleAsync(
        int? scheduleId,
        SaveEmailScheduleDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a schedule.
    /// </summary>
    /// <param name="scheduleId">Schedule ID.</param>
    /// <param name="isEnabled">Enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated schedule.</returns>
    Task<EmailScheduleDto> SetScheduleEnabledAsync(
        int scheduleId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule (soft delete).
    /// </summary>
    /// <param name="scheduleId">Schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteScheduleAsync(
        int scheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules that are due to execute.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of due schedules.</returns>
    Task<List<EmailScheduleDto>> GetDueSchedulesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last executed time for a schedule.
    /// </summary>
    /// <param name="scheduleId">Schedule ID.</param>
    /// <param name="executedAt">Execution timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    Task UpdateScheduleExecutedAsync(
        int scheduleId,
        DateTime executedAt,
        CancellationToken cancellationToken = default);

    #endregion

    #region Email Sending

    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="message">Email message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<EmailSendResultDto> SendEmailAsync(
        EmailMessageDto message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test email to verify configuration.
    /// </summary>
    /// <param name="configId">Configuration ID.</param>
    /// <param name="toAddress">Test recipient address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<EmailSendResultDto> SendTestEmailAsync(
        int configId,
        string toAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries sending a failed email.
    /// </summary>
    /// <param name="emailLogId">Email log ID to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<EmailSendResultDto> RetryEmailAsync(
        int emailLogId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Email Logging

    /// <summary>
    /// Gets email logs with filtering and pagination.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated email logs.</returns>
    Task<EmailLogResultDto> GetEmailLogsAsync(
        EmailLogQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific email log by ID.
    /// </summary>
    /// <param name="logId">Log ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Email log DTO or null.</returns>
    Task<EmailLogDto?> GetEmailLogAsync(
        int logId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an email log entry.
    /// </summary>
    /// <param name="message">Email message.</param>
    /// <param name="status">Initial status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created log ID.</returns>
    Task<int> CreateEmailLogAsync(
        EmailMessageDto message,
        EmailSendStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates email log status.
    /// </summary>
    /// <param name="logId">Log ID.</param>
    /// <param name="status">New status.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    Task UpdateEmailLogStatusAsync(
        int logId,
        EmailSendStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending emails that need to be retried.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of email logs to retry.</returns>
    Task<List<EmailLogDto>> GetPendingRetriesAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Alert Configuration

    /// <summary>
    /// Gets low stock alert configuration.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert configuration.</returns>
    Task<LowStockAlertConfigDto?> GetLowStockAlertConfigAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves low stock alert configuration.
    /// </summary>
    /// <param name="configId">Config ID for update, null for create.</param>
    /// <param name="dto">Configuration data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved configuration.</returns>
    Task<LowStockAlertConfigDto> SaveLowStockAlertConfigAsync(
        int? configId,
        SaveLowStockAlertConfigDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expiry alert configuration.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert configuration.</returns>
    Task<ExpiryAlertConfigDto?> GetExpiryAlertConfigAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves expiry alert configuration.
    /// </summary>
    /// <param name="configId">Config ID for update, null for create.</param>
    /// <param name="dto">Configuration data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved configuration.</returns>
    Task<ExpiryAlertConfigDto> SaveExpiryAlertConfigAsync(
        int? configId,
        SaveExpiryAlertConfigDto dto,
        CancellationToken cancellationToken = default);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets email system dashboard summary.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard data.</returns>
    Task<EmailDashboardDto> GetDashboardAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Encryption

    /// <summary>
    /// Encrypts a string value for secure storage.
    /// </summary>
    /// <param name="plainText">Plain text to encrypt.</param>
    /// <returns>Encrypted string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="cipherText">Encrypted text.</param>
    /// <returns>Decrypted plain text.</returns>
    string Decrypt(string cipherText);

    #endregion
}
