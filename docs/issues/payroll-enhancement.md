## Overview

Enhance the Payroll module with additional Kenya-specific statutory deductions, advanced features, and best practice implementations based on current Kenya Revenue Authority (KRA) and statutory body requirements.

## Current State Analysis

### Existing Implementation (GOOD)
Located: `HospitalityPOS.Infrastructure/Services/PayrollService.cs`

**Currently Implemented:**
- ✓ PAYE (Pay As You Earn) - Income tax
- ✓ NHIF (National Hospital Insurance Fund)
- ✓ NSSF (National Social Security Fund)
- ✓ Housing Levy (2% of gross)
- ✓ Employee entity with NssfNumber, NhifNumber, HelbNumber
- ✓ PayrollPeriod with approval workflow
- ✓ Payslip generation with details

### What Needs Enhancement

1. **HELB Deduction** - Field exists but no automated deduction
2. **SHIF** - New Social Health Insurance Fund (replacing NHIF)
3. **Tax Reliefs** - Personal relief, insurance relief
4. **Pension Contributions** - Beyond NSSF
5. **Loan Deductions** - Staff advances, salary loans
6. **Overtime & Allowances** - House, transport, etc.
7. **Leave Management Integration**
8. **P9 Form Generation** - Annual tax certificate
9. **Statutory Reports** - NSSF returns, NHIF returns

## Requirements

### 1. SHIF/SHA Implementation (2024 Transition)

Kenya transitioned from NHIF to SHIF (Social Health Insurance Fund) under SHA (Social Health Authority).

```csharp
// SHIF Calculation (2.75% of gross salary)
public decimal CalculateSHIF(decimal grossSalary)
{
    decimal shif = grossSalary * 0.0275m;
    return Math.Max(shif, 300m); // Minimum KES 300
}
```

### 2. HELB Deduction Automation

For employees with HELB loans:

```csharp
public class HelbDeduction
{
    public int EmployeeId { get; set; }
    public string HelbNumber { get; set; }
    public decimal MonthlyDeduction { get; set; }
    public decimal TotalLoanAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceRemaining { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
```

### 3. Tax Reliefs Enhancement

```csharp
public class TaxReliefs
{
    // Personal Relief - KES 2,400 per month (KES 28,800 per year)
    public decimal PersonalRelief { get; set; } = 2400m;

    // Insurance Relief - 15% of premiums paid, max KES 5,000/month
    public decimal InsuranceRelief { get; set; }

    // Disability Relief - If applicable
    public decimal DisabilityRelief { get; set; }

    // Mortgage Interest Relief - Up to KES 30,000/month
    public decimal MortgageInterestRelief { get; set; }

    // Pension Contribution Relief - Contributions to registered schemes
    public decimal PensionContributionRelief { get; set; }
}
```

### 4. Allowances and Benefits

```csharp
public class EmployeeAllowances
{
    public decimal HouseAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal ActingAllowance { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; } // 1.5x or 2x normal rate
    public decimal CommissionEarned { get; set; }
    public decimal BonusAmount { get; set; }
}
```

### 5. Loan and Advance Deductions

```csharp
public class EmployeeLoan : BaseEntity
{
    public int EmployeeId { get; set; }
    public LoanType Type { get; set; } // SalaryAdvance, StaffLoan, CooperativeLoan
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyDeduction { get; set; }
    public int TotalInstallments { get; set; }
    public int InstallmentsPaid { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceRemaining { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public LoanStatus Status { get; set; } // Active, Completed, Suspended
    public string ApprovedBy { get; set; }
}

public enum LoanType
{
    SalaryAdvance,
    StaffLoan,
    CooperativeLoan,
    EmergencyLoan
}
```

### 6. Enhanced Payslip

```
═══════════════════════════════════════════════════════════
                    PAYSLIP
═══════════════════════════════════════════════════════════
Employee: John Doe                    Employee No: EMP001
Period: January 2024                  Pay Date: 31-Jan-2024
Department: Operations                Position: Supervisor
───────────────────────────────────────────────────────────

EARNINGS                                           AMOUNT
───────────────────────────────────────────────────────────
Basic Salary                                    50,000.00
House Allowance                                 10,000.00
Transport Allowance                              5,000.00
Overtime (20 hrs @ 1.5x)                         4,500.00
───────────────────────────────────────────────────────────
GROSS PAY                                       69,500.00

DEDUCTIONS
───────────────────────────────────────────────────────────
PAYE (Income Tax)                               12,850.00
NSSF (Employee Contribution)                       200.00
SHIF (Social Health Insurance)                   1,911.25
Housing Levy (1.5%)                              1,042.50
HELB Loan Repayment                              3,000.00
Staff Loan Repayment                             5,000.00
───────────────────────────────────────────────────────────
TOTAL DEDUCTIONS                                24,003.75

───────────────────────────────────────────────────────────
NET PAY                                         45,496.25
═══════════════════════════════════════════════════════════

STATUTORY CONTRIBUTIONS (Employer)
───────────────────────────────────────────────────────────
NSSF (Employer)                                    200.00
Housing Levy (Employer 1.5%)                     1,042.50

TAX COMPUTATION
───────────────────────────────────────────────────────────
Gross Pay                                       69,500.00
Less: NSSF                                        (200.00)
Less: SHIF                                      (1,911.25)
Less: Housing Levy                              (1,042.50)
───────────────────────────────────────────────────────────
Taxable Pay                                     66,346.25
Tax Before Relief                               15,250.00
Less: Personal Relief                           (2,400.00)
───────────────────────────────────────────────────────────
PAYE Payable                                    12,850.00

Year-to-Date Summary
───────────────────────────────────────────────────────────
Gross Earnings YTD:     69,500.00
PAYE YTD:               12,850.00
NSSF YTD:                  200.00
SHIF YTD:                1,911.25
───────────────────────────────────────────────────────────
```

### 7. P9 Form Generation

Annual tax certificate (Form P9) for employees:

```csharp
public class P9Form
{
    public int Year { get; set; }
    public int EmployeeId { get; set; }
    public string EmployerPIN { get; set; }
    public string EmployeePIN { get; set; }

    // Monthly breakdown
    public List<MonthlyP9Entry> MonthlyEntries { get; set; }

    // Annual totals
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDefinedContribution { get; set; }
    public decimal TotalOwnerOccupiedInterest { get; set; }
    public decimal TotalRetirementContribution { get; set; }
    public decimal TotalTaxCharged { get; set; }
    public decimal TotalPersonalRelief { get; set; }
    public decimal TotalInsuranceRelief { get; set; }
    public decimal TotalPAYE { get; set; }
}

public class MonthlyP9Entry
{
    public int Month { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Benefits { get; set; }
    public decimal TotalGross { get; set; }
    public decimal DefinedContribution { get; set; }
    public decimal OwnerOccupiedInterest { get; set; }
    public decimal RetirementContribution { get; set; }
    public decimal Chargeable { get; set; }
    public decimal TaxCharged { get; set; }
    public decimal PersonalRelief { get; set; }
    public decimal InsuranceRelief { get; set; }
    public decimal PAYE { get; set; }
}
```

### 8. Statutory Returns

**NSSF Return:**
- Monthly submission via eCitizen
- Employee details and contributions
- Export in NSSF format

**PAYE Return (P10):**
- Monthly submission to KRA
- Employee PAYE details
- Export in iTax format

**SHIF Return:**
- Monthly submission to SHA
- Employee SHIF contributions

### 9. Payroll Reports

- **Payroll Summary** - Department/company totals
- **Bank Schedule** - For salary transfers
- **Cash Payment List** - For cash payments
- **Deduction Summary** - By deduction type
- **Statutory Summary** - PAYE, NSSF, SHIF totals
- **Variance Report** - Month-over-month comparison
- **Cost Analysis** - Labor cost by department

### 10. Service Interface Enhancement

```csharp
public interface IPayrollService
{
    // Existing methods...

    // HELB
    Task<HelbDeduction> GetHelbDeductionAsync(int employeeId, CancellationToken ct = default);
    Task SetHelbDeductionAsync(int employeeId, decimal monthlyAmount, CancellationToken ct = default);

    // Tax Reliefs
    Task<TaxReliefs> GetEmployeeTaxReliefsAsync(int employeeId, CancellationToken ct = default);
    Task UpdateTaxReliefsAsync(int employeeId, TaxReliefs reliefs, CancellationToken ct = default);

    // Loans
    Task<EmployeeLoan> CreateLoanAsync(CreateLoanDto dto, CancellationToken ct = default);
    Task<IEnumerable<EmployeeLoan>> GetActiveLoansAsync(int employeeId, CancellationToken ct = default);

    // P9 Form
    Task<P9Form> GenerateP9FormAsync(int employeeId, int year, CancellationToken ct = default);
    Task<byte[]> ExportP9ToPdfAsync(int employeeId, int year, CancellationToken ct = default);

    // Statutory Returns
    Task<byte[]> GenerateNSSFReturnAsync(int payrollPeriodId, CancellationToken ct = default);
    Task<byte[]> GeneratePAYEReturnAsync(int payrollPeriodId, CancellationToken ct = default);
    Task<byte[]> GenerateSHIFReturnAsync(int payrollPeriodId, CancellationToken ct = default);

    // Reports
    Task<PayrollSummaryReport> GetPayrollSummaryAsync(int payrollPeriodId, CancellationToken ct = default);
    Task<BankSchedule> GenerateBankScheduleAsync(int payrollPeriodId, CancellationToken ct = default);
}
```

## Acceptance Criteria

- [ ] SHIF calculation implemented (2.75% with KES 300 minimum)
- [ ] HELB deductions automated when employee has HELB number
- [ ] Tax reliefs (personal, insurance) applied correctly
- [ ] Employee loans can be created and auto-deducted
- [ ] Enhanced payslip shows all earnings and deductions
- [ ] P9 form can be generated for any year
- [ ] NSSF return exportable in correct format
- [ ] PAYE return (P10) exportable for iTax
- [ ] Payroll summary report available
- [ ] Bank schedule for salary transfers generated

## Implementation Notes

### Existing Code to Modify
- `PayrollService` - Add new calculation methods
- `Employee` entity - Ensure all fields present
- `PayslipDetail` - Support new components
- `Payslip` entity - Add YTD fields

### New Components
- `HelbDeduction` entity
- `EmployeeLoan` entity
- `TaxReliefs` model
- `P9Form` model and generator
- Statutory return exporters

## References

- [KRA PAYE Guide 2025](https://www.kra.go.ke/images/publications/PAYE-AS-YOU-EARN-PAYE_4-01-2025.pdf)
- [NSSF Employer Resources](https://www.nssfkenya.or.ke/employer-resources/)
- [Kenya Payroll Compliance 2025](https://faidihr.com/blog/kenya-payroll-compliance-checklist-shif-nssf-paye-2025)
- [ClearTax PAYE Calculator](https://www.cleartax.co.ke/best-paye-payroll-tax-calculator-kenya.html)

---

**Priority**: Medium
**Estimated Complexity**: Large
**Labels**: feature, payroll, compliance, kenya
