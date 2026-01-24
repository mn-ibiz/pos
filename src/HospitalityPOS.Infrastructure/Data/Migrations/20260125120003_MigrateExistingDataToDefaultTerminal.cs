using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Data migration to assign existing records to a default terminal.
    /// This ensures backwards compatibility with pre-multi-terminal data.
    /// </summary>
    public partial class MigrateExistingDataToDefaultTerminal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration creates a default terminal and assigns all existing
            // records that have NULL TerminalId to this default terminal.
            migrationBuilder.Sql(@"
                -- Migration: MigrateExistingDataToDefaultTerminal
                BEGIN TRANSACTION;

                -- 1. Ensure default terminal exists
                IF NOT EXISTS (SELECT 1 FROM Terminals WHERE Code = 'REG-001')
                BEGIN
                    DECLARE @StoreId INT = (SELECT TOP 1 Id FROM Stores ORDER BY Id);
                    DECLARE @AdminUserId INT = (SELECT TOP 1 Id FROM Users WHERE Username = 'admin');

                    IF @StoreId IS NULL
                        SET @StoreId = 1;

                    IF @AdminUserId IS NULL
                        SET @AdminUserId = 1;

                    INSERT INTO Terminals (
                        StoreId, Code, Name, Description,
                        MachineIdentifier, TerminalType, BusinessMode,
                        IsActive, IsMainRegister, CreatedAt, CreatedByUserId
                    )
                    VALUES (
                        @StoreId,
                        'REG-001',
                        'Register 1 (Migrated)',
                        'Default terminal created during multi-terminal migration',
                        'MIGRATED-' + CAST(NEWID() AS NVARCHAR(36)),
                        1, -- Register
                        1, -- Supermarket
                        1, -- IsActive
                        1, -- IsMainRegister
                        GETUTCDATE(),
                        @AdminUserId
                    );
                END

                DECLARE @DefaultTerminalId INT = (SELECT TOP 1 Id FROM Terminals WHERE Code = 'REG-001');
                DECLARE @DefaultTerminalCode NVARCHAR(20) = 'REG-001';

                -- 2. Update WorkPeriods where TerminalId is NULL
                UPDATE WorkPeriods
                SET TerminalId = @DefaultTerminalId,
                    TerminalCode = @DefaultTerminalCode
                WHERE TerminalId IS NULL;

                -- 3. Update Receipts where TerminalId is NULL
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Receipts') AND name = 'TerminalId')
                BEGIN
                    UPDATE Receipts
                    SET TerminalId = @DefaultTerminalId,
                        TerminalCode = @DefaultTerminalCode
                    WHERE TerminalId IS NULL;
                END

                -- 4. Update Orders where TerminalId is NULL
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TerminalId')
                BEGIN
                    UPDATE Orders
                    SET TerminalId = @DefaultTerminalId,
                        TerminalCode = @DefaultTerminalCode
                    WHERE TerminalId IS NULL;
                END

                -- 5. Update Payments where TerminalId is NULL
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'TerminalId')
                BEGIN
                    UPDATE Payments
                    SET TerminalId = @DefaultTerminalId,
                        TerminalCode = @DefaultTerminalCode
                    WHERE TerminalId IS NULL;
                END

                -- 6. Update ZReportRecords where TerminalId is NULL (if column exists)
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ZReportRecords') AND name = 'TerminalId')
                BEGIN
                    UPDATE ZReportRecords
                    SET TerminalId = @DefaultTerminalId,
                        TerminalCode = @DefaultTerminalCode
                    WHERE TerminalId IS NULL;
                END

                -- 7. Update CashPayouts where TerminalId is NULL (if column exists)
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CashPayouts') AND name = 'TerminalId')
                BEGIN
                    UPDATE CashPayouts
                    SET TerminalId = @DefaultTerminalId
                    WHERE TerminalId IS NULL;
                END

                -- 8. Log migration to AuditLogs
                DECLARE @AuditUserId INT = (SELECT TOP 1 Id FROM Users WHERE Username = 'admin');
                IF @AuditUserId IS NULL
                    SET @AuditUserId = 1;

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
                BEGIN
                    INSERT INTO AuditLogs (
                        EntityType, EntityId, Action, Details,
                        UserId, Timestamp
                    )
                    VALUES (
                        'System',
                        0,
                        'DataMigration',
                        'Migrated existing data to default terminal REG-001 (ID: ' + CAST(@DefaultTerminalId AS VARCHAR) + ')',
                        @AuditUserId,
                        GETUTCDATE()
                    );
                END

                COMMIT TRANSACTION;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: This rollback removes terminal associations but keeps the default terminal
            // as it may still be referenced. Manual cleanup may be needed.
            migrationBuilder.Sql(@"
                BEGIN TRANSACTION;

                DECLARE @MigratedTerminalId INT = (
                    SELECT TOP 1 Id FROM Terminals
                    WHERE Code = 'REG-001' AND Description LIKE '%Migrated%'
                );

                IF @MigratedTerminalId IS NOT NULL
                BEGIN
                    -- Only clear associations for records pointing to the migrated terminal
                    UPDATE WorkPeriods
                    SET TerminalId = NULL, TerminalCode = NULL
                    WHERE TerminalId = @MigratedTerminalId;

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Receipts') AND name = 'TerminalId')
                    BEGIN
                        UPDATE Receipts
                        SET TerminalId = NULL, TerminalCode = NULL
                        WHERE TerminalId = @MigratedTerminalId;
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TerminalId')
                    BEGIN
                        UPDATE Orders
                        SET TerminalId = NULL, TerminalCode = NULL
                        WHERE TerminalId = @MigratedTerminalId;
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'TerminalId')
                    BEGIN
                        UPDATE Payments
                        SET TerminalId = NULL, TerminalCode = NULL
                        WHERE TerminalId = @MigratedTerminalId;
                    END
                END

                COMMIT TRANSACTION;
            ");
        }
    }
}
