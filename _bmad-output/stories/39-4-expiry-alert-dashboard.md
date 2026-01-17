# Story 39.4: Expiry Alert Dashboard and POS Blocking

Status: Done

## Story

As a **store manager**,
I want **a dashboard showing products approaching expiry and automatic blocking of expired items at checkout**,
so that **I can proactively manage expiring stock, prevent sale of expired goods, and minimize waste**.

## Business Context

**CRITICAL - FOOD SAFETY**

Even with batch/expiry data stored, the system needs active monitoring and enforcement:
- Proactive alerts before products expire
- Blocking mechanism at point of sale
- Reporting for waste management
- Compliance with food safety regulations

**Legal Risk:** Selling expired products can result in fines, lawsuits, and business closure.

**Market Reality:** DigitalPOS, Smartwas, Uzalynx all have expiry alerts and blocking.

## Acceptance Criteria

### AC1: Expiry Alert Dashboard
- [ ] Dashboard widget shows products expiring within configured thresholds
- [ ] Three alert levels: 30 days (yellow), 14 days (orange), 7 days (red)
- [ ] Grouped by urgency level
- [ ] Click to view affected products and quantities
- [ ] Shows batch number, product name, expiry date, quantity

### AC2: Configurable Alert Thresholds
- [ ] System-wide default thresholds (7, 14, 30 days)
- [ ] Category-specific override thresholds
- [ ] Product-specific override thresholds (for short-shelf-life items)

### AC3: POS Expiry Warning
- [ ] Warning icon displayed when scanning soon-to-expire item
- [ ] Toast notification: "Item expires in X days"
- [ ] Warning does not block sale

### AC4: POS Expired Item Blocking
- [ ] Hard block when scanning expired item
- [ ] Clear message: "Cannot sell - Item expired on [DATE]"
- [ ] Block is enforced - sale cannot proceed without override

### AC5: Manager Override
- [ ] Manager can override with PIN
- [ ] Reason required for override
- [ ] Override logged with: user, reason, timestamp, batch, product
- [ ] Override permission configurable by role

### AC6: Auto-Expire Batches
- [ ] Background job marks batches as "Expired" when ExpiryDate passes
- [ ] Runs daily at midnight
- [ ] Notification to managers when batches expire

### AC7: Expiry Waste Report
- [ ] Report shows expired items with write-off value
- [ ] Filter by date range, category, product
- [ ] Shows: Product, Batch, ExpiryDate, Quantity, UnitCost, TotalValue
- [ ] Summary: Total units expired, Total value lost
- [ ] Export to Excel

## Tasks / Subtasks

- [ ] **Task 1: Expiry Alert Settings** (AC: 2)
  - [ ] 1.1 Create ExpiryAlertSettings table (CategoryId, WarningDays1/2/3, BlockSaleOnExpiry, AllowOverride)
  - [ ] 1.2 Create default system settings (7, 14, 30 days)
  - [ ] 1.3 Create settings UI for category overrides
  - [ ] 1.4 Migration for ExpiryAlertSettings

- [ ] **Task 2: Expiry Alert Dashboard Widget** (AC: 1)
  - [ ] 2.1 Create ExpiryAlertService with GetExpiringProducts(days) method
  - [ ] 2.2 Create ExpiryAlertViewModel
  - [ ] 2.3 Create ExpiryAlertWidget.xaml for dashboard
  - [ ] 2.4 Color-code by urgency (red/orange/yellow)
  - [ ] 2.5 Click action to navigate to full report
  - [ ] 2.6 Auto-refresh every 5 minutes

- [ ] **Task 3: POS Warning Integration** (AC: 3)
  - [ ] 3.1 Modify product lookup to check batch expiry
  - [ ] 3.2 Calculate days until expiry
  - [ ] 3.3 Display warning icon on product tile if expiring soon
  - [ ] 3.4 Show toast notification when adding to order
  - [ ] 3.5 Get threshold from product/category/system settings (cascade)

- [ ] **Task 4: POS Blocking Integration** (AC: 4, 5)
  - [ ] 4.1 Check if any active batch is expired on product add
  - [ ] 4.2 Block sale with modal dialog: "Item Expired"
  - [ ] 4.3 Implement override flow with PIN entry
  - [ ] 4.4 Create ExpiredItemOverrides table for audit
  - [ ] 4.5 Require reason selection or text entry
  - [ ] 4.6 Log override to audit table
  - [ ] 4.7 Check permission: CanOverrideExpiredSale
  - [ ] 4.8 Unit tests for blocking logic

- [ ] **Task 5: Auto-Expire Background Job** (AC: 6)
  - [ ] 5.1 Create ExpireBatchesJob background service
  - [ ] 5.2 Query batches where ExpiryDate < Today AND Status = 'Active'
  - [ ] 5.3 Update Status to 'Expired'
  - [ ] 5.4 Record in audit log
  - [ ] 5.5 Generate notification for managers
  - [ ] 5.6 Schedule to run daily at midnight
  - [ ] 5.7 Unit tests for job logic

- [ ] **Task 6: Expiry Waste Report** (AC: 7)
  - [ ] 6.1 Add GetExpiredBatchesReport to ReportingService
  - [ ] 6.2 Create ExpiredStockReportView.xaml
  - [ ] 6.3 Create ExpiredStockReportViewModel
  - [ ] 6.4 Add date range filter, category filter
  - [ ] 6.5 Calculate write-off value (Quantity * UnitCost)
  - [ ] 6.6 Add summary totals
  - [ ] 6.7 Export to Excel

- [ ] **Task 7: Near-Expiry Discount (Optional)** (AC: 1)
  - [ ] 7.1 Add "Auto-Discount Near Expiry" setting
  - [ ] 7.2 Configure discount percentage per threshold (e.g., 7 days = 30% off)
  - [ ] 7.3 Auto-apply discount on checkout for qualifying items
  - [ ] 7.4 Display "Expiry Discount" on receipt

## Dev Notes

### Expiry Check Flow at POS

```
[Cashier scans barcode]
    ↓
[Get product with batch info]
    ↓
[Check earliest-expiring batch]
    ↓
[If ExpiryDate < Today]
    → BLOCK: "Cannot sell - Item expired"
    → [Manager Override with PIN?]
        → Yes: Log override, proceed
        → No: Remove item from order
    ↓
[If ExpiryDate within warning threshold]
    → WARN: Show warning icon + toast
    → Continue (sale allowed)
    ↓
[Add to order normally]
```

### Database Schema

```sql
CREATE TABLE ExpiryAlertSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id), -- NULL = system default
    WarningDays1 INT DEFAULT 30, -- Yellow
    WarningDays2 INT DEFAULT 14, -- Orange
    WarningDays3 INT DEFAULT 7,  -- Red
    BlockSaleOnExpiry BIT DEFAULT 1,
    AllowOverrideWithPin BIT DEFAULT 1,
    AutoDiscountPercent DECIMAL(5,2), -- Optional discount for near-expiry
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE ExpiredItemOverrides (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductBatchId INT FOREIGN KEY REFERENCES ProductBatches(Id),
    OrderItemId INT FOREIGN KEY REFERENCES OrderItems(Id),
    OverrideByUserId INT FOREIGN KEY REFERENCES Users(Id),
    Reason NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### Threshold Cascade Logic

```csharp
public int GetWarningThreshold(Product product)
{
    // Product-level override first
    if (product.ExpiryWarningDays.HasValue)
        return product.ExpiryWarningDays.Value;

    // Category-level override
    var categorySetting = _expirySettings.FirstOrDefault(s => s.CategoryId == product.CategoryId);
    if (categorySetting != null)
        return categorySetting.WarningDays3; // Use most urgent threshold

    // System default
    var systemSetting = _expirySettings.FirstOrDefault(s => s.CategoryId == null);
    return systemSetting?.WarningDays3 ?? 7;
}
```

### Architecture Compliance

- **Layer:** Business (ExpiryAlertService), WPF (Dashboard, POS integration)
- **Background Job:** Use Timer or HostedService for daily expiry job
- **Permissions:** Add CanOverrideExpiredSale permission to RBAC

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.4-Expiry-Alert-Dashboard-and-Blocking]
- [Source: _bmad-output/stories/39-3-batch-expiry-tracking.md] (dependency)
- [Source: _bmad-output/architecture.md#Security-RBAC]

### Dependencies

- **Requires:** Story 39-3 (Batch and Expiry Tracking) must be completed first

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
