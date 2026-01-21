// src/HospitalityPOS.Infrastructure/Data/Configurations/StatutoryConfigurationConfiguration.cs
// EF Core configurations for statutory deduction rate entities.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for PAYETaxBand entity.
/// </summary>
public class PAYETaxBandConfiguration : IEntityTypeConfiguration<PAYETaxBand>
{
    public void Configure(EntityTypeBuilder<PAYETaxBand> builder)
    {
        builder.ToTable("PAYETaxBands");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LowerLimit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.UpperLimit)
            .HasPrecision(18, 2);

        builder.Property(e => e.Rate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => new { e.EffectiveFrom, e.DisplayOrder });
        builder.HasIndex(e => e.EffectiveFrom);
    }
}

/// <summary>
/// Configuration for PAYERelief entity.
/// </summary>
public class PAYEReliefConfiguration : IEntityTypeConfiguration<PAYERelief>
{
    public void Configure(EntityTypeBuilder<PAYERelief> builder)
    {
        builder.ToTable("PAYEReliefs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.MonthlyAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.MaximumAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.PercentageRate)
            .HasPrecision(8, 6);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.EffectiveFrom);
    }
}

/// <summary>
/// Configuration for NSSFConfiguration entity.
/// </summary>
public class NSSFConfigurationConfiguration : IEntityTypeConfiguration<NSSFConfiguration>
{
    public void Configure(EntityTypeBuilder<NSSFConfiguration> builder)
    {
        builder.ToTable("NSSFConfigurations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeRate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.EmployerRate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.Tier1Limit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Tier2Limit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.MaxEmployeeContribution)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.MaxEmployerContribution)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.EffectiveFrom);
    }
}

/// <summary>
/// Configuration for SHIFConfiguration entity.
/// </summary>
public class SHIFConfigurationConfiguration : IEntityTypeConfiguration<SHIFConfiguration>
{
    public void Configure(EntityTypeBuilder<SHIFConfiguration> builder)
    {
        builder.ToTable("SHIFConfigurations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Rate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.MinimumContribution)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaximumContribution)
            .HasPrecision(18, 2);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.EffectiveFrom);
    }
}

/// <summary>
/// Configuration for HousingLevyConfiguration entity.
/// </summary>
public class HousingLevyConfigurationConfiguration : IEntityTypeConfiguration<HousingLevyConfiguration>
{
    public void Configure(EntityTypeBuilder<HousingLevyConfiguration> builder)
    {
        builder.ToTable("HousingLevyConfigurations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeRate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.EmployerRate)
            .HasPrecision(8, 6)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.EffectiveFrom);
    }
}

/// <summary>
/// Configuration for HELBDeductionBand entity.
/// </summary>
public class HELBDeductionBandConfiguration : IEntityTypeConfiguration<HELBDeductionBand>
{
    public void Configure(EntityTypeBuilder<HELBDeductionBand> builder)
    {
        builder.ToTable("HELBDeductionBands");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LowerSalaryLimit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.UpperSalaryLimit)
            .HasPrecision(18, 2);

        builder.Property(e => e.DeductionAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(e => new { e.EffectiveFrom, e.DisplayOrder });
    }
}
