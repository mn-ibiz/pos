using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SupplierContact entity.
/// </summary>
public class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> builder)
    {
        builder.ToTable("SupplierContacts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(200);

        builder.Property(e => e.Phone)
            .HasMaxLength(50);

        builder.Property(e => e.Mobile)
            .HasMaxLength(50);

        builder.Property(e => e.Position)
            .HasMaxLength(100);

        builder.Property(e => e.Department)
            .HasMaxLength(100);

        builder.Property(e => e.PreferredContactMethod)
            .HasMaxLength(50);

        builder.Property(e => e.PreferredLanguage)
            .HasMaxLength(20);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.Contacts)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SupplierId);
        builder.HasIndex(e => e.Email);
        builder.HasIndex(e => new { e.SupplierId, e.IsPrimaryContact });
    }
}

/// <summary>
/// EF Core configuration for POEmailLog entity.
/// </summary>
public class POEmailLogConfiguration : IEntityTypeConfiguration<POEmailLog>
{
    public void Configure(EntityTypeBuilder<POEmailLog> builder)
    {
        builder.ToTable("POEmailLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Recipients)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.CcRecipients)
            .HasMaxLength(1000);

        builder.Property(e => e.BccRecipients)
            .HasMaxLength(1000);

        builder.Property(e => e.Subject)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Body)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AttachmentNames)
            .HasMaxLength(500);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.MessageId)
            .HasMaxLength(200);

        builder.Property(e => e.CustomMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.OpenedFromIp)
            .HasMaxLength(50);

        builder.HasOne(e => e.PurchaseOrder)
            .WithMany(po => po.EmailLogs)
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SentByUser)
            .WithMany()
            .HasForeignKey(e => e.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PurchaseOrderId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.QueuedAt);
        builder.HasIndex(e => new { e.PurchaseOrderId, e.EmailType });
    }
}
