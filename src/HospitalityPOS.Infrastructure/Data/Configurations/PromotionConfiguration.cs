using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for CentralPromotion.
/// </summary>
public class CentralPromotionConfiguration : IEntityTypeConfiguration<CentralPromotion>
{
    public void Configure(EntityTypeBuilder<CentralPromotion> builder)
    {
        builder.ToTable("CentralPromotions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PromotionCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.InternalNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.Type)
            .HasConversion<int>();

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.OfferPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.MinimumPurchase)
            .HasPrecision(18, 2);

        builder.Property(e => e.CouponCode)
            .HasMaxLength(50);

        builder.Property(e => e.ValidDaysOfWeek)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(PromotionStatus.Draft);

        // Unique index on PromotionCode
        builder.HasIndex(e => e.PromotionCode)
            .IsUnique()
            .HasDatabaseName("IX_CentralPromotions_PromotionCode");

        // Index on Status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_CentralPromotions_Status");

        // Index on dates for active promotion queries
        builder.HasIndex(e => new { e.StartDate, e.EndDate })
            .HasDatabaseName("IX_CentralPromotions_DateRange");

        // Composite index for active promotions
        builder.HasIndex(e => new { e.Status, e.StartDate, e.EndDate, e.IsActive })
            .HasDatabaseName("IX_CentralPromotions_Active");

        // Index on CouponCode for coupon lookups
        builder.HasIndex(e => e.CouponCode)
            .HasDatabaseName("IX_CentralPromotions_CouponCode");

        // Navigation to products
        builder.HasMany(e => e.Products)
            .WithOne(p => p.Promotion)
            .HasForeignKey(p => p.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to categories
        builder.HasMany(e => e.Categories)
            .WithOne(c => c.Promotion)
            .HasForeignKey(c => c.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to deployments
        builder.HasMany(e => e.Deployments)
            .WithOne(d => d.Promotion)
            .HasForeignKey(d => d.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to redemptions
        builder.HasMany(e => e.Redemptions)
            .WithOne(r => r.Promotion)
            .HasForeignKey(r => r.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Computed columns ignored
        builder.Ignore(e => e.IsCurrentlyActive);
        builder.Ignore(e => e.ComputedStatus);
    }
}

/// <summary>
/// Entity configuration for PromotionProduct.
/// </summary>
public class PromotionProductConfiguration : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("PromotionProducts");

        builder.HasKey(e => e.Id);

        // Foreign key to Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Products)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique composite index
        builder.HasIndex(e => new { e.PromotionId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionProducts_Promotion_Product");
    }
}

/// <summary>
/// Entity configuration for PromotionCategory.
/// </summary>
public class PromotionCategoryConfiguration : IEntityTypeConfiguration<PromotionCategory>
{
    public void Configure(EntityTypeBuilder<PromotionCategory> builder)
    {
        builder.ToTable("PromotionCategories");

        builder.HasKey(e => e.Id);

        // Foreign key to Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Categories)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Category
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique composite index
        builder.HasIndex(e => new { e.PromotionId, e.CategoryId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionCategories_Promotion_Category");
    }
}

/// <summary>
/// Entity configuration for PromotionDeployment.
/// </summary>
public class PromotionDeploymentConfiguration : IEntityTypeConfiguration<PromotionDeployment>
{
    public void Configure(EntityTypeBuilder<PromotionDeployment> builder)
    {
        builder.ToTable("PromotionDeployments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Scope)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(DeploymentStatus.Pending);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Foreign key to Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Deployments)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to zones
        builder.HasMany(e => e.Zones)
            .WithOne(z => z.Deployment)
            .HasForeignKey(z => z.DeploymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to stores
        builder.HasMany(e => e.Stores)
            .WithOne(s => s.Deployment)
            .HasForeignKey(s => s.DeploymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on PromotionId
        builder.HasIndex(e => e.PromotionId)
            .HasDatabaseName("IX_PromotionDeployments_PromotionId");

        // Index on Status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_PromotionDeployments_Status");

        // Index on DeployedAt
        builder.HasIndex(e => e.DeployedAt)
            .HasDatabaseName("IX_PromotionDeployments_DeployedAt");
    }
}

/// <summary>
/// Entity configuration for DeploymentZone.
/// </summary>
public class DeploymentZoneConfiguration : IEntityTypeConfiguration<DeploymentZone>
{
    public void Configure(EntityTypeBuilder<DeploymentZone> builder)
    {
        builder.ToTable("DeploymentZones");

        builder.HasKey(e => e.Id);

        // Foreign key to Deployment
        builder.HasOne(e => e.Deployment)
            .WithMany(d => d.Zones)
            .HasForeignKey(e => e.DeploymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to PricingZone
        builder.HasOne(e => e.PricingZone)
            .WithMany()
            .HasForeignKey(e => e.PricingZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique composite index
        builder.HasIndex(e => new { e.DeploymentId, e.PricingZoneId })
            .IsUnique()
            .HasDatabaseName("IX_DeploymentZones_Deployment_Zone");
    }
}

/// <summary>
/// Entity configuration for DeploymentStore.
/// </summary>
public class DeploymentStoreConfiguration : IEntityTypeConfiguration<DeploymentStore>
{
    public void Configure(EntityTypeBuilder<DeploymentStore> builder)
    {
        builder.ToTable("DeploymentStores");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(DeploymentStatus.Pending);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        // Foreign key to Deployment
        builder.HasOne(e => e.Deployment)
            .WithMany(d => d.Stores)
            .HasForeignKey(e => e.DeploymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique composite index
        builder.HasIndex(e => new { e.DeploymentId, e.StoreId })
            .IsUnique()
            .HasDatabaseName("IX_DeploymentStores_Deployment_Store");

        // Index on Status for filtering
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_DeploymentStores_Status");
    }
}

/// <summary>
/// Entity configuration for PromotionRedemption.
/// </summary>
public class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionRedemption> builder)
    {
        builder.ToTable("PromotionRedemptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OriginalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.DiscountGiven)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.FinalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CouponCodeUsed)
            .HasMaxLength(50);

        builder.Property(e => e.VoidReason)
            .HasMaxLength(200);

        // Foreign key to Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Redemptions)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Receipt
        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ReceiptItem (optional)
        builder.HasOne(e => e.ReceiptItem)
            .WithMany()
            .HasForeignKey(e => e.ReceiptItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to LoyaltyMember (optional)
        builder.HasOne(e => e.LoyaltyMember)
            .WithMany()
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on PromotionId for promotion-specific queries
        builder.HasIndex(e => e.PromotionId)
            .HasDatabaseName("IX_PromotionRedemptions_PromotionId");

        // Index on StoreId for store-specific queries
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_PromotionRedemptions_StoreId");

        // Index on RedeemedAt for date range queries
        builder.HasIndex(e => e.RedeemedAt)
            .HasDatabaseName("IX_PromotionRedemptions_RedeemedAt");

        // Composite index for promotion redemption by store
        builder.HasIndex(e => new { e.PromotionId, e.StoreId, e.RedeemedAt })
            .HasDatabaseName("IX_PromotionRedemptions_Promotion_Store_Date");

        // Index for non-voided redemptions
        builder.HasIndex(e => new { e.PromotionId, e.IsVoided })
            .HasDatabaseName("IX_PromotionRedemptions_Promotion_NotVoided");
    }
}
