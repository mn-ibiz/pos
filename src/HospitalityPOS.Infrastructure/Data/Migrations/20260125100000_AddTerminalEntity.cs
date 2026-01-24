using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add the Terminals table for multi-terminal POS support.
    /// </summary>
    public partial class AddTerminalEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Terminals table
            migrationBuilder.CreateTable(
                name: "Terminals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MachineIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TerminalType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusinessMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsMainRegister = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginUserId = table.Column<int>(type: "int", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    PrinterConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HardwareConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terminals_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_Users_LastLoginUserId",
                        column: x => x.LastLoginUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Unique constraint: Code must be unique within a store
            migrationBuilder.CreateIndex(
                name: "UQ_Terminals_StoreId_Code",
                table: "Terminals",
                columns: new[] { "StoreId", "Code" },
                unique: true);

            // Unique constraint: MachineIdentifier must be globally unique
            migrationBuilder.CreateIndex(
                name: "UQ_Terminals_MachineIdentifier",
                table: "Terminals",
                column: "MachineIdentifier",
                unique: true);

            // Index for quick lookup by store
            migrationBuilder.CreateIndex(
                name: "IX_Terminals_StoreId",
                table: "Terminals",
                column: "StoreId");

            // Index for active terminals
            migrationBuilder.CreateIndex(
                name: "IX_Terminals_IsActive",
                table: "Terminals",
                column: "IsActive");

            // Index for heartbeat monitoring
            migrationBuilder.CreateIndex(
                name: "IX_Terminals_LastHeartbeat",
                table: "Terminals",
                column: "LastHeartbeat");

            // Index for user lookups
            migrationBuilder.CreateIndex(
                name: "IX_Terminals_CreatedByUserId",
                table: "Terminals",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_UpdatedByUserId",
                table: "Terminals",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_LastLoginUserId",
                table: "Terminals",
                column: "LastLoginUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Terminals");
        }
    }
}
