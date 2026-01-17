using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for KDS order timing and priority management.
/// </summary>
public interface IKdsTimerService
{
    #region Timer Status

    /// <summary>
    /// Gets the timer status for a KDS order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>Timer status with color and alerts.</returns>
    Task<KdsTimerStatusDto> GetOrderTimerStatusAsync(int kdsOrderId);

    /// <summary>
    /// Gets the timer status for a KDS order entity (synchronous).
    /// </summary>
    /// <param name="order">The KDS order.</param>
    /// <returns>Timer status with color and alerts.</returns>
    KdsTimerStatusDto GetOrderTimerStatus(KdsOrder order);

    /// <summary>
    /// Gets timer status for multiple orders.
    /// </summary>
    /// <param name="kdsOrderIds">List of KDS order IDs.</param>
    /// <returns>Dictionary of order ID to timer status.</returns>
    Task<Dictionary<int, KdsTimerStatusDto>> GetOrderTimerStatusesAsync(List<int> kdsOrderIds);

    /// <summary>
    /// Calculates the timer color based on elapsed time.
    /// </summary>
    /// <param name="elapsed">Time elapsed since order received.</param>
    /// <param name="storeId">Optional store ID for store-specific settings.</param>
    /// <returns>Timer color.</returns>
    TimerColorDto CalculateTimerColor(TimeSpan elapsed, int? storeId = null);

    #endregion

    #region Priority Management

    /// <summary>
    /// Marks an order as rush priority.
    /// </summary>
    /// <param name="dto">Mark rush request.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> MarkAsRushAsync(MarkRushOrderDto dto);

    /// <summary>
    /// Clears rush priority from an order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="userId">The user clearing the priority.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> ClearRushAsync(int kdsOrderId, int? userId = null);

    /// <summary>
    /// Updates order priority.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="priority">The new priority level.</param>
    /// <param name="userId">The user updating the priority.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> UpdatePriorityAsync(int kdsOrderId, OrderPriorityDto priority, int? userId = null);

    /// <summary>
    /// Gets rush/priority orders for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of priority orders.</returns>
    Task<List<KdsOrderDto>> GetPriorityOrdersAsync(int stationId);

    #endregion

    #region Overdue Detection

    /// <summary>
    /// Gets overdue orders for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of overdue orders.</returns>
    Task<List<KdsOrderDto>> GetOverdueOrdersAsync(int stationId);

    /// <summary>
    /// Gets overdue orders summary for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>Overdue orders summary.</returns>
    Task<OverdueOrdersSummaryDto> GetOverdueOrdersSummaryAsync(int stationId);

    /// <summary>
    /// Checks if an order is overdue.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>True if the order is overdue.</returns>
    Task<bool> IsOrderOverdueAsync(int kdsOrderId);

    /// <summary>
    /// Gets all overdue orders across all stations for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of overdue orders.</returns>
    Task<List<KdsOrderDto>> GetAllOverdueOrdersAsync(int storeId);

    #endregion

    #region Timer Configuration

    /// <summary>
    /// Gets the timer configuration for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>Timer configuration.</returns>
    Task<KdsTimerConfigDto> GetTimerConfigAsync(int storeId);

    /// <summary>
    /// Updates the timer configuration for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="dto">Configuration update data.</param>
    /// <returns>Updated timer configuration.</returns>
    Task<KdsTimerConfigDto> UpdateTimerConfigAsync(int storeId, UpdateKdsTimerConfigDto dto);

    /// <summary>
    /// Creates timer configuration for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>Created timer configuration.</returns>
    Task<KdsTimerConfigDto> CreateTimerConfigAsync(int storeId);

    /// <summary>
    /// Gets default timer configuration.
    /// </summary>
    /// <returns>Default timer configuration.</returns>
    KdsTimerConfigDto GetDefaultTimerConfig();

    #endregion

    #region Alert Management

    /// <summary>
    /// Determines if audio alert should play for an order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="lastAlertTime">Last time an alert was played.</param>
    /// <returns>True if audio alert should play.</returns>
    Task<bool> ShouldPlayAudioAlertAsync(int kdsOrderId, DateTime? lastAlertTime = null);

    /// <summary>
    /// Determines if order should flash on display.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>True if order should flash.</returns>
    Task<bool> ShouldFlashOrderAsync(int kdsOrderId);

    /// <summary>
    /// Gets orders that need alerts at a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of order IDs needing alerts.</returns>
    Task<List<int>> GetOrdersNeedingAlertsAsync(int stationId);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets average time in each status for orders at a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Dictionary of status to average time.</returns>
    Task<Dictionary<KdsOrderStatusDto, TimeSpan>> GetAverageTimeByStatusAsync(
        int stationId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets on-time performance percentage for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Percentage of orders completed on time.</returns>
    Task<decimal> GetOnTimePercentageAsync(int stationId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when an order becomes overdue.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderOverdue;

    /// <summary>
    /// Event raised when order priority changes.
    /// </summary>
    event EventHandler<KdsOrderPriorityChangeEventArgs>? PriorityChanged;

    /// <summary>
    /// Event raised when an audio alert should play.
    /// </summary>
    event EventHandler<AudioAlertEventArgs>? AudioAlertNeeded;

    #endregion
}

/// <summary>
/// Event arguments for order priority change.
/// </summary>
public class KdsOrderPriorityChangeEventArgs : EventArgs
{
    public int KdsOrderId { get; set; }
    public OrderPriorityDto PreviousPriority { get; set; }
    public OrderPriorityDto NewPriority { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Event arguments for audio alert.
/// </summary>
public class AudioAlertEventArgs : EventArgs
{
    public int StationId { get; set; }
    public List<int> OverdueOrderIds { get; set; } = new();
    public string AlertType { get; set; } = "Overdue";
}
