# Story 24.1: Batch Recording on Receipt

## Story
**As a** receiving clerk,
**I want to** record batch numbers and expiry dates,
**So that** inventory is traceable.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 24: Batch & Expiry Tracking**

## Acceptance Criteria

### AC1: Batch/Expiry Entry
**Given** goods are being received
**When** entering receipt details
**Then** can enter batch/lot number and expiry date per item

### AC2: Expiry Validation
**Given** expiry date is required
**When** leaving blank
**Then** system warns or blocks based on product configuration

### AC3: Batch Linking
**Given** batch is recorded
**When** saving
**Then** batch details linked to stock movement and stored

## Technical Notes
```csharp
public class ProductBatch
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public int InitialQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? GRNId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public BatchStatus Status { get; set; }
}

public enum BatchStatus
{
    Active,
    LowStock,
    Expired,
    Recalled,
    Disposed
}

public class ProductBatchConfiguration
{
    public Guid ProductId { get; set; }
    public bool RequiresBatchTracking { get; set; }
    public bool RequiresExpiryDate { get; set; }
    public int ExpiryWarningDays { get; set; } = 30;
    public bool BlockExpiredSales { get; set; } = true;
}

public class BatchStockMovement
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid ProductId { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string ReferenceType { get; set; }  // GRN, Sale, Adjustment, Disposal
    public Guid ReferenceId { get; set; }
    public DateTime MovedAt { get; set; }
}
```

## Definition of Done
- [x] Batch entry fields on goods receiving
- [x] Expiry date validation (warn/block)
- [x] Batch linked to stock movements
- [x] Product batch configuration screen
- [x] Unit tests passing

## Implementation Summary

### Entities Created (BatchTrackingEntities.cs)

#### Enums
- `BatchStatus` - Active, LowStock, Expired, Recalled, Disposed
- `BatchMovementType` - Receipt, Sale, TransferOut, TransferIn, Adjustment, Disposal, Return, Reserved, Released
- `ExpiryAction` - Warn, Block, RequireOverride
- `DisposalReason` - Expired, Damaged, Recalled, QualityIssue, Contamination, Other

#### Entities
- `ProductBatch` - Main batch/lot tracking entity with:
  - ProductId, StoreId, BatchNumber
  - ExpiryDate, ManufactureDate
  - InitialQuantity, CurrentQuantity, ReservedQuantity, SoldQuantity, DisposedQuantity
  - SupplierId, GrnId, TransferReceiptId references
  - ReceivedAt, ReceivedByUserId, Status, UnitCost
  - Computed: DaysUntilExpiry, IsExpired, AvailableQuantity

- `ProductBatchConfiguration` - Per-product tracking settings:
  - RequiresBatchTracking, RequiresExpiryDate
  - ExpiryWarningDays, ExpiryCriticalDays
  - ExpiredItemAction, NearExpiryAction
  - UseFifo, UseFefo, TrackManufactureDate
  - MinimumShelfLifeDaysOnReceipt

- `BatchStockMovement` - Batch-level movement history:
  - BatchId, ProductId, StoreId
  - MovementType, Quantity, QuantityBefore, QuantityAfter
  - ReferenceType, ReferenceId, ReferenceNumber
  - MovedAt, MovedByUserId, UnitCost

- `BatchDisposal` - Disposal/waste tracking:
  - BatchId, StoreId, Quantity, Reason, Description
  - DisposedAt, ApprovedByUserId, DisposedByUserId
  - IsWitnessed, WitnessName, PhotoPath

### DTOs Created (BatchTrackingDtos.cs)
- `ProductBatchDto`, `CreateProductBatchDto`, `BatchReceivingEntryDto`
- `BatchSelectionDto`, `ProductBatchSummaryDto`, `BatchQueryDto`
- `ProductBatchConfigurationDto`, `UpdateProductBatchConfigurationDto`
- `BatchStockMovementDto`, `RecordBatchMovementDto`
- `BatchDisposalDto`, `CreateBatchDisposalDto`
- `ExpiryValidationResultDto`, `BatchAvailabilityDto`, `ShelfLifeValidationDto`
- `BatchAllocationDto`, `AllocateBatchesRequestDto`, `BatchAllocationResultDto`

### EF Configuration (BatchTrackingConfiguration.cs)
- `ProductBatchConfiguration` - Unique index on ProductId+StoreId+BatchNumber
- `ProductBatchConfigurationEntityConfiguration` - One config per product
- `BatchStockMovementConfiguration` - Indexes on BatchId, ProductId, MovedAt
- `BatchDisposalConfiguration` - Indexes on BatchId, DisposedAt, Reason

### Interface (IProductBatchService.cs)
- Batch Management: Create, Get, Query, Update batches
- Batch Configuration: Get/Save configuration, check requirements
- Expiry Validation: ValidateShelfLife, ValidateBatchForSale, CheckAvailability
- Batch Allocation: FIFO/FEFO allocation, Reserve, Release, Deduct
- Batch Movements: Record and query movements
- Batch Disposal: Create and query disposals
- Expiry Monitoring: GetExpiring, GetExpired, GetCritical, UpdateStatuses

### Service Implementation (ProductBatchService.cs)
- Full implementation of IProductBatchService
- FIFO/FEFO batch allocation algorithm
- Shelf life validation on receipt
- Expiry validation for sales (Warn/Block/RequireOverride)
- Automatic status updates based on expiry
- Movement tracking for all batch operations
- Disposal workflow with witness support

### Unit Tests (ProductBatchServiceTests.cs)
- Constructor validation tests (3 tests)
- Batch management tests (5 tests)
- Batch configuration tests (4 tests)
- Expiry validation tests (4 tests)
- Batch allocation tests (4 tests)
- Batch disposal tests (1 test)
- Expiry monitoring tests (3 tests)
- Batch movement tests (1 test)
