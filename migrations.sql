IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [EtimsDevices] (
    [Id] int NOT NULL IDENTITY,
    [DeviceSerialNumber] nvarchar(100) NOT NULL,
    [ControlUnitId] nvarchar(50) NOT NULL,
    [BusinessPin] nvarchar(20) NOT NULL,
    [BusinessName] nvarchar(200) NOT NULL,
    [BranchCode] nvarchar(10) NOT NULL,
    [BranchName] nvarchar(100) NULL,
    [ApiBaseUrl] nvarchar(255) NULL,
    [ApiKey] nvarchar(500) NULL,
    [ApiSecret] nvarchar(500) NULL,
    [RegistrationDate] datetime2 NULL,
    [LastCommunication] datetime2 NULL,
    [Status] int NOT NULL,
    [LastInvoiceNumber] int NOT NULL,
    [LastCreditNoteNumber] int NOT NULL,
    [IsPrimary] bit NOT NULL,
    [Environment] nvarchar(20) NOT NULL DEFAULT N'Sandbox',
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsDevices] PRIMARY KEY ([Id])
);

CREATE TABLE [EtimsInvoices] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [DeviceId] int NOT NULL,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [InternalReceiptNumber] nvarchar(50) NULL,
    [InvoiceDate] datetime2 NOT NULL,
    [DocumentType] int NOT NULL,
    [CustomerType] int NOT NULL,
    [CustomerPin] nvarchar(20) NULL,
    [CustomerName] nvarchar(200) NULL,
    [CustomerPhone] nvarchar(20) NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [StandardRatedAmount] decimal(18,2) NOT NULL,
    [ZeroRatedAmount] decimal(18,2) NOT NULL,
    [ExemptAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SubmissionAttempts] int NOT NULL,
    [LastSubmissionAttempt] datetime2 NULL,
    [SubmittedAt] datetime2 NULL,
    [ReceiptSignature] nvarchar(500) NULL,
    [KraInternalData] nvarchar(max) NULL,
    [QrCode] nvarchar(500) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsInvoices_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EtimsInvoices_EtimsDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [EtimsDevices] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EtimsInvoiceItems] (
    [Id] int NOT NULL IDENTITY,
    [EtimsInvoiceId] int NOT NULL,
    [SequenceNumber] int NOT NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemDescription] nvarchar(200) NOT NULL,
    [HsCode] nvarchar(20) NULL,
    [UnitOfMeasure] nvarchar(10) NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxType] int NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsInvoiceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsInvoiceItems_EtimsInvoices_EtimsInvoiceId] FOREIGN KEY ([EtimsInvoiceId]) REFERENCES [EtimsInvoices] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EtimsCreditNotes] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptVoidId] int NULL,
    [OriginalInvoiceId] int NOT NULL,
    [DeviceId] int NOT NULL,
    [CreditNoteNumber] nvarchar(50) NOT NULL,
    [OriginalInvoiceNumber] nvarchar(50) NOT NULL,
    [CreditNoteDate] datetime2 NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [CustomerPin] nvarchar(20) NULL,
    [CustomerName] nvarchar(200) NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SubmissionAttempts] int NOT NULL,
    [LastSubmissionAttempt] datetime2 NULL,
    [SubmittedAt] datetime2 NULL,
    [KraSignature] nvarchar(500) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsCreditNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsCreditNotes_EtimsInvoices_OriginalInvoiceId] FOREIGN KEY ([OriginalInvoiceId]) REFERENCES [EtimsInvoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EtimsCreditNotes_EtimsDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [EtimsDevices] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EtimsCreditNoteItems] (
    [Id] int NOT NULL IDENTITY,
    [EtimsCreditNoteId] int NOT NULL,
    [SequenceNumber] int NOT NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemDescription] nvarchar(200) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TaxType] int NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsCreditNoteItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsCreditNoteItems_EtimsCreditNotes_EtimsCreditNoteId] FOREIGN KEY ([EtimsCreditNoteId]) REFERENCES [EtimsCreditNotes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EtimsQueue] (
    [Id] int NOT NULL IDENTITY,
    [DocumentType] int NOT NULL,
    [DocumentId] int NOT NULL,
    [Priority] int NOT NULL DEFAULT 100,
    [QueuedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [RetryAfter] datetime2 NULL,
    [Attempts] int NOT NULL DEFAULT 0,
    [MaxAttempts] int NOT NULL DEFAULT 10,
    [Status] int NOT NULL,
    [LastError] nvarchar(1000) NULL,
    [LastProcessedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsQueue] PRIMARY KEY ([Id])
);

CREATE TABLE [EtimsSyncLogs] (
    [Id] int NOT NULL IDENTITY,
    [OperationType] nvarchar(50) NOT NULL,
    [DocumentType] int NULL,
    [DocumentId] int NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(1000) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [HttpStatusCode] int NULL,
    [DurationMs] bigint NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsSyncLogs] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_EtimsDevices_DeviceSerialNumber] ON [EtimsDevices] ([DeviceSerialNumber]);

CREATE UNIQUE INDEX [IX_EtimsDevices_ControlUnitId] ON [EtimsDevices] ([ControlUnitId]);

CREATE INDEX [IX_EtimsDevices_IsPrimary] ON [EtimsDevices] ([IsPrimary]);

CREATE UNIQUE INDEX [IX_EtimsInvoices_InvoiceNumber] ON [EtimsInvoices] ([InvoiceNumber]);

CREATE INDEX [IX_EtimsInvoices_ReceiptId] ON [EtimsInvoices] ([ReceiptId]);

CREATE INDEX [IX_EtimsInvoices_DeviceId] ON [EtimsInvoices] ([DeviceId]);

CREATE INDEX [IX_EtimsInvoices_Status] ON [EtimsInvoices] ([Status]);

CREATE INDEX [IX_EtimsInvoices_InvoiceDate] ON [EtimsInvoices] ([InvoiceDate]);

CREATE INDEX [IX_EtimsInvoiceItems_EtimsInvoiceId] ON [EtimsInvoiceItems] ([EtimsInvoiceId]);

CREATE UNIQUE INDEX [IX_EtimsCreditNotes_CreditNoteNumber] ON [EtimsCreditNotes] ([CreditNoteNumber]);

CREATE INDEX [IX_EtimsCreditNotes_OriginalInvoiceId] ON [EtimsCreditNotes] ([OriginalInvoiceId]);

CREATE INDEX [IX_EtimsCreditNotes_DeviceId] ON [EtimsCreditNotes] ([DeviceId]);

CREATE INDEX [IX_EtimsCreditNotes_Status] ON [EtimsCreditNotes] ([Status]);

CREATE INDEX [IX_EtimsCreditNoteItems_EtimsCreditNoteId] ON [EtimsCreditNoteItems] ([EtimsCreditNoteId]);

CREATE INDEX [IX_EtimsQueue_Status] ON [EtimsQueue] ([Status]);

CREATE INDEX [IX_EtimsQueue_Priority] ON [EtimsQueue] ([Priority]);

CREATE INDEX [IX_EtimsQueue_QueuedAt] ON [EtimsQueue] ([QueuedAt]);

CREATE INDEX [IX_EtimsQueue_RetryAfter] ON [EtimsQueue] ([RetryAfter]);

CREATE INDEX [IX_EtimsSyncLogs_StartedAt] ON [EtimsSyncLogs] ([StartedAt]);

CREATE INDEX [IX_EtimsSyncLogs_IsSuccess] ON [EtimsSyncLogs] ([IsSuccess]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260116000001_AddEtimsEntities', N'10.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'IsActive');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var + ';');

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'CreatedAt');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var1 + ';');

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'BranchName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var2 + ';');
UPDATE [EtimsDevices] SET [BranchName] = N'' WHERE [BranchName] IS NULL;
ALTER TABLE [EtimsDevices] ALTER COLUMN [BranchName] nvarchar(100) NOT NULL;
ALTER TABLE [EtimsDevices] ADD DEFAULT N'' FOR [BranchName];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'ApiSecret');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var3 + ';');
UPDATE [EtimsDevices] SET [ApiSecret] = N'' WHERE [ApiSecret] IS NULL;
ALTER TABLE [EtimsDevices] ALTER COLUMN [ApiSecret] nvarchar(500) NOT NULL;
ALTER TABLE [EtimsDevices] ADD DEFAULT N'' FOR [ApiSecret];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'ApiKey');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var4 + ';');
UPDATE [EtimsDevices] SET [ApiKey] = N'' WHERE [ApiKey] IS NULL;
ALTER TABLE [EtimsDevices] ALTER COLUMN [ApiKey] nvarchar(500) NOT NULL;
ALTER TABLE [EtimsDevices] ADD DEFAULT N'' FOR [ApiKey];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EtimsDevices]') AND [c].[name] = N'ApiBaseUrl');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [EtimsDevices] DROP CONSTRAINT ' + @var5 + ';');
UPDATE [EtimsDevices] SET [ApiBaseUrl] = N'' WHERE [ApiBaseUrl] IS NULL;
ALTER TABLE [EtimsDevices] ALTER COLUMN [ApiBaseUrl] nvarchar(255) NOT NULL;
ALTER TABLE [EtimsDevices] ADD DEFAULT N'' FOR [ApiBaseUrl];

CREATE TABLE [AdjustmentReasons] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(10) NOT NULL,
    [RequiresNote] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsIncrease] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsDecrease] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DisplayOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_AdjustmentReasons] PRIMARY KEY ([Id])
);

CREATE TABLE [ApiScopes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [DisplayName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Resource] nvarchar(max) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [IsSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiScopes] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [ParentCategoryId] int NULL,
    [ImagePath] nvarchar(500) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ChartOfAccounts] (
    [Id] int NOT NULL IDENTITY,
    [AccountCode] nvarchar(20) NOT NULL,
    [AccountName] nvarchar(100) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [AccountType] nvarchar(20) NOT NULL,
    [ParentAccountId] int NULL,
    [Description] nvarchar(200) NULL,
    [IsSystemAccount] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ChartOfAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChartOfAccounts_ChartOfAccounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EtimsQueue] (
    [Id] int NOT NULL IDENTITY,
    [DocumentType] int NOT NULL,
    [DocumentId] int NOT NULL,
    [Priority] int NOT NULL,
    [QueuedAt] datetime2 NOT NULL,
    [RetryAfter] datetime2 NULL,
    [Attempts] int NOT NULL,
    [MaxAttempts] int NOT NULL,
    [Status] int NOT NULL,
    [LastError] nvarchar(1000) NULL,
    [LastProcessedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsQueue] PRIMARY KEY ([Id])
);

CREATE TABLE [EtimsSyncLogs] (
    [Id] int NOT NULL IDENTITY,
    [OperationType] nvarchar(50) NOT NULL,
    [DocumentType] int NULL,
    [DocumentId] int NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(1000) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [HttpStatusCode] int NULL,
    [DurationMs] bigint NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsSyncLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [ExpenseCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NULL,
    [ParentCategoryId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpenseCategories_ExpenseCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Floors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [GridWidth] int NOT NULL DEFAULT 10,
    [GridHeight] int NOT NULL DEFAULT 10,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Floors] PRIMARY KEY ([Id])
);

CREATE TABLE [InternalBarcodeSequences] (
    [Id] int NOT NULL IDENTITY,
    [Prefix] nvarchar(10) NOT NULL,
    [LastSequenceNumber] int NOT NULL,
    [SequenceDigits] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_InternalBarcodeSequences] PRIMARY KEY ([Id])
);

CREATE TABLE [LoyaltyMembers] (
    [Id] int NOT NULL IDENTITY,
    [PhoneNumber] nvarchar(15) NOT NULL,
    [Name] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [MembershipNumber] nvarchar(20) NOT NULL,
    [Tier] int NOT NULL DEFAULT 1,
    [PointsBalance] decimal(18,2) NOT NULL DEFAULT 0.0,
    [LifetimePoints] decimal(18,2) NOT NULL DEFAULT 0.0,
    [LifetimeSpend] decimal(18,2) NOT NULL DEFAULT 0.0,
    [EnrolledAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [LastVisit] datetime2 NULL,
    [VisitCount] int NOT NULL DEFAULT 0,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_LoyaltyMembers] PRIMARY KEY ([Id])
);

CREATE TABLE [MpesaConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Environment] int NOT NULL,
    [ConsumerKey] nvarchar(200) NOT NULL,
    [ConsumerSecret] nvarchar(200) NOT NULL,
    [BusinessShortCode] nvarchar(20) NOT NULL,
    [Passkey] nvarchar(500) NOT NULL,
    [TransactionType] int NOT NULL,
    [CallbackUrl] nvarchar(500) NOT NULL,
    [ApiBaseUrl] nvarchar(200) NOT NULL,
    [AccountReferencePrefix] nvarchar(20) NOT NULL,
    [DefaultDescription] nvarchar(200) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastSuccessfulCall] datetime2 NULL,
    [CachedAccessToken] nvarchar(2000) NULL,
    [TokenExpiry] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MpesaConfigurations] PRIMARY KEY ([Id])
);

CREATE TABLE [PaymentMethods] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [RequiresReference] bit NOT NULL,
    [ReferenceLabel] nvarchar(50) NULL,
    [ReferenceMinLength] int NULL,
    [ReferenceMaxLength] int NULL,
    [SupportsChange] bit NOT NULL,
    [OpensDrawer] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IconPath] nvarchar(200) NULL,
    [BackgroundColor] nvarchar(20) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
);

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Category] nvarchar(50) NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [PMSErrorMappings] (
    [Id] int NOT NULL IDENTITY,
    [PMSType] int NULL,
    [ErrorCode] nvarchar(max) NOT NULL,
    [OriginalMessage] nvarchar(max) NULL,
    [FriendlyMessage] nvarchar(max) NOT NULL,
    [SuggestedAction] nvarchar(max) NULL,
    [IsRetryable] bit NOT NULL,
    [Severity] int NOT NULL,
    [NotifyAdmin] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSErrorMappings] PRIMARY KEY ([Id])
);

CREATE TABLE [PointsConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [EarningRate] decimal(18,2) NOT NULL DEFAULT 100.0,
    [RedemptionValue] decimal(18,2) NOT NULL DEFAULT 1.0,
    [MinimumRedemptionPoints] int NOT NULL DEFAULT 100,
    [MaximumRedemptionPoints] int NOT NULL DEFAULT 0,
    [MaxRedemptionPercentage] int NOT NULL DEFAULT 50,
    [EarnOnDiscountedItems] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EarnOnTax] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PointsExpiryDays] int NOT NULL DEFAULT 0,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PointsConfigurations] PRIMARY KEY ([Id])
);

CREATE TABLE [PricingZones] (
    [Id] int NOT NULL IDENTITY,
    [ZoneCode] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CurrencyCode] nvarchar(3) NOT NULL DEFAULT N'KES',
    [DefaultTaxRate] decimal(5,2) NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PricingZones] PRIMARY KEY ([Id])
);

CREATE TABLE [Printers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [ConnectionType] nvarchar(20) NOT NULL,
    [PortName] nvarchar(50) NULL,
    [IpAddress] nvarchar(50) NULL,
    [Port] int NULL,
    [UsbPath] nvarchar(500) NULL,
    [WindowsPrinterName] nvarchar(200) NULL,
    [PaperWidth] int NOT NULL,
    [CharsPerLine] int NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [LastStatusCheck] datetime2 NULL,
    [LastError] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Printers] PRIMARY KEY ([Id])
);

CREATE TABLE [ReceiptTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [BusinessName] nvarchar(200) NOT NULL,
    [BusinessSubtitle] nvarchar(200) NULL,
    [Address] nvarchar(500) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(100) NULL,
    [TaxPin] nvarchar(50) NULL,
    [FooterLine1] nvarchar(200) NULL,
    [FooterLine2] nvarchar(200) NULL,
    [FooterLine3] nvarchar(200) NULL,
    [ShowTaxBreakdown] bit NOT NULL,
    [ShowCashierName] bit NOT NULL,
    [ShowTableNumber] bit NOT NULL,
    [ShowQRCode] bit NOT NULL,
    [QRCodeContent] nvarchar(500) NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ReceiptTemplates] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [SalaryComponents] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ComponentType] nvarchar(20) NOT NULL,
    [IsFixed] bit NOT NULL,
    [DefaultAmount] decimal(18,2) NULL,
    [DefaultPercent] decimal(5,2) NULL,
    [IsTaxable] bit NOT NULL,
    [IsStatutory] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SalaryComponents] PRIMARY KEY ([Id])
);

CREATE TABLE [ScaleConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [ScaleType] int NOT NULL,
    [Protocol] int NOT NULL,
    [ConnectionString] nvarchar(200) NOT NULL,
    [BaudRate] int NULL,
    [DataBits] int NULL,
    [StopBits] int NULL,
    [Parity] nvarchar(20) NULL,
    [Port] int NULL,
    [WeightUnit] int NOT NULL,
    [Decimals] int NOT NULL,
    [MinWeight] decimal(18,4) NOT NULL,
    [MaxWeight] decimal(18,4) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastStatus] int NOT NULL,
    [LastConnectedAt] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ScaleConfigurations] PRIMARY KEY ([Id])
);

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [ContactPerson] nvarchar(100) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(100) NULL,
    [Address] nvarchar(500) NULL,
    [City] nvarchar(100) NULL,
    [Country] nvarchar(100) NULL,
    [TaxId] nvarchar(50) NULL,
    [BankAccount] nvarchar(50) NULL,
    [BankName] nvarchar(100) NULL,
    [PaymentTermDays] int NOT NULL,
    [CreditLimit] decimal(18,2) NOT NULL DEFAULT 0.0,
    [CurrentBalance] decimal(18,2) NOT NULL DEFAULT 0.0,
    [Notes] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);

CREATE TABLE [SystemConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Mode] int NOT NULL,
    [BusinessName] nvarchar(200) NOT NULL,
    [BusinessAddress] nvarchar(500) NULL,
    [BusinessPhone] nvarchar(50) NULL,
    [BusinessEmail] nvarchar(200) NULL,
    [TaxRegistrationNumber] nvarchar(100) NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'KES',
    [CurrencySymbol] nvarchar(10) NOT NULL DEFAULT N'Ksh',
    [EnableTableManagement] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EnableKitchenDisplay] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EnableWaiterAssignment] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EnableCourseSequencing] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableReservations] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableBarcodeAutoFocus] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableProductOffers] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableSupplierCredit] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableLoyaltyProgram] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableBatchExpiry] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableScaleIntegration] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnablePayroll] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableAccounting] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableMultiStore] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableCloudSync] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EnableKenyaETims] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EnableMpesa] bit NOT NULL DEFAULT CAST(1 AS bit),
    [SetupCompleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SetupCompletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY ([Id])
);

CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [SettingKey] nvarchar(100) NOT NULL,
    [SettingValue] nvarchar(max) NULL,
    [SettingType] nvarchar(50) NULL,
    [Category] nvarchar(50) NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [TierConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Tier] int NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [SpendThreshold] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PointsThreshold] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PointsMultiplier] decimal(5,2) NOT NULL DEFAULT 1.0,
    [DiscountPercent] decimal(5,2) NOT NULL DEFAULT 0.0,
    [FreeDelivery] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PriorityService] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SortOrder] int NOT NULL DEFAULT 0,
    [ColorCode] nvarchar(10) NULL,
    [IconName] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TierConfigurations] PRIMARY KEY ([Id])
);

CREATE TABLE [VoidReasons] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [RequiresNote] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DisplayOrder] int NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_VoidReasons] PRIMARY KEY ([Id])
);

CREATE TABLE [WeightedBarcodeConfigs] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Prefix] int NOT NULL,
    [Format] int NOT NULL,
    [ArticleCodeStart] int NOT NULL,
    [ArticleCodeLength] int NOT NULL,
    [ValueStart] int NOT NULL,
    [ValueLength] int NOT NULL,
    [ValueDecimals] int NOT NULL,
    [IsPrice] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_WeightedBarcodeConfigs] PRIMARY KEY ([Id])
);

CREATE TABLE [CategoryExpirySettings] (
    [Id] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [RequiresExpiryTracking] bit NOT NULL,
    [BlockExpiredSales] bit NOT NULL,
    [AllowManagerOverride] bit NOT NULL,
    [WarningDays] int NOT NULL,
    [CriticalDays] int NOT NULL,
    [ExpiredItemAction] int NOT NULL,
    [NearExpiryAction] int NOT NULL,
    [MinimumShelfLifeDaysOnReceipt] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CategoryExpirySettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CategoryExpirySettings_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [SKU] nvarchar(max) NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CategoryId] int NULL,
    [SellingPrice] decimal(18,2) NOT NULL,
    [CostPrice] decimal(18,2) NULL,
    [StockQuantity] int NOT NULL,
    [ReorderLevel] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL DEFAULT 16.0,
    [UnitOfMeasure] nvarchar(20) NOT NULL DEFAULT N'Each',
    [ImagePath] nvarchar(500) NULL,
    [Barcode] nvarchar(50) NULL,
    [MinStockLevel] decimal(18,3) NULL,
    [MaxStockLevel] decimal(18,3) NULL,
    [SupplierId] int NULL,
    [TrackInventory] bit NOT NULL DEFAULT CAST(1 AS bit),
    [KitchenStation] nvarchar(max) NULL,
    [IsCentralProduct] bit NOT NULL,
    [AllowStoreOverride] bit NOT NULL,
    [LastSyncTime] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [BankAccounts] (
    [Id] int NOT NULL IDENTITY,
    [BankName] nvarchar(max) NOT NULL,
    [Branch] nvarchar(max) NULL,
    [AccountNumber] nvarchar(max) NOT NULL,
    [AccountName] nvarchar(max) NOT NULL,
    [AccountType] nvarchar(max) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [SwiftCode] nvarchar(max) NULL,
    [MpesaShortCode] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [LastReconciledBalance] decimal(18,2) NULL,
    [LastReconciliationDate] datetime2 NULL,
    [ChartOfAccountId] int NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankAccounts_ChartOfAccounts_ChartOfAccountId] FOREIGN KEY ([ChartOfAccountId]) REFERENCES [ChartOfAccounts] ([Id])
);

CREATE TABLE [CashFlowMappings] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [ActivityType] int NOT NULL,
    [LineItem] nvarchar(max) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsInflow] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CashFlowMappings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashFlowMappings_ChartOfAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Sections] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [ColorCode] nvarchar(10) NOT NULL DEFAULT N'#4CAF50',
    [FloorId] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sections_Floors_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [Floors] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CustomerCreditAccounts] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NULL,
    [AccountNumber] nvarchar(max) NOT NULL,
    [BusinessName] nvarchar(max) NULL,
    [ContactName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [BillingAddress] nvarchar(max) NULL,
    [KRAPin] nvarchar(max) NULL,
    [CreditLimit] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [PaymentTermsDays] int NOT NULL,
    [Status] int NOT NULL,
    [AccountOpenedDate] datetime2 NOT NULL,
    [LastTransactionDate] datetime2 NULL,
    [LastPaymentDate] datetime2 NULL,
    [DefaultDiscountPercent] decimal(18,2) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CustomerCreditAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerCreditAccounts_LoyaltyMembers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [LoyaltyMembers] ([Id])
);

CREATE TABLE [Stores] (
    [Id] int NOT NULL IDENTITY,
    [StoreCode] nvarchar(20) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Address] nvarchar(200) NULL,
    [City] nvarchar(50) NULL,
    [Region] nvarchar(50) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Phone] nvarchar(max) NULL,
    [Email] nvarchar(100) NULL,
    [TaxRegistrationNumber] nvarchar(50) NULL,
    [EtimsDeviceSerial] nvarchar(50) NULL,
    [IsHeadquarters] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ReceivesCentralUpdates] bit NOT NULL DEFAULT CAST(1 AS bit),
    [LastSyncTime] datetime2 NULL,
    [TimeZone] nvarchar(50) NOT NULL DEFAULT N'Africa/Nairobi',
    [OpeningTime] time NULL,
    [ClosingTime] time NULL,
    [ManagerName] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [PricingZoneId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Stores] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stores_PricingZones_PricingZoneId] FOREIGN KEY ([PricingZoneId]) REFERENCES [PricingZones] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [KOTSettings] (
    [Id] int NOT NULL IDENTITY,
    [PrinterId] int NOT NULL,
    [TitleFontSize] nvarchar(20) NOT NULL,
    [ItemFontSize] nvarchar(20) NOT NULL,
    [ModifierFontSize] nvarchar(20) NOT NULL,
    [ShowTableNumber] bit NOT NULL,
    [ShowWaiterName] bit NOT NULL,
    [ShowOrderTime] bit NOT NULL,
    [ShowOrderNumber] bit NOT NULL,
    [ShowCategoryHeader] bit NOT NULL,
    [GroupByCategory] bit NOT NULL,
    [ShowQuantityLarge] bit NOT NULL,
    [ShowModifiersIndented] bit NOT NULL,
    [ShowNotesHighlighted] bit NOT NULL,
    [PrintRushOrders] bit NOT NULL,
    [HighlightAllergies] bit NOT NULL,
    [BeepOnPrint] bit NOT NULL,
    [BeepCount] int NOT NULL DEFAULT 2,
    [CopiesPerOrder] int NOT NULL DEFAULT 1,
    CONSTRAINT [PK_KOTSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KOTSettings_Printers_PrinterId] FOREIGN KEY ([PrinterId]) REFERENCES [Printers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PrinterCategoryMappings] (
    [Id] int NOT NULL IDENTITY,
    [PrinterId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PrinterCategoryMappings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrinterCategoryMappings_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PrinterCategoryMappings_Printers_PrinterId] FOREIGN KEY ([PrinterId]) REFERENCES [Printers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PrinterSettings] (
    [Id] int NOT NULL IDENTITY,
    [PrinterId] int NOT NULL,
    [UseEscPos] bit NOT NULL,
    [AutoCut] bit NOT NULL,
    [PartialCut] bit NOT NULL,
    [OpenCashDrawer] bit NOT NULL,
    [CutFeedLines] int NOT NULL DEFAULT 3,
    [PrintLogo] bit NOT NULL,
    [LogoBitmap] varbinary(max) NULL,
    [LogoWidth] int NOT NULL DEFAULT 200,
    [BeepOnPrint] bit NOT NULL,
    [BeepCount] int NOT NULL DEFAULT 1,
    [PrintDensity] int NOT NULL DEFAULT 7,
    CONSTRAINT [PK_PrinterSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrinterSettings_Printers_PrinterId] FOREIGN KEY ([PrinterId]) REFERENCES [Printers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AutomaticMarkdowns] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [CategoryId] int NULL,
    [RuleName] nvarchar(max) NOT NULL,
    [TriggerType] int NOT NULL,
    [TriggerTime] time NULL,
    [HoursBeforeClosing] int NULL,
    [DaysBeforeExpiry] int NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [FinalPrice] decimal(18,2) NULL,
    [ValidDaysOfWeek] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    [StackWithOtherMarkdowns] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_AutomaticMarkdowns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AutomaticMarkdowns_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]),
    CONSTRAINT [FK_AutomaticMarkdowns_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PLUCodes] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(10) NOT NULL,
    [ProductId] int NOT NULL,
    [DisplayName] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsWeighted] bit NOT NULL,
    [TareWeight] decimal(18,4) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PLUCodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PLUCodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProductBarcodes] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [Barcode] nvarchar(50) NOT NULL,
    [BarcodeType] int NOT NULL,
    [IsPrimary] bit NOT NULL,
    [PackSize] decimal(18,4) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ProductBarcodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBarcodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProductBatchConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [RequiresBatchTracking] bit NOT NULL,
    [RequiresExpiryDate] bit NOT NULL,
    [ExpiryWarningDays] int NOT NULL,
    [ExpiryCriticalDays] int NOT NULL,
    [ExpiredItemAction] int NOT NULL,
    [NearExpiryAction] int NOT NULL,
    [UseFifo] bit NOT NULL,
    [UseFefo] bit NOT NULL,
    [TrackManufactureDate] bit NOT NULL,
    [MinimumShelfLifeDaysOnReceipt] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ProductBatchConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBatchConfigurations_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ZonePrices] (
    [Id] int NOT NULL IDENTITY,
    [PricingZoneId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CostPrice] decimal(18,2) NULL,
    [MinimumPrice] decimal(18,2) NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [Reason] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ZonePrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ZonePrices_PricingZones_PricingZoneId] FOREIGN KEY ([PricingZoneId]) REFERENCES [PricingZones] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ZonePrices_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReconciliationMatchingRules] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [MatchOnReference] bit NOT NULL,
    [MatchOnAmount] bit NOT NULL,
    [AmountTolerance] decimal(18,2) NOT NULL,
    [MatchOnDate] bit NOT NULL,
    [DateToleranceDays] int NOT NULL,
    [MatchOnMpesaCode] bit NOT NULL,
    [MatchOnChequeNumber] bit NOT NULL,
    [DescriptionPattern] nvarchar(max) NULL,
    [MinimumConfidence] int NOT NULL,
    [BankAccountId] int NULL,
    [TransactionType] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReconciliationMatchingRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReconciliationMatchingRules_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id])
);

CREATE TABLE [CustomerStatements] (
    [Id] int NOT NULL IDENTITY,
    [CreditAccountId] int NOT NULL,
    [StatementNumber] nvarchar(max) NOT NULL,
    [PeriodStartDate] datetime2 NOT NULL,
    [PeriodEndDate] datetime2 NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [TotalCharges] decimal(18,2) NOT NULL,
    [TotalPayments] decimal(18,2) NOT NULL,
    [TotalCredits] decimal(18,2) NOT NULL,
    [ClosingBalance] decimal(18,2) NOT NULL,
    [GeneratedAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    [SentVia] nvarchar(max) NULL,
    [PdfPath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CustomerStatements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerStatements_CustomerCreditAccounts_CreditAccountId] FOREIGN KEY ([CreditAccountId]) REFERENCES [CustomerCreditAccounts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AirtelMoneyConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [MerchantCode] nvarchar(max) NOT NULL,
    [ClientId] nvarchar(max) NOT NULL,
    [ClientSecretEncrypted] nvarchar(max) NOT NULL,
    [ApiBaseUrl] nvarchar(max) NOT NULL,
    [CallbackUrl] nvarchar(max) NOT NULL,
    [CountryCode] nvarchar(max) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [Environment] nvarchar(max) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [TimeoutSeconds] int NOT NULL,
    [LastTestSuccessful] bit NULL,
    [LastTestedAt] datetime2 NULL,
    [LastTestError] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_AirtelMoneyConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AirtelMoneyConfigurations_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [CustomerDisplayConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [TerminalId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [DisplayType] nvarchar(max) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [ScreenWidth] int NOT NULL,
    [ScreenHeight] int NOT NULL,
    [BackgroundColor] nvarchar(max) NOT NULL,
    [PrimaryTextColor] nvarchar(max) NOT NULL,
    [AccentColor] nvarchar(max) NOT NULL,
    [FontFamily] nvarchar(max) NOT NULL,
    [HeaderFontSize] int NOT NULL,
    [ItemFontSize] int NOT NULL,
    [TotalFontSize] int NOT NULL,
    [ShowItemImages] bit NOT NULL,
    [ShowPromotionalMessages] bit NOT NULL,
    [PromotionalRotationSeconds] int NOT NULL,
    [ShowStoreLogo] bit NOT NULL,
    [LogoUrl] nvarchar(max) NULL,
    [WelcomeMessage] nvarchar(max) NULL,
    [ThankYouMessage] nvarchar(max) NULL,
    [IdleScreenType] nvarchar(max) NOT NULL,
    [IdleTimeoutSeconds] int NOT NULL,
    [ShowCurrencySymbol] bit NOT NULL,
    [CurrencySymbol] nvarchar(max) NOT NULL,
    [LayoutTemplate] nvarchar(max) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CustomerDisplayConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerDisplayConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DeadStockConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [SlowMovingDays] int NOT NULL,
    [NonMovingDays] int NOT NULL,
    [DeadStockDays] int NOT NULL,
    [MinStockValue] decimal(18,2) NOT NULL,
    [ExcludedCategoryIds] nvarchar(max) NULL,
    [ExcludedProductIds] nvarchar(max) NULL,
    [AnalysisFrequencyDays] int NOT NULL,
    [LastAnalysisDate] datetime2 NULL,
    [DefaultClearanceDiscountPercent] decimal(18,2) NOT NULL,
    [SendAlerts] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_DeadStockConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeadStockConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DeadStockItems] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Classification] int NOT NULL,
    [DaysSinceLastSale] int NOT NULL,
    [LastSaleDate] datetime2 NULL,
    [QuantityOnHand] decimal(18,2) NOT NULL,
    [StockValue] decimal(18,2) NOT NULL,
    [OldestStockAgeDays] int NULL,
    [RecommendedAction] nvarchar(max) NOT NULL,
    [SuggestedClearancePrice] decimal(18,2) NULL,
    [PotentialLoss] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ActionTaken] nvarchar(max) NULL,
    [ActionTakenDate] datetime2 NULL,
    [IdentifiedDate] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_DeadStockItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeadStockItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DeadStockItems_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EmailConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [SmtpHost] nvarchar(max) NOT NULL,
    [SmtpPort] int NOT NULL,
    [SmtpUsername] nvarchar(max) NULL,
    [SmtpPasswordEncrypted] nvarchar(max) NULL,
    [UseSsl] bit NOT NULL,
    [UseStartTls] bit NOT NULL,
    [FromAddress] nvarchar(max) NOT NULL,
    [FromName] nvarchar(max) NOT NULL,
    [ReplyToAddress] nvarchar(max) NULL,
    [TimeoutSeconds] int NOT NULL,
    [MaxRetryAttempts] int NOT NULL,
    [RetryDelayMinutes] int NOT NULL,
    [LastConnectionTest] datetime2 NULL,
    [ConnectionTestSuccessful] bit NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmailConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailConfigurations_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [EmailSchedules] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [ReportType] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [SendTime] time NOT NULL,
    [DayOfWeek] int NULL,
    [DayOfMonth] int NULL,
    [TimeZone] nvarchar(max) NOT NULL,
    [AlertFrequency] int NOT NULL,
    [LastExecutedAt] datetime2 NULL,
    [NextScheduledAt] datetime2 NULL,
    [CustomSubject] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmailSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailSchedules_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [EmailTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ReportType] int NOT NULL,
    [SubjectTemplate] nvarchar(max) NOT NULL,
    [HtmlBodyTemplate] nvarchar(max) NOT NULL,
    [PlainTextTemplate] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    [StoreId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailTemplates_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [ExpiryAlertConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [IsEnabled] bit NOT NULL,
    [AlertFrequency] int NOT NULL,
    [AlertThresholdDays] int NOT NULL,
    [UrgentThresholdDays] int NOT NULL,
    [MaxItemsPerEmail] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ExpiryAlertConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpiryAlertConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [Inventory] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [StoreId] int NOT NULL,
    [CurrentStock] decimal(18,3) NOT NULL DEFAULT 0.0,
    [ReservedStock] decimal(18,3) NOT NULL DEFAULT 0.0,
    [ReorderLevel] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Inventory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Inventory_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Inventory_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [InventoryTurnoverAnalyses] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NULL,
    [CategoryId] int NULL,
    [PeriodStart] datetime2 NOT NULL,
    [PeriodEnd] datetime2 NOT NULL,
    [COGS] decimal(18,2) NOT NULL,
    [AverageInventoryValue] decimal(18,2) NOT NULL,
    [TurnoverRatio] decimal(18,2) NOT NULL,
    [DaysSalesOfInventory] decimal(18,2) NOT NULL,
    [BenchmarkTurnover] decimal(18,2) NULL,
    [PerformanceRating] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_InventoryTurnoverAnalyses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTurnoverAnalyses_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]),
    CONSTRAINT [FK_InventoryTurnoverAnalyses_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
    CONSTRAINT [FK_InventoryTurnoverAnalyses_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [LowStockAlertConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [IsEnabled] bit NOT NULL,
    [AlertFrequency] int NOT NULL,
    [ThresholdPercent] int NOT NULL,
    [MinimumItemsForAlert] int NOT NULL,
    [MaxItemsPerEmail] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_LowStockAlertConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LowStockAlertConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [MarginThresholds] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [CategoryId] int NULL,
    [ProductId] int NULL,
    [MinMarginPercent] decimal(18,2) NOT NULL,
    [TargetMarginPercent] decimal(18,2) NOT NULL,
    [AlertOnBelowMinimum] bit NOT NULL,
    [AlertOnBelowTarget] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MarginThresholds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MarginThresholds_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]),
    CONSTRAINT [FK_MarginThresholds_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
    CONSTRAINT [FK_MarginThresholds_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [MobileMoneyTransactionLogs] (
    [Id] int NOT NULL IDENTITY,
    [Provider] int NOT NULL,
    [RequestId] int NULL,
    [StoreId] int NOT NULL,
    [TransactionReference] nvarchar(max) NOT NULL,
    [ProviderTransactionId] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [EntryType] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NULL,
    [LoggedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MobileMoneyTransactionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MobileMoneyTransactionLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OverheadAllocationRules] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [SourceAccountId] int NOT NULL,
    [AllocationBasis] nvarchar(max) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_OverheadAllocationRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OverheadAllocationRules_ChartOfAccounts_SourceAccountId] FOREIGN KEY ([SourceAccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OverheadAllocationRules_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [PMSConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [PMSType] int NOT NULL,
    [PropertyCode] nvarchar(max) NOT NULL,
    [ApiEndpoint] nvarchar(max) NOT NULL,
    [ApiUsername] nvarchar(max) NULL,
    [ApiPassword] nvarchar(max) NULL,
    [ApiKey] nvarchar(max) NULL,
    [TokenEndpoint] nvarchar(max) NULL,
    [AccessToken] nvarchar(max) NULL,
    [RefreshToken] nvarchar(max) NULL,
    [TokenExpiresAt] datetime2 NULL,
    [TimeoutSeconds] int NOT NULL,
    [MaxRetries] int NOT NULL,
    [RetryDelaySeconds] int NOT NULL,
    [AutoPostEnabled] bit NOT NULL,
    [Status] int NOT NULL,
    [LastConnectedAt] datetime2 NULL,
    [LastErrorMessage] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    [StoreId] int NULL,
    [AdditionalSettings] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PMSConfigurations_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [QuickAmountButtons] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ButtonType] nvarchar(max) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [ButtonColor] nvarchar(max) NULL,
    [TextColor] nvarchar(max) NULL,
    [KeyboardShortcut] nvarchar(max) NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_QuickAmountButtons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuickAmountButtons_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuickAmountButtonSets] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [ButtonIds] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_QuickAmountButtonSets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuickAmountButtonSets_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReorderRules] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ReorderPoint] decimal(18,2) NOT NULL,
    [ReorderQuantity] decimal(18,2) NOT NULL,
    [MaxStockLevel] decimal(18,2) NULL,
    [SafetyStock] decimal(18,2) NOT NULL,
    [LeadTimeDays] int NOT NULL,
    [PreferredSupplierId] int NULL,
    [IsAutoReorderEnabled] bit NOT NULL,
    [ConsolidateReorders] bit NOT NULL,
    [MinOrderQuantity] decimal(18,2) NULL,
    [OrderMultiple] decimal(18,2) NULL,
    [EconomicOrderQuantity] decimal(18,2) NULL,
    [LastCalculatedDate] datetime2 NULL,
    [AverageDailySales] decimal(18,2) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReorderRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReorderRules_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReorderRules_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReorderRules_Suppliers_PreferredSupplierId] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [Suppliers] ([Id])
);

CREATE TABLE [ScheduledPriceChanges] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [PricingZoneId] int NULL,
    [StoreId] int NULL,
    [OldPrice] decimal(18,2) NOT NULL,
    [NewPrice] decimal(18,2) NOT NULL,
    [NewCostPrice] decimal(18,2) NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [Status] int NOT NULL DEFAULT 0,
    [AppliedAt] datetime2 NULL,
    [AppliedByUserId] int NULL,
    [Reason] nvarchar(200) NULL,
    [Notes] nvarchar(500) NULL,
    [WasAutoApplied] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ScheduledPriceChanges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ScheduledPriceChanges_PricingZones_PricingZoneId] FOREIGN KEY ([PricingZoneId]) REFERENCES [PricingZones] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ScheduledPriceChanges_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ScheduledPriceChanges_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [ShrinkageAnalysisPeriods] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [PeriodStart] datetime2 NOT NULL,
    [PeriodEnd] datetime2 NOT NULL,
    [TotalShrinkageValue] decimal(18,2) NOT NULL,
    [TotalSales] decimal(18,2) NOT NULL,
    [ShrinkageRate] decimal(18,2) NOT NULL,
    [IncidentCount] int NOT NULL,
    [ProductsAffected] int NOT NULL,
    [PriorPeriodChange] decimal(18,2) NULL,
    [IndustryBenchmark] decimal(18,2) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ShrinkageAnalysisPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShrinkageAnalysisPeriods_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SplitPaymentConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [MaxSplitWays] int NOT NULL,
    [MinSplitAmount] decimal(18,2) NOT NULL,
    [AllowMixedPaymentMethods] bit NOT NULL,
    [AllowItemSplit] bit NOT NULL,
    [AllowEqualSplit] bit NOT NULL,
    [AllowCustomAmountSplit] bit NOT NULL,
    [AllowPercentageSplit] bit NOT NULL,
    [DefaultSplitMethod] int NOT NULL,
    [RequireAllPartiesPresent] bit NOT NULL,
    [PrintSeparateReceipts] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SplitPaymentConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SplitPaymentConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StockReservations] (
    [Id] int NOT NULL IDENTITY,
    [LocationId] int NOT NULL,
    [LocationType] int NOT NULL,
    [ProductId] int NOT NULL,
    [ReservedQuantity] int NOT NULL,
    [ReferenceId] int NOT NULL,
    [ReferenceType] int NOT NULL,
    [Status] int NOT NULL DEFAULT 1,
    [ReservedAt] datetime2 NOT NULL,
    [ReservedByUserId] int NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CompletedByUserId] int NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockReservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockReservations_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockReservations_Stores_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StockTransferRequests] (
    [Id] int NOT NULL IDENTITY,
    [RequestNumber] nvarchar(50) NOT NULL,
    [RequestingStoreId] int NOT NULL,
    [SourceLocationId] int NOT NULL,
    [SourceLocationType] int NOT NULL,
    [Status] int NOT NULL DEFAULT 0,
    [Priority] int NOT NULL DEFAULT 2,
    [Reason] int NOT NULL DEFAULT 1,
    [SubmittedAt] datetime2 NULL,
    [SubmittedByUserId] int NULL,
    [ApprovedAt] datetime2 NULL,
    [ApprovedByUserId] int NULL,
    [ApprovalNotes] nvarchar(1000) NULL,
    [RequestedDeliveryDate] datetime2 NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [Notes] nvarchar(2000) NULL,
    [RejectionReason] nvarchar(1000) NULL,
    [TotalItemsRequested] int NOT NULL,
    [TotalItemsApproved] int NOT NULL,
    [TotalEstimatedValue] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockTransferRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferRequests_Stores_RequestingStoreId] FOREIGN KEY ([RequestingStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTransferRequests_Stores_SourceLocationId] FOREIGN KEY ([SourceLocationId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StockValuationConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [DefaultMethod] int NOT NULL,
    [AutoCalculateOnMovement] bit NOT NULL,
    [IncludeTaxInCost] bit NOT NULL,
    [IncludeFreightInCost] bit NOT NULL,
    [StandardCostUpdateFrequencyDays] int NOT NULL,
    [LastStandardCostCalculation] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockValuationConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockValuationConfigs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StockValuationSnapshots] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [SnapshotDate] datetime2 NOT NULL,
    [Method] int NOT NULL,
    [TotalValue] decimal(18,2) NOT NULL,
    [TotalQuantity] decimal(18,2) NOT NULL,
    [SkuCount] int NOT NULL,
    [IsPeriodEnd] bit NOT NULL,
    [Period] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockValuationSnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockValuationSnapshots_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StoreProductOverrides] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NOT NULL,
    [OverridePrice] decimal(18,2) NULL,
    [OverrideCost] decimal(18,2) NULL,
    [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit),
    [OverrideMinStock] decimal(18,4) NULL,
    [OverrideMaxStock] decimal(18,4) NULL,
    [OverrideTaxRate] decimal(5,2) NULL,
    [OverrideKitchenStation] nvarchar(50) NULL,
    [OverrideReason] nvarchar(200) NULL,
    [LastSyncTime] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StoreProductOverrides] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StoreProductOverrides_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StoreProductOverrides_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyncBatches] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [Direction] int NOT NULL,
    [EntityType] int NOT NULL,
    [RecordCount] int NOT NULL,
    [ProcessedCount] int NOT NULL,
    [SuccessCount] int NOT NULL,
    [FailedCount] int NOT NULL,
    [ConflictCount] int NOT NULL,
    [Status] int NOT NULL DEFAULT 0,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [BatchData] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncBatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncBatches_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyncConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [SyncIntervalSeconds] int NOT NULL DEFAULT 30,
    [IsEnabled] bit NOT NULL,
    [AutoSyncOnStartup] bit NOT NULL,
    [MaxBatchSize] int NOT NULL DEFAULT 100,
    [RetryAttempts] int NOT NULL DEFAULT 3,
    [RetryDelaySeconds] int NOT NULL DEFAULT 60,
    [LastSuccessfulSync] datetime2 NULL,
    [LastAttemptedSync] datetime2 NULL,
    [LastSyncError] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncConfigurations_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TKashConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [MerchantId] nvarchar(max) NOT NULL,
    [ApiKey] nvarchar(max) NOT NULL,
    [ApiSecretEncrypted] nvarchar(max) NOT NULL,
    [ApiBaseUrl] nvarchar(max) NOT NULL,
    [CallbackUrl] nvarchar(max) NOT NULL,
    [Environment] nvarchar(max) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [TimeoutSeconds] int NOT NULL,
    [LastTestSuccessful] bit NULL,
    [LastTestedAt] datetime2 NULL,
    [LastTestError] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TKashConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TKashConfigurations_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [PIN] nvarchar(256) NULL,
    [FullName] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(max) NOT NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [FailedLoginAttempts] int NOT NULL DEFAULT 0,
    [LockoutEnd] datetime2 NULL,
    [LastLoginAt] datetime2 NULL,
    [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PasswordChangedAt] datetime2 NULL,
    [StoreId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [CustomerDisplayMessages] (
    [Id] int NOT NULL IDENTITY,
    [DisplayConfigId] int NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CustomerDisplayMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerDisplayMessages_CustomerDisplayConfigs_DisplayConfigId] FOREIGN KEY ([DisplayConfigId]) REFERENCES [CustomerDisplayConfigs] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EmailLogs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [ReportType] int NOT NULL,
    [Recipients] nvarchar(max) NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [RetryCount] int NOT NULL,
    [ScheduledAt] datetime2 NULL,
    [SentAt] datetime2 NULL,
    [GenerationTimeMs] int NULL,
    [BodySizeBytes] int NULL,
    [HasAttachment] bit NOT NULL,
    [AttachmentName] nvarchar(max) NULL,
    [EmailScheduleId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailLogs_EmailSchedules_EmailScheduleId] FOREIGN KEY ([EmailScheduleId]) REFERENCES [EmailSchedules] ([Id]),
    CONSTRAINT [FK_EmailLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [PMSGuestLookups] (
    [Id] int NOT NULL IDENTITY,
    [PMSConfigurationId] int NOT NULL,
    [RoomNumber] nvarchar(max) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [FolioNumber] nvarchar(max) NULL,
    [ConfirmationNumber] nvarchar(max) NULL,
    [PMSGuestId] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [CheckInDate] datetime2 NULL,
    [CheckOutDate] datetime2 NULL,
    [VIPStatus] nvarchar(max) NULL,
    [CreditLimit] decimal(18,2) NULL,
    [CurrentBalance] decimal(18,2) NULL,
    [AllowRoomCharges] bit NOT NULL,
    [ChargeBlockReason] nvarchar(max) NULL,
    [CompanyName] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CachedAt] datetime2 NOT NULL,
    [CacheExpiresAt] datetime2 NOT NULL,
    [RawResponse] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSGuestLookups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PMSGuestLookups_PMSConfigurations_PMSConfigurationId] FOREIGN KEY ([PMSConfigurationId]) REFERENCES [PMSConfigurations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PMSRevenueCenters] (
    [Id] int NOT NULL IDENTITY,
    [PMSConfigurationId] int NOT NULL,
    [StoreId] int NULL,
    [RevenueCenterCode] nvarchar(max) NOT NULL,
    [RevenueCenterName] nvarchar(max) NOT NULL,
    [DefaultChargeType] int NOT NULL,
    [TransactionCode] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSRevenueCenters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PMSRevenueCenters_PMSConfigurations_PMSConfigurationId] FOREIGN KEY ([PMSConfigurationId]) REFERENCES [PMSConfigurations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PMSRevenueCenters_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [StockTransferReceipts] (
    [Id] int NOT NULL IDENTITY,
    [TransferRequestId] int NOT NULL,
    [ReceiptNumber] nvarchar(50) NOT NULL,
    [ReceivedAt] datetime2 NOT NULL,
    [ReceivedByUserId] int NOT NULL,
    [IsComplete] bit NOT NULL,
    [HasIssues] bit NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockTransferReceipts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferReceipts_StockTransferRequests_TransferRequestId] FOREIGN KEY ([TransferRequestId]) REFERENCES [StockTransferRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StockTransferShipments] (
    [Id] int NOT NULL IDENTITY,
    [TransferRequestId] int NOT NULL,
    [ShipmentNumber] nvarchar(50) NOT NULL,
    [ShippedAt] datetime2 NULL,
    [ShippedByUserId] int NULL,
    [ExpectedArrivalDate] datetime2 NULL,
    [ActualArrivalDate] datetime2 NULL,
    [Carrier] nvarchar(100) NULL,
    [TrackingNumber] nvarchar(100) NULL,
    [VehicleDetails] nvarchar(200) NULL,
    [DriverName] nvarchar(100) NULL,
    [DriverContact] nvarchar(50) NULL,
    [PackageCount] int NOT NULL,
    [TotalWeightKg] decimal(10,2) NULL,
    [Notes] nvarchar(1000) NULL,
    [IsComplete] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockTransferShipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferShipments_StockTransferRequests_TransferRequestId] FOREIGN KEY ([TransferRequestId]) REFERENCES [StockTransferRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TransferActivityLogs] (
    [Id] int NOT NULL IDENTITY,
    [TransferRequestId] int NOT NULL,
    [Activity] nvarchar(200) NOT NULL,
    [PreviousStatus] int NULL,
    [NewStatus] int NULL,
    [PerformedByUserId] int NOT NULL,
    [PerformedAt] datetime2 NOT NULL,
    [Details] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TransferActivityLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferActivityLogs_StockTransferRequests_TransferRequestId] FOREIGN KEY ([TransferRequestId]) REFERENCES [StockTransferRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TransferRequestLines] (
    [Id] int NOT NULL IDENTITY,
    [TransferRequestId] int NOT NULL,
    [ProductId] int NOT NULL,
    [RequestedQuantity] int NOT NULL,
    [ApprovedQuantity] int NULL,
    [ShippedQuantity] int NOT NULL,
    [ReceivedQuantity] int NOT NULL,
    [IssueQuantity] int NOT NULL,
    [SourceAvailableStock] int NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [ApprovalNotes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TransferRequestLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferRequestLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TransferRequestLines_StockTransferRequests_TransferRequestId] FOREIGN KEY ([TransferRequestId]) REFERENCES [StockTransferRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StockValuationDetails] (
    [Id] int NOT NULL IDENTITY,
    [SnapshotId] int NOT NULL,
    [ProductId] int NOT NULL,
    [QuantityOnHand] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [TotalValue] decimal(18,2) NOT NULL,
    [WeightedAverageCost] decimal(18,2) NULL,
    [FifoCost] decimal(18,2) NULL,
    [StandardCost] decimal(18,2) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockValuationDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockValuationDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockValuationDetails_StockValuationSnapshots_SnapshotId] FOREIGN KEY ([SnapshotId]) REFERENCES [StockValuationSnapshots] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyncConflicts] (
    [Id] int NOT NULL IDENTITY,
    [SyncBatchId] int NOT NULL,
    [EntityType] int NOT NULL,
    [EntityId] int NOT NULL,
    [LocalData] nvarchar(max) NOT NULL,
    [RemoteData] nvarchar(max) NOT NULL,
    [LocalTimestamp] datetime2 NOT NULL,
    [RemoteTimestamp] datetime2 NOT NULL,
    [IsResolved] bit NOT NULL,
    [Resolution] int NULL,
    [ResolvedByUserId] int NULL,
    [ResolvedAt] datetime2 NULL,
    [ResolutionNotes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncConflicts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncConflicts_SyncBatches_SyncBatchId] FOREIGN KEY ([SyncBatchId]) REFERENCES [SyncBatches] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyncLogs] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [SyncBatchId] int NULL,
    [Operation] nvarchar(100) NOT NULL,
    [Details] nvarchar(2000) NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [Timestamp] datetime2 NOT NULL,
    [DurationMs] bigint NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SyncLogs_SyncBatches_SyncBatchId] FOREIGN KEY ([SyncBatchId]) REFERENCES [SyncBatches] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [SyncQueues] (
    [Id] int NOT NULL IDENTITY,
    [EntityType] nvarchar(max) NOT NULL,
    [EntityId] int NOT NULL,
    [OperationType] int NOT NULL,
    [Priority] int NOT NULL,
    [Status] int NOT NULL,
    [Payload] nvarchar(max) NULL,
    [RetryCount] int NOT NULL,
    [MaxRetries] int NOT NULL,
    [LastAttemptAt] datetime2 NULL,
    [LastError] nvarchar(max) NULL,
    [NextRetryAt] datetime2 NULL,
    [StoreId] int NULL,
    [CreatedByUserId] int NULL,
    [SyncBatchId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncQueues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncQueues_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_SyncQueues_SyncBatches_SyncBatchId] FOREIGN KEY ([SyncBatchId]) REFERENCES [SyncBatches] ([Id])
);

CREATE TABLE [SyncRecords] (
    [Id] int NOT NULL IDENTITY,
    [SyncBatchId] int NOT NULL,
    [EntityType] int NOT NULL,
    [EntityId] int NOT NULL,
    [EntityData] nvarchar(max) NOT NULL,
    [EntityTimestamp] datetime2 NOT NULL,
    [IsProcessed] bit NOT NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(1000) NULL,
    [ProcessedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncRecords_SyncBatches_SyncBatchId] FOREIGN KEY ([SyncBatchId]) REFERENCES [SyncBatches] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyncEntityRules] (
    [Id] int NOT NULL IDENTITY,
    [SyncConfigurationId] int NOT NULL,
    [EntityType] int NOT NULL,
    [Direction] int NOT NULL,
    [ConflictResolution] int NOT NULL DEFAULT 1,
    [FlagConflictsForReview] bit NOT NULL,
    [Priority] int NOT NULL DEFAULT 100,
    [IsEnabled] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SyncEntityRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncEntityRules_SyncConfigurations_SyncConfigurationId] FOREIGN KEY ([SyncConfigurationId]) REFERENCES [SyncConfigurations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AccountingPeriods] (
    [Id] int NOT NULL IDENTITY,
    [PeriodName] nvarchar(50) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Open',
    [ClosedByUserId] int NULL,
    [ClosedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_AccountingPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountingPeriods_Users_ClosedByUserId] FOREIGN KEY ([ClosedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ApiClients] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ClientId] nvarchar(max) NOT NULL,
    [ClientSecretHash] nvarchar(max) NOT NULL,
    [ApiKeyHash] nvarchar(max) NOT NULL,
    [AuthType] int NOT NULL,
    [Status] int NOT NULL,
    [AllowedIPs] nvarchar(max) NULL,
    [AllowedOrigins] nvarchar(max) NULL,
    [RateLimitPerMinute] int NOT NULL,
    [RateLimitPerHour] int NOT NULL,
    [RateLimitPerDay] int NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [StoreId] int NULL,
    [UserId] int NULL,
    [LastUsedAt] datetime2 NULL,
    [TotalRequests] bigint NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiClients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiClients_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_ApiClients_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [AuditLog] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] int NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(100) NULL,
    [EntityId] int NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [IpAddress] nvarchar(50) NULL,
    [MachineName] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLog_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [BankStatementImports] (
    [Id] int NOT NULL IDENTITY,
    [BankAccountId] int NOT NULL,
    [BatchId] nvarchar(max) NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FileFormat] nvarchar(max) NOT NULL,
    [StatementStartDate] datetime2 NOT NULL,
    [StatementEndDate] datetime2 NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [ClosingBalance] decimal(18,2) NOT NULL,
    [TotalTransactions] int NOT NULL,
    [TotalDeposits] decimal(18,2) NOT NULL,
    [TotalWithdrawals] decimal(18,2) NOT NULL,
    [ImportedCount] int NOT NULL,
    [SkippedCount] int NOT NULL,
    [FailedCount] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [ImportedByUserId] int NULL,
    [ImportedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BankStatementImports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankStatementImports_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BankStatementImports_Users_ImportedByUserId] FOREIGN KEY ([ImportedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Budgets] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [FiscalYear] int NOT NULL,
    [PeriodType] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [CreatedByUserId] int NULL,
    [ApprovedByUserId] int NULL,
    [ApprovedAt] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [IsBasedOnPriorYear] bit NOT NULL,
    [PriorYearAdjustmentPercent] decimal(18,2) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Budgets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Budgets_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_Budgets_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Budgets_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [CashDrawers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LinkedPrinterId] int NOT NULL,
    [DrawerPin] nvarchar(10) NOT NULL,
    [AutoOpenOnCashPayment] bit NOT NULL,
    [AutoOpenOnCashRefund] bit NOT NULL,
    [AutoOpenOnDrawerCount] bit NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [LastOpenedAt] datetime2 NULL,
    [LastOpenedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CashDrawers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashDrawers_Printers_LinkedPrinterId] FOREIGN KEY ([LinkedPrinterId]) REFERENCES [Printers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CashDrawers_Users_LastOpenedByUserId] FOREIGN KEY ([LastOpenedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [CentralPromotions] (
    [Id] int NOT NULL IDENTITY,
    [PromotionCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [InternalNotes] nvarchar(1000) NULL,
    [Type] int NOT NULL,
    [DiscountAmount] decimal(18,2) NULL,
    [DiscountPercent] decimal(5,2) NULL,
    [OfferPrice] decimal(18,2) NULL,
    [MinimumPurchase] decimal(18,2) NULL,
    [MinQuantity] int NOT NULL,
    [MaxQuantityPerTransaction] int NULL,
    [MaxTotalRedemptions] int NULL,
    [MaxRedemptionsPerCustomer] int NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] int NOT NULL DEFAULT 0,
    [ValidDaysOfWeek] nvarchar(50) NULL,
    [ValidFromTime] time NULL,
    [ValidToTime] time NULL,
    [RequiresCouponCode] bit NOT NULL,
    [CouponCode] nvarchar(50) NULL,
    [IsCombinableWithOtherPromotions] bit NOT NULL,
    [Priority] int NOT NULL,
    [IsCentrallyManaged] bit NOT NULL,
    [CreatedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CentralPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CentralPromotions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [CustomerPayments] (
    [Id] int NOT NULL IDENTITY,
    [CreditAccountId] int NOT NULL,
    [PaymentNumber] nvarchar(max) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethodId] int NOT NULL,
    [ExternalReference] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [AllocatedAmount] decimal(18,2) NOT NULL,
    [ReceivedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CustomerPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerPayments_CustomerCreditAccounts_CreditAccountId] FOREIGN KEY ([CreditAccountId]) REFERENCES [CustomerCreditAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CustomerPayments_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CustomerPayments_Users_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ManagerUserId] int NULL,
    [ParentDepartmentId] int NULL,
    [DisplayOrder] int NOT NULL,
    [IsProfitCenter] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    [AllocatedCategoryIds] nvarchar(max) NULL,
    [GLAccountId] int NULL,
    [ManagerId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_ChartOfAccounts_GLAccountId] FOREIGN KEY ([GLAccountId]) REFERENCES [ChartOfAccounts] ([Id]),
    CONSTRAINT [FK_Departments_Departments_ParentDepartmentId] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_Departments_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_Departments_Users_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [EmailRecipients] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NULL,
    [StoreId] int NULL,
    [UserId] int NULL,
    [ReceiveDailySales] bit NOT NULL,
    [ReceiveWeeklyReport] bit NOT NULL,
    [ReceiveLowStockAlerts] bit NOT NULL,
    [ReceiveExpiryAlerts] bit NOT NULL,
    [ReceiveMonthlyReport] bit NOT NULL,
    [IsCc] bit NOT NULL,
    [IsBcc] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmailRecipients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailRecipients_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_EmailRecipients_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [EmployeeNumber] nvarchar(20) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [NationalId] nvarchar(50) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(100) NULL,
    [Address] nvarchar(500) NULL,
    [DateOfBirth] datetime2 NULL,
    [HireDate] datetime2 NOT NULL,
    [TerminationDate] datetime2 NULL,
    [Department] nvarchar(50) NULL,
    [Position] nvarchar(100) NULL,
    [EmploymentType] nvarchar(20) NOT NULL DEFAULT N'FullTime',
    [BasicSalary] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PayFrequency] nvarchar(20) NOT NULL DEFAULT N'Monthly',
    [BankName] nvarchar(100) NULL,
    [BankAccountNumber] nvarchar(50) NULL,
    [TaxId] nvarchar(50) NULL,
    [NssfNumber] nvarchar(50) NULL,
    [NhifNumber] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [Expenses] (
    [Id] int NOT NULL IDENTITY,
    [ExpenseNumber] nvarchar(20) NOT NULL,
    [ExpenseCategoryId] int NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [ExpenseDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(50) NULL,
    [PaymentReference] nvarchar(100) NULL,
    [SupplierId] int NULL,
    [ReceiptImagePath] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [ApprovedByUserId] int NULL,
    [ApprovedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Expenses_ExpenseCategories_ExpenseCategoryId] FOREIGN KEY ([ExpenseCategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Expenses_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Expenses_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Expenses_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PayrollPeriods] (
    [Id] int NOT NULL IDENTITY,
    [PeriodName] nvarchar(50) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [PayDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
    [ProcessedByUserId] int NULL,
    [ApprovedByUserId] int NULL,
    [ProcessedAt] datetime2 NULL,
    [ApprovedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PayrollPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollPeriods_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PayrollPeriods_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProductOffers] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [OfferName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [PricingType] int NOT NULL DEFAULT 1,
    [OfferPrice] decimal(18,4) NOT NULL,
    [DiscountPercent] decimal(5,2) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [MinQuantity] int NOT NULL DEFAULT 1,
    [MaxQuantity] int NULL,
    [CreatedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ProductOffers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductOffers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductOffers_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [PONumber] nvarchar(30) NOT NULL,
    [SupplierId] int NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [ExpectedDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
    [SubTotal] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PaymentStatus] nvarchar(20) NOT NULL DEFAULT N'Unpaid',
    [AmountPaid] decimal(18,2) NOT NULL DEFAULT 0.0,
    [DueDate] datetime2 NULL,
    [PaidDate] datetime2 NULL,
    [InvoiceNumber] nvarchar(50) NULL,
    [CreatedByUserId] int NOT NULL,
    [ReceivedAt] datetime2 NULL,
    [Notes] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ReconciliationSessions] (
    [Id] int NOT NULL IDENTITY,
    [BankAccountId] int NOT NULL,
    [SessionNumber] nvarchar(max) NOT NULL,
    [PeriodStartDate] datetime2 NOT NULL,
    [PeriodEndDate] datetime2 NOT NULL,
    [StatementBalance] decimal(18,2) NOT NULL,
    [BookBalance] decimal(18,2) NOT NULL,
    [AdjustedBankBalance] decimal(18,2) NULL,
    [AdjustedBookBalance] decimal(18,2) NULL,
    [Difference] decimal(18,2) NOT NULL,
    [UnreconciledBankItems] decimal(18,2) NOT NULL,
    [UnreconciledPOSItems] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [MatchedCount] int NOT NULL,
    [UnmatchedCount] int NOT NULL,
    [DiscrepancyCount] int NOT NULL,
    [StartedByUserId] int NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedByUserId] int NULL,
    [CompletedAt] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReconciliationSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReconciliationSessions_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReconciliationSessions_Users_CompletedByUserId] FOREIGN KEY ([CompletedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ReconciliationSessions_Users_StartedByUserId] FOREIGN KEY ([StartedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [SavedReports] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [ReportType] nvarchar(max) NOT NULL,
    [ParametersJson] nvarchar(max) NULL,
    [CreatedByUserId] int NULL,
    [IsScheduled] bit NOT NULL,
    [ScheduleJson] nvarchar(max) NULL,
    [EmailRecipients] nvarchar(max) NULL,
    [LastRunAt] datetime2 NULL,
    [NextRunAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SavedReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedReports_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_SavedReports_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [StockMovements] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [StoreId] int NULL,
    [IsDeleted] bit NOT NULL,
    [MovementType] nvarchar(20) NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitCost] decimal(18,2) NULL,
    [PreviousStock] decimal(18,3) NOT NULL,
    [NewStock] decimal(18,3) NOT NULL,
    [ReferenceType] nvarchar(50) NULL,
    [ReferenceId] int NULL,
    [Reason] nvarchar(200) NULL,
    [Notes] nvarchar(500) NULL,
    [UserId] int NOT NULL,
    [AdjustmentReasonId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockMovements_AdjustmentReasons_AdjustmentReasonId] FOREIGN KEY ([AdjustmentReasonId]) REFERENCES [AdjustmentReasons] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockMovements_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_StockMovements_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StockTakes] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [StockTakeNumber] nvarchar(50) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [StartedByUserId] int NOT NULL,
    [ApprovedByUserId] int NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [TotalVarianceValue] decimal(18,2) NOT NULL,
    [ItemsWithVariance] int NOT NULL,
    [TotalItems] int NOT NULL,
    [CountedItems] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockTakes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTakes_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTakes_Users_StartedByUserId] FOREIGN KEY ([StartedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WorkPeriods] (
    [Id] int NOT NULL IDENTITY,
    [OpenedAt] datetime2 NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [ClosedAt] datetime2 NULL,
    [EndTime] datetime2 NULL,
    [OpenedByUserId] int NOT NULL,
    [ClosedByUserId] int NULL,
    [OpeningFloat] decimal(18,2) NOT NULL,
    [ClosingCash] decimal(18,2) NULL,
    [ExpectedCash] decimal(18,2) NULL,
    [Variance] decimal(18,2) NULL,
    [ZReportNumber] int NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Open',
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_WorkPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkPeriods_Users_ClosedByUserId] FOREIGN KEY ([ClosedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WorkPeriods_Users_OpenedByUserId] FOREIGN KEY ([OpenedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TransferReceiptLines] (
    [Id] int NOT NULL IDENTITY,
    [TransferReceiptId] int NOT NULL,
    [TransferRequestLineId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ExpectedQuantity] int NOT NULL,
    [ReceivedQuantity] int NOT NULL,
    [IssueQuantity] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TransferReceiptLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferReceiptLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TransferReceiptLines_StockTransferReceipts_TransferReceiptId] FOREIGN KEY ([TransferReceiptId]) REFERENCES [StockTransferReceipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransferReceiptLines_TransferRequestLines_TransferRequestLineId] FOREIGN KEY ([TransferRequestLineId]) REFERENCES [TransferRequestLines] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [JournalEntries] (
    [Id] int NOT NULL IDENTITY,
    [EntryNumber] nvarchar(20) NOT NULL,
    [EntryDate] datetime2 NOT NULL,
    [Description] nvarchar(500) NULL,
    [ReferenceType] nvarchar(50) NULL,
    [ReferenceId] int NULL,
    [AccountingPeriodId] int NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Posted',
    [IsPosted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalEntries_AccountingPeriods_AccountingPeriodId] FOREIGN KEY ([AccountingPeriodId]) REFERENCES [AccountingPeriods] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JournalEntries_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ApiAccessTokens] (
    [Id] int NOT NULL IDENTITY,
    [ApiClientId] int NOT NULL,
    [TokenHash] nvarchar(max) NOT NULL,
    [RefreshTokenHash] nvarchar(max) NULL,
    [TokenType] nvarchar(max) NOT NULL,
    [IssuedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [RefreshExpiresAt] datetime2 NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAt] datetime2 NULL,
    [ClientIP] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    [GrantedScopes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiAccessTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiAccessTokens_ApiClients_ApiClientId] FOREIGN KEY ([ApiClientId]) REFERENCES [ApiClients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ApiClientScopes] (
    [Id] int NOT NULL IDENTITY,
    [ApiClientId] int NOT NULL,
    [ApiScopeId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiClientScopes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiClientScopes_ApiClients_ApiClientId] FOREIGN KEY ([ApiClientId]) REFERENCES [ApiClients] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ApiClientScopes_ApiScopes_ApiScopeId] FOREIGN KEY ([ApiScopeId]) REFERENCES [ApiScopes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ApiRateLimitEntries] (
    [Id] int NOT NULL IDENTITY,
    [ApiClientId] int NOT NULL,
    [WindowType] nvarchar(max) NOT NULL,
    [WindowStart] datetime2 NOT NULL,
    [RequestCount] int NOT NULL,
    [LastRequestAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiRateLimitEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiRateLimitEntries_ApiClients_ApiClientId] FOREIGN KEY ([ApiClientId]) REFERENCES [ApiClients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ApiRequestLogs] (
    [Id] int NOT NULL IDENTITY,
    [ApiClientId] int NULL,
    [RequestId] nvarchar(max) NOT NULL,
    [HttpMethod] nvarchar(max) NOT NULL,
    [Path] nvarchar(max) NOT NULL,
    [QueryString] nvarchar(max) NULL,
    [RequestBody] nvarchar(max) NULL,
    [StatusCode] int NOT NULL,
    [ResponseBody] nvarchar(max) NULL,
    [DurationMs] int NOT NULL,
    [ClientIP] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    [RequestedAt] datetime2 NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [UserId] int NULL,
    [StoreId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ApiRequestLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiRequestLogs_ApiClients_ApiClientId] FOREIGN KEY ([ApiClientId]) REFERENCES [ApiClients] ([Id]),
    CONSTRAINT [FK_ApiRequestLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [WebhookConfigs] (
    [Id] int NOT NULL IDENTITY,
    [ApiClientId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [Secret] nvarchar(max) NULL,
    [Events] nvarchar(max) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [Headers] nvarchar(max) NULL,
    [RetryCount] int NOT NULL,
    [TimeoutSeconds] int NOT NULL,
    [LastTriggeredAt] datetime2 NULL,
    [SuccessCount] int NOT NULL,
    [FailureCount] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_WebhookConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebhookConfigs_ApiClients_ApiClientId] FOREIGN KEY ([ApiClientId]) REFERENCES [ApiClients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CashDrawerLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [CashDrawerId] int NOT NULL,
    [UserId] int NOT NULL,
    [Reason] nvarchar(20) NOT NULL,
    [Reference] nvarchar(50) NULL,
    [Notes] nvarchar(500) NULL,
    [OpenedAt] datetime2 NOT NULL,
    [AuthorizedByUserId] int NULL,
    [Success] bit NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    CONSTRAINT [PK_CashDrawerLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashDrawerLogs_CashDrawers_CashDrawerId] FOREIGN KEY ([CashDrawerId]) REFERENCES [CashDrawers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CashDrawerLogs_Users_AuthorizedByUserId] FOREIGN KEY ([AuthorizedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CashDrawerLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BogoPromotions] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [BogoType] int NOT NULL,
    [BuyQuantity] int NOT NULL,
    [GetQuantity] int NOT NULL,
    [DiscountPercentOnGetItems] decimal(18,2) NOT NULL,
    [MaxApplicationsPerTransaction] int NULL,
    [GetProductId] int NULL,
    [GetCategoryId] int NULL,
    [CheapestItemFree] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BogoPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BogoPromotions_Categories_GetCategoryId] FOREIGN KEY ([GetCategoryId]) REFERENCES [Categories] ([Id]),
    CONSTRAINT [FK_BogoPromotions_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BogoPromotions_Products_GetProductId] FOREIGN KEY ([GetProductId]) REFERENCES [Products] ([Id])
);

CREATE TABLE [ComboPromotions] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [ComboName] nvarchar(max) NOT NULL,
    [ComboPrice] decimal(18,2) NOT NULL,
    [OriginalTotalPrice] decimal(18,2) NOT NULL,
    [AllItemsRequired] bit NOT NULL,
    [MinItemsRequired] int NULL,
    [MaxPerTransaction] int NULL,
    [ComboPLU] nvarchar(max) NULL,
    [ImagePath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ComboPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComboPromotions_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CouponBatches] (
    [Id] int NOT NULL IDENTITY,
    [BatchName] nvarchar(max) NOT NULL,
    [PromotionId] int NULL,
    [CodePrefix] nvarchar(max) NOT NULL,
    [TotalCoupons] int NOT NULL,
    [RedeemedCount] int NOT NULL,
    [GeneratedAt] datetime2 NOT NULL,
    [GeneratedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CouponBatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CouponBatches_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]),
    CONSTRAINT [FK_CouponBatches_Users_GeneratedByUserId] FOREIGN KEY ([GeneratedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [MixMatchPromotions] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [MixMatchType] int NOT NULL,
    [RequiredQuantity] int NOT NULL,
    [FixedPrice] decimal(18,2) NULL,
    [DiscountPercent] decimal(18,2) NULL,
    [MaxApplicationsPerTransaction] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MixMatchPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MixMatchPromotions_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PromotionCategories] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PromotionCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionCategories_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PromotionDeployments] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [Scope] int NOT NULL,
    [DeployedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [Status] int NOT NULL DEFAULT 0,
    [StoresDeployedCount] int NOT NULL,
    [StoresFailedCount] int NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [Notes] nvarchar(500) NULL,
    [DeployedByUserId] int NULL,
    [OverwriteExisting] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PromotionDeployments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionDeployments_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionDeployments_Users_DeployedByUserId] FOREIGN KEY ([DeployedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PromotionProducts] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [ProductId] int NOT NULL,
    [IsQualifyingProduct] bit NOT NULL,
    [RequiredQuantity] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PromotionProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionProducts_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuantityBreakTiers] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [MinQuantity] int NOT NULL,
    [MaxQuantity] int NULL,
    [UnitPrice] decimal(18,2) NULL,
    [DiscountPercent] decimal(18,2) NULL,
    [DisplayLabel] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_QuantityBreakTiers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuantityBreakTiers_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BudgetLines] (
    [Id] int NOT NULL IDENTITY,
    [BudgetId] int NOT NULL,
    [AccountId] int NOT NULL,
    [DepartmentId] int NULL,
    [PeriodNumber] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BudgetLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BudgetLines_Budgets_BudgetId] FOREIGN KEY ([BudgetId]) REFERENCES [Budgets] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BudgetLines_ChartOfAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BudgetLines_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id])
);

CREATE TABLE [OverheadAllocationDetails] (
    [Id] int NOT NULL IDENTITY,
    [AllocationRuleId] int NOT NULL,
    [DepartmentId] int NOT NULL,
    [AllocationPercentage] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_OverheadAllocationDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OverheadAllocationDetails_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OverheadAllocationDetails_OverheadAllocationRules_AllocationRuleId] FOREIGN KEY ([AllocationRuleId]) REFERENCES [OverheadAllocationRules] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RecurringExpenseTemplates] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ExpenseCategoryId] int NOT NULL,
    [AccountId] int NOT NULL,
    [Amount] decimal(18,2) NULL,
    [IsVariableAmount] bit NOT NULL,
    [Frequency] nvarchar(max) NOT NULL,
    [DayOfMonth] int NULL,
    [DayOfWeek] int NULL,
    [AutoPost] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [LastGeneratedDate] datetime2 NULL,
    [NextScheduledDate] datetime2 NULL,
    [DepartmentId] int NULL,
    [SupplierId] int NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_RecurringExpenseTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecurringExpenseTemplates_ChartOfAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecurringExpenseTemplates_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_RecurringExpenseTemplates_ExpenseCategories_ExpenseCategoryId] FOREIGN KEY ([ExpenseCategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecurringExpenseTemplates_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_RecurringExpenseTemplates_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
);

CREATE TABLE [ShrinkageRecords] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ShrinkageDate] datetime2 NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [TotalValue] decimal(18,2) NOT NULL,
    [Type] int NOT NULL,
    [Cause] nvarchar(max) NULL,
    [SourceReference] nvarchar(max) NULL,
    [SourceType] nvarchar(max) NOT NULL,
    [IsInvestigated] bit NOT NULL,
    [InvestigationNotes] nvarchar(max) NULL,
    [RecoveredQuantity] decimal(18,2) NULL,
    [RecoveredValue] decimal(18,2) NULL,
    [RecordedByUserId] int NOT NULL,
    [DepartmentId] int NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ShrinkageRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShrinkageRecords_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_ShrinkageRecords_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ShrinkageRecords_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ShrinkageRecords_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Attendances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [AttendanceDate] datetime2 NOT NULL,
    [ClockIn] time NULL,
    [ClockOut] time NULL,
    [BreakStart] time NULL,
    [BreakEnd] time NULL,
    [HoursWorked] decimal(5,2) NOT NULL DEFAULT 0.0,
    [OvertimeHours] decimal(5,2) NOT NULL DEFAULT 0.0,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Present',
    [Notes] nvarchar(500) NULL,
    [IsManualEntry] bit NOT NULL,
    [ApprovedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attendances_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeSalaryComponents] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [SalaryComponentId] int NOT NULL,
    [Amount] decimal(18,2) NULL,
    [Percent] decimal(5,2) NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EmployeeSalaryComponents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeSalaryComponents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeSalaryComponents_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponents] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payslips] (
    [Id] int NOT NULL IDENTITY,
    [PayrollPeriodId] int NOT NULL,
    [EmployeeId] int NOT NULL,
    [BasicSalary] decimal(18,2) NOT NULL,
    [TotalEarnings] decimal(18,2) NOT NULL,
    [TotalDeductions] decimal(18,2) NOT NULL,
    [NetPay] decimal(18,2) NOT NULL,
    [PaymentStatus] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [PaymentMethod] nvarchar(50) NULL,
    [PaymentReference] nvarchar(100) NULL,
    [PaidAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Payslips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payslips_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payslips_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [GoodsReceivedNotes] (
    [Id] int NOT NULL IDENTITY,
    [GRNNumber] nvarchar(30) NOT NULL,
    [PurchaseOrderId] int NULL,
    [SupplierId] int NULL,
    [ReceivedDate] datetime2 NOT NULL,
    [DeliveryNote] nvarchar(100) NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [ReceivedByUserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_GoodsReceivedNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GoodsReceivedNotes_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GoodsReceivedNotes_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GoodsReceivedNotes_Users_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PurchaseOrderItems] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [OrderedQuantity] decimal(18,3) NOT NULL,
    [ReceivedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0,
    [UnitCost] decimal(18,2) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReorderSuggestions] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [ProductId] int NOT NULL,
    [SupplierId] int NULL,
    [CurrentStock] decimal(18,2) NOT NULL,
    [ReorderPoint] decimal(18,2) NOT NULL,
    [SuggestedQuantity] decimal(18,2) NOT NULL,
    [EstimatedCost] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Priority] nvarchar(max) NOT NULL,
    [DaysUntilStockout] int NULL,
    [PurchaseOrderId] int NULL,
    [ApprovedByUserId] int NULL,
    [ApprovedAt] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReorderSuggestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReorderSuggestions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReorderSuggestions_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]),
    CONSTRAINT [FK_ReorderSuggestions_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReorderSuggestions_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]),
    CONSTRAINT [FK_ReorderSuggestions_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [SupplierInvoices] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [SupplierId] int NOT NULL,
    [PurchaseOrderId] int NULL,
    [InvoiceDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Unpaid',
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SupplierInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierInvoices_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplierInvoices_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ReportExecutionLogs] (
    [Id] int NOT NULL IDENTITY,
    [SavedReportId] int NULL,
    [ReportType] nvarchar(max) NOT NULL,
    [ParametersJson] nvarchar(max) NULL,
    [UserId] int NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [DurationMs] int NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [OutputFormat] nvarchar(max) NULL,
    [FilePath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReportExecutionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportExecutionLogs_SavedReports_SavedReportId] FOREIGN KEY ([SavedReportId]) REFERENCES [SavedReports] ([Id]),
    CONSTRAINT [FK_ReportExecutionLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [StockTakeItems] (
    [Id] int NOT NULL IDENTITY,
    [StockTakeId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [ProductCode] nvarchar(50) NOT NULL,
    [SystemQuantity] decimal(18,3) NOT NULL,
    [PhysicalQuantity] decimal(18,3) NULL,
    [VarianceQuantity] decimal(18,3) NOT NULL,
    [Variance] decimal(18,2) NOT NULL,
    [CostPrice] decimal(18,2) NOT NULL,
    [VarianceValue] decimal(18,2) NOT NULL,
    [IsCounted] bit NOT NULL,
    [IsApproved] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CountedAt] datetime2 NULL,
    [CountedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_StockTakeItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTakeItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTakeItems_StockTakes_StockTakeId] FOREIGN KEY ([StockTakeId]) REFERENCES [StockTakes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTakeItems_Users_CountedByUserId] FOREIGN KEY ([CountedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] nvarchar(20) NOT NULL,
    [WorkPeriodId] int NULL,
    [UserId] int NOT NULL,
    [TableNumber] nvarchar(20) NULL,
    [CustomerName] nvarchar(100) NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Open',
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_WorkPeriods_WorkPeriodId] FOREIGN KEY ([WorkPeriodId]) REFERENCES [WorkPeriods] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TransferReceiptIssues] (
    [Id] int NOT NULL IDENTITY,
    [TransferReceiptId] int NOT NULL,
    [TransferReceiptLineId] int NOT NULL,
    [IssueType] int NOT NULL,
    [AffectedQuantity] int NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [PhotoPath] nvarchar(500) NULL,
    [IsResolved] bit NOT NULL,
    [ResolutionNotes] nvarchar(1000) NULL,
    [ResolvedAt] datetime2 NULL,
    [ResolvedByUserId] int NULL,
    [TransferReceiptId1] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TransferReceiptIssues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferReceiptIssues_StockTransferReceipts_TransferReceiptId] FOREIGN KEY ([TransferReceiptId]) REFERENCES [StockTransferReceipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransferReceiptIssues_StockTransferReceipts_TransferReceiptId1] FOREIGN KEY ([TransferReceiptId1]) REFERENCES [StockTransferReceipts] ([Id]),
    CONSTRAINT [FK_TransferReceiptIssues_TransferReceiptLines_TransferReceiptLineId] FOREIGN KEY ([TransferReceiptLineId]) REFERENCES [TransferReceiptLines] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [JournalEntryLines] (
    [Id] int NOT NULL IDENTITY,
    [JournalEntryId] int NOT NULL,
    [AccountId] int NOT NULL,
    [Description] nvarchar(200) NULL,
    [DebitAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [CreditAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_JournalEntryLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalEntryLines_ChartOfAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [ChartOfAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JournalEntryLines_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WebhookDeliveries] (
    [Id] int NOT NULL IDENTITY,
    [WebhookConfigId] int NOT NULL,
    [Event] nvarchar(max) NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [ResponseCode] int NULL,
    [ResponseBody] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [AttemptNumber] int NOT NULL,
    [DurationMs] int NULL,
    [DeliveredAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_WebhookDeliveries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebhookDeliveries_WebhookConfigs_WebhookConfigId] FOREIGN KEY ([WebhookConfigId]) REFERENCES [WebhookConfigs] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ComboItems] (
    [Id] int NOT NULL IDENTITY,
    [ComboPromotionId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [IsRequired] bit NOT NULL,
    [AddOnPrice] decimal(18,2) NULL,
    [DisplayOrder] int NOT NULL,
    [SubstitutionCategoryId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ComboItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComboItems_Categories_SubstitutionCategoryId] FOREIGN KEY ([SubstitutionCategoryId]) REFERENCES [Categories] ([Id]),
    CONSTRAINT [FK_ComboItems_ComboPromotions_ComboPromotionId] FOREIGN KEY ([ComboPromotionId]) REFERENCES [ComboPromotions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ComboItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Coupons] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NULL,
    [CouponCode] nvarchar(max) NOT NULL,
    [CouponType] int NOT NULL,
    [DiscountAmount] decimal(18,2) NULL,
    [DiscountPercent] decimal(18,2) NULL,
    [MinimumPurchase] decimal(18,2) NULL,
    [MaxDiscountAmount] decimal(18,2) NULL,
    [ValidFrom] datetime2 NOT NULL,
    [ValidTo] datetime2 NOT NULL,
    [MaxUses] int NULL,
    [UseCount] int NOT NULL,
    [CustomerId] int NULL,
    [BatchId] int NULL,
    [IsVoided] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Coupons_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]),
    CONSTRAINT [FK_Coupons_CouponBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [CouponBatches] ([Id]),
    CONSTRAINT [FK_Coupons_LoyaltyMembers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [LoyaltyMembers] ([Id])
);

CREATE TABLE [MixMatchGroups] (
    [Id] int NOT NULL IDENTITY,
    [MixMatchPromotionId] int NOT NULL,
    [GroupName] nvarchar(max) NOT NULL,
    [GroupType] nvarchar(max) NOT NULL,
    [MinQuantity] int NOT NULL,
    [MaxQuantity] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MixMatchGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MixMatchGroups_MixMatchPromotions_MixMatchPromotionId] FOREIGN KEY ([MixMatchPromotionId]) REFERENCES [MixMatchPromotions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DeploymentStores] (
    [Id] int NOT NULL IDENTITY,
    [DeploymentId] int NOT NULL,
    [StoreId] int NOT NULL,
    [Status] int NOT NULL DEFAULT 0,
    [SyncedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [RetryCount] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_DeploymentStores] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeploymentStores_PromotionDeployments_DeploymentId] FOREIGN KEY ([DeploymentId]) REFERENCES [PromotionDeployments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DeploymentStores_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DeploymentZones] (
    [Id] int NOT NULL IDENTITY,
    [DeploymentId] int NOT NULL,
    [PricingZoneId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_DeploymentZones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeploymentZones_PricingZones_PricingZoneId] FOREIGN KEY ([PricingZoneId]) REFERENCES [PricingZones] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DeploymentZones_PromotionDeployments_DeploymentId] FOREIGN KEY ([DeploymentId]) REFERENCES [PromotionDeployments] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RecurringExpenseEntries] (
    [Id] int NOT NULL IDENTITY,
    [TemplateId] int NOT NULL,
    [ExpenseId] int NULL,
    [ScheduledDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NULL,
    [Notes] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [ProcessedAt] datetime2 NULL,
    [ConfirmedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_RecurringExpenseEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecurringExpenseEntries_Expenses_ExpenseId] FOREIGN KEY ([ExpenseId]) REFERENCES [Expenses] ([Id]),
    CONSTRAINT [FK_RecurringExpenseEntries_RecurringExpenseTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [RecurringExpenseTemplates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecurringExpenseEntries_Users_ConfirmedByUserId] FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PayslipDetails] (
    [Id] int NOT NULL IDENTITY,
    [PayslipId] int NOT NULL,
    [SalaryComponentId] int NOT NULL,
    [ComponentType] nvarchar(20) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PayslipDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayslipDetails_Payslips_PayslipId] FOREIGN KEY ([PayslipId]) REFERENCES [Payslips] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PayslipDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponents] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProductBatches] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [StoreId] int NOT NULL,
    [BatchNumber] nvarchar(100) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [ManufactureDate] datetime2 NULL,
    [InitialQuantity] int NOT NULL,
    [CurrentQuantity] int NOT NULL,
    [ReservedQuantity] int NOT NULL,
    [SoldQuantity] int NOT NULL,
    [DisposedQuantity] int NOT NULL,
    [SupplierId] int NULL,
    [GrnId] int NULL,
    [TransferReceiptId] int NULL,
    [ReceivedAt] datetime2 NOT NULL,
    [ReceivedByUserId] int NOT NULL,
    [Status] int NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ProductBatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBatches_GoodsReceivedNotes_GrnId] FOREIGN KEY ([GrnId]) REFERENCES [GoodsReceivedNotes] ([Id]),
    CONSTRAINT [FK_ProductBatches_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductBatches_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductBatches_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
);

CREATE TABLE [GRNItems] (
    [Id] int NOT NULL IDENTITY,
    [GoodsReceivedNoteId] int NOT NULL,
    [PurchaseOrderItemId] int NULL,
    [ProductId] int NOT NULL,
    [OrderedQuantity] decimal(18,3) NOT NULL,
    [ReceivedQuantity] decimal(18,3) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_GRNItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GRNItems_GoodsReceivedNotes_GoodsReceivedNoteId] FOREIGN KEY ([GoodsReceivedNoteId]) REFERENCES [GoodsReceivedNotes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GRNItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GRNItems_PurchaseOrderItems_PurchaseOrderItemId] FOREIGN KEY ([PurchaseOrderItemId]) REFERENCES [PurchaseOrderItems] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SupplierPayments] (
    [Id] int NOT NULL IDENTITY,
    [SupplierInvoiceId] int NULL,
    [SupplierId] int NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] nvarchar(50) NULL,
    [Reference] nvarchar(100) NULL,
    [ProcessedByUserId] int NOT NULL,
    [Notes] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SupplierPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierPayments_SupplierInvoices_SupplierInvoiceId] FOREIGN KEY ([SupplierInvoiceId]) REFERENCES [SupplierInvoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplierPayments_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplierPayments_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Modifiers] nvarchar(500) NULL,
    [Notes] nvarchar(200) NULL,
    [BatchNumber] int NOT NULL,
    [PrintedToKitchen] bit NOT NULL,
    [OriginalUnitPrice] decimal(18,2) NULL,
    [AppliedOfferId] int NULL,
    [AppliedOfferName] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_ProductOffers_AppliedOfferId] FOREIGN KEY ([AppliedOfferId]) REFERENCES [ProductOffers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Receipts] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptNumber] nvarchar(20) NOT NULL,
    [OrderId] int NULL,
    [WorkPeriodId] int NULL,
    [StoreId] int NULL,
    [OwnerId] int NOT NULL,
    [TableNumber] nvarchar(20) NULL,
    [CustomerName] nvarchar(100) NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [IsVoided] bit NOT NULL,
    [IsPaid] bit NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [ChangeAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [VoidedAt] datetime2 NULL,
    [VoidedByUserId] int NULL,
    [VoidReason] nvarchar(200) NULL,
    [ParentReceiptId] int NULL,
    [IsSplit] bit NOT NULL,
    [SplitNumber] int NULL,
    [SplitType] int NULL,
    [MergedIntoReceiptId] int NULL,
    [IsMerged] bit NOT NULL,
    [CustomerId] int NULL,
    [LoyaltyMemberId] int NULL,
    [PointsEarned] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PointsRedeemed] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PointsDiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PointsBalanceAfter] decimal(18,2) NULL,
    [SettledAt] datetime2 NULL,
    [SettledByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Receipts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Receipts_LoyaltyMembers_LoyaltyMemberId] FOREIGN KEY ([LoyaltyMemberId]) REFERENCES [LoyaltyMembers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Receipts_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_Receipts_MergedIntoReceiptId] FOREIGN KEY ([MergedIntoReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_Receipts_ParentReceiptId] FOREIGN KEY ([ParentReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
    CONSTRAINT [FK_Receipts_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_Users_SettledByUserId] FOREIGN KEY ([SettledByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_Users_VoidedByUserId] FOREIGN KEY ([VoidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Receipts_WorkPeriods_WorkPeriodId] FOREIGN KEY ([WorkPeriodId]) REFERENCES [WorkPeriods] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MixMatchGroupCategories] (
    [Id] int NOT NULL IDENTITY,
    [MixMatchGroupId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MixMatchGroupCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MixMatchGroupCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MixMatchGroupCategories_MixMatchGroups_MixMatchGroupId] FOREIGN KEY ([MixMatchGroupId]) REFERENCES [MixMatchGroups] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MixMatchGroupProducts] (
    [Id] int NOT NULL IDENTITY,
    [MixMatchGroupId] int NOT NULL,
    [ProductId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MixMatchGroupProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MixMatchGroupProducts_MixMatchGroups_MixMatchGroupId] FOREIGN KEY ([MixMatchGroupId]) REFERENCES [MixMatchGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MixMatchGroupProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BatchDisposals] (
    [Id] int NOT NULL IDENTITY,
    [BatchId] int NOT NULL,
    [StoreId] int NOT NULL,
    [Quantity] int NOT NULL,
    [Reason] int NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [DisposedAt] datetime2 NOT NULL,
    [ApprovedByUserId] int NOT NULL,
    [DisposedByUserId] int NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [IsWitnessed] bit NOT NULL,
    [WitnessName] nvarchar(100) NULL,
    [PhotoPath] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BatchDisposals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BatchDisposals_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BatchDisposals_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BatchRecallAlerts] (
    [Id] int NOT NULL IDENTITY,
    [BatchId] int NOT NULL,
    [ProductId] int NOT NULL,
    [BatchNumber] nvarchar(100) NOT NULL,
    [RecallReason] nvarchar(1000) NOT NULL,
    [Severity] int NOT NULL,
    [Status] int NOT NULL,
    [IssuedAt] datetime2 NOT NULL,
    [IssuedByUserId] int NOT NULL,
    [AffectedQuantity] int NOT NULL,
    [QuantityRecovered] int NOT NULL,
    [QuantitySold] int NOT NULL,
    [QuantityInStock] int NOT NULL,
    [ExternalReference] nvarchar(100) NULL,
    [SupplierContactInfo] nvarchar(500) NULL,
    [ResolutionNotes] nvarchar(2000) NULL,
    [ResolvedAt] datetime2 NULL,
    [ResolvedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BatchRecallAlerts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BatchRecallAlerts_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BatchRecallAlerts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BatchStockMovements] (
    [Id] int NOT NULL IDENTITY,
    [BatchId] int NOT NULL,
    [ProductId] int NOT NULL,
    [StoreId] int NOT NULL,
    [MovementType] int NOT NULL,
    [Quantity] int NOT NULL,
    [QuantityBefore] int NOT NULL,
    [QuantityAfter] int NOT NULL,
    [ReferenceType] nvarchar(50) NOT NULL,
    [ReferenceId] int NOT NULL,
    [ReferenceNumber] nvarchar(50) NULL,
    [MovedAt] datetime2 NOT NULL,
    [MovedByUserId] int NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BatchStockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BatchStockMovements_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BatchStockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BatchStockMovements_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ExpirySaleBlocks] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [BatchId] int NOT NULL,
    [StoreId] int NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [DaysExpired] int NOT NULL,
    [AttemptedByUserId] int NOT NULL,
    [AttemptedAt] datetime2 NOT NULL,
    [AttemptedQuantity] int NOT NULL,
    [WasBlocked] bit NOT NULL,
    [OverrideApplied] bit NOT NULL,
    [OverrideByUserId] int NULL,
    [OverrideAt] datetime2 NULL,
    [OverrideReason] nvarchar(500) NULL,
    [ReceiptId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ExpirySaleBlocks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpirySaleBlocks_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExpirySaleBlocks_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExpirySaleBlocks_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AirtelMoneyRequests] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NULL,
    [StoreId] int NOT NULL,
    [TransactionReference] nvarchar(max) NOT NULL,
    [AirtelTransactionId] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [RawRequest] nvarchar(max) NULL,
    [RawResponse] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [ErrorCode] nvarchar(max) NULL,
    [CallbackData] nvarchar(max) NULL,
    [CallbackReceivedAt] datetime2 NULL,
    [StatusCheckAttempts] int NOT NULL,
    [UserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_AirtelMoneyRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AirtelMoneyRequests_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_AirtelMoneyRequests_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AirtelMoneyRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [CouponRedemptions] (
    [Id] int NOT NULL IDENTITY,
    [CouponId] int NOT NULL,
    [ReceiptId] int NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [RedeemedAt] datetime2 NOT NULL,
    [RedeemedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CouponRedemptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CouponRedemptions_Coupons_CouponId] FOREIGN KEY ([CouponId]) REFERENCES [Coupons] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CouponRedemptions_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CouponRedemptions_Users_RedeemedByUserId] FOREIGN KEY ([RedeemedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [CreditTransactions] (
    [Id] int NOT NULL IDENTITY,
    [CreditAccountId] int NOT NULL,
    [TransactionType] int NOT NULL,
    [ReferenceNumber] nvarchar(max) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ReceiptId] int NULL,
    [PaymentId] int NULL,
    [RunningBalance] decimal(18,2) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [ProcessedByUserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_CreditTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CreditTransactions_CustomerCreditAccounts_CreditAccountId] FOREIGN KEY ([CreditAccountId]) REFERENCES [CustomerCreditAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CreditTransactions_CustomerPayments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [CustomerPayments] ([Id]),
    CONSTRAINT [FK_CreditTransactions_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_CreditTransactions_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [EtimsInvoices] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [DeviceId] int NOT NULL,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [InternalReceiptNumber] nvarchar(50) NOT NULL,
    [InvoiceDate] datetime2 NOT NULL,
    [DocumentType] int NOT NULL,
    [CustomerType] int NOT NULL,
    [CustomerPin] nvarchar(20) NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [CustomerPhone] nvarchar(20) NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [StandardRatedAmount] decimal(18,2) NOT NULL,
    [ZeroRatedAmount] decimal(18,2) NOT NULL,
    [ExemptAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SubmissionAttempts] int NOT NULL,
    [LastSubmissionAttempt] datetime2 NULL,
    [SubmittedAt] datetime2 NULL,
    [ReceiptSignature] nvarchar(500) NULL,
    [KraInternalData] nvarchar(max) NULL,
    [QrCode] nvarchar(500) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsInvoices_EtimsDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [EtimsDevices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EtimsInvoices_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoyaltyTransactions] (
    [Id] int NOT NULL IDENTITY,
    [LoyaltyMemberId] int NOT NULL,
    [ReceiptId] int NULL,
    [TransactionType] int NOT NULL,
    [Points] decimal(18,2) NOT NULL,
    [MonetaryValue] decimal(18,2) NOT NULL,
    [BalanceAfter] decimal(18,2) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ReferenceNumber] nvarchar(50) NULL,
    [BonusPoints] decimal(18,2) NOT NULL DEFAULT 0.0,
    [BonusMultiplier] decimal(5,2) NOT NULL DEFAULT 1.0,
    [TransactionDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ProcessedByUserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_LoyaltyTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoyaltyTransactions_LoyaltyMembers_LoyaltyMemberId] FOREIGN KEY ([LoyaltyMemberId]) REFERENCES [LoyaltyMembers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoyaltyTransactions_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_LoyaltyTransactions_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [PaymentMethodId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [TenderedAmount] decimal(18,2) NOT NULL,
    [ChangeAmount] decimal(18,2) NOT NULL,
    [Reference] nvarchar(50) NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [ProcessedByUserId] int NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [UserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Payments_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PromotionApplications] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [PromotionId] int NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [ApplicationCount] int NOT NULL,
    [ApplicationDetails] nvarchar(max) NULL,
    [AppliedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PromotionApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionApplications_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionApplications_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReceiptItems] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [OrderItemId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(100) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [Modifiers] nvarchar(500) NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReceiptItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReceiptItems_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptItems_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReceiptVoids] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [VoidReasonId] int NOT NULL,
    [AdditionalNotes] nvarchar(500) NULL,
    [VoidedByUserId] int NOT NULL,
    [AuthorizedByUserId] int NULL,
    [VoidedAmount] decimal(18,2) NOT NULL,
    [VoidedAt] datetime2 NOT NULL,
    [StockRestored] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReceiptVoids] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReceiptVoids_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptVoids_Users_AuthorizedByUserId] FOREIGN KEY ([AuthorizedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptVoids_Users_VoidedByUserId] FOREIGN KEY ([VoidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptVoids_VoidReasons_VoidReasonId] FOREIGN KEY ([VoidReasonId]) REFERENCES [VoidReasons] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RoomChargePostings] (
    [Id] int NOT NULL IDENTITY,
    [PMSConfigurationId] int NOT NULL,
    [PostingReference] nvarchar(max) NOT NULL,
    [RoomNumber] nvarchar(max) NOT NULL,
    [GuestName] nvarchar(max) NOT NULL,
    [FolioNumber] nvarchar(max) NULL,
    [ConfirmationNumber] nvarchar(max) NULL,
    [ChargeType] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [ServiceCharge] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ReceiptId] int NULL,
    [OrderId] int NULL,
    [RevenueCenterCode] nvarchar(max) NULL,
    [TransactionCode] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [AttemptCount] int NOT NULL,
    [LastAttemptAt] datetime2 NULL,
    [PostedAt] datetime2 NULL,
    [PMSTransactionId] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [ProcessedByUserId] int NULL,
    [SignatureData] nvarchar(max) NULL,
    [AdditionalData] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_RoomChargePostings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomChargePostings_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_RoomChargePostings_PMSConfigurations_PMSConfigurationId] FOREIGN KEY ([PMSConfigurationId]) REFERENCES [PMSConfigurations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoomChargePostings_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_RoomChargePostings_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [SplitPaymentSessions] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [SplitMethod] int NOT NULL,
    [NumberOfSplits] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [IsComplete] bit NOT NULL,
    [InitiatedByUserId] int NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SplitPaymentSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SplitPaymentSessions_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SplitPaymentSessions_Users_InitiatedByUserId] FOREIGN KEY ([InitiatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SuspendedTransactions] (
    [Id] int NOT NULL IDENTITY,
    [StoreId] int NOT NULL,
    [TerminalId] int NULL,
    [ParkedByUserId] int NOT NULL,
    [ReferenceNumber] nvarchar(max) NOT NULL,
    [CustomerName] nvarchar(max) NULL,
    [TableNumber] nvarchar(max) NULL,
    [OrderType] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [ItemCount] int NOT NULL,
    [Status] int NOT NULL,
    [ParkedAt] datetime2 NOT NULL,
    [RecalledAt] datetime2 NULL,
    [RecalledByUserId] int NULL,
    [CompletedReceiptId] int NULL,
    [ExpiresAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [LoyaltyMemberId] int NULL,
    [AppliedPromotionIds] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SuspendedTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspendedTransactions_LoyaltyMembers_LoyaltyMemberId] FOREIGN KEY ([LoyaltyMemberId]) REFERENCES [LoyaltyMembers] ([Id]),
    CONSTRAINT [FK_SuspendedTransactions_Receipts_CompletedReceiptId] FOREIGN KEY ([CompletedReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_SuspendedTransactions_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SuspendedTransactions_Users_ParkedByUserId] FOREIGN KEY ([ParkedByUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SuspendedTransactions_Users_RecalledByUserId] FOREIGN KEY ([RecalledByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Tables] (
    [Id] int NOT NULL IDENTITY,
    [TableNumber] nvarchar(20) NOT NULL,
    [Capacity] int NOT NULL DEFAULT 4,
    [FloorId] int NOT NULL,
    [SectionId] int NULL,
    [GridX] int NOT NULL DEFAULT 0,
    [GridY] int NOT NULL DEFAULT 0,
    [Width] int NOT NULL DEFAULT 1,
    [Height] int NOT NULL DEFAULT 1,
    [Shape] int NOT NULL DEFAULT 0,
    [Status] int NOT NULL DEFAULT 0,
    [CurrentReceiptId] int NULL,
    [AssignedUserId] int NULL,
    [OccupiedSince] datetime2 NULL,
    [GuestCount] int NULL,
    [RowVersion] rowversion NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_Tables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tables_Floors_FloorId] FOREIGN KEY ([FloorId]) REFERENCES [Floors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Tables_Receipts_CurrentReceiptId] FOREIGN KEY ([CurrentReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tables_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tables_Users_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [TKashRequests] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NULL,
    [StoreId] int NOT NULL,
    [TransactionReference] nvarchar(max) NOT NULL,
    [TKashTransactionId] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [RawRequest] nvarchar(max) NULL,
    [RawResponse] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [ErrorCode] nvarchar(max) NULL,
    [CallbackData] nvarchar(max) NULL,
    [CallbackReceivedAt] datetime2 NULL,
    [StatusCheckAttempts] int NOT NULL,
    [UserId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_TKashRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TKashRequests_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_TKashRequests_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TKashRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [RecallActions] (
    [Id] int NOT NULL IDENTITY,
    [RecallAlertId] int NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [StoreId] int NULL,
    [Quantity] int NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ActionDate] datetime2 NOT NULL,
    [PerformedByUserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_RecallActions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecallActions_BatchRecallAlerts_RecallAlertId] FOREIGN KEY ([RecallAlertId]) REFERENCES [BatchRecallAlerts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecallActions_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
);

CREATE TABLE [PaymentAllocations] (
    [Id] int NOT NULL IDENTITY,
    [PaymentId] int NOT NULL,
    [TransactionId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [AllocationDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PaymentAllocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentAllocations_CreditTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CreditTransactions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PaymentAllocations_CustomerPayments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [CustomerPayments] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EtimsCreditNotes] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptVoidId] int NULL,
    [OriginalInvoiceId] int NOT NULL,
    [DeviceId] int NOT NULL,
    [CreditNoteNumber] nvarchar(50) NOT NULL,
    [OriginalInvoiceNumber] nvarchar(50) NOT NULL,
    [CreditNoteDate] datetime2 NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [CustomerPin] nvarchar(20) NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SubmissionAttempts] int NOT NULL,
    [LastSubmissionAttempt] datetime2 NULL,
    [SubmittedAt] datetime2 NULL,
    [KraSignature] nvarchar(500) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsCreditNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsCreditNotes_EtimsDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [EtimsDevices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EtimsCreditNotes_EtimsInvoices_OriginalInvoiceId] FOREIGN KEY ([OriginalInvoiceId]) REFERENCES [EtimsInvoices] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EtimsInvoiceItems] (
    [Id] int NOT NULL IDENTITY,
    [EtimsInvoiceId] int NOT NULL,
    [SequenceNumber] int NOT NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemDescription] nvarchar(200) NOT NULL,
    [HsCode] nvarchar(20) NULL,
    [UnitOfMeasure] nvarchar(10) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxType] int NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsInvoiceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsInvoiceItems_EtimsInvoices_EtimsInvoiceId] FOREIGN KEY ([EtimsInvoiceId]) REFERENCES [EtimsInvoices] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BankTransactions] (
    [Id] int NOT NULL IDENTITY,
    [BankAccountId] int NOT NULL,
    [TransactionType] int NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [ValueDate] datetime2 NULL,
    [BankReference] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DepositAmount] decimal(18,2) NULL,
    [WithdrawalAmount] decimal(18,2) NULL,
    [RunningBalance] decimal(18,2) NULL,
    [ChequeNumber] nvarchar(max) NULL,
    [PayeePayer] nvarchar(max) NULL,
    [MpesaCode] nvarchar(max) NULL,
    [MatchStatus] int NOT NULL,
    [MatchedPaymentId] int NULL,
    [ReconciliationSessionId] int NULL,
    [ImportBatchId] nvarchar(max) NULL,
    [ImportedAt] datetime2 NOT NULL,
    [SourceFileName] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_BankTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankTransactions_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BankTransactions_Payments_MatchedPaymentId] FOREIGN KEY ([MatchedPaymentId]) REFERENCES [Payments] ([Id]),
    CONSTRAINT [FK_BankTransactions_ReconciliationSessions_ReconciliationSessionId] FOREIGN KEY ([ReconciliationSessionId]) REFERENCES [ReconciliationSessions] ([Id])
);

CREATE TABLE [MpesaStkPushRequests] (
    [Id] int NOT NULL IDENTITY,
    [PaymentId] int NULL,
    [ReceiptId] int NULL,
    [ConfigurationId] int NOT NULL,
    [MerchantRequestId] nvarchar(100) NOT NULL,
    [CheckoutRequestId] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [AccountReference] nvarchar(50) NOT NULL,
    [TransactionDescription] nvarchar(200) NOT NULL,
    [Status] int NOT NULL,
    [ResponseCode] nvarchar(20) NULL,
    [ResponseDescription] nvarchar(500) NULL,
    [ResultCode] nvarchar(20) NULL,
    [ResultDescription] nvarchar(500) NULL,
    [MpesaReceiptNumber] nvarchar(50) NULL,
    [TransactionDate] datetime2 NULL,
    [PhoneNumberUsed] nvarchar(20) NULL,
    [RequestedAt] datetime2 NOT NULL,
    [CallbackReceivedAt] datetime2 NULL,
    [QueryAttempts] int NOT NULL,
    [LastQueryAt] datetime2 NULL,
    [RequestJson] nvarchar(max) NULL,
    [ResponseJson] nvarchar(max) NULL,
    [CallbackJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MpesaStkPushRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MpesaStkPushRequests_MpesaConfigurations_ConfigurationId] FOREIGN KEY ([ConfigurationId]) REFERENCES [MpesaConfigurations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MpesaStkPushRequests_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MpesaStkPushRequests_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [PromotionRedemptions] (
    [Id] int NOT NULL IDENTITY,
    [PromotionId] int NOT NULL,
    [StoreId] int NOT NULL,
    [ReceiptId] int NOT NULL,
    [ReceiptItemId] int NULL,
    [OriginalAmount] decimal(18,2) NOT NULL,
    [DiscountGiven] decimal(18,2) NOT NULL,
    [FinalAmount] decimal(18,2) NOT NULL,
    [QuantityApplied] int NOT NULL,
    [RedeemedAt] datetime2 NOT NULL,
    [CouponCodeUsed] nvarchar(50) NULL,
    [LoyaltyMemberId] int NULL,
    [ProcessedByUserId] int NULL,
    [IsVoided] bit NOT NULL,
    [VoidedAt] datetime2 NULL,
    [VoidedByUserId] int NULL,
    [VoidReason] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PromotionRedemptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionRedemptions_CentralPromotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [CentralPromotions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PromotionRedemptions_LoyaltyMembers_LoyaltyMemberId] FOREIGN KEY ([LoyaltyMemberId]) REFERENCES [LoyaltyMembers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_PromotionRedemptions_ReceiptItems_ReceiptItemId] FOREIGN KEY ([ReceiptItemId]) REFERENCES [ReceiptItems] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_PromotionRedemptions_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PromotionRedemptions_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PromotionRedemptions_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_PromotionRedemptions_Users_VoidedByUserId] FOREIGN KEY ([VoidedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PMSActivityLogs] (
    [Id] int NOT NULL IDENTITY,
    [PMSConfigurationId] int NOT NULL,
    [ActivityType] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [RoomChargePostingId] int NULL,
    [RequestPayload] nvarchar(max) NULL,
    [ResponsePayload] nvarchar(max) NULL,
    [HttpStatusCode] int NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [DurationMs] int NULL,
    [UserId] int NULL,
    [IpAddress] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSActivityLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PMSActivityLogs_PMSConfigurations_PMSConfigurationId] FOREIGN KEY ([PMSConfigurationId]) REFERENCES [PMSConfigurations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PMSActivityLogs_RoomChargePostings_RoomChargePostingId] FOREIGN KEY ([RoomChargePostingId]) REFERENCES [RoomChargePostings] ([Id]),
    CONSTRAINT [FK_PMSActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PMSPostingQueues] (
    [Id] int NOT NULL IDENTITY,
    [RoomChargePostingId] int NOT NULL,
    [Priority] int NOT NULL,
    [ScheduledAt] datetime2 NOT NULL,
    [Attempts] int NOT NULL,
    [MaxAttempts] int NOT NULL,
    [IsProcessing] bit NOT NULL,
    [ProcessingStartedAt] datetime2 NULL,
    [LastError] nvarchar(max) NULL,
    [NextRetryAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_PMSPostingQueues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PMSPostingQueues_RoomChargePostings_RoomChargePostingId] FOREIGN KEY ([RoomChargePostingId]) REFERENCES [RoomChargePostings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SplitPaymentParts] (
    [Id] int NOT NULL IDENTITY,
    [SplitSessionId] int NOT NULL,
    [PartNumber] int NOT NULL,
    [PayerName] nvarchar(max) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IsPaid] bit NOT NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [PaymentReference] nvarchar(max) NULL,
    [PaidAt] datetime2 NULL,
    [IncludedItemIds] nvarchar(max) NULL,
    [Percentage] decimal(18,2) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SplitPaymentParts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SplitPaymentParts_SplitPaymentSessions_SplitSessionId] FOREIGN KEY ([SplitSessionId]) REFERENCES [SplitPaymentSessions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SuspendedTransactionItems] (
    [Id] int NOT NULL IDENTITY,
    [SuspendedTransactionId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(max) NOT NULL,
    [ProductCode] nvarchar(max) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [ModifiersJson] nvarchar(max) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_SuspendedTransactionItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspendedTransactionItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SuspendedTransactionItems_SuspendedTransactions_SuspendedTransactionId] FOREIGN KEY ([SuspendedTransactionId]) REFERENCES [SuspendedTransactions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TableTransferLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [TableId] int NOT NULL,
    [TableNumber] nvarchar(20) NOT NULL,
    [FromUserId] int NOT NULL,
    [FromUserName] nvarchar(200) NOT NULL,
    [ToUserId] int NOT NULL,
    [ToUserName] nvarchar(200) NOT NULL,
    [ReceiptId] int NULL,
    [ReceiptAmount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [TransferredAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [TransferredByUserId] int NOT NULL,
    CONSTRAINT [PK_TableTransferLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TableTransferLogs_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TableTransferLogs_Tables_TableId] FOREIGN KEY ([TableId]) REFERENCES [Tables] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TableTransferLogs_Users_FromUserId] FOREIGN KEY ([FromUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_TableTransferLogs_Users_ToUserId] FOREIGN KEY ([ToUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_TableTransferLogs_Users_TransferredByUserId] FOREIGN KEY ([TransferredByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [EtimsCreditNoteItems] (
    [Id] int NOT NULL IDENTITY,
    [EtimsCreditNoteId] int NOT NULL,
    [SequenceNumber] int NOT NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemDescription] nvarchar(200) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TaxType] int NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [TaxableAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_EtimsCreditNoteItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EtimsCreditNoteItems_EtimsCreditNotes_EtimsCreditNoteId] FOREIGN KEY ([EtimsCreditNoteId]) REFERENCES [EtimsCreditNotes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReconciliationDiscrepancies] (
    [Id] int NOT NULL IDENTITY,
    [ReconciliationSessionId] int NOT NULL,
    [DiscrepancyNumber] nvarchar(max) NOT NULL,
    [DiscrepancyType] int NOT NULL,
    [BankTransactionId] int NULL,
    [PaymentId] int NULL,
    [ReceiptId] int NULL,
    [BankAmount] decimal(18,2) NULL,
    [POSAmount] decimal(18,2) NULL,
    [DifferenceAmount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ResolutionStatus] int NOT NULL,
    [ResolutionAction] nvarchar(max) NULL,
    [ResolvedByUserId] int NULL,
    [ResolvedAt] datetime2 NULL,
    [AdjustmentJournalEntryId] int NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReconciliationDiscrepancies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReconciliationDiscrepancies_BankTransactions_BankTransactionId] FOREIGN KEY ([BankTransactionId]) REFERENCES [BankTransactions] ([Id]),
    CONSTRAINT [FK_ReconciliationDiscrepancies_JournalEntries_AdjustmentJournalEntryId] FOREIGN KEY ([AdjustmentJournalEntryId]) REFERENCES [JournalEntries] ([Id]),
    CONSTRAINT [FK_ReconciliationDiscrepancies_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]),
    CONSTRAINT [FK_ReconciliationDiscrepancies_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_ReconciliationDiscrepancies_ReconciliationSessions_ReconciliationSessionId] FOREIGN KEY ([ReconciliationSessionId]) REFERENCES [ReconciliationSessions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReconciliationDiscrepancies_Users_ResolvedByUserId] FOREIGN KEY ([ResolvedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [ReconciliationMatches] (
    [Id] int NOT NULL IDENTITY,
    [ReconciliationSessionId] int NOT NULL,
    [BankTransactionId] int NOT NULL,
    [PaymentId] int NULL,
    [ReceiptId] int NULL,
    [BankAmount] decimal(18,2) NOT NULL,
    [POSAmount] decimal(18,2) NULL,
    [AmountDifference] decimal(18,2) NULL,
    [MatchType] int NOT NULL,
    [MatchConfidence] int NULL,
    [MatchingRule] nvarchar(max) NULL,
    [MatchedAt] datetime2 NOT NULL,
    [MatchedByUserId] int NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_ReconciliationMatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReconciliationMatches_BankTransactions_BankTransactionId] FOREIGN KEY ([BankTransactionId]) REFERENCES [BankTransactions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReconciliationMatches_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]),
    CONSTRAINT [FK_ReconciliationMatches_Receipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Receipts] ([Id]),
    CONSTRAINT [FK_ReconciliationMatches_ReconciliationSessions_ReconciliationSessionId] FOREIGN KEY ([ReconciliationSessionId]) REFERENCES [ReconciliationSessions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReconciliationMatches_Users_MatchedByUserId] FOREIGN KEY ([MatchedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [MpesaTransactions] (
    [Id] int NOT NULL IDENTITY,
    [PaymentId] int NULL,
    [StkPushRequestId] int NULL,
    [MpesaReceiptNumber] nvarchar(50) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [IsManualEntry] bit NOT NULL,
    [RecordedByUserId] int NULL,
    [Notes] nvarchar(500) NULL,
    [IsVerified] bit NOT NULL,
    [VerifiedByUserId] int NULL,
    [VerifiedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    CONSTRAINT [PK_MpesaTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MpesaTransactions_MpesaStkPushRequests_StkPushRequestId] FOREIGN KEY ([StkPushRequestId]) REFERENCES [MpesaStkPushRequests] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MpesaTransactions_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MpesaTransactions_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MpesaTransactions_Users_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsDecrease], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (1, N'DMG', '2026-01-18T12:15:07.3996746Z', NULL, 1, CAST(1 AS bit), CAST(1 AS bit), N'Damaged/Broken', NULL, NULL),
(2, N'EXP', '2026-01-18T12:15:07.3998160Z', NULL, 2, CAST(1 AS bit), CAST(1 AS bit), N'Expired', NULL, NULL),
(3, N'WST', '2026-01-18T12:15:07.3998163Z', NULL, 3, CAST(1 AS bit), CAST(1 AS bit), N'Wastage', NULL, NULL),
(4, N'THF', '2026-01-18T12:15:07.3998164Z', NULL, 4, CAST(1 AS bit), CAST(1 AS bit), N'Theft/Missing', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsIncrease], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (5, N'FND', '2026-01-18T12:15:07.3998166Z', NULL, 5, CAST(1 AS bit), CAST(1 AS bit), N'Found/Recovered', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsDecrease], [IsIncrease], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (6, N'COR', '2026-01-18T12:15:07.3998167Z', NULL, 6, CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'Correction', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsIncrease], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (7, N'TRI', '2026-01-18T12:15:07.3998168Z', NULL, 7, CAST(1 AS bit), CAST(1 AS bit), N'Transfer In', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsIncrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsDecrease], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (8, N'TRO', '2026-01-18T12:15:07.3998170Z', NULL, 8, CAST(1 AS bit), CAST(1 AS bit), N'Transfer Out', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'IsIncrease', N'Name', N'RequiresNote', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] ON;
INSERT INTO [AdjustmentReasons] ([Id], [Code], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [IsDecrease], [IsIncrease], [Name], [RequiresNote], [UpdatedAt], [UpdatedByUserId])
VALUES (9, N'OTH', '2026-01-18T12:15:07.3998171Z', NULL, 99, CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'Other', CAST(1 AS bit), NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'IsDecrease', N'IsIncrease', N'Name', N'RequiresNote', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[AdjustmentReasons]'))
    SET IDENTITY_INSERT [AdjustmentReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackgroundColor', N'Code', N'CreatedAt', N'CreatedByUserId', N'Description', N'DisplayOrder', N'IconPath', N'IsActive', N'Name', N'OpensDrawer', N'ReferenceLabel', N'ReferenceMaxLength', N'ReferenceMinLength', N'RequiresReference', N'SupportsChange', N'Type', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[PaymentMethods]'))
    SET IDENTITY_INSERT [PaymentMethods] ON;
INSERT INTO [PaymentMethods] ([Id], [BackgroundColor], [Code], [CreatedAt], [CreatedByUserId], [Description], [DisplayOrder], [IconPath], [IsActive], [Name], [OpensDrawer], [ReferenceLabel], [ReferenceMaxLength], [ReferenceMinLength], [RequiresReference], [SupportsChange], [Type], [UpdatedAt], [UpdatedByUserId])
VALUES (1, N'#4CAF50', N'CASH', '2026-01-18T12:15:07.4687081Z', NULL, N'Cash payment', 1, NULL, CAST(1 AS bit), N'Cash', CAST(1 AS bit), NULL, NULL, NULL, CAST(0 AS bit), CAST(1 AS bit), N'Cash', NULL, NULL),
(2, N'#00C853', N'MPESA', '2026-01-18T12:15:07.4689069Z', NULL, N'Safaricom M-Pesa mobile money', 2, NULL, CAST(1 AS bit), N'M-Pesa', CAST(0 AS bit), N'M-Pesa Code', 10, 10, CAST(1 AS bit), CAST(0 AS bit), N'MPesa', NULL, NULL),
(3, N'#FF5722', N'AIRTEL', '2026-01-18T12:15:07.4689654Z', NULL, N'Airtel Money mobile payment', 3, NULL, CAST(1 AS bit), N'Airtel Money', CAST(0 AS bit), N'Airtel Code', 10, 10, CAST(1 AS bit), CAST(0 AS bit), N'MPesa', NULL, NULL),
(4, N'#2196F3', N'CREDIT_CARD', '2026-01-18T12:15:07.4689658Z', NULL, N'Credit card payment', 4, NULL, CAST(1 AS bit), N'Credit Card', CAST(0 AS bit), N'Last 4 Digits (Optional)', 4, 4, CAST(0 AS bit), CAST(0 AS bit), N'Card', NULL, NULL),
(5, N'#9C27B0', N'DEBIT_CARD', '2026-01-18T12:15:07.4689661Z', NULL, N'Debit card payment', 5, NULL, CAST(1 AS bit), N'Debit Card', CAST(0 AS bit), N'Last 4 Digits (Optional)', 4, 4, CAST(0 AS bit), CAST(0 AS bit), N'Card', NULL, NULL),
(6, N'#607D8B', N'BANK_TRANSFER', '2026-01-18T12:15:07.4689663Z', NULL, N'Bank transfer or RTGS', 6, NULL, CAST(0 AS bit), N'Bank Transfer', CAST(0 AS bit), N'Reference Number', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N'BankTransfer', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackgroundColor', N'Code', N'CreatedAt', N'CreatedByUserId', N'Description', N'DisplayOrder', N'IconPath', N'IsActive', N'Name', N'OpensDrawer', N'ReferenceLabel', N'ReferenceMaxLength', N'ReferenceMinLength', N'RequiresReference', N'SupportsChange', N'Type', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[PaymentMethods]'))
    SET IDENTITY_INSERT [PaymentMethods] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[VoidReasons]'))
    SET IDENTITY_INSERT [VoidReasons] ON;
INSERT INTO [VoidReasons] ([Id], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [Name], [UpdatedAt], [UpdatedByUserId])
VALUES (1, '2026-01-18T12:15:07.6157298Z', NULL, 1, CAST(1 AS bit), N'Customer complaint', NULL, NULL),
(2, '2026-01-18T12:15:07.6158054Z', NULL, 2, CAST(1 AS bit), N'Wrong order', NULL, NULL),
(3, '2026-01-18T12:15:07.6158056Z', NULL, 3, CAST(1 AS bit), N'Item unavailable', NULL, NULL),
(4, '2026-01-18T12:15:07.6158057Z', NULL, 4, CAST(1 AS bit), N'Duplicate transaction', NULL, NULL),
(5, '2026-01-18T12:15:07.6158058Z', NULL, 5, CAST(1 AS bit), N'Test transaction', NULL, NULL),
(6, '2026-01-18T12:15:07.6158059Z', NULL, 6, CAST(1 AS bit), N'System error', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'Name', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[VoidReasons]'))
    SET IDENTITY_INSERT [VoidReasons] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'Name', N'RequiresNote', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[VoidReasons]'))
    SET IDENTITY_INSERT [VoidReasons] ON;
INSERT INTO [VoidReasons] ([Id], [CreatedAt], [CreatedByUserId], [DisplayOrder], [IsActive], [Name], [RequiresNote], [UpdatedAt], [UpdatedByUserId])
VALUES (7, '2026-01-18T12:15:07.6158060Z', NULL, 99, CAST(1 AS bit), N'Other', CAST(1 AS bit), NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedByUserId', N'DisplayOrder', N'IsActive', N'Name', N'RequiresNote', N'UpdatedAt', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[VoidReasons]'))
    SET IDENTITY_INSERT [VoidReasons] OFF;

CREATE INDEX [IX_AccountingPeriods_ClosedByUserId] ON [AccountingPeriods] ([ClosedByUserId]);

CREATE UNIQUE INDEX [IX_AdjustmentReasons_Code] ON [AdjustmentReasons] ([Code]);

CREATE INDEX [IX_AdjustmentReasons_DisplayOrder] ON [AdjustmentReasons] ([DisplayOrder]);

CREATE UNIQUE INDEX [IX_AdjustmentReasons_Name] ON [AdjustmentReasons] ([Name]);

CREATE INDEX [IX_AirtelMoneyConfigurations_StoreId] ON [AirtelMoneyConfigurations] ([StoreId]);

CREATE INDEX [IX_AirtelMoneyRequests_ReceiptId] ON [AirtelMoneyRequests] ([ReceiptId]);

CREATE INDEX [IX_AirtelMoneyRequests_StoreId] ON [AirtelMoneyRequests] ([StoreId]);

CREATE INDEX [IX_AirtelMoneyRequests_UserId] ON [AirtelMoneyRequests] ([UserId]);

CREATE INDEX [IX_ApiAccessTokens_ApiClientId] ON [ApiAccessTokens] ([ApiClientId]);

CREATE INDEX [IX_ApiClients_StoreId] ON [ApiClients] ([StoreId]);

CREATE INDEX [IX_ApiClients_UserId] ON [ApiClients] ([UserId]);

CREATE INDEX [IX_ApiClientScopes_ApiClientId] ON [ApiClientScopes] ([ApiClientId]);

CREATE INDEX [IX_ApiClientScopes_ApiScopeId] ON [ApiClientScopes] ([ApiScopeId]);

CREATE INDEX [IX_ApiRateLimitEntries_ApiClientId] ON [ApiRateLimitEntries] ([ApiClientId]);

CREATE INDEX [IX_ApiRequestLogs_ApiClientId] ON [ApiRequestLogs] ([ApiClientId]);

CREATE INDEX [IX_ApiRequestLogs_UserId] ON [ApiRequestLogs] ([UserId]);

CREATE INDEX [IX_Attendances_ApprovedByUserId] ON [Attendances] ([ApprovedByUserId]);

CREATE UNIQUE INDEX [IX_Attendances_EmployeeId_AttendanceDate] ON [Attendances] ([EmployeeId], [AttendanceDate]);

CREATE INDEX [IX_AuditLog_CreatedAt] ON [AuditLog] ([CreatedAt]);

CREATE INDEX [IX_AuditLog_EntityType_EntityId] ON [AuditLog] ([EntityType], [EntityId]);

CREATE INDEX [IX_AuditLog_UserId_CreatedAt] ON [AuditLog] ([UserId], [CreatedAt]);

CREATE INDEX [IX_AutomaticMarkdowns_CategoryId] ON [AutomaticMarkdowns] ([CategoryId]);

CREATE INDEX [IX_AutomaticMarkdowns_ProductId] ON [AutomaticMarkdowns] ([ProductId]);

CREATE INDEX [IX_BankAccounts_ChartOfAccountId] ON [BankAccounts] ([ChartOfAccountId]);

CREATE INDEX [IX_BankStatementImports_BankAccountId] ON [BankStatementImports] ([BankAccountId]);

CREATE INDEX [IX_BankStatementImports_ImportedByUserId] ON [BankStatementImports] ([ImportedByUserId]);

CREATE INDEX [IX_BankTransactions_BankAccountId] ON [BankTransactions] ([BankAccountId]);

CREATE INDEX [IX_BankTransactions_MatchedPaymentId] ON [BankTransactions] ([MatchedPaymentId]);

CREATE INDEX [IX_BankTransactions_ReconciliationSessionId] ON [BankTransactions] ([ReconciliationSessionId]);

CREATE INDEX [IX_BatchDisposals_BatchId] ON [BatchDisposals] ([BatchId]);

CREATE INDEX [IX_BatchDisposals_StoreId] ON [BatchDisposals] ([StoreId]);

CREATE INDEX [IX_BatchRecallAlerts_BatchId] ON [BatchRecallAlerts] ([BatchId]);

CREATE INDEX [IX_BatchRecallAlerts_ProductId] ON [BatchRecallAlerts] ([ProductId]);

CREATE INDEX [IX_BatchStockMovements_BatchId] ON [BatchStockMovements] ([BatchId]);

CREATE INDEX [IX_BatchStockMovements_ProductId] ON [BatchStockMovements] ([ProductId]);

CREATE INDEX [IX_BatchStockMovements_StoreId] ON [BatchStockMovements] ([StoreId]);

CREATE INDEX [IX_BogoPromotions_GetCategoryId] ON [BogoPromotions] ([GetCategoryId]);

CREATE INDEX [IX_BogoPromotions_GetProductId] ON [BogoPromotions] ([GetProductId]);

CREATE INDEX [IX_BogoPromotions_PromotionId] ON [BogoPromotions] ([PromotionId]);

CREATE INDEX [IX_BudgetLines_AccountId] ON [BudgetLines] ([AccountId]);

CREATE INDEX [IX_BudgetLines_BudgetId] ON [BudgetLines] ([BudgetId]);

CREATE INDEX [IX_BudgetLines_DepartmentId] ON [BudgetLines] ([DepartmentId]);

CREATE INDEX [IX_Budgets_ApprovedByUserId] ON [Budgets] ([ApprovedByUserId]);

CREATE INDEX [IX_Budgets_CreatedByUserId] ON [Budgets] ([CreatedByUserId]);

CREATE INDEX [IX_Budgets_StoreId] ON [Budgets] ([StoreId]);

CREATE INDEX [IX_CashDrawerLogs_AuthorizedByUserId] ON [CashDrawerLogs] ([AuthorizedByUserId]);

CREATE INDEX [IX_CashDrawerLogs_CashDrawerId] ON [CashDrawerLogs] ([CashDrawerId]);

CREATE INDEX [IX_CashDrawerLogs_CashDrawerId_OpenedAt] ON [CashDrawerLogs] ([CashDrawerId], [OpenedAt]);

CREATE INDEX [IX_CashDrawerLogs_OpenedAt] ON [CashDrawerLogs] ([OpenedAt]);

CREATE INDEX [IX_CashDrawerLogs_UserId] ON [CashDrawerLogs] ([UserId]);

CREATE INDEX [IX_CashDrawers_IsActive] ON [CashDrawers] ([IsActive]);

CREATE INDEX [IX_CashDrawers_LastOpenedByUserId] ON [CashDrawers] ([LastOpenedByUserId]);

CREATE INDEX [IX_CashDrawers_LinkedPrinterId] ON [CashDrawers] ([LinkedPrinterId]);

CREATE INDEX [IX_CashFlowMappings_AccountId] ON [CashFlowMappings] ([AccountId]);

CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);

CREATE INDEX [IX_CategoryExpirySettings_CategoryId] ON [CategoryExpirySettings] ([CategoryId]);

CREATE INDEX [IX_CentralPromotions_Active] ON [CentralPromotions] ([Status], [StartDate], [EndDate], [IsActive]);

CREATE INDEX [IX_CentralPromotions_CouponCode] ON [CentralPromotions] ([CouponCode]);

CREATE INDEX [IX_CentralPromotions_CreatedByUserId] ON [CentralPromotions] ([CreatedByUserId]);

CREATE INDEX [IX_CentralPromotions_DateRange] ON [CentralPromotions] ([StartDate], [EndDate]);

CREATE UNIQUE INDEX [IX_CentralPromotions_PromotionCode] ON [CentralPromotions] ([PromotionCode]);

CREATE INDEX [IX_CentralPromotions_Status] ON [CentralPromotions] ([Status]);

CREATE UNIQUE INDEX [IX_ChartOfAccounts_AccountCode] ON [ChartOfAccounts] ([AccountCode]);

CREATE INDEX [IX_ChartOfAccounts_ParentAccountId] ON [ChartOfAccounts] ([ParentAccountId]);

CREATE INDEX [IX_ComboItems_ComboPromotionId] ON [ComboItems] ([ComboPromotionId]);

CREATE INDEX [IX_ComboItems_ProductId] ON [ComboItems] ([ProductId]);

CREATE INDEX [IX_ComboItems_SubstitutionCategoryId] ON [ComboItems] ([SubstitutionCategoryId]);

CREATE INDEX [IX_ComboPromotions_PromotionId] ON [ComboPromotions] ([PromotionId]);

CREATE INDEX [IX_CouponBatches_GeneratedByUserId] ON [CouponBatches] ([GeneratedByUserId]);

CREATE INDEX [IX_CouponBatches_PromotionId] ON [CouponBatches] ([PromotionId]);

CREATE INDEX [IX_CouponRedemptions_CouponId] ON [CouponRedemptions] ([CouponId]);

CREATE INDEX [IX_CouponRedemptions_ReceiptId] ON [CouponRedemptions] ([ReceiptId]);

CREATE INDEX [IX_CouponRedemptions_RedeemedByUserId] ON [CouponRedemptions] ([RedeemedByUserId]);

CREATE INDEX [IX_Coupons_BatchId] ON [Coupons] ([BatchId]);

CREATE INDEX [IX_Coupons_CustomerId] ON [Coupons] ([CustomerId]);

CREATE INDEX [IX_Coupons_PromotionId] ON [Coupons] ([PromotionId]);

CREATE INDEX [IX_CreditTransactions_CreditAccountId] ON [CreditTransactions] ([CreditAccountId]);

CREATE INDEX [IX_CreditTransactions_PaymentId] ON [CreditTransactions] ([PaymentId]);

CREATE INDEX [IX_CreditTransactions_ProcessedByUserId] ON [CreditTransactions] ([ProcessedByUserId]);

CREATE INDEX [IX_CreditTransactions_ReceiptId] ON [CreditTransactions] ([ReceiptId]);

CREATE INDEX [IX_CustomerCreditAccounts_CustomerId] ON [CustomerCreditAccounts] ([CustomerId]);

CREATE INDEX [IX_CustomerDisplayConfigs_StoreId] ON [CustomerDisplayConfigs] ([StoreId]);

CREATE INDEX [IX_CustomerDisplayMessages_DisplayConfigId] ON [CustomerDisplayMessages] ([DisplayConfigId]);

CREATE INDEX [IX_CustomerPayments_CreditAccountId] ON [CustomerPayments] ([CreditAccountId]);

CREATE INDEX [IX_CustomerPayments_PaymentMethodId] ON [CustomerPayments] ([PaymentMethodId]);

CREATE INDEX [IX_CustomerPayments_ReceivedByUserId] ON [CustomerPayments] ([ReceivedByUserId]);

CREATE INDEX [IX_CustomerStatements_CreditAccountId] ON [CustomerStatements] ([CreditAccountId]);

CREATE INDEX [IX_DeadStockConfigs_StoreId] ON [DeadStockConfigs] ([StoreId]);

CREATE INDEX [IX_DeadStockItems_ProductId] ON [DeadStockItems] ([ProductId]);

CREATE INDEX [IX_DeadStockItems_StoreId] ON [DeadStockItems] ([StoreId]);

CREATE INDEX [IX_Departments_GLAccountId] ON [Departments] ([GLAccountId]);

CREATE INDEX [IX_Departments_ManagerId] ON [Departments] ([ManagerId]);

CREATE INDEX [IX_Departments_ParentDepartmentId] ON [Departments] ([ParentDepartmentId]);

CREATE INDEX [IX_Departments_StoreId] ON [Departments] ([StoreId]);

CREATE UNIQUE INDEX [IX_DeploymentStores_Deployment_Store] ON [DeploymentStores] ([DeploymentId], [StoreId]);

CREATE INDEX [IX_DeploymentStores_Status] ON [DeploymentStores] ([Status]);

CREATE INDEX [IX_DeploymentStores_StoreId] ON [DeploymentStores] ([StoreId]);

CREATE UNIQUE INDEX [IX_DeploymentZones_Deployment_Zone] ON [DeploymentZones] ([DeploymentId], [PricingZoneId]);

CREATE INDEX [IX_DeploymentZones_PricingZoneId] ON [DeploymentZones] ([PricingZoneId]);

CREATE INDEX [IX_EmailConfigurations_StoreId] ON [EmailConfigurations] ([StoreId]);

CREATE INDEX [IX_EmailLogs_EmailScheduleId] ON [EmailLogs] ([EmailScheduleId]);

CREATE INDEX [IX_EmailLogs_StoreId] ON [EmailLogs] ([StoreId]);

CREATE INDEX [IX_EmailRecipients_StoreId] ON [EmailRecipients] ([StoreId]);

CREATE INDEX [IX_EmailRecipients_UserId] ON [EmailRecipients] ([UserId]);

CREATE INDEX [IX_EmailSchedules_StoreId] ON [EmailSchedules] ([StoreId]);

CREATE INDEX [IX_EmailTemplates_StoreId] ON [EmailTemplates] ([StoreId]);

CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [Employees] ([EmployeeNumber]);

CREATE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);

CREATE UNIQUE INDEX [IX_EmployeeSalaryComponents_EmployeeId_SalaryComponentId_EffectiveFrom] ON [EmployeeSalaryComponents] ([EmployeeId], [SalaryComponentId], [EffectiveFrom]);

CREATE INDEX [IX_EmployeeSalaryComponents_SalaryComponentId] ON [EmployeeSalaryComponents] ([SalaryComponentId]);

CREATE INDEX [IX_EtimsCreditNoteItems_EtimsCreditNoteId] ON [EtimsCreditNoteItems] ([EtimsCreditNoteId]);

CREATE UNIQUE INDEX [IX_EtimsCreditNotes_CreditNoteNumber] ON [EtimsCreditNotes] ([CreditNoteNumber]);

CREATE INDEX [IX_EtimsCreditNotes_DeviceId] ON [EtimsCreditNotes] ([DeviceId]);

CREATE INDEX [IX_EtimsCreditNotes_OriginalInvoiceId] ON [EtimsCreditNotes] ([OriginalInvoiceId]);

CREATE INDEX [IX_EtimsCreditNotes_Status] ON [EtimsCreditNotes] ([Status]);

CREATE INDEX [IX_EtimsInvoiceItems_EtimsInvoiceId] ON [EtimsInvoiceItems] ([EtimsInvoiceId]);

CREATE INDEX [IX_EtimsInvoices_DeviceId] ON [EtimsInvoices] ([DeviceId]);

CREATE INDEX [IX_EtimsInvoices_InvoiceDate] ON [EtimsInvoices] ([InvoiceDate]);

CREATE UNIQUE INDEX [IX_EtimsInvoices_InvoiceNumber] ON [EtimsInvoices] ([InvoiceNumber]);

CREATE INDEX [IX_EtimsInvoices_ReceiptId] ON [EtimsInvoices] ([ReceiptId]);

CREATE INDEX [IX_EtimsInvoices_Status] ON [EtimsInvoices] ([Status]);

CREATE INDEX [IX_EtimsQueue_Priority] ON [EtimsQueue] ([Priority]);

CREATE INDEX [IX_EtimsQueue_QueuedAt] ON [EtimsQueue] ([QueuedAt]);

CREATE INDEX [IX_EtimsQueue_RetryAfter] ON [EtimsQueue] ([RetryAfter]);

CREATE INDEX [IX_EtimsQueue_Status] ON [EtimsQueue] ([Status]);

CREATE INDEX [IX_EtimsSyncLogs_IsSuccess] ON [EtimsSyncLogs] ([IsSuccess]);

CREATE INDEX [IX_EtimsSyncLogs_StartedAt] ON [EtimsSyncLogs] ([StartedAt]);

CREATE INDEX [IX_ExpenseCategories_ParentCategoryId] ON [ExpenseCategories] ([ParentCategoryId]);

CREATE INDEX [IX_Expenses_ApprovedByUserId] ON [Expenses] ([ApprovedByUserId]);

CREATE INDEX [IX_Expenses_CreatedByUserId] ON [Expenses] ([CreatedByUserId]);

CREATE INDEX [IX_Expenses_ExpenseCategoryId] ON [Expenses] ([ExpenseCategoryId]);

CREATE UNIQUE INDEX [IX_Expenses_ExpenseNumber] ON [Expenses] ([ExpenseNumber]);

CREATE INDEX [IX_Expenses_SupplierId] ON [Expenses] ([SupplierId]);

CREATE INDEX [IX_ExpiryAlertConfigs_StoreId] ON [ExpiryAlertConfigs] ([StoreId]);

CREATE INDEX [IX_ExpirySaleBlocks_BatchId] ON [ExpirySaleBlocks] ([BatchId]);

CREATE INDEX [IX_ExpirySaleBlocks_ProductId] ON [ExpirySaleBlocks] ([ProductId]);

CREATE INDEX [IX_ExpirySaleBlocks_StoreId] ON [ExpirySaleBlocks] ([StoreId]);

CREATE INDEX [IX_Floors_DisplayOrder] ON [Floors] ([DisplayOrder]);

CREATE UNIQUE INDEX [IX_Floors_Name] ON [Floors] ([Name]) WHERE [IsActive] = 1;

CREATE UNIQUE INDEX [IX_GoodsReceivedNotes_GRNNumber] ON [GoodsReceivedNotes] ([GRNNumber]);

CREATE INDEX [IX_GoodsReceivedNotes_PurchaseOrderId] ON [GoodsReceivedNotes] ([PurchaseOrderId]);

CREATE INDEX [IX_GoodsReceivedNotes_ReceivedByUserId] ON [GoodsReceivedNotes] ([ReceivedByUserId]);

CREATE INDEX [IX_GoodsReceivedNotes_ReceivedDate] ON [GoodsReceivedNotes] ([ReceivedDate]);

CREATE INDEX [IX_GoodsReceivedNotes_SupplierId] ON [GoodsReceivedNotes] ([SupplierId]);

CREATE INDEX [IX_GRNItems_GoodsReceivedNoteId] ON [GRNItems] ([GoodsReceivedNoteId]);

CREATE INDEX [IX_GRNItems_ProductId] ON [GRNItems] ([ProductId]);

CREATE INDEX [IX_GRNItems_PurchaseOrderItemId] ON [GRNItems] ([PurchaseOrderItemId]);

CREATE UNIQUE INDEX [IX_Inventory_ProductId] ON [Inventory] ([ProductId]);

CREATE INDEX [IX_Inventory_StoreId] ON [Inventory] ([StoreId]);

CREATE INDEX [IX_InventoryTurnoverAnalyses_CategoryId] ON [InventoryTurnoverAnalyses] ([CategoryId]);

CREATE INDEX [IX_InventoryTurnoverAnalyses_ProductId] ON [InventoryTurnoverAnalyses] ([ProductId]);

CREATE INDEX [IX_InventoryTurnoverAnalyses_StoreId] ON [InventoryTurnoverAnalyses] ([StoreId]);

CREATE INDEX [IX_JournalEntries_AccountingPeriodId] ON [JournalEntries] ([AccountingPeriodId]);

CREATE INDEX [IX_JournalEntries_CreatedByUserId] ON [JournalEntries] ([CreatedByUserId]);

CREATE UNIQUE INDEX [IX_JournalEntries_EntryNumber] ON [JournalEntries] ([EntryNumber]);

CREATE INDEX [IX_JournalEntryLines_AccountId] ON [JournalEntryLines] ([AccountId]);

CREATE INDEX [IX_JournalEntryLines_JournalEntryId] ON [JournalEntryLines] ([JournalEntryId]);

CREATE UNIQUE INDEX [IX_KOTSettings_PrinterId] ON [KOTSettings] ([PrinterId]);

CREATE INDEX [IX_LowStockAlertConfigs_StoreId] ON [LowStockAlertConfigs] ([StoreId]);

CREATE INDEX [IX_LoyaltyMembers_IsActive] ON [LoyaltyMembers] ([IsActive]);

CREATE INDEX [IX_LoyaltyMembers_LastVisit] ON [LoyaltyMembers] ([LastVisit]);

CREATE UNIQUE INDEX [IX_LoyaltyMembers_MembershipNumber] ON [LoyaltyMembers] ([MembershipNumber]);

CREATE UNIQUE INDEX [IX_LoyaltyMembers_PhoneNumber] ON [LoyaltyMembers] ([PhoneNumber]);

CREATE INDEX [IX_LoyaltyMembers_Tier] ON [LoyaltyMembers] ([Tier]);

CREATE INDEX [IX_LoyaltyTransactions_LoyaltyMemberId] ON [LoyaltyTransactions] ([LoyaltyMemberId]);

CREATE INDEX [IX_LoyaltyTransactions_Member_Date] ON [LoyaltyTransactions] ([LoyaltyMemberId], [TransactionDate]);

CREATE INDEX [IX_LoyaltyTransactions_ProcessedByUserId] ON [LoyaltyTransactions] ([ProcessedByUserId]);

CREATE INDEX [IX_LoyaltyTransactions_ReceiptId] ON [LoyaltyTransactions] ([ReceiptId]);

CREATE INDEX [IX_LoyaltyTransactions_TransactionDate] ON [LoyaltyTransactions] ([TransactionDate]);

CREATE INDEX [IX_LoyaltyTransactions_TransactionType] ON [LoyaltyTransactions] ([TransactionType]);

CREATE INDEX [IX_MarginThresholds_CategoryId] ON [MarginThresholds] ([CategoryId]);

CREATE INDEX [IX_MarginThresholds_ProductId] ON [MarginThresholds] ([ProductId]);

CREATE INDEX [IX_MarginThresholds_StoreId] ON [MarginThresholds] ([StoreId]);

CREATE INDEX [IX_MixMatchGroupCategories_CategoryId] ON [MixMatchGroupCategories] ([CategoryId]);

CREATE INDEX [IX_MixMatchGroupCategories_MixMatchGroupId] ON [MixMatchGroupCategories] ([MixMatchGroupId]);

CREATE INDEX [IX_MixMatchGroupProducts_MixMatchGroupId] ON [MixMatchGroupProducts] ([MixMatchGroupId]);

CREATE INDEX [IX_MixMatchGroupProducts_ProductId] ON [MixMatchGroupProducts] ([ProductId]);

CREATE INDEX [IX_MixMatchGroups_MixMatchPromotionId] ON [MixMatchGroups] ([MixMatchPromotionId]);

CREATE INDEX [IX_MixMatchPromotions_PromotionId] ON [MixMatchPromotions] ([PromotionId]);

CREATE INDEX [IX_MobileMoneyTransactionLogs_StoreId] ON [MobileMoneyTransactionLogs] ([StoreId]);

CREATE INDEX [IX_MpesaConfigurations_BusinessShortCode] ON [MpesaConfigurations] ([BusinessShortCode]);

CREATE INDEX [IX_MpesaConfigurations_IsActive] ON [MpesaConfigurations] ([IsActive]);

CREATE INDEX [IX_MpesaStkPushRequests_CheckoutRequestId] ON [MpesaStkPushRequests] ([CheckoutRequestId]);

CREATE INDEX [IX_MpesaStkPushRequests_ConfigurationId] ON [MpesaStkPushRequests] ([ConfigurationId]);

CREATE INDEX [IX_MpesaStkPushRequests_MerchantRequestId] ON [MpesaStkPushRequests] ([MerchantRequestId]);

CREATE INDEX [IX_MpesaStkPushRequests_MpesaReceiptNumber] ON [MpesaStkPushRequests] ([MpesaReceiptNumber]);

CREATE INDEX [IX_MpesaStkPushRequests_PaymentId] ON [MpesaStkPushRequests] ([PaymentId]);

CREATE INDEX [IX_MpesaStkPushRequests_ReceiptId] ON [MpesaStkPushRequests] ([ReceiptId]);

CREATE INDEX [IX_MpesaStkPushRequests_RequestedAt] ON [MpesaStkPushRequests] ([RequestedAt]);

CREATE INDEX [IX_MpesaStkPushRequests_Status] ON [MpesaStkPushRequests] ([Status]);

CREATE INDEX [IX_MpesaTransactions_IsManualEntry] ON [MpesaTransactions] ([IsManualEntry]);

CREATE INDEX [IX_MpesaTransactions_IsVerified] ON [MpesaTransactions] ([IsVerified]);

CREATE UNIQUE INDEX [IX_MpesaTransactions_MpesaReceiptNumber] ON [MpesaTransactions] ([MpesaReceiptNumber]);

CREATE INDEX [IX_MpesaTransactions_PaymentId] ON [MpesaTransactions] ([PaymentId]);

CREATE INDEX [IX_MpesaTransactions_RecordedByUserId] ON [MpesaTransactions] ([RecordedByUserId]);

CREATE INDEX [IX_MpesaTransactions_Status] ON [MpesaTransactions] ([Status]);

CREATE INDEX [IX_MpesaTransactions_StkPushRequestId] ON [MpesaTransactions] ([StkPushRequestId]);

CREATE INDEX [IX_MpesaTransactions_TransactionDate] ON [MpesaTransactions] ([TransactionDate]);

CREATE INDEX [IX_MpesaTransactions_VerifiedByUserId] ON [MpesaTransactions] ([VerifiedByUserId]);

CREATE INDEX [IX_OrderItems_AppliedOfferId] ON [OrderItems] ([AppliedOfferId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_OrderId_ProductId] ON [OrderItems] ([OrderId], [ProductId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);

CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);

CREATE INDEX [IX_Orders_WorkPeriodId] ON [Orders] ([WorkPeriodId]);

CREATE INDEX [IX_OverheadAllocationDetails_AllocationRuleId] ON [OverheadAllocationDetails] ([AllocationRuleId]);

CREATE INDEX [IX_OverheadAllocationDetails_DepartmentId] ON [OverheadAllocationDetails] ([DepartmentId]);

CREATE INDEX [IX_OverheadAllocationRules_SourceAccountId] ON [OverheadAllocationRules] ([SourceAccountId]);

CREATE INDEX [IX_OverheadAllocationRules_StoreId] ON [OverheadAllocationRules] ([StoreId]);

CREATE INDEX [IX_PaymentAllocations_PaymentId] ON [PaymentAllocations] ([PaymentId]);

CREATE INDEX [IX_PaymentAllocations_TransactionId] ON [PaymentAllocations] ([TransactionId]);

CREATE UNIQUE INDEX [IX_PaymentMethods_Code] ON [PaymentMethods] ([Code]);

CREATE INDEX [IX_PaymentMethods_DisplayOrder] ON [PaymentMethods] ([DisplayOrder]);

CREATE INDEX [IX_PaymentMethods_IsActive] ON [PaymentMethods] ([IsActive]);

CREATE INDEX [IX_Payments_PaymentMethodId] ON [Payments] ([PaymentMethodId]);

CREATE INDEX [IX_Payments_ProcessedByUserId] ON [Payments] ([ProcessedByUserId]);

CREATE INDEX [IX_Payments_ReceiptId] ON [Payments] ([ReceiptId]);

CREATE INDEX [IX_Payments_Reference] ON [Payments] ([Reference]);

CREATE INDEX [IX_Payments_UserId] ON [Payments] ([UserId]);

CREATE INDEX [IX_PayrollPeriods_ApprovedByUserId] ON [PayrollPeriods] ([ApprovedByUserId]);

CREATE INDEX [IX_PayrollPeriods_ProcessedByUserId] ON [PayrollPeriods] ([ProcessedByUserId]);

CREATE INDEX [IX_PayslipDetails_PayslipId] ON [PayslipDetails] ([PayslipId]);

CREATE INDEX [IX_PayslipDetails_SalaryComponentId] ON [PayslipDetails] ([SalaryComponentId]);

CREATE INDEX [IX_Payslips_EmployeeId] ON [Payslips] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_Payslips_PayrollPeriodId_EmployeeId] ON [Payslips] ([PayrollPeriodId], [EmployeeId]);

CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);

CREATE UNIQUE INDEX [IX_PLUCodes_Code] ON [PLUCodes] ([Code]);

CREATE INDEX [IX_PLUCodes_IsActive] ON [PLUCodes] ([IsActive]);

CREATE INDEX [IX_PLUCodes_ProductId] ON [PLUCodes] ([ProductId]);

CREATE INDEX [IX_PMSActivityLogs_PMSConfigurationId] ON [PMSActivityLogs] ([PMSConfigurationId]);

CREATE INDEX [IX_PMSActivityLogs_RoomChargePostingId] ON [PMSActivityLogs] ([RoomChargePostingId]);

CREATE INDEX [IX_PMSActivityLogs_UserId] ON [PMSActivityLogs] ([UserId]);

CREATE INDEX [IX_PMSConfigurations_StoreId] ON [PMSConfigurations] ([StoreId]);

CREATE INDEX [IX_PMSGuestLookups_PMSConfigurationId] ON [PMSGuestLookups] ([PMSConfigurationId]);

CREATE INDEX [IX_PMSPostingQueues_RoomChargePostingId] ON [PMSPostingQueues] ([RoomChargePostingId]);

CREATE INDEX [IX_PMSRevenueCenters_PMSConfigurationId] ON [PMSRevenueCenters] ([PMSConfigurationId]);

CREATE INDEX [IX_PMSRevenueCenters_StoreId] ON [PMSRevenueCenters] ([StoreId]);

CREATE INDEX [IX_PointsConfigurations_IsDefault] ON [PointsConfigurations] ([IsDefault]);

CREATE UNIQUE INDEX [IX_PointsConfigurations_Name] ON [PointsConfigurations] ([Name]);

CREATE INDEX [IX_PricingZones_IsDefault] ON [PricingZones] ([IsDefault]);

CREATE UNIQUE INDEX [IX_PricingZones_ZoneCode] ON [PricingZones] ([ZoneCode]);

CREATE INDEX [IX_PrinterCategoryMappings_CategoryId] ON [PrinterCategoryMappings] ([CategoryId]);

CREATE INDEX [IX_PrinterCategoryMappings_PrinterId] ON [PrinterCategoryMappings] ([PrinterId]);

CREATE UNIQUE INDEX [IX_PrinterCategoryMappings_PrinterId_CategoryId] ON [PrinterCategoryMappings] ([PrinterId], [CategoryId]);

CREATE INDEX [IX_Printers_IsActive] ON [Printers] ([IsActive]);

CREATE INDEX [IX_Printers_Type_IsDefault] ON [Printers] ([Type], [IsDefault]);

CREATE UNIQUE INDEX [IX_PrinterSettings_PrinterId] ON [PrinterSettings] ([PrinterId]);

CREATE UNIQUE INDEX [IX_ProductBarcodes_Barcode] ON [ProductBarcodes] ([Barcode]);

CREATE INDEX [IX_ProductBarcodes_ProductId] ON [ProductBarcodes] ([ProductId]);

CREATE INDEX [IX_ProductBarcodes_ProductId_IsPrimary] ON [ProductBarcodes] ([ProductId], [IsPrimary]);

CREATE INDEX [IX_ProductBatchConfigurations_ProductId] ON [ProductBatchConfigurations] ([ProductId]);

CREATE INDEX [IX_ProductBatches_GrnId] ON [ProductBatches] ([GrnId]);

CREATE INDEX [IX_ProductBatches_ProductId] ON [ProductBatches] ([ProductId]);

CREATE INDEX [IX_ProductBatches_StoreId] ON [ProductBatches] ([StoreId]);

CREATE INDEX [IX_ProductBatches_SupplierId] ON [ProductBatches] ([SupplierId]);

CREATE INDEX [IX_ProductOffers_ActiveLookup] ON [ProductOffers] ([ProductId], [StartDate], [EndDate], [IsActive]);

CREATE INDEX [IX_ProductOffers_CreatedByUserId] ON [ProductOffers] ([CreatedByUserId]);

CREATE INDEX [IX_ProductOffers_DateRange] ON [ProductOffers] ([StartDate], [EndDate]);

CREATE INDEX [IX_ProductOffers_ProductId] ON [ProductOffers] ([ProductId]);

CREATE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE UNIQUE INDEX [IX_Products_Code] ON [Products] ([Code]);

CREATE INDEX [IX_PromotionApplications_PromotionId] ON [PromotionApplications] ([PromotionId]);

CREATE INDEX [IX_PromotionApplications_ReceiptId] ON [PromotionApplications] ([ReceiptId]);

CREATE INDEX [IX_PromotionCategories_CategoryId] ON [PromotionCategories] ([CategoryId]);

CREATE UNIQUE INDEX [IX_PromotionCategories_Promotion_Category] ON [PromotionCategories] ([PromotionId], [CategoryId]);

CREATE INDEX [IX_PromotionDeployments_DeployedAt] ON [PromotionDeployments] ([DeployedAt]);

CREATE INDEX [IX_PromotionDeployments_DeployedByUserId] ON [PromotionDeployments] ([DeployedByUserId]);

CREATE INDEX [IX_PromotionDeployments_PromotionId] ON [PromotionDeployments] ([PromotionId]);

CREATE INDEX [IX_PromotionDeployments_Status] ON [PromotionDeployments] ([Status]);

CREATE INDEX [IX_PromotionProducts_ProductId] ON [PromotionProducts] ([ProductId]);

CREATE UNIQUE INDEX [IX_PromotionProducts_Promotion_Product] ON [PromotionProducts] ([PromotionId], [ProductId]);

CREATE INDEX [IX_PromotionRedemptions_LoyaltyMemberId] ON [PromotionRedemptions] ([LoyaltyMemberId]);

CREATE INDEX [IX_PromotionRedemptions_ProcessedByUserId] ON [PromotionRedemptions] ([ProcessedByUserId]);

CREATE INDEX [IX_PromotionRedemptions_Promotion_NotVoided] ON [PromotionRedemptions] ([PromotionId], [IsVoided]);

CREATE INDEX [IX_PromotionRedemptions_Promotion_Store_Date] ON [PromotionRedemptions] ([PromotionId], [StoreId], [RedeemedAt]);

CREATE INDEX [IX_PromotionRedemptions_PromotionId] ON [PromotionRedemptions] ([PromotionId]);

CREATE INDEX [IX_PromotionRedemptions_ReceiptId] ON [PromotionRedemptions] ([ReceiptId]);

CREATE INDEX [IX_PromotionRedemptions_ReceiptItemId] ON [PromotionRedemptions] ([ReceiptItemId]);

CREATE INDEX [IX_PromotionRedemptions_RedeemedAt] ON [PromotionRedemptions] ([RedeemedAt]);

CREATE INDEX [IX_PromotionRedemptions_StoreId] ON [PromotionRedemptions] ([StoreId]);

CREATE INDEX [IX_PromotionRedemptions_VoidedByUserId] ON [PromotionRedemptions] ([VoidedByUserId]);

CREATE INDEX [IX_PurchaseOrderItems_ProductId] ON [PurchaseOrderItems] ([ProductId]);

CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);

CREATE INDEX [IX_PurchaseOrders_CreatedByUserId] ON [PurchaseOrders] ([CreatedByUserId]);

CREATE UNIQUE INDEX [IX_PurchaseOrders_PONumber] ON [PurchaseOrders] ([PONumber]);

CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);

CREATE INDEX [IX_QuantityBreakTiers_PromotionId] ON [QuantityBreakTiers] ([PromotionId]);

CREATE INDEX [IX_QuickAmountButtons_StoreId] ON [QuickAmountButtons] ([StoreId]);

CREATE INDEX [IX_QuickAmountButtonSets_StoreId] ON [QuickAmountButtonSets] ([StoreId]);

CREATE INDEX [IX_RecallActions_RecallAlertId] ON [RecallActions] ([RecallAlertId]);

CREATE INDEX [IX_RecallActions_StoreId] ON [RecallActions] ([StoreId]);

CREATE INDEX [IX_ReceiptItems_OrderItemId] ON [ReceiptItems] ([OrderItemId]);

CREATE INDEX [IX_ReceiptItems_ProductId] ON [ReceiptItems] ([ProductId]);

CREATE INDEX [IX_ReceiptItems_ReceiptId] ON [ReceiptItems] ([ReceiptId]);

CREATE INDEX [IX_Receipts_LoyaltyMemberId] ON [Receipts] ([LoyaltyMemberId]);

CREATE INDEX [IX_Receipts_MergedIntoReceiptId] ON [Receipts] ([MergedIntoReceiptId]);

CREATE INDEX [IX_Receipts_OrderId] ON [Receipts] ([OrderId]);

CREATE INDEX [IX_Receipts_OwnerId] ON [Receipts] ([OwnerId]);

CREATE INDEX [IX_Receipts_ParentReceiptId] ON [Receipts] ([ParentReceiptId]);

CREATE UNIQUE INDEX [IX_Receipts_ReceiptNumber] ON [Receipts] ([ReceiptNumber]);

CREATE INDEX [IX_Receipts_SettledByUserId] ON [Receipts] ([SettledByUserId]);

CREATE INDEX [IX_Receipts_Status] ON [Receipts] ([Status]);

CREATE INDEX [IX_Receipts_StoreId] ON [Receipts] ([StoreId]);

CREATE INDEX [IX_Receipts_VoidedByUserId] ON [Receipts] ([VoidedByUserId]);

CREATE INDEX [IX_Receipts_WorkPeriodId] ON [Receipts] ([WorkPeriodId]);

CREATE INDEX [IX_ReceiptTemplates_IsActive] ON [ReceiptTemplates] ([IsActive]);

CREATE INDEX [IX_ReceiptTemplates_IsDefault] ON [ReceiptTemplates] ([IsDefault]);

CREATE INDEX [IX_ReceiptVoids_AuthorizedByUserId] ON [ReceiptVoids] ([AuthorizedByUserId]);

CREATE INDEX [IX_ReceiptVoids_ReceiptId] ON [ReceiptVoids] ([ReceiptId]);

CREATE INDEX [IX_ReceiptVoids_VoidedAt] ON [ReceiptVoids] ([VoidedAt]);

CREATE INDEX [IX_ReceiptVoids_VoidedByUserId] ON [ReceiptVoids] ([VoidedByUserId]);

CREATE INDEX [IX_ReceiptVoids_VoidReasonId] ON [ReceiptVoids] ([VoidReasonId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_AdjustmentJournalEntryId] ON [ReconciliationDiscrepancies] ([AdjustmentJournalEntryId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_BankTransactionId] ON [ReconciliationDiscrepancies] ([BankTransactionId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_PaymentId] ON [ReconciliationDiscrepancies] ([PaymentId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_ReceiptId] ON [ReconciliationDiscrepancies] ([ReceiptId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_ReconciliationSessionId] ON [ReconciliationDiscrepancies] ([ReconciliationSessionId]);

CREATE INDEX [IX_ReconciliationDiscrepancies_ResolvedByUserId] ON [ReconciliationDiscrepancies] ([ResolvedByUserId]);

CREATE INDEX [IX_ReconciliationMatches_BankTransactionId] ON [ReconciliationMatches] ([BankTransactionId]);

CREATE INDEX [IX_ReconciliationMatches_MatchedByUserId] ON [ReconciliationMatches] ([MatchedByUserId]);

CREATE INDEX [IX_ReconciliationMatches_PaymentId] ON [ReconciliationMatches] ([PaymentId]);

CREATE INDEX [IX_ReconciliationMatches_ReceiptId] ON [ReconciliationMatches] ([ReceiptId]);

CREATE INDEX [IX_ReconciliationMatches_ReconciliationSessionId] ON [ReconciliationMatches] ([ReconciliationSessionId]);

CREATE INDEX [IX_ReconciliationMatchingRules_BankAccountId] ON [ReconciliationMatchingRules] ([BankAccountId]);

CREATE INDEX [IX_ReconciliationSessions_BankAccountId] ON [ReconciliationSessions] ([BankAccountId]);

CREATE INDEX [IX_ReconciliationSessions_CompletedByUserId] ON [ReconciliationSessions] ([CompletedByUserId]);

CREATE INDEX [IX_ReconciliationSessions_StartedByUserId] ON [ReconciliationSessions] ([StartedByUserId]);

CREATE INDEX [IX_RecurringExpenseEntries_ConfirmedByUserId] ON [RecurringExpenseEntries] ([ConfirmedByUserId]);

CREATE INDEX [IX_RecurringExpenseEntries_ExpenseId] ON [RecurringExpenseEntries] ([ExpenseId]);

CREATE INDEX [IX_RecurringExpenseEntries_TemplateId] ON [RecurringExpenseEntries] ([TemplateId]);

CREATE INDEX [IX_RecurringExpenseTemplates_AccountId] ON [RecurringExpenseTemplates] ([AccountId]);

CREATE INDEX [IX_RecurringExpenseTemplates_DepartmentId] ON [RecurringExpenseTemplates] ([DepartmentId]);

CREATE INDEX [IX_RecurringExpenseTemplates_ExpenseCategoryId] ON [RecurringExpenseTemplates] ([ExpenseCategoryId]);

CREATE INDEX [IX_RecurringExpenseTemplates_StoreId] ON [RecurringExpenseTemplates] ([StoreId]);

CREATE INDEX [IX_RecurringExpenseTemplates_SupplierId] ON [RecurringExpenseTemplates] ([SupplierId]);

CREATE INDEX [IX_ReorderRules_PreferredSupplierId] ON [ReorderRules] ([PreferredSupplierId]);

CREATE INDEX [IX_ReorderRules_ProductId] ON [ReorderRules] ([ProductId]);

CREATE INDEX [IX_ReorderRules_StoreId] ON [ReorderRules] ([StoreId]);

CREATE INDEX [IX_ReorderSuggestions_ApprovedByUserId] ON [ReorderSuggestions] ([ApprovedByUserId]);

CREATE INDEX [IX_ReorderSuggestions_ProductId] ON [ReorderSuggestions] ([ProductId]);

CREATE INDEX [IX_ReorderSuggestions_PurchaseOrderId] ON [ReorderSuggestions] ([PurchaseOrderId]);

CREATE INDEX [IX_ReorderSuggestions_StoreId] ON [ReorderSuggestions] ([StoreId]);

CREATE INDEX [IX_ReorderSuggestions_SupplierId] ON [ReorderSuggestions] ([SupplierId]);

CREATE INDEX [IX_ReportExecutionLogs_SavedReportId] ON [ReportExecutionLogs] ([SavedReportId]);

CREATE INDEX [IX_ReportExecutionLogs_UserId] ON [ReportExecutionLogs] ([UserId]);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);

CREATE INDEX [IX_RoomChargePostings_OrderId] ON [RoomChargePostings] ([OrderId]);

CREATE INDEX [IX_RoomChargePostings_PMSConfigurationId] ON [RoomChargePostings] ([PMSConfigurationId]);

CREATE INDEX [IX_RoomChargePostings_ProcessedByUserId] ON [RoomChargePostings] ([ProcessedByUserId]);

CREATE INDEX [IX_RoomChargePostings_ReceiptId] ON [RoomChargePostings] ([ReceiptId]);

CREATE INDEX [IX_SavedReports_CreatedByUserId] ON [SavedReports] ([CreatedByUserId]);

CREATE INDEX [IX_SavedReports_StoreId] ON [SavedReports] ([StoreId]);

CREATE INDEX [IX_ScaleConfigurations_IsActive] ON [ScaleConfigurations] ([IsActive]);

CREATE INDEX [IX_ScheduledPriceChanges_EffectiveDate] ON [ScheduledPriceChanges] ([EffectiveDate]);

CREATE INDEX [IX_ScheduledPriceChanges_PricingZoneId] ON [ScheduledPriceChanges] ([PricingZoneId]);

CREATE INDEX [IX_ScheduledPriceChanges_ProductId] ON [ScheduledPriceChanges] ([ProductId]);

CREATE INDEX [IX_ScheduledPriceChanges_Status] ON [ScheduledPriceChanges] ([Status]);

CREATE INDEX [IX_ScheduledPriceChanges_Status_EffectiveDate] ON [ScheduledPriceChanges] ([Status], [EffectiveDate]);

CREATE INDEX [IX_ScheduledPriceChanges_StoreId] ON [ScheduledPriceChanges] ([StoreId]);

CREATE UNIQUE INDEX [IX_Sections_FloorId_Name] ON [Sections] ([FloorId], [Name]) WHERE [IsActive] = 1;

CREATE INDEX [IX_ShrinkageAnalysisPeriods_StoreId] ON [ShrinkageAnalysisPeriods] ([StoreId]);

CREATE INDEX [IX_ShrinkageRecords_DepartmentId] ON [ShrinkageRecords] ([DepartmentId]);

CREATE INDEX [IX_ShrinkageRecords_ProductId] ON [ShrinkageRecords] ([ProductId]);

CREATE INDEX [IX_ShrinkageRecords_RecordedByUserId] ON [ShrinkageRecords] ([RecordedByUserId]);

CREATE INDEX [IX_ShrinkageRecords_StoreId] ON [ShrinkageRecords] ([StoreId]);

CREATE INDEX [IX_SplitPaymentConfigs_StoreId] ON [SplitPaymentConfigs] ([StoreId]);

CREATE INDEX [IX_SplitPaymentParts_SplitSessionId] ON [SplitPaymentParts] ([SplitSessionId]);

CREATE INDEX [IX_SplitPaymentSessions_InitiatedByUserId] ON [SplitPaymentSessions] ([InitiatedByUserId]);

CREATE INDEX [IX_SplitPaymentSessions_ReceiptId] ON [SplitPaymentSessions] ([ReceiptId]);

CREATE INDEX [IX_StockMovements_AdjustmentReasonId] ON [StockMovements] ([AdjustmentReasonId]);

CREATE INDEX [IX_StockMovements_ProductId] ON [StockMovements] ([ProductId]);

CREATE INDEX [IX_StockMovements_StoreId] ON [StockMovements] ([StoreId]);

CREATE INDEX [IX_StockMovements_UserId] ON [StockMovements] ([UserId]);

CREATE INDEX [IX_StockReservations_ExpiresAt] ON [StockReservations] ([ExpiresAt]);

CREATE INDEX [IX_StockReservations_Location_Product_Status] ON [StockReservations] ([LocationId], [ProductId], [Status]);

CREATE INDEX [IX_StockReservations_LocationId] ON [StockReservations] ([LocationId]);

CREATE INDEX [IX_StockReservations_ProductId] ON [StockReservations] ([ProductId]);

CREATE INDEX [IX_StockReservations_Reference] ON [StockReservations] ([ReferenceType], [ReferenceId]);

CREATE INDEX [IX_StockReservations_Status] ON [StockReservations] ([Status]);

CREATE INDEX [IX_StockTakeItems_CountedByUserId] ON [StockTakeItems] ([CountedByUserId]);

CREATE INDEX [IX_StockTakeItems_ProductId] ON [StockTakeItems] ([ProductId]);

CREATE UNIQUE INDEX [IX_StockTakeItems_StockTakeId_ProductId] ON [StockTakeItems] ([StockTakeId], [ProductId]);

CREATE INDEX [IX_StockTakes_ApprovedByUserId] ON [StockTakes] ([ApprovedByUserId]);

CREATE INDEX [IX_StockTakes_StartedByUserId] ON [StockTakes] ([StartedByUserId]);

CREATE INDEX [IX_StockTakes_Status] ON [StockTakes] ([Status]);

CREATE UNIQUE INDEX [IX_StockTakes_StockTakeNumber] ON [StockTakes] ([StockTakeNumber]);

CREATE INDEX [IX_StockTransferReceipts_HasIssues] ON [StockTransferReceipts] ([HasIssues]);

CREATE UNIQUE INDEX [IX_StockTransferReceipts_ReceiptNumber] ON [StockTransferReceipts] ([ReceiptNumber]);

CREATE INDEX [IX_StockTransferReceipts_ReceivedAt] ON [StockTransferReceipts] ([ReceivedAt]);

CREATE INDEX [IX_StockTransferReceipts_TransferRequestId] ON [StockTransferReceipts] ([TransferRequestId]);

CREATE INDEX [IX_StockTransferRequests_RequestingStore] ON [StockTransferRequests] ([RequestingStoreId]);

CREATE UNIQUE INDEX [IX_StockTransferRequests_RequestNumber] ON [StockTransferRequests] ([RequestNumber]);

CREATE INDEX [IX_StockTransferRequests_SourceLocation] ON [StockTransferRequests] ([SourceLocationId]);

CREATE INDEX [IX_StockTransferRequests_Status] ON [StockTransferRequests] ([Status]);

CREATE INDEX [IX_StockTransferRequests_Status_Priority] ON [StockTransferRequests] ([Status], [Priority]);

CREATE INDEX [IX_StockTransferRequests_SubmittedAt] ON [StockTransferRequests] ([SubmittedAt]);

CREATE UNIQUE INDEX [IX_StockTransferShipments_ShipmentNumber] ON [StockTransferShipments] ([ShipmentNumber]);

CREATE INDEX [IX_StockTransferShipments_ShippedAt] ON [StockTransferShipments] ([ShippedAt]);

CREATE UNIQUE INDEX [IX_StockTransferShipments_TransferRequestId] ON [StockTransferShipments] ([TransferRequestId]);

CREATE INDEX [IX_StockValuationConfigs_StoreId] ON [StockValuationConfigs] ([StoreId]);

CREATE INDEX [IX_StockValuationDetails_ProductId] ON [StockValuationDetails] ([ProductId]);

CREATE INDEX [IX_StockValuationDetails_SnapshotId] ON [StockValuationDetails] ([SnapshotId]);

CREATE INDEX [IX_StockValuationSnapshots_StoreId] ON [StockValuationSnapshots] ([StoreId]);

CREATE INDEX [IX_StoreProductOverrides_IsAvailable] ON [StoreProductOverrides] ([IsAvailable]);

CREATE INDEX [IX_StoreProductOverrides_ProductId] ON [StoreProductOverrides] ([ProductId]);

CREATE UNIQUE INDEX [IX_StoreProductOverrides_Store_Product] ON [StoreProductOverrides] ([StoreId], [ProductId]);

CREATE INDEX [IX_StoreProductOverrides_StoreId] ON [StoreProductOverrides] ([StoreId]);

CREATE INDEX [IX_Stores_City] ON [Stores] ([City]);

CREATE INDEX [IX_Stores_IsActive] ON [Stores] ([IsActive]);

CREATE INDEX [IX_Stores_IsHeadquarters] ON [Stores] ([IsHeadquarters]);

CREATE INDEX [IX_Stores_PricingZoneId] ON [Stores] ([PricingZoneId]);

CREATE UNIQUE INDEX [IX_Stores_StoreCode] ON [Stores] ([StoreCode]);

CREATE INDEX [IX_SupplierInvoices_PurchaseOrderId] ON [SupplierInvoices] ([PurchaseOrderId]);

CREATE UNIQUE INDEX [IX_SupplierInvoices_SupplierId_InvoiceNumber] ON [SupplierInvoices] ([SupplierId], [InvoiceNumber]);

CREATE INDEX [IX_SupplierPayments_ProcessedByUserId] ON [SupplierPayments] ([ProcessedByUserId]);

CREATE INDEX [IX_SupplierPayments_SupplierId] ON [SupplierPayments] ([SupplierId]);

CREATE INDEX [IX_SupplierPayments_SupplierInvoiceId] ON [SupplierPayments] ([SupplierInvoiceId]);

CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]);

CREATE INDEX [IX_SuspendedTransactionItems_ProductId] ON [SuspendedTransactionItems] ([ProductId]);

CREATE INDEX [IX_SuspendedTransactionItems_SuspendedTransactionId] ON [SuspendedTransactionItems] ([SuspendedTransactionId]);

CREATE INDEX [IX_SuspendedTransactions_CompletedReceiptId] ON [SuspendedTransactions] ([CompletedReceiptId]);

CREATE INDEX [IX_SuspendedTransactions_LoyaltyMemberId] ON [SuspendedTransactions] ([LoyaltyMemberId]);

CREATE INDEX [IX_SuspendedTransactions_ParkedByUserId] ON [SuspendedTransactions] ([ParkedByUserId]);

CREATE INDEX [IX_SuspendedTransactions_RecalledByUserId] ON [SuspendedTransactions] ([RecalledByUserId]);

CREATE INDEX [IX_SuspendedTransactions_StoreId] ON [SuspendedTransactions] ([StoreId]);

CREATE INDEX [IX_SyncBatches_Direction] ON [SyncBatches] ([Direction]);

CREATE INDEX [IX_SyncBatches_Status] ON [SyncBatches] ([Status]);

CREATE INDEX [IX_SyncBatches_Store_Created] ON [SyncBatches] ([StoreId], [CreatedAt]);

CREATE UNIQUE INDEX [IX_SyncConfigurations_StoreId] ON [SyncConfigurations] ([StoreId]);

CREATE INDEX [IX_SyncConflicts_BatchId] ON [SyncConflicts] ([SyncBatchId]);

CREATE INDEX [IX_SyncConflicts_Entity] ON [SyncConflicts] ([EntityType], [EntityId]);

CREATE INDEX [IX_SyncConflicts_Resolved] ON [SyncConflicts] ([IsResolved]);

CREATE UNIQUE INDEX [IX_SyncEntityRules_Config_Entity] ON [SyncEntityRules] ([SyncConfigurationId], [EntityType]);

CREATE INDEX [IX_SyncLogs_Store_Timestamp] ON [SyncLogs] ([StoreId], [Timestamp]);

CREATE INDEX [IX_SyncLogs_Success] ON [SyncLogs] ([IsSuccess]);

CREATE INDEX [IX_SyncLogs_SyncBatchId] ON [SyncLogs] ([SyncBatchId]);

CREATE INDEX [IX_SyncLogs_Timestamp] ON [SyncLogs] ([Timestamp]);

CREATE INDEX [IX_SyncQueues_StoreId] ON [SyncQueues] ([StoreId]);

CREATE INDEX [IX_SyncQueues_SyncBatchId] ON [SyncQueues] ([SyncBatchId]);

CREATE INDEX [IX_SyncRecords_BatchId] ON [SyncRecords] ([SyncBatchId]);

CREATE INDEX [IX_SyncRecords_Entity] ON [SyncRecords] ([EntityType], [EntityId]);

CREATE UNIQUE INDEX [IX_SystemSettings_SettingKey] ON [SystemSettings] ([SettingKey]);

CREATE INDEX [IX_Tables_AssignedUserId] ON [Tables] ([AssignedUserId]);

CREATE INDEX [IX_Tables_CurrentReceiptId] ON [Tables] ([CurrentReceiptId]);

CREATE UNIQUE INDEX [IX_Tables_FloorId_TableNumber] ON [Tables] ([FloorId], [TableNumber]) WHERE [IsActive] = 1;

CREATE INDEX [IX_Tables_SectionId] ON [Tables] ([SectionId]);

CREATE INDEX [IX_Tables_Status] ON [Tables] ([Status]);

CREATE INDEX [IX_TableTransferLogs_FromUserId] ON [TableTransferLogs] ([FromUserId]);

CREATE INDEX [IX_TableTransferLogs_ReceiptId] ON [TableTransferLogs] ([ReceiptId]);

CREATE INDEX [IX_TableTransferLogs_TableId] ON [TableTransferLogs] ([TableId]);

CREATE INDEX [IX_TableTransferLogs_ToUserId] ON [TableTransferLogs] ([ToUserId]);

CREATE INDEX [IX_TableTransferLogs_TransferredAt] ON [TableTransferLogs] ([TransferredAt]);

CREATE INDEX [IX_TableTransferLogs_TransferredByUserId] ON [TableTransferLogs] ([TransferredByUserId]);

CREATE INDEX [IX_TierConfigurations_IsActive] ON [TierConfigurations] ([IsActive]);

CREATE INDEX [IX_TierConfigurations_SortOrder] ON [TierConfigurations] ([SortOrder]);

CREATE UNIQUE INDEX [IX_TierConfigurations_Tier] ON [TierConfigurations] ([Tier]);

CREATE INDEX [IX_TKashConfigurations_StoreId] ON [TKashConfigurations] ([StoreId]);

CREATE INDEX [IX_TKashRequests_ReceiptId] ON [TKashRequests] ([ReceiptId]);

CREATE INDEX [IX_TKashRequests_StoreId] ON [TKashRequests] ([StoreId]);

CREATE INDEX [IX_TKashRequests_UserId] ON [TKashRequests] ([UserId]);

CREATE INDEX [IX_TransferActivityLogs_PerformedAt] ON [TransferActivityLogs] ([PerformedAt]);

CREATE INDEX [IX_TransferActivityLogs_RequestId] ON [TransferActivityLogs] ([TransferRequestId]);

CREATE INDEX [IX_TransferReceiptIssues_IssueType] ON [TransferReceiptIssues] ([IssueType]);

CREATE INDEX [IX_TransferReceiptIssues_ReceiptId] ON [TransferReceiptIssues] ([TransferReceiptId]);

CREATE INDEX [IX_TransferReceiptIssues_Resolved] ON [TransferReceiptIssues] ([IsResolved]);

CREATE INDEX [IX_TransferReceiptIssues_TransferReceiptId1] ON [TransferReceiptIssues] ([TransferReceiptId1]);

CREATE INDEX [IX_TransferReceiptIssues_TransferReceiptLineId] ON [TransferReceiptIssues] ([TransferReceiptLineId]);

CREATE INDEX [IX_TransferReceiptLines_ProductId] ON [TransferReceiptLines] ([ProductId]);

CREATE INDEX [IX_TransferReceiptLines_ReceiptId] ON [TransferReceiptLines] ([TransferReceiptId]);

CREATE INDEX [IX_TransferReceiptLines_TransferRequestLineId] ON [TransferReceiptLines] ([TransferRequestLineId]);

CREATE INDEX [IX_TransferRequestLines_ProductId] ON [TransferRequestLines] ([ProductId]);

CREATE INDEX [IX_TransferRequestLines_RequestId] ON [TransferRequestLines] ([TransferRequestId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [IX_Users_PIN] ON [Users] ([PIN]);

CREATE INDEX [IX_Users_StoreId] ON [Users] ([StoreId]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE INDEX [IX_VoidReasons_DisplayOrder] ON [VoidReasons] ([DisplayOrder]);

CREATE UNIQUE INDEX [IX_VoidReasons_Name] ON [VoidReasons] ([Name]);

CREATE INDEX [IX_WebhookConfigs_ApiClientId] ON [WebhookConfigs] ([ApiClientId]);

CREATE INDEX [IX_WebhookDeliveries_WebhookConfigId] ON [WebhookDeliveries] ([WebhookConfigId]);

CREATE INDEX [IX_WeightedBarcodeConfigs_IsActive] ON [WeightedBarcodeConfigs] ([IsActive]);

CREATE INDEX [IX_WeightedBarcodeConfigs_Prefix] ON [WeightedBarcodeConfigs] ([Prefix]);

CREATE INDEX [IX_WorkPeriods_ClosedByUserId] ON [WorkPeriods] ([ClosedByUserId]);

CREATE INDEX [IX_WorkPeriods_OpenedByUserId] ON [WorkPeriods] ([OpenedByUserId]);

CREATE INDEX [IX_ZonePrices_EffectiveFrom] ON [ZonePrices] ([EffectiveFrom]);

CREATE INDEX [IX_ZonePrices_ProductId] ON [ZonePrices] ([ProductId]);

CREATE UNIQUE INDEX [IX_ZonePrices_Zone_Product_EffectiveFrom] ON [ZonePrices] ([PricingZoneId], [ProductId], [EffectiveFrom]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260118121509_InitialCreate', N'10.0.0');

COMMIT;
GO

