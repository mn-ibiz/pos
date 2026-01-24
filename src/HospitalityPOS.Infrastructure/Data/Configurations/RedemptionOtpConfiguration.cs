using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the RedemptionOtp entity.
/// </summary>
public class RedemptionOtpConfiguration : IEntityTypeConfiguration<RedemptionOtp>
{
    public void Configure(EntityTypeBuilder<RedemptionOtp> builder)
    {
        builder.ToTable("RedemptionOtps");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(e => e.AuthorizedPoints)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxAttempts)
            .HasDefaultValue(3);

        builder.Property(e => e.AttemptCount)
            .HasDefaultValue(0);

        builder.Property(e => e.IsVerified)
            .HasDefaultValue(false);

        // Indexes for efficient queries
        builder.HasIndex(e => new { e.LoyaltyMemberId, e.IsActive, e.IsVerified })
            .HasDatabaseName("IX_RedemptionOtps_Member_Active");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_RedemptionOtps_ExpiresAt");

        // Foreign key relationships
        builder.HasOne(e => e.LoyaltyMember)
            .WithMany()
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.VerifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsLocked);
        builder.Ignore(e => e.CanVerify);
        builder.Ignore(e => e.RemainingAttempts);
    }
}
