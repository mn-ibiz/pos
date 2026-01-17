# Story 5.3: Order Item Management

Status: done

## Story

As a waiter/cashier,
I want to modify order items before printing,
So that I can correct mistakes or accommodate changes.

## Acceptance Criteria

1. **Given** an order has items
   **When** managing order items
   **Then** user can increase/decrease quantity

2. **Given** an item needs to be removed
   **When** quantity reaches zero or remove is pressed
   **Then** user can remove items entirely

3. **Given** an item needs customization
   **When** modifiers are available
   **Then** user can add modifiers/notes to items (e.g., "no ice", "extra spicy")

4. **Given** items are modified
   **When** totals are displayed
   **Then** changes should reflect immediately in the order total

5. **Given** user has permission
   **When** discounts are needed
   **Then** user can apply item-level discounts (if permitted by role)

## Tasks / Subtasks

- [ ] Task 1: Implement Quantity Controls (AC: #1)
  - [ ] Add +/- buttons to each order item
  - [ ] Bind to increase/decrease commands
  - [ ] Update totals on change
  - [ ] Minimum quantity is 1 (or remove)

- [ ] Task 2: Implement Item Removal (AC: #2)
  - [ ] Swipe-to-remove gesture
  - [ ] Long-press context menu with Remove option
  - [ ] Confirmation for items already printed
  - [ ] Update totals on removal

- [ ] Task 3: Implement Modifiers Dialog (AC: #3)
  - [ ] Create ItemModifiersDialog.xaml
  - [ ] Show available modifiers for product
  - [ ] Allow free-text notes
  - [ ] Display modifiers on order item

- [ ] Task 4: Real-Time Total Updates (AC: #4)
  - [ ] Recalculate on any item change
  - [ ] Update subtotal, tax, total
  - [ ] Animate total changes

- [ ] Task 5: Implement Item Discounts (AC: #5)
  - [ ] Add discount option in context menu
  - [ ] Check user permission for discount level
  - [ ] Calculate discount (percentage or fixed)
  - [ ] Show discount on item line

## Dev Notes

### Order Item Context Menu

```
+----------------------------------+
|  Tusker Lager  x2   KSh 700.00   |
|----------------------------------|
|  > Add Modifiers                 |
|  > Add Note                      |
|  > Apply Discount                |
|  > Remove Item                   |
+----------------------------------+
```

### Modifiers Dialog

```
+------------------------------------------+
|  Modifiers: Tusker Lager                  |
+------------------------------------------+
|                                           |
|  Temperature:                             |
|  [x] Cold   [ ] Room Temp                 |
|                                           |
|  Extras:                                  |
|  [ ] Lemon slice  (+KSh 20)               |
|  [ ] Salt rim     (+KSh 10)               |
|                                           |
|  Notes:                                   |
|  [Serve with ice on side________]         |
|                                           |
|  [Apply]  [Cancel]                        |
+------------------------------------------+
```

### OrderItemViewModel

```csharp
public partial class OrderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _productId;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _discountPercent;

    [ObservableProperty]
    private string? _modifiers;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private decimal _taxRate = 16m;

    public decimal LineSubtotal => Quantity * UnitPrice;
    public decimal LineDiscount => DiscountAmount > 0 ? DiscountAmount : (LineSubtotal * DiscountPercent / 100);
    public decimal LineTaxable => LineSubtotal - LineDiscount;
    public decimal TaxAmount => LineTaxable * TaxRate / 100;
    public decimal LineTotal => LineTaxable + TaxAmount;

    public bool HasNotes => !string.IsNullOrEmpty(Notes);
    public bool HasModifiers => !string.IsNullOrEmpty(Modifiers);
    public bool HasDiscount => DiscountAmount > 0 || DiscountPercent > 0;

    [RelayCommand]
    private void IncreaseQuantity()
    {
        Quantity++;
        OnPropertyChanged(nameof(LineTotal));
    }

    [RelayCommand]
    private void DecreaseQuantity()
    {
        if (Quantity > 1)
        {
            Quantity--;
            OnPropertyChanged(nameof(LineTotal));
        }
    }
}
```

### Swipe-to-Remove Behavior

```csharp
public class SwipeToRemoveBehavior : Behavior<FrameworkElement>
{
    private double _startX;
    private const double SwipeThreshold = 100;

    protected override void OnAttached()
    {
        AssociatedObject.ManipulationStarted += OnManipulationStarted;
        AssociatedObject.ManipulationDelta += OnManipulationDelta;
        AssociatedObject.ManipulationCompleted += OnManipulationCompleted;
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
        var translateX = e.CumulativeManipulation.Translation.X;

        // Move element with finger
        var transform = AssociatedObject.RenderTransform as TranslateTransform
            ?? new TranslateTransform();
        transform.X = translateX;
        AssociatedObject.RenderTransform = transform;

        // Show delete background
        if (translateX < -50)
        {
            // Show red background
        }
    }

    private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
    {
        if (e.TotalManipulation.Translation.X < -SwipeThreshold)
        {
            // Trigger remove
            var item = AssociatedObject.DataContext as OrderItemViewModel;
            // Remove item
        }
        else
        {
            // Snap back
            AnimateBack();
        }
    }
}
```

### Discount Dialog

```
+------------------------------------------+
|  Apply Discount                           |
+------------------------------------------+
|                                           |
|  Item: Tusker Lager x2                    |
|  Current: KSh 700.00                      |
|                                           |
|  Discount Type:                           |
|  ( ) Percentage  (x) Fixed Amount         |
|                                           |
|  Amount: [___50___]                       |
|                                           |
|  New Total: KSh 650.00                    |
|                                           |
|  Reason: [Manager approved________]       |
|                                           |
|  [Apply Discount]  [Cancel]               |
+------------------------------------------+
```

### Permission Check for Discounts

```csharp
private bool CanApplyDiscount(decimal discountPercent)
{
    if (discountPercent <= 10)
        return _authService.HasPermissionAsync(Permission.Discounts_Apply10).Result;
    if (discountPercent <= 20)
        return _authService.HasPermissionAsync(Permission.Discounts_Apply20).Result;
    if (discountPercent <= 50)
        return _authService.HasPermissionAsync(Permission.Discounts_Apply50).Result;

    return _authService.HasPermissionAsync(Permission.Discounts_ApplyAny).Result;
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.2.1-Creating-Orders]
- [Source: docs/PRD_Hospitality_POS_System.md#SO-004 to SO-006]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- AC#1 (Quantity controls): Already implemented in Story 5-1 with +/- buttons
- AC#2 (Item removal): Already implemented in Story 5-1 (decrease to 0 removes, RemoveOrderItem command)
  - Added context menu with "Remove Item" option for explicit removal
- AC#3 (Item modifiers/notes): Added Notes and Modifiers properties to OrderItemViewModel
  - EditItemNotesAsyncCommand for adding notes via input dialog
  - EditItemModifiersAsyncCommand for adding modifiers via input dialog
  - Modifiers display in amber/orange, Notes display with 📝 emoji
- AC#4 (Real-time totals): Already implemented, updated to include discount in calculations
  - Added OrderDiscount property for total discount display
  - Added HasOrderDiscount for conditional visibility
- AC#5 (Item discounts): Added permission-based discount functionality
  - ApplyItemDiscountAsyncCommand with permission checks (10%, 20%, 50%, Any)
  - RemoveItemDiscountCommand to clear discounts
  - DiscountAmount and DiscountPercent properties on OrderItemViewModel
  - Discount displayed in red in both item and totals section

### Implementation Details
- Context menu on order items for Add Notes, Add Modifiers, Apply Discount, Remove Item
- LineSubtotal, LineDiscount, LineTotal computed properties for accurate calculations
- Auto-save includes all new item fields (notes, modifiers, discountAmount, discountPercent)

### Code Review Fixes Applied
- Fixed HIGH: Changed context menu bindings from Window to UserControl
- Fixed MEDIUM: Added [NotifyPropertyChangedFor] to _orderDiscount for HasOrderDiscount
- Fixed MEDIUM: Removed emoji from Notes display, using "Note: " prefix instead
- Fixed LOW: Added null coalescing for Notes/Modifiers during recovery

### File List
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - OrderItemViewModel enhanced, new commands added)
- src/HospitalityPOS.WPF/Views/POSView.xaml (modified - context menu, notes/modifiers/discount display)
