# Story 8.5: Stock Take (Physical Inventory)

Status: done

## Story

As a manager,
I want to conduct stock takes and reconcile physical counts,
So that system inventory matches actual inventory.

## Acceptance Criteria

1. **Given** products exist in the system
   **When** stock take is initiated
   **Then** user can enter physical count for each product

2. **Given** counts are entered
   **When** calculating variance
   **Then** system should calculate variance (system vs physical)

3. **Given** variance is calculated
   **When** showing value
   **Then** variance value (quantity * cost) should be calculated

4. **Given** discrepancies are found
   **When** manager reviews
   **Then** manager can approve adjustments to align system with physical counts

5. **Given** adjustments are approved
   **When** generating report
   **Then** stock take report should be generated showing all variances

6. **Given** report is approved
   **When** finalizing stock take
   **Then** approved adjustments should create stock movements

## Tasks / Subtasks

- [ ] Task 1: Create Stock Take Entities
  - [ ] Create StockTake entity
  - [ ] Create StockTakeItem entity
  - [ ] Configure EF Core mappings
  - [ ] Create database migration

- [ ] Task 2: Create Stock Take Screen
  - [ ] Create StockTakeView.xaml
  - [ ] Create StockTakeViewModel
  - [ ] Show product list with count entry
  - [ ] Display variance calculations

- [ ] Task 3: Implement Count Entry
  - [ ] Input physical counts
  - [ ] Calculate variance on entry
  - [ ] Show variance value
  - [ ] Highlight discrepancies

- [ ] Task 4: Implement Approval Flow
  - [ ] Review variance summary
  - [ ] Approve/reject adjustments
  - [ ] Require manager authorization
  - [ ] Log approval decisions

- [ ] Task 5: Generate Stock Take Report
  - [ ] Create variance report
  - [ ] Show quantity differences
  - [ ] Show value impact
  - [ ] Format for 80mm printing

## Dev Notes

### StockTake Entity

```csharp
public class StockTake
{
    public int Id { get; set; }
    public string StockTakeNumber { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int StartedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string Status { get; set; } = "InProgress";  // InProgress, PendingApproval, Approved, Cancelled
    public string? Notes { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public int ItemsWithVariance { get; set; }

    // Navigation
    public User StartedByUser { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();
}
```

### StockTakeItem Entity

```csharp
public class StockTakeItem
{
    public int Id { get; set; }
    public int StockTakeId { get; set; }
    public int ProductId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal? PhysicalQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }  // Physical - System
    public decimal CostPrice { get; set; }
    public decimal VarianceValue { get; set; }  // Variance * Cost
    public bool IsCounted { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public string? Notes { get; set; }
    public DateTime? CountedAt { get; set; }
    public int? CountedByUserId { get; set; }

    // Navigation
    public StockTake StockTake { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User? CountedByUser { get; set; }
}
```

### Stock Take Screen Layout

```
+------------------------------------------+
|      STOCK TAKE                           |
|      ST-20251220-001                      |
+------------------------------------------+
| Started: 2025-12-20 09:00  By: John       |
| Status: In Progress  Counted: 15/50       |
+------------------------------------------+
| Search: [__________]  Category: [All___] |
| Filter: [ ]Counted [ ]Not Counted [x]All |
+------------------------------------------+
| Product       | System | Count | Variance |
|---------------|--------|-------|----------|
| Tusker Lager  |   50   |  [48] |    -2    |
| Coca Cola     |   30   |  [30] |     0    |
| Chips Regular |   45   |  [__] |    --    |
| Grilled Chick |   10   |  [12] |    +2    |
+------------------------------------------+
| Progress: 30%  Variance: KSh -5,400       |
+------------------------------------------+
| [Cancel]  [Save Draft]  [Submit for Review]|
+------------------------------------------+
```

### StockTakeViewModel

```csharp
public partial class StockTakeViewModel : BaseViewModel
{
    [ObservableProperty]
    private StockTake _stockTake = null!;

    [ObservableProperty]
    private ObservableCollection<StockTakeItemViewModel> _items = new();

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _countedItems;

    [ObservableProperty]
    private decimal _totalVarianceValue;

    [ObservableProperty]
    private int _itemsWithVariance;

    public async Task StartNewStockTakeAsync()
    {
        var stockTake = new StockTake
        {
            StockTakeNumber = await GenerateStockTakeNumberAsync(),
            StartedByUserId = _authService.CurrentUser.Id,
            Status = "InProgress"
        };

        // Load all tracked products
        var products = await _productRepo.GetTrackedProductsAsync();

        foreach (var product in products)
        {
            stockTake.Items.Add(new StockTakeItem
            {
                ProductId = product.Id,
                SystemQuantity = product.CurrentStock,
                CostPrice = product.CostPrice
            });
        }

        await _stockTakeRepo.AddAsync(stockTake);
        await _unitOfWork.SaveChangesAsync();

        StockTake = stockTake;
        LoadItems();
    }

    private void LoadItems()
    {
        Items = new ObservableCollection<StockTakeItemViewModel>(
            StockTake.Items.Select(i => new StockTakeItemViewModel
            {
                Item = i,
                ProductName = i.Product?.Name ?? "Unknown",
                Unit = i.Product?.StockUnit ?? "pcs"
            }));

        TotalItems = Items.Count;
        UpdateSummary();
    }

    [RelayCommand]
    private void UpdateCount(StockTakeItemViewModel item)
    {
        if (item.PhysicalQuantity.HasValue)
        {
            item.Item.PhysicalQuantity = item.PhysicalQuantity;
            item.Item.VarianceQuantity = item.PhysicalQuantity.Value - item.Item.SystemQuantity;
            item.Item.VarianceValue = item.Item.VarianceQuantity * item.Item.CostPrice;
            item.Item.IsCounted = true;
            item.Item.CountedAt = DateTime.UtcNow;
            item.Item.CountedByUserId = _authService.CurrentUser.Id;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        CountedItems = Items.Count(i => i.Item.IsCounted);
        TotalVarianceValue = Items.Sum(i => i.Item.VarianceValue);
        ItemsWithVariance = Items.Count(i => i.Item.IsCounted && i.Item.VarianceQuantity != 0);
    }

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        foreach (var item in Items)
        {
            await _stockTakeItemRepo.UpdateAsync(item.Item);
        }
        await _unitOfWork.SaveChangesAsync();

        await _dialogService.ShowMessageAsync("Saved", "Stock take draft saved");
    }

    [RelayCommand]
    private async Task SubmitForReviewAsync()
    {
        // Check all items counted
        var uncounted = Items.Count(i => !i.Item.IsCounted);
        if (uncounted > 0)
        {
            var proceed = await _dialogService.ShowConfirmationAsync(
                "Uncounted Items",
                $"{uncounted} items haven't been counted. Submit anyway?");

            if (!proceed) return;
        }

        StockTake.Status = "PendingApproval";
        StockTake.TotalVarianceValue = TotalVarianceValue;
        StockTake.ItemsWithVariance = ItemsWithVariance;

        await SaveDraftAsync();

        await _dialogService.ShowMessageAsync("Submitted",
            "Stock take submitted for approval");
    }
}

public class StockTakeItemViewModel : ObservableObject
{
    public StockTakeItem Item { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";

    [ObservableProperty]
    private decimal? _physicalQuantity;

    public decimal VarianceQuantity => Item.VarianceQuantity;
    public decimal VarianceValue => Item.VarianceValue;
    public bool HasVariance => Item.IsCounted && Item.VarianceQuantity != 0;
}
```

### Approval Screen

```
+------------------------------------------+
|      STOCK TAKE APPROVAL                  |
|      ST-20251220-001                      |
+------------------------------------------+
| Submitted by: John  Date: 2025-12-20     |
| Total Items: 50  With Variance: 8        |
+------------------------------------------+
| VARIANCE SUMMARY                          |
+------------------------------------------+
| Product       | System | Count | Value    |
|---------------|--------|-------|----------|
| Tusker Lager  |   50   |   48  | -KSh 700 |
| Coca Cola     |   30   |   28  | -KSh 100 |
| Grilled Chick |   10   |   12  | +KSh 500 |
+------------------------------------------+
| NET VARIANCE:              -KSh 5,400     |
+------------------------------------------+
|                                           |
|  [Reject]         [Approve & Adjust]      |
+------------------------------------------+
```

### Apply Adjustments on Approval

```csharp
public async Task ApproveStockTakeAsync(int stockTakeId)
{
    var stockTake = await _stockTakeRepo.GetByIdWithItemsAsync(stockTakeId);

    if (!await _authService.HasPermissionAsync(Permission.StockTake_Approve))
    {
        throw new UnauthorizedException("Not authorized to approve stock takes");
    }

    foreach (var item in stockTake.Items.Where(i => i.IsCounted && i.VarianceQuantity != 0))
    {
        // Create stock movement for each variance
        await _inventoryService.AdjustStockAsync(
            item.ProductId,
            item.PhysicalQuantity!.Value,
            "Stock Take Adjustment",
            $"Stock Take: {stockTake.StockTakeNumber}");

        item.IsApproved = true;
    }

    stockTake.Status = "Approved";
    stockTake.CompletedAt = DateTime.UtcNow;
    stockTake.ApprovedByUserId = _authService.CurrentUser.Id;

    await _stockTakeRepo.UpdateAsync(stockTake);
    await _unitOfWork.SaveChangesAsync();
}
```

### Stock Take Report (80mm)

```
================================================
     STOCK TAKE REPORT
     ST-20251220-001
================================================
Started: 2025-12-20 09:00 by John
Completed: 2025-12-20 14:30
Approved by: Mary (Manager)
------------------------------------------------
VARIANCE DETAILS:
------------------------------------------------
Product          | Sys | Phy | Var | Value
-----------------|-----|-----|-----|----------
Tusker Lager     |  50 |  48 |  -2 | -KSh 700
Coca Cola        |  30 |  28 |  -2 | -KSh 100
Grilled Chicken  |  10 |  12 |  +2 | +KSh 500
Fanta            |  25 |  22 |  -3 | -KSh 150
------------------------------------------------
SUMMARY:
Total Items Counted: 50
Items with Variance: 4
------------------------------------------------
Shortages (negative): KSh 950
Overages (positive):  KSh 500
NET VARIANCE:        -KSh 450
------------------------------------------------
Approved: _____________________
Signature: ____________________
Date: ________________________
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.5.5-Stock-Taking]
- [Source: docs/PRD_Hospitality_POS_System.md#IM-040 to IM-045]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
