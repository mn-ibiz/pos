using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to update ZReports table with terminal scope support.
    /// Adds formatted report number, terminal code, and cashier sessions JSON.
    /// </summary>
    public partial class UpdateZReportsForTerminalScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ReportNumberFormatted column
            migrationBuilder.AddColumn<string>(
                name: "ReportNumberFormatted",
                table: "ZReportRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            // Add TerminalCode column for denormalization
            migrationBuilder.AddColumn<string>(
                name: "TerminalCode",
                table: "ZReportRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Add CashierSessionsJson column
            migrationBuilder.AddColumn<string>(
                name: "CashierSessionsJson",
                table: "ZReportRecords",
                type: "nvarchar(max)",
                nullable: true);

            // Add index for formatted report number
            migrationBuilder.CreateIndex(
                name: "IX_ZReportRecords_ReportNumberFormatted",
                table: "ZReportRecords",
                column: "ReportNumberFormatted");

            // Add index for terminal code
            migrationBuilder.CreateIndex(
                name: "IX_ZReportRecords_TerminalCode",
                table: "ZReportRecords",
                column: "TerminalCode");

            // Add foreign key relationship for Terminal (if not exists)
            migrationBuilder.CreateIndex(
                name: "IX_ZReportRecords_TerminalId",
                table: "ZReportRecords",
                column: "TerminalId");

            migrationBuilder.AddForeignKey(
                name: "FK_ZReportRecords_Terminals_TerminalId",
                table: "ZReportRecords",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Update existing records to have formatted report numbers
            migrationBuilder.Sql(@"
                UPDATE z
                SET z.ReportNumberFormatted = CONCAT(
                    'Z-',
                    YEAR(z.ReportDateTime),
                    '-',
                    COALESCE(RIGHT('000' + CAST(t.Id AS VARCHAR(10)), 3), '000'),
                    '-',
                    RIGHT('0000' + CAST(z.ReportNumber AS VARCHAR(10)), 4)
                ),
                z.TerminalCode = t.Code
                FROM ZReportRecords z
                LEFT JOIN Terminals t ON z.TerminalId = t.Id
                WHERE z.ReportNumberFormatted IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZReportRecords_Terminals_TerminalId",
                table: "ZReportRecords");

            migrationBuilder.DropIndex(
                name: "IX_ZReportRecords_TerminalId",
                table: "ZReportRecords");

            migrationBuilder.DropIndex(
                name: "IX_ZReportRecords_TerminalCode",
                table: "ZReportRecords");

            migrationBuilder.DropIndex(
                name: "IX_ZReportRecords_ReportNumberFormatted",
                table: "ZReportRecords");

            migrationBuilder.DropColumn(
                name: "CashierSessionsJson",
                table: "ZReportRecords");

            migrationBuilder.DropColumn(
                name: "TerminalCode",
                table: "ZReportRecords");

            migrationBuilder.DropColumn(
                name: "ReportNumberFormatted",
                table: "ZReportRecords");
        }
    }
}
