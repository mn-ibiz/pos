using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PrepTimingConfiguration entity.
/// </summary>
public class PrepTimingConfigurationEntityConfiguration : IEntityTypeConfiguration<Core.Entities.PrepTimingConfiguration>
{
    public void Configure(EntityTypeBuilder<Core.Entities.PrepTimingConfiguration> builder)
    {
        builder.ToTable("PrepTimingConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.EnablePrepTiming)
            .HasDefaultValue(false);

        builder.Property(c => c.DefaultPrepTimeSeconds)
            .HasDefaultValue(300);

        builder.Property(c => c.MinPrepTimeSeconds)
            .HasDefaultValue(60);

        builder.Property(c => c.TargetReadyBufferSeconds)
            .HasDefaultValue(60);

        builder.Property(c => c.AllowManualFireOverride)
            .HasDefaultValue(true);

        builder.Property(c => c.ShowWaitingItemsOnStation)
            .HasDefaultValue(true);

        builder.Property(c => c.Mode)
            .HasConversion<int>()
            .HasDefaultValue(PrepTimingMode.CourseLevel);

        builder.Property(c => c.AutoFireEnabled)
            .HasDefaultValue(true);

        builder.Property(c => c.OverdueThresholdSeconds)
            .HasDefaultValue(120);

        builder.Property(c => c.AlertOnOverdue)
            .HasDefaultValue(true);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.StoreId)
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for ItemFireSchedule entity.
/// </summary>
public class ItemFireScheduleConfiguration : IEntityTypeConfiguration<ItemFireSchedule>
{
    public void Configure(EntityTypeBuilder<ItemFireSchedule> builder)
    {
        builder.ToTable("ItemFireSchedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .HasDefaultValue(ItemFireStatus.Waiting);

        builder.Property(s => s.WasManuallyFired)
            .HasDefaultValue(false);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.HasOne(s => s.KdsOrderItem)
            .WithMany()
            .HasForeignKey(s => s.KdsOrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.KdsOrder)
            .WithMany()
            .HasForeignKey(s => s.KdsOrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Station)
            .WithMany()
            .HasForeignKey(s => s.StationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.FiredByUser)
            .WithMany()
            .HasForeignKey(s => s.FiredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes for common queries
        builder.HasIndex(s => s.KdsOrderItemId)
            .IsUnique();

        builder.HasIndex(s => s.KdsOrderId);

        builder.HasIndex(s => new { s.StoreId, s.Status });

        builder.HasIndex(s => new { s.StationId, s.Status });

        builder.HasIndex(s => new { s.Status, s.ScheduledFireAt });

        builder.HasIndex(s => new { s.StoreId, s.TargetReadyAt });
    }
}

/// <summary>
/// EF Core configuration for ProductPrepTimeConfig entity.
/// </summary>
public class ProductPrepTimeConfigConfiguration : IEntityTypeConfiguration<ProductPrepTimeConfig>
{
    public void Configure(EntityTypeBuilder<ProductPrepTimeConfig> builder)
    {
        builder.ToTable("ProductPrepTimeConfigs");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PrepTimeMinutes)
            .HasDefaultValue(5);

        builder.Property(p => p.PrepTimeSeconds)
            .HasDefaultValue(0);

        builder.Property(p => p.UsesPrepTiming)
            .HasDefaultValue(true);

        builder.Property(p => p.IsTimingIntegral)
            .HasDefaultValue(true);

        builder.Ignore(p => p.TotalPrepTimeSeconds);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ProductId, p.StoreId })
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for ModifierPrepTimeAdjustment entity.
/// </summary>
public class ModifierPrepTimeAdjustmentConfiguration : IEntityTypeConfiguration<ModifierPrepTimeAdjustment>
{
    public void Configure(EntityTypeBuilder<ModifierPrepTimeAdjustment> builder)
    {
        builder.ToTable("ModifierPrepTimeAdjustments");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AdjustmentSeconds)
            .HasDefaultValue(0);

        builder.Property(m => m.AdjustmentType)
            .HasConversion<int>()
            .HasDefaultValue(PrepTimeAdjustmentType.Integral);

        builder.HasOne(m => m.ModifierItem)
            .WithMany()
            .HasForeignKey(m => m.ModifierItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ModifierItemId, m.StoreId })
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for CategoryPrepTimeDefault entity.
/// </summary>
public class CategoryPrepTimeDefaultConfiguration : IEntityTypeConfiguration<CategoryPrepTimeDefault>
{
    public void Configure(EntityTypeBuilder<CategoryPrepTimeDefault> builder)
    {
        builder.ToTable("CategoryPrepTimeDefaults");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DefaultPrepTimeMinutes)
            .HasDefaultValue(5);

        builder.Property(c => c.DefaultPrepTimeSeconds)
            .HasDefaultValue(0);

        builder.Ignore(c => c.TotalPrepTimeSeconds);

        builder.HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.CategoryId, c.StoreId })
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for PrepTimingDailyMetrics entity.
/// </summary>
public class PrepTimingDailyMetricsConfiguration : IEntityTypeConfiguration<PrepTimingDailyMetrics>
{
    public void Configure(EntityTypeBuilder<PrepTimingDailyMetrics> builder)
    {
        builder.ToTable("PrepTimingDailyMetrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AccuracyRate)
            .HasPrecision(5, 4);

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.StoreId, m.Date })
            .IsUnique();

        builder.HasIndex(m => m.Date);
    }
}

/// <summary>
/// EF Core configuration for ProductPrepTimeAccuracy entity.
/// </summary>
public class ProductPrepTimeAccuracyConfiguration : IEntityTypeConfiguration<ProductPrepTimeAccuracy>
{
    public void Configure(EntityTypeBuilder<ProductPrepTimeAccuracy> builder)
    {
        builder.ToTable("ProductPrepTimeAccuracies");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StandardDeviationSeconds)
            .HasPrecision(10, 2);

        builder.Property(a => a.AccuracyRate)
            .HasPrecision(5, 4);

        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Store)
            .WithMany()
            .HasForeignKey(a => a.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(a => new { a.ProductId, a.StoreId })
            .IsUnique();

        builder.HasIndex(a => new { a.StoreId, a.AccuracyRate });
    }
}
