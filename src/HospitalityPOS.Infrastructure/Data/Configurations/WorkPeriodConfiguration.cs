using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class WorkPeriodConfiguration : IEntityTypeConfiguration<WorkPeriod>
{
    public void Configure(EntityTypeBuilder<WorkPeriod> builder)
    {
        builder.ToTable("WorkPeriods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OpenedAt)
            .IsRequired();

        builder.Property(e => e.OpeningFloat)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.ClosingCash)
            .HasPrecision(18, 2);

        builder.Property(e => e.ExpectedCash)
            .HasPrecision(18, 2);

        builder.Property(e => e.Variance)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(HospitalityPOS.Core.Enums.WorkPeriodStatus.Open);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.OpenedByUser)
            .WithMany(u => u.OpenedWorkPeriods)
            .HasForeignKey(e => e.OpenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ClosedByUser)
            .WithMany(u => u.ClosedWorkPeriods)
            .HasForeignKey(e => e.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
