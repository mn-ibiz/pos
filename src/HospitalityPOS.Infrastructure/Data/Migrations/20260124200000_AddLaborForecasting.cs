using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborForecasting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Labor Configuration
            migrationBuilder.CreateTable(
                name: "LaborConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    TargetLaborPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 25m),
                    TargetSPLH = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 50m),
                    MinStaffPerShift = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    MaxStaffPerShift = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    OvertimeThresholdHours = table.Column<int>(type: "int", nullable: false, defaultValue: 40),
                    OvertimeMultiplier = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 1.5m),
                    MinShiftHours = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    MaxShiftHours = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    MinHoursBetweenShifts = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    EnableForecasting = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ForecastHistoryDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    ForecastAheadWeeks = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborConfigurations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborConfigurations_StoreId",
                table: "LaborConfigurations",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborConfigurations_StoreId_IsActive",
                table: "LaborConfigurations",
                columns: new[] { "StoreId", "IsActive" });

            // Labor Role Configuration
            migrationBuilder.CreateTable(
                name: "LaborRoleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MinStaff = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaxStaff = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    TransactionsPerHour = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 20m),
                    IsRequiredRole = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborRoleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborRoleConfigurations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborRoleConfigurations_StoreId",
                table: "LaborRoleConfigurations",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborRoleConfigurations_StoreId_RoleName",
                table: "LaborRoleConfigurations",
                columns: new[] { "StoreId", "RoleName" },
                unique: true);

            // Daily Labor Forecast
            migrationBuilder.CreateTable(
                name: "DailyLaborForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    TotalForecastedSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLaborHoursNeeded = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ForecastedLaborPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ConfidenceLevel = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: true),
                    SpecialFactors = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLaborForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyLaborForecasts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyLaborForecasts_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLaborForecasts_StoreId",
                table: "DailyLaborForecasts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLaborForecasts_Date",
                table: "DailyLaborForecasts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLaborForecasts_StoreId_Date",
                table: "DailyLaborForecasts",
                columns: new[] { "StoreId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLaborForecasts_StoreId_Date_Status",
                table: "DailyLaborForecasts",
                columns: new[] { "StoreId", "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLaborForecasts_GeneratedByUserId",
                table: "DailyLaborForecasts",
                column: "GeneratedByUserId");

            // Hourly Labor Forecast
            migrationBuilder.CreateTable(
                name: "HourlyLaborForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyForecastId = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    HourDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForecastedSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ForecastedTransactions = table.Column<int>(type: "int", nullable: false),
                    ForecastedCovers = table.Column<int>(type: "int", nullable: false),
                    TargetSPLH = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    RecommendedTotalStaff = table.Column<int>(type: "int", nullable: false),
                    LaborCostEstimate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceLevel = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    Factors = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyLaborForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyLaborForecasts_DailyLaborForecasts_DailyForecastId",
                        column: x => x.DailyForecastId,
                        principalTable: "DailyLaborForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourlyLaborForecasts_DailyForecastId",
                table: "HourlyLaborForecasts",
                column: "DailyForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyLaborForecasts_DailyForecastId_Hour",
                table: "HourlyLaborForecasts",
                columns: new[] { "DailyForecastId", "Hour" },
                unique: true);

            // Hourly Role Forecast
            migrationBuilder.CreateTable(
                name: "HourlyRoleForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HourlyForecastId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecommendedStaff = table.Column<int>(type: "int", nullable: false),
                    LaborCostEstimate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyRoleForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyRoleForecasts_HourlyLaborForecasts_HourlyForecastId",
                        column: x => x.HourlyForecastId,
                        principalTable: "HourlyLaborForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRoleForecasts_HourlyForecastId",
                table: "HourlyRoleForecasts",
                column: "HourlyForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRoleForecasts_HourlyForecastId_RoleName",
                table: "HourlyRoleForecasts",
                columns: new[] { "HourlyForecastId", "RoleName" },
                unique: true);

            // Shift Recommendation
            migrationBuilder.CreateTable(
                name: "ShiftRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyForecastId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    HeadCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftRecommendations_DailyLaborForecasts_DailyForecastId",
                        column: x => x.DailyForecastId,
                        principalTable: "DailyLaborForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecommendations_DailyForecastId",
                table: "ShiftRecommendations",
                column: "DailyForecastId");

            // Staffing Issue
            migrationBuilder.CreateTable(
                name: "StaffingIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    IssueDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentStaff = table.Column<int>(type: "int", nullable: false),
                    RecommendedStaff = table.Column<int>(type: "int", nullable: false),
                    Variance = table.Column<int>(type: "int", nullable: false),
                    ImpactEstimate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffingIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffingIssues_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffingIssues_StoreId",
                table: "StaffingIssues",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffingIssues_IssueDateTime",
                table: "StaffingIssues",
                column: "IssueDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_StaffingIssues_StoreId_IssueDateTime",
                table: "StaffingIssues",
                columns: new[] { "StoreId", "IssueDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffingIssues_StoreId_IsResolved",
                table: "StaffingIssues",
                columns: new[] { "StoreId", "IsResolved" });

            // Optimization Suggestion
            migrationBuilder.CreateTable(
                name: "OptimizationSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ScheduleDate = table.Column<DateTime>(type: "date", nullable: false),
                    SuggestionType = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SuggestedValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstimatedSavings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationSuggestions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptimizationSuggestions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_StoreId",
                table: "OptimizationSuggestions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_ScheduleDate",
                table: "OptimizationSuggestions",
                column: "ScheduleDate");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_StoreId_ScheduleDate",
                table: "OptimizationSuggestions",
                columns: new[] { "StoreId", "ScheduleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_StoreId_IsApplied",
                table: "OptimizationSuggestions",
                columns: new[] { "StoreId", "IsApplied" });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_EmployeeId",
                table: "OptimizationSuggestions",
                column: "EmployeeId");

            // Labor Efficiency Metrics
            migrationBuilder.CreateTable(
                name: "LaborEfficiencyMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ForecastedSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesForecastAccuracy = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ForecastedLaborHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ActualLaborHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ForecastedLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualSPLH = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TargetSPLH = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ActualLaborPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TargetLaborPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UnderstaffedHours = table.Column<int>(type: "int", nullable: false),
                    OverstaffedHours = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    OvertimeCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborEfficiencyMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborEfficiencyMetrics_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborEfficiencyMetrics_StoreId",
                table: "LaborEfficiencyMetrics",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborEfficiencyMetrics_Date",
                table: "LaborEfficiencyMetrics",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_LaborEfficiencyMetrics_StoreId_Date",
                table: "LaborEfficiencyMetrics",
                columns: new[] { "StoreId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LaborEfficiencyMetrics");
            migrationBuilder.DropTable(name: "OptimizationSuggestions");
            migrationBuilder.DropTable(name: "StaffingIssues");
            migrationBuilder.DropTable(name: "ShiftRecommendations");
            migrationBuilder.DropTable(name: "HourlyRoleForecasts");
            migrationBuilder.DropTable(name: "HourlyLaborForecasts");
            migrationBuilder.DropTable(name: "DailyLaborForecasts");
            migrationBuilder.DropTable(name: "LaborRoleConfigurations");
            migrationBuilder.DropTable(name: "LaborConfigurations");
        }
    }
}
