# Story 10.2: Void and Discount Reports

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/ReportService.cs` - Void and discount reporting with:
  - `GenerateVoidReportAsync` - Voided items/transactions
  - `GenerateDiscountReportAsync` - Discount analysis by type and user

## Story

As a manager,
I want void and discount reports,
So that I can monitor exceptions and potential issues.

## Acceptance Criteria

1. **Given** voids and discounts have occurred
   **When** generating exception reports
   **Then** void report should show: receipt #, amount, user, authorizer, reason, timestamp

2. **Given** discounts exist
   **When** generating discount report
   **Then** discount report should show: receipt #, original amount, discount, user

3. **Given** report parameters
   **When** filtering results
   **Then** reports can be filtered by date range and user

4. **Given** report is generated
   **When** viewing totals
   **Then** totals should be calculated for each report

## Tasks / Subtasks

- [ ] Task 1: Create Void Report
  - [ ] Query voided receipts
  - [ ] Include void details
  - [ ] Show authorizer info
  - [ ] Calculate totals

- [ ] Task 2: Create Discount Report
  - [ ] Query discounted items
  - [ ] Show discount breakdown
  - [ ] Group by user if needed
  - [ ] Calculate totals

- [ ] Task 3: Create Exception Reports Screen
  - [ ] Create ExceptionReportsView.xaml
  - [ ] Create ExceptionReportsViewModel
  - [ ] Date range filter
  - [ ] User filter

- [ ] Task 4: Implement Report Filtering
  - [ ] Filter by date range
  - [ ] Filter by user
  - [ ] Filter by void reason
  - [ ] Show filtered totals

- [ ] Task 5: Implement Report Printing
  - [ ] Format void report for 80mm
  - [ ] Format discount report for 80mm
  - [ ] Include summary section
  - [ ] Export to CSV option

## Dev Notes

### Exception Reports Screen

```
+------------------------------------------+
|      EXCEPTION REPORTS                    |
+------------------------------------------+
| Report: [Void Report____________] [v]     |
| From: [2025-12-20]  To: [2025-12-20]      |
| User: [All Users____________] [v]         |
| [Generate Report]                         |
+------------------------------------------+
|                                           |
|     VOID REPORT                           |
|     2025-12-20                            |
|                                           |
|  +------------------------------------+   |
|  | R-0042 | KSh 2,262 | 15:45         |   |
|  | By: John  Auth: Mary               |   |
|  | Reason: Customer complaint         |   |
|  +------------------------------------+   |
|  | R-0048 | KSh 1,500 | 16:30         |   |
|  | By: Peter  Auth: Mary              |   |
|  | Reason: Wrong order                |   |
|  +------------------------------------+   |
|  | R-0055 | KSh   850 | 18:15         |   |
|  | By: John  Auth: (Self)             |   |
|  | Reason: Test transaction           |   |
|  +------------------------------------+   |
|                                           |
|  Total Voids: 3    Amount: KSh 4,612      |
|                                           |
+------------------------------------------+
| [Print Report]          [Export CSV]      |
+------------------------------------------+
```

### VoidReportViewModel

```csharp
public partial class VoidReportViewModel : BaseViewModel
{
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private ObservableCollection<VoidReportItem> _voidItems = new();

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private decimal _totalAmount;

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var query = _context.ReceiptVoids
            .Where(v => v.VoidedAt >= FromDate && v.VoidedAt < ToDate.AddDays(1))
            .Include(v => v.Receipt)
            .Include(v => v.VoidReason)
            .Include(v => v.VoidedByUser)
            .Include(v => v.AuthorizedByUser)
            .AsQueryable();

        if (SelectedUser != null)
        {
            query = query.Where(v => v.VoidedByUserId == SelectedUser.Id);
        }

        var voids = await query
            .OrderByDescending(v => v.VoidedAt)
            .Select(v => new VoidReportItem
            {
                ReceiptNumber = v.Receipt.ReceiptNumber,
                VoidedAmount = v.VoidedAmount,
                VoidedAt = v.VoidedAt,
                VoidedBy = v.VoidedByUser.FullName,
                AuthorizedBy = v.AuthorizedByUser != null
                    ? v.AuthorizedByUser.FullName
                    : "(Self)",
                Reason = v.VoidReason.Name,
                Notes = v.AdditionalNotes
            })
            .ToListAsync();

        VoidItems = new ObservableCollection<VoidReportItem>(voids);
        TotalCount = voids.Count;
        TotalAmount = voids.Sum(v => v.VoidedAmount);
    }
}

public class VoidReportItem
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal VoidedAmount { get; set; }
    public DateTime VoidedAt { get; set; }
    public string VoidedBy { get; set; } = string.Empty;
    public string AuthorizedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
```

### Discount Report Screen

```
+------------------------------------------+
|      DISCOUNT REPORT                      |
|      2025-12-20                           |
+------------------------------------------+
|                                           |
|  +------------------------------------+   |
|  | R-0035 | Tusker Lager x2           |   |
|  | Original: KSh 700  Discount: 50    |   |
|  | Type: Item Discount  By: John      |   |
|  +------------------------------------+   |
|  | R-0035 | Grilled Chicken           |   |
|  | Original: KSh 850  Discount: 100   |   |
|  | Type: Manager Override  By: Mary   |   |
|  +------------------------------------+   |
|  | R-0041 | Bill Discount             |   |
|  | Original: KSh 3,200  Discount: 320 |   |
|  | Type: 10% Order Discount  By: Peter|   |
|  +------------------------------------+   |
|                                           |
|  SUMMARY                                  |
|  ─────────────────────────────────────    |
|  Total Discounts Given: KSh 5,200         |
|  Discount Transactions: 15                |
|  Avg Discount: KSh 347                    |
|  Discount Rate: 4.1% of sales             |
|                                           |
+------------------------------------------+
```

### DiscountReportViewModel

```csharp
public partial class DiscountReportViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<DiscountReportItem> _discountItems = new();

    [ObservableProperty]
    private decimal _totalDiscounts;

    [ObservableProperty]
    private int _discountTransactions;

    [ObservableProperty]
    private decimal _averageDiscount;

    [ObservableProperty]
    private decimal _discountRate;

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        // Get receipt-level discounts
        var receiptDiscounts = await _context.Receipts
            .Where(r => r.DiscountAmount > 0)
            .Where(r => r.SettledAt >= FromDate && r.SettledAt < ToDate.AddDays(1))
            .Select(r => new DiscountReportItem
            {
                ReceiptNumber = r.ReceiptNumber,
                ItemDescription = "Order Discount",
                OriginalAmount = r.Subtotal + r.DiscountAmount,
                DiscountAmount = r.DiscountAmount,
                DiscountType = "Order",
                AppliedBy = r.User.FullName,
                AppliedAt = r.SettledAt ?? r.CreatedAt
            })
            .ToListAsync();

        // Get item-level discounts
        var itemDiscounts = await _context.ReceiptItems
            .Where(ri => ri.DiscountAmount > 0)
            .Where(ri => ri.Receipt.SettledAt >= FromDate &&
                        ri.Receipt.SettledAt < ToDate.AddDays(1))
            .Select(ri => new DiscountReportItem
            {
                ReceiptNumber = ri.Receipt.ReceiptNumber,
                ItemDescription = $"{ri.ProductName} x{ri.Quantity}",
                OriginalAmount = ri.Quantity * ri.UnitPrice,
                DiscountAmount = ri.DiscountAmount,
                DiscountType = "Item",
                AppliedBy = ri.Receipt.User.FullName,
                AppliedAt = ri.Receipt.SettledAt ?? ri.Receipt.CreatedAt
            })
            .ToListAsync();

        var allDiscounts = receiptDiscounts.Concat(itemDiscounts)
            .OrderByDescending(d => d.AppliedAt)
            .ToList();

        DiscountItems = new ObservableCollection<DiscountReportItem>(allDiscounts);

        // Calculate summary
        TotalDiscounts = allDiscounts.Sum(d => d.DiscountAmount);
        DiscountTransactions = allDiscounts.Select(d => d.ReceiptNumber).Distinct().Count();
        AverageDiscount = allDiscounts.Any() ? allDiscounts.Average(d => d.DiscountAmount) : 0;

        // Calculate discount rate
        var totalSales = await _context.Receipts
            .Where(r => r.Status == "Settled")
            .Where(r => r.SettledAt >= FromDate && r.SettledAt < ToDate.AddDays(1))
            .SumAsync(r => r.Subtotal);

        DiscountRate = totalSales > 0 ? (TotalDiscounts / totalSales) * 100 : 0;
    }
}

public class DiscountReportItem
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public string AppliedBy { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}
```

### Void Report Print (80mm)

```
================================================
     VOID REPORT
     2025-12-20
================================================

Receipt  | Amount   | Time  | Reason
---------|----------|-------|------------------
R-0042   | 2,262    | 15:45 | Customer complaint
         | By: John | Auth: Mary
---------|----------|-------|------------------
R-0048   | 1,500    | 16:30 | Wrong order
         | By: Peter | Auth: Mary
---------|----------|-------|------------------
R-0055   |   850    | 18:15 | Test transaction
         | By: John | Auth: (Self)
================================================
SUMMARY
------------------------------------------------
Total Void Transactions:              3
Total Voided Amount:        KSh   4,612
------------------------------------------------
By Reason:
  Customer complaint:       KSh   2,262 (1)
  Wrong order:              KSh   1,500 (1)
  Test transaction:         KSh     850 (1)
================================================
Generated: 2025-12-20 23:55
================================================
```

### Discount Report Print (80mm)

```
================================================
     DISCOUNT REPORT
     2025-12-20
================================================

Receipt  | Item             | Discount
---------|------------------|------------------
R-0035   | Tusker Lager x2  | KSh      50
         | Item Discount    | By: John
---------|------------------|------------------
R-0035   | Grilled Chicken  | KSh     100
         | Manager Override | By: Mary
---------|------------------|------------------
R-0041   | Order Discount   | KSh     320
         | 10% Discount     | By: Peter
================================================
SUMMARY
------------------------------------------------
Total Discounts Given:      KSh   5,200
Discount Transactions:              15
Average Discount:           KSh     347
Discount Rate:                    4.1%
------------------------------------------------
By Type:
  Item Discounts:           KSh   2,100 (8)
  Order Discounts:          KSh   3,100 (7)
================================================
Generated: 2025-12-20 23:55
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.7.2-Exception-Reports]
- [Source: docs/PRD_Hospitality_POS_System.md#RP-015 to RP-020]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
