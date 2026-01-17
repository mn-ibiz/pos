using HospitalityPOS.Core.Printing;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for low-level printer communication.
/// </summary>
public interface IPrinterCommunicationService
{
    /// <summary>
    /// Sends raw data to a printer.
    /// </summary>
    /// <param name="printerName">The Windows printer name.</param>
    /// <param name="data">The raw data to send.</param>
    /// <param name="documentName">Optional document name for spooler.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> SendRawDataAsync(string printerName, byte[] data, string documentName = "POS Document");

    /// <summary>
    /// Sends raw data to a printer synchronously.
    /// </summary>
    bool SendRawData(string printerName, byte[] data, string documentName = "POS Document");

    /// <summary>
    /// Gets the status of a printer.
    /// </summary>
    /// <param name="printerName">The Windows printer name.</param>
    /// <returns>Printer status information.</returns>
    Task<PrinterStatus> GetPrinterStatusAsync(string printerName);

    /// <summary>
    /// Gets the status of a printer synchronously.
    /// </summary>
    PrinterStatus GetPrinterStatus(string printerName);

    /// <summary>
    /// Checks if a printer is available and ready.
    /// </summary>
    /// <param name="printerName">The Windows printer name.</param>
    /// <returns>True if printer is ready.</returns>
    Task<bool> IsPrinterReadyAsync(string printerName);

    /// <summary>
    /// Sends a test print to verify printer connectivity.
    /// </summary>
    /// <param name="printerName">The Windows printer name.</param>
    /// <returns>Print result.</returns>
    Task<PrintResult> SendTestPrintAsync(string printerName);

    /// <summary>
    /// Opens the cash drawer connected to a printer.
    /// </summary>
    /// <param name="printerName">The printer name (cash drawer connected via printer).</param>
    /// <param name="pin">Drawer pin (0 for pin 2, 1 for pin 5).</param>
    /// <returns>True if command sent successfully.</returns>
    Task<bool> OpenCashDrawerAsync(string printerName, int pin = 0);

    /// <summary>
    /// Sends a beep command to the printer.
    /// </summary>
    /// <param name="printerName">The printer name.</param>
    /// <param name="times">Number of beeps.</param>
    /// <param name="duration">Duration per beep.</param>
    Task<bool> BeepAsync(string printerName, int times = 1, int duration = 2);
}
