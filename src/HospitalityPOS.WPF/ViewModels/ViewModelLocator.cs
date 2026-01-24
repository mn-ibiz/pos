using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Locator class that provides access to ViewModels from XAML.
/// </summary>
public class ViewModelLocator
{
    /// <summary>
    /// Gets the MainViewModel instance.
    /// </summary>
    public MainViewModel Main => App.Services.GetRequiredService<MainViewModel>();

    /// <summary>
    /// Gets the LoginViewModel instance.
    /// </summary>
    public LoginViewModel Login => App.Services.GetRequiredService<LoginViewModel>();

    /// <summary>
    /// Gets the AutoLogoutSettingsViewModel instance.
    /// </summary>
    public AutoLogoutSettingsViewModel AutoLogoutSettings => App.Services.GetRequiredService<AutoLogoutSettingsViewModel>();

    /// <summary>
    /// Gets the SalesReportsViewModel instance.
    /// </summary>
    public SalesReportsViewModel SalesReports => App.Services.GetRequiredService<SalesReportsViewModel>();

    /// <summary>
    /// Gets the ExceptionReportsViewModel instance.
    /// </summary>
    public ExceptionReportsViewModel ExceptionReports => App.Services.GetRequiredService<ExceptionReportsViewModel>();

    /// <summary>
    /// Gets the InventoryReportsViewModel instance.
    /// </summary>
    public InventoryReportsViewModel InventoryReports => App.Services.GetRequiredService<InventoryReportsViewModel>();

    /// <summary>
    /// Gets the AuditReportsViewModel instance.
    /// </summary>
    public AuditReportsViewModel AuditReports => App.Services.GetRequiredService<AuditReportsViewModel>();

    /// <summary>
    /// Gets the EmailSettingsViewModel instance.
    /// </summary>
    public EmailSettingsViewModel EmailSettings => App.Services.GetRequiredService<EmailSettingsViewModel>();

    /// <summary>
    /// Gets the ExpenseDashboardViewModel instance.
    /// </summary>
    public ExpenseDashboardViewModel ExpenseDashboard => App.Services.GetRequiredService<ExpenseDashboardViewModel>();

    /// <summary>
    /// Gets the ExpenseListViewModel instance.
    /// </summary>
    public ExpenseListViewModel ExpenseList => App.Services.GetRequiredService<ExpenseListViewModel>();

    /// <summary>
    /// Gets the ExpenseCategoryManagementViewModel instance.
    /// </summary>
    public ExpenseCategoryManagementViewModel ExpenseCategoryManagement => App.Services.GetRequiredService<ExpenseCategoryManagementViewModel>();

    // HR Extended ViewModels
    public LeaveManagementViewModel LeaveManagement => App.Services.GetRequiredService<LeaveManagementViewModel>();
    public LoanManagementViewModel LoanManagement => App.Services.GetRequiredService<LoanManagementViewModel>();
    public CommissionManagementViewModel CommissionManagement => App.Services.GetRequiredService<CommissionManagementViewModel>();
    public EmployeeTerminationViewModel EmployeeTermination => App.Services.GetRequiredService<EmployeeTerminationViewModel>();
    public DisciplinaryDeductionsViewModel DisciplinaryDeductions => App.Services.GetRequiredService<DisciplinaryDeductionsViewModel>();

    // Finance Extended ViewModels
    public BudgetManagementViewModel BudgetManagement => App.Services.GetRequiredService<BudgetManagementViewModel>();
    public BankReconciliationViewModel BankReconciliation => App.Services.GetRequiredService<BankReconciliationViewModel>();
    public AccountsReceivableViewModel AccountsReceivable => App.Services.GetRequiredService<AccountsReceivableViewModel>();
    public SupplierCreditViewModel SupplierCredit => App.Services.GetRequiredService<SupplierCreditViewModel>();

    // Operations ViewModels
    public TimePricingViewModel TimePricing => App.Services.GetRequiredService<TimePricingViewModel>();
    public WasteReportingViewModel WasteReporting => App.Services.GetRequiredService<WasteReportingViewModel>();
    public ChainReportingViewModel ChainReporting => App.Services.GetRequiredService<ChainReportingViewModel>();
    public HotelPMSIntegrationViewModel HotelPMSIntegration => App.Services.GetRequiredService<HotelPMSIntegrationViewModel>();

    // System ViewModels
    public ConflictResolutionViewModel ConflictResolution => App.Services.GetRequiredService<ConflictResolutionViewModel>();
    public PermissionOverrideViewModel PermissionOverride => App.Services.GetRequiredService<PermissionOverrideViewModel>();

    // Add other ViewModels as they are created:
    // public POSViewModel POS => App.Services.GetRequiredService<POSViewModel>();
}
