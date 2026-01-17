# Story 12.4: Cash Drawer Integration

Status: done

## Story

As a cashier,
I want the cash drawer to open automatically on cash transactions,
So that I don't have to manually open it.

## Acceptance Criteria

1. **Given** cash drawer is connected (via printer RJ11 port)
   **When** cash payment is processed
   **Then** drawer should open automatically

2. **Given** manual access needed
   **When** authorized
   **Then** manual drawer open should be available (with permission)

3. **Given** drawer activity
   **When** any drawer open event occurs
   **Then** all drawer open events should be logged

4. **Given** drawer connection
   **When** using printer
   **Then** drawer can also open via receipt printer kick command

## Tasks / Subtasks

- [x] Task 1: Create Cash Drawer Service
  - [x] Create ICashDrawerService interface
  - [x] Implement drawer kick command
  - [x] Handle printer communication
  - [x] Support multiple drawer pins

- [x] Task 2: Implement Automatic Open
  - [x] Open on cash payment
  - [x] Open on manual drawer request
  - [x] Configure auto-open behavior
  - [x] Handle drawer already open

- [x] Task 3: Create Cash Drawer Settings
  - [x] Drawer pin configuration
  - [x] Linked printer selection
  - [x] Auto-open options
  - [x] Test drawer button

- [x] Task 4: Implement Drawer Logging
  - [x] Log all open events
  - [x] Record reason/trigger
  - [x] Track user who opened
  - [x] Integrate with audit trail

- [x] Task 5: Add Permission Controls
  - [x] Permission for manual open
  - [x] Require reason for manual open
  - [x] Manager override support
  - [x] End-of-day drawer count

## Dev Notes

### Cash Drawer Entity

```csharp
public class CashDrawer
{
    public int Id { get; set; }
    public string Name { get; set; } = "Main Drawer";
    public int LinkedPrinterId { get; set; }
    public CashDrawerPin DrawerPin { get; set; } = CashDrawerPin.Pin2;

    // Auto-open settings
    public bool AutoOpenOnCashPayment { get; set; } = true;
    public bool AutoOpenOnCashRefund { get; set; } = true;
    public bool AutoOpenOnDrawerCount { get; set; } = true;

    // Current status
    public CashDrawerStatus Status { get; set; } = CashDrawerStatus.Closed;
    public DateTime? LastOpenedAt { get; set; }
    public int? LastOpenedByUserId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Printer LinkedPrinter { get; set; } = null!;
    public User? LastOpenedByUser { get; set; }
}

public enum CashDrawerPin
{
    Pin2 = 0,   // Most common - DK (drawer kick) pin 2
    Pin5 = 1    // Alternative - DK pin 5
}

public enum CashDrawerStatus
{
    Closed,
    Open,
    Unknown
}
```

### CashDrawerLog Entity

```csharp
public class CashDrawerLog
{
    public long Id { get; set; }
    public int CashDrawerId { get; set; }
    public int UserId { get; set; }
    public CashDrawerOpenReason Reason { get; set; }
    public string? Reference { get; set; }      // Receipt number, etc.
    public string? Notes { get; set; }          // Manual open reason
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public int? AuthorizedByUserId { get; set; } // For manager overrides

    // Navigation
    public CashDrawer CashDrawer { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? AuthorizedByUser { get; set; }
}

public enum CashDrawerOpenReason
{
    CashPayment,
    CashRefund,
    ManualOpen,
    DrawerCount,
    OpeningFloat,
    ClosingCount,
    CashDrop,
    PettyCash,
    Other
}
```

### Cash Drawer Service

```csharp
public interface ICashDrawerService
{
    Task<bool> OpenDrawerAsync(int drawerId, CashDrawerOpenReason reason,
        string? reference = null, string? notes = null);
    Task<bool> OpenDrawerForPaymentAsync(int paymentId);
    Task<CashDrawerStatus> GetStatusAsync(int drawerId);
    Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime date);
    Task TestDrawerAsync(int drawerId);
}

public class CashDrawerService : ICashDrawerService
{
    private readonly ApplicationDbContext _context;
    private readonly IPrinterCommunicationService _printerService;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CashDrawerService> _logger;

    public async Task<bool> OpenDrawerAsync(
        int drawerId,
        CashDrawerOpenReason reason,
        string? reference = null,
        string? notes = null)
    {
        var drawer = await _context.CashDrawers
            .Include(d => d.LinkedPrinter)
            .FirstOrDefaultAsync(d => d.Id == drawerId);

        if (drawer == null)
        {
            _logger.LogWarning("Cash drawer {DrawerId} not found", drawerId);
            return false;
        }

        if (drawer.LinkedPrinter == null)
        {
            _logger.LogWarning("Cash drawer {DrawerId} has no linked printer", drawerId);
            return false;
        }

        // Generate drawer kick command
        var command = GetDrawerKickCommand(drawer.DrawerPin);

        // Send to printer
        var result = await _printerService.SendAsync(drawer.LinkedPrinter, command);

        if (result.Success)
        {
            // Update drawer status
            drawer.Status = CashDrawerStatus.Open;
            drawer.LastOpenedAt = DateTime.UtcNow;
            drawer.LastOpenedByUserId = _authService.CurrentUser?.Id;

            // Log the event
            var log = new CashDrawerLog
            {
                CashDrawerId = drawerId,
                UserId = _authService.CurrentUser?.Id ?? 0,
                Reason = reason,
                Reference = reference,
                Notes = notes,
                OpenedAt = DateTime.UtcNow
            };

            _context.CashDrawerLogs.Add(log);
            await _context.SaveChangesAsync();

            // Audit trail
            await _auditService.LogAsync(
                AuditAction.CashDrawerOpen,
                $"Cash drawer opened: {reason}",
                new Dictionary<string, object>
                {
                    { "DrawerId", drawerId },
                    { "Reason", reason.ToString() },
                    { "Reference", reference ?? "" }
                });

            _logger.LogInformation(
                "Cash drawer {DrawerId} opened by {UserId} - Reason: {Reason}",
                drawerId, _authService.CurrentUser?.Id, reason);

            return true;
        }

        _logger.LogError(
            "Failed to open cash drawer {DrawerId}: {Error}",
            drawerId, result.ErrorMessage);

        return false;
    }

    public async Task<bool> OpenDrawerForPaymentAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null) return false;

        // Only open for cash payments
        if (payment.Method != "Cash") return true;

        // Get default drawer
        var drawer = await _context.CashDrawers
            .Where(d => d.IsActive && d.AutoOpenOnCashPayment)
            .FirstOrDefaultAsync();

        if (drawer == null) return true;

        return await OpenDrawerAsync(
            drawer.Id,
            CashDrawerOpenReason.CashPayment,
            payment.Receipt?.ReceiptNumber);
    }

    private byte[] GetDrawerKickCommand(CashDrawerPin pin)
    {
        // ESC p m t1 t2
        // m = drawer pin (0 = pin 2, 1 = pin 5)
        // t1, t2 = pulse timing (25ms on, 250ms off typical)
        return pin switch
        {
            CashDrawerPin.Pin2 => new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA },
            CashDrawerPin.Pin5 => new byte[] { 0x1B, 0x70, 0x01, 0x19, 0xFA },
            _ => new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }
        };
    }

    public async Task<CashDrawerStatus> GetStatusAsync(int drawerId)
    {
        var drawer = await _context.CashDrawers.FindAsync(drawerId);
        return drawer?.Status ?? CashDrawerStatus.Unknown;
    }

    public async Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime date)
    {
        return await _context.CashDrawerLogs
            .Where(l => l.CashDrawerId == drawerId)
            .Where(l => l.OpenedAt.Date == date.Date)
            .Include(l => l.User)
            .Include(l => l.AuthorizedByUser)
            .OrderByDescending(l => l.OpenedAt)
            .ToListAsync();
    }

    public async Task TestDrawerAsync(int drawerId)
    {
        await OpenDrawerAsync(drawerId, CashDrawerOpenReason.Other, notes: "Test open");
    }
}
```

### Cash Drawer Settings Screen

```
+------------------------------------------+
|      CASH DRAWER CONFIGURATION            |
+------------------------------------------+
|                                           |
|  Cash Drawers                             |
|  +--------------------------------------+ |
|  | [+] Add Cash Drawer                  | |
|  +--------------------------------------+ |
|  | Main Drawer        [Active]  [Edit]  | |
|  | Linked to: POS Printer               | |
|  +--------------------------------------+ |
|                                           |
+------------------------------------------+
|      DRAWER SETTINGS                      |
+------------------------------------------+
|                                           |
|  Drawer Name: [Main Drawer____________]   |
|                                           |
|  Linked Printer:                          |
|  [POS Printer______________] [v]          |
|                                           |
|  Drawer Pin:                              |
|  (x) Pin 2 (Standard)                     |
|  ( ) Pin 5 (Alternative)                  |
|                                           |
|  AUTO-OPEN OPTIONS                        |
|  ─────────────────────────────────────    |
|  [x] Open on cash payment                 |
|  [x] Open on cash refund                  |
|  [x] Open on drawer count                 |
|                                           |
|  [Test Drawer]            [Save]          |
+------------------------------------------+
```

### Manual Drawer Open Dialog

```
+------------------------------------------+
|      OPEN CASH DRAWER                     |
+------------------------------------------+
|                                           |
|  [!] Manual drawer access requires        |
|      authorization                        |
|                                           |
|  Reason:                                  |
|  +------------------------------------+   |
|  | ( ) Cash count                     |   |
|  | ( ) Petty cash                     |   |
|  | (x) Other                          |   |
|  +------------------------------------+   |
|                                           |
|  Notes (Required):                        |
|  +------------------------------------+   |
|  | Making change for customer         |   |
|  +------------------------------------+   |
|                                           |
|  Manager PIN (if required):               |
|  [________]                               |
|                                           |
|  [Cancel]                [Open Drawer]    |
+------------------------------------------+
```

### CashDrawerViewModel

```csharp
public partial class CashDrawerViewModel : BaseViewModel
{
    private readonly ICashDrawerService _drawerService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private CashDrawer? _currentDrawer;

    [ObservableProperty]
    private CashDrawerStatus _drawerStatus;

    [ObservableProperty]
    private ObservableCollection<CashDrawerLog> _todayLogs = new();

    public async Task InitializeAsync()
    {
        CurrentDrawer = await _drawerService.GetDefaultDrawerAsync();
        if (CurrentDrawer != null)
        {
            DrawerStatus = await _drawerService.GetStatusAsync(CurrentDrawer.Id);
            await LoadTodayLogsAsync();
        }
    }

    private async Task LoadTodayLogsAsync()
    {
        if (CurrentDrawer == null) return;

        TodayLogs = new ObservableCollection<CashDrawerLog>(
            await _drawerService.GetLogsAsync(CurrentDrawer.Id, DateTime.Today));
    }

    [RelayCommand]
    private async Task OpenDrawerManualAsync()
    {
        if (CurrentDrawer == null) return;

        // Check permission
        var hasPermission = await _permissionService.HasPermissionAsync(
            Permission.CashDrawerManualOpen);

        if (!hasPermission)
        {
            // Request manager override
            var authorized = await RequestManagerOverrideAsync();
            if (!authorized) return;
        }

        // Show reason dialog
        var dialog = new ManualDrawerOpenDialog();
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            var success = await _drawerService.OpenDrawerAsync(
                CurrentDrawer.Id,
                dialog.SelectedReason,
                notes: dialog.Notes);

            if (success)
            {
                DrawerStatus = CashDrawerStatus.Open;
                await LoadTodayLogsAsync();
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    "Error",
                    "Failed to open cash drawer. Check printer connection.");
            }
        }
    }

    private async Task<bool> RequestManagerOverrideAsync()
    {
        var pinDialog = new ManagerPinDialog
        {
            Title = "Manager Authorization Required",
            Message = "Manual drawer access requires manager approval"
        };

        var result = await _dialogService.ShowDialogAsync(pinDialog);

        if (result == true)
        {
            var isValid = await _permissionService.ValidateManagerPinAsync(
                pinDialog.EnteredPin,
                Permission.CashDrawerManualOpen);

            if (isValid) return true;

            await _dialogService.ShowMessageAsync(
                "Invalid PIN",
                "The entered PIN is not valid or does not have the required permission.");
        }

        return false;
    }

    [RelayCommand]
    private async Task TestDrawerAsync()
    {
        if (CurrentDrawer == null) return;

        await _drawerService.TestDrawerAsync(CurrentDrawer.Id);
        DrawerStatus = CashDrawerStatus.Open;
    }
}
```

### Integration with Payment Processing

```csharp
public class PaymentService : IPaymentService
{
    private readonly ICashDrawerService _drawerService;

    public async Task<PaymentResult> ProcessCashPaymentAsync(
        int receiptId,
        decimal amount,
        decimal tendered)
    {
        // ... payment processing logic ...

        // Create payment record
        var payment = new Payment
        {
            ReceiptId = receiptId,
            Method = "Cash",
            Amount = amount,
            Tendered = tendered,
            Change = tendered - amount,
            ProcessedAt = DateTime.UtcNow,
            ProcessedByUserId = _authService.CurrentUser?.Id ?? 0
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Open cash drawer
        await _drawerService.OpenDrawerForPaymentAsync(payment.Id);

        return PaymentResult.Success(payment);
    }
}
```

### Cash Drawer Log Report Print (80mm)

```
================================================
     CASH DRAWER LOG
     2025-12-20
     Main Drawer
================================================

Time  | User     | Reason      | Reference
------|----------|-------------|-------------
08:05 | John     | OpenFloat   | WP-001
09:15 | John     | CashPayment | R-0042
09:32 | John     | CashPayment | R-0043
10:15 | Mary     | ManualOpen  | -
      | Auth: Admin
      | Note: Change for customer
11:00 | John     | CashPayment | R-0048
12:30 | John     | DrawerCount | -
14:15 | Peter    | CashPayment | R-0055
15:00 | Peter    | CashRefund  | R-0042
16:45 | John     | CashPayment | R-0062
18:00 | Mary     | ClosingCount| WP-001
------------------------------------------------
SUMMARY
------------------------------------------------
Total Opens Today:              15
Cash Payments:                   8
Manual Opens:                    2
Drawer Counts:                   2
Opening/Closing:                 2
Refunds:                         1
================================================
```

### Drawer Open Event Permissions

```csharp
public static class Permission
{
    // Cash drawer permissions
    public const string CashDrawerManualOpen = "CashDrawer.ManualOpen";
    public const string CashDrawerViewLogs = "CashDrawer.ViewLogs";
    public const string CashDrawerConfigure = "CashDrawer.Configure";
}

// Default role mappings
public static readonly Dictionary<string, List<string>> CashDrawerPermissions = new()
{
    { "Cashier", new List<string>
        {
            // Cashiers can only open via payments (automatic)
        }
    },
    { "Supervisor", new List<string>
        {
            Permission.CashDrawerManualOpen,
            Permission.CashDrawerViewLogs
        }
    },
    { "Manager", new List<string>
        {
            Permission.CashDrawerManualOpen,
            Permission.CashDrawerViewLogs,
            Permission.CashDrawerConfigure
        }
    },
    { "Admin", new List<string>
        {
            Permission.CashDrawerManualOpen,
            Permission.CashDrawerViewLogs,
            Permission.CashDrawerConfigure
        }
    }
};
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.10.4-Cash-Drawer-Integration]
- [Source: docs/PRD_Hospitality_POS_System.md#PR-031 to PR-040]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created CashDrawer entity with CashDrawerPin and CashDrawerStatus enums
- Created CashDrawerLog entity with CashDrawerOpenReason enum (10 reason types)
- Created EF Core configurations for CashDrawer and CashDrawerLog entities
- Updated POSDbContext with CashDrawers and CashDrawerLogs DbSets
- Implemented comprehensive ICashDrawerService interface with CRUD, open, test, and log methods
- Implemented CashDrawerService using IPrinterCommunicationService for ESC/POS drawer kick commands
- Supports Pin 2 (standard) and Pin 5 (alternative) drawer configurations
- Auto-open on cash payment, refund, and drawer count with configurable settings
- All drawer opens logged with user, reason, reference, notes, and authorization
- Created CashDrawerSettingsViewModel with full CRUD, test, and log viewing functionality
- Created CashDrawerSettingsView.xaml with three-panel layout (drawer list, settings form, activity log)
- Green accent theme (#10B981) for cash drawer UI
- Date navigator for viewing historical drawer logs
- Manual drawer open with reason/notes dialog

### File List
- src/HospitalityPOS.Core/Entities/CashDrawer.cs
- src/HospitalityPOS.Core/Entities/CashDrawerLog.cs
- src/HospitalityPOS.Core/Interfaces/ICashDrawerService.cs (updated)
- src/HospitalityPOS.Infrastructure/Data/Configurations/CashDrawerConfiguration.cs
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (updated)
- src/HospitalityPOS.Infrastructure/Services/CashDrawerService.cs (rewritten)
- src/HospitalityPOS.WPF/ViewModels/CashDrawerSettingsViewModel.cs
- src/HospitalityPOS.WPF/Views/CashDrawerSettingsView.xaml
- src/HospitalityPOS.WPF/Views/CashDrawerSettingsView.xaml.cs
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
