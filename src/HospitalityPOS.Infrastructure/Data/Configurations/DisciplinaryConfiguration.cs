// src/HospitalityPOS.Infrastructure/Data/Configurations/DisciplinaryConfiguration.cs
// EF Core configuration for disciplinary deduction entities.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for DisciplinaryDeduction entity.
/// </summary>
public class DisciplinaryDeductionConfiguration : IEntityTypeConfiguration<DisciplinaryDeduction>
{
    public void Configure(EntityTypeBuilder<DisciplinaryDeduction> builder)
    {
        builder.ToTable("DisciplinaryDeductions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReferenceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ReasonType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.DailyWageRate)
            .HasPrecision(18, 2);

        builder.Property(e => e.ActualLossAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.EvidenceDocumentPath)
            .HasMaxLength(500);

        builder.Property(e => e.EmployeeResponse)
            .HasMaxLength(1000);

        builder.Property(e => e.ApprovalNotes)
            .HasMaxLength(500);

        builder.Property(e => e.AppealReason)
            .HasMaxLength(1000);

        builder.Property(e => e.AppealDecision)
            .HasMaxLength(1000);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.ReferenceNumber).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IncidentDate);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.DisciplinaryDeductions)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WitnessEmployee)
            .WithMany()
            .HasForeignKey(e => e.WitnessEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AppealReviewedByUser)
            .WithMany()
            .HasForeignKey(e => e.AppealReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DeductedInPayslip)
            .WithMany()
            .HasForeignKey(e => e.DeductedInPayslipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
