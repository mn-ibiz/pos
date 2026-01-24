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

        builder.Property(e => e.DateOfBirth);

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

/// <summary>
/// Entity configuration for OneTimeReward.
/// </summary>
public class OneTimeRewardConfiguration : IEntityTypeConfiguration<OneTimeReward>
{
    public void Configure(EntityTypeBuilder<OneTimeReward> builder)
    {
        builder.ToTable("OneTimeRewards");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.RewardType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ValueType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Value)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.MinimumTier)
            .HasConversion<int?>();

        builder.Property(e => e.ValidityDays)
            .HasDefaultValue(30);

        builder.Property(e => e.MinimumPurchaseAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaximumDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.SmsTemplate)
            .HasMaxLength(500);

        builder.Property(e => e.EmailTemplate)
            .HasMaxLength(2000);

        builder.Property(e => e.SendSmsNotification)
            .HasDefaultValue(true);

        builder.Property(e => e.SendEmailNotification)
            .HasDefaultValue(true);

        builder.Property(e => e.DaysBeforeToIssue)
            .HasDefaultValue(0);

        builder.Property(e => e.DaysAfterEventValid)
            .HasDefaultValue(7);

        // Foreign key to Product for free item rewards
        builder.HasOne(e => e.FreeItemProduct)
            .WithMany()
            .HasForeignKey(e => e.FreeItemProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on RewardType for filtering
        builder.HasIndex(e => e.RewardType)
            .HasDatabaseName("IX_OneTimeRewards_RewardType");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_OneTimeRewards_IsActive");

        // Composite index for active rewards by type
        builder.HasIndex(e => new { e.RewardType, e.IsActive })
            .HasDatabaseName("IX_OneTimeRewards_Type_Active");
    }
}

/// <summary>
/// Entity configuration for MemberReward.
/// </summary>
public class MemberRewardConfiguration : IEntityTypeConfiguration<MemberReward>
{
    public void Configure(EntityTypeBuilder<MemberReward> builder)
    {
        builder.ToTable("MemberRewards");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RedemptionCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(Core.Enums.MemberRewardStatus.Active);

        builder.Property(e => e.IssuedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.RedeemedValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.PointsAwarded)
            .HasPrecision(18, 2);

        builder.Property(e => e.RewardYear)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.LoyaltyMember)
            .WithMany(m => m.MemberRewards)
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to OneTimeReward
        builder.HasOne(e => e.OneTimeReward)
            .WithMany(r => r.MemberRewards)
            .HasForeignKey(e => e.OneTimeRewardId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Receipt (where redeemed)
        builder.HasOne(e => e.RedeemedOnReceipt)
            .WithMany()
            .HasForeignKey(e => e.RedeemedOnReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique index on RedemptionCode
        builder.HasIndex(e => e.RedemptionCode)
            .IsUnique()
            .HasDatabaseName("IX_MemberRewards_RedemptionCode");

        // Index on LoyaltyMemberId for member lookups
        builder.HasIndex(e => e.LoyaltyMemberId)
            .HasDatabaseName("IX_MemberRewards_LoyaltyMemberId");

        // Index on Status for filtering active rewards
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_MemberRewards_Status");

        // Index on ExpiresAt for expiry processing
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_MemberRewards_ExpiresAt");

        // Composite index for member + year to prevent duplicates
        builder.HasIndex(e => new { e.LoyaltyMemberId, e.OneTimeRewardId, e.RewardYear })
            .HasDatabaseName("IX_MemberRewards_Member_Reward_Year");

        // Composite index for member + status for active reward lookup
        builder.HasIndex(e => new { e.LoyaltyMemberId, e.Status })
            .HasDatabaseName("IX_MemberRewards_Member_Status");
    }
}

/// <summary>
/// Entity configuration for PointsMultiplierRule.
/// </summary>
public class PointsMultiplierRuleConfiguration : IEntityTypeConfiguration<PointsMultiplierRule>
{
    public void Configure(EntityTypeBuilder<PointsMultiplierRule> builder)
    {
        builder.ToTable("PointsMultiplierRules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.RuleType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Multiplier)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0m);

        builder.Property(e => e.IsStackable)
            .HasDefaultValue(false);

        builder.Property(e => e.Priority)
            .HasDefaultValue(100);

        builder.Property(e => e.MinimumTier)
            .HasConversion<int?>();

        builder.Property(e => e.DaysOfWeek)
            .HasMaxLength(100);

        builder.Property(e => e.MinimumPurchaseAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxBonusPointsPerTransaction)
            .HasPrecision(18, 2);

        builder.Property(e => e.CurrentUsageCount)
            .HasDefaultValue(0);

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to Category
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on RuleType for filtering
        builder.HasIndex(e => e.RuleType)
            .HasDatabaseName("IX_PointsMultiplierRules_RuleType");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_PointsMultiplierRules_IsActive");

        // Index on ProductId for product-specific rules
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_PointsMultiplierRules_ProductId");

        // Index on CategoryId for category-specific rules
        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_PointsMultiplierRules_CategoryId");

        // Index on Priority for ordered evaluation
        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("IX_PointsMultiplierRules_Priority");

        // Composite index for date range queries
        builder.HasIndex(e => new { e.StartDate, e.EndDate })
            .HasDatabaseName("IX_PointsMultiplierRules_DateRange");

        // Composite index for active rules by type
        builder.HasIndex(e => new { e.IsActive, e.RuleType, e.Priority })
            .HasDatabaseName("IX_PointsMultiplierRules_Active_Type_Priority");
    }
}

/// <summary>
/// Entity configuration for PointsMultiplierUsage.
/// </summary>
public class PointsMultiplierUsageConfiguration : IEntityTypeConfiguration<PointsMultiplierUsage>
{
    public void Configure(EntityTypeBuilder<PointsMultiplierUsage> builder)
    {
        builder.ToTable("PointsMultiplierUsages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UsedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.BasePoints)
            .HasPrecision(18, 2);

        builder.Property(e => e.BonusPointsEarned)
            .HasPrecision(18, 2);

        builder.Property(e => e.MultiplierApplied)
            .HasPrecision(5, 2);

        // Foreign key to PointsMultiplierRule
        builder.HasOne(e => e.Rule)
            .WithMany(r => r.Usages)
            .HasForeignKey(e => e.PointsMultiplierRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.LoyaltyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to LoyaltyTransaction
        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.LoyaltyTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on PointsMultiplierRuleId for rule usage queries
        builder.HasIndex(e => e.PointsMultiplierRuleId)
            .HasDatabaseName("IX_PointsMultiplierUsages_RuleId");

        // Index on LoyaltyMemberId for member usage queries
        builder.HasIndex(e => e.LoyaltyMemberId)
            .HasDatabaseName("IX_PointsMultiplierUsages_MemberId");

        // Index on UsedAt for date range queries
        builder.HasIndex(e => e.UsedAt)
            .HasDatabaseName("IX_PointsMultiplierUsages_UsedAt");

        // Composite index for rule + member usage count
        builder.HasIndex(e => new { e.PointsMultiplierRuleId, e.LoyaltyMemberId })
            .HasDatabaseName("IX_PointsMultiplierUsages_Rule_Member");
    }
}

#region Referral Program Configurations

/// <summary>
/// Entity configuration for ReferralCode.
/// </summary>
public class ReferralCodeConfiguration : IEntityTypeConfiguration<ReferralCode>
{
    public void Configure(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("ReferralCodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ShareableUrl)
            .HasMaxLength(500);

        builder.Property(e => e.TimesUsed)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalPointsEarned)
            .HasDefaultValue(0);

        // Unique index on Code
        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("IX_ReferralCodes_Code");

        // Index on MemberId for member lookups
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_ReferralCodes_MemberId");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_ReferralCodes_IsActive");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to referrals using this code
        builder.HasMany(e => e.Referrals)
            .WithOne(r => r.ReferralCode)
            .HasForeignKey(r => r.ReferralCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for Referral.
/// </summary>
public class ReferralEntityConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ReferralStatus.Pending);

        builder.Property(e => e.ReferrerBonusPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.RefereeBonusPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.ReferredAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.QualifyingAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // Index on ReferrerId for referrer queries
        builder.HasIndex(e => e.ReferrerId)
            .HasDatabaseName("IX_Referrals_ReferrerId");

        // Index on RefereeId for referee queries
        builder.HasIndex(e => e.RefereeId)
            .HasDatabaseName("IX_Referrals_RefereeId");

        // Index on Status for filtering
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Referrals_Status");

        // Index on ExpiresAt for expiry processing
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_Referrals_ExpiresAt");

        // Index on ReferralCodeId for code lookups
        builder.HasIndex(e => e.ReferralCodeId)
            .HasDatabaseName("IX_Referrals_ReferralCodeId");

        // Composite index for pending referrals expiring
        builder.HasIndex(e => new { e.Status, e.ExpiresAt })
            .HasDatabaseName("IX_Referrals_Status_ExpiresAt");

        // Composite index for referrer stats
        builder.HasIndex(e => new { e.ReferrerId, e.Status })
            .HasDatabaseName("IX_Referrals_Referrer_Status");

        // Foreign key to Referrer
        builder.HasOne(e => e.Referrer)
            .WithMany()
            .HasForeignKey(e => e.ReferrerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Referee
        builder.HasOne(e => e.Referee)
            .WithMany()
            .HasForeignKey(e => e.RefereeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ReferralCode
        builder.HasOne(e => e.ReferralCode)
            .WithMany(c => c.Referrals)
            .HasForeignKey(e => e.ReferralCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to qualifying receipt (optional)
        builder.HasOne(e => e.QualifyingReceipt)
            .WithMany()
            .HasForeignKey(e => e.QualifyingReceiptId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for ReferralConfiguration.
/// </summary>
public class ReferralConfigurationEntityConfiguration : IEntityTypeConfiguration<ReferralConfiguration>
{
    public void Configure(EntityTypeBuilder<ReferralConfiguration> builder)
    {
        builder.ToTable("ReferralConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReferrerBonusPoints)
            .HasDefaultValue(500);

        builder.Property(e => e.RefereeBonusPoints)
            .HasDefaultValue(200);

        builder.Property(e => e.MinPurchaseAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(500m);

        builder.Property(e => e.ExpiryDays)
            .HasDefaultValue(30);

        builder.Property(e => e.EnableLeaderboard)
            .HasDefaultValue(true);

        builder.Property(e => e.RequireNewMember)
            .HasDefaultValue(true);

        builder.Property(e => e.IsProgramActive)
            .HasDefaultValue(true);

        builder.Property(e => e.ReferrerSmsTemplate)
            .HasMaxLength(500);

        builder.Property(e => e.RefereeSmsTemplate)
            .HasMaxLength(500);

        builder.Property(e => e.ShareableLinkBaseUrl)
            .HasMaxLength(500);

        // Index on StoreId for store-specific lookups
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_ReferralConfigurations_StoreId");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_ReferralConfigurations_IsActive");

        // Foreign key to Store (optional)
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for ReferralMilestone.
/// </summary>
public class ReferralMilestoneConfiguration : IEntityTypeConfiguration<ReferralMilestone>
{
    public void Configure(EntityTypeBuilder<ReferralMilestone> builder)
    {
        builder.ToTable("ReferralMilestones");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ReferralCount)
            .IsRequired();

        builder.Property(e => e.BonusPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.BadgeIcon)
            .HasMaxLength(200);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Unique index on ReferralCount to ensure one milestone per count
        builder.HasIndex(e => e.ReferralCount)
            .IsUnique()
            .HasDatabaseName("IX_ReferralMilestones_ReferralCount");

        // Index on SortOrder for ordered display
        builder.HasIndex(e => e.SortOrder)
            .HasDatabaseName("IX_ReferralMilestones_SortOrder");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_ReferralMilestones_IsActive");

        // Navigation to member milestones
        builder.HasMany(e => e.MemberMilestones)
            .WithOne(mm => mm.Milestone)
            .HasForeignKey(mm => mm.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for MemberReferralMilestone.
/// </summary>
public class MemberReferralMilestoneConfiguration : IEntityTypeConfiguration<MemberReferralMilestone>
{
    public void Configure(EntityTypeBuilder<MemberReferralMilestone> builder)
    {
        builder.ToTable("MemberReferralMilestones");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AchievedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.BonusPointsAwarded)
            .HasDefaultValue(0);

        builder.Property(e => e.ReferralCountAtAchievement)
            .HasDefaultValue(0);

        // Index on MemberId for member lookups
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_MemberReferralMilestones_MemberId");

        // Index on MilestoneId for milestone lookups
        builder.HasIndex(e => e.MilestoneId)
            .HasDatabaseName("IX_MemberReferralMilestones_MilestoneId");

        // Unique composite index to prevent duplicate milestone achievements
        builder.HasIndex(e => new { e.MemberId, e.MilestoneId })
            .IsUnique()
            .HasDatabaseName("IX_MemberReferralMilestones_Member_Milestone");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ReferralMilestone
        builder.HasOne(e => e.Milestone)
            .WithMany(m => m.MemberMilestones)
            .HasForeignKey(e => e.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

#endregion
