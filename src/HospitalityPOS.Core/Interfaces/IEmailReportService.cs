using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for generating email report content and attachments.
/// </summary>
public interface IEmailReportService
{
    #region Daily Sales Report

    /// <summary>
    /// Generates daily sales summary data for a specific date and store.
    /// </summary>
    /// <param name="date">Report date.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily sales email data.</returns>
    Task<DailySalesEmailDataDto> GenerateDailySalesDataAsync(
        DateTime date,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the daily sales email HTML content.
    /// </summary>
    /// <param name="data">Daily sales data.</param>
    /// <returns>Rendered HTML string.</returns>
    string RenderDailySalesEmail(DailySalesEmailDataDto data);

    /// <summary>
    /// Gets the subject line for daily sales email.
    /// </summary>
    /// <param name="data">Daily sales data.</param>
    /// <param name="customSubject">Optional custom subject template.</param>
    /// <returns>Subject line.</returns>
    string GetDailySalesSubject(DailySalesEmailDataDto data, string? customSubject = null);

    #endregion

    #region Weekly Report

    /// <summary>
    /// Generates weekly performance data for a specific week.
    /// </summary>
    /// <param name="weekEndDate">Last day of the report week.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Weekly report email data.</returns>
    Task<WeeklyReportEmailDataDto> GenerateWeeklyReportDataAsync(
        DateTime weekEndDate,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the weekly report email HTML content.
    /// </summary>
    /// <param name="data">Weekly report data.</param>
    /// <returns>Rendered HTML string.</returns>
    string RenderWeeklyReportEmail(WeeklyReportEmailDataDto data);

    /// <summary>
    /// Gets the subject line for weekly report email.
    /// </summary>
    /// <param name="data">Weekly report data.</param>
    /// <param name="customSubject">Optional custom subject template.</param>
    /// <returns>Subject line.</returns>
    string GetWeeklyReportSubject(WeeklyReportEmailDataDto data, string? customSubject = null);

    /// <summary>
    /// Generates Excel attachment for weekly report.
    /// </summary>
    /// <param name="data">Weekly report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Excel file as byte array with filename.</returns>
    Task<(byte[] Content, string FileName)> GenerateWeeklyReportExcelAsync(
        WeeklyReportEmailDataDto data,
        CancellationToken cancellationToken = default);

    #endregion

    #region Low Stock Alert

    /// <summary>
    /// Generates low stock alert data.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Low stock alert email data.</returns>
    Task<LowStockAlertEmailDataDto> GenerateLowStockAlertDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the low stock alert email HTML content.
    /// </summary>
    /// <param name="data">Low stock alert data.</param>
    /// <returns>Rendered HTML string.</returns>
    string RenderLowStockAlertEmail(LowStockAlertEmailDataDto data);

    /// <summary>
    /// Gets the subject line for low stock alert email.
    /// </summary>
    /// <param name="data">Low stock alert data.</param>
    /// <param name="customSubject">Optional custom subject template.</param>
    /// <returns>Subject line.</returns>
    string GetLowStockAlertSubject(LowStockAlertEmailDataDto data, string? customSubject = null);

    #endregion

    #region Expiry Alert

    /// <summary>
    /// Generates expiry alert data.
    /// </summary>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expiry alert email data.</returns>
    Task<ExpiryAlertEmailDataDto> GenerateExpiryAlertDataAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the expiry alert email HTML content.
    /// </summary>
    /// <param name="data">Expiry alert data.</param>
    /// <returns>Rendered HTML string.</returns>
    string RenderExpiryAlertEmail(ExpiryAlertEmailDataDto data);

    /// <summary>
    /// Gets the subject line for expiry alert email.
    /// </summary>
    /// <param name="data">Expiry alert data.</param>
    /// <param name="customSubject">Optional custom subject template.</param>
    /// <returns>Subject line.</returns>
    string GetExpiryAlertSubject(ExpiryAlertEmailDataDto data, string? customSubject = null);

    #endregion

    #region Template Management

    /// <summary>
    /// Gets the default template for a report type.
    /// </summary>
    /// <param name="reportType">Report type.</param>
    /// <param name="storeId">Optional store ID for store-specific template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Template HTML or null if not found.</returns>
    Task<string?> GetTemplateAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a template with provided data dictionary.
    /// </summary>
    /// <param name="template">Template HTML with placeholders.</param>
    /// <param name="data">Data dictionary for placeholder replacement.</param>
    /// <returns>Rendered HTML.</returns>
    string RenderTemplate(string template, Dictionary<string, object?> data);

    #endregion

    #region Full Report Generation

    /// <summary>
    /// Generates a complete email message ready to send for a report type.
    /// </summary>
    /// <param name="reportType">Report type to generate.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete email message DTO.</returns>
    Task<EmailMessageDto?> GenerateReportEmailAsync(
        EmailReportType reportType,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    #endregion
}
