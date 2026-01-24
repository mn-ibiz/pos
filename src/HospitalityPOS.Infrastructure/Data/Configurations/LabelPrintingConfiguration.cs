using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for LabelSize entity.
/// </summary>
public class LabelSizeConfiguration : IEntityTypeConfiguration<LabelSize>
{
    public void Configure(EntityTypeBuilder<LabelSize> builder)
    {
        builder.ToTable("LabelSizes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.WidthMm)
            .HasPrecision(10, 2);

        builder.Property(e => e.HeightMm)
            .HasPrecision(10, 2);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelPrinter entity.
/// </summary>
public class LabelPrinterConfiguration : IEntityTypeConfiguration<LabelPrinter>
{
    public void Configure(EntityTypeBuilder<LabelPrinter> builder)
    {
        builder.ToTable("LabelPrinters");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ConnectionString)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PrinterType)
            .IsRequired();

        builder.Property(e => e.PrintLanguage)
            .IsRequired();

        builder.Property(e => e.LastErrorMessage)
            .HasMaxLength(500);

        builder.HasOne(e => e.DefaultLabelSize)
            .WithMany()
            .HasForeignKey(e => e.DefaultLabelSizeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.IsDefault);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelTemplate entity.
/// </summary>
public class LabelTemplateConfiguration : IEntityTypeConfiguration<LabelTemplate>
{
    public void Configure(EntityTypeBuilder<LabelTemplate> builder)
    {
        builder.ToTable("LabelTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.TemplateContent)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.PrintLanguage)
            .IsRequired();

        builder.HasOne(e => e.LabelSize)
            .WithMany(s => s.Templates)
            .HasForeignKey(e => e.LabelSizeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.LabelSizeId);
        builder.HasIndex(e => e.IsDefault);
        builder.HasIndex(e => e.IsPromoTemplate);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelTemplateField entity.
/// </summary>
public class LabelTemplateFieldConfiguration : IEntityTypeConfiguration<LabelTemplateField>
{
    public void Configure(EntityTypeBuilder<LabelTemplateField> builder)
    {
        builder.ToTable("LabelTemplateFields");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FieldName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FontName)
            .HasMaxLength(50);

        builder.Property(e => e.FieldType)
            .IsRequired();

        builder.HasOne(e => e.Template)
            .WithMany(t => t.Fields)
            .HasForeignKey(e => e.LabelTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.LabelTemplateId);
        builder.HasIndex(e => e.DisplayOrder);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for CategoryPrinterAssignment entity.
/// </summary>
public class CategoryPrinterAssignmentConfiguration : IEntityTypeConfiguration<CategoryPrinterAssignment>
{
    public void Configure(EntityTypeBuilder<CategoryPrinterAssignment> builder)
    {
        builder.ToTable("CategoryPrinterAssignments");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LabelPrinter)
            .WithMany(p => p.CategoryAssignments)
            .HasForeignKey(e => e.LabelPrinterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LabelTemplate)
            .WithMany(t => t.CategoryAssignments)
            .HasForeignKey(e => e.LabelTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.CategoryId, e.StoreId })
            .IsUnique();
        builder.HasIndex(e => e.LabelPrinterId);
        builder.HasIndex(e => e.LabelTemplateId);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelPrintJob entity.
/// </summary>
public class LabelPrintJobConfiguration : IEntityTypeConfiguration<LabelPrintJob>
{
    public void Configure(EntityTypeBuilder<LabelPrintJob> builder)
    {
        builder.ToTable("LabelPrintJobs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobType)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Printer)
            .WithMany(p => p.PrintJobs)
            .HasForeignKey(e => e.PrinterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Template)
            .WithMany()
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.PrinterId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.InitiatedByUserId);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelPrintJobItem entity.
/// </summary>
public class LabelPrintJobItemConfiguration : IEntityTypeConfiguration<LabelPrintJobItem>
{
    public void Configure(EntityTypeBuilder<LabelPrintJobItem> builder)
    {
        builder.ToTable("LabelPrintJobItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Barcode)
            .HasMaxLength(50);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.OriginalPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.HasOne(e => e.Job)
            .WithMany(j => j.Items)
            .HasForeignKey(e => e.LabelPrintJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.LabelPrintJobId);
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for LabelTemplateLibrary entity.
/// </summary>
public class LabelTemplateLibraryConfiguration : IEntityTypeConfiguration<LabelTemplateLibrary>
{
    public void Configure(EntityTypeBuilder<LabelTemplateLibrary> builder)
    {
        builder.ToTable("LabelTemplateLibraries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.TemplateContent)
            .IsRequired();

        builder.Property(e => e.WidthMm)
            .HasPrecision(10, 2);

        builder.Property(e => e.HeightMm)
            .HasPrecision(10, 2);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.PrintLanguage)
            .IsRequired();

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.IsBuiltIn);
        builder.HasIndex(e => e.IsActive);
    }
}
