using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <summary>
/// Migration to add gamification system - badges, challenges, and streaks.
/// </summary>
public partial class AddGamification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Badges table
        migrationBuilder.CreateTable(
            name: "Badges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Category = table.Column<int>(type: "int", nullable: false),
                TriggerType = table.Column<int>(type: "int", nullable: false),
                Rarity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                PointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsSecret = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                IsRepeatable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                MaxEarnings = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CriteriaJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                ThresholdValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                ProductId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Badges", x => x.Id);
                table.ForeignKey(
                    name: "FK_Badges_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Badges_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Badges_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create MemberBadges table
        migrationBuilder.CreateTable(
            name: "MemberBadges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                BadgeId = table.Column<int>(type: "int", nullable: false),
                EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                TimesEarned = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                PointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                TriggeredByOrderId = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsViewed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsPinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberBadges", x => x.Id);
                table.ForeignKey(
                    name: "FK_MemberBadges_LoyaltyMembers_MemberId",
                    column: x => x.MemberId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberBadges_Badges_BadgeId",
                    column: x => x.BadgeId,
                    principalTable: "Badges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MemberBadges_Orders_TriggeredByOrderId",
                    column: x => x.TriggeredByOrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_MemberBadges_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create Challenges table
        migrationBuilder.CreateTable(
            name: "Challenges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Period = table.Column<int>(type: "int", nullable: false),
                GoalType = table.Column<int>(type: "int", nullable: false),
                TargetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RewardBadgeId = table.Column<int>(type: "int", nullable: true),
                BonusMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsRecurring = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ProductId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                MinimumTier = table.Column<int>(type: "int", nullable: true),
                MaxParticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ShowLeaderboard = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Challenges", x => x.Id);
                table.ForeignKey(
                    name: "FK_Challenges_Badges_RewardBadgeId",
                    column: x => x.RewardBadgeId,
                    principalTable: "Badges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Challenges_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Challenges_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Challenges_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create MemberChallenges table
        migrationBuilder.CreateTable(
            name: "MemberChallenges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                ChallengeId = table.Column<int>(type: "int", nullable: false),
                CurrentProgress = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                PointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                AwardedBadgeId = table.Column<int>(type: "int", nullable: true),
                LastProgressAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberChallenges", x => x.Id);
                table.ForeignKey(
                    name: "FK_MemberChallenges_LoyaltyMembers_MemberId",
                    column: x => x.MemberId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberChallenges_Challenges_ChallengeId",
                    column: x => x.ChallengeId,
                    principalTable: "Challenges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MemberChallenges_MemberBadges_AwardedBadgeId",
                    column: x => x.AwardedBadgeId,
                    principalTable: "MemberBadges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create StreakMilestoneDefinitions table
        migrationBuilder.CreateTable(
            name: "StreakMilestoneDefinitions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StreakType = table.Column<int>(type: "int", nullable: false),
                StreakCount = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RewardBadgeId = table.Column<int>(type: "int", nullable: true),
                FreezeTokensAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StreakMilestoneDefinitions", x => x.Id);
                table.ForeignKey(
                    name: "FK_StreakMilestoneDefinitions_Badges_RewardBadgeId",
                    column: x => x.RewardBadgeId,
                    principalTable: "Badges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create MemberStreaks table
        migrationBuilder.CreateTable(
            name: "MemberStreaks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                StreakType = table.Column<int>(type: "int", nullable: false),
                CurrentStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LongestStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                StreakStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                TimesReset = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                NextActivityDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsAtRisk = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsFrozen = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                FreezeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FreezeTokensRemaining = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberStreaks", x => x.Id);
                table.ForeignKey(
                    name: "FK_MemberStreaks_LoyaltyMembers_MemberId",
                    column: x => x.MemberId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberStreaks_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create StreakMilestones table
        migrationBuilder.CreateTable(
            name: "StreakMilestones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                MemberStreakId = table.Column<int>(type: "int", nullable: false),
                MilestoneDefinitionId = table.Column<int>(type: "int", nullable: false),
                AchievedAtStreak = table.Column<int>(type: "int", nullable: false),
                AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                PointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                AwardedBadgeId = table.Column<int>(type: "int", nullable: true),
                FreezeTokensAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StreakMilestones", x => x.Id);
                table.ForeignKey(
                    name: "FK_StreakMilestones_LoyaltyMembers_MemberId",
                    column: x => x.MemberId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StreakMilestones_MemberStreaks_MemberStreakId",
                    column: x => x.MemberStreakId,
                    principalTable: "MemberStreaks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StreakMilestones_StreakMilestoneDefinitions_MilestoneDefinitionId",
                    column: x => x.MilestoneDefinitionId,
                    principalTable: "StreakMilestoneDefinitions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StreakMilestones_MemberBadges_AwardedBadgeId",
                    column: x => x.AwardedBadgeId,
                    principalTable: "MemberBadges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create GamificationConfigurations table
        migrationBuilder.CreateTable(
            name: "GamificationConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                BadgesEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ChallengesEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                StreaksEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                DefaultFreezeTokens = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                StreakAtRiskHours = table.Column<int>(type: "int", nullable: false, defaultValue: 12),
                ShowBadgesOnReceipt = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                MaxBadgesOnReceipt = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                NotifyOnBadgeEarned = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                NotifyOnChallengeProgress = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ProgressNotificationThresholds = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "50,75,90"),
                NotifyOnStreakAtRisk = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                AutoEnrollInChallenges = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GamificationConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_GamificationConfigurations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for Badges
        migrationBuilder.CreateIndex(
            name: "IX_Badges_Category",
            table: "Badges",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_Badges_TriggerType",
            table: "Badges",
            column: "TriggerType");

        migrationBuilder.CreateIndex(
            name: "IX_Badges_IsActive",
            table: "Badges",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Badges_StoreId",
            table: "Badges",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_Badges_Active_DateRange",
            table: "Badges",
            columns: new[] { "IsActive", "StartDate", "EndDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Badges_ProductId",
            table: "Badges",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Badges_CategoryId",
            table: "Badges",
            column: "CategoryId");

        // Create indexes for MemberBadges
        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_Member_Badge",
            table: "MemberBadges",
            columns: new[] { "MemberId", "BadgeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_MemberId",
            table: "MemberBadges",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_EarnedAt",
            table: "MemberBadges",
            column: "EarnedAt");

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_IsViewed",
            table: "MemberBadges",
            column: "IsViewed");

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_BadgeId",
            table: "MemberBadges",
            column: "BadgeId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_TriggeredByOrderId",
            table: "MemberBadges",
            column: "TriggeredByOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberBadges_StoreId",
            table: "MemberBadges",
            column: "StoreId");

        // Create indexes for Challenges
        migrationBuilder.CreateIndex(
            name: "IX_Challenges_Period",
            table: "Challenges",
            column: "Period");

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_IsActive",
            table: "Challenges",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_DateRange",
            table: "Challenges",
            columns: new[] { "StartDate", "EndDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_Active_DateRange_Store",
            table: "Challenges",
            columns: new[] { "IsActive", "StartDate", "EndDate", "StoreId" });

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_StoreId",
            table: "Challenges",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_RewardBadgeId",
            table: "Challenges",
            column: "RewardBadgeId");

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_ProductId",
            table: "Challenges",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Challenges_CategoryId",
            table: "Challenges",
            column: "CategoryId");

        // Create indexes for MemberChallenges
        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_Member_Challenge",
            table: "MemberChallenges",
            columns: new[] { "MemberId", "ChallengeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_MemberId",
            table: "MemberChallenges",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_Status",
            table: "MemberChallenges",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_Member_Status_Active",
            table: "MemberChallenges",
            columns: new[] { "MemberId", "Status", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_ChallengeId",
            table: "MemberChallenges",
            column: "ChallengeId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberChallenges_AwardedBadgeId",
            table: "MemberChallenges",
            column: "AwardedBadgeId");

        // Create indexes for StreakMilestoneDefinitions
        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestoneDefinitions_Type_Count",
            table: "StreakMilestoneDefinitions",
            columns: new[] { "StreakType", "StreakCount" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestoneDefinitions_StreakType",
            table: "StreakMilestoneDefinitions",
            column: "StreakType");

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestoneDefinitions_RewardBadgeId",
            table: "StreakMilestoneDefinitions",
            column: "RewardBadgeId");

        // Create indexes for MemberStreaks
        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_Member_Type_Store",
            table: "MemberStreaks",
            columns: new[] { "MemberId", "StreakType", "StoreId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_MemberId",
            table: "MemberStreaks",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_IsAtRisk",
            table: "MemberStreaks",
            column: "IsAtRisk");

        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_NextActivityDeadline",
            table: "MemberStreaks",
            column: "NextActivityDeadline");

        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_Active_Current_AtRisk",
            table: "MemberStreaks",
            columns: new[] { "IsActive", "CurrentStreak", "IsAtRisk" });

        migrationBuilder.CreateIndex(
            name: "IX_MemberStreaks_StoreId",
            table: "MemberStreaks",
            column: "StoreId");

        // Create indexes for StreakMilestones
        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_Streak_Milestone",
            table: "StreakMilestones",
            columns: new[] { "MemberStreakId", "MilestoneDefinitionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_MemberId",
            table: "StreakMilestones",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_AchievedAt",
            table: "StreakMilestones",
            column: "AchievedAt");

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_MemberStreakId",
            table: "StreakMilestones",
            column: "MemberStreakId");

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_MilestoneDefinitionId",
            table: "StreakMilestones",
            column: "MilestoneDefinitionId");

        migrationBuilder.CreateIndex(
            name: "IX_StreakMilestones_AwardedBadgeId",
            table: "StreakMilestones",
            column: "AwardedBadgeId");

        // Create indexes for GamificationConfigurations
        migrationBuilder.CreateIndex(
            name: "IX_GamificationConfigurations_StoreId",
            table: "GamificationConfigurations",
            column: "StoreId",
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // Seed default badges
        migrationBuilder.InsertData(
            table: "Badges",
            columns: new[] { "Name", "Description", "Category", "TriggerType", "Rarity", "PointsAwarded", "ThresholdValue", "DisplayOrder" },
            values: new object[,]
            {
                { "First Visit", "Welcome! Thanks for your first visit.", 1, 1, 1, 50, 1m, 1 },
                { "Regular", "You've visited us 5 times!", 1, 1, 2, 100, 5m, 2 },
                { "Loyal Customer", "10 visits! You're one of our favorites.", 1, 1, 3, 200, 10m, 3 },
                { "VIP", "25 visits! VIP status achieved.", 1, 1, 4, 500, 25m, 4 },
                { "Legend", "50 visits! You're a legend!", 1, 1, 5, 1000, 50m, 5 },
                { "Big Spender", "Spent over 10,000 KES", 2, 1, 2, 150, 10000m, 10 },
                { "High Roller", "Spent over 50,000 KES", 2, 1, 4, 500, 50000m, 11 },
                { "Social Butterfly", "Referred 3 friends", 4, 5, 3, 300, 3m, 20 },
                { "Influencer", "Referred 10 friends", 4, 5, 4, 750, 10m, 21 },
                { "Silver Member", "Reached Silver tier", 5, 1, 2, 100, 2m, 30 },
                { "Gold Member", "Reached Gold tier", 5, 1, 3, 250, 3m, 31 },
                { "Platinum Member", "Reached Platinum tier", 5, 1, 4, 500, 4m, 32 }
            });

        // Seed default streak milestone definitions
        migrationBuilder.InsertData(
            table: "StreakMilestoneDefinitions",
            columns: new[] { "StreakType", "StreakCount", "Name", "Description", "RewardPoints", "FreezeTokensAwarded", "DisplayOrder" },
            values: new object[,]
            {
                { 1, 3, "3-Day Streak", "Visited 3 days in a row!", 50, 0, 1 },
                { 1, 7, "Week Warrior", "A full week of visits!", 150, 1, 2 },
                { 1, 14, "Two Week Champion", "14 consecutive days!", 300, 1, 3 },
                { 1, 30, "Monthly Master", "30 days strong!", 750, 2, 4 },
                { 2, 2, "2-Week Run", "2 consecutive weeks!", 100, 0, 10 },
                { 2, 4, "Monthly Regular", "4 consecutive weeks!", 250, 1, 11 },
                { 2, 8, "Two Month Streak", "8 weeks running!", 500, 2, 12 },
                { 2, 12, "Quarterly Champion", "12 weeks in a row!", 1000, 3, 13 }
            });

        // Seed default gamification configuration
        migrationBuilder.InsertData(
            table: "GamificationConfigurations",
            columns: new[] { "StoreId", "IsEnabled", "BadgesEnabled", "ChallengesEnabled", "StreaksEnabled" },
            values: new object[] { null, true, true, true, true });

        // Seed sample challenges
        migrationBuilder.InsertData(
            table: "Challenges",
            columns: new[] { "Name", "Description", "Period", "GoalType", "TargetValue", "RewardPoints", "StartDate", "EndDate", "IsRecurring", "ShowLeaderboard" },
            values: new object[,]
            {
                { "Weekly Visit Challenge", "Visit 3 times this week", 2, 1, 3m, 100, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7), true, true },
                { "Monthly Spender", "Spend 5,000 KES this month", 3, 2, 5000m, 250, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30), true, true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GamificationConfigurations");
        migrationBuilder.DropTable(name: "StreakMilestones");
        migrationBuilder.DropTable(name: "MemberStreaks");
        migrationBuilder.DropTable(name: "StreakMilestoneDefinitions");
        migrationBuilder.DropTable(name: "MemberChallenges");
        migrationBuilder.DropTable(name: "Challenges");
        migrationBuilder.DropTable(name: "MemberBadges");
        migrationBuilder.DropTable(name: "Badges");
    }
}
