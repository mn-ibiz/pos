# Story 9.3: Goods Receiving (with PO)

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/GoodsReceivingService.cs` - Full implementation with `ReceiveGoodsAsync` for PO-based receiving
- Supports partial receiving, actual cost tracking, automatic stock updates
- GRN generation with printable format
- Tests in `tests/HospitalityPOS.Business.Tests/Services/GoodsReceivingServiceTests.cs`

## Story

As a stock clerk,
I want to receive goods against a purchase order,
So that inventory is updated with accurate quantities.

## Acceptance Criteria

1. **Given** a purchase order has been created
   **When** goods are received
   **Then** user can select the PO to receive against

2. **Given** PO is selected
   **When** entering received quantities
   **Then** user can enter received quantities (may differ from ordered)

3. **Given** prices may vary
   **When** recording costs
   **Then** actual cost prices can be recorded

4. **Given** delivery is incomplete
   **When** receiving partial shipment
   **Then** partial receiving is supported

5. **Given** receiving is complete
   **When** saving the record
   **Then** stock is automatically increased on save

6. **Given** receiving is saved
   **When** documenting the transaction
   **Then** Goods Received Note (GRN) is generated and printable

## Tasks / Subtasks

- [ ] Task 1: Create GRN Entity
  - [ ] Create GoodsReceivedNote entity
  - [ ] Create GRNItem entity
  - [ ] Configure EF Core mappings
  - [ ] Create database migration

- [ ] Task 2: Create Goods Receiving Screen
  - [ ] Create GoodsReceivingView.xaml
  - [ ] Create GoodsReceivingViewModel
  - [ ] PO selection dropdown
  - [ ] Quantity entry grid

- [ ] Task 3: Implement PO Selection
  - [ ] Filter sent/partially received POs
  - [ ] Load PO items on selection
  - [ ] Show ordered vs received quantities
  - [ ] Pre-populate expected costs

- [ ] Task 4: Implement Quantity Entry
  - [ ] Enter received quantities
  - [ ] Override cost if different
  - [ ] Calculate line totals
  - [ ] Validate against ordered

- [ ] Task 5: Implement Stock Update
  - [ ] Update product stock on save
  - [ ] Create stock movements
  - [ ] Update PO status
  - [ ] Print GRN

## Dev Notes

### GoodsReceivedNote Entity

```csharp
public class GoodsReceivedNote
{
    public int Id { get; set; }
    public string GRNNumber { get; set; } = string.Empty;
    public int? PurchaseOrderId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public string? DeliveryNote { get; set; }  // Supplier's delivery note #
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public int ReceivedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Supplier? Supplier { get; set; }
    public User ReceivedByUser { get; set; } = null!;
    public ICollection<GRNItem> Items { get; set; } = new List<GRNItem>();
}
```

### GRNItem Entity

```csharp
public class GRNItem
{
    public int Id { get; set; }
    public int GRNId { get; set; }
    public int? PurchaseOrderItemId { get; set; }
    public int ProductId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public GoodsReceivedNote GRN { get; set; } = null!;
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
    public Product Product { get; set; } = null!;
}
```

### Goods Receiving Screen

```
+------------------------------------------+
|      GOODS RECEIVING                      |
+------------------------------------------+
| Select PO: [PO-20251220-001 - ABC Bev] [v]|
| Delivery Note #: [DN-12345__________]     |
+------------------------------------------+
| PO Details:                               |
| Supplier: ABC Beverages Ltd               |
| Ordered: 2025-12-20  Expected: 2025-12-22 |
+------------------------------------------+
| RECEIVE ITEMS                             |
+------------------------------------------+
| Product       |Order|Prev |Recv|Cost|Total|
|---------------|-----|-----|----|----|-----|
| Tusker Lager  |  24 |   0 |[24]|300 |7,200|
| Coca Cola     |  48 |  24 |[24]| 50 |1,200|
| Fanta         |  24 |   0 |[ 0]| 50 |    0|
+------------------------------------------+
| Items to receive: 2   Total: KSh 8,400    |
+------------------------------------------+
| Notes:                                    |
| [Fanta out of stock at supplier_______]   |
+------------------------------------------+
| [Cancel]              [Receive & Update]  |
+------------------------------------------+
```

### GoodsReceivingViewModel

```csharp
public partial class GoodsReceivingViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _pendingPOs = new();

    [ObservableProperty]
    private PurchaseOrder? _selectedPO;

    [ObservableProperty]
    private string _deliveryNoteNumber = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GRNItemViewModel> _items = new();

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _notes = string.Empty;

    public async Task LoadAsync()
    {
        var pos = await _poRepo.GetPendingReceivingAsync();
        PendingPOs = new ObservableCollection<PurchaseOrder>(pos);
    }

    partial void OnSelectedPOChanged(PurchaseOrder? value)
    {
        if (value == null)
        {
            Items.Clear();
            return;
        }

        LoadPOItems(value);
    }

    private void LoadPOItems(PurchaseOrder po)
    {
        Items = new ObservableCollection<GRNItemViewModel>(
            po.Items.Select(item => new GRNItemViewModel
            {
                PurchaseOrderItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                OrderedQuantity = item.OrderedQuantity,
                PreviouslyReceived = item.ReceivedQuantity,
                RemainingQuantity = item.OrderedQuantity - item.ReceivedQuantity,
                UnitCost = item.UnitCost
            }));

        foreach (var item in Items)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GRNItemViewModel.TotalCost))
                {
                    RecalculateTotal();
                }
            };
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.TotalCost);
    }

    [RelayCommand]
    private async Task ReceiveGoodsAsync()
    {
        var itemsToReceive = Items.Where(i => i.ReceivedQuantity > 0).ToList();

        if (!itemsToReceive.Any())
        {
            await _dialogService.ShowMessageAsync("Error", "No items to receive");
            return;
        }

        // Create GRN
        var grn = new GoodsReceivedNote
        {
            GRNNumber = await _grnNumberGenerator.GenerateNextAsync(),
            PurchaseOrderId = SelectedPO!.Id,
            SupplierId = SelectedPO.SupplierId,
            DeliveryNote = DeliveryNoteNumber,
            TotalAmount = TotalAmount,
            Notes = Notes,
            ReceivedByUserId = _authService.CurrentUser.Id
        };

        foreach (var item in itemsToReceive)
        {
            grn.Items.Add(new GRNItem
            {
                PurchaseOrderItemId = item.PurchaseOrderItemId,
                ProductId = item.ProductId,
                OrderedQuantity = item.OrderedQuantity,
                ReceivedQuantity = item.ReceivedQuantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            });

            // Update stock
            await _inventoryService.ReceiveStockAsync(
                item.ProductId,
                item.ReceivedQuantity,
                item.UnitCost,
                grn.GRNNumber);

            // Update PO item received quantity
            var poItem = SelectedPO.Items.First(i => i.Id == item.PurchaseOrderItemId);
            poItem.ReceivedQuantity += item.ReceivedQuantity;
        }

        // Update PO status
        UpdatePOStatus(SelectedPO);

        await _grnRepo.AddAsync(grn);
        await _unitOfWork.SaveChangesAsync();

        // Print GRN
        await _printService.PrintGRNAsync(grn);

        await _dialogService.ShowMessageAsync("Success",
            $"Goods received. GRN: {grn.GRNNumber}");

        await LoadAsync();
    }

    private void UpdatePOStatus(PurchaseOrder po)
    {
        var totalOrdered = po.Items.Sum(i => i.OrderedQuantity);
        var totalReceived = po.Items.Sum(i => i.ReceivedQuantity);

        if (totalReceived >= totalOrdered)
        {
            po.Status = "Complete";
        }
        else if (totalReceived > 0)
        {
            po.Status = "PartiallyReceived";
        }

        po.UpdatedAt = DateTime.UtcNow;
    }
}

public class GRNItemViewModel : ObservableObject
{
    public int PurchaseOrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyReceived { get; set; }
    public decimal RemainingQuantity { get; set; }

    [ObservableProperty]
    private decimal _receivedQuantity;

    [ObservableProperty]
    private decimal _unitCost;

    public decimal TotalCost => ReceivedQuantity * UnitCost;

    partial void OnReceivedQuantityChanged(decimal value)
    {
        // Validate
        if (value > RemainingQuantity)
        {
            ReceivedQuantity = RemainingQuantity;
        }
        OnPropertyChanged(nameof(TotalCost));
    }
}
```

### Inventory Service - Receive Stock

```csharp
public async Task<StockMovement> ReceiveStockAsync(
    int productId,
    decimal quantity,
    decimal unitCost,
    string reference)
{
    var product = await _productRepo.GetByIdAsync(productId);
    if (product == null)
        throw new NotFoundException("Product not found");

    var previousStock = product.CurrentStock;
    product.CurrentStock += quantity;

    // Update cost price (weighted average or latest)
    product.CostPrice = unitCost;

    await _productRepo.UpdateAsync(product);

    var movement = new StockMovement
    {
        ProductId = productId,
        MovementType = StockMovementType.PurchaseReceive,
        Quantity = quantity,
        PreviousStock = previousStock,
        NewStock = product.CurrentStock,
        Reference = reference,
        UserId = _authService.CurrentUser.Id
    };

    await _movementRepo.AddAsync(movement);
    await _unitOfWork.SaveChangesAsync();

    _messenger.Send(new StockChangedMessage(productId));

    return movement;
}
```

### GRN Print (80mm)

```
================================================
     GOODS RECEIVED NOTE
     GRN-20251220-001
================================================
Date: 2025-12-20 14:30
Received By: John Smith

Supplier: ABC Beverages Ltd
PO #: PO-20251220-001
Delivery Note: DN-12345
------------------------------------------------

ITEMS RECEIVED:
------------------------------------------------
Product             | Qty Recv | Cost | Total
--------------------|----------|------|--------
Tusker Lager        |       24 |  300 |  7,200
Coca Cola 500ml     |       24 |   50 |  1,200
------------------------------------------------
TOTAL RECEIVED:              KSh    8,400
------------------------------------------------

Notes: Fanta out of stock at supplier

Received By: _______________
Signature: _________________

================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.6.3-Goods-Receiving]
- [Source: docs/PRD_Hospitality_POS_System.md#PS-020 to PS-025]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created GoodsReceivedNote entity with support for both PO-based and direct receiving
- Created GRNItem entity for line items
- Added GRN number generation: GRN-{yyyyMMdd}-{sequence}
- Implemented IGoodsReceivingService interface with methods for PO selection, goods receiving, and stock updates
- Implemented GoodsReceivingService with full receiving workflow
- Added ReceiveStockAsync method to IInventoryService with MovementType.PurchaseReceive
- Created GoodsReceivingView.xaml with PO selection, item grid, quantity entry, and receiving actions
- Created GoodsReceivingViewModel with MVVM pattern using CommunityToolkit.Mvvm
- Integrated with IInventoryService for automatic stock updates
- PO status automatically updates to PartiallyReceived or Complete based on received quantities
- Created comprehensive unit tests for GoodsReceivingService

### File List
- src/HospitalityPOS.Core/Entities/GoodsReceivedNote.cs
- src/HospitalityPOS.Core/Entities/GRNItem.cs
- src/HospitalityPOS.Core/Interfaces/IGoodsReceivingService.cs
- src/HospitalityPOS.Core/Interfaces/IInventoryService.cs (modified - added ReceiveStockAsync)
- src/HospitalityPOS.Core/Enums/SystemEnums.cs (modified - added PurchaseReceive)
- src/HospitalityPOS.Infrastructure/Data/Configurations/GoodsReceivingConfiguration.cs
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (modified)
- src/HospitalityPOS.Infrastructure/Services/GoodsReceivingService.cs
- src/HospitalityPOS.Infrastructure/Services/InventoryService.cs (modified)
- src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/GoodsReceivingViewModel.cs
- src/HospitalityPOS.WPF/Views/GoodsReceivingView.xaml
- src/HospitalityPOS.WPF/Views/GoodsReceivingView.xaml.cs
- src/HospitalityPOS.WPF/App.xaml.cs (modified)
- tests/HospitalityPOS.Business.Tests/Services/GoodsReceivingServiceTests.cs
