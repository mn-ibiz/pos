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
}
