# Story 15.2: Delivery Payment Status

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/SupplierCreditService.cs` - Invoice status tracking with:
  - `UpdateInvoiceStatusAsync` - Mark Unpaid, PartiallyPaid, Paid, Overdue
  - `GetOutstandingInvoicesAsync` - Unpaid invoice list
  - `GetOverdueInvoicesAsync` - Overdue invoice list

## Story

As a receiving clerk,
I want to mark whether a delivery is paid or on credit,
So that accounts payable is accurately tracked.

## Acceptance Criteria

1. **Given** receiving goods on a PO
   **When** completing receipt
   **Then** must select: Paid Now or Credit (use supplier terms)

2. **Given** Credit is selected
   **When** delivery is completed
   **Then** supplier balance is updated, due date calculated from terms

3. **Given** Paid Now is selected
   **When** delivery is completed
   **Then** no accounts payable is created

## Tasks / Subtasks

- [ ] Task 1: Update Goods Receiving Flow
  - [ ] Add payment status selection to receiving screen
  - [ ] Radio buttons: Paid / Credit
  - [ ] Show supplier payment terms

- [ ] Task 2: Implement Credit Processing
  - [ ] Calculate due date from supplier terms
  - [ ] Update supplier CurrentBalance
  - [ ] Create SupplierInvoice record

- [ ] Task 3: Implement Paid Processing
  - [ ] Mark PO as paid
  - [ ] No balance update needed
  - [ ] Record payment method

## Dev Notes

### Updated PurchaseOrder

```csharp
public class PurchaseOrder
{
    // ... existing properties

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal AmountPaid { get; set; } = 0;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? InvoiceNumber { get; set; }
}

public enum PaymentStatus
{
    Paid,
    Unpaid,
    PartiallyPaid
}
```

### Receiving Screen Addition

```
+------------------------------------------+
|        GOODS RECEIVING - PO #12345       |
+------------------------------------------+
| Supplier: ABC Distributors               |
| Payment Terms: Net 30                    |
| Total: KSh 50,000.00                     |
|                                          |
| Payment Status:                          |
|   (•) Credit (Due: 2025-02-15)          |
|   ( ) Paid Now                           |
|                                          |
| Supplier Invoice #: [INV-2025-001    ]   |
|                                          |
|        [Cancel]        [Complete]        |
+------------------------------------------+
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
