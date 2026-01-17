using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(e => e.Id);

        // Use BIGINT for high volume
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100);

        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.MachineName)
            .HasMaxLength(100);

        builder.HasIndex(e => e.CreatedAt);

        // Additional indexes for common query patterns
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        builder.HasOne(e => e.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SettingKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SettingValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.SettingType)
            .HasMaxLength(50);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.SettingKey)
            .IsUnique();
    }
}
