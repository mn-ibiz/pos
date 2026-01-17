using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing feature toggles.
/// </summary>
public partial class FeatureSettingsViewModel : ObservableObject
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<FeatureCategory> _featureCategories = new();

    [ObservableProperty]
    private BusinessMode _currentMode;

    [ObservableProperty]
    private string _currentModeDisplay = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public FeatureSettingsViewModel(
        ISystemConfigurationService configurationService,
        IDialogService dialogService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Initializes the ViewModel by loading feature settings.
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading feature settings...";

        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config != null)
            {
                CurrentMode = config.Mode;
                CurrentModeDisplay = config.GetModeDisplayName();
                LoadFeatureCategories(config);
            }

            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
            await _dialogService.ShowErrorAsync("Load Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadFeatureCategories(SystemConfiguration config)
    {
        FeatureCategories = new ObservableCollection<FeatureCategory>
        {
            new FeatureCategory("Restaurant / Hospitality", "#F59E0B", new[]
            {
                new FeatureToggle(
                    FeatureFlags.TableManagement,
                    "Table Management",
                    "Enable floor plan and table status tracking",
                    config.EnableTableManagement,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.KitchenDisplay,
                    "Kitchen Display",
                    "Enable kitchen order tickets and display system",
                    config.EnableKitchenDisplay,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.WaiterAssignment,
                    "Waiter Assignment",
                    "Enable table assignment to waiters",
                    config.EnableWaiterAssignment,
                    OnFeatureToggled,
                    dependsOn: FeatureFlags.TableManagement),

                new FeatureToggle(
                    FeatureFlags.CourseSequencing,
                    "Course Sequencing",
                    "Enable multi-course meal ordering",
                    config.EnableCourseSequencing,
                    OnFeatureToggled,
                    dependsOn: FeatureFlags.KitchenDisplay),

                new FeatureToggle(
                    FeatureFlags.Reservations,
                    "Reservations",
                    "Enable table reservation system",
                    config.EnableReservations,
                    OnFeatureToggled,
                    dependsOn: FeatureFlags.TableManagement)
            }),

            new FeatureCategory("Retail / Supermarket", "#10B981", new[]
            {
                new FeatureToggle(
                    FeatureFlags.BarcodeAutoFocus,
                    "Barcode Auto-Focus",
                    "Enable automatic barcode scanner focus on POS screen",
                    config.EnableBarcodeAutoFocus,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.ProductOffers,
                    "Product Offers",
                    "Enable promotions and discount offers",
                    config.EnableProductOffers,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.SupplierCredit,
                    "Supplier Credit",
                    "Enable supplier credit tracking and payments",
                    config.EnableSupplierCredit,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.LoyaltyProgram,
                    "Loyalty Program",
                    "Enable customer loyalty points and rewards",
                    config.EnableLoyaltyProgram,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.BatchExpiry,
                    "Batch & Expiry",
                    "Enable batch tracking and expiry date management",
                    config.EnableBatchExpiry,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.ScaleIntegration,
                    "Scale Integration",
                    "Enable weighing scale integration for produce",
                    config.EnableScaleIntegration,
                    OnFeatureToggled)
            }),

            new FeatureCategory("Enterprise", "#6366F1", new[]
            {
                new FeatureToggle(
                    FeatureFlags.Payroll,
                    "Payroll",
                    "Enable employee payroll management",
                    config.EnablePayroll,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.Accounting,
                    "Accounting",
                    "Enable chart of accounts and journal entries",
                    config.EnableAccounting,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.MultiStore,
                    "Multi-Store",
                    "Enable multi-store management and synchronization",
                    config.EnableMultiStore,
                    OnFeatureToggled),

                new FeatureToggle(
                    FeatureFlags.CloudSync,
                    "Cloud Sync",
                    "Enable cloud synchronization for data backup",
                    config.EnableCloudSync,
                    OnFeatureToggled,
                    dependsOn: FeatureFlags.MultiStore)
            }),

            new FeatureCategory("Kenya Compliance", "#EF4444", new[]
            {
                new FeatureToggle(
                    FeatureFlags.KenyaETims,
                    "Kenya eTIMS",
                    "Enable KRA eTIMS tax compliance integration (required in Kenya)",
                    config.EnableKenyaETims,
                    OnFeatureToggled,
                    isRequired: true),

                new FeatureToggle(
                    FeatureFlags.Mpesa,
                    "M-Pesa Integration",
                    "Enable M-Pesa Daraja API payment integration",
                    config.EnableMpesa,
                    OnFeatureToggled)
            })
        };
    }

    private void OnFeatureToggled(FeatureToggle toggle)
    {
        HasUnsavedChanges = true;

        // Check dependencies
        if (!toggle.IsEnabled && !string.IsNullOrEmpty(toggle.DependsOn))
        {
            // Feature was disabled, no dependency issue
        }
        else if (toggle.IsEnabled && !string.IsNullOrEmpty(toggle.DependsOn))
        {
            // Feature enabled, check if dependency is enabled
            var dependency = FindFeature(toggle.DependsOn);
            if (dependency != null && !dependency.IsEnabled)
            {
                // Auto-enable dependency
                dependency.IsEnabled = true;
                StatusMessage = $"'{dependency.Name}' was automatically enabled (required by '{toggle.Name}')";
            }
        }

        // Check if any feature depends on this one
        if (!toggle.IsEnabled)
        {
            foreach (var category in FeatureCategories)
            {
                foreach (var feature in category.Features)
                {
                    if (feature.DependsOn == toggle.Key && feature.IsEnabled)
                    {
                        feature.IsEnabled = false;
                        StatusMessage = $"'{feature.Name}' was automatically disabled (depends on '{toggle.Name}')";
                    }
                }
            }
        }
    }

    private FeatureToggle? FindFeature(string key)
    {
        foreach (var category in FeatureCategories)
        {
            var feature = category.Features.FirstOrDefault(f => f.Key == key);
            if (feature != null)
            {
                return feature;
            }
        }
        return null;
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (!HasUnsavedChanges)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Saving feature settings...";

        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config != null)
            {
                // Apply all feature toggles
                foreach (var category in FeatureCategories)
                {
                    foreach (var feature in category.Features)
                    {
                        await _configurationService.SetFeatureEnabledAsync(feature.Key, feature.IsEnabled);
                    }
                }

                HasUnsavedChanges = false;
                StatusMessage = "Feature settings saved successfully.";

                await _dialogService.ShowMessageAsync(
                    "Settings Saved",
                    "Feature settings have been saved. Some changes may require an application restart to take effect.");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
            await _dialogService.ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetToModeDefaultsAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Reset to Defaults",
            $"Are you sure you want to reset all features to '{CurrentModeDisplay}' mode defaults?");

        if (!confirmed)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Resetting to defaults...";

        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config != null)
            {
                config.ApplyModeDefaults();
                await _configurationService.SaveConfigurationAsync(config);
                LoadFeatureCategories(config);
                HasUnsavedChanges = false;
                StatusMessage = "Features reset to mode defaults.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting settings: {ex.Message}";
            await _dialogService.ShowErrorAsync("Reset Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void DiscardChanges()
    {
        if (HasUnsavedChanges)
        {
            _ = InitializeAsync();
        }
    }
}

/// <summary>
/// Represents a category of features.
/// </summary>
public class FeatureCategory
{
    public string Name { get; }
    public string AccentColor { get; }
    public ObservableCollection<FeatureToggle> Features { get; }

    public FeatureCategory(string name, string accentColor, FeatureToggle[] features)
    {
        Name = name;
        AccentColor = accentColor;
        Features = new ObservableCollection<FeatureToggle>(features);
    }
}

/// <summary>
/// Represents a single feature toggle.
/// </summary>
public partial class FeatureToggle : ObservableObject
{
    private readonly Action<FeatureToggle>? _onToggled;

    public string Key { get; }
    public string Name { get; }
    public string Description { get; }
    public string? DependsOn { get; }
    public bool IsRequired { get; }

    [ObservableProperty]
    private bool _isEnabled;

    public FeatureToggle(
        string key,
        string name,
        string description,
        bool isEnabled,
        Action<FeatureToggle>? onToggled = null,
        string? dependsOn = null,
        bool isRequired = false)
    {
        Key = key;
        Name = name;
        Description = description;
        _isEnabled = isEnabled;
        _onToggled = onToggled;
        DependsOn = dependsOn;
        IsRequired = isRequired;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _onToggled?.Invoke(this);
    }
}
