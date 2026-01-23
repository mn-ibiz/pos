# feat: Add Auto-Generate PO System Configuration Settings

**Labels:** `enhancement` `backend` `settings` `priority-high`

## Overview

Add system configuration settings to control the behavior of automatic purchase order generation. These settings allow administrators to configure whether POs are auto-generated, auto-sent, and set approval thresholds.

## Background

The system already has a `SystemConfiguration` entity and `ISystemConfigurationService`. This feature extends those to include PO-related settings.

## Requirements

### New Configuration Properties

Add the following properties to `SystemConfiguration` entity or create a new `PurchaseOrderSettings` table:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AutoGeneratePurchaseOrders` | bool | false | Enable automatic PO generation from reorder suggestions |
| `AutoSendPurchaseOrders` | bool | false | Auto-send POs to suppliers (vs keep as draft) |
| `AutoApprovalThreshold` | decimal | 0 | Auto-approve POs under this amount (0 = always require approval) |
| `RequireManagerApproval` | bool | true | Require manager approval for all POs |
| `StockCheckIntervalMinutes` | int | 15 | How often to check stock levels |
| `NotifyOnLowStock` | bool | true | Send notification when stock is low |
| `NotifyOnPOGenerated` | bool | true | Send notification when PO is generated |
| `NotifyOnPOSent` | bool | true | Send notification when PO is sent |
| `LowStockThresholdDays` | int | 7 | Warn when stock covers fewer than X days |
| `DefaultLeadTimeDays` | int | 3 | Default supplier lead time if not specified |
| `ConsolidatePOsBySupplier` | bool | true | Combine multiple items into single PO per supplier |
| `MinimumPOAmount` | decimal | 0 | Don't generate POs below this amount |
| `MaxItemsPerPO` | int | 50 | Maximum items per purchase order |

### Entity Changes

**Option A: Extend SystemConfiguration**
```csharp
public class SystemConfiguration
{
    // Existing properties...

    // PO Auto-Generation Settings
    public bool AutoGeneratePurchaseOrders { get; set; } = false;
    public bool AutoSendPurchaseOrders { get; set; } = false;
    public decimal AutoApprovalThreshold { get; set; } = 0;
    public bool RequireManagerApproval { get; set; } = true;
    public int StockCheckIntervalMinutes { get; set; } = 15;

    // Notification Settings
    public bool NotifyOnLowStock { get; set; } = true;
    public bool NotifyOnPOGenerated { get; set; } = true;
    public bool NotifyOnPOSent { get; set; } = true;

    // Reorder Settings
    public int LowStockThresholdDays { get; set; } = 7;
    public int DefaultLeadTimeDays { get; set; } = 3;
    public bool ConsolidatePOsBySupplier { get; set; } = true;
    public decimal MinimumPOAmount { get; set; } = 0;
    public int MaxItemsPerPO { get; set; } = 50;
}
```

**Option B: Separate PurchaseOrderSettings Entity** (Recommended for cleaner separation)
```csharp
public class PurchaseOrderSettings
{
    public int Id { get; set; }

    // Auto-Generation
    public bool AutoGeneratePurchaseOrders { get; set; } = false;
    public bool AutoSendPurchaseOrders { get; set; } = false;
    public decimal AutoApprovalThreshold { get; set; } = 0;
    public bool RequireManagerApproval { get; set; } = true;
    public int StockCheckIntervalMinutes { get; set; } = 15;

    // Notifications
    public bool NotifyOnLowStock { get; set; } = true;
    public bool NotifyOnPOGenerated { get; set; } = true;
    public bool NotifyOnPOSent { get; set; } = true;
    public bool SendDailyPendingPODigest { get; set; } = true;
    public TimeSpan DigestSendTime { get; set; } = new TimeSpan(8, 0, 0); // 8:00 AM

    // Reorder Logic
    public int LowStockThresholdDays { get; set; } = 7;
    public int DefaultLeadTimeDays { get; set; } = 3;
    public bool ConsolidatePOsBySupplier { get; set; } = true;
    public decimal MinimumPOAmount { get; set; } = 0;
    public int MaxItemsPerPO { get; set; } = 50;

    // Audit
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
```

### Service Interface

```csharp
public interface IPurchaseOrderSettingsService
{
    Task<PurchaseOrderSettings> GetSettingsAsync();
    Task SaveSettingsAsync(PurchaseOrderSettings settings, int userId);
    Task<bool> ShouldAutoGeneratePOsAsync();
    Task<bool> ShouldAutoSendPOAsync(decimal poTotal);
    Task<bool> RequiresApprovalAsync(decimal poTotal);
}
```

### Settings UI

Create a settings panel in the application (could be a tab in existing Settings view or separate view):

```
+----------------------------------------------------------+
| PURCHASE ORDER SETTINGS                                   |
+----------------------------------------------------------+
|                                                          |
| [ ] Enable automatic PO generation                       |
|     When enabled, the system will automatically          |
|     generate POs when stock falls below reorder point    |
|                                                          |
| Stock Check Interval: [15] minutes                       |
|                                                          |
| --- Auto-Send Options ---                                |
| ( ) Keep all POs as draft for review                     |
| (x) Auto-send POs below threshold                        |
|     Threshold: [$500.00]                                 |
| ( ) Auto-send all POs                                    |
|                                                          |
| [ ] Require manager approval for all POs                 |
|                                                          |
| --- Consolidation ---                                    |
| [x] Combine items by supplier (recommended)              |
| Maximum items per PO: [50]                               |
| Minimum PO amount: [$0.00]                               |
|                                                          |
| --- Notifications ---                                    |
| [x] Notify when stock is low                             |
| [x] Notify when PO is generated                          |
| [x] Notify when PO is sent to supplier                   |
| [x] Send daily pending PO digest                         |
|     Send time: [08:00 AM]                                |
|                                                          |
| [Save Settings]                                          |
+----------------------------------------------------------+
```

## Acceptance Criteria

### Database
- [ ] Migration created for new settings table/columns
- [ ] Default values set appropriately
- [ ] Settings load on application startup

### Service Layer
- [ ] `IPurchaseOrderSettingsService` implemented
- [ ] Settings cached for performance (invalidate on save)
- [ ] Audit trail for setting changes (who changed what, when)

### UI
- [ ] Settings panel accessible from main Settings view
- [ ] All settings editable with appropriate controls
- [ ] Validation for numeric fields (no negative intervals, etc.)
- [ ] Save button persists changes
- [ ] Changes take effect immediately (no restart required)
- [ ] Only users with appropriate role can modify settings

### Integration
- [ ] `StockMonitoringJob` reads `StockCheckIntervalMinutes` setting
- [ ] `StockMonitoringJob` respects `AutoGeneratePurchaseOrders` setting
- [ ] PO creation logic respects `AutoSendPurchaseOrders` setting
- [ ] PO creation logic respects `AutoApprovalThreshold` setting
- [ ] PO creation logic respects `ConsolidatePOsBySupplier` setting
- [ ] Notification service reads notification settings

### Validation Rules
- [ ] `StockCheckIntervalMinutes` must be >= 5 and <= 1440 (24 hours)
- [ ] `AutoApprovalThreshold` must be >= 0
- [ ] `LowStockThresholdDays` must be >= 1 and <= 90
- [ ] `DefaultLeadTimeDays` must be >= 1 and <= 90
- [ ] `MaxItemsPerPO` must be >= 1 and <= 100
- [ ] `MinimumPOAmount` must be >= 0

## Technical Notes

### Settings Caching

```csharp
public class PurchaseOrderSettingsService : IPurchaseOrderSettingsService
{
    private PurchaseOrderSettings? _cachedSettings;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<PurchaseOrderSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        await _lock.WaitAsync();
        try
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            _cachedSettings = await _context.PurchaseOrderSettings.FirstOrDefaultAsync()
                ?? new PurchaseOrderSettings();

            return _cachedSettings;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSettingsAsync(PurchaseOrderSettings settings, int userId)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = userId;

        // Save to database...

        _cachedSettings = null; // Invalidate cache
    }
}
```

### Role-Based Access

Only users with `Admin` or `Manager` role should be able to modify these settings. Regular users can view but not edit.

## Test Cases

1. **Default values** - New installation has sensible defaults
2. **Save and reload** - Changes persist across application restarts
3. **Cache invalidation** - Changes take effect immediately
4. **Validation** - Invalid values are rejected with clear error messages
5. **Role check** - Non-admin users cannot save changes
6. **Audit trail** - Changes are logged with user ID and timestamp

## Dependencies
- None

## Blocked By
- None

## Blocks
- Issue #002: Stock Monitoring Background Service (uses these settings)
- Issue #004: PO Consolidation by Supplier (uses ConsolidatePOsBySupplier)
- Issue #005: Notification Service (uses notification settings)

## Estimated Complexity
**Low-Medium** - Standard CRUD with caching and validation
