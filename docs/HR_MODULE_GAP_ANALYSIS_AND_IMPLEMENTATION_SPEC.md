# HR & Payroll Module - Gap Analysis and Implementation Specification

## Document Information
- **Version:** 1.0
- **Date:** January 21, 2026
- **Author:** System Analysis
- **Project:** HospitalityPOS - Supermarket HR Module

---

## Table of Contents
1. [Executive Summary](#1-executive-summary)
2. [Kenyan HR/Payroll Compliance Requirements](#2-kenyan-hrpayroll-compliance-requirements)
3. [Current Implementation Analysis](#3-current-implementation-analysis)
4. [Gap Analysis](#4-gap-analysis)
5. [Implementation Specifications](#5-implementation-specifications)
6. [Database Schema Designs](#6-database-schema-designs)
7. [Implementation Roadmap](#7-implementation-roadmap)

---

## 1. Executive Summary

### 1.1 Purpose
This document provides a comprehensive analysis of the HR and Payroll module requirements for Kenyan supermarkets, compares them against the current implementation in HospitalityPOS, identifies gaps, and provides detailed technical specifications for implementing missing features.

### 1.2 Scope
The analysis covers:
- Employee personal information management
- Payroll processing with Kenyan statutory compliance
- Leave management (annual, sick, maternity, paternity, etc.)
- Salary advances and employee loans
- Fines and disciplinary deductions
- Off-day and scheduling management
- Master payroll report generation
- Termination and severance calculations

### 1.3 Key Findings Summary

| Feature Area | Status | Priority |
|-------------|--------|----------|
| Basic Employee Management | ✅ Implemented | - |
| **Employee Photo Upload** | ❌ Not Implemented | **HIGH** |
| Payroll with Statutory Deductions | ✅ Implemented | - |
| **Configurable Statutory Rates** | ❌ Not Implemented (Hardcoded) | **CRITICAL** |
| Attendance Tracking | ✅ Implemented | - |
| Leave Management | ⚠️ Partial (In-Memory Only) | **HIGH** |
| Salary Advance/Loans | ❌ Not Implemented | **HIGH** |
| Fines/Penalties | ❌ Not Implemented | **MEDIUM** |
| Department Management | ⚠️ Partial (String only) | **MEDIUM** |
| Emergency Contacts | ❌ Not Implemented | **LOW** |
| Document Management | ❌ Not Implemented | **LOW** |
| Master Payroll Reports | ⚠️ Partial | **MEDIUM** |
| Termination/Severance | ⚠️ Partial | **MEDIUM** |

---

## 2. Kenyan HR/Payroll Compliance Requirements

### 2.1 Statutory Deductions (Employment Act 2007, Section 19)

#### 2.1.1 PAYE (Pay As You Earn)
- **Authority:** Kenya Revenue Authority (KRA)
- **Basis:** Progressive tax on taxable income
- **2025/2026 Tax Bands:**
  | Monthly Income (KES) | Rate |
  |---------------------|------|
  | 0 - 24,000 | 10% |
  | 24,001 - 32,333 | 25% |
  | 32,334 - 500,000 | 30% |
  | 500,001 - 800,000 | 32.5% |
  | Above 800,000 | 35% |
- **Personal Relief:** KES 2,400/month
- **Remittance Deadline:** 9th of following month via iTax

#### 2.1.2 NSSF (National Social Security Fund)
- **Authority:** NSSF Kenya
- **Contribution Rate:** 6% Employee + 6% Employer
- **Tier System:**
  - Tier I: Up to KES 7,000 (Lower Earnings Limit)
  - Tier II: KES 7,001 to KES 36,000 (Upper Earnings Limit)
- **Maximum Monthly Contribution:** KES 2,160 each (total KES 4,320)
- **Remittance Deadline:** 9th of following month via NSSF i-portal

#### 2.1.3 SHIF (Social Health Insurance Fund) - Replaced NHIF
- **Authority:** Social Health Authority (SHA)
- **Contribution Rate:** 2.75% of gross salary
- **No Upper Limit** (unlike old NHIF)
- **Remittance Deadline:** 9th of following month

#### 2.1.4 Housing Levy (Affordable Housing Levy)
- **Authority:** National Housing Corporation
- **Rate:** 1.5% of gross salary (Employee) + 1.5% (Employer)
- **Remittance Deadline:** 9th of following month

#### 2.1.5 HELB (Higher Education Loans Board)
- **Applicable:** Only to employees who took student loans
- **Deduction:** Per Board instructions based on salary band
- **Must be disclosed** by employee during onboarding

### 2.2 Leave Entitlements (Employment Act 2007)

| Leave Type | Entitlement | Paid | Documentation |
|-----------|-------------|------|---------------|
| Annual Leave | 21 working days after 12 months | Yes | None required |
| Sick Leave | 14 days per year (full pay) + 14 (half pay) | Yes/Half | Medical certificate |
| Maternity Leave | 3 months (90 days) | Yes | Medical documentation |
| Paternity Leave | 2 weeks (14 days) | Yes | Birth notification |
| Compassionate Leave | 5-7 days (company policy) | Yes | Death certificate/proof |
| Study Leave | As per company policy | Varies | Course enrollment |

**Key Rules:**
- Annual leave must be taken within 12 months of accrual
- Carry-over limited to employer's discretion (typically 5-10 days)
- Leave cannot be converted to cash except on termination
- Probation period may affect leave accrual

### 2.3 Salary Advance and Loan Regulations

#### 2.3.1 Salary Advances (Section 17)
- Maximum advance: **2 months' salary** (under written contract)
- Excess over 2 months is **not recoverable in court**
- Must be recovered through payroll deductions
- Employee must consent in writing

#### 2.3.2 Employee Loans (Section 19)
- **Maximum loan deduction:** 50% of wages after other deductions
- **Written agreement required** specifying:
  - Principal amount
  - Interest rate (if any)
  - Repayment schedule
  - Total amount to be repaid
- Must be clearly separate from employment contract

#### 2.3.3 Total Deductions Limit
- **Employee must take home at least 1/3 (33.33%) of gross salary**
- Maximum total deductions: 2/3 (66.67%) of gross salary
- This includes ALL deductions: statutory, loans, advances, etc.

### 2.4 Permitted Deductions for Fines/Penalties (Section 19)

| Deduction Type | Maximum | Documentation Required |
|---------------|---------|----------------------|
| Damage to employer property (willful) | Reasonable amount | Incident report, proof of cost |
| Absence without leave | 1 day's wages per day | Attendance records |
| Money shortage (if handling cash) | Actual shortage | Investigation report |
| Amounts paid in error | Actual overpayment | Payroll records |

**Important:**
- Fines are **NOT explicitly permitted** in Kenyan law
- Deductions must be for actual loss/damage, not penalties
- Employee must be given opportunity to explain
- Cannot deduct without employee consent for non-statutory items

### 2.5 Termination Requirements

#### 2.5.1 Notice Periods
| Employment Type | Notice Required |
|----------------|-----------------|
| Daily wage | None (end of day) |
| Probation | 7 days |
| Weekly/Fortnightly | Same as payment period |
| Monthly or longer | 28 days |

#### 2.5.2 Severance Pay (Redundancy Only)
- **Rate:** 15 days' basic pay per completed year of service
- **Calculation:** (Monthly salary / 30) × 15 × Years of service
- **NOT payable** for misconduct termination
- **Payable** for: redundancy, restructuring, business closure

#### 2.5.3 Terminal Benefits
- Wages for work done up to termination
- Payment for untaken annual leave
- Notice pay (if not served)
- Severance pay (if eligible)
- Certificate of Service (mandatory)

---

## 3. Current Implementation Analysis

### 3.1 Database Entities (Persisted to SQL Server)

#### ✅ Fully Implemented

| Entity | Table | Key Features |
|--------|-------|--------------|
| `Employee` | Employees | Full profile, Kenya IDs (KRA PIN, NSSF, NHIF) |
| `SalaryComponent` | SalaryComponents | Earning/Deduction templates |
| `EmployeeSalaryComponent` | EmployeeSalaryComponents | Employee-specific assignments |
| `PayrollPeriod` | PayrollPeriods | Period management with workflow |
| `Payslip` | Payslips | Generated payslips with totals |
| `PayslipDetail` | PayslipDetails | Line item breakdown |
| `Attendance` | Attendances | Daily clock in/out, breaks |

### 3.2 Service Interfaces

| Interface | Implementation | Storage |
|-----------|---------------|---------|
| `IEmployeeService` | `EmployeeService.cs` | Database |
| `IPayrollService` | `PayrollService.cs` | Database |
| `IAttendanceService` | `AttendanceService.cs` | Database |
| `ILeaveService` | `LeaveService.cs` | **IN-MEMORY (Dictionary)** |
| `ICommissionService` | `CommissionService.cs` | In-Memory |
| `ISchedulingService` | `SchedulingService.cs` | In-Memory |

### 3.3 Current Employee Entity Fields

```csharp
public class Employee : BaseEntity
{
    public int? UserId { get; set; }
    public string EmployeeNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? Department { get; set; }  // String only, not entity
    public string? Position { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public decimal BasicSalary { get; set; }
    public PayFrequency PayFrequency { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? TaxId { get; set; }        // KRA PIN
    public string? NssfNumber { get; set; }
    public string? NhifNumber { get; set; }
}
```

### 3.4 Kenyan Statutory Calculations (Already Implemented)

The `IPayrollService` interface includes:
```csharp
Task<StatutoryDeductions> CalculateStatutoryDeductionsAsync(decimal grossSalary);
Task<decimal> CalculatePAYEAsync(decimal taxableIncome);
Task<decimal> CalculateNHIFAsync(decimal grossSalary);
Task<decimal> CalculateNSSFAsync(decimal grossSalary);
Task<decimal> CalculateHousingLevyAsync(decimal grossSalary);
```

---

## 4. Gap Analysis

### 4.1 Critical Gaps (Must Implement)

#### GAP-000A: Employee Photo Upload Missing
- **Current State:** Employee entity has no PhotoPath field
- **Impact:** Cannot visually identify employees, no photo for ID cards/badges
- **Required:**
  - Add `PhotoPath` field to Employee entity
  - Photo upload functionality in Employee Editor
  - Image storage (file system or blob)
  - Thumbnail generation for list views
  - Photo display in employee profile and attendance views

#### GAP-000B: Configurable Statutory Deduction Rates Missing
- **Current State:** PAYE bands, NSSF rates, SHIF rate, Housing Levy rate are HARDCODED in PayrollService
- **Impact:** When government changes rates (happens frequently), code must be modified and redeployed
- **Required:** Database-stored, UI-configurable statutory rate entities:
  - PAYETaxBand (with effective dates)
  - NSSFConfiguration (tier limits, rates)
  - SHIFConfiguration (percentage rate)
  - HousingLevyConfiguration (employee/employer rates)
  - Personal relief amount configuration
  - Effective date support for rate changes

#### GAP-001: Leave Management Not Persisted
- **Current State:** LeaveService uses in-memory Dictionary storage
- **Impact:** All leave data lost on application restart
- **Required:** Database entities and migrations for:
  - LeaveType
  - LeaveAllocation
  - LeaveRequest
  - LeaveBalanceAdjustment
  - PublicHoliday

#### GAP-002: Salary Advance/Loan System Missing
- **Current State:** No implementation exists
- **Impact:** Cannot track employee advances or loans
- **Required:** Complete feature implementation including:
  - Loan/Advance entities
  - Approval workflow
  - Automatic payroll deductions
  - Balance tracking
  - Interest calculation (optional)
  - Kenya 50% deduction limit validation

#### GAP-003: Fine/Penalty System Missing
- **Current State:** No implementation exists
- **Impact:** Cannot record or deduct fines/penalties
- **Required:** Disciplinary deduction system:
  - Fine/Penalty entity
  - Reason categorization (damage, absence, shortage)
  - Approval workflow
  - Integration with payroll deductions
  - 1/3 take-home validation

### 4.2 Important Gaps (Should Implement)

#### GAP-004: Department Not a Proper Entity
- **Current State:** String field on Employee
- **Impact:** No department reporting, no hierarchy
- **Required:** Department entity with:
  - Department codes
  - Manager assignment
  - Cost center linkage

#### GAP-005: Off-Day/Schedule Management Not Persisted
- **Current State:** SchedulingService uses in-memory storage
- **Impact:** Shift schedules lost on restart
- **Required:** Database entities for:
  - Shift
  - EmployeeSchedule
  - ShiftSwapRequest
  - OffDay

#### GAP-006: Master Payroll Report Enhancement
- **Current State:** Basic HTML report generation
- **Impact:** No export to Excel, no bank file generation
- **Required:**
  - Excel export (OpenXML)
  - Bank payment file generation (various formats)
  - Statutory returns (KRA P9, NSSF, NHIF/SHIF reports)

#### GAP-007: Termination/Separation Enhancement
- **Current State:** Only TerminationDate field exists
- **Impact:** No severance calculation, no exit workflow
- **Required:**
  - Termination entity with reason, type
  - Automatic severance calculation
  - Final settlement calculation
  - Certificate of Service generation

### 4.3 Nice-to-Have Gaps (Consider Implementing)

#### GAP-008: Emergency Contacts
- Track next of kin for HR purposes

#### GAP-009: Employee Document Management
- Store copies of ID, certificates, contracts

#### GAP-010: Training/Certification Tracking
- Track employee skills and training completion

#### GAP-011: HELB Deduction Support
- For employees with student loans

---

## 5. Implementation Specifications

### 5.0A GAP-000A: Employee Photo Upload

#### 5.0A.1 Employee Entity Update

```csharp
// Update Employee entity - add to src/HospitalityPOS.Core/Entities/Employee.cs

public class Employee : BaseEntity
{
    // ... existing fields ...

    /// <summary>
    /// Path to employee photo image file.
    /// Stored relative to application's employee photos directory.
    /// </summary>
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Small thumbnail version of the photo for list views.
    /// </summary>
    public string? ThumbnailPath { get; set; }
}
```

#### 5.0A.2 Photo Service Interface

```csharp
// src/HospitalityPOS.Core/Interfaces/IEmployeePhotoService.cs

namespace HospitalityPOS.Core.Interfaces;

public interface IEmployeePhotoService
{
    /// <summary>
    /// Uploads and saves an employee photo.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="imageData">The image file bytes.</param>
    /// <param name="fileName">Original file name for extension detection.</param>
    /// <returns>The relative path to the saved photo.</returns>
    Task<string> UploadPhotoAsync(int employeeId, byte[] imageData, string fileName);

    /// <summary>
    /// Gets the full path to an employee's photo.
    /// </summary>
    Task<string?> GetPhotoPathAsync(int employeeId);

    /// <summary>
    /// Gets the photo as a byte array for display.
    /// </summary>
    Task<byte[]?> GetPhotoDataAsync(int employeeId);

    /// <summary>
    /// Gets the thumbnail as a byte array for list views.
    /// </summary>
    Task<byte[]?> GetThumbnailDataAsync(int employeeId);

    /// <summary>
    /// Deletes an employee's photo.
    /// </summary>
    Task<bool> DeletePhotoAsync(int employeeId);

    /// <summary>
    /// Generates a thumbnail from the main photo.
    /// </summary>
    Task<string> GenerateThumbnailAsync(string photoPath, int width = 100, int height = 100);
}
```

#### 5.0A.3 Photo Storage Configuration

```csharp
// Add to appsettings.json
{
  "EmployeePhotos": {
    "BasePath": "Data/EmployeePhotos",
    "AllowedExtensions": [".jpg", ".jpeg", ".png"],
    "MaxFileSizeBytes": 5242880,  // 5 MB
    "ThumbnailWidth": 100,
    "ThumbnailHeight": 100
  }
}
```

#### 5.0A.4 WPF Implementation Notes

```csharp
// In EmployeeEditorViewModel, add photo handling:

public class EmployeeEditorViewModel : ViewModelBase
{
    private BitmapImage? _employeePhoto;
    public BitmapImage? EmployeePhoto
    {
        get => _employeePhoto;
        set => SetProperty(ref _employeePhoto, value);
    }

    public ICommand SelectPhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }

    private async Task SelectPhotoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png",
            Title = "Select Employee Photo"
        };

        if (dialog.ShowDialog() == true)
        {
            var imageData = await File.ReadAllBytesAsync(dialog.FileName);
            var path = await _photoService.UploadPhotoAsync(EmployeeId, imageData, dialog.FileName);
            await LoadPhotoAsync();
        }
    }
}
```

---

### 5.0B GAP-000B: Configurable Statutory Deduction Rates

#### 5.0B.1 Entity Classes

```csharp
// src/HospitalityPOS.Core/Entities/StatutoryConfigurationEntities.cs

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// PAYE (Pay As You Earn) tax band configuration.
/// Kenya has progressive tax bands that can be adjusted by government.
/// </summary>
public class PAYETaxBand : BaseEntity
{
    /// <summary>
    /// Lower limit of this tax band (inclusive).
    /// </summary>
    public decimal LowerLimit { get; set; }

    /// <summary>
    /// Upper limit of this tax band (exclusive). NULL for the top band.
    /// </summary>
    public decimal? UpperLimit { get; set; }

    /// <summary>
    /// Tax rate as a decimal (e.g., 0.10 for 10%).
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Display order for UI and calculation sequence.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Date this rate becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this rate expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Description for admin reference (e.g., "2025/2026 Budget").
    /// </summary>
    public string? Description { get; set; }

    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// PAYE personal relief configuration.
/// </summary>
public class PAYERelief : BaseEntity
{
    public string Name { get; set; } = string.Empty;  // "Personal Relief", "Insurance Relief"
    public decimal MonthlyAmount { get; set; }
    public decimal? MaximumAmount { get; set; }  // For reliefs with caps
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// NSSF (National Social Security Fund) configuration.
/// </summary>
public class NSSFConfiguration : BaseEntity
{
    /// <summary>
    /// Employee contribution rate as decimal (e.g., 0.06 for 6%).
    /// </summary>
    public decimal EmployeeRate { get; set; }

    /// <summary>
    /// Employer contribution rate as decimal (e.g., 0.06 for 6%).
    /// </summary>
    public decimal EmployerRate { get; set; }

    /// <summary>
    /// Lower Earnings Limit (Tier I ceiling).
    /// </summary>
    public decimal Tier1Limit { get; set; }

    /// <summary>
    /// Upper Earnings Limit (Tier II ceiling).
    /// </summary>
    public decimal Tier2Limit { get; set; }

    /// <summary>
    /// Maximum employee contribution per month.
    /// </summary>
    public decimal MaxEmployeeContribution { get; set; }

    /// <summary>
    /// Maximum employer contribution per month.
    /// </summary>
    public decimal MaxEmployerContribution { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// SHIF (Social Health Insurance Fund) configuration.
/// Replaced NHIF effective 2024.
/// </summary>
public class SHIFConfiguration : BaseEntity
{
    /// <summary>
    /// Contribution rate as decimal (e.g., 0.0275 for 2.75%).
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Minimum monthly contribution (if any).
    /// </summary>
    public decimal? MinimumContribution { get; set; }

    /// <summary>
    /// Maximum monthly contribution (NULL = no cap).
    /// </summary>
    public decimal? MaximumContribution { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// Housing Levy (Affordable Housing Levy) configuration.
/// </summary>
public class HousingLevyConfiguration : BaseEntity
{
    /// <summary>
    /// Employee contribution rate as decimal (e.g., 0.015 for 1.5%).
    /// </summary>
    public decimal EmployeeRate { get; set; }

    /// <summary>
    /// Employer contribution rate as decimal (e.g., 0.015 for 1.5%).
    /// </summary>
    public decimal EmployerRate { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// HELB (Higher Education Loans Board) deduction bands.
/// </summary>
public class HELBDeductionBand : BaseEntity
{
    public decimal LowerSalaryLimit { get; set; }
    public decimal? UpperSalaryLimit { get; set; }
    public decimal DeductionAmount { get; set; }  // Fixed amount per band
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}
```

#### 5.0B.2 Service Interface

```csharp
// src/HospitalityPOS.Core/Interfaces/IStatutoryConfigurationService.cs

namespace HospitalityPOS.Core.Interfaces;

public interface IStatutoryConfigurationService
{
    // PAYE Tax Bands
    Task<IReadOnlyList<PAYETaxBand>> GetActivePAYEBandsAsync(DateOnly? asOfDate = null);
    Task<PAYETaxBand> CreatePAYEBandAsync(PAYETaxBandRequest request);
    Task<PAYETaxBand> UpdatePAYEBandAsync(int id, PAYETaxBandRequest request);
    Task<bool> DeactivatePAYEBandAsync(int id, DateOnly effectiveDate);
    Task<int> CopyPAYEBandsForNewPeriodAsync(DateOnly effectiveFrom, string description);

    // PAYE Reliefs
    Task<IReadOnlyList<PAYERelief>> GetActiveReliefsAsync(DateOnly? asOfDate = null);
    Task<PAYERelief> CreateReliefAsync(PAYEReliefRequest request);
    Task<PAYERelief> UpdateReliefAsync(int id, PAYEReliefRequest request);

    // NSSF Configuration
    Task<NSSFConfiguration?> GetActiveNSSFConfigAsync(DateOnly? asOfDate = null);
    Task<NSSFConfiguration> CreateNSSFConfigAsync(NSSFConfigurationRequest request);
    Task<NSSFConfiguration> UpdateNSSFConfigAsync(int id, NSSFConfigurationRequest request);
    Task<IReadOnlyList<NSSFConfiguration>> GetNSSFHistoryAsync();

    // SHIF Configuration
    Task<SHIFConfiguration?> GetActiveSHIFConfigAsync(DateOnly? asOfDate = null);
    Task<SHIFConfiguration> CreateSHIFConfigAsync(SHIFConfigurationRequest request);
    Task<SHIFConfiguration> UpdateSHIFConfigAsync(int id, SHIFConfigurationRequest request);
    Task<IReadOnlyList<SHIFConfiguration>> GetSHIFHistoryAsync();

    // Housing Levy Configuration
    Task<HousingLevyConfiguration?> GetActiveHousingLevyConfigAsync(DateOnly? asOfDate = null);
    Task<HousingLevyConfiguration> CreateHousingLevyConfigAsync(HousingLevyConfigurationRequest request);
    Task<HousingLevyConfiguration> UpdateHousingLevyConfigAsync(int id, HousingLevyConfigurationRequest request);
    Task<IReadOnlyList<HousingLevyConfiguration>> GetHousingLevyHistoryAsync();

    // HELB Bands
    Task<IReadOnlyList<HELBDeductionBand>> GetActiveHELBBandsAsync(DateOnly? asOfDate = null);
    Task<HELBDeductionBand> CreateHELBBandAsync(HELBBandRequest request);
    Task<HELBDeductionBand> UpdateHELBBandAsync(int id, HELBBandRequest request);

    // Utility
    Task<StatutoryRatesSummary> GetCurrentRatesSummaryAsync();
    Task ValidateEffectiveDatesAsync(DateOnly effectiveFrom, DateOnly? effectiveTo);
}

// Request DTOs
public record PAYETaxBandRequest(
    decimal LowerLimit,
    decimal? UpperLimit,
    decimal Rate,
    int DisplayOrder,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

public record NSSFConfigurationRequest(
    decimal EmployeeRate,
    decimal EmployerRate,
    decimal Tier1Limit,
    decimal Tier2Limit,
    decimal MaxEmployeeContribution,
    decimal MaxEmployerContribution,
    DateOnly EffectiveFrom,
    string? Description
);

public record SHIFConfigurationRequest(
    decimal Rate,
    decimal? MinimumContribution,
    decimal? MaximumContribution,
    DateOnly EffectiveFrom,
    string? Description
);

public record HousingLevyConfigurationRequest(
    decimal EmployeeRate,
    decimal EmployerRate,
    DateOnly EffectiveFrom,
    string? Description
);

public record StatutoryRatesSummary(
    IReadOnlyList<PAYETaxBand> PAYEBands,
    decimal PersonalRelief,
    NSSFConfiguration? NSSF,
    SHIFConfiguration? SHIF,
    HousingLevyConfiguration? HousingLevy,
    DateOnly AsOfDate
);
```

#### 5.0B.3 Database Configuration

```csharp
// src/HospitalityPOS.Infrastructure/Data/Configurations/StatutoryConfigurationConfiguration.cs

public class PAYETaxBandConfiguration : IEntityTypeConfiguration<PAYETaxBand>
{
    public void Configure(EntityTypeBuilder<PAYETaxBand> builder)
    {
        builder.ToTable("PAYETaxBands");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LowerLimit).HasPrecision(18, 2);
        builder.Property(e => e.UpperLimit).HasPrecision(18, 2);
        builder.Property(e => e.Rate).HasPrecision(8, 6);  // Precision for percentages
        builder.Property(e => e.Description).HasMaxLength(200);

        builder.HasIndex(e => new { e.EffectiveFrom, e.DisplayOrder });
    }
}

public class NSSFConfigurationConfiguration : IEntityTypeConfiguration<NSSFConfiguration>
{
    public void Configure(EntityTypeBuilder<NSSFConfiguration> builder)
    {
        builder.ToTable("NSSFConfigurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeRate).HasPrecision(8, 6);
        builder.Property(e => e.EmployerRate).HasPrecision(8, 6);
        builder.Property(e => e.Tier1Limit).HasPrecision(18, 2);
        builder.Property(e => e.Tier2Limit).HasPrecision(18, 2);
        builder.Property(e => e.MaxEmployeeContribution).HasPrecision(18, 2);
        builder.Property(e => e.MaxEmployerContribution).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(200);
    }
}

public class SHIFConfigurationConfiguration : IEntityTypeConfiguration<SHIFConfiguration>
{
    public void Configure(EntityTypeBuilder<SHIFConfiguration> builder)
    {
        builder.ToTable("SHIFConfigurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Rate).HasPrecision(8, 6);
        builder.Property(e => e.MinimumContribution).HasPrecision(18, 2);
        builder.Property(e => e.MaximumContribution).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(200);
    }
}

public class HousingLevyConfigurationConfiguration : IEntityTypeConfiguration<HousingLevyConfiguration>
{
    public void Configure(EntityTypeBuilder<HousingLevyConfiguration> builder)
    {
        builder.ToTable("HousingLevyConfigurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeRate).HasPrecision(8, 6);
        builder.Property(e => e.EmployerRate).HasPrecision(8, 6);
        builder.Property(e => e.Description).HasMaxLength(200);
    }
}
```

#### 5.0B.4 Seed Data (2025/2026 Kenya Rates)

```csharp
// In migration or seeding service

// PAYE Tax Bands 2025/2026
new PAYETaxBand { LowerLimit = 0, UpperLimit = 24000, Rate = 0.10m, DisplayOrder = 1, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },
new PAYETaxBand { LowerLimit = 24000, UpperLimit = 32333, Rate = 0.25m, DisplayOrder = 2, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },
new PAYETaxBand { LowerLimit = 32333, UpperLimit = 500000, Rate = 0.30m, DisplayOrder = 3, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },
new PAYETaxBand { LowerLimit = 500000, UpperLimit = 800000, Rate = 0.325m, DisplayOrder = 4, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },
new PAYETaxBand { LowerLimit = 800000, UpperLimit = null, Rate = 0.35m, DisplayOrder = 5, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },

// Personal Relief
new PAYERelief { Name = "Personal Relief", MonthlyAmount = 2400, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "2025/2026 Budget" },
new PAYERelief { Name = "Insurance Relief", MonthlyAmount = 0, MaximumAmount = 5000, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "15% of premiums, max 5,000/month" },

// NSSF
new NSSFConfiguration { EmployeeRate = 0.06m, EmployerRate = 0.06m, Tier1Limit = 7000, Tier2Limit = 36000, MaxEmployeeContribution = 2160, MaxEmployerContribution = 2160, EffectiveFrom = new DateOnly(2025, 1, 1), Description = "NSSF Act 2013" },

// SHIF (replaces NHIF)
new SHIFConfiguration { Rate = 0.0275m, MinimumContribution = null, MaximumContribution = null, EffectiveFrom = new DateOnly(2024, 10, 1), Description = "Social Health Insurance Act 2023" },

// Housing Levy
new HousingLevyConfiguration { EmployeeRate = 0.015m, EmployerRate = 0.015m, EffectiveFrom = new DateOnly(2024, 3, 1), Description = "Affordable Housing Act" }
```

#### 5.0B.5 Updated PayrollService Integration

```csharp
// Update PayrollService to use configurable rates

public class PayrollService : IPayrollService
{
    private readonly IStatutoryConfigurationService _statutoryConfig;

    public async Task<decimal> CalculatePAYEAsync(decimal taxableIncome, CancellationToken cancellationToken = default)
    {
        var bands = await _statutoryConfig.GetActivePAYEBandsAsync();
        var reliefs = await _statutoryConfig.GetActiveReliefsAsync();

        decimal tax = 0;
        decimal remainingIncome = taxableIncome;

        foreach (var band in bands.OrderBy(b => b.DisplayOrder))
        {
            var bandLimit = band.UpperLimit ?? decimal.MaxValue;
            var taxableInBand = Math.Min(remainingIncome, bandLimit - band.LowerLimit);

            if (taxableInBand > 0)
            {
                tax += taxableInBand * band.Rate;
                remainingIncome -= taxableInBand;
            }

            if (remainingIncome <= 0) break;
        }

        // Apply personal relief
        var personalRelief = reliefs.FirstOrDefault(r => r.Name == "Personal Relief")?.MonthlyAmount ?? 0;
        tax = Math.Max(0, tax - personalRelief);

        return Math.Round(tax, 2);
    }

    public async Task<decimal> CalculateNSSFAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        var config = await _statutoryConfig.GetActiveNSSFConfigAsync();
        if (config == null) return 0;

        var pensionableEarnings = Math.Min(grossPay, config.Tier2Limit);
        var contribution = pensionableEarnings * config.EmployeeRate;

        return Math.Min(Math.Round(contribution, 2), config.MaxEmployeeContribution);
    }

    public async Task<decimal> CalculateNHIFAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        // Now using SHIF
        var config = await _statutoryConfig.GetActiveSHIFConfigAsync();
        if (config == null) return 0;

        var contribution = grossPay * config.Rate;

        if (config.MinimumContribution.HasValue)
            contribution = Math.Max(contribution, config.MinimumContribution.Value);

        if (config.MaximumContribution.HasValue)
            contribution = Math.Min(contribution, config.MaximumContribution.Value);

        return Math.Round(contribution, 2);
    }

    public async Task<decimal> CalculateHousingLevyAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        var config = await _statutoryConfig.GetActiveHousingLevyConfigAsync();
        if (config == null) return 0;

        return Math.Round(grossPay * config.EmployeeRate, 2);
    }
}
```

#### 5.0B.6 Admin UI View (XAML Outline)

```xml
<!-- StatutoryRatesView.xaml - Admin configuration screen -->
<TabControl>
    <TabItem Header="PAYE Tax Bands">
        <DataGrid ItemsSource="{Binding PAYETaxBands}" AutoGenerateColumns="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="From (KES)" Binding="{Binding LowerLimit, StringFormat=N0}" />
                <DataGridTextColumn Header="To (KES)" Binding="{Binding UpperLimit, StringFormat=N0}" />
                <DataGridTextColumn Header="Rate (%)" Binding="{Binding Rate, StringFormat=P2}" />
                <DataGridTextColumn Header="Effective From" Binding="{Binding EffectiveFrom, StringFormat=d}" />
                <DataGridTemplateColumn Header="Actions">
                    <!-- Edit/Deactivate buttons -->
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
        <Button Content="Add New Tax Band" Command="{Binding AddPAYEBandCommand}" />
    </TabItem>

    <TabItem Header="NSSF">
        <!-- NSSF configuration form -->
        <StackPanel>
            <TextBlock Text="Employee Rate (%)" />
            <TextBox Text="{Binding NSSFConfig.EmployeeRate}" />
            <TextBlock Text="Employer Rate (%)" />
            <TextBox Text="{Binding NSSFConfig.EmployerRate}" />
            <TextBlock Text="Tier I Limit (KES)" />
            <TextBox Text="{Binding NSSFConfig.Tier1Limit}" />
            <TextBlock Text="Tier II Limit (KES)" />
            <TextBox Text="{Binding NSSFConfig.Tier2Limit}" />
            <Button Content="Save Changes" Command="{Binding SaveNSSFConfigCommand}" />
        </StackPanel>
    </TabItem>

    <TabItem Header="SHIF">
        <!-- SHIF configuration form -->
    </TabItem>

    <TabItem Header="Housing Levy">
        <!-- Housing Levy configuration form -->
    </TabItem>

    <TabItem Header="Rate History">
        <!-- Historical rates view -->
    </TabItem>
</TabControl>
```

---

### 5.1 GAP-001: Leave Management Database Migration

#### 5.1.1 New Entities Required

```csharp
// src/HospitalityPOS.Core/Entities/LeaveEntities.cs

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a type of leave (Annual, Sick, Maternity, etc.)
/// </summary>
public class LeaveType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowCarryOver { get; set; }
    public decimal MaxCarryOverDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public int? MinimumNoticeDays { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public int? MinServiceMonthsRequired { get; set; }
    public bool IsActive { get; set; } = true;

    // Kenya-specific
    public bool IsStatutory { get; set; } // Annual, Sick, Maternity, Paternity

    public virtual ICollection<LeaveAllocation> Allocations { get; set; } = new List<LeaveAllocation>();
    public virtual ICollection<LeaveRequest> Requests { get; set; } = new List<LeaveRequest>();
}

/// <summary>
/// Tracks leave balance allocated to an employee for a specific year.
/// </summary>
public class LeaveAllocation : BaseEntity
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal AllocatedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal PendingDays { get; set; }

    // Computed properties
    public decimal TotalAvailable => AllocatedDays + CarriedOverDays;
    public decimal RemainingDays => TotalAvailable - UsedDays - PendingDays;
    public decimal AvailableForRequest => RemainingDays;

    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
}

public enum LeaveRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

/// <summary>
/// Employee leave request with approval workflow.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public bool IsHalfDayStart { get; set; }
    public bool IsHalfDayEnd { get; set; }
    public string? DocumentationPath { get; set; }

    // Approval tracking
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual User? ReviewedByUser { get; set; }
}

/// <summary>
/// Manual adjustments to leave balances.
/// </summary>
public class LeaveBalanceAdjustment : BaseEntity
{
    public int LeaveAllocationId { get; set; }
    public decimal Days { get; set; } // Positive for addition, negative for deduction
    public string Reason { get; set; } = string.Empty;
    public int AdjustedByUserId { get; set; }

    public virtual LeaveAllocation? LeaveAllocation { get; set; }
    public virtual User? AdjustedByUser { get; set; }
}

/// <summary>
/// Public holidays for Kenya.
/// </summary>
public class PublicHoliday : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRecurring { get; set; } // Annually recurring
    public int? RecurringMonth { get; set; }
    public int? RecurringDay { get; set; }
}
```

#### 5.1.2 Database Configuration

```csharp
// src/HospitalityPOS.Infrastructure/Data/Configurations/LeaveConfiguration.cs

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.DefaultDaysPerYear).HasPrecision(5, 2);
        builder.Property(e => e.MaxCarryOverDays).HasPrecision(5, 2);

        builder.HasIndex(e => e.Name).IsUnique();
    }
}

public class LeaveAllocationConfiguration : IEntityTypeConfiguration<LeaveAllocation>
{
    public void Configure(EntityTypeBuilder<LeaveAllocation> builder)
    {
        builder.ToTable("LeaveAllocations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AllocatedDays).HasPrecision(5, 2);
        builder.Property(e => e.UsedDays).HasPrecision(5, 2);
        builder.Property(e => e.CarriedOverDays).HasPrecision(5, 2);
        builder.Property(e => e.PendingDays).HasPrecision(5, 2);

        builder.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LeaveType)
            .WithMany(t => t.Allocations)
            .HasForeignKey(e => e.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DaysRequested).HasPrecision(5, 2);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.ReviewNotes).HasMaxLength(500);
        builder.Property(e => e.DocumentationPath).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>();

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LeaveType)
            .WithMany(t => t.Requests)
            .HasForeignKey(e => e.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 5.2 GAP-002: Salary Advance/Loan System

#### 5.2.1 Entities

```csharp
// src/HospitalityPOS.Core/Entities/LoanEntities.cs

namespace HospitalityPOS.Core.Entities;

public enum LoanType
{
    SalaryAdvance = 0,      // Short-term, usually 1-2 months max
    EmployeeLoan = 1,       // Longer-term with defined repayment schedule
    EmergencyLoan = 2       // Special circumstances
}

public enum LoanStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Active = 3,             // Currently being repaid
    Completed = 4,          // Fully repaid
    WrittenOff = 5,         // Bad debt
    Cancelled = 6
}

/// <summary>
/// Employee loan or salary advance.
/// Kenya Employment Act Section 17 & 19 compliant.
/// </summary>
public class EmployeeLoan : BaseEntity
{
    public int EmployeeId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;  // Auto-generated
    public LoanType LoanType { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    // Financial details
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }  // Annual rate, 0 for advances
    public decimal TotalInterest { get; set; }
    public decimal TotalAmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingBalance => TotalAmountDue - AmountPaid;

    // Repayment terms
    public int NumberOfInstallments { get; set; }  // 1 for advance
    public decimal MonthlyInstallment { get; set; }
    public DateOnly DisbursementDate { get; set; }
    public DateOnly FirstDeductionDate { get; set; }
    public DateOnly ExpectedCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }

    // Purpose and documentation
    public string? Purpose { get; set; }
    public string? AgreementDocumentPath { get; set; }  // Signed loan agreement

    // Approval workflow
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Kenya compliance
    public bool ExceedsTwoMonthsSalary { get; set; }  // Warning flag
    public decimal EmployeeBasicSalaryAtApplication { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual ICollection<LoanRepayment> Repayments { get; set; } = new List<LoanRepayment>();
}

/// <summary>
/// Individual loan repayment record.
/// </summary>
public class LoanRepayment : BaseEntity
{
    public int EmployeeLoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? PaidDate { get; set; }
    public bool IsPaid => AmountPaid >= AmountDue;
    public bool IsFromPayroll { get; set; }  // True if deducted via payroll
    public int? PayslipDetailId { get; set; }  // Link to payslip if payroll deduction
    public string? Notes { get; set; }

    public virtual EmployeeLoan? EmployeeLoan { get; set; }
    public virtual PayslipDetail? PayslipDetail { get; set; }
}
```

#### 5.2.2 Service Interface

```csharp
// src/HospitalityPOS.Core/Interfaces/ILoanService.cs

public interface ILoanService
{
    // Loan/Advance CRUD
    Task<EmployeeLoan> CreateLoanApplicationAsync(LoanApplicationRequest request);
    Task<EmployeeLoan?> GetLoanByIdAsync(int loanId);
    Task<EmployeeLoan?> GetLoanByNumberAsync(string loanNumber);
    Task<IReadOnlyList<EmployeeLoan>> GetEmployeeLoansAsync(int employeeId, bool includeCompleted = false);
    Task<IReadOnlyList<EmployeeLoan>> GetPendingApprovalsAsync();
    Task<IReadOnlyList<EmployeeLoan>> GetActiveLoansAsync();

    // Approval workflow
    Task<LoanResult> ApproveLoanAsync(int loanId, int approverUserId, string? notes = null);
    Task<LoanResult> RejectLoanAsync(int loanId, int approverUserId, string reason);
    Task<LoanResult> CancelLoanAsync(int loanId, int userId, string reason);

    // Disbursement
    Task<LoanResult> MarkAsDisbursedAsync(int loanId, DateOnly disbursementDate);

    // Repayment
    Task<LoanResult> RecordRepaymentAsync(int loanId, decimal amount, DateOnly paymentDate, string? notes = null);
    Task<IReadOnlyList<LoanRepayment>> GetRepaymentScheduleAsync(int loanId);
    Task<decimal> GetPendingDeductionAsync(int employeeId, DateOnly payrollDate);
    Task RecordPayrollDeductionAsync(int loanId, int payslipDetailId, decimal amount);

    // Validation (Kenya compliance)
    Task<LoanEligibilityResult> CheckEligibilityAsync(int employeeId, decimal requestedAmount);
    Task<decimal> CalculateMaxLoanAmountAsync(int employeeId);
    Task<decimal> CalculateMaxDeductionAsync(int employeeId, decimal grossSalary, decimal otherDeductions);
    Task<bool> WouldExceedTwoThirdsRuleAsync(int employeeId, decimal newDeduction, decimal grossSalary, decimal existingDeductions);

    // Reports
    Task<LoanSummaryReport> GenerateSummaryReportAsync(DateOnly? asOfDate = null);
    Task<EmployeeLoanStatement> GenerateEmployeeStatementAsync(int employeeId, int loanId);
}

public record LoanApplicationRequest(
    int EmployeeId,
    LoanType LoanType,
    decimal Amount,
    int NumberOfInstallments,
    string? Purpose,
    DateOnly RequestedDisbursementDate
);

public record LoanResult(bool Success, string? Message, EmployeeLoan? Loan = null);
public record LoanEligibilityResult(
    bool IsEligible,
    decimal MaxEligibleAmount,
    decimal CurrentOutstandingLoans,
    List<string> Warnings
);
```

### 5.3 GAP-003: Fines/Penalties System

#### 5.3.1 Entities

```csharp
// src/HospitalityPOS.Core/Entities/DisciplinaryEntities.cs

namespace HospitalityPOS.Core.Entities;

public enum DeductionReasonType
{
    DamageToProperty = 0,       // Section 19(1)(b)
    AbsenceWithoutLeave = 1,    // Section 19(1)(c) - 1 day's wages per day
    CashShortage = 2,           // Section 19(1)(d) - negligence/dishonesty
    Overpayment = 3,            // Section 19(1)(e) - recovery of excess payment
    Other = 4                   // Must have written consent
}

public enum DisciplinaryDeductionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Deducted = 3,              // Applied to payroll
    Cancelled = 4,
    Appealed = 5
}

/// <summary>
/// Disciplinary deduction (fine/penalty).
/// Kenya Employment Act Section 19 compliant.
/// </summary>
public class DisciplinaryDeduction : BaseEntity
{
    public int EmployeeId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DeductionReasonType ReasonType { get; set; }
    public DisciplinaryDeductionStatus Status { get; set; } = DisciplinaryDeductionStatus.Pending;

    // Incident details
    public DateOnly IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // For absence deductions
    public int? DaysAbsent { get; set; }
    public decimal? DailyWageRate { get; set; }

    // For damage/shortage
    public string? EvidenceDocumentPath { get; set; }
    public decimal? ActualLossAmount { get; set; }

    // Employee acknowledgment
    public bool EmployeeAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? EmployeeResponse { get; set; }  // Employee's explanation

    // Approval
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    // Payroll integration
    public int? DeductedInPayslipId { get; set; }
    public DateOnly? DeductionDate { get; set; }

    // Appeal
    public bool IsAppealed { get; set; }
    public string? AppealReason { get; set; }
    public DateTime? AppealedAt { get; set; }
    public int? AppealReviewedByUserId { get; set; }
    public string? AppealDecision { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual Payslip? DeductedInPayslip { get; set; }
}
```

#### 5.3.2 Service Interface

```csharp
// src/HospitalityPOS.Core/Interfaces/IDisciplinaryDeductionService.cs

public interface IDisciplinaryDeductionService
{
    // CRUD
    Task<DisciplinaryDeduction> CreateDeductionAsync(DisciplinaryDeductionRequest request);
    Task<DisciplinaryDeduction?> GetByIdAsync(int id);
    Task<IReadOnlyList<DisciplinaryDeduction>> GetEmployeeDeductionsAsync(int employeeId);
    Task<IReadOnlyList<DisciplinaryDeduction>> GetPendingApprovalsAsync();

    // Workflow
    Task<DeductionResult> ApproveDeductionAsync(int deductionId, int approverUserId, string? notes = null);
    Task<DeductionResult> RejectDeductionAsync(int deductionId, int reviewerUserId, string reason);
    Task<DeductionResult> RecordEmployeeAcknowledgmentAsync(int deductionId, string? response = null);

    // Appeal
    Task<DeductionResult> SubmitAppealAsync(int deductionId, string reason);
    Task<DeductionResult> ProcessAppealAsync(int deductionId, int reviewerUserId, bool upheld, string decision);

    // Payroll integration
    Task<decimal> GetPendingDeductionsAsync(int employeeId);
    Task RecordPayrollDeductionAsync(int deductionId, int payslipId);

    // Validation
    Task<bool> WouldViolateTakeHomeRuleAsync(int employeeId, decimal deductionAmount, decimal grossSalary, decimal otherDeductions);

    // Helpers for specific deduction types
    Task<DisciplinaryDeduction> CreateAbsenceDeductionAsync(int employeeId, DateOnly[] absenceDates, string description);
    Task<DisciplinaryDeduction> CreateDamageDeductionAsync(int employeeId, decimal damageAmount, string description, string? evidencePath);
    Task<DisciplinaryDeduction> CreateCashShortageDeductionAsync(int employeeId, decimal shortageAmount, DateOnly incidentDate, string description);
}
```

### 5.4 GAP-004: Department Entity

```csharp
// src/HospitalityPOS.Core/Entities/Department.cs

public class Department : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ManagerEmployeeId { get; set; }
    public int? ParentDepartmentId { get; set; }
    public string? CostCenter { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Employee? Manager { get; set; }
    public virtual Department? ParentDepartment { get; set; }
    public virtual ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
```

**Employee Update:**
```csharp
// Update Employee entity
public class Employee : BaseEntity
{
    // ... existing fields ...

    // Change from string to foreign key
    public int? DepartmentId { get; set; }  // NEW: Replace Department string
    public virtual Department? DepartmentEntity { get; set; }  // NEW: Navigation

    // Keep for backward compatibility during migration
    [Obsolete("Use DepartmentId instead")]
    public string? Department { get; set; }
}
```

### 5.5 GAP-006: Master Payroll Report Enhancement

#### 5.5.1 New Report Service Methods

```csharp
// Add to IPayrollService

// Excel export using ClosedXML or EPPlus
Task<byte[]> ExportPayrollToExcelAsync(int payrollPeriodId);

// Bank payment files
Task<byte[]> GenerateBankPaymentFileAsync(int payrollPeriodId, BankFileFormat format);

// Statutory returns
Task<byte[]> GenerateKRAP9FormAsync(int employeeId, int year);
Task<byte[]> GenerateNSSFReturnAsync(int payrollPeriodId);
Task<byte[]> GenerateSHIFReturnAsync(int payrollPeriodId);
Task<byte[]> GenerateHousingLevyReturnAsync(int payrollPeriodId);

public enum BankFileFormat
{
    KCB,
    Equity,
    Cooperative,
    StandardChartered,
    NCBA,
    Generic // CSV format
}
```

#### 5.5.2 Master Payroll Summary Model

```csharp
public class MasterPayrollSummary
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly PayDate { get; set; }

    // Employee counts
    public int TotalEmployees { get; set; }
    public int FullTimeCount { get; set; }
    public int PartTimeCount { get; set; }
    public int ContractCount { get; set; }

    // Financial summary
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalGrossEarnings { get; set; }

    // Statutory deductions
    public decimal TotalPAYE { get; set; }
    public decimal TotalNSSFEmployee { get; set; }
    public decimal TotalNSSFEmployer { get; set; }
    public decimal TotalSHIF { get; set; }
    public decimal TotalHousingLevyEmployee { get; set; }
    public decimal TotalHousingLevyEmployer { get; set; }

    // Other deductions
    public decimal TotalLoanDeductions { get; set; }
    public decimal TotalAdvanceDeductions { get; set; }
    public decimal TotalDisciplinaryDeductions { get; set; }
    public decimal TotalOtherDeductions { get; set; }

    // Net pay
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }

    // Employer costs
    public decimal TotalEmployerStatutory { get; set; }
    public decimal GrandTotalCost { get; set; }

    // By department breakdown
    public List<DepartmentPayrollSummary> ByDepartment { get; set; } = new();

    // Individual payslips
    public List<PayslipSummaryLine> Payslips { get; set; } = new();
}
```

### 5.6 GAP-007: Termination/Separation Entity

```csharp
// src/HospitalityPOS.Core/Entities/EmployeeTermination.cs

public enum TerminationType
{
    Resignation = 0,
    Redundancy = 1,
    Dismissal = 2,          // For cause
    EndOfContract = 3,
    Retirement = 4,
    Death = 5,
    MutualAgreement = 6
}

public enum TerminationStatus
{
    Initiated = 0,
    PendingApproval = 1,
    Approved = 2,
    Processing = 3,         // Calculating final dues
    AwaitingPayment = 4,
    Completed = 5
}

public class EmployeeTermination : BaseEntity
{
    public int EmployeeId { get; set; }
    public TerminationType TerminationType { get; set; }
    public TerminationStatus Status { get; set; } = TerminationStatus.Initiated;

    // Dates
    public DateOnly NoticeDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly LastWorkingDay { get; set; }
    public int NoticePeriodDays { get; set; }
    public bool NoticePeriodServed { get; set; }

    // Reason
    public string Reason { get; set; } = string.Empty;
    public string? DetailedNotes { get; set; }

    // Final settlement calculation
    public decimal DaysWorkedInFinalMonth { get; set; }
    public decimal ProRataBasicSalary { get; set; }
    public decimal AccruedLeavedays { get; set; }
    public decimal LeavePayment { get; set; }
    public decimal NoticePay { get; set; }          // If notice not served
    public decimal SeverancePay { get; set; }       // 15 days per year
    public decimal OtherEarnings { get; set; }
    public decimal TotalEarnings { get; set; }

    // Deductions
    public decimal OutstandingLoans { get; set; }
    public decimal OutstandingAdvances { get; set; }
    public decimal PendingDeductions { get; set; }
    public decimal TaxOnTermination { get; set; }
    public decimal TotalDeductions { get; set; }

    // Final amount
    public decimal NetFinalSettlement { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? PaymentReference { get; set; }

    // Approval
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Certificate of Service
    public bool CertificateIssued { get; set; }
    public DateOnly? CertificateIssuedDate { get; set; }

    // Clearance
    public bool ITClearance { get; set; }
    public bool FinanceClearance { get; set; }
    public bool HRClearance { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual User? ApprovedByUser { get; set; }
}
```

#### Severance Calculation Service

```csharp
// Add to IEmployeeService or create ITerminationService

public interface ITerminationService
{
    Task<EmployeeTermination> InitiateTerminationAsync(TerminationRequest request);
    Task<EmployeeTermination?> GetByIdAsync(int terminationId);
    Task<EmployeeTermination?> GetByEmployeeIdAsync(int employeeId);

    // Calculations
    Task<TerminationCalculation> CalculateFinalSettlementAsync(int employeeId, TerminationType type, DateOnly effectiveDate);
    Task<decimal> CalculateSeverancePayAsync(int employeeId, DateOnly terminationDate);
    Task<decimal> CalculateLeaveEncashmentAsync(int employeeId, DateOnly terminationDate);
    Task<decimal> CalculateNoticePayAsync(int employeeId, bool noticePeriodServed);

    // Workflow
    Task<TerminationResult> ApproveTerminationAsync(int terminationId, int approverUserId);
    Task<TerminationResult> ProcessFinalPaymentAsync(int terminationId);
    Task<TerminationResult> CompleteClearanceAsync(int terminationId, ClearanceType clearanceType);

    // Documents
    Task<byte[]> GenerateCertificateOfServiceAsync(int terminationId);
    Task<byte[]> GenerateFinalSettlementStatementAsync(int terminationId);
}

public record TerminationCalculation(
    decimal ProRataSalary,
    decimal LeaveEncashment,
    decimal NoticePay,
    decimal SeverancePay,
    decimal TotalEarnings,
    decimal OutstandingLoans,
    decimal TaxPayable,
    decimal TotalDeductions,
    decimal NetSettlement,
    int YearsOfService,
    int AccruedLeaveDays
);
```

---

## 6. Database Schema Designs

### 6.1 New Tables Summary

#### Employee Updates
| Table | Change | Description |
|-------|--------|-------------|
| `Employees` | Add `PhotoPath` | Path to employee photo file |
| `Employees` | Add `ThumbnailPath` | Path to thumbnail image |
| `Employees` | Add `DepartmentId` | FK to Departments table |

#### Statutory Configuration Tables (CRITICAL)
| Table | Primary Key | Description |
|-------|-------------|-------------|
| `PAYETaxBands` | Id | Progressive tax bands with effective dates |
| `PAYEReliefs` | Id | Personal relief, insurance relief amounts |
| `NSSFConfigurations` | Id | NSSF rates, tier limits, caps |
| `SHIFConfigurations` | Id | SHIF (ex-NHIF) rate configuration |
| `HousingLevyConfigurations` | Id | Housing levy employee/employer rates |
| `HELBDeductionBands` | Id | HELB salary bands and deduction amounts |

#### HR Module Tables
| Table | Primary Key | Foreign Keys |
|-------|-------------|--------------|
| `LeaveTypes` | Id | - |
| `LeaveAllocations` | Id | EmployeeId, LeaveTypeId |
| `LeaveRequests` | Id | EmployeeId, LeaveTypeId, ReviewedByUserId |
| `LeaveBalanceAdjustments` | Id | LeaveAllocationId, AdjustedByUserId |
| `PublicHolidays` | Id | - |
| `EmployeeLoans` | Id | EmployeeId, ApprovedByUserId |
| `LoanRepayments` | Id | EmployeeLoanId, PayslipDetailId |
| `DisciplinaryDeductions` | Id | EmployeeId, ApprovedByUserId, PayslipId |
| `Departments` | Id | ManagerEmployeeId, ParentDepartmentId |
| `EmployeeTerminations` | Id | EmployeeId, ApprovedByUserId |

### 6.2 Migration Script Structure

```csharp
// Migration: AddHRModuleEntities

public partial class AddHRModuleEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Create Departments table first (Employee FK depends on it)
        migrationBuilder.CreateTable(
            name: "Departments",
            columns: table => new { /* ... */ },
            constraints: table => { /* ... */ });

        // 2. Add DepartmentId to Employees
        migrationBuilder.AddColumn<int>(
            name: "DepartmentId",
            table: "Employees",
            nullable: true);

        // 3. Create Leave tables
        migrationBuilder.CreateTable(name: "LeaveTypes", /* ... */);
        migrationBuilder.CreateTable(name: "LeaveAllocations", /* ... */);
        migrationBuilder.CreateTable(name: "LeaveRequests", /* ... */);
        migrationBuilder.CreateTable(name: "LeaveBalanceAdjustments", /* ... */);
        migrationBuilder.CreateTable(name: "PublicHolidays", /* ... */);

        // 4. Create Loan tables
        migrationBuilder.CreateTable(name: "EmployeeLoans", /* ... */);
        migrationBuilder.CreateTable(name: "LoanRepayments", /* ... */);

        // 5. Create Disciplinary tables
        migrationBuilder.CreateTable(name: "DisciplinaryDeductions", /* ... */);

        // 6. Create Termination table
        migrationBuilder.CreateTable(name: "EmployeeTerminations", /* ... */);

        // 7. Seed Kenya leave types
        migrationBuilder.InsertData(table: "LeaveTypes", /* Kenya statutory leaves */);

        // 8. Seed Kenya public holidays 2026
        migrationBuilder.InsertData(table: "PublicHolidays", /* Kenya holidays */);
    }
}
```

---

## 7. Implementation Roadmap

### Phase 0: Foundation (Week 1) - CRITICAL

| Task | Priority | Effort | Dependencies |
|------|----------|--------|--------------|
| Add PhotoPath to Employee entity | **CRITICAL** | 1h | None |
| Create IEmployeePhotoService | **CRITICAL** | 2h | Entity update |
| Implement photo upload in WPF | **CRITICAL** | 4h | Service |
| Add photo display in Employee views | **CRITICAL** | 2h | Upload |
| Create StatutoryConfiguration entities | **CRITICAL** | 4h | None |
| Create StatutoryConfiguration DB config | **CRITICAL** | 2h | Entities |
| Generate Statutory migration with seed data | **CRITICAL** | 2h | Configuration |
| Implement IStatutoryConfigurationService | **CRITICAL** | 6h | Migration |
| Update PayrollService to use config DB | **CRITICAL** | 4h | Config service |
| Create StatutoryRatesViewModel | **CRITICAL** | 3h | Service |
| Create Admin UI for statutory rates | **CRITICAL** | 6h | ViewModel |

### Phase 1: Leave Management (Weeks 2-3)

| Task | Priority | Effort | Dependencies |
|------|----------|--------|--------------|
| Create Leave entity classes | HIGH | 4h | None |
| Create Leave database configuration | HIGH | 2h | Entities |
| Generate Leave migration | HIGH | 1h | Configuration |
| Update LeaveService for database | HIGH | 8h | Migration |
| Create LeaveViewModel | HIGH | 4h | Service |
| Create Leave UI views | HIGH | 8h | ViewModel |

### Phase 2: Loans & Advances (Weeks 4-5)

| Task | Priority | Effort | Dependencies |
|------|----------|--------|--------------|
| Create Loan/Advance entities | HIGH | 4h | None |
| Create Loan database configuration | HIGH | 2h | Entities |
| Generate Loan migration | HIGH | 1h | Configuration |
| Implement LoanService | HIGH | 12h | Migration |
| Integrate loans with payroll | HIGH | 8h | Service |
| Create Loan UI views | HIGH | 8h | Service |

### Phase 3: Department & Disciplinary (Weeks 6-7)

| Task | Priority | Effort | Dependencies |
|------|----------|--------|--------------|
| Create Department entity | MEDIUM | 2h | None |
| Migrate Employee.Department to FK | MEDIUM | 4h | Entity |
| Create Disciplinary entities | MEDIUM | 4h | None |
| Implement DisciplinaryService | MEDIUM | 8h | Entities |
| Create Disciplinary UI views | MEDIUM | 6h | Service |

### Phase 4: Termination & Reports (Weeks 8-9)

| Task | Priority | Effort | Dependencies |
|------|----------|--------|--------------|
| Create Termination entity | MEDIUM | 4h | None |
| Implement TerminationService | MEDIUM | 8h | Entity |
| Severance calculation logic | MEDIUM | 4h | Service |
| Excel payroll export | MEDIUM | 8h | None |
| Bank file generation | MEDIUM | 8h | None |
| Statutory returns (P9, NSSF) | MEDIUM | 12h | None |

### Phase 5: Nice-to-Have (Future)

| Task | Priority | Effort |
|------|----------|--------|
| Emergency contacts entity | LOW | 4h |
| Document management | LOW | 12h |
| Training tracking | LOW | 8h |
| HELB deduction support | LOW | 4h |

---

## Appendix A: Kenya Public Holidays 2026

```csharp
// Seed data for PublicHolidays table
new[] {
    new { Date = "2026-01-01", Name = "New Year's Day" },
    new { Date = "2026-04-10", Name = "Good Friday" },
    new { Date = "2026-04-13", Name = "Easter Monday" },
    new { Date = "2026-05-01", Name = "Labour Day" },
    new { Date = "2026-06-01", Name = "Madaraka Day" },
    new { Date = "2026-10-10", Name = "Huduma Day" },
    new { Date = "2026-10-20", Name = "Mashujaa Day" },
    new { Date = "2026-12-12", Name = "Jamhuri Day" },
    new { Date = "2026-12-25", Name = "Christmas Day" },
    new { Date = "2026-12-26", Name = "Boxing Day" }
}
```

---

## Appendix B: PAYE Tax Tables 2025/2026

```csharp
public static class KenyaPAYETaxBands
{
    public static readonly (decimal UpperLimit, decimal Rate)[] Bands2025 = new[]
    {
        (24000m, 0.10m),      // 10% up to 24,000
        (32333m, 0.25m),      // 25% from 24,001 to 32,333
        (500000m, 0.30m),     // 30% from 32,334 to 500,000
        (800000m, 0.325m),    // 32.5% from 500,001 to 800,000
        (decimal.MaxValue, 0.35m)  // 35% above 800,000
    };

    public const decimal PersonalRelief = 2400m;  // Monthly
    public const decimal InsuranceRelief = 5000m; // Monthly max
}
```

---

## Appendix C: Reference Links

1. **Kenya Employment Act 2007** - https://kenyalaw.org
2. **KRA iTax Portal** - https://itax.kra.go.ke
3. **NSSF Kenya** - https://www.nssf.or.ke
4. **Social Health Authority** - https://www.sha.go.ke
5. **Housing Levy** - https://housinglevy.go.ke

---

*Document End*
