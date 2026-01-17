using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Status of a product batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>Batch is active and can be sold.</summary>
    Active = 1,
    /// <summary>Batch has low stock (below threshold).</summary>
    LowStock = 2,
    /// <summary>Batch has expired.</summary>
    Expired = 3,
    /// <summary>Batch has been recalled.</summary>
    Recalled = 4,
    /// <summary>Batch has been disposed of.</summary>
    Disposed = 5
}

/// <summary>
/// Type of batch stock movement.
/// </summary>
public enum BatchMovementType
{
    /// <summary>Stock received via GRN.</summary>
    Receipt = 1,
    /// <summary>Stock sold via POS.</summary>
    Sale = 2,
    /// <summary>Stock transferred to another location.</summary>
    TransferOut = 3,
    /// <summary>Stock received from another location.</summary>
    TransferIn = 4,
    /// <summary>Stock adjustment (increase or decrease).</summary>
    Adjustment = 5,
    /// <summary>Stock disposed of (expired/damaged).</summary>
    Disposal = 6,
    /// <summary>Stock returned by customer.</summary>
    Return = 7,
    /// <summary>Stock reserved for order.</summary>
    Reserved = 8,
    /// <summary>Reserved stock released.</summary>
    Released = 9
}

/// <summary>
/// Type of expiry action.
/// </summary>
public enum ExpiryAction
{
    /// <summary>Show warning but allow sale.</summary>
    Warn = 1,
    /// <summary>Block sale completely.</summary>
    Block = 2,
    /// <summary>Require manager override.</summary>
    RequireOverride = 3
}

#endregion

#region Entities

/// <summary>
/// Represents a batch/lot of a product with tracking information.
/// </summary>
public class ProductBatch : BaseEntity
{
    /// <summary>Reference to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Reference to the store/location.</summary>
    public int StoreId { get; set; }

    /// <summary>Unique batch or lot number.</summary>
    [Required]
    [MaxLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>Expiry date of the batch (null if no expiry).</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Manufacturing date of the batch.</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Initial quantity received.</summary>
    public int InitialQuantity { get; set; }

    /// <summary>Current available quantity.</summary>
    public int CurrentQuantity { get; set; }

    /// <summary>Quantity reserved for orders.</summary>
    public int ReservedQuantity { get; set; }

    /// <summary>Quantity sold from this batch.</summary>
    public int SoldQuantity { get; set; }

    /// <summary>Quantity disposed (expired/damaged).</summary>
    public int DisposedQuantity { get; set; }

    /// <summary>Reference to the supplier.</summary>
    public int? SupplierId { get; set; }

    /// <summary>Reference to the GRN (Goods Received Note).</summary>
    public int? GrnId { get; set; }

    /// <summary>Reference to the transfer receipt (if from transfer).</summary>
    public int? TransferReceiptId { get; set; }

    /// <summary>Date and time the batch was received.</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who received the batch.</summary>
    public int ReceivedByUserId { get; set; }

    /// <summary>Current status of the batch.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Active;

    /// <summary>Unit cost when received.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Additional notes about the batch.</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Number of days until expiry (calculated).</summary>
    public int? DaysUntilExpiry => ExpiryDate.HasValue
        ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays
        : null;

    /// <summary>Whether batch is expired.</summary>
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    /// <summary>Available quantity (current - reserved).</summary>
    public int AvailableQuantity => CurrentQuantity - ReservedQuantity;

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public virtual GoodsReceivedNote? Grn { get; set; }
}

/// <summary>
/// Configuration for batch tracking per product.
/// </summary>
public class ProductBatchConfiguration : BaseEntity
{
    /// <summary>Reference to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Whether batch tracking is required for this product.</summary>
    public bool RequiresBatchTracking { get; set; }

    /// <summary>Whether expiry date is required when receiving.</summary>
    public bool RequiresExpiryDate { get; set; }

    /// <summary>Days before expiry to start warning.</summary>
    public int ExpiryWarningDays { get; set; } = 30;

    /// <summary>Days before expiry to mark as critical.</summary>
    public int ExpiryCriticalDays { get; set; } = 7;

    /// <summary>Action to take when selling expired items.</summary>
    public ExpiryAction ExpiredItemAction { get; set; } = ExpiryAction.Block;

    /// <summary>Action to take when selling items about to expire.</summary>
    public ExpiryAction NearExpiryAction { get; set; } = ExpiryAction.Warn;

    /// <summary>Whether to use FIFO (First In First Out) for batch selection.</summary>
    public bool UseFifo { get; set; } = true;

    /// <summary>Whether to use FEFO (First Expiry First Out) for batch selection.</summary>
    public bool UseFefo { get; set; } = true;

    /// <summary>Whether to track manufacture date.</summary>
    public bool TrackManufactureDate { get; set; }

    /// <summary>Minimum shelf life days required on receipt (0 = no minimum).</summary>
    public int MinimumShelfLifeDaysOnReceipt { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Records movement of stock at the batch level.
/// </summary>
public class BatchStockMovement : BaseEntity
{
    /// <summary>Reference to the batch.</summary>
    public int BatchId { get; set; }

    /// <summary>Reference to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Reference to the store.</summary>
    public int StoreId { get; set; }

    /// <summary>Type of movement.</summary>
    public BatchMovementType MovementType { get; set; }

    /// <summary>Quantity moved (positive for in, negative for out).</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity before movement.</summary>
    public int QuantityBefore { get; set; }

    /// <summary>Quantity after movement.</summary>
    public int QuantityAfter { get; set; }

    /// <summary>Type of reference document (GRN, Receipt, Transfer, Adjustment).</summary>
    [MaxLength(50)]
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>ID of the reference document.</summary>
    public int ReferenceId { get; set; }

    /// <summary>Reference number for display.</summary>
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }

    /// <summary>Date and time of movement.</summary>
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who performed the movement.</summary>
    public int? MovedByUserId { get; set; }

    /// <summary>Unit cost at time of movement.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Total value of the movement.</summary>
    public decimal TotalValue => Math.Abs(Quantity) * UnitCost;

    /// <summary>Notes about the movement.</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ProductBatch? Batch { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Records disposal of expired or damaged batches.
/// </summary>
public class BatchDisposal : BaseEntity
{
    /// <summary>Reference to the batch.</summary>
    public int BatchId { get; set; }

    /// <summary>Reference to the store.</summary>
    public int StoreId { get; set; }

    /// <summary>Quantity disposed.</summary>
    public int Quantity { get; set; }

    /// <summary>Reason for disposal.</summary>
    public DisposalReason Reason { get; set; }

    /// <summary>Detailed description of reason.</summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Date of disposal.</summary>
    public DateTime DisposedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who approved disposal.</summary>
    public int ApprovedByUserId { get; set; }

    /// <summary>User who performed disposal.</summary>
    public int DisposedByUserId { get; set; }

    /// <summary>Unit cost of disposed items.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Total value of disposed items.</summary>
    public decimal TotalValue => Quantity * UnitCost;

    /// <summary>Whether disposal was witnessed.</summary>
    public bool IsWitnessed { get; set; }

    /// <summary>Name of witness.</summary>
    [MaxLength(100)]
    public string? WitnessName { get; set; }

    /// <summary>Path to photo evidence.</summary>
    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    // Navigation properties
    public virtual ProductBatch? Batch { get; set; }
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Reason for batch disposal.
/// </summary>
public enum DisposalReason
{
    /// <summary>Product has expired.</summary>
    Expired = 1,
    /// <summary>Product is damaged.</summary>
    Damaged = 2,
    /// <summary>Product has been recalled.</summary>
    Recalled = 3,
    /// <summary>Quality issues found.</summary>
    QualityIssue = 4,
    /// <summary>Contamination detected.</summary>
    Contamination = 5,
    /// <summary>Other reason.</summary>
    Other = 6
}

/// <summary>
/// Records attempted sales of expired items and any overrides applied.
/// </summary>
public class ExpirySaleBlock : BaseEntity
{
    /// <summary>Reference to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Reference to the batch.</summary>
    public int BatchId { get; set; }

    /// <summary>Reference to the store.</summary>
    public int StoreId { get; set; }

    /// <summary>Expiry date of the batch.</summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>Days the product has been expired.</summary>
    public int DaysExpired { get; set; }

    /// <summary>User who attempted the sale.</summary>
    public int AttemptedByUserId { get; set; }

    /// <summary>Date and time of the sale attempt.</summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Quantity attempted to be sold.</summary>
    public int AttemptedQuantity { get; set; }

    /// <summary>Whether the sale was blocked.</summary>
    public bool WasBlocked { get; set; } = true;

    /// <summary>Whether an override was applied.</summary>
    public bool OverrideApplied { get; set; }

    /// <summary>User who approved the override (if any).</summary>
    public int? OverrideByUserId { get; set; }

    /// <summary>Date and time of override.</summary>
    public DateTime? OverrideAt { get; set; }

    /// <summary>Reason provided for override.</summary>
    [MaxLength(500)]
    public string? OverrideReason { get; set; }

    /// <summary>Reference to the sale receipt (if sale proceeded).</summary>
    public int? ReceiptId { get; set; }

    // Navigation properties
    public virtual Product? Product { get; set; }
    public virtual ProductBatch? Batch { get; set; }
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Category-level expiry settings.
/// </summary>
public class CategoryExpirySettings : BaseEntity
{
    /// <summary>Reference to the category.</summary>
    public int CategoryId { get; set; }

    /// <summary>Whether expiry tracking is required for products in this category.</summary>
    public bool RequiresExpiryTracking { get; set; }

    /// <summary>Whether to block sales of expired products.</summary>
    public bool BlockExpiredSales { get; set; } = true;

    /// <summary>Whether manager override is allowed for expired sales.</summary>
    public bool AllowManagerOverride { get; set; } = true;

    /// <summary>Days before expiry to start warning.</summary>
    public int WarningDays { get; set; } = 30;

    /// <summary>Days before expiry to mark as critical.</summary>
    public int CriticalDays { get; set; } = 7;

    /// <summary>Action for expired items (overrides product settings).</summary>
    public ExpiryAction ExpiredItemAction { get; set; } = ExpiryAction.Block;

    /// <summary>Action for near-expiry items.</summary>
    public ExpiryAction NearExpiryAction { get; set; } = ExpiryAction.Warn;

    /// <summary>Minimum shelf life days for receiving.</summary>
    public int MinimumShelfLifeDaysOnReceipt { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
}

/// <summary>
/// Severity levels for product recalls.
/// </summary>
public enum RecallSeverity
{
    /// <summary>Low severity - quality issue.</summary>
    Low = 1,
    /// <summary>Medium severity - potential health concern.</summary>
    Medium = 2,
    /// <summary>High severity - immediate health risk.</summary>
    High = 3,
    /// <summary>Critical severity - life-threatening.</summary>
    Critical = 4
}

/// <summary>
/// Status of a recall alert.
/// </summary>
public enum RecallStatus
{
    /// <summary>Recall is active and being processed.</summary>
    Active = 1,
    /// <summary>All affected stock has been recovered.</summary>
    Recovered = 2,
    /// <summary>Recall has been partially resolved.</summary>
    PartiallyResolved = 3,
    /// <summary>Recall has been closed.</summary>
    Closed = 4,
    /// <summary>Recall was cancelled (false alarm).</summary>
    Cancelled = 5
}

/// <summary>
/// Records a product batch recall alert.
/// </summary>
public class BatchRecallAlert : BaseEntity
{
    /// <summary>Reference to the batch.</summary>
    public int BatchId { get; set; }

    /// <summary>Reference to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Batch number for quick reference.</summary>
    [Required]
    [MaxLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>Reason for the recall.</summary>
    [Required]
    [MaxLength(1000)]
    public string RecallReason { get; set; } = string.Empty;

    /// <summary>Severity of the recall.</summary>
    public RecallSeverity Severity { get; set; } = RecallSeverity.Medium;

    /// <summary>Current status of the recall.</summary>
    public RecallStatus Status { get; set; } = RecallStatus.Active;

    /// <summary>Date and time recall was issued.</summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who issued the recall.</summary>
    public int IssuedByUserId { get; set; }

    /// <summary>Total quantity affected by recall.</summary>
    public int AffectedQuantity { get; set; }

    /// <summary>Quantity successfully recovered.</summary>
    public int QuantityRecovered { get; set; }

    /// <summary>Quantity already sold before recall.</summary>
    public int QuantitySold { get; set; }

    /// <summary>Quantity remaining in stock.</summary>
    public int QuantityInStock { get; set; }

    /// <summary>External recall reference (e.g., manufacturer recall number).</summary>
    [MaxLength(100)]
    public string? ExternalReference { get; set; }

    /// <summary>Contact information for manufacturer/supplier.</summary>
    [MaxLength(500)]
    public string? SupplierContactInfo { get; set; }

    /// <summary>Resolution notes.</summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>Date recall was resolved.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>User who resolved the recall.</summary>
    public int? ResolvedByUserId { get; set; }

    // Navigation properties
    public virtual ProductBatch? Batch { get; set; }
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Tracks actions taken during a recall.
/// </summary>
public class RecallAction : BaseEntity
{
    /// <summary>Reference to the recall alert.</summary>
    public int RecallAlertId { get; set; }

    /// <summary>Type of action taken.</summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty; // Quarantine, Dispose, Return, Notify

    /// <summary>Store where action was taken.</summary>
    public int? StoreId { get; set; }

    /// <summary>Quantity affected by this action.</summary>
    public int Quantity { get; set; }

    /// <summary>Description of the action.</summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Date and time of action.</summary>
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    /// <summary>User who performed the action.</summary>
    public int PerformedByUserId { get; set; }

    // Navigation properties
    public virtual BatchRecallAlert? RecallAlert { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion
