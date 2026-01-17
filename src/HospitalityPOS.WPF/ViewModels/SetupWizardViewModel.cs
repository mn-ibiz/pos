using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the initial setup wizard.
/// </summary>
public partial class SetupWizardViewModel : ObservableObject
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _totalSteps = 3;

    [ObservableProperty]
    private string _stepTitle = "Welcome";

    [ObservableProperty]
    private string _stepDescription = "Let's set up your POS system";

    [ObservableProperty]
    private BusinessMode _selectedMode = BusinessMode.Restaurant;

    [ObservableProperty]
    private string _businessName = string.Empty;

    [ObservableProperty]
    private string? _businessAddress;

    [ObservableProperty]
    private string? _businessPhone;

    [ObservableProperty]
    private string? _businessEmail;

    [ObservableProperty]
    private string _currencyCode = "KES";

    [ObservableProperty]
    private string _currencySymbol = "Ksh";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoNext = true;

    [ObservableProperty]
    private bool _isLastStep;

    [ObservableProperty]
    private ObservableCollection<BusinessModeOption> _modeOptions = new();

    [ObservableProperty]
    private BusinessModeOption? _selectedModeOption;

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

    public SetupWizardViewModel(
        ISystemConfigurationService configurationService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        InitializeModeOptions();
        UpdateStepInfo();
    }

    private void InitializeModeOptions()
    {
        ModeOptions = new ObservableCollection<BusinessModeOption>
        {
            new BusinessModeOption(
                BusinessMode.Restaurant,
                "Restaurant / Hospitality",
                "Table management, kitchen display, waiter assignment, and hospitality-focused features.",
                "\uE8BD", // Restaurant icon
                new[] { "Table Management", "Kitchen Display", "Waiter Assignment", "Course Sequencing" }),

            new BusinessModeOption(
                BusinessMode.Supermarket,
                "Supermarket / Retail",
                "Barcode scanning, product promotions, loyalty program, and retail-focused features.",
                "\uE7BF", // Shopping cart icon
                new[] { "Barcode Scanning", "Product Offers", "Loyalty Program", "Batch Tracking" }),

            new BusinessModeOption(
                BusinessMode.Hybrid,
                "Hybrid (All Features)",
                "Full feature set for businesses that need both hospitality and retail capabilities.",
                "\uE8F1", // Combo icon
                new[] { "All Restaurant Features", "All Retail Features", "Payroll", "Accounting" })
        };

        SelectedModeOption = ModeOptions[0];
    }

    partial void OnSelectedModeOptionChanged(BusinessModeOption? value)
    {
        if (value != null)
        {
            SelectedMode = value.Mode;
        }
    }

    partial void OnCurrentStepChanged(int value)
    {
        UpdateStepInfo();
    }

    private void UpdateStepInfo()
    {
        CanGoBack = CurrentStep > 1;
        IsLastStep = CurrentStep == TotalSteps;
        CanGoNext = ValidateCurrentStep();

        (StepTitle, StepDescription) = CurrentStep switch
        {
            1 => ("Select Business Type", "Choose the type of business you're setting up"),
            2 => ("Business Details", "Enter your business information"),
            3 => ("Review & Complete", "Review your settings and complete setup"),
            _ => ("Setup", "Configure your POS system")
        };
    }

    private bool ValidateCurrentStep()
    {
        return CurrentStep switch
        {
            1 => SelectedModeOption != null,
            2 => !string.IsNullOrWhiteSpace(BusinessName),
            3 => true,
            _ => true
        };
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    [RelayCommand]
    private async Task GoNextAsync()
    {
        if (!ValidateCurrentStep())
        {
            if (CurrentStep == 2 && string.IsNullOrWhiteSpace(BusinessName))
            {
                await _dialogService.ShowErrorAsync("Validation Error", "Please enter your business name.");
            }
            return;
        }

        if (IsLastStep)
        {
            await CompleteSetupAsync();
        }
        else
        {
            CurrentStep++;
        }
    }

    private async Task CompleteSetupAsync()
    {
        IsLoading = true;
        StatusMessage = "Setting up your POS system...";

        try
        {
            var configuration = await _configurationService.CompleteSetupAsync(
                SelectedMode,
                BusinessName,
                BusinessAddress,
                BusinessPhone);

            if (configuration != null)
            {
                // Update currency settings
                configuration.CurrencyCode = CurrencyCode;
                configuration.CurrencySymbol = CurrencySymbol;
                configuration.BusinessEmail = BusinessEmail;

                await _configurationService.SaveConfigurationAsync(configuration);

                StatusMessage = "Setup complete! Redirecting to login...";

                await Task.Delay(1500); // Brief pause to show success

                // Navigate to login
                _navigationService.NavigateTo<LoginViewModel>();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Setup failed: {ex.Message}";
            await _dialogService.ShowErrorAsync("Setup Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectMode(BusinessModeOption? option)
    {
        if (option != null)
        {
            SelectedModeOption = option;
        }
    }
}

/// <summary>
/// Represents a business mode option for the setup wizard.
/// </summary>
public class BusinessModeOption
{
    public BusinessMode Mode { get; }
    public string Title { get; }
    public string Description { get; }
    public string Icon { get; }
    public string[] Features { get; }

    public BusinessModeOption(BusinessMode mode, string title, string description, string icon, string[] features)
    {
        Mode = mode;
        Title = title;
        Description = description;
        Icon = icon;
        Features = features;
    }
}

/// <summary>
/// Represents a currency option.
/// </summary>
public class CurrencyOption
{
    public string Code { get; }
    public string Symbol { get; }
    public string Name { get; }
    public string Display => $"{Code} ({Symbol}) - {Name}";

    public CurrencyOption(string code, string symbol, string name)
    {
        Code = code;
        Symbol = symbol;
        Name = name;
    }
}
