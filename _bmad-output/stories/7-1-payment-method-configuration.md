# Story 7.1: Payment Method Configuration

Status: done

## Story

As an administrator,
I want to configure available payment methods,
So that the business can accept various forms of payment.

## Acceptance Criteria

1. **Given** the admin is logged in
   **When** configuring payment methods
   **Then** admin can enable/disable payment methods: Cash, M-Pesa, Airtel Money, Credit Card, Debit Card

2. **Given** payment methods are configured
   **When** setting display preferences
   **Then** admin can set display order for payment method buttons

3. **Given** payment method settings
   **When** configuring requirements
   **Then** admin can configure if method requires reference number (e.g., M-Pesa code)

4. **Given** configuration changes are made
   **When** saving changes
   **Then** changes should take effect immediately on POS screen

## Tasks / Subtasks

- [ ] Task 1: Create PaymentMethod Entity
  - [ ] Create entity with required fields
  - [ ] Configure EF Core mappings
  - [ ] Create database migration
  - [ ] Seed default payment methods

- [ ] Task 2: Create Payment Methods Management Screen
  - [ ] Create PaymentMethodsView.xaml
  - [ ] Create PaymentMethodsViewModel
  - [ ] Show list of payment methods
  - [ ] Enable/disable toggle for each

- [ ] Task 3: Implement Drag-to-Reorder
  - [ ] Add drag handles to list items
  - [ ] Implement reorder logic
  - [ ] Update display order on drop
  - [ ] Save order to database

- [ ] Task 4: Implement Settings Per Method
  - [ ] Add settings dialog per method
  - [ ] Configure reference number requirement
  - [ ] Set validation rules
  - [ ] Configure icon/color

- [ ] Task 5: Implement Live Updates
  - [ ] Reload payment methods on POS screen
  - [ ] Subscribe to configuration changes
  - [ ] Update button visibility immediately

## Dev Notes

### PaymentMethod Entity

```csharp
public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;  // CASH, MPESA, CARD, etc.
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresReference { get; set; } = false;
    public string? ReferenceLabel { get; set; }  // "M-Pesa Code", "Card Last 4"
    public int? ReferenceMinLength { get; set; }
    public int? ReferenceMaxLength { get; set; }
    public bool SupportsChange { get; set; } = false;  // Cash only
    public bool OpensDrawer { get; set; } = false;     // Cash only
    public int DisplayOrder { get; set; }
    public string? IconPath { get; set; }
    public string? BackgroundColor { get; set; }  // Hex color

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
```

### Seed Data

```csharp
var paymentMethods = new List<PaymentMethod>
{
    new()
    {
        Name = "Cash",
        Code = "CASH",
        IsActive = true,
        RequiresReference = false,
        SupportsChange = true,
        OpensDrawer = true,
        DisplayOrder = 1,
        BackgroundColor = "#4CAF50"  // Green
    },
    new()
    {
        Name = "M-Pesa",
        Code = "MPESA",
        IsActive = true,
        RequiresReference = true,
        ReferenceLabel = "M-Pesa Code",
        ReferenceMinLength = 10,
        ReferenceMaxLength = 10,
        DisplayOrder = 2,
        BackgroundColor = "#00C853"  // M-Pesa Green
    },
    new()
    {
        Name = "Airtel Money",
        Code = "AIRTEL",
        IsActive = true,
        RequiresReference = true,
        ReferenceLabel = "Airtel Code",
        ReferenceMinLength = 10,
        ReferenceMaxLength = 10,
        DisplayOrder = 3,
        BackgroundColor = "#FF5722"  // Airtel Red
    },
    new()
    {
        Name = "Credit Card",
        Code = "CREDIT_CARD",
        IsActive = true,
        RequiresReference = false,
        ReferenceLabel = "Last 4 Digits (Optional)",
        ReferenceMinLength = 4,
        ReferenceMaxLength = 4,
        DisplayOrder = 4,
        BackgroundColor = "#2196F3"  // Blue
    },
    new()
    {
        Name = "Debit Card",
        Code = "DEBIT_CARD",
        IsActive = true,
        RequiresReference = false,
        ReferenceLabel = "Last 4 Digits (Optional)",
        ReferenceMinLength = 4,
        ReferenceMaxLength = 4,
        DisplayOrder = 5,
        BackgroundColor = "#9C27B0"  // Purple
    },
    new()
    {
        Name = "Bank Transfer",
        Code = "BANK_TRANSFER",
        IsActive = false,
        RequiresReference = true,
        ReferenceLabel = "Reference Number",
        DisplayOrder = 6,
        BackgroundColor = "#607D8B"  // Gray
    }
};
```

### Payment Methods Management Screen

```
+------------------------------------------+
|     PAYMENT METHODS CONFIGURATION         |
+------------------------------------------+
|                                           |
|  Drag to reorder, toggle to enable/disable|
|                                           |
|  +------------------------------------+   |
|  | [=] 1. Cash            [ON]   [>]  |   |
|  +------------------------------------+   |
|  | [=] 2. M-Pesa          [ON]   [>]  |   |
|  +------------------------------------+   |
|  | [=] 3. Airtel Money    [ON]   [>]  |   |
|  +------------------------------------+   |
|  | [=] 4. Credit Card     [ON]   [>]  |   |
|  +------------------------------------+   |
|  | [=] 5. Debit Card      [ON]   [>]  |   |
|  +------------------------------------+   |
|  | [=] 6. Bank Transfer   [OFF]  [>]  |   |
|  +------------------------------------+   |
|                                           |
|  [+ Add Payment Method]                   |
|                                           |
+------------------------------------------+
```

### Payment Method Settings Dialog

```
+------------------------------------------+
|     M-PESA SETTINGS                       |
+------------------------------------------+
|                                           |
|  Name: [M-Pesa_________________]          |
|                                           |
|  Code: [MPESA__________________]          |
|                                           |
|  [x] Active                               |
|  [x] Requires Reference Number            |
|                                           |
|  Reference Label:                         |
|  [M-Pesa Code__________________]          |
|                                           |
|  Reference Length:                        |
|  Min: [10]    Max: [10]                   |
|                                           |
|  Appearance:                              |
|  Background: [#00C853] [Pick Color]       |
|  Icon: [mpesa.png] [Browse]               |
|                                           |
|  [ ] Opens Cash Drawer                    |
|  [ ] Supports Change Calculation          |
|                                           |
|  [Cancel]              [Save]             |
+------------------------------------------+
```

### PaymentMethodsViewModel

```csharp
public partial class PaymentMethodsViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _paymentMethods = new();

    public async Task LoadAsync()
    {
        var methods = await _paymentMethodRepo.GetAllOrderedAsync();
        PaymentMethods = new ObservableCollection<PaymentMethod>(methods);
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(PaymentMethod method)
    {
        method.IsActive = !method.IsActive;
        await _paymentMethodRepo.UpdateAsync(method);
        await _unitOfWork.SaveChangesAsync();

        // Notify POS screen
        _messenger.Send(new PaymentMethodsChangedMessage());
    }

    [RelayCommand]
    private async Task ReorderAsync((PaymentMethod item, int newIndex) args)
    {
        var oldIndex = PaymentMethods.IndexOf(args.item);
        if (oldIndex == args.newIndex) return;

        PaymentMethods.Move(oldIndex, args.newIndex);

        // Update display order for all items
        for (int i = 0; i < PaymentMethods.Count; i++)
        {
            PaymentMethods[i].DisplayOrder = i + 1;
        }

        await _paymentMethodRepo.UpdateRangeAsync(PaymentMethods);
        await _unitOfWork.SaveChangesAsync();

        _messenger.Send(new PaymentMethodsChangedMessage());
    }

    [RelayCommand]
    private async Task EditMethodAsync(PaymentMethod method)
    {
        var dialog = new PaymentMethodSettingsDialog(method);
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            await _paymentMethodRepo.UpdateAsync(method);
            await _unitOfWork.SaveChangesAsync();
            _messenger.Send(new PaymentMethodsChangedMessage());
        }
    }

    [RelayCommand]
    private async Task AddMethodAsync()
    {
        var newMethod = new PaymentMethod
        {
            DisplayOrder = PaymentMethods.Count + 1
        };

        var dialog = new PaymentMethodSettingsDialog(newMethod);
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            await _paymentMethodRepo.AddAsync(newMethod);
            await _unitOfWork.SaveChangesAsync();
            PaymentMethods.Add(newMethod);
            _messenger.Send(new PaymentMethodsChangedMessage());
        }
    }
}
```

### Live Update in POS Screen

```csharp
public partial class POSViewModel : BaseViewModel
{
    public POSViewModel(IMessenger messenger)
    {
        messenger.Register<PaymentMethodsChangedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadPaymentMethodsAsync();
            });
        });
    }

    private async Task LoadPaymentMethodsAsync()
    {
        var activeMethods = await _paymentMethodRepo.GetActiveOrderedAsync();
        PaymentMethods = new ObservableCollection<PaymentMethod>(activeMethods);
    }
}
```

### Payment Button XAML

```xml
<ItemsControl ItemsSource="{Binding PaymentMethods}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Button Width="120" Height="60"
                    Margin="5"
                    Command="{Binding DataContext.SelectPaymentMethodCommand,
                              RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                    CommandParameter="{Binding}">
                <Button.Style>
                    <Style TargetType="Button">
                        <Setter Property="Background"
                                Value="{Binding BackgroundColor,
                                        Converter={StaticResource HexToBrush}}"/>
                    </Style>
                </Button.Style>
                <StackPanel>
                    <Image Source="{Binding IconPath}"
                           Width="24" Height="24"/>
                    <TextBlock Text="{Binding Name}"
                               FontWeight="Bold"
                               Foreground="White"/>
                </StackPanel>
            </Button>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.4-Payment-Processing]
- [Source: docs/PRD_Hospitality_POS_System.md#PM-001 to PM-005]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
