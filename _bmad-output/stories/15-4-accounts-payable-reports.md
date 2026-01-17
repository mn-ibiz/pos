# Story 15.4: Accounts Payable Reports

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/SupplierCreditService.cs` - AP reports with:
  - `GetAgingSummaryAsync` - Aging report (Current, 30, 60, 90+ days)
  - `GetAllAgingSummariesAsync` - All suppliers aging
  - `GenerateStatementAsync` - Supplier statement with opening/closing balance
  - `RecalculateBalanceAsync` - Balance recalculation

## Story

As a manager,
I want accounts payable reports,
So that I can manage cash flow and supplier relationships.

## Acceptance Criteria

1. **Given** unpaid invoices exist
   **When** running AP Aging Report
   **Then** shows invoices grouped by age (Current, 30, 60, 90+ days)

2. **Given** a supplier selected
   **When** running Supplier Statement
   **Then** shows all transactions and running balance

## Tasks / Subtasks

- [ ] Task 1: Create AP Aging Report
  - [ ] Create APAgingReportView.xaml
  - [ ] Group by aging buckets (Current, 30, 60, 90+)
  - [ ] Summary totals per bucket
  - [ ] Export to PDF/Excel

- [ ] Task 2: Create Supplier Statement Report
  - [ ] Create SupplierStatementView.xaml
  - [ ] Show all invoices and payments
  - [ ] Running balance column
  - [ ] Date range filter

## Dev Notes

### AP Aging Report

```
+------------------------------------------------------------------+
|            ACCOUNTS PAYABLE AGING REPORT                          |
|                    As of: 2025-01-15                              |
+------------------------------------------------------------------+
| Supplier     | Current | 1-30 Days | 31-60 Days | 61-90 | 90+    |
|--------------|---------|-----------|------------|-------|--------|
| ABC Dist.    | 50,000  | 25,000    | 0          | 0     | 0      |
| XYZ Trading  | 0       | 15,000    | 10,000     | 0     | 5,000  |
| 123 Supplies | 30,000  | 0         | 0          | 0     | 0      |
|--------------|---------|-----------|------------|-------|--------|
| TOTALS       | 80,000  | 40,000    | 10,000     | 0     | 5,000  |
+------------------------------------------------------------------+
| TOTAL PAYABLE: KSh 135,000                                        |
+------------------------------------------------------------------+
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
