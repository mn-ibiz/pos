# Story 15.3: Supplier Invoice Tracking

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/SupplierCreditService.cs` - Invoice tracking with:
  - `CreateInvoiceAsync` - Create supplier invoices
  - `GetSupplierInvoicesAsync` / `GetInvoiceByIdAsync` - Invoice retrieval
  - `RecordPaymentAsync` - Record payments against invoices
  - `GetInvoicePaymentsAsync` - Payment history

## Story

As an accounts clerk,
I want to track supplier invoices and payments,
So that I know what we owe and when.

## Acceptance Criteria

1. **Given** a credit delivery received
   **When** invoice is entered
   **Then** can record supplier's invoice number and date

2. **Given** invoices exist
   **When** viewing Accounts Payable
   **Then** list shows all unpaid invoices with due dates

3. **Given** making a payment
   **When** recording payment
   **Then** can apply to one or more invoices (partial or full)

## Tasks / Subtasks

- [ ] Task 1: Create Supplier Invoice Entity
  - [ ] Create SupplierInvoice class
  - [ ] Create SupplierPayment class
  - [ ] Add EF Core configuration
  - [ ] Create migration

- [ ] Task 2: Create Accounts Payable View
  - [ ] Create AccountsPayableView.xaml
  - [ ] Create AccountsPayableViewModel
  - [ ] List unpaid invoices
  - [ ] Filter by supplier, status, due date

- [ ] Task 3: Create Payment Recording Dialog
  - [ ] Create RecordPaymentDialog.xaml
  - [ ] Select invoices to pay
  - [ ] Enter payment amount
  - [ ] Payment method and reference

- [ ] Task 4: Implement Payment Application
  - [ ] Apply payment to selected invoices
  - [ ] Handle partial payments
  - [ ] Update supplier balance

## Dev Notes

### SupplierInvoice Entity

```csharp
public class SupplierInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public decimal BalanceDue => TotalAmount - PaidAmount;
    public bool IsOverdue => Status != InvoiceStatus.Paid && DateTime.Now > DueDate;
}
```

### Accounts Payable List

```
+------------------------------------------------------------------+
|                    ACCOUNTS PAYABLE                               |
+------------------------------------------------------------------+
| Filter: [All Suppliers ▼] [Unpaid ▼] [This Month ▼]  [Search]    |
+------------------------------------------------------------------+
| Supplier      | Invoice #   | Date       | Due Date   | Amount    |
|---------------|-------------|------------|------------|-----------|
| ABC Dist.     | INV-001     | 2025-01-15 | 2025-02-14 | KSh 50,000|
| XYZ Trading   | INV-045     | 2025-01-10 | 2025-02-09 | KSh 25,000|
| ** OVERDUE ** | INV-032     | 2024-12-15 | 2025-01-14 | KSh 15,000|
+------------------------------------------------------------------+
| Total Outstanding: KSh 90,000    [Record Payment]                 |
+------------------------------------------------------------------+
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
