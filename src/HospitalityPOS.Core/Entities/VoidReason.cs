namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a predefined reason for voiding receipts.
/// </summary>
public class VoidReason : BaseEntity
{
    /// <summary>
    /// Gets or sets the reason name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this reason is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether additional notes are required when selecting this reason.
    /// </summary>
    public bool RequiresNote { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the receipt voids using this reason.
    /// </summary>
    public virtual ICollection<ReceiptVoid> ReceiptVoids { get; set; } = new List<ReceiptVoid>();
}
