using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for KDS order status management.
/// </summary>
public interface IKdsStatusService
{
    #region Status Transitions

    /// <summary>
    /// Starts preparation of an order at a station.
    /// </summary>
    /// <param name="dto">Start preparation request.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> StartPreparationAsync(StartPreparationDto dto);

    /// <summary>
    /// Marks a specific item as done.
    /// </summary>
    /// <param name="dto">Mark item done request.</param>
    /// <returns>The updated item.</returns>
    Task<KdsOrderItemDto> MarkItemDoneAsync(MarkItemDoneDto dto);

    /// <summary>
    /// Marks all items at a station as done and bumps the order.
    /// </summary>
    /// <param name="dto">Bump order request.</param>
    /// <returns>Bump result with status information.</returns>
    Task<BumpOrderResultDto> BumpOrderAsync(BumpOrderDto dto);

    /// <summary>
    /// Recalls a bumped order back to active display.
    /// </summary>
    /// <param name="dto">Recall order request.</param>
    /// <returns>Recall result.</returns>
    Task<RecallOrderResultDto> RecallOrderAsync(RecallOrderDto dto);

    /// <summary>
    /// Marks an order as served (final state).
    /// </summary>
    /// <param name="dto">Mark served request.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> MarkOrderServedAsync(MarkOrderServedDto dto);

    #endregion

    #region Item Status

    /// <summary>
    /// Starts preparation of a specific item.
    /// </summary>
    /// <param name="kdsOrderItemId">The KDS order item ID.</param>
    /// <param name="userId">The user starting preparation.</param>
    /// <returns>The updated item.</returns>
    Task<KdsOrderItemDto> StartItemPreparationAsync(int kdsOrderItemId, int? userId = null);

    /// <summary>
    /// Reverts an item status to pending.
    /// </summary>
    /// <param name="kdsOrderItemId">The KDS order item ID.</param>
    /// <param name="userId">The user reverting the status.</param>
    /// <returns>The updated item.</returns>
    Task<KdsOrderItemDto> RevertItemToPendingAsync(int kdsOrderItemId, int? userId = null);

    /// <summary>
    /// Gets items by status for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="status">The item status.</param>
    /// <returns>List of items with the specified status.</returns>
    Task<List<KdsOrderItemDto>> GetItemsByStatusAsync(int stationId, KdsItemStatusDto status);

    #endregion

    #region Ready Orders

    /// <summary>
    /// Gets ready orders for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of ready orders.</returns>
    Task<List<KdsOrderDto>> GetReadyOrdersAsync(int stationId);

    /// <summary>
    /// Gets orders that can be recalled within the recall window.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="recallWindowMinutes">Minutes within which recall is allowed.</param>
    /// <returns>List of recallable orders.</returns>
    Task<List<KdsOrderDto>> GetRecallableOrdersAsync(int stationId, int recallWindowMinutes = 10);

    /// <summary>
    /// Checks if an order can be recalled.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="stationId">The station ID.</param>
    /// <returns>True if the order can be recalled.</returns>
    Task<bool> CanRecallOrderAsync(int kdsOrderId, int stationId);

    #endregion

    #region Status Logs

    /// <summary>
    /// Gets status logs for an order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>List of status log entries.</returns>
    Task<List<KdsOrderStatusLogDto>> GetOrderStatusLogsAsync(int kdsOrderId);

    /// <summary>
    /// Gets status logs for a station within a date range.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of status log entries.</returns>
    Task<List<KdsOrderStatusLogDto>> GetStationStatusLogsAsync(int stationId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bumps all ready orders at a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="userId">The user performing the bump.</param>
    /// <returns>Number of orders bumped.</returns>
    Task<int> BumpAllReadyOrdersAsync(int stationId, int? userId = null);

    /// <summary>
    /// Marks all items at a station as done for an order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="stationId">The station ID.</param>
    /// <param name="userId">The user completing the items.</param>
    /// <returns>Number of items marked done.</returns>
    Task<int> MarkAllStationItemsDoneAsync(int kdsOrderId, int stationId, int? userId = null);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when an order status changes.
    /// </summary>
    event EventHandler<KdsOrderStatusChangeEventArgs>? OrderStatusChanged;

    /// <summary>
    /// Event raised when an order is bumped.
    /// </summary>
    event EventHandler<BumpOrderEventArgs>? OrderBumped;

    /// <summary>
    /// Event raised when an order is recalled.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderRecalled;

    /// <summary>
    /// Event raised when an order is ready (all items done).
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderReady;

    /// <summary>
    /// Event raised when an order is served.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderServed;

    #endregion
}

/// <summary>
/// Event arguments for order status change.
/// </summary>
public class KdsOrderStatusChangeEventArgs : EventArgs
{
    public int KdsOrderId { get; set; }
    public int? StationId { get; set; }
    public KdsOrderStatusDto PreviousStatus { get; set; }
    public KdsOrderStatusDto NewStatus { get; set; }
    public int? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event arguments for order bump.
/// </summary>
public class BumpOrderEventArgs : EventArgs
{
    public int KdsOrderId { get; set; }
    public int StationId { get; set; }
    public bool AllItemsDone { get; set; }
    public bool OrderComplete { get; set; }
    public bool PlayAudio { get; set; }
}
