using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add terminal tracking to orders.
    /// </summary>
    public partial class AddOrderTerminalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TerminalId to Orders table
            migrationBuilder.AddColumn<int>(
                name: "TerminalId",
                table: "Orders",
                type: "int",
                nullable: true);

            // Add TerminalCode to Orders table (denormalized for queries)
            migrationBuilder.AddColumn<string>(
                name: "TerminalCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Create index for terminal-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Orders_TerminalId",
                table: "Orders",
                column: "TerminalId");

            // Add foreign key to Terminals
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Terminals_TerminalId",
                table: "Orders",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Terminals_TerminalId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TerminalId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TerminalCode",
                table: "Orders");
        }
    }
}
