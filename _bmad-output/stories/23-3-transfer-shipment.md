# Story 23.3: Transfer Shipment

## Story
**As a** warehouse clerk,
**I want to** process the physical transfer,
**So that** goods are properly picked and shipped.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 23: Stock Transfers**

## Acceptance Criteria

### AC1: Pick List Generation
**Given** transfer is approved
**When** processing shipment
**Then** generates pick list for warehouse

### AC2: Shipment Confirmation
**Given** items are picked
**When** confirming shipment
**Then** stock is deducted from source location

### AC3: Dispatch Documentation
**Given** shipment is ready
**When** dispatching
**Then** generates transfer document and updates status to "In Transit"

## Technical Notes
```csharp
public class TransferShipment
{
    public Guid Id { get; set; }
    public string ShipmentNumber { get; set; }
    public Guid TransferRequestId { get; set; }
    public Guid SourceLocationId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime? PickedAt { get; set; }
    public Guid? PickedBy { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public Guid? DispatchedBy { get; set; }
    public string DriverName { get; set; }
    public string VehicleNumber { get; set; }
    public List<TransferShipmentLine> Lines { get; set; }
}

public class TransferShipmentLine
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid ProductId { get; set; }
    public int ApprovedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public enum ShipmentStatus
{
    PendingPick,
    Picking,
    Picked,
    Dispatched,
    InTransit,
    Delivered
}
```

## Definition of Done
- [x] Pick list generation and printing
- [x] Pick confirmation workflow
- [x] Stock deduction on shipment
- [x] Transfer document generation (printable)
- [x] Status update to "In Transit"
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (StockTransferDtos.cs)

#### Pick List DTOs
- `PickListStatus` enum - Pending, InProgress, Completed, Cancelled
- `PickListDto` - Full pick list with request info, status, totals, and lines
- `PickListLineDto` - Individual pick line with product info, approved/picked quantities, barcode
- `ConfirmPickDto` - Single line pick confirmation
- `ConfirmAllPicksDto` - Batch pick confirmation for entire request

#### Transfer Document DTOs
- `TransferDocumentDto` - Complete delivery note with source/destination info, shipment details, line items, totals
- `TransferDocumentLineDto` - Line item with quantities, costs, line numbers

### Interface Methods Added (IStockTransferService.cs)

#### Pick List Operations
- `GetPickListAsync(int requestId)` - Generate pick list for approved request
- `ConfirmPicksAsync(ConfirmAllPicksDto dto, int userId)` - Confirm picked quantities
- `GetPendingPickListsAsync(int? sourceLocationId)` - Get all pending pick lists

#### Transfer Document Operations
- `GenerateTransferDocumentAsync(int shipmentId)` - Generate transfer document for shipment
- `GetTransferDocumentForRequestAsync(int requestId)` - Get document by request ID

### Service Implementation (StockTransferService.cs)

#### Pick List Operations
- **GetPickListAsync**: Validates approved status, builds pick list with product info, calculates totals and status
- **ConfirmPicksAsync**: Validates quantities don't exceed approved, updates ShippedQuantity on lines
- **GetPendingPickListsAsync**: Filters approved requests without dispatched shipments, orders by priority

#### Transfer Document Operations
- **GenerateTransferDocumentAsync**: Builds complete document with source/destination info, shipment details, line items with line numbers
- **GetTransferDocumentForRequestAsync**: Wrapper to get document by request ID

#### Stock Deduction (DispatchShipmentAsync Updated)
- Deducts stock from source inventory for each shipped product
- Updates both CurrentStock and ReservedStock
- Validates sufficient stock before deduction
- Logs stock deduction activity

#### Helper Method Added
- `DeductInventoryFromSourceAsync(int sourceLocationId, int productId, int quantity)` - Handles inventory deduction with validation

### Unit Tests Added (StockTransferServiceTests.cs)

#### Shipment Tests (3 tests)
- `DispatchShipmentAsync_DispatchesShipmentAndDeductsStock` - Verifies stock deduction
- `DispatchShipmentAsync_ThrowsForAlreadyDispatchedShipment` - Error handling

#### Pick List Tests (5 tests)
- `GetPickListAsync_GeneratesPickListForApprovedRequest` - Full pick list generation
- `GetPickListAsync_ThrowsForNonApprovedRequest` - Validation
- `ConfirmPicksAsync_UpdatesPickedQuantities` - Pick confirmation
- `ConfirmPicksAsync_ThrowsWhenPickedExceedsApproved` - Quantity validation
- `GetPendingPickListsAsync_ReturnsApprovedRequestsWithoutDispatchedShipments` - Filtering

#### Transfer Document Tests (4 tests)
- `GenerateTransferDocumentAsync_GeneratesDocument` - Full document generation
- `GenerateTransferDocumentAsync_ThrowsForNonExistentShipment` - Error handling
- `GetTransferDocumentForRequestAsync_ReturnsDocumentIfShipmentExists` - Retrieval
- `GetTransferDocumentForRequestAsync_ReturnsNullIfNoShipment` - Null handling

### Key Features
1. **Pick List Generation**: Shows approved items, quantities, product details, barcodes for scanning
2. **Pick Status Tracking**: Pending → InProgress → Completed based on picked quantities
3. **Pick Confirmation**: Updates shipped quantities with validation against approved amounts
4. **Stock Deduction**: Automatically deducts from source on dispatch
5. **Transfer Document**: Complete delivery note with source/destination, line items, totals, driver info
