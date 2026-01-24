using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for generating X-Reports (mid-shift examination reports).
/// X-Reports are non-destructive and can be generated multiple times during a shift.
/// </summary>
public interface IXReportService
{
    /// <summary>
    /// Generates an X-Report for a terminal's current work period.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated X-Report data.</returns>
    Task<XReportData> GenerateXReportAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an X-Report for a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated X-Report data.</returns>
    Task<XReportData> GenerateXReportForWorkPeriodAsync(
        int workPeriodId,
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an X-Report filtered by specific cashier.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The user ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated X-Report data.</returns>
    Task<XReportData> GenerateXReportForCashierAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of X-Reports generated for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of X-Report records.</returns>
    Task<IReadOnlyList<XReportRecord>> GetXReportHistoryAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of X-Reports for a specific terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="startDate">Start date for the history.</param>
    /// <param name="endDate">End date for the history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of X-Report records.</returns>
    Task<IReadOnlyList<XReportRecord>> GetXReportHistoryByTerminalAsync(
        int terminalId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an X-Report record to history.
    /// </summary>
    /// <param name="report">The X-Report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved X-Report record.</returns>
    Task<XReportRecord> SaveXReportAsync(
        XReportData report,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next report number for X-Reports.
    /// Format: X-YYYY-TID-NNNN (e.g., X-2024-001-0042)
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated report number.</returns>
    Task<string> GenerateReportNumberAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports an X-Report to the specified format.
    /// </summary>
    /// <param name="report">The X-Report data.</param>
    /// <param name="format">The export format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exported report as bytes.</returns>
    Task<byte[]> ExportXReportAsync(
        XReportData report,
        ReportExportFormat format,
        CancellationToken cancellationToken = default);
}
