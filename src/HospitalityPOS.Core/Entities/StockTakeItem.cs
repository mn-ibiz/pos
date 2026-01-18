namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an item in a stock take with system vs physical count comparison.
/// </summary>
public class StockTakeItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the parent stock take ID.
    /// </summary>
    public int StockTakeId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name (denormalized for reporting).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code (denormalized for reporting).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the system quantity at the time of stock take.
    /// </summary>
    public decimal SystemQuantity { get; set; }

    /// <summary>
    /// Gets or sets the physical quantity entered by the user.
    /// </summary>
    public decimal? PhysicalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the variance quantity (Physical - System).
    /// </summary>
    public decimal VarianceQuantity { get; set; }
    public decimal Variance { get => VarianceQuantity; set => VarianceQuantity = value; }

    /// <summary>
    /// Gets or sets the cost price for variance value calculation.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the variance value (VarianceQuantity * CostPrice).
    /// </summary>
    public decimal VarianceValue { get; set; }

    /// <summary>
    /// Gets or sets whether this item has been counted.
    /// </summary>
    public bool IsCounted { get; set; }

    /// <summary>
    /// Gets or sets whether this item's adjustment has been approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets any notes for this item.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets when this item was counted.
    /// </summary>
    public DateTime? CountedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who counted this item.
    /// </summary>
    public int? CountedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the parent stock take.
    /// </summary>
    public virtual StockTake StockTake { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who counted this item.
    /// </summary>
    public virtual User? CountedByUser { get; set; }

    /// <summary>
    /// Gets whether this item has a variance after being counted.
    /// </summary>
    public bool HasVariance => IsCounted && VarianceQuantity != 0;
}
