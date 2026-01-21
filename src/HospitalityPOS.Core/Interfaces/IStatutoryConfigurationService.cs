// src/HospitalityPOS.Core/Interfaces/IStatutoryConfigurationService.cs
// Service interface for managing configurable statutory deduction rates.

using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing statutory deduction rate configurations.
/// Allows admin to update rates when government changes them.
/// </summary>
public interface IStatutoryConfigurationService
{
    #region PAYE Tax Bands

    /// <summary>
    /// Gets active PAYE tax bands as of a specific date.
    /// </summary>
    Task<IReadOnlyList<PAYETaxBand>> GetActivePAYEBandsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all PAYE tax bands including inactive ones.
    /// </summary>
    Task<IReadOnlyList<PAYETaxBand>> GetAllPAYEBandsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new PAYE tax band.
    /// </summary>
    Task<PAYETaxBand> CreatePAYEBandAsync(PAYETaxBandRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing PAYE tax band.
    /// </summary>
    Task<PAYETaxBand> UpdatePAYEBandAsync(int id, PAYETaxBandRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a PAYE tax band by setting its effective end date.
    /// </summary>
    Task<bool> DeactivatePAYEBandAsync(int id, DateOnly effectiveDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies existing PAYE bands for a new period with new effective dates.
    /// </summary>
    Task<int> CopyPAYEBandsForNewPeriodAsync(DateOnly effectiveFrom, string description, CancellationToken cancellationToken = default);

    #endregion

    #region PAYE Reliefs

    /// <summary>
    /// Gets active PAYE reliefs (personal relief, insurance relief, etc.).
    /// </summary>
    Task<IReadOnlyList<PAYERelief>> GetActiveReliefsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all PAYE reliefs including inactive ones.
    /// </summary>
    Task<IReadOnlyList<PAYERelief>> GetAllReliefsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new PAYE relief.
    /// </summary>
    Task<PAYERelief> CreateReliefAsync(PAYEReliefRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing PAYE relief.
    /// </summary>
    Task<PAYERelief> UpdateReliefAsync(int id, PAYEReliefRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current personal relief amount.
    /// </summary>
    Task<decimal> GetPersonalReliefAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    #endregion

    #region NSSF Configuration

    /// <summary>
    /// Gets the active NSSF configuration.
    /// </summary>
    Task<NSSFConfiguration?> GetActiveNSSFConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all NSSF configurations (history).
    /// </summary>
    Task<IReadOnlyList<NSSFConfiguration>> GetNSSFHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new NSSF configuration.
    /// </summary>
    Task<NSSFConfiguration> CreateNSSFConfigAsync(NSSFConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing NSSF configuration.
    /// </summary>
    Task<NSSFConfiguration> UpdateNSSFConfigAsync(int id, NSSFConfigurationRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region SHIF Configuration

    /// <summary>
    /// Gets the active SHIF (ex-NHIF) configuration.
    /// </summary>
    Task<SHIFConfiguration?> GetActiveSHIFConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SHIF configurations (history).
    /// </summary>
    Task<IReadOnlyList<SHIFConfiguration>> GetSHIFHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new SHIF configuration.
    /// </summary>
    Task<SHIFConfiguration> CreateSHIFConfigAsync(SHIFConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing SHIF configuration.
    /// </summary>
    Task<SHIFConfiguration> UpdateSHIFConfigAsync(int id, SHIFConfigurationRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Housing Levy Configuration

    /// <summary>
    /// Gets the active Housing Levy configuration.
    /// </summary>
    Task<HousingLevyConfiguration?> GetActiveHousingLevyConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Housing Levy configurations (history).
    /// </summary>
    Task<IReadOnlyList<HousingLevyConfiguration>> GetHousingLevyHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Housing Levy configuration.
    /// </summary>
    Task<HousingLevyConfiguration> CreateHousingLevyConfigAsync(HousingLevyConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Housing Levy configuration.
    /// </summary>
    Task<HousingLevyConfiguration> UpdateHousingLevyConfigAsync(int id, HousingLevyConfigurationRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region HELB Deduction Bands

    /// <summary>
    /// Gets active HELB deduction bands.
    /// </summary>
    Task<IReadOnlyList<HELBDeductionBand>> GetActiveHELBBandsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new HELB deduction band.
    /// </summary>
    Task<HELBDeductionBand> CreateHELBBandAsync(HELBBandRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing HELB deduction band.
    /// </summary>
    Task<HELBDeductionBand> UpdateHELBBandAsync(int id, HELBBandRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the HELB deduction amount for a given salary.
    /// </summary>
    Task<decimal> GetHELBDeductionAsync(decimal grossSalary, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets a comprehensive summary of all current statutory rates.
    /// </summary>
    Task<StatutoryRatesSummary> GetCurrentRatesSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates effective dates for a new configuration.
    /// </summary>
    Task ValidateEffectiveDatesAsync(DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default Kenya statutory rates for 2025/2026.
    /// </summary>
    Task SeedDefaultRatesAsync(CancellationToken cancellationToken = default);

    #endregion
}

#region Request DTOs

/// <summary>
/// Request DTO for creating/updating PAYE tax bands.
/// </summary>
public record PAYETaxBandRequest(
    decimal LowerLimit,
    decimal? UpperLimit,
    decimal Rate,
    int DisplayOrder,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

/// <summary>
/// Request DTO for creating/updating PAYE reliefs.
/// </summary>
public record PAYEReliefRequest(
    string Name,
    string Code,
    decimal MonthlyAmount,
    decimal? MaximumAmount,
    decimal? PercentageRate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

/// <summary>
/// Request DTO for creating/updating NSSF configuration.
/// </summary>
public record NSSFConfigurationRequest(
    decimal EmployeeRate,
    decimal EmployerRate,
    decimal Tier1Limit,
    decimal Tier2Limit,
    decimal MaxEmployeeContribution,
    decimal MaxEmployerContribution,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

/// <summary>
/// Request DTO for creating/updating SHIF configuration.
/// </summary>
public record SHIFConfigurationRequest(
    decimal Rate,
    decimal? MinimumContribution,
    decimal? MaximumContribution,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

/// <summary>
/// Request DTO for creating/updating Housing Levy configuration.
/// </summary>
public record HousingLevyConfigurationRequest(
    decimal EmployeeRate,
    decimal EmployerRate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description
);

/// <summary>
/// Request DTO for creating/updating HELB deduction bands.
/// </summary>
public record HELBBandRequest(
    decimal LowerSalaryLimit,
    decimal? UpperSalaryLimit,
    decimal DeductionAmount,
    int DisplayOrder,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo
);

/// <summary>
/// Summary of all current statutory rates.
/// </summary>
public record StatutoryRatesSummary(
    IReadOnlyList<PAYETaxBand> PAYEBands,
    decimal PersonalRelief,
    decimal? InsuranceReliefMax,
    NSSFConfiguration? NSSF,
    SHIFConfiguration? SHIF,
    HousingLevyConfiguration? HousingLevy,
    IReadOnlyList<HELBDeductionBand> HELBBands,
    DateOnly AsOfDate
);

#endregion
