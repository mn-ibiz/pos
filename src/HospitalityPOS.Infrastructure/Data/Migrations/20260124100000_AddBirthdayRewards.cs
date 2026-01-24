using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBirthdayRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DateOfBirth column to LoyaltyMembers
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "LoyaltyMembers",
                type: "date",
                nullable: true);

            // Create OneTimeRewards table
            migrationBuilder.CreateTable(
                name: "OneTimeRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumTier = table.Column<int>(type: "int", nullable: true),
                    ValidityDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MinimumPurchaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FreeItemProductId = table.Column<int>(type: "int", nullable: true),
                    SmsTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SendSmsNotification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SendEmailNotification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DaysBeforeToIssue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DaysAfterEventValid = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeRewards_Products_FreeItemProductId",
                        column: x => x.FreeItemProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create MemberRewards table
            migrationBuilder.CreateTable(
                name: "MemberRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoyaltyMemberId = table.Column<int>(type: "int", nullable: false),
                    OneTimeRewardId = table.Column<int>(type: "int", nullable: false),
                    RedemptionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 2), // Active
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedeemedOnReceiptId = table.Column<int>(type: "int", nullable: true),
                    RedeemedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PointsAwarded = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RewardYear = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SmsNotificationSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SmsNotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailNotificationSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EmailNotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberRewards_LoyaltyMembers_LoyaltyMemberId",
                        column: x => x.LoyaltyMemberId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberRewards_OneTimeRewards_OneTimeRewardId",
                        column: x => x.OneTimeRewardId,
                        principalTable: "OneTimeRewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberRewards_Receipts_RedeemedOnReceiptId",
                        column: x => x.RedeemedOnReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes for OneTimeRewards
            migrationBuilder.CreateIndex(
                name: "IX_OneTimeRewards_RewardType",
                table: "OneTimeRewards",
                column: "RewardType");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeRewards_IsActive",
                table: "OneTimeRewards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeRewards_Type_Active",
                table: "OneTimeRewards",
                columns: new[] { "RewardType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeRewards_FreeItemProductId",
                table: "OneTimeRewards",
                column: "FreeItemProductId");

            // Create indexes for MemberRewards
            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_RedemptionCode",
                table: "MemberRewards",
                column: "RedemptionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_LoyaltyMemberId",
                table: "MemberRewards",
                column: "LoyaltyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_Status",
                table: "MemberRewards",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_ExpiresAt",
                table: "MemberRewards",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_Member_Reward_Year",
                table: "MemberRewards",
                columns: new[] { "LoyaltyMemberId", "OneTimeRewardId", "RewardYear" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_Member_Status",
                table: "MemberRewards",
                columns: new[] { "LoyaltyMemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_OneTimeRewardId",
                table: "MemberRewards",
                column: "OneTimeRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRewards_RedeemedOnReceiptId",
                table: "MemberRewards",
                column: "RedeemedOnReceiptId");

            // Seed default birthday reward template
            migrationBuilder.InsertData(
                table: "OneTimeRewards",
                columns: new[]
                {
                    "Name", "Description", "RewardType", "ValueType", "Value",
                    "ValidityDays", "SmsTemplate", "SendSmsNotification", "SendEmailNotification",
                    "DaysBeforeToIssue", "DaysAfterEventValid", "IsActive", "CreatedAt"
                },
                values: new object[]
                {
                    "Birthday Reward",
                    "Automatic birthday reward for loyalty members",
                    1, // Birthday
                    1, // FixedPoints
                    100m, // 100 bonus points
                    30, // Valid for 30 days
                    "Happy Birthday {Name}! Enjoy {Value} bonus points as our gift. Use code {Code} before {ExpiryDate}. Thank you for being a valued customer!",
                    true,
                    true,
                    0, // Issue on birthday
                    7, // Valid 7 days after birthday
                    true,
                    DateTime.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberRewards");

            migrationBuilder.DropTable(
                name: "OneTimeRewards");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "LoyaltyMembers");
        }
    }
}
