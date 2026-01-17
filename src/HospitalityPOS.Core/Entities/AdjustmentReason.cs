namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a predefined reason for stock adjustments.
/// </summary>
public class AdjustmentReason : BaseEntity
{
    /// <summary>
    /// Gets or sets the reason name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason code (short identifier).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether additional notes are required when selecting this reason.
    /// </summary>
    public bool RequiresNote { get; set; }

    /// <summary>
    /// Gets or sets whether this reason can be used for stock increases.
    /// </summary>
    public bool IsIncrease { get; set; }

    /// <summary>
    /// Gets or sets whether this reason can be used for stock decreases.
    /// </summary>
    public bool IsDecrease { get; set; } = true;

    /// <summary>
    /// Gets or sets the display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the stock movements using this reason.
    /// </summary>
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
