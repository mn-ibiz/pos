// src/HospitalityPOS.Core/Entities/EmployeeTaxRelief.cs
// Employee tax relief claims for PAYE calculation.
// Compliant with Kenya Income Tax Act.

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of tax relief available in Kenya.
/// </summary>
public enum TaxReliefType
{
    /// <summary>
    /// Personal relief - KES 2,400/month (KES 28,800/year).
    /// </summary>
    PersonalRelief = 0,

    /// <summary>
    /// Insurance relief - 15% of premiums up to KES 5,000/month (life, education, health).
    /// </summary>
    InsuranceRelief = 1,

    /// <summary>
    /// Disability exemption - up to KES 150,000/month for registered PWDs.
    /// </summary>
    DisabilityExemption = 2,

    /// <summary>
    /// Mortgage interest relief - 15% of interest up to KES 25,000/month.
    /// </summary>
    MortgageInterestRelief = 3,

    /// <summary>
    /// Housing levy affordability relief - 15% of contribution.
    /// </summary>
    HousingLevyRelief = 4,

    /// <summary>
    /// Home ownership savings plan - up to KES 8,000/month.
    /// </summary>
    HomeOwnershipSavingsPlan = 5,

    /// <summary>
    /// Pension contribution relief - up to 30% of gross or KES 20,000/month.
    /// </summary>
    PensionContributionRelief = 6,

    /// <summary>
    /// Registered retirement scheme relief.
    /// </summary>
    RetirementSchemeRelief = 7
}

/// <summary>
/// Employee's tax relief claim for PAYE deduction purposes.
/// </summary>
public class EmployeeTaxRelief : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Type of tax relief.
    /// </summary>
    public TaxReliefType ReliefType { get; set; }

    /// <summary>
    /// Monthly relief amount claimed (actual contribution or premium).
    /// </summary>
    public decimal MonthlyAmount { get; set; }

    /// <summary>
    /// Calculated monthly relief (after applying relief percentage and caps).
    /// </summary>
    public decimal CalculatedRelief { get; set; }

    /// <summary>
    /// Insurance policy number or pension reference (where applicable).
    /// </summary>
    public string? PolicyReference { get; set; }

    /// <summary>
    /// Provider name (insurance company, bank for mortgage, pension fund).
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Effective start date.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Effective end date (null if ongoing).
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Whether this relief is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Path to supporting document (P10A, insurance certificate, etc.).
    /// </summary>
    public string? SupportingDocumentPath { get; set; }

    /// <summary>
    /// KRA PIN of the provider (for registered providers).
    /// </summary>
    public string? ProviderKraPin { get; set; }

    /// <summary>
    /// Notes or additional information.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation property
    public virtual Employee? Employee { get; set; }
}

/// <summary>
/// HELB deduction schedule for an employee.
/// </summary>
public class HelbDeduction : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// HELB account number.
    /// </summary>
    public string HelbAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Total loan balance at registration.
    /// </summary>
    public decimal TotalLoanBalance { get; set; }

    /// <summary>
    /// Monthly deduction amount (from HELB schedule).
    /// </summary>
    public decimal MonthlyDeduction { get; set; }

    /// <summary>
    /// Amount already repaid.
    /// </summary>
    public decimal TotalRepaid { get; set; }

    /// <summary>
    /// Outstanding balance.
    /// </summary>
    public decimal OutstandingBalance => TotalLoanBalance - TotalRepaid;

    /// <summary>
    /// Date when employer was notified by HELB.
    /// </summary>
    public DateOnly NotificationDate { get; set; }

    /// <summary>
    /// Effective start date for deductions.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Effective end date (null if ongoing).
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Whether deductions are currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Path to HELB notification letter.
    /// </summary>
    public string? NotificationDocumentPath { get; set; }

    /// <summary>
    /// Last month deducted.
    /// </summary>
    public DateOnly? LastDeductionMonth { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation property
    public virtual Employee? Employee { get; set; }
}

/// <summary>
/// Annual P9 form record for employee tax certificates.
/// </summary>
public class P9Record : BaseEntity
{
    /// <summary>
    /// Employee ID.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Tax year (e.g., 2024).
    /// </summary>
    public int TaxYear { get; set; }

    /// <summary>
    /// Employee's KRA PIN.
    /// </summary>
    public string EmployeePIN { get; set; } = string.Empty;

    /// <summary>
    /// Employer's KRA PIN.
    /// </summary>
    public string EmployerPIN { get; set; } = string.Empty;

    // Monthly data stored as JSON or separate fields
    // Using individual month properties for clarity

    /// <summary>
    /// Total gross pay for the year.
    /// </summary>
    public decimal TotalGrossPay { get; set; }

    /// <summary>
    /// Total defined contribution (NSSF).
    /// </summary>
    public decimal TotalDefinedContribution { get; set; }

    /// <summary>
    /// Total owner-occupied interest (mortgage relief).
    /// </summary>
    public decimal TotalOwnerOccupiedInterest { get; set; }

    /// <summary>
    /// Retirement scheme contribution.
    /// </summary>
    public decimal TotalRetirementContribution { get; set; }

    /// <summary>
    /// Total chargeable pay (taxable income).
    /// </summary>
    public decimal TotalChargeablePay { get; set; }

    /// <summary>
    /// Total tax charged (before reliefs).
    /// </summary>
    public decimal TotalTaxCharged { get; set; }

    /// <summary>
    /// Total personal relief.
    /// </summary>
    public decimal TotalPersonalRelief { get; set; }

    /// <summary>
    /// Total insurance relief.
    /// </summary>
    public decimal TotalInsuranceRelief { get; set; }

    /// <summary>
    /// Total PAYE tax payable (after reliefs).
    /// </summary>
    public decimal TotalPAYE { get; set; }

    /// <summary>
    /// Date when P9 was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// User who generated the P9.
    /// </summary>
    public int? GeneratedByUserId { get; set; }

    /// <summary>
    /// Path to the generated P9 PDF.
    /// </summary>
    public string? DocumentPath { get; set; }

    /// <summary>
    /// Whether this P9 has been issued to the employee.
    /// </summary>
    public bool IsIssued { get; set; }

    /// <summary>
    /// Date when P9 was issued.
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Monthly breakdown stored as JSON.
    /// </summary>
    public string MonthlyDataJson { get; set; } = "[]";

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual User? GeneratedByUser { get; set; }
}

/// <summary>
/// Monthly P9 data structure for JSON storage.
/// </summary>
public class P9MonthlyData
{
    public int Month { get; set; }
    public decimal GrossPay { get; set; }
    public decimal DefinedContribution { get; set; }
    public decimal OwnerOccupiedInterest { get; set; }
    public decimal RetirementContribution { get; set; }
    public decimal ChargeablePay { get; set; }
    public decimal TaxCharged { get; set; }
    public decimal PersonalRelief { get; set; }
    public decimal InsuranceRelief { get; set; }
    public decimal PAYE { get; set; }
}

/// <summary>
/// Statutory return submission record.
/// </summary>
public class StatutoryReturn : BaseEntity
{
    /// <summary>
    /// Type of statutory return.
    /// </summary>
    public StatutoryReturnType ReturnType { get; set; }

    /// <summary>
    /// Payroll period this return covers.
    /// </summary>
    public int PayrollPeriodId { get; set; }

    /// <summary>
    /// Month of the return (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year of the return.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total amount for this return.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of employees included.
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Whether the return has been submitted.
    /// </summary>
    public bool IsSubmitted { get; set; }

    /// <summary>
    /// Submission date.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Reference number from the authority (after submission).
    /// </summary>
    public string? SubmissionReference { get; set; }

    /// <summary>
    /// Due date for submission.
    /// </summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Whether submitted late.
    /// </summary>
    public bool IsLate => IsSubmitted && SubmittedAt.HasValue &&
        DateOnly.FromDateTime(SubmittedAt.Value) > DueDate;

    /// <summary>
    /// Path to generated return file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Detailed breakdown as JSON.
    /// </summary>
    public string DetailsJson { get; set; } = "[]";

    /// <summary>
    /// User who generated/submitted.
    /// </summary>
    public int? GeneratedByUserId { get; set; }

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Notes about this return.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual PayrollPeriod? PayrollPeriod { get; set; }
    public virtual User? GeneratedByUser { get; set; }
}

/// <summary>
/// Type of statutory return.
/// </summary>
public enum StatutoryReturnType
{
    /// <summary>
    /// PAYE return to KRA.
    /// </summary>
    PAYE = 0,

    /// <summary>
    /// NSSF return.
    /// </summary>
    NSSF = 1,

    /// <summary>
    /// SHIF return (formerly NHIF).
    /// </summary>
    SHIF = 2,

    /// <summary>
    /// Housing Levy return.
    /// </summary>
    HousingLevy = 3,

    /// <summary>
    /// HELB deduction return.
    /// </summary>
    HELB = 4
}
