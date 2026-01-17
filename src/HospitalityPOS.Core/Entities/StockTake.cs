using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a stock take (physical inventory count) session.
/// </summary>
public class StockTake : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique stock take number (format: ST-yyyyMMdd-sequence).
    /// </summary>
    public string StockTakeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the stock take was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the stock take was completed/approved.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who started the stock take.
    /// </summary>
    public int StartedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who approved the stock take.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the status of the stock take.
    /// </summary>
    public StockTakeStatus Status { get; set; } = StockTakeStatus.InProgress;

    /// <summary>
    /// Gets or sets any notes for the stock take.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the total variance value (sum of all item variance values).
    /// </summary>
    public decimal TotalVarianceValue { get; set; }

    /// <summary>
    /// Gets or sets the count of items with variance.
    /// </summary>
    public int ItemsWithVariance { get; set; }

    /// <summary>
    /// Gets or sets the total number of items in this stock take.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of items that have been counted.
    /// </summary>
    public int CountedItems { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user who started the stock take.
    /// </summary>
    public virtual User StartedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who approved the stock take.
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Gets or sets the items in this stock take.
    /// </summary>
    public virtual ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();

    /// <summary>
    /// Gets the progress percentage of counted items.
    /// </summary>
    public decimal ProgressPercentage => TotalItems > 0 ? Math.Round((decimal)CountedItems / TotalItems * 100, 1) : 0;
}
