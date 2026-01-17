using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for MpesaConfiguration entity.
/// </summary>
public class MpesaConfigurationConfiguration : IEntityTypeConfiguration<MpesaConfiguration>
{
    public void Configure(EntityTypeBuilder<MpesaConfiguration> builder)
    {
        builder.ToTable("MpesaConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .IsRequired();

        builder.Property(e => e.ConsumerKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ConsumerSecret)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.BusinessShortCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Passkey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TransactionType)
            .IsRequired();

        builder.Property(e => e.CallbackUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ApiBaseUrl)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AccountReferencePrefix)
            .HasMaxLength(20);

        builder.Property(e => e.DefaultDescription)
            .HasMaxLength(200);

        builder.Property(e => e.CachedAccessToken)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.BusinessShortCode);
    }
}

/// <summary>
/// EF Core configuration for MpesaStkPushRequest entity.
/// </summary>
public class MpesaStkPushRequestConfiguration : IEntityTypeConfiguration<MpesaStkPushRequest>
{
    public void Configure(EntityTypeBuilder<MpesaStkPushRequest> builder)
    {
        builder.ToTable("MpesaStkPushRequests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MerchantRequestId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CheckoutRequestId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.AccountReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TransactionDescription)
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.ResponseCode)
            .HasMaxLength(20);

        builder.Property(e => e.ResponseDescription)
            .HasMaxLength(500);

        builder.Property(e => e.ResultCode)
            .HasMaxLength(20);

        builder.Property(e => e.ResultDescription)
            .HasMaxLength(500);

        builder.Property(e => e.MpesaReceiptNumber)
            .HasMaxLength(50);

        builder.Property(e => e.PhoneNumberUsed)
            .HasMaxLength(20);

        builder.HasOne(e => e.Payment)
            .WithMany()
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Configuration)
            .WithMany()
            .HasForeignKey(e => e.ConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CheckoutRequestId);
        builder.HasIndex(e => e.MerchantRequestId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequestedAt);
        builder.HasIndex(e => e.MpesaReceiptNumber);
    }
}

/// <summary>
/// EF Core configuration for MpesaTransaction entity.
/// </summary>
public class MpesaTransactionConfiguration : IEntityTypeConfiguration<MpesaTransaction>
{
    public void Configure(EntityTypeBuilder<MpesaTransaction> builder)
    {
        builder.ToTable("MpesaTransactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MpesaReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TransactionDate)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.Payment)
            .WithMany()
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.StkPushRequest)
            .WithMany()
            .HasForeignKey(e => e.StkPushRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.VerifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.MpesaReceiptNumber)
            .IsUnique();
        builder.HasIndex(e => e.TransactionDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsManualEntry);
        builder.HasIndex(e => e.IsVerified);
    }
}
