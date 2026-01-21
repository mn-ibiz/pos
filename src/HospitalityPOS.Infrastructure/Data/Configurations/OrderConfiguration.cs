using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TableNumber)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(100);

        builder.Property(e => e.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OrderStatus.Open);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Ignore computed properties
        builder.Ignore(e => e.TotalOfferSavings);
        builder.Ignore(e => e.HasOffersApplied);
        builder.Ignore(e => e.OfferItemsCount);

        builder.HasIndex(e => e.OrderNumber)
            .IsUnique();

        builder.HasIndex(e => e.WorkPeriodId);

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.WorkPeriod)
            .WithMany(w => w.Orders)
            .HasForeignKey(e => e.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Modifiers)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(200);

        // Offer tracking properties
        builder.Property(e => e.OriginalUnitPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.AppliedOfferName)
            .HasMaxLength(100);

        // Ignore computed properties
        builder.Ignore(e => e.SavingsAmount);
        builder.Ignore(e => e.HasOfferApplied);

        // Indexes for order item queries
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => new { e.OrderId, e.ProductId });

        builder.HasOne(e => e.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AppliedOffer)
            .WithMany()
            .HasForeignKey(e => e.AppliedOfferId)
            .OnDelete(DeleteBehavior.SetNull);

        // Product Variant relationship
        builder.Property(e => e.VariantText)
            .HasMaxLength(200);

        builder.HasOne(e => e.ProductVariant)
            .WithMany(pv => pv.OrderItems)
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ProductVariantId);
    }
}
