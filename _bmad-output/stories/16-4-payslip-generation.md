# Story 16.4: Payslip Generation

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/PayrollService.cs` - Payslip generation with:
  - `GeneratePayslipAsync` / `GenerateAllPayslipsAsync` - Generate payslips
  - `CalculateStatutoryDeductionsAsync` - Kenya PAYE, NHIF, NSSF, Housing Levy
  - `RecalculatePayslipAsync` - Recalculate after changes
  - `GeneratePayslipHtmlAsync` - Printable payslip format

## Story

As an employee,
I want to receive a detailed payslip,
So that I understand my pay breakdown.

## Acceptance Criteria

1. **Given** approved payroll
   **When** generating payslips
   **Then** creates individual payslip for each employee

2. **Given** a payslip
   **When** viewing/printing
   **Then** shows: basic salary, all earnings, all deductions, net pay

## Tasks / Subtasks

- [ ] Task 1: Create Payslip Entities
  - [ ] Create Payslip class
  - [ ] Create PayslipDetail class
  - [ ] Add EF Core configuration

- [ ] Task 2: Create Payslip View
  - [ ] Create PayslipView.xaml
  - [ ] Display employee info
  - [ ] Display earnings section
  - [ ] Display deductions section
  - [ ] Display net pay

- [ ] Task 3: Implement Payslip Printing
  - [ ] Create payslip print template
  - [ ] Print individual payslip
  - [ ] Batch print all payslips

## Dev Notes

### Payslip Template

```
+------------------------------------------+
|              PAYSLIP                      |
|          December 2025                    |
+------------------------------------------+
| Employee: John Doe                        |
| ID: EMP-001                              |
| Position: Sales Associate                 |
+------------------------------------------+
| EARNINGS                                  |
|   Basic Salary          KSh 50,000.00    |
|   House Allowance       KSh 10,000.00    |
|   Transport             KSh  5,000.00    |
|   ─────────────────────────────────────  |
|   Gross Pay             KSh 65,000.00    |
+------------------------------------------+
| DEDUCTIONS                                |
|   PAYE                  KSh  8,500.00    |
|   NSSF                  KSh  2,160.00    |
|   NHIF                  KSh  1,700.00    |
|   ─────────────────────────────────────  |
|   Total Deductions      KSh 12,360.00    |
+------------------------------------------+
| NET PAY                 KSh 52,640.00    |
+------------------------------------------+
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
