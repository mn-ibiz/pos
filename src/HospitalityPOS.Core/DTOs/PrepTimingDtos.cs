using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Configuration DTOs

/// <summary>
/// Prep timing configuration DTO.
/// </summary>
public class PrepTimingConfigurationDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public bool EnablePrepTiming { get; set; }
    public int DefaultPrepTimeSeconds { get; set; }
    public int MinPrepTimeSeconds { get; set; }
    public int TargetReadyBufferSeconds { get; set; }
    public bool AllowManualFireOverride { get; set; }
    public bool ShowWaitingItemsOnStation { get; set; }
    public PrepTimingMode Mode { get; set; }
    public bool AutoFireEnabled { get; set; }
    public int OverdueThresholdSeconds { get; set; }
    public bool AlertOnOverdue { get; set; }
}

#endregion

#region Product Prep Time DTOs

/// <summary>
/// Product prep time DTO.
/// </summary>
public class ProductPrepTimeDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int PrepTimeSeconds { get; set; }
    public int TotalPrepTimeSeconds { get; set; }
    public bool UsesPrepTiming { get; set; }
    public bool IsTimingIntegral { get; set; }
    public int? StoreId { get; set; }
}

/// <summary>
/// Modifier prep time adjustment DTO.
/// </summary>
public class ModifierPrepTimeDto
{
    public int ModifierItemId { get; set; }
    public string ModifierItemName { get; set; } = string.Empty;
    public int AdjustmentSeconds { get; set; }
    public PrepTimeAdjustmentType AdjustmentType { get; set; }
}

/// <summary>
/// Category default prep time DTO.
/// </summary>
public class CategoryPrepTimeDefaultDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DefaultPrepTimeMinutes { get; set; }
    public int DefaultPrepTimeSeconds { get; set; }
    public int TotalPrepTimeSeconds { get; set; }
    public int? StoreId { get; set; }
}

/// <summary>
/// Request to set product prep time.
/// </summary>
public class SetProductPrepTimeRequest
{
    public int ProductId { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int PrepTimeSeconds { get; set; }
    public bool UsesPrepTiming { get; set; } = true;
    public bool IsTimingIntegral { get; set; } = true;
    public int? StoreId { get; set; }
}

/// <summary>
/// Request to bulk update prep times.
/// </summary>
public class BulkPrepTimeUpdateRequest
{
    public List<SetProductPrepTimeRequest> Products { get; set; } = new();
}

#endregion

#region Fire Schedule DTOs

/// <summary>
/// Item fire schedule DTO.
/// </summary>
public class ItemFireScheduleDto
{
    public int Id { get; set; }
    public int KdsOrderItemId { get; set; }
    public int KdsOrderId { get; set; }
    public int? CourseNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? StationId { get; set; }
    public string? StationName { get; set; }
    public int PrepTimeSeconds { get; set; }
    public DateTime OrderReceivedAt { get; set; }
    public DateTime TargetReadyAt { get; set; }
    public DateTime ScheduledFireAt { get; set; }
    public DateTime? ActualFiredAt { get; set; }
    public DateTime? ActualReadyAt { get; set; }
    public ItemFireStatus Status { get; set; }
    public bool WasManuallyFired { get; set; }
    public int? FiredByUserId { get; set; }
    public string? FiredByUserName { get; set; }
    public TimeSpan? TimeUntilFire { get; set; }
    public TimeSpan? TimeUntilReady { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>
/// Result of firing an item.
/// </summary>
public class FireResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<int> FiredItemIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime FiredAt { get; set; }
}

#endregion

#region Status DTOs

/// <summary>
/// Overall prep timing status for an order.
/// </summary>
public class PrepTimingStatus
{
    public int KdsOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderReceivedAt { get; set; }
    public DateTime TargetReadyAt { get; set; }
    public TimeSpan TimeUntilReady { get; set; }
    public List<ItemTimingStatus> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int FiredItems { get; set; }
    public int WaitingItems { get; set; }
    public int PreparingItems { get; set; }
    public int CompletedItems { get; set; }
    public TimeSpan LongestPrepTime { get; set; }
    public bool IsOnTrack { get; set; }
    public decimal CompletionPercentage { get; set; }
}

/// <summary>
/// Status of an individual item in the timing schedule.
/// </summary>
public class ItemTimingStatus
{
    public int KdsOrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PrepTimeSeconds { get; set; }
    public ItemFireStatus Status { get; set; }
    public DateTime? ScheduledFireAt { get; set; }
    public DateTime? ActualFiredAt { get; set; }
    public DateTime TargetReadyAt { get; set; }
    public TimeSpan? TimeUntilFire { get; set; }
    public TimeSpan? TimeUntilReady { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public bool IsOverdue { get; set; }
    public string? StationName { get; set; }
    public decimal ProgressPercentage { get; set; }
}

/// <summary>
/// KDS item display model with prep timing info.
/// </summary>
public class KdsItemTimingDisplay
{
    public int ItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ItemFireStatus FireStatus { get; set; }
    public TimeSpan? TimeUntilFire { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public string DisplayStyle { get; set; } = "normal"; // "normal", "waiting", "overdue", "preparing"
    public bool IsActionable { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = new();
}

#endregion

#region Analytics DTOs

/// <summary>
/// Prep timing accuracy report.
/// </summary>
public class PrepTimingAccuracyReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalItemsScheduled { get; set; }
    public int ItemsFiredOnTime { get; set; }
    public int ItemsFiredLate { get; set; }
    public int ItemsManuallyFired { get; set; }
    public decimal OnTimeFireRate { get; set; }
    public int ItemsCompletedOnTarget { get; set; }
    public int ItemsCompletedEarly { get; set; }
    public int ItemsCompletedLate { get; set; }
    public decimal CompletionAccuracyRate { get; set; }
    public int AverageDeviationSeconds { get; set; }
    public List<DailyAccuracyMetrics> DailyMetrics { get; set; } = new();
}

/// <summary>
/// Daily accuracy metrics.
/// </summary>
public class DailyAccuracyMetrics
{
    public DateTime Date { get; set; }
    public int TotalItems { get; set; }
    public decimal AccuracyRate { get; set; }
    public int AverageDeviationSeconds { get; set; }
}

/// <summary>
/// Product prep time accuracy report.
/// </summary>
public class ProductAccuracyReport
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ConfiguredPrepTimeSeconds { get; set; }
    public int AverageActualPrepTimeSeconds { get; set; }
    public int SampleCount { get; set; }
    public decimal StandardDeviationSeconds { get; set; }
    public int MinPrepTimeSeconds { get; set; }
    public int MaxPrepTimeSeconds { get; set; }
    public int SuggestedPrepTimeSeconds { get; set; }
    public decimal AccuracyRate { get; set; }
    public int VarianceSeconds { get; set; }
    public bool NeedsAdjustment { get; set; }
}

/// <summary>
/// Summary of prep timing performance.
/// </summary>
public class PrepTimingPerformanceSummary
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersCompletedOnTime { get; set; }
    public decimal OnTimeCompletionRate { get; set; }
    public TimeSpan AverageOrderPrepTime { get; set; }
    public TimeSpan AverageDeviation { get; set; }
    public List<StationPerformance> ByStation { get; set; } = new();
    public List<ProductAccuracyReport> ProductsNeedingAdjustment { get; set; } = new();
}

/// <summary>
/// Station-level performance metrics.
/// </summary>
public class StationPerformance
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal AccuracyRate { get; set; }
    public int AverageDeviationSeconds { get; set; }
}

#endregion

#region Job Result DTOs

/// <summary>
/// Result of the prep timing background job run.
/// </summary>
public class PrepTimingJobResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFired { get; set; }
    public int ItemsMarkedOverdue { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;
}

#endregion
