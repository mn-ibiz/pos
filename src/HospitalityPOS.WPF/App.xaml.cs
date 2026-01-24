using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Extensions;
using HospitalityPOS.Infrastructure.Services;
using HospitalityPOS.Infrastructure.Printing;
using HospitalityPOS.Business.Extensions;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels;
using HospitalityPOS.WPF.Views;
using HospitalityPOS.Infrastructure.BackgroundJobs;

namespace HospitalityPOS.WPF;

/// <summary>
/// Application entry point and dependency injection configuration.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public static IConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        // Build configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "logs/pos-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        Services = _host.Services;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Get connection string from configuration
        var connectionString = Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

        // Register Infrastructure services (DbContext, UnitOfWork, Repositories)
        services.AddInfrastructureServices(connectionString);

        // Register Business services
        services.AddBusinessServices();

        // Register HttpClientFactory with named clients for external API integrations
        services.AddHttpClient("EtimsApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("MpesaApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Cloud Sync API client (Epic 25)
        services.AddHttpClient("CloudSync", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // SMS API client (Africa's Talking, etc.)
        services.AddHttpClient("SmsApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Configure Cloud API settings
        services.Configure<CloudApiSettings>(Configuration.GetSection(CloudApiSettings.SectionName));

        // Password service (Singleton - stateless)
        services.AddSingleton<IPasswordService, PasswordService>();

        // User and Session services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ILoginAuditService, LoginAuditService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAutoLogoutService, AutoLogoutService>();

        // Authorization service (Singleton - caches permissions per user)
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Permission override service (Singleton - uses IServiceScopeFactory internally)
        services.AddSingleton<IPermissionOverrideService, PermissionOverrideService>();

        // Work period service (Scoped - accesses DbContext directly)
        services.AddScoped<IWorkPeriodService, WorkPeriodService>();

        // Cash payout service (Scoped - accesses DbContext directly)
        services.AddScoped<ICashPayoutService, CashPayoutService>();

        // Category service (Scoped - accesses DbContext directly)
        services.AddScoped<ICategoryService, CategoryService>();

        // Product service (Scoped - accesses DbContext directly)
        services.AddScoped<IProductService, ProductService>();

        // Offer service (Scoped - accesses DbContext directly)
        services.AddScoped<IOfferService, OfferService>();

        // Supplier credit service (Scoped - accesses DbContext directly)
        services.AddScoped<ISupplierCreditService, SupplierCreditService>();

        // Employee service (Scoped - accesses DbContext directly)
        services.AddScoped<IEmployeeService, EmployeeService>();

        // Attendance service (Scoped - accesses DbContext directly)
        services.AddScoped<IAttendanceService, AttendanceService>();

        // Payroll service (Scoped - accesses DbContext directly)
        services.AddScoped<IPayrollService, PayrollService>();

        // Accounting service (Scoped - accesses DbContext directly)
        services.AddScoped<IAccountingService, AccountingService>();

        // eTIMS service (Scoped - accesses DbContext directly)
        services.AddScoped<IEtimsService, EtimsService>();

        // eTIMS QR Code service (Scoped - generates QR codes for receipts)
        services.AddScoped<IEtimsQrCodeService, EtimsQrCodeService>();

        // M-Pesa service (Scoped - accesses DbContext directly)
        services.AddScoped<IMpesaService, MpesaService>();

        // Barcode service (Scoped - accesses DbContext directly)
        services.AddScoped<IBarcodeService, BarcodeService>();

        // Label preview service (Scoped - renders visual label previews)
        services.AddScoped<ILabelPreviewService, LabelPreviewService>();

        // Labelary API client for ZPL preview
        services.AddHttpClient("LabelaryApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Order service (Scoped - accesses DbContext directly)
        services.AddScoped<IOrderService, OrderService>();

        // Purchase order service (Scoped - accesses DbContext directly)
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

        // Purchase order settings service (Scoped - accesses DbContext directly)
        services.AddScoped<IPurchaseOrderSettingsService, PurchaseOrderSettingsService>();

        // Purchase order consolidation service (Scoped - accesses DbContext directly)
        services.AddScoped<IPurchaseOrderConsolidationService, PurchaseOrderConsolidationService>();

        // Notification service (Scoped - accesses DbContext directly)
        services.AddScoped<INotificationService, NotificationService>();

        // Stock monitoring service (Scoped - accesses DbContext directly)
        services.AddScoped<IStockMonitoringService, StockMonitoringService>();

        // Email digest service (Scoped - accesses DbContext directly)
        services.AddScoped<IEmailDigestService, EmailDigestService>();

        // Receipt service (Scoped - accesses DbContext directly)
        services.AddScoped<IReceiptService, ReceiptService>();

        // Receipt split service (Scoped - accesses DbContext directly)
        // services.AddScoped<IReceiptSplitService, ReceiptSplitService>(); // Excluded from compilation

        // Receipt merge service (Scoped - accesses DbContext directly)
        // services.AddScoped<IReceiptMergeService, ReceiptMergeService>(); // Excluded from compilation

        // Receipt void service (Scoped - accesses DbContext directly)
        // services.AddScoped<IReceiptVoidService, ReceiptVoidService>(); // Excluded from compilation

        // Payment method service (Scoped - accesses DbContext directly)
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();

        // Floor service (Scoped - accesses DbContext directly)
        services.AddScoped<IFloorService, FloorService>();

        // Table transfer service (Scoped - accesses DbContext directly)
        services.AddScoped<ITableTransferService, TableTransferService>();

        // Ownership service (Singleton - uses IServiceScopeFactory internally)
        services.AddSingleton<IOwnershipService, OwnershipService>();

        // Kitchen print service (Singleton - stateless printer operations)
        services.AddSingleton<IKitchenPrintService, KitchenPrintService>();

        // Report print service (Singleton - stateless printer operations)
        services.AddSingleton<IReportPrintService, ReportPrintService>();

        // Cash drawer service (Singleton - stateless drawer operations)
        services.AddSingleton<ICashDrawerService, CashDrawerService>();

        // System configuration service (Singleton - manages business mode and feature flags)
        services.AddSingleton<ISystemConfigurationService, SystemConfigurationService>();

        // UI shell service (Singleton - manages layout mode based on business mode)
        services.AddSingleton<IUiShellService, UiShellService>();

        // Image service (Singleton - stateless file operations)
        services.AddSingleton<IImageService, ImageService>();

        // Export service (Singleton - stateless export operations)
        services.AddSingleton<IExportService, ExportService>();

        // Dashboard service (Scoped - accesses DbContext for real-time data)
        services.AddScoped<IDashboardService, DashboardService>();

        // System health service (Scoped - checks database and system resources)
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // Background Jobs (Hosted Services)
        services.AddHostedService<ExpireBatchesJob>();    // Daily at midnight
        services.AddHostedService<ExpirePointsJob>();     // Monthly on 1st
        services.AddHostedService<ExpireReservationsJob>(); // Hourly

        // Configure points expiry options
        services.Configure<PointsExpiryOptions>(Configuration.GetSection(PointsExpiryOptions.SectionName));

        // Printer discovery service (Singleton - stateless printer detection)
        // services.AddSingleton<IPrinterDiscoveryService, PrinterDiscoveryService>(); // Excluded from compilation

        // Printer service (Scoped - accesses DbContext directly)
        services.AddScoped<IPrinterService, PrinterService>();

        // Kitchen order routing service (Scoped - accesses DbContext for order routing)
        // services.AddScoped<IKitchenOrderRoutingService, KitchenOrderRoutingService>(); // Excluded from compilation

        // Printer communication service (Singleton - Windows API for raw printing)
        services.AddSingleton<IPrinterCommunicationService, PrinterCommunicationService>();

        // Print queue manager (Singleton - manages print job queue with retry)
        services.AddSingleton<IPrintQueueManager, PrintQueueManager>();

        // Image converter (Singleton - converts images to ESC/POS raster format)
        // services.AddSingleton<IImageConverter, ImageConverter>(); // Excluded from compilation (ambiguous reference)

        // Navigation and Dialog services (Singleton - shared across app)
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // Theme service (Singleton - manages application theme)
        services.AddSingleton<IThemeService, ThemeService>();

        // Register ViewModels (Transient - new instance each navigation)
        // NOTE: Many ViewModels temporarily excluded due to API mismatches
        // services.AddTransient<MainViewModel>(); // Excluded - using MainWindowViewModel instead
        services.AddSingleton<MainWindowViewModel>(); // Singleton - one instance for the main window
        services.AddTransient<ModeSelectionViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AutoLogoutSettingsViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<RoleManagementViewModel>();
        services.AddTransient<RoleEditorViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserEditorViewModel>();
        services.AddTransient<CategoryManagementViewModel>();
        services.AddTransient<ProductManagementViewModel>();
        services.AddTransient<VariantOptionsViewModel>();
        services.AddTransient<ModifierGroupsViewModel>();
        services.AddTransient<POSViewModel>(); // Now enabled
        services.AddTransient<SettlementViewModel>(); // Now enabled
        services.AddTransient<PaymentMethodsViewModel>();
        services.AddTransient<InventoryViewModel>();
        // services.AddTransient<StockAlertWidgetViewModel>(); // Excluded
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<PurchaseOrdersViewModel>();
        services.AddTransient<PurchaseOrderReviewViewModel>();
        services.AddTransient<ReorderRulesViewModel>();
        // services.AddTransient<SalesReportsViewModel>(); // Disabled - IReportService not available
        services.AddTransient<ExceptionReportsViewModel>();
        services.AddTransient<InventoryReportsViewModel>();
        services.AddTransient<AuditReportsViewModel>();
        services.AddTransient<ZReportHistoryViewModel>();
        services.AddTransient<LoginAuditViewModel>();
        services.AddTransient<GoodsReceivingViewModel>();
        services.AddTransient<DirectReceivingViewModel>();
        services.AddTransient<ExportDialogViewModel>();
        // Factory for creating ExportDialogViewModel (used by SalesReportsViewModel)
        services.AddSingleton<Func<ExportDialogViewModel>>(sp => () => sp.GetRequiredService<ExportDialogViewModel>());
        services.AddTransient<FloorManagementViewModel>();
        services.AddTransient<FloorDialogViewModel>();
        services.AddTransient<TableDialogViewModel>();
        services.AddTransient<SectionDialogViewModel>();
        // services.AddTransient<TableMapViewModel>(); // Excluded
        services.AddTransient<PrinterSettingsViewModel>();
        services.AddTransient<KitchenPrinterSettingsViewModel>();
        services.AddTransient<CashDrawerSettingsViewModel>();
        services.AddTransient<SetupWizardViewModel>();
        services.AddTransient<OrganizationSettingsViewModel>();
        services.AddSingleton<CashierShellViewModel>(); // Singleton - one instance for cashier shell
        services.AddTransient<FeatureSettingsViewModel>();
        services.AddTransient<OffersViewModel>();
        services.AddTransient<OfferEditorViewModel>();
        // services.AddTransient<OfferReportViewModel>(); // Excluded
        services.AddTransient<SupplierInvoicesViewModel>();
        // services.AddTransient<SupplierStatementViewModel>(); // Excluded
        // HR ViewModels
        services.AddTransient<EmployeesViewModel>();
        services.AddTransient<EmployeeEditorViewModel>();
        services.AddTransient<AttendanceViewModel>();
        services.AddTransient<PayrollViewModel>();
        services.AddTransient<PayslipHistoryViewModel>();
        services.AddTransient<ChartOfAccountsViewModel>();
        // services.AddTransient<JournalEntriesViewModel>(); // Excluded - API mismatches with JournalEntry entity
        services.AddTransient<FinancialReportsViewModel>();
        services.AddTransient<EtimsDashboardViewModel>();
        services.AddTransient<EtimsDeviceSettingsViewModel>();
        services.AddTransient<MpesaDashboardViewModel>();
        services.AddTransient<MpesaSettingsViewModel>();
        services.AddTransient<QrPaymentDialogViewModel>();
        services.AddTransient<SmsSettingsViewModel>();
        // services.AddTransient<PLUManagementViewModel>(); // Excluded
        services.AddTransient<BarcodeSettingsViewModel>();
        // services.AddTransient<CustomerEnrollmentViewModel>(); // EXCLUDED - API mismatches
        services.AddTransient<DashboardViewModel>();

        // Expense Management ViewModels
        services.AddTransient<ExpenseDashboardViewModel>();
        services.AddTransient<ExpenseListViewModel>();
        services.AddTransient<ExpenseCategoryManagementViewModel>();
        services.AddTransient<ExpenseReportsViewModel>();
        services.AddTransient<RecurringExpenseListViewModel>();
        services.AddTransient<ExpenseBudgetViewModel>();

        // Analytics ViewModels
        services.AddTransient<AIInsightsDashboardViewModel>();
        services.AddTransient<PrimeCostReportViewModel>();
        services.AddTransient<MenuEngineeringViewModel>();
        services.AddTransient<CustomerAnalyticsViewModel>();

        // Marketing ViewModels
        services.AddTransient<Marketing.SmsCampaignDashboardViewModel>();
        services.AddTransient<Marketing.SmsTemplateListViewModel>();
        services.AddTransient<Marketing.CustomerSegmentListViewModel>();

        // Label Printing ViewModels
        services.AddTransient<LabelPrinterConfigurationViewModel>();
        services.AddTransient<LabelTemplateManagementViewModel>();
        services.AddTransient<LabelSizeConfigurationViewModel>();
        services.AddTransient<LabelTemplateDesignerViewModel>();

        // Stock Transfer ViewModels (Epic 23) - NOW ENABLED after API fixes
        services.AddTransient<StockTransferViewModel>();
        services.AddTransient<CreateTransferRequestViewModel>();
        services.AddTransient<TransferDetailsViewModel>();
        services.AddTransient<TransferApprovalViewModel>();
        services.AddTransient<TransferReceiveViewModel>();

        // HR Extended ViewModels (Leave, Loan, Commission, Termination, Disciplinary)
        services.AddTransient<LeaveManagementViewModel>();
        services.AddTransient<LoanManagementViewModel>();
        services.AddTransient<CommissionManagementViewModel>();
        services.AddTransient<EmployeeTerminationViewModel>();
        services.AddTransient<DisciplinaryDeductionsViewModel>();

        // Finance Extended ViewModels (Budget, Bank Reconciliation, AR, AP)
        services.AddTransient<BudgetManagementViewModel>();
        services.AddTransient<BankReconciliationViewModel>();
        services.AddTransient<AccountsReceivableViewModel>();
        services.AddTransient<SupplierCreditViewModel>();

        // Operations ViewModels (Time Pricing, Waste, Chain Reporting, Hotel PMS)
        services.AddTransient<TimePricingViewModel>();
        services.AddTransient<WasteReportingViewModel>();
        services.AddTransient<ChainReportingViewModel>();
        services.AddTransient<HotelPMSIntegrationViewModel>();

        // System ViewModels (Conflict Resolution, Permission Override)
        services.AddTransient<ConflictResolutionViewModel>();
        services.AddTransient<PermissionOverrideViewModel>();

        // Batch/Expiry ViewModels (Epic 24) - Excluded
        // services.AddTransient<BatchManagementViewModel>();
        // services.AddTransient<ExpiryAlertsViewModel>();

        // Loyalty ViewModels (Epic 39) - EXCLUDED due to API mismatches
        // services.AddTransient<CustomerListViewModel>();
        // services.AddTransient<LoyaltySettingsViewModel>();

        // Email ViewModels (Epic 40) - Excluded due to API mismatches
        // services.AddTransient<EmailSettingsViewModel>();

        // Email Services (Epic 40)
        services.AddScoped<IEmailService, EmailService>();
        // EmailReportService excluded due to API mismatches - use EmailService directly for sending
        // services.AddScoped<IEmailReportService, EmailReportService>();
        // services.AddScoped<EmailTriggerService>(); // Enable when trigger service is needed
        // services.AddHostedService<EmailSchedulerService>(); // Enable for background scheduled emails

        // Future ViewModels:
        // services.AddTransient<SettingsViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();

        // Serilog logger
        services.AddSingleton(Log.Logger);

        // Configuration
        services.AddSingleton(Configuration);
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            await _host.StartAsync();

            Log.Information("Application starting up");

            // Initialize database - apply migrations and seed data
            Log.Information("Initializing database...");
            await Services.InitializeDatabaseAsync(seed: true);
            Log.Information("Database initialized successfully");

            // Initialize theme service and load saved preference
            var themeService = Services.GetRequiredService<IThemeService>();
            await themeService.LoadSavedThemeAsync();
            Log.Information("Theme initialized: {Theme}", themeService.CurrentTheme);

            // Show the main window
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Check if initial setup is complete
            var configService = Services.GetRequiredService<ISystemConfigurationService>();
            var setupComplete = await configService.IsSetupCompleteAsync();

            // Refresh UI shell service to ensure correct business mode is loaded
            var uiShellService = Services.GetRequiredService<IUiShellService>();
            await uiShellService.RefreshAsync();
            Log.Information("UI Shell refreshed with mode: {Mode}", uiShellService.CurrentMode);

            var navigationService = Services.GetRequiredService<INavigationService>();

            if (!setupComplete)
            {
                // First run - show setup wizard
                Log.Information("First run detected - showing setup wizard");
                navigationService.NavigateTo<SetupWizardViewModel>();
            }
            else
            {
                // Setup complete - navigate to mode selection screen
                navigationService.NavigateTo<ModeSelectionViewModel>();
                Log.Information("Navigation to Mode Selection screen completed");
            }

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed during application startup");
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\nPlease ensure SQL Server Express is running.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutting down");

            await _host.StopAsync();
            _host.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application shutdown");
        }
        finally
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
