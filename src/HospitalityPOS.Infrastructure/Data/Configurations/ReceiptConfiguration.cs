using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TableNumber)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ReceiptStatus.Pending);

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

        builder.Property(e => e.PaidAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.ChangeAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.VoidReason)
            .HasMaxLength(200);

        // Loyalty program fields
        builder.Property(e => e.PointsEarned)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.PointsRedeemed)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.PointsDiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.PointsBalanceAfter)
            .HasPrecision(18, 2);

        builder.HasIndex(e => e.ReceiptNumber)
            .IsUnique();

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Order)
            .WithMany(o => o.Receipts)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WorkPeriod)
            .WithMany(w => w.Receipts)
            .HasForeignKey(e => e.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Owner)
            .WithMany(u => u.OwnedReceipts)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VoidedByUser)
            .WithMany(u => u.VoidedReceipts)
            .HasForeignKey(e => e.VoidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SettledByUser)
            .WithMany(u => u.SettledReceipts)
            .HasForeignKey(e => e.SettledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentReceipt)
            .WithMany(r => r.SplitReceipts)
            .HasForeignKey(e => e.ParentReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.MergedIntoReceipt)
            .WithMany(r => r.MergedFromReceipts)
            .HasForeignKey(e => e.MergedIntoReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        // Loyalty member relationship
        builder.HasOne(e => e.LoyaltyMember)
            .WithMany(m => m.Receipts)
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index for loyalty member receipts lookup
        builder.HasIndex(e => e.LoyaltyMemberId)
            .HasDatabaseName("IX_Receipts_LoyaltyMemberId");
    }
}

public class ReceiptItemConfiguration : IEntityTypeConfiguration<ReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReceiptItem> builder)
    {
        builder.ToTable("ReceiptItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Modifiers)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.Receipt)
            .WithMany(r => r.ReceiptItems)
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.OrderItem)
            .WithMany()
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VoidReasonConfiguration : IEntityTypeConfiguration<VoidReason>
{
    public void Configure(EntityTypeBuilder<VoidReason> builder)
    {
        builder.ToTable("VoidReasons");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.RequiresNote)
            .HasDefaultValue(false);

        builder.Property(e => e.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.HasIndex(e => e.DisplayOrder);

        // Seed default void reasons
        builder.HasData(
            new VoidReason { Id = 1, Name = "Customer complaint", DisplayOrder = 1, IsActive = true },
            new VoidReason { Id = 2, Name = "Wrong order", DisplayOrder = 2, IsActive = true },
            new VoidReason { Id = 3, Name = "Item unavailable", DisplayOrder = 3, IsActive = true },
            new VoidReason { Id = 4, Name = "Duplicate transaction", DisplayOrder = 4, IsActive = true },
            new VoidReason { Id = 5, Name = "Test transaction", DisplayOrder = 5, IsActive = true },
            new VoidReason { Id = 6, Name = "System error", DisplayOrder = 6, IsActive = true },
            new VoidReason { Id = 7, Name = "Other", RequiresNote = true, DisplayOrder = 99, IsActive = true }
        );
    }
}

public class ReceiptVoidConfiguration : IEntityTypeConfiguration<ReceiptVoid>
{
    public void Configure(EntityTypeBuilder<ReceiptVoid> builder)
    {
        builder.ToTable("ReceiptVoids");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AdditionalNotes)
            .HasMaxLength(500);

        builder.Property(e => e.VoidedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.VoidedAt)
            .IsRequired();

        builder.Property(e => e.StockRestored)
            .HasDefaultValue(false);

        builder.HasIndex(e => e.VoidedAt);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VoidReason)
            .WithMany(vr => vr.ReceiptVoids)
            .HasForeignKey(e => e.VoidReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VoidedByUser)
            .WithMany()
            .HasForeignKey(e => e.VoidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AuthorizedByUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
