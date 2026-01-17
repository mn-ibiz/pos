namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a goods received note (GRN) for receiving stock.
/// Can be linked to a purchase order or used for direct receiving.
/// </summary>
public class GoodsReceivedNote : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique GRN number (format: GRN-{yyyyMMdd}-{sequence}).
    /// </summary>
    public string GRNNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purchase order ID (null for direct receiving).
    /// </summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the supplier ID (optional for direct receiving).
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the date goods were received.
    /// </summary>
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the supplier's delivery note number.
    /// </summary>
    public string? DeliveryNote { get; set; }

    /// <summary>
    /// Gets or sets the total value of received goods.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the receiving.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the user who received the goods.
    /// </summary>
    public int ReceivedByUserId { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is a direct receiving (no PO).
    /// </summary>
    public bool IsDirectReceiving => !PurchaseOrderId.HasValue;

    // Navigation properties

    /// <summary>
    /// Gets or sets the related purchase order.
    /// </summary>
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// Gets or sets the supplier.
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// Gets or sets the user who received the goods.
    /// </summary>
    public virtual User ReceivedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the GRN line items.
    /// </summary>
    public virtual ICollection<GRNItem> Items { get; set; } = new List<GRNItem>();
}
