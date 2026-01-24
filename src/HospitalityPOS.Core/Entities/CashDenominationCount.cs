namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a cash count with denomination breakdown for a work period.
/// </summary>
public class CashDenominationCount : BaseEntity
{
    /// <summary>
    /// Gets or sets the work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets the type of count (Opening, Closing, MidShift).
    /// </summary>
    public CashCountType CountType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the count.
    /// </summary>
    public int CountedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the count was performed.
    /// </summary>
    public DateTime CountedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who verified the count.
    /// </summary>
    public int? VerifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the count was verified.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the total amount for notes.
    /// </summary>
    public decimal TotalNotes { get; set; }

    /// <summary>
    /// Gets or sets the total amount for coins.
    /// </summary>
    public decimal TotalCoins { get; set; }

    /// <summary>
    /// Gets or sets the grand total (notes + coins).
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the count.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual WorkPeriod WorkPeriod { get; set; } = null!;
    public virtual User CountedByUser { get; set; } = null!;
    public virtual User? VerifiedByUser { get; set; }
    public virtual ICollection<CashCountLine> Lines { get; set; } = new List<CashCountLine>();
}

/// <summary>
/// Represents a single denomination line in a cash count.
/// </summary>
public class CashCountLine : BaseEntity
{
    /// <summary>
    /// Gets or sets the parent cash count ID.
    /// </summary>
    public int CashDenominationCountId { get; set; }

    /// <summary>
    /// Gets or sets the denomination ID.
    /// </summary>
    public int DenominationId { get; set; }

    /// <summary>
    /// Gets or sets the quantity counted.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the line total (Quantity x Denomination.Value).
    /// </summary>
    public decimal LineTotal { get; set; }

    // Navigation properties
    public virtual CashDenominationCount CashDenominationCount { get; set; } = null!;
    public virtual CashDenomination Denomination { get; set; } = null!;
}

/// <summary>
/// Type of cash count.
/// </summary>
public enum CashCountType
{
    /// <summary>
    /// Count performed when opening a work period.
    /// </summary>
    Opening = 0,

    /// <summary>
    /// Count performed when closing a work period.
    /// </summary>
    Closing = 1,

    /// <summary>
    /// Count performed during the shift (e.g., cash drop).
    /// </summary>
    MidShift = 2,

    /// <summary>
    /// Verification count by manager.
    /// </summary>
    Verification = 3
}
