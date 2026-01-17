# Story 7.2: Cash Payment Processing

Status: done

## Story

As a cashier,
I want to process cash payments with change calculation,
So that customers can pay with physical currency.

## Acceptance Criteria

1. **Given** a receipt is being settled
   **When** cash payment is selected
   **Then** total amount due should be prominently displayed

2. **Given** cash payment screen
   **When** entering payment
   **Then** cashier can enter amount tendered

3. **Given** amount is entered
   **When** calculating change
   **Then** system should calculate and display change due

4. **Given** quick payment options
   **When** selecting amounts
   **Then** quick-amount buttons should be available (exact, round up)

5. **Given** cash payment complete
   **When** finalizing transaction
   **Then** cash drawer should open automatically (if configured)

6. **Given** payment is recorded
   **When** printing receipt
   **Then** receipt should print showing cash amount and change

## Tasks / Subtasks

- [ ] Task 1: Create Cash Payment Panel
  - [ ] Create CashPaymentPanel.xaml
  - [ ] Create CashPaymentViewModel
  - [ ] Display amount due prominently
  - [ ] Large numeric keypad

- [ ] Task 2: Implement Amount Entry
  - [ ] Create numeric input control
  - [ ] Support backspace/clear
  - [ ] Format with currency symbol
  - [ ] Validate minimum amount

- [ ] Task 3: Implement Change Calculation
  - [ ] Calculate change on input
  - [ ] Display change prominently
  - [ ] Show error if insufficient
  - [ ] Highlight when exact amount

- [ ] Task 4: Add Quick Amount Buttons
  - [ ] Calculate round-up amounts
  - [ ] Add common denomination buttons
  - [ ] Add exact amount button
  - [ ] Update on selection

- [ ] Task 5: Implement Cash Drawer Integration
  - [ ] Trigger drawer open command
  - [ ] Send ESC/POS kick command
  - [ ] Log drawer open event
  - [ ] Handle drawer offline

## Dev Notes

### Cash Payment Panel Layout

```
+------------------------------------------+
|          CASH PAYMENT                     |
+------------------------------------------+
|                                           |
|  Amount Due:                              |
|  +------------------------------------+   |
|  |         KSh 2,262.00               |   |
|  +------------------------------------+   |
|                                           |
|  Amount Tendered:                         |
|  +------------------------------------+   |
|  |              0                     |   |
|  +------------------------------------+   |
|                                           |
|  Quick Amounts:                           |
|  [Exact]  [2,300]  [2,500]  [3,000]       |
|  [5,000]  [10,000]                        |
|                                           |
|  +---+  +---+  +---+                      |
|  | 7 |  | 8 |  | 9 |                      |
|  +---+  +---+  +---+                      |
|  +---+  +---+  +---+                      |
|  | 4 |  | 5 |  | 6 |                      |
|  +---+  +---+  +---+                      |
|  +---+  +---+  +---+                      |
|  | 1 |  | 2 |  | 3 |                      |
|  +---+  +---+  +---+                      |
|  +---+  +---+  +---+                      |
|  | C |  | 0 |  |00 |                      |
|  +---+  +---+  +---+                      |
|                                           |
|  ─────────────────────────────────────    |
|  CHANGE DUE:                              |
|  +------------------------------------+   |
|  |         KSh     0.00               |   |
|  +------------------------------------+   |
|                                           |
|  [Cancel]              [Complete Sale]    |
+------------------------------------------+
```

### CashPaymentViewModel

```csharp
public partial class CashPaymentViewModel : BaseViewModel
{
    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private string _enteredAmount = "0";

    [ObservableProperty]
    private decimal _amountTendered;

    [ObservableProperty]
    private decimal _changeDue;

    [ObservableProperty]
    private bool _isAmountSufficient;

    [ObservableProperty]
    private ObservableCollection<QuickAmount> _quickAmounts = new();

    public void Initialize(decimal amountDue)
    {
        AmountDue = amountDue;
        GenerateQuickAmounts();
    }

    private void GenerateQuickAmounts()
    {
        QuickAmounts.Clear();

        // Exact amount
        QuickAmounts.Add(new QuickAmount
        {
            Label = "Exact",
            Amount = AmountDue
        });

        // Round up to nearest 100
        var roundUp100 = Math.Ceiling(AmountDue / 100) * 100;
        if (roundUp100 != AmountDue)
        {
            QuickAmounts.Add(new QuickAmount
            {
                Label = roundUp100.ToString("N0"),
                Amount = roundUp100
            });
        }

        // Round up to nearest 500
        var roundUp500 = Math.Ceiling(AmountDue / 500) * 500;
        if (roundUp500 != roundUp100)
        {
            QuickAmounts.Add(new QuickAmount
            {
                Label = roundUp500.ToString("N0"),
                Amount = roundUp500
            });
        }

        // Common denominations
        var denominations = new[] { 1000m, 2000m, 5000m, 10000m };
        foreach (var denom in denominations)
        {
            if (denom >= AmountDue && !QuickAmounts.Any(q => q.Amount == denom))
            {
                QuickAmounts.Add(new QuickAmount
                {
                    Label = denom.ToString("N0"),
                    Amount = denom
                });
            }
        }
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (EnteredAmount == "0")
        {
            EnteredAmount = digit;
        }
        else
        {
            EnteredAmount += digit;
        }

        UpdateCalculation();
    }

    [RelayCommand]
    private void AppendDoubleZero()
    {
        if (EnteredAmount != "0")
        {
            EnteredAmount += "00";
            UpdateCalculation();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        EnteredAmount = "0";
        UpdateCalculation();
    }

    [RelayCommand]
    private void Backspace()
    {
        if (EnteredAmount.Length > 1)
        {
            EnteredAmount = EnteredAmount[..^1];
        }
        else
        {
            EnteredAmount = "0";
        }

        UpdateCalculation();
    }

    [RelayCommand]
    private void SelectQuickAmount(QuickAmount quick)
    {
        EnteredAmount = quick.Amount.ToString("0");
        UpdateCalculation();
    }

    private void UpdateCalculation()
    {
        if (decimal.TryParse(EnteredAmount, out var amount))
        {
            AmountTendered = amount;
            ChangeDue = amount - AmountDue;
            IsAmountSufficient = ChangeDue >= 0;

            if (ChangeDue < 0)
            {
                ChangeDue = 0;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsAmountSufficient))]
    private async Task CompleteSaleAsync()
    {
        var payment = new Payment
        {
            PaymentMethodId = _cashPaymentMethodId,
            Amount = AmountDue,
            TenderedAmount = AmountTendered,
            ChangeAmount = ChangeDue
        };

        var result = await _settlementService.ProcessPaymentAsync(payment);

        if (result.Success)
        {
            // Open cash drawer
            await OpenCashDrawerAsync();

            // Close dialog with payment result
            CloseDialog(payment);
        }
    }

    private async Task OpenCashDrawerAsync()
    {
        try
        {
            var printer = await _printerConfig.GetReceiptPrinterAsync();
            await _escPosService.OpenCashDrawerAsync(printer);

            await _auditService.LogAsync(AuditAction.CashDrawerOpen,
                "Cash drawer opened for cash payment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open cash drawer");
            // Don't fail the transaction, just log
        }
    }
}

public class QuickAmount
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

### Cash Drawer ESC/POS Command

```csharp
public class EscPosService
{
    public async Task OpenCashDrawerAsync(PrinterConfig printer)
    {
        // ESC/POS drawer kick command
        // Pin 2: ESC p 0 25 250
        // Pin 5: ESC p 1 25 250
        var command = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };

        await SendToPrinterAsync(printer, command);
    }
}
```

### Cash Payment XAML

```xml
<UserControl x:Class="HospitalityPOS.Views.CashPaymentPanel">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Amount Due -->
        <Border Grid.Row="0" Background="#E3F2FD" Padding="20">
            <StackPanel>
                <TextBlock Text="Amount Due" FontSize="14"/>
                <TextBlock Text="{Binding AmountDue, StringFormat='KSh {0:N2}'}"
                           FontSize="36" FontWeight="Bold"/>
            </StackPanel>
        </Border>

        <!-- Amount Tendered -->
        <Border Grid.Row="1" Background="#FFF3E0" Padding="20">
            <StackPanel>
                <TextBlock Text="Amount Tendered" FontSize="14"/>
                <TextBlock Text="{Binding EnteredAmount, StringFormat='KSh {0}'}"
                           FontSize="36" FontWeight="Bold"/>
            </StackPanel>
        </Border>

        <!-- Quick Amounts -->
        <ItemsControl Grid.Row="2" ItemsSource="{Binding QuickAmounts}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Button Content="{Binding Label}"
                            Command="{Binding DataContext.SelectQuickAmountCommand,
                                      RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                            CommandParameter="{Binding}"
                            Width="80" Height="40" Margin="5"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- Numeric Keypad -->
        <UniformGrid Grid.Row="3" Rows="4" Columns="3" Margin="10">
            <Button Content="7" Command="{Binding AppendDigitCommand}" CommandParameter="7"/>
            <Button Content="8" Command="{Binding AppendDigitCommand}" CommandParameter="8"/>
            <Button Content="9" Command="{Binding AppendDigitCommand}" CommandParameter="9"/>
            <Button Content="4" Command="{Binding AppendDigitCommand}" CommandParameter="4"/>
            <Button Content="5" Command="{Binding AppendDigitCommand}" CommandParameter="5"/>
            <Button Content="6" Command="{Binding AppendDigitCommand}" CommandParameter="6"/>
            <Button Content="1" Command="{Binding AppendDigitCommand}" CommandParameter="1"/>
            <Button Content="2" Command="{Binding AppendDigitCommand}" CommandParameter="2"/>
            <Button Content="3" Command="{Binding AppendDigitCommand}" CommandParameter="3"/>
            <Button Content="C" Command="{Binding ClearCommand}" Background="#FFCDD2"/>
            <Button Content="0" Command="{Binding AppendDigitCommand}" CommandParameter="0"/>
            <Button Content="00" Command="{Binding AppendDoubleZeroCommand}"/>
        </UniformGrid>

        <!-- Change Due -->
        <Border Grid.Row="4" Padding="20"
                Background="{Binding IsAmountSufficient,
                             Converter={StaticResource BoolToBackground},
                             ConverterParameter='#C8E6C9|#FFCDD2'}">
            <StackPanel>
                <TextBlock Text="Change Due" FontSize="14"/>
                <TextBlock Text="{Binding ChangeDue, StringFormat='KSh {0:N2}'}"
                           FontSize="48" FontWeight="Bold"
                           Foreground="{Binding IsAmountSufficient,
                                        Converter={StaticResource BoolToForeground},
                                        ConverterParameter='#2E7D32|#C62828'}"/>
            </StackPanel>
        </Border>

        <!-- Action Buttons -->
        <Grid Grid.Row="5" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Button Content="Cancel" Command="{Binding CancelCommand}"
                    Height="50" Margin="0,0,5,0"/>
            <Button Grid.Column="1" Content="Complete Sale"
                    Command="{Binding CompleteSaleCommand}"
                    Height="50" Margin="5,0,0,0"
                    Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

### Receipt Print - Cash Payment Section

```
================================================
Payment:
Cash Tendered:                      KSh  3,000
Change Given:                       KSh    738
================================================
```

### Audit Log Entry

```csharp
await _auditService.LogAsync(AuditAction.CashPayment,
    $"Cash payment: {amountDue:N0} paid with {amountTendered:N0}",
    new Dictionary<string, object>
    {
        { "ReceiptId", receiptId },
        { "AmountDue", amountDue },
        { "AmountTendered", amountTendered },
        { "ChangeDue", changeDue }
    });
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.4.1-Cash-Payments]
- [Source: docs/PRD_Hospitality_POS_System.md#PM-010 to PM-015]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
