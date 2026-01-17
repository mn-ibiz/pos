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

public class ProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
{
    public void Configure(EntityTypeBuilder<ProductOffer> builder)
    {
        builder.ToTable("ProductOffers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OfferName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.OfferPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate)
            .IsRequired();

        builder.HasIndex(e => new { e.ProductId, e.StartDate, e.EndDate })
            .HasFilter("[IsActive] = 1");

        builder.HasOne(e => e.Product)
            .WithMany(p => p.ProductOffers)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map CreatedByUser navigation to BaseEntity.CreatedByUserId
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
