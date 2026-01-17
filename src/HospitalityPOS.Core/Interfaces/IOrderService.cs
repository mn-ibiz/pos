using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="order">The order to create.</param>
    /// <returns>The created order with generated ID and order number.</returns>
    Task<Order> CreateOrderAsync(Order order);

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The order if found, null otherwise.</returns>
    Task<Order?> GetByIdAsync(int id);

    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <returns>The order if found, null otherwise.</returns>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Gets all open orders for the current work period.
    /// </summary>
    /// <returns>List of open orders.</returns>
    Task<IEnumerable<Order>> GetOpenOrdersAsync();

    /// <summary>
    /// Gets all orders for a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <returns>List of orders.</returns>
    Task<IEnumerable<Order>> GetOrdersByWorkPeriodAsync(int workPeriodId);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="order">The order to update.</param>
    /// <returns>The updated order.</returns>
    Task<Order> UpdateOrderAsync(Order order);

    /// <summary>
    /// Generates the next order number for the current date.
    /// </summary>
    /// <returns>The generated order number (format: O-yyyyMMdd-sequence).</returns>
    Task<string> GenerateOrderNumberAsync();

    /// <summary>
    /// Marks order items as printed to kitchen.
    /// </summary>
    /// <param name="orderItemIds">The IDs of items to mark as printed.</param>
    /// <returns>A task representing the operation.</returns>
    Task MarkItemsAsPrintedAsync(IEnumerable<int> orderItemIds);

    /// <summary>
    /// Gets all held orders for the current work period.
    /// </summary>
    /// <param name="userId">Optional user ID to filter by.</param>
    /// <returns>List of held orders.</returns>
    Task<IEnumerable<Order>> GetHeldOrdersAsync(int? userId = null);

    /// <summary>
    /// Gets unprinted items for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>List of order items that haven't been printed to kitchen.</returns>
    Task<IEnumerable<OrderItem>> GetUnprintedItemsAsync(int orderId);

    /// <summary>
    /// Adds items to an existing order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="items">The items to add.</param>
    /// <returns>The updated order.</returns>
    Task<Order> AddItemsToOrderAsync(int orderId, IEnumerable<OrderItem> items);

    /// <summary>
    /// Holds an order for later.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The updated order.</returns>
    Task<Order> HoldOrderAsync(int orderId);

    /// <summary>
    /// Recalls a held order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The recalled order.</returns>
    Task<Order> RecallOrderAsync(int orderId);
}
