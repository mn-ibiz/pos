# Story 17.4: Trial Balance Report

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/AccountingService.cs` - Trial balance with:
  - `GetTrialBalanceAsync` - Generate trial balance as of date
  - `GenerateTrialBalanceHtmlAsync` - Printable HTML format
  - Balanced/unbalanced validation indicator

## Story

As an accountant,
I want a trial balance report,
So that I can verify accounts are balanced.

## Acceptance Criteria

1. **Given** journal entries exist
   **When** running Trial Balance
   **Then** shows all accounts with debit and credit balances

2. **Given** trial balance
   **When** viewing totals
   **Then** total debits must equal total credits

## Tasks / Subtasks

- [ ] Task 1: Create Trial Balance Service
  - [ ] Create ITrialBalanceService interface
  - [ ] Implement balance calculation per account
  - [ ] Calculate totals

- [ ] Task 2: Create Trial Balance View
  - [ ] Create TrialBalanceView.xaml
  - [ ] Date selector (as of date)
  - [ ] Account list with balances
  - [ ] Totals row

- [ ] Task 3: Add Validation
  - [ ] Check debits = credits
  - [ ] Display warning if unbalanced
  - [ ] Show difference amount

- [ ] Task 4: Export Functionality
  - [ ] Export to PDF
  - [ ] Export to Excel

## Dev Notes

### Trial Balance Report Layout

```
+----------------------------------------------------------------------+
|                        TRIAL BALANCE                                  |
|                     As of December 31, 2025                           |
+----------------------------------------------------------------------+
| Account Code | Account Name              | Debit       | Credit      |
|--------------|---------------------------|-------------|-------------|
| ASSETS       |                           |             |             |
| 1010         | Cash on Hand              | 150,000.00  |             |
| 1020         | Cash in Bank              | 500,000.00  |             |
| 1100         | Accounts Receivable       | 75,000.00   |             |
| 1210         | Merchandise Inventory     | 350,000.00  |             |
|--------------|---------------------------|-------------|-------------|
| LIABILITIES  |                           |             |             |
| 2000         | Accounts Payable          |             | 135,000.00  |
| 2100         | Salaries Payable          |             | 50,000.00   |
| 2210         | VAT Payable               |             | 45,000.00   |
|--------------|---------------------------|-------------|-------------|
| EQUITY       |                           |             |             |
| 3000         | Owner's Capital           |             | 500,000.00  |
| 3100         | Retained Earnings         |             | 195,000.00  |
|--------------|---------------------------|-------------|-------------|
| REVENUE      |                           |             |             |
| 4000         | Sales Revenue             |             | 850,000.00  |
|--------------|---------------------------|-------------|-------------|
| EXPENSES     |                           |             |             |
| 5000         | Cost of Goods Sold        | 400,000.00  |             |
| 5100         | Salaries & Wages          | 200,000.00  |             |
| 5200         | Rent Expense              | 50,000.00   |             |
| 5300         | Utilities Expense         | 30,000.00   |             |
| 5500         | Other Expenses            | 20,000.00   |             |
|--------------|---------------------------|-------------|-------------|
|              | TOTALS                    | 1,775,000.00| 1,775,000.00|
+----------------------------------------------------------------------+
| [✓] Trial Balance is BALANCED                                         |
+----------------------------------------------------------------------+
```

### ITrialBalanceService Interface

```csharp
public interface ITrialBalanceService
{
    Task<TrialBalanceReport> GenerateTrialBalanceAsync(DateTime asOfDate);
}

public class TrialBalanceReport
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLine> Lines { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced => TotalDebits == TotalCredits;
    public decimal Difference => TotalDebits - TotalCredits;
}

public class TrialBalanceLine
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}
```

### Balance Calculation Logic

- **Assets & Expenses**: Normal balance is DEBIT
  - Debit increases, Credit decreases
  - Display in Debit column if positive

- **Liabilities, Equity & Revenue**: Normal balance is CREDIT
  - Credit increases, Debit decreases
  - Display in Credit column if positive

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
