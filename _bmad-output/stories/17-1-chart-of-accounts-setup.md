# Story 17.1: Chart of Accounts Setup

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/AccountingService.cs` (981 lines) - Chart of accounts with:
  - `CreateAccountAsync` / `UpdateAccountAsync` / `DeleteAccountAsync` - Account CRUD
  - `GetAccountsByTypeAsync` - Filter by Asset, Liability, Equity, Revenue, Expense
  - `SeedDefaultAccountsAsync` - Kenya-specific defaults including VAT, PAYE, NHIF, NSSF, Housing Levy payables

## Story

As an accountant,
I want a chart of accounts,
So that transactions are properly categorized.

## Acceptance Criteria

1. **Given** first-time setup
   **When** accounting module initializes
   **Then** default chart of accounts is created (Assets, Liabilities, Revenue, Expenses)

2. **Given** existing accounts
   **When** adding custom accounts
   **Then** can create under appropriate parent category

## Tasks / Subtasks

- [ ] Task 1: Create Chart of Accounts Entity
  - [ ] Create ChartOfAccount class
  - [ ] Add AccountType enum
  - [ ] Add EF Core configuration
  - [ ] Create migration

- [ ] Task 2: Seed Default Chart of Accounts
  - [ ] Create default account categories
  - [ ] Create default accounts per category
  - [ ] Run seed on first launch

- [ ] Task 3: Create Chart of Accounts View
  - [ ] Create ChartOfAccountsView.xaml
  - [ ] Hierarchical tree view
  - [ ] Add/Edit/Deactivate accounts

## Dev Notes

### ChartOfAccount Entity

```csharp
public class ChartOfAccount
{
    public int Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public int? ParentAccountId { get; set; }
    public ChartOfAccount? ParentAccount { get; set; }
    public bool IsSystemAccount { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}
```

### Default Chart of Accounts Structure

```
ASSETS (1000-1999)
├── 1000 Cash and Cash Equivalents
│   ├── 1010 Cash on Hand
│   ├── 1020 Cash in Bank
│   └── 1030 Petty Cash
├── 1100 Accounts Receivable
├── 1200 Inventory
│   └── 1210 Merchandise Inventory
└── 1300 Fixed Assets
    ├── 1310 Equipment
    └── 1320 Furniture & Fixtures

LIABILITIES (2000-2999)
├── 2000 Accounts Payable
├── 2100 Salaries Payable
├── 2200 Taxes Payable
│   ├── 2210 VAT Payable
│   ├── 2220 PAYE Payable
│   └── 2230 NSSF/NHIF Payable
└── 2300 Other Liabilities

EQUITY (3000-3999)
├── 3000 Owner's Capital
└── 3100 Retained Earnings

REVENUE (4000-4999)
├── 4000 Sales Revenue
├── 4100 Service Revenue
└── 4200 Other Income

EXPENSES (5000-5999)
├── 5000 Cost of Goods Sold
├── 5100 Salaries & Wages
├── 5200 Rent Expense
├── 5300 Utilities Expense
├── 5400 Office Supplies
└── 5500 Other Expenses
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
