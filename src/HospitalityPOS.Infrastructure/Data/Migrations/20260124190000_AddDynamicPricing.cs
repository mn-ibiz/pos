using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddDynamicPricing : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DynamicPricingConfigurations - Store-level configuration
        migrationBuilder.CreateTable(
            name: "DynamicPricingConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StoreId = table.Column<int>(type: "int", nullable: false),
                EnableDynamicPricing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                RequireManagerApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                MaxPriceIncreasePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 25m),
                MaxPriceDecreasePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 50m),
                PriceUpdateIntervalMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                ShowOriginalPrice = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                NotifyOnPriceChange = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                MinMarginPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 10m),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPricingConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPricingConfigurations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingConfigurations_StoreId",
            table: "DynamicPricingConfigurations",
            column: "StoreId",
            unique: true);

        // DynamicPricingRules - Pricing rules
        migrationBuilder.CreateTable(
            name: "DynamicPricingRules",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Trigger = table.Column<int>(type: "int", nullable: false),
                AdjustmentType = table.Column<int>(type: "int", nullable: false),
                AdjustmentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                MinPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                MaxPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                ProductId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                AppliesToAllProducts = table.Column<bool>(type: "bit", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                ActiveFromTime = table.Column<TimeOnly>(type: "time", nullable: true),
                ActiveToTime = table.Column<TimeOnly>(type: "time", nullable: true),
                ActiveDays = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                DemandThresholdHigh = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                DemandThresholdLow = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                StockThresholdLow = table.Column<int>(type: "int", nullable: true),
                StockThresholdHigh = table.Column<int>(type: "int", nullable: true),
                DaysToExpiry = table.Column<int>(type: "int", nullable: true),
                WeatherCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPricingRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPricingRules_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPricingRules_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPricingRules_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPricingRules_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_IsActive",
            table: "DynamicPricingRules",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_StoreId_IsActive",
            table: "DynamicPricingRules",
            columns: new[] { "StoreId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_ProductId_IsActive",
            table: "DynamicPricingRules",
            columns: new[] { "ProductId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_CategoryId_IsActive",
            table: "DynamicPricingRules",
            columns: new[] { "CategoryId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_Priority",
            table: "DynamicPricingRules",
            column: "Priority");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRules_CreatedByUserId",
            table: "DynamicPricingRules",
            column: "CreatedByUserId");

        // DynamicPricingExceptions - Product exceptions from rules
        migrationBuilder.CreateTable(
            name: "DynamicPricingExceptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RuleId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPricingExceptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPricingExceptions_DynamicPricingRules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "DynamicPricingRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPricingExceptions_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingExceptions_RuleId_ProductId",
            table: "DynamicPricingExceptions",
            columns: new[] { "RuleId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingExceptions_ProductId",
            table: "DynamicPricingExceptions",
            column: "ProductId");

        // DynamicPriceLogs - Price change history
        migrationBuilder.CreateTable(
            name: "DynamicPriceLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                RuleId = table.Column<int>(type: "int", nullable: true),
                OriginalPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                AdjustedPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                AdjustmentAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                AdjustmentPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPriceLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPriceLogs_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPriceLogs_DynamicPricingRules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "DynamicPricingRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPriceLogs_Users_ApprovedByUserId",
                    column: x => x.ApprovedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPriceLogs_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPriceLogs_ProductId",
            table: "DynamicPriceLogs",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPriceLogs_RuleId",
            table: "DynamicPriceLogs",
            column: "RuleId");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPriceLogs_AppliedAt",
            table: "DynamicPriceLogs",
            column: "AppliedAt");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPriceLogs_StoreId_AppliedAt",
            table: "DynamicPriceLogs",
            columns: new[] { "StoreId", "AppliedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPriceLogs_ApprovedByUserId",
            table: "DynamicPriceLogs",
            column: "ApprovedByUserId");

        // PendingPriceChanges - Approval workflow
        migrationBuilder.CreateTable(
            name: "PendingPriceChanges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                RuleId = table.Column<int>(type: "int", nullable: true),
                CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                ProposedPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PendingPriceChanges", x => x.Id);
                table.ForeignKey(
                    name: "FK_PendingPriceChanges_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PendingPriceChanges_DynamicPricingRules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "DynamicPricingRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PendingPriceChanges_Users_RequestedByUserId",
                    column: x => x.RequestedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PendingPriceChanges_Users_ReviewedByUserId",
                    column: x => x.ReviewedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PendingPriceChanges_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_Status",
            table: "PendingPriceChanges",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_StoreId_Status",
            table: "PendingPriceChanges",
            columns: new[] { "StoreId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_ExpiresAt",
            table: "PendingPriceChanges",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_ProductId",
            table: "PendingPriceChanges",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_RuleId",
            table: "PendingPriceChanges",
            column: "RuleId");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_RequestedByUserId",
            table: "PendingPriceChanges",
            column: "RequestedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPriceChanges_ReviewedByUserId",
            table: "PendingPriceChanges",
            column: "ReviewedByUserId");

        // CurrentDynamicPrices - Cached current prices
        migrationBuilder.CreateTable(
            name: "CurrentDynamicPrices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                AppliedRuleId = table.Column<int>(type: "int", nullable: true),
                CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsAdjusted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CurrentDynamicPrices", x => x.Id);
                table.ForeignKey(
                    name: "FK_CurrentDynamicPrices_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CurrentDynamicPrices_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CurrentDynamicPrices_DynamicPricingRules_AppliedRuleId",
                    column: x => x.AppliedRuleId,
                    principalTable: "DynamicPricingRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CurrentDynamicPrices_ProductId_StoreId",
            table: "CurrentDynamicPrices",
            columns: new[] { "ProductId", "StoreId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CurrentDynamicPrices_ExpiresAt",
            table: "CurrentDynamicPrices",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_CurrentDynamicPrices_AppliedRuleId",
            table: "CurrentDynamicPrices",
            column: "AppliedRuleId");

        // DynamicPricingDailyMetrics - Daily analytics
        migrationBuilder.CreateTable(
            name: "DynamicPricingDailyMetrics",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                ProductsWithDynamicPricing = table.Column<int>(type: "int", nullable: false),
                TotalPriceChanges = table.Column<int>(type: "int", nullable: false),
                PriceIncreases = table.Column<int>(type: "int", nullable: false),
                PriceDecreases = table.Column<int>(type: "int", nullable: false),
                AverageAdjustmentPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                EstimatedRevenueImpact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                ActiveRulesCount = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPricingDailyMetrics", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPricingDailyMetrics_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingDailyMetrics_StoreId_Date",
            table: "DynamicPricingDailyMetrics",
            columns: new[] { "StoreId", "Date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingDailyMetrics_Date",
            table: "DynamicPricingDailyMetrics",
            column: "Date");

        // DynamicPricingRuleMetrics - Rule-level analytics
        migrationBuilder.CreateTable(
            name: "DynamicPricingRuleMetrics",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RuleId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                TimesApplied = table.Column<int>(type: "int", nullable: false),
                ProductsAffected = table.Column<int>(type: "int", nullable: false),
                TotalSalesValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                ItemsSold = table.Column<int>(type: "int", nullable: false),
                EstimatedRevenueImpact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DynamicPricingRuleMetrics", x => x.Id);
                table.ForeignKey(
                    name: "FK_DynamicPricingRuleMetrics_DynamicPricingRules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "DynamicPricingRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DynamicPricingRuleMetrics_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRuleMetrics_RuleId_StoreId_Date",
            table: "DynamicPricingRuleMetrics",
            columns: new[] { "RuleId", "StoreId", "Date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRuleMetrics_Date",
            table: "DynamicPricingRuleMetrics",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_DynamicPricingRuleMetrics_StoreId",
            table: "DynamicPricingRuleMetrics",
            column: "StoreId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DynamicPricingRuleMetrics");
        migrationBuilder.DropTable(name: "DynamicPricingDailyMetrics");
        migrationBuilder.DropTable(name: "CurrentDynamicPrices");
        migrationBuilder.DropTable(name: "PendingPriceChanges");
        migrationBuilder.DropTable(name: "DynamicPriceLogs");
        migrationBuilder.DropTable(name: "DynamicPricingExceptions");
        migrationBuilder.DropTable(name: "DynamicPricingRules");
        migrationBuilder.DropTable(name: "DynamicPricingConfigurations");
    }
}
