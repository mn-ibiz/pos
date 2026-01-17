# Story 10.3: Inventory Reports

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/ReportService.cs` - Inventory reporting with:
  - `GenerateCurrentStockReportAsync` - Current stock levels
  - `GenerateLowStockReportAsync` - Below minimum threshold
  - `GenerateStockMovementReportAsync` - In/out movements
  - `GenerateStockValuationReportAsync` - Inventory valuation
  - `GenerateDeadStockReportAsync` - Non-moving stock

## Story

As a manager,
I want inventory reports,
So that I can monitor stock levels and movement.

## Acceptance Criteria

1. **Given** inventory data exists
   **When** generating inventory reports
   **Then** available reports should include: Current Stock Report, Low Stock Report, Stock Movement Report, Stock Valuation Report, Dead Stock Report

2. **Given** report parameters
   **When** filtering
   **Then** reports can be filtered by category

3. **Given** reports are generated
   **When** printing
   **Then** reports are formatted for 80mm thermal printing

## Tasks / Subtasks

- [ ] Task 1: Create Inventory Report Screen
  - [ ] Create InventoryReportsView.xaml
  - [ ] Create InventoryReportsViewModel
  - [ ] Report type selection
  - [ ] Category filter

- [ ] Task 2: Implement Current Stock Report
  - [ ] List all products with stock
  - [ ] Show stock value
  - [ ] Group by category option
  - [ ] Sort by various columns

- [ ] Task 3: Implement Low Stock Report
  - [ ] Filter products below minimum
  - [ ] Show reorder quantity
  - [ ] Highlight critical items
  - [ ] Calculate reorder value

- [ ] Task 4: Implement Stock Movement Report
  - [ ] Show all movements
  - [ ] Filter by date range
  - [ ] Filter by movement type
  - [ ] Show running balance

- [ ] Task 5: Implement Stock Valuation Report
  - [ ] Calculate stock value
  - [ ] Show cost vs selling value
  - [ ] Group by category
  - [ ] Show margins

## Dev Notes

### Inventory Reports Screen

```
+------------------------------------------+
|      INVENTORY REPORTS                    |
+------------------------------------------+
| Report: [Current Stock_________] [v]      |
| Category: [All Categories_______] [v]     |
| [Generate Report]                         |
+------------------------------------------+
|                                           |
|     CURRENT STOCK REPORT                  |
|     As of 2025-12-20 18:00                |
|                                           |
|  Product         | Stock | Cost  | Value  |
|  ----------------|-------|-------|--------|
|  BEVERAGES                                |
|  Tusker Lager    |   48  |  300  | 14,400 |
|  Coca Cola       |   96  |   50  |  4,800 |
|  Fanta Orange    |   72  |   50  |  3,600 |
|  ----------------|-------|-------|--------|
|  FOOD                                     |
|  Grilled Chicken |   15  |  500  |  7,500 |
|  Fish & Chips    |   12  |  600  |  7,200 |
|  Chips Regular   |   45  |  150  |  6,750 |
|  ─────────────────────────────────────    |
|  TOTAL VALUE:             KSh 185,400     |
|                                           |
+------------------------------------------+
| [Print Report]          [Export CSV]      |
+------------------------------------------+
```

### InventoryReportsViewModel

```csharp
public partial class InventoryReportsViewModel : BaseViewModel
{
    [ObservableProperty]
    private InventoryReportType _selectedReportType;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private object? _reportData;

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        ReportData = SelectedReportType switch
        {
            InventoryReportType.CurrentStock => await GenerateCurrentStockAsync(),
            InventoryReportType.LowStock => await GenerateLowStockAsync(),
            InventoryReportType.StockMovement => await GenerateStockMovementAsync(),
            InventoryReportType.StockValuation => await GenerateStockValuationAsync(),
            InventoryReportType.DeadStock => await GenerateDeadStockAsync(),
            _ => null
        };
    }

    private async Task<List<CurrentStockItem>> GenerateCurrentStockAsync()
    {
        var query = _context.Products
            .Where(p => p.TrackInventory && p.IsActive)
            .Include(p => p.Category)
            .AsQueryable();

        if (SelectedCategory != null)
        {
            query = query.Where(p => p.CategoryId == SelectedCategory.Id);
        }

        return await query
            .OrderBy(p => p.Category.Name)
            .ThenBy(p => p.Name)
            .Select(p => new CurrentStockItem
            {
                CategoryName = p.Category.Name,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                StockUnit = p.StockUnit ?? "pcs",
                CostPrice = p.CostPrice,
                StockValue = p.CurrentStock * p.CostPrice,
                MinStock = p.MinStockLevel,
                Status = p.CurrentStock <= 0 ? "OUT"
                       : p.CurrentStock <= p.MinStockLevel ? "LOW"
                       : "OK"
            })
            .ToListAsync();
    }

    private async Task<List<LowStockItem>> GenerateLowStockAsync()
    {
        return await _context.Products
            .Where(p => p.TrackInventory && p.IsActive)
            .Where(p => p.CurrentStock <= p.MinStockLevel)
            .Include(p => p.Category)
            .OrderBy(p => p.CurrentStock)
            .Select(p => new LowStockItem
            {
                CategoryName = p.Category.Name,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                MinStock = p.MinStockLevel,
                ReorderQty = p.MaxStockLevel - p.CurrentStock,
                CostPrice = p.CostPrice,
                ReorderValue = (p.MaxStockLevel - p.CurrentStock) * p.CostPrice,
                Status = p.CurrentStock <= 0 ? "CRITICAL" : "LOW"
            })
            .ToListAsync();
    }

    private async Task<List<StockMovementItem>> GenerateStockMovementAsync()
    {
        var query = _context.StockMovements
            .Where(sm => sm.CreatedAt >= FromDate && sm.CreatedAt < ToDate.AddDays(1))
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .AsQueryable();

        if (SelectedCategory != null)
        {
            query = query.Where(sm => sm.Product.CategoryId == SelectedCategory.Id);
        }

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .Select(sm => new StockMovementItem
            {
                Date = sm.CreatedAt,
                ProductName = sm.Product.Name,
                MovementType = sm.MovementType,
                Quantity = sm.Quantity,
                PreviousStock = sm.PreviousStock,
                NewStock = sm.NewStock,
                Reference = sm.Reference,
                UserName = sm.User.FullName
            })
            .ToListAsync();
    }

    private async Task<StockValuationReport> GenerateStockValuationAsync()
    {
        var products = await _context.Products
            .Where(p => p.TrackInventory && p.IsActive)
            .Include(p => p.Category)
            .ToListAsync();

        var categories = products
            .GroupBy(p => p.Category?.Name ?? "Uncategorized")
            .Select(g => new CategoryValuation
            {
                CategoryName = g.Key,
                ItemCount = g.Count(),
                TotalUnits = g.Sum(p => p.CurrentStock),
                CostValue = g.Sum(p => p.CurrentStock * p.CostPrice),
                RetailValue = g.Sum(p => p.CurrentStock * p.SellingPrice)
            })
            .OrderByDescending(c => c.CostValue)
            .ToList();

        return new StockValuationReport
        {
            AsOfDate = DateTime.Now,
            Categories = categories,
            TotalCostValue = categories.Sum(c => c.CostValue),
            TotalRetailValue = categories.Sum(c => c.RetailValue),
            PotentialProfit = categories.Sum(c => c.RetailValue - c.CostValue)
        };
    }

    private async Task<List<DeadStockItem>> GenerateDeadStockAsync()
    {
        var cutoffDate = DateTime.Today.AddDays(-30);  // No movement in 30 days

        var productsWithRecentMovement = await _context.StockMovements
            .Where(sm => sm.CreatedAt >= cutoffDate)
            .Select(sm => sm.ProductId)
            .Distinct()
            .ToListAsync();

        return await _context.Products
            .Where(p => p.TrackInventory && p.IsActive && p.CurrentStock > 0)
            .Where(p => !productsWithRecentMovement.Contains(p.Id))
            .Include(p => p.Category)
            .Select(p => new DeadStockItem
            {
                CategoryName = p.Category.Name,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                StockValue = p.CurrentStock * p.CostPrice,
                LastMovementDate = _context.StockMovements
                    .Where(sm => sm.ProductId == p.Id)
                    .OrderByDescending(sm => sm.CreatedAt)
                    .Select(sm => (DateTime?)sm.CreatedAt)
                    .FirstOrDefault(),
                DaysSinceMovement = 30
            })
            .OrderByDescending(p => p.StockValue)
            .ToListAsync();
    }
}
```

### Current Stock Report Print (80mm)

```
================================================
     CURRENT STOCK REPORT
     As of 2025-12-20 18:00
================================================

** BEVERAGES **
Product              | Stk | Cost | Value
---------------------|-----|------|----------
Tusker Lager         |  48 |  300 |   14,400
Coca Cola 500ml      |  96 |   50 |    4,800
Fanta Orange         |  72 |   50 |    3,600
Category Total:              KSh   22,800
------------------------------------------------

** FOOD **
Product              | Stk | Cost | Value
---------------------|-----|------|----------
Grilled Chicken      |  15 |  500 |    7,500
Fish & Chips         |  12 |  600 |    7,200
Chips Regular        |  45 |  150 |    6,750
Category Total:              KSh   21,450
================================================
TOTAL STOCK VALUE:           KSh  185,400
Total SKUs: 45
Items in Stock: 42
Out of Stock: 3
================================================
```

### Low Stock Report Print (80mm)

```
================================================
     LOW STOCK REPORT
     2025-12-20
================================================

** CRITICAL (OUT OF STOCK) **
Product              | Stk | Min | Reorder
---------------------|-----|-----|----------
Grilled Chicken      |   0 |   5 |      10
Fresh Juice          |   0 |  10 |      20
------------------------------------------------

** LOW STOCK **
Product              | Stk | Min | Reorder
---------------------|-----|-----|----------
Tusker Lager         |   5 |  10 |      20
Coca Cola            |   3 |   5 |      20
Chips Regular        |   8 |  10 |      15
================================================
REORDER SUMMARY:
Items to Reorder: 5
Total Reorder Value:         KSh   15,400
================================================
```

### Stock Movement Report Print (80mm)

```
================================================
     STOCK MOVEMENT REPORT
     2025-12-20
================================================

Product: Tusker Lager
------------------------------------------------
Time  | Type     | Qty  | Stock | Reference
------|----------|------|-------|-------------
09:15 | Sale     |   -2 |    48 | R-0042
10:30 | Sale     |   -4 |    44 | R-0045
11:00 | Receive  |  +24 |    68 | GRN-001
12:45 | Sale     |   -2 |    66 | R-0048
14:20 | Void     |   +2 |    68 | R-0048
------------------------------------------------
Opening: 50   Closing: 68   Net: +18
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.7.3-Inventory-Reports]
- [Source: docs/PRD_Hospitality_POS_System.md#RP-025 to RP-030]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
