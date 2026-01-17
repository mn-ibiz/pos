# Story 6.2: Receipt Settlement

Status: done

## Story

As a cashier,
I want to settle receipts by recording payment,
So that the sale is completed.

## Acceptance Criteria

1. **Given** a pending receipt exists
   **When** the cashier settles the receipt
   **Then** cashier must select payment method(s) ✅

2. **Given** payment is being recorded
   **When** entering payment amount
   **Then** payment amount must equal or exceed receipt total ✅

3. **Given** cash payment is selected
   **When** amount tendered exceeds total
   **Then** change should be calculated and displayed ✅

4. **Given** payment is complete
   **When** settlement is finalized
   **Then** receipt status should change to "Settled" ✅

5. **Given** settlement is complete
   **When** recording the transaction
   **Then** settlement timestamp and user should be recorded ✅

6. **Given** receipt is settled
   **When** completing the transaction
   **Then** customer receipt should be printed on 80mm thermal printer ⏳ (deferred to Epic 12)

## Tasks / Subtasks

- [x] Task 1: Create Payment Entity
  - [x] Create Payment entity class (already exists from previous session)
  - [x] Configure EF Core mappings (updated with TenderedAmount/ChangeAmount)
  - [ ] Create database migration (deferred - manual migration needed)

- [x] Task 2: Create Settlement Screen
  - [x] Create SettlementView.xaml
  - [x] Create SettlementViewModel
  - [x] Display receipt summary
  - [x] Show payment method buttons

- [x] Task 3: Implement Cash Payment
  - [x] Add amount tendered input
  - [x] Calculate change
  - [x] Add quick amount buttons
  - [x] Show large change display

- [x] Task 4: Implement Settlement Service
  - [x] Create SettleReceiptAsync method
  - [x] Validate payment covers total
  - [x] Create payment records
  - [x] Update receipt status

- [ ] Task 5: Implement Receipt Printing (deferred to Epic 12)
  - [ ] Generate customer receipt
  - [ ] Format for 80mm paper
  - [ ] Include all payment details
  - [ ] Print on settlement complete

## Dev Notes

### Payment Entity

```csharp
public class Payment
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public decimal TenderedAmount { get; set; }  // For cash
    public decimal ChangeAmount { get; set; }     // For cash
    public string? ReferenceNumber { get; set; }  // M-Pesa code, etc.
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Receipt Receipt { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

### PaymentMethod Entity

```csharp
public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Cash, M-Pesa, Card
    public string Code { get; set; } = string.Empty;  // CASH, MPESA, CARD
    public bool IsActive { get; set; } = true;
    public bool RequiresReference { get; set; } = false;
    public int DisplayOrder { get; set; }
    public string? IconPath { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
```

### Settlement Screen Layout

```
+------------------------------------------+
|          SETTLE RECEIPT                   |
|          R-20251220-0042                  |
+------------------------------------------+
|                                           |
|  Table: 5          Customer: John         |
|                                           |
|  ─────────────────────────────────────    |
|  2x Tusker Lager           KSh    700     |
|  1x Grilled Chicken        KSh    850     |
|  2x Chips                  KSh    400     |
|  ─────────────────────────────────────    |
|  Subtotal:                 KSh  1,950     |
|  Tax (16%):                KSh    312     |
|  ─────────────────────────────────────    |
|  TOTAL:                    KSh  2,262     |
+------------------------------------------+
|                                           |
|  [CASH]  [M-PESA]  [CARD]  [SPLIT]        |
|                                           |
+------------------------------------------+
```

### Cash Payment Panel

```
+------------------------------------------+
|          CASH PAYMENT                     |
+------------------------------------------+
|                                           |
|  Amount Due:          KSh 2,262.00        |
|                                           |
|  Amount Tendered:                         |
|  +------------------------------------+   |
|  |              2,300                 |   |
|  +------------------------------------+   |
|                                           |
|  Quick Amounts:                           |
|  [2,262] [2,300] [2,500] [3,000]          |
|                                           |
|  ─────────────────────────────────────    |
|                                           |
|  CHANGE DUE:          KSh   38.00         |
|                                           |
|  [CANCEL]                [COMPLETE]       |
+------------------------------------------+
```

### SettlementViewModel

```csharp
public partial class SettlementViewModel : BaseViewModel
{
    [ObservableProperty]
    private Receipt _receipt = null!;

    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _amountTendered;

    [ObservableProperty]
    private decimal _changeDue;

    [ObservableProperty]
    private decimal _remainingBalance;

    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _paymentMethods = new();

    [ObservableProperty]
    private ObservableCollection<PaymentLine> _paymentLines = new();

    [ObservableProperty]
    private PaymentMethod? _selectedPaymentMethod;

    partial void OnAmountTenderedChanged(decimal value)
    {
        if (SelectedPaymentMethod?.Code == "CASH")
        {
            ChangeDue = value - RemainingBalance;
            if (ChangeDue < 0) ChangeDue = 0;
        }
    }

    [RelayCommand]
    private async Task SelectPaymentMethodAsync(PaymentMethod method)
    {
        SelectedPaymentMethod = method;

        if (method.Code == "CASH")
        {
            // Show cash payment panel
        }
        else if (method.RequiresReference)
        {
            // Show reference input dialog
        }
        else
        {
            // Direct amount entry
        }
    }

    [RelayCommand]
    private async Task ApplyQuickAmountAsync(decimal amount)
    {
        AmountTendered = amount;
    }

    [RelayCommand]
    private async Task CompletePaymentAsync()
    {
        if (AmountTendered < RemainingBalance)
        {
            await _dialogService.ShowMessageAsync("Error", "Amount insufficient");
            return;
        }

        var payment = new Payment
        {
            ReceiptId = Receipt.Id,
            PaymentMethodId = SelectedPaymentMethod!.Id,
            Amount = RemainingBalance,
            TenderedAmount = AmountTendered,
            ChangeAmount = ChangeDue,
            UserId = _authService.CurrentUser.Id
        };

        PaymentLines.Add(new PaymentLine
        {
            Method = SelectedPaymentMethod.Name,
            Amount = RemainingBalance
        });

        AmountPaid += RemainingBalance;
        RemainingBalance = AmountDue - AmountPaid;

        if (RemainingBalance <= 0)
        {
            await FinalizeSettlementAsync();
        }

        // Reset for next payment
        SelectedPaymentMethod = null;
        AmountTendered = 0;
        ChangeDue = 0;
    }

    private async Task FinalizeSettlementAsync()
    {
        Receipt.Status = "Settled";
        Receipt.SettledAt = DateTime.UtcNow;
        Receipt.SettledByUserId = _authService.CurrentUser.Id;

        await _receiptRepo.UpdateAsync(Receipt);
        await _unitOfWork.SaveChangesAsync();

        // Print receipt
        await _printService.PrintCustomerReceiptAsync(Receipt);

        // Open cash drawer if cash payment
        if (PaymentLines.Any(p => p.Method == "Cash"))
        {
            await _printerService.OpenCashDrawerAsync();
        }

        await _dialogService.ShowMessageAsync("Success", "Receipt settled successfully");
        await _navigationService.NavigateToAsync("POS");
    }
}
```

### Customer Receipt Layout (80mm)

```
================================================
          HOSPITALITY POS
        123 Main Street
        Tel: +254 xxx xxx xxx
================================================
Receipt #: R-20251220-0042
Date: 2025-12-20 15:45
Server: John
Table: 5
------------------------------------------------
2x Tusker Lager            @350     KSh    700
1x Grilled Chicken                  KSh    850
2x Chips                   @200     KSh    400
------------------------------------------------
Subtotal:                           KSh  1,950
VAT (16%):                          KSh    312
------------------------------------------------
TOTAL:                              KSh  2,262
================================================
Payment:
Cash                                KSh  2,300
Change                              KSh     38
================================================

     Thank you for your visit!
      Please come again soon

================================================
```

### Settlement Service

```csharp
public class SettlementService : ISettlementService
{
    public async Task<SettlementResult> SettleReceiptAsync(
        int receiptId,
        List<PaymentRequest> payments)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId);
        if (receipt == null)
            throw new NotFoundException("Receipt not found");

        if (receipt.Status != "Pending")
            throw new InvalidOperationException("Receipt is not pending");

        var totalPayment = payments.Sum(p => p.Amount);
        if (totalPayment < receipt.TotalAmount)
            throw new InvalidOperationException("Payment amount is insufficient");

        // Create payment records
        foreach (var paymentReq in payments)
        {
            var payment = new Payment
            {
                ReceiptId = receiptId,
                PaymentMethodId = paymentReq.PaymentMethodId,
                Amount = paymentReq.Amount,
                TenderedAmount = paymentReq.TenderedAmount,
                ChangeAmount = paymentReq.ChangeAmount,
                ReferenceNumber = paymentReq.ReferenceNumber,
                UserId = _authService.CurrentUser.Id
            };

            await _paymentRepo.AddAsync(payment);
        }

        // Update receipt
        receipt.Status = "Settled";
        receipt.SettledAt = DateTime.UtcNow;
        receipt.SettledByUserId = _authService.CurrentUser.Id;

        await _receiptRepo.UpdateAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        // Deduct inventory
        await _inventoryService.DeductStockForReceiptAsync(receipt);

        return new SettlementResult
        {
            Success = true,
            Receipt = receipt,
            ChangeAmount = payments.Sum(p => p.ChangeAmount)
        };
    }
}
```

### Quick Amount Buttons
- Exact amount (2,262)
- Round up to 50 (2,300)
- Round up to 100 (2,300)
- Common amounts (2,500, 3,000, 5,000)

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.3.1-Receipt-Settlement]
- [Source: docs/PRD_Hospitality_POS_System.md#RS-001 to RS-005]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Extended Payment entity with TenderedAmount and ChangeAmount properties
- Extended IReceiptService with GetPaymentMethodsAsync, SettleReceiptAsync, AddPaymentAsync methods
- Implemented settlement methods in ReceiptService
- Created SettlementViewModel with full settlement workflow support
- Created SettlementView.xaml with modern dark UI following project patterns
- Created PaymentMethodColorConverter for payment method button colors
- Added navigation from POS to Settlement screen
- Registered SettlementViewModel in DI container
- Added DataTemplate mapping in MainWindow.xaml
- Receipt printing deferred to Epic 12 (Printing & Hardware Integration)
- Database migration needs manual creation for TenderedAmount/ChangeAmount columns

### File List
- src/HospitalityPOS.Core/Entities/Payment.cs (modified - TenderedAmount, ChangeAmount)
- src/HospitalityPOS.Core/Interfaces/IReceiptService.cs (extended)
- src/HospitalityPOS.Infrastructure/Services/ReceiptService.cs (implemented settlement methods)
- src/HospitalityPOS.Infrastructure/Data/Configurations/ReceiptConfiguration.cs (PaymentConfiguration updated)
- src/HospitalityPOS.WPF/ViewModels/SettlementViewModel.cs (new)
- src/HospitalityPOS.WPF/Views/SettlementView.xaml (new)
- src/HospitalityPOS.WPF/Views/SettlementView.xaml.cs (new)
- src/HospitalityPOS.WPF/Converters/PaymentMethodColorConverter.cs (new)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (added SettleReceiptAsync command)
- src/HospitalityPOS.WPF/Views/POSView.xaml (added settlement button)
- src/HospitalityPOS.WPF/Views/MainWindow.xaml (added SettlementViewModel DataTemplate)
- src/HospitalityPOS.WPF/App.xaml.cs (registered SettlementViewModel)
