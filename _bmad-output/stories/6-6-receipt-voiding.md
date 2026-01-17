# Story 6.6: Receipt Voiding

Status: done

## Story

As a manager,
I want to void receipts with a mandatory reason,
So that erroneous transactions can be cancelled with proper documentation.

## Acceptance Criteria

1. **Given** a receipt exists (pending or settled)
   **When** manager voids the receipt
   **Then** void reason must be selected or entered (mandatory)

2. **Given** void is executed
   **When** updating status
   **Then** receipt status should change to "Voided"

3. **Given** receipt is voided
   **When** viewing the system
   **Then** voided receipt should remain visible in the system (marked as VOID)

4. **Given** totals are calculated
   **When** including voided receipts
   **Then** voided amount should not count toward sales totals

5. **Given** items were sold
   **When** receipt is voided
   **Then** inventory should be restored (stock returned) for voided items

6. **Given** void is complete
   **When** recording the transaction
   **Then** void should be recorded with: timestamp, user, reason

7. **Given** voids occur
   **When** generating reports
   **Then** voided receipts should appear in void report

## Tasks / Subtasks

- [ ] Task 1: Create Void Receipt Dialog
  - [ ] Create VoidReceiptDialog.xaml
  - [ ] Create VoidReceiptViewModel
  - [ ] Show receipt summary
  - [ ] Require reason selection/entry

- [ ] Task 2: Implement Void Service
  - [ ] Create VoidReceiptAsync method
  - [ ] Validate permissions
  - [ ] Update receipt status
  - [ ] Record void details

- [ ] Task 3: Implement Stock Restoration
  - [ ] Identify inventory-tracked items
  - [ ] Restore stock quantities
  - [ ] Create stock movement records
  - [ ] Reference void transaction

- [ ] Task 4: Create Void Reasons Management
  - [ ] Create VoidReason entity
  - [ ] Seed common void reasons
  - [ ] Allow custom reason entry
  - [ ] Admin can manage void reasons

- [ ] Task 5: Update Reports and Totals
  - [ ] Exclude voided from sales totals
  - [ ] Add void report
  - [ ] Show voided receipts distinctly
  - [ ] Print void slip

## Dev Notes

### Void Dialog Layout

```
+------------------------------------------+
|          VOID RECEIPT                     |
|          R-20251220-0042                  |
+------------------------------------------+
|                                           |
|  [!] This action cannot be undone         |
|                                           |
|  Receipt Summary:                         |
|  ─────────────────────────────────────    |
|  Table: 5          Status: Settled        |
|  Total: KSh 2,262  Items: 5               |
|  Settled: 15:45 by John                   |
|  ─────────────────────────────────────    |
|                                           |
|  Void Reason (Required):                  |
|  +------------------------------------+   |
|  | [x] Customer complaint             |   |
|  | [ ] Wrong order                    |   |
|  | [ ] Test transaction               |   |
|  | [ ] System error                   |   |
|  | [ ] Other (specify below)          |   |
|  +------------------------------------+   |
|                                           |
|  Additional Notes:                        |
|  +------------------------------------+   |
|  | Customer returned food, was cold  |    |
|  +------------------------------------+   |
|                                           |
|  [Cancel]               [Void Receipt]    |
+------------------------------------------+
```

### VoidReason Entity

```csharp
public class VoidReason
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequiresNote { get; set; } = false;
    public int DisplayOrder { get; set; }
}

// Seed Data
var voidReasons = new List<VoidReason>
{
    new() { Name = "Customer complaint", DisplayOrder = 1 },
    new() { Name = "Wrong order", DisplayOrder = 2 },
    new() { Name = "Item unavailable", DisplayOrder = 3 },
    new() { Name = "Duplicate transaction", DisplayOrder = 4 },
    new() { Name = "Test transaction", DisplayOrder = 5 },
    new() { Name = "System error", DisplayOrder = 6 },
    new() { Name = "Other", RequiresNote = true, DisplayOrder = 99 }
};
```

### Receipt Void Entity

```csharp
public class ReceiptVoid
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int VoidReasonId { get; set; }
    public string? AdditionalNotes { get; set; }
    public int VoidedByUserId { get; set; }
    public int? AuthorizedByUserId { get; set; }  // If override was needed
    public decimal VoidedAmount { get; set; }
    public DateTime VoidedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Receipt Receipt { get; set; } = null!;
    public VoidReason VoidReason { get; set; } = null!;
    public User VoidedByUser { get; set; } = null!;
    public User? AuthorizedByUser { get; set; }
}
```

### VoidReceiptViewModel

```csharp
public partial class VoidReceiptViewModel : BaseViewModel
{
    [ObservableProperty]
    private Receipt _receipt = null!;

    [ObservableProperty]
    private ObservableCollection<VoidReason> _voidReasons = new();

    [ObservableProperty]
    private VoidReason? _selectedReason;

    [ObservableProperty]
    private string _additionalNotes = string.Empty;

    [ObservableProperty]
    private bool _isReasonSelected;

    partial void OnSelectedReasonChanged(VoidReason? value)
    {
        IsReasonSelected = value != null;
    }

    [RelayCommand(CanExecute = nameof(CanVoid))]
    private async Task VoidReceiptAsync()
    {
        if (SelectedReason == null)
        {
            await _dialogService.ShowMessageAsync("Error", "Please select a void reason");
            return;
        }

        if (SelectedReason.RequiresNote && string.IsNullOrWhiteSpace(AdditionalNotes))
        {
            await _dialogService.ShowMessageAsync("Error", "Please provide additional notes");
            return;
        }

        // Check permission
        if (!await _authService.HasPermissionAsync(Permission.Receipts_Void))
        {
            var overrideResult = await _dialogService.ShowManagerOverrideDialogAsync(
                "Void Receipt",
                Permission.Receipts_Void);

            if (!overrideResult.Success)
            {
                return;
            }

            // Proceed with override
            await ExecuteVoidWithOverrideAsync(overrideResult.AuthorizingUser!.Id);
        }
        else
        {
            await ExecuteVoidAsync(null);
        }
    }

    private async Task ExecuteVoidAsync(int? authorizerId)
    {
        var result = await _voidService.VoidReceiptAsync(new VoidRequest
        {
            ReceiptId = Receipt.Id,
            VoidReasonId = SelectedReason!.Id,
            AdditionalNotes = AdditionalNotes,
            AuthorizedByUserId = authorizerId
        });

        if (result.Success)
        {
            // Print void slip
            await _printService.PrintVoidSlipAsync(result.VoidRecord);

            await _dialogService.ShowMessageAsync(
                "Void Complete",
                $"Receipt {Receipt.ReceiptNumber} has been voided");

            CloseDialog(true);
        }
    }

    private bool CanVoid() => IsReasonSelected;
}
```

### Void Service

```csharp
public class ReceiptVoidService : IReceiptVoidService
{
    private readonly IReceiptRepository _receiptRepo;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<VoidResult> VoidReceiptAsync(VoidRequest request)
    {
        var receipt = await _receiptRepo.GetByIdWithItemsAsync(request.ReceiptId);
        if (receipt == null)
            throw new NotFoundException("Receipt not found");

        if (receipt.Status == "Voided")
            throw new InvalidOperationException("Receipt is already voided");

        // Create void record
        var voidRecord = new ReceiptVoid
        {
            ReceiptId = receipt.Id,
            VoidReasonId = request.VoidReasonId,
            AdditionalNotes = request.AdditionalNotes,
            VoidedByUserId = _authService.CurrentUser.Id,
            AuthorizedByUserId = request.AuthorizedByUserId,
            VoidedAmount = receipt.TotalAmount,
            VoidedAt = DateTime.UtcNow
        };

        await _voidRecordRepo.AddAsync(voidRecord);

        // Update receipt status
        receipt.Status = "Voided";
        receipt.VoidedAt = DateTime.UtcNow;
        await _receiptRepo.UpdateAsync(receipt);

        // Restore inventory for tracked items
        foreach (var item in receipt.ReceiptItems)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);
            if (product != null && product.TrackInventory)
            {
                await _inventoryService.RestoreStockAsync(
                    item.ProductId,
                    item.Quantity,
                    StockMovementType.Void,
                    $"Void: {receipt.ReceiptNumber}");
            }
        }

        // Audit log
        await _auditService.LogAsync(AuditAction.ReceiptVoid,
            $"Receipt {receipt.ReceiptNumber} voided. Amount: {receipt.TotalAmount:N0}",
            new Dictionary<string, object>
            {
                { "ReceiptId", receipt.Id },
                { "ReceiptNumber", receipt.ReceiptNumber },
                { "VoidedAmount", receipt.TotalAmount },
                { "VoidReason", request.VoidReasonId },
                { "VoidedBy", _authService.CurrentUser.Id },
                { "AuthorizedBy", request.AuthorizedByUserId }
            });

        await _unitOfWork.SaveChangesAsync();

        return new VoidResult
        {
            Success = true,
            VoidRecord = voidRecord
        };
    }
}
```

### Void Slip Print Layout (80mm)

```
================================================
         *** VOID SLIP ***
================================================
Date: 2025-12-20 16:30

Receipt #: R-20251220-0042
Original Date: 2025-12-20 15:45
Original Amount: KSh 2,262.00

VOIDED ITEMS:
------------------------------------------------
2x Tusker Lager                     KSh    700
1x Grilled Chicken                  KSh    850
2x Chips                            KSh    400
------------------------------------------------
TOTAL VOIDED:                       KSh  2,262
------------------------------------------------

Void Reason: Customer complaint
Notes: Customer returned food, was cold

Voided By: John Smith
Authorized By: Mary Manager (PIN Override)

================================================
         ORIGINAL RECEIPT CANCELLED
================================================
```

### Stock Restoration Logic

```csharp
public class InventoryService
{
    public async Task RestoreStockAsync(
        int productId,
        decimal quantity,
        StockMovementType movementType,
        string reference)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null || !product.TrackInventory)
            return;

        var previousStock = product.CurrentStock;
        product.CurrentStock += quantity;

        await _productRepo.UpdateAsync(product);

        // Create stock movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = movementType.ToString(),
            Quantity = quantity,
            PreviousStock = previousStock,
            NewStock = product.CurrentStock,
            Reference = reference,
            UserId = _authService.CurrentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _stockMovementRepo.AddAsync(movement);
    }
}
```

### Void Report Query

```csharp
public async Task<List<VoidReportItem>> GetVoidReportAsync(
    DateTime startDate,
    DateTime endDate)
{
    return await _context.ReceiptVoids
        .Where(v => v.VoidedAt >= startDate && v.VoidedAt <= endDate)
        .Include(v => v.Receipt)
        .Include(v => v.VoidReason)
        .Include(v => v.VoidedByUser)
        .Include(v => v.AuthorizedByUser)
        .Select(v => new VoidReportItem
        {
            ReceiptNumber = v.Receipt.ReceiptNumber,
            VoidedAmount = v.VoidedAmount,
            VoidReason = v.VoidReason.Name,
            Notes = v.AdditionalNotes,
            VoidedBy = v.VoidedByUser.FullName,
            AuthorizedBy = v.AuthorizedByUser != null
                ? v.AuthorizedByUser.FullName
                : null,
            VoidedAt = v.VoidedAt
        })
        .OrderByDescending(v => v.VoidedAt)
        .ToListAsync();
}
```

### Permissions Required
- `Receipts_Void` - Void receipts
- `Receipts_VoidSettled` - Void already settled receipts (higher permission)

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.3.5-Receipt-Voiding]
- [Source: docs/PRD_Hospitality_POS_System.md#RS-020 to RS-025]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created VoidReason entity for predefined void reasons with seeded data
- Created ReceiptVoid entity for detailed void audit records
- Updated DbContext with VoidReasons and ReceiptVoids DbSets
- Added EF Core configurations for VoidReason and ReceiptVoid with seed data
- Created VoidModels.cs with VoidRequest, VoidResult, and VoidReportItem classes
- Created IReceiptVoidService interface with full void functionality
- Implemented ReceiptVoidService with transaction support, validation, and stock restoration
- Created VoidReceiptDialog with reason selection UI (red theme)
- Added ShowVoidReceiptDialogAsync to IDialogService interface
- Implemented void dialog in DialogService
- Added VoidReceiptAsyncCommand to POSViewModel
- Added Void Receipt button (red #EF4444) to POSView.xaml
- Stock is automatically restored for inventory-tracked items on void
- Full audit trail with void reason, notes, and authorizing user

### File List
- src/HospitalityPOS.Core/Entities/VoidReason.cs (new)
- src/HospitalityPOS.Core/Entities/ReceiptVoid.cs (new)
- src/HospitalityPOS.Core/Models/VoidModels.cs (new)
- src/HospitalityPOS.Core/Interfaces/IReceiptVoidService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/ReceiptVoidService.cs (new)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (modified - added DbSets)
- src/HospitalityPOS.Infrastructure/Data/Configurations/ReceiptConfiguration.cs (modified - added void configurations)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered IReceiptVoidService)
- src/HospitalityPOS.WPF/Views/Dialogs/VoidReceiptDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/VoidReceiptDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified - added ShowVoidReceiptDialogAsync)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified - implemented void dialog)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - added VoidReceiptAsyncCommand)
- src/HospitalityPOS.WPF/Views/POSView.xaml (modified - added Void Receipt button)
