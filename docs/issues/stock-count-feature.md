## Overview

Implement a comprehensive Stock Count (Physical Inventory) system that enables staff to perform physical counts, reconcile discrepancies with system records, automatically adjust inventory levels, and generate detailed variance/discrepancy reports for audit and loss prevention.

## Background Research

### What is Stock Count/Inventory Reconciliation?
- **Definition**: The process of comparing recorded inventory levels in the POS/inventory system with actual physical stock on hand
- **Purpose**: Identify discrepancies caused by theft, damage, administrative errors, supplier fraud, or system issues
- **Outcome**: Adjust system records to match physical reality and document variances for analysis

### Key Concepts
- **Theoretical Stock**: Expected quantity based on purchases, sales, waste, and transfers
- **Actual Stock**: Physical count result
- **Variance**: Difference between theoretical and actual (can be positive overage or negative shrinkage)
- **Shrinkage**: Loss of inventory due to theft, damage, or errors - calculated as: `(Recorded Inventory - Physical Count) / Recorded Inventory`

### Best Practices from Research
1. **Double-blind counting**: Two employees count separately, compare results
2. **Freeze inventory**: Lock system during count to prevent transactions
3. **Regular cycle counts**: Frequent partial counts vs. annual full counts
4. **Root cause analysis**: Categorize variances by cause (theft, damage, admin error, etc.)
5. **Variance thresholds**: Set acceptable limits, investigate beyond threshold

## Current State Analysis

The codebase has existing infrastructure:
- `StockTake` and `StockTakeItem` entities exist
- `Inventory` entity tracks current stock per product/store
- `StockMovement` entity with `MovementType.StockTake` enum
- `IInventoryService` for stock operations
- `IInventoryAnalyticsService` for reporting

## Requirements

### 1. Stock Count Workflow

```
STOCK COUNT LIFECYCLE

1. INITIATION
   ├── Create Stock Count session
   ├── Select scope (Full inventory / Category / Location / ABC class)
   ├── Select count method (Single count / Double-blind / Cycle count)
   ├── Generate count sheets/worksheets
   └── Optionally freeze inventory transactions

2. COUNTING
   ├── Enter physical counts by item
   ├── Support barcode scanning for item lookup
   ├── Support batch entry from handheld devices
   ├── Track count progress (items counted / total)
   ├── Allow re-counts for specific items
   └── Double-blind: Second counter enters independently

3. REVIEW
   ├── Calculate variances (system vs counted)
   ├── Highlight significant variances
   ├── Double-blind: Compare counter results, flag mismatches
   ├── Manager review and approval
   └── Require explanation for large variances

4. ADJUSTMENT
   ├── Post inventory adjustments
   ├── Create stock movement records
   ├── Update inventory quantities
   ├── Generate journal entries (if accounting active)
   └── Unfreeze inventory transactions

5. REPORTING
   ├── Variance Report (detailed by item)
   ├── Shrinkage Analysis
   ├── Adjustment Summary
   ├── Historical Comparison
   └── Audit Trail
```

### 2. Core Features

#### 2.1 Stock Count Session Management
- [ ] Create new stock count session with configurable scope
- [ ] Support multiple count types:
  - **Full Count**: All inventory items
  - **Cycle Count**: Subset of items (by category, value, velocity)
  - **Spot Count**: Specific items only
- [ ] Set count frequency schedules
- [ ] Track session status (Draft, InProgress, PendingReview, Approved, Posted)

#### 2.2 Count Entry Interface
- [ ] Mobile-friendly count entry screen
- [ ] Barcode scanner integration for item lookup
- [ ] Batch import from CSV/Excel
- [ ] Show item details (name, SKU, location, UOM)
- [ ] Optionally hide system quantity (blind count)
- [ ] Support multiple units of measure
- [ ] Allow notes/comments per item
- [ ] Track who counted each item and when

#### 2.3 Double-Blind Counting
- [ ] Assign multiple counters to same session
- [ ] Each counter enters independently (cannot see others)
- [ ] System compares counts after both complete
- [ ] Flag mismatches exceeding threshold
- [ ] Require re-count or manager resolution for mismatches

#### 2.4 Variance Calculation & Analysis
- [ ] Calculate variance: `System Quantity - Counted Quantity`
- [ ] Calculate variance value: `Variance Qty x Cost Price`
- [ ] Calculate shrinkage percentage: `Variance Value / Total Inventory Value`
- [ ] Categorize variances by cause:
  - Theft/Pilferage
  - Damage/Spoilage
  - Administrative Error
  - Receiving Error
  - System Error
  - Unknown
- [ ] Set variance thresholds (by percentage or absolute value)
- [ ] Auto-flag items exceeding threshold

#### 2.5 Adjustment Posting
- [ ] Preview adjustments before posting
- [ ] Require approval for large adjustments
- [ ] Post adjustments to inventory
- [ ] Create StockMovement records with MovementType.StockTake
- [ ] Generate accounting journal entries:
  - DR: Inventory Shrinkage Expense (or specific expense account)
  - CR: Inventory Asset
- [ ] Track adjustment audit trail

#### 2.6 Reporting Suite
- [ ] **Variance Report**: Detailed item-by-item variance analysis
- [ ] **Shrinkage Report**: Summary of losses by category, cause, location
- [ ] **Adjustment Report**: List of all adjustments posted
- [ ] **Historical Trend**: Shrinkage trends over time
- [ ] **Counter Performance**: Accuracy by counter (for double-blind)
- [ ] **Exception Report**: Items with recurring variances

### 3. Database Schema Changes

```csharp
public enum StockCountStatus
{
    Draft,
    InProgress,
    CountingComplete,
    PendingReview,
    Approved,
    Posted,
    Cancelled
}

public enum StockCountType
{
    FullCount,
    CycleCount,
    SpotCount,
    CategoryCount
}

public enum VarianceCause
{
    Unknown,
    Theft,
    Damage,
    Spoilage,
    AdminError,
    ReceivingError,
    SystemError,
    Transfer,
    Sampling
}

public class StockCount : BaseEntity
{
    public string CountNumber { get; set; } // Auto-generated: SC-2024-001
    public int StoreId { get; set; }
    public StockCountType CountType { get; set; }
    public StockCountStatus Status { get; set; }
    public DateTime CountDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    // Scope
    public int? CategoryId { get; set; } // If category count
    public string LocationFilter { get; set; } // Storage location filter
    public bool IsBlindCount { get; set; } // Hide system quantities
    public bool IsDoubleBlind { get; set; } // Require two counters

    // Freeze
    public bool FreezeInventory { get; set; }
    public DateTime? FrozenAt { get; set; }
    public DateTime? UnfrozenAt { get; set; }

    // Summary (calculated after counting)
    public int TotalItemsToCount { get; set; }
    public int ItemsCounted { get; set; }
    public decimal TotalSystemValue { get; set; }
    public decimal TotalCountedValue { get; set; }
    public decimal TotalVarianceValue { get; set; }
    public decimal ShrinkagePercentage { get; set; }

    // Approval
    public int? ApprovedByUserId { get; set; }
    public string ApprovalNotes { get; set; }

    // Navigation
    public virtual Store Store { get; set; }
    public virtual Category Category { get; set; }
    public virtual User ApprovedByUser { get; set; }
    public virtual ICollection<StockCountItem> Items { get; set; }
    public virtual ICollection<StockCountCounter> Counters { get; set; }
}

public class StockCountItem : BaseEntity
{
    public int StockCountId { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; }
    public string ProductName { get; set; }
    public string Location { get; set; }

    // System values (frozen at count start)
    public decimal SystemQuantity { get; set; }
    public decimal SystemCostPrice { get; set; }
    public decimal SystemValue { get; set; }

    // Counted values
    public decimal? CountedQuantity { get; set; }
    public decimal? CountedValue { get; set; }
    public int? CountedByUserId { get; set; }
    public DateTime? CountedAt { get; set; }

    // Second count (for double-blind)
    public decimal? SecondCountQuantity { get; set; }
    public int? SecondCountedByUserId { get; set; }
    public DateTime? SecondCountedAt { get; set; }
    public bool CountMismatch { get; set; }

    // Variance
    public decimal VarianceQuantity { get; set; }
    public decimal VarianceValue { get; set; }
    public decimal VariancePercentage { get; set; }
    public bool ExceedsThreshold { get; set; }

    // Resolution
    public VarianceCause? VarianceCause { get; set; }
    public string VarianceNotes { get; set; }
    public bool AdjustmentPosted { get; set; }
    public int? StockMovementId { get; set; }

    // Navigation
    public virtual StockCount StockCount { get; set; }
    public virtual Product Product { get; set; }
    public virtual User CountedByUser { get; set; }
    public virtual User SecondCountedByUser { get; set; }
    public virtual StockMovement StockMovement { get; set; }
}

public class StockCountCounter : BaseEntity
{
    public int StockCountId { get; set; }
    public int UserId { get; set; }
    public bool IsPrimaryCounter { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ItemsCounted { get; set; }

    public virtual StockCount StockCount { get; set; }
    public virtual User User { get; set; }
}

public class StockCountSchedule : BaseEntity
{
    public int StoreId { get; set; }
    public StockCountType CountType { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int? DayOfWeek { get; set; } // For weekly
    public int? DayOfMonth { get; set; } // For monthly
    public int? CategoryId { get; set; } // For cycle counts
    public bool IsEnabled { get; set; }
    public DateTime? LastRunDate { get; set; }
    public DateTime? NextRunDate { get; set; }
    public bool SendReminder { get; set; }
    public int ReminderDaysBefore { get; set; }
}

public class VarianceThreshold : BaseEntity
{
    public int StoreId { get; set; }
    public int? CategoryId { get; set; } // Null = all categories
    public decimal? QuantityThreshold { get; set; } // Absolute quantity
    public decimal? PercentageThreshold { get; set; } // Percentage of system qty
    public decimal? ValueThreshold { get; set; } // Dollar value
    public bool RequireApproval { get; set; }
    public bool RequireExplanation { get; set; }
}
```

### 4. Service Interface

```csharp
public interface IStockCountService
{
    // Session Management
    Task<StockCount> CreateStockCountAsync(CreateStockCountDto dto, CancellationToken ct = default);
    Task<StockCount> GetStockCountAsync(int stockCountId, CancellationToken ct = default);
    Task<IEnumerable<StockCount>> GetStockCountsAsync(StockCountFilterDto filter, CancellationToken ct = default);
    Task<StockCount> StartCountingAsync(int stockCountId, CancellationToken ct = default);
    Task<StockCount> CancelStockCountAsync(int stockCountId, string reason, CancellationToken ct = default);

    // Counting
    Task<StockCountItem> RecordCountAsync(int stockCountId, int productId, decimal countedQuantity, int userId, string notes = null, CancellationToken ct = default);
    Task<IEnumerable<StockCountItem>> RecordBatchCountAsync(int stockCountId, IEnumerable<CountEntryDto> entries, int userId, CancellationToken ct = default);
    Task<StockCountItem> RecordSecondCountAsync(int stockCountId, int productId, decimal countedQuantity, int userId, CancellationToken ct = default);
    Task CompleteCountingAsync(int stockCountId, CancellationToken ct = default);

    // Variance Management
    Task CalculateVariancesAsync(int stockCountId, CancellationToken ct = default);
    Task<StockCountItem> SetVarianceCauseAsync(int stockCountItemId, VarianceCause cause, string notes, CancellationToken ct = default);
    Task<IEnumerable<StockCountItem>> GetVarianceItemsAsync(int stockCountId, bool exceedsThresholdOnly = false, CancellationToken ct = default);

    // Approval & Posting
    Task<StockCount> SubmitForApprovalAsync(int stockCountId, CancellationToken ct = default);
    Task<StockCount> ApproveStockCountAsync(int stockCountId, int approverUserId, string notes, CancellationToken ct = default);
    Task<StockCount> RejectStockCountAsync(int stockCountId, int approverUserId, string reason, CancellationToken ct = default);
    Task<StockCount> PostAdjustmentsAsync(int stockCountId, CancellationToken ct = default);

    // Reporting
    Task<StockCountVarianceReport> GetVarianceReportAsync(int stockCountId, CancellationToken ct = default);
    Task<ShrinkageAnalysisReport> GetShrinkageAnalysisAsync(ShrinkageReportFilterDto filter, CancellationToken ct = default);
    Task<HistoricalVarianceReport> GetHistoricalVarianceAsync(int storeId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Export
    Task<byte[]> ExportCountSheetAsync(int stockCountId, CancellationToken ct = default);
    Task<byte[]> ExportVarianceReportAsync(int stockCountId, ExportFormat format, CancellationToken ct = default);

    // Scheduling
    Task<StockCountSchedule> GetScheduleAsync(int storeId, CancellationToken ct = default);
    Task<StockCountSchedule> UpdateScheduleAsync(StockCountScheduleDto dto, CancellationToken ct = default);
    Task TriggerScheduledCountsAsync(CancellationToken ct = default);
}
```

### 5. Variance Report Structure

```
STOCK COUNT VARIANCE REPORT
===========================

HEADER
├── Report Date
├── Stock Count Number
├── Count Type (Full/Cycle/Spot)
├── Store Name
├── Count Period (Start - End)
├── Prepared By

EXECUTIVE SUMMARY
├── Total Items Counted: XXX
├── Items with Variance: XXX (XX%)
├── Total System Value: $XXX,XXX
├── Total Counted Value: $XXX,XXX
├── Total Variance Value: $XXX (X.X%)
├── Shrinkage Rate: X.X%

VARIANCE BY CATEGORY
├── Category Name
│   ├── Items Counted
│   ├── System Value
│   ├── Counted Value
│   ├── Variance Value
│   └── Variance %

VARIANCE BY CAUSE
├── Cause
│   ├── Item Count
│   ├── Total Variance Value
│   └── % of Total Shrinkage

DETAILED ITEM VARIANCE (sorted by variance value desc)
├── SKU
├── Product Name
├── Category
├── Location
├── System Qty
├── Counted Qty
├── Variance Qty
├── Unit Cost
├── Variance Value
├── Variance %
├── Cause
└── Notes

SIGNIFICANT VARIANCES (threshold exceeded)
[Same structure as above, filtered]

HISTORICAL COMPARISON
├── This Count
├── Previous Count
├── Variance Change
└── Trend Indicator

SIGNATURES
├── Counted By
├── Verified By
├── Approved By
└── Date
```

### 6. UI Components

#### 6.1 Stock Count List View
- Filter by status, date range, type
- Quick stats (counts in progress, pending approval)
- Create new count button
- Drill-down to count details

#### 6.2 Stock Count Entry View
- Search/scan products
- Grid with product, location, system qty (if not blind), count field
- Progress indicator
- Quick entry mode for high-volume counting
- Save draft / Submit for review

#### 6.3 Variance Review View
- Summary cards (total items, variances, shrinkage)
- Variance items grid with cause dropdown
- Threshold exceeded highlighting
- Approve/Reject actions
- Notes input

#### 6.4 Variance Reports View
- Date range selection
- Category/Location filters
- Summary charts (shrinkage trend, by category, by cause)
- Detailed variance grid
- Export options

### 7. Business Rules

1. **Count Initiation**
   - Cannot start new count if another is in progress for same scope
   - Freezing inventory blocks sales/receiving during count (optional)

2. **Counting**
   - Counted quantity cannot be negative
   - Items not counted default to system quantity (no adjustment)
   - Double-blind requires both counters to complete before review

3. **Variance Thresholds**
   - Items exceeding threshold require explanation
   - Large value variances require manager approval
   - Configurable thresholds by category

4. **Adjustments**
   - Cannot post adjustments without approval (if required by threshold)
   - Adjustments create StockMovement with MovementType.StockTake
   - Journal entry created: DR Shrinkage Expense, CR Inventory Asset

5. **Audit Trail**
   - All count entries logged with user and timestamp
   - Approval/rejection logged
   - Adjustment posting logged
   - Cannot modify posted counts

## Acceptance Criteria

- [ ] Stock count session can be created with configurable scope
- [ ] Items can be counted via manual entry or barcode scan
- [ ] System calculates variances automatically after counting
- [ ] Double-blind counting works with two independent counters
- [ ] Variance causes can be assigned to each item
- [ ] Thresholds flag significant variances for review
- [ ] Approval workflow enforces review before posting
- [ ] Adjustments post correctly to inventory and create stock movements
- [ ] Journal entries generated for accounting integration
- [ ] Variance report shows detailed item-by-item analysis
- [ ] Shrinkage analysis shows trends and patterns
- [ ] Reports exportable to PDF/Excel

## Implementation Notes

### Existing Code to Leverage
- `StockTake` and `StockTakeItem` entities (enhance existing)
- `StockMovement` with MovementType.StockTake
- `IInventoryService` for stock adjustments
- `IInventoryAnalyticsService` patterns

### Integration Points
- Inventory module for stock levels
- Accounting module for journal entries
- Reporting infrastructure for exports
- User permissions for approval workflow

## References

- [Lightspeed - Inventory Reconciliation](https://www.lightspeedhq.com/blog/inventory-reconciliation/)
- [POS Nation - Inventory Reconciliation](https://www.posnation.com/blog/inventory-reconciliation)
- [Corporate Finance Institute - Inventory Shrinkage](https://corporatefinanceinstitute.com/resources/accounting/inventory-shrinkage/)
- [Lightspeed K-Series - Discrepancy Reports](https://k-series-support.lightspeedhq.com/hc/en-us/articles/20537741099419-Discrepancy-reports)

---

**Priority**: High
**Estimated Complexity**: Large
**Labels**: feature, inventory, reporting, audit
