# Story 27.5: Expo Station and All-Call

## Story
**As an** expo/food runner,
**I want to** see all orders across all stations,
**So that** I can coordinate plating and delivery.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 27: Kitchen Display System (KDS)**

## Acceptance Criteria

### AC1: Expo Station View
**Given** expo station configured
**When** viewing display
**Then** shows orders from ALL stations with status

### AC2: Order Completeness
**Given** order has items from multiple stations
**When** all items ready
**Then** order shows "Complete" for plating

### AC3: All-Call Communication
**Given** communication needed
**When** using all-call
**Then** can send message to all stations

## Technical Notes
```csharp
public class ExpoOrderView
{
    public Guid KdsOrderId { get; set; }
    public string OrderNumber { get; set; }
    public string TableNumber { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public TimeSpan ElapsedTime => DateTime.UtcNow - ReceivedAt;
    public bool IsComplete { get; set; }
    public List<ExpoStationStatus> StationStatuses { get; set; }
}

public class AllCallMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; }
    public Guid SentByUserId { get; set; }
    public DateTime SentAt { get; set; }
    public List<Guid> TargetStationIds { get; set; }  // Empty = all stations
    public AllCallPriority Priority { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum AllCallPriority
{
    Normal,
    Urgent
}
```

## Definition of Done
- [x] Expo station type with all-station visibility
- [x] Consolidated order view across stations
- [x] Station status indicators per order
- [x] Order completeness detection
- [x] All-call messaging system
- [x] Message broadcasting via SignalR
- [x] Message priority and expiration
- [x] Expo bump for served orders
- [x] Unit tests passing

## Implementation Summary

### Entities Created (KdsEntities.cs)
- **AllCallMessage**: StoreId, Message, SentByUserId, SentAt, ExpiresAt, Priority, navigation to Targets
- **AllCallMessageTarget**: Links message to specific stations
- **AllCallMessageDismissal**: Tracks which stations dismissed which messages
- **AllCallPriority enum**: Normal, High, Urgent

### DTOs Created (KdsDtos.cs)
- ExpoOrderViewDto with OrderId, OrderNumber, TableNumber, StationStatuses, IsReadyToServe, TotalItems, CompletedItems
- ExpoStationStatusDto with StationId, StationName, TotalItems, CompletedItems, Status, IsComplete
- AllCallMessageDto, SendAllCallDto, DismissAllCallDto
- ExpoStatisticsDto with TotalActiveOrders, ReadyForPickup, AverageWaitTimeSeconds, ServedToday

### Service Implementation (ExpoService.cs ~600 lines)
- **GetExpoOrderViewAsync**: Single order with all station statuses
- **GetAllExpoOrdersAsync**: All active orders with consolidated station status
- **MarkOrderCompleteAsync**: Marks order ready for pickup
- **MarkOrderServedAsync**: Transitions to Served status
- **SendAllCallAsync**: Broadcasts message to all/specific stations, creates targets
- **GetActiveAllCallsAsync**: Returns non-expired, non-dismissed messages for station
- **DismissAllCallAsync**: Records dismissal for station
- **SendOrderBackAsync**: Returns order to kitchen with reason, increments SentBackCount
- **GetExpoStatisticsAsync**: Real-time statistics (active orders, ready count, served today, avg wait time)
- Event-driven notifications (OrderComplete, OrderServed, AllCallSent, OrderSentBack)

### Unit Tests (ExpoServiceTests.cs ~25 tests)
- Constructor null argument validation
- GetExpoOrderViewAsync tests (valid order, all items done, invalid order)
- GetAllExpoOrdersAsync tests
- MarkOrderCompleteAsync tests
- MarkOrderServedAsync tests
- SendAllCallAsync tests (with targets, broadcast to all)
- GetActiveAllCallsAsync tests
- DismissAllCallAsync tests (new dismissal, already dismissed)
- SendOrderBackAsync tests
- GetExpoStatisticsAsync tests
