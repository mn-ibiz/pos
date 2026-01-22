using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultExpenseCategoryId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxDeductible",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Expenses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurringExpenseId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Expenses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExpenseCategories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "ExpenseCategories",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultAccountId",
                table: "ExpenseCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "ExpenseCategories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemCategory",
                table: "ExpenseCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ExpenseCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ExpenseCategories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Operating");

            migrationBuilder.CreateTable(
                name: "ExpenseAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseAttachments_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Monthly"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Quarter = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlertThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 80),
                    AlertSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseBudgets_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsEstimatedAmount = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Monthly"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    ReminderDaysBefore = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    AutoApprove = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AutoGenerate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastGeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(774));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2123));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2126));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2127));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2128));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2129));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2131));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2132));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 684, DateTimeKind.Utc).AddTicks(2133));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(2923));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(5908));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(7550));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(7555));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(7558));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 813, DateTimeKind.Utc).AddTicks(7561));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(1712));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2441));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2443));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2444));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2445));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2446));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 22, 15, 31, 31, 962, DateTimeKind.Utc).AddTicks(2447));

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_DefaultExpenseCategoryId",
                table: "Suppliers",
                column: "DefaultExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PaymentMethodId",
                table: "Expenses",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecurringExpenseId",
                table: "Expenses",
                column: "RecurringExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Status",
                table: "Expenses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_DefaultAccountId",
                table: "ExpenseCategories",
                column: "DefaultAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Name",
                table: "ExpenseCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_SortOrder",
                table: "ExpenseCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Type",
                table: "ExpenseCategories",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttachments_ExpenseId",
                table: "ExpenseAttachments",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttachments_UploadedByUserId",
                table: "ExpenseAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_ExpenseCategoryId",
                table: "ExpenseBudgets",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_StartDate_EndDate",
                table: "ExpenseBudgets",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_Year_Month",
                table: "ExpenseBudgets",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_ExpenseCategoryId",
                table: "RecurringExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_Frequency",
                table: "RecurringExpenses",
                column: "Frequency");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_IsActive",
                table: "RecurringExpenses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_NextDueDate",
                table: "RecurringExpenses",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_PaymentMethodId",
                table: "RecurringExpenses",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_SupplierId",
                table: "RecurringExpenses",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_ChartOfAccounts_DefaultAccountId",
                table: "ExpenseCategories",
                column: "DefaultAccountId",
                principalTable: "ChartOfAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_PaymentMethods_PaymentMethodId",
                table: "Expenses",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_RecurringExpenses_RecurringExpenseId",
                table: "Expenses",
                column: "RecurringExpenseId",
                principalTable: "RecurringExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_ExpenseCategories_DefaultExpenseCategoryId",
                table: "Suppliers",
                column: "DefaultExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_ChartOfAccounts_DefaultAccountId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_PaymentMethods_PaymentMethodId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_RecurringExpenses_RecurringExpenseId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_ExpenseCategories_DefaultExpenseCategoryId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "ExpenseAttachments");

            migrationBuilder.DropTable(
                name: "ExpenseBudgets");

            migrationBuilder.DropTable(
                name: "RecurringExpenses");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_DefaultExpenseCategoryId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_PaymentMethodId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_RecurringExpenseId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Status",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_DefaultAccountId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_Name",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_SortOrder",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_Type",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "DefaultExpenseCategoryId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "IsTaxDeductible",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RecurringExpenseId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "DefaultAccountId",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "IsSystemCategory",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ExpenseCategories");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExpenseCategories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(7484));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9182));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9185));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9187));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9189));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9190));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9192));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9194));

            migrationBuilder.UpdateData(
                table: "AdjustmentReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 276, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 504, DateTimeKind.Utc).AddTicks(6628));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 504, DateTimeKind.Utc).AddTicks(9941));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 505, DateTimeKind.Utc).AddTicks(1029));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 505, DateTimeKind.Utc).AddTicks(1036));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 505, DateTimeKind.Utc).AddTicks(1041));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 505, DateTimeKind.Utc).AddTicks(1045));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(1708));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2723));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2725));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2726));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2728));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2729));

            migrationBuilder.UpdateData(
                table: "VoidReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 18, 59, 1, 695, DateTimeKind.Utc).AddTicks(2730));
        }
    }
}
