using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a counter assigned to a stock take session.
/// </summary>
public class StockCountCounter : BaseEntity
{
    /// <summary>
    /// Gets or sets the stock take ID.
    /// </summary>
    public int StockTakeId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the counter.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary counter.
    /// </summary>
    public bool IsPrimaryCounter { get; set; }

    /// <summary>
    /// Gets or sets when this counter was assigned.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the counter started counting.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the counter completed counting.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of items counted by this counter.
    /// </summary>
    public int ItemsCounted { get; set; }

    /// <summary>
    /// Gets or sets notes from this counter.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Gets or sets the stock take.
    /// </summary>
    public virtual StockTake StockTake { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public virtual User User { get; set; } = null!;

    // Computed Properties

    /// <summary>
    /// Gets whether this counter has completed counting.
    /// </summary>
    public bool IsComplete => CompletedAt.HasValue;
}

/// <summary>
/// Represents a scheduled stock count configuration.
/// </summary>
public class StockCountSchedule : BaseEntity
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count type.
    /// </summary>
    public StockCountType CountType { get; set; }

    /// <summary>
    /// Gets or sets the recurrence frequency.
    /// </summary>
    public RecurrenceFrequency Frequency { get; set; }

    /// <summary>
    /// Gets or sets the day of week for weekly schedules (0 = Sunday, 6 = Saturday).
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the day of month for monthly schedules (1-31).
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Gets or sets the month for quarterly/annual schedules (1-12).
    /// </summary>
    public int? Month { get; set; }

    /// <summary>
    /// Gets or sets the time of day to create the count.
    /// </summary>
    public TimeSpan? TimeOfDay { get; set; }

    /// <summary>
    /// Gets or sets the category ID for category-specific counts.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the location filter.
    /// </summary>
    public string? LocationFilter { get; set; }

    /// <summary>
    /// Gets or sets whether this schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the last run date.
    /// </summary>
    public DateTime? LastRunDate { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled run date.
    /// </summary>
    public DateTime? NextRunDate { get; set; }

    /// <summary>
    /// Gets or sets whether to send a reminder before count.
    /// </summary>
    public bool SendReminder { get; set; }

    /// <summary>
    /// Gets or sets the number of days before to send reminder.
    /// </summary>
    public int ReminderDaysBefore { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default assignees (user IDs, comma-separated).
    /// </summary>
    public string? DefaultAssigneeIds { get; set; }

    /// <summary>
    /// Gets or sets whether to use blind count mode.
    /// </summary>
    public bool UseBlindCount { get; set; }

    /// <summary>
    /// Gets or sets whether to use double-blind count mode.
    /// </summary>
    public bool UseDoubleBlind { get; set; }

    /// <summary>
    /// Gets or sets notes about this schedule.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Gets or sets the store.
    /// </summary>
    public virtual Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the category for category counts.
    /// </summary>
    public virtual Category? Category { get; set; }

    // Computed Properties

    /// <summary>
    /// Gets whether this schedule is due.
    /// </summary>
    public bool IsDue => IsEnabled && NextRunDate.HasValue && NextRunDate.Value <= DateTime.UtcNow;
}

/// <summary>
/// Represents variance threshold configuration for a store/category.
/// </summary>
public class VarianceThreshold : BaseEntity
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the category ID (null = applies to all categories).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the product ID for product-specific thresholds.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the threshold name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute quantity threshold.
    /// </summary>
    public decimal? QuantityThreshold { get; set; }

    /// <summary>
    /// Gets or sets the percentage threshold (of system quantity).
    /// </summary>
    public decimal? PercentageThreshold { get; set; }

    /// <summary>
    /// Gets or sets the value threshold (dollar/currency amount).
    /// </summary>
    public decimal? ValueThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether to require approval when exceeded.
    /// </summary>
    public bool RequireApproval { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require explanation when exceeded.
    /// </summary>
    public bool RequireExplanation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send alert when exceeded.
    /// </summary>
    public bool SendAlert { get; set; }

    /// <summary>
    /// Gets or sets the alert recipient email addresses (comma-separated).
    /// </summary>
    public string? AlertRecipients { get; set; }

    /// <summary>
    /// Gets or sets the priority level (higher = more important).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this threshold is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties

    /// <summary>
    /// Gets or sets the store.
    /// </summary>
    public virtual Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// Checks if the given variance exceeds this threshold.
    /// </summary>
    /// <param name="varianceQuantity">The variance quantity.</param>
    /// <param name="variancePercentage">The variance percentage.</param>
    /// <param name="varianceValue">The variance value.</param>
    /// <returns>True if any threshold is exceeded.</returns>
    public bool IsExceeded(decimal varianceQuantity, decimal variancePercentage, decimal varianceValue)
    {
        var absQty = Math.Abs(varianceQuantity);
        var absPct = Math.Abs(variancePercentage);
        var absVal = Math.Abs(varianceValue);

        if (QuantityThreshold.HasValue && absQty > QuantityThreshold.Value)
            return true;

        if (PercentageThreshold.HasValue && absPct > PercentageThreshold.Value)
            return true;

        if (ValueThreshold.HasValue && absVal > ValueThreshold.Value)
            return true;

        return false;
    }
}

/// <summary>
/// Represents a variance report entry for reporting.
/// </summary>
public class StockCountVarianceReport
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
    /// Gets or sets the store name.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Gets or sets the count type.
    /// </summary>
    public StockCountType CountType { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the count date.
    /// </summary>
    public DateTime CountDate { get; set; }

    /// <summary>
    /// Gets or sets the count start date.
    /// </summary>
    public DateTime? CountStarted { get; set; }

    /// <summary>
    /// Gets or sets the count end date.
    /// </summary>
    public DateTime? CountCompleted { get; set; }

    /// <summary>
    /// Gets or sets the prepared by user name.
    /// </summary>
    public string? PreparedBy { get; set; }

    #region Executive Summary

    /// <summary>
    /// Gets or sets the total items counted.
    /// </summary>
    public int TotalItemsCounted { get; set; }

    /// <summary>
    /// Gets or sets the items with variance.
    /// </summary>
    public int ItemsWithVariance { get; set; }

    /// <summary>
    /// Gets or sets the variance percentage (items with variance / total items).
    /// </summary>
    public decimal ItemVariancePercentage { get; set; }

    /// <summary>
    /// Gets or sets the total system value.
    /// </summary>
    public decimal TotalSystemValue { get; set; }

    /// <summary>
    /// Gets or sets the total counted value.
    /// </summary>
    public decimal TotalCountedValue { get; set; }

    /// <summary>
    /// Gets or sets the total variance value.
    /// </summary>
    public decimal TotalVarianceValue { get; set; }

    /// <summary>
    /// Gets or sets the total variance percentage.
    /// </summary>
    public decimal TotalVariancePercentage { get; set; }

    /// <summary>
    /// Gets or sets the shrinkage rate.
    /// </summary>
    public decimal ShrinkageRate { get; set; }

    #endregion

    /// <summary>
    /// Gets or sets the variance breakdown by category.
    /// </summary>
    public IList<CategoryVarianceSummary> VarianceByCategory { get; set; } = new List<CategoryVarianceSummary>();

    /// <summary>
    /// Gets or sets the variance breakdown by cause.
    /// </summary>
    public IList<CauseVarianceSummary> VarianceByCause { get; set; } = new List<CauseVarianceSummary>();

    /// <summary>
    /// Gets or sets the detailed item variances.
    /// </summary>
    public IList<ItemVarianceDetail> ItemVariances { get; set; } = new List<ItemVarianceDetail>();

    /// <summary>
    /// Gets or sets items with significant variances (exceeded threshold).
    /// </summary>
    public IList<ItemVarianceDetail> SignificantVariances { get; set; } = new List<ItemVarianceDetail>();
}

/// <summary>
/// Variance summary by category.
/// </summary>
public class CategoryVarianceSummary
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ItemsCounted { get; set; }
    public decimal SystemValue { get; set; }
    public decimal CountedValue { get; set; }
    public decimal VarianceValue { get; set; }
    public decimal VariancePercentage { get; set; }
}

/// <summary>
/// Variance summary by cause.
/// </summary>
public class CauseVarianceSummary
{
    public VarianceCause Cause { get; set; }
    public string CauseName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public decimal PercentOfTotalShrinkage { get; set; }
}

/// <summary>
/// Detailed variance for a single item.
/// </summary>
public class ItemVarianceDetail
{
    public int StockTakeItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? Location { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal VarianceValue { get; set; }
    public decimal VariancePercentage { get; set; }
    public VarianceCause? Cause { get; set; }
    public string? Notes { get; set; }
    public bool ExceedsThreshold { get; set; }
    public string? CountedBy { get; set; }
    public DateTime? CountedAt { get; set; }
}

/// <summary>
/// Shrinkage analysis report.
/// </summary>
public class ShrinkageAnalysisReport
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Gets or sets the report period start.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the report period end.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the total inventory value during period.
    /// </summary>
    public decimal TotalInventoryValue { get; set; }

    /// <summary>
    /// Gets or sets the total shrinkage value.
    /// </summary>
    public decimal TotalShrinkageValue { get; set; }

    /// <summary>
    /// Gets or sets the shrinkage rate.
    /// </summary>
    public decimal ShrinkageRate { get; set; }

    /// <summary>
    /// Gets or sets the number of counts during period.
    /// </summary>
    public int CountsDuringPeriod { get; set; }

    /// <summary>
    /// Gets or sets the shrinkage trend (month-over-month).
    /// </summary>
    public IList<ShrinkageTrend> MonthlyTrend { get; set; } = new List<ShrinkageTrend>();

    /// <summary>
    /// Gets or sets the shrinkage by category.
    /// </summary>
    public IList<CategoryVarianceSummary> ByCategory { get; set; } = new List<CategoryVarianceSummary>();

    /// <summary>
    /// Gets or sets the shrinkage by cause.
    /// </summary>
    public IList<CauseVarianceSummary> ByCause { get; set; } = new List<CauseVarianceSummary>();

    /// <summary>
    /// Gets or sets the top shrinking items.
    /// </summary>
    public IList<ItemVarianceDetail> TopShrinkingItems { get; set; } = new List<ItemVarianceDetail>();

    /// <summary>
    /// Gets or sets items with recurring variances.
    /// </summary>
    public IList<RecurringVarianceItem> RecurringVariances { get; set; } = new List<RecurringVarianceItem>();
}

/// <summary>
/// Shrinkage trend data point.
/// </summary>
public class ShrinkageTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal ShrinkageValue { get; set; }
    public decimal ShrinkageRate { get; set; }
    public int CountsPerformed { get; set; }
}

/// <summary>
/// Item with recurring variances.
/// </summary>
public class RecurringVarianceItem
{
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VarianceOccurrences { get; set; }
    public decimal AverageVarianceQuantity { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public VarianceCause? MostCommonCause { get; set; }
}

/// <summary>
/// Historical variance report comparing multiple counts.
/// </summary>
public class HistoricalVarianceReport
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public IList<StockCountSummary> CountSummaries { get; set; } = new List<StockCountSummary>();
    public IList<ShrinkageTrend> Trend { get; set; } = new List<ShrinkageTrend>();
}

/// <summary>
/// Summary of a single stock count for historical comparison.
/// </summary>
public class StockCountSummary
{
    public int StockTakeId { get; set; }
    public string StockTakeNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public StockCountType CountType { get; set; }
    public int ItemsCounted { get; set; }
    public int ItemsWithVariance { get; set; }
    public decimal TotalSystemValue { get; set; }
    public decimal TotalCountedValue { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public decimal ShrinkageRate { get; set; }
}

/// <summary>
/// DTO for creating a new stock count.
/// </summary>
public class CreateStockCountDto
{
    public int? StoreId { get; set; }
    public StockCountType CountType { get; set; } = StockCountType.FullCount;
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
    public int? CategoryId { get; set; }
    public string? LocationFilter { get; set; }
    public bool IsBlindCount { get; set; }
    public bool IsDoubleBlind { get; set; }
    public bool FreezeInventory { get; set; }
    public string? ABCClassFilter { get; set; }
    public IList<int>? SpotCountProductIds { get; set; }
    public decimal? VarianceThresholdPercent { get; set; }
    public decimal? VarianceThresholdValue { get; set; }
    public string? Notes { get; set; }
    public IList<int>? AssignedCounterUserIds { get; set; }
}

/// <summary>
/// DTO for count entry.
/// </summary>
public class CountEntryDto
{
    public int ProductId { get; set; }
    public decimal CountedQuantity { get; set; }
    public string? Notes { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// Filter DTO for stock count queries.
/// </summary>
public class StockCountFilterDto
{
    public int? StoreId { get; set; }
    public StockTakeStatus? Status { get; set; }
    public StockCountType? CountType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CategoryId { get; set; }
    public int? AssignedUserId { get; set; }
    public bool? HasVariance { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}

/// <summary>
/// Filter DTO for shrinkage report.
/// </summary>
public class ShrinkageReportFilterDto
{
    public int? StoreId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? CategoryId { get; set; }
    public int TopItemsCount { get; set; } = 20;
}

/// <summary>
/// DTO for updating stock count schedule.
/// </summary>
public class StockCountScheduleDto
{
    public int? Id { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public StockCountType CountType { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int? Month { get; set; }
    public TimeSpan? TimeOfDay { get; set; }
    public int? CategoryId { get; set; }
    public string? LocationFilter { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool SendReminder { get; set; }
    public int ReminderDaysBefore { get; set; } = 1;
    public IList<int>? DefaultAssigneeIds { get; set; }
    public bool UseBlindCount { get; set; }
    public bool UseDoubleBlind { get; set; }
    public string? Notes { get; set; }
}
