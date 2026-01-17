# Story 24.4: Batch Traceability

## Story
**As a** manager,
**I want to** trace a batch from receipt to sale,
**So that** I can handle recalls.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 24: Batch & Expiry Tracking**

## Acceptance Criteria

### AC1: Batch Search
**Given** a batch recall is needed
**When** searching by batch number
**Then** shows: supplier, receipt date, quantity received, quantity sold

### AC2: Transaction Drill-Down
**Given** sold items are identified
**When** drilling down
**Then** can view transactions containing the batch

### AC3: Remaining Stock View
**Given** remaining stock exists
**When** viewing
**Then** shows current quantity and location

## Technical Notes
```csharp
public class BatchTraceabilityReport
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductSKU { get; set; }

    // Source Information
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string GRNNumber { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }

    // Current Status
    public int CurrentQuantity { get; set; }
    public BatchStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // Movement Summary
    public int QuantitySold { get; set; }
    public int QuantityAdjusted { get; set; }
    public int QuantityDisposed { get; set; }
    public int QuantityTransferred { get; set; }

    // Detailed Movements
    public List<BatchMovementDetail> Movements { get; set; }
}

public class BatchMovementDetail
{
    public DateTime MovementDate { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; }
    public string Details { get; set; }  // Receipt #, Adjustment reason, etc.
}

public class BatchSaleTransaction
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public int QuantitySold { get; set; }
    public decimal SalePrice { get; set; }
    public Guid CashierId { get; set; }
    public string CashierName { get; set; }
}

public class BatchRecallAlert
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; }
    public Guid ProductId { get; set; }
    public string RecallReason { get; set; }
    public RecallSeverity Severity { get; set; }
    public DateTime IssuedAt { get; set; }
    public int AffectedQuantity { get; set; }
    public int QuantityRecovered { get; set; }
}
```

## Definition of Done
- [x] Batch search functionality
- [x] Full traceability report (receipt to sale)
- [x] Transaction list for batch
- [x] Current stock visibility
- [x] Recall alert creation
- [x] Unit tests passing

## Implementation Summary

### Entities Added (BatchTrackingEntities.cs)

#### Enums
- `RecallSeverity` - Low, Medium, High, Critical
- `RecallStatus` - Active, Recovered, PartiallyResolved, Closed, Cancelled

#### BatchRecallAlert
- BatchId, ProductId, BatchNumber references
- RecallReason, Severity, Status
- IssuedAt, IssuedByUserId
- AffectedQuantity, QuantityRecovered, QuantitySold, QuantityInStock
- ExternalReference, SupplierContactInfo
- ResolutionNotes, ResolvedAt, ResolvedByUserId

#### RecallAction
- RecallAlertId, ActionType (Quarantine, Dispose, Return, Notify)
- StoreId, Quantity, Description
- ActionDate, PerformedByUserId

### DTOs Added (BatchTrackingDtos.cs)

#### Traceability Report
- `BatchTraceabilityReportDto` - Full report with source, status, movements, sales
- `BatchMovementDetailDto` - Individual movement with reference details
- `BatchSaleTransactionDto` - Sale transaction linked to batch
- `BatchLocationDto` - Stock location with quantities

#### Batch Search
- `BatchSearchQueryDto` - Search parameters (batch number, product, store, dates)
- `BatchSearchResultDto` - Search result with recall flag

#### Recall Management
- `BatchRecallAlertDto` - Full recall details with actions
- `CreateBatchRecallAlertDto` - Create new recall
- `UpdateRecallStatusDto` - Update recall status
- `RecallActionDto` - Action taken during recall
- `CreateRecallActionDto` - Create new action
- `RecallQueryDto` - Query filters for recalls
- `RecallSummaryDto` - Summary statistics

### Interface (IBatchTraceabilityService.cs)

#### Batch Search
- `SearchBatchesAsync` - Search with full query support
- `SearchByBatchNumberAsync` - Quick search by batch number
- `GetProductBatchesAsync` - Get all batches for a product

#### Traceability Report
- `GetTraceabilityReportAsync` - Full traceability report
- `GetBatchMovementsAsync` - Movement history
- `GetBatchSaleTransactionsAsync` - Sale transactions
- `GetBatchLocationsAsync` - Current stock locations

#### Recall Management
- `CreateRecallAlertAsync` - Create recall alert
- `GetRecallAlertAsync` - Get recall by ID
- `GetRecallAlertsAsync` - Query recalls
- `UpdateRecallStatusAsync` - Update recall status
- `RecordRecallActionAsync` - Record action taken
- `GetRecallActionsAsync` - Get actions for recall
- `GetRecallSummaryAsync` - Get summary statistics
- `GetActiveRecallsForStoreAsync` - Get store's active recalls
- `HasActiveRecallAsync` - Check if batch has recall
- `QuarantineBatchAsync` - Quarantine batch for recall

### Service Implementation (BatchTraceabilityService.cs)

- Full batch search with multiple filters
- Complete traceability from GRN to sale
- Movement history with user tracking
- Sale transaction drill-down
- Recall creation with batch status update
- Recall action recording with quantity tracking
- Recovery rate calculation
- Quarantine workflow

### EF Configuration (BatchTrackingConfiguration.cs)

- `BatchRecallAlertConfiguration` - Indexes on batch, product, status, severity
- `RecallActionConfiguration` - Indexes on recall, action type, date

### Unit Tests (BatchTraceabilityServiceTests.cs)

- Constructor validation (2 tests)
- Batch search tests (2 tests)
- Traceability report tests (4 tests)
- Recall management tests (6 tests)
- Quarantine tests (1 test)
