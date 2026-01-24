using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EmployeeTaxRelief entity.
/// </summary>
public class EmployeeTaxReliefConfiguration : IEntityTypeConfiguration<EmployeeTaxRelief>
{
    public void Configure(EntityTypeBuilder<EmployeeTaxRelief> builder)
    {
        builder.ToTable("EmployeeTaxReliefs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MonthlyAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.CalculatedRelief)
            .HasPrecision(18, 2);

        builder.Property(e => e.PolicyReference)
            .HasMaxLength(100);

        builder.Property(e => e.ProviderName)
            .HasMaxLength(200);

        builder.Property(e => e.ProviderKraPin)
            .HasMaxLength(20);

        builder.Property(e => e.SupportingDocumentPath)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.TaxReliefs)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => new { e.EmployeeId, e.ReliefType, e.IsActive });
    }
}

/// <summary>
/// EF Core configuration for HelbDeduction entity.
/// </summary>
public class HelbDeductionConfiguration : IEntityTypeConfiguration<HelbDeduction>
{
    public void Configure(EntityTypeBuilder<HelbDeduction> builder)
    {
        builder.ToTable("HelbDeductions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.HelbAccountNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TotalLoanBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.MonthlyDeduction)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalRepaid)
            .HasPrecision(18, 2);

        builder.Property(e => e.NotificationDocumentPath)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.HelbDeductions)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.HelbAccountNumber);
        builder.HasIndex(e => new { e.EmployeeId, e.IsActive });
    }
}

/// <summary>
/// EF Core configuration for P9Record entity.
/// </summary>
public class P9RecordConfiguration : IEntityTypeConfiguration<P9Record>
{
    public void Configure(EntityTypeBuilder<P9Record> builder)
    {
        builder.ToTable("P9Records");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeePIN)
            .HasMaxLength(20);

        builder.Property(e => e.EmployerPIN)
            .HasMaxLength(20);

        builder.Property(e => e.TotalGrossPay)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalDefinedContribution)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalOwnerOccupiedInterest)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalRetirementContribution)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalChargeablePay)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalTaxCharged)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalPersonalRelief)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalInsuranceRelief)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalPAYE)
            .HasPrecision(18, 2);

        builder.Property(e => e.DocumentPath)
            .HasMaxLength(500);

        builder.Property(e => e.MonthlyDataJson)
            .HasColumnType("nvarchar(max)");

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.P9Records)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.EmployeeId, e.TaxYear }).IsUnique();
        builder.HasIndex(e => e.TaxYear);
    }
}

/// <summary>
/// EF Core configuration for StatutoryReturn entity.
/// </summary>
public class StatutoryReturnConfiguration : IEntityTypeConfiguration<StatutoryReturn>
{
    public void Configure(EntityTypeBuilder<StatutoryReturn> builder)
    {
        builder.ToTable("StatutoryReturns");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.SubmissionReference)
            .HasMaxLength(100);

        builder.Property(e => e.FilePath)
            .HasMaxLength(500);

        builder.Property(e => e.DetailsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.PayrollPeriod)
            .WithMany()
            .HasForeignKey(e => e.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.PayrollPeriodId);
        builder.HasIndex(e => e.ReturnType);
        builder.HasIndex(e => new { e.Year, e.Month, e.ReturnType });
        builder.HasIndex(e => e.IsSubmitted);
    }
}
