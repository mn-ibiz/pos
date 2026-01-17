namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a receipt void record with full audit details.
/// </summary>
public class ReceiptVoid : BaseEntity
{
    /// <summary>
    /// Gets or sets the receipt ID that was voided.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the void reason ID.
    /// </summary>
    public int VoidReasonId { get; set; }

    /// <summary>
    /// Gets or sets additional notes for the void.
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the void.
    /// </summary>
    public int VoidedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who authorized the void (for permission override).
    /// </summary>
    public int? AuthorizedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the amount that was voided.
    /// </summary>
    public decimal VoidedAmount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the void occurred.
    /// </summary>
    public DateTime VoidedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether stock was restored for this void.
    /// </summary>
    public bool StockRestored { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the voided receipt.
    /// </summary>
    public virtual Receipt Receipt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the void reason.
    /// </summary>
    public virtual VoidReason VoidReason { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who performed the void.
    /// </summary>
    public virtual User VoidedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who authorized the void.
    /// </summary>
    public virtual User? AuthorizedByUser { get; set; }
}
