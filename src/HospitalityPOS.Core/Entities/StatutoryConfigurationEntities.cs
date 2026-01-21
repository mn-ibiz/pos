// src/HospitalityPOS.Core/Entities/StatutoryConfigurationEntities.cs
// Configurable statutory deduction rates for Kenya payroll compliance.
// Allows admin to update rates when government changes them without code changes.

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
    /// Upper limit of this tax band (exclusive). NULL for the top band (unlimited).
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

    /// <summary>
    /// Whether this band is currently active based on effective dates.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// PAYE personal relief and other tax reliefs configuration.
/// </summary>
public class PAYERelief : BaseEntity
{
    /// <summary>
    /// Relief type name (e.g., "Personal Relief", "Insurance Relief", "Mortgage Relief").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Relief code for system reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Monthly relief amount.
    /// </summary>
    public decimal MonthlyAmount { get; set; }

    /// <summary>
    /// Maximum monthly relief amount (for reliefs with caps like insurance relief).
    /// </summary>
    public decimal? MaximumAmount { get; set; }

    /// <summary>
    /// Percentage rate if relief is calculated as percentage (e.g., 15% for insurance relief).
    /// </summary>
    public decimal? PercentageRate { get; set; }

    /// <summary>
    /// Date this relief becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this relief expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Description for admin reference.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this relief is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// NSSF (National Social Security Fund) configuration.
/// Kenya NSSF Act 2013 with tier system.
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
    /// Lower Earnings Limit (Tier I ceiling) - KES 7,000 as of 2024.
    /// </summary>
    public decimal Tier1Limit { get; set; }

    /// <summary>
    /// Upper Earnings Limit (Tier II ceiling) - KES 36,000 as of 2024.
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

    /// <summary>
    /// Date this configuration becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this configuration expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Description for admin reference (e.g., "NSSF Act 2013").
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// SHIF (Social Health Insurance Fund) configuration.
/// Replaced NHIF effective October 2024 under Social Health Insurance Act 2023.
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
    /// Maximum monthly contribution (NULL = no cap, which is current SHIF rule).
    /// </summary>
    public decimal? MaximumContribution { get; set; }

    /// <summary>
    /// Date this configuration becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this configuration expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Description for admin reference.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// Housing Levy (Affordable Housing Levy) configuration.
/// Introduced under Affordable Housing Act.
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

    /// <summary>
    /// Date this configuration becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this configuration expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Description for admin reference.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// HELB (Higher Education Loans Board) deduction bands.
/// Deductions are based on salary bands for employees with student loans.
/// </summary>
public class HELBDeductionBand : BaseEntity
{
    /// <summary>
    /// Lower salary limit for this band (inclusive).
    /// </summary>
    public decimal LowerSalaryLimit { get; set; }

    /// <summary>
    /// Upper salary limit for this band (exclusive). NULL for top band.
    /// </summary>
    public decimal? UpperSalaryLimit { get; set; }

    /// <summary>
    /// Fixed deduction amount for this salary band.
    /// </summary>
    public decimal DeductionAmount { get; set; }

    /// <summary>
    /// Display order for UI and lookup sequence.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Date this band becomes effective.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Date this band expires. NULL if currently active.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Whether this band is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}
