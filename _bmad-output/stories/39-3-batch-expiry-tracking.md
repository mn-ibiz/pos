# Story 39.3: Batch and Expiry Date Tracking

Status: Done

## Story

As a **supermarket manager**,
I want **the system to track batch numbers and expiry dates for all perishable products**,
so that **I can ensure food safety, comply with health regulations, and minimize waste from expired goods**.

## Business Context

**CRITICAL - FOOD SAFETY & LEGAL COMPLIANCE**

Supermarkets selling food products MUST track:
- Batch/lot numbers for traceability (product recalls)
- Expiry dates to prevent sale of expired goods
- FIFO/FEFO to ensure oldest stock sells first

Without this:
- Cannot serve food retailers legally
- Creates food safety liability
- Causes inventory waste (expired goods)
- Fails health inspections
- Cannot execute product recalls

**Market Reality:** DigitalPOS, Smartwas, Uzalynx all have batch/expiry tracking as standard.

## Acceptance Criteria

### AC1: Batch Recording at Goods Receiving
- [ ] GRN form includes BatchNumber field (required for perishables)
- [ ] GRN form includes ExpiryDate field (required for perishables)
- [ ] ManufactureDate field (optional)
- [ ] System validates expiry date is in future
- [ ] Multiple batches can be received for same product

### AC2: Batch-Level Stock Tracking
- [ ] Inventory tracks stock by individual batch
- [ ] Each batch has its own CurrentQuantity
- [ ] Batch status tracked: Active, Depleted, Expired, Recalled
- [ ] Stock reports show batch breakdown

### AC3: FEFO Stock Deduction
- [ ] Sales automatically deduct from earliest-expiring batch first (FEFO)
- [ ] System selects batch with nearest ExpiryDate that has stock
- [ ] Depleted batches marked automatically
- [ ] Stock movement records batch reference

### AC4: Product Configuration
- [ ] Products can be marked as "Requires Batch Tracking"
- [ ] Category-level default for batch tracking requirement
- [ ] Perishable product categories auto-require batch tracking

### AC5: Batch Traceability
- [ ] Report shows which customers bought which batch
- [ ] Link OrderItem to ProductBatch
- [ ] Support product recall workflow (identify affected sales)

## Tasks / Subtasks

- [ ] **Task 1: Database Schema for Batches** (AC: 1, 2)
  - [ ] 1.1 Create ProductBatches table (ProductId, BatchNumber, ExpiryDate, ManufactureDate, ReceivedDate, ReceivedQuantity, CurrentQuantity, Status)
  - [ ] 1.2 Add FEFO index on ProductBatches (ProductId, ExpiryDate ASC WHERE Active)
  - [ ] 1.3 Add BatchId foreign key to OrderItems table
  - [ ] 1.4 Add RequiresBatchTracking to Products table
  - [ ] 1.5 Add RequiresBatchTracking to Categories table (default for new products)
  - [ ] 1.6 Create migration

- [ ] **Task 2: Batch Repository and Service** (AC: 2, 3)
  - [ ] 2.1 Create IProductBatchRepository interface
  - [ ] 2.2 Implement ProductBatchRepository
  - [ ] 2.3 Create IBatchTrackingService interface
  - [ ] 2.4 Implement FEFO batch selection: GetBatchForDeduction(productId, quantity)
  - [ ] 2.5 Implement batch quantity update on sale
  - [ ] 2.6 Implement batch status auto-update (Depleted when CurrentQuantity = 0)
  - [ ] 2.7 Unit tests for FEFO logic

- [ ] **Task 3: Goods Receiving Integration** (AC: 1)
  - [ ] 3.1 Modify GoodsReceivedItem entity to include BatchNumber, ExpiryDate
  - [ ] 3.2 Update GoodsReceivingView to show batch fields
  - [ ] 3.3 Add validation: expiry date must be future
  - [ ] 3.4 Add validation: batch required for products with RequiresBatchTracking
  - [ ] 3.5 Create ProductBatch record on GRN completion
  - [ ] 3.6 Integration tests for GRN with batches

- [ ] **Task 4: Sales Integration (FEFO)** (AC: 3)
  - [ ] 4.1 Modify InventoryService.DeductStockAsync to use batch service
  - [ ] 4.2 Get earliest-expiring batch with available stock
  - [ ] 4.3 Deduct from selected batch
  - [ ] 4.4 Record BatchId on OrderItem
  - [ ] 4.5 Handle multi-batch deduction (if single batch insufficient)
  - [ ] 4.6 Integration tests for FEFO deduction

- [ ] **Task 5: Product Configuration UI** (AC: 4)
  - [ ] 5.1 Add "Requires Batch Tracking" checkbox to Product form
  - [ ] 5.2 Add "Default Batch Tracking" checkbox to Category form
  - [ ] 5.3 Auto-set product flag based on category default
  - [ ] 5.4 Bulk update: set batch tracking for existing perishable products

- [ ] **Task 6: Batch Traceability Report** (AC: 5)
  - [ ] 6.1 Create BatchTraceabilityReport in ReportingService
  - [ ] 6.2 Query: For batch X, show all OrderItems/Receipts/Customers
  - [ ] 6.3 Create report view with batch search
  - [ ] 6.4 Support recall workflow: flag affected transactions
  - [ ] 6.5 Export to Excel

- [ ] **Task 7: Batch Stock Report** (AC: 2)
  - [ ] 7.1 Modify inventory reports to show batch breakdown
  - [ ] 7.2 Show: Product, BatchNumber, ExpiryDate, CurrentQuantity, Status
  - [ ] 7.3 Filter by product, category, expiry range
  - [ ] 7.4 Highlight soon-to-expire batches

## Dev Notes

### FEFO Algorithm

```csharp
public async Task<ProductBatch> GetBatchForDeductionAsync(int productId, decimal quantity)
{
    // Get batches ordered by expiry date (earliest first), excluding expired
    var batches = await _context.ProductBatches
        .Where(b => b.ProductId == productId
                 && b.Status == "Active"
                 && b.CurrentQuantity > 0
                 && (b.ExpiryDate == null || b.ExpiryDate > DateTime.Today))
        .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
        .ToListAsync();

    // Return first batch with sufficient stock, or first batch if partial
    return batches.FirstOrDefault(b => b.CurrentQuantity >= quantity)
        ?? batches.FirstOrDefault();
}
```

### Database Schema (from Gap Analysis)

```sql
CREATE TABLE ProductBatches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE,
    ManufactureDate DATE,
    ReceivedDate DATE NOT NULL DEFAULT GETDATE(),
    ReceivedQuantity DECIMAL(18,3) NOT NULL,
    CurrentQuantity DECIMAL(18,3) NOT NULL,
    UnitCost DECIMAL(18,2),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    GoodsReceivedId INT FOREIGN KEY REFERENCES GoodsReceived(Id),
    Status NVARCHAR(20) DEFAULT 'Active',
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_ProductBatch UNIQUE (ProductId, BatchNumber)
);

CREATE INDEX IX_ProductBatches_FEFO ON ProductBatches (ProductId, ExpiryDate ASC)
    WHERE Status = 'Active' AND CurrentQuantity > 0;

-- Add to OrderItems
ALTER TABLE OrderItems ADD BatchId INT FOREIGN KEY REFERENCES ProductBatches(Id);

-- Add to Products
ALTER TABLE Products ADD RequiresBatchTracking BIT DEFAULT 0;

-- Add to Categories
ALTER TABLE Categories ADD RequiresBatchTracking BIT DEFAULT 0;
```

### Architecture Compliance

- **Layer:** Core (Entities), Infrastructure (Repository), Business (Service)
- **Pattern:** Repository pattern for batches, FEFO service
- **Transactions:** Use Unit of Work for GRN + batch creation
- **Testing:** Comprehensive tests for FEFO logic

### Perishable Category Configuration

Pre-configure these categories to require batch tracking:
- Dairy Products
- Meat & Poultry
- Bakery
- Fresh Produce
- Frozen Foods
- Beverages
- Pharmaceuticals

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.3-Batch-and-Expiry-Date-Tracking]
- [Source: _bmad-output/architecture.md#Inventory]
- [Source: _bmad-output/project-context.md#Database-Guidelines]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
