# Story 10.1: Sales Reports

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/ReportService.cs` (1806 lines) - Full implementation with:
  - `GenerateDailySummaryAsync` - Daily sales summary
  - `GenerateProductSalesAsync` - Product-level sales
  - `GenerateCategorySalesAsync` - Category breakdown
  - `GenerateCashierSalesAsync` - Per-cashier performance
  - `GeneratePaymentMethodSalesAsync` - Payment type analysis
  - `GenerateHourlySalesAsync` - Hourly trends

## Story

As a manager,
I want comprehensive sales reports,
So that I can analyze business performance.

## Acceptance Criteria

1. **Given** sales data exists
   **When** generating sales reports
   **Then** available reports should include: Daily Sales Summary, Sales by Product, Sales by Category, Sales by Cashier/User, Sales by Payment Method, Hourly Sales Analysis

2. **Given** date range selection
   **When** filtering reports
   **Then** reports can be filtered by date range

3. **Given** report is generated
   **When** viewing output
   **Then** reports can be viewed on screen or printed (80mm optimized)

## Tasks / Subtasks

- [ ] Task 1: Create Report Infrastructure
  - [ ] Create IReportService interface
  - [ ] Create base report classes
  - [ ] Create report parameter classes
  - [ ] Setup report generation pipeline

- [ ] Task 2: Create Sales Report Screen
  - [ ] Create SalesReportsView.xaml
  - [ ] Create SalesReportsViewModel
  - [ ] Report type selection
  - [ ] Date range picker

- [ ] Task 3: Implement Daily Sales Summary
  - [ ] Calculate gross sales
  - [ ] Calculate discounts
  - [ ] Calculate net sales
  - [ ] Calculate tax collected

- [ ] Task 4: Implement Sales Breakdown Reports
  - [ ] Sales by Product report
  - [ ] Sales by Category report
  - [ ] Sales by Cashier report
  - [ ] Sales by Payment Method report

- [ ] Task 5: Implement Report Printing
  - [ ] Format for 80mm paper
  - [ ] Create print templates
  - [ ] Handle long reports
  - [ ] Print preview option

## Dev Notes

### Report Types

```csharp
public enum SalesReportType
{
    DailySummary,
    ByProduct,
    ByCategory,
    ByCashier,
    ByPaymentMethod,
    HourlySales
}
```

### Sales Report Screen

```
+------------------------------------------+
|      SALES REPORTS                        |
+------------------------------------------+
| Report Type: [Daily Summary_________] [v] |
| From: [2025-12-20]  To: [2025-12-20]      |
| [Generate Report]                         |
+------------------------------------------+
|                                           |
|     DAILY SALES SUMMARY                   |
|     2025-12-20                            |
|                                           |
|  ─────────────────────────────────────    |
|  Gross Sales:            KSh 125,400      |
|  Discounts:              KSh   5,200      |
|  ─────────────────────────────────────    |
|  Net Sales:              KSh 120,200      |
|  VAT Collected:          KSh  19,232      |
|  ─────────────────────────────────────    |
|  Total Revenue:          KSh 139,432      |
|  ─────────────────────────────────────    |
|                                           |
|  Transactions:           85               |
|  Avg Transaction:        KSh   1,640      |
|  Voided:                 3 (KSh 2,500)    |
|                                           |
+------------------------------------------+
| [Print Report]          [Export CSV]      |
+------------------------------------------+
```

### SalesReportsViewModel

```csharp
public partial class SalesReportsViewModel : BaseViewModel
{
    [ObservableProperty]
    private SalesReportType _selectedReportType = SalesReportType.DailySummary;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private object? _reportData;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        IsLoading = true;

        try
        {
            ReportData = SelectedReportType switch
            {
                SalesReportType.DailySummary => await GenerateDailySummaryAsync(),
                SalesReportType.ByProduct => await GenerateByProductAsync(),
                SalesReportType.ByCategory => await GenerateByCategoryAsync(),
                SalesReportType.ByCashier => await GenerateByCashierAsync(),
                SalesReportType.ByPaymentMethod => await GenerateByPaymentMethodAsync(),
                SalesReportType.HourlySales => await GenerateHourlySalesAsync(),
                _ => null
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<DailySalesSummary> GenerateDailySummaryAsync()
    {
        var receipts = await _receiptRepo.GetSettledByDateRangeAsync(FromDate, ToDate);
        var voidedReceipts = await _receiptRepo.GetVoidedByDateRangeAsync(FromDate, ToDate);

        return new DailySalesSummary
        {
            StartDate = FromDate,
            EndDate = ToDate,
            GrossSales = receipts.Sum(r => r.Subtotal + r.DiscountAmount),
            Discounts = receipts.Sum(r => r.DiscountAmount),
            NetSales = receipts.Sum(r => r.Subtotal),
            TaxCollected = receipts.Sum(r => r.TaxAmount),
            TotalRevenue = receipts.Sum(r => r.TotalAmount),
            TransactionCount = receipts.Count,
            AverageTransaction = receipts.Any() ? receipts.Average(r => r.TotalAmount) : 0,
            VoidedCount = voidedReceipts.Count,
            VoidedAmount = voidedReceipts.Sum(r => r.TotalAmount)
        };
    }

    private async Task<List<ProductSalesReport>> GenerateByProductAsync()
    {
        return await _context.ReceiptItems
            .Where(ri => ri.Receipt.Status == "Settled")
            .Where(ri => ri.Receipt.SettledAt >= FromDate && ri.Receipt.SettledAt < ToDate.AddDays(1))
            .GroupBy(ri => new { ri.ProductId, ri.ProductName })
            .Select(g => new ProductSalesReport
            {
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(ri => ri.Quantity),
                GrossSales = g.Sum(ri => ri.TotalAmount),
                Discounts = g.Sum(ri => ri.DiscountAmount),
                NetSales = g.Sum(ri => ri.TotalAmount - ri.DiscountAmount)
            })
            .OrderByDescending(p => p.NetSales)
            .ToListAsync();
    }

    // EF Core 10 LeftJoin - Include products with zero sales
    private async Task<List<ProductSalesReport>> GenerateByProductWithZeroSalesAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .LeftJoin(
                _context.ReceiptItems
                    .Where(ri => ri.Receipt.Status == "Settled")
                    .Where(ri => ri.Receipt.SettledAt >= FromDate && ri.Receipt.SettledAt < ToDate.AddDays(1)),
                p => p.Id,
                ri => ri.ProductId,
                (product, receiptItem) => new { product, receiptItem })
            .GroupBy(x => new { x.product.Id, x.product.Name })
            .Select(g => new ProductSalesReport
            {
                ProductId = g.Key.Id,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(x => x.receiptItem != null ? x.receiptItem.Quantity : 0),
                GrossSales = g.Sum(x => x.receiptItem != null ? x.receiptItem.TotalAmount : 0),
                Discounts = g.Sum(x => x.receiptItem != null ? x.receiptItem.DiscountAmount : 0),
                NetSales = g.Sum(x => x.receiptItem != null
                    ? x.receiptItem.TotalAmount - x.receiptItem.DiscountAmount : 0)
            })
            .OrderByDescending(p => p.NetSales)
            .ToListAsync();
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (ReportData == null) return;

        await _printService.PrintSalesReportAsync(SelectedReportType, ReportData);
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (ReportData == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{SelectedReportType}_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            await _exportService.ExportToCsvAsync(ReportData, dialog.FileName);
            await _dialogService.ShowMessageAsync("Exported", $"Report saved to {dialog.FileName}");
        }
    }
}
```

### Daily Summary Print (80mm)

```
================================================
     DAILY SALES SUMMARY
     2025-12-20
================================================

SALES OVERVIEW
------------------------------------------------
Gross Sales:                    KSh   125,400
Less: Discounts                 KSh    -5,200
------------------------------------------------
Net Sales:                      KSh   120,200
VAT Collected (16%):            KSh    19,232
------------------------------------------------
TOTAL REVENUE:                  KSh   139,432
================================================

TRANSACTION SUMMARY
------------------------------------------------
Total Transactions:                       85
Average Transaction:            KSh     1,640
Largest Transaction:            KSh     8,500
Smallest Transaction:           KSh       150
------------------------------------------------

VOIDS & EXCEPTIONS
------------------------------------------------
Voided Transactions:                       3
Voided Amount:                  KSh     2,500
------------------------------------------------

Generated: 2025-12-20 23:55
By: John Smith (Manager)
================================================
```

### Sales by Product Print (80mm)

```
================================================
     SALES BY PRODUCT
     2025-12-20
================================================

Product              | Qty  | Sales
---------------------|------|------------------
Tusker Lager         |   45 | KSh      15,750
Grilled Chicken      |   32 | KSh      27,200
Coca Cola 500ml      |   78 | KSh       3,900
Chips Regular        |   54 | KSh      10,800
Fish & Chips         |   28 | KSh      23,800
Fanta Orange         |   42 | KSh       2,100
Beef Steak           |   15 | KSh      18,750
Fresh Juice          |   33 | KSh       8,250
... (continued)
---------------------|------|------------------
TOTAL                |  425 | KSh     125,400
================================================

Top 5 Products by Revenue:
1. Grilled Chicken   - KSh 27,200 (21.7%)
2. Fish & Chips      - KSh 23,800 (19.0%)
3. Beef Steak        - KSh 18,750 (14.9%)
4. Tusker Lager      - KSh 15,750 (12.6%)
5. Chips Regular     - KSh 10,800 (8.6%)
================================================
```

### Sales by Cashier Print (80mm)

```
================================================
     SALES BY CASHIER
     2025-12-20
================================================

Cashier          | Trans | Sales    | Avg
-----------------|-------|----------|--------
John Smith       |    28 |   45,200 |  1,614
Mary Johnson     |    32 |   52,800 |  1,650
Peter Wanjiku    |    25 |   27,400 |  1,096
-----------------|-------|----------|--------
TOTAL            |    85 |  125,400 |  1,475
================================================
```

### Report Data Classes

```csharp
public class DailySalesSummary
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public int VoidedCount { get; set; }
    public decimal VoidedAmount { get; set; }
}

public class ProductSalesReport
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal Percentage { get; set; }
}

public class CashierSalesReport
{
    public int UserId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageTransaction { get; set; }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.7.1-Sales-Reports]
- [Source: docs/PRD_Hospitality_POS_System.md#RP-001 to RP-010]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
