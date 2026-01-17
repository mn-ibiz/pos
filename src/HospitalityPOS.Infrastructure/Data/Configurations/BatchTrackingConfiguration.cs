using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the ProductBatch entity.
/// </summary>
public class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("ProductBatches");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.InitialQuantity)
            .IsRequired();

        builder.Property(e => e.CurrentQuantity)
            .IsRequired();

        builder.Property(e => e.ReservedQuantity)
            .HasDefaultValue(0);

        builder.Property(e => e.SoldQuantity)
            .HasDefaultValue(0);

        builder.Property(e => e.DisposedQuantity)
            .HasDefaultValue(0);

        builder.Property(e => e.ReceivedAt)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Unique constraint on batch number per product per store
        builder.HasIndex(e => new { e.ProductId, e.StoreId, e.BatchNumber })
            .IsUnique();

        builder.HasIndex(e => e.ExpiryDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ReceivedAt);

        // Relationships
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Grn)
            .WithMany()
            .HasForeignKey(e => e.GrnId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(e => e.DaysUntilExpiry);
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.AvailableQuantity);
    }
}

/// <summary>
/// EF Core configuration for the ProductBatchConfiguration entity.
/// </summary>
public class ProductBatchConfigurationEntityConfiguration : IEntityTypeConfiguration<ProductBatchConfiguration>
{
    public void Configure(EntityTypeBuilder<ProductBatchConfiguration> builder)
    {
        builder.ToTable("ProductBatchConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequiresBatchTracking)
            .HasDefaultValue(false);

        builder.Property(e => e.RequiresExpiryDate)
            .HasDefaultValue(false);

        builder.Property(e => e.ExpiryWarningDays)
            .HasDefaultValue(30);

        builder.Property(e => e.ExpiryCriticalDays)
            .HasDefaultValue(7);

        builder.Property(e => e.ExpiredItemAction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpiryAction.Block);

        builder.Property(e => e.NearExpiryAction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpiryAction.Warn);

        builder.Property(e => e.UseFifo)
            .HasDefaultValue(true);

        builder.Property(e => e.UseFefo)
            .HasDefaultValue(true);

        builder.Property(e => e.TrackManufactureDate)
            .HasDefaultValue(false);

        builder.Property(e => e.MinimumShelfLifeDaysOnReceipt)
            .HasDefaultValue(0);

        // One configuration per product
        builder.HasIndex(e => e.ProductId)
            .IsUnique();

        // Relationship
        builder.HasOne(e => e.Product)
            .WithOne()
            .HasForeignKey<ProductBatchConfiguration>(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for the BatchStockMovement entity.
/// </summary>
public class BatchStockMovementConfiguration : IEntityTypeConfiguration<BatchStockMovement>
{
    public void Configure(EntityTypeBuilder<BatchStockMovement> builder)
    {
        builder.ToTable("BatchStockMovements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MovementType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.Property(e => e.QuantityBefore)
            .IsRequired();

        builder.Property(e => e.QuantityAfter)
            .IsRequired();

        builder.Property(e => e.ReferenceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(e => e.MovedAt)
            .IsRequired();

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.BatchId);
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.MovedAt);
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });

        // Relationships
        builder.HasOne(e => e.Batch)
            .WithMany()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed property
        builder.Ignore(e => e.TotalValue);
    }
}

/// <summary>
/// EF Core configuration for the BatchDisposal entity.
/// </summary>
public class BatchDisposalConfiguration : IEntityTypeConfiguration<BatchDisposal>
{
    public void Configure(EntityTypeBuilder<BatchDisposal> builder)
    {
        builder.ToTable("BatchDisposals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.DisposedAt)
            .IsRequired();

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.WitnessName)
            .HasMaxLength(100);

        builder.Property(e => e.PhotoPath)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.BatchId);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.DisposedAt);
        builder.HasIndex(e => e.Reason);

        // Relationships
        builder.HasOne(e => e.Batch)
            .WithMany()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed property
        builder.Ignore(e => e.TotalValue);
    }
}

/// <summary>
/// EF Core configuration for the ExpirySaleBlock entity.
/// </summary>
public class ExpirySaleBlockConfiguration : IEntityTypeConfiguration<ExpirySaleBlock>
{
    public void Configure(EntityTypeBuilder<ExpirySaleBlock> builder)
    {
        builder.ToTable("ExpirySaleBlocks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpiryDate)
            .IsRequired();

        builder.Property(e => e.DaysExpired)
            .IsRequired();

        builder.Property(e => e.AttemptedByUserId)
            .IsRequired();

        builder.Property(e => e.AttemptedAt)
            .IsRequired();

        builder.Property(e => e.AttemptedQuantity)
            .IsRequired();

        builder.Property(e => e.WasBlocked)
            .HasDefaultValue(true);

        builder.Property(e => e.OverrideApplied)
            .HasDefaultValue(false);

        builder.Property(e => e.OverrideReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.BatchId);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.AttemptedAt);
        builder.HasIndex(e => e.OverrideApplied);
        builder.HasIndex(e => new { e.StoreId, e.AttemptedAt });

        // Relationships
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Batch)
            .WithMany()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF Core configuration for the CategoryExpirySettings entity.
/// </summary>
public class CategoryExpirySettingsConfiguration : IEntityTypeConfiguration<CategoryExpirySettings>
{
    public void Configure(EntityTypeBuilder<CategoryExpirySettings> builder)
    {
        builder.ToTable("CategoryExpirySettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequiresExpiryTracking)
            .HasDefaultValue(false);

        builder.Property(e => e.BlockExpiredSales)
            .HasDefaultValue(true);

        builder.Property(e => e.AllowManagerOverride)
            .HasDefaultValue(true);

        builder.Property(e => e.WarningDays)
            .HasDefaultValue(30);

        builder.Property(e => e.CriticalDays)
            .HasDefaultValue(7);

        builder.Property(e => e.ExpiredItemAction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpiryAction.Block);

        builder.Property(e => e.NearExpiryAction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpiryAction.Warn);

        builder.Property(e => e.MinimumShelfLifeDaysOnReceipt)
            .HasDefaultValue(0);

        // One configuration per category
        builder.HasIndex(e => e.CategoryId)
            .IsUnique();

        // Relationship
        builder.HasOne(e => e.Category)
            .WithOne()
            .HasForeignKey<CategoryExpirySettings>(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for the BatchRecallAlert entity.
/// </summary>
public class BatchRecallAlertConfiguration : IEntityTypeConfiguration<BatchRecallAlert>
{
    public void Configure(EntityTypeBuilder<BatchRecallAlert> builder)
    {
        builder.ToTable("BatchRecallAlerts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.RecallReason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.IssuedAt)
            .IsRequired();

        builder.Property(e => e.ExternalReference)
            .HasMaxLength(100);

        builder.Property(e => e.SupplierContactInfo)
            .HasMaxLength(500);

        builder.Property(e => e.ResolutionNotes)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(e => e.BatchId);
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.BatchNumber);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.IssuedAt);

        // Relationships
        builder.HasOne(e => e.Batch)
            .WithMany()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF Core configuration for the RecallAction entity.
/// </summary>
public class RecallActionConfiguration : IEntityTypeConfiguration<RecallAction>
{
    public void Configure(EntityTypeBuilder<RecallAction> builder)
    {
        builder.ToTable("RecallActions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ActionDate)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.RecallAlertId);
        builder.HasIndex(e => e.ActionType);
        builder.HasIndex(e => e.ActionDate);

        // Relationships
        builder.HasOne(e => e.RecallAlert)
            .WithMany()
            .HasForeignKey(e => e.RecallAlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
