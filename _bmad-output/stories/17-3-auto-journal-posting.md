# Story 17.3: Auto Journal Posting

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/AccountingService.cs` - Auto journal posting with:
  - `PostSalesJournalAsync` - Sales transactions (Dr Cash, Cr Revenue)
  - `PostPaymentJournalAsync` - Payment receipts (Dr Cash, Cr AR)
  - `PostPurchaseJournalAsync` - Purchases (Dr Inventory, Cr AP)
  - `PostPayrollJournalAsync` - Payroll with Kenya statutory deductions
  - `ReverseJournalEntryAsync` - Entry reversals

## Story

As the system,
I want to automatically create journal entries for transactions,
So that the ledger is always current without manual entry.

## Acceptance Criteria

1. **Given** a sale is completed
   **When** receipt is settled
   **Then** journal entry posts: Dr Cash/AR, Cr Sales Revenue

2. **Given** a purchase is received on credit
   **When** delivery is confirmed
   **Then** journal entry posts: Dr Inventory, Cr Accounts Payable

3. **Given** payroll is approved
   **When** period is closed
   **Then** journal entry posts: Dr Salaries Expense, Cr Cash/Salaries Payable

## Tasks / Subtasks

- [ ] Task 1: Create Journal Entry Entities
  - [ ] Create JournalEntry class
  - [ ] Create JournalEntryLine class
  - [ ] Add EF Core configuration
  - [ ] Create migration

- [ ] Task 2: Create Journal Posting Service
  - [ ] Create IJournalPostingService interface
  - [ ] Implement PostSaleJournal method
  - [ ] Implement PostPurchaseJournal method
  - [ ] Implement PostPayrollJournal method
  - [ ] Implement PostExpenseJournal method

- [ ] Task 3: Integrate with Existing Transactions
  - [ ] Hook into receipt settlement
  - [ ] Hook into delivery confirmation
  - [ ] Hook into payroll approval
  - [ ] Hook into expense approval

- [ ] Task 4: Create Journal Entry View
  - [ ] Create JournalEntriesView.xaml
  - [ ] List all journal entries
  - [ ] View entry details
  - [ ] Filter by date/type

## Dev Notes

### JournalEntry Entity

```csharp
public class JournalEntry
{
    public int Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryType EntryType { get; set; }
    public string? ReferenceType { get; set; }  // Receipt, Delivery, Payroll, Expense
    public int? ReferenceId { get; set; }
    public bool IsPosted { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

public class JournalEntryLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public int ChartOfAccountId { get; set; }
    public ChartOfAccount ChartOfAccount { get; set; } = null!;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}

public enum JournalEntryType
{
    Sale,
    Purchase,
    Payroll,
    Expense,
    Manual
}
```

### Auto-Posting Rules

#### Sale Transaction
```
Dr 1010 Cash on Hand          [Total Amount]
   Cr 4000 Sales Revenue         [Net Amount]
   Cr 2210 VAT Payable           [Tax Amount]
```

#### Credit Sale
```
Dr 1100 Accounts Receivable   [Total Amount]
   Cr 4000 Sales Revenue         [Net Amount]
   Cr 2210 VAT Payable           [Tax Amount]
```

#### Purchase on Credit
```
Dr 1210 Merchandise Inventory [Total Amount]
   Cr 2000 Accounts Payable      [Total Amount]
```

#### Payroll
```
Dr 5100 Salaries & Wages      [Gross Pay]
   Cr 1010/1020 Cash             [Net Pay]
   Cr 2220 PAYE Payable          [PAYE Amount]
   Cr 2230 NSSF/NHIF Payable     [Statutory Amount]
```

#### Expense
```
Dr 5xxx [Expense Account]     [Amount]
   Cr 1010/1020 Cash             [Amount]
```

### IJournalPostingService Interface

```csharp
public interface IJournalPostingService
{
    Task<JournalEntry> PostSaleJournalAsync(Receipt receipt);
    Task<JournalEntry> PostPurchaseJournalAsync(Delivery delivery);
    Task<JournalEntry> PostPayrollJournalAsync(PayrollPeriod payroll);
    Task<JournalEntry> PostExpenseJournalAsync(Expense expense);
    Task<JournalEntry> CreateManualEntryAsync(JournalEntryDto dto);
}
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
