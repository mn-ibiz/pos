using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class VariantOptionConfiguration : IEntityTypeConfiguration<VariantOption>
{
    public void Configure(EntityTypeBuilder<VariantOption> builder)
    {
        builder.ToTable("VariantOptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.IsGlobal)
            .HasDefaultValue(true);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.OptionType);
    }
}

public class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        builder.ToTable("VariantOptionValues");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);

        builder.Property(e => e.PriceAdjustment)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.SkuSuffix)
            .HasMaxLength(20);

        builder.HasOne(e => e.VariantOption)
            .WithMany(vo => vo.Values)
            .HasForeignKey(e => e.VariantOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.VariantOptionId);
    }
}

public class ProductVariantOptionConfiguration : IEntityTypeConfiguration<ProductVariantOption>
{
    public void Configure(EntityTypeBuilder<ProductVariantOption> builder)
    {
        builder.ToTable("ProductVariantOptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsRequired)
            .HasDefaultValue(true);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.VariantOptions)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.VariantOption)
            .WithMany(vo => vo.ProductVariantOptions)
            .HasForeignKey(e => e.VariantOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ProductId, e.VariantOptionId })
            .IsUnique();
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Barcode)
            .HasMaxLength(50);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(300);

        builder.Property(e => e.SellingPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Weight)
            .HasPrecision(18, 4);

        builder.Property(e => e.WeightUnit)
            .HasMaxLength(10);

        builder.Property(e => e.Dimensions)
            .HasMaxLength(100);

        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);

        builder.Property(e => e.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(e => e.TrackInventory)
            .HasDefaultValue(true);

        // Computed properties
        builder.Ignore(e => e.EffectivePrice);
        builder.Ignore(e => e.IsLowStock);
        builder.Ignore(e => e.IsOutOfStock);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SKU)
            .IsUnique();

        builder.HasIndex(e => e.Barcode);
        builder.HasIndex(e => e.ProductId);
    }
}

public class ProductVariantValueConfiguration : IEntityTypeConfiguration<ProductVariantValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantValue> builder)
    {
        builder.ToTable("ProductVariantValues");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.ProductVariant)
            .WithMany(pv => pv.VariantValues)
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.VariantOptionValue)
            .WithMany(vov => vov.ProductVariantValues)
            .HasForeignKey(e => e.VariantOptionValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProductVariantId, e.VariantOptionValueId })
            .IsUnique();
    }
}

