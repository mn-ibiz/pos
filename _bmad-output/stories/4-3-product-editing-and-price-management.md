# Story 4.3: Product Editing and Price Management

Status: done

## Story

As an administrator,
I want to edit products and manage prices,
So that the catalog stays current.

## Acceptance Criteria

1. **Given** products exist in the system
   **When** editing a product
   **Then** admin can modify all product fields

2. **Given** a product price is changed
   **When** the change is saved
   **Then** price changes should be logged in audit trail with old and new values

3. **Given** a product is being edited
   **When** scheduling is needed
   **Then** admin can set effective date for price changes (optional)

4. **Given** a product exists
   **When** status change is needed
   **Then** admin can activate/deactivate products

5. **Given** a product is deactivated
   **When** viewing POS screen
   **Then** inactive products should not appear on POS screen

## Tasks / Subtasks

- [x] Task 1: Implement Product Edit View (AC: #1, #4)
  - [x] Reuse ProductEditorDialog from Story 4.2 (handles both create/edit modes)
  - [x] Load existing product data into dialog fields
  - [x] Allow modification of all fields (Name, Description, Category, Price, etc.)
  - [x] Add activate/deactivate toggle via ToggleActiveAsync command

- [x] Task 2: Implement Price Change Logging (AC: #2)
  - [x] ProductService.UpdateProductAsync logs old/new values to AuditLog
  - [x] Captures OldValues including previous price
  - [x] Captures NewValues including new price
  - [x] Stores user ID, timestamp, and machine name

- [~] Task 3: Implement Scheduled Price Changes (AC: #3) - DEFERRED (marked optional)
  - [ ] Add effective date field for price changes
  - [ ] Create ScheduledPriceChange entity
  - [ ] Implement job to apply scheduled changes
  - [ ] Show pending price changes indicator
  - Note: This feature is optional per AC#3 and deferred to future sprint

- [x] Task 4: Filter Inactive Products (AC: #5)
  - [x] GetActiveProductsAsync excludes inactive products
  - [x] Status column shows Active/Inactive badge with color coding
  - [x] ShowInactiveProducts checkbox to toggle visibility of inactive products

- [~] Task 5: Create Price History View - DEFERRED
  - [ ] Create PriceHistoryDialog
  - [ ] Show all price changes from AuditLog for a product
  - [ ] Display: date, old price, new price, changed by
  - Note: Deferred to future sprint; audit data is captured and queryable

## Dev Notes

### Price Change Audit

```csharp
public class PriceChangeAudit
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangedAt { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public bool IsApplied { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
```

### Audit Service Integration

```csharp
public async Task UpdateProductAsync(int id, UpdateProductDto dto)
{
    var product = await _repository.GetByIdAsync(id);
    if (product == null) throw new NotFoundException("Product not found");

    // Check for price change
    if (product.SellingPrice != dto.SellingPrice)
    {
        await _auditService.LogPriceChangeAsync(new PriceChangeAuditDto
        {
            ProductId = id,
            OldPrice = product.SellingPrice,
            NewPrice = dto.SellingPrice,
            ChangedByUserId = _sessionService.CurrentUser!.Id,
            EffectiveDate = dto.PriceEffectiveDate
        });
    }

    // Update product
    product.Name = dto.Name;
    product.SellingPrice = dto.SellingPrice;
    // ... other fields
    product.UpdatedAt = DateTime.UtcNow;

    await _unitOfWork.SaveChangesAsync();
}
```

### Price History View

```
+------------------------------------------+
|  Price History: Tusker Lager              |
+------------------------------------------+
|  Current Price: KSh 350.00                |
|                                           |
|  History:                                 |
|  +------------------------------------+   |
|  | Date       | Old   | New   | By    |   |
|  |------------|-------|-------|-------|   |
|  | 2025-12-15 | 300   | 350   | Admin |   |
|  | 2025-10-01 | 280   | 300   | Admin |   |
|  | 2025-06-15 | 250   | 280   | Admin |   |
|  | 2025-01-01 | 220   | 250   | Admin |   |
|  +------------------------------------+   |
|                                           |
|  Pending Changes:                         |
|  - KSh 380.00 effective 2026-01-01        |
|                                           |
|  [Close]                                  |
+------------------------------------------+
```

### Scheduled Price Change Job

```csharp
public class PriceSchedulerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ApplyScheduledPriceChangesAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ApplyScheduledPriceChangesAsync()
    {
        var pendingChanges = await _priceChangeService
            .GetPendingChangesAsync(DateTime.Now);

        foreach (var change in pendingChanges)
        {
            await _productService.ApplyPriceChangeAsync(
                change.ProductId, change.NewPrice);
            await _priceChangeService.MarkAsAppliedAsync(change.Id);
        }
    }
}
```

### Product Status Indicators
In product list view:
- Active: Normal display
- Inactive: Grayed out with "Inactive" badge
- Pending price change: Price in orange with clock icon

### Validation Rules for Edit
- Cannot change product code (immutable)
- Price must be > 0
- Effective date must be in the future
- Cannot deactivate product with pending orders

### References
- [Source: docs/PRD_Hospitality_POS_System.md#6.1.1-Product-Information]
- [Source: docs/PRD_Hospitality_POS_System.md#10.3-Audit-Trail]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List

1. **Overlap with Story 4-2**: Most of this story's functionality was already implemented as part of Story 4-2 (Product Creation), which included both create AND edit modes in the same ProductEditorDialog.

2. **Product Edit View**: ProductEditorDialog.xaml.cs detects edit mode by checking if `_existingProduct is not null` and pre-populates all fields with existing data.

3. **Price Change Logging**: The ProductService.UpdateProductAsync method already captures:
   - OldValues: Code, Name, CategoryId, SellingPrice, CostPrice, TaxRate, IsActive
   - NewValues: Same fields with updated values
   - Stored in AuditLog with UserId, EntityType, EntityId, MachineName, CreatedAt

4. **Activate/Deactivate**: Implemented via:
   - ProductManagementViewModel.ToggleActiveAsync command
   - ProductService.SetProductActiveAsync method
   - Creates audit log entry with "ProductActivated" or "ProductDeactivated" action

5. **Inactive Filtering**: Implemented via:
   - ShowInactiveProducts checkbox in ProductManagementView
   - GetActiveProductsAsync returns only IsActive=true products
   - Status column with colored badges (green=Active, gray=Inactive)

6. **Deferred Features**:
   - Scheduled Price Changes (Task 3): Requires new entity, migration, background service - deferred as optional
   - Price History View (Task 5): Audit data is captured; UI dialog can be added in future sprint

### File List

**Files from Story 4-2 (no new files required):**
- src/HospitalityPOS.WPF/Views/Dialogs/ProductEditorDialog.xaml (edit mode support)
- src/HospitalityPOS.WPF/Views/Dialogs/ProductEditorDialog.xaml.cs (edit mode logic)
- src/HospitalityPOS.WPF/ViewModels/ProductManagementViewModel.cs (EditProductAsync, ToggleActiveAsync)
- src/HospitalityPOS.Infrastructure/Services/ProductService.cs (UpdateProductAsync with audit, SetProductActiveAsync)

### Acceptance Criteria Verification

| AC | Status | Implementation |
|----|--------|----------------|
| #1 | ✓ PASS | ProductEditorDialog loads existing product and allows editing all fields |
| #2 | ✓ PASS | UpdateProductAsync logs old/new price values to AuditLog |
| #3 | ~ DEFERRED | Optional feature; scheduled changes not implemented |
| #4 | ✓ PASS | ToggleActiveAsync enables activate/deactivate with confirmation |
| #5 | ✓ PASS | GetActiveProductsAsync excludes inactive; ShowInactiveProducts filter |
