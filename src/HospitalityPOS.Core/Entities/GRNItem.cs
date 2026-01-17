namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a goods received note.
/// </summary>
public class GRNItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the GRN ID this item belongs to.
    /// </summary>
    public int GoodsReceivedNoteId { get; set; }

    /// <summary>
    /// Gets or sets the purchase order item ID (null for direct receiving).
    /// </summary>
    public int? PurchaseOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity ordered (0 for direct receiving).
    /// </summary>
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the quantity actually received.
    /// </summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit cost at time of receiving.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Gets or sets the total cost (ReceivedQuantity * UnitCost).
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets any notes about this line item.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the parent GRN.
    /// </summary>
    public virtual GoodsReceivedNote GoodsReceivedNote { get; set; } = null!;

    /// <summary>
    /// Gets or sets the related purchase order item.
    /// </summary>
    public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }

    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public virtual Product Product { get; set; } = null!;
}
