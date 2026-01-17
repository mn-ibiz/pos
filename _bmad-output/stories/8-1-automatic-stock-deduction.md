# Story 8.1: Automatic Stock Deduction

Status: done

## Story

As the system,
I want to automatically deduct stock when items are sold,
So that inventory levels stay accurate in real-time.

## Acceptance Criteria

1. **Given** a product has inventory tracking enabled
   **When** the product is sold (receipt settled)
   **Then** stock quantity should be deducted by the sold amount

2. **Given** stock is deducted
   **When** recording the movement
   **Then** stock movement should be recorded with reference to the order

3. **Given** stock levels change
   **When** tracking history
   **Then** previous and new stock levels should be logged

4. **Given** stock reaches zero
   **When** product is depleted
   **Then** product should be flagged as out-of-stock

## Tasks / Subtasks

- [x] Task 1: Create Stock Movement Entity
  - [x] Create StockMovement entity class (already exists in HospitalityPOS.Core/Entities/StockMovement.cs)
  - [x] Configure EF Core mappings (already configured)
  - [x] Create database migration (TrackInventory field added)
  - [x] Add movement type enum (MovementType enum in SystemEnums.cs)

- [x] Task 2: Add Tracking Fields to Product
  - [x] Add TrackInventory boolean (added to Product.cs)
  - [x] CurrentStock decimal (in Inventory entity via one-to-one relationship)
  - [x] MinStockLevel decimal (already exists in Product.cs)
  - [x] MaxStockLevel decimal (already exists in Product.cs)
  - [x] Add computed IsLowStock and IsOutOfStock properties

- [x] Task 3: Create Inventory Service
  - [x] Create IInventoryService interface (IInventoryService.cs)
  - [x] Implement DeductStockAsync method
  - [x] Implement RestoreStockAsync method
  - [x] Implement GetStockLevelAsync method
  - [x] Implement AdjustStockAsync method
  - [x] Implement CheckAvailabilityAsync method

- [x] Task 4: Integrate with Receipt Settlement
  - [x] Call DeductStockAsync on settlement (ReceiptService.SettleReceiptAsync)
  - [x] Process each receipt item via DeductStockForReceiptAsync
  - [x] Handle tracking vs non-tracking products (skip if TrackInventory=false)
  - [x] Handle out-of-stock gracefully (cap at 0, don't throw)

- [x] Task 5: Implement Out-of-Stock Detection
  - [x] Check stock after deduction (in DeductStockAsync)
  - [x] Log warnings for out-of-stock and low-stock products
  - [x] GetLowStockProductsAsync and GetOutOfStockProductsAsync methods
  - [x] Computed IsLowStock/IsOutOfStock properties on Product entity

## Dev Notes

### StockMovement Entity

```csharp
public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }  // Positive for in, negative for out
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string? Reference { get; set; }  // Order/Receipt/PO number
    public int? ReferenceId { get; set; }   // Receipt ID, PO ID, etc.
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

### Stock Movement Types

```csharp
public static class StockMovementType
{
    public const string Sale = "Sale";
    public const string Void = "Void";
    public const string Adjustment = "Adjustment";
    public const string PurchaseReceive = "PurchaseReceive";
    public const string Transfer = "Transfer";
    public const string Wastage = "Wastage";
    public const string StockTake = "StockTake";
    public const string Return = "Return";
}
```

### Product Inventory Fields

```csharp
public class Product
{
    // ... existing properties

    // Inventory tracking
    public bool TrackInventory { get; set; } = true;
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinStockLevel { get; set; } = 5;
    public decimal MaxStockLevel { get; set; } = 100;
    public string? StockUnit { get; set; }  // "pcs", "kg", "liters"

    // Computed properties
    public bool IsLowStock => TrackInventory && CurrentStock <= MinStockLevel && CurrentStock > 0;
    public bool IsOutOfStock => TrackInventory && CurrentStock <= 0;
}
```

### Inventory Service

```csharp
public interface IInventoryService
{
    Task<StockMovement> DeductStockAsync(int productId, decimal quantity, string reference, int? referenceId = null);
    Task<StockMovement> RestoreStockAsync(int productId, decimal quantity, string movementType, string reference);
    Task<StockMovement> AdjustStockAsync(int productId, decimal newQuantity, string reason);
    Task<decimal> GetStockLevelAsync(int productId);
    Task<bool> CheckAvailabilityAsync(int productId, decimal requiredQuantity);
    Task DeductStockForReceiptAsync(Receipt receipt);
}

public class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessenger _messenger;

    public async Task<StockMovement> DeductStockAsync(
        int productId,
        decimal quantity,
        string reference,
        int? referenceId = null)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null)
            throw new NotFoundException("Product not found");

        if (!product.TrackInventory)
            return null!;  // No tracking needed

        var previousStock = product.CurrentStock;
        product.CurrentStock -= quantity;

        // Don't allow negative stock
        if (product.CurrentStock < 0)
        {
            product.CurrentStock = 0;
        }

        await _productRepo.UpdateAsync(product);

        // Create movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = StockMovementType.Sale,
            Quantity = -quantity,  // Negative for deduction
            PreviousStock = previousStock,
            NewStock = product.CurrentStock,
            Reference = reference,
            ReferenceId = referenceId,
            UserId = _authService.CurrentUser.Id
        };

        await _movementRepo.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        // Check stock levels and send notifications
        await CheckStockLevelsAsync(product);

        return movement;
    }

    public async Task DeductStockForReceiptAsync(Receipt receipt)
    {
        foreach (var item in receipt.ReceiptItems)
        {
            await DeductStockAsync(
                item.ProductId,
                item.Quantity,
                receipt.ReceiptNumber,
                receipt.Id);
        }
    }

    private async Task CheckStockLevelsAsync(Product product)
    {
        if (product.IsOutOfStock)
        {
            _messenger.Send(new StockOutMessage(product.Id, product.Name));

            await _auditService.LogAsync(AuditAction.StockOut,
                $"Product {product.Name} is now out of stock",
                new Dictionary<string, object>
                {
                    { "ProductId", product.Id },
                    { "ProductName", product.Name }
                });
        }
        else if (product.IsLowStock)
        {
            _messenger.Send(new LowStockMessage(product.Id, product.Name, product.CurrentStock));
        }
    }

    public async Task<StockMovement> RestoreStockAsync(
        int productId,
        decimal quantity,
        string movementType,
        string reference)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null || !product.TrackInventory)
            return null!;

        var previousStock = product.CurrentStock;
        product.CurrentStock += quantity;

        await _productRepo.UpdateAsync(product);

        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = movementType,
            Quantity = quantity,  // Positive for addition
            PreviousStock = previousStock,
            NewStock = product.CurrentStock,
            Reference = reference,
            UserId = _authService.CurrentUser.Id
        };

        await _movementRepo.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        return movement;
    }
}
```

### Settlement Integration

```csharp
public class SettlementService : ISettlementService
{
    private readonly IInventoryService _inventoryService;

    public async Task<SettlementResult> SettleReceiptAsync(
        int receiptId,
        List<PaymentRequest> payments)
    {
        // ... payment processing ...

        // Deduct inventory for all items
        var receipt = await _receiptRepo.GetByIdWithItemsAsync(receiptId);
        await _inventoryService.DeductStockForReceiptAsync(receipt);

        // ... finalize settlement ...
    }
}
```

### Stock Movement Report (80mm)

```
================================================
     STOCK MOVEMENT REPORT
     Product: Tusker Lager
     2025-12-20
================================================
Time  | Type    | Qty   | Stock | Reference
------|---------|-------|-------|------------
09:15 | Sale    | -2    | 48    | R-0042
10:30 | Sale    | -4    | 44    | R-0045
11:00 | Receive | +24   | 68    | PO-0012
12:45 | Sale    | -2    | 66    | R-0048
14:20 | Void    | +2    | 68    | R-0048
------------------------------------------------
Opening: 50   Closing: 68   Net: +18
================================================
```

### EF Core Configuration

```csharp
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(sm => sm.PreviousStock).HasColumnType("decimal(18,4)");
        builder.Property(sm => sm.NewStock).HasColumnType("decimal(18,4)");

        builder.HasOne(sm => sm.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sm => sm.ProductId);
        builder.HasIndex(sm => sm.CreatedAt);
        builder.HasIndex(sm => sm.MovementType);
    }
}
```

### EF Core 10 Bulk Operations

```csharp
// EF Core 10 ExecuteUpdateAsync - Update stock without loading entities
public async Task MarkCategoryOutOfStockAsync(int categoryId)
{
    await _context.Products
        .Where(p => p.CategoryId == categoryId && p.CurrentStock <= 0)
        .ExecuteUpdateAsync(s =>
        {
            s.IsActive = false;
            s.UpdatedAt = DateTime.UtcNow;
        });
}

// EF Core 10 - Bulk reset for stock take
public async Task ResetStockForCategoryAsync(int categoryId, decimal defaultStock)
{
    await _context.Products
        .Where(p => p.CategoryId == categoryId && p.TrackInventory)
        .ExecuteUpdateAsync(s =>
        {
            s.CurrentStock = defaultStock;
            s.UpdatedAt = DateTime.UtcNow;
        });
}

// EF Core 10 - Archive old stock movements
public async Task ArchiveOldMovementsAsync(DateTime beforeDate)
{
    await _context.StockMovements
        .Where(sm => sm.CreatedAt < beforeDate)
        .ExecuteDeleteAsync();
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.5.1-Inventory-Tracking]
- [Source: docs/PRD_Hospitality_POS_System.md#IM-001 to IM-005]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Stock deduction is now triggered automatically when a receipt is settled via ReceiptService.SettleReceiptAsync
- Non-tracked products (TrackInventory=false) are skipped during deduction
- Stock movements are recorded for every deduction with full audit trail
- Idempotency check in RestoreStockForVoidAsync prevents double restoration
- Comprehensive unit tests written covering all acceptance criteria

### File List
- src/HospitalityPOS.Core/Entities/Product.cs (modified - added TrackInventory, IsLowStock, IsOutOfStock)
- src/HospitalityPOS.Core/Interfaces/IInventoryService.cs (new)
- src/HospitalityPOS.Core/Interfaces/IEntityRepositories.cs (modified - added IStockMovementRepository)
- src/HospitalityPOS.Infrastructure/Services/InventoryService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/ReceiptService.cs (modified - integrated IInventoryService)
- src/HospitalityPOS.Infrastructure/Repositories/EntityRepositories.cs (modified - added StockMovementRepository, fixed InventoryRepository)
- src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs (modified - registered new services)
- src/HospitalityPOS.Infrastructure/Data/Configurations/ProductConfiguration.cs (modified - added TrackInventory config)
- tests/HospitalityPOS.Business.Tests/Services/InventoryServiceTests.cs (new)
