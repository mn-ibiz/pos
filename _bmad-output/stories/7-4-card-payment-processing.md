# Story 7.4: Card Payment Processing

Status: done

## Story

As a cashier,
I want to record card payments,
So that credit/debit card transactions are documented.

## Acceptance Criteria

1. **Given** a receipt is being settled
   **When** card payment is selected
   **Then** payment amount should be recorded

2. **Given** card details are optional
   **When** recording payment
   **Then** optionally, last 4 digits of card can be captured

3. **Given** payment is complete
   **When** printing receipt
   **Then** receipt should print showing card payment type

4. **Given** card processing
   **When** understanding integration
   **Then** no card processing integration required (external terminal)

## Tasks / Subtasks

- [ ] Task 1: Create Card Payment Panel
  - [ ] Create CardPaymentPanel.xaml
  - [ ] Create CardPaymentViewModel
  - [ ] Display amount due
  - [ ] Card type selection

- [ ] Task 2: Implement Card Type Selection
  - [ ] Add Credit Card option
  - [ ] Add Debit Card option
  - [ ] Visual card type indicators
  - [ ] Store selected type

- [ ] Task 3: Implement Optional Card Details
  - [ ] Add last 4 digits input (optional)
  - [ ] Numeric only, 4 digits max
  - [ ] Skip option available
  - [ ] Mask display

- [ ] Task 4: Implement Payment Recording
  - [ ] Store card type
  - [ ] Store optional reference
  - [ ] Link to receipt
  - [ ] No external integration

- [ ] Task 5: Update Receipt Print
  - [ ] Show card payment
  - [ ] Show card type (Credit/Debit)
  - [ ] Show masked digits if captured

## Dev Notes

### Card Payment Panel Layout

```
+------------------------------------------+
|          CARD PAYMENT                     |
+------------------------------------------+
|                                           |
|  Amount Due:                              |
|  +------------------------------------+   |
|  |         KSh 2,262.00               |   |
|  +------------------------------------+   |
|                                           |
|  Select Card Type:                        |
|                                           |
|  +----------------+  +----------------+   |
|  |                |  |                |   |
|  | [CREDIT CARD]  |  | [DEBIT CARD]   |   |
|  |                |  |                |   |
|  |  VISA/MC/AMEX  |  |  VISA/MC       |   |
|  +----------------+  +----------------+   |
|                                           |
|  Last 4 Digits (Optional):                |
|  +------------------------------------+   |
|  |  * * * *  [  ][  ][  ][  ]         |   |
|  +------------------------------------+   |
|                                           |
|  Process on external terminal,            |
|  then confirm payment here.               |
|                                           |
|  [Cancel]              [Confirm Payment]  |
+------------------------------------------+
```

### CardPaymentViewModel

```csharp
public partial class CardPaymentViewModel : BaseViewModel
{
    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private string? _cardType;  // "Credit" or "Debit"

    [ObservableProperty]
    private string _lastFourDigits = string.Empty;

    [ObservableProperty]
    private bool _isCardTypeSelected;

    [RelayCommand]
    private void SelectCardType(string type)
    {
        CardType = type;
        IsCardTypeSelected = true;
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (LastFourDigits.Length < 4)
        {
            LastFourDigits += digit;
        }
    }

    [RelayCommand]
    private void Backspace()
    {
        if (LastFourDigits.Length > 0)
        {
            LastFourDigits = LastFourDigits[..^1];
        }
    }

    [RelayCommand]
    private void ClearDigits()
    {
        LastFourDigits = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(IsCardTypeSelected))]
    private async Task ConfirmPaymentAsync()
    {
        // Determine payment method ID based on card type
        var paymentMethodId = CardType == "Credit"
            ? _creditCardPaymentMethodId
            : _debitCardPaymentMethodId;

        var payment = new Payment
        {
            PaymentMethodId = paymentMethodId,
            Amount = AmountDue,
            ReferenceNumber = string.IsNullOrEmpty(LastFourDigits)
                ? null
                : $"****{LastFourDigits}"
        };

        CloseDialog(payment);
    }
}
```

### Card Type Selection XAML

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- Credit Card -->
    <Button Grid.Column="0" Margin="5"
            Command="{Binding SelectCardTypeCommand}"
            CommandParameter="Credit"
            Height="100">
        <Button.Style>
            <Style TargetType="Button">
                <Setter Property="Background" Value="#E3F2FD"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding CardType}" Value="Credit">
                        <Setter Property="Background" Value="#2196F3"/>
                        <Setter Property="Foreground" Value="White"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
        <StackPanel>
            <TextBlock Text="💳" FontSize="32"/>
            <TextBlock Text="CREDIT CARD" FontWeight="Bold" FontSize="14"/>
            <TextBlock Text="Visa / Mastercard / Amex" FontSize="10"/>
        </StackPanel>
    </Button>

    <!-- Debit Card -->
    <Button Grid.Column="1" Margin="5"
            Command="{Binding SelectCardTypeCommand}"
            CommandParameter="Debit"
            Height="100">
        <Button.Style>
            <Style TargetType="Button">
                <Setter Property="Background" Value="#F3E5F5"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding CardType}" Value="Debit">
                        <Setter Property="Background" Value="#9C27B0"/>
                        <Setter Property="Foreground" Value="White"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
        <StackPanel>
            <TextBlock Text="💳" FontSize="32"/>
            <TextBlock Text="DEBIT CARD" FontWeight="Bold" FontSize="14"/>
            <TextBlock Text="Visa Debit / MC Debit" FontSize="10"/>
        </StackPanel>
    </Button>
</Grid>
```

### Last 4 Digits Input

```xml
<StackPanel>
    <TextBlock Text="Last 4 Digits (Optional)" Margin="0,20,0,10"/>

    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <!-- Masked prefix -->
        <TextBlock Text="* * * *  " FontSize="24" VerticalAlignment="Center"/>

        <!-- 4 digit boxes -->
        <ItemsControl ItemsSource="{Binding DigitSlots}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border Width="40" Height="50" Margin="2"
                            BorderThickness="1" BorderBrush="#9C27B0"
                            Background="White" CornerRadius="3">
                        <TextBlock Text="{Binding}"
                                   FontSize="24" FontWeight="Bold"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"/>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </StackPanel>

    <!-- Numeric keypad -->
    <UniformGrid Rows="4" Columns="3" Margin="20">
        <Button Content="1" Command="{Binding AppendDigitCommand}" CommandParameter="1"/>
        <Button Content="2" Command="{Binding AppendDigitCommand}" CommandParameter="2"/>
        <Button Content="3" Command="{Binding AppendDigitCommand}" CommandParameter="3"/>
        <Button Content="4" Command="{Binding AppendDigitCommand}" CommandParameter="4"/>
        <Button Content="5" Command="{Binding AppendDigitCommand}" CommandParameter="5"/>
        <Button Content="6" Command="{Binding AppendDigitCommand}" CommandParameter="6"/>
        <Button Content="7" Command="{Binding AppendDigitCommand}" CommandParameter="7"/>
        <Button Content="8" Command="{Binding AppendDigitCommand}" CommandParameter="8"/>
        <Button Content="9" Command="{Binding AppendDigitCommand}" CommandParameter="9"/>
        <Button Content="C" Command="{Binding ClearDigitsCommand}" Background="#FFCDD2"/>
        <Button Content="0" Command="{Binding AppendDigitCommand}" CommandParameter="0"/>
        <Button Content="⌫" Command="{Binding BackspaceCommand}"/>
    </UniformGrid>

    <TextBlock Text="Skip if not required"
               HorizontalAlignment="Center"
               Foreground="Gray" FontStyle="Italic"/>
</StackPanel>
```

### Receipt Print - Card Payment Section

```
================================================
Payment:
Credit Card                         KSh  2,262
Card: ****1234
================================================
```

Or without card digits:

```
================================================
Payment:
Debit Card                          KSh  2,262
================================================
```

### Payment Storage

```csharp
// Card payment without integration
var payment = new Payment
{
    ReceiptId = receiptId,
    PaymentMethodId = creditCardMethodId,
    Amount = amountDue,
    ReferenceNumber = "****1234",  // Optional
    UserId = currentUserId,
    CreatedAt = DateTime.UtcNow
};
```

### Card Payment Report

```
================================================
     CARD PAYMENTS REPORT
     2025-12-20
================================================
Receipt # | Type   | Last 4 | Amount    | Time
----------|--------|--------|-----------|-------
R-0042    | Credit | ****   | 2,262.00  | 15:45
R-0045    | Debit  | 1234   | 1,500.00  | 16:20
R-0048    | Credit | 5678   | 850.00    | 17:05
--------------------------------------------------
TOTAL CREDIT:              KSh 3,112.00 (2)
TOTAL DEBIT:               KSh 1,500.00 (1)
--------------------------------------------------
TOTAL CARD:                KSh 4,612.00 (3)
================================================
```

### External Terminal Note

The system does NOT integrate with card payment terminals. The workflow is:

1. Cashier selects Card Payment in POS
2. Cashier processes payment on external terminal
3. Once approved on terminal, cashier confirms in POS
4. POS records the card payment

This approach:
- Avoids complex PCI-DSS compliance
- Works with any card terminal
- Simple and reliable

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.4.3-Card-Payments]
- [Source: docs/PRD_Hospitality_POS_System.md#PM-030 to PM-035]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
