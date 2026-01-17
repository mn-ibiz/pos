using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for batch and expiry tracking operations.
/// </summary>
public interface IProductBatchService
{
    #region Batch Management

    /// <summary>
    /// Creates a new product batch during goods receiving.
    /// </summary>
    Task<ProductBatchDto> CreateBatchAsync(CreateProductBatchDto dto, int userId);

    /// <summary>
    /// Creates multiple batches from goods receiving entry.
    /// </summary>
    Task<List<ProductBatchDto>> CreateBatchesFromReceivingAsync(int grnId, List<BatchReceivingEntryDto> entries, int storeId, int supplierId, int userId);

    /// <summary>
    /// Gets a batch by ID.
    /// </summary>
    Task<ProductBatchDto?> GetBatchAsync(int batchId);

    /// <summary>
    /// Gets a batch by batch number and product.
    /// </summary>
    Task<ProductBatchDto?> GetBatchByNumberAsync(string batchNumber, int productId, int storeId);

    /// <summary>
    /// Gets all batches for a product.
    /// </summary>
    Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId, int? storeId = null);

    /// <summary>
    /// Gets batches based on query parameters.
    /// </summary>
    Task<List<ProductBatchDto>> GetBatchesAsync(BatchQueryDto query);

    /// <summary>
    /// Gets available batches for selection during sale or transfer.
    /// </summary>
    Task<List<BatchSelectionDto>> GetAvailableBatchesAsync(int productId, int storeId, bool includeExpired = false);

    /// <summary>
    /// Gets batch summary for a product.
    /// </summary>
    Task<ProductBatchSummaryDto> GetProductBatchSummaryAsync(int productId, int? storeId = null);

    /// <summary>
    /// Updates batch status.
    /// </summary>
    Task<ProductBatchDto> UpdateBatchStatusAsync(int batchId, BatchStatus status, int userId);

    #endregion

    #region Batch Configuration

    /// <summary>
    /// Gets batch configuration for a product.
    /// </summary>
    Task<ProductBatchConfigurationDto?> GetBatchConfigurationAsync(int productId);

    /// <summary>
    /// Creates or updates batch configuration for a product.
    /// </summary>
    Task<ProductBatchConfigurationDto> SaveBatchConfigurationAsync(UpdateProductBatchConfigurationDto dto, int userId);

    /// <summary>
    /// Gets all products with batch tracking enabled.
    /// </summary>
    Task<List<ProductBatchConfigurationDto>> GetBatchTrackingProductsAsync();

    /// <summary>
    /// Checks if a product requires batch tracking.
    /// </summary>
    Task<bool> RequiresBatchTrackingAsync(int productId);

    /// <summary>
    /// Checks if a product requires expiry date.
    /// </summary>
    Task<bool> RequiresExpiryDateAsync(int productId);

    #endregion

    #region Expiry Validation

    /// <summary>
    /// Validates expiry date during goods receiving.
    /// </summary>
    Task<ShelfLifeValidationDto> ValidateShelfLifeAsync(int productId, DateTime expiryDate);

    /// <summary>
    /// Validates batch for sale.
    /// </summary>
    Task<ExpiryValidationResultDto> ValidateBatchForSaleAsync(int batchId);

    /// <summary>
    /// Validates product availability for sale including expiry checks.
    /// </summary>
    Task<BatchAvailabilityDto> CheckBatchAvailabilityAsync(int productId, int storeId, int quantity);

    #endregion

    #region Batch Allocation

    /// <summary>
    /// Allocates batches for a sale using FIFO/FEFO strategy.
    /// </summary>
    Task<BatchAllocationResultDto> AllocateBatchesAsync(AllocateBatchesRequestDto request);

    /// <summary>
    /// Reserves quantity from specific batches.
    /// </summary>
    Task ReserveBatchQuantityAsync(int batchId, int quantity, int userId);

    /// <summary>
    /// Releases reserved quantity from batches.
    /// </summary>
    Task ReleaseBatchQuantityAsync(int batchId, int quantity, int userId);

    /// <summary>
    /// Deducts quantity from batch (after sale confirmation).
    /// </summary>
    Task DeductBatchQuantityAsync(int batchId, int quantity, string referenceType, int referenceId, string? referenceNumber, int userId);

    #endregion

    #region Batch Movements

    /// <summary>
    /// Records a batch stock movement.
    /// </summary>
    Task<BatchStockMovementDto> RecordMovementAsync(RecordBatchMovementDto dto, int userId);

    /// <summary>
    /// Gets movement history for a batch.
    /// </summary>
    Task<List<BatchStockMovementDto>> GetBatchMovementsAsync(int batchId);

    /// <summary>
    /// Gets movements by reference.
    /// </summary>
    Task<List<BatchStockMovementDto>> GetMovementsByReferenceAsync(string referenceType, int referenceId);

    #endregion

    #region Batch Disposal

    /// <summary>
    /// Creates a disposal record for expired or damaged batch.
    /// </summary>
    Task<BatchDisposalDto> CreateDisposalAsync(CreateBatchDisposalDto dto, int userId);

    /// <summary>
    /// Gets disposal records for a batch.
    /// </summary>
    Task<List<BatchDisposalDto>> GetBatchDisposalsAsync(int batchId);

    /// <summary>
    /// Gets disposal records for a store.
    /// </summary>
    Task<List<BatchDisposalDto>> GetStoreDisposalsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion

    #region Expiry Monitoring

    /// <summary>
    /// Gets batches expiring within specified days.
    /// </summary>
    Task<List<ProductBatchDto>> GetExpiringBatchesAsync(int days, int? storeId = null);

    /// <summary>
    /// Gets expired batches.
    /// </summary>
    Task<List<ProductBatchDto>> GetExpiredBatchesAsync(int? storeId = null);

    /// <summary>
    /// Gets batches in critical expiry range.
    /// </summary>
    Task<List<ProductBatchDto>> GetCriticalExpiryBatchesAsync(int? storeId = null);

    /// <summary>
    /// Updates batch statuses based on expiry dates.
    /// </summary>
    Task UpdateExpiryStatusesAsync();

    #endregion

    #region Expiry Dashboard

    /// <summary>
    /// Gets the expiry alert dashboard with grouped batches by expiry timeline.
    /// </summary>
    Task<ExpiryDashboardDto> GetExpiryDashboardAsync(ExpiryDashboardQueryDto? query = null);

    /// <summary>
    /// Gets expiry alerts for notification.
    /// </summary>
    Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int? storeId = null, ExpiryAlertSeverity? minSeverity = null);

    /// <summary>
    /// Gets suggested actions for a batch based on expiry status.
    /// </summary>
    Task<List<SuggestedAction>> GetSuggestedActionsAsync(int batchId);

    /// <summary>
    /// Acknowledges an expiry alert.
    /// </summary>
    Task AcknowledgeAlertAsync(int batchId, int userId);

    /// <summary>
    /// Gets export data for expiring/expired batches.
    /// </summary>
    Task<List<ExpiryExportDto>> GetExpiryExportDataAsync(ExpiryDashboardQueryDto? query = null);

    /// <summary>
    /// Gets count of batches requiring action by severity.
    /// </summary>
    Task<ExpiryDashboardSummaryDto> GetExpirySummaryAsync(int? storeId = null);

    #endregion
}
