using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("ModifierGroups");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.Property(e => e.IconPath)
            .HasMaxLength(500);

        builder.Property(e => e.KitchenStation)
            .HasMaxLength(50);

        builder.Property(e => e.PrintOnKOT)
            .HasDefaultValue(true);

        builder.Property(e => e.ShowOnReceipt)
            .HasDefaultValue(true);

        builder.HasIndex(e => e.Name);
    }
}

public class ModifierItemConfiguration : IEntityTypeConfiguration<ModifierItem>
{
    public void Configure(EntityTypeBuilder<ModifierItem> builder)
    {
        builder.ToTable("ModifierItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.ShortCode)
            .HasMaxLength(10);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxQuantity)
            .HasDefaultValue(10);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);

        builder.Property(e => e.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(e => e.KOTText)
            .HasMaxLength(100);

        builder.Property(e => e.TaxRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(16.00m);

        builder.Property(e => e.InventoryDeductQuantity)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.Allergens)
            .HasMaxLength(500);

        builder.HasOne(e => e.ModifierGroup)
            .WithMany(mg => mg.Items)
            .HasForeignKey(e => e.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InventoryProduct)
            .WithMany()
            .HasForeignKey(e => e.InventoryProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.ModifierGroupId);
        builder.HasIndex(e => e.ShortCode);
    }
}

public class ModifierItemNestedGroupConfiguration : IEntityTypeConfiguration<ModifierItemNestedGroup>
{
    public void Configure(EntityTypeBuilder<ModifierItemNestedGroup> builder)
    {
        builder.ToTable("ModifierItemNestedGroups");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.ModifierItem)
            .WithMany(mi => mi.NestedGroups)
            .HasForeignKey(e => e.ModifierItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.NestedModifierGroup)
            .WithMany()
            .HasForeignKey(e => e.NestedModifierGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ModifierItemId, e.NestedModifierGroupId })
            .IsUnique();
    }
}

public class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("ProductModifierGroups");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.ModifierGroups)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ModifierGroup)
            .WithMany(mg => mg.ProductModifierGroups)
            .HasForeignKey(e => e.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ProductId, e.ModifierGroupId })
            .IsUnique();
    }
}

public class CategoryModifierGroupConfiguration : IEntityTypeConfiguration<CategoryModifierGroup>
{
    public void Configure(EntityTypeBuilder<CategoryModifierGroup> builder)
    {
        builder.ToTable("CategoryModifierGroups");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.InheritToProducts)
            .HasDefaultValue(true);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.DefaultModifierGroups)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ModifierGroup)
            .WithMany(mg => mg.CategoryModifierGroups)
            .HasForeignKey(e => e.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CategoryId, e.ModifierGroupId })
            .IsUnique();
    }
}

public class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("OrderItemModifiers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .HasDefaultValue(1);

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.OrderItem)
            .WithMany(oi => oi.OrderItemModifiers)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ModifierItem)
            .WithMany()
            .HasForeignKey(e => e.ModifierItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.OrderItemId);
    }
}

public class ModifierPresetConfiguration : IEntityTypeConfiguration<ModifierPreset>
{
    public void Configure(EntityTypeBuilder<ModifierPreset> builder)
    {
        builder.ToTable("ModifierPresets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.ModifierPresets)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.ModifierPresets)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.CategoryId);
    }
}

public class ModifierPresetItemConfiguration : IEntityTypeConfiguration<ModifierPresetItem>
{
    public void Configure(EntityTypeBuilder<ModifierPresetItem> builder)
    {
        builder.ToTable("ModifierPresetItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .HasDefaultValue(1);

        builder.HasOne(e => e.ModifierPreset)
            .WithMany(mp => mp.PresetItems)
            .HasForeignKey(e => e.ModifierPresetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ModifierItem)
            .WithMany()
            .HasForeignKey(e => e.ModifierItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ModifierPresetId);
    }
}
