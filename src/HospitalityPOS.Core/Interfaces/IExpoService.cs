using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for expo station management and all-call messaging.
/// </summary>
public interface IExpoService
{
    #region Expo Order View

    /// <summary>
    /// Gets all orders with station status for expo display.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of expo order views.</returns>
    Task<List<ExpoOrderViewDto>> GetAllOrdersAsync(int storeId);

    /// <summary>
    /// Gets a specific order with station status breakdown.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>Expo order view with station statuses.</returns>
    Task<ExpoOrderViewDto?> GetOrderWithStationStatusAsync(int kdsOrderId);

    /// <summary>
    /// Gets the full expo display data.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>Expo display with pending/ready orders and messages.</returns>
    Task<ExpoDisplayDto> GetExpoDisplayAsync(int storeId);

    /// <summary>
    /// Gets pending orders (not yet ready).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of pending expo orders.</returns>
    Task<List<ExpoOrderViewDto>> GetPendingOrdersAsync(int storeId);

    /// <summary>
    /// Gets ready orders (all items complete).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of ready expo orders.</returns>
    Task<List<ExpoOrderViewDto>> GetReadyOrdersAsync(int storeId);

    /// <summary>
    /// Gets complete orders awaiting service.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of complete expo orders.</returns>
    Task<List<ExpoOrderViewDto>> GetCompleteOrdersAsync(int storeId);

    #endregion

    #region Order Actions

    /// <summary>
    /// Marks an order as served from expo.
    /// </summary>
    /// <param name="dto">Mark served request.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> MarkOrderServedAsync(MarkOrderServedDto dto);

    /// <summary>
    /// Marks multiple orders as served.
    /// </summary>
    /// <param name="kdsOrderIds">List of KDS order IDs.</param>
    /// <param name="userId">The user marking orders served.</param>
    /// <returns>Number of orders marked served.</returns>
    Task<int> BulkMarkServedAsync(List<int> kdsOrderIds, int? userId = null);

    /// <summary>
    /// Sends an order back to a station for rework.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="stationId">The station to send back to.</param>
    /// <param name="reason">Reason for rework.</param>
    /// <param name="userId">The user initiating rework.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> SendBackToStationAsync(int kdsOrderId, int stationId, string? reason = null, int? userId = null);

    #endregion

    #region All-Call Messaging

    /// <summary>
    /// Sends an all-call message to stations.
    /// </summary>
    /// <param name="dto">Send all-call request.</param>
    /// <returns>The created message.</returns>
    Task<AllCallMessageDto> SendAllCallAsync(SendAllCallDto dto);

    /// <summary>
    /// Dismisses an all-call message at a station.
    /// </summary>
    /// <param name="dto">Dismiss all-call request.</param>
    /// <returns>The dismissal record.</returns>
    Task<AllCallDismissalDto> DismissAllCallAsync(DismissAllCallDto dto);

    /// <summary>
    /// Gets active all-call messages for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of active messages.</returns>
    Task<List<AllCallMessageDto>> GetActiveMessagesAsync(int stationId);

    /// <summary>
    /// Gets active all-call messages for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of active messages.</returns>
    Task<List<AllCallMessageDto>> GetStoreMessagesAsync(int storeId);

    /// <summary>
    /// Gets all-call message history.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of messages.</returns>
    Task<List<AllCallMessageDto>> GetMessageHistoryAsync(int storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Expires old all-call messages.
    /// </summary>
    /// <returns>Number of messages expired.</returns>
    Task<int> ExpireOldMessagesAsync();

    /// <summary>
    /// Gets dismissal status for a message across all stations.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>List of dismissals.</returns>
    Task<List<AllCallDismissalDto>> GetMessageDismissalsAsync(int messageId);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets expo summary statistics.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>Expo summary.</returns>
    Task<ExpoSummaryDto> GetExpoSummaryAsync(int storeId);

    /// <summary>
    /// Gets performance summary for a date range.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Performance summary.</returns>
    Task<KdsPerformanceSummaryDto> GetPerformanceSummaryAsync(int storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets average service time for orders.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Average service time.</returns>
    Task<TimeSpan> GetAverageServiceTimeAsync(int storeId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when all stations complete an order.
    /// </summary>
    event EventHandler<ExpoOrderViewDto>? OrderComplete;

    /// <summary>
    /// Event raised when an order is served.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderServed;

    /// <summary>
    /// Event raised when an all-call message is sent.
    /// </summary>
    event EventHandler<AllCallMessageDto>? AllCallSent;

    /// <summary>
    /// Event raised when an order is sent back for rework.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderSentBack;

    #endregion
}
