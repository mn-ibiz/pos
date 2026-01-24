using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for Z Report (end-of-day) generation, management, and scheduling.
/// Z Reports are immutable fiscal documents that serve as official accounting records.
/// </summary>
public interface IZReportService
{
    #region Preview & Validation

    /// <summary>
    /// Generates a preview of the Z Report before finalization.
    /// </summary>
    /// <param name="workPeriodId">The work period to preview.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview data including sales summary, validation issues, and expected values.</returns>
    Task<ZReportPreview> PreviewZReportAsync(int workPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Generates a preview of the Z Report for a specific terminal.
    /// </summary>
    /// <param name="workPeriodId">The work period to preview.</param>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview data including sales summary, validation issues, and expected values.</returns>
    Task<ZReportPreview> PreviewZReportForTerminalAsync(int workPeriodId, int terminalId, CancellationToken ct = default);

    /// <summary>
    /// Validates whether a Z Report can be generated for the specified work period.
    /// </summary>
    /// <param name="workPeriodId">The work period to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with any blocking or warning issues.</returns>
    Task<ZReportValidationResult> ValidateCanGenerateAsync(int workPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Validates whether a Z Report can be generated for a specific terminal's work period.
    /// </summary>
    /// <param name="workPeriodId">The work period to validate.</param>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with any blocking or warning issues.</returns>
    Task<ZReportValidationResult> ValidateCanGenerateForTerminalAsync(int workPeriodId, int terminalId, CancellationToken ct = default);

    #endregion

    #region Generation

    /// <summary>
    /// Generates and finalizes a Z Report for the specified work period.
    /// This action is permanent - the report becomes immutable once generated.
    /// </summary>
    /// <param name="workPeriodId">The work period to close and report on.</param>
    /// <param name="actualCashCounted">The actual cash amount counted in the drawer.</param>
    /// <param name="generatedByUserId">The user generating the report.</param>
    /// <param name="varianceExplanation">Optional explanation for any cash variance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated and finalized Z Report record.</returns>
    Task<ZReportRecord> GenerateZReportAsync(
        int workPeriodId,
        decimal actualCashCounted,
        int generatedByUserId,
        string? varianceExplanation = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates and finalizes a Z Report for a specific terminal's work period.
    /// This action is permanent - the report becomes immutable once generated.
    /// </summary>
    /// <param name="workPeriodId">The work period to close and report on.</param>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="actualCashCounted">The actual cash amount counted in the drawer.</param>
    /// <param name="generatedByUserId">The user generating the report.</param>
    /// <param name="varianceExplanation">Optional explanation for any cash variance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated and finalized Z Report record.</returns>
    Task<ZReportRecord> GenerateZReportForTerminalAsync(
        int workPeriodId,
        int terminalId,
        decimal actualCashCounted,
        int generatedByUserId,
        string? varianceExplanation = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a consolidated Z Report across multiple terminals for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="reportDate">The date to consolidate.</param>
    /// <param name="generatedByUserId">The user generating the report.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The consolidated Z Report record.</returns>
    Task<ZReportRecord> GenerateConsolidatedZReportAsync(
        int storeId,
        DateTime reportDate,
        int generatedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Re-generates report data from a Z Report record (for reprinting/re-exporting).
    /// Note: This does not modify the original report - it just reconstructs the display data.
    /// </summary>
    /// <param name="reportId">The Z Report record ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The reconstructed report model for display/export.</returns>
    Task<Models.Reports.ZReport> ReconstructReportModelAsync(int reportId, CancellationToken ct = default);

    #endregion

    #region Retrieval

    /// <summary>
    /// Gets a Z Report by its ID.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Z Report record or null if not found.</returns>
    Task<ZReportRecord?> GetZReportAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets a Z Report by its sequential report number and store.
    /// </summary>
    /// <param name="reportNumber">The sequential report number.</param>
    /// <param name="storeId">The store ID (null for default store).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Z Report record or null if not found.</returns>
    Task<ZReportRecord?> GetZReportByNumberAsync(int reportNumber, int? storeId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a Z Report for a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Z Report record or null if not found.</returns>
    Task<ZReportRecord?> GetZReportByWorkPeriodAsync(int workPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Gets Z Reports matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching Z Report records.</returns>
    Task<IReadOnlyList<ZReportRecord>> GetZReportsAsync(ZReportFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Gets Z Report summaries for list views.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of Z Report summaries.</returns>
    Task<IReadOnlyList<ZReportSummaryDto>> GetZReportSummariesAsync(ZReportFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent Z Report for a store/terminal.
    /// </summary>
    /// <param name="storeId">The store ID (null for default).</param>
    /// <param name="terminalId">The terminal ID (null for any terminal).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The most recent Z Report or null.</returns>
    Task<ZReportRecord?> GetMostRecentZReportAsync(int? storeId = null, int? terminalId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the next sequential report number for a store.
    /// </summary>
    /// <param name="storeId">The store ID (null for default).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next report number.</returns>
    Task<int> GetNextReportNumberAsync(int? storeId = null, CancellationToken ct = default);

    /// <summary>
    /// Generates a formatted report number for Z Reports.
    /// Format: Z-YYYY-TID-NNNN (e.g., Z-2024-001-0042).
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="sequentialNumber">The sequential report number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The formatted report number string.</returns>
    Task<string> GenerateFormattedReportNumberAsync(int terminalId, int sequentialNumber, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of Z Reports matching the filter.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Total count.</returns>
    Task<int> GetZReportCountAsync(ZReportFilterDto filter, CancellationToken ct = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports a Z Report to PDF format.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF file as byte array.</returns>
    Task<byte[]> ExportToPdfAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Exports a Z Report to Excel format.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Excel file as byte array.</returns>
    Task<byte[]> ExportToExcelAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Exports a Z Report to CSV format.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>CSV file as byte array.</returns>
    Task<byte[]> ExportToCsvAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Generates HTML content for a Z Report (for display or printing).
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTML content as string.</returns>
    Task<string> GenerateHtmlReportAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Generates receipt-formatted content for thermal printer.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Receipt-formatted text content.</returns>
    Task<string> GenerateReceiptFormatAsync(int reportId, CancellationToken ct = default);

    #endregion

    #region Printing & Email

    /// <summary>
    /// Prints a Z Report to the specified printer.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="printerName">The printer name (null for default printer).</param>
    /// <param name="ct">Cancellation token.</param>
    Task PrintZReportAsync(int reportId, string? printerName = null, CancellationToken ct = default);

    /// <summary>
    /// Emails a Z Report to specified recipients.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="recipients">Email recipients.</param>
    /// <param name="subject">Email subject (null for default).</param>
    /// <param name="message">Optional message body.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EmailZReportAsync(
        int reportId,
        string[] recipients,
        string? subject = null,
        string? message = null,
        CancellationToken ct = default);

    #endregion

    #region Scheduling

    /// <summary>
    /// Gets the Z Report schedule for a store/terminal.
    /// </summary>
    /// <param name="storeId">The store ID (null for default).</param>
    /// <param name="terminalId">The terminal ID (null for store-level schedule).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The schedule configuration or null if not configured.</returns>
    Task<ZReportSchedule?> GetScheduleAsync(int? storeId = null, int? terminalId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the Z Report schedule for a store/terminal.
    /// </summary>
    /// <param name="storeId">The store ID (null for default).</param>
    /// <param name="schedule">The schedule configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated schedule.</returns>
    Task<ZReportSchedule> UpdateScheduleAsync(int? storeId, ZReportScheduleDto schedule, CancellationToken ct = default);

    /// <summary>
    /// Triggers scheduled Z Report generation for all due schedules.
    /// Called by background service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of reports generated.</returns>
    Task<int> TriggerScheduledZReportsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends warning notifications for schedules about to execute.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SendScheduleWarningNotificationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all active schedules.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of active schedules.</returns>
    Task<IReadOnlyList<ZReportSchedule>> GetActiveSchedulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default);

    #endregion

    #region Variance Management

    /// <summary>
    /// Gets variance threshold configuration for a store.
    /// </summary>
    /// <param name="storeId">The store ID (null for global settings).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Variance threshold configuration.</returns>
    Task<ZReportVarianceThreshold?> GetVarianceThresholdAsync(int? storeId = null, CancellationToken ct = default);

    /// <summary>
    /// Updates variance threshold configuration.
    /// </summary>
    /// <param name="threshold">The threshold configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated threshold.</returns>
    Task<ZReportVarianceThreshold> UpdateVarianceThresholdAsync(ZReportVarianceThreshold threshold, CancellationToken ct = default);

    /// <summary>
    /// Approves a variance for a Z Report.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="approverUserId">The approving manager's user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ApproveVarianceAsync(int reportId, int approverUserId, CancellationToken ct = default);

    /// <summary>
    /// Gets Z Reports with unapproved variances.
    /// </summary>
    /// <param name="storeId">The store ID (null for all stores).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of reports with unapproved variances.</returns>
    Task<IReadOnlyList<ZReportRecord>> GetReportsWithUnapprovedVariancesAsync(int? storeId = null, CancellationToken ct = default);

    #endregion

    #region Integrity & Audit

    /// <summary>
    /// Verifies the integrity of a Z Report by checking its hash.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if integrity is verified, false if compromised.</returns>
    Task<bool> VerifyReportIntegrityAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Verifies integrity of all Z Reports in a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of report IDs that failed integrity check.</returns>
    Task<IReadOnlyList<int>> VerifyReportIntegrityBatchAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>
    /// Gets audit log entries related to a Z Report.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of audit log entries.</returns>
    Task<IReadOnlyList<AuditLog>> GetReportAuditLogAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Checks for gaps in sequential report numbering.
    /// </summary>
    /// <param name="storeId">The store ID (null for default).</param>
    /// <param name="startDate">Start date for check.</param>
    /// <param name="endDate">End date for check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of missing report numbers.</returns>
    Task<IReadOnlyList<int>> CheckForSequenceGapsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    #endregion

    #region Statistics & Analytics

    /// <summary>
    /// Gets variance statistics over a date range.
    /// </summary>
    /// <param name="storeId">The store ID (null for all stores).</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Variance statistics.</returns>
    Task<VarianceStatistics> GetVarianceStatisticsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Gets daily sales totals from Z Reports over a date range.
    /// </summary>
    /// <param name="storeId">The store ID (null for all stores).</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Daily sales totals.</returns>
    Task<IReadOnlyList<DailySalesTotalDto>> GetDailySalesTotalsAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    #endregion
}

#region Supporting DTOs

/// <summary>
/// Statistics about cash variances.
/// </summary>
public class VarianceStatistics
{
    public int TotalReports { get; set; }
    public int ReportsWithVariance { get; set; }
    public decimal TotalVarianceAmount { get; set; }
    public decimal AverageVariance { get; set; }
    public decimal MaxShortage { get; set; }
    public decimal MaxOverage { get; set; }
    public int ShortageCount { get; set; }
    public int OverageCount { get; set; }
    public int ExactCount { get; set; }
    public decimal VariancePercentage { get; set; }
}

/// <summary>
/// Daily sales total from Z Reports.
/// </summary>
public class DailySalesTotalDto
{
    public DateTime Date { get; set; }
    public int ReportCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal TipsCollected { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal CashVariance { get; set; }
}

#endregion
