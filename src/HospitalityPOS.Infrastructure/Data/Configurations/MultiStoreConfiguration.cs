using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Store.
/// </summary>
public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StoreCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasMaxLength(200);

        builder.Property(e => e.City)
            .HasMaxLength(50);

        builder.Property(e => e.Region)
            .HasMaxLength(50);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.TaxRegistrationNumber)
            .HasMaxLength(50);

        builder.Property(e => e.EtimsDeviceSerial)
            .HasMaxLength(50);

        builder.Property(e => e.IsHeadquarters)
            .HasDefaultValue(false);

        builder.Property(e => e.ReceivesCentralUpdates)
            .HasDefaultValue(true);

        builder.Property(e => e.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("Africa/Nairobi");

        builder.Property(e => e.ManagerName)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Unique index on StoreCode
        builder.HasIndex(e => e.StoreCode)
            .IsUnique()
            .HasDatabaseName("IX_Stores_StoreCode");

        // Index on IsHeadquarters for quick HQ lookup
        builder.HasIndex(e => e.IsHeadquarters)
            .HasDatabaseName("IX_Stores_IsHeadquarters");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Stores_IsActive");

        // Index on City for regional queries
        builder.HasIndex(e => e.City)
            .HasDatabaseName("IX_Stores_City");

        // Navigation to product overrides
        builder.HasMany(e => e.ProductOverrides)
            .WithOne(o => o.Store)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity configuration for StoreProductOverride.
/// </summary>
public class StoreProductOverrideConfiguration : IEntityTypeConfiguration<StoreProductOverride>
{
    public void Configure(EntityTypeBuilder<StoreProductOverride> builder)
    {
        builder.ToTable("StoreProductOverrides");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OverridePrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.OverrideCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(e => e.OverrideMinStock)
            .HasPrecision(18, 4);

        builder.Property(e => e.OverrideMaxStock)
            .HasPrecision(18, 4);

        builder.Property(e => e.OverrideTaxRate)
            .HasPrecision(5, 2);

        builder.Property(e => e.OverrideKitchenStation)
            .HasMaxLength(50);

        builder.Property(e => e.OverrideReason)
            .HasMaxLength(200);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany(s => s.ProductOverrides)
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.StoreOverrides)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique composite index on StoreId + ProductId
        builder.HasIndex(e => new { e.StoreId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_StoreProductOverrides_Store_Product");

        // Index on StoreId for store-specific queries
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_StoreProductOverrides_StoreId");

        // Index on ProductId for product-specific queries
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_StoreProductOverrides_ProductId");

        // Index on IsAvailable for filtering
        builder.HasIndex(e => e.IsAvailable)
            .HasDatabaseName("IX_StoreProductOverrides_IsAvailable");
    }
}
