using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for label printer management and operations.
/// </summary>
public interface ILabelPrinterService
{
    #region Events

    /// <summary>
    /// Raised when a printer status changes.
    /// </summary>
    event EventHandler<LabelPrinterDto>? PrinterStatusChanged;

    /// <summary>
    /// Raised when a printer is successfully connected.
    /// </summary>
    event EventHandler<LabelPrinterDto>? PrinterConnected;

    /// <summary>
    /// Raised when a printer connection is lost.
    /// </summary>
    event EventHandler<LabelPrinterDto>? PrinterDisconnected;

    #endregion

    #region Printer CRUD

    /// <summary>
    /// Creates a new label printer configuration.
    /// </summary>
    Task<LabelPrinterDto> CreatePrinterAsync(CreateLabelPrinterDto dto);

    /// <summary>
    /// Gets a printer by ID.
    /// </summary>
    Task<LabelPrinterDto?> GetPrinterAsync(int printerId);

    /// <summary>
    /// Gets all printers for a store.
    /// </summary>
    Task<List<LabelPrinterDto>> GetAllPrintersAsync(int storeId);

    /// <summary>
    /// Updates an existing printer configuration.
    /// </summary>
    Task<LabelPrinterDto> UpdatePrinterAsync(int printerId, UpdateLabelPrinterDto dto);

    /// <summary>
    /// Deletes a printer (soft delete).
    /// </summary>
    Task<bool> DeletePrinterAsync(int printerId);

    #endregion

    #region Printer Operations

    /// <summary>
    /// Tests printer connection.
    /// </summary>
    Task<PrinterConnectionTestResultDto> TestPrinterConnectionAsync(int printerId);

    /// <summary>
    /// Prints a test label on the specified printer.
    /// </summary>
    Task<TestLabelResultDto> PrintTestLabelAsync(int printerId);

    /// <summary>
    /// Sets the default printer for a store.
    /// </summary>
    Task SetDefaultPrinterAsync(int printerId, int storeId);

    /// <summary>
    /// Gets the default printer for a store.
    /// </summary>
    Task<LabelPrinterDto?> GetDefaultPrinterAsync(int storeId);

    /// <summary>
    /// Gets the printer assigned to a category.
    /// </summary>
    Task<LabelPrinterDto?> GetPrinterForCategoryAsync(int categoryId, int storeId);

    /// <summary>
    /// Sends raw content to a printer.
    /// </summary>
    Task<bool> SendToPrinterAsync(int printerId, string content);

    #endregion

    #region Label Sizes

    /// <summary>
    /// Creates a new label size.
    /// </summary>
    Task<LabelSizeDto> CreateLabelSizeAsync(CreateLabelSizeDto dto);

    /// <summary>
    /// Gets all available label sizes.
    /// </summary>
    Task<List<LabelSizeDto>> GetAllLabelSizesAsync();

    /// <summary>
    /// Updates a label size.
    /// </summary>
    Task<LabelSizeDto> UpdateLabelSizeAsync(int sizeId, UpdateLabelSizeDto dto);

    /// <summary>
    /// Deletes a label size.
    /// </summary>
    Task<bool> DeleteLabelSizeAsync(int sizeId);

    #endregion

    #region Category Assignments

    /// <summary>
    /// Assigns a printer and template to a category.
    /// </summary>
    Task<CategoryPrinterAssignmentDto> AssignCategoryPrinterAsync(AssignCategoryPrinterDto dto, int storeId);

    /// <summary>
    /// Gets all category printer assignments for a store.
    /// </summary>
    Task<List<CategoryPrinterAssignmentDto>> GetCategoryAssignmentsAsync(int storeId);

    /// <summary>
    /// Removes a category printer assignment.
    /// </summary>
    Task<bool> RemoveCategoryAssignmentAsync(int assignmentId);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets printer usage statistics.
    /// </summary>
    Task<Dictionary<int, int>> GetPrinterUsageAsync(int storeId, DateTime from, DateTime to);

    #endregion
}
