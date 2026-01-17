# Story 3.4: Close Work Period with Z Report

Status: done

## Story

As a manager,
I want to close the work period with cash count reconciliation,
So that the business day is properly finalized with complete records.

## Acceptance Criteria

1. **Given** a work period is open
   **When** manager initiates work period close
   **Then** system should warn if there are unsettled receipts

2. **Given** work period close is initiated
   **When** cash count is required
   **Then** system should prompt for physical cash count entry

3. **Given** cash count is entered
   **When** reconciliation is calculated
   **Then** system should calculate expected cash and display variance

4. **Given** cash count is complete
   **When** close is confirmed
   **Then** Z Report should be automatically generated with all required data

5. **Given** Z Report is generated
   **When** user wants to print
   **Then** Z Report should be printed on 80mm thermal printer

6. **Given** work period is closed
   **When** closure is complete
   **Then** all transactions for the period should be locked

7. **Given** work period is closed
   **When** closure is complete
   **Then** work period status should change to "Closed"

## Tasks / Subtasks

- [x] Task 1: Create Close Work Period Dialog (AC: #1, #2, #3)
  - [x] Create CloseWorkPeriodDialog.xaml
  - [x] Show unsettled receipts warning with count
  - [x] Add cash count entry with numeric keypad
  - [x] Calculate and display expected cash
  - [x] Show variance (over/short) with color coding

- [x] Task 2: Implement Close Service (AC: #6, #7)
  - [x] Implement CloseWorkPeriodAsync in IWorkPeriodService
  - [x] Validate all receipts are settled (or warn)
  - [x] Record closing cash, expected cash, variance
  - [x] Set status to "Closed"
  - [x] Lock all transactions for the period

- [x] Task 3: Implement Z Report Generation (AC: #4)
  - [x] Create ZReport model (extends XReport with additional fields)
  - [x] Add cash drawer reconciliation section
  - [x] Add sequential Z-Report number
  - [x] Include all X-Report data plus work period closure info

- [x] Task 4: Implement Z Report Printing (AC: #5)
  - [x] Create Z Report print template (80mm format)
  - [x] Auto-print Z Report on successful close
  - [x] Allow reprint of Z Report

- [x] Task 5: Handle Unsettled Receipts (AC: #1)
  - [x] Check for pending receipts before close
  - [x] Display list of unsettled receipts
  - [x] Allow manager to force close with warning
  - [x] Log forced close in audit trail

## Dev Notes

### Close Work Period Dialog Layout

```
+------------------------------------------+
|  Close Work Period                        |
+------------------------------------------+
|                                           |
|  UNSETTLED RECEIPTS WARNING              |
|  +------------------------------------+  |
|  | 3 receipts pending settlement:     |  |
|  | R-0045: KSh 1,250.00 - Table 5     |  |
|  | R-0047: KSh 800.00 - Table 8       |  |
|  | R-0051: KSh 450.00 - Bar           |  |
|  +------------------------------------+  |
|                                           |
|  CASH DRAWER RECONCILIATION              |
|  Expected Cash:     KSh 25,460.00        |
|  (Opening Float:    KSh 10,000.00)       |
|  (Cash Sales:       KSh 18,460.00)       |
|  (Cash Payouts:    -KSh  3,000.00)       |
|                                           |
|  Actual Cash Count:                       |
|  +-----------------------------------+    |
|  |  KSh 25,200.00                    |    |
|  +-----------------------------------+    |
|                                           |
|  Variance: KSh -260.00 (SHORT)           |
|                                           |
|  Notes:                                   |
|  +-----------------------------------+    |
|  |                                   |    |
|  +-----------------------------------+    |
|                                           |
|  [Close Period]  [Cancel]                 |
+------------------------------------------+
```

### ZReport Model

```csharp
public class ZReport : XReport
{
    // Z-Report Specific
    public int ZReportNumber { get; set; }
    public DateTime WorkPeriodClosedAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;

    // Cash Drawer Reconciliation
    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CashPayouts { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }

    // Settlement Summary
    public int SettledReceiptsCount { get; set; }
    public decimal SettledReceiptsTotal { get; set; }
    public int PendingReceiptsCount { get; set; }
    public decimal PendingReceiptsTotal { get; set; }

    // Items Sold Summary
    public List<ItemSoldSummary> ItemsSold { get; set; } = new();
}

public class ItemSoldSummary
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalValue { get; set; }
}
```

### 80mm Z Report Format

```
================================================
        HOSPITALITY POS
        123 Main Street
================================================
             *** Z REPORT ***
             Z-Report #: Z-0089
================================================
Date: 2025-12-20
Period Open:  08:00 by Admin
Period Close: 22:15 by John Manager
Duration: 14h 15m
================================================
SALES SUMMARY
------------------------------------------------
Gross Sales:              KSh 125,650.00
Discounts:                -KSh  5,150.00
Net Sales:                KSh 120,500.00
Tax (16%):                KSh  19,280.00
                         ------------------
GRAND TOTAL:              KSh 139,780.00
================================================
SALES BY CASHIER
------------------------------------------------
John Cashier:
  Transactions: 45
  Total: KSh 55,000.00
  Average: KSh 1,222.22

Mary Server:
  Transactions: 52
  Total: KSh 64,500.00
  Average: KSh 1,240.38
================================================
PAYMENT METHODS
------------------------------------------------
Cash (68):                KSh 75,460.00
M-Pesa (42):              KSh 48,000.00
Card (15):                KSh 16,320.00
================================================
RECEIPTS
------------------------------------------------
Settled: 125              KSh 139,780.00
Pending: 0                KSh      0.00
Voided: 3                 KSh  1,250.00
================================================
VOIDS DETAIL
------------------------------------------------
R-0023: KSh 450.00
  Reason: Wrong item
  Voided by: Manager

R-0041: KSh 200.00
  Reason: Customer left

R-0089: KSh 600.00
  Reason: Kitchen error
================================================
CASH DRAWER
------------------------------------------------
Opening Float:            KSh 10,000.00
+ Cash Sales:             KSh 75,460.00
- Cash Payouts:          -KSh  5,000.00
                         ------------------
EXPECTED:                 KSh 80,460.00

Actual Count:             KSh 80,200.00
                         ------------------
VARIANCE:                -KSh    260.00
                         *** SHORT ***
================================================
TOP SELLING ITEMS
------------------------------------------------
1. House Beer (156)       KSh 31,200.00
2. Grilled Chicken (78)   KSh 35,100.00
3. Chips (124)            KSh 12,400.00
4. Soda (145)             KSh  7,250.00
5. Pizza (45)             KSh 22,500.00
================================================
       *** END OF Z REPORT ***
       This is an official document
       Do not discard
================================================
```

### Close Service Implementation

```csharp
public async Task<WorkPeriod> CloseWorkPeriodAsync(
    decimal closingCash, int userId)
{
    var workPeriod = await GetCurrentWorkPeriodAsync();
    if (workPeriod == null)
        throw new InvalidOperationException("No work period is open");

    // Calculate expected cash
    var cashSales = await CalculateCashSalesAsync(workPeriod.Id);
    var cashPayouts = await CalculateCashPayoutsAsync(workPeriod.Id);
    var expectedCash = workPeriod.OpeningFloat + cashSales - cashPayouts;

    // Update work period
    workPeriod.ClosedAt = DateTime.UtcNow;
    workPeriod.ClosedByUserId = userId;
    workPeriod.ClosingCash = closingCash;
    workPeriod.ExpectedCash = expectedCash;
    workPeriod.Variance = closingCash - expectedCash;
    workPeriod.Status = "Closed";

    // Generate Z-Report number (sequential)
    workPeriod.ZReportNumber = await GetNextZReportNumberAsync();

    await _unitOfWork.SaveChangesAsync();

    // Generate and store Z-Report
    var zReport = await GenerateZReportAsync(workPeriod.Id);
    // Store zReport as JSON or print immediately

    return workPeriod;
}
```

### Variance Color Coding
- **Over (positive)**: Yellow/Orange background
- **Short (negative)**: Red background
- **Exact (zero)**: Green background

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.1.3-Closing-Work-Period]
- [Source: docs/PRD_Hospitality_POS_System.md#7.2-Z-Report]
- [Source: docs/PRD_Hospitality_POS_System.md#7.2.1-Z-Report-Contents]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created ZReport model with comprehensive end-of-day report fields including cash drawer reconciliation
- Added ItemSoldSummary for top selling items tracking
- Extended IWorkPeriodService with GenerateZReportAsync, GetUnsettledReceiptsAsync, GetUnsettledReceiptsCountAsync
- Implemented GenerateZReportAsync in WorkPeriodService with full sales aggregations and cash drawer calculations
- Created CloseWorkPeriodDialog with unsettled receipts warning, cash count entry, and variance display
- Variance color coding: Green (exact), Yellow/Orange (over), Red (short)
- Created ZReportDialog with comprehensive report display and 80mm thermal print functionality
- Updated IDialogService and DialogService with ShowZReportDialogAsync and ShowCloseWorkPeriodDialogAsync
- Added CloseWorkPeriodCommand to MainViewModel with permission check (WorkPeriod.Close)
- Z-Report auto-displays after successful work period close
- Large variance warning (>1000 KSh) prompts for confirmation

### File List
- src/HospitalityPOS.Core/Models/Reports/ZReport.cs (new)
- src/HospitalityPOS.Core/Interfaces/IWorkPeriodService.cs (modified)
- src/HospitalityPOS.Infrastructure/Services/WorkPeriodService.cs (modified)
- src/HospitalityPOS.WPF/Views/Dialogs/CloseWorkPeriodDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/CloseWorkPeriodDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/ZReportDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/ZReportDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs (modified)

### Change Log
- 2025-12-30: Story implemented - All tasks completed
- 2025-12-30: Code review completed - No blocking issues found
