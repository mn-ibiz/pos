using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EtimsDevice entity.
/// </summary>
public class EtimsDeviceConfiguration : IEntityTypeConfiguration<EtimsDevice>
{
    public void Configure(EntityTypeBuilder<EtimsDevice> builder)
    {
        builder.ToTable("EtimsDevices");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DeviceSerialNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ControlUnitId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.BusinessPin)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.BranchCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.BranchName)
            .HasMaxLength(100);

        builder.Property(e => e.ApiBaseUrl)
            .HasMaxLength(255);

        builder.Property(e => e.ApiKey)
            .HasMaxLength(500);

        builder.Property(e => e.ApiSecret)
            .HasMaxLength(500);

        builder.Property(e => e.Environment)
            .HasMaxLength(20)
            .HasDefaultValue("Sandbox");

        builder.HasIndex(e => e.DeviceSerialNumber).IsUnique();
        builder.HasIndex(e => e.ControlUnitId).IsUnique();
        builder.HasIndex(e => e.IsPrimary);
    }
}

/// <summary>
/// EF Core configuration for EtimsInvoice entity.
/// </summary>
public class EtimsInvoiceConfiguration : IEntityTypeConfiguration<EtimsInvoice>
{
    public void Configure(EntityTypeBuilder<EtimsInvoice> builder)
    {
        builder.ToTable("EtimsInvoices");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.InternalReceiptNumber)
            .HasMaxLength(50);

        builder.Property(e => e.CustomerPin)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200);

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(e => e.TaxableAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.StandardRatedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.ZeroRatedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.ExemptAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.ReceiptSignature)
            .HasMaxLength(500);

        builder.Property(e => e.QrCode)
            .HasMaxLength(500);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Device)
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.EtimsInvoice)
            .HasForeignKey(i => i.EtimsInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.InvoiceNumber).IsUnique();
        builder.HasIndex(e => e.ReceiptId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.InvoiceDate);
    }
}

/// <summary>
/// EF Core configuration for EtimsInvoiceItem entity.
/// </summary>
public class EtimsInvoiceItemConfiguration : IEntityTypeConfiguration<EtimsInvoiceItem>
{
    public void Configure(EntityTypeBuilder<EtimsInvoiceItem> builder)
    {
        builder.ToTable("EtimsInvoiceItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ItemCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ItemDescription)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.HsCode)
            .HasMaxLength(20);

        builder.Property(e => e.UnitOfMeasure)
            .HasMaxLength(10);

        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(18,4)");

        builder.Property(e => e.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.TaxableAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(e => e.EtimsInvoiceId);
    }
}

/// <summary>
/// EF Core configuration for EtimsCreditNote entity.
/// </summary>
public class EtimsCreditNoteConfiguration : IEntityTypeConfiguration<EtimsCreditNote>
{
    public void Configure(EntityTypeBuilder<EtimsCreditNote> builder)
    {
        builder.ToTable("EtimsCreditNotes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreditNoteNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OriginalInvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.CustomerPin)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200);

        builder.Property(e => e.CreditAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.KraSignature)
            .HasMaxLength(500);

        builder.HasOne(e => e.OriginalInvoice)
            .WithMany()
            .HasForeignKey(e => e.OriginalInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Device)
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.EtimsCreditNote)
            .HasForeignKey(i => i.EtimsCreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CreditNoteNumber).IsUnique();
        builder.HasIndex(e => e.OriginalInvoiceId);
        builder.HasIndex(e => e.Status);
    }
}

/// <summary>
/// EF Core configuration for EtimsCreditNoteItem entity.
/// </summary>
public class EtimsCreditNoteItemConfiguration : IEntityTypeConfiguration<EtimsCreditNoteItem>
{
    public void Configure(EntityTypeBuilder<EtimsCreditNoteItem> builder)
    {
        builder.ToTable("EtimsCreditNoteItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ItemCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ItemDescription)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(18,4)");

        builder.Property(e => e.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.TaxableAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(e => e.EtimsCreditNoteId);
    }
}

/// <summary>
/// EF Core configuration for EtimsQueueEntry entity.
/// </summary>
public class EtimsQueueEntryConfiguration : IEntityTypeConfiguration<EtimsQueueEntry>
{
    public void Configure(EntityTypeBuilder<EtimsQueueEntry> builder)
    {
        builder.ToTable("EtimsQueue");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LastError)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.QueuedAt);
        builder.HasIndex(e => e.RetryAfter);
    }
}

/// <summary>
/// EF Core configuration for EtimsSyncLog entity.
/// </summary>
public class EtimsSyncLogConfiguration : IEntityTypeConfiguration<EtimsSyncLog>
{
    public void Configure(EntityTypeBuilder<EtimsSyncLog> builder)
    {
        builder.ToTable("EtimsSyncLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OperationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.IsSuccess);
    }
}
