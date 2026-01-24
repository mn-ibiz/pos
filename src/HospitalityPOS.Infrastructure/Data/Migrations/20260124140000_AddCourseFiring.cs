using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add course firing tables for sequential course pacing.
    /// </summary>
    public partial class AddCourseFiring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CourseDefinitions table
            migrationBuilder.CreateTable(
                name: "CourseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseNumber = table.Column<int>(type: "int", nullable: false),
                    DefaultDelayMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDefinitions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // CourseConfigurations table
            migrationBuilder.CreateTable(
                name: "CourseConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    EnableCoursing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FireMode = table.Column<int>(type: "int", nullable: false, defaultValue: 2), // AutoOnBump
                    DefaultCoursePacingMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    AutoFireOnPreviousBump = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowHeldCoursesOnPrepStation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequireExpoConfirmation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AllowManualFireOverride = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowRushMode = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoFireFirstCourse = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FireGracePeriodSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    ShowCountdownToNextCourse = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AlertOnReadyToFire = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FireAlertSound = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseConfigurations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // KdsCourseStates table
            migrationBuilder.CreateTable(
                name: "KdsCourseStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KdsOrderId = table.Column<int>(type: "int", nullable: false),
                    CourseDefinitionId = table.Column<int>(type: "int", nullable: true),
                    CourseNumber = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0), // Pending
                    ScheduledFireAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiredByUserId = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsOnHold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HoldReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HeldByUserId = table.Column<int>(type: "int", nullable: true),
                    HeldAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetMinutesAfterPrevious = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    DisplayColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TotalItems = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedItems = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KdsCourseStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KdsCourseStates_KdsOrders_KdsOrderId",
                        column: x => x.KdsOrderId,
                        principalTable: "KdsOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KdsCourseStates_CourseDefinitions_CourseDefinitionId",
                        column: x => x.CourseDefinitionId,
                        principalTable: "CourseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KdsCourseStates_Users_FiredByUserId",
                        column: x => x.FiredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KdsCourseStates_Users_ServedByUserId",
                        column: x => x.ServedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KdsCourseStates_Users_HeldByUserId",
                        column: x => x.HeldByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // CourseFiringLogs table
            migrationBuilder.CreateTable(
                name: "CourseFiringLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KdsOrderId = table.Column<int>(type: "int", nullable: false),
                    CourseStateId = table.Column<int>(type: "int", nullable: true),
                    CourseNumber = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StationId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFiringLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseFiringLogs_KdsOrders_KdsOrderId",
                        column: x => x.KdsOrderId,
                        principalTable: "KdsOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseFiringLogs_KdsCourseStates_CourseStateId",
                        column: x => x.CourseStateId,
                        principalTable: "KdsCourseStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseFiringLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseFiringLogs_KdsStations_StationId",
                        column: x => x.StationId,
                        principalTable: "KdsStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Add columns to KdsOrderItems for course firing
            migrationBuilder.AddColumn<int>(
                name: "CourseStateId",
                table: "KdsOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemFireStatus",
                table: "KdsOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0); // Waiting

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFireAt",
                table: "KdsOrderItems",
                type: "datetime2",
                nullable: true);

            // Indexes for CourseDefinitions
            migrationBuilder.CreateIndex(
                name: "IX_CourseDefinitions_Store_CourseNumber",
                table: "CourseDefinitions",
                columns: new[] { "StoreId", "CourseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseDefinitions_StoreId",
                table: "CourseDefinitions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDefinitions_IsActive",
                table: "CourseDefinitions",
                column: "IsActive");

            // Indexes for CourseConfigurations
            migrationBuilder.CreateIndex(
                name: "IX_CourseConfigurations_StoreId",
                table: "CourseConfigurations",
                column: "StoreId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseConfigurations_IsActive",
                table: "CourseConfigurations",
                column: "IsActive");

            // Indexes for KdsCourseStates
            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_Order_CourseNumber",
                table: "KdsCourseStates",
                columns: new[] { "KdsOrderId", "CourseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_Status",
                table: "KdsCourseStates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_ScheduledFireAt",
                table: "KdsCourseStates",
                column: "ScheduledFireAt");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_IsOnHold",
                table: "KdsCourseStates",
                column: "IsOnHold");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_Status_Scheduled_Hold",
                table: "KdsCourseStates",
                columns: new[] { "Status", "ScheduledFireAt", "IsOnHold" });

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_CourseDefinitionId",
                table: "KdsCourseStates",
                column: "CourseDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_FiredByUserId",
                table: "KdsCourseStates",
                column: "FiredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_ServedByUserId",
                table: "KdsCourseStates",
                column: "ServedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KdsCourseStates_HeldByUserId",
                table: "KdsCourseStates",
                column: "HeldByUserId");

            // Indexes for CourseFiringLogs
            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_KdsOrderId",
                table: "CourseFiringLogs",
                column: "KdsOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_CourseStateId",
                table: "CourseFiringLogs",
                column: "CourseStateId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_ActionAt",
                table: "CourseFiringLogs",
                column: "ActionAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_Action",
                table: "CourseFiringLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_UserId",
                table: "CourseFiringLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFiringLogs_StationId",
                table: "CourseFiringLogs",
                column: "StationId");

            // Index for KdsOrderItems.CourseStateId
            migrationBuilder.CreateIndex(
                name: "IX_KdsOrderItems_CourseStateId",
                table: "KdsOrderItems",
                column: "CourseStateId");

            // Foreign key for KdsOrderItems.CourseStateId
            migrationBuilder.AddForeignKey(
                name: "FK_KdsOrderItems_KdsCourseStates_CourseStateId",
                table: "KdsOrderItems",
                column: "CourseStateId",
                principalTable: "KdsCourseStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed default course definitions for each store
            migrationBuilder.Sql(@"
                INSERT INTO CourseDefinitions (Name, CourseNumber, DefaultDelayMinutes, Color, Description, StoreId, CreatedAt, IsActive)
                SELECT 'Drinks', 1, 0, '#3498db', 'Beverages and cocktails', Id, GETUTCDATE(), 1 FROM Stores
                UNION ALL
                SELECT 'Appetizers', 2, 5, '#e74c3c', 'Starters and appetizers', Id, GETUTCDATE(), 1 FROM Stores
                UNION ALL
                SELECT 'Mains', 3, 10, '#27ae60', 'Main courses', Id, GETUTCDATE(), 1 FROM Stores
                UNION ALL
                SELECT 'Desserts', 4, 15, '#9b59b6', 'Desserts and after-dinner items', Id, GETUTCDATE(), 1 FROM Stores
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key from KdsOrderItems
            migrationBuilder.DropForeignKey(
                name: "FK_KdsOrderItems_KdsCourseStates_CourseStateId",
                table: "KdsOrderItems");

            // Remove index from KdsOrderItems
            migrationBuilder.DropIndex(
                name: "IX_KdsOrderItems_CourseStateId",
                table: "KdsOrderItems");

            // Remove columns from KdsOrderItems
            migrationBuilder.DropColumn(
                name: "CourseStateId",
                table: "KdsOrderItems");

            migrationBuilder.DropColumn(
                name: "ItemFireStatus",
                table: "KdsOrderItems");

            migrationBuilder.DropColumn(
                name: "ScheduledFireAt",
                table: "KdsOrderItems");

            // Drop tables in reverse order
            migrationBuilder.DropTable(
                name: "CourseFiringLogs");

            migrationBuilder.DropTable(
                name: "KdsCourseStates");

            migrationBuilder.DropTable(
                name: "CourseConfigurations");

            migrationBuilder.DropTable(
                name: "CourseDefinitions");
        }
    }
}
