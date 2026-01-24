using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for exporting X-Reports and Z-Reports to various formats.
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// Exports an X-Report to the specified format.
    /// </summary>
    /// <param name="report">The X-Report data to export.</param>
    /// <param name="format">The export format (PDF, Excel, CSV).</param>
    /// <param name="filePath">Optional specific file path. If null, opens save dialog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the exported file, or null if cancelled.</returns>
    Task<string?> ExportXReportAsync(
        XReportData report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a Z-Report to the specified format.
    /// </summary>
    /// <param name="report">The Z-Report data to export.</param>
    /// <param name="format">The export format (PDF, Excel, CSV).</param>
    /// <param name="filePath">Optional specific file path. If null, opens save dialog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the exported file, or null if cancelled.</returns>
    Task<string?> ExportZReportAsync(
        ZReport report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a Combined X-Report to the specified format.
    /// </summary>
    /// <param name="report">The Combined X-Report data to export.</param>
    /// <param name="format">The export format (PDF, Excel, CSV).</param>
    /// <param name="filePath">Optional specific file path. If null, opens save dialog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the exported file, or null if cancelled.</returns>
    Task<string?> ExportCombinedXReportAsync(
        CombinedXReportData report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a Combined Z-Report preview to the specified format.
    /// </summary>
    /// <param name="report">The Combined Z-Report preview data to export.</param>
    /// <param name="format">The export format (PDF, Excel, CSV).</param>
    /// <param name="filePath">Optional specific file path. If null, opens save dialog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the exported file, or null if cancelled.</returns>
    Task<string?> ExportCombinedZReportAsync(
        CombinedZReportPreview report,
        ExportFormat format,
        string? filePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the suggested file name for an X-Report export.
    /// </summary>
    /// <param name="report">The X-Report.</param>
    /// <param name="format">The export format.</param>
    /// <returns>A suggested file name.</returns>
    string GetXReportFileName(XReportData report, ExportFormat format);

    /// <summary>
    /// Gets the suggested file name for a Z-Report export.
    /// </summary>
    /// <param name="report">The Z-Report.</param>
    /// <param name="format">The export format.</param>
    /// <returns>A suggested file name.</returns>
    string GetZReportFileName(ZReport report, ExportFormat format);
}
