using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the CashDrawer entity.
/// </summary>
public class CashDrawerConfiguration : IEntityTypeConfiguration<CashDrawer>
{
    public void Configure(EntityTypeBuilder<CashDrawer> builder)
    {
        builder.ToTable("CashDrawers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LinkedPrinterId)
            .IsRequired();

        builder.Property(e => e.DrawerPin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_CashDrawers_IsActive");

        builder.HasIndex(e => e.LinkedPrinterId)
            .HasDatabaseName("IX_CashDrawers_LinkedPrinterId");

        // Relationships
        builder.HasOne(e => e.LinkedPrinter)
            .WithMany()
            .HasForeignKey(e => e.LinkedPrinterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LastOpenedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastOpenedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Logs)
            .WithOne(l => l.CashDrawer)
            .HasForeignKey(l => l.CashDrawerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity Framework Core configuration for the CashDrawerLog entity.
/// </summary>
public class CashDrawerLogConfiguration : IEntityTypeConfiguration<CashDrawerLog>
{
    public void Configure(EntityTypeBuilder<CashDrawerLog> builder)
    {
        builder.ToTable("CashDrawerLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CashDrawerId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Reference)
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.OpenedAt)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.CashDrawerId)
            .HasDatabaseName("IX_CashDrawerLogs_CashDrawerId");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_CashDrawerLogs_UserId");

        builder.HasIndex(e => e.OpenedAt)
            .HasDatabaseName("IX_CashDrawerLogs_OpenedAt");

        builder.HasIndex(e => new { e.CashDrawerId, e.OpenedAt })
            .HasDatabaseName("IX_CashDrawerLogs_CashDrawerId_OpenedAt");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AuthorizedByUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorizedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
