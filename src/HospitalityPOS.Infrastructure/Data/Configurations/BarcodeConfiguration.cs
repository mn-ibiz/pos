using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PLUCode entity.
/// </summary>
public class PLUCodeConfiguration : IEntityTypeConfiguration<PLUCode>
{
    public void Configure(EntityTypeBuilder<PLUCode> builder)
    {
        builder.ToTable("PLUCodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.TareWeight)
            .HasPrecision(18, 4);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Code)
            .IsUnique();
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for ProductBarcode entity.
/// </summary>
public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("ProductBarcodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.BarcodeType)
            .IsRequired();

        builder.Property(e => e.PackSize)
            .HasPrecision(18, 4);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Barcode)
            .IsUnique();
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => new { e.ProductId, e.IsPrimary });
    }
}

/// <summary>
/// EF Core configuration for WeightedBarcodeConfig entity.
/// </summary>
public class WeightedBarcodeConfigConfiguration : IEntityTypeConfiguration<WeightedBarcodeConfig>
{
    public void Configure(EntityTypeBuilder<WeightedBarcodeConfig> builder)
    {
        builder.ToTable("WeightedBarcodeConfigs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Prefix)
            .IsRequired();

        builder.Property(e => e.Format)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => e.Prefix);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for ScaleConfiguration entity.
/// </summary>
public class ScaleConfigurationConfiguration : IEntityTypeConfiguration<ScaleConfiguration>
{
    public void Configure(EntityTypeBuilder<ScaleConfiguration> builder)
    {
        builder.ToTable("ScaleConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ScaleType)
            .IsRequired();

        builder.Property(e => e.Protocol)
            .IsRequired();

        builder.Property(e => e.ConnectionString)
            .HasMaxLength(200);

        builder.Property(e => e.Parity)
            .HasMaxLength(20);

        builder.Property(e => e.MinWeight)
            .HasPrecision(18, 4);

        builder.Property(e => e.MaxWeight)
            .HasPrecision(18, 4);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for InternalBarcodeSequence entity.
/// </summary>
public class InternalBarcodeSequenceConfiguration : IEntityTypeConfiguration<InternalBarcodeSequence>
{
    public void Configure(EntityTypeBuilder<InternalBarcodeSequence> builder)
    {
        builder.ToTable("InternalBarcodeSequences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Prefix)
            .IsRequired()
            .HasMaxLength(10);
    }
}
