# Story 6.4: Bill Splitting

Status: done

## Story

As a cashier,
I want to split a receipt into multiple receipts,
So that customers can pay separately.

## Acceptance Criteria

1. **Given** a pending receipt with multiple items
   **When** split bill is requested
   **Then** user can choose to split equally by number of people

2. **Given** split by items is needed
   **When** selecting items
   **Then** user can select specific items for each split

3. **Given** items are being moved
   **When** organizing splits
   **Then** items can be dragged between split receipts

4. **Given** split is created
   **When** assigning receipt numbers
   **Then** each split receipt gets a new receipt number referencing the original

5. **Given** split receipts exist
   **When** settling
   **Then** each split can be settled with different payment methods

6. **Given** split is performed
   **When** tracking relationships
   **Then** original receipt should maintain reference to split receipts

7. **Given** split occurs
   **When** auditing
   **Then** split operation should be logged in audit trail

## Tasks / Subtasks

- [ ] Task 1: Create Split Bill Dialog
  - [ ] Create SplitBillDialog.xaml
  - [ ] Create SplitBillViewModel
  - [ ] Show split options (equal/by item)
  - [ ] Display original receipt items

- [ ] Task 2: Implement Equal Split
  - [ ] Input number of ways to split
  - [ ] Calculate per-person amount
  - [ ] Handle remainder/rounding
  - [ ] Generate split receipts

- [ ] Task 3: Implement Item-Based Split
  - [ ] Create drag-and-drop interface
  - [ ] Show multiple receipt containers
  - [ ] Move items between containers
  - [ ] Recalculate totals dynamically

- [ ] Task 4: Implement Split Service
  - [ ] Create SplitReceiptAsync method
  - [ ] Generate child receipt numbers
  - [ ] Link to parent receipt
  - [ ] Preserve item references

- [ ] Task 5: Update Receipt Settlement
  - [ ] Handle split receipt settlement
  - [ ] Track partial settlements
  - [ ] Update parent status when all splits settled

## Dev Notes

### Split Options Dialog

```
+------------------------------------------+
|          SPLIT BILL                       |
|          R-20251220-0042                  |
+------------------------------------------+
|                                           |
|  Total: KSh 2,262.00                      |
|                                           |
|  How would you like to split?             |
|                                           |
|  +------------------+ +------------------+ |
|  |                  | |                  | |
|  |  [SPLIT EQUAL]   | |  [SPLIT ITEMS]   | |
|  |                  | |                  | |
|  |  Divide total    | |  Select items    | |
|  |  by # of people  | |  for each split  | |
|  +------------------+ +------------------+ |
|                                           |
|  [Cancel]                                 |
+------------------------------------------+
```

### Equal Split Panel

```
+------------------------------------------+
|          EQUAL SPLIT                      |
+------------------------------------------+
|                                           |
|  Total: KSh 2,262.00                      |
|                                           |
|  Split into how many ways?                |
|                                           |
|  [-]    3    [+]                          |
|                                           |
|  Each person pays: KSh 754.00             |
|                                           |
|  Split 1: KSh 754.00                      |
|  Split 2: KSh 754.00                      |
|  Split 3: KSh 754.00                      |
|                                           |
|  [Cancel]              [Create Splits]    |
+------------------------------------------+
```

### Item-Based Split Panel

```
+------------------------------------------+
|          SPLIT BY ITEMS                   |
+------------------------------------------+
|                                           |
|  Original Receipt        Split 1          |
|  +----------------+     +----------------+|
|  | 2x Tusker     |     |                 ||
|  |    KSh 700    |     |                 ||
|  | 1x Chicken    |     |                 ||
|  |    KSh 850    | --> |                 ||
|  | 2x Chips      |     |                 ||
|  |    KSh 400    |     |                 ||
|  +----------------+     +----------------+|
|  Total: 2,262           Total: 0          |
|                                           |
|  [+ Add Split]                            |
|                                           |
|  [Cancel]              [Create Splits]    |
+------------------------------------------+
```

### Split Receipt Entity Extensions

```csharp
public class Receipt
{
    // ... existing properties

    // Split tracking
    public int? ParentReceiptId { get; set; }
    public bool IsSplit { get; set; } = false;
    public int? SplitNumber { get; set; }  // 1, 2, 3...
    public string? SplitType { get; set; }  // "Equal", "ByItem"

    // Navigation
    public Receipt? ParentReceipt { get; set; }
    public ICollection<Receipt> ChildReceipts { get; set; } = new List<Receipt>();
}
```

### SplitBillViewModel

```csharp
public partial class SplitBillViewModel : BaseViewModel
{
    [ObservableProperty]
    private Receipt _originalReceipt = null!;

    [ObservableProperty]
    private int _numberOfSplits = 2;

    [ObservableProperty]
    private decimal _amountPerPerson;

    [ObservableProperty]
    private ObservableCollection<SplitContainer> _splitContainers = new();

    [ObservableProperty]
    private string _splitType = "Equal";  // "Equal" or "ByItem"

    partial void OnNumberOfSplitsChanged(int value)
    {
        if (value > 0 && OriginalReceipt != null)
        {
            AmountPerPerson = Math.Ceiling(OriginalReceipt.TotalAmount / value);
        }
    }

    [RelayCommand]
    private void SelectSplitType(string type)
    {
        SplitType = type;

        if (type == "Equal")
        {
            NumberOfSplits = 2;
        }
        else
        {
            // Initialize item-based split
            InitializeItemBasedSplit();
        }
    }

    private void InitializeItemBasedSplit()
    {
        SplitContainers.Clear();

        // Original container with all items
        var original = new SplitContainer
        {
            Name = "Original",
            Items = new ObservableCollection<ReceiptItem>(
                OriginalReceipt.ReceiptItems)
        };
        SplitContainers.Add(original);

        // Empty split container
        var split1 = new SplitContainer { Name = "Split 1" };
        SplitContainers.Add(split1);
    }

    [RelayCommand]
    private void AddSplitContainer()
    {
        var newSplit = new SplitContainer
        {
            Name = $"Split {SplitContainers.Count}"
        };
        SplitContainers.Add(newSplit);
    }

    [RelayCommand]
    private void MoveItemToSplit(MoveItemRequest request)
    {
        var sourceContainer = SplitContainers.FirstOrDefault(
            c => c.Items.Contains(request.Item));

        if (sourceContainer != null)
        {
            sourceContainer.Items.Remove(request.Item);
            request.TargetContainer.Items.Add(request.Item);

            // Recalculate totals
            RecalculateAllTotals();
        }
    }

    [RelayCommand]
    private async Task CreateSplitsAsync()
    {
        if (SplitType == "Equal")
        {
            await CreateEqualSplitsAsync();
        }
        else
        {
            await CreateItemBasedSplitsAsync();
        }
    }

    private async Task CreateEqualSplitsAsync()
    {
        var result = await _receiptService.SplitReceiptEquallyAsync(
            OriginalReceipt.Id,
            NumberOfSplits);

        if (result.Success)
        {
            await _dialogService.ShowMessageAsync(
                "Split Complete",
                $"Created {NumberOfSplits} split receipts");

            CloseDialog(true);
        }
    }

    private async Task CreateItemBasedSplitsAsync()
    {
        var splitRequests = SplitContainers
            .Skip(1) // Skip original
            .Where(c => c.Items.Any())
            .Select(c => new SplitRequest
            {
                ItemIds = c.Items.Select(i => i.Id).ToList()
            })
            .ToList();

        var result = await _receiptService.SplitReceiptByItemsAsync(
            OriginalReceipt.Id,
            splitRequests);

        if (result.Success)
        {
            await _dialogService.ShowMessageAsync(
                "Split Complete",
                $"Created {splitRequests.Count} split receipts");

            CloseDialog(true);
        }
    }
}

public class SplitContainer : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ReceiptItem> _items = new();

    [ObservableProperty]
    private decimal _total;

    partial void OnItemsChanged(ObservableCollection<ReceiptItem> value)
    {
        Total = value?.Sum(i => i.TotalAmount) ?? 0;
    }
}
```

### Split Receipt Service

```csharp
public class ReceiptSplitService : IReceiptSplitService
{
    private readonly IReceiptRepository _receiptRepo;
    private readonly ReceiptNumberGenerator _numberGenerator;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<SplitResult> SplitReceiptEquallyAsync(
        int receiptId,
        int numberOfWays)
    {
        var original = await _receiptRepo.GetByIdWithItemsAsync(receiptId);
        if (original == null)
            throw new NotFoundException("Receipt not found");

        if (original.Status != "Pending")
            throw new InvalidOperationException("Can only split pending receipts");

        var amountPerSplit = Math.Ceiling(original.TotalAmount / numberOfWays);
        var splits = new List<Receipt>();

        for (int i = 1; i <= numberOfWays; i++)
        {
            var splitReceipt = new Receipt
            {
                ReceiptNumber = await _numberGenerator.GenerateNextAsync(),
                OrderId = original.OrderId,
                WorkPeriodId = original.WorkPeriodId,
                UserId = _authService.CurrentUser.Id,
                TableNumber = original.TableNumber,
                CustomerName = original.CustomerName,
                Subtotal = amountPerSplit / 1.16m,  // Approximate
                TaxAmount = amountPerSplit - (amountPerSplit / 1.16m),
                TotalAmount = amountPerSplit,
                Status = "Pending",
                ParentReceiptId = original.Id,
                IsSplit = true,
                SplitNumber = i,
                SplitType = "Equal"
            };

            await _receiptRepo.AddAsync(splitReceipt);
            splits.Add(splitReceipt);
        }

        // Update original receipt
        original.Status = "Split";
        await _receiptRepo.UpdateAsync(original);

        // Audit log
        await _auditService.LogAsync(AuditAction.ReceiptSplit,
            $"Receipt {original.ReceiptNumber} split equally into {numberOfWays} parts",
            new Dictionary<string, object>
            {
                { "OriginalReceiptId", original.Id },
                { "NumberOfSplits", numberOfWays },
                { "SplitReceiptIds", splits.Select(s => s.Id).ToList() }
            });

        await _unitOfWork.SaveChangesAsync();

        return new SplitResult
        {
            Success = true,
            OriginalReceipt = original,
            SplitReceipts = splits
        };
    }

    public async Task<SplitResult> SplitReceiptByItemsAsync(
        int receiptId,
        List<SplitRequest> splitRequests)
    {
        var original = await _receiptRepo.GetByIdWithItemsAsync(receiptId);
        if (original == null)
            throw new NotFoundException("Receipt not found");

        var splits = new List<Receipt>();
        var splitNumber = 1;

        foreach (var request in splitRequests)
        {
            var itemsForSplit = original.ReceiptItems
                .Where(i => request.ItemIds.Contains(i.Id))
                .ToList();

            if (!itemsForSplit.Any()) continue;

            var splitReceipt = new Receipt
            {
                ReceiptNumber = await _numberGenerator.GenerateNextAsync(),
                OrderId = original.OrderId,
                WorkPeriodId = original.WorkPeriodId,
                UserId = _authService.CurrentUser.Id,
                TableNumber = original.TableNumber,
                CustomerName = original.CustomerName,
                Subtotal = itemsForSplit.Sum(i => i.TotalAmount - i.TaxAmount),
                TaxAmount = itemsForSplit.Sum(i => i.TaxAmount),
                TotalAmount = itemsForSplit.Sum(i => i.TotalAmount),
                Status = "Pending",
                ParentReceiptId = original.Id,
                IsSplit = true,
                SplitNumber = splitNumber++,
                SplitType = "ByItem"
            };

            // Copy items to new receipt
            foreach (var item in itemsForSplit)
            {
                splitReceipt.ReceiptItems.Add(new ReceiptItem
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxAmount = item.TaxAmount,
                    TotalAmount = item.TotalAmount,
                    Modifiers = item.Modifiers,
                    Notes = item.Notes
                });

                // Remove from original
                original.ReceiptItems.Remove(item);
            }

            await _receiptRepo.AddAsync(splitReceipt);
            splits.Add(splitReceipt);
        }

        // Recalculate original totals
        original.Subtotal = original.ReceiptItems.Sum(i => i.TotalAmount - i.TaxAmount);
        original.TaxAmount = original.ReceiptItems.Sum(i => i.TaxAmount);
        original.TotalAmount = original.ReceiptItems.Sum(i => i.TotalAmount);

        if (!original.ReceiptItems.Any())
        {
            original.Status = "Split";
        }

        await _receiptRepo.UpdateAsync(original);
        await _unitOfWork.SaveChangesAsync();

        return new SplitResult
        {
            Success = true,
            OriginalReceipt = original,
            SplitReceipts = splits
        };
    }
}
```

### Split Receipt Number Format
- Parent: R-20251220-0042
- Split 1: R-20251220-0043 (reference: R-20251220-0042/1)
- Split 2: R-20251220-0044 (reference: R-20251220-0042/2)

### Printed Split Receipt

```
================================================
          HOSPITALITY POS
================================================
Receipt #: R-20251220-0043
** SPLIT 1 of 3 from R-20251220-0042 **
------------------------------------------------
1x Grilled Chicken                  KSh    850
------------------------------------------------
Subtotal:                           KSh    733
VAT (16%):                          KSh    117
------------------------------------------------
TOTAL:                              KSh    850
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.3.3-Bill-Splitting]
- [Source: docs/PRD_Hospitality_POS_System.md#RS-010 to RS-012]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5

### Completion Notes List
- Implemented equal split and item-based split functionality
- Created SplitBillDialog with two split modes (equal/by items)
- Split service supports 2-10 way equal splits with proper remainder handling
- Item-based split allows selecting specific items to move to new receipts
- Full audit logging for all split operations
- Split receipts are linked to parent via ParentReceiptId
- Original receipt marked with Split status when all items moved
- Added Split Bill button to POS view (appears after order is submitted)

### File List
- src/HospitalityPOS.Core/Enums/SystemEnums.cs (modified - added SplitType enum, Split/Merged statuses)
- src/HospitalityPOS.Core/Entities/Receipt.cs (modified - added IsSplit, SplitNumber, SplitType properties)
- src/HospitalityPOS.Core/Models/SplitModels.cs (new - SplitResult, SplitItemRequest, EqualSplitRequest, ItemBasedSplitRequest)
- src/HospitalityPOS.Core/Interfaces/IReceiptSplitService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/ReceiptSplitService.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/SplitBillDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/SplitBillDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified - added ShowSplitBillDialogAsync, SplitBillDialogResult)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified - implemented ShowSplitBillDialogAsync)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - added IReceiptSplitService, SplitBillCommand)
- src/HospitalityPOS.WPF/Views/POSView.xaml (modified - added Split Bill button)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered IReceiptSplitService)
