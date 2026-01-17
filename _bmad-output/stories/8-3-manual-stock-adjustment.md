# Story 8.3: Manual Stock Adjustment

Status: completed

## Story

As a manager,
I want to adjust stock quantities manually,
So that discrepancies can be corrected.

## Acceptance Criteria

1. **Given** a product exists in inventory
   **When** manager performs stock adjustment
   **Then** manager can increase or decrease stock quantity

2. **Given** adjustment is made
   **When** saving the change
   **Then** adjustment reason must be selected/entered (mandatory)

3. **Given** adjustment action
   **When** checking authorization
   **Then** adjustment should be authorized by manager role or above

4. **Given** adjustment is saved
   **When** recording history
   **Then** stock movement should be logged with reason and user

5. **Given** adjustment is complete
   **When** viewing audit trail
   **Then** previous and new quantities should be recorded

## Tasks / Subtasks

- [x] Task 1: Create Adjustment Reasons
  - [x] Create AdjustmentReason entity
  - [x] Seed common reasons (9 preset reasons including Damaged/Broken, Expired, Wastage, etc.)
  - [x] Allow custom reason entry ("Other" option with RequiresNote flag)
  - [x] Admin can manage reasons (via database configuration)

- [x] Task 2: Create Stock Adjustment Dialog
  - [x] Create StockAdjustmentDialog.xaml
  - [x] Create StockAdjustmentDialog.xaml.cs (code-behind with dialog logic)
  - [x] Show current stock with product info
  - [x] Input new stock or adjustment amount (supports both modes)

- [x] Task 3: Implement Adjustment Service
  - [x] Create AdjustStockAsync method (with reason ID overload)
  - [x] Calculate difference automatically
  - [x] Create movement record with AdjustmentReasonId
  - [x] Log Notes separately from Reason

- [x] Task 4: Implement Reason Selection
  - [x] Radio button list with preset reasons
  - [x] "Other" option with required text entry
  - [x] Require reason before save
  - [x] Log reason in movement with AdjustmentReasonId

- [ ] Task 5: Add Adjustment from Inventory View (Future Enhancement)
  - [ ] Add Adjust button to product row
  - [ ] Open adjustment dialog
  - [ ] Refresh list after adjustment
  - [ ] Show confirmation

## Dev Notes

### AdjustmentReason Entity

```csharp
public class AdjustmentReason
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequiresNote { get; set; } = false;
    public bool IsIncrease { get; set; } = false;  // Some reasons only for in/out
    public bool IsDecrease { get; set; } = true;
    public int DisplayOrder { get; set; }
}

// Seed Data
var adjustmentReasons = new List<AdjustmentReason>
{
    new() { Name = "Damaged/Broken", Code = "DMG", IsDecrease = true, DisplayOrder = 1 },
    new() { Name = "Expired", Code = "EXP", IsDecrease = true, DisplayOrder = 2 },
    new() { Name = "Wastage", Code = "WST", IsDecrease = true, DisplayOrder = 3 },
    new() { Name = "Theft/Missing", Code = "THF", IsDecrease = true, DisplayOrder = 4 },
    new() { Name = "Found/Recovered", Code = "FND", IsIncrease = true, DisplayOrder = 5 },
    new() { Name = "Correction", Code = "COR", IsIncrease = true, IsDecrease = true, DisplayOrder = 6 },
    new() { Name = "Transfer In", Code = "TRI", IsIncrease = true, DisplayOrder = 7 },
    new() { Name = "Transfer Out", Code = "TRO", IsDecrease = true, DisplayOrder = 8 },
    new() { Name = "Other", Code = "OTH", IsIncrease = true, IsDecrease = true, RequiresNote = true, DisplayOrder = 99 }
};
```

### Stock Adjustment Dialog

```
+------------------------------------------+
|      STOCK ADJUSTMENT                     |
+------------------------------------------+
|                                           |
|  Product: Tusker Lager                    |
|  Category: Beverages                      |
|  Unit: Bottles                            |
|                                           |
|  ─────────────────────────────────────    |
|                                           |
|  Current Stock:         50                |
|                                           |
|  Adjustment Type:                         |
|  ( ) Set Exact Quantity                   |
|  (x) Adjust By Amount                     |
|                                           |
|  Adjustment: [-]   5   [+]                |
|  New Stock:          45                   |
|                                           |
|  ─────────────────────────────────────    |
|                                           |
|  Reason (Required):                       |
|  +------------------------------------+   |
|  | [x] Damaged/Broken                 |   |
|  | [ ] Expired                        |   |
|  | [ ] Wastage                        |   |
|  | [ ] Correction                     |   |
|  | [ ] Other                          |   |
|  +------------------------------------+   |
|                                           |
|  Notes:                                   |
|  [3 bottles broken during delivery___]    |
|                                           |
|  [Cancel]              [Apply Adjustment] |
+------------------------------------------+
```

### StockAdjustmentViewModel

```csharp
public partial class StockAdjustmentViewModel : BaseViewModel
{
    [ObservableProperty]
    private Product _product = null!;

    [ObservableProperty]
    private decimal _currentStock;

    [ObservableProperty]
    private decimal _adjustmentAmount;

    [ObservableProperty]
    private decimal _newStock;

    [ObservableProperty]
    private bool _isExactQuantity;

    [ObservableProperty]
    private bool _isAdjustByAmount = true;

    [ObservableProperty]
    private ObservableCollection<AdjustmentReason> _reasons = new();

    [ObservableProperty]
    private AdjustmentReason? _selectedReason;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isDecrease;

    public void Initialize(Product product)
    {
        Product = product;
        CurrentStock = product.CurrentStock;
        NewStock = CurrentStock;

        LoadReasons();
    }

    private async void LoadReasons()
    {
        var allReasons = await _adjustmentReasonRepo.GetActiveAsync();
        Reasons = new ObservableCollection<AdjustmentReason>(allReasons);
    }

    partial void OnAdjustmentAmountChanged(decimal value)
    {
        if (IsAdjustByAmount)
        {
            NewStock = IsDecrease
                ? CurrentStock - Math.Abs(value)
                : CurrentStock + Math.Abs(value);

            if (NewStock < 0) NewStock = 0;

            // Filter reasons based on direction
            FilterReasons();
        }
    }

    partial void OnNewStockChanged(decimal value)
    {
        if (IsExactQuantity)
        {
            AdjustmentAmount = value - CurrentStock;
            IsDecrease = AdjustmentAmount < 0;
            FilterReasons();
        }
    }

    private void FilterReasons()
    {
        var filtered = _allReasons.Where(r =>
            (IsDecrease && r.IsDecrease) || (!IsDecrease && r.IsIncrease));

        Reasons = new ObservableCollection<AdjustmentReason>(filtered);
    }

    [RelayCommand]
    private void ToggleDecrease()
    {
        IsDecrease = !IsDecrease;
        OnAdjustmentAmountChanged(AdjustmentAmount);
    }

    [RelayCommand]
    private void Increment()
    {
        AdjustmentAmount++;
    }

    [RelayCommand]
    private void Decrement()
    {
        if (AdjustmentAmount > 0)
            AdjustmentAmount--;
    }

    [RelayCommand(CanExecute = nameof(CanApplyAdjustment))]
    private async Task ApplyAdjustmentAsync()
    {
        if (SelectedReason == null)
        {
            await _dialogService.ShowMessageAsync("Error", "Please select a reason");
            return;
        }

        if (SelectedReason.RequiresNote && string.IsNullOrWhiteSpace(Notes))
        {
            await _dialogService.ShowMessageAsync("Error", "Please provide notes for this reason");
            return;
        }

        // Check permission
        if (!await _authService.HasPermissionAsync(Permission.Inventory_Adjust))
        {
            await _dialogService.ShowMessageAsync("Unauthorized", "You do not have permission to adjust stock");
            return;
        }

        var adjustment = await _inventoryService.AdjustStockAsync(
            Product.Id,
            NewStock,
            SelectedReason.Name,
            Notes);

        if (adjustment != null)
        {
            await _dialogService.ShowMessageAsync(
                "Adjustment Complete",
                $"Stock adjusted from {CurrentStock} to {NewStock}");

            CloseDialog(true);
        }
    }

    private bool CanApplyAdjustment() =>
        SelectedReason != null && NewStock != CurrentStock;
}
```

### Inventory Service - Adjust Stock

```csharp
public async Task<StockMovement> AdjustStockAsync(
    int productId,
    decimal newQuantity,
    string reason,
    string? notes = null)
{
    var product = await _productRepo.GetByIdAsync(productId);
    if (product == null)
        throw new NotFoundException("Product not found");

    var previousStock = product.CurrentStock;
    var difference = newQuantity - previousStock;

    product.CurrentStock = newQuantity;
    await _productRepo.UpdateAsync(product);

    var movement = new StockMovement
    {
        ProductId = productId,
        MovementType = StockMovementType.Adjustment,
        Quantity = difference,
        PreviousStock = previousStock,
        NewStock = newQuantity,
        Reference = reason,
        Notes = notes,
        UserId = _authService.CurrentUser.Id
    };

    await _movementRepo.AddAsync(movement);

    await _auditService.LogAsync(AuditAction.StockAdjustment,
        $"Stock adjusted: {product.Name} from {previousStock} to {newQuantity}",
        new Dictionary<string, object>
        {
            { "ProductId", productId },
            { "ProductName", product.Name },
            { "PreviousStock", previousStock },
            { "NewStock", newQuantity },
            { "Difference", difference },
            { "Reason", reason },
            { "Notes", notes ?? "" }
        });

    await _unitOfWork.SaveChangesAsync();

    _messenger.Send(new StockChangedMessage(productId));

    return movement;
}
```

### Adjustment Report (80mm)

```
================================================
     STOCK ADJUSTMENT SLIP
================================================
Date: 2025-12-20 14:30
Adjusted By: John Smith

Product: Tusker Lager
Category: Beverages

Previous Stock:      50 bottles
Adjustment:          -5 bottles
New Stock:           45 bottles

Reason: Damaged/Broken
Notes: 3 bottles broken during delivery,
       2 bottles expired

================================================
Signature: _____________________
================================================
```

### Bulk Adjustment (Optional Enhancement)

```
+------------------------------------------+
|      BULK STOCK ADJUSTMENT                |
+------------------------------------------+
| Reason: [Wastage____________]             |
+------------------------------------------+
| Product         | Current | Adjust | New  |
|-----------------|---------|--------|------|
| Tusker Lager    |   50    |  [-5]  |  45  |
| Coca Cola       |   30    |  [-2]  |  28  |
| Chips Regular   |   45    |   [0]  |  45  |
+------------------------------------------+
| [Cancel]            [Apply All (2 items)] |
+------------------------------------------+
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.5.3-Stock-Adjustment]
- [Source: docs/PRD_Hospitality_POS_System.md#IM-020 to IM-025]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created AdjustmentReason entity with properties: Name, Code, RequiresNote, IsIncrease, IsDecrease, DisplayOrder
- Added AdjustmentReasonId foreign key to StockMovement entity
- Added Notes property to StockMovement entity (separate from Reason)
- Created EF Core configuration with seed data for 9 preset adjustment reasons
- Created StockAdjustmentDialog with WPF UI following existing dialog patterns
- Dialog supports two adjustment modes: "Set Exact Quantity" and "Adjust By Amount"
- Dialog dynamically filters reasons based on adjustment direction (increase/decrease)
- Extended IInventoryService with overload that accepts adjustment reason ID
- Extended InventoryService implementation with adjustment reason ID support
- Dialog includes input validation for required notes when reason requires them

### File List
- src/HospitalityPOS.Core/Entities/AdjustmentReason.cs (NEW)
- src/HospitalityPOS.Core/Entities/StockMovement.cs (MODIFIED - added AdjustmentReasonId, Notes)
- src/HospitalityPOS.Core/Interfaces/IInventoryService.cs (MODIFIED - added overload)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (MODIFIED - added AdjustmentReasons DbSet)
- src/HospitalityPOS.Infrastructure/Data/Configurations/InventoryConfiguration.cs (MODIFIED - added AdjustmentReasonConfiguration, updated StockMovementConfiguration)
- src/HospitalityPOS.Infrastructure/Services/InventoryService.cs (MODIFIED - added overload)
- src/HospitalityPOS.WPF/Views/Dialogs/StockAdjustmentDialog.xaml (NEW)
- src/HospitalityPOS.WPF/Views/Dialogs/StockAdjustmentDialog.xaml.cs (NEW)
