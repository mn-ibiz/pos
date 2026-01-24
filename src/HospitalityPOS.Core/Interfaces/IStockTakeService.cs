using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing stock take (physical inventory) operations.
/// </summary>
public interface IStockTakeService
{
    #region Session Management

    /// <summary>
    /// Creates a new stock take session with configurable options.
    /// </summary>
    /// <param name="dto">The stock take creation parameters.</param>
    /// <param name="userId">The ID of the user creating the stock take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created stock take with populated items.</returns>
    Task<StockTake> CreateStockCountAsync(CreateStockCountDto dto, int userId, CancellationToken ct = default);

    /// <summary>
    /// Starts a new stock take session (legacy method).
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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stock take with items, or null if not found.</returns>
    Task<StockTake?> GetStockTakeAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Gets all stock takes with optional filtering.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of stock takes.</returns>
    Task<IEnumerable<StockTake>> GetStockCountsAsync(StockCountFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Gets all stock takes with optional filtering (legacy method).
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
    /// Starts the counting phase (transitions from Draft to InProgress).
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated stock take.</returns>
    Task<StockTake> StartCountingAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Cancels a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cancelled stock take.</returns>
    Task<StockTake> CancelStockCountAsync(int stockTakeId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Cancels a stock take (legacy method).
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <returns>The cancelled stock take.</returns>
    Task<StockTake> CancelStockTakeAsync(int stockTakeId, string reason);

    #endregion

    #region Counting

    /// <summary>
    /// Records a physical count for a stock take item.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="countedQuantity">The counted quantity.</param>
    /// <param name="userId">The ID of the user who counted.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated stock take item.</returns>
    Task<StockTakeItem> RecordCountAsync(
        int stockTakeId,
        int productId,
        decimal countedQuantity,
        int userId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Records a physical count by item ID (legacy method).
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
    /// Records multiple counts in a batch.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="entries">The count entries.</param>
    /// <param name="userId">The ID of the user who counted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of updated stock take items.</returns>
    Task<IEnumerable<StockTakeItem>> RecordBatchCountAsync(
        int stockTakeId,
        IEnumerable<CountEntryDto> entries,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Records multiple physical counts at once (legacy method).
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
    /// Records a second count for double-blind counting.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="countedQuantity">The counted quantity.</param>
    /// <param name="userId">The ID of the second counter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated stock take item.</returns>
    Task<StockTakeItem> RecordSecondCountAsync(
        int stockTakeId,
        int productId,
        decimal countedQuantity,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a count mismatch between two counters.
    /// </summary>
    /// <param name="stockTakeItemId">The item ID.</param>
    /// <param name="resolvedQuantity">The resolved quantity.</param>
    /// <param name="userId">The ID of the manager resolving.</param>
    /// <param name="notes">Resolution notes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item.</returns>
    Task<StockTakeItem> ResolveMismatchAsync(
        int stockTakeItemId,
        decimal resolvedQuantity,
        int userId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks counting as complete.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated stock take.</returns>
    Task<StockTake> CompleteCountingAsync(int stockTakeId, CancellationToken ct = default);

    #endregion

    #region Variance Management

    /// <summary>
    /// Calculates variances for all items in a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CalculateVariancesAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Sets the variance cause for an item.
    /// </summary>
    /// <param name="stockTakeItemId">The item ID.</param>
    /// <param name="cause">The variance cause.</param>
    /// <param name="notes">Notes explaining the variance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item.</returns>
    Task<StockTakeItem> SetVarianceCauseAsync(
        int stockTakeItemId,
        VarianceCause cause,
        string? notes,
        CancellationToken ct = default);

    /// <summary>
    /// Gets items with variances.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="exceedsThresholdOnly">Whether to return only items exceeding threshold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of variance items.</returns>
    Task<IEnumerable<StockTakeItem>> GetVarianceItemsAsync(
        int stockTakeId,
        bool exceedsThresholdOnly = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets variance summary for a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <returns>Variance summary including totals and item-level details.</returns>
    Task<StockTakeVarianceSummary> GetVarianceSummaryAsync(int stockTakeId);

    #endregion

    #region Approval & Posting

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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated stock take.</returns>
    Task<StockTake> SubmitForApprovalAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Approves a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="approverUserId">The ID of the approving user.</param>
    /// <param name="notes">Approval notes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The approved stock take.</returns>
    Task<StockTake> ApproveStockCountAsync(
        int stockTakeId,
        int approverUserId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Approves a stock take and applies adjustments (legacy method).
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="approverUserId">The ID of the approving user.</param>
    /// <returns>The approved stock take.</returns>
    Task<StockTake> ApproveStockTakeAsync(int stockTakeId, int approverUserId);

    /// <summary>
    /// Rejects a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="approverUserId">The ID of the rejecting user.</param>
    /// <param name="reason">Rejection reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rejected stock take.</returns>
    Task<StockTake> RejectStockCountAsync(
        int stockTakeId,
        int approverUserId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Posts inventory adjustments.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="userId">The user posting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The posted stock take.</returns>
    Task<StockTake> PostAdjustmentsAsync(int stockTakeId, int userId, CancellationToken ct = default);

    #endregion

    #region Counter Management

    /// <summary>
    /// Assigns a counter to a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="userId">The counter user ID.</param>
    /// <param name="isPrimary">Whether this is the primary counter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The counter assignment.</returns>
    Task<StockCountCounter> AssignCounterAsync(
        int stockTakeId,
        int userId,
        bool isPrimary = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets counters assigned to a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of counters.</returns>
    Task<IEnumerable<StockCountCounter>> GetCountersAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Marks counter as started.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="userId">The counter user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The counter.</returns>
    Task<StockCountCounter> StartCounterSessionAsync(int stockTakeId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Marks counter as completed.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="userId">The counter user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The counter.</returns>
    Task<StockCountCounter> CompleteCounterSessionAsync(int stockTakeId, int userId, CancellationToken ct = default);

    #endregion

    #region Reporting

    /// <summary>
    /// Gets the variance report for a stock take.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The variance report.</returns>
    Task<StockCountVarianceReport> GetVarianceReportAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Gets shrinkage analysis report.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The shrinkage report.</returns>
    Task<ShrinkageAnalysisReport> GetShrinkageAnalysisAsync(ShrinkageReportFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Gets historical variance report.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The historical report.</returns>
    Task<HistoricalVarianceReport> GetHistoricalVarianceAsync(
        int storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports count sheet for printing.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF byte array.</returns>
    Task<byte[]> ExportCountSheetAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Exports variance report.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="format">Export format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export file byte array.</returns>
    Task<byte[]> ExportVarianceReportAsync(int stockTakeId, ExportFormat format, CancellationToken ct = default);

    /// <summary>
    /// Generates variance report HTML.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTML string.</returns>
    Task<string> GenerateVarianceReportHtmlAsync(int stockTakeId, CancellationToken ct = default);

    #endregion

    #region Scheduling

    /// <summary>
    /// Gets schedules for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of schedules.</returns>
    Task<IEnumerable<StockCountSchedule>> GetSchedulesAsync(int storeId, CancellationToken ct = default);

    /// <summary>
    /// Gets a schedule by ID.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The schedule.</returns>
    Task<StockCountSchedule?> GetScheduleAsync(int scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a schedule.
    /// </summary>
    /// <param name="dto">The schedule DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated schedule.</returns>
    Task<StockCountSchedule> SaveScheduleAsync(StockCountScheduleDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Triggers scheduled counts that are due.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of created stock takes.</returns>
    Task<IEnumerable<StockTake>> TriggerScheduledCountsAsync(CancellationToken ct = default);

    #endregion

    #region Threshold Configuration

    /// <summary>
    /// Gets thresholds for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of thresholds.</returns>
    Task<IEnumerable<VarianceThreshold>> GetThresholdsAsync(int storeId, CancellationToken ct = default);

    /// <summary>
    /// Saves a threshold configuration.
    /// </summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved threshold.</returns>
    Task<VarianceThreshold> SaveThresholdAsync(VarianceThreshold threshold, CancellationToken ct = default);

    /// <summary>
    /// Deletes a threshold configuration.
    /// </summary>
    /// <param name="thresholdId">The threshold ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteThresholdAsync(int thresholdId, CancellationToken ct = default);

    /// <summary>
    /// Gets the applicable threshold for a product.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The applicable threshold, if any.</returns>
    Task<VarianceThreshold?> GetApplicableThresholdAsync(
        int storeId,
        int productId,
        int? categoryId,
        CancellationToken ct = default);

    #endregion

    #region Utility

    /// <summary>
    /// Generates the next stock take number.
    /// </summary>
    /// <returns>The generated stock take number (format: SC-yyyy-NNN).</returns>
    Task<string> GenerateStockTakeNumberAsync();

    /// <summary>
    /// Gets items pending count by a user.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Items pending count.</returns>
    Task<IEnumerable<StockTakeItem>> GetItemsPendingCountAsync(
        int stockTakeId,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets count progress statistics.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Progress statistics.</returns>
    Task<StockCountProgress> GetCountProgressAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Freezes inventory for counting (blocks transactions).
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FreezeInventoryAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Unfreezes inventory after counting.
    /// </summary>
    /// <param name="stockTakeId">The stock take ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnfreezeInventoryAsync(int stockTakeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if inventory is currently frozen.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if frozen.</returns>
    Task<bool> IsInventoryFrozenAsync(int? storeId, CancellationToken ct = default);

    #endregion
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
    /// Gets or sets the total system value.
    /// </summary>
    public decimal TotalSystemValue { get; set; }

    /// <summary>
    /// Gets or sets the total counted value.
    /// </summary>
    public decimal TotalCountedValue { get; set; }

    /// <summary>
    /// Gets or sets the shrinkage rate.
    /// </summary>
    public decimal ShrinkageRate { get; set; }

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

/// <summary>
/// Stock count progress statistics.
/// </summary>
public class StockCountProgress
{
    public int StockTakeId { get; set; }
    public int TotalItems { get; set; }
    public int ItemsCounted { get; set; }
    public int ItemsRemaining { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int ItemsWithVariance { get; set; }
    public int ItemsExceedingThreshold { get; set; }
    public int MismatchesRequiringResolution { get; set; }
    public IDictionary<int, CounterProgress>? CounterProgress { get; set; }
}

/// <summary>
/// Individual counter progress.
/// </summary>
public class CounterProgress
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int ItemsCounted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsComplete { get; set; }
}
