using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for system configuration and business mode management.
/// </summary>
public interface ISystemConfigurationService
{
    /// <summary>
    /// Gets the current system configuration.
    /// </summary>
    /// <returns>The system configuration or null if not set up.</returns>
    Task<SystemConfiguration?> GetConfigurationAsync();

    /// <summary>
    /// Gets the current business mode.
    /// </summary>
    /// <returns>The current business mode.</returns>
    Task<BusinessMode> GetCurrentModeAsync();

    /// <summary>
    /// Checks if the initial setup has been completed.
    /// </summary>
    /// <returns>True if setup is complete.</returns>
    Task<bool> IsSetupCompleteAsync();

    /// <summary>
    /// Saves the system configuration.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SaveConfigurationAsync(SystemConfiguration configuration);

    /// <summary>
    /// Completes the initial setup wizard.
    /// </summary>
    /// <param name="mode">The selected business mode.</param>
    /// <param name="businessName">The business name.</param>
    /// <param name="businessAddress">Optional business address.</param>
    /// <param name="businessPhone">Optional business phone.</param>
    /// <returns>The created configuration.</returns>
    Task<SystemConfiguration> CompleteSetupAsync(
        BusinessMode mode,
        string businessName,
        string? businessAddress = null,
        string? businessPhone = null);

    /// <summary>
    /// Changes the business mode and applies new defaults.
    /// </summary>
    /// <param name="newMode">The new business mode.</param>
    /// <param name="applyDefaults">Whether to apply default feature flags for the mode.</param>
    /// <returns>True if successful.</returns>
    Task<bool> ChangeModeAsync(BusinessMode newMode, bool applyDefaults = true);

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <returns>True if the feature is enabled.</returns>
    Task<bool> IsFeatureEnabledAsync(string featureName);

    /// <summary>
    /// Updates a specific feature flag.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="enabled">Whether to enable the feature.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled);

    /// <summary>
    /// Gets all feature flags as a dictionary.
    /// </summary>
    /// <returns>Dictionary of feature names and their enabled status.</returns>
    Task<Dictionary<string, bool>> GetAllFeaturesAsync();

    /// <summary>
    /// Gets the cached configuration (synchronous for UI binding).
    /// </summary>
    SystemConfiguration? CachedConfiguration { get; }

    /// <summary>
    /// Refreshes the cached configuration from the database.
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Event raised when configuration changes (requires restart for some changes).
    /// </summary>
    event EventHandler<SystemConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event arguments for configuration changes.
/// </summary>
public class SystemConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the previous mode.
    /// </summary>
    public BusinessMode? PreviousMode { get; set; }

    /// <summary>
    /// Gets or sets the new mode.
    /// </summary>
    public BusinessMode NewMode { get; set; }

    /// <summary>
    /// Gets or sets the changed feature names.
    /// </summary>
    public List<string> ChangedFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets whether a restart is required.
    /// </summary>
    public bool RequiresRestart { get; set; }
}

/// <summary>
/// Feature flag constants for type-safe feature checking.
/// </summary>
public static class FeatureFlags
{
    // Restaurant features
    public const string TableManagement = nameof(SystemConfiguration.EnableTableManagement);
    public const string KitchenDisplay = nameof(SystemConfiguration.EnableKitchenDisplay);
    public const string WaiterAssignment = nameof(SystemConfiguration.EnableWaiterAssignment);
    public const string CourseSequencing = nameof(SystemConfiguration.EnableCourseSequencing);
    public const string Reservations = nameof(SystemConfiguration.EnableReservations);

    // Retail features
    public const string BarcodeAutoFocus = nameof(SystemConfiguration.EnableBarcodeAutoFocus);
    public const string ProductOffers = nameof(SystemConfiguration.EnableProductOffers);
    public const string SupplierCredit = nameof(SystemConfiguration.EnableSupplierCredit);
    public const string LoyaltyProgram = nameof(SystemConfiguration.EnableLoyaltyProgram);
    public const string BatchExpiry = nameof(SystemConfiguration.EnableBatchExpiry);
    public const string ScaleIntegration = nameof(SystemConfiguration.EnableScaleIntegration);

    // Enterprise features
    public const string Payroll = nameof(SystemConfiguration.EnablePayroll);
    public const string Accounting = nameof(SystemConfiguration.EnableAccounting);
    public const string MultiStore = nameof(SystemConfiguration.EnableMultiStore);
    public const string CloudSync = nameof(SystemConfiguration.EnableCloudSync);

    // Kenya features
    public const string KenyaETims = nameof(SystemConfiguration.EnableKenyaETims);
    public const string Mpesa = nameof(SystemConfiguration.EnableMpesa);
}
