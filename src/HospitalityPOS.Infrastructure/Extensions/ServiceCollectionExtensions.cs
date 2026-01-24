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
        // services.AddScoped<IStockTakeService, StockTakeService>(); // Excluded from compilation
        // services.AddScoped<IReportService, ReportService>(); // Excluded from compilation
        services.AddScoped<IGoodsReceivingService, GoodsReceivingService>();
        services.AddScoped<ISupplierService, SupplierService>();
        // services.AddScoped<IPurchaseOrderService, PurchaseOrderService>(); // Excluded from compilation

        // Loyalty Services
        services.AddScoped<ILoyaltyMemberRepository, LoyaltyMemberRepository>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
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
        // services.AddScoped<IMobileMoneyService, MobileMoneyService>(); // Excluded from compilation

        // Enhanced Financial Reporting
        // services.AddScoped<IFinancialReportingService, FinancialReportingService>(); // Excluded from compilation

        // Budget & Cost Management
        // services.AddScoped<IBudgetService, BudgetService>(); // Excluded from compilation

        // Checkout Enhancements
        // services.AddScoped<ICheckoutEnhancementService, CheckoutEnhancementService>(); // Excluded from compilation

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

        // Stock Transfer Services (Epic 23)
        // services.AddScoped<IStockTransferService, StockTransferService>(); // Excluded from compilation
        // services.AddScoped<IStockReservationService, StockReservationService>(); // Excluded from compilation

        // Batch/Expiry Services (Epic 24)
        services.AddScoped<IProductBatchService, ProductBatchService>();
        // services.AddScoped<IExpiryValidationService, ExpiryValidationService>(); // Excluded from compilation
        // services.AddScoped<IBatchTraceabilityService, BatchTraceabilityService>(); // Excluded from compilation
        // services.AddScoped<IWasteReportService, WasteReportService>(); // Excluded from compilation

        // Sync Services (Epic 25)
        services.AddScoped<ILocalDatabaseService, LocalDatabaseService>();
        services.AddScoped<ISyncQueueService, SyncQueueService>();
        services.AddScoped<ISyncHubService, SyncHubService>();
        services.AddScoped<ISyncHubServiceFactory, SyncHubServiceFactory>();
        services.AddScoped<IConflictResolutionService, ConflictResolutionService>();
        // services.AddScoped<ISyncStatusService, SyncStatusService>(); // Excluded from compilation

        // Product Variant & Modifier Services
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IModifierService, ModifierService>();

        // Recipe & KDS Services (Epic 26-27)
        services.AddScoped<IRecipeService, RecipeService>();
        // services.AddScoped<IRecipeCostService, RecipeCostService>(); // Excluded from compilation
        // services.AddScoped<IIngredientDeductionService, IngredientDeductionService>(); // Excluded from compilation
        // services.AddScoped<IBatchPrepService, BatchPrepService>(); // Excluded from compilation
        services.AddScoped<IKdsStationService, KdsStationService>();
        // services.AddScoped<IKdsOrderService, KdsOrderService>(); // Excluded from compilation
        // services.AddScoped<IKdsStatusService, KdsStatusService>(); // Excluded from compilation
        services.AddScoped<IKdsTimerService, KdsTimerService>();
        services.AddScoped<IExpoService, ExpoService>();

        // KDS SignalR Hub Service (Epic 26-27)
        services.Configure<KdsHubConfiguration>(options => { });
        services.AddScoped<IKdsHubService, KdsHubService>();
        services.AddScoped<IKdsHubServiceFactory, KdsHubServiceFactory>();

        // Label Services (Epic 28)
        // services.AddScoped<ILabelPrinterService, LabelPrinterService>(); // Excluded from compilation
        services.AddScoped<ILabelTemplateService, LabelTemplateService>();
        // services.AddScoped<ILabelPrintService, LabelPrintService>(); // Excluded from compilation

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
        // services.AddScoped<IMultiStoreService, MultiStoreService>(); // Excluded from compilation
        // services.AddScoped<IChainReportingService, ChainReportingService>(); // Excluded from compilation
        // services.AddScoped<ICentralPromotionService, CentralPromotionService>(); // Excluded from compilation
        services.AddScoped<IStoreSyncService, StoreSyncService>();

        // Dashboard Service (Epic 40)
        // services.AddScoped<IDashboardService, DashboardService>(); // Excluded from compilation

        // Receipt Service
        services.AddScoped<IReceiptService, ReceiptService>();
        // services.AddScoped<IReceiptVoidService, ReceiptVoidService>(); // Excluded from compilation

        // Export Service
        // services.AddScoped<IExportService, ExportService>(); // Excluded from compilation

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
