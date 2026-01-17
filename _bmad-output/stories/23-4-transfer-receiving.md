# Story 23.4: Transfer Receiving

## Story
**As a** receiving store manager,
**I want to** receive transferred stock,
**So that** my inventory is updated.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 23: Stock Transfers**

## Acceptance Criteria

### AC1: Shipment Receiving
**Given** transfer shipment arrives
**When** receiving
**Then** can enter actual received quantities

### AC2: Variance Handling
**Given** variance exists (short/over)
**When** recording
**Then** variance is logged and flagged for investigation

### AC3: Stock Update
**Given** receiving is complete
**When** confirming
**Then** stock is added to destination location

## Technical Notes
```csharp
public class TransferReceipt
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid ReceivingLocationId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid ReceivedBy { get; set; }
    public ReceiptStatus Status { get; set; }
    public string Notes { get; set; }
    public List<TransferReceiptLine> Lines { get; set; }
}

public class TransferReceiptLine
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int Variance => ReceivedQuantity - ExpectedQuantity;
    public VarianceReason? VarianceReason { get; set; }
    public string VarianceNotes { get; set; }
}

public enum VarianceReason
{
    ShortShipment,
    Damaged,
    Expired,
    CountError,
    Other
}

public class TransferVarianceInvestigation
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public InvestigationStatus Status { get; set; }
    public string Resolution { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
```

## Definition of Done
- [x] Pending shipments queue displayed
- [x] Receiving workflow implemented
- [x] Variance recording and flagging
- [x] Stock increase on confirmation
- [x] Variance investigation workflow
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (StockTransferDtos.cs)

#### Variance and Receipt Summary DTOs
- `ReceivingSummaryDto` - Complete receiving summary with variance indicators:
  - TotalExpected, TotalReceived, TotalVariance
  - HasVariance, HasShortage, HasSurplus, HasIssues flags
  - Value summaries (TotalExpectedValue, TotalReceivedValue, VarianceValue)
  - Unresolved/Resolved issue counts
  - List of ReceivingLineVarianceDto lines

- `ReceivingLineVarianceDto` - Individual line variance details:
  - Expected, Received, Issue quantities
  - Calculated Variance property
  - VarianceType (None/Shortage/Surplus)
  - UnitCost and VarianceValue calculation
  - Associated issues

- `PendingReceiptsQueryDto` - Query parameters for pending receipts
- `PendingReceiptDto` - Pending shipment details for receiving queue:
  - Source location, expected items/quantity/value
  - Dispatch and expected arrival dates
  - Shipment/tracking info, driver name
  - IsOverdue and DaysInTransit calculations

- `VarianceInvestigationDto` - Variance investigation tracking:
  - Total variance value and quantity
  - Unresolved issue count
  - Investigation status (Pending/InProgress/Resolved/WrittenOff)
  - Investigation and resolution timestamps

- `VarianceInvestigationStatus` enum - Pending, InProgress, Resolved, WrittenOff

### Interface Methods Added (IStockTransferService.cs)

- `GetPendingReceiptsAsync(int? storeId)` - Get pending shipments queue with details
- `GetReceivingSummaryAsync(int receiptId)` - Get variance summary for a receipt
- `GetReceiptsWithVarianceAsync(int? storeId)` - Get receipts requiring investigation

### Service Implementation (StockTransferService.cs)

#### GetPendingReceiptsAsync
- Retrieves in-transit requests for a store
- Includes shipment details (dispatch time, tracking, driver)
- Calculates expected quantities and values
- Orders by expected delivery date

#### GetReceivingSummaryAsync
- Generates comprehensive variance report for a receipt
- Calculates line-by-line and total variances
- Includes all associated issues (resolved and unresolved)
- Calculates variance values using unit costs

#### GetReceiptsWithVarianceAsync
- Identifies receipts with quantity discrepancies
- Filters out receipts with no variance and no unresolved issues
- Determines investigation status based on issue resolution
- Orders by variance value (highest first)

### Unit Tests Added (StockTransferServiceTests.cs)

#### Receipt and Receiving Tests (4 tests)
- `GetPendingReceiptsAsync_ReturnsInTransitRequests` - Pending queue filtering
- `GetReceivingSummaryAsync_ReturnsVarianceDetails` - Variance summary generation
- `GetReceiptsWithVarianceAsync_ReturnsOnlyVarianceReceipts` - Variance filtering
- `CompleteReceiptAsync_UpdatesInventoryAndStatus` - Stock update verification

### Key Features
1. **Pending Shipments Queue**: Shows in-transit transfers with tracking info, expected arrival, overdue status
2. **Variance Detection**: Automatic detection of shortage/surplus/no variance per line and total
3. **Variance Value Calculation**: Monetary impact of variances using unit costs
4. **Issue Integration**: Links to TransferReceiptIssue for detailed tracking
5. **Investigation Workflow**: Status tracking for variance investigation
6. **Stock Update**: Inventory updated at destination on receipt completion
7. **Existing Functionality Leveraged**:
   - CreateReceiptAsync - Creates receipt with received quantities
   - CompleteReceiptAsync - Updates inventory at destination
   - LogIssueAsync/ResolveIssueAsync - Issue management workflow
