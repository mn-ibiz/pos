# Story 7.5: Split Payment Processing

Status: done

## Story

As a cashier,
I want to accept multiple payment methods for one receipt,
So that customers can pay with a combination of methods.

## Acceptance Criteria

1. **Given** a receipt is being settled
   **When** split payment is needed
   **Then** cashier can add multiple payment lines

2. **Given** payment lines are added
   **When** recording payments
   **Then** each line has: payment method, amount, reference (if required)

3. **Given** payments are being entered
   **When** tracking balance
   **Then** running balance should show remaining amount due

4. **Given** payment entry continues
   **When** checking completion
   **Then** settlement cannot complete until full amount is covered

5. **Given** settlement is complete
   **When** printing receipt
   **Then** all payment methods should appear on printed receipt

## Tasks / Subtasks

- [ ] Task 1: Create Split Payment Screen
  - [ ] Create SplitPaymentView.xaml
  - [ ] Create SplitPaymentViewModel
  - [ ] Show receipt total
  - [ ] Show payment lines list

- [ ] Task 2: Implement Add Payment Line
  - [ ] Select payment method
  - [ ] Enter amount (with remaining balance default)
  - [ ] Enter reference if required
  - [ ] Add to payment lines

- [ ] Task 3: Implement Balance Tracking
  - [ ] Calculate total paid
  - [ ] Calculate remaining balance
  - [ ] Update on each payment add/remove
  - [ ] Show clear balance indicator

- [ ] Task 4: Implement Payment Removal
  - [ ] Allow removing payment lines
  - [ ] Recalculate balance
  - [ ] Confirm before removing

- [ ] Task 5: Implement Settlement Completion
  - [ ] Validate full amount covered
  - [ ] Create all payment records
  - [ ] Handle overpayment as cash change
  - [ ] Print multi-payment receipt

## Dev Notes

### Split Payment Screen Layout

```
+------------------------------------------+
|          SPLIT PAYMENT                    |
|          R-20251220-0042                  |
+------------------------------------------+
|                                           |
|  Total Due:               KSh 2,262.00    |
|                                           |
|  Payment Lines:                           |
|  +------------------------------------+   |
|  | Cash            KSh 1,000    [X]   |   |
|  +------------------------------------+   |
|  | M-Pesa          KSh   500    [X]   |   |
|  | Ref: QJK2ABC123                    |   |
|  +------------------------------------+   |
|                                           |
|  Total Paid:              KSh 1,500.00    |
|  ─────────────────────────────────────    |
|  REMAINING:               KSh   762.00    |
|                                           |
|  Add Payment:                             |
|  [CASH]  [M-PESA]  [CARD]                 |
|                                           |
|  [Cancel]              [Complete Sale]    |
+------------------------------------------+
```

### SplitPaymentViewModel

```csharp
public partial class SplitPaymentViewModel : BaseViewModel
{
    [ObservableProperty]
    private Receipt _receipt = null!;

    [ObservableProperty]
    private decimal _totalDue;

    [ObservableProperty]
    private decimal _totalPaid;

    [ObservableProperty]
    private decimal _remainingBalance;

    [ObservableProperty]
    private bool _isFullyPaid;

    [ObservableProperty]
    private decimal _changeAmount;

    [ObservableProperty]
    private ObservableCollection<PaymentLine> _paymentLines = new();

    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _availableMethods = new();

    public void Initialize(Receipt receipt)
    {
        Receipt = receipt;
        TotalDue = receipt.TotalAmount;
        RemainingBalance = TotalDue;

        LoadPaymentMethods();
    }

    private async void LoadPaymentMethods()
    {
        var methods = await _paymentMethodRepo.GetActiveOrderedAsync();
        AvailableMethods = new ObservableCollection<PaymentMethod>(methods);
    }

    [RelayCommand]
    private async Task AddPaymentAsync(PaymentMethod method)
    {
        var paymentAmount = RemainingBalance;

        if (method.Code == "CASH")
        {
            var cashDialog = new CashPaymentDialog(paymentAmount);
            var result = await _dialogService.ShowDialogAsync(cashDialog);

            if (result is Payment cashPayment)
            {
                AddPaymentLine(new PaymentLine
                {
                    PaymentMethodId = method.Id,
                    PaymentMethodName = method.Name,
                    PaymentMethodCode = method.Code,
                    Amount = cashPayment.Amount,
                    TenderedAmount = cashPayment.TenderedAmount,
                    ChangeAmount = cashPayment.ChangeAmount
                });
            }
        }
        else if (method.RequiresReference)
        {
            var refDialog = new ReferencePaymentDialog(method, paymentAmount);
            var result = await _dialogService.ShowDialogAsync(refDialog);

            if (result is Payment refPayment)
            {
                AddPaymentLine(new PaymentLine
                {
                    PaymentMethodId = method.Id,
                    PaymentMethodName = method.Name,
                    PaymentMethodCode = method.Code,
                    Amount = refPayment.Amount,
                    ReferenceNumber = refPayment.ReferenceNumber
                });
            }
        }
        else
        {
            // Simple amount entry
            var amountDialog = new AmountEntryDialog(paymentAmount);
            var amount = await _dialogService.ShowDialogAsync(amountDialog);

            if (amount is decimal enteredAmount && enteredAmount > 0)
            {
                AddPaymentLine(new PaymentLine
                {
                    PaymentMethodId = method.Id,
                    PaymentMethodName = method.Name,
                    PaymentMethodCode = method.Code,
                    Amount = Math.Min(enteredAmount, RemainingBalance)
                });
            }
        }
    }

    private void AddPaymentLine(PaymentLine line)
    {
        PaymentLines.Add(line);
        RecalculateBalances();
    }

    [RelayCommand]
    private async Task RemovePaymentLineAsync(PaymentLine line)
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            "Remove Payment",
            $"Remove {line.PaymentMethodName} payment of {line.Amount:N0}?");

        if (confirm)
        {
            PaymentLines.Remove(line);
            RecalculateBalances();
        }
    }

    private void RecalculateBalances()
    {
        TotalPaid = PaymentLines.Sum(p => p.Amount);
        RemainingBalance = TotalDue - TotalPaid;

        if (RemainingBalance < 0)
        {
            // Overpayment - treat as cash change
            ChangeAmount = Math.Abs(RemainingBalance);
            RemainingBalance = 0;
        }
        else
        {
            ChangeAmount = PaymentLines
                .Where(p => p.PaymentMethodCode == "CASH")
                .Sum(p => p.ChangeAmount);
        }

        IsFullyPaid = RemainingBalance <= 0;
    }

    [RelayCommand(CanExecute = nameof(IsFullyPaid))]
    private async Task CompleteSaleAsync()
    {
        var payments = PaymentLines.Select(line => new Payment
        {
            ReceiptId = Receipt.Id,
            PaymentMethodId = line.PaymentMethodId,
            Amount = line.Amount,
            TenderedAmount = line.TenderedAmount,
            ChangeAmount = line.ChangeAmount,
            ReferenceNumber = line.ReferenceNumber,
            UserId = _authService.CurrentUser.Id
        }).ToList();

        var result = await _settlementService.SettleReceiptAsync(Receipt.Id, payments);

        if (result.Success)
        {
            // Open cash drawer if any cash payment
            if (PaymentLines.Any(p => p.PaymentMethodCode == "CASH"))
            {
                await _printerService.OpenCashDrawerAsync();
            }

            CloseDialog(true);
        }
    }
}

public class PaymentLine : ObservableObject
{
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string PaymentMethodCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? ReferenceNumber { get; set; }
}
```

### Split Payment XAML

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- Header -->
    <Border Grid.Row="0" Background="#1976D2" Padding="15">
        <StackPanel>
            <TextBlock Text="SPLIT PAYMENT" Foreground="White" FontWeight="Bold"/>
            <TextBlock Text="{Binding Receipt.ReceiptNumber}" Foreground="White"/>
            <TextBlock Text="{Binding TotalDue, StringFormat='Total: KSh {0:N2}'}"
                       Foreground="White" FontSize="24" FontWeight="Bold"/>
        </StackPanel>
    </Border>

    <!-- Payment Lines -->
    <ListView Grid.Row="1" ItemsSource="{Binding PaymentLines}" Margin="10">
        <ListView.ItemTemplate>
            <DataTemplate>
                <Border Background="#F5F5F5" Padding="10" Margin="0,5" CornerRadius="5">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <StackPanel>
                            <TextBlock Text="{Binding PaymentMethodName}" FontWeight="Bold"/>
                            <TextBlock Text="{Binding ReferenceNumber}"
                                       Foreground="Gray" FontSize="11"
                                       Visibility="{Binding ReferenceNumber,
                                                    Converter={StaticResource NullToCollapsed}}"/>
                        </StackPanel>

                        <TextBlock Grid.Column="1"
                                   Text="{Binding Amount, StringFormat='KSh {0:N2}'}"
                                   FontWeight="Bold" VerticalAlignment="Center"/>

                        <Button Grid.Column="2" Content="X"
                                Command="{Binding DataContext.RemovePaymentLineCommand,
                                          RelativeSource={RelativeSource AncestorType=ListView}}"
                                CommandParameter="{Binding}"
                                Width="30" Height="30" Margin="10,0,0,0"
                                Background="#FFCDD2"/>
                    </Grid>
                </Border>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>

    <!-- Balance Summary -->
    <Border Grid.Row="2" Background="#E8F5E9" Padding="15">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock Text="Total Paid:"/>
            <TextBlock Grid.Column="1"
                       Text="{Binding TotalPaid, StringFormat='KSh {0:N2}'}"/>

            <TextBlock Grid.Row="1" Text="Remaining:" FontWeight="Bold"/>
            <TextBlock Grid.Row="1" Grid.Column="1" FontWeight="Bold" FontSize="20"
                       Text="{Binding RemainingBalance, StringFormat='KSh {0:N2}'}"
                       Foreground="{Binding IsFullyPaid,
                                   Converter={StaticResource BoolToForeground},
                                   ConverterParameter='#2E7D32|#C62828'}"/>

            <TextBlock Grid.Row="2" Text="Change:"
                       Visibility="{Binding ChangeAmount,
                                   Converter={StaticResource PositiveToVisible}}"/>
            <TextBlock Grid.Row="2" Grid.Column="1"
                       Text="{Binding ChangeAmount, StringFormat='KSh {0:N2}'}"
                       Visibility="{Binding ChangeAmount,
                                   Converter={StaticResource PositiveToVisible}}"/>
        </Grid>
    </Border>

    <!-- Payment Method Buttons -->
    <ItemsControl Grid.Row="3" ItemsSource="{Binding AvailableMethods}" Margin="10">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Button Width="100" Height="50" Margin="5"
                        Command="{Binding DataContext.AddPaymentCommand,
                                  RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                        CommandParameter="{Binding}"
                        Background="{Binding BackgroundColor,
                                    Converter={StaticResource HexToBrush}}">
                    <TextBlock Text="{Binding Name}" Foreground="White" FontWeight="Bold"/>
                </Button>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>

    <!-- Action Buttons -->
    <Grid Grid.Row="4" Margin="10">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Button Content="Cancel" Command="{Binding CancelCommand}"
                Height="50" Margin="0,0,5,0"/>
        <Button Grid.Column="1" Content="Complete Sale"
                Command="{Binding CompleteSaleCommand}"
                IsEnabled="{Binding IsFullyPaid}"
                Height="50" Margin="5,0,0,0"
                Background="#4CAF50" Foreground="White"/>
    </Grid>
</Grid>
```

### Receipt Print - Split Payment Section

```
================================================
Payment:
Cash                                KSh  1,000
  Tendered: KSh 1,000
M-Pesa                              KSh    500
  Ref: QJK2ABC123
Credit Card                         KSh    762
------------------------------------------------
TOTAL PAID:                         KSh  2,262
================================================
```

### Overpayment Handling

When total payments exceed the amount due (only possible with cash):

```csharp
if (TotalPaid > TotalDue)
{
    // Find the cash payment line and update its change
    var cashLine = PaymentLines.FirstOrDefault(p => p.PaymentMethodCode == "CASH");
    if (cashLine != null)
    {
        var overage = TotalPaid - TotalDue;
        cashLine.ChangeAmount += overage;
        cashLine.Amount -= overage;  // Reduce effective amount
    }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.4.4-Split-Payments]
- [Source: docs/PRD_Hospitality_POS_System.md#PM-040 to PM-045]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
