using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add terminal and session tracking to receipts.
    /// </summary>
    public partial class AddReceiptTerminalAndSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add WorkPeriodSessionId to Receipts table
            migrationBuilder.AddColumn<int>(
                name: "WorkPeriodSessionId",
                table: "Receipts",
                type: "int",
                nullable: true);

            // Add TerminalId to Receipts table
            migrationBuilder.AddColumn<int>(
                name: "TerminalId",
                table: "Receipts",
                type: "int",
                nullable: true);

            // Add TerminalCode to Receipts table (denormalized for queries)
            migrationBuilder.AddColumn<string>(
                name: "TerminalCode",
                table: "Receipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Create index for session-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Receipts_WorkPeriodSessionId",
                table: "Receipts",
                column: "WorkPeriodSessionId");

            // Create index for terminal-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Receipts_TerminalId",
                table: "Receipts",
                column: "TerminalId");

            // Add foreign key to WorkPeriodSessions
            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_WorkPeriodSessions_WorkPeriodSessionId",
                table: "Receipts",
                column: "WorkPeriodSessionId",
                principalTable: "WorkPeriodSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add foreign key to Terminals
            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Terminals_TerminalId",
                table: "Receipts",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_WorkPeriodSessions_WorkPeriodSessionId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Terminals_TerminalId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_WorkPeriodSessionId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_TerminalId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "WorkPeriodSessionId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "TerminalCode",
                table: "Receipts");
        }
    }
}
