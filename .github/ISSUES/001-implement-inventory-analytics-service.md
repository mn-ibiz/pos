# feat: Implement IInventoryAnalyticsService for Reorder Suggestions

**Labels:** `enhancement` `backend` `inventory` `priority-high`

## Overview

Implement the `IInventoryAnalyticsService` interface to provide core functionality for generating reorder suggestions based on stock levels. This is the foundational service that enables automatic purchase order generation.

The interface already exists at `Core/Interfaces/IInventoryAnalyticsService.cs` but has no implementation. The `ReorderRule` and `ReorderSuggestion` entities are already defined in `Core/Entities/InventoryAnalyticsEntities.cs`.

## Background

Currently in `PurchaseOrdersViewModel.cs:685-688`, the reorder suggestions functionality is disabled:
```csharp
private Task LoadReorderSuggestionsAsync()
{
    // IInventoryAnalyticsService not available - reorder suggestions disabled
    return Task.CompletedTask;
}
```

## Requirements

### Core Service Implementation

Create `Infrastructure/Services/InventoryAnalyticsService.cs` implementing `IInventoryAnalyticsService` with the following methods:

| Method | Description |
|--------|-------------|
| `GetReorderRuleAsync(int productId, int storeId)` | Get reorder configuration for a product |
| `GetReorderRulesAsync(int? storeId)` | Get all reorder rules, optionally filtered by store |
| `SaveReorderRuleAsync(ReorderRule rule)` | Create/update reorder configuration |
| `DeleteReorderRuleAsync(int ruleId)` | Remove a reorder rule |
| `GenerateReorderSuggestionsAsync(int? storeId)` | Main method - scan inventory and create suggestions |
| `GetPendingReorderSuggestionsAsync(int? storeId)` | Get suggestions awaiting action |
| `ApproveReorderSuggestionAsync(int suggestionId, int userId)` | Mark suggestion as approved |
| `RejectReorderSuggestionAsync(int suggestionId, int userId, string reason)` | Reject with reason |
| `ConvertSuggestionToPurchaseOrderAsync(int suggestionId)` | Convert to actual PO |
| `ConvertSuggestionsToPurchaseOrdersAsync(List<int> suggestionIds)` | Batch convert with supplier grouping |

### Reorder Point Calculation

Implement the standard reorder point formula:
```
Reorder Point = (Lead Time Days x Average Daily Sales) + Safety Stock
```

For Economic Order Quantity (EOQ):
```
EOQ = sqrt((2 x Annual Demand x Order Cost) / Holding Cost per Unit)
```

### Suggestion Generation Logic

```csharp
public async Task<List<ReorderSuggestion>> GenerateReorderSuggestionsAsync(int? storeId)
{
    // 1. Get all products with TrackInventory = true
    // 2. For each product, get current stock from Inventory table
    // 3. Compare against ReorderRule.ReorderPoint (or Product.MinStockLevel if no rule)
    // 4. If CurrentStock <= ReorderPoint, create ReorderSuggestion
    // 5. Calculate SuggestedQuantity based on:
    //    - ReorderRule.ReorderQuantity if set
    //    - Or EOQ calculation
    //    - Or (MaxStockLevel - CurrentStock)
    // 6. Set Priority based on DaysUntilStockout:
    //    - Critical: <= 1 day
    //    - High: <= 3 days
    //    - Medium: <= 7 days
    //    - Low: > 7 days
    // 7. Avoid duplicates - check for existing pending suggestions
    // 8. Save suggestions to database
    // 9. Return list of newly created suggestions
}
```

### Priority Calculation

```csharp
public enum ReorderPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

private ReorderPriority CalculatePriority(decimal currentStock, decimal avgDailySales)
{
    if (avgDailySales <= 0) return ReorderPriority.Low;

    var daysUntilStockout = currentStock / avgDailySales;

    return daysUntilStockout switch
    {
        <= 1 => ReorderPriority.Critical,
        <= 3 => ReorderPriority.High,
        <= 7 => ReorderPriority.Medium,
        _ => ReorderPriority.Low
    };
}
```

### Average Daily Sales Calculation

```csharp
private async Task<decimal> CalculateAverageDailySalesAsync(int productId, int storeId, int lookbackDays = 30)
{
    // Query StockMovement table for Sale type movements
    // Sum quantities over lookbackDays
    // Divide by lookbackDays
    // Return average
}
```

## Acceptance Criteria

### Functional Requirements
- [ ] Service implements all methods defined in `IInventoryAnalyticsService`
- [ ] Products below reorder point generate `ReorderSuggestion` records
- [ ] Suggestions include accurate `EstimatedCost` based on product cost price
- [ ] Priority is calculated based on days until stockout
- [ ] `DaysUntilStockout` is populated for each suggestion
- [ ] Duplicate suggestions are not created for products with pending suggestions
- [ ] Suggestions respect `ReorderRule.MinOrderQuantity` constraint
- [ ] Suggestions respect `ReorderRule.OrderMultiple` constraint (round up to nearest multiple)
- [ ] Service correctly uses `PreferredSupplierId` from ReorderRule, falling back to `Product.SupplierId`
- [ ] Products without a supplier are skipped (logged as warning)

### Data Integrity
- [ ] Suggestions are linked to correct Product, Store, and Supplier
- [ ] `CreatedAt` timestamp is set when suggestion is created
- [ ] `ApprovedAt` and `ApprovedByUserId` set when approved
- [ ] `RejectedAt` and reason set when rejected
- [ ] Status transitions are validated (can't approve already rejected, etc.)
- [ ] Audit logging for all suggestion state changes

### Performance
- [ ] Batch processing for large product catalogs (process in chunks of 100)
- [ ] Async/await pattern used throughout
- [ ] Efficient database queries with proper `.Include()` statements
- [ ] Use `AsNoTracking()` for read-only queries

### Registration
- [ ] Service registered in DI container (`Infrastructure/DependencyInjection.cs`)
- [ ] Scoped lifetime (new instance per request/operation)

## Technical Notes

### Existing Entities to Use
- `ReorderRule` - Configuration per product/store (in InventoryAnalyticsEntities.cs)
- `ReorderSuggestion` - Generated suggestions (in InventoryAnalyticsEntities.cs)
- `Inventory` - Current stock levels
- `Product` - Product info including `MinStockLevel`, `SupplierId`
- `StockMovement` - For calculating average daily sales

### Database Considerations
- Add index on `ReorderSuggestion.Status` for filtering pending suggestions
- Add index on `Inventory.CurrentStock` for low stock queries
- Add composite index on `StockMovement (ProductId, StoreId, MovementType, CreatedAt)`

### Sample Query for Low Stock Products

```csharp
var lowStockProducts = await _context.Inventories
    .Include(i => i.Product)
        .ThenInclude(p => p.Supplier)
    .Where(i => i.Product.TrackInventory)
    .Where(i => i.CurrentStock <= (i.ReorderLevel ?? i.Product.MinStockLevel ?? 0))
    .Where(i => !i.Product.IsDeleted)
    .ToListAsync();
```

## Test Cases

1. **Product at reorder point** - Should generate suggestion
2. **Product below reorder point** - Should generate suggestion with higher priority
3. **Product above reorder point** - Should NOT generate suggestion
4. **Product with existing pending suggestion** - Should NOT create duplicate
5. **Product without supplier** - Should skip and log warning
6. **Product with ReorderRule** - Should use rule's settings over product defaults
7. **Order multiple constraint** - Quantity 7 with multiple 5 = suggest 10
8. **Min order quantity** - Quantity 3 with min 10 = suggest 10

## Dependencies
- None (this is a foundational service)

## Blocked By
- None

## Blocks
- Issue #002: Stock Monitoring Background Service
- Issue #004: PO Consolidation by Supplier
- Issue #007: Manager PO Review Dashboard

## Estimated Complexity
**Medium-High** - Core business logic with multiple calculations and database operations
