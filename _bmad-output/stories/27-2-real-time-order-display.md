# Story 27.2: Real-Time Order Display

## Story
**As a** kitchen staff member,
**I want to** see orders appear in real-time on the KDS,
**So that** I can prepare orders as they come in.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 27: Kitchen Display System (KDS)**

## Acceptance Criteria

### AC1: Instant Order Appearance
**Given** order is submitted
**When** containing items for this station
**Then** order appears on KDS within 2 seconds

### AC2: Order Information Display
**Given** order is displayed
**When** viewing
**Then** shows: order #, table #, items with modifiers, time elapsed

### AC3: Queue Ordering
**Given** multiple orders exist
**When** viewing queue
**Then** orders sorted by submission time (oldest first)

## Technical Notes
```csharp
public class KdsOrder
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    public string OrderNumber { get; set; }
    public string TableNumber { get; set; }
    public DateTime ReceivedAt { get; set; }
    public KdsOrderStatus Status { get; set; } = KdsOrderStatus.New;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsPriority { get; set; }
    public List<KdsOrderItem> Items { get; set; }
}

public class KdsOrderItem
{
    public Guid Id { get; set; }
    public Guid KdsOrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public List<string> Modifiers { get; set; }
    public string SpecialInstructions { get; set; }
    public Guid StationId { get; set; }
    public KdsItemStatus Status { get; set; } = KdsItemStatus.Pending;
}

public enum KdsOrderStatus
{
    New,
    InProgress,
    Ready,
    Served,
    Recalled
}

public enum KdsItemStatus
{
    Pending,
    Preparing,
    Done
}

// SignalR Hub for real-time updates
public class KdsHub : Hub
{
    public async Task JoinStation(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task SendOrderToStation(string stationId, KdsOrderDto order)
    {
        await Clients.Group($"station_{stationId}").SendAsync("ReceiveOrder", order);
    }

    public async Task UpdateOrderStatus(string stationId, Guid orderId, KdsOrderStatus status)
    {
        await Clients.Group($"station_{stationId}").SendAsync("OrderStatusChanged", orderId, status);
    }
}

public interface IKdsOrderService
{
    Task RouteOrderToStationsAsync(Order order);
    Task<List<KdsOrder>> GetStationOrdersAsync(Guid stationId);
    Task<KdsOrder> GetOrderAsync(Guid kdsOrderId);
}
```

## Definition of Done
- [x] KdsOrder and KdsOrderItem entities
- [x] SignalR hub for real-time communication
- [x] Order routing logic based on categories
- [x] Order display with modifiers and instructions
- [x] Queue ordering by time (FIFO)
- [x] Timer display showing elapsed time
- [x] Sub-2-second order appearance latency
- [x] Unit tests passing

## Implementation Summary

### Entities Created (KdsEntities.cs)
- **KdsOrder**: Links to source Order with OrderNumber, TableNumber, Status, timestamps (ReceivedAt, StartedAt, BumpedAt, ServedAt), Priority, IsPriority, RecallCount, SentBackReason
- **KdsOrderItem**: Links to source OrderItem with ProductName, Quantity, Modifiers (JSON), SpecialInstructions, StationId, Status, timestamps
- **KdsOrderStatus enum**: New, InProgress, Ready, Bumped, Recalled, Served, Voided
- **KdsItemStatus enum**: Pending, Preparing, Done, Voided

### DTOs Created (KdsDtos.cs)
- KdsOrderDto, KdsOrderItemDto for display
- RouteOrderToKdsDto, RouteOrderResultDto for routing
- KdsOrderDisplayStateDto for real-time updates
- GetOrdersByDateRangeDto for filtering

### Service Implementation (KdsOrderService.cs ~550 lines)
- **RouteOrderToStationsAsync**: Routes orders to appropriate stations based on product categories
- **GetOrderAsync/GetActiveOrdersAsync**: Retrieves orders with items and station info
- **UpdateOrderPriorityAsync**: Sets order priority (Normal/Rush/VIP)
- **VoidOrderAsync**: Voids order and all items
- **GetOrderCountByStatusAsync**: Statistics by status
- **GetOrderDisplayStateAsync**: Real-time display state with elapsed time
- Event-driven notifications (OrderRouted, OrderUpdated, OrderVoided)

### Unit Tests (KdsOrderServiceTests.cs ~15 tests)
- Constructor null argument validation
- RouteOrderToStationsAsync tests (valid order, invalid order, already routed)
- GetOrderAsync tests (valid/invalid ID)
- GetActiveOrdersAsync tests
- UpdateOrderPriorityAsync tests
- VoidOrderAsync tests
- GetOrderCountByStatusAsync tests
