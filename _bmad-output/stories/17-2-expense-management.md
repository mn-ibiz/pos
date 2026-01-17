# Story 17.2: Expense Management

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/AccountingService.cs` - Expense management with:
  - `PostExpenseJournalAsync` - Automatic journal entries for expenses
  - Expense entity integration with categories
  - Debit expense accounts, credit cash/bank accounts

## Story

As an accounts clerk,
I want to record business expenses,
So that all costs are tracked.

## Acceptance Criteria

1. **Given** accessing Expenses
   **When** creating expense
   **Then** enter: category, description, amount, date, payment method

2. **Given** expense entered
   **When** saving
   **Then** can attach receipt image (optional)

3. **Given** expenses require approval
   **When** manager reviews
   **Then** can approve or reject with reason

## Tasks / Subtasks

- [ ] Task 1: Create Expense Entities
  - [ ] Create Expense class
  - [ ] Create ExpenseCategory class
  - [ ] Add EF Core configuration
  - [ ] Create migration

- [ ] Task 2: Seed Default Expense Categories
  - [ ] Create common expense categories
  - [ ] Link to chart of accounts

- [ ] Task 3: Create Expenses View
  - [ ] Create ExpensesView.xaml
  - [ ] List expenses with filters
  - [ ] Add/Edit expense dialog

- [ ] Task 4: Implement Receipt Attachment
  - [ ] Image upload control
  - [ ] Store in local folder
  - [ ] Display receipt preview

- [ ] Task 5: Implement Approval Workflow
  - [ ] Pending approval list
  - [ ] Approve/Reject buttons
  - [ ] Rejection reason dialog

## Dev Notes

### Expense Entity

```csharp
public class Expense
{
    public int Id { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? ReceiptImagePath { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? JournalEntryId { get; set; }
}

public class ExpenseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
    public ChartOfAccount? ChartOfAccount { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public decimal? ApprovalThreshold { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ExpenseStatus { Pending, Approved, Rejected, Posted }
public enum PaymentMethod { Cash, BankTransfer, Mpesa, Check, PettyCash }
```

### Default Expense Categories

| Category | Requires Approval | Approval Threshold |
|----------|-------------------|-------------------|
| Office Supplies | No | - |
| Utilities | No | - |
| Rent | No | - |
| Transport | Yes | KSh 5,000 |
| Meals & Entertainment | Yes | KSh 2,000 |
| Equipment | Yes | KSh 10,000 |
| Repairs & Maintenance | Yes | KSh 5,000 |
| Miscellaneous | Yes | KSh 1,000 |

### Expense Workflow

```
[Create] → [Pending] → [Approve/Reject] → [Approved] → [Post to Journal]
                              ↓
                         [Rejected]
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
