using HospitalityPOS.Core.Enums;

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
    /// Gets or sets the product SKU (denormalized for reporting).
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name (denormalized for reporting).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code (denormalized for reporting).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage location.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    #region System Values (Frozen at Count Start)

    /// <summary>
    /// Gets or sets the system quantity at the time of stock take.
    /// </summary>
    public decimal SystemQuantity { get; set; }

    /// <summary>
    /// Gets or sets the cost price for variance value calculation.
    /// </summary>
    public decimal SystemCostPrice { get; set; }

    /// <summary>
    /// Gets or sets the cost price for variance value calculation.
    /// Alias for SystemCostPrice for backward compatibility.
    /// </summary>
    public decimal CostPrice { get => SystemCostPrice; set => SystemCostPrice = value; }

    /// <summary>
    /// Gets or sets the system value (SystemQuantity * SystemCostPrice).
    /// </summary>
    public decimal SystemValue { get; set; }

    #endregion

    #region Primary Count

    /// <summary>
    /// Gets or sets the physical quantity entered by the primary counter.
    /// </summary>
    public decimal? CountedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the physical quantity entered by the user.
    /// Alias for CountedQuantity for backward compatibility.
    /// </summary>
    public decimal? PhysicalQuantity { get => CountedQuantity; set => CountedQuantity = value; }

    /// <summary>
    /// Gets or sets the counted value (CountedQuantity * CostPrice).
    /// </summary>
    public decimal? CountedValue { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who counted this item.
    /// </summary>
    public int? CountedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when this item was counted.
    /// </summary>
    public DateTime? CountedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this item has been counted.
    /// </summary>
    public bool IsCounted { get; set; }

    #endregion

    #region Second Count (Double-Blind)

    /// <summary>
    /// Gets or sets the quantity from second counter (for double-blind).
    /// </summary>
    public decimal? SecondCountQuantity { get; set; }

    /// <summary>
    /// Gets or sets the ID of the second counter.
    /// </summary>
    public int? SecondCountedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the second count was recorded.
    /// </summary>
    public DateTime? SecondCountedAt { get; set; }

    /// <summary>
    /// Gets or sets whether there's a mismatch between counters.
    /// </summary>
    public bool CountMismatch { get; set; }

    /// <summary>
    /// Gets or sets the resolved quantity after mismatch review.
    /// </summary>
    public decimal? ResolvedQuantity { get; set; }

    /// <summary>
    /// Gets or sets who resolved the mismatch.
    /// </summary>
    public int? ResolvedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the mismatch was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    #endregion

    #region Variance

    /// <summary>
    /// Gets or sets the variance quantity (Physical - System).
    /// </summary>
    public decimal VarianceQuantity { get; set; }

    /// <summary>
    /// Gets or sets the variance quantity.
    /// Alias for backward compatibility.
    /// </summary>
    public decimal Variance { get => VarianceQuantity; set => VarianceQuantity = value; }

    /// <summary>
    /// Gets or sets the variance value (VarianceQuantity * CostPrice).
    /// </summary>
    public decimal VarianceValue { get; set; }

    /// <summary>
    /// Gets or sets the variance percentage (VarianceQuantity / SystemQuantity * 100).
    /// </summary>
    public decimal VariancePercentage { get; set; }

    /// <summary>
    /// Gets or sets whether the variance exceeds the threshold.
    /// </summary>
    public bool ExceedsThreshold { get; set; }

    #endregion

    #region Variance Resolution

    /// <summary>
    /// Gets or sets the variance cause.
    /// </summary>
    public VarianceCause? VarianceCause { get; set; }

    /// <summary>
    /// Gets or sets notes explaining the variance.
    /// </summary>
    public string? VarianceNotes { get; set; }

    /// <summary>
    /// Gets or sets any general notes for this item.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Adjustment Tracking

    /// <summary>
    /// Gets or sets whether this item's adjustment has been approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets whether the adjustment has been posted.
    /// </summary>
    public bool AdjustmentPosted { get; set; }

    /// <summary>
    /// Gets or sets the stock movement ID created for adjustment.
    /// </summary>
    public int? StockMovementId { get; set; }

    /// <summary>
    /// Gets or sets when the adjustment was posted.
    /// </summary>
    public DateTime? AdjustmentPostedAt { get; set; }

    #endregion

    #region Navigation Properties

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
    /// Gets or sets the second counter user.
    /// </summary>
    public virtual User? SecondCountedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who resolved the mismatch.
    /// </summary>
    public virtual User? ResolvedByUser { get; set; }

    /// <summary>
    /// Gets or sets the stock movement for this adjustment.
    /// </summary>
    public virtual StockMovement? StockMovement { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether this item has a variance after being counted.
    /// </summary>
    public bool HasVariance => IsCounted && VarianceQuantity != 0;

    /// <summary>
    /// Gets the final count quantity (resolved or primary count).
    /// </summary>
    public decimal? FinalCountQuantity => ResolvedQuantity ?? CountedQuantity;

    /// <summary>
    /// Gets whether this item requires resolution (mismatch not resolved).
    /// </summary>
    public bool RequiresResolution => CountMismatch && !ResolvedQuantity.HasValue;

    /// <summary>
    /// Gets whether second count has been completed.
    /// </summary>
    public bool HasSecondCount => SecondCountQuantity.HasValue;

    #endregion
}
