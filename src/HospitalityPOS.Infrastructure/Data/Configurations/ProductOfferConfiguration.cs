using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ProductOffer entity.
/// </summary>
public class ProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
{
    public void Configure(EntityTypeBuilder<ProductOffer> builder)
    {
        builder.ToTable("ProductOffers");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.OfferName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.Description)
            .HasMaxLength(500);

        builder.Property(po => po.PricingType)
            .IsRequired()
            .HasDefaultValue(OfferPricingType.FixedPrice);

        builder.Property(po => po.OfferPrice)
            .HasPrecision(18, 4);

        builder.Property(po => po.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(po => po.StartDate)
            .IsRequired();

        builder.Property(po => po.EndDate)
            .IsRequired();

        builder.Property(po => po.MinQuantity)
            .HasDefaultValue(1);

        // Relationships
        builder.HasOne(po => po.Product)
            .WithMany(p => p.ProductOffers)
            .HasForeignKey(po => po.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(po => po.CreatedByUser)
            .WithMany()
            .HasForeignKey(po => po.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for quick offer lookups
        builder.HasIndex(po => po.ProductId)
            .HasDatabaseName("IX_ProductOffers_ProductId");

        builder.HasIndex(po => new { po.StartDate, po.EndDate })
            .HasDatabaseName("IX_ProductOffers_DateRange");

        builder.HasIndex(po => new { po.ProductId, po.StartDate, po.EndDate, po.IsActive })
            .HasDatabaseName("IX_ProductOffers_ActiveLookup");
    }
}
