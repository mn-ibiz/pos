## Overview

Implement a comprehensive Accounting Module that integrates with the POS system to provide double-entry bookkeeping, automatic journal entries from transactions, financial statement generation (P&L, Balance Sheet, General Ledger), and correlation of expenses with sales revenue for complete financial visibility.

## Background Research

### POS-Accounting Integration Best Practices
- **Automated Data Flow**: Sales transactions from POS automatically create journal entries in the General Ledger
- **Real-time Updates**: Financial data synchronized immediately, not batched
- **Daily Sales Summary (DSS)**: Consolidated daily entries posted to GL accounts
- **Payment Method Mapping**: Each payment type maps to specific GL accounts
- **Tax Liability Tracking**: Taxes collected automatically credited to tax liability accounts

### Key Accounting Concepts for POS
1. **Double-Entry Bookkeeping**: Every transaction has equal debits and credits
2. **Chart of Accounts**: Hierarchical structure organizing all accounts
3. **General Ledger**: Master record of all financial transactions
4. **Journal Entries**: Individual transaction records with line items
5. **Accounting Periods**: Fiscal periods for reporting (monthly, quarterly, annual)

### Standard POS Transaction Journal Entries
```
CASH SALE:
  DR: Cash/Bank                    $XX.XX
  CR: Sales Revenue                $XX.XX
  CR: Sales Tax Payable            $XX.XX

CREDIT CARD SALE:
  DR: Accounts Receivable (Clearing) $XX.XX
  CR: Sales Revenue                  $XX.XX
  CR: Sales Tax Payable              $XX.XX

REFUND:
  DR: Sales Returns                $XX.XX
  DR: Sales Tax Payable            $XX.XX
  CR: Cash/Bank                    $XX.XX

EXPENSE PAYMENT:
  DR: Expense Account              $XX.XX
  CR: Cash/Bank                    $XX.XX

INVENTORY PURCHASE:
  DR: Inventory Asset              $XX.XX
  CR: Accounts Payable             $XX.XX

COST OF GOODS SOLD (at sale):
  DR: Cost of Goods Sold           $XX.XX
  CR: Inventory Asset              $XX.XX
```

## Current State Analysis

The codebase has substantial accounting infrastructure:
- `ChartOfAccount` entity with account types (Asset, Liability, Equity, Revenue, Expense)
- `JournalEntry` and `JournalEntryLine` entities for double-entry
- `AccountingPeriod` for fiscal period management
- `IFinancialReportingService` with some reporting methods
- `FinancialCostCenter` for departmental allocation
- `Budget` and `BudgetLine` for budgeting
- Expense system with categories mapped to GL accounts

**What's Missing**:
- Automated journal entry generation from POS transactions
- Complete P&L statement generation
- Balance Sheet generation
- General Ledger report with full trial balance
- Cash flow statement
- Account reconciliation
- Period close workflow
- Full financial dashboard

## Requirements

### 1. Chart of Accounts Enhancement

```
STANDARD RESTAURANT/RETAIL CHART OF ACCOUNTS
=============================================

1000-1999: ASSETS
├── 1000 Cash and Cash Equivalents
│   ├── 1010 Cash on Hand
│   ├── 1020 Cash in Bank - Operating
│   ├── 1030 Cash in Bank - Payroll
│   ├── 1040 Petty Cash
│   └── 1050 Undeposited Funds
├── 1100 Accounts Receivable
│   ├── 1110 Customer Receivables
│   ├── 1120 Credit Card Clearing
│   └── 1130 Employee Advances
├── 1200 Inventory
│   ├── 1210 Food Inventory
│   ├── 1220 Beverage Inventory
│   ├── 1230 Bar Inventory
│   └── 1240 Supplies Inventory
├── 1300 Prepaid Expenses
│   ├── 1310 Prepaid Rent
│   ├── 1320 Prepaid Insurance
│   └── 1330 Prepaid Licenses
└── 1400 Fixed Assets
    ├── 1410 Furniture & Fixtures
    ├── 1420 Equipment
    ├── 1430 Accumulated Depreciation
    └── 1440 Leasehold Improvements

2000-2999: LIABILITIES
├── 2000 Accounts Payable
├── 2100 Accrued Liabilities
│   ├── 2110 Accrued Wages
│   ├── 2120 Accrued Taxes
│   └── 2130 Accrued Interest
├── 2200 Sales Tax Payable
│   ├── 2210 VAT Payable
│   └── 2220 Service Tax Payable
├── 2300 Payroll Liabilities
│   ├── 2310 PAYE Payable
│   ├── 2320 NSSF Payable
│   ├── 2330 SHIF Payable
│   └── 2340 Other Deductions Payable
├── 2400 Gift Card Liability
├── 2500 Customer Deposits
└── 2600 Notes Payable / Loans

3000-3999: EQUITY
├── 3000 Owner's Capital
├── 3100 Owner's Draws
├── 3200 Retained Earnings
└── 3300 Current Year Earnings

4000-4999: REVENUE
├── 4000 Sales Revenue
│   ├── 4100 Food Sales
│   ├── 4200 Beverage Sales
│   ├── 4300 Alcohol Sales
│   ├── 4400 Merchandise Sales
│   └── 4500 Catering Sales
├── 4600 Service Revenue
│   ├── 4610 Delivery Fees
│   └── 4620 Service Charges
├── 4700 Tips (if tracked as revenue)
└── 4800 Other Income
    ├── 4810 Interest Income
    └── 4820 Rebates & Refunds

5000-5999: COST OF GOODS SOLD
├── 5000 Cost of Goods Sold
│   ├── 5100 Food Cost
│   ├── 5200 Beverage Cost
│   ├── 5300 Alcohol Cost
│   └── 5400 Merchandise Cost
├── 5500 Purchase Discounts (contra)
└── 5600 Inventory Adjustments
    ├── 5610 Shrinkage
    ├── 5620 Waste
    └── 5630 Spoilage

6000-6999: LABOR COSTS
├── 6000 Wages & Salaries
│   ├── 6100 Management Salaries
│   ├── 6200 Kitchen Wages
│   ├── 6300 Service Wages
│   └── 6400 Administrative Wages
├── 6500 Payroll Taxes
├── 6600 Employee Benefits
└── 6700 Contract Labor

7000-7999: OPERATING EXPENSES
├── 7000 Occupancy Costs
│   ├── 7100 Rent
│   ├── 7200 Utilities
│   ├── 7300 Property Insurance
│   └── 7400 Property Taxes
├── 7500 Operating Supplies
├── 7600 Repairs & Maintenance
├── 7700 Marketing & Advertising
├── 7800 Professional Services
│   ├── 7810 Accounting Fees
│   ├── 7820 Legal Fees
│   └── 7830 Consulting Fees
└── 7900 Other Operating Expenses
    ├── 7910 Bank Fees
    ├── 7920 Credit Card Fees
    ├── 7930 Licenses & Permits
    └── 7940 Training & Development

8000-8999: OTHER INCOME/EXPENSES
├── 8000 Interest Expense
├── 8100 Depreciation Expense
├── 8200 Amortization Expense
└── 8900 Extraordinary Items
```

### 2. Core Features

#### 2.1 Automated Journal Entry Generation
- [ ] **Sales Transaction Journal Entries**
  - Auto-generate entries when receipt is settled
  - Debit appropriate asset (Cash, Card Clearing, AR)
  - Credit Sales Revenue (by category)
  - Credit Tax Liability accounts
  - Debit COGS, Credit Inventory (for inventory items)

- [ ] **Payment Processing Entries**
  - Card settlement: DR Bank, CR Card Clearing
  - M-Pesa settlement: DR Bank, CR Mobile Money Clearing
  - Daily bank deposit: DR Bank, CR Undeposited Funds

- [ ] **Expense Entries**
  - Auto-generate from approved expenses
  - DR Expense Account (from category mapping)
  - CR Cash/Bank or AP

- [ ] **Inventory Entries**
  - Purchase receipt: DR Inventory, CR AP
  - Supplier payment: DR AP, CR Bank
  - Stock adjustment: DR Shrinkage Expense, CR Inventory

- [ ] **Payroll Entries**
  - Salary expense: DR Wages, CR Payroll Liabilities, CR Bank
  - Tax remittance: DR Payroll Liabilities, CR Bank

#### 2.2 General Ledger Management
- [ ] GL account listing with balances
- [ ] Account transaction history (ledger card)
- [ ] Account balance inquiry
- [ ] Trial balance generation
- [ ] Opening balance entry
- [ ] Adjusting journal entries
- [ ] Reversing entries

#### 2.3 Financial Statements

##### Profit & Loss Statement (Income Statement)
```
PROFIT & LOSS STATEMENT
Period: [Start Date] to [End Date]

REVENUE
  Food Sales                          $XXX,XXX
  Beverage Sales                       $XX,XXX
  Alcohol Sales                        $XX,XXX
  Other Revenue                         $X,XXX
  -------------------------------------------
  GROSS REVENUE                       $XXX,XXX
  Less: Discounts & Allowances         ($X,XXX)
  -------------------------------------------
  NET REVENUE                         $XXX,XXX

COST OF GOODS SOLD
  Food Cost                            $XX,XXX
  Beverage Cost                         $X,XXX
  Alcohol Cost                          $X,XXX
  -------------------------------------------
  TOTAL COGS                           $XX,XXX

GROSS PROFIT                          $XXX,XXX
Gross Profit Margin                       XX.X%

OPERATING EXPENSES
  Labor Costs
    Wages & Salaries                   $XX,XXX
    Payroll Taxes                       $X,XXX
    Benefits                            $X,XXX
    -------------------------------------------
    Total Labor                        $XX,XXX
    Labor Cost %                          XX.X%

  Occupancy Costs
    Rent                               $XX,XXX
    Utilities                           $X,XXX
    Insurance                           $X,XXX
    -------------------------------------------
    Total Occupancy                    $XX,XXX

  Operating Expenses
    Supplies                            $X,XXX
    Marketing                           $X,XXX
    Repairs & Maintenance               $X,XXX
    Professional Services               $X,XXX
    Other                               $X,XXX
    -------------------------------------------
    Total Operating                    $XX,XXX

  -------------------------------------------
  TOTAL OPERATING EXPENSES             $XX,XXX

OPERATING INCOME (EBITDA)              $XX,XXX
Operating Margin                          XX.X%

OTHER INCOME/(EXPENSES)
  Interest Income                         $XXX
  Interest Expense                      ($X,XXX)
  Depreciation                          ($X,XXX)
  -------------------------------------------
  Total Other                          ($X,XXX)

NET INCOME BEFORE TAX                  $XX,XXX

Income Tax Expense                      ($X,XXX)

NET INCOME                             $XX,XXX
Net Profit Margin                         XX.X%
```

##### Balance Sheet
```
BALANCE SHEET
As of: [Date]

ASSETS
  Current Assets
    Cash & Cash Equivalents            $XX,XXX
    Accounts Receivable                 $X,XXX
    Inventory                          $XX,XXX
    Prepaid Expenses                    $X,XXX
    -------------------------------------------
    Total Current Assets               $XX,XXX

  Fixed Assets
    Property & Equipment               $XX,XXX
    Less: Accumulated Depreciation    ($XX,XXX)
    -------------------------------------------
    Net Fixed Assets                   $XX,XXX

  -------------------------------------------
  TOTAL ASSETS                        $XXX,XXX

LIABILITIES
  Current Liabilities
    Accounts Payable                   $XX,XXX
    Accrued Liabilities                 $X,XXX
    Sales Tax Payable                   $X,XXX
    Payroll Liabilities                 $X,XXX
    Current Portion of Long-Term Debt   $X,XXX
    -------------------------------------------
    Total Current Liabilities          $XX,XXX

  Long-Term Liabilities
    Notes Payable                      $XX,XXX
    -------------------------------------------
    Total Long-Term Liabilities        $XX,XXX

  -------------------------------------------
  TOTAL LIABILITIES                    $XX,XXX

EQUITY
  Owner's Capital                      $XX,XXX
  Retained Earnings                    $XX,XXX
  Current Year Earnings                $XX,XXX
  -------------------------------------------
  TOTAL EQUITY                         $XX,XXX

-------------------------------------------
TOTAL LIABILITIES & EQUITY            $XXX,XXX
```

##### Cash Flow Statement
```
CASH FLOW STATEMENT
Period: [Start Date] to [End Date]

OPERATING ACTIVITIES
  Net Income                           $XX,XXX
  Adjustments:
    Depreciation & Amortization         $X,XXX
    (Increase)/Decrease in Receivables ($X,XXX)
    (Increase)/Decrease in Inventory   ($X,XXX)
    Increase/(Decrease) in Payables     $X,XXX
    Increase/(Decrease) in Accruals     $X,XXX
  -------------------------------------------
  Net Cash from Operations             $XX,XXX

INVESTING ACTIVITIES
  Purchase of Equipment               ($XX,XXX)
  -------------------------------------------
  Net Cash from Investing             ($XX,XXX)

FINANCING ACTIVITIES
  Loan Proceeds                        $XX,XXX
  Loan Repayments                     ($X,XXX)
  Owner Draws                         ($X,XXX)
  -------------------------------------------
  Net Cash from Financing              $XX,XXX

-------------------------------------------
NET CHANGE IN CASH                     $XX,XXX

Beginning Cash Balance                 $XX,XXX
Ending Cash Balance                    $XX,XXX
```

#### 2.4 Period Management
- [ ] Define accounting periods (monthly/quarterly/annual)
- [ ] Period close workflow
- [ ] Prevent posting to closed periods
- [ ] Year-end close with retained earnings roll
- [ ] Period comparison reports

#### 2.5 Account Reconciliation
- [ ] Bank reconciliation interface
- [ ] Outstanding items tracking
- [ ] Reconciliation status by account
- [ ] Adjustment entries for reconciling items

#### 2.6 Financial Dashboard
- [ ] Real-time P&L summary
- [ ] Key financial ratios
- [ ] Cash position
- [ ] Revenue vs Budget variance
- [ ] Expense trends
- [ ] Prime cost tracking (COGS + Labor / Revenue)

### 3. Database Schema Changes

```csharp
// Enhance existing ChartOfAccount
public class ChartOfAccount : BaseEntity
{
    // ... existing fields ...

    // Additional fields for full accounting
    public string AccountCode { get; set; } // 4-digit code like 1010
    public int? ParentAccountId { get; set; }
    public AccountType AccountType { get; set; }
    public AccountSubType SubType { get; set; }
    public bool IsSystemAccount { get; set; }
    public bool IsPostable { get; set; } // Can post JE to this account
    public bool IsBankAccount { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int? DefaultTaxRateId { get; set; }
    public bool IsReconcilable { get; set; }
    public string CashFlowCategory { get; set; } // Operating, Investing, Financing

    // Navigation
    public virtual ChartOfAccount ParentAccount { get; set; }
    public virtual ICollection<ChartOfAccount> ChildAccounts { get; set; }
}

public enum AccountSubType
{
    // Assets
    Cash,
    Bank,
    AccountsReceivable,
    Inventory,
    PrepaidExpense,
    FixedAsset,
    AccumulatedDepreciation,

    // Liabilities
    AccountsPayable,
    AccruedLiabilities,
    TaxPayable,
    ShortTermDebt,
    LongTermDebt,

    // Equity
    OwnersCapital,
    RetainedEarnings,
    Draws,

    // Revenue
    SalesRevenue,
    ServiceRevenue,
    OtherIncome,

    // Expenses
    CostOfGoodsSold,
    LaborCost,
    OccupancyCost,
    OperatingExpense,
    OtherExpense
}

// Journal Entry Enhancement
public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; set; } // JE-2024-00001
    public int AccountingPeriodId { get; set; }
    public DateTime EntryDate { get; set; }
    public string Description { get; set; }
    public JournalEntryType EntryType { get; set; }
    public JournalEntryStatus Status { get; set; }
    public bool IsAutoGenerated { get; set; }
    public string SourceType { get; set; } // Receipt, Expense, Payroll, etc.
    public int? SourceId { get; set; }
    public int? ReversedFromId { get; set; }
    public bool IsReversing { get; set; }
    public DateTime? ReversalDate { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public virtual ICollection<JournalEntryLine> Lines { get; set; }
}

public enum JournalEntryType
{
    Standard,
    Adjusting,
    Closing,
    Reversing,
    Opening
}

public enum JournalEntryStatus
{
    Draft,
    PendingApproval,
    Posted,
    Reversed
}

// New: GL Transaction Mapping
public class GLAccountMapping : BaseEntity
{
    public string TransactionType { get; set; } // CashSale, CardSale, Refund, Expense, etc.
    public int? PaymentMethodId { get; set; }
    public int? CategoryId { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public int DebitAccountId { get; set; }
    public int CreditAccountId { get; set; }
    public string Description { get; set; }

    public virtual ChartOfAccount DebitAccount { get; set; }
    public virtual ChartOfAccount CreditAccount { get; set; }
}

// New: Bank Reconciliation
public class BankReconciliation : BaseEntity
{
    public int BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal ReconciledBalance { get; set; }
    public decimal Difference { get; set; }
    public BankReconciliationStatus Status { get; set; }
    public int? ReconciledByUserId { get; set; }
    public DateTime? ReconciledAt { get; set; }

    public virtual ChartOfAccount BankAccount { get; set; }
    public virtual ICollection<BankReconciliationItem> Items { get; set; }
}

public class BankReconciliationItem : BaseEntity
{
    public int BankReconciliationId { get; set; }
    public int? JournalEntryLineId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsCleared { get; set; }
    public DateTime? ClearedDate { get; set; }
}

// Period Close Tracking
public class PeriodClose : BaseEntity
{
    public int AccountingPeriodId { get; set; }
    public PeriodCloseStatus Status { get; set; }
    public DateTime? SoftCloseDate { get; set; }
    public DateTime? HardCloseDate { get; set; }
    public int? ClosedByUserId { get; set; }
    public string CloseNotes { get; set; }

    // Checklist items
    public bool AllTransactionsPosted { get; set; }
    public bool BankReconciled { get; set; }
    public bool InventoryReconciled { get; set; }
    public bool AdjustingEntriesComplete { get; set; }
    public bool FinancialStatementsReviewed { get; set; }
}
```

### 4. Service Interfaces

```csharp
public interface IAccountingService
{
    // Chart of Accounts
    Task<ChartOfAccount> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<ChartOfAccount> GetAccountAsync(int accountId, CancellationToken ct = default);
    Task<IEnumerable<ChartOfAccount>> GetAccountsAsync(AccountFilterDto filter, CancellationToken ct = default);
    Task<ChartOfAccount> UpdateAccountAsync(int accountId, UpdateAccountDto dto, CancellationToken ct = default);
    Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null, CancellationToken ct = default);
    Task<IEnumerable<ChartOfAccount>> GetChartOfAccountsTreeAsync(CancellationToken ct = default);

    // Journal Entries
    Task<JournalEntry> CreateJournalEntryAsync(CreateJournalEntryDto dto, CancellationToken ct = default);
    Task<JournalEntry> GetJournalEntryAsync(int entryId, CancellationToken ct = default);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryFilterDto filter, CancellationToken ct = default);
    Task<JournalEntry> PostJournalEntryAsync(int entryId, CancellationToken ct = default);
    Task<JournalEntry> ReverseJournalEntryAsync(int entryId, DateTime reversalDate, CancellationToken ct = default);

    // Auto-Generation
    Task<JournalEntry> GenerateReceiptJournalEntryAsync(int receiptId, CancellationToken ct = default);
    Task<JournalEntry> GenerateExpenseJournalEntryAsync(int expenseId, CancellationToken ct = default);
    Task<JournalEntry> GenerateInventoryJournalEntryAsync(int stockMovementId, CancellationToken ct = default);
    Task<JournalEntry> GeneratePayrollJournalEntryAsync(int payrollPeriodId, CancellationToken ct = default);

    // Period Management
    Task<AccountingPeriod> GetCurrentPeriodAsync(CancellationToken ct = default);
    Task<AccountingPeriod> CreatePeriodAsync(CreatePeriodDto dto, CancellationToken ct = default);
    Task ClosePeriodAsync(int periodId, CancellationToken ct = default);
    Task ReopenPeriodAsync(int periodId, string reason, CancellationToken ct = default);

    // GL Mapping
    Task<GLAccountMapping> GetMappingAsync(string transactionType, int? paymentMethodId = null, int? categoryId = null, CancellationToken ct = default);
    Task<GLAccountMapping> CreateMappingAsync(GLAccountMappingDto dto, CancellationToken ct = default);
}

public interface IFinancialStatementService
{
    // Trial Balance
    Task<TrialBalanceReport> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<TrialBalanceReport> GetTrialBalanceForPeriodAsync(int periodId, CancellationToken ct = default);

    // Income Statement / P&L
    Task<ProfitLossStatement> GetProfitLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<ProfitLossStatement> GetProfitLossForPeriodAsync(int periodId, CancellationToken ct = default);
    Task<ComparativePLStatement> GetComparativePLAsync(int currentPeriodId, int comparePeriodId, CancellationToken ct = default);
    Task<DepartmentalPLStatement> GetDepartmentalPLAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Balance Sheet
    Task<BalanceSheetReport> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<ComparativeBalanceSheet> GetComparativeBalanceSheetAsync(DateTime currentDate, DateTime compareDate, CancellationToken ct = default);

    // Cash Flow
    Task<CashFlowStatement> GetCashFlowStatementAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // General Ledger
    Task<GeneralLedgerReport> GetGeneralLedgerAsync(int accountId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<GeneralLedgerReport> GetGeneralLedgerAllAccountsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Export
    Task<byte[]> ExportStatementToPdfAsync(string statementType, object parameters, CancellationToken ct = default);
    Task<byte[]> ExportStatementToExcelAsync(string statementType, object parameters, CancellationToken ct = default);
}

public interface IBankReconciliationService
{
    Task<BankReconciliation> CreateReconciliationAsync(int bankAccountId, DateTime statementDate, decimal statementBalance, CancellationToken ct = default);
    Task<BankReconciliation> GetReconciliationAsync(int reconciliationId, CancellationToken ct = default);
    Task<IEnumerable<JournalEntryLine>> GetUnreconciledTransactionsAsync(int bankAccountId, CancellationToken ct = default);
    Task ClearTransactionAsync(int reconciliationId, int journalEntryLineId, DateTime clearedDate, CancellationToken ct = default);
    Task<BankReconciliation> CompleteReconciliationAsync(int reconciliationId, CancellationToken ct = default);
}
```

### 5. UI Components

#### 5.1 Chart of Accounts View
- Tree view showing account hierarchy
- Add/edit account forms
- Account balance display
- Quick filters by type
- Search functionality

#### 5.2 Journal Entry View
- Entry form with debit/credit lines
- Balance validation (debits = credits)
- Source document reference
- Draft/Post workflow
- Entry history with filters

#### 5.3 Financial Dashboard
- Revenue summary cards
- Expense breakdown chart
- Cash position
- Key ratios (Gross Margin, Net Margin, Prime Cost %)
- Period comparison sparklines

#### 5.4 P&L Statement View
- Period selector
- Drill-down capability (click line to see transactions)
- Comparison mode (vs prior period, vs budget)
- Export options
- Print-friendly format

#### 5.5 Balance Sheet View
- As-of date selector
- Account drill-down
- Comparison mode
- Export options

#### 5.6 General Ledger View
- Account selector
- Date range filter
- Running balance display
- Transaction details on click
- Export to Excel

#### 5.7 Bank Reconciliation View
- Bank account selector
- Statement entry form
- Uncleared transactions list
- Clear/unclear toggle
- Reconciliation summary
- Variance display

### 6. Business Rules

1. **Journal Entry Validation**
   - Debits must equal credits
   - Cannot post to non-postable accounts
   - Cannot post to closed periods
   - Cannot post to inactive accounts

2. **Period Management**
   - Soft close allows adjusting entries only
   - Hard close prevents all posting
   - Year-end close creates retained earnings entry

3. **Auto-Generation Rules**
   - Receipt settlement triggers sales JE
   - Expense approval triggers expense JE
   - GRN posting triggers inventory JE
   - Payroll finalization triggers payroll JE

4. **Reconciliation**
   - Bank reconciliation difference must be zero to complete
   - Reconciled transactions cannot be modified
   - Adjustment entries created for reconciling items

5. **Financial Statement Integrity**
   - Balance Sheet must balance (Assets = Liabilities + Equity)
   - P&L Net Income flows to Balance Sheet
   - Cash Flow ending balance matches Balance Sheet cash

## Acceptance Criteria

- [ ] Chart of Accounts can be created with hierarchical structure
- [ ] Journal entries follow double-entry with balanced debits/credits
- [ ] Auto-generated entries created for sales, expenses, inventory, payroll
- [ ] P&L statement generated accurately for any date range
- [ ] Balance Sheet generated and balanced
- [ ] General Ledger shows all transactions by account
- [ ] Trial Balance produced with correct balances
- [ ] Accounting periods can be opened and closed
- [ ] Period close prevents posting to closed periods
- [ ] Bank reconciliation workflow functional
- [ ] Financial dashboard shows real-time metrics
- [ ] Reports exportable to PDF/Excel
- [ ] GL account mappings configurable for transaction types

## Implementation Notes

### Existing Code to Leverage
- `ChartOfAccount`, `JournalEntry`, `JournalEntryLine` entities
- `AccountingPeriod` entity
- `IFinancialReportingService` interface
- `Expense` and `ExpenseCategory` with GL mapping
- Budget infrastructure

### Integration Points
- Receipt settlement triggers accounting entry
- Expense approval triggers accounting entry
- Inventory movements trigger accounting entry
- Payroll processing triggers accounting entry
- Z Report can post daily summary entry

### Migration Considerations
- Seed standard Chart of Accounts
- Create GL account mappings for payment methods
- Set up default period structure
- Configure opening balances

## References

- [Restaurant365 - POS Integration](https://docs.restaurant365.com/docs/pos-integration-overview)
- [QuickBooks - Record Daily Sales](https://quickbooks.intuit.com/learn-support/en-us/help-article/sales-receipts/record-total-daily-sales-quickbooks-online/L0rHb69Mh_US_en_US)
- [Sage - Restaurant Accounting](https://www.sage.com/en-us/blog/ultimate-guide-to-restaurant-accounting/)
- [Restaurant CFO - Chart of Accounts](https://therestaurantcfo.com/a-restaurant-chart-of-accounts-that-any-bookkeeper-or-accountant-can-use/)
- [NaviPartner - POS Accounting Entries](https://docs.navipartner.com/docs/retail/posting_setup/explanation/accounting_entries/)

---

**Priority**: High
**Estimated Complexity**: Very Large (Epic)
**Labels**: feature, accounting, finance, reporting
