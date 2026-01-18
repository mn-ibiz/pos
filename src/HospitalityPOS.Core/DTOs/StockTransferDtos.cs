using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO for stock transfer request.
/// </summary>
public class StockTransferRequestDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int RequestingStoreId { get; set; }
    public string RequestingStoreName { get; set; } = string.Empty;
    public int SourceLocationId { get; set; }
    public string SourceLocationName { get; set; } = string.Empty;
    public TransferLocationType SourceLocationType { get; set; }
    public string SourceLocationTypeName => SourceLocationType.ToString();
    public TransferRequestStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public TransferPriority Priority { get; set; }
    public string PriorityName => Priority.ToString();
    public TransferReason Reason { get; set; }
    public string ReasonName => Reason.ToString();
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserName { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public int TotalItemsRequested { get; set; }
    public int TotalItemsApproved { get; set; }
    public decimal TotalEstimatedValue { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TransferRequestLineDto> Lines { get; set; } = new();
    public StockTransferShipmentDto? Shipment { get; set; }
}

/// <summary>
/// DTO for creating a transfer request.
/// </summary>
public class CreateTransferRequestDto
{
    public int RequestingStoreId { get; set; }
    public int SourceLocationId { get; set; }
    public TransferLocationType SourceLocationType { get; set; } = TransferLocationType.Warehouse;
    public TransferPriority Priority { get; set; } = TransferPriority.Normal;
    public TransferReason Reason { get; set; } = TransferReason.Replenishment;
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateTransferRequestLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for updating a transfer request (draft status only).
/// </summary>
public class UpdateTransferRequestDto
{
    public int SourceLocationId { get; set; }
    public TransferLocationType SourceLocationType { get; set; }
    public TransferPriority Priority { get; set; }
    public TransferReason Reason { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for transfer request line item.
/// </summary>
public class TransferRequestLineDto
{
    public int Id { get; set; }
    public int TransferRequestId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? ProductBarcode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int? ApprovedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int IssueQuantity { get; set; }
    public int SourceAvailableStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// DTO for creating a transfer request line.
/// </summary>
public class CreateTransferRequestLineDto
{
    public int ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for approving a transfer request.
/// </summary>
public class ApproveTransferRequestDto
{
    public int RequestId { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<ApproveLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for approving individual line items.
/// </summary>
public class ApproveLineDto
{
    public int LineId { get; set; }
    public int ApprovedQuantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for rejecting a transfer request.
/// </summary>
public class RejectTransferRequestDto
{
    public int RequestId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for stock transfer shipment.
/// </summary>
public class StockTransferShipmentDto
{
    public int Id { get; set; }
    public int TransferRequestId { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public string? ShippedByUserName { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ActualArrivalDate { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public string? VehicleDetails { get; set; }
    public string? DriverName { get; set; }
    public string? DriverContact { get; set; }
    public int PackageCount { get; set; }
    public decimal? TotalWeightKg { get; set; }
    public string? Notes { get; set; }
    public bool IsComplete { get; set; }
}

/// <summary>
/// DTO for creating a shipment.
/// </summary>
public class CreateShipmentDto
{
    public int TransferRequestId { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public string? VehicleDetails { get; set; }
    public string? DriverName { get; set; }
    public string? DriverContact { get; set; }
    public int PackageCount { get; set; }
    public decimal? TotalWeightKg { get; set; }
    public string? Notes { get; set; }
    public List<ShipmentLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for shipment line quantities.
/// </summary>
public class ShipmentLineDto
{
    public int RequestLineId { get; set; }
    public int ShippedQuantity { get; set; }
}

/// <summary>
/// DTO for transfer receipt.
/// </summary>
public class StockTransferReceiptDto
{
    public int Id { get; set; }
    public int TransferRequestId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string ReceivedByUserName { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool HasIssues { get; set; }
    public string? Notes { get; set; }
    public int TotalReceived { get; set; }
    public int TotalIssues { get; set; }
    public List<TransferReceiptLineDto> Lines { get; set; } = new();
    public List<TransferReceiptIssueDto> Issues { get; set; } = new();
}

/// <summary>
/// DTO for creating a receipt.
/// </summary>
public class CreateReceiptDto
{
    public int TransferRequestId { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for creating a receipt line.
/// </summary>
public class CreateReceiptLineDto
{
    public int RequestLineId { get; set; }
    public int ReceivedQuantity { get; set; }
    public int IssueQuantity { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptIssueDto>? Issues { get; set; }
}

/// <summary>
/// DTO for transfer receipt line.
/// </summary>
public class TransferReceiptLineDto
{
    public int Id { get; set; }
    public int TransferReceiptId { get; set; }
    public int TransferRequestLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int IssueQuantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for transfer receipt issue.
/// </summary>
public class TransferReceiptIssueDto
{
    public int Id { get; set; }
    public int TransferReceiptId { get; set; }
    public int TransferReceiptLineId { get; set; }
    public TransferIssueType IssueType { get; set; }
    public string IssueTypeName => IssueType.ToString();
    public int AffectedQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserName { get; set; }
}

/// <summary>
/// DTO for creating a receipt issue.
/// </summary>
public class CreateReceiptIssueDto
{
    public TransferIssueType IssueType { get; set; }
    public int AffectedQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
}

/// <summary>
/// DTO for resolving a receipt issue.
/// </summary>
public class ResolveIssueDto
{
    public int IssueId { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
}

/// <summary>
/// DTO for transfer activity log.
/// </summary>
public class TransferActivityLogDto
{
    public int Id { get; set; }
    public int TransferRequestId { get; set; }
    public string Activity { get; set; } = string.Empty;
    public TransferRequestStatus? PreviousStatus { get; set; }
    public string? PreviousStatusName => PreviousStatus?.ToString();
    public TransferRequestStatus? NewStatus { get; set; }
    public string? NewStatusName => NewStatus?.ToString();
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Source location with available stock info.
/// </summary>
public class SourceLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TransferLocationType LocationType { get; set; }
    public string LocationTypeName => LocationType.ToString();
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsInStock { get; set; }
}

/// <summary>
/// Product stock at source location.
/// </summary>
public class SourceProductStockDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int TransferableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public int? ReorderLevel { get; set; }
}

/// <summary>
/// Transfer request summary for listing.
/// </summary>
public class TransferRequestSummaryDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestingStoreName { get; set; } = string.Empty;
    public string SourceLocationName { get; set; } = string.Empty;
    public TransferRequestStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public TransferPriority Priority { get; set; }
    public string PriorityName => Priority.ToString();
    public int LineCount { get; set; }
    public int TotalItemsRequested { get; set; }
    public decimal TotalEstimatedValue { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Query parameters for transfer requests.
/// </summary>
public class TransferRequestQueryDto
{
    public int? RequestingStoreId { get; set; }
    public int? SourceLocationId { get; set; }
    public TransferRequestStatus? Status { get; set; }
    public TransferPriority? Priority { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int? Limit { get; set; } = 50;
    public int? Offset { get; set; } = 0;
}

/// <summary>
/// Transfer dashboard for a store.
/// </summary>
public class StoreTransferDashboardDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;

    // Outgoing requests (this store requesting from others)
    public int OutgoingDraftCount { get; set; }
    public int OutgoingSubmittedCount { get; set; }
    public int OutgoingApprovedCount { get; set; }
    public int OutgoingInTransitCount { get; set; }

    // Incoming requests (others requesting from this store)
    public int IncomingPendingApprovalCount { get; set; }
    public int IncomingApprovedCount { get; set; }
    public int IncomingToShipCount { get; set; }

    // Recent activity
    public List<TransferRequestSummaryDto> RecentOutgoing { get; set; } = new();
    public List<TransferRequestSummaryDto> RecentIncoming { get; set; } = new();

    // Pending issues
    public int UnresolvedIssuesCount { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Chain-wide transfer dashboard.
/// </summary>
public class ChainTransferDashboardDto
{
    public int TotalActiveTransfers { get; set; }
    public int TotalPendingApprovals { get; set; }
    public int TotalInTransit { get; set; }
    public int TotalPendingReceipt { get; set; }
    public int TotalUnresolvedIssues { get; set; }
    public decimal TotalValueInTransit { get; set; }

    // By status counts
    public Dictionary<TransferRequestStatus, int> CountByStatus { get; set; } = new();

    // By priority counts
    public Dictionary<TransferPriority, int> CountByPriority { get; set; } = new();

    // Top requesting stores
    public List<StoreTransferVolumeDto> TopRequestingStores { get; set; } = new();

    // Top supplying locations
    public List<StoreTransferVolumeDto> TopSupplyingLocations { get; set; } = new();

    // Recent transfers
    public List<TransferRequestSummaryDto> RecentTransfers { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Store transfer volume for rankings.
/// </summary>
public class StoreTransferVolumeDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int TransferCount { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Transfer statistics for reporting.
/// </summary>
public class TransferStatisticsDto
{
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int RejectedRequests { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageProcessingDays { get; set; }
    public int TotalItemsTransferred { get; set; }
    public decimal TotalValueTransferred { get; set; }
    public int TotalIssuesReported { get; set; }
    public int ResolvedIssues { get; set; }
    public decimal IssueRate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

#region Stock Reservation DTOs

/// <summary>
/// DTO for stock reservation.
/// </summary>
public class StockReservationDto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public TransferLocationType LocationType { get; set; }
    public string LocationTypeName => LocationType.ToString();
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ReservedQuantity { get; set; }
    public int ReferenceId { get; set; }
    public ReservationType ReferenceType { get; set; }
    public string ReferenceTypeName => ReferenceType.ToString();
    public string? ReferenceNumber { get; set; }
    public ReservationStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime ReservedAt { get; set; }
    public string ReservedByUserName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => Status == ReservationStatus.Active && DateTime.UtcNow > ExpiresAt;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByUserName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a stock reservation.
/// </summary>
public class CreateStockReservationDto
{
    public int LocationId { get; set; }
    public TransferLocationType LocationType { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReferenceId { get; set; }
    public ReservationType ReferenceType { get; set; }
    public int ExpirationHours { get; set; } = 48;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating multiple reservations.
/// </summary>
public class CreateBatchReservationsDto
{
    public int LocationId { get; set; }
    public TransferLocationType LocationType { get; set; }
    public int ReferenceId { get; set; }
    public ReservationType ReferenceType { get; set; }
    public int ExpirationHours { get; set; } = 48;
    public List<ReservationLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for a reservation line item.
/// </summary>
public class ReservationLineDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// DTO for reservation summary by location.
/// </summary>
public class LocationReservationSummaryDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int ActiveReservations { get; set; }
    public int TotalQuantityReserved { get; set; }
    public int ExpiringWithin24Hours { get; set; }
    public int ExpiredReservations { get; set; }
}

/// <summary>
/// DTO for product reservation summary.
/// </summary>
public class ProductReservationSummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int TotalStockOnHand { get; set; }
    public int TotalReserved { get; set; }
    public int AvailableQuantity { get; set; }
    public List<ReservationDetailDto> ActiveReservations { get; set; } = new();
}

/// <summary>
/// DTO for reservation detail within summary.
/// </summary>
public class ReservationDetailDto
{
    public int ReservationId { get; set; }
    public int Quantity { get; set; }
    public ReservationType ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Query parameters for reservations.
/// </summary>
public class ReservationQueryDto
{
    public int? LocationId { get; set; }
    public int? ProductId { get; set; }
    public ReservationType? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public ReservationStatus? Status { get; set; }
    public bool? IsExpired { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}

#endregion

#region Pick List DTOs

/// <summary>
/// Status of a pick list.
/// </summary>
public enum PickListStatus
{
    /// <summary>Pick list generated, awaiting picking.</summary>
    Pending = 1,
    /// <summary>Picking in progress.</summary>
    InProgress = 2,
    /// <summary>All items picked.</summary>
    Completed = 3,
    /// <summary>Pick list cancelled.</summary>
    Cancelled = 4
}

/// <summary>
/// DTO for a warehouse pick list.
/// </summary>
public class PickListDto
{
    public int TransferRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string SourceLocationName { get; set; } = string.Empty;
    public string DestinationStoreName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public PickListStatus Status { get; set; } = PickListStatus.Pending;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public TransferPriority Priority { get; set; }
    public string PriorityName => Priority.ToString();
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public int PickedItems { get; set; }
    public int PickedQuantity { get; set; }
    public List<PickListLineDto> Lines { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for a pick list line item.
/// </summary>
public class PickListLineDto
{
    public int RequestLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? BinLocation { get; set; }
    public int ApprovedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public bool IsPicked { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for confirming picked items.
/// </summary>
public class ConfirmPickDto
{
    public int RequestLineId { get; set; }
    public int PickedQuantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for confirming all picks for a request.
/// </summary>
public class ConfirmAllPicksDto
{
    public int TransferRequestId { get; set; }
    public List<ConfirmPickDto> Lines { get; set; } = new();
}

#endregion

#region Transfer Document DTOs

/// <summary>
/// DTO for transfer document (delivery note/packing slip).
/// </summary>
public class TransferDocumentDto
{
    public int ShipmentId { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Source information
    public string SourceLocationName { get; set; } = string.Empty;
    public string? SourceAddress { get; set; }
    public string? SourcePhone { get; set; }

    // Destination information
    public string DestinationStoreName { get; set; } = string.Empty;
    public string? DestinationAddress { get; set; }
    public string? DestinationPhone { get; set; }

    // Shipment details
    public DateTime? DispatchedAt { get; set; }
    public string? DispatchedByUserName { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public string? VehicleDetails { get; set; }
    public string? DriverName { get; set; }
    public string? DriverContact { get; set; }
    public int PackageCount { get; set; }
    public decimal? TotalWeightKg { get; set; }

    // Line items
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public List<TransferDocumentLineDto> Lines { get; set; } = new();

    // Signatures
    public string? PreparedByName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for transfer document line item.
/// </summary>
public class TransferDocumentLineDto
{
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Transfer Detail View DTOs

/// <summary>
/// Detailed transfer request DTO for viewing transfer details.
/// </summary>
public class TransferRequestDetailDto : StockTransferRequestDto
{
    public new List<TransferLineDetailDto> Lines { get; set; } = new();
}

/// <summary>
/// Detailed line item DTO for transfer details view.
/// </summary>
public class TransferLineDetailDto : TransferRequestLineDto
{
    public string StockUnit { get; set; } = string.Empty;
    public decimal TotalValue => (ApprovedQuantity ?? RequestedQuantity) * UnitCost;
}

#endregion

#region Variance and Receipt Summary DTOs

/// <summary>
/// DTO for receiving summary with variance details.
/// </summary>
public class ReceivingSummaryDto
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int TransferRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string SourceLocationName { get; set; } = string.Empty;
    public string DestinationStoreName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? ReceivedByUserName { get; set; }
    public bool IsComplete { get; set; }

    // Quantity summaries
    public int TotalExpected { get; set; }
    public int TotalReceived { get; set; }
    public int TotalVariance => TotalReceived - TotalExpected;
    public int TotalIssueQuantity { get; set; }

    // Variance indicators
    public bool HasVariance => TotalVariance != 0;
    public bool HasShortage => TotalVariance < 0;
    public bool HasSurplus => TotalVariance > 0;
    public bool HasIssues => TotalIssueQuantity > 0;

    // Value summaries
    public decimal TotalExpectedValue { get; set; }
    public decimal TotalReceivedValue { get; set; }
    public decimal VarianceValue { get; set; }

    // Lines
    public List<ReceivingLineVarianceDto> Lines { get; set; } = new();

    // Issues
    public int UnresolvedIssueCount { get; set; }
    public int ResolvedIssueCount { get; set; }
}

/// <summary>
/// DTO for individual line variance details.
/// </summary>
public class ReceivingLineVarianceDto
{
    public int ReceiptLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int IssueQuantity { get; set; }
    public int Variance => ReceivedQuantity - ExpectedQuantity;
    public bool HasVariance => Variance != 0;
    public decimal UnitCost { get; set; }
    public decimal VarianceValue => Variance * UnitCost;
    public string VarianceType => Variance switch
    {
        0 => "None",
        < 0 => "Shortage",
        > 0 => "Surplus"
    };
    public List<TransferReceiptIssueDto> Issues { get; set; } = new();
}

/// <summary>
/// Query parameters for pending receipts.
/// </summary>
public class PendingReceiptsQueryDto
{
    public int? DestinationStoreId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludePartiallyReceived { get; set; } = true;
}

/// <summary>
/// DTO for pending shipment awaiting receipt.
/// </summary>
public class PendingReceiptDto
{
    public int TransferRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string SourceLocationName { get; set; } = string.Empty;
    public int TotalExpectedItems { get; set; }
    public int TotalExpectedQuantity { get; set; }
    public decimal TotalExpectedValue { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public string? ShipmentNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DriverName { get; set; }
    public TransferPriority Priority { get; set; }
    public bool IsOverdue => ExpectedArrivalDate.HasValue && ExpectedArrivalDate < DateTime.UtcNow;
    public int DaysInTransit => (int)(DateTime.UtcNow - (DispatchedAt ?? DateTime.UtcNow)).TotalDays;
}

/// <summary>
/// DTO for variance investigation.
/// </summary>
public class VarianceInvestigationDto
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int TransferRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public int TotalVarianceQuantity { get; set; }
    public int UnresolvedIssueCount { get; set; }
    public VarianceInvestigationStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? InvestigationStartedAt { get; set; }
    public string? InvestigatedByUserName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserName { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<ReceivingLineVarianceDto> VarianceLines { get; set; } = new();
}

/// <summary>
/// Status of a variance investigation.
/// </summary>
public enum VarianceInvestigationStatus
{
    /// <summary>Variance detected but not yet investigated.</summary>
    Pending = 1,
    /// <summary>Investigation in progress.</summary>
    InProgress = 2,
    /// <summary>Investigation completed and resolved.</summary>
    Resolved = 3,
    /// <summary>Variance written off.</summary>
    WrittenOff = 4
}

#endregion
