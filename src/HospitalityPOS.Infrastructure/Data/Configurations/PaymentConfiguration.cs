using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PaymentMethod entity.
/// </summary>
public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.Property(e => e.ReferenceLabel)
            .HasMaxLength(50);

        builder.Property(e => e.IconPath)
            .HasMaxLength(200);

        builder.Property(e => e.BackgroundColor)
            .HasMaxLength(20);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.DisplayOrder);

        builder.HasIndex(e => e.IsActive);

        // Seed default payment methods
        builder.HasData(
            new PaymentMethod
            {
                Id = 1,
                Name = "Cash",
                Code = "CASH",
                Type = PaymentMethodType.Cash,
                Description = "Cash payment",
                IsActive = true,
                RequiresReference = false,
                SupportsChange = true,
                OpensDrawer = true,
                DisplayOrder = 1,
                BackgroundColor = "#4CAF50"
            },
            new PaymentMethod
            {
                Id = 2,
                Name = "M-Pesa",
                Code = "MPESA",
                Type = PaymentMethodType.MPesa,
                Description = "Safaricom M-Pesa mobile money",
                IsActive = true,
                RequiresReference = true,
                ReferenceLabel = "M-Pesa Code",
                ReferenceMinLength = 10,
                ReferenceMaxLength = 10,
                SupportsChange = false,
                OpensDrawer = false,
                DisplayOrder = 2,
                BackgroundColor = "#00C853"
            },
            new PaymentMethod
            {
                Id = 3,
                Name = "Airtel Money",
                Code = "AIRTEL",
                Type = PaymentMethodType.MPesa,
                Description = "Airtel Money mobile payment",
                IsActive = true,
                RequiresReference = true,
                ReferenceLabel = "Airtel Code",
                ReferenceMinLength = 10,
                ReferenceMaxLength = 10,
                SupportsChange = false,
                OpensDrawer = false,
                DisplayOrder = 3,
                BackgroundColor = "#FF5722"
            },
            new PaymentMethod
            {
                Id = 4,
                Name = "Credit Card",
                Code = "CREDIT_CARD",
                Type = PaymentMethodType.Card,
                Description = "Credit card payment",
                IsActive = true,
                RequiresReference = false,
                ReferenceLabel = "Last 4 Digits (Optional)",
                ReferenceMinLength = 4,
                ReferenceMaxLength = 4,
                SupportsChange = false,
                OpensDrawer = false,
                DisplayOrder = 4,
                BackgroundColor = "#2196F3"
            },
            new PaymentMethod
            {
                Id = 5,
                Name = "Debit Card",
                Code = "DEBIT_CARD",
                Type = PaymentMethodType.Card,
                Description = "Debit card payment",
                IsActive = true,
                RequiresReference = false,
                ReferenceLabel = "Last 4 Digits (Optional)",
                ReferenceMinLength = 4,
                ReferenceMaxLength = 4,
                SupportsChange = false,
                OpensDrawer = false,
                DisplayOrder = 5,
                BackgroundColor = "#9C27B0"
            },
            new PaymentMethod
            {
                Id = 6,
                Name = "Bank Transfer",
                Code = "BANK_TRANSFER",
                Type = PaymentMethodType.BankTransfer,
                Description = "Bank transfer or RTGS",
                IsActive = false,
                RequiresReference = true,
                ReferenceLabel = "Reference Number",
                SupportsChange = false,
                OpensDrawer = false,
                DisplayOrder = 6,
                BackgroundColor = "#607D8B"
            }
        );
    }
}

/// <summary>
/// EF Core configuration for Payment entity.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TenderedAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.ChangeAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Reference)
            .HasMaxLength(50);

        builder.Property(e => e.TerminalCode)
            .HasMaxLength(20);

        builder.HasIndex(e => e.ReceiptId);

        builder.HasIndex(e => e.PaymentMethodId);

        builder.HasIndex(e => e.Reference);

        // Index for terminal-based payment queries
        builder.HasIndex(e => e.TerminalId)
            .HasDatabaseName("IX_Payments_TerminalId");

        builder.HasOne(e => e.Receipt)
            .WithMany(r => r.Payments)
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PaymentMethod)
            .WithMany(pm => pm.Payments)
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Terminal)
            .WithMany()
            .HasForeignKey(e => e.TerminalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
