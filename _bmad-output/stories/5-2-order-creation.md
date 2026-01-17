# Story 5.2: Order Creation

Status: done

## Story

As a waiter/cashier,
I want to create orders with products, quantities, and table assignment,
So that customer orders are properly recorded.

## Acceptance Criteria

1. **Given** the user is on the POS screen
   **When** creating an order
   **Then** user can add products by tapping on product tiles

2. **Given** products are added
   **When** adjusting quantities
   **Then** quantity can be adjusted with +/- buttons

3. **Given** an order is being created
   **When** assigning location
   **Then** order can be associated with a table number

4. **Given** an order is being created
   **When** customer info is needed
   **Then** optional customer name can be added

5. **Given** products are added to order
   **When** viewing order total
   **Then** running total (subtotal, tax, total) should update in real-time

6. **Given** special instructions are needed
   **When** adding notes
   **Then** order notes/special instructions can be added

7. **Given** an order is being created
   **When** connection is interrupted
   **Then** order should be auto-saved to prevent data loss

## Tasks / Subtasks

- [ ] Task 1: Create Order Panel Component (AC: #1, #2, #5)
  - [ ] Create OrderPanelControl.xaml
  - [ ] Display order items list
  - [ ] Show subtotal, tax, total
  - [ ] Add quantity +/- buttons per item

- [ ] Task 2: Implement Add Product to Order (AC: #1)
  - [ ] Create AddProductToOrderCommand
  - [ ] Handle duplicate products (increase qty)
  - [ ] Calculate item totals
  - [ ] Trigger total recalculation

- [ ] Task 3: Implement Table/Customer Assignment (AC: #3, #4)
  - [ ] Add table number input
  - [ ] Add optional customer name input
  - [ ] Quick-select table buttons
  - [ ] Show assigned table in header

- [ ] Task 4: Implement Order Notes (AC: #6)
  - [ ] Add order notes text field
  - [ ] Limit character count
  - [ ] Display notes in order summary

- [ ] Task 5: Implement Auto-Save (AC: #7)
  - [ ] Save order to local storage on each change
  - [ ] Recover unsaved orders on startup
  - [ ] Clear saved order after submission

## Dev Notes

### Order Panel Layout

```
+----------------------------------+
|  Table: [5]  Customer: [______]  |
+----------------------------------+
|  ORDER ITEMS                     |
|----------------------------------|
|  Tusker Lager                    |
|  [-] 2 [+]     KSh 700.00        |
|----------------------------------|
|  Grilled Chicken                 |
|  [-] 1 [+]     KSh 850.00        |
|----------------------------------|
|  Chips                           |
|  [-] 2 [+]     KSh 400.00        |
|----------------------------------|
|                                  |
|  Notes: [Extra spicy please___]  |
|                                  |
+----------------------------------+
|  Subtotal:        KSh 1,950.00   |
|  Tax (16%):       KSh   312.00   |
|  ─────────────────────────────   |
|  TOTAL:           KSh 2,262.00   |
+----------------------------------+
|  [HOLD]  [CLEAR]  [PRINT ORDER]  |
+----------------------------------+
```

### Order Entity

```csharp
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int WorkPeriodId { get; set; }
    public int UserId { get; set; }
    public string? TableNumber { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Open";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public WorkPeriod WorkPeriod { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

### OrderItem Entity

```csharp
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Modifiers { get; set; }
    public string? Notes { get; set; }
    public int BatchNumber { get; set; } = 1;
    public bool PrintedToKitchen { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

### ViewModel Implementation

```csharp
public partial class POSViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<OrderItemViewModel> _orderItems = new();

    [ObservableProperty]
    private string? _tableNumber;

    [ObservableProperty]
    private string? _customerName;

    [ObservableProperty]
    private string? _orderNotes;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _totalAmount;

    [RelayCommand]
    private void AddToOrder(Product product)
    {
        var existingItem = OrderItems.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            OrderItems.Add(new OrderItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.SellingPrice,
                Quantity = 1,
                TaxRate = product.TaxRate
            });
        }

        RecalculateTotals();
        AutoSaveOrder();
    }

    private void RecalculateTotals()
    {
        Subtotal = OrderItems.Sum(i => i.Quantity * i.UnitPrice);
        TaxAmount = OrderItems.Sum(i => i.TaxAmount);
        TotalAmount = Subtotal + TaxAmount;
    }
}
```

### Order Item Control

```xml
<Grid Height="60">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>

    <!-- Product Name and Price -->
    <StackPanel VerticalAlignment="Center">
        <TextBlock Text="{Binding ProductName}"
                   FontWeight="SemiBold"/>
        <TextBlock Text="{Binding Notes}"
                   FontSize="11"
                   Foreground="Gray"
                   Visibility="{Binding HasNotes, Converter={StaticResource BoolToVisibility}}"/>
    </StackPanel>

    <!-- Quantity and Total -->
    <StackPanel Grid.Column="1"
                Orientation="Horizontal"
                VerticalAlignment="Center">
        <Button Content="-"
                Command="{Binding DecreaseQuantityCommand}"
                Width="40" Height="40"
                Style="{StaticResource QuantityButton}"/>
        <TextBlock Text="{Binding Quantity}"
                   Width="30"
                   TextAlignment="Center"
                   VerticalAlignment="Center"
                   FontWeight="Bold"/>
        <Button Content="+"
                Command="{Binding IncreaseQuantityCommand}"
                Width="40" Height="40"
                Style="{StaticResource QuantityButton}"/>
        <TextBlock Text="{Binding LineTotal, StringFormat='KSh {0:N0}'}"
                   Width="100"
                   TextAlignment="Right"
                   VerticalAlignment="Center"
                   Margin="16,0,0,0"/>
    </StackPanel>
</Grid>
```

### Auto-Save Implementation

```csharp
private void AutoSaveOrder()
{
    var orderData = new LocalOrderData
    {
        TableNumber = TableNumber,
        CustomerName = CustomerName,
        Notes = OrderNotes,
        Items = OrderItems.Select(i => new LocalOrderItemData
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Notes = i.Notes
        }).ToList(),
        SavedAt = DateTime.Now
    };

    var json = JsonSerializer.Serialize(orderData);
    File.WriteAllText("Data/current_order.json", json);
}
```

### Order Number Generation
Format: `O-{yyyyMMdd}-{sequence}`
Example: `O-20251220-0042`

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.2.1-Creating-Orders]
- [Source: docs/PRD_Hospitality_POS_System.md#SO-001 to SO-010]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- AC#1, AC#2, AC#5 were implemented as part of Story 5-1 (Touch-optimized Product Grid)
- AC#3 (Table assignment): Added TableName input in Order Header section of POSView.xaml
- AC#4 (Customer name): Added CustomerName input in Order Header section of POSView.xaml
- AC#6 (Order notes): Added OrderNotes multiline input with 200 char limit in Order Header
- AC#7 (Auto-save): Implemented AutoSaveOrder(), RecoverAutoSavedOrderAsync(), ClearAutoSave()
  - Saves to LocalApplicationData/HospitalityPOS/Data/current_order.json on each order change
  - Recovers on POS screen startup with user confirmation
  - Clears stale orders older than 4 hours (configurable via AutoSaveStalenessHours constant)
  - ClearOrderAsync resets all order fields (auto-save cleared via RecalculateOrderTotals flow)

### Code Review Fixes Applied
- Fixed HIGH: Added partial methods OnTableNameChanged, OnCustomerNameChanged, OnOrderNotesChanged to trigger auto-save
- Fixed MEDIUM: Changed AutoSaveFolder to use LocalApplicationData instead of relative path
- Fixed MEDIUM: Extracted magic number 4 to AutoSaveStalenessHours constant
- Fixed MEDIUM: Removed redundant ClearAutoSave() call in ClearOrderAsync

### File List
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - added CustomerName, OrderNotes, auto-save, property change handlers)
- src/HospitalityPOS.WPF/Views/POSView.xaml (modified - added table, customer, notes inputs)
