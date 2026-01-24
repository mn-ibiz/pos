using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for printing sales reports.
/// </summary>
public interface IReportPrintService
{
    /// <summary>
    /// Prints a sales report to the default printer.
    /// </summary>
    /// <param name="report">The sales report result to print.</param>
    /// <returns>True if the report was successfully sent to the printer.</returns>
    bool PrintSalesReport(SalesReportResult report);

    /// <summary>
    /// Generates a printable string representation of the sales report.
    /// </summary>
    /// <param name="report">The sales report result.</param>
    /// <returns>The formatted report string for printing.</returns>
    string GeneratePrintContent(SalesReportResult report);

    /// <summary>
    /// Prints a report asynchronously with the given content.
    /// </summary>
    /// <param name="content">The report content (text or HTML).</param>
    /// <param name="title">Optional title for the print job.</param>
    Task PrintReportAsync(string content, string? title = null);

    /// <summary>
    /// Exports report content to PDF file.
    /// </summary>
    /// <param name="content">The report content (HTML).</param>
    /// <param name="fileName">The base file name (without extension).</param>
    Task ExportToPdfAsync(string content, string fileName);

    /// <summary>
    /// Exports report content to CSV file.
    /// </summary>
    /// <param name="content">The CSV content.</param>
    /// <param name="fileName">The base file name (without extension).</param>
    Task ExportToCsvAsync(string content, string fileName);

    #region Inventory Reports

    /// <summary>
    /// Prints the current stock report.
    /// </summary>
    /// <param name="report">The current stock report result.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrintCurrentStockReportAsync(CurrentStockReportResult report);

    /// <summary>
    /// Prints the low stock alert report.
    /// </summary>
    /// <param name="report">The low stock report result.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrintLowStockReportAsync(LowStockReportResult report);

    /// <summary>
    /// Prints the stock movement report.
    /// </summary>
    /// <param name="report">The stock movement report result.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrintStockMovementReportAsync(StockMovementReportResult report);

    /// <summary>
    /// Prints the stock valuation report.
    /// </summary>
    /// <param name="report">The stock valuation report result.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrintStockValuationReportAsync(StockValuationReportResult report);

    /// <summary>
    /// Prints the dead stock report.
    /// </summary>
    /// <param name="report">The dead stock report result.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrintDeadStockReportAsync(DeadStockReportResult report);

    #endregion
}
