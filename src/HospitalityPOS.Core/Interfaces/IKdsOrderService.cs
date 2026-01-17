using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for KDS order management and routing.
/// </summary>
public interface IKdsOrderService
{
    #region Order Routing

    /// <summary>
    /// Routes an order to appropriate KDS stations based on item categories.
    /// </summary>
    /// <param name="dto">Route order request.</param>
    /// <returns>Routing result with item assignments.</returns>
    Task<RouteOrderResultDto> RouteOrderToStationsAsync(RouteOrderToKdsDto dto);

    /// <summary>
    /// Re-routes an existing KDS order (e.g., after modifications).
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>Updated routing result.</returns>
    Task<RouteOrderResultDto> ReRouteOrderAsync(int kdsOrderId);

    /// <summary>
    /// Routes a single item to a station.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The KDS order item routing info.</returns>
    Task<KdsOrderItemRoutingDto> RouteItemToStationAsync(int orderItemId, int storeId);

    #endregion

    #region Order Retrieval

    /// <summary>
    /// Gets a KDS order by ID.
    /// </summary>
    /// <param name="id">The KDS order ID.</param>
    /// <returns>The KDS order or null.</returns>
    Task<KdsOrderDto?> GetOrderAsync(int id);

    /// <summary>
    /// Gets a KDS order by the original order ID.
    /// </summary>
    /// <param name="orderId">The original order ID.</param>
    /// <returns>The KDS order or null.</returns>
    Task<KdsOrderDto?> GetOrderByOrderIdAsync(int orderId);

    /// <summary>
    /// Gets orders for a specific station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="includeCompleted">Whether to include completed orders.</param>
    /// <returns>List of KDS orders for the station.</returns>
    Task<List<KdsOrderDto>> GetStationOrdersAsync(int stationId, bool includeCompleted = false);

    /// <summary>
    /// Gets orders based on query parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of KDS orders.</returns>
    Task<List<KdsOrderListDto>> GetOrdersAsync(KdsOrderQueryDto query);

    /// <summary>
    /// Gets active orders (New or InProgress) for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of active orders.</returns>
    Task<List<KdsOrderDto>> GetActiveOrdersAsync(int stationId);

    /// <summary>
    /// Gets ready orders for a station (for recall purposes).
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="recallWindowMinutes">Number of minutes to include.</param>
    /// <returns>List of ready orders.</returns>
    Task<List<KdsOrderDto>> GetReadyOrdersAsync(int stationId, int recallWindowMinutes = 10);

    /// <summary>
    /// Gets orders sorted by priority and time (for display).
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>Sorted list of orders.</returns>
    Task<List<KdsOrderDto>> GetOrderQueueAsync(int stationId);

    #endregion

    #region Order Items

    /// <summary>
    /// Gets items for a specific station from an order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of items for the station.</returns>
    Task<List<KdsOrderItemDto>> GetStationItemsAsync(int kdsOrderId, int stationId);

    /// <summary>
    /// Gets all items for a KDS order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <returns>List of all items.</returns>
    Task<List<KdsOrderItemDto>> GetOrderItemsAsync(int kdsOrderId);

    #endregion

    #region Display State

    /// <summary>
    /// Gets the current display state for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>Station display state.</returns>
    Task<KdsStationDisplayDto> GetStationDisplayAsync(int stationId);

    /// <summary>
    /// Gets order view models for display.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of order view models.</returns>
    Task<List<KdsOrderViewModel>> GetOrderViewModelsAsync(int stationId);

    #endregion

    #region Order Management

    /// <summary>
    /// Updates an order's priority.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="priority">The new priority.</param>
    /// <returns>The updated order.</returns>
    Task<KdsOrderDto> UpdateOrderPriorityAsync(int kdsOrderId, OrderPriorityDto priority);

    /// <summary>
    /// Voids a KDS order.
    /// </summary>
    /// <param name="kdsOrderId">The KDS order ID.</param>
    /// <param name="userId">The user performing the void.</param>
    /// <returns>The voided order.</returns>
    Task<KdsOrderDto> VoidOrderAsync(int kdsOrderId, int? userId = null);

    /// <summary>
    /// Voids a specific item on a KDS order.
    /// </summary>
    /// <param name="kdsOrderItemId">The KDS order item ID.</param>
    /// <param name="userId">The user performing the void.</param>
    /// <returns>The voided item.</returns>
    Task<KdsOrderItemDto> VoidOrderItemAsync(int kdsOrderItemId, int? userId = null);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets order count by status for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>Dictionary of status to count.</returns>
    Task<Dictionary<KdsOrderStatusDto, int>> GetOrderCountByStatusAsync(int stationId);

    /// <summary>
    /// Gets average wait time for orders at a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Average wait time.</returns>
    Task<TimeSpan> GetAverageWaitTimeAsync(int stationId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a new order is routed.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderRouted;

    /// <summary>
    /// Event raised when an order is updated.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderUpdated;

    /// <summary>
    /// Event raised when an order is voided.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderVoided;

    #endregion
}
