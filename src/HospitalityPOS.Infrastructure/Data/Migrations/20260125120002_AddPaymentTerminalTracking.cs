using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add terminal tracking to payments.
    /// </summary>
    public partial class AddPaymentTerminalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TerminalId to Payments table
            migrationBuilder.AddColumn<int>(
                name: "TerminalId",
                table: "Payments",
                type: "int",
                nullable: true);

            // Add TerminalCode to Payments table (denormalized for queries)
            migrationBuilder.AddColumn<string>(
                name: "TerminalCode",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Create index for terminal-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Payments_TerminalId",
                table: "Payments",
                column: "TerminalId");

            // Add foreign key to Terminals
            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Terminals_TerminalId",
                table: "Payments",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Terminals_TerminalId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TerminalId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TerminalCode",
                table: "Payments");
        }
    }
}
