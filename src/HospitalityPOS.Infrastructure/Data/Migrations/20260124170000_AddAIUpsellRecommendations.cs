using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddAIUpsellRecommendations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create ProductAssociations table
        migrationBuilder.CreateTable(
            name: "ProductAssociations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                AssociatedProductId = table.Column<int>(type: "int", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                Support = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: false),
                Lift = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                TransactionCount = table.Column<int>(type: "int", nullable: false),
                CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                AnalysisStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                AnalysisEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductAssociations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductAssociations_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProductAssociations_Products_AssociatedProductId",
                    column: x => x.AssociatedProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProductAssociations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create UpsellRules table
        migrationBuilder.CreateTable(
            name: "UpsellRules",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SourceProductId = table.Column<int>(type: "int", nullable: true),
                SourceCategoryId = table.Column<int>(type: "int", nullable: true),
                TargetProductId = table.Column<int>(type: "int", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                SuggestionText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                SavingsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                MaxSuggestionsPerDay = table.Column<int>(type: "int", nullable: true),
                TodaySuggestionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LastCountResetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                TimeOfDayFilter = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpsellRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_UpsellRules_Products_SourceProductId",
                    column: x => x.SourceProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellRules_Categories_SourceCategoryId",
                    column: x => x.SourceCategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellRules_Products_TargetProductId",
                    column: x => x.TargetProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UpsellRules_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create UpsellSuggestionLogs table
        migrationBuilder.CreateTable(
            name: "UpsellSuggestionLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReceiptId = table.Column<int>(type: "int", nullable: false),
                SuggestedProductId = table.Column<int>(type: "int", nullable: false),
                AssociationId = table.Column<int>(type: "int", nullable: true),
                RuleId = table.Column<int>(type: "int", nullable: true),
                SuggestionType = table.Column<int>(type: "int", nullable: false),
                ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                WasAccepted = table.Column<bool>(type: "bit", nullable: true),
                SuggestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                OutcomeRecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                AcceptedQuantity = table.Column<int>(type: "int", nullable: true),
                AcceptedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                UserId = table.Column<int>(type: "int", nullable: true),
                CustomerId = table.Column<int>(type: "int", nullable: true),
                TriggerProductIds = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpsellSuggestionLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_Receipts_ReceiptId",
                    column: x => x.ReceiptId,
                    principalTable: "Receipts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_Products_SuggestedProductId",
                    column: x => x.SuggestedProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_ProductAssociations_AssociationId",
                    column: x => x.AssociationId,
                    principalTable: "ProductAssociations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_UpsellRules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "UpsellRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_LoyaltyMembers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_UpsellSuggestionLogs_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create CustomerPreferences table
        migrationBuilder.CreateTable(
            name: "CustomerPreferences",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                PurchaseCount = table.Column<int>(type: "int", nullable: false),
                TotalSpent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AverageQuantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                LastPurchased = table.Column<DateTime>(type: "datetime2", nullable: false),
                FirstPurchased = table.Column<DateTime>(type: "datetime2", nullable: false),
                PreferenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerPreferences", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerPreferences_LoyaltyMembers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CustomerPreferences_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CustomerPreferences_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create UpsellConfigurations table
        migrationBuilder.CreateTable(
            name: "UpsellConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                MaxSuggestions = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                MinConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.3m),
                MinSupport = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.01m),
                MinAssociationConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.25m),
                MinLift = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.2m),
                AnalysisDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                IncludePersonalized = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                IncludeTrending = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                EnforceCategoryDiversity = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ExcludeRecentPurchaseDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                RuleWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.5m),
                AssociationWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.0m),
                PersonalizedWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.2m),
                TrendingWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.8m),
                TrendingDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                ShowSavingsAmount = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                DefaultSuggestionText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "Customers also bought {{ProductName}}"),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpsellConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_UpsellConfigurations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create UpsellDailyMetrics table
        migrationBuilder.CreateTable(
            name: "UpsellDailyMetrics",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                TotalSuggestions = table.Column<int>(type: "int", nullable: false),
                AcceptedSuggestions = table.Column<int>(type: "int", nullable: false),
                RejectedSuggestions = table.Column<int>(type: "int", nullable: false),
                AcceptanceRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AverageValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RuleBasedRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AssociationBasedRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                PersonalizedRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                UniqueProductsSuggested = table.Column<int>(type: "int", nullable: false),
                UniqueProductsAccepted = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpsellDailyMetrics", x => x.Id);
                table.ForeignKey(
                    name: "FK_UpsellDailyMetrics_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for ProductAssociations
        migrationBuilder.CreateIndex(
            name: "IX_ProductAssociations_ProductId",
            table: "ProductAssociations",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductAssociations_AssociatedProductId",
            table: "ProductAssociations",
            column: "AssociatedProductId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductAssociations_Product_Associated",
            table: "ProductAssociations",
            columns: new[] { "ProductId", "AssociatedProductId" });

        migrationBuilder.CreateIndex(
            name: "IX_ProductAssociations_Active_Lift",
            table: "ProductAssociations",
            columns: new[] { "IsActive", "Lift" });

        migrationBuilder.CreateIndex(
            name: "IX_ProductAssociations_StoreId",
            table: "ProductAssociations",
            column: "StoreId");

        // Create indexes for UpsellRules
        migrationBuilder.CreateIndex(
            name: "IX_UpsellRules_SourceProductId",
            table: "UpsellRules",
            column: "SourceProductId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellRules_SourceCategoryId",
            table: "UpsellRules",
            column: "SourceCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellRules_TargetProductId",
            table: "UpsellRules",
            column: "TargetProductId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellRules_Active_Enabled_Priority",
            table: "UpsellRules",
            columns: new[] { "IsActive", "IsEnabled", "Priority" });

        migrationBuilder.CreateIndex(
            name: "IX_UpsellRules_StoreId",
            table: "UpsellRules",
            column: "StoreId");

        // Create indexes for UpsellSuggestionLogs
        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_ReceiptId",
            table: "UpsellSuggestionLogs",
            column: "ReceiptId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_SuggestedProductId",
            table: "UpsellSuggestionLogs",
            column: "SuggestedProductId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_SuggestedAt",
            table: "UpsellSuggestionLogs",
            column: "SuggestedAt");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_Date_Accepted",
            table: "UpsellSuggestionLogs",
            columns: new[] { "SuggestedAt", "WasAccepted" });

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_RuleId",
            table: "UpsellSuggestionLogs",
            column: "RuleId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_AssociationId",
            table: "UpsellSuggestionLogs",
            column: "AssociationId");

        migrationBuilder.CreateIndex(
            name: "IX_UpsellSuggestionLogs_StoreId",
            table: "UpsellSuggestionLogs",
            column: "StoreId");

        // Create indexes for CustomerPreferences
        migrationBuilder.CreateIndex(
            name: "IX_CustomerPreferences_Customer_Product",
            table: "CustomerPreferences",
            columns: new[] { "CustomerId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CustomerPreferences_Customer_Score",
            table: "CustomerPreferences",
            columns: new[] { "CustomerId", "PreferenceScore" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerPreferences_StoreId",
            table: "CustomerPreferences",
            column: "StoreId");

        // Create indexes for UpsellConfigurations
        migrationBuilder.CreateIndex(
            name: "IX_UpsellConfigurations_StoreId",
            table: "UpsellConfigurations",
            column: "StoreId",
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // Create indexes for UpsellDailyMetrics
        migrationBuilder.CreateIndex(
            name: "IX_UpsellDailyMetrics_Date_Store",
            table: "UpsellDailyMetrics",
            columns: new[] { "Date", "StoreId" },
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // Seed default configuration
        migrationBuilder.Sql(@"
            INSERT INTO UpsellConfigurations (
                StoreId, IsEnabled, MaxSuggestions, MinConfidenceScore, MinSupport,
                MinAssociationConfidence, MinLift, AnalysisDays, IncludePersonalized,
                IncludeTrending, EnforceCategoryDiversity, ExcludeRecentPurchaseDays,
                RuleWeight, AssociationWeight, PersonalizedWeight, TrendingWeight,
                TrendingDays, ShowSavingsAmount, DefaultSuggestionText, IsActive, CreatedAt
            )
            VALUES (
                NULL, 1, 3, 0.3, 0.01,
                0.25, 1.2, 90, 1,
                1, 1, 7,
                1.5, 1.0, 1.2, 0.8,
                7, 1, 'Customers also bought {{ProductName}}', 1, GETUTCDATE()
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UpsellDailyMetrics");
        migrationBuilder.DropTable(name: "UpsellConfigurations");
        migrationBuilder.DropTable(name: "CustomerPreferences");
        migrationBuilder.DropTable(name: "UpsellSuggestionLogs");
        migrationBuilder.DropTable(name: "UpsellRules");
        migrationBuilder.DropTable(name: "ProductAssociations");
    }
}
