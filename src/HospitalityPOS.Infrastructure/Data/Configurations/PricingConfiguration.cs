using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for PricingZone.
/// </summary>
public class PricingZoneConfiguration : IEntityTypeConfiguration<PricingZone>
{
    public void Configure(EntityTypeBuilder<PricingZone> builder)
    {
        builder.ToTable("PricingZones");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ZoneCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.CurrencyCode)
            .HasMaxLength(3)
            .HasDefaultValue("KES");

        builder.Property(e => e.DefaultTaxRate)
            .HasPrecision(5, 2);

        builder.Property(e => e.IsDefault)
            .HasDefaultValue(false);

        // Unique index on ZoneCode
        builder.HasIndex(e => e.ZoneCode)
            .IsUnique()
            .HasDatabaseName("IX_PricingZones_ZoneCode");

        // Index on IsDefault
        builder.HasIndex(e => e.IsDefault)
            .HasDatabaseName("IX_PricingZones_IsDefault");

        // Navigation to stores
        builder.HasMany(e => e.Stores)
            .WithOne(s => s.PricingZone)
            .HasForeignKey(s => s.PricingZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to zone prices
        builder.HasMany(e => e.ZonePrices)
            .WithOne(zp => zp.PricingZone)
            .HasForeignKey(zp => zp.PricingZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to scheduled price changes
        builder.HasMany(e => e.ScheduledPriceChanges)
            .WithOne(spc => spc.PricingZone)
            .HasForeignKey(spc => spc.PricingZoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for ZonePrice.
/// </summary>
public class ZonePriceConfiguration : IEntityTypeConfiguration<ZonePrice>
{
    public void Configure(EntityTypeBuilder<ZonePrice> builder)
    {
        builder.ToTable("ZonePrices");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.MinimumPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Reason)
            .HasMaxLength(200);

        // Foreign key to PricingZone
        builder.HasOne(e => e.PricingZone)
            .WithMany(pz => pz.ZonePrices)
            .HasForeignKey(e => e.PricingZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.ZonePrices)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique composite index on ZoneId + ProductId + EffectiveFrom
        builder.HasIndex(e => new { e.PricingZoneId, e.ProductId, e.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("IX_ZonePrices_Zone_Product_EffectiveFrom");

        // Index on ProductId for product-specific queries
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ZonePrices_ProductId");

        // Index on EffectiveFrom for date range queries
        builder.HasIndex(e => e.EffectiveFrom)
            .HasDatabaseName("IX_ZonePrices_EffectiveFrom");

        // Computed column ignored
        builder.Ignore(e => e.IsCurrentlyEffective);
    }
}

/// <summary>
/// Entity configuration for ScheduledPriceChange.
/// </summary>
public class ScheduledPriceChangeConfiguration : IEntityTypeConfiguration<ScheduledPriceChange>
{
    public void Configure(EntityTypeBuilder<ScheduledPriceChange> builder)
    {
        builder.ToTable("ScheduledPriceChanges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OldPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.NewPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.NewCostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(PriceChangeStatus.Scheduled);

        builder.Property(e => e.Reason)
            .HasMaxLength(200);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.ScheduledPriceChanges)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to PricingZone (optional)
        builder.HasOne(e => e.PricingZone)
            .WithMany(pz => pz.ScheduledPriceChanges)
            .HasForeignKey(e => e.PricingZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to Store (optional)
        builder.HasOne(e => e.Store)
            .WithMany(s => s.ScheduledPriceChanges)
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on ProductId
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ScheduledPriceChanges_ProductId");

        // Index on Status for filtering pending changes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_ScheduledPriceChanges_Status");

        // Index on EffectiveDate for scheduling queries
        builder.HasIndex(e => e.EffectiveDate)
            .HasDatabaseName("IX_ScheduledPriceChanges_EffectiveDate");

        // Composite index for finding pending changes by date
        builder.HasIndex(e => new { e.Status, e.EffectiveDate })
            .HasDatabaseName("IX_ScheduledPriceChanges_Status_EffectiveDate");
    }
}
