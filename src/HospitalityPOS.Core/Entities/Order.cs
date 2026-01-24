using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a sales order containing order items.
/// </summary>
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int? WorkPeriodId { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the terminal ID where the order was created.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// Gets or sets the terminal code (denormalized for reporting/KDS).
    /// </summary>
    public string? TerminalCode { get; set; }

    public string? TableNumber { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the total savings from applied offers.
    /// </summary>
    public decimal TotalOfferSavings => OrderItems?.Sum(i => i.SavingsAmount) ?? 0;

    /// <summary>
    /// Gets whether any offers were applied to this order.
    /// </summary>
    public bool HasOffersApplied => OrderItems?.Any(i => i.HasOfferApplied) ?? false;

    /// <summary>
    /// Gets the count of items with offers applied.
    /// </summary>
    public int OfferItemsCount => OrderItems?.Count(i => i.HasOfferApplied) ?? 0;

    // Navigation properties
    public virtual WorkPeriod? WorkPeriod { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual Terminal? Terminal { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
