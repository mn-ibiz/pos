using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPointsMultipliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PointsMultiplier column to Products
            migrationBuilder.AddColumn<decimal>(
                name: "PointsMultiplier",
                table: "Products",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromLoyaltyPoints",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Add PointsMultiplier column to Categories
            migrationBuilder.AddColumn<decimal>(
                name: "PointsMultiplier",
                table: "Categories",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromLoyaltyPoints",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Create PointsMultiplierRules table
            migrationBuilder.CreateTable(
                name: "PointsMultiplierRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    IsStackable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    MinimumTier = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    MinimumPurchaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinimumQuantity = table.Column<int>(type: "int", nullable: true),
                    MaxBonusPointsPerTransaction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxTotalUsages = table.Column<int>(type: "int", nullable: true),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxUsagesPerMember = table.Column<int>(type: "int", nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsMultiplierRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierRules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierRules_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create PointsMultiplierUsages table
            migrationBuilder.CreateTable(
                name: "PointsMultiplierUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointsMultiplierRuleId = table.Column<int>(type: "int", nullable: false),
                    LoyaltyMemberId = table.Column<int>(type: "int", nullable: false),
                    LoyaltyTransactionId = table.Column<int>(type: "int", nullable: true),
                    ReceiptId = table.Column<int>(type: "int", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    BasePoints = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusPointsEarned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MultiplierApplied = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsMultiplierUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierUsages_PointsMultiplierRules_PointsMultiplierRuleId",
                        column: x => x.PointsMultiplierRuleId,
                        principalTable: "PointsMultiplierRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierUsages_LoyaltyMembers_LoyaltyMemberId",
                        column: x => x.LoyaltyMemberId,
                        principalTable: "LoyaltyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointsMultiplierUsages_LoyaltyTransactions_LoyaltyTransactionId",
                        column: x => x.LoyaltyTransactionId,
                        principalTable: "LoyaltyTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes for PointsMultiplierRules
            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_RuleType",
                table: "PointsMultiplierRules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_IsActive",
                table: "PointsMultiplierRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_ProductId",
                table: "PointsMultiplierRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_CategoryId",
                table: "PointsMultiplierRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_StoreId",
                table: "PointsMultiplierRules",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_Priority",
                table: "PointsMultiplierRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_DateRange",
                table: "PointsMultiplierRules",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierRules_Active_Type_Priority",
                table: "PointsMultiplierRules",
                columns: new[] { "IsActive", "RuleType", "Priority" });

            // Create indexes for PointsMultiplierUsages
            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierUsages_RuleId",
                table: "PointsMultiplierUsages",
                column: "PointsMultiplierRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierUsages_MemberId",
                table: "PointsMultiplierUsages",
                column: "LoyaltyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierUsages_TransactionId",
                table: "PointsMultiplierUsages",
                column: "LoyaltyTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierUsages_UsedAt",
                table: "PointsMultiplierUsages",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PointsMultiplierUsages_Rule_Member",
                table: "PointsMultiplierUsages",
                columns: new[] { "PointsMultiplierRuleId", "LoyaltyMemberId" });

            // Create index for Products.PointsMultiplier
            migrationBuilder.CreateIndex(
                name: "IX_Products_PointsMultiplier",
                table: "Products",
                column: "PointsMultiplier");

            // Create index for Categories.PointsMultiplier
            migrationBuilder.CreateIndex(
                name: "IX_Categories_PointsMultiplier",
                table: "Categories",
                column: "PointsMultiplier");

            // Insert sample promotional rules for demonstration
            migrationBuilder.InsertData(
                table: "PointsMultiplierRules",
                columns: new[] { "Name", "Description", "RuleType", "Multiplier", "IsStackable", "Priority", "DaysOfWeek", "IsActive", "CreatedAt" },
                values: new object[] { "Double Points Tuesdays", "Earn double points on all purchases every Tuesday", 5, 2.0m, false, 100, "Tuesday", true, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "PointsMultiplierRules",
                columns: new[] { "Name", "Description", "RuleType", "Multiplier", "IsStackable", "Priority", "StartTime", "EndTime", "IsActive", "CreatedAt" },
                values: new object[] { "Happy Hour Bonus", "1.5x points during happy hour (4PM-7PM)", 6, 1.5m, true, 50, new TimeOnly(16, 0), new TimeOnly(19, 0), true, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "PointsMultiplierRules",
                columns: new[] { "Name", "Description", "RuleType", "Multiplier", "MinimumTier", "IsStackable", "Priority", "IsActive", "CreatedAt" },
                values: new object[] { "VIP Gold Bonus", "Extra 25% points for Gold members", 4, 1.25m, 2, true, 75, true, DateTime.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop PointsMultiplierUsages table
            migrationBuilder.DropTable(name: "PointsMultiplierUsages");

            // Drop PointsMultiplierRules table
            migrationBuilder.DropTable(name: "PointsMultiplierRules");

            // Drop indexes
            migrationBuilder.DropIndex(name: "IX_Products_PointsMultiplier", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Categories_PointsMultiplier", table: "Categories");

            // Remove columns from Products
            migrationBuilder.DropColumn(name: "PointsMultiplier", table: "Products");
            migrationBuilder.DropColumn(name: "ExcludeFromLoyaltyPoints", table: "Products");

            // Remove columns from Categories
            migrationBuilder.DropColumn(name: "PointsMultiplier", table: "Categories");
            migrationBuilder.DropColumn(name: "ExcludeFromLoyaltyPoints", table: "Categories");
        }
    }
}
