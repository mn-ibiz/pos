using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <summary>
/// Migration to add Kitchen Display System (KDS) tables.
/// </summary>
public partial class AddKdsTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create KdsDisplaySettings table first (no dependencies)
        migrationBuilder.CreateTable(
            name: "KdsDisplaySettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ColumnsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                FontSize = table.Column<int>(type: "int", nullable: false, defaultValue: 16),
                WarningThresholdMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                AlertThresholdMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                GreenThresholdMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                ShowModifiers = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ShowSpecialInstructions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                AudioAlerts = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                FlashWhenOverdue = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                FlashIntervalSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                AudioRepeatIntervalSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                RecallWindowMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                ThemeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsDisplaySettings", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_KdsDisplaySettings_IsActive", table: "KdsDisplaySettings", column: "IsActive");

        // Create KdsStations table
        migrationBuilder.CreateTable(
            name: "KdsStations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                StationType = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsExpo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                LastConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DisplaySettingsId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsStations", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsStations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_KdsStations_KdsDisplaySettings_DisplaySettingsId",
                    column: x => x.DisplaySettingsId,
                    principalTable: "KdsDisplaySettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_KdsStations_Name", table: "KdsStations", column: "Name");
        migrationBuilder.CreateIndex(name: "IX_KdsStations_StoreId", table: "KdsStations", column: "StoreId");
        migrationBuilder.CreateIndex(name: "IX_KdsStations_StationType", table: "KdsStations", column: "StationType");
        migrationBuilder.CreateIndex(name: "IX_KdsStations_Status", table: "KdsStations", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_KdsStations_IsActive", table: "KdsStations", column: "IsActive");

        // Create KdsStationCategories table
        migrationBuilder.CreateTable(
            name: "KdsStationCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StationId = table.Column<int>(type: "int", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsStationCategories", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsStationCategories_KdsStations_StationId",
                    column: x => x.StationId,
                    principalTable: "KdsStations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_KdsStationCategories_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_KdsStationCategories_StationId_CategoryId",
            table: "KdsStationCategories",
            columns: new[] { "StationId", "CategoryId" },
            unique: true);
        migrationBuilder.CreateIndex(name: "IX_KdsStationCategories_IsActive", table: "KdsStationCategories", column: "IsActive");

        // Create KdsOrders table
        migrationBuilder.CreateTable(
            name: "KdsOrders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                TableNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                GuestCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                Status = table.Column<int>(type: "int", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsPriority = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ServedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ServedByUserId = table.Column<int>(type: "int", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                FireOnDemandEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                FireOnDemandEnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FireOnDemandEnabledByUserId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsOrders", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsOrders_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_KdsOrders_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_KdsOrders_OrderId", table: "KdsOrders", column: "OrderId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrders_StoreId", table: "KdsOrders", column: "StoreId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrders_Status", table: "KdsOrders", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_KdsOrders_Priority", table: "KdsOrders", column: "Priority");
        migrationBuilder.CreateIndex(name: "IX_KdsOrders_ReceivedAt", table: "KdsOrders", column: "ReceivedAt");
        migrationBuilder.CreateIndex(name: "IX_KdsOrders_IsActive", table: "KdsOrders", column: "IsActive");

        // Create KdsOrderItems table
        migrationBuilder.CreateTable(
            name: "KdsOrderItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                KdsOrderId = table.Column<int>(type: "int", nullable: false),
                OrderItemId = table.Column<int>(type: "int", nullable: false),
                StationId = table.Column<int>(type: "int", nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Modifiers = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SpecialInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                ItemFireStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SequenceNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CourseNumber = table.Column<int>(type: "int", nullable: true),
                CourseStateId = table.Column<int>(type: "int", nullable: true),
                ScheduledFireAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FiredByUserId = table.Column<int>(type: "int", nullable: true),
                FireOnDemand = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                IsOnHold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                HeldAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                HeldByUserId = table.Column<int>(type: "int", nullable: true),
                HoldReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedByUserId = table.Column<int>(type: "int", nullable: true),
                EstimatedPrepTimeMinutes = table.Column<int>(type: "int", nullable: true),
                TargetReadyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsOrderItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsOrderItems_KdsOrders_KdsOrderId",
                    column: x => x.KdsOrderId,
                    principalTable: "KdsOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_KdsOrderItems_OrderItems_OrderItemId",
                    column: x => x.OrderItemId,
                    principalTable: "OrderItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_KdsOrderItems_KdsStations_StationId",
                    column: x => x.StationId,
                    principalTable: "KdsStations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_KdsOrderId", table: "KdsOrderItems", column: "KdsOrderId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_OrderItemId", table: "KdsOrderItems", column: "OrderItemId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_StationId", table: "KdsOrderItems", column: "StationId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_CourseNumber", table: "KdsOrderItems", column: "CourseNumber");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_Status", table: "KdsOrderItems", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_ItemFireStatus", table: "KdsOrderItems", column: "ItemFireStatus");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderItems_IsActive", table: "KdsOrderItems", column: "IsActive");

        // Create KdsOrderStatusLogs table
        migrationBuilder.CreateTable(
            name: "KdsOrderStatusLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                KdsOrderId = table.Column<int>(type: "int", nullable: false),
                PreviousStatus = table.Column<int>(type: "int", nullable: false),
                NewStatus = table.Column<int>(type: "int", nullable: false),
                ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsOrderStatusLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsOrderStatusLogs_KdsOrders_KdsOrderId",
                    column: x => x.KdsOrderId,
                    principalTable: "KdsOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_KdsOrderStatusLogs_Users_ChangedByUserId",
                    column: x => x.ChangedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_KdsOrderStatusLogs_KdsOrderId", table: "KdsOrderStatusLogs", column: "KdsOrderId");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderStatusLogs_ChangedAt", table: "KdsOrderStatusLogs", column: "ChangedAt");
        migrationBuilder.CreateIndex(name: "IX_KdsOrderStatusLogs_IsActive", table: "KdsOrderStatusLogs", column: "IsActive");

        // Create AllCallMessages table
        migrationBuilder.CreateTable(
            name: "AllCallMessages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false),
                SentByUserId = table.Column<int>(type: "int", nullable: false),
                SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsGlobal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllCallMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_AllCallMessages_Users_SentByUserId",
                    column: x => x.SentByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_AllCallMessages_SentByUserId", table: "AllCallMessages", column: "SentByUserId");
        migrationBuilder.CreateIndex(name: "IX_AllCallMessages_SentAt", table: "AllCallMessages", column: "SentAt");
        migrationBuilder.CreateIndex(name: "IX_AllCallMessages_ExpiresAt", table: "AllCallMessages", column: "ExpiresAt");
        migrationBuilder.CreateIndex(name: "IX_AllCallMessages_IsActive", table: "AllCallMessages", column: "IsActive");

        // Create AllCallMessageTargets table
        migrationBuilder.CreateTable(
            name: "AllCallMessageTargets",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AllCallMessageId = table.Column<int>(type: "int", nullable: false),
                KdsStationId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllCallMessageTargets", x => x.Id);
                table.ForeignKey(
                    name: "FK_AllCallMessageTargets_AllCallMessages_AllCallMessageId",
                    column: x => x.AllCallMessageId,
                    principalTable: "AllCallMessages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AllCallMessageTargets_KdsStations_KdsStationId",
                    column: x => x.KdsStationId,
                    principalTable: "KdsStations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AllCallMessageTargets_AllCallMessageId_KdsStationId",
            table: "AllCallMessageTargets",
            columns: new[] { "AllCallMessageId", "KdsStationId" },
            unique: true);
        migrationBuilder.CreateIndex(name: "IX_AllCallMessageTargets_IsActive", table: "AllCallMessageTargets", column: "IsActive");

        // Create AllCallMessageDismissals table
        migrationBuilder.CreateTable(
            name: "AllCallMessageDismissals",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AllCallMessageId = table.Column<int>(type: "int", nullable: false),
                KdsStationId = table.Column<int>(type: "int", nullable: false),
                DismissedByUserId = table.Column<int>(type: "int", nullable: true),
                DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllCallMessageDismissals", x => x.Id);
                table.ForeignKey(
                    name: "FK_AllCallMessageDismissals_AllCallMessages_AllCallMessageId",
                    column: x => x.AllCallMessageId,
                    principalTable: "AllCallMessages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AllCallMessageDismissals_KdsStations_KdsStationId",
                    column: x => x.KdsStationId,
                    principalTable: "KdsStations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AllCallMessageDismissals_Users_DismissedByUserId",
                    column: x => x.DismissedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AllCallMessageDismissals_AllCallMessageId_KdsStationId",
            table: "AllCallMessageDismissals",
            columns: new[] { "AllCallMessageId", "KdsStationId" },
            unique: true);
        migrationBuilder.CreateIndex(name: "IX_AllCallMessageDismissals_IsActive", table: "AllCallMessageDismissals", column: "IsActive");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AllCallMessageDismissals");
        migrationBuilder.DropTable(name: "AllCallMessageTargets");
        migrationBuilder.DropTable(name: "AllCallMessages");
        migrationBuilder.DropTable(name: "KdsOrderStatusLogs");
        migrationBuilder.DropTable(name: "KdsOrderItems");
        migrationBuilder.DropTable(name: "KdsOrders");
        migrationBuilder.DropTable(name: "KdsStationCategories");
        migrationBuilder.DropTable(name: "KdsStations");
        migrationBuilder.DropTable(name: "KdsDisplaySettings");
    }
}
