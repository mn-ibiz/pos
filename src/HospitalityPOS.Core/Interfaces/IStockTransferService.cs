using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing stock transfers between stores and warehouses.
/// </summary>
public interface IStockTransferService
{
    #region Transfer Request Management

    /// <summary>
    /// Creates a new transfer request.
    /// </summary>
    Task<StockTransferRequestDto> CreateTransferRequestAsync(CreateTransferRequestDto dto, int userId);

    /// <summary>
    /// Gets a transfer request by ID.
    /// </summary>
    Task<StockTransferRequestDto?> GetTransferRequestAsync(int requestId);

    /// <summary>
    /// Gets a transfer request by request number.
    /// </summary>
    Task<StockTransferRequestDto?> GetTransferRequestByNumberAsync(string requestNumber);

    /// <summary>
    /// Updates a draft transfer request.
    /// </summary>
    Task<StockTransferRequestDto> UpdateTransferRequestAsync(int requestId, UpdateTransferRequestDto dto, int userId);

    /// <summary>
    /// Deletes a draft transfer request.
    /// </summary>
    Task<bool> DeleteTransferRequestAsync(int requestId, int userId);

    /// <summary>
    /// Adds a line to a transfer request.
    /// </summary>
    Task<TransferRequestLineDto> AddRequestLineAsync(int requestId, CreateTransferRequestLineDto dto);

    /// <summary>
    /// Updates a request line.
    /// </summary>
    Task<TransferRequestLineDto> UpdateRequestLineAsync(int lineId, int quantity, string? notes);

    /// <summary>
    /// Removes a line from a request.
    /// </summary>
    Task<bool> RemoveRequestLineAsync(int lineId);

    /// <summary>
    /// Submits a draft request for approval.
    /// </summary>
    Task<StockTransferRequestDto> SubmitRequestAsync(int requestId, int userId);

    /// <summary>
    /// Cancels a transfer request.
    /// </summary>
    Task<StockTransferRequestDto> CancelRequestAsync(int requestId, int userId, string? reason = null);

    /// <summary>
    /// Gets transfer requests based on query.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetTransferRequestsAsync(TransferRequestQueryDto query);

    /// <summary>
    /// Gets pending approval requests for a source location.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetPendingApprovalsAsync(int sourceLocationId);

    /// <summary>
    /// Gets requests for a requesting store.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetStoreRequestsAsync(int storeId, TransferRequestStatus? status = null);

    #endregion

    #region Source Location and Stock

    /// <summary>
    /// Gets available source locations for a store.
    /// </summary>
    Task<List<SourceLocationDto>> GetSourceLocationsAsync(int requestingStoreId);

    /// <summary>
    /// Gets available stock at a source location.
    /// </summary>
    Task<List<SourceProductStockDto>> GetSourceStockAsync(int sourceLocationId, string? searchTerm = null);

    /// <summary>
    /// Gets stock for a specific product at source location.
    /// </summary>
    Task<SourceProductStockDto?> GetProductStockAtSourceAsync(int sourceLocationId, int productId);

    /// <summary>
    /// Gets products with low stock at destination that are available at source.
    /// </summary>
    Task<List<SourceProductStockDto>> GetSuggestedTransferProductsAsync(int requestingStoreId, int sourceLocationId);

    #endregion

    #region Approval Operations

    /// <summary>
    /// Approves a transfer request (full or partial).
    /// </summary>
    Task<StockTransferRequestDto> ApproveRequestAsync(ApproveTransferRequestDto dto, int userId);

    /// <summary>
    /// Rejects a transfer request.
    /// </summary>
    Task<StockTransferRequestDto> RejectRequestAsync(RejectTransferRequestDto dto, int userId);

    /// <summary>
    /// Gets requests awaiting approval.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetRequestsAwaitingApprovalAsync();

    #endregion

    #region Shipment Operations

    /// <summary>
    /// Creates a shipment for an approved request.
    /// </summary>
    Task<StockTransferShipmentDto> CreateShipmentAsync(CreateShipmentDto dto, int userId);

    /// <summary>
    /// Gets shipment by ID.
    /// </summary>
    Task<StockTransferShipmentDto?> GetShipmentAsync(int shipmentId);

    /// <summary>
    /// Gets shipment for a request.
    /// </summary>
    Task<StockTransferShipmentDto?> GetRequestShipmentAsync(int requestId);

    /// <summary>
    /// Updates shipment details.
    /// </summary>
    Task<StockTransferShipmentDto> UpdateShipmentAsync(int shipmentId, CreateShipmentDto dto);

    /// <summary>
    /// Dispatches a shipment (deducts stock from source and updates status to InTransit).
    /// </summary>
    Task<StockTransferShipmentDto> DispatchShipmentAsync(int shipmentId, int userId);

    /// <summary>
    /// Gets shipments in transit.
    /// </summary>
    Task<List<StockTransferShipmentDto>> GetShipmentsInTransitAsync(int? destinationStoreId = null);

    /// <summary>
    /// Gets approved requests awaiting shipment.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetRequestsAwaitingShipmentAsync(int? sourceLocationId = null);

    #endregion

    #region Pick List Operations

    /// <summary>
    /// Generates a pick list for an approved transfer request.
    /// </summary>
    Task<PickListDto> GetPickListAsync(int requestId);

    /// <summary>
    /// Confirms picked items for a transfer request.
    /// </summary>
    Task<PickListDto> ConfirmPicksAsync(ConfirmAllPicksDto dto, int userId);

    /// <summary>
    /// Gets pending pick lists for a source location.
    /// </summary>
    Task<List<PickListDto>> GetPendingPickListsAsync(int? sourceLocationId = null);

    #endregion

    #region Transfer Document Operations

    /// <summary>
    /// Generates a transfer document (delivery note/packing slip) for a shipment.
    /// </summary>
    Task<TransferDocumentDto> GenerateTransferDocumentAsync(int shipmentId);

    /// <summary>
    /// Generates a transfer document for a dispatched request.
    /// </summary>
    Task<TransferDocumentDto?> GetTransferDocumentForRequestAsync(int requestId);

    #endregion

    #region Receipt Operations

    /// <summary>
    /// Creates a receipt for received transfer.
    /// </summary>
    Task<StockTransferReceiptDto> CreateReceiptAsync(CreateReceiptDto dto, int userId);

    /// <summary>
    /// Gets receipt by ID.
    /// </summary>
    Task<StockTransferReceiptDto?> GetReceiptAsync(int receiptId);

    /// <summary>
    /// Gets receipt for a request.
    /// </summary>
    Task<StockTransferReceiptDto?> GetRequestReceiptAsync(int requestId);

    /// <summary>
    /// Gets transfers awaiting receipt.
    /// </summary>
    Task<List<TransferRequestSummaryDto>> GetTransfersAwaitingReceiptAsync(int? storeId = null);

    /// <summary>
    /// Gets pending shipments queue with details for receiving.
    /// </summary>
    Task<List<PendingReceiptDto>> GetPendingReceiptsAsync(int? storeId = null);

    /// <summary>
    /// Gets receiving summary with variance details.
    /// </summary>
    Task<ReceivingSummaryDto> GetReceivingSummaryAsync(int receiptId);

    /// <summary>
    /// Completes the receipt and updates inventory.
    /// </summary>
    Task<StockTransferReceiptDto> CompleteReceiptAsync(int receiptId, int userId);

    /// <summary>
    /// Gets receipts with variances requiring investigation.
    /// </summary>
    Task<List<VarianceInvestigationDto>> GetReceiptsWithVarianceAsync(int? storeId = null);

    #endregion

    #region Issue Management

    /// <summary>
    /// Logs an issue for a receipt line.
    /// </summary>
    Task<TransferReceiptIssueDto> LogIssueAsync(int receiptLineId, CreateReceiptIssueDto dto);

    /// <summary>
    /// Resolves an issue.
    /// </summary>
    Task<TransferReceiptIssueDto> ResolveIssueAsync(ResolveIssueDto dto, int userId);

    /// <summary>
    /// Gets unresolved issues.
    /// </summary>
    Task<List<TransferReceiptIssueDto>> GetUnresolvedIssuesAsync(int? storeId = null);

    /// <summary>
    /// Gets issues for a receipt.
    /// </summary>
    Task<List<TransferReceiptIssueDto>> GetReceiptIssuesAsync(int receiptId);

    #endregion

    #region Activity Logging

    /// <summary>
    /// Gets activity log for a request.
    /// </summary>
    Task<List<TransferActivityLogDto>> GetActivityLogAsync(int requestId);

    /// <summary>
    /// Logs an activity.
    /// </summary>
    Task LogActivityAsync(int requestId, string activity, int userId, TransferRequestStatus? previousStatus = null, TransferRequestStatus? newStatus = null, string? details = null);

    #endregion

    #region Dashboard and Reporting

    /// <summary>
    /// Gets transfer dashboard for a store.
    /// </summary>
    Task<StoreTransferDashboardDto> GetStoreDashboardAsync(int storeId);

    /// <summary>
    /// Gets chain-wide transfer dashboard.
    /// </summary>
    Task<ChainTransferDashboardDto> GetChainDashboardAsync();

    /// <summary>
    /// Gets transfer statistics.
    /// </summary>
    Task<TransferStatisticsDto> GetStatisticsAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion

    #region Request Number Generation

    /// <summary>
    /// Generates the next request number.
    /// </summary>
    Task<string> GenerateRequestNumberAsync();

    /// <summary>
    /// Generates the next shipment number.
    /// </summary>
    Task<string> GenerateShipmentNumberAsync();

    /// <summary>
    /// Generates the next receipt number.
    /// </summary>
    Task<string> GenerateReceiptNumberAsync();

    #endregion
}
