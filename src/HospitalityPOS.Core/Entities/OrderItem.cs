namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in an order.
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Modifiers { get; set; }
    public string? Notes { get; set; }
    public int BatchNumber { get; set; } = 1;
    public bool PrintedToKitchen { get; set; }

    /// <summary>
    /// Gets or sets the original unit price before any offer was applied.
    /// </summary>
    public decimal? OriginalUnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the ID of the applied product offer.
    /// </summary>
    public int? AppliedOfferId { get; set; }

    /// <summary>
    /// Gets or sets the name of the applied offer.
    /// </summary>
    public string? AppliedOfferName { get; set; }

    /// <summary>
    /// Gets the savings amount from the applied offer.
    /// </summary>
    public decimal SavingsAmount =>
        OriginalUnitPrice.HasValue
            ? (OriginalUnitPrice.Value - UnitPrice) * Quantity
            : 0;

    /// <summary>
    /// Gets whether an offer was applied to this item.
    /// </summary>
    public bool HasOfferApplied => AppliedOfferId.HasValue;

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductOffer? AppliedOffer { get; set; }
}
