using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add terminal scope to work periods and create work period sessions table.
    /// </summary>
    public partial class AddWorkPeriodTerminalScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TerminalId and TerminalCode to WorkPeriods table
            migrationBuilder.AddColumn<int>(
                name: "TerminalId",
                table: "WorkPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminalCode",
                table: "WorkPeriods",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Create WorkPeriodSessions table
            migrationBuilder.CreateTable(
                name: "WorkPeriodSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPeriodId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SalesTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TransactionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CashReceived = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CashPaidOut = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    RefundTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    VoidTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CardTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    MpesaTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPeriodSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkPeriodSessions_WorkPeriods_WorkPeriodId",
                        column: x => x.WorkPeriodId,
                        principalTable: "WorkPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPeriodSessions_Terminals_TerminalId",
                        column: x => x.TerminalId,
                        principalTable: "Terminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPeriodSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Add foreign key from WorkPeriods to Terminals
            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriods_TerminalId",
                table: "WorkPeriods",
                column: "TerminalId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPeriods_Terminals_TerminalId",
                table: "WorkPeriods",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Indexes for WorkPeriodSessions
            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriodSessions_WorkPeriodId",
                table: "WorkPeriodSessions",
                column: "WorkPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriodSessions_TerminalId",
                table: "WorkPeriodSessions",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriodSessions_UserId",
                table: "WorkPeriodSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriodSessions_Active",
                table: "WorkPeriodSessions",
                columns: new[] { "TerminalId", "UserId", "LogoutAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriodSessions_TerminalId_LoginAt",
                table: "WorkPeriodSessions",
                columns: new[] { "TerminalId", "LoginAt" });

            // Index for terminal-based work period queries
            migrationBuilder.CreateIndex(
                name: "IX_WorkPeriods_TerminalId_Status",
                table: "WorkPeriods",
                columns: new[] { "TerminalId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkPeriodSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkPeriods_Terminals_TerminalId",
                table: "WorkPeriods");

            migrationBuilder.DropIndex(
                name: "IX_WorkPeriods_TerminalId",
                table: "WorkPeriods");

            migrationBuilder.DropIndex(
                name: "IX_WorkPeriods_TerminalId_Status",
                table: "WorkPeriods");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "WorkPeriods");

            migrationBuilder.DropColumn(
                name: "TerminalCode",
                table: "WorkPeriods");
        }
    }
}
