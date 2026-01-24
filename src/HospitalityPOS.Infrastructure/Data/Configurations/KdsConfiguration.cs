using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for KdsStation entity.
/// </summary>
public class KdsStationConfiguration : IEntityTypeConfiguration<KdsStation>
{
    public void Configure(EntityTypeBuilder<KdsStation> builder)
    {
        builder.ToTable("KdsStations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DeviceIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.StationType)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.StationType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for KdsStationCategory entity.
/// </summary>
public class KdsStationCategoryConfiguration : IEntityTypeConfiguration<KdsStationCategory>
{
    public void Configure(EntityTypeBuilder<KdsStationCategory> builder)
    {
        builder.ToTable("KdsStationCategories");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Station)
            .WithMany(s => s.Categories)
            .HasForeignKey(e => e.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.StationId, e.CategoryId })
            .IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for KdsDisplaySettings entity.
/// </summary>
public class KdsDisplaySettingsConfiguration : IEntityTypeConfiguration<KdsDisplaySettings>
{
    public void Configure(EntityTypeBuilder<KdsDisplaySettings> builder)
    {
        builder.ToTable("KdsDisplaySettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ThemeName)
            .HasMaxLength(50);

        builder.Property(e => e.BackgroundColor)
            .HasMaxLength(20);

        // KdsStation has the FK to DisplaySettings, not the other way around
        builder.HasMany(e => e.Stations)
            .WithOne(s => s.DisplaySettings)
            .HasForeignKey(s => s.DisplaySettingsId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for KdsOrder entity.
/// </summary>
public class KdsOrderConfiguration : IEntityTypeConfiguration<KdsOrder>
{
    public void Configure(EntityTypeBuilder<KdsOrder> builder)
    {
        builder.ToTable("KdsOrders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderNumber)
            .HasMaxLength(50);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(100);

        builder.Property(e => e.TableNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.Priority)
            .IsRequired();

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for KdsOrderItem entity.
/// </summary>
public class KdsOrderItemConfiguration : IEntityTypeConfiguration<KdsOrderItem>
{
    public void Configure(EntityTypeBuilder<KdsOrderItem> builder)
    {
        builder.ToTable("KdsOrderItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Modifiers)
            .HasMaxLength(500);

        builder.Property(e => e.SpecialInstructions)
            .HasMaxLength(500);

        builder.Property(e => e.HoldReason)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.ItemFireStatus)
            .IsRequired();

        builder.HasOne(e => e.KdsOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(e => e.KdsOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.OrderItem)
            .WithMany()
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Station)
            .WithMany()
            .HasForeignKey(e => e.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CourseState)
            .WithMany(cs => cs.Items)
            .HasForeignKey(e => e.CourseStateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.KdsOrderId);
        builder.HasIndex(e => e.OrderItemId);
        builder.HasIndex(e => e.StationId);
        builder.HasIndex(e => e.CourseNumber);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ItemFireStatus);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for KdsOrderStatusLog entity.
/// </summary>
public class KdsOrderStatusLogConfiguration : IEntityTypeConfiguration<KdsOrderStatusLog>
{
    public void Configure(EntityTypeBuilder<KdsOrderStatusLog> builder)
    {
        builder.ToTable("KdsOrderStatusLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.KdsOrder)
            .WithMany(o => o.StatusLogs)
            .HasForeignKey(e => e.KdsOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ChangedByUser)
            .WithMany()
            .HasForeignKey(e => e.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.KdsOrderId);
        builder.HasIndex(e => e.ChangedAt);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for AllCallMessage entity.
/// </summary>
public class AllCallMessageConfiguration : IEntityTypeConfiguration<AllCallMessage>
{
    public void Configure(EntityTypeBuilder<AllCallMessage> builder)
    {
        builder.ToTable("AllCallMessages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Priority)
            .IsRequired();

        builder.HasOne(e => e.SentByUser)
            .WithMany()
            .HasForeignKey(e => e.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.SentByUserId);
        builder.HasIndex(e => e.SentAt);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for AllCallMessageTarget entity.
/// </summary>
public class AllCallMessageTargetConfiguration : IEntityTypeConfiguration<AllCallMessageTarget>
{
    public void Configure(EntityTypeBuilder<AllCallMessageTarget> builder)
    {
        builder.ToTable("AllCallMessageTargets");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Message)
            .WithMany(m => m.Targets)
            .HasForeignKey(e => e.AllCallMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Station)
            .WithMany()
            .HasForeignKey(e => e.KdsStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.AllCallMessageId, e.KdsStationId })
            .IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for AllCallMessageDismissal entity.
/// </summary>
public class AllCallMessageDismissalConfiguration : IEntityTypeConfiguration<AllCallMessageDismissal>
{
    public void Configure(EntityTypeBuilder<AllCallMessageDismissal> builder)
    {
        builder.ToTable("AllCallMessageDismissals");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Message)
            .WithMany(m => m.Dismissals)
            .HasForeignKey(e => e.AllCallMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Station)
            .WithMany()
            .HasForeignKey(e => e.KdsStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.DismissedByUser)
            .WithMany()
            .HasForeignKey(e => e.DismissedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AllCallMessageId, e.KdsStationId })
            .IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}
