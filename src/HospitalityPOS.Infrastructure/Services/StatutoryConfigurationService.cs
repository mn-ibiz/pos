// src/HospitalityPOS.Infrastructure/Services/StatutoryConfigurationService.cs
// Implementation of statutory deduction configuration service.
// Allows configuration of Kenya statutory rates (PAYE, NSSF, SHIF, Housing Levy, HELB).

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing statutory deduction rate configurations.
/// </summary>
public class StatutoryConfigurationService : IStatutoryConfigurationService
{
    private readonly POSDbContext _context;

    public StatutoryConfigurationService(POSDbContext context)
    {
        _context = context;
    }

    #region PAYE Tax Bands

    public async Task<IReadOnlyList<PAYETaxBand>> GetActivePAYEBandsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.PAYETaxBands
            .Where(b => b.IsActive && b.EffectiveFrom <= date && (b.EffectiveTo == null || b.EffectiveTo >= date))
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PAYETaxBand>> GetAllPAYEBandsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PAYETaxBands
            .OrderByDescending(b => b.EffectiveFrom)
            .ThenBy(b => b.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<PAYETaxBand> CreatePAYEBandAsync(PAYETaxBandRequest request, CancellationToken cancellationToken = default)
    {
        var band = new PAYETaxBand
        {
            LowerLimit = request.LowerLimit,
            UpperLimit = request.UpperLimit,
            Rate = request.Rate,
            DisplayOrder = request.DisplayOrder,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.PAYETaxBands.Add(band);
        await _context.SaveChangesAsync(cancellationToken);
        return band;
    }

    public async Task<PAYETaxBand> UpdatePAYEBandAsync(int id, PAYETaxBandRequest request, CancellationToken cancellationToken = default)
    {
        var band = await _context.PAYETaxBands.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"PAYE tax band {id} not found");

        band.LowerLimit = request.LowerLimit;
        band.UpperLimit = request.UpperLimit;
        band.Rate = request.Rate;
        band.DisplayOrder = request.DisplayOrder;
        band.EffectiveFrom = request.EffectiveFrom;
        band.EffectiveTo = request.EffectiveTo;
        band.Description = request.Description;
        band.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return band;
    }

    public async Task<bool> DeactivatePAYEBandAsync(int id, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        var band = await _context.PAYETaxBands.FindAsync(new object[] { id }, cancellationToken);
        if (band == null) return false;

        band.EffectiveTo = effectiveDate;
        band.IsActive = false;
        band.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CopyPAYEBandsForNewPeriodAsync(DateOnly effectiveFrom, string description, CancellationToken cancellationToken = default)
    {
        var currentBands = await GetActivePAYEBandsAsync(cancellationToken: cancellationToken);
        var count = 0;

        foreach (var band in currentBands)
        {
            var newBand = new PAYETaxBand
            {
                LowerLimit = band.LowerLimit,
                UpperLimit = band.UpperLimit,
                Rate = band.Rate,
                DisplayOrder = band.DisplayOrder,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = null,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.PAYETaxBands.Add(newBand);
            count++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return count;
    }

    #endregion

    #region PAYE Reliefs

    public async Task<IReadOnlyList<PAYERelief>> GetActiveReliefsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.PAYEReliefs
            .Where(r => r.IsActive && r.EffectiveFrom <= date && (r.EffectiveTo == null || r.EffectiveTo >= date))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PAYERelief>> GetAllReliefsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PAYEReliefs
            .OrderByDescending(r => r.EffectiveFrom)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PAYERelief> CreateReliefAsync(PAYEReliefRequest request, CancellationToken cancellationToken = default)
    {
        var relief = new PAYERelief
        {
            Name = request.Name,
            Code = request.Code,
            MonthlyAmount = request.MonthlyAmount,
            MaximumAmount = request.MaximumAmount,
            PercentageRate = request.PercentageRate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.PAYEReliefs.Add(relief);
        await _context.SaveChangesAsync(cancellationToken);
        return relief;
    }

    public async Task<PAYERelief> UpdateReliefAsync(int id, PAYEReliefRequest request, CancellationToken cancellationToken = default)
    {
        var relief = await _context.PAYEReliefs.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"PAYE relief {id} not found");

        relief.Name = request.Name;
        relief.Code = request.Code;
        relief.MonthlyAmount = request.MonthlyAmount;
        relief.MaximumAmount = request.MaximumAmount;
        relief.PercentageRate = request.PercentageRate;
        relief.EffectiveFrom = request.EffectiveFrom;
        relief.EffectiveTo = request.EffectiveTo;
        relief.Description = request.Description;
        relief.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return relief;
    }

    public async Task<decimal> GetPersonalReliefAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var reliefs = await GetActiveReliefsAsync(asOfDate, cancellationToken);
        var personalRelief = reliefs.FirstOrDefault(r => r.Code == "PR" || r.Name.Contains("Personal", StringComparison.OrdinalIgnoreCase));
        return personalRelief?.MonthlyAmount ?? 2400m; // Default Kenya personal relief
    }

    #endregion

    #region NSSF Configuration

    public async Task<NSSFConfiguration?> GetActiveNSSFConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.NSSFConfigurations
            .Where(c => c.IsActive && c.EffectiveFrom <= date && (c.EffectiveTo == null || c.EffectiveTo >= date))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NSSFConfiguration>> GetNSSFHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NSSFConfigurations
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<NSSFConfiguration> CreateNSSFConfigAsync(NSSFConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = new NSSFConfiguration
        {
            EmployeeRate = request.EmployeeRate,
            EmployerRate = request.EmployerRate,
            Tier1Limit = request.Tier1Limit,
            Tier2Limit = request.Tier2Limit,
            MaxEmployeeContribution = request.MaxEmployeeContribution,
            MaxEmployerContribution = request.MaxEmployerContribution,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.NSSFConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<NSSFConfiguration> UpdateNSSFConfigAsync(int id, NSSFConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _context.NSSFConfigurations.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"NSSF configuration {id} not found");

        config.EmployeeRate = request.EmployeeRate;
        config.EmployerRate = request.EmployerRate;
        config.Tier1Limit = request.Tier1Limit;
        config.Tier2Limit = request.Tier2Limit;
        config.MaxEmployeeContribution = request.MaxEmployeeContribution;
        config.MaxEmployerContribution = request.MaxEmployerContribution;
        config.EffectiveFrom = request.EffectiveFrom;
        config.EffectiveTo = request.EffectiveTo;
        config.Description = request.Description;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    #endregion

    #region SHIF Configuration

    public async Task<SHIFConfiguration?> GetActiveSHIFConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.SHIFConfigurations
            .Where(c => c.IsActive && c.EffectiveFrom <= date && (c.EffectiveTo == null || c.EffectiveTo >= date))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SHIFConfiguration>> GetSHIFHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SHIFConfigurations
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<SHIFConfiguration> CreateSHIFConfigAsync(SHIFConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = new SHIFConfiguration
        {
            Rate = request.Rate,
            MinimumContribution = request.MinimumContribution,
            MaximumContribution = request.MaximumContribution,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SHIFConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<SHIFConfiguration> UpdateSHIFConfigAsync(int id, SHIFConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _context.SHIFConfigurations.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"SHIF configuration {id} not found");

        config.Rate = request.Rate;
        config.MinimumContribution = request.MinimumContribution;
        config.MaximumContribution = request.MaximumContribution;
        config.EffectiveFrom = request.EffectiveFrom;
        config.EffectiveTo = request.EffectiveTo;
        config.Description = request.Description;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    #endregion

    #region Housing Levy Configuration

    public async Task<HousingLevyConfiguration?> GetActiveHousingLevyConfigAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.HousingLevyConfigurations
            .Where(c => c.IsActive && c.EffectiveFrom <= date && (c.EffectiveTo == null || c.EffectiveTo >= date))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HousingLevyConfiguration>> GetHousingLevyHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HousingLevyConfigurations
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<HousingLevyConfiguration> CreateHousingLevyConfigAsync(HousingLevyConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = new HousingLevyConfiguration
        {
            EmployeeRate = request.EmployeeRate,
            EmployerRate = request.EmployerRate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.HousingLevyConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<HousingLevyConfiguration> UpdateHousingLevyConfigAsync(int id, HousingLevyConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _context.HousingLevyConfigurations.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Housing Levy configuration {id} not found");

        config.EmployeeRate = request.EmployeeRate;
        config.EmployerRate = request.EmployerRate;
        config.EffectiveFrom = request.EffectiveFrom;
        config.EffectiveTo = request.EffectiveTo;
        config.Description = request.Description;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    #endregion

    #region HELB Deduction Bands

    public async Task<IReadOnlyList<HELBDeductionBand>> GetActiveHELBBandsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _context.HELBDeductionBands
            .Where(b => b.IsActive && b.EffectiveFrom <= date && (b.EffectiveTo == null || b.EffectiveTo >= date))
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<HELBDeductionBand> CreateHELBBandAsync(HELBBandRequest request, CancellationToken cancellationToken = default)
    {
        var band = new HELBDeductionBand
        {
            LowerSalaryLimit = request.LowerSalaryLimit,
            UpperSalaryLimit = request.UpperSalaryLimit,
            DeductionAmount = request.DeductionAmount,
            DisplayOrder = request.DisplayOrder,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.HELBDeductionBands.Add(band);
        await _context.SaveChangesAsync(cancellationToken);
        return band;
    }

    public async Task<HELBDeductionBand> UpdateHELBBandAsync(int id, HELBBandRequest request, CancellationToken cancellationToken = default)
    {
        var band = await _context.HELBDeductionBands.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"HELB band {id} not found");

        band.LowerSalaryLimit = request.LowerSalaryLimit;
        band.UpperSalaryLimit = request.UpperSalaryLimit;
        band.DeductionAmount = request.DeductionAmount;
        band.DisplayOrder = request.DisplayOrder;
        band.EffectiveFrom = request.EffectiveFrom;
        band.EffectiveTo = request.EffectiveTo;
        band.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return band;
    }

    public async Task<decimal> GetHELBDeductionAsync(decimal grossSalary, DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var bands = await GetActiveHELBBandsAsync(asOfDate, cancellationToken);

        var applicableBand = bands.FirstOrDefault(b =>
            grossSalary >= b.LowerSalaryLimit &&
            (b.UpperSalaryLimit == null || grossSalary <= b.UpperSalaryLimit));

        return applicableBand?.DeductionAmount ?? 0m;
    }

    #endregion

    #region Utility Methods

    public async Task<StatutoryRatesSummary> GetCurrentRatesSummaryAsync(CancellationToken cancellationToken = default)
    {
        var asOfDate = DateOnly.FromDateTime(DateTime.Today);

        var payeBands = await GetActivePAYEBandsAsync(asOfDate, cancellationToken);
        var personalRelief = await GetPersonalReliefAsync(asOfDate, cancellationToken);
        var reliefs = await GetActiveReliefsAsync(asOfDate, cancellationToken);
        var insuranceRelief = reliefs.FirstOrDefault(r => r.Code == "IR" || r.Name.Contains("Insurance", StringComparison.OrdinalIgnoreCase));
        var nssf = await GetActiveNSSFConfigAsync(asOfDate, cancellationToken);
        var shif = await GetActiveSHIFConfigAsync(asOfDate, cancellationToken);
        var housingLevy = await GetActiveHousingLevyConfigAsync(asOfDate, cancellationToken);
        var helbBands = await GetActiveHELBBandsAsync(asOfDate, cancellationToken);

        return new StatutoryRatesSummary(
            PAYEBands: payeBands,
            PersonalRelief: personalRelief,
            InsuranceReliefMax: insuranceRelief?.MaximumAmount,
            NSSF: nssf,
            SHIF: shif,
            HousingLevy: housingLevy,
            HELBBands: helbBands,
            AsOfDate: asOfDate
        );
    }

    public Task ValidateEffectiveDatesAsync(DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("Effective end date cannot be before effective start date");
        }

        return Task.CompletedTask;
    }

    public async Task SeedDefaultRatesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfYear = new DateOnly(today.Year, 1, 1);

        // Check if data already exists
        if (await _context.PAYETaxBands.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        // Kenya PAYE Tax Bands (2025/2026)
        var payeBands = new[]
        {
            new PAYETaxBand { LowerLimit = 0, UpperLimit = 24000, Rate = 10, DisplayOrder = 1, EffectiveFrom = firstOfYear, IsActive = true, Description = "First KES 24,000", CreatedAt = DateTime.UtcNow },
            new PAYETaxBand { LowerLimit = 24000.01m, UpperLimit = 32333, Rate = 25, DisplayOrder = 2, EffectiveFrom = firstOfYear, IsActive = true, Description = "KES 24,001 - 32,333", CreatedAt = DateTime.UtcNow },
            new PAYETaxBand { LowerLimit = 32333.01m, UpperLimit = 500000, Rate = 30, DisplayOrder = 3, EffectiveFrom = firstOfYear, IsActive = true, Description = "KES 32,334 - 500,000", CreatedAt = DateTime.UtcNow },
            new PAYETaxBand { LowerLimit = 500000.01m, UpperLimit = 800000, Rate = 32.5m, DisplayOrder = 4, EffectiveFrom = firstOfYear, IsActive = true, Description = "KES 500,001 - 800,000", CreatedAt = DateTime.UtcNow },
            new PAYETaxBand { LowerLimit = 800000.01m, UpperLimit = null, Rate = 35, DisplayOrder = 5, EffectiveFrom = firstOfYear, IsActive = true, Description = "Above KES 800,000", CreatedAt = DateTime.UtcNow }
        };
        _context.PAYETaxBands.AddRange(payeBands);

        // PAYE Reliefs
        var reliefs = new[]
        {
            new PAYERelief { Name = "Personal Relief", Code = "PR", MonthlyAmount = 2400, EffectiveFrom = firstOfYear, IsActive = true, Description = "Monthly personal relief", CreatedAt = DateTime.UtcNow },
            new PAYERelief { Name = "Insurance Relief", Code = "IR", MonthlyAmount = 0, MaximumAmount = 5000, PercentageRate = 15, EffectiveFrom = firstOfYear, IsActive = true, Description = "15% of premiums, max KES 5,000/month", CreatedAt = DateTime.UtcNow }
        };
        _context.PAYEReliefs.AddRange(reliefs);

        // NSSF Configuration (New rates - tiered system)
        var nssf = new NSSFConfiguration
        {
            EmployeeRate = 6,
            EmployerRate = 6,
            Tier1Limit = 7000, // Pensionable earnings up to KES 7,000
            Tier2Limit = 36000, // Pensionable earnings above KES 7,000 up to KES 36,000
            MaxEmployeeContribution = 2160, // 6% of 36,000
            MaxEmployerContribution = 2160,
            EffectiveFrom = firstOfYear,
            IsActive = true,
            Description = "NSSF Tier I & II contributions",
            CreatedAt = DateTime.UtcNow
        };
        _context.NSSFConfigurations.Add(nssf);

        // SHIF Configuration (replaced NHIF)
        var shif = new SHIFConfiguration
        {
            Rate = 2.75m,
            MinimumContribution = null,
            MaximumContribution = null,
            EffectiveFrom = firstOfYear,
            IsActive = true,
            Description = "SHIF 2.75% of gross salary",
            CreatedAt = DateTime.UtcNow
        };
        _context.SHIFConfigurations.Add(shif);

        // Housing Levy
        var housingLevy = new HousingLevyConfiguration
        {
            EmployeeRate = 1.5m,
            EmployerRate = 1.5m,
            EffectiveFrom = firstOfYear,
            IsActive = true,
            Description = "Affordable Housing Levy 1.5% employee + 1.5% employer",
            CreatedAt = DateTime.UtcNow
        };
        _context.HousingLevyConfigurations.Add(housingLevy);

        // HELB Deduction Bands
        var helbBands = new[]
        {
            new HELBDeductionBand { LowerSalaryLimit = 0, UpperSalaryLimit = 50000, DeductionAmount = 1500, DisplayOrder = 1, EffectiveFrom = firstOfYear, IsActive = true, CreatedAt = DateTime.UtcNow },
            new HELBDeductionBand { LowerSalaryLimit = 50000.01m, UpperSalaryLimit = 100000, DeductionAmount = 2000, DisplayOrder = 2, EffectiveFrom = firstOfYear, IsActive = true, CreatedAt = DateTime.UtcNow },
            new HELBDeductionBand { LowerSalaryLimit = 100000.01m, UpperSalaryLimit = 150000, DeductionAmount = 3000, DisplayOrder = 3, EffectiveFrom = firstOfYear, IsActive = true, CreatedAt = DateTime.UtcNow },
            new HELBDeductionBand { LowerSalaryLimit = 150000.01m, UpperSalaryLimit = null, DeductionAmount = 4000, DisplayOrder = 4, EffectiveFrom = firstOfYear, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        _context.HELBDeductionBands.AddRange(helbBands);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
