# Story 11.3: Table Transfer

Status: done

## Story

As a waiter,
I want to transfer a table to another waiter,
So that handoffs between shifts are smooth.

## Acceptance Criteria

1. **Given** a table is assigned to a waiter
   **When** transfer is requested
   **Then** waiter can select another active user to transfer to

2. **Given** transfer is initiated
   **When** selecting new waiter
   **Then** all pending orders/receipts transfer with the table

3. **Given** transfer is completed
   **When** logging the action
   **Then** transfer should be logged in audit trail

4. **Given** audit trail entry
   **When** viewing transfer log
   **Then** both original and new waiter appear in log

## Tasks / Subtasks

- [x] Task 1: Create Transfer Dialog
  - [x] Create TableTransferDialog.xaml
  - [x] Create code-behind with transfer logic
  - [x] Display active waiters list
  - [x] Show transfer summary and reason input

- [x] Task 2: Implement Transfer Logic
  - [x] Create ITableTransferService interface
  - [x] Create TableTransferService implementation
  - [x] Transfer table ownership (AssignedUserId)
  - [x] Transfer receipt ownership (UserId)
  - [x] Create TableTransferLog entity for audit

- [x] Task 3: Add Audit Logging
  - [x] Create TableTransferLog entity with all required fields
  - [x] Record original waiter (FromUserId, FromUserName)
  - [x] Record new waiter (ToUserId, ToUserName)
  - [x] Store transfer reason and amount

- [x] Task 4: Add Transfer Notifications
  - [x] Update table map display (via refresh)
  - [x] Show transfer confirmation dialog
  - [x] Loading overlay during transfer

- [x] Task 5: Implement Bulk Transfer
  - [x] Create BulkTransferDialog.xaml
  - [x] Select multiple tables with checkboxes
  - [x] Transfer all to one waiter
  - [x] End-of-shift handoff with reason field

## Dev Notes

### Transfer Dialog

```
+------------------------------------------+
|      TRANSFER TABLE                       |
+------------------------------------------+
|                                           |
|  Table: 07                                |
|  Current Bill: KSh 3,500                  |
|  Occupied: 2h 30m                         |
|  Items: 8                                 |
|                                           |
|  Current Waiter:                          |
|  +------------------------------------+   |
|  | John Smith                         |   |
|  +------------------------------------+   |
|                                           |
|  Transfer To:                             |
|  +------------------------------------+   |
|  | ( ) Mary Johnson                   |   |
|  | (x) Peter Wanjiku                  |   |
|  | ( ) Sarah Kimani                   |   |
|  +------------------------------------+   |
|                                           |
|  Reason (Optional):                       |
|  +------------------------------------+   |
|  | End of shift                       |   |
|  +------------------------------------+   |
|                                           |
|  [Cancel]                   [Transfer]    |
+------------------------------------------+
```

### TableTransferService

```csharp
public interface ITableTransferService
{
    Task<TransferResult> TransferTableAsync(TransferTableRequest request);
    Task<TransferResult> BulkTransferAsync(BulkTransferRequest request);
    Task<List<TableTransferLog>> GetTransferHistoryAsync(int tableId);
}

public class TableTransferService : ITableTransferService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ITableStatusNotifier _statusNotifier;

    public async Task<TransferResult> TransferTableAsync(TransferTableRequest request)
    {
        var table = await _context.Tables
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == request.TableId);

        if (table == null)
        {
            return TransferResult.Failed("Table not found");
        }

        if (table.Status != TableStatus.Occupied)
        {
            return TransferResult.Failed("Only occupied tables can be transferred");
        }

        var originalUserId = table.AssignedUserId;
        var originalUserName = table.AssignedUser?.FullName ?? "Unknown";

        var newUser = await _context.Users.FindAsync(request.NewWaiterId);
        if (newUser == null)
        {
            return TransferResult.Failed("New waiter not found");
        }

        if (!newUser.IsActive)
        {
            return TransferResult.Failed("Selected waiter is not active");
        }

        // Transfer table
        table.AssignedUserId = request.NewWaiterId;
        table.AssignedUser = newUser;
        table.UpdatedAt = DateTime.UtcNow;

        // Transfer receipt ownership
        if (table.CurrentReceipt != null)
        {
            table.CurrentReceipt.UserId = request.NewWaiterId;
            table.CurrentReceipt.UpdatedAt = DateTime.UtcNow;
        }

        // Create transfer log
        var transferLog = new TableTransferLog
        {
            TableId = table.Id,
            TableNumber = table.TableNumber,
            FromUserId = originalUserId ?? 0,
            FromUserName = originalUserName,
            ToUserId = request.NewWaiterId,
            ToUserName = newUser.FullName,
            ReceiptId = table.CurrentReceiptId,
            ReceiptAmount = table.CurrentReceipt?.TotalAmount ?? 0,
            Reason = request.Reason,
            TransferredAt = DateTime.UtcNow,
            TransferredByUserId = request.TransferredByUserId
        };

        _context.TableTransferLogs.Add(transferLog);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            AuditAction.TableTransfer,
            $"Table {table.TableNumber} transferred from {originalUserName} to {newUser.FullName}",
            new Dictionary<string, object>
            {
                { "TableId", table.Id },
                { "TableNumber", table.TableNumber },
                { "FromUserId", originalUserId ?? 0 },
                { "ToUserId", request.NewWaiterId },
                { "ReceiptId", table.CurrentReceiptId ?? 0 },
                { "Reason", request.Reason ?? "" }
            });

        // Notify status change
        await _statusNotifier.NotifyTableStatusChangedAsync(table);

        return TransferResult.Success(transferLog);
    }

    public async Task<TransferResult> BulkTransferAsync(BulkTransferRequest request)
    {
        var results = new List<TableTransferLog>();
        var errors = new List<string>();

        foreach (var tableId in request.TableIds)
        {
            var result = await TransferTableAsync(new TransferTableRequest
            {
                TableId = tableId,
                NewWaiterId = request.NewWaiterId,
                Reason = request.Reason ?? "Bulk transfer",
                TransferredByUserId = request.TransferredByUserId
            });

            if (result.IsSuccess)
            {
                results.Add(result.TransferLog!);
            }
            else
            {
                errors.Add($"Table {tableId}: {result.ErrorMessage}");
            }
        }

        if (errors.Any())
        {
            return TransferResult.PartialSuccess(results, errors);
        }

        return TransferResult.Success(results);
    }

    public async Task<List<TableTransferLog>> GetTransferHistoryAsync(int tableId)
    {
        return await _context.TableTransferLogs
            .Where(t => t.TableId == tableId)
            .OrderByDescending(t => t.TransferredAt)
            .Take(50)
            .ToListAsync();
    }
}

public class TransferTableRequest
{
    public int TableId { get; set; }
    public int NewWaiterId { get; set; }
    public string? Reason { get; set; }
    public int TransferredByUserId { get; set; }
}

public class BulkTransferRequest
{
    public List<int> TableIds { get; set; } = new();
    public int NewWaiterId { get; set; }
    public string? Reason { get; set; }
    public int TransferredByUserId { get; set; }
}

public class TransferResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TableTransferLog? TransferLog { get; set; }
    public List<TableTransferLog>? TransferLogs { get; set; }
    public List<string>? Errors { get; set; }

    public static TransferResult Success(TableTransferLog log) =>
        new() { IsSuccess = true, TransferLog = log };

    public static TransferResult Success(List<TableTransferLog> logs) =>
        new() { IsSuccess = true, TransferLogs = logs };

    public static TransferResult Failed(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };

    public static TransferResult PartialSuccess(
        List<TableTransferLog> logs,
        List<string> errors) =>
        new() { IsSuccess = false, TransferLogs = logs, Errors = errors };
}
```

### TableTransferLog Entity

```csharp
public class TableTransferLog
{
    public long Id { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public int ToUserId { get; set; }
    public string ToUserName { get; set; } = string.Empty;
    public int? ReceiptId { get; set; }
    public decimal ReceiptAmount { get; set; }
    public string? Reason { get; set; }
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public int TransferredByUserId { get; set; }

    // Navigation
    public Table Table { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
    public Receipt? Receipt { get; set; }
    public User TransferredByUser { get; set; } = null!;
}
```

### TableTransferViewModel

```csharp
public partial class TableTransferViewModel : BaseViewModel
{
    private readonly ITableTransferService _transferService;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private TableDisplayItem _table = null!;

    [ObservableProperty]
    private ObservableCollection<User> _availableWaiters = new();

    [ObservableProperty]
    private User? _selectedWaiter;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private bool _isTransferring;

    public async Task InitializeAsync(TableDisplayItem table)
    {
        Table = table;

        // Get all active waiters except current
        var waiters = await _userService.GetActiveWaitersAsync();
        AvailableWaiters = new ObservableCollection<User>(
            waiters.Where(w => w.Id != table.Table.AssignedUserId));
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (SelectedWaiter == null)
        {
            await _dialogService.ShowMessageAsync(
                "Select Waiter",
                "Please select a waiter to transfer the table to.");
            return;
        }

        IsTransferring = true;

        try
        {
            var result = await _transferService.TransferTableAsync(
                new TransferTableRequest
                {
                    TableId = Table.Table.Id,
                    NewWaiterId = SelectedWaiter.Id,
                    Reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason,
                    TransferredByUserId = _authService.CurrentUser?.Id ?? 0
                });

            if (result.IsSuccess)
            {
                await _dialogService.ShowMessageAsync(
                    "Transfer Complete",
                    $"Table {Table.TableNumber} has been transferred to {SelectedWaiter.FullName}.");

                CloseDialog(true);
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    "Transfer Failed",
                    result.ErrorMessage ?? "An error occurred during transfer.");
            }
        }
        finally
        {
            IsTransferring = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(false);
    }
}
```

### Bulk Transfer Dialog

```
+------------------------------------------+
|      BULK TABLE TRANSFER                  |
|      End of Shift Handoff                 |
+------------------------------------------+
|                                           |
|  From: John Smith                         |
|  Active Tables: 4                         |
|                                           |
|  Tables to Transfer:                      |
|  +------------------------------------+   |
|  | [x] Table 02 - KSh 850             |   |
|  | [x] Table 07 - KSh 3,500           |   |
|  | [x] Table 09 - KSh 1,200           |   |
|  | [x] Table 11 - KSh 2,100           |   |
|  +------------------------------------+   |
|                                           |
|  Total Value: KSh 7,650                   |
|                                           |
|  Transfer All To:                         |
|  +------------------------------------+   |
|  | (x) Mary Johnson                   |   |
|  | ( ) Peter Wanjiku                  |   |
|  +------------------------------------+   |
|                                           |
|  Reason: End of shift handoff             |
|                                           |
|  [Cancel]    [Print Summary]   [Transfer] |
+------------------------------------------+
```

### BulkTransferViewModel

```csharp
public partial class BulkTransferViewModel : BaseViewModel
{
    [ObservableProperty]
    private User _fromWaiter = null!;

    [ObservableProperty]
    private ObservableCollection<SelectableTable> _tables = new();

    [ObservableProperty]
    private ObservableCollection<User> _availableWaiters = new();

    [ObservableProperty]
    private User? _selectedWaiter;

    [ObservableProperty]
    private string _reason = "End of shift handoff";

    [ObservableProperty]
    private decimal _totalValue;

    [ObservableProperty]
    private int _selectedCount;

    public async Task InitializeAsync(User fromWaiter)
    {
        FromWaiter = fromWaiter;

        // Get all tables assigned to this waiter
        var tables = await _tableService.GetTablesByWaiterAsync(fromWaiter.Id);
        Tables = new ObservableCollection<SelectableTable>(
            tables.Select(t => new SelectableTable
            {
                Table = t,
                IsSelected = true
            }));

        UpdateTotals();

        // Get other active waiters
        var waiters = await _userService.GetActiveWaitersAsync();
        AvailableWaiters = new ObservableCollection<User>(
            waiters.Where(w => w.Id != fromWaiter.Id));
    }

    partial void OnTablesChanged(ObservableCollection<SelectableTable>? value)
    {
        if (value != null)
        {
            foreach (var table in value)
            {
                table.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectableTable.IsSelected))
                    {
                        UpdateTotals();
                    }
                };
            }
        }
    }

    private void UpdateTotals()
    {
        var selected = Tables.Where(t => t.IsSelected).ToList();
        SelectedCount = selected.Count;
        TotalValue = selected.Sum(t => t.Table.CurrentReceipt?.TotalAmount ?? 0);
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var table in Tables)
        {
            table.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var table in Tables)
        {
            table.IsSelected = false;
        }
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (SelectedWaiter == null)
        {
            await _dialogService.ShowMessageAsync(
                "Select Waiter",
                "Please select a waiter to transfer tables to.");
            return;
        }

        var selectedTables = Tables
            .Where(t => t.IsSelected)
            .Select(t => t.Table.Id)
            .ToList();

        if (!selectedTables.Any())
        {
            await _dialogService.ShowMessageAsync(
                "No Tables Selected",
                "Please select at least one table to transfer.");
            return;
        }

        var result = await _transferService.BulkTransferAsync(
            new BulkTransferRequest
            {
                TableIds = selectedTables,
                NewWaiterId = SelectedWaiter.Id,
                Reason = Reason,
                TransferredByUserId = _authService.CurrentUser?.Id ?? 0
            });

        if (result.IsSuccess)
        {
            await _dialogService.ShowMessageAsync(
                "Transfer Complete",
                $"{selectedTables.Count} tables transferred to {SelectedWaiter.FullName}.");

            CloseDialog(true);
        }
        else if (result.TransferLogs?.Any() == true)
        {
            var message = $"Transferred {result.TransferLogs.Count} tables.\n" +
                         $"Errors: {string.Join("\n", result.Errors ?? new())}";
            await _dialogService.ShowMessageAsync("Partial Transfer", message);

            CloseDialog(true);
        }
        else
        {
            await _dialogService.ShowMessageAsync(
                "Transfer Failed",
                result.ErrorMessage ?? "An error occurred.");
        }
    }

    [RelayCommand]
    private async Task PrintSummaryAsync()
    {
        await _printService.PrintTransferSummaryAsync(new TransferSummary
        {
            FromWaiter = FromWaiter.FullName,
            ToWaiter = SelectedWaiter?.FullName ?? "Not Selected",
            Tables = Tables.Where(t => t.IsSelected).Select(t => t.Table).ToList(),
            TotalValue = TotalValue,
            Reason = Reason,
            PrintedAt = DateTime.Now
        });
    }
}

public class SelectableTable : ObservableObject
{
    private bool _isSelected;

    public Table Table { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
```

### Transfer Summary Print (80mm)

```
================================================
     TABLE TRANSFER SUMMARY
     2025-12-20 22:00
================================================

TRANSFER DETAILS:
------------------------------------------------
From: John Smith
To:   Mary Johnson
Reason: End of shift handoff
------------------------------------------------

TABLES TRANSFERRED:
------------------------------------------------
Table | Bill Amount | Time Occupied
------|-------------|---------------
  02  | KSh     850 | 45 min
  07  | KSh   3,500 | 2h 30m
  09  | KSh   1,200 | 1h 15m
  11  | KSh   2,100 | 55 min
------|-------------|---------------
Total | KSh   7,650 | 4 tables
------------------------------------------------

ACKNOWLEDGEMENT:
------------------------------------------------
Transferred By: John Smith
Received By:    __________________

Signature:      __________________

Date/Time:      __________________
================================================
```

### Transfer History Report Print (80mm)

```
================================================
     TABLE TRANSFER HISTORY
     Table 07
     2025-12-20
================================================

Time  | From       | To         | Amount
------|------------|------------|--------
08:30 | System     | John       |      -
10:15 | John       | Mary       |    850
14:00 | Mary       | Peter      |  1,200
18:30 | Peter      | John       |  2,500
22:00 | John       | Mary       |  3,500
------|------------|------------|--------
Total Transfers Today: 5
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.9.3-Table-Transfer]
- [Source: docs/PRD_Hospitality_POS_System.md#TM-021 to TM-030]

## Dev Agent Record

### Agent Model Used
Claude claude-opus-4-5-20251101

### Completion Notes List
- Created TableTransferLog entity with navigation properties for audit logging
- Created ITableTransferService interface with TransferTableAsync, BulkTransferAsync, GetTransferHistoryAsync, GetTablesByWaiterAsync, GetActiveWaitersAsync
- Implemented TableTransferService with full transfer logic including receipt ownership transfer
- Created TransferModels (TransferTableRequest, BulkTransferRequest, TransferResult, TransferSummary)
- Added TableTransferLogConfiguration with proper indexes and foreign keys
- Created TableTransferDialog for single table transfer with waiter selection
- Created BulkTransferDialog for end-of-shift bulk table handoff
- Registered ITableTransferService in DI container
- Added TableTransferLogs DbSet to POSDbContext

### File List
- src/HospitalityPOS.Core/Entities/TableTransferLog.cs (NEW)
- src/HospitalityPOS.Core/Models/TransferModels.cs (NEW)
- src/HospitalityPOS.Core/Interfaces/ITableTransferService.cs (NEW)
- src/HospitalityPOS.Infrastructure/Services/TableTransferService.cs (NEW)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (UPDATED - added TableTransferLogs DbSet)
- src/HospitalityPOS.Infrastructure/Data/Configurations/FloorConfiguration.cs (UPDATED - added TableTransferLogConfiguration)
- src/HospitalityPOS.WPF/Views/Dialogs/TableTransferDialog.xaml (NEW)
- src/HospitalityPOS.WPF/Views/Dialogs/TableTransferDialog.xaml.cs (NEW)
- src/HospitalityPOS.WPF/Views/Dialogs/BulkTransferDialog.xaml (NEW)
- src/HospitalityPOS.WPF/Views/Dialogs/BulkTransferDialog.xaml.cs (NEW)
- src/HospitalityPOS.WPF/App.xaml.cs (UPDATED - registered ITableTransferService)
