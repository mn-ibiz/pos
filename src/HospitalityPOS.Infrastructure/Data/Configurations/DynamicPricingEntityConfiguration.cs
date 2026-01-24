using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for DynamicPricingConfiguration entity.
/// </summary>
public class DynamicPricingConfigurationEntityConfiguration : IEntityTypeConfiguration<Core.Entities.DynamicPricingConfiguration>
{
    public void Configure(EntityTypeBuilder<Core.Entities.DynamicPricingConfiguration> builder)
    {
        builder.ToTable("DynamicPricingConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.EnableDynamicPricing)
            .HasDefaultValue(false);

        builder.Property(c => c.RequireManagerApproval)
            .HasDefaultValue(true);

        builder.Property(c => c.MaxPriceIncreasePercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(25m);

        builder.Property(c => c.MaxPriceDecreasePercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(50m);

        builder.Property(c => c.PriceUpdateIntervalMinutes)
            .HasDefaultValue(15);

        builder.Property(c => c.ShowOriginalPrice)
            .HasDefaultValue(true);

        builder.Property(c => c.NotifyOnPriceChange)
            .HasDefaultValue(true);

        builder.Property(c => c.MinMarginPercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(10m);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.StoreId)
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for DynamicPricingRule entity.
/// </summary>
public class DynamicPricingRuleConfiguration : IEntityTypeConfiguration<DynamicPricingRule>
{
    public void Configure(EntityTypeBuilder<DynamicPricingRule> builder)
    {
        builder.ToTable("DynamicPricingRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.Trigger)
            .HasConversion<int>();

        builder.Property(r => r.AdjustmentType)
            .HasConversion<int>();

        builder.Property(r => r.AdjustmentValue)
            .HasPrecision(18, 4);

        builder.Property(r => r.MinPrice)
            .HasPrecision(18, 4);

        builder.Property(r => r.MaxPrice)
            .HasPrecision(18, 4);

        builder.Property(r => r.Priority)
            .HasDefaultValue(100);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.RequiresApproval)
            .HasDefaultValue(false);

        builder.Property(r => r.ActiveDays)
            .HasMaxLength(20);

        builder.Property(r => r.DemandThresholdHigh)
            .HasPrecision(18, 4);

        builder.Property(r => r.DemandThresholdLow)
            .HasPrecision(18, 4);

        builder.Property(r => r.WeatherCondition)
            .HasMaxLength(50);

        builder.Property(r => r.EventName)
            .HasMaxLength(100);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Store)
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Exceptions)
            .WithOne(e => e.Rule)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => new { r.StoreId, r.IsActive });
        builder.HasIndex(r => new { r.ProductId, r.IsActive });
        builder.HasIndex(r => new { r.CategoryId, r.IsActive });
        builder.HasIndex(r => r.Priority);
    }
}

/// <summary>
/// EF Core configuration for DynamicPricingException entity.
/// </summary>
public class DynamicPricingExceptionConfiguration : IEntityTypeConfiguration<DynamicPricingException>
{
    public void Configure(EntityTypeBuilder<DynamicPricingException> builder)
    {
        builder.ToTable("DynamicPricingExceptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason)
            .HasMaxLength(200);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.RuleId, e.ProductId })
            .IsUnique();
    }
}

/// <summary>
/// EF Core configuration for DynamicPriceLog entity.
/// </summary>
public class DynamicPriceLogConfiguration : IEntityTypeConfiguration<DynamicPriceLog>
{
    public void Configure(EntityTypeBuilder<DynamicPriceLog> builder)
    {
        builder.ToTable("DynamicPriceLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OriginalPrice)
            .HasPrecision(18, 4);

        builder.Property(l => l.AdjustedPrice)
            .HasPrecision(18, 4);

        builder.Property(l => l.AdjustmentAmount)
            .HasPrecision(18, 4);

        builder.Property(l => l.AdjustmentPercent)
            .HasPrecision(10, 4);

        builder.Property(l => l.Reason)
            .HasMaxLength(500);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Rule)
            .WithMany()
            .HasForeignKey(l => l.RuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.ApprovedByUser)
            .WithMany()
            .HasForeignKey(l => l.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Store)
            .WithMany()
            .HasForeignKey(l => l.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.RuleId);
        builder.HasIndex(l => l.AppliedAt);
        builder.HasIndex(l => new { l.StoreId, l.AppliedAt });
    }
}

/// <summary>
/// EF Core configuration for PendingPriceChange entity.
/// </summary>
public class PendingPriceChangeConfiguration : IEntityTypeConfiguration<PendingPriceChange>
{
    public void Configure(EntityTypeBuilder<PendingPriceChange> builder)
    {
        builder.ToTable("PendingPriceChanges");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CurrentPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.ProposedPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.Reason)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .HasDefaultValue(PriceChangeStatus.Pending);

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(500);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Rule)
            .WithMany()
            .HasForeignKey(p => p.RuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.RequestedByUser)
            .WithMany()
            .HasForeignKey(p => p.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReviewedByUser)
            .WithMany()
            .HasForeignKey(p => p.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.StoreId, p.Status });
        builder.HasIndex(p => p.ExpiresAt);
    }
}

/// <summary>
/// EF Core configuration for CurrentDynamicPrice entity.
/// </summary>
public class CurrentDynamicPriceConfiguration : IEntityTypeConfiguration<CurrentDynamicPrice>
{
    public void Configure(EntityTypeBuilder<CurrentDynamicPrice> builder)
    {
        builder.ToTable("CurrentDynamicPrices");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BasePrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.CurrentPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.IsAdjusted)
            .HasDefaultValue(false);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.AppliedRule)
            .WithMany()
            .HasForeignKey(p => p.AppliedRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.ProductId, p.StoreId })
            .IsUnique();

        builder.HasIndex(p => p.ExpiresAt);
    }
}

/// <summary>
/// EF Core configuration for DynamicPricingDailyMetrics entity.
/// </summary>
public class DynamicPricingDailyMetricsConfiguration : IEntityTypeConfiguration<DynamicPricingDailyMetrics>
{
    public void Configure(EntityTypeBuilder<DynamicPricingDailyMetrics> builder)
    {
        builder.ToTable("DynamicPricingDailyMetrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AverageAdjustmentPercent)
            .HasPrecision(10, 4);

        builder.Property(m => m.EstimatedRevenueImpact)
            .HasPrecision(18, 4);

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
/// EF Core configuration for DynamicPricingRuleMetrics entity.
/// </summary>
public class DynamicPricingRuleMetricsConfiguration : IEntityTypeConfiguration<DynamicPricingRuleMetrics>
{
    public void Configure(EntityTypeBuilder<DynamicPricingRuleMetrics> builder)
    {
        builder.ToTable("DynamicPricingRuleMetrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TotalSalesValue)
            .HasPrecision(18, 4);

        builder.Property(m => m.EstimatedRevenueImpact)
            .HasPrecision(18, 4);

        builder.HasOne(m => m.Rule)
            .WithMany()
            .HasForeignKey(m => m.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(m => new { m.RuleId, m.StoreId, m.Date })
            .IsUnique();

        builder.HasIndex(m => m.Date);
    }
}
