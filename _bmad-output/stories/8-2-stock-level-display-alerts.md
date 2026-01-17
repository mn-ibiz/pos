# Story 8.2: Stock Level Display and Alerts

Status: done

## Story

As a manager,
I want to see current stock levels and receive low-stock alerts,
So that I can reorder products before they run out.

## Acceptance Criteria

1. **Given** products have stock levels configured
   **When** stock falls below minimum level
   **Then** low-stock alert should appear on dashboard

2. **Given** stock is depleted
   **When** products reach zero
   **Then** out-of-stock products should be prominently flagged

3. **Given** inventory view
   **When** viewing stock levels
   **Then** inventory view should show: product, current stock, min level, status

4. **Given** stock status display
   **When** viewing products
   **Then** color coding should indicate: green (OK), yellow (low), red (out)

## Tasks / Subtasks

- [x] Task 1: Create Inventory Dashboard Widget
  - [x] Create StockAlertWidget.xaml
  - [x] Create StockAlertWidgetViewModel
  - [x] Show low stock count
  - [x] Show out-of-stock count

- [x] Task 2: Create Stock List View
  - [x] Create InventoryView.xaml
  - [x] Create InventoryViewModel
  - [x] Display product stock grid
  - [x] Add filter/search

- [x] Task 3: Implement Color Coding
  - [x] Green for adequate stock
  - [x] Yellow for low stock
  - [x] Red for out-of-stock
  - [x] Apply to list and tiles

- [x] Task 4: Create enums/helpers for stock status
  - [x] StockStatus enum (already existed, enhanced)
  - [x] Value converters for color coding

- [ ] Task 5: Implement Real-Time Updates (Future Enhancement)
  - [ ] Subscribe to stock changes
  - [ ] Update counts immediately
  - [ ] Flash new alerts
  - [ ] Update product tiles on POS

- [ ] Task 6: Create Stock Alert Notifications (Future Enhancement)
  - [ ] Show toast on low stock
  - [ ] Show toast on stock out
  - [ ] Allow dismissing alerts
  - [ ] Log alert history

## Dev Notes

### Stock Dashboard Widget

```
+------------------------------------------+
|  STOCK ALERTS                    [View]   |
+------------------------------------------+
|                                           |
|  [!] LOW STOCK                            |
|  +------------------------------------+   |
|  | Tusker Lager         5 remaining   |   |
|  | Coca Cola            3 remaining   |   |
|  | Chips (Regular)      8 remaining   |   |
|  +------------------------------------+   |
|                                           |
|  [X] OUT OF STOCK                         |
|  +------------------------------------+   |
|  | Grilled Chicken                    |   |
|  | Fresh Orange Juice                 |   |
|  +------------------------------------+   |
|                                           |
+------------------------------------------+
```

### Inventory List View

```
+------------------------------------------+
|  INVENTORY MANAGEMENT                     |
+------------------------------------------+
| Search: [______________]  [All Categories]|
| Filter: [x]Low [x]Out [ ]OK               |
+------------------------------------------+
| Product         | Stock | Min | Status    |
|-----------------|-------|-----|-----------|
| [!] Tusker      |   5   | 10  | [LOW]     |
| [X] Grilled Ch  |   0   |  5  | [OUT]     |
| [OK] Chips Reg  |  45   | 10  | [OK]      |
| [!] Coca Cola   |   3   |  5  | [LOW]     |
| [OK] Fanta      |  28   | 10  | [OK]      |
+------------------------------------------+
| Summary: 5 Low Stock, 1 Out of Stock      |
+------------------------------------------+
```

### InventoryViewModel

```csharp
public partial class InventoryViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<ProductStockViewModel> _products = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showLowStock = true;

    [ObservableProperty]
    private bool _showOutOfStock = true;

    [ObservableProperty]
    private bool _showOkStock = true;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _outOfStockCount;

    [ObservableProperty]
    private Category? _selectedCategory;

    public InventoryViewModel(IMessenger messenger)
    {
        messenger.Register<StockChangedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.Invoke(() => RefreshProductAsync(m.ProductId));
        });
    }

    public async Task LoadAsync()
    {
        var products = await _productRepo.GetAllWithStockAsync();

        Products = new ObservableCollection<ProductStockViewModel>(
            products.Select(p => new ProductStockViewModel
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category?.Name ?? "Uncategorized",
                CurrentStock = p.CurrentStock,
                MinStockLevel = p.MinStockLevel,
                MaxStockLevel = p.MaxStockLevel,
                StockUnit = p.StockUnit ?? "pcs",
                StockStatus = GetStockStatus(p)
            }));

        UpdateCounts();
        ApplyFilters();
    }

    private StockStatus GetStockStatus(Product product)
    {
        if (!product.TrackInventory)
            return StockStatus.NotTracked;
        if (product.CurrentStock <= 0)
            return StockStatus.OutOfStock;
        if (product.CurrentStock <= product.MinStockLevel)
            return StockStatus.Low;
        return StockStatus.Ok;
    }

    private void UpdateCounts()
    {
        LowStockCount = Products.Count(p => p.StockStatus == StockStatus.Low);
        OutOfStockCount = Products.Count(p => p.StockStatus == StockStatus.OutOfStock);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowLowStockChanged(bool value) => ApplyFilters();
    partial void OnShowOutOfStockChanged(bool value) => ApplyFilters();
    partial void OnShowOkStockChanged(bool value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = Products.Where(p =>
        {
            // Text search
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (!p.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                    !p.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Category filter
            if (SelectedCategory != null && p.CategoryId != SelectedCategory.Id)
                return false;

            // Status filter
            return p.StockStatus switch
            {
                StockStatus.Low => ShowLowStock,
                StockStatus.OutOfStock => ShowOutOfStock,
                StockStatus.Ok => ShowOkStock,
                _ => true
            };
        });

        FilteredProducts = new ObservableCollection<ProductStockViewModel>(filtered);
    }
}

public class ProductStockViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public string StockUnit { get; set; } = "pcs";
    public StockStatus StockStatus { get; set; }

    public string StockDisplay => $"{CurrentStock:N0} {StockUnit}";
    public string MinDisplay => $"{MinStockLevel:N0}";

    public Brush StatusBackground => StockStatus switch
    {
        StockStatus.Ok => Brushes.LightGreen,
        StockStatus.Low => Brushes.Yellow,
        StockStatus.OutOfStock => Brushes.LightCoral,
        _ => Brushes.LightGray
    };

    public string StatusText => StockStatus switch
    {
        StockStatus.Ok => "OK",
        StockStatus.Low => "LOW",
        StockStatus.OutOfStock => "OUT",
        _ => "-"
    };
}

public enum StockStatus
{
    NotTracked,
    Ok,
    Low,
    OutOfStock
}
```

### Stock Alert Widget ViewModel

```csharp
public partial class StockAlertWidgetViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<ProductStockAlert> _lowStockItems = new();

    [ObservableProperty]
    private ObservableCollection<ProductStockAlert> _outOfStockItems = new();

    [ObservableProperty]
    private int _totalAlerts;

    public StockAlertWidgetViewModel(IMessenger messenger)
    {
        messenger.Register<LowStockMessage>(this, (r, m) => AddLowStockAlert(m));
        messenger.Register<StockOutMessage>(this, (r, m) => AddOutOfStockAlert(m));
    }

    public async Task LoadAsync()
    {
        var lowStock = await _productRepo.GetLowStockProductsAsync();
        LowStockItems = new ObservableCollection<ProductStockAlert>(
            lowStock.Select(p => new ProductStockAlert
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                MinStock = p.MinStockLevel
            }));

        var outOfStock = await _productRepo.GetOutOfStockProductsAsync();
        OutOfStockItems = new ObservableCollection<ProductStockAlert>(
            outOfStock.Select(p => new ProductStockAlert
            {
                ProductId = p.Id,
                ProductName = p.Name
            }));

        TotalAlerts = LowStockItems.Count + OutOfStockItems.Count;
    }

    private void AddLowStockAlert(LowStockMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = LowStockItems.FirstOrDefault(l => l.ProductId == message.ProductId);
            if (existing != null)
            {
                existing.CurrentStock = message.CurrentStock;
            }
            else
            {
                LowStockItems.Add(new ProductStockAlert
                {
                    ProductId = message.ProductId,
                    ProductName = message.ProductName,
                    CurrentStock = message.CurrentStock
                });
            }

            TotalAlerts = LowStockItems.Count + OutOfStockItems.Count;
        });
    }
}
```

### Product Tile Stock Indicator

```xml
<Border>
    <Grid>
        <!-- Product image and name -->

        <!-- Stock indicator badge -->
        <Border CornerRadius="3" Padding="3"
                HorizontalAlignment="Right"
                VerticalAlignment="Top"
                Margin="5">
            <Border.Style>
                <Style TargetType="Border">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding StockStatus}" Value="Ok">
                            <Setter Property="Background" Value="#4CAF50"/>
                            <Setter Property="Visibility" Value="Collapsed"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding StockStatus}" Value="Low">
                            <Setter Property="Background" Value="#FFC107"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding StockStatus}" Value="OutOfStock">
                            <Setter Property="Background" Value="#F44336"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
            <TextBlock Text="{Binding CurrentStock, StringFormat='{0:N0}'}"
                       Foreground="White" FontSize="10" FontWeight="Bold"/>
        </Border>

        <!-- Out of stock overlay -->
        <Border Background="#80000000"
                Visibility="{Binding IsOutOfStock, Converter={StaticResource BoolToVisibility}}">
            <TextBlock Text="OUT OF STOCK"
                       Foreground="White"
                       FontWeight="Bold"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"/>
        </Border>
    </Grid>
</Border>
```

### Low Stock Report (80mm)

```
================================================
     LOW STOCK REPORT
     2025-12-20 16:30
================================================
Product              | Stock | Min  | Status
---------------------|-------|------|--------
Tusker Lager         |   5   |  10  | LOW
Coca Cola            |   3   |   5  | LOW
Chips (Regular)      |   8   |  10  | LOW
Grilled Chicken      |   0   |   5  | OUT
Fresh Orange Juice   |   0   |  10  | OUT
================================================
Total Low Stock: 3 items
Total Out of Stock: 2 items
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.5.2-Stock-Alerts]
- [Source: docs/PRD_Hospitality_POS_System.md#IM-010 to IM-015]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Implemented StockAlertWidget with dashboard-style display of low stock and out-of-stock items
- Implemented InventoryView with full-featured data grid including search, filtering, and sorting
- Enhanced existing StockStatusConverters with additional converters for enum-based status and visibility
- Added color-coded status display: Green (#22C55E) for OK, Yellow/Orange (#F59E0B) for Low, Red (#EF4444) for Out
- Registered new ViewModels in DI container (App.xaml.cs)
- Added navigation command in MainViewModel for Inventory access
- Added DataTemplate mapping in MainWindow.xaml for InventoryViewModel
- Widget can be embedded in a dashboard view for quick stock alerts overview
- Real-time updates and toast notifications marked as future enhancements

### File List
- `src/HospitalityPOS.WPF/ViewModels/StockAlertWidgetViewModel.cs` (new)
- `src/HospitalityPOS.WPF/ViewModels/InventoryViewModel.cs` (new)
- `src/HospitalityPOS.WPF/Views/Inventory/StockAlertWidget.xaml` (new)
- `src/HospitalityPOS.WPF/Views/Inventory/StockAlertWidget.xaml.cs` (new)
- `src/HospitalityPOS.WPF/Views/Inventory/InventoryView.xaml` (new)
- `src/HospitalityPOS.WPF/Views/Inventory/InventoryView.xaml.cs` (new)
- `src/HospitalityPOS.WPF/Converters/StockStatusConverters.cs` (enhanced)
- `src/HospitalityPOS.WPF/Resources/Converters.xaml` (updated)
- `src/HospitalityPOS.WPF/App.xaml.cs` (updated)
- `src/HospitalityPOS.WPF/Views/MainWindow.xaml` (updated)
- `src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs` (updated)
