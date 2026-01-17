using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Printer entity.
/// </summary>
public class PrinterConfiguration : IEntityTypeConfiguration<Printer>
{
    public void Configure(EntityTypeBuilder<Printer> builder)
    {
        builder.ToTable("Printers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.ConnectionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.PortName)
            .HasMaxLength(50);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.UsbPath)
            .HasMaxLength(500);

        builder.Property(e => e.WindowsPrinterName)
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.LastError)
            .HasMaxLength(500);

        // Index for quick lookup by type and default status
        builder.HasIndex(e => new { e.Type, e.IsDefault })
            .HasDatabaseName("IX_Printers_Type_IsDefault");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Printers_IsActive");

        // One-to-one relationship with PrinterSettings
        builder.HasOne(e => e.Settings)
            .WithOne(s => s.Printer)
            .HasForeignKey<PrinterSettings>(s => s.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity Framework Core configuration for the PrinterSettings entity.
/// </summary>
public class PrinterSettingsConfiguration : IEntityTypeConfiguration<PrinterSettings>
{
    public void Configure(EntityTypeBuilder<PrinterSettings> builder)
    {
        builder.ToTable("PrinterSettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PrinterId)
            .IsRequired();

        builder.Property(e => e.CutFeedLines)
            .HasDefaultValue(3);

        builder.Property(e => e.LogoWidth)
            .HasDefaultValue(200);

        builder.Property(e => e.BeepCount)
            .HasDefaultValue(1);

        builder.Property(e => e.PrintDensity)
            .HasDefaultValue(7);

        builder.Property(e => e.LogoBitmap)
            .HasColumnType("varbinary(max)");

        // Index for quick lookup by printer
        builder.HasIndex(e => e.PrinterId)
            .IsUnique()
            .HasDatabaseName("IX_PrinterSettings_PrinterId");
    }
}

/// <summary>
/// Entity Framework Core configuration for the PrinterCategoryMapping entity.
/// </summary>
public class PrinterCategoryMappingConfiguration : IEntityTypeConfiguration<PrinterCategoryMapping>
{
    public void Configure(EntityTypeBuilder<PrinterCategoryMapping> builder)
    {
        builder.ToTable("PrinterCategoryMappings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PrinterId)
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .IsRequired();

        // Index for quick lookup by printer
        builder.HasIndex(e => e.PrinterId)
            .HasDatabaseName("IX_PrinterCategoryMappings_PrinterId");

        // Index for quick lookup by category
        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_PrinterCategoryMappings_CategoryId");

        // Unique constraint - one mapping per printer-category pair
        builder.HasIndex(e => new { e.PrinterId, e.CategoryId })
            .IsUnique()
            .HasDatabaseName("IX_PrinterCategoryMappings_PrinterId_CategoryId");

        // Relationships
        builder.HasOne(e => e.Printer)
            .WithMany(p => p.CategoryMappings)
            .HasForeignKey(e => e.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity Framework Core configuration for the KOTSettings entity.
/// </summary>
public class KOTSettingsConfiguration : IEntityTypeConfiguration<KOTSettings>
{
    public void Configure(EntityTypeBuilder<KOTSettings> builder)
    {
        builder.ToTable("KOTSettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PrinterId)
            .IsRequired();

        builder.Property(e => e.TitleFontSize)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.ItemFontSize)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.ModifierFontSize)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.BeepCount)
            .HasDefaultValue(2);

        builder.Property(e => e.CopiesPerOrder)
            .HasDefaultValue(1);

        // Index for quick lookup by printer
        builder.HasIndex(e => e.PrinterId)
            .IsUnique()
            .HasDatabaseName("IX_KOTSettings_PrinterId");

        // Relationship
        builder.HasOne(e => e.Printer)
            .WithOne(p => p.KOTSettings)
            .HasForeignKey<KOTSettings>(e => e.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity Framework Core configuration for the ReceiptTemplate entity.
/// </summary>
public class ReceiptTemplateConfiguration : IEntityTypeConfiguration<ReceiptTemplate>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplate> builder)
    {
        builder.ToTable("ReceiptTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.BusinessSubtitle)
            .HasMaxLength(200);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.Phone)
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.TaxPin)
            .HasMaxLength(50);

        builder.Property(e => e.FooterLine1)
            .HasMaxLength(200);

        builder.Property(e => e.FooterLine2)
            .HasMaxLength(200);

        builder.Property(e => e.FooterLine3)
            .HasMaxLength(200);

        builder.Property(e => e.QRCodeContent)
            .HasMaxLength(500);

        // Index for default template lookup
        builder.HasIndex(e => e.IsDefault)
            .HasDatabaseName("IX_ReceiptTemplates_IsDefault");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_ReceiptTemplates_IsActive");
    }
}
