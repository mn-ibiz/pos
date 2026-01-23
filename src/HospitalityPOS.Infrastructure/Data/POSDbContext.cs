using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the POS system.
/// </summary>
public class POSDbContext : DbContext
{
    private readonly int? _currentWorkPeriodId;
    private readonly int? _currentUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="POSDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    [ActivatorUtilitiesConstructor]
    public POSDbContext(DbContextOptions<POSDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance with work period context.
    /// </summary>
    /// <param name="options">The database context options.</param>
    /// <param name="currentWorkPeriodId">The current work period ID for filtering.</param>
    /// <param name="currentUserId">The current user ID for auditing.</param>
    public POSDbContext(
        DbContextOptions<POSDbContext> options,
        int? currentWorkPeriodId,
        int? currentUserId) : base(options)
    {
        _currentWorkPeriodId = currentWorkPeriodId;
        _currentUserId = currentUserId;
    }

    #region User & Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    #endregion

    #region Work Period
    public DbSet<WorkPeriod> WorkPeriods => Set<WorkPeriod>();
    #endregion

    #region Products & Categories
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOffer> ProductOffers => Set<ProductOffer>();
    public DbSet<ProductFavorite> ProductFavorites => Set<ProductFavorite>();
    #endregion

    #region Product Variants
    public DbSet<VariantOption> VariantOptions => Set<VariantOption>();
    public DbSet<VariantOptionValue> VariantOptionValues => Set<VariantOptionValue>();
    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantValue> ProductVariantValues => Set<ProductVariantValue>();
    #endregion

    #region Product Modifiers
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<ModifierItem> ModifierItems => Set<ModifierItem>();
    public DbSet<ModifierItemNestedGroup> ModifierItemNestedGroups => Set<ModifierItemNestedGroup>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();
    public DbSet<CategoryModifierGroup> CategoryModifierGroups => Set<CategoryModifierGroup>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<ModifierPreset> ModifierPresets => Set<ModifierPreset>();
    public DbSet<ModifierPresetItem> ModifierPresetItems => Set<ModifierPresetItem>();
    #endregion

    #region Orders & Receipts
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<VoidReason> VoidReasons => Set<VoidReason>();
    public DbSet<ReceiptVoid> ReceiptVoids => Set<ReceiptVoid>();
    #endregion

    #region Inventory
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AdjustmentReason> AdjustmentReasons => Set<AdjustmentReason>();
    public DbSet<StockTake> StockTakes => Set<StockTake>();
    public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();
    #endregion

    #region Suppliers & Purchasing
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceivedNote> GoodsReceivedNotes => Set<GoodsReceivedNote>();
    public DbSet<GRNItem> GRNItems => Set<GRNItem>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    #endregion

    #region Employees & Payroll
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents => Set<EmployeeSalaryComponent>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<PayslipDetail> PayslipDetails => Set<PayslipDetail>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    #endregion

    #region HR - Statutory Configuration
    public DbSet<PAYETaxBand> PAYETaxBands => Set<PAYETaxBand>();
    public DbSet<PAYERelief> PAYEReliefs => Set<PAYERelief>();
    public DbSet<NSSFConfiguration> NSSFConfigurations => Set<NSSFConfiguration>();
    public DbSet<SHIFConfiguration> SHIFConfigurations => Set<SHIFConfiguration>();
    public DbSet<HousingLevyConfiguration> HousingLevyConfigurations => Set<HousingLevyConfiguration>();
    public DbSet<HELBDeductionBand> HELBDeductionBands => Set<HELBDeductionBand>();
    #endregion

    #region HR - Leave Management
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveAllocation> LeaveAllocations => Set<LeaveAllocation>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments => Set<LeaveBalanceAdjustment>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    #endregion

    #region HR - Loans & Advances
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<LoanRepayment> LoanRepayments => Set<LoanRepayment>();
    #endregion

    #region HR - Disciplinary & Termination
    public DbSet<DisciplinaryDeduction> DisciplinaryDeductions => Set<DisciplinaryDeduction>();
    public DbSet<EmployeeTermination> EmployeeTerminations => Set<EmployeeTermination>();
    #endregion

    #region Expenses
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();
    public DbSet<ExpenseBudget> ExpenseBudgets => Set<ExpenseBudget>();
    public DbSet<ExpenseAttachment> ExpenseAttachments => Set<ExpenseAttachment>();
    #endregion

    #region Accounting
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    #endregion

    #region eTIMS
    public DbSet<EtimsDevice> EtimsDevices => Set<EtimsDevice>();
    public DbSet<EtimsInvoice> EtimsInvoices => Set<EtimsInvoice>();
    public DbSet<EtimsInvoiceItem> EtimsInvoiceItems => Set<EtimsInvoiceItem>();
    public DbSet<EtimsCreditNote> EtimsCreditNotes => Set<EtimsCreditNote>();
    public DbSet<EtimsCreditNoteItem> EtimsCreditNoteItems => Set<EtimsCreditNoteItem>();
    public DbSet<EtimsQueueEntry> EtimsQueue => Set<EtimsQueueEntry>();
    public DbSet<EtimsSyncLog> EtimsSyncLogs => Set<EtimsSyncLog>();
    #endregion

    #region M-Pesa
    public DbSet<MpesaConfiguration> MpesaConfigurations => Set<MpesaConfiguration>();
    public DbSet<MpesaStkPushRequest> MpesaStkPushRequests => Set<MpesaStkPushRequest>();
    public DbSet<MpesaTransaction> MpesaTransactions => Set<MpesaTransaction>();
    #endregion

    #region Additional Mobile Money (Airtel, T-Kash)
    public DbSet<AirtelMoneyConfiguration> AirtelMoneyConfigurations => Set<AirtelMoneyConfiguration>();
    public DbSet<AirtelMoneyRequest> AirtelMoneyRequests => Set<AirtelMoneyRequest>();
    public DbSet<TKashConfiguration> TKashConfigurations => Set<TKashConfiguration>();
    public DbSet<TKashRequest> TKashRequests => Set<TKashRequest>();
    public DbSet<MobileMoneyTransactionLog> MobileMoneyTransactionLogs => Set<MobileMoneyTransactionLog>();
    #endregion

    #region Barcode & PLU
    public DbSet<PLUCode> PLUCodes => Set<PLUCode>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<WeightedBarcodeConfig> WeightedBarcodeConfigs => Set<WeightedBarcodeConfig>();
    public DbSet<ScaleConfiguration> ScaleConfigurations => Set<ScaleConfiguration>();
    public DbSet<InternalBarcodeSequence> InternalBarcodeSequences => Set<InternalBarcodeSequence>();
    #endregion

    #region System
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    #endregion

    #region Printers
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PrinterSettings> PrinterSettings => Set<PrinterSettings>();
    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();
    public DbSet<PrinterCategoryMapping> PrinterCategoryMappings => Set<PrinterCategoryMapping>();
    public DbSet<KOTSettings> KOTSettings => Set<KOTSettings>();
    #endregion

    #region Cash Drawers
    public DbSet<CashDrawer> CashDrawers => Set<CashDrawer>();
    public DbSet<CashDrawerLog> CashDrawerLogs => Set<CashDrawerLog>();
    #endregion

    #region Floor & Table Management
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TableTransferLog> TableTransferLogs => Set<TableTransferLog>();
    #endregion

    #region Loyalty Program
    public DbSet<LoyaltyMember> LoyaltyMembers => Set<LoyaltyMember>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<PointsConfiguration> PointsConfigurations => Set<PointsConfiguration>();
    public DbSet<TierConfiguration> TierConfigurations => Set<TierConfiguration>();
    #endregion

    #region Multi-Store Management
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreProductOverride> StoreProductOverrides => Set<StoreProductOverride>();
    public DbSet<PricingZone> PricingZones => Set<PricingZone>();
    public DbSet<ZonePrice> ZonePrices => Set<ZonePrice>();
    public DbSet<ScheduledPriceChange> ScheduledPriceChanges => Set<ScheduledPriceChange>();
    public DbSet<CentralPromotion> CentralPromotions => Set<CentralPromotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<PromotionCategory> PromotionCategories => Set<PromotionCategory>();
    public DbSet<PromotionDeployment> PromotionDeployments => Set<PromotionDeployment>();
    public DbSet<DeploymentZone> DeploymentZones => Set<DeploymentZone>();
    public DbSet<DeploymentStore> DeploymentStores => Set<DeploymentStore>();
    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
    #endregion

    #region Advanced Promotions
    public DbSet<BogoPromotion> BogoPromotions => Set<BogoPromotion>();
    public DbSet<MixMatchPromotion> MixMatchPromotions => Set<MixMatchPromotion>();
    public DbSet<MixMatchGroup> MixMatchGroups => Set<MixMatchGroup>();
    public DbSet<MixMatchGroupProduct> MixMatchGroupProducts => Set<MixMatchGroupProduct>();
    public DbSet<MixMatchGroupCategory> MixMatchGroupCategories => Set<MixMatchGroupCategory>();
    public DbSet<QuantityBreakTier> QuantityBreakTiers => Set<QuantityBreakTier>();
    public DbSet<ComboPromotion> ComboPromotions => Set<ComboPromotion>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponBatch> CouponBatches => Set<CouponBatch>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<AutomaticMarkdown> AutomaticMarkdowns => Set<AutomaticMarkdown>();
    public DbSet<PromotionApplication> PromotionApplications => Set<PromotionApplication>();
    #endregion

    #region Accounts Receivable
    public DbSet<CustomerCreditAccount> CustomerCreditAccounts => Set<CustomerCreditAccount>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<CustomerStatement> CustomerStatements => Set<CustomerStatement>();
    #endregion

    #region Bank Reconciliation
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankStatementImport> BankStatementImports => Set<BankStatementImport>();
    public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();
    public DbSet<ReconciliationDiscrepancy> ReconciliationDiscrepancies => Set<ReconciliationDiscrepancy>();
    public DbSet<ReconciliationMatchingRule> ReconciliationMatchingRules => Set<ReconciliationMatchingRule>();
    #endregion

    #region Hotel PMS Integration
    public DbSet<PMSConfiguration> PMSConfigurations => Set<PMSConfiguration>();
    public DbSet<PMSRevenueCenter> PMSRevenueCenters => Set<PMSRevenueCenter>();
    public DbSet<RoomChargePosting> RoomChargePostings => Set<RoomChargePosting>();
    public DbSet<PMSGuestLookup> PMSGuestLookups => Set<PMSGuestLookup>();
    public DbSet<PMSPostingQueue> PMSPostingQueues => Set<PMSPostingQueue>();
    public DbSet<PMSActivityLog> PMSActivityLogs => Set<PMSActivityLog>();
    public DbSet<PMSErrorMapping> PMSErrorMappings => Set<PMSErrorMapping>();
    #endregion

    #region REST API
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
    public DbSet<ApiClientScope> ApiClientScopes => Set<ApiClientScope>();
    public DbSet<ApiScope> ApiScopes => Set<ApiScope>();
    public DbSet<ApiAccessToken> ApiAccessTokens => Set<ApiAccessToken>();
    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();
    public DbSet<ApiRateLimitEntry> ApiRateLimitEntries => Set<ApiRateLimitEntry>();
    public DbSet<WebhookConfig> WebhookConfigs => Set<WebhookConfig>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    #endregion

    #region Financial Reporting
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<OverheadAllocationRule> OverheadAllocationRules => Set<OverheadAllocationRule>();
    public DbSet<OverheadAllocationDetail> OverheadAllocationDetails => Set<OverheadAllocationDetail>();
    public DbSet<CashFlowMapping> CashFlowMappings => Set<CashFlowMapping>();
    public DbSet<SavedReport> SavedReports => Set<SavedReport>();
    public DbSet<ReportExecutionLog> ReportExecutionLogs => Set<ReportExecutionLog>();
    public DbSet<MarginThreshold> MarginThresholds => Set<MarginThreshold>();
    #endregion

    #region Budget & Cost Management
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<RecurringExpenseTemplate> RecurringExpenseTemplates => Set<RecurringExpenseTemplate>();
    public DbSet<RecurringExpenseEntry> RecurringExpenseEntries => Set<RecurringExpenseEntry>();
    #endregion

    #region Checkout Enhancements
    public DbSet<SuspendedTransaction> SuspendedTransactions => Set<SuspendedTransaction>();
    public DbSet<SuspendedTransactionItem> SuspendedTransactionItems => Set<SuspendedTransactionItem>();
    public DbSet<CustomerDisplayConfig> CustomerDisplayConfigs => Set<CustomerDisplayConfig>();
    public DbSet<CustomerDisplayMessage> CustomerDisplayMessages => Set<CustomerDisplayMessage>();
    public DbSet<SplitPaymentConfig> SplitPaymentConfigs => Set<SplitPaymentConfig>();
    public DbSet<SplitPaymentSession> SplitPaymentSessions => Set<SplitPaymentSession>();
    public DbSet<SplitPaymentPart> SplitPaymentParts => Set<SplitPaymentPart>();
    public DbSet<QuickAmountButton> QuickAmountButtons => Set<QuickAmountButton>();
    public DbSet<QuickAmountButtonSet> QuickAmountButtonSets => Set<QuickAmountButtonSet>();
    #endregion

    #region Inventory Analytics
    public DbSet<StockValuationConfig> StockValuationConfigs => Set<StockValuationConfig>();
    public DbSet<StockValuationSnapshot> StockValuationSnapshots => Set<StockValuationSnapshot>();
    public DbSet<StockValuationDetail> StockValuationDetails => Set<StockValuationDetail>();
    public DbSet<ReorderRule> ReorderRules => Set<ReorderRule>();
    public DbSet<ReorderSuggestion> ReorderSuggestions => Set<ReorderSuggestion>();
    public DbSet<ShrinkageRecord> ShrinkageRecords => Set<ShrinkageRecord>();
    public DbSet<ShrinkageAnalysisPeriod> ShrinkageAnalysisPeriods => Set<ShrinkageAnalysisPeriod>();
    public DbSet<DeadStockItem> DeadStockItems => Set<DeadStockItem>();
    public DbSet<DeadStockConfig> DeadStockConfigs => Set<DeadStockConfig>();
    public DbSet<InventoryTurnoverAnalysis> InventoryTurnoverAnalyses => Set<InventoryTurnoverAnalysis>();
    #endregion

    #region Store Synchronization
    public DbSet<SyncConfiguration> SyncConfigurations => Set<SyncConfiguration>();
    public DbSet<SyncEntityRule> SyncEntityRules => Set<SyncEntityRule>();
    public DbSet<SyncBatch> SyncBatches => Set<SyncBatch>();
    public DbSet<SyncRecord> SyncRecords => Set<SyncRecord>();
    public DbSet<SyncConflict> SyncConflicts => Set<SyncConflict>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<SyncQueueItem> SyncQueues => Set<SyncQueueItem>();
    #endregion

    #region Stock Transfers
    public DbSet<StockTransferRequest> StockTransferRequests => Set<StockTransferRequest>();
    public DbSet<TransferRequestLine> TransferRequestLines => Set<TransferRequestLine>();
    public DbSet<StockTransferShipment> StockTransferShipments => Set<StockTransferShipment>();
    public DbSet<StockTransferReceipt> StockTransferReceipts => Set<StockTransferReceipt>();
    public DbSet<TransferReceiptLine> TransferReceiptLines => Set<TransferReceiptLine>();
    public DbSet<TransferReceiptIssue> TransferReceiptIssues => Set<TransferReceiptIssue>();
    public DbSet<TransferActivityLog> TransferActivityLogs => Set<TransferActivityLog>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    #endregion

    #region Batch & Expiry Tracking
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<ProductBatchConfiguration> ProductBatchConfigurations => Set<ProductBatchConfiguration>();
    public DbSet<BatchStockMovement> BatchStockMovements => Set<BatchStockMovement>();
    public DbSet<BatchDisposal> BatchDisposals => Set<BatchDisposal>();
    public DbSet<ExpirySaleBlock> ExpirySaleBlocks => Set<ExpirySaleBlock>();
    public DbSet<CategoryExpirySettings> CategoryExpirySettings => Set<CategoryExpirySettings>();
    public DbSet<BatchRecallAlert> BatchRecallAlerts => Set<BatchRecallAlert>();
    public DbSet<RecallAction> RecallActions => Set<RecallAction>();
    #endregion

    #region Email & Notifications
    public DbSet<EmailConfiguration> EmailConfigurations => Set<EmailConfiguration>();
    public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
    public DbSet<EmailSchedule> EmailSchedules => Set<EmailSchedule>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<LowStockAlertConfig> LowStockAlertConfigs => Set<LowStockAlertConfig>();
    public DbSet<ExpiryAlertConfig> ExpiryAlertConfigs => Set<ExpiryAlertConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();
    #endregion

    #region Purchase Order Automation
    public DbSet<PurchaseOrderSettings> PurchaseOrderSettings => Set<PurchaseOrderSettings>();
    #endregion

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(POSDbContext).Assembly);

        // SQL Server does not support multiple cascade paths - disable all cascade deletes globally
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Apply global soft delete filter to all ISoftDeletable entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
                var filter = System.Linq.Expressions.Expression.Lambda(property, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditable>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedByUserId = _currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedByUserId = _currentUserId;
                    break;
            }
        }
    }
}
