using System.Data;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for exporting data to various file formats.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports a collection of data to CSV format.
    /// </summary>
    /// <typeparam name="T">The type of data to export.</typeparam>
    /// <param name="data">The collection of data to export.</param>
    /// <param name="filePath">The path where the CSV file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a DataTable to CSV format.
    /// </summary>
    /// <param name="dataTable">The DataTable to export.</param>
    /// <param name="filePath">The path where the CSV file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportToCsvAsync(DataTable dataTable, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a collection of data to PDF format.
    /// </summary>
    /// <typeparam name="T">The type of data to export.</typeparam>
    /// <param name="data">The collection of data to export.</param>
    /// <param name="filePath">The path where the PDF file will be saved.</param>
    /// <param name="reportTitle">The title to display in the PDF header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if export was successful, false otherwise.</returns>
    Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string reportTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a standardized filename for exports.
    /// </summary>
    /// <param name="reportType">The type/name of the report.</param>
    /// <param name="startDate">The start date of the report period.</param>
    /// <param name="endDate">Optional end date of the report period.</param>
    /// <param name="extension">The file extension (default: csv).</param>
    /// <returns>A standardized filename.</returns>
    string GenerateFilename(string reportType, DateTime startDate, DateTime? endDate = null, string extension = "csv");

    /// <summary>
    /// Gets the default export directory.
    /// </summary>
    /// <returns>The path to the default export directory.</returns>
    string GetDefaultExportDirectory();

    /// <summary>
    /// Exports a collection of data to Excel format.
    /// Opens a save dialog for the user to choose the location.
    /// </summary>
    /// <typeparam name="T">The type of data to export.</typeparam>
    /// <param name="data">The collection of data to export.</param>
    /// <param name="defaultFileName">The default file name (without extension).</param>
    /// <param name="sheetName">The name for the Excel worksheet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if export was successful, false if cancelled or failed.</returns>
    Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string defaultFileName, string sheetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a DataTable to Excel format.
    /// Opens a save dialog for the user to choose the location.
    /// </summary>
    /// <param name="dataTable">The DataTable to export.</param>
    /// <param name="defaultFileName">The default file name (without extension).</param>
    /// <param name="sheetName">The name for the Excel worksheet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if export was successful, false if cancelled or failed.</returns>
    Task<bool> ExportToExcelAsync(DataTable dataTable, string defaultFileName, string sheetName, CancellationToken cancellationToken = default);
}
