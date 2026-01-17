# Story 16.2: Salary Component Configuration

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/PayrollService.cs` (725 lines) - Salary components with:
  - `GetAllSalaryComponentsAsync` / `CreateSalaryComponentAsync` - Component management
  - `GetEmployeeSalaryComponentsAsync` / `AddEmployeeSalaryComponentAsync` - Per-employee
  - Kenya statutory components: PAYE, NHIF, NSSF, Housing Levy pre-configured

## Story

As an administrator,
I want to configure salary components,
So that payroll calculations are automated.

## Acceptance Criteria

1. **Given** accessing Salary Settings
   **When** creating component
   **Then** can specify: name, type (earning/deduction), fixed/percentage, taxable

2. **Given** statutory deductions
   **When** configured
   **Then** PAYE, NSSF, NHIF are auto-calculated based on rates

## Tasks / Subtasks

- [ ] Task 1: Create Salary Component Entity
  - [ ] Create SalaryComponent class
  - [ ] Create EmployeeSalaryComponent class
  - [ ] Add EF Core configuration
  - [ ] Seed default components (PAYE, NSSF, NHIF)

- [ ] Task 2: Create Salary Settings View
  - [ ] Create SalarySettingsView.xaml
  - [ ] List all components
  - [ ] Add/Edit/Delete components

- [ ] Task 3: Implement Statutory Calculations
  - [ ] PAYE tax brackets
  - [ ] NSSF tiers
  - [ ] NHIF bands

## Dev Notes

### Default Salary Components

| Name | Type | Fixed/% | Taxable | Statutory |
|------|------|---------|---------|-----------|
| Basic Salary | Earning | Fixed | Yes | No |
| House Allowance | Earning | Fixed | Yes | No |
| Transport Allowance | Earning | Fixed | Yes | No |
| PAYE | Deduction | % | N/A | Yes |
| NSSF | Deduction | % | N/A | Yes |
| NHIF | Deduction | Fixed | N/A | Yes |

### SalaryComponent Entity

```csharp
public class SalaryComponent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }
    public bool IsFixed { get; set; } = true;
    public decimal? DefaultAmount { get; set; }
    public decimal? DefaultPercent { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsStatutory { get; set; } = false;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ComponentType { Earning, Deduction }
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
