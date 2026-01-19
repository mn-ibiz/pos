using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing organization/business settings.
/// Allows editing business info that was captured during setup.
/// </summary>
public partial class OrganizationSettingsViewModel : ViewModelBase, INavigationAware
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    [ObservableProperty]
    private string _businessName = string.Empty;

    [ObservableProperty]
    private string? _businessAddress;

    [ObservableProperty]
    private string? _businessPhone;

    [ObservableProperty]
    private string? _businessEmail;

    [ObservableProperty]
    private string? _kraPinNumber;

    [ObservableProperty]
    private string? _vatNumber;

    [ObservableProperty]
    private decimal _vatRate = 16m;

    [ObservableProperty]
    private string _currencyCode = "KES";

    [ObservableProperty]
    private string _currencySymbol = "Ksh";

    [ObservableProperty]
    private BusinessMode _selectedMode = BusinessMode.Supermarket;

    [ObservableProperty]
    private string _modeDisplayName = string.Empty;

    [ObservableProperty]
    private string _modeDescription = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string? _lastSavedInfo;

    [ObservableProperty]
    private string? _logoPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    [NotifyPropertyChangedFor(nameof(HasNoLogo))]
    private BitmapImage? _logoPreview;

    /// <summary>
    /// Gets whether a logo is currently set.
    /// </summary>
    public bool HasLogo => LogoPreview != null;

    /// <summary>
    /// Gets whether no logo is set (for placeholder visibility).
    /// </summary>
    public bool HasNoLogo => LogoPreview == null;

    #endregion

    /// <summary>
    /// Available currency options.
    /// </summary>
    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new()
    {
        new CurrencyOption("KES", "Ksh", "Kenya Shilling"),
        new CurrencyOption("USD", "$", "US Dollar"),
        new CurrencyOption("EUR", "\u20ac", "Euro"),
        new CurrencyOption("GBP", "\u00a3", "British Pound"),
        new CurrencyOption("TZS", "TSh", "Tanzania Shilling"),
        new CurrencyOption("UGX", "USh", "Uganda Shilling")
    };

    /// <summary>
    /// Available business modes.
    /// </summary>
    public ObservableCollection<BusinessModeOption> ModeOptions { get; } = new()
    {
        new BusinessModeOption(
            BusinessMode.Restaurant,
            "Restaurant / Hospitality",
            "Table management, kitchen display, waiter assignment, and hospitality features.",
            "\uE8BD",
            new[] { "Table Management", "Kitchen Display", "Waiter Assignment" }),
        new BusinessModeOption(
            BusinessMode.Supermarket,
            "Supermarket / Retail",
            "Barcode scanning, product offers, loyalty program, and retail features.",
            "\uE7BF",
            new[] { "Barcode Scanning", "Product Offers", "Loyalty Program" }),
        new BusinessModeOption(
            BusinessMode.Hybrid,
            "Hybrid (All Features)",
            "All features enabled for businesses that need both hospitality and retail capabilities.",
            "\uE8F1",
            new[] { "All Features Enabled" })
    };

    public OrganizationSettingsViewModel(
        ILogger logger,
        ISystemConfigurationService configurationService,
        IDialogService dialogService)
        : base(logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Organization Settings";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadSettingsAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Could prompt to save if HasUnsavedChanges
    }

    private async Task LoadSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config != null)
            {
                BusinessName = config.BusinessName;
                BusinessAddress = config.BusinessAddress;
                BusinessPhone = config.BusinessPhone;
                BusinessEmail = config.BusinessEmail;
                KraPinNumber = config.KraPinNumber;
                VatNumber = config.VatRegistrationNumber;
                VatRate = config.DefaultTaxRate;
                CurrencyCode = config.CurrencyCode;
                CurrencySymbol = config.CurrencySymbol;
                SelectedMode = config.Mode;
                ModeDisplayName = config.GetModeDisplayName();
                ModeDescription = config.GetModeDescription();

                // Load logo
                LogoPath = config.LogoPath;
                LoadLogoPreview();

                if (config.UpdatedAt.HasValue)
                {
                    LastSavedInfo = $"Last updated: {config.UpdatedAt.Value:g}";
                }
                else if (config.SetupCompletedAt.HasValue)
                {
                    LastSavedInfo = $"Setup completed: {config.SetupCompletedAt.Value:g}";
                }

                HasUnsavedChanges = false;
                _logger.Information("Organization settings loaded successfully");
            }
            else
            {
                _logger.Warning("No configuration found in database");
                await _dialogService.ShowErrorAsync("Configuration Error",
                    "No configuration found. Please run the setup wizard.");
            }
        }, "Loading settings...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Business name is required.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config == null)
            {
                await _dialogService.ShowErrorAsync("Error", "Configuration not found.");
                return;
            }

            // Update configuration
            config.BusinessName = BusinessName;
            config.BusinessAddress = BusinessAddress;
            config.BusinessPhone = BusinessPhone;
            config.BusinessEmail = BusinessEmail;
            config.KraPinNumber = KraPinNumber;
            config.VatRegistrationNumber = VatNumber;
            config.DefaultTaxRate = VatRate;
            config.CurrencyCode = CurrencyCode;
            config.CurrencySymbol = CurrencySymbol;
            config.LogoPath = LogoPath;

            // Note: Mode change requires special handling (may need app restart)
            if (config.Mode != SelectedMode)
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Change Business Mode",
                    "Changing the business mode will update feature availability. " +
                    "Some features may become unavailable. Continue?");

                if (confirm)
                {
                    await _configurationService.ChangeModeAsync(SelectedMode, applyDefaults: true);
                }
            }
            else
            {
                var success = await _configurationService.SaveConfigurationAsync(config);
                if (!success)
                {
                    await _dialogService.ShowErrorAsync("Save Error", "Failed to save settings. Please try again.");
                    return;
                }
            }

            HasUnsavedChanges = false;
            LastSavedInfo = $"Last updated: {DateTime.Now:g}";
            ModeDisplayName = config.GetModeDisplayName();
            ModeDescription = config.GetModeDescription();

            _logger.Information("Organization settings saved successfully");
            await _dialogService.ShowInfoAsync("Settings Saved", "Organization settings have been saved successfully.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private async Task ReloadSettingsAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirm = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                "You have unsaved changes. Discard them and reload?");
            if (!confirm) return;
        }

        await _configurationService.RefreshCacheAsync();
        await LoadSettingsAsync();
    }

    [RelayCommand]
    private void UploadLogo()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Business Logo",
            Filter = "Image Files|*.png;*.jpg;*.jpeg|PNG Images|*.png|JPEG Images|*.jpg;*.jpeg",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);

                // Validate file size (max 500KB)
                if (fileInfo.Length > 500 * 1024)
                {
                    _dialogService.ShowErrorAsync("File Too Large",
                        "The logo file must be less than 500KB. Please choose a smaller image.");
                    return;
                }

                // Copy to app data folder
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HospitalityPOS",
                    "Images");

                Directory.CreateDirectory(appDataPath);

                var logoFileName = $"logo{fileInfo.Extension}";
                var destPath = Path.Combine(appDataPath, logoFileName);

                // Delete existing logo file if different extension
                var existingLogos = Directory.GetFiles(appDataPath, "logo.*");
                foreach (var existing in existingLogos)
                {
                    try { File.Delete(existing); } catch { /* ignore */ }
                }

                File.Copy(openFileDialog.FileName, destPath, overwrite: true);

                LogoPath = destPath;
                LoadLogoPreview();
                HasUnsavedChanges = true;

                _logger.Information("Logo uploaded: {Path}", destPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to upload logo");
                _dialogService.ShowErrorAsync("Upload Error",
                    "Failed to upload the logo. Please try again.");
            }
        }
    }

    [RelayCommand]
    private async Task RemoveLogoAsync()
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            "Remove Logo",
            "Are you sure you want to remove the business logo?");

        if (!confirm) return;

        try
        {
            // Delete the file if it exists
            if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
            {
                File.Delete(LogoPath);
            }

            LogoPath = null;
            LogoPreview = null;
            HasUnsavedChanges = true;

            _logger.Information("Logo removed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove logo");
            await _dialogService.ShowErrorAsync("Error", "Failed to remove the logo. Please try again.");
        }
    }

    private void LoadLogoPreview()
    {
        if (string.IsNullOrEmpty(LogoPath) || !File.Exists(LogoPath))
        {
            LogoPreview = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(LogoPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // For cross-thread access
            LogoPreview = bitmap;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load logo preview from {Path}", LogoPath);
            LogoPreview = null;
        }
    }

    // Property change tracking
    partial void OnBusinessNameChanged(string value) => HasUnsavedChanges = true;
    partial void OnBusinessAddressChanged(string? value) => HasUnsavedChanges = true;
    partial void OnBusinessPhoneChanged(string? value) => HasUnsavedChanges = true;
    partial void OnBusinessEmailChanged(string? value) => HasUnsavedChanges = true;
    partial void OnKraPinNumberChanged(string? value) => HasUnsavedChanges = true;
    partial void OnVatNumberChanged(string? value) => HasUnsavedChanges = true;
    partial void OnVatRateChanged(decimal value) => HasUnsavedChanges = true;
    partial void OnCurrencyCodeChanged(string value) => HasUnsavedChanges = true;
    partial void OnCurrencySymbolChanged(string value) => HasUnsavedChanges = true;
    partial void OnSelectedModeChanged(BusinessMode value)
    {
        HasUnsavedChanges = true;
        var option = ModeOptions.FirstOrDefault(m => m.Mode == value);
        if (option != null)
        {
            ModeDisplayName = option.Title;
            ModeDescription = option.Description;
        }
    }
}
