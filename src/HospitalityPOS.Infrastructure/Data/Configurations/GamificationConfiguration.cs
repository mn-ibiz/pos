using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Badge.
/// </summary>
public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.TriggerType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Rarity)
            .HasConversion<int>()
            .HasDefaultValue(BadgeRarity.Common);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        builder.Property(e => e.PointsAwarded)
            .HasDefaultValue(0);

        builder.Property(e => e.IsSecret)
            .HasDefaultValue(false);

        builder.Property(e => e.IsRepeatable)
            .HasDefaultValue(false);

        builder.Property(e => e.MaxEarnings)
            .HasDefaultValue(0);

        builder.Property(e => e.CriteriaJson)
            .HasMaxLength(2000);

        builder.Property(e => e.ThresholdValue)
            .HasPrecision(18, 2);

        // Index on Category for filtering
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_Badges_Category");

        // Index on TriggerType for automatic badge processing
        builder.HasIndex(e => e.TriggerType)
            .HasDatabaseName("IX_Badges_TriggerType");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Badges_IsActive");

        // Index on StoreId for store-specific badges
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_Badges_StoreId");

        // Composite index for available badges query
        builder.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate })
            .HasDatabaseName("IX_Badges_Active_DateRange");

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

        // Navigation to member badges
        builder.HasMany(e => e.MemberBadges)
            .WithOne(mb => mb.Badge)
            .HasForeignKey(mb => mb.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for MemberBadge.
/// </summary>
public class MemberBadgeConfiguration : IEntityTypeConfiguration<MemberBadge>
{
    public void Configure(EntityTypeBuilder<MemberBadge> builder)
    {
        builder.ToTable("MemberBadges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EarnedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.TimesEarned)
            .HasDefaultValue(1);

        builder.Property(e => e.PointsAwarded)
            .HasDefaultValue(0);

        builder.Property(e => e.IsViewed)
            .HasDefaultValue(false);

        builder.Property(e => e.IsPinned)
            .HasDefaultValue(false);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Unique index on MemberId + BadgeId (one record per badge per member)
        builder.HasIndex(e => new { e.MemberId, e.BadgeId })
            .IsUnique()
            .HasDatabaseName("IX_MemberBadges_Member_Badge");

        // Index on MemberId for member badge queries
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_MemberBadges_MemberId");

        // Index on EarnedAt for recent badges
        builder.HasIndex(e => e.EarnedAt)
            .HasDatabaseName("IX_MemberBadges_EarnedAt");

        // Index on IsViewed for unviewed badge notifications
        builder.HasIndex(e => e.IsViewed)
            .HasDatabaseName("IX_MemberBadges_IsViewed");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Badge
        builder.HasOne(e => e.Badge)
            .WithMany(b => b.MemberBadges)
            .HasForeignKey(e => e.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Order
        builder.HasOne(e => e.TriggeredByOrder)
            .WithMany()
            .HasForeignKey(e => e.TriggeredByOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for Challenge.
/// </summary>
public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Period)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.GoalType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.TargetValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.RewardPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.BonusMultiplier)
            .HasPrecision(5, 2);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        builder.Property(e => e.IsRecurring)
            .HasDefaultValue(false);

        builder.Property(e => e.MinimumTier)
            .HasConversion<int?>();

        builder.Property(e => e.MaxParticipants)
            .HasDefaultValue(0);

        builder.Property(e => e.ShowLeaderboard)
            .HasDefaultValue(true);

        // Index on Period for period-based queries
        builder.HasIndex(e => e.Period)
            .HasDatabaseName("IX_Challenges_Period");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Challenges_IsActive");

        // Index on date range for active challenges
        builder.HasIndex(e => new { e.StartDate, e.EndDate })
            .HasDatabaseName("IX_Challenges_DateRange");

        // Composite index for active challenge queries
        builder.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate, e.StoreId })
            .HasDatabaseName("IX_Challenges_Active_DateRange_Store");

        // Index on StoreId for store-specific challenges
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_Challenges_StoreId");

        // Foreign key to Badge (reward)
        builder.HasOne(e => e.RewardBadge)
            .WithMany()
            .HasForeignKey(e => e.RewardBadgeId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // Navigation to member challenges
        builder.HasMany(e => e.MemberChallenges)
            .WithOne(mc => mc.Challenge)
            .HasForeignKey(mc => mc.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for MemberChallenge.
/// </summary>
public class MemberChallengeConfiguration : IEntityTypeConfiguration<MemberChallenge>
{
    public void Configure(EntityTypeBuilder<MemberChallenge> builder)
    {
        builder.ToTable("MemberChallenges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurrentProgress)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ChallengeStatus.Active);

        builder.Property(e => e.JoinedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.PointsAwarded)
            .HasDefaultValue(0);

        // Unique index on MemberId + ChallengeId
        builder.HasIndex(e => new { e.MemberId, e.ChallengeId })
            .IsUnique()
            .HasDatabaseName("IX_MemberChallenges_Member_Challenge");

        // Index on MemberId for member challenge queries
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_MemberChallenges_MemberId");

        // Index on Status for filtering
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_MemberChallenges_Status");

        // Composite index for active challenge queries
        builder.HasIndex(e => new { e.MemberId, e.Status, e.IsActive })
            .HasDatabaseName("IX_MemberChallenges_Member_Status_Active");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Challenge
        builder.HasOne(e => e.Challenge)
            .WithMany(c => c.MemberChallenges)
            .HasForeignKey(e => e.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to awarded badge
        builder.HasOne(e => e.AwardedBadge)
            .WithMany()
            .HasForeignKey(e => e.AwardedBadgeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for MemberStreak.
/// </summary>
public class MemberStreakConfiguration : IEntityTypeConfiguration<MemberStreak>
{
    public void Configure(EntityTypeBuilder<MemberStreak> builder)
    {
        builder.ToTable("MemberStreaks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StreakType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.CurrentStreak)
            .HasDefaultValue(0);

        builder.Property(e => e.LongestStreak)
            .HasDefaultValue(0);

        builder.Property(e => e.StreakStartedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.LastActivityAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.TimesReset)
            .HasDefaultValue(0);

        builder.Property(e => e.IsAtRisk)
            .HasDefaultValue(false);

        builder.Property(e => e.IsFrozen)
            .HasDefaultValue(false);

        builder.Property(e => e.FreezeTokensRemaining)
            .HasDefaultValue(3);

        // Unique index on MemberId + StreakType + StoreId
        builder.HasIndex(e => new { e.MemberId, e.StreakType, e.StoreId })
            .IsUnique()
            .HasDatabaseName("IX_MemberStreaks_Member_Type_Store");

        // Index on MemberId for member streak queries
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_MemberStreaks_MemberId");

        // Index on IsAtRisk for at-risk notifications
        builder.HasIndex(e => e.IsAtRisk)
            .HasDatabaseName("IX_MemberStreaks_IsAtRisk");

        // Index on NextActivityDeadline for broken streak processing
        builder.HasIndex(e => e.NextActivityDeadline)
            .HasDatabaseName("IX_MemberStreaks_NextActivityDeadline");

        // Composite index for streak status queries
        builder.HasIndex(e => new { e.IsActive, e.CurrentStreak, e.IsAtRisk })
            .HasDatabaseName("IX_MemberStreaks_Active_Current_AtRisk");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to milestones
        builder.HasMany(e => e.Milestones)
            .WithOne(m => m.MemberStreak)
            .HasForeignKey(m => m.MemberStreakId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity configuration for StreakMilestoneDefinition.
/// </summary>
public class StreakMilestoneDefinitionConfiguration : IEntityTypeConfiguration<StreakMilestoneDefinition>
{
    public void Configure(EntityTypeBuilder<StreakMilestoneDefinition> builder)
    {
        builder.ToTable("StreakMilestoneDefinitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StreakType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.StreakCount)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.RewardPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.FreezeTokensAwarded)
            .HasDefaultValue(0);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        // Unique index on StreakType + StreakCount
        builder.HasIndex(e => new { e.StreakType, e.StreakCount })
            .IsUnique()
            .HasDatabaseName("IX_StreakMilestoneDefinitions_Type_Count");

        // Index on StreakType for filtering
        builder.HasIndex(e => e.StreakType)
            .HasDatabaseName("IX_StreakMilestoneDefinitions_StreakType");

        // Foreign key to Badge (reward)
        builder.HasOne(e => e.RewardBadge)
            .WithMany()
            .HasForeignKey(e => e.RewardBadgeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for StreakMilestone.
/// </summary>
public class StreakMilestoneConfiguration : IEntityTypeConfiguration<StreakMilestone>
{
    public void Configure(EntityTypeBuilder<StreakMilestone> builder)
    {
        builder.ToTable("StreakMilestones");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AchievedAtStreak)
            .IsRequired();

        builder.Property(e => e.AchievedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.PointsAwarded)
            .HasDefaultValue(0);

        builder.Property(e => e.FreezeTokensAwarded)
            .HasDefaultValue(0);

        // Unique index on MemberStreakId + MilestoneDefinitionId
        builder.HasIndex(e => new { e.MemberStreakId, e.MilestoneDefinitionId })
            .IsUnique()
            .HasDatabaseName("IX_StreakMilestones_Streak_Milestone");

        // Index on MemberId for member milestone queries
        builder.HasIndex(e => e.MemberId)
            .HasDatabaseName("IX_StreakMilestones_MemberId");

        // Index on AchievedAt for recent milestones
        builder.HasIndex(e => e.AchievedAt)
            .HasDatabaseName("IX_StreakMilestones_AchievedAt");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to MemberStreak
        builder.HasOne(e => e.MemberStreak)
            .WithMany(s => s.Milestones)
            .HasForeignKey(e => e.MemberStreakId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to StreakMilestoneDefinition
        builder.HasOne(e => e.MilestoneDefinition)
            .WithMany()
            .HasForeignKey(e => e.MilestoneDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to awarded badge
        builder.HasOne(e => e.AwardedBadge)
            .WithMany()
            .HasForeignKey(e => e.AwardedBadgeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for GamificationConfiguration.
/// </summary>
public class GamificationConfigurationEntityConfiguration : IEntityTypeConfiguration<GamificationConfiguration>
{
    public void Configure(EntityTypeBuilder<GamificationConfiguration> builder)
    {
        builder.ToTable("GamificationConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.BadgesEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.ChallengesEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.StreaksEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.DefaultFreezeTokens)
            .HasDefaultValue(3);

        builder.Property(e => e.StreakAtRiskHours)
            .HasDefaultValue(12);

        builder.Property(e => e.ShowBadgesOnReceipt)
            .HasDefaultValue(true);

        builder.Property(e => e.MaxBadgesOnReceipt)
            .HasDefaultValue(3);

        builder.Property(e => e.NotifyOnBadgeEarned)
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyOnChallengeProgress)
            .HasDefaultValue(true);

        builder.Property(e => e.ProgressNotificationThresholds)
            .HasMaxLength(100)
            .HasDefaultValue("50,75,90");

        builder.Property(e => e.NotifyOnStreakAtRisk)
            .HasDefaultValue(true);

        builder.Property(e => e.AutoEnrollInChallenges)
            .HasDefaultValue(true);

        // Unique index on StoreId (one config per store)
        builder.HasIndex(e => e.StoreId)
            .IsUnique()
            .HasDatabaseName("IX_GamificationConfigurations_StoreId");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
