using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for KDS prep timing and sequencing.
/// Ensures all items in an order/course are ready simultaneously.
/// </summary>
public interface IKdsPrepTimingService
{
    #region Configuration

    /// <summary>
    /// Gets prep timing configuration for a store.
    /// </summary>
    Task<PrepTimingConfigurationDto> GetConfigurationAsync(int storeId);

    /// <summary>
    /// Updates prep timing configuration.
    /// </summary>
    Task<PrepTimingConfigurationDto> UpdateConfigurationAsync(PrepTimingConfigurationDto config);

    /// <summary>
    /// Checks if prep timing is enabled for a store.
    /// </summary>
    Task<bool> IsPrepTimingEnabledAsync(int storeId);

    #endregion

    #region Product Prep Times

    /// <summary>
    /// Gets prep time for a product in seconds.
    /// </summary>
    Task<int> GetProductPrepTimeAsync(int productId, int? storeId = null);

    /// <summary>
    /// Sets prep time for a product.
    /// </summary>
    Task SetProductPrepTimeAsync(SetProductPrepTimeRequest request);

    /// <summary>
    /// Calculates total prep time for an item including modifiers.
    /// </summary>
    Task<int> CalculateItemPrepTimeAsync(int productId, List<int> modifierIds, int? storeId = null);

    /// <summary>
    /// Gets all product prep times for a store.
    /// </summary>
    Task<List<ProductPrepTimeDto>> GetAllProductPrepTimesAsync(int? storeId = null);

    /// <summary>
    /// Bulk updates prep times for multiple products.
    /// </summary>
    Task BulkUpdatePrepTimesAsync(BulkPrepTimeUpdateRequest request);

    /// <summary>
    /// Gets category default prep times.
    /// </summary>
    Task<List<CategoryPrepTimeDefaultDto>> GetCategoryPrepTimeDefaultsAsync(int? storeId = null);

    /// <summary>
    /// Sets category default prep time.
    /// </summary>
    Task SetCategoryPrepTimeDefaultAsync(int categoryId, int minutes, int seconds, int? storeId = null);

    /// <summary>
    /// Gets modifier prep time adjustments.
    /// </summary>
    Task<List<ModifierPrepTimeDto>> GetModifierPrepTimeAdjustmentsAsync(int? storeId = null);

    /// <summary>
    /// Sets modifier prep time adjustment.
    /// </summary>
    Task SetModifierPrepTimeAdjustmentAsync(int modifierItemId, int adjustmentSeconds, PrepTimeAdjustmentType type, int? storeId = null);

    #endregion

    #region Fire Schedule Calculation

    /// <summary>
    /// Calculates fire schedule for all items in an order.
    /// </summary>
    Task<List<ItemFireScheduleDto>> CalculateFireScheduleAsync(int kdsOrderId);

    /// <summary>
    /// Calculates fire schedule for a specific course in an order.
    /// </summary>
    Task<List<ItemFireScheduleDto>> CalculateCourseFireScheduleAsync(int kdsOrderId, int courseNumber);

    /// <summary>
    /// Recalculates fire time for a specific item.
    /// </summary>
    Task<ItemFireScheduleDto> RecalculateItemFireTimeAsync(int kdsOrderItemId);

    /// <summary>
    /// Updates target ready time for an order and recalculates schedules.
    /// </summary>
    Task UpdateTargetReadyTimeAsync(int kdsOrderId, DateTime newTargetTime);

    /// <summary>
    /// Creates fire schedules for a new order.
    /// </summary>
    Task<List<ItemFireScheduleDto>> CreateFireSchedulesForOrderAsync(int kdsOrderId);

    #endregion

    #region Schedule Execution

    /// <summary>
    /// Processes all scheduled fires that are due (background job).
    /// </summary>
    Task<PrepTimingJobResult> ProcessScheduledFiresAsync();

    /// <summary>
    /// Manually fires an item to its station.
    /// </summary>
    Task<FireResult> FireItemAsync(int kdsOrderItemId, int userId, string? notes = null);

    /// <summary>
    /// Manually fires multiple items.
    /// </summary>
    Task<FireResult> FireItemsAsync(List<int> kdsOrderItemIds, int userId, string? notes = null);

    /// <summary>
    /// Fires all waiting items for an order immediately.
    /// </summary>
    Task<FireResult> FireAllOrderItemsAsync(int kdsOrderId, int userId);

    /// <summary>
    /// Puts an item on hold.
    /// </summary>
    Task HoldItemAsync(int kdsOrderItemId, string? reason = null);

    /// <summary>
    /// Releases a held item back to waiting.
    /// </summary>
    Task ReleaseHeldItemAsync(int kdsOrderItemId);

    /// <summary>
    /// Marks an item as done.
    /// </summary>
    Task MarkItemDoneAsync(int kdsOrderItemId);

    #endregion

    #region Status Tracking

    /// <summary>
    /// Gets full prep timing status for an order.
    /// </summary>
    Task<PrepTimingStatus> GetOrderPrepTimingStatusAsync(int kdsOrderId);

    /// <summary>
    /// Gets all scheduled items for a store.
    /// </summary>
    Task<List<ItemFireScheduleDto>> GetScheduledItemsAsync(int storeId);

    /// <summary>
    /// Gets waiting items for a specific station.
    /// </summary>
    Task<List<ItemFireScheduleDto>> GetWaitingItemsAsync(int stationId);

    /// <summary>
    /// Gets items ready to fire (past their scheduled time).
    /// </summary>
    Task<List<ItemFireScheduleDto>> GetReadyToFireItemsAsync(int storeId);

    /// <summary>
    /// Gets overdue items.
    /// </summary>
    Task<List<ItemFireScheduleDto>> GetOverdueItemsAsync(int storeId);

    /// <summary>
    /// Gets fire schedule for a specific item.
    /// </summary>
    Task<ItemFireScheduleDto?> GetItemFireScheduleAsync(int kdsOrderItemId);

    /// <summary>
    /// Gets display data for items on a station with timing info.
    /// </summary>
    Task<List<KdsItemTimingDisplay>> GetStationItemsWithTimingAsync(int stationId);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets prep timing accuracy for a date range.
    /// </summary>
    Task<PrepTimingAccuracyReport> GetPrepTimingAccuracyAsync(int storeId, DateTime from, DateTime to);

    /// <summary>
    /// Gets product-level accuracy data.
    /// </summary>
    Task<List<ProductAccuracyReport>> GetProductAccuracyAsync(int storeId, DateTime from, DateTime to);

    /// <summary>
    /// Gets performance summary.
    /// </summary>
    Task<PrepTimingPerformanceSummary> GetPerformanceSummaryAsync(int storeId, DateTime from, DateTime to);

    /// <summary>
    /// Updates product accuracy data based on recent completions.
    /// </summary>
    Task UpdateProductAccuracyDataAsync(int productId, int storeId);

    /// <summary>
    /// Gets products that need prep time adjustment.
    /// </summary>
    Task<List<ProductAccuracyReport>> GetProductsNeedingAdjustmentAsync(int storeId, decimal varianceThreshold = 0.2m);

    #endregion
}

/// <summary>
/// Interface for the prep timing background job.
/// </summary>
public interface IPrepTimingJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    PrepTimingJobResult? LastResult { get; }

    /// <summary>
    /// Triggers an immediate processing run.
    /// </summary>
    Task TriggerProcessingAsync();
}
