using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.BackgroundJobs;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Jobs;
using HospitalityPOS.Infrastructure.Repositories;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the database connection service for connection configuration management.
    /// This should be called early, before other infrastructure services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseConnectionService(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseConnectionService, DatabaseConnectionService>();
        return services;
    }

    /// <summary>
    /// Adds all infrastructure layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString)
    {
        // Database Context
        services.AddDbContext<POSDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sqlOptions.MigrationsAssembly(typeof(POSDbContext).Assembly.FullName);

                // Command timeout for long-running operations
                sqlOptions.CommandTimeout(30);
            });

#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        // DbContext factory shares same configuration as AddDbContext
        services.AddDbContextFactory<POSDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsAssembly(typeof(POSDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(30);
            });
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        }, lifetime: ServiceLifetime.Scoped);

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Entity-specific Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IWorkPeriodRepository, WorkPeriodRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStockTakeRepository, StockTakeRepository>();
        services.AddScoped<IStockTakeItemRepository, StockTakeItemRepository>();

        // Infrastructure Services
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockTakeService, StockTakeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IGoodsReceivingService, GoodsReceivingService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

        // Loyalty Services
        services.AddScoped<ILoyaltyMemberRepository, LoyaltyMemberRepository>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IBirthdayRewardService, BirthdayRewardService>();
        services.AddScoped<IPointsMultiplierService, PointsMultiplierService>();
        services.AddSingleton<ISmsService, SmsService>();
        services.AddScoped<IOtpService, OtpService>();

        // Advanced Promotions
        services.AddScoped<IAdvancedPromotionService, AdvancedPromotionService>();

        // Accounts Receivable
        services.AddScoped<IAccountsReceivableService, AccountsReceivableService>();

        // Bank Reconciliation
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();

        // Hotel PMS Integration - configure with timeout to prevent hanging requests
        services.AddHttpClient("PMS", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IHotelPMSService, HotelPMSService>();

        // REST API Layer - configure with timeout for webhook delivery
        services.AddHttpClient("Webhook", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IApiService, ApiService>();

        // Additional Mobile Money (Airtel, T-Kash) - configure with timeout for payment APIs
        services.AddHttpClient("MobileMoney", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for payment processing
        });
        services.AddScoped<IMobileMoneyService, MobileMoneyService>();

        // Enhanced Financial Reporting
        services.AddScoped<IFinancialReportingService, FinancialReportingService>();

        // Budget & Cost Management
        services.AddScoped<IBudgetService, BudgetService>();

        // Checkout Enhancements
        services.AddScoped<ICheckoutEnhancementService, CheckoutEnhancementService>();

        // Inventory Analytics
        services.AddScoped<IInventoryAnalyticsService, InventoryAnalyticsService>();

        // Stock Monitoring Background Job
        services.Configure<StockMonitoringOptions>(options => { });
        services.AddSingleton<StockMonitoringJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<StockMonitoringJob>());
        services.AddSingleton<IStockMonitoringService>(sp => sp.GetRequiredService<StockMonitoringJob>());

        // Loyalty Points Expiry Background Job
        services.AddSingleton<ExpirePointsJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ExpirePointsJob>());
        services.AddSingleton<IExpirePointsJob>(sp => sp.GetRequiredService<ExpirePointsJob>());

        // Birthday Reward Background Job
        services.AddSingleton<BirthdayRewardJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BirthdayRewardJob>());
        services.AddSingleton<IBirthdayRewardJob>(sp => sp.GetRequiredService<BirthdayRewardJob>());

        // Referral Program Services
        services.AddScoped<IReferralService, ReferralService>();

        // Referral Expiry Background Job
        services.AddSingleton<ReferralExpiryJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ReferralExpiryJob>());
        services.AddSingleton<IReferralExpiryJob>(sp => sp.GetRequiredService<ReferralExpiryJob>());

        // Gamification Services (Badges, Challenges, Streaks)
        services.AddScoped<IGamificationService, GamificationService>();

        // Gamification Background Jobs
        services.AddSingleton<StreakCheckJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<StreakCheckJob>());
        services.AddSingleton<IStreakCheckJob>(sp => sp.GetRequiredService<StreakCheckJob>());

        services.AddSingleton<ChallengeExpiryJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ChallengeExpiryJob>());
        services.AddSingleton<IChallengeExpiryJob>(sp => sp.GetRequiredService<ChallengeExpiryJob>());

        // Marketing Campaign Flow Services
        services.AddScoped<ICampaignFlowService, CampaignFlowService>();
        services.AddScoped<ICampaignFlowTriggerService, CampaignFlowService>();

        // Campaign Flow Background Jobs
        services.AddSingleton<CampaignFlowProcessorJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CampaignFlowProcessorJob>());
        services.AddSingleton<ICampaignFlowProcessorJob>(sp => sp.GetRequiredService<CampaignFlowProcessorJob>());

        services.AddSingleton<CampaignFlowTriggerJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CampaignFlowTriggerJob>());
        services.AddSingleton<ICampaignFlowTriggerJob>(sp => sp.GetRequiredService<CampaignFlowTriggerJob>());

        // AI Upsell Recommendation Services
        services.AddScoped<IUpsellService, UpsellService>();

        // Upsell Background Jobs
        services.AddSingleton<AssociationMiningJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AssociationMiningJob>());
        services.AddSingleton<IAssociationMiningJob>(sp => sp.GetRequiredService<AssociationMiningJob>());

        services.AddSingleton<CustomerPreferenceJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CustomerPreferenceJob>());
        services.AddSingleton<ICustomerPreferenceJob>(sp => sp.GetRequiredService<CustomerPreferenceJob>());

        // Stock Transfer Services (Epic 23)
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<IStockReservationService, StockReservationService>();

        // Batch/Expiry Services (Epic 24)
        services.AddScoped<IProductBatchService, ProductBatchService>();
        services.AddScoped<IExpiryValidationService, ExpiryValidationService>();
        services.AddScoped<IBatchTraceabilityService, BatchTraceabilityService>();
        services.AddScoped<IWasteReportService, WasteReportService>();
        services.AddScoped<IWasteService, WasteService>();

        // Sync Services (Epic 25)
        services.AddScoped<ILocalDatabaseService, LocalDatabaseService>();
        services.AddScoped<ISyncQueueService, SyncQueueService>();
        services.AddScoped<ISyncHubService, SyncHubService>();
        services.AddScoped<ISyncHubServiceFactory, SyncHubServiceFactory>();
        services.AddScoped<IConflictResolutionService, ConflictResolutionService>();
        services.AddScoped<ISyncStatusService, SyncStatusService>();

        // Product Variant & Modifier Services
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IModifierService, ModifierService>();

        // Recipe & KDS Services (Epic 26-27)
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IRecipeCostService, RecipeCostService>();
        services.AddScoped<IIngredientDeductionService, IngredientDeductionService>();
        services.AddScoped<IBatchPrepService, BatchPrepService>();
        services.AddScoped<IKdsStationService, KdsStationService>();
        services.AddScoped<IKdsOrderService, KdsOrderService>();
        services.AddScoped<IKdsStatusService, KdsStatusService>();
        services.AddScoped<IKdsTimerService, KdsTimerService>();
        services.AddScoped<IExpoService, ExpoService>();
        services.AddScoped<IKdsCoursingService, KdsCoursingService>();

        // KDS SignalR Hub Service (Epic 26-27)
        services.Configure<KdsHubConfiguration>(options => { });
        services.AddScoped<IKdsHubService, KdsHubService>();
        services.AddScoped<IKdsHubServiceFactory, KdsHubServiceFactory>();

        // KDS Prep Timing Services (Issue #103)
        services.AddScoped<IKdsPrepTimingService, KdsPrepTimingService>();

        // Prep Timing Background Job (runs every 15 seconds)
        services.AddSingleton<PrepTimingJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<PrepTimingJob>());
        services.AddSingleton<IPrepTimingJob>(sp => sp.GetRequiredService<PrepTimingJob>());

        // Label Services (Epic 28)
        services.AddScoped<ILabelPrinterService, LabelPrinterService>();
        services.AddScoped<ILabelTemplateService, LabelTemplateService>();
        services.AddScoped<ILabelPrintService, LabelPrintService>();

        // Kenya Integration Services (Epic 39)
        services.AddScoped<IEtimsService, EtimsService>();
        services.AddScoped<IEtimsQrCodeService, EtimsQrCodeService>();
        services.AddScoped<IMpesaService, MpesaService>();

        // Connectivity Service (Epic 39)
        services.AddHttpClient("Connectivity", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.Configure<ConnectivityServiceOptions>(options => { });
        services.AddScoped<IConnectivityService, ConnectivityService>();

        // Multi-Store HQ Services (Epic 22)
        services.AddScoped<IMultiStoreService, MultiStoreService>();
        services.AddScoped<IChainReportingService, ChainReportingService>();
        services.AddScoped<ICentralPromotionService, CentralPromotionService>();
        services.AddScoped<IStoreSyncService, StoreSyncService>();

        // Dashboard Service (Epic 40)
        services.AddScoped<IDashboardService, DashboardService>();

        // Receipt Service
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IReceiptVoidService, ReceiptVoidService>();

        // Export Service
        services.AddScoped<IExportService, ExportService>();

        // Printer Service
        services.AddScoped<IPrinterService, PrinterService>();

        // System Configuration (Singleton to maintain cache across requests)
        services.AddSingleton<ISystemConfigurationService, SystemConfigurationService>();

        // Floor Service
        services.AddScoped<IFloorService, FloorService>();

        // HR Services (Kenya Compliant)
        services.AddScoped<IStatutoryConfigurationService, StatutoryConfigurationService>();
        services.AddScoped<IEmployeePhotoService, EmployeePhotoService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IDisciplinaryDeductionService, DisciplinaryDeductionService>();
        services.AddScoped<ITerminationService, TerminationService>();

        // Advanced Analytics & AI Services
        services.AddScoped<IPrimeCostReportService, PrimeCostReportService>();
        services.AddScoped<IMenuEngineeringService, MenuEngineeringService>();
        services.AddScoped<ICustomerAnalyticsService, CustomerAnalyticsService>();
        services.AddScoped<IAIInsightsService, AIInsightsService>();

        // Expense Management Services
        services.AddScoped<IExpenseService, ExpenseService>();

        // Notification Services
        services.AddScoped<INotificationService, NotificationService>();

        // Email Digest Services
        services.AddScoped<IEmailDigestService, EmailDigestService>();

        // Dynamic Pricing Services (Issue #105)
        services.AddScoped<IDynamicPricingService, DynamicPricingService>();

        // Dynamic Pricing Background Jobs
        services.AddSingleton<DynamicPricingJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DynamicPricingJob>());
        services.AddSingleton<IDynamicPricingJob>(sp => sp.GetRequiredService<DynamicPricingJob>());

        services.AddSingleton<Jobs.ExpiryPricingJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<Jobs.ExpiryPricingJob>());
        services.AddSingleton<IExpiryPricingJob>(sp => sp.GetRequiredService<Jobs.ExpiryPricingJob>());

        // Labor Forecasting Services (Issue #107)
        services.AddScoped<ILaborForecastingService, LaborForecastingService>();

        // Labor Forecasting Background Jobs
        services.AddSingleton<LaborForecastingJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LaborForecastingJob>());
        services.AddSingleton<ILaborForecastingJob>(sp => sp.GetRequiredService<LaborForecastingJob>());

        services.AddSingleton<LaborForecastCleanupJob>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LaborForecastCleanupJob>());

        // Terminal Management Services (Multi-Terminal Support)
        services.AddScoped<ITerminalService, TerminalService>();
        services.AddSingleton<IMachineIdentifierService, MachineIdentifierService>();
        services.AddSingleton<ITerminalConfigurationService, TerminalConfigurationService>();
        services.AddSingleton<ITerminalSessionContext, TerminalSessionContext>();
        services.AddScoped<ITerminalRegistrationService, TerminalRegistrationService>();
        services.AddScoped<IWorkPeriodSessionService, WorkPeriodSessionService>();

        // Report Services
        services.AddScoped<IXReportService, XReportService>();
        services.AddScoped<ICombinedReportService, CombinedReportService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with additional DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        // Database Context
        services.AddDbContext<POSDbContext>(configureOptions);

        // DbContext factory shares same configuration as AddDbContext
        services.AddDbContextFactory<POSDbContext>(configureOptions, lifetime: ServiceLifetime.Scoped);

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }

    /// <summary>
    /// Ensures the database is created and applies pending migrations.
    /// Call this during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="seed">Whether to seed default data.</param>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider,
        bool seed = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Seed default data if requested
        if (seed)
        {
            await DatabaseSeeder.SeedAsync(context);
        }
    }

    /// <summary>
    /// Ensures the database is created (without migrations) for testing scenarios.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        await context.Database.EnsureCreatedAsync();
    }
}
