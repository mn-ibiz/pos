using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing printer configurations.
/// </summary>
public interface IPrinterService
{
    /// <summary>
    /// Gets all printers of a specific type.
    /// </summary>
    /// <param name="type">The printer type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of printers.</returns>
    Task<List<Printer>> GetPrintersAsync(PrinterType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active printers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active printers.</returns>
    Task<List<Printer>> GetAllPrintersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a printer by ID including settings.
    /// </summary>
    /// <param name="id">The printer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The printer or null if not found.</returns>
    Task<Printer?> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default printer for a type.
    /// </summary>
    /// <param name="type">The printer type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default printer or null.</returns>
    Task<Printer?> GetDefaultPrinterAsync(PrinterType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a printer (create or update).
    /// </summary>
    /// <param name="printer">The printer to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved printer.</returns>
    Task<Printer> SavePrinterAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a printer as the default for its type.
    /// </summary>
    /// <param name="printerId">The printer ID to set as default.</param>
    /// <param name="type">The printer type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetDefaultPrinterAsync(int printerId, PrinterType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a printer (soft delete).
    /// </summary>
    /// <param name="id">The printer ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeletePrinterAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the printer connection and prints a test page.
    /// </summary>
    /// <param name="printer">The printer to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    Task<PrintTestResult> TestPrintAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a printer.
    /// </summary>
    /// <param name="printer">The printer to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The status result.</returns>
    Task<PrinterStatusResult> CheckPrinterStatusAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all receipt templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of receipt templates.</returns>
    Task<List<ReceiptTemplate>> GetReceiptTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a receipt template by ID.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template or null.</returns>
    Task<ReceiptTemplate?> GetReceiptTemplateByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default receipt template.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default template or null.</returns>
    Task<ReceiptTemplate?> GetDefaultReceiptTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a receipt template (create or update).
    /// </summary>
    /// <param name="template">The template to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved template.</returns>
    Task<ReceiptTemplate> SaveReceiptTemplateAsync(ReceiptTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a template as the default.
    /// </summary>
    /// <param name="templateId">The template ID to set as default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetDefaultReceiptTemplateAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a receipt template (soft delete).
    /// </summary>
    /// <param name="id">The template ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteReceiptTemplateAsync(int id, CancellationToken cancellationToken = default);

    #region Kitchen Printer Methods

    /// <summary>
    /// Gets category mappings for a kitchen printer.
    /// </summary>
    /// <param name="printerId">The printer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of category mappings.</returns>
    Task<List<PrinterCategoryMapping>> GetCategoryMappingsAsync(int printerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves category mappings for a kitchen printer.
    /// </summary>
    /// <param name="printerId">The printer ID.</param>
    /// <param name="categoryIds">List of category IDs to map.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveCategoryMappingsAsync(int printerId, List<int> categoryIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KOT settings for a kitchen printer.
    /// </summary>
    /// <param name="printerId">The printer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The KOT settings or null.</returns>
    Task<KOTSettings?> GetKOTSettingsAsync(int printerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves KOT settings for a kitchen printer.
    /// </summary>
    /// <param name="kotSettings">The KOT settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveKOTSettingsAsync(KOTSettings kotSettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints a test KOT to a kitchen printer.
    /// </summary>
    /// <param name="printer">The printer to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    Task<PrintTestResult> PrintTestKOTAsync(Printer printer, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service for discovering printers on the system and network.
/// </summary>
public interface IPrinterDiscoveryService
{
    /// <summary>
    /// Gets the list of installed Windows printers.
    /// </summary>
    /// <returns>List of printer names.</returns>
    Task<List<string>> GetWindowsPrintersAsync();

    /// <summary>
    /// Gets the list of available serial/COM ports.
    /// </summary>
    /// <returns>List of port names.</returns>
    Task<List<string>> GetSerialPortsAsync();

    /// <summary>
    /// Discovers network printers on the local network.
    /// </summary>
    /// <returns>List of discovered printers.</returns>
    Task<List<DiscoveredPrinter>> DiscoverNetworkPrintersAsync();

    /// <summary>
    /// Tests the connection to a printer.
    /// </summary>
    /// <param name="printer">The printer to test.</param>
    /// <returns>True if connection successful.</returns>
    Task<bool> TestConnectionAsync(Printer printer);
}
