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

    // Add other ViewModels as they are created:
    // public POSViewModel POS => App.Services.GetRequiredService<POSViewModel>();
}
