using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing stock take (physical inventory) operations.
/// </summary>
public interface IStockTakeService
{
    /// <summary>
    /// Starts a new stock take session.
    /// </summary>
    /// <param name="userId">The ID of the user starting the stock take.</param>
    /// <param name="notes">Optional notes for the stock take.</param>
    /// <param name="categoryId">Optional category ID to limit products.</param>
    /// <returns>The created stock take with populated items.</returns>
    Task<StockTake> StartStockTakeAsync(int userId, string? notes = null, int? categoryId = null);

    /// <summary>
    /// Gets a stock take by ID with all items.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <returns>The stock take with items, or null if not found.</returns>
    Task<StockTake?> GetStockTakeAsync(int stockTakeId);

    /// <summary>
    /// Gets all stock takes with optional filtering.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>List of stock takes.</returns>
    Task<IEnumerable<StockTake>> GetStockTakesAsync(
        StockTakeStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets the current in-progress stock take if any.
    /// </summary>
    /// <returns>The in-progress stock take, or null.</returns>
    Task<StockTake?> GetInProgressStockTakeAsync();

    /// <summary>
    /// Records a physical count for a stock take item.
    /// </summary>
    /// <param name="stockTakeItemId">The stock take item ID.</param>
    /// <param name="physicalQuantity">The physical quantity counted.</param>
    /// <param name="userId">The ID of the user who counted.</param>
    /// <param name="notes">Optional notes for this item.</param>
    /// <returns>The updated stock take item.</returns>
    Task<StockTakeItem> RecordCountAsync(
        int stockTakeItemId,
        decimal physicalQuantity,
        int userId,
        string? notes = null);

    /// <summary>
    /// Records multiple physical counts at once.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="counts">Dictionary of product ID to physical quantity.</param>
    /// <param name="userId">The ID of the user who counted.</param>
    /// <returns>List of updated stock take items.</returns>
    Task<IEnumerable<StockTakeItem>> RecordCountsAsync(
        int stockTakeId,
        Dictionary<int, decimal> counts,
        int userId);

    /// <summary>
    /// Saves draft changes to a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <returns>The updated stock take.</returns>
    Task<StockTake> SaveDraftAsync(int stockTakeId);

    /// <summary>
    /// Submits a stock take for approval.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <returns>The updated stock take.</returns>
    Task<StockTake> SubmitForApprovalAsync(int stockTakeId);

    /// <summary>
    /// Approves a stock take and applies all adjustments.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="approverUserId">The ID of the approving user.</param>
    /// <returns>The approved stock take.</returns>
    Task<StockTake> ApproveStockTakeAsync(int stockTakeId, int approverUserId);

    /// <summary>
    /// Cancels a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <returns>The cancelled stock take.</returns>
    Task<StockTake> CancelStockTakeAsync(int stockTakeId, string reason);

    /// <summary>
    /// Gets variance summary for a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <returns>Variance summary including totals and item-level details.</returns>
    Task<StockTakeVarianceSummary> GetVarianceSummaryAsync(int stockTakeId);

    /// <summary>
    /// Generates the next stock take number.
    /// </summary>
    /// <returns>The generated stock take number (format: ST-yyyyMMdd-NNN).</returns>
    Task<string> GenerateStockTakeNumberAsync();
}

/// <summary>
/// Represents a summary of stock take variances.
/// </summary>
public class StockTakeVarianceSummary
{
    /// <summary>
    /// Gets or sets the stock take ID.
    /// </summary>
    public int StockTakeId { get; set; }

    /// <summary>
    /// Gets or sets the stock take number.
    /// </summary>
    public string StockTakeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of counted items.
    /// </summary>
    public int CountedItems { get; set; }

    /// <summary>
    /// Gets or sets the number of items with variance.
    /// </summary>
    public int ItemsWithVariance { get; set; }

    /// <summary>
    /// Gets or sets the total shortage value (negative variance).
    /// </summary>
    public decimal ShortageValue { get; set; }

    /// <summary>
    /// Gets or sets the total overage value (positive variance).
    /// </summary>
    public decimal OverageValue { get; set; }

    /// <summary>
    /// Gets or sets the net variance value.
    /// </summary>
    public decimal NetVarianceValue { get; set; }

    /// <summary>
    /// Gets or sets the items with negative variance (shortages).
    /// </summary>
    public IEnumerable<StockTakeItem> Shortages { get; set; } = new List<StockTakeItem>();

    /// <summary>
    /// Gets or sets the items with positive variance (overages).
    /// </summary>
    public IEnumerable<StockTakeItem> Overages { get; set; } = new List<StockTakeItem>();

    /// <summary>
    /// Gets the progress percentage.
    /// </summary>
    public decimal ProgressPercentage => TotalItems > 0
        ? Math.Round((decimal)CountedItems / TotalItems * 100, 1)
        : 0;
}
