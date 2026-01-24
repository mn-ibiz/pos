using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add referral program tables (referral codes, referrals, configurations, milestones).
    /// </summary>
    public partial class AddReferralProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ReferralCodes table
            migrationBuilder.CreateTable(
                name: "ReferralCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShareableUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalPointsEarned = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralCodes_LoyaltyMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Referrals table
            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerId = table.Column<int>(type: "int", nullable: false),
                    RefereeId = table.Column<int>(type: "int", nullable: false),
                    ReferralCodeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReferrerBonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RefereeBonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReferredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QualifyingReceiptId = table.Column<int>(type: "int", nullable: true),
                    QualifyingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_LoyaltyMembers_ReferrerId",
                        column: x => x.ReferrerId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_LoyaltyMembers_RefereeId",
                        column: x => x.RefereeId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_ReferralCodes_ReferralCodeId",
                        column: x => x.ReferralCodeId,
                        principalTable: "ReferralCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Receipts_QualifyingReceiptId",
                        column: x => x.QualifyingReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // ReferralConfigurations table
            migrationBuilder.CreateTable(
                name: "ReferralConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    ReferrerBonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 500),
                    RefereeBonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 200),
                    MinPurchaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 500m),
                    ExpiryDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MaxReferralsPerMember = table.Column<int>(type: "int", nullable: true),
                    EnableLeaderboard = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequireNewMember = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsProgramActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReferrerSmsTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefereeSmsTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShareableLinkBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralConfigurations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // ReferralMilestones table
            migrationBuilder.CreateTable(
                name: "ReferralMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferralCount = table.Column<int>(type: "int", nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BadgeIcon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralMilestones", x => x.Id);
                });

            // MemberReferralMilestones table
            migrationBuilder.CreateTable(
                name: "MemberReferralMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    BonusPointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReferralCountAtAchievement = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberReferralMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberReferralMilestones_LoyaltyMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberReferralMilestones_ReferralMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "ReferralMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Indexes for ReferralCodes
            migrationBuilder.CreateIndex(
                name: "IX_ReferralCodes_Code",
                table: "ReferralCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralCodes_MemberId",
                table: "ReferralCodes",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralCodes_IsActive",
                table: "ReferralCodes",
                column: "IsActive");

            // Indexes for Referrals
            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerId",
                table: "Referrals",
                column: "ReferrerId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_RefereeId",
                table: "Referrals",
                column: "RefereeId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status",
                table: "Referrals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ExpiresAt",
                table: "Referrals",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralCodeId",
                table: "Referrals",
                column: "ReferralCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status_ExpiresAt",
                table: "Referrals",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Referrer_Status",
                table: "Referrals",
                columns: new[] { "ReferrerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_QualifyingReceiptId",
                table: "Referrals",
                column: "QualifyingReceiptId");

            // Indexes for ReferralConfigurations
            migrationBuilder.CreateIndex(
                name: "IX_ReferralConfigurations_StoreId",
                table: "ReferralConfigurations",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralConfigurations_IsActive",
                table: "ReferralConfigurations",
                column: "IsActive");

            // Indexes for ReferralMilestones
            migrationBuilder.CreateIndex(
                name: "IX_ReferralMilestones_ReferralCount",
                table: "ReferralMilestones",
                column: "ReferralCount",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralMilestones_SortOrder",
                table: "ReferralMilestones",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralMilestones_IsActive",
                table: "ReferralMilestones",
                column: "IsActive");

            // Indexes for MemberReferralMilestones
            migrationBuilder.CreateIndex(
                name: "IX_MemberReferralMilestones_MemberId",
                table: "MemberReferralMilestones",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberReferralMilestones_MilestoneId",
                table: "MemberReferralMilestones",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberReferralMilestones_Member_Milestone",
                table: "MemberReferralMilestones",
                columns: new[] { "MemberId", "MilestoneId" },
                unique: true);

            // Seed default configuration
            migrationBuilder.InsertData(
                table: "ReferralConfigurations",
                columns: new[] { "StoreId", "ReferrerBonusPoints", "RefereeBonusPoints", "MinPurchaseAmount", "ExpiryDays", "MaxReferralsPerMember", "EnableLeaderboard", "RequireNewMember", "IsProgramActive", "ReferrerSmsTemplate", "RefereeSmsTemplate", "ShareableLinkBaseUrl", "CreatedAt", "IsActive" },
                values: new object[] { null, 500, 200, 500m, 30, null, true, true, true, "Congratulations! Your referral {RefereeName} made their first purchase. You earned {Points} bonus points!", "Welcome! Thanks to {ReferrerName}'s referral, you earned {Points} bonus points on your first purchase!", null, DateTime.UtcNow, true });

            // Seed default milestones
            migrationBuilder.InsertData(
                table: "ReferralMilestones",
                columns: new[] { "Name", "Description", "ReferralCount", "BonusPoints", "BadgeIcon", "SortOrder", "CreatedAt", "IsActive" },
                values: new object[,]
                {
                    { "First Referral", "Successfully referred your first friend", 1, 100, "badge-first-referral", 1, DateTime.UtcNow, true },
                    { "Social Butterfly", "Referred 5 friends", 5, 500, "badge-social-butterfly", 2, DateTime.UtcNow, true },
                    { "Referral Champion", "Referred 10 friends", 10, 1000, "badge-referral-champion", 3, DateTime.UtcNow, true },
                    { "Ambassador", "Referred 25 friends", 25, 2500, "badge-ambassador", 4, DateTime.UtcNow, true },
                    { "Referral Legend", "Referred 50 friends", 50, 5000, "badge-legend", 5, DateTime.UtcNow, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberReferralMilestones");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "ReferralConfigurations");

            migrationBuilder.DropTable(
                name: "ReferralMilestones");

            migrationBuilder.DropTable(
                name: "ReferralCodes");
        }
    }
}
