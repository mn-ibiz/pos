namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a payment made against a receipt.
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payment amount applied to the receipt.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the amount tendered (for cash payments).
    /// </summary>
    public decimal TenderedAmount { get; set; }

    /// <summary>
    /// Gets or sets the change amount (for cash payments).
    /// </summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>
    /// Gets or sets the reference number (M-Pesa code, card auth code, etc.).
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets the user who processed the payment.
    /// </summary>
    public int ProcessedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the receipt.
    /// </summary>
    public virtual Receipt Receipt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who processed the payment.
    /// </summary>
    public virtual User ProcessedByUser { get; set; } = null!;
}
