using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for batch traceability and recall management.
/// </summary>
public interface IBatchTraceabilityService
{
    #region Batch Search

    /// <summary>
    /// Searches for batches matching the query criteria.
    /// </summary>
    /// <param name="query">Search parameters.</param>
    /// <returns>List of matching batches.</returns>
    Task<List<BatchSearchResultDto>> SearchBatchesAsync(BatchSearchQueryDto query);

    /// <summary>
    /// Searches for batches by batch number.
    /// </summary>
    /// <param name="batchNumber">The batch number to search.</param>
    /// <param name="productId">Optional product ID filter.</param>
    /// <returns>List of matching batches.</returns>
    Task<List<BatchSearchResultDto>> SearchByBatchNumberAsync(string batchNumber, int? productId = null);

    /// <summary>
    /// Gets batches for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="includeExpired">Whether to include expired batches.</param>
    /// <returns>List of batches.</returns>
    Task<List<BatchSearchResultDto>> GetProductBatchesAsync(int productId, bool includeExpired = true);

    #endregion

    #region Traceability Report

    /// <summary>
    /// Gets full traceability report for a batch.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <returns>Full traceability report.</returns>
    Task<BatchTraceabilityReportDto?> GetTraceabilityReportAsync(int batchId);

    /// <summary>
    /// Gets movement history for a batch.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <returns>List of movement details.</returns>
    Task<List<BatchMovementDetailDto>> GetBatchMovementsAsync(int batchId);

    /// <summary>
    /// Gets sale transactions containing a batch.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <returns>List of sale transactions.</returns>
    Task<List<BatchSaleTransactionDto>> GetBatchSaleTransactionsAsync(int batchId);

    /// <summary>
    /// Gets current stock locations for a batch.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <returns>List of locations with quantities.</returns>
    Task<List<BatchLocationDto>> GetBatchLocationsAsync(int batchId);

    #endregion

    #region Recall Management

    /// <summary>
    /// Creates a recall alert for a batch.
    /// </summary>
    /// <param name="dto">Recall alert details.</param>
    /// <param name="userId">User creating the recall.</param>
    /// <returns>The created recall alert.</returns>
    Task<BatchRecallAlertDto> CreateRecallAlertAsync(CreateBatchRecallAlertDto dto, int userId);

    /// <summary>
    /// Gets a recall alert by ID.
    /// </summary>
    /// <param name="recallId">The recall alert ID.</param>
    /// <returns>The recall alert details.</returns>
    Task<BatchRecallAlertDto?> GetRecallAlertAsync(int recallId);

    /// <summary>
    /// Gets recall alerts matching the query.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of recall alerts.</returns>
    Task<List<BatchRecallAlertDto>> GetRecallAlertsAsync(RecallQueryDto query);

    /// <summary>
    /// Updates recall status.
    /// </summary>
    /// <param name="dto">Status update details.</param>
    /// <param name="userId">User updating the recall.</param>
    /// <returns>Updated recall alert.</returns>
    Task<BatchRecallAlertDto> UpdateRecallStatusAsync(UpdateRecallStatusDto dto, int userId);

    /// <summary>
    /// Records an action taken for a recall.
    /// </summary>
    /// <param name="dto">Action details.</param>
    /// <param name="userId">User performing the action.</param>
    /// <returns>The recorded action.</returns>
    Task<RecallActionDto> RecordRecallActionAsync(CreateRecallActionDto dto, int userId);

    /// <summary>
    /// Gets actions for a recall.
    /// </summary>
    /// <param name="recallId">The recall alert ID.</param>
    /// <returns>List of actions.</returns>
    Task<List<RecallActionDto>> GetRecallActionsAsync(int recallId);

    /// <summary>
    /// Gets recall summary statistics.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <returns>Recall summary.</returns>
    Task<RecallSummaryDto> GetRecallSummaryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets active recalls for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of active recalls affecting the store.</returns>
    Task<List<BatchRecallAlertDto>> GetActiveRecallsForStoreAsync(int storeId);

    /// <summary>
    /// Checks if a batch has an active recall.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <returns>True if batch has active recall.</returns>
    Task<bool> HasActiveRecallAsync(int batchId);

    /// <summary>
    /// Quarantines a batch due to recall.
    /// </summary>
    /// <param name="batchId">The batch ID.</param>
    /// <param name="recallId">The recall alert ID.</param>
    /// <param name="userId">User performing the quarantine.</param>
    Task QuarantineBatchAsync(int batchId, int recallId, int userId);

    #endregion
}
