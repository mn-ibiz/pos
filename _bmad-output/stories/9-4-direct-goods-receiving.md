# Story 9.4: Direct Goods Receiving (without PO)

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/GoodsReceivingService.cs` - `ReceiveDirectAsync` for ad-hoc deliveries without PO
- Supports supplier selection, product entry, cost recording
- Automatic stock updates and GRN generation

## Story

As a stock clerk,
I want to receive goods without a purchase order,
So that ad-hoc deliveries can be recorded.

## Acceptance Criteria

1. **Given** products and suppliers exist
   **When** receiving goods directly
   **Then** user can select supplier (optional)

2. **Given** direct receiving
   **When** adding items
   **Then** user can add products with quantities and costs

3. **Given** items are entered
   **When** saving the receipt
   **Then** stock is automatically increased on save

4. **Given** direct receiving is complete
   **When** documenting the transaction
   **Then** GRN is generated for the direct receiving

## Tasks / Subtasks

- [ ] Task 1: Create Direct Receiving Screen
  - [ ] Create DirectReceivingView.xaml
  - [ ] Create DirectReceivingViewModel
  - [ ] Optional supplier selection
  - [ ] Product search and add

- [ ] Task 2: Implement Product Search
  - [ ] Quick search by name/code
  - [ ] Barcode scanning support
  - [ ] Show current stock info
  - [ ] Add to receiving list

- [ ] Task 3: Implement Item Entry
  - [ ] Enter quantity
  - [ ] Enter unit cost
  - [ ] Calculate total
  - [ ] Edit/remove items

- [ ] Task 4: Implement Stock Update
  - [ ] Update product stock
  - [ ] Create stock movements
  - [ ] No PO link required
  - [ ] Log as direct receive

- [ ] Task 5: Generate and Print GRN
  - [ ] Create GRN record
  - [ ] Mark as "Direct Receiving"
  - [ ] Print GRN document
  - [ ] Include in reports

## Dev Notes

### Direct Receiving Screen

```
+------------------------------------------+
|      DIRECT GOODS RECEIVING               |
+------------------------------------------+
| Supplier (Optional): [____________] [v]   |
| Delivery Note #: [__________________]     |
+------------------------------------------+
| Search Product: [___________] [+ Add]     |
+------------------------------------------+
| ITEMS TO RECEIVE                          |
+------------------------------------------+
| Product       | Qty  | Unit Cost | Total  |
|---------------|------|-----------|--------|
| Tusker Lager  | [12] |   [300]   | 3,600  |
| Coca Cola     | [24] |    [50]   | 1,200  |
|               |      |           |        |
|               |      |           |        |
+------------------------------------------+
| Total Items: 2        Total: KSh 4,800    |
+------------------------------------------+
| Notes:                                    |
| [Emergency stock replenishment________]   |
+------------------------------------------+
| [Cancel]              [Receive & Update]  |
+------------------------------------------+
```

### DirectReceivingViewModel

```csharp
public partial class DirectReceivingViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string _deliveryNoteNumber = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Product> _searchResults = new();

    [ObservableProperty]
    private ObservableCollection<DirectReceiveItemViewModel> _items = new();

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _notes = string.Empty;

    public async Task LoadAsync()
    {
        var suppliers = await _supplierRepo.GetActiveAsync();
        Suppliers = new ObservableCollection<Supplier>(suppliers);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        SearchProductsAsync(value);
    }

    private async void SearchProductsAsync(string search)
    {
        var products = await _productRepo.SearchAsync(search);
        SearchResults = new ObservableCollection<Product>(products.Take(10));
    }

    [RelayCommand]
    private void AddProduct(Product product)
    {
        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            var item = new DirectReceiveItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Code,
                CurrentStock = product.CurrentStock,
                Quantity = 1,
                UnitCost = product.CostPrice,
                Unit = product.StockUnit ?? "pcs"
            };

            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DirectReceiveItemViewModel.TotalCost))
                {
                    RecalculateTotal();
                }
            };

            Items.Add(item);
        }

        SearchText = string.Empty;
        RecalculateTotal();
    }

    [RelayCommand]
    private void RemoveItem(DirectReceiveItemViewModel item)
    {
        Items.Remove(item);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.TotalCost);
    }

    [RelayCommand]
    private async Task ReceiveGoodsAsync()
    {
        if (!Items.Any())
        {
            await _dialogService.ShowMessageAsync("Error", "No items to receive");
            return;
        }

        // Create GRN without PO
        var grn = new GoodsReceivedNote
        {
            GRNNumber = await _grnNumberGenerator.GenerateNextAsync(),
            PurchaseOrderId = null,  // No PO
            SupplierId = SelectedSupplier?.Id,
            DeliveryNote = DeliveryNoteNumber,
            TotalAmount = TotalAmount,
            Notes = string.IsNullOrEmpty(Notes) ? "Direct Receiving" : Notes,
            ReceivedByUserId = _authService.CurrentUser.Id
        };

        foreach (var item in Items)
        {
            grn.Items.Add(new GRNItem
            {
                PurchaseOrderItemId = null,  // No PO item
                ProductId = item.ProductId,
                OrderedQuantity = 0,  // Direct receiving has no ordered qty
                ReceivedQuantity = item.Quantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            });

            // Update stock
            await _inventoryService.ReceiveStockAsync(
                item.ProductId,
                item.Quantity,
                item.UnitCost,
                grn.GRNNumber);
        }

        await _grnRepo.AddAsync(grn);
        await _unitOfWork.SaveChangesAsync();

        // Print GRN
        await _printService.PrintGRNAsync(grn);

        await _dialogService.ShowMessageAsync("Success",
            $"Goods received. GRN: {grn.GRNNumber}");

        // Clear form
        ClearForm();
    }

    private void ClearForm()
    {
        SelectedSupplier = null;
        DeliveryNoteNumber = string.Empty;
        Items.Clear();
        Notes = string.Empty;
        TotalAmount = 0;
    }
}

public class DirectReceiveItemViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = "pcs";

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _unitCost;

    public decimal TotalCost => Quantity * UnitCost;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(TotalCost));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(TotalCost));
}
```

### Direct GRN Print (80mm)

```
================================================
     GOODS RECEIVED NOTE
     GRN-20251220-002
     ** DIRECT RECEIVING **
================================================
Date: 2025-12-20 16:30
Received By: John Smith

Supplier: ABC Beverages Ltd (Optional)
Delivery Note: (Not Specified)
------------------------------------------------

ITEMS RECEIVED:
------------------------------------------------
Product             | Qty Recv | Cost | Total
--------------------|----------|------|--------
Tusker Lager        |       12 |  300 |  3,600
Coca Cola 500ml     |       24 |   50 |  1,200
------------------------------------------------
TOTAL RECEIVED:              KSh    4,800
------------------------------------------------

Notes: Emergency stock replenishment

Received By: _______________
Signature: _________________

================================================
```

### Barcode Scanning Integration

```csharp
// In DirectReceivingViewModel
public void ProcessBarcode(string barcode)
{
    var product = _productRepo.GetByBarcodeAsync(barcode).Result;

    if (product != null)
    {
        AddProduct(product);
    }
    else
    {
        _dialogService.ShowMessageAsync("Not Found",
            $"No product found with barcode: {barcode}");
    }
}
```

### Product Search Panel

```xml
<StackPanel>
    <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
             Watermark="Search product by name or scan barcode..."/>

    <ListBox ItemsSource="{Binding SearchResults}"
             Visibility="{Binding SearchResults.Count,
                         Converter={StaticResource CountToVisibility}}">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <Border Padding="10" Cursor="Hand"
                        Background="{Binding Converter={StaticResource AlternateRowBackground}}">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <StackPanel>
                            <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                            <TextBlock Text="{Binding Code}" FontSize="11" Foreground="Gray"/>
                        </StackPanel>

                        <TextBlock Grid.Column="1"
                                   Text="{Binding CurrentStock, StringFormat='Stock: {0:N0}'}"
                                   VerticalAlignment="Center"/>
                    </Grid>

                    <i:Interaction.Triggers>
                        <i:EventTrigger EventName="MouseLeftButtonUp">
                            <i:InvokeCommandAction
                                Command="{Binding DataContext.AddProductCommand,
                                          RelativeSource={RelativeSource AncestorType=ListBox}}"
                                CommandParameter="{Binding}"/>
                        </i:EventTrigger>
                    </i:Interaction.Triggers>
                </Border>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</StackPanel>
```

### Use Cases for Direct Receiving

1. **Emergency stock replenishment** - Quick purchases from local shops
2. **Sample deliveries** - Supplier samples not on PO
3. **Owner purchases** - Owner buying stock directly
4. **Transfers between branches** - Stock moved from another location
5. **Returns from customers** - Items returned to stock

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.6.4-Direct-Receiving]
- [Source: docs/PRD_Hospitality_POS_System.md#PS-030]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Implemented direct receiving functionality using the same GoodsReceivingService
- Created DirectReceivingView.xaml with optional supplier selection, product search, and item entry
- Created DirectReceivingViewModel with product search, add/remove items, and receiving actions
- Direct receiving creates GRN with PurchaseOrderId = null (IsDirectReceiving property returns true)
- Product search supports name, code, and barcode lookup
- Quantity and unit cost editable for each item
- Default notes set to "Direct Receiving" when no notes provided
- Integrated with IInventoryService.ReceiveStockAsync for stock updates
- Unit tests cover direct receiving scenarios in GoodsReceivingServiceTests

### File List
- src/HospitalityPOS.WPF/ViewModels/DirectReceivingViewModel.cs
- src/HospitalityPOS.WPF/Views/DirectReceivingView.xaml
- src/HospitalityPOS.WPF/Views/DirectReceivingView.xaml.cs
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered DirectReceivingViewModel)
- tests/HospitalityPOS.Business.Tests/Services/GoodsReceivingServiceTests.cs (includes direct receiving tests)
