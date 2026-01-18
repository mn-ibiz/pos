namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a stock transfer request.
/// </summary>
public enum TransferRequestStatus
{
    /// <summary>Request is being drafted.</summary>
    Draft = 0,
    /// <summary>Request has been submitted for approval.</summary>
    Submitted = 1,
    /// <summary>Request is partially approved.</summary>
    PartiallyApproved = 2,
    /// <summary>Request is fully approved.</summary>
    Approved = 3,
    /// <summary>Request has been rejected.</summary>
    Rejected = 4,
    /// <summary>Transfer is in transit.</summary>
    InTransit = 5,
    /// <summary>Transfer has been partially received.</summary>
    PartiallyReceived = 6,
    /// <summary>Transfer has been fully received.</summary>
    Received = 7,
    /// <summary>Request has been cancelled.</summary>
    Cancelled = 8
}

/// <summary>
/// Type of transfer location (store or warehouse).
/// </summary>
public enum TransferLocationType
{
    Store = 1,
    Warehouse = 2,
    HQ = 3
}

/// <summary>
/// Priority level for transfer requests.
/// </summary>
public enum TransferPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Reason for transfer request.
/// </summary>
public enum TransferReason
{
    /// <summary>Regular replenishment.</summary>
    Replenishment = 1,
    /// <summary>Emergency stock needed.</summary>
    Emergency = 2,
    /// <summary>Seasonal demand.</summary>
    Seasonal = 3,
    /// <summary>Promotional event.</summary>
    Promotion = 4,
    /// <summary>Stock rebalancing.</summary>
    Rebalancing = 5,
    /// <summary>Product recall return.</summary>
    Recall = 6,
    /// <summary>Damaged goods return.</summary>
    DamagedReturn = 7,
    /// <summary>Slow-moving stock transfer.</summary>
    SlowMoving = 8,
    /// <summary>Other reason.</summary>
    Other = 9
}

/// <summary>
/// Stock transfer request between locations.
/// </summary>
public class StockTransferRequest : BaseEntity
{
    /// <summary>Auto-generated request number (e.g., TR-2024-00001).</summary>
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>ID of the store requesting the transfer.</summary>
    public int RequestingStoreId { get; set; }

    /// <summary>ID of the source location (store/warehouse).</summary>
    public int SourceLocationId { get; set; }

    /// <summary>Type of source location.</summary>
    public TransferLocationType SourceLocationType { get; set; }

    /// <summary>Current status of the request.</summary>
    public TransferRequestStatus Status { get; set; } = TransferRequestStatus.Draft;

    /// <summary>Priority level.</summary>
    public TransferPriority Priority { get; set; } = TransferPriority.Normal;

    /// <summary>Reason for transfer.</summary>
    public TransferReason Reason { get; set; } = TransferReason.Replenishment;

    /// <summary>When the request was submitted.</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>User who submitted the request.</summary>
    public int? SubmittedByUserId { get; set; }

    /// <summary>When the request was approved/rejected.</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>User who approved/rejected.</summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>Approval notes.</summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>Requested delivery date.</summary>
    public DateTime? RequestedDeliveryDate { get; set; }

    /// <summary>Expected delivery date (set after approval).</summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>Additional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Rejection reason if rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Total number of items requested.</summary>
    public int TotalItemsRequested { get; set; }

    /// <summary>Total number of items approved.</summary>
    public int TotalItemsApproved { get; set; }

    /// <summary>Total estimated value of transfer.</summary>
    public decimal TotalEstimatedValue { get; set; }

    // Navigation properties
    public virtual Store? RequestingStore { get; set; }
    public virtual Store? SourceLocation { get; set; }
    public virtual ICollection<TransferRequestLine> Lines { get; set; } = new List<TransferRequestLine>();
    public virtual StockTransferShipment? Shipment { get; set; }
}

/// <summary>
/// Individual line item in a transfer request.
/// </summary>
public class TransferRequestLine : BaseEntity
{
    /// <summary>Reference to the parent request.</summary>
    public int TransferRequestId { get; set; }

    /// <summary>Product being requested.</summary>
    public int ProductId { get; set; }

    /// <summary>Quantity requested.</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>Quantity approved (null until approved).</summary>
    public int? ApprovedQuantity { get; set; }

    /// <summary>Quantity shipped.</summary>
    public int ShippedQuantity { get; set; }

    /// <summary>Quantity received.</summary>
    public int ReceivedQuantity { get; set; }

    /// <summary>Quantity with issues (damaged, missing).</summary>
    public int IssueQuantity { get; set; }

    /// <summary>Available stock at source when request was created.</summary>
    public int SourceAvailableStock { get; set; }

    /// <summary>Unit cost at time of request.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Line total (quantity * unit cost).</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Notes for this line.</summary>
    public string? Notes { get; set; }

    /// <summary>Reason for approval quantity difference.</summary>
    public string? ApprovalNotes { get; set; }

    // Navigation properties
    public virtual StockTransferRequest? TransferRequest { get; set; }
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Shipment for an approved transfer.
/// </summary>
public class StockTransferShipment : BaseEntity
{
    /// <summary>Reference to the transfer request.</summary>
    public int TransferRequestId { get; set; }

    /// <summary>Auto-generated shipment number.</summary>
    public string ShipmentNumber { get; set; } = string.Empty;

    /// <summary>When the shipment was dispatched.</summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>User who dispatched the shipment.</summary>
    public int? ShippedByUserId { get; set; }

    /// <summary>Expected arrival date.</summary>
    public DateTime? ExpectedArrivalDate { get; set; }

    /// <summary>Actual arrival date.</summary>
    public DateTime? ActualArrivalDate { get; set; }

    /// <summary>Carrier/transport method.</summary>
    public string? Carrier { get; set; }

    /// <summary>Tracking number if applicable.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Vehicle number/details.</summary>
    public string? VehicleDetails { get; set; }

    /// <summary>Driver name.</summary>
    public string? DriverName { get; set; }

    /// <summary>Driver contact number.</summary>
    public string? DriverContact { get; set; }

    /// <summary>Number of packages/boxes.</summary>
    public int PackageCount { get; set; }

    /// <summary>Total weight in KG.</summary>
    public decimal? TotalWeightKg { get; set; }

    /// <summary>Shipping notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Whether shipment is complete.</summary>
    public bool IsComplete { get; set; }

    // Navigation properties
    public virtual StockTransferRequest? TransferRequest { get; set; }
}

/// <summary>
/// Receipt of stock transfer at destination.
/// </summary>
public class StockTransferReceipt : BaseEntity
{
    /// <summary>Reference to the transfer request.</summary>
    public int TransferRequestId { get; set; }

    /// <summary>Auto-generated receipt number.</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>When the receipt was created.</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>User who received the shipment.</summary>
    public int ReceivedByUserId { get; set; }

    /// <summary>Overall receipt status.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Whether there were any issues.</summary>
    public bool HasIssues { get; set; }

    /// <summary>Receipt notes.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual StockTransferRequest? TransferRequest { get; set; }
    public virtual ICollection<TransferReceiptLine> Lines { get; set; } = new List<TransferReceiptLine>();
    public virtual ICollection<TransferReceiptIssue> Issues { get; set; } = new List<TransferReceiptIssue>();
}

/// <summary>
/// Individual line item in a transfer receipt.
/// </summary>
public class TransferReceiptLine : BaseEntity
{
    /// <summary>Reference to the parent receipt.</summary>
    public int TransferReceiptId { get; set; }

    /// <summary>Reference to the original request line.</summary>
    public int TransferRequestLineId { get; set; }

    /// <summary>Product received.</summary>
    public int ProductId { get; set; }

    /// <summary>Quantity expected (from shipment).</summary>
    public int ExpectedQuantity { get; set; }

    /// <summary>Quantity actually received in good condition.</summary>
    public int ReceivedQuantity { get; set; }

    /// <summary>Quantity with issues.</summary>
    public int IssueQuantity { get; set; }

    /// <summary>Notes for this line.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual StockTransferReceipt? TransferReceipt { get; set; }
    public virtual TransferRequestLine? TransferRequestLine { get; set; }
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Type of issue during receipt.
/// </summary>
public enum TransferIssueType
{
    /// <summary>Item is damaged.</summary>
    Damaged = 1,
    /// <summary>Item is missing from shipment.</summary>
    Missing = 2,
    /// <summary>Wrong item received.</summary>
    WrongItem = 3,
    /// <summary>Quantity mismatch.</summary>
    QuantityMismatch = 4,
    /// <summary>Item is expired.</summary>
    Expired = 5,
    /// <summary>Other issue.</summary>
    Other = 6
}

/// <summary>
/// Issue recorded during transfer receipt.
/// </summary>
public class TransferReceiptIssue : BaseEntity
{
    /// <summary>Reference to the parent receipt.</summary>
    public int TransferReceiptId { get; set; }

    /// <summary>Reference to the receipt line.</summary>
    public int TransferReceiptLineId { get; set; }

    /// <summary>Type of issue.</summary>
    public TransferIssueType IssueType { get; set; }

    /// <summary>Quantity affected.</summary>
    public int AffectedQuantity { get; set; }

    /// <summary>Description of the issue.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Photo evidence path.</summary>
    public string? PhotoPath { get; set; }

    /// <summary>Whether issue has been resolved.</summary>
    public bool IsResolved { get; set; }

    /// <summary>Resolution notes.</summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>When resolved.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>User who resolved.</summary>
    public int? ResolvedByUserId { get; set; }

    // Navigation properties
    public virtual StockTransferReceipt? TransferReceipt { get; set; }
    public virtual TransferReceiptLine? TransferReceiptLine { get; set; }
}

/// <summary>
/// Log entry for transfer activities.
/// </summary>
public class TransferActivityLog : BaseEntity
{
    /// <summary>Reference to the transfer request.</summary>
    public int TransferRequestId { get; set; }

    /// <summary>Activity type/description.</summary>
    public string Activity { get; set; } = string.Empty;

    /// <summary>Previous status (if status change).</summary>
    public TransferRequestStatus? PreviousStatus { get; set; }

    /// <summary>New status (if status change).</summary>
    public TransferRequestStatus? NewStatus { get; set; }

    /// <summary>User who performed the activity.</summary>
    public int PerformedByUserId { get; set; }

    /// <summary>When the activity occurred.</summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Additional details.</summary>
    public string? Details { get; set; }

    // Navigation properties
    public virtual StockTransferRequest? TransferRequest { get; set; }
}

/// <summary>
/// Type of reservation reference.
/// </summary>
public enum ReservationType
{
    /// <summary>Stock reserved for transfer.</summary>
    Transfer = 1,
    /// <summary>Stock reserved for customer order.</summary>
    CustomerOrder = 2,
    /// <summary>Stock reserved for promotion.</summary>
    Promotion = 3
}

/// <summary>
/// Status of a stock reservation.
/// </summary>
public enum ReservationStatus
{
    /// <summary>Reservation is active.</summary>
    Active = 1,
    /// <summary>Reservation has been fulfilled (stock shipped/used).</summary>
    Fulfilled = 2,
    /// <summary>Reservation was released/cancelled.</summary>
    Released = 3,
    /// <summary>Reservation expired automatically.</summary>
    Expired = 4
}

/// <summary>
/// Stock reservation for holding inventory for transfers/orders.
/// </summary>
public class StockReservation : BaseEntity
{
    /// <summary>Location where stock is reserved.</summary>
    public int LocationId { get; set; }

    /// <summary>Type of location (Store/Warehouse).</summary>
    public TransferLocationType LocationType { get; set; }

    /// <summary>Product being reserved.</summary>
    public int ProductId { get; set; }

    /// <summary>Quantity reserved.</summary>
    public int ReservedQuantity { get; set; }

    /// <summary>Reference ID (e.g., TransferRequestId).</summary>
    public int ReferenceId { get; set; }

    /// <summary>Type of reservation reference.</summary>
    public ReservationType ReferenceType { get; set; }

    /// <summary>Current status of the reservation.</summary>
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    /// <summary>When the reservation was created.</summary>
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who created the reservation.</summary>
    public int ReservedByUserId { get; set; }

    /// <summary>When the reservation expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the reservation was fulfilled or released.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>User who fulfilled/released the reservation.</summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>Notes for the reservation.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Store? Location { get; set; }
    public virtual Product? Product { get; set; }
}
