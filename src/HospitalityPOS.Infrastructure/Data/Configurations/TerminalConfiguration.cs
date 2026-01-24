using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Terminal entity.
/// </summary>
public class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("Terminals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.MachineIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.TerminalType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.BusinessMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.IsMainRegister)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.PrinterConfiguration)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.HardwareConfiguration)
            .HasColumnType("nvarchar(max)");

        // Unique constraint: Code must be unique within a store
        builder.HasIndex(e => new { e.StoreId, e.Code })
            .IsUnique()
            .HasDatabaseName("UQ_Terminals_StoreId_Code");

        // Unique constraint: MachineIdentifier must be globally unique
        builder.HasIndex(e => e.MachineIdentifier)
            .IsUnique()
            .HasDatabaseName("UQ_Terminals_MachineIdentifier");

        // Index for quick lookup by store
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_Terminals_StoreId");

        // Index for active terminals
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Terminals_IsActive");

        // Index for heartbeat monitoring
        builder.HasIndex(e => e.LastHeartbeat)
            .HasDatabaseName("IX_Terminals_LastHeartbeat");

        // Relationships
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LastLoginUser)
            .WithMany()
            .HasForeignKey(e => e.LastLoginUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
