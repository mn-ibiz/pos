# Story 15.1: Supplier Payment Terms

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/SupplierCreditService.cs` (557 lines) - Payment terms with:
  - `UpdateCreditTermsAsync` - Set COD, Net 15, Net 30, Net 60
  - `IsCreditLimitExceededAsync` - Credit limit checking
  - `GetAvailableCreditAsync` - Available credit calculation

## Story

As a procurement manager,
I want to configure payment terms for each supplier,
So that I know when payments are due.

## Acceptance Criteria

1. **Given** creating/editing a supplier
   **When** entering details
   **Then** can set Payment Terms: COD, Net 15, Net 30, Net 60

2. **Given** supplier has credit terms
   **When** viewing supplier
   **Then** shows current balance owed and credit limit

## Tasks / Subtasks

- [ ] Task 1: Update Supplier Entity
  - [ ] Add PaymentTermDays field
  - [ ] Add CreditLimit field
  - [ ] Add CurrentBalance field
  - [ ] Create migration

- [ ] Task 2: Update Supplier Editor
  - [ ] Add Payment Terms dropdown (COD, Net 15, Net 30, Net 60)
  - [ ] Add Credit Limit input
  - [ ] Display Current Balance (read-only)

- [ ] Task 3: Create Balance Tracking
  - [ ] Update balance on credit delivery
  - [ ] Reduce balance on payment
  - [ ] Alert when approaching credit limit

## Dev Notes

### Updated Supplier Entity

```csharp
public class Supplier
{
    // ... existing properties

    // Credit Terms
    public int PaymentTermDays { get; set; } = 0; // 0 = COD
    public decimal CreditLimit { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
}
```

### Payment Terms Options

| Option | Days | Description |
|--------|------|-------------|
| COD | 0 | Cash on Delivery |
| Net 15 | 15 | Payment due in 15 days |
| Net 30 | 30 | Payment due in 30 days |
| Net 60 | 60 | Payment due in 60 days |

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
