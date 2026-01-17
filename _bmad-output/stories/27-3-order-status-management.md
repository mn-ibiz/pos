# Story 27.3: Order Status Management

## Story
**As a** kitchen staff member,
**I want to** update order status as I prepare it,
**So that** the front of house knows order progress.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 27: Kitchen Display System (KDS)**

## Acceptance Criteria

### AC1: Start Preparation
**Given** order on KDS
**When** starting preparation
**Then** can mark as "In Progress" (color changes)

### AC2: Bump Order Ready
**Given** order is ready
**When** bumping order
**Then** order moves to "Ready" queue with audio alert

### AC3: Order Recall
**Given** bumped order needs recall
**When** pressing recall
**Then** order returns to active display

## Technical Notes
```csharp
public interface IKdsStatusService
{
    Task StartPreparationAsync(Guid kdsOrderId, Guid stationId);
    Task MarkItemDoneAsync(Guid kdsOrderItemId);
    Task BumpOrderAsync(Guid kdsOrderId, Guid stationId);
    Task RecallOrderAsync(Guid kdsOrderId, Guid stationId);
    Task<List<KdsOrder>> GetReadyOrdersAsync(Guid stationId);
    Task<List<KdsOrder>> GetRecallableOrdersAsync(Guid stationId, TimeSpan window);
}

public class KdsOrderStatusLog
{
    public Guid Id { get; set; }
    public Guid KdsOrderId { get; set; }
    public Guid StationId { get; set; }
    public KdsOrderStatus PreviousStatus { get; set; }
    public KdsOrderStatus NewStatus { get; set; }
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Definition of Done
- [x] Status transition logic (New → InProgress → Ready)
- [x] Touch-screen bump interface
- [x] Color coding by status (New=White, InProgress=Yellow, Ready=Green)
- [x] Audio alerts on bump
- [x] Recall functionality with time window
- [x] Status synchronization across stations
- [x] Status change logging/audit
- [x] Unit tests passing

## Implementation Summary

### Entities Created (KdsEntities.cs)
- **KdsOrderStatusLog**: Audit log with KdsOrderId, StationId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Notes

### DTOs Created (KdsDtos.cs)
- KdsOrderStatusLogDto for status history
- KdsOrderStatusChangeEventArgs for events
- BumpOrderEventArgs with audio alert flag
- GetStatusLogsDto for filtering

### Service Implementation (KdsStatusService.cs ~450 lines)
- **StartOrderAsync**: Transitions New → InProgress, sets StartedAt, updates all items to Preparing
- **MarkItemDoneAsync**: Marks individual items done, checks if order complete
- **BumpOrderAsync**: Bumps order to Ready, marks all station items Done, triggers audio alert
- **RecallOrderAsync**: Returns bumped order to InProgress, increments RecallCount, resets items to Preparing
- **MarkOrderServedAsync**: Transitions Ready/Bumped → Served
- **GetReadyOrdersAsync/GetRecallableOrdersAsync**: Retrieves orders by status
- **GetStatusLogAsync**: Full audit trail
- **BulkBumpOrdersAsync**: Bumps multiple orders at once
- Status change logging on every transition
- Event-driven notifications (OrderStatusChanged, OrderBumped, OrderRecalled, OrderReady, OrderServed)

### Unit Tests (KdsStatusServiceTests.cs ~25 tests)
- Constructor null argument validation
- StartOrderAsync tests (valid/invalid order, already started)
- MarkItemDoneAsync tests (valid/invalid item)
- BumpOrderAsync tests (valid/invalid order)
- RecallOrderAsync tests (bumped/non-bumped order)
- MarkOrderServedAsync tests
- GetReadyOrdersAsync tests
- GetStatusLogAsync tests
- BulkBumpOrdersAsync tests
