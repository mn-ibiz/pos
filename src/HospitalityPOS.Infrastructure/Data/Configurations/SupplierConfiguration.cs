using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ContactPerson)
            .HasMaxLength(100);

        builder.Property(e => e.Phone)
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.City)
            .HasMaxLength(100);

        builder.Property(e => e.Country)
            .HasMaxLength(100);

        builder.Property(e => e.TaxId)
            .HasMaxLength(50);

        builder.Property(e => e.BankAccount)
            .HasMaxLength(50);

        builder.Property(e => e.BankName)
            .HasMaxLength(100);

        builder.Property(e => e.CreditLimit)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.CurrentBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PONumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(e => e.PONumber)
            .IsUnique();

        builder.Property(e => e.OrderDate)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PurchaseOrderStatus.Draft);

        builder.Property(e => e.SubTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Unpaid);

        builder.Property(e => e.AmountPaid)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.InvoiceNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.ReceivedQuantity)
            .HasPrecision(18, 3)
            .HasDefaultValue(0);

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TotalCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.PurchaseOrder)
            .WithMany(po => po.PurchaseOrderItems)
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.PurchaseOrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
    {
        builder.ToTable("SupplierInvoices");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.InvoiceDate)
            .IsRequired();

        builder.Property(e => e.DueDate)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.PaidAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InvoiceStatus.Unpaid);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.SupplierId, e.InvoiceNumber })
            .IsUnique();

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.SupplierInvoices)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PurchaseOrder)
            .WithMany(po => po.SupplierInvoices)
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PaymentDate)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(e => e.Reference)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(200);

        builder.HasOne(e => e.SupplierInvoice)
            .WithMany(si => si.SupplierPayments)
            .HasForeignKey(e => e.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.SupplierPayments)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
