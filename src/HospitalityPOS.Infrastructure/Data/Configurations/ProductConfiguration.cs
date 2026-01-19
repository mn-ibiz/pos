using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);

        builder.HasOne(e => e.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(e => e.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.SellingPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(16.00m);

        builder.Property(e => e.UnitOfMeasure)
            .HasMaxLength(20)
            .HasDefaultValue("Each");

        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);

        builder.Property(e => e.Barcode)
            .HasMaxLength(50);

        builder.Property(e => e.MinStockLevel)
            .HasPrecision(18, 3);

        builder.Property(e => e.MaxStockLevel)
            .HasPrecision(18, 3);

        builder.Property(e => e.TrackInventory)
            .HasDefaultValue(true);

        // Computed properties are not mapped to database
        builder.Ignore(e => e.IsLowStock);
        builder.Ignore(e => e.IsOutOfStock);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.Barcode);

        builder.HasIndex(e => e.CategoryId);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductFavoriteConfiguration : IEntityTypeConfiguration<ProductFavorite>
{
    public void Configure(EntityTypeBuilder<ProductFavorite> builder)
    {
        builder.ToTable("ProductFavorites");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DisplayOrder)
            .HasDefaultValue(0);

        // Unique constraint: each user can only favorite a product once
        builder.HasIndex(e => new { e.UserId, e.ProductId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
