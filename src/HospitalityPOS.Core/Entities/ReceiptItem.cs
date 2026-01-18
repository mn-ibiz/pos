namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a receipt.
/// </summary>
public class ReceiptItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the original order item ID.
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name (denormalized for historical record).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price at time of sale.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this line item.
    /// </summary>
    public decimal TotalAmount { get; set; }
    public decimal TotalPrice { get => TotalAmount; set => TotalAmount = value; }

    /// <summary>
    /// Gets or sets the item modifiers.
    /// </summary>
    public string? Modifiers { get; set; }

    /// <summary>
    /// Gets or sets the item notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the parent receipt.
    /// </summary>
    public virtual Receipt Receipt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original order item.
    /// </summary>
    public virtual OrderItem OrderItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public virtual Product Product { get; set; } = null!;
}
