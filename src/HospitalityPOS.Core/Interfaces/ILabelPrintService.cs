using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for label printing operations.
/// </summary>
public interface ILabelPrintService
{
    #region Events

    /// <summary>
    /// Raised when a print job starts.
    /// </summary>
    event EventHandler<LabelPrintJobDto>? JobStarted;

    /// <summary>
    /// Raised when a print job completes.
    /// </summary>
    event EventHandler<LabelPrintJobDto>? JobCompleted;

    /// <summary>
    /// Raised when a print job fails.
    /// </summary>
    event EventHandler<LabelPrintJobDto>? JobFailed;

    /// <summary>
    /// Raised when print job progress updates.
    /// </summary>
    event EventHandler<LabelPrintJobDto>? JobProgressUpdated;

    /// <summary>
    /// Raised when a label is printed.
    /// </summary>
    event EventHandler<LabelPrintJobItemDto>? LabelPrinted;

    #endregion

    #region Single Label Printing

    /// <summary>
    /// Prints a single product label.
    /// </summary>
    Task<bool> PrintSingleLabelAsync(PrintSingleLabelRequestDto request, int storeId, int userId);

    /// <summary>
    /// Prints a label with custom data.
    /// </summary>
    Task<bool> PrintCustomLabelAsync(int templateId, int printerId, ProductLabelDataDto data, int copies = 1);

    #endregion

    #region Batch Printing

    /// <summary>
    /// Prints labels for multiple products.
    /// </summary>
    Task<LabelPrintJobDto> PrintBatchLabelsAsync(LabelBatchRequestDto request, int storeId, int userId);

    /// <summary>
    /// Prints labels for all products with price changes since a date.
    /// </summary>
    Task<LabelPrintJobDto> PrintPriceChangeLabelsAsync(PrintPriceChangeLabelsRequestDto request, int storeId, int userId);

    /// <summary>
    /// Prints labels for all products in a category.
    /// </summary>
    Task<LabelPrintJobDto> PrintCategoryLabelsAsync(PrintCategoryLabelsRequestDto request, int storeId, int userId);

    /// <summary>
    /// Prints labels for new products since a date.
    /// </summary>
    Task<LabelPrintJobDto> PrintNewProductLabelsAsync(DateTime since, int storeId, int userId, int? templateId = null, int? printerId = null);

    #endregion

    #region Job Management

    /// <summary>
    /// Gets a print job by ID.
    /// </summary>
    Task<LabelPrintJobDto?> GetPrintJobAsync(int jobId);

    /// <summary>
    /// Gets the current status of a print job.
    /// </summary>
    Task<LabelPrintJobDto?> GetPrintJobStatusAsync(int jobId);

    /// <summary>
    /// Gets print job history.
    /// </summary>
    Task<List<LabelPrintJobDto>> GetPrintJobHistoryAsync(GetPrintJobHistoryRequestDto request, int storeId);

    /// <summary>
    /// Gets active (in-progress) print jobs.
    /// </summary>
    Task<List<LabelPrintJobDto>> GetActiveJobsAsync(int storeId);

    /// <summary>
    /// Cancels a print job.
    /// </summary>
    Task<bool> CancelJobAsync(int jobId, int userId);

    /// <summary>
    /// Retries failed items in a job.
    /// </summary>
    Task<LabelPrintJobDto> RetryFailedItemsAsync(int jobId, int userId);

    /// <summary>
    /// Gets failed items from a job.
    /// </summary>
    Task<List<LabelPrintJobItemDto>> GetFailedItemsAsync(int jobId);

    #endregion

    #region Product Label Data

    /// <summary>
    /// Gets label data for a product.
    /// </summary>
    Task<ProductLabelDataDto?> GetProductLabelDataAsync(int productId);

    /// <summary>
    /// Gets label data for multiple products.
    /// </summary>
    Task<List<ProductLabelDataDto>> GetProductsLabelDataAsync(List<int> productIds);

    /// <summary>
    /// Gets products with price changes since a date.
    /// </summary>
    Task<List<ProductLabelDataDto>> GetPriceChangedProductsAsync(DateTime since, int storeId, int? categoryId = null);

    /// <summary>
    /// Gets products in a category for label printing.
    /// </summary>
    Task<List<ProductLabelDataDto>> GetCategoryProductsAsync(int categoryId, bool includeSubcategories = false);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets label printing statistics for a store.
    /// </summary>
    Task<LabelPrintingStatisticsDto> GetStatisticsAsync(int storeId);

    /// <summary>
    /// Gets daily label counts for a date range.
    /// </summary>
    Task<Dictionary<DateTime, int>> GetDailyLabelCountsAsync(int storeId, DateTime from, DateTime to);

    #endregion
}
