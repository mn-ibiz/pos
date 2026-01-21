// src/HospitalityPOS.Infrastructure/Data/Configurations/TerminationConfiguration.cs
// EF Core configuration for employee termination entity.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for EmployeeTermination entity.
/// </summary>
public class EmployeeTerminationConfiguration : IEntityTypeConfiguration<EmployeeTermination>
{
    public void Configure(EntityTypeBuilder<EmployeeTermination> builder)
    {
        builder.ToTable("EmployeeTerminations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReferenceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TerminationType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.DetailedNotes)
            .HasMaxLength(2000);

        // Financial fields - Earnings
        builder.Property(e => e.DaysWorkedInFinalMonth).HasPrecision(5, 2);
        builder.Property(e => e.ProRataBasicSalary).HasPrecision(18, 2);
        builder.Property(e => e.AccruedLeaveDays).HasPrecision(5, 2);
        builder.Property(e => e.LeavePayment).HasPrecision(18, 2);
        builder.Property(e => e.NoticePay).HasPrecision(18, 2);
        builder.Property(e => e.SeverancePay).HasPrecision(18, 2);
        builder.Property(e => e.OtherEarnings).HasPrecision(18, 2);
        builder.Property(e => e.TotalEarnings).HasPrecision(18, 2);

        builder.Property(e => e.OtherEarningsDescription).HasMaxLength(500);

        // Financial fields - Deductions
        builder.Property(e => e.OutstandingLoans).HasPrecision(18, 2);
        builder.Property(e => e.OutstandingAdvances).HasPrecision(18, 2);
        builder.Property(e => e.PendingDeductions).HasPrecision(18, 2);
        builder.Property(e => e.TaxOnTermination).HasPrecision(18, 2);
        builder.Property(e => e.OtherDeductions).HasPrecision(18, 2);
        builder.Property(e => e.TotalDeductions).HasPrecision(18, 2);
        builder.Property(e => e.NetFinalSettlement).HasPrecision(18, 2);

        builder.Property(e => e.OtherDeductionsDescription).HasMaxLength(500);

        // Payment
        builder.Property(e => e.PaymentReference).HasMaxLength(100);
        builder.Property(e => e.PaymentMethod).HasMaxLength(50);

        // Approval
        builder.Property(e => e.ApprovalNotes).HasMaxLength(500);

        // Certificate
        builder.Property(e => e.CertificateDocumentPath).HasMaxLength(500);

        // Clearance notes
        builder.Property(e => e.ITClearanceNotes).HasMaxLength(500);
        builder.Property(e => e.FinanceClearanceNotes).HasMaxLength(500);
        builder.Property(e => e.HRClearanceNotes).HasMaxLength(500);
        builder.Property(e => e.OperationsClearanceNotes).HasMaxLength(500);

        // Exit interview
        builder.Property(e => e.ExitInterviewNotes).HasMaxLength(2000);

        // Indexes
        builder.HasIndex(e => e.ReferenceNumber).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.EffectiveDate);

        // Relationships
        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
