using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddKdsPrepTiming : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PrepTimingConfigurations - Store-level configuration for prep timing
        migrationBuilder.CreateTable(
            name: "PrepTimingConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StoreId = table.Column<int>(type: "int", nullable: false),
                EnablePrepTiming = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DefaultPrepTimeSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 300),
                MinPrepTimeSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                TargetReadyBufferSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                AllowManualFireOverride = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ShowWaitingItemsOnStation = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Mode = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                AutoFireEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                OverdueThresholdSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                AlertOnOverdue = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PrepTimingConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_PrepTimingConfigurations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PrepTimingConfigurations_StoreId",
            table: "PrepTimingConfigurations",
            column: "StoreId",
            unique: true);

        // ItemFireSchedules - Schedule for when items should fire to stations
        migrationBuilder.CreateTable(
            name: "ItemFireSchedules",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                KdsOrderItemId = table.Column<int>(type: "int", nullable: false),
                KdsOrderId = table.Column<int>(type: "int", nullable: false),
                CourseNumber = table.Column<int>(type: "int", nullable: true),
                ProductId = table.Column<int>(type: "int", nullable: false),
                StationId = table.Column<int>(type: "int", nullable: true),
                PrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                OrderReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                TargetReadyAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ScheduledFireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ActualFiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ActualReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                WasManuallyFired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                FiredByUserId = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemFireSchedules", x => x.Id);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_KdsOrderItems_KdsOrderItemId",
                    column: x => x.KdsOrderItemId,
                    principalTable: "KdsOrderItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_KdsOrders_KdsOrderId",
                    column: x => x.KdsOrderId,
                    principalTable: "KdsOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_KdsStations_StationId",
                    column: x => x.StationId,
                    principalTable: "KdsStations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_Users_FiredByUserId",
                    column: x => x.FiredByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItemFireSchedules_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_KdsOrderItemId",
            table: "ItemFireSchedules",
            column: "KdsOrderItemId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_KdsOrderId",
            table: "ItemFireSchedules",
            column: "KdsOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_ProductId",
            table: "ItemFireSchedules",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_StationId",
            table: "ItemFireSchedules",
            column: "StationId");

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_FiredByUserId",
            table: "ItemFireSchedules",
            column: "FiredByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_StoreId_Status",
            table: "ItemFireSchedules",
            columns: new[] { "StoreId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_StationId_Status",
            table: "ItemFireSchedules",
            columns: new[] { "StationId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_Status_ScheduledFireAt",
            table: "ItemFireSchedules",
            columns: new[] { "Status", "ScheduledFireAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ItemFireSchedules_StoreId_TargetReadyAt",
            table: "ItemFireSchedules",
            columns: new[] { "StoreId", "TargetReadyAt" });

        // ProductPrepTimeConfigs - Product-specific prep times
        migrationBuilder.CreateTable(
            name: "ProductPrepTimeConfigs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                PrepTimeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                PrepTimeSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                UsesPrepTiming = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                IsTimingIntegral = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPrepTimeConfigs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductPrepTimeConfigs_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProductPrepTimeConfigs_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductPrepTimeConfigs_ProductId_StoreId",
            table: "ProductPrepTimeConfigs",
            columns: new[] { "ProductId", "StoreId" },
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // ModifierPrepTimeAdjustments - Modifier time adjustments
        migrationBuilder.CreateTable(
            name: "ModifierPrepTimeAdjustments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ModifierItemId = table.Column<int>(type: "int", nullable: false),
                AdjustmentSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                AdjustmentType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModifierPrepTimeAdjustments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ModifierPrepTimeAdjustments_ModifierItems_ModifierItemId",
                    column: x => x.ModifierItemId,
                    principalTable: "ModifierItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ModifierPrepTimeAdjustments_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ModifierPrepTimeAdjustments_ModifierItemId_StoreId",
            table: "ModifierPrepTimeAdjustments",
            columns: new[] { "ModifierItemId", "StoreId" },
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // CategoryPrepTimeDefaults - Category-level default prep times
        migrationBuilder.CreateTable(
            name: "CategoryPrepTimeDefaults",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                DefaultPrepTimeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                DefaultPrepTimeSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryPrepTimeDefaults", x => x.Id);
                table.ForeignKey(
                    name: "FK_CategoryPrepTimeDefaults_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CategoryPrepTimeDefaults_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryPrepTimeDefaults_CategoryId_StoreId",
            table: "CategoryPrepTimeDefaults",
            columns: new[] { "CategoryId", "StoreId" },
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // PrepTimingDailyMetrics - Daily accuracy metrics
        migrationBuilder.CreateTable(
            name: "PrepTimingDailyMetrics",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                TotalItemsScheduled = table.Column<int>(type: "int", nullable: false),
                ItemsFiredOnTime = table.Column<int>(type: "int", nullable: false),
                ItemsFiredLate = table.Column<int>(type: "int", nullable: false),
                ItemsManuallyFired = table.Column<int>(type: "int", nullable: false),
                ItemsCompletedOnTarget = table.Column<int>(type: "int", nullable: false),
                ItemsCompletedEarly = table.Column<int>(type: "int", nullable: false),
                ItemsCompletedLate = table.Column<int>(type: "int", nullable: false),
                AverageDeviationSeconds = table.Column<int>(type: "int", nullable: false),
                AccuracyRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PrepTimingDailyMetrics", x => x.Id);
                table.ForeignKey(
                    name: "FK_PrepTimingDailyMetrics_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PrepTimingDailyMetrics_StoreId_Date",
            table: "PrepTimingDailyMetrics",
            columns: new[] { "StoreId", "Date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PrepTimingDailyMetrics_Date",
            table: "PrepTimingDailyMetrics",
            column: "Date");

        // ProductPrepTimeAccuracies - Product-level accuracy tracking
        migrationBuilder.CreateTable(
            name: "ProductPrepTimeAccuracies",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                ConfiguredPrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                AverageActualPrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                SampleCount = table.Column<int>(type: "int", nullable: false),
                StandardDeviationSeconds = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                MinPrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                MaxPrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                SuggestedPrepTimeSeconds = table.Column<int>(type: "int", nullable: false),
                AccuracyRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPrepTimeAccuracies", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductPrepTimeAccuracies_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProductPrepTimeAccuracies_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductPrepTimeAccuracies_ProductId_StoreId",
            table: "ProductPrepTimeAccuracies",
            columns: new[] { "ProductId", "StoreId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductPrepTimeAccuracies_StoreId_AccuracyRate",
            table: "ProductPrepTimeAccuracies",
            columns: new[] { "StoreId", "AccuracyRate" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductPrepTimeAccuracies");
        migrationBuilder.DropTable(name: "PrepTimingDailyMetrics");
        migrationBuilder.DropTable(name: "CategoryPrepTimeDefaults");
        migrationBuilder.DropTable(name: "ModifierPrepTimeAdjustments");
        migrationBuilder.DropTable(name: "ProductPrepTimeConfigs");
        migrationBuilder.DropTable(name: "ItemFireSchedules");
        migrationBuilder.DropTable(name: "PrepTimingConfigurations");
    }
}
