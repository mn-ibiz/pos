using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the XReportEntity.
/// </summary>
public class XReportConfiguration : IEntityTypeConfiguration<XReportEntity>
{
    public void Configure(EntityTypeBuilder<XReportEntity> builder)
    {
        builder.ToTable("XReports");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TerminalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ReportNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.GeneratedAt)
            .IsRequired();

        builder.Property(e => e.GrossSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.NetSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalPayments)
            .HasPrecision(18, 2);

        builder.Property(e => e.ExpectedCash)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(e => e.ReportNumber)
            .IsUnique()
            .HasDatabaseName("IX_XReports_ReportNumber");

        builder.HasIndex(e => e.WorkPeriodId)
            .HasDatabaseName("IX_XReports_WorkPeriodId");

        builder.HasIndex(e => e.TerminalId)
            .HasDatabaseName("IX_XReports_TerminalId");

        builder.HasIndex(e => new { e.TerminalId, e.GeneratedAt })
            .HasDatabaseName("IX_XReports_TerminalId_GeneratedAt");

        // Relationships
        builder.HasOne(e => e.WorkPeriod)
            .WithMany()
            .HasForeignKey(e => e.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Terminal)
            .WithMany()
            .HasForeignKey(e => e.TerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
