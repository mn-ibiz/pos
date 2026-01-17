using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEtimsEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create EtimsDevices table
        migrationBuilder.CreateTable(
            name: "EtimsDevices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DeviceSerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ControlUnitId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                BusinessPin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ApiBaseUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ApiSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastCommunication = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                LastInvoiceNumber = table.Column<int>(type: "int", nullable: false),
                LastCreditNoteNumber = table.Column<int>(type: "int", nullable: false),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Sandbox"),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsDevices", x => x.Id);
            });

        // Create EtimsInvoices table
        migrationBuilder.CreateTable(
            name: "EtimsInvoices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReceiptId = table.Column<int>(type: "int", nullable: false),
                DeviceId = table.Column<int>(type: "int", nullable: false),
                InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                InternalReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                DocumentType = table.Column<int>(type: "int", nullable: false),
                CustomerType = table.Column<int>(type: "int", nullable: false),
                CustomerPin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                StandardRatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ZeroRatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ExemptAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SubmissionAttempts = table.Column<int>(type: "int", nullable: false),
                LastSubmissionAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReceiptSignature = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                KraInternalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                QrCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsInvoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_EtimsInvoices_Receipts_ReceiptId",
                    column: x => x.ReceiptId,
                    principalTable: "Receipts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_EtimsInvoices_EtimsDevices_DeviceId",
                    column: x => x.DeviceId,
                    principalTable: "EtimsDevices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create EtimsInvoiceItems table
        migrationBuilder.CreateTable(
            name: "EtimsInvoiceItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EtimsInvoiceId = table.Column<int>(type: "int", nullable: false),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ItemDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                UnitOfMeasure = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxType = table.Column<int>(type: "int", nullable: false),
                TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsInvoiceItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_EtimsInvoiceItems_EtimsInvoices_EtimsInvoiceId",
                    column: x => x.EtimsInvoiceId,
                    principalTable: "EtimsInvoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create EtimsCreditNotes table
        migrationBuilder.CreateTable(
            name: "EtimsCreditNotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReceiptVoidId = table.Column<int>(type: "int", nullable: true),
                OriginalInvoiceId = table.Column<int>(type: "int", nullable: false),
                DeviceId = table.Column<int>(type: "int", nullable: false),
                CreditNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                OriginalInvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreditNoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CustomerPin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SubmissionAttempts = table.Column<int>(type: "int", nullable: false),
                LastSubmissionAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                KraSignature = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsCreditNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_EtimsCreditNotes_EtimsInvoices_OriginalInvoiceId",
                    column: x => x.OriginalInvoiceId,
                    principalTable: "EtimsInvoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_EtimsCreditNotes_EtimsDevices_DeviceId",
                    column: x => x.DeviceId,
                    principalTable: "EtimsDevices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create EtimsCreditNoteItems table
        migrationBuilder.CreateTable(
            name: "EtimsCreditNoteItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EtimsCreditNoteId = table.Column<int>(type: "int", nullable: false),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ItemDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxType = table.Column<int>(type: "int", nullable: false),
                TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsCreditNoteItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_EtimsCreditNoteItems_EtimsCreditNotes_EtimsCreditNoteId",
                    column: x => x.EtimsCreditNoteId,
                    principalTable: "EtimsCreditNotes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create EtimsQueue table
        migrationBuilder.CreateTable(
            name: "EtimsQueue",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DocumentType = table.Column<int>(type: "int", nullable: false),
                DocumentId = table.Column<int>(type: "int", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                RetryAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                Status = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                LastProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsQueue", x => x.Id);
            });

        // Create EtimsSyncLogs table
        migrationBuilder.CreateTable(
            name: "EtimsSyncLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DocumentType = table.Column<int>(type: "int", nullable: true),
                DocumentId = table.Column<int>(type: "int", nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                DurationMs = table.Column<long>(type: "bigint", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtimsSyncLogs", x => x.Id);
            });

        // Create indexes for EtimsDevices
        migrationBuilder.CreateIndex(
            name: "IX_EtimsDevices_DeviceSerialNumber",
            table: "EtimsDevices",
            column: "DeviceSerialNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EtimsDevices_ControlUnitId",
            table: "EtimsDevices",
            column: "ControlUnitId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EtimsDevices_IsPrimary",
            table: "EtimsDevices",
            column: "IsPrimary");

        // Create indexes for EtimsInvoices
        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoices_InvoiceNumber",
            table: "EtimsInvoices",
            column: "InvoiceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoices_ReceiptId",
            table: "EtimsInvoices",
            column: "ReceiptId");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoices_DeviceId",
            table: "EtimsInvoices",
            column: "DeviceId");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoices_Status",
            table: "EtimsInvoices",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoices_InvoiceDate",
            table: "EtimsInvoices",
            column: "InvoiceDate");

        // Create indexes for EtimsInvoiceItems
        migrationBuilder.CreateIndex(
            name: "IX_EtimsInvoiceItems_EtimsInvoiceId",
            table: "EtimsInvoiceItems",
            column: "EtimsInvoiceId");

        // Create indexes for EtimsCreditNotes
        migrationBuilder.CreateIndex(
            name: "IX_EtimsCreditNotes_CreditNoteNumber",
            table: "EtimsCreditNotes",
            column: "CreditNoteNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EtimsCreditNotes_OriginalInvoiceId",
            table: "EtimsCreditNotes",
            column: "OriginalInvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsCreditNotes_DeviceId",
            table: "EtimsCreditNotes",
            column: "DeviceId");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsCreditNotes_Status",
            table: "EtimsCreditNotes",
            column: "Status");

        // Create indexes for EtimsCreditNoteItems
        migrationBuilder.CreateIndex(
            name: "IX_EtimsCreditNoteItems_EtimsCreditNoteId",
            table: "EtimsCreditNoteItems",
            column: "EtimsCreditNoteId");

        // Create indexes for EtimsQueue
        migrationBuilder.CreateIndex(
            name: "IX_EtimsQueue_Status",
            table: "EtimsQueue",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsQueue_Priority",
            table: "EtimsQueue",
            column: "Priority");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsQueue_QueuedAt",
            table: "EtimsQueue",
            column: "QueuedAt");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsQueue_RetryAfter",
            table: "EtimsQueue",
            column: "RetryAfter");

        // Create indexes for EtimsSyncLogs
        migrationBuilder.CreateIndex(
            name: "IX_EtimsSyncLogs_StartedAt",
            table: "EtimsSyncLogs",
            column: "StartedAt");

        migrationBuilder.CreateIndex(
            name: "IX_EtimsSyncLogs_IsSuccess",
            table: "EtimsSyncLogs",
            column: "IsSuccess");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EtimsSyncLogs");
        migrationBuilder.DropTable(name: "EtimsQueue");
        migrationBuilder.DropTable(name: "EtimsCreditNoteItems");
        migrationBuilder.DropTable(name: "EtimsCreditNotes");
        migrationBuilder.DropTable(name: "EtimsInvoiceItems");
        migrationBuilder.DropTable(name: "EtimsInvoices");
        migrationBuilder.DropTable(name: "EtimsDevices");
    }
}
