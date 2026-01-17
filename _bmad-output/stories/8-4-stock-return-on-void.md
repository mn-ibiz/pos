# Story 8.4: Stock Return on Void

Status: completed

## Story

As the system,
I want to restore stock when receipts are voided,
So that inventory remains accurate.

## Acceptance Criteria

1. **Given** a receipt with inventory-tracked items is voided
   **When** the void is processed
   **Then** stock should be increased by the voided quantities

2. **Given** stock is restored
   **When** recording the movement
   **Then** stock movement should be recorded with void reference

3. **Given** movement is recorded
   **When** categorizing the transaction
   **Then** movement type should be "Void/Return"

## Tasks / Subtasks

- [x] Task 1: Update Void Service
  - [x] Call inventory restore on void
  - [x] Process each voided item
  - [x] Handle tracking vs non-tracking products
  - [x] Create movement records

- [x] Task 2: Implement Restore Stock Method
  - [x] Add RestoreStockForVoidAsync method
  - [x] Calculate quantities to restore
  - [x] Update product stock
  - [x] Log movements

- [x] Task 3: Update Stock Movement Types
  - [x] Add Void movement type
  - [x] Distinguish from manual returns
  - [x] Link to void record
  - [x] Include in reports

- [x] Task 4: Handle Partial Voids
  - [x] Support voiding specific items
  - [x] Restore only voided items
  - [x] Track partial void references

- [x] Task 5: Test Void-Restore Flow
  - [x] Unit tests for stock restoration
  - [x] Integration tests with void
  - [x] Verify movement records
  - [x] Test idempotency

## Dev Notes

### Void Service Integration

```csharp
public class ReceiptVoidService : IReceiptVoidService
{
    private readonly IInventoryService _inventoryService;

    public async Task<VoidResult> VoidReceiptAsync(VoidRequest request)
    {
        var receipt = await _receiptRepo.GetByIdWithItemsAsync(request.ReceiptId);

        // ... void processing ...

        // Restore inventory for tracked items
        await RestoreInventoryForVoidAsync(receipt);

        // ... complete void ...
    }

    private async Task RestoreInventoryForVoidAsync(Receipt receipt)
    {
        foreach (var item in receipt.ReceiptItems)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);

            if (product != null && product.TrackInventory)
            {
                await _inventoryService.RestoreStockAsync(
                    item.ProductId,
                    item.Quantity,
                    StockMovementType.Void,
                    $"Void: {receipt.ReceiptNumber}");
            }
        }
    }
}
```

### Inventory Service - Restore Stock

```csharp
public async Task<StockMovement> RestoreStockAsync(
    int productId,
    decimal quantity,
    string movementType,
    string reference)
{
    var product = await _productRepo.GetByIdAsync(productId);
    if (product == null)
        throw new NotFoundException("Product not found");

    if (!product.TrackInventory)
        return null!;  // No tracking needed

    var previousStock = product.CurrentStock;
    product.CurrentStock += quantity;

    await _productRepo.UpdateAsync(product);

    var movement = new StockMovement
    {
        ProductId = productId,
        MovementType = movementType,
        Quantity = quantity,  // Positive for restoration
        PreviousStock = previousStock,
        NewStock = product.CurrentStock,
        Reference = reference,
        UserId = _authService.CurrentUser.Id
    };

    await _movementRepo.AddAsync(movement);

    await _auditService.LogAsync(AuditAction.StockRestore,
        $"Stock restored: {product.Name} +{quantity}. {reference}",
        new Dictionary<string, object>
        {
            { "ProductId", productId },
            { "Quantity", quantity },
            { "Reference", reference },
            { "PreviousStock", previousStock },
            { "NewStock", product.CurrentStock }
        });

    await _unitOfWork.SaveChangesAsync();

    // Send update notification
    _messenger.Send(new StockChangedMessage(productId));

    // Check if product was out of stock and is now available
    if (previousStock <= 0 && product.CurrentStock > 0)
    {
        _messenger.Send(new StockRestoredMessage(productId, product.Name));
    }

    return movement;
}
```

### Stock Movement for Void

```csharp
// Example movement record for void
var movement = new StockMovement
{
    ProductId = 123,
    MovementType = "Void",
    Quantity = 2,  // Positive - adding back
    PreviousStock = 48,
    NewStock = 50,
    Reference = "Void: R-20251220-0042",
    Notes = "Customer returned - wrong order",
    UserId = currentUserId,
    CreatedAt = DateTime.UtcNow
};
```

### Void with Stock Report (80mm)

```
================================================
         *** VOID SLIP ***
================================================
Receipt #: R-20251220-0042
Void Date: 2025-12-20 16:30

VOIDED ITEMS:
------------------------------------------------
2x Tusker Lager                     KSh    700
   Stock: 48 -> 50 (Restored)
1x Grilled Chicken                  KSh    850
   Stock: 10 -> 11 (Restored)
2x Chips                            KSh    400
   (No inventory tracking)
------------------------------------------------
TOTAL VOIDED:                       KSh  1,950
STOCK ITEMS RESTORED: 2
------------------------------------------------

Void Reason: Wrong order
Notes: Customer received wrong table's order

Voided By: John Smith

================================================
```

### Prevent Double Restoration

```csharp
public async Task<VoidResult> VoidReceiptAsync(VoidRequest request)
{
    var receipt = await _receiptRepo.GetByIdAsync(request.ReceiptId);

    // Check if already voided
    if (receipt.Status == "Voided")
    {
        throw new InvalidOperationException("Receipt has already been voided");
    }

    // Check if stock was already restored (edge case)
    var existingVoidMovements = await _movementRepo.GetByReferenceAsync(
        $"Void: {receipt.ReceiptNumber}");

    if (existingVoidMovements.Any())
    {
        throw new InvalidOperationException("Stock has already been restored for this receipt");
    }

    // Proceed with void...
}
```

### Idempotency Check

```csharp
public async Task<StockMovement> RestoreStockAsync(
    int productId,
    decimal quantity,
    string movementType,
    string reference)
{
    // Check for existing movement with same reference
    var existing = await _movementRepo.GetByReferenceAndProductAsync(
        reference, productId);

    if (existing != null)
    {
        _logger.LogWarning(
            "Attempted duplicate stock restore: {Reference}, Product {ProductId}",
            reference, productId);
        return existing;  // Return existing instead of creating duplicate
    }

    // Continue with restoration...
}
```

### Stock Movement Query for Voids

```csharp
public async Task<List<StockMovement>> GetVoidMovementsAsync(
    DateTime startDate,
    DateTime endDate)
{
    return await _context.StockMovements
        .Where(m => m.MovementType == StockMovementType.Void)
        .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
        .Include(m => m.Product)
        .Include(m => m.User)
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.5.4-Void-Stock-Return]
- [Source: docs/PRD_Hospitality_POS_System.md#IM-030]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List

1. **Integrated IInventoryService into ReceiptVoidService**: Added `IInventoryService` as a constructor dependency to centralize stock restoration logic.

2. **Replaced custom stock restoration with centralized service**: The private `RestoreStockForReceiptAsync` method was refactored to call `_inventoryService.RestoreStockForVoidAsync(receipt)` instead of implementing stock restoration logic directly. This:
   - Eliminates code duplication between ReceiptVoidService and InventoryService
   - Leverages the idempotency check built into `InventoryService.RestoreStockForVoidAsync`
   - Follows the same pattern used in `ReceiptService.SettleReceiptAsync` for stock deduction

3. **Idempotency is handled by InventoryService**: The `RestoreStockForVoidAsync` method in InventoryService checks for existing void movements by `ReferenceType == "Void"` and `ReferenceId == receipt.Id` before creating new movements, preventing double restoration.

4. **Existing tests cover the functionality**: The `InventoryServiceTests.cs` file contains tests for:
   - `RestoreStockForVoidAsync_ShouldRestoreAllItems`: Verifies stock is restored for all items
   - `RestoreStockForVoidAsync_ShouldBeIdempotent`: Verifies that calling restore twice only restores stock once

5. **Movement type and reference**: Stock movements created during void are of type `MovementType.Void` with `ReferenceType = "Void"` and `ReferenceId = receipt.Id`, enabling proper tracking and reporting.

### File List

- `/Users/linuxlab/Desktop/POS/src/HospitalityPOS.Infrastructure/Services/ReceiptVoidService.cs` - Modified to inject IInventoryService and use centralized stock restoration
- `/Users/linuxlab/Desktop/POS/src/HospitalityPOS.Infrastructure/Services/InventoryService.cs` - Already contained RestoreStockForVoidAsync with idempotency (no changes needed)
- `/Users/linuxlab/Desktop/POS/src/HospitalityPOS.Core/Interfaces/IInventoryService.cs` - Already defined RestoreStockForVoidAsync interface (no changes needed)
- `/Users/linuxlab/Desktop/POS/tests/HospitalityPOS.Business.Tests/Services/InventoryServiceTests.cs` - Already contains unit tests for RestoreStockForVoidAsync including idempotency test (no changes needed)
