// src/HospitalityPOS.Infrastructure/Data/Configurations/LoanConfiguration.cs
// EF Core configurations for loan management entities.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for EmployeeLoan entity.
/// </summary>
public class EmployeeLoanConfiguration : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        builder.ToTable("EmployeeLoans");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LoanNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.LoanType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.PrincipalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.InterestRate)
            .HasPrecision(8, 4);

        builder.Property(e => e.TotalInterest)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmountDue)
            .HasPrecision(18, 2);

        builder.Property(e => e.AmountPaid)
            .HasPrecision(18, 2);

        builder.Property(e => e.MonthlyInstallment)
            .HasPrecision(18, 2);

        builder.Property(e => e.EmployeeBasicSalaryAtApplication)
            .HasPrecision(18, 2);

        builder.Property(e => e.Purpose)
            .HasMaxLength(500);

        builder.Property(e => e.AgreementDocumentPath)
            .HasMaxLength(500);

        builder.Property(e => e.ApprovalNotes)
            .HasMaxLength(500);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.LoanNumber).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.Loans)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Guarantor)
            .WithMany()
            .HasForeignKey(e => e.GuarantorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RejectedByUser)
            .WithMany()
            .HasForeignKey(e => e.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for LoanRepayment entity.
/// </summary>
public class LoanRepaymentConfiguration : IEntityTypeConfiguration<LoanRepayment>
{
    public void Configure(EntityTypeBuilder<LoanRepayment> builder)
    {
        builder.ToTable("LoanRepayments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AmountDue)
            .HasPrecision(18, 2);

        builder.Property(e => e.AmountPaid)
            .HasPrecision(18, 2);

        builder.Property(e => e.PrincipalPortion)
            .HasPrecision(18, 2);

        builder.Property(e => e.InterestPortion)
            .HasPrecision(18, 2);

        builder.Property(e => e.BalanceAfterPayment)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.EmployeeLoanId, e.InstallmentNumber }).IsUnique();
        builder.HasIndex(e => e.DueDate);

        builder.HasOne(e => e.EmployeeLoan)
            .WithMany(loan => loan.Repayments)
            .HasForeignKey(e => e.EmployeeLoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PayslipDetail)
            .WithMany()
            .HasForeignKey(e => e.PayslipDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
