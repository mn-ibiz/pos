using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing orders.
/// </summary>
public class OrderService : IOrderService
{
    private readonly POSDbContext _context;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    public OrderService(POSDbContext context, IWorkPeriodService workPeriodService, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Order> CreateOrderAsync(Order order)
    {
        // Generate order number if not set
        if (string.IsNullOrEmpty(order.OrderNumber))
        {
            order.OrderNumber = await GenerateOrderNumberAsync().ConfigureAwait(false);
        }

        // Set work period if not set
        if (!order.WorkPeriodId.HasValue)
        {
            var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync().ConfigureAwait(false);
            order.WorkPeriodId = currentPeriod?.Id;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Reload with navigation properties for KOT printing
        var savedOrder = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstAsync(o => o.Id == order.Id)
            .ConfigureAwait(false);

        _logger.Information("Created order {OrderNumber} with {ItemCount} items",
            savedOrder.OrderNumber, savedOrder.OrderItems.Count);

        return savedOrder;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetOpenOrdersAsync()
    {
        var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync().ConfigureAwait(false);
        if (currentPeriod is null)
        {
            return Enumerable.Empty<Order>();
        }

        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Where(o => o.WorkPeriodId == currentPeriod.Id && o.Status == OrderStatus.Open)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetOrdersByWorkPeriodAsync(int workPeriodId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Where(o => o.WorkPeriodId == workPeriodId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order> UpdateOrderAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Debug("Updated order {OrderNumber}", order.OrderNumber);

        return order;
    }

    /// <inheritdoc />
    public async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"O-{today:yyyyMMdd}-";

        // Get the count of orders for today (more reliable than parsing max sequence)
        var orderCountToday = await _context.Orders
            .CountAsync(o => o.OrderNumber.StartsWith(prefix))
            .ConfigureAwait(false);

        // Start sequence from count + 1
        int sequence = orderCountToday + 1;

        // Handle potential gaps by finding the actual max sequence
        var lastOrder = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (lastOrder is not null)
        {
            var lastSequence = lastOrder.OrderNumber[(prefix.Length)..];
            if (int.TryParse(lastSequence, out var parsed) && parsed >= sequence)
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }

    /// <inheritdoc />
    public async Task MarkItemsAsPrintedAsync(IEnumerable<int> orderItemIds)
    {
        var items = await _context.OrderItems
            .Where(oi => orderItemIds.Contains(oi.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            item.PrintedToKitchen = true;
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Debug("Marked {ItemCount} items as printed to kitchen", items.Count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetHeldOrdersAsync(int? userId = null)
    {
        var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync().ConfigureAwait(false);
        if (currentPeriod is null)
        {
            return Enumerable.Empty<Order>();
        }

        var query = _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Where(o => o.WorkPeriodId == currentPeriod.Id && o.Status == OrderStatus.OnHold);

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderItem>> GetUnprintedItemsAsync(int orderId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.OrderId == orderId && !oi.PrintedToKitchen)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Order> AddItemsToOrderAsync(int orderId, IEnumerable<OrderItem> items)
    {
        var order = await GetByIdAsync(orderId);
        if (order is null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
        }

        // Get current max batch number to increment for new items
        var currentMaxBatch = order.OrderItems.Any()
            ? order.OrderItems.Max(oi => oi.BatchNumber)
            : 0;
        var newBatchNumber = currentMaxBatch + 1;

        var itemsList = items.ToList();
        foreach (var item in itemsList)
        {
            item.OrderId = orderId;
            item.PrintedToKitchen = false;
            item.BatchNumber = newBatchNumber;
            _context.OrderItems.Add(item);
        }

        // Recalculate order totals
        var allItems = order.OrderItems.Concat(itemsList).ToList();
        order.Subtotal = allItems.Sum(oi => oi.UnitPrice * oi.Quantity - oi.DiscountAmount);
        order.DiscountAmount = allItems.Sum(oi => oi.DiscountAmount);
        order.TaxAmount = allItems.Sum(oi => oi.TaxAmount);
        order.TotalAmount = order.Subtotal + order.TaxAmount;

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Added {ItemCount} items (batch {BatchNumber}) to order {OrderNumber}",
            itemsList.Count, newBatchNumber, order.OrderNumber);

        // Reload to get updated items with navigation properties
        return (await GetByIdAsync(orderId))!;
    }

    /// <inheritdoc />
    public async Task<Order> HoldOrderAsync(int orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order is null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
        }

        order.Status = OrderStatus.OnHold;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Order {OrderNumber} placed on hold", order.OrderNumber);

        return order;
    }

    /// <inheritdoc />
    public async Task<Order> RecallOrderAsync(int orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order is null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
        }

        if (order.Status != OrderStatus.OnHold)
        {
            throw new InvalidOperationException($"Order {order.OrderNumber} is not on hold.");
        }

        order.Status = OrderStatus.Open;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Order {OrderNumber} recalled from hold", order.OrderNumber);

        return order;
    }
}
