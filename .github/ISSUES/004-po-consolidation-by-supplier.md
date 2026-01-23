# feat: Implement PO Consolidation by Supplier

**Labels:** `enhancement` `backend` `purchase-orders` `priority-high`

## Overview

Implement logic to consolidate multiple reorder suggestions into single purchase orders grouped by supplier. When multiple products from the same supplier need reordering, they should be combined into a single PO rather than creating separate POs for each product.

## Background

Currently, the `ReorderRule` entity has a `ConsolidateReorders` flag, but there's no implementation. The industry best practice is to consolidate orders to:
- Reduce the number of POs to manage
- Potentially qualify for volume discounts
- Reduce shipping costs
- Simplify receiving process

## Requirements

### Consolidation Logic

```csharp
public interface IPurchaseOrderConsolidationService
{
    /// <summary>
    /// Groups reorder suggestions by supplier and creates consolidated POs
    /// </summary>
    Task<List<PurchaseOrder>> CreateConsolidatedPurchaseOrdersAsync(
        List<ReorderSuggestion> suggestions,
        bool sendImmediately = false);

    /// <summary>
    /// Determines the optimal grouping for a set of suggestions
    /// </summary>
    Task<Dictionary<int, List<ReorderSuggestion>>> GroupSuggestionsBySupplierAsync(
        List<ReorderSuggestion> suggestions);
}
```

### Consolidation Service Implementation

```csharp
public class PurchaseOrderConsolidationService : IPurchaseOrderConsolidationService
{
    public async Task<List<PurchaseOrder>> CreateConsolidatedPurchaseOrdersAsync(
        List<ReorderSuggestion> suggestions,
        bool sendImmediately = false)
    {
        var settings = await _settingsService.GetSettingsAsync();
        var createdPOs = new List<PurchaseOrder>();

        // Group by supplier
        var grouped = await GroupSuggestionsBySupplierAsync(suggestions);

        foreach (var (supplierId, supplierSuggestions) in grouped)
        {
            // Split into chunks if exceeding MaxItemsPerPO
            var chunks = supplierSuggestions
                .Chunk(settings.MaxItemsPerPO)
                .ToList();

            foreach (var chunk in chunks)
            {
                var po = await CreatePurchaseOrderFromSuggestionsAsync(
                    supplierId,
                    chunk.ToList(),
                    sendImmediately);

                if (po != null)
                {
                    createdPOs.Add(po);
                }
            }
        }

        return createdPOs;
    }

    private async Task<PurchaseOrder?> CreatePurchaseOrderFromSuggestionsAsync(
        int supplierId,
        List<ReorderSuggestion> suggestions,
        bool sendImmediately)
    {
        var settings = await _settingsService.GetSettingsAsync();

        // Calculate total to check against minimum
        var estimatedTotal = suggestions.Sum(s => s.EstimatedCost);
        if (estimatedTotal < settings.MinimumPOAmount)
        {
            _logger.LogInformation(
                "Skipping PO for supplier {SupplierId}: total {Total} below minimum {Min}",
                supplierId, estimatedTotal, settings.MinimumPOAmount);
            return null;
        }

        // Create PO
        var po = new PurchaseOrder
        {
            SupplierId = supplierId,
            OrderDate = DateTime.UtcNow,
            Status = sendImmediately ? PurchaseOrderStatus.Sent : PurchaseOrderStatus.Draft,
            Items = suggestions.Select(s => new PurchaseOrderItem
            {
                ProductId = s.ProductId,
                OrderedQuantity = s.SuggestedQuantity,
                UnitCost = s.Product.CostPrice,
                TotalCost = s.SuggestedQuantity * s.Product.CostPrice
            }).ToList()
        };

        // Generate PO number
        po.PONumber = await _poService.GeneratePONumberAsync();

        // Calculate totals
        po.SubTotal = po.Items.Sum(i => i.TotalCost);
        po.TaxAmount = 0; // Or calculate based on tax rules
        po.TotalAmount = po.SubTotal + po.TaxAmount;

        // Set expected delivery date based on supplier lead time
        var supplier = await _supplierService.GetSupplierByIdAsync(supplierId);
        var leadTime = supplier?.LeadTimeDays ?? settings.DefaultLeadTimeDays;
        po.ExpectedDate = DateTime.UtcNow.AddDays(leadTime);

        // Save PO
        await _poService.CreatePurchaseOrderAsync(po);

        // Update suggestions to link to this PO
        foreach (var suggestion in suggestions)
        {
            suggestion.Status = ReorderSuggestionStatus.Converted;
            suggestion.PurchaseOrderId = po.Id;
        }
        await _context.SaveChangesAsync();

        return po;
    }

    public async Task<Dictionary<int, List<ReorderSuggestion>>> GroupSuggestionsBySupplierAsync(
        List<ReorderSuggestion> suggestions)
    {
        var grouped = new Dictionary<int, List<ReorderSuggestion>>();

        foreach (var suggestion in suggestions)
        {
            // Determine supplier: PreferredSupplierId from ReorderRule, or Product.SupplierId
            var supplierId = await GetSupplierIdForSuggestionAsync(suggestion);

            if (supplierId == null)
            {
                _logger.LogWarning(
                    "No supplier found for product {ProductId}, skipping",
                    suggestion.ProductId);
                continue;
            }

            if (!grouped.ContainsKey(supplierId.Value))
            {
                grouped[supplierId.Value] = new List<ReorderSuggestion>();
            }

            grouped[supplierId.Value].Add(suggestion);
        }

        return grouped;
    }
}
```

### Grouping Rules

1. **Primary Grouping**: By Supplier ID
2. **Secondary Split**: If items exceed `MaxItemsPerPO`, split into multiple POs
3. **Minimum Check**: Skip creating PO if total is below `MinimumPOAmount`
4. **Supplier Priority**: Use `ReorderRule.PreferredSupplierId` first, fall back to `Product.SupplierId`

### Handling Products with Multiple Suppliers

Currently, the system only supports one supplier per product (`Product.SupplierId`). For future enhancement:

```csharp
// Future: ProductSupplier junction table
public class ProductSupplier
{
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public decimal UnitCost { get; set; }
    public int LeadTimeDays { get; set; }
    public int Priority { get; set; } // 1 = primary, 2 = secondary, etc.
    public bool IsActive { get; set; }
}
```

## Acceptance Criteria

### Core Consolidation
- [ ] Suggestions with same supplier are grouped into single PO
- [ ] PO is created with all items from grouped suggestions
- [ ] Each item has correct quantity, unit cost, and total cost
- [ ] PO total is calculated correctly
- [ ] PO number is auto-generated

### Settings Respect
- [ ] `ConsolidatePOsBySupplier` setting is checked (if false, create individual POs)
- [ ] `MaxItemsPerPO` is respected (split large groups)
- [ ] `MinimumPOAmount` is respected (skip tiny POs)

### Suggestion Updates
- [ ] Suggestions are marked as `Converted` after PO creation
- [ ] Suggestions are linked to created PO via `PurchaseOrderId`
- [ ] Original suggestion data is preserved

### Status Handling
- [ ] PO created as `Draft` when `sendImmediately = false`
- [ ] PO created as `Sent` when `sendImmediately = true`
- [ ] Auto-approval threshold is checked for status determination

### Error Handling
- [ ] Products without suppliers are skipped with warning log
- [ ] Failed PO creation doesn't affect other supplier groups
- [ ] Partial success returns list of successfully created POs

### Logging
- [ ] Log when consolidation starts
- [ ] Log number of suggestions being processed
- [ ] Log resulting PO count per supplier
- [ ] Log when suggestions are skipped (no supplier, below minimum)
- [ ] Log any errors with full context

## Technical Notes

### Chunk Extension Method

```csharp
public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Chunk<T>(
        this IEnumerable<T> source, int size)
    {
        return source
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / size)
            .Select(g => g.Select(x => x.item));
    }
}
```

### Transaction Handling

All PO creations for a single consolidation run should be in a transaction:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Create all POs
    // Update all suggestions
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### UI Integration

Add a button to the PurchaseOrders view to manually trigger consolidation:

```
[Create POs from Suggestions] -> Opens dialog showing:
- X suggestions pending
- Will create Y purchase orders
- [Cancel] [Create as Draft] [Create and Send]
```

## Test Cases

1. **Single supplier, single product** - Creates one PO with one item
2. **Single supplier, multiple products** - Creates one PO with multiple items
3. **Multiple suppliers** - Creates separate PO for each supplier
4. **Exceeds MaxItemsPerPO** - Splits into multiple POs for same supplier
5. **Below MinimumPOAmount** - Skips PO creation, suggestions remain pending
6. **Product without supplier** - Skipped with warning, others processed
7. **Mixed approved/pending** - Only approved suggestions are converted
8. **Consolidation disabled** - Creates individual PO per suggestion

## Dependencies
- Issue #001: IInventoryAnalyticsService (provides suggestions)
- Issue #003: System Configuration (provides settings)

## Blocked By
- Issue #001: Implement IInventoryAnalyticsService

## Blocks
- Issue #007: Manager PO Review Dashboard (uses consolidated POs)

## Estimated Complexity
**Medium** - Clear algorithm with several edge cases
