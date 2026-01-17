# Story 17.5: Income Statement & Balance Sheet

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/AccountingService.cs` - Financial statements with:
  - `GetIncomeStatementAsync` - P&L for period with revenue/expense sections
  - `GetBalanceSheetAsync` - Assets, Liabilities, Equity with retained earnings
  - `GetAccountLedgerAsync` - Account ledger with running balances
  - `GenerateIncomeStatementHtmlAsync` / `GenerateBalanceSheetHtmlAsync` - Printable formats

## Story

As a manager,
I want financial statements,
So that I can understand business performance.

## Acceptance Criteria

1. **Given** date range selected
   **When** running Income Statement
   **Then** shows: Revenue - Expenses = Net Income

2. **Given** a date selected
   **When** running Balance Sheet
   **Then** shows: Assets = Liabilities + Equity

3. **Given** financial reports
   **When** exporting
   **Then** can export to PDF or Excel

## Tasks / Subtasks

- [ ] Task 1: Create Financial Report Service
  - [ ] Create IFinancialReportService interface
  - [ ] Implement GenerateIncomeStatementAsync
  - [ ] Implement GenerateBalanceSheetAsync

- [ ] Task 2: Create Income Statement View
  - [ ] Create IncomeStatementView.xaml
  - [ ] Date range selector
  - [ ] Revenue section
  - [ ] Expense section
  - [ ] Net income calculation

- [ ] Task 3: Create Balance Sheet View
  - [ ] Create BalanceSheetView.xaml
  - [ ] As-of date selector
  - [ ] Assets section
  - [ ] Liabilities section
  - [ ] Equity section

- [ ] Task 4: Export Functionality
  - [ ] Export Income Statement to PDF/Excel
  - [ ] Export Balance Sheet to PDF/Excel

## Dev Notes

### Income Statement Layout

```
+----------------------------------------------------------------------+
|                        INCOME STATEMENT                               |
|                  For the Period: Jan 1 - Dec 31, 2025                 |
+----------------------------------------------------------------------+
| REVENUE                                                               |
|   Sales Revenue                                      KSh 8,500,000.00 |
|   Service Revenue                                    KSh   500,000.00 |
|   Other Income                                       KSh    50,000.00 |
|   ────────────────────────────────────────────────────────────────    |
|   Total Revenue                                      KSh 9,050,000.00 |
+----------------------------------------------------------------------+
| COST OF GOODS SOLD                                                    |
|   Cost of Goods Sold                                 KSh 4,000,000.00 |
|   ────────────────────────────────────────────────────────────────    |
|   Gross Profit                                       KSh 5,050,000.00 |
+----------------------------------------------------------------------+
| OPERATING EXPENSES                                                    |
|   Salaries & Wages                                   KSh 2,000,000.00 |
|   Rent Expense                                       KSh   600,000.00 |
|   Utilities Expense                                  KSh   360,000.00 |
|   Office Supplies                                    KSh   120,000.00 |
|   Other Expenses                                     KSh   200,000.00 |
|   ────────────────────────────────────────────────────────────────    |
|   Total Operating Expenses                           KSh 3,280,000.00 |
+----------------------------------------------------------------------+
| NET INCOME                                           KSh 1,770,000.00 |
+----------------------------------------------------------------------+
```

### Balance Sheet Layout

```
+----------------------------------------------------------------------+
|                         BALANCE SHEET                                 |
|                      As of December 31, 2025                          |
+----------------------------------------------------------------------+
| ASSETS                                                                |
|   Current Assets                                                      |
|     Cash on Hand                                     KSh   150,000.00 |
|     Cash in Bank                                     KSh 1,500,000.00 |
|     Accounts Receivable                              KSh   750,000.00 |
|     Merchandise Inventory                            KSh 3,500,000.00 |
|     ──────────────────────────────────────────────────────────────    |
|     Total Current Assets                             KSh 5,900,000.00 |
|                                                                       |
|   Fixed Assets                                                        |
|     Equipment                                        KSh   800,000.00 |
|     Furniture & Fixtures                             KSh   300,000.00 |
|     ──────────────────────────────────────────────────────────────    |
|     Total Fixed Assets                               KSh 1,100,000.00 |
|                                                                       |
|   TOTAL ASSETS                                       KSh 7,000,000.00 |
+----------------------------------------------------------------------+
| LIABILITIES                                                           |
|   Current Liabilities                                                 |
|     Accounts Payable                                 KSh 1,350,000.00 |
|     Salaries Payable                                 KSh   200,000.00 |
|     VAT Payable                                      KSh   180,000.00 |
|     ──────────────────────────────────────────────────────────────    |
|     Total Current Liabilities                        KSh 1,730,000.00 |
|                                                                       |
|   TOTAL LIABILITIES                                  KSh 1,730,000.00 |
+----------------------------------------------------------------------+
| EQUITY                                                                |
|   Owner's Capital                                    KSh 3,500,000.00 |
|   Retained Earnings                                  KSh 1,770,000.00 |
|   ────────────────────────────────────────────────────────────────    |
|   TOTAL EQUITY                                       KSh 5,270,000.00 |
+----------------------------------------------------------------------+
| TOTAL LIABILITIES + EQUITY                           KSh 7,000,000.00 |
+----------------------------------------------------------------------+
| [✓] Balance Sheet is BALANCED (Assets = Liabilities + Equity)         |
+----------------------------------------------------------------------+
```

### IFinancialReportService Interface

```csharp
public interface IFinancialReportService
{
    Task<IncomeStatementReport> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate);
    Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate);
}

public class IncomeStatementReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<IncomeStatementSection> RevenueSections { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public List<IncomeStatementSection> ExpenseSections { get; set; } = new();
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class BalanceSheetReport
{
    public DateTime AsOfDate { get; set; }
    public List<BalanceSheetSection> AssetSections { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public List<BalanceSheetSection> LiabilitySections { get; set; } = new();
    public decimal TotalLiabilities { get; set; }
    public List<BalanceSheetSection> EquitySections { get; set; } = new();
    public decimal TotalEquity { get; set; }
    public bool IsBalanced => TotalAssets == TotalLiabilities + TotalEquity;
}
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
