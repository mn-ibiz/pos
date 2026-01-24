using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for CashPayout entity.
/// </summary>
public class CashPayoutConfiguration : IEntityTypeConfiguration<CashPayout>
{
    public void Configure(EntityTypeBuilder<CashPayout> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Reason)
            .IsRequired();

        builder.Property(p => p.CustomReason)
            .HasMaxLength(200);

        builder.Property(p => p.Reference)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.RecordedAt)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(p => p.WorkPeriod)
            .WithMany()
            .HasForeignKey(p => p.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RecordedByUser)
            .WithMany()
            .HasForeignKey(p => p.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ApprovedByUser)
            .WithMany()
            .HasForeignKey(p => p.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.WorkPeriodId);
        builder.HasIndex(p => new { p.WorkPeriodId, p.Status });
        builder.HasIndex(p => p.RecordedAt);
    }
}
