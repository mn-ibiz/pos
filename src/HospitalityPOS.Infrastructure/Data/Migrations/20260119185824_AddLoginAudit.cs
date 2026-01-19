using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: DefaultTaxRate, KraPinNumber, VatRegistrationNumber columns already exist in SystemConfigurations
            // These were added manually or in a prior migration, so we skip adding them here

            migrationBuilder.CreateTable(
                name: "LoginAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLogout = table.Column<bool>(type: "bit", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAudits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(2239));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4012));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4015));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4018));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4019));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4021));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4023));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4024));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 574, DateTimeKind.Utc).AddTicks(4026));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(196));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(2663));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(3427));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(3432));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(3435));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 675, DateTimeKind.Utc).AddTicks(3438));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 849, DateTimeKind.Utc).AddTicks(9628));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(578));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(580));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(582));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(583));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(584));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 19, 18, 58, 20, 850, DateTimeKind.Utc).AddTicks(585));

            migrationBuilder.CreateIndex(
                name: "IX_LoginAudits_UserId",
                table: "LoginAudits",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAudits");

            // Note: Keeping DefaultTaxRate, KraPinNumber, VatRegistrationNumber columns in SystemConfigurations

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(3091));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4949));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4952));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4954));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4956));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4957));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4959));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4960));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 59, DateTimeKind.Utc).AddTicks(4962));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(4964));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(7355));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(8152));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(8157));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(8160));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 152, DateTimeKind.Utc).AddTicks(8163));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(6280));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7370));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7373));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7374));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7375));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7377));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 18, 12, 31, 10, 310, DateTimeKind.Utc).AddTicks(7378));
        }
    }
}
