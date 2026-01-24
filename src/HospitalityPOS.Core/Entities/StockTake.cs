using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a stock take (physical inventory count) session.
/// </summary>
public class StockTake : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique stock take number (format: SC-yyyy-NNN).
    /// </summary>
    public string StockTakeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the count type (Full, Cycle, Spot, Category).
    /// </summary>
    public StockCountType CountType { get; set; } = StockCountType.FullCount;

    /// <summary>
    /// Gets or sets the status of the stock take.
    /// </summary>
    public StockTakeStatus Status { get; set; } = StockTakeStatus.Draft;

    /// <summary>
    /// Gets or sets the scheduled count date.
    /// </summary>
    public DateTime CountDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the stock take was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    public DateTime StartDate { get => StartedAt ?? CreatedAt; set => StartedAt = value; }

    /// <summary>
    /// Gets or sets when counting was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets when the stock take was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets when adjustments were posted.
    /// </summary>
    public DateTime? PostedAt { get; set; }

    #region Scope Settings

    /// <summary>
    /// Gets or sets the category ID if this is a category-specific count.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the storage location filter.
    /// </summary>
    public string? LocationFilter { get; set; }

    /// <summary>
    /// Gets or sets whether this is a blind count (system quantities hidden).
    /// </summary>
    public bool IsBlindCount { get; set; }

    /// <summary>
    /// Gets or sets whether this is a double-blind count (two independent counters).
    /// </summary>
    public bool IsDoubleBlind { get; set; }

    /// <summary>
    /// Gets or sets the ABC class filter (A, B, C or null for all).
    /// </summary>
    public string? ABCClassFilter { get; set; }

    /// <summary>
    /// Gets or sets product IDs for spot count (comma-separated or JSON array).
    /// </summary>
    public string? SpotCountProductIds { get; set; }

    #endregion

    #region Inventory Freeze

    /// <summary>
    /// Gets or sets whether to freeze inventory during count.
    /// </summary>
    public bool FreezeInventory { get; set; }

    /// <summary>
    /// Gets or sets when inventory was frozen.
    /// </summary>
    public DateTime? FrozenAt { get; set; }

    /// <summary>
    /// Gets or sets when inventory was unfrozen.
    /// </summary>
    public DateTime? UnfrozenAt { get; set; }

    #endregion

    #region Summary Statistics

    /// <summary>
    /// Gets or sets the total number of items to count.
    /// </summary>
    public int TotalItemsToCount { get; set; }

    /// <summary>
    /// Gets or sets the number of items counted.
    /// </summary>
    public int ItemsCounted { get; set; }
    public int CountedItems { get => ItemsCounted; set => ItemsCounted = value; }

    /// <summary>
    /// Gets or sets the count of items with variance.
    /// </summary>
    public int ItemsWithVariance { get; set; }

    /// <summary>
    /// Gets or sets the total system value at count start.
    /// </summary>
    public decimal TotalSystemValue { get; set; }

    /// <summary>
    /// Gets or sets the total counted value.
    /// </summary>
    public decimal TotalCountedValue { get; set; }

    /// <summary>
    /// Gets or sets the total variance value (sum of all item variance values).
    /// </summary>
    public decimal TotalVarianceValue { get; set; }

    /// <summary>
    /// Gets or sets the shrinkage percentage.
    /// </summary>
    public decimal ShrinkagePercentage { get; set; }

    /// <summary>
    /// Gets or sets the total number of items in this stock take.
    /// </summary>
    public int TotalItems { get => TotalItemsToCount; set => TotalItemsToCount = value; }

    #endregion

    #region User Tracking

    /// <summary>
    /// Gets or sets the ID of the user who started the stock take.
    /// </summary>
    public int StartedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who approved the stock take.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the approval notes.
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who posted adjustments.
    /// </summary>
    public int? PostedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets any general notes for the stock take.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Variance Threshold Settings

    /// <summary>
    /// Gets or sets the quantity variance threshold percentage (flag if exceeded).
    /// </summary>
    public decimal? VarianceThresholdPercent { get; set; }

    /// <summary>
    /// Gets or sets the value variance threshold (flag if exceeded).
    /// </summary>
    public decimal? VarianceThresholdValue { get; set; }

    /// <summary>
    /// Gets or sets whether to require approval for any variance.
    /// </summary>
    public bool RequireApprovalForVariance { get; set; } = true;

    #endregion

    #region Journal Entry Integration

    /// <summary>
    /// Gets or sets the journal entry ID for shrinkage expense.
    /// </summary>
    public int? JournalEntryId { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the store.
    /// </summary>
    public virtual Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the category (for category counts).
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the user who started the stock take.
    /// </summary>
    public virtual User StartedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who approved the stock take.
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who posted adjustments.
    /// </summary>
    public virtual User? PostedByUser { get; set; }

    /// <summary>
    /// Gets or sets the items in this stock take.
    /// </summary>
    public virtual ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();

    /// <summary>
    /// Gets or sets the counters assigned to this stock take.
    /// </summary>
    public virtual ICollection<StockCountCounter> Counters { get; set; } = new List<StockCountCounter>();

    /// <summary>
    /// Gets or sets the journal entry for this stock take.
    /// </summary>
    public virtual JournalEntry? JournalEntry { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the progress percentage of counted items.
    /// </summary>
    public decimal ProgressPercentage => TotalItemsToCount > 0
        ? Math.Round((decimal)ItemsCounted / TotalItemsToCount * 100, 1)
        : 0;

    /// <summary>
    /// Gets whether the count can still be modified.
    /// </summary>
    public bool CanModify => Status == StockTakeStatus.Draft || Status == StockTakeStatus.InProgress;

    /// <summary>
    /// Gets whether counting is complete.
    /// </summary>
    public bool IsCountingComplete => ItemsCounted >= TotalItemsToCount && TotalItemsToCount > 0;

    #endregion
}
