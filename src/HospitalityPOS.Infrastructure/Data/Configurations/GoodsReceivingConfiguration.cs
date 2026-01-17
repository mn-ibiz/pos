using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the GoodsReceivedNote entity.
/// </summary>
public class GoodsReceivedNoteConfiguration : IEntityTypeConfiguration<GoodsReceivedNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceivedNote> builder)
    {
        builder.ToTable("GoodsReceivedNotes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.GRNNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.ReceivedDate)
            .IsRequired();

        builder.Property(e => e.DeliveryNote)
            .HasMaxLength(100);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => e.GRNNumber)
            .IsUnique();

        builder.HasIndex(e => e.ReceivedDate);

        builder.HasOne(e => e.PurchaseOrder)
            .WithMany(po => po.GoodsReceivedNotes)
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.GoodsReceivedNotes)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReceivedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF Core configuration for the GRNItem entity.
/// </summary>
public class GRNItemConfiguration : IEntityTypeConfiguration<GRNItem>
{
    public void Configure(EntityTypeBuilder<GRNItem> builder)
    {
        builder.ToTable("GRNItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderedQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.ReceivedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TotalCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.GoodsReceivedNote)
            .WithMany(grn => grn.Items)
            .HasForeignKey(e => e.GoodsReceivedNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PurchaseOrderItem)
            .WithMany(poi => poi.GRNItems)
            .HasForeignKey(e => e.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.GRNItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
