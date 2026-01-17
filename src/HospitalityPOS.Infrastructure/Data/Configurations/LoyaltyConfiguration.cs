using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for LoyaltyMember.
/// </summary>
public class LoyaltyMemberConfiguration : IEntityTypeConfiguration<LoyaltyMember>
{
    public void Configure(EntityTypeBuilder<LoyaltyMember> builder)
    {
        builder.ToTable("LoyaltyMembers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(e => e.Name)
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.MembershipNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Tier)
            .HasConversion<int>()
            .HasDefaultValue(Core.Enums.MembershipTier.Bronze);

        builder.Property(e => e.PointsBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.LifetimePoints)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.LifetimeSpend)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.LastVisit);

        builder.Property(e => e.VisitCount)
            .HasDefaultValue(0);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Unique index on PhoneNumber for duplicate prevention
        builder.HasIndex(e => e.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_LoyaltyMembers_PhoneNumber");

        // Unique index on MembershipNumber
        builder.HasIndex(e => e.MembershipNumber)
            .IsUnique()
            .HasDatabaseName("IX_LoyaltyMembers_MembershipNumber");

        // Index on Tier for tier-based queries
        builder.HasIndex(e => e.Tier)
            .HasDatabaseName("IX_LoyaltyMembers_Tier");

        // Index on LastVisit for activity tracking
        builder.HasIndex(e => e.LastVisit)
            .HasDatabaseName("IX_LoyaltyMembers_LastVisit");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_LoyaltyMembers_IsActive");

        // Navigation to transactions
        builder.HasMany(e => e.Transactions)
            .WithOne(t => t.LoyaltyMember)
            .HasForeignKey(t => t.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for LoyaltyTransaction.
/// </summary>
public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("LoyaltyTransactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Points)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.MonetaryValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.BonusPoints)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.BonusMultiplier)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0m);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(e => e.TransactionDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.LoyaltyMember)
            .WithMany(m => m.Transactions)
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Receipt (optional)
        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to User who processed
        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on LoyaltyMemberId for member transaction history
        builder.HasIndex(e => e.LoyaltyMemberId)
            .HasDatabaseName("IX_LoyaltyTransactions_LoyaltyMemberId");

        // Index on TransactionDate for date range queries
        builder.HasIndex(e => e.TransactionDate)
            .HasDatabaseName("IX_LoyaltyTransactions_TransactionDate");

        // Index on TransactionType for filtering
        builder.HasIndex(e => e.TransactionType)
            .HasDatabaseName("IX_LoyaltyTransactions_TransactionType");

        // Index on ReceiptId for receipt lookups
        builder.HasIndex(e => e.ReceiptId)
            .HasDatabaseName("IX_LoyaltyTransactions_ReceiptId");

        // Composite index for member + date queries
        builder.HasIndex(e => new { e.LoyaltyMemberId, e.TransactionDate })
            .HasDatabaseName("IX_LoyaltyTransactions_Member_Date");
    }
}

/// <summary>
/// Entity configuration for PointsConfiguration.
/// </summary>
public class PointsConfigurationConfiguration : IEntityTypeConfiguration<PointsConfiguration>
{
    public void Configure(EntityTypeBuilder<PointsConfiguration> builder)
    {
        builder.ToTable("PointsConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EarningRate)
            .HasPrecision(18, 2)
            .HasDefaultValue(100m);

        builder.Property(e => e.RedemptionValue)
            .HasPrecision(18, 2)
            .HasDefaultValue(1m);

        builder.Property(e => e.MinimumRedemptionPoints)
            .HasDefaultValue(100);

        builder.Property(e => e.MaximumRedemptionPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.MaxRedemptionPercentage)
            .HasDefaultValue(50);

        builder.Property(e => e.EarnOnDiscountedItems)
            .HasDefaultValue(true);

        builder.Property(e => e.EarnOnTax)
            .HasDefaultValue(false);

        builder.Property(e => e.PointsExpiryDays)
            .HasDefaultValue(0);

        builder.Property(e => e.IsDefault)
            .HasDefaultValue(false);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        // Index on IsDefault for quick default config lookup
        builder.HasIndex(e => e.IsDefault)
            .HasDatabaseName("IX_PointsConfigurations_IsDefault");

        // Unique index on Name
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_PointsConfigurations_Name");
    }
}

/// <summary>
/// Entity configuration for TierConfiguration.
/// </summary>
public class TierConfigurationConfiguration : IEntityTypeConfiguration<TierConfiguration>
{
    public void Configure(EntityTypeBuilder<TierConfiguration> builder)
    {
        builder.ToTable("TierConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Tier)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.SpendThreshold)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.PointsThreshold)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.PointsMultiplier)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0m);

        builder.Property(e => e.DiscountPercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.FreeDelivery)
            .HasDefaultValue(false);

        builder.Property(e => e.PriorityService)
            .HasDefaultValue(false);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(10);

        builder.Property(e => e.IconName)
            .HasMaxLength(50);

        // Unique index on Tier - only one config per tier
        builder.HasIndex(e => e.Tier)
            .IsUnique()
            .HasDatabaseName("IX_TierConfigurations_Tier");

        // Index on SortOrder for display ordering
        builder.HasIndex(e => e.SortOrder)
            .HasDatabaseName("IX_TierConfigurations_SortOrder");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_TierConfigurations_IsActive");
    }
}
