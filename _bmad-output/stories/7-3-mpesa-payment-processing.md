# Story 7.3: M-Pesa Payment Processing

Status: done

## Story

As a cashier,
I want to process M-Pesa payments with transaction code capture,
So that mobile money payments are properly recorded.

## Acceptance Criteria

1. **Given** a receipt is being settled
   **When** M-Pesa payment is selected
   **Then** cashier should enter the M-Pesa transaction code

2. **Given** M-Pesa code is entered
   **When** validating payment
   **Then** amount should equal the receipt total (or partial for split payments)

3. **Given** payment is recorded
   **When** storing transaction
   **Then** transaction code should be stored with the payment record

4. **Given** payment is complete
   **When** printing receipt
   **Then** receipt should print showing M-Pesa payment and transaction code

## Tasks / Subtasks

- [ ] Task 1: Create M-Pesa Payment Panel
  - [ ] Create MpesaPaymentPanel.xaml
  - [ ] Create MpesaPaymentViewModel
  - [ ] Display amount due
  - [ ] Add transaction code input

- [ ] Task 2: Implement Transaction Code Entry
  - [ ] Create alphanumeric input field
  - [ ] Validate code format (10 chars)
  - [ ] Auto-uppercase input
  - [ ] Show format hint

- [ ] Task 3: Implement Code Validation
  - [ ] Check code length
  - [ ] Check code format (starts with letter)
  - [ ] Prevent duplicate codes
  - [ ] Show validation errors

- [ ] Task 4: Implement Payment Recording
  - [ ] Store transaction code
  - [ ] Store payment amount
  - [ ] Link to receipt
  - [ ] Log for reconciliation

- [ ] Task 5: Update Receipt Print
  - [ ] Include M-Pesa as payment method
  - [ ] Print transaction code
  - [ ] Format for 80mm paper

## Dev Notes

### M-Pesa Payment Panel Layout

```
+------------------------------------------+
|          M-PESA PAYMENT                   |
+------------------------------------------+
|                                           |
|  [M-PESA LOGO]                            |
|                                           |
|  Amount Due:                              |
|  +------------------------------------+   |
|  |         KSh 2,262.00               |   |
|  +------------------------------------+   |
|                                           |
|  Enter M-Pesa Transaction Code:           |
|  (10 characters, e.g., QJK2XXXXXX)        |
|                                           |
|  +------------------------------------+   |
|  |  Q  J  K  2  X  X  X  X  X  X      |   |
|  +------------------------------------+   |
|                                           |
|  +---+  +---+  +---+  +---+  +---+        |
|  | Q |  | W |  | E |  | R |  | T |  ...   |
|  +---+  +---+  +---+  +---+  +---+        |
|  +---+  +---+  +---+  +---+  +---+        |
|  | A |  | S |  | D |  | F |  | G |  ...   |
|  +---+  +---+  +---+  +---+  +---+        |
|  +---+  +---+  +---+  +---+  +---+        |
|  | 1 |  | 2 |  | 3 |  | 4 |  | 5 |  ...   |
|  +---+  +---+  +---+  +---+  +---+        |
|                                           |
|  [ ] Customer shown the message           |
|                                           |
|  [Cancel]              [Confirm Payment]  |
+------------------------------------------+
```

### M-Pesa Code Format
- 10 characters
- Starts with letter (usually Q, R, S, T, P, N, O)
- Mix of uppercase letters and numbers
- Example: `QJK2ABCDEF`, `RAB12CD345`

### C# 14 Extension Members for M-Pesa Validation

```csharp
// C# 14 extension members provide cleaner validation on string type
public extension class MpesaValidation for string
{
    private const int MpesaCodeLength = 10;

    public bool IsValidMpesaCode =>
        Length == MpesaCodeLength &&
        !string.IsNullOrEmpty(this) &&
        char.IsLetter(this[0]) &&
        All(char.IsLetterOrDigit);

    public string ToMpesaFormat =>
        ToUpperInvariant().Trim();
}

// Usage in ViewModel - much cleaner validation
private void ValidateCode()
{
    ValidationError = null;
    IsCodeValid = false;

    if (string.IsNullOrEmpty(TransactionCode))
        return;

    // C# 14 extension member makes validation readable
    if (TransactionCode.IsValidMpesaCode)
    {
        IsCodeValid = true;
    }
    else
    {
        ValidationError = "Invalid M-Pesa code format";
    }
}
```

### MpesaPaymentViewModel

```csharp
public partial class MpesaPaymentViewModel : BaseViewModel
{
    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private string _transactionCode = string.Empty;

    [ObservableProperty]
    private bool _isCodeValid;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _customerConfirmed;

    private const int CodeLength = 10;

    partial void OnTransactionCodeChanged(string value)
    {
        ValidateCode();
    }

    [RelayCommand]
    private void AppendCharacter(string character)
    {
        if (TransactionCode.Length < CodeLength)
        {
            TransactionCode += character.ToUpper();
        }
    }

    [RelayCommand]
    private void Backspace()
    {
        if (TransactionCode.Length > 0)
        {
            TransactionCode = TransactionCode[..^1];
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TransactionCode = string.Empty;
    }

    private void ValidateCode()
    {
        ValidationError = null;
        IsCodeValid = false;

        if (string.IsNullOrEmpty(TransactionCode))
        {
            return;
        }

        if (TransactionCode.Length != CodeLength)
        {
            ValidationError = $"Code must be {CodeLength} characters";
            return;
        }

        if (!char.IsLetter(TransactionCode[0]))
        {
            ValidationError = "Code must start with a letter";
            return;
        }

        if (!TransactionCode.All(c => char.IsLetterOrDigit(c)))
        {
            ValidationError = "Code must contain only letters and numbers";
            return;
        }

        IsCodeValid = true;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmPayment))]
    private async Task ConfirmPaymentAsync()
    {
        // Check for duplicate code
        var existingPayment = await _paymentRepo.GetByReferenceAsync(TransactionCode);
        if (existingPayment != null)
        {
            await _dialogService.ShowMessageAsync(
                "Duplicate Code",
                $"This M-Pesa code was already used on receipt {existingPayment.Receipt.ReceiptNumber}");
            return;
        }

        var payment = new Payment
        {
            PaymentMethodId = _mpesaPaymentMethodId,
            Amount = AmountDue,
            ReferenceNumber = TransactionCode
        };

        CloseDialog(payment);
    }

    private bool CanConfirmPayment() => IsCodeValid && CustomerConfirmed;
}
```

### M-Pesa Payment Entity

```csharp
public class Payment
{
    // ... existing properties

    // For M-Pesa specific fields
    public string? MpesaPhoneNumber { get; set; }  // Optional
    public string? MpesaName { get; set; }         // Optional, from confirmation
}
```

### Receipt Print - M-Pesa Section

```
================================================
Payment:
M-Pesa                              KSh  2,262
Ref: QJK2ABCDEF
================================================
```

### M-Pesa Keyboard Layout

```xml
<Grid>
    <!-- Row 1: Q W E R T Y U I O P -->
    <UniformGrid Rows="1" Columns="10">
        <Button Content="Q" Command="{Binding AppendCharacterCommand}" CommandParameter="Q"/>
        <Button Content="W" Command="{Binding AppendCharacterCommand}" CommandParameter="W"/>
        <Button Content="E" Command="{Binding AppendCharacterCommand}" CommandParameter="E"/>
        <Button Content="R" Command="{Binding AppendCharacterCommand}" CommandParameter="R"/>
        <Button Content="T" Command="{Binding AppendCharacterCommand}" CommandParameter="T"/>
        <Button Content="Y" Command="{Binding AppendCharacterCommand}" CommandParameter="Y"/>
        <Button Content="U" Command="{Binding AppendCharacterCommand}" CommandParameter="U"/>
        <Button Content="I" Command="{Binding AppendCharacterCommand}" CommandParameter="I"/>
        <Button Content="O" Command="{Binding AppendCharacterCommand}" CommandParameter="O"/>
        <Button Content="P" Command="{Binding AppendCharacterCommand}" CommandParameter="P"/>
    </UniformGrid>

    <!-- Row 2: A S D F G H J K L -->
    <UniformGrid Rows="1" Columns="9">
        <Button Content="A" Command="{Binding AppendCharacterCommand}" CommandParameter="A"/>
        <!-- ... -->
    </UniformGrid>

    <!-- Row 3: Z X C V B N M -->
    <UniformGrid Rows="1" Columns="7">
        <Button Content="Z" Command="{Binding AppendCharacterCommand}" CommandParameter="Z"/>
        <!-- ... -->
    </UniformGrid>

    <!-- Row 4: Numbers 0-9 -->
    <UniformGrid Rows="1" Columns="12">
        <Button Content="1" Command="{Binding AppendCharacterCommand}" CommandParameter="1"/>
        <Button Content="2" Command="{Binding AppendCharacterCommand}" CommandParameter="2"/>
        <!-- ... -->
        <Button Content="0" Command="{Binding AppendCharacterCommand}" CommandParameter="0"/>
        <Button Content="⌫" Command="{Binding BackspaceCommand}" Background="#FFCDD2"/>
        <Button Content="CLR" Command="{Binding ClearCommand}" Background="#FFCDD2"/>
    </UniformGrid>
</Grid>
```

### Transaction Code Display

```xml
<Border Background="#E8F5E9" CornerRadius="5" Padding="10">
    <ItemsControl ItemsSource="{Binding TransactionCodeChars}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Width="35" Height="45" Margin="2"
                        BorderThickness="1" BorderBrush="#4CAF50"
                        Background="White" CornerRadius="3">
                    <TextBlock Text="{Binding}"
                               FontSize="24" FontWeight="Bold"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center"/>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Border>
```

### Duplicate Check Query

```csharp
public async Task<Payment?> GetByReferenceAsync(string referenceNumber)
{
    return await _context.Payments
        .Include(p => p.Receipt)
        .FirstOrDefaultAsync(p =>
            p.ReferenceNumber == referenceNumber &&
            p.Receipt.Status != "Voided");
}
```

### M-Pesa Reconciliation Report

```
================================================
     M-PESA RECONCILIATION REPORT
     2025-12-20
================================================
Receipt #    | Code       | Amount     | Time
-------------|------------|------------|-------
R-0042       | QJK2ABC123 | 2,262.00   | 15:45
R-0045       | RAB45XYZ89 | 1,500.00   | 16:20
R-0048       | PLM90QWE12 | 850.00     | 17:05
------------------------------------------------
TOTAL M-PESA:              KSh 4,612.00
Transaction Count: 3
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.4.2-Mobile-Money-Payments]
- [Source: docs/PRD_Hospitality_POS_System.md#PM-020 to PM-025]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
