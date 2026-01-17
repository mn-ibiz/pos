# Story 6.5: Bill Merging

Status: done

## Story

As a cashier,
I want to merge multiple receipts into one,
So that a customer can pay for multiple tabs at once.

## Acceptance Criteria

1. **Given** multiple pending receipts exist
   **When** merge is requested
   **Then** user can select 2 or more receipts to merge

2. **Given** receipts are selected for merge
   **When** validating receipts
   **Then** only pending/unsettled receipts can be merged

3. **Given** merge is executed
   **When** creating merged receipt
   **Then** merged receipt should contain all items from source receipts

4. **Given** source receipts
   **When** merge completes
   **Then** source receipts should be archived with reference to merged receipt

5. **Given** merged receipt exists
   **When** calculating totals
   **Then** new totals should be calculated correctly

6. **Given** merge operation
   **When** tracking changes
   **Then** merge operation should be logged in audit trail

## Tasks / Subtasks

- [ ] Task 1: Create Merge Bill Dialog
  - [ ] Create MergeBillDialog.xaml
  - [ ] Create MergeBillViewModel
  - [ ] Show list of pending receipts
  - [ ] Enable multi-select

- [ ] Task 2: Implement Receipt Selection
  - [ ] Filter pending receipts only
  - [ ] Show receipt summaries
  - [ ] Show checkbox selection
  - [ ] Calculate combined total

- [ ] Task 3: Implement Merge Service
  - [ ] Create MergeReceiptsAsync method
  - [ ] Combine items from all receipts
  - [ ] Generate new receipt number
  - [ ] Archive source receipts

- [ ] Task 4: Update Source Receipts
  - [ ] Set status to "Merged"
  - [ ] Store reference to merged receipt
  - [ ] Preserve for audit/history

- [ ] Task 5: Implement Merge Audit
  - [ ] Log merge operation
  - [ ] Record all source receipt IDs
  - [ ] Record merged receipt ID
  - [ ] Store reason if provided

## Dev Notes

### Merge Dialog Layout

```
+------------------------------------------+
|          MERGE BILLS                      |
+------------------------------------------+
|                                           |
|  Select receipts to merge:                |
|                                           |
|  +------------------------------------+   |
|  | [x] R-20251220-0042                |   |
|  |     Table 5 | KSh 1,250 | John     |   |
|  +------------------------------------+   |
|  | [x] R-20251220-0045                |   |
|  |     Table 5 | KSh 850   | John     |   |
|  +------------------------------------+   |
|  | [ ] R-20251220-0046                |   |
|  |     Bar     | KSh 700   | Mary     |   |
|  +------------------------------------+   |
|  | [ ] R-20251220-0048                |   |
|  |     Table 8 | KSh 1,500 | Peter    |   |
|  +------------------------------------+   |
|                                           |
|  ─────────────────────────────────────    |
|  Selected: 2 receipts                     |
|  Combined Total: KSh 2,100                |
|                                           |
|  [Cancel]              [Merge Selected]   |
+------------------------------------------+
```

### MergeBillViewModel

```csharp
public partial class MergeBillViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<SelectableReceipt> _availableReceipts = new();

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private decimal _combinedTotal;

    public async Task LoadReceiptsAsync()
    {
        var pendingReceipts = await _receiptRepo.GetPendingReceiptsAsync(
            _workPeriodService.CurrentWorkPeriodId);

        AvailableReceipts = new ObservableCollection<SelectableReceipt>(
            pendingReceipts.Select(r => new SelectableReceipt
            {
                Receipt = r,
                IsSelected = false
            }));
    }

    [RelayCommand]
    private void ToggleSelection(SelectableReceipt item)
    {
        item.IsSelected = !item.IsSelected;
        RecalculateSelection();
    }

    private void RecalculateSelection()
    {
        var selected = AvailableReceipts.Where(r => r.IsSelected).ToList();
        SelectedCount = selected.Count;
        CombinedTotal = selected.Sum(r => r.Receipt.TotalAmount);
    }

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private async Task MergeSelectedAsync()
    {
        var selectedReceipts = AvailableReceipts
            .Where(r => r.IsSelected)
            .Select(r => r.Receipt)
            .ToList();

        if (selectedReceipts.Count < 2)
        {
            await _dialogService.ShowMessageAsync(
                "Error",
                "Select at least 2 receipts to merge");
            return;
        }

        var result = await _mergeService.MergeReceiptsAsync(
            selectedReceipts.Select(r => r.Id).ToList());

        if (result.Success)
        {
            await _dialogService.ShowMessageAsync(
                "Merge Complete",
                $"Created merged receipt {result.MergedReceipt.ReceiptNumber}");

            CloseDialog(result.MergedReceipt);
        }
    }

    private bool CanMerge() => SelectedCount >= 2;
}

public class SelectableReceipt : ObservableObject
{
    [ObservableProperty]
    private Receipt _receipt = null!;

    [ObservableProperty]
    private bool _isSelected;
}
```

### Merge Service

```csharp
public class ReceiptMergeService : IReceiptMergeService
{
    private readonly IReceiptRepository _receiptRepo;
    private readonly ReceiptNumberGenerator _numberGenerator;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<MergeResult> MergeReceiptsAsync(List<int> receiptIds)
    {
        if (receiptIds.Count < 2)
            throw new InvalidOperationException("Need at least 2 receipts to merge");

        var receipts = new List<Receipt>();
        foreach (var id in receiptIds)
        {
            var receipt = await _receiptRepo.GetByIdWithItemsAsync(id);
            if (receipt == null)
                throw new NotFoundException($"Receipt {id} not found");

            if (receipt.Status != "Pending")
                throw new InvalidOperationException(
                    $"Receipt {receipt.ReceiptNumber} is not pending");

            receipts.Add(receipt);
        }

        // Create merged receipt
        var mergedReceipt = new Receipt
        {
            ReceiptNumber = await _numberGenerator.GenerateNextAsync(),
            OrderId = receipts.First().OrderId,  // Use first order
            WorkPeriodId = receipts.First().WorkPeriodId,
            UserId = _authService.CurrentUser.Id,
            TableNumber = string.Join(", ",
                receipts.Select(r => r.TableNumber).Distinct()),
            CustomerName = string.Join(", ",
                receipts.Select(r => r.CustomerName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()),
            Status = "Pending",
            IsMerged = true
        };

        // Copy all items from source receipts
        foreach (var receipt in receipts)
        {
            foreach (var item in receipt.ReceiptItems)
            {
                mergedReceipt.ReceiptItems.Add(new ReceiptItem
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
            }
        }

        // Calculate totals
        mergedReceipt.Subtotal = mergedReceipt.ReceiptItems
            .Sum(i => i.TotalAmount - i.TaxAmount);
        mergedReceipt.TaxAmount = mergedReceipt.ReceiptItems
            .Sum(i => i.TaxAmount);
        mergedReceipt.DiscountAmount = mergedReceipt.ReceiptItems
            .Sum(i => i.DiscountAmount);
        mergedReceipt.TotalAmount = mergedReceipt.ReceiptItems
            .Sum(i => i.TotalAmount);

        await _receiptRepo.AddAsync(mergedReceipt);

        // Archive source receipts
        foreach (var receipt in receipts)
        {
            receipt.Status = "Merged";
            receipt.MergedIntoReceiptId = mergedReceipt.Id;
            await _receiptRepo.UpdateAsync(receipt);
        }

        // Audit log
        await _auditService.LogAsync(AuditAction.ReceiptMerge,
            $"Merged {receipts.Count} receipts into {mergedReceipt.ReceiptNumber}",
            new Dictionary<string, object>
            {
                { "SourceReceiptIds", receiptIds },
                { "SourceReceiptNumbers", receipts.Select(r => r.ReceiptNumber).ToList() },
                { "MergedReceiptId", mergedReceipt.Id },
                { "MergedReceiptNumber", mergedReceipt.ReceiptNumber },
                { "CombinedTotal", mergedReceipt.TotalAmount }
            });

        await _unitOfWork.SaveChangesAsync();

        return new MergeResult
        {
            Success = true,
            MergedReceipt = mergedReceipt,
            SourceReceipts = receipts
        };
    }
}
```

### Receipt Entity Extensions for Merge

```csharp
public class Receipt
{
    // ... existing properties

    // Merge tracking
    public bool IsMerged { get; set; } = false;
    public int? MergedIntoReceiptId { get; set; }

    // Navigation
    public Receipt? MergedIntoReceipt { get; set; }
    public ICollection<Receipt> MergedFromReceipts { get; set; } = new List<Receipt>();
}
```

### Merged Receipt Print Layout (80mm)

```
================================================
          HOSPITALITY POS
================================================
Receipt #: R-20251220-0050
** MERGED BILL **
Merged from: R-20251220-0042, R-20251220-0045
Tables: 5, 5
------------------------------------------------
FROM RECEIPT R-20251220-0042:
2x Tusker Lager            @350     KSh    700
1x Grilled Chicken                  KSh    850

FROM RECEIPT R-20251220-0045:
1x Fish and Chips                   KSh    850
------------------------------------------------
Subtotal:                           KSh  2,069
VAT (16%):                          KSh    331
------------------------------------------------
TOTAL:                              KSh  2,400
================================================
```

### Validation Rules

1. **Minimum 2 receipts** required for merge
2. **Only pending receipts** can be merged
3. **Same work period** - receipts must be from current work period
4. **Ownership check** - user must have permission or be owner of all receipts
5. **Not already merged/split** - source receipts must not have been previously merged

### Merge Validation Service

```csharp
public class MergeValidationService
{
    public async Task<ValidationResult> ValidateMergeAsync(List<int> receiptIds)
    {
        var errors = new List<string>();

        if (receiptIds.Count < 2)
        {
            errors.Add("Select at least 2 receipts to merge");
            return new ValidationResult { IsValid = false, Errors = errors };
        }

        var receipts = await _receiptRepo.GetByIdsAsync(receiptIds);

        // Check all found
        if (receipts.Count != receiptIds.Count)
        {
            errors.Add("One or more receipts not found");
        }

        // Check all pending
        var nonPending = receipts.Where(r => r.Status != "Pending").ToList();
        if (nonPending.Any())
        {
            errors.Add($"Receipts not pending: {string.Join(", ",
                nonPending.Select(r => r.ReceiptNumber))}");
        }

        // Check same work period
        var workPeriods = receipts.Select(r => r.WorkPeriodId).Distinct().ToList();
        if (workPeriods.Count > 1)
        {
            errors.Add("All receipts must be from the same work period");
        }

        // Check not already merged
        var alreadyMerged = receipts.Where(r => r.IsMerged || r.MergedIntoReceiptId.HasValue).ToList();
        if (alreadyMerged.Any())
        {
            errors.Add($"Receipts already merged: {string.Join(", ",
                alreadyMerged.Select(r => r.ReceiptNumber))}");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.3.4-Bill-Merging]
- [Source: docs/PRD_Hospitality_POS_System.md#RS-015 to RS-017]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Added IsMerged property to Receipt entity for tracking merged receipts
- Created MergeResult model in SplitModels.cs for operation results
- Created IReceiptMergeService interface with full merge functionality
- Implemented ReceiptMergeService with transaction support and audit logging
- Created MergeBillDialog with receipt selection UI matching design spec
- Added ShowMergeBillDialogAsync to IDialogService interface
- Implemented merge dialog in DialogService
- Added MergeBillsAsyncCommand to POSViewModel
- Added Merge Bills button to POSView.xaml
- Validation includes: minimum 2 receipts, all pending, same work period, not already merged

### File List
- src/HospitalityPOS.Core/Entities/Receipt.cs (modified - added IsMerged property)
- src/HospitalityPOS.Core/Models/SplitModels.cs (modified - added MergeResult class)
- src/HospitalityPOS.Core/Interfaces/IReceiptMergeService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/ReceiptMergeService.cs (new)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered IReceiptMergeService)
- src/HospitalityPOS.WPF/Views/Dialogs/MergeBillDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/MergeBillDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified - added ShowMergeBillDialogAsync)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified - implemented merge dialog)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - added MergeBillsAsyncCommand)
- src/HospitalityPOS.WPF/Views/POSView.xaml (modified - added Merge Bills button)
