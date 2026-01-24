using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKdsFireOnDemand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Fire on Demand columns to KdsOrders
            migrationBuilder.AddColumn<bool>(
                name: "FireOnDemandEnabled",
                table: "KdsOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FireOnDemandEnabledAt",
                table: "KdsOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FireOnDemandEnabledByUserId",
                table: "KdsOrders",
                type: "int",
                nullable: true);

            // Add Hold/Fire columns to KdsOrderItems
            migrationBuilder.AddColumn<bool>(
                name: "FireOnDemand",
                table: "KdsOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnHold",
                table: "KdsOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HeldAt",
                table: "KdsOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeldByUserId",
                table: "KdsOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoldReason",
                table: "KdsOrderItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiredAt",
                table: "KdsOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiredByUserId",
                table: "KdsOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedPrepTimeMinutes",
                table: "KdsOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetReadyTime",
                table: "KdsOrderItems",
                type: "datetime2",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_KdsOrderItems_IsOnHold",
                table: "KdsOrderItems",
                column: "IsOnHold");

            migrationBuilder.CreateIndex(
                name: "IX_KdsOrderItems_Status_IsOnHold",
                table: "KdsOrderItems",
                columns: new[] { "Status", "IsOnHold" });

            migrationBuilder.CreateIndex(
                name: "IX_KdsOrders_FireOnDemandEnabled",
                table: "KdsOrders",
                column: "FireOnDemandEnabled");

            // Add foreign key for FireOnDemandEnabledByUserId
            migrationBuilder.CreateIndex(
                name: "IX_KdsOrders_FireOnDemandEnabledByUserId",
                table: "KdsOrders",
                column: "FireOnDemandEnabledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KdsOrders_Users_FireOnDemandEnabledByUserId",
                table: "KdsOrders",
                column: "FireOnDemandEnabledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Add foreign keys for HeldByUserId and FiredByUserId
            migrationBuilder.CreateIndex(
                name: "IX_KdsOrderItems_HeldByUserId",
                table: "KdsOrderItems",
                column: "HeldByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KdsOrderItems_FiredByUserId",
                table: "KdsOrderItems",
                column: "FiredByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KdsOrderItems_Users_HeldByUserId",
                table: "KdsOrderItems",
                column: "HeldByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_KdsOrderItems_Users_FiredByUserId",
                table: "KdsOrderItems",
                column: "FiredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_KdsOrders_Users_FireOnDemandEnabledByUserId",
                table: "KdsOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_KdsOrderItems_Users_HeldByUserId",
                table: "KdsOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_KdsOrderItems_Users_FiredByUserId",
                table: "KdsOrderItems");

            // Remove indexes
            migrationBuilder.DropIndex(
                name: "IX_KdsOrderItems_IsOnHold",
                table: "KdsOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_KdsOrderItems_Status_IsOnHold",
                table: "KdsOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_KdsOrders_FireOnDemandEnabled",
                table: "KdsOrders");

            migrationBuilder.DropIndex(
                name: "IX_KdsOrders_FireOnDemandEnabledByUserId",
                table: "KdsOrders");

            migrationBuilder.DropIndex(
                name: "IX_KdsOrderItems_HeldByUserId",
                table: "KdsOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_KdsOrderItems_FiredByUserId",
                table: "KdsOrderItems");

            // Remove columns from KdsOrderItems
            migrationBuilder.DropColumn(name: "FireOnDemand", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "IsOnHold", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "HeldAt", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "HeldByUserId", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "HoldReason", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "FiredAt", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "FiredByUserId", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "EstimatedPrepTimeMinutes", table: "KdsOrderItems");
            migrationBuilder.DropColumn(name: "TargetReadyTime", table: "KdsOrderItems");

            // Remove columns from KdsOrders
            migrationBuilder.DropColumn(name: "FireOnDemandEnabled", table: "KdsOrders");
            migrationBuilder.DropColumn(name: "FireOnDemandEnabledAt", table: "KdsOrders");
            migrationBuilder.DropColumn(name: "FireOnDemandEnabledByUserId", table: "KdsOrders");
        }
    }
}
