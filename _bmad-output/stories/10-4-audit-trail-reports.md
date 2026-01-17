# Story 10.4: Audit Trail Reports

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/ReportService.cs` - Audit trail reporting with:
  - `GenerateUserActivityReportAsync` - User login/activity logs
  - `GenerateTransactionLogReportAsync` - Transaction audit trail
  - `GenerateVoidRefundLogReportAsync` - Void/refund tracking
  - `GeneratePriceChangeLogReportAsync` - Price modifications
  - `GeneratePermissionOverrideLogReportAsync` - Security overrides

## Story

As an administrator,
I want audit trail reports,
So that all system activities can be reviewed.

## Acceptance Criteria

1. **Given** audit data is being captured
   **When** generating audit reports
   **Then** available reports should include: User Activity Log, Transaction Log, Void/Refund Log, Price Change Log, Permission Override Log

2. **Given** report parameters
   **When** filtering
   **Then** reports can be filtered by user, date range, action type

3. **Given** audit data
   **When** viewing report
   **Then** reports show: timestamp, user, action, before/after values

## Tasks / Subtasks

- [ ] Task 1: Create Audit Trail Entity
  - [ ] Create AuditLog entity
  - [ ] Configure EF Core mappings
  - [ ] Create database migration
  - [ ] Setup audit log interceptor

- [ ] Task 2: Create Audit Reports Screen
  - [ ] Create AuditReportsView.xaml
  - [ ] Create AuditReportsViewModel
  - [ ] Report type selection
  - [ ] Advanced filters

- [ ] Task 3: Implement User Activity Log
  - [ ] Track login/logout
  - [ ] Track major actions
  - [ ] Show session info
  - [ ] Filter by user

- [ ] Task 4: Implement Transaction Logs
  - [ ] Void/Refund log
  - [ ] Price change log
  - [ ] Override log
  - [ ] Show before/after

- [ ] Task 5: Implement Log Printing
  - [ ] Format for 80mm paper
  - [ ] Handle large logs
  - [ ] Export to CSV
  - [ ] Archival support

## Dev Notes

### AuditLog Entity

```csharp
public class AuditLog
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }  // JSON
    public string? NewValues { get; set; }  // JSON
    public string? AdditionalData { get; set; }  // JSON
    public int UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? MachineName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
```

### Audit Actions

```csharp
public static class AuditAction
{
    // Authentication
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    public const string PasswordChanged = "PasswordChanged";

    // Sales
    public const string OrderCreated = "OrderCreated";
    public const string ReceiptSettled = "ReceiptSettled";
    public const string ReceiptVoid = "ReceiptVoid";
    public const string DiscountApplied = "DiscountApplied";

    // Inventory
    public const string StockAdjustment = "StockAdjustment";
    public const string StockReceived = "StockReceived";
    public const string StockTakeApproved = "StockTakeApproved";

    // Admin
    public const string PriceChanged = "PriceChanged";
    public const string UserCreated = "UserCreated";
    public const string PermissionOverride = "PermissionOverride";
    public const string CashDrawerOpen = "CashDrawerOpen";
}

public static class AuditCategory
{
    public const string Authentication = "Authentication";
    public const string Sales = "Sales";
    public const string Inventory = "Inventory";
    public const string Admin = "Admin";
    public const string Security = "Security";
}
```

### Audit Reports Screen

```
+------------------------------------------+
|      AUDIT TRAIL REPORTS                  |
+------------------------------------------+
| Report: [User Activity________] [v]       |
| From: [2025-12-20]  To: [2025-12-20]      |
| User: [All Users____________] [v]         |
| Action: [All Actions________] [v]         |
| [Generate Report]                         |
+------------------------------------------+
|                                           |
|     USER ACTIVITY LOG                     |
|     2025-12-20                            |
|                                           |
|  +------------------------------------+   |
|  | 08:00 | John Smith                 |   |
|  | Login | Session started            |   |
|  +------------------------------------+   |
|  | 08:15 | John Smith                 |   |
|  | OrderCreated | Order O-0042        |   |
|  +------------------------------------+   |
|  | 09:30 | John Smith                 |   |
|  | ReceiptSettled | R-0042 KSh 2,262  |   |
|  +------------------------------------+   |
|  | 10:00 | Mary Johnson               |   |
|  | PermissionOverride | Void R-0042   |   |
|  | Auth for: John Smith               |   |
|  +------------------------------------+   |
|                                           |
|  Total Actions: 45                        |
|                                           |
+------------------------------------------+
```

### AuditReportsViewModel

```csharp
public partial class AuditReportsViewModel : BaseViewModel
{
    [ObservableProperty]
    private AuditReportType _selectedReportType;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string? _selectedAction;

    [ObservableProperty]
    private ObservableCollection<AuditLogItem> _auditLogs = new();

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var query = _context.AuditLogs
            .Where(a => a.CreatedAt >= FromDate && a.CreatedAt < ToDate.AddDays(1))
            .Include(a => a.User)
            .AsQueryable();

        // Apply filters based on report type
        query = SelectedReportType switch
        {
            AuditReportType.UserActivity =>
                query.Where(a => a.Category == AuditCategory.Authentication ||
                                a.Category == AuditCategory.Sales),
            AuditReportType.TransactionLog =>
                query.Where(a => a.Category == AuditCategory.Sales),
            AuditReportType.VoidRefundLog =>
                query.Where(a => a.Action == AuditAction.ReceiptVoid),
            AuditReportType.PriceChangeLog =>
                query.Where(a => a.Action == AuditAction.PriceChanged),
            AuditReportType.PermissionOverrideLog =>
                query.Where(a => a.Action == AuditAction.PermissionOverride),
            _ => query
        };

        if (SelectedUser != null)
        {
            query = query.Where(a => a.UserId == SelectedUser.Id);
        }

        if (!string.IsNullOrEmpty(SelectedAction))
        {
            query = query.Where(a => a.Action == SelectedAction);
        }

        AuditLogs = new ObservableCollection<AuditLogItem>(
            await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AuditLogItem
                {
                    Timestamp = a.CreatedAt,
                    UserName = a.User.FullName,
                    Action = a.Action,
                    Description = a.Description,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues
                })
                .Take(1000)
                .ToListAsync());
    }
}
```

### Audit Service

```csharp
public interface IAuditService
{
    Task LogAsync(string action, string description, Dictionary<string, object>? additionalData = null);
    Task LogChangeAsync<T>(string action, T oldEntity, T newEntity, string description);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public async Task LogAsync(
        string action,
        string description,
        Dictionary<string, object>? additionalData = null)
    {
        var log = new AuditLog
        {
            Action = action,
            Category = GetCategory(action),
            Description = description,
            AdditionalData = additionalData != null
                ? JsonSerializer.Serialize(additionalData)
                : null,
            UserId = _authService.CurrentUser?.Id ?? 0,
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogChangeAsync<T>(
        string action,
        T oldEntity,
        T newEntity,
        string description)
    {
        var log = new AuditLog
        {
            Action = action,
            Category = GetCategory(action),
            EntityType = typeof(T).Name,
            Description = description,
            OldValues = JsonSerializer.Serialize(oldEntity),
            NewValues = JsonSerializer.Serialize(newEntity),
            UserId = _authService.CurrentUser?.Id ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private string GetCategory(string action)
    {
        return action switch
        {
            _ when action.StartsWith("Login") || action.StartsWith("Logout") => AuditCategory.Authentication,
            _ when action.Contains("Stock") || action.Contains("Inventory") => AuditCategory.Inventory,
            _ when action.Contains("Receipt") || action.Contains("Order") => AuditCategory.Sales,
            _ when action.Contains("Permission") || action.Contains("Override") => AuditCategory.Security,
            _ => AuditCategory.Admin
        };
    }
}
```

### Permission Override Log Print (80mm)

```
================================================
     PERMISSION OVERRIDE LOG
     2025-12-20
================================================

Time  | Action           | Users
------|------------------|----------------------
10:15 | Void Receipt     |
      | R-0042           |
      | Requested: John  |
      | Auth: Mary (Mgr) |
      | Reason: Wrong order
------|------------------|----------------------
14:30 | Apply 20% Disc   |
      | R-0055           |
      | Requested: Peter |
      | Auth: Mary (Mgr) |
      | Reason: VIP customer
------|------------------|----------------------
16:45 | Price Change     |
      | Tusker Lager     |
      | Old: 350  New: 380
      | Changed: John    |
      | Auth: Admin      |
================================================
Total Overrides: 12
================================================
```

### Price Change Log Print (80mm)

```
================================================
     PRICE CHANGE LOG
     2025-12-20
================================================

Time  | Product          | Change
------|------------------|----------------------
09:00 | Tusker Lager     |
      | Old: KSh 350     |
      | New: KSh 380     |
      | By: John (Admin) |
------|------------------|----------------------
09:05 | Coca Cola 500ml  |
      | Old: KSh 50      |
      | New: KSh 60      |
      | By: John (Admin) |
------|------------------|----------------------
14:00 | Grilled Chicken  |
      | Old: KSh 800     |
      | New: KSh 850     |
      | By: Mary (Mgr)   |
================================================
Total Price Changes: 8
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.7.4-Audit-Reports]
- [Source: docs/PRD_Hospitality_POS_System.md#RP-035 to RP-040]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
