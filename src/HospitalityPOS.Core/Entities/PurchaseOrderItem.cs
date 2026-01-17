namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a purchase order.
/// </summary>
public class PurchaseOrderItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the purchase order ID.
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ordered quantity.
    /// </summary>
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the received quantity.
    /// </summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit cost.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Gets or sets the total cost (OrderedQuantity * UnitCost).
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets additional notes for this line item.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<GRNItem> GRNItems { get; set; } = new List<GRNItem>();
}
