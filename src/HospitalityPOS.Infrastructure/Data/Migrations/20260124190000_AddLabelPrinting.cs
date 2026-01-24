using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <summary>
/// Migration to add label printing system - printers, templates, sizes, and print jobs.
/// </summary>
public partial class AddLabelPrinting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create LabelSizes table
        migrationBuilder.CreateTable(
            name: "LabelSizes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                WidthMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                HeightMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                DotsPerMm = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelSizes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelSizes_Name",
            table: "LabelSizes",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_LabelSizes_IsActive",
            table: "LabelSizes",
            column: "IsActive");

        // Create LabelPrinters table
        migrationBuilder.CreateTable(
            name: "LabelPrinters",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ConnectionString = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                PrinterType = table.Column<int>(type: "int", nullable: false),
                PrintLanguage = table.Column<int>(type: "int", nullable: false),
                DefaultLabelSizeId = table.Column<int>(type: "int", nullable: true),
                IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LastConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                BaudRate = table.Column<int>(type: "int", nullable: true),
                DataBits = table.Column<int>(type: "int", nullable: true),
                Port = table.Column<int>(type: "int", nullable: true),
                TimeoutMs = table.Column<int>(type: "int", nullable: true, defaultValue: 5000),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelPrinters", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabelPrinters_LabelSizes_DefaultLabelSizeId",
                    column: x => x.DefaultLabelSizeId,
                    principalTable: "LabelSizes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrinters_Name",
            table: "LabelPrinters",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrinters_StoreId",
            table: "LabelPrinters",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrinters_DefaultLabelSizeId",
            table: "LabelPrinters",
            column: "DefaultLabelSizeId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrinters_IsDefault",
            table: "LabelPrinters",
            column: "IsDefault");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrinters_IsActive",
            table: "LabelPrinters",
            column: "IsActive");

        // Create LabelTemplates table
        migrationBuilder.CreateTable(
            name: "LabelTemplates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LabelSizeId = table.Column<int>(type: "int", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                PrintLanguage = table.Column<int>(type: "int", nullable: false),
                TemplateContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                IsPromoTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelTemplates", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabelTemplates_LabelSizes_LabelSizeId",
                    column: x => x.LabelSizeId,
                    principalTable: "LabelSizes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_Name",
            table: "LabelTemplates",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_StoreId",
            table: "LabelTemplates",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_LabelSizeId",
            table: "LabelTemplates",
            column: "LabelSizeId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_IsDefault",
            table: "LabelTemplates",
            column: "IsDefault");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_IsPromoTemplate",
            table: "LabelTemplates",
            column: "IsPromoTemplate");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplates_IsActive",
            table: "LabelTemplates",
            column: "IsActive");

        // Create LabelTemplateFields table
        migrationBuilder.CreateTable(
            name: "LabelTemplateFields",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LabelTemplateId = table.Column<int>(type: "int", nullable: false),
                FieldName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                FieldType = table.Column<int>(type: "int", nullable: false),
                PositionX = table.Column<int>(type: "int", nullable: false),
                PositionY = table.Column<int>(type: "int", nullable: false),
                Width = table.Column<int>(type: "int", nullable: false),
                Height = table.Column<int>(type: "int", nullable: false),
                FontName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                FontSize = table.Column<int>(type: "int", nullable: false),
                Alignment = table.Column<int>(type: "int", nullable: false),
                IsBold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Rotation = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                BarcodeType = table.Column<int>(type: "int", nullable: true),
                BarcodeHeight = table.Column<int>(type: "int", nullable: true),
                ShowBarcodeText = table.Column<bool>(type: "bit", nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelTemplateFields", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabelTemplateFields_LabelTemplates_LabelTemplateId",
                    column: x => x.LabelTemplateId,
                    principalTable: "LabelTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateFields_LabelTemplateId",
            table: "LabelTemplateFields",
            column: "LabelTemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateFields_DisplayOrder",
            table: "LabelTemplateFields",
            column: "DisplayOrder");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateFields_IsActive",
            table: "LabelTemplateFields",
            column: "IsActive");

        // Create CategoryPrinterAssignments table
        migrationBuilder.CreateTable(
            name: "CategoryPrinterAssignments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                LabelPrinterId = table.Column<int>(type: "int", nullable: false),
                LabelTemplateId = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryPrinterAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_CategoryPrinterAssignments_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryPrinterAssignments_LabelPrinters_LabelPrinterId",
                    column: x => x.LabelPrinterId,
                    principalTable: "LabelPrinters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryPrinterAssignments_LabelTemplates_LabelTemplateId",
                    column: x => x.LabelTemplateId,
                    principalTable: "LabelTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryPrinterAssignments_CategoryId_StoreId",
            table: "CategoryPrinterAssignments",
            columns: new[] { "CategoryId", "StoreId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CategoryPrinterAssignments_LabelPrinterId",
            table: "CategoryPrinterAssignments",
            column: "LabelPrinterId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryPrinterAssignments_LabelTemplateId",
            table: "CategoryPrinterAssignments",
            column: "LabelTemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryPrinterAssignments_IsActive",
            table: "CategoryPrinterAssignments",
            column: "IsActive");

        // Create LabelPrintJobs table
        migrationBuilder.CreateTable(
            name: "LabelPrintJobs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                JobType = table.Column<int>(type: "int", nullable: false),
                TotalLabels = table.Column<int>(type: "int", nullable: false),
                PrintedLabels = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                FailedLabels = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SkippedLabels = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                Status = table.Column<int>(type: "int", nullable: false),
                PrinterId = table.Column<int>(type: "int", nullable: false),
                TemplateId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                InitiatedByUserId = table.Column<int>(type: "int", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CopiesPerLabel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelPrintJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabelPrintJobs_LabelPrinters_PrinterId",
                    column: x => x.PrinterId,
                    principalTable: "LabelPrinters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LabelPrintJobs_LabelTemplates_TemplateId",
                    column: x => x.TemplateId,
                    principalTable: "LabelTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LabelPrintJobs_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_StoreId",
            table: "LabelPrintJobs",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_PrinterId",
            table: "LabelPrintJobs",
            column: "PrinterId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_TemplateId",
            table: "LabelPrintJobs",
            column: "TemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_CategoryId",
            table: "LabelPrintJobs",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_Status",
            table: "LabelPrintJobs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_StartedAt",
            table: "LabelPrintJobs",
            column: "StartedAt");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_InitiatedByUserId",
            table: "LabelPrintJobs",
            column: "InitiatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobs_IsActive",
            table: "LabelPrintJobs",
            column: "IsActive");

        // Create LabelPrintJobItems table
        migrationBuilder.CreateTable(
            name: "LabelPrintJobItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LabelPrintJobId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CopiesPrinted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelPrintJobItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabelPrintJobItems_LabelPrintJobs_LabelPrintJobId",
                    column: x => x.LabelPrintJobId,
                    principalTable: "LabelPrintJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LabelPrintJobItems_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobItems_LabelPrintJobId",
            table: "LabelPrintJobItems",
            column: "LabelPrintJobId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobItems_ProductId",
            table: "LabelPrintJobItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobItems_Status",
            table: "LabelPrintJobItems",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_LabelPrintJobItems_IsActive",
            table: "LabelPrintJobItems",
            column: "IsActive");

        // Create LabelTemplateLibraries table
        migrationBuilder.CreateTable(
            name: "LabelTemplateLibraries",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                PrintLanguage = table.Column<int>(type: "int", nullable: false),
                TemplateContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                WidthMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                HeightMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                IsBuiltIn = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Standard"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabelTemplateLibraries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateLibraries_Name",
            table: "LabelTemplateLibraries",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateLibraries_Category",
            table: "LabelTemplateLibraries",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateLibraries_IsBuiltIn",
            table: "LabelTemplateLibraries",
            column: "IsBuiltIn");

        migrationBuilder.CreateIndex(
            name: "IX_LabelTemplateLibraries_IsActive",
            table: "LabelTemplateLibraries",
            column: "IsActive");

        // Seed default label sizes
        migrationBuilder.InsertData(
            table: "LabelSizes",
            columns: new[] { "Name", "WidthMm", "HeightMm", "DotsPerMm", "Description", "CreatedAt", "IsActive" },
            values: new object[,]
            {
                { "Small (38x25mm)", 38m, 25m, 8, "Standard small shelf label", DateTime.UtcNow, true },
                { "Medium (50x30mm)", 50m, 30m, 8, "Standard medium shelf label", DateTime.UtcNow, true },
                { "Large (100x50mm)", 100m, 50m, 8, "Large shelf label with description", DateTime.UtcNow, true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LabelPrintJobItems");
        migrationBuilder.DropTable(name: "CategoryPrinterAssignments");
        migrationBuilder.DropTable(name: "LabelTemplateFields");
        migrationBuilder.DropTable(name: "LabelPrintJobs");
        migrationBuilder.DropTable(name: "LabelTemplateLibraries");
        migrationBuilder.DropTable(name: "LabelTemplates");
        migrationBuilder.DropTable(name: "LabelPrinters");
        migrationBuilder.DropTable(name: "LabelSizes");
    }
}
