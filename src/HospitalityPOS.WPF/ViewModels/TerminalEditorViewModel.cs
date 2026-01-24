using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for creating and editing terminals.
/// </summary>
public partial class TerminalEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly ITerminalService _terminalService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private int? _terminalId;
    private Terminal? _existingTerminal;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "New Terminal";

    // Form Fields
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TerminalType _selectedTerminalType = TerminalType.Register;

    [ObservableProperty]
    private BusinessMode _selectedBusinessMode = BusinessMode.Supermarket;

    [ObservableProperty]
    private bool _isMainRegister;

    [ObservableProperty]
    private string _machineIdentifier = string.Empty;

    [ObservableProperty]
    private string _printerConfiguration = string.Empty;

    [ObservableProperty]
    private string _hardwareConfiguration = string.Empty;

    // Validation
    [ObservableProperty]
    private string _codeError = string.Empty;

    [ObservableProperty]
    private string _nameError = string.Empty;

    // Options
    [ObservableProperty]
    private ObservableCollection<TerminalTypeOption> _terminalTypes = [];

    [ObservableProperty]
    private ObservableCollection<BusinessModeOption> _businessModes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalEditorViewModel"/> class.
    /// </summary>
    public TerminalEditorViewModel(
        ITerminalService terminalService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        // Initialize terminal type options
        TerminalTypes =
        [
            new TerminalTypeOption { Type = TerminalType.Register, DisplayName = "Register" },
            new TerminalTypeOption { Type = TerminalType.Till, DisplayName = "Till" },
            new TerminalTypeOption { Type = TerminalType.AdminWorkstation, DisplayName = "Admin Workstation" },
            new TerminalTypeOption { Type = TerminalType.KitchenDisplay, DisplayName = "Kitchen Display" },
            new TerminalTypeOption { Type = TerminalType.MobileTerminal, DisplayName = "Mobile Terminal" },
            new TerminalTypeOption { Type = TerminalType.SelfCheckout, DisplayName = "Self Checkout" }
        ];

        // Initialize business mode options
        BusinessModes =
        [
            new BusinessModeOption { Mode = BusinessMode.Supermarket, DisplayName = "Supermarket" },
            new BusinessModeOption { Mode = BusinessMode.Restaurant, DisplayName = "Restaurant" },
            new BusinessModeOption { Mode = BusinessMode.Admin, DisplayName = "Admin" }
        ];
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int terminalId)
        {
            _terminalId = terminalId;
            IsEditMode = true;
            PageTitle = "Edit Terminal";
            _ = LoadTerminalAsync(terminalId);
        }
        else
        {
            IsEditMode = false;
            PageTitle = "New Terminal";
            _ = GenerateCodeAsync();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    private async Task LoadTerminalAsync(int terminalId)
    {
        try
        {
            IsLoading = true;
            _existingTerminal = await _terminalService.GetTerminalByIdAsync(terminalId).ConfigureAwait(true);

            if (_existingTerminal is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Terminal not found.");
                _navigationService.GoBack();
                return;
            }

            // Populate form
            Code = _existingTerminal.Code;
            Name = _existingTerminal.Name;
            Description = _existingTerminal.Description ?? string.Empty;
            SelectedTerminalType = _existingTerminal.TerminalType;
            SelectedBusinessMode = _existingTerminal.BusinessMode;
            IsMainRegister = _existingTerminal.IsMainRegister;
            MachineIdentifier = _existingTerminal.MachineIdentifier;
            PrinterConfiguration = _existingTerminal.PrinterConfiguration ?? string.Empty;
            HardwareConfiguration = _existingTerminal.HardwareConfiguration ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading terminal {TerminalId}", terminalId);
            await _dialogService.ShowErrorAsync("Error", $"Failed to load terminal: {ex.Message}");
            _navigationService.GoBack();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GenerateCodeAsync()
    {
        try
        {
            var generatedCode = await _terminalService.GenerateTerminalCodeAsync(
                _sessionService.CurrentStoreId ?? 1,
                SelectedTerminalType).ConfigureAwait(true);
            Code = generatedCode;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to generate terminal code");
        }
    }

    partial void OnSelectedTerminalTypeChanged(TerminalType value)
    {
        if (!IsEditMode && string.IsNullOrEmpty(Code))
        {
            _ = GenerateCodeAsync();
        }
    }

    /// <summary>
    /// Validates the form and returns true if valid.
    /// </summary>
    private async Task<bool> ValidateFormAsync()
    {
        var isValid = true;
        CodeError = string.Empty;
        NameError = string.Empty;

        // Validate code
        if (string.IsNullOrWhiteSpace(Code))
        {
            CodeError = "Terminal code is required.";
            isValid = false;
        }
        else
        {
            var isUnique = await _terminalService.IsTerminalCodeUniqueAsync(
                _sessionService.CurrentStoreId ?? 1,
                Code.Trim(),
                _terminalId).ConfigureAwait(true);

            if (!isUnique)
            {
                CodeError = "This code is already in use.";
                isValid = false;
            }
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(Name))
        {
            NameError = "Terminal name is required.";
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Saves the terminal.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_sessionService.CurrentUser is null)
        {
            return;
        }

        if (!await ValidateFormAsync())
        {
            return;
        }

        try
        {
            IsLoading = true;

            if (IsEditMode && _terminalId.HasValue)
            {
                // Update existing terminal
                var request = new UpdateTerminalRequest
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    TerminalType = SelectedTerminalType,
                    BusinessMode = SelectedBusinessMode,
                    IsMainRegister = IsMainRegister,
                    PrinterConfiguration = string.IsNullOrWhiteSpace(PrinterConfiguration) ? null : PrinterConfiguration,
                    HardwareConfiguration = string.IsNullOrWhiteSpace(HardwareConfiguration) ? null : HardwareConfiguration
                };

                await _terminalService.UpdateTerminalAsync(
                    _terminalId.Value,
                    request,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);

                await _dialogService.ShowInfoAsync("Success", "Terminal updated successfully.");
            }
            else
            {
                // Create new terminal
                var request = new CreateTerminalRequest
                {
                    StoreId = _sessionService.CurrentStoreId ?? 1,
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    TerminalType = SelectedTerminalType,
                    BusinessMode = SelectedBusinessMode,
                    IsMainRegister = IsMainRegister,
                    MachineIdentifier = string.IsNullOrWhiteSpace(MachineIdentifier) ? null : MachineIdentifier.Trim()
                };

                await _terminalService.CreateTerminalAsync(
                    request,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);

                await _dialogService.ShowInfoAsync("Success", "Terminal created successfully.");
            }

            _navigationService.GoBack();
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Validation Error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving terminal");
            await _dialogService.ShowErrorAsync("Error", $"Failed to save terminal: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Cancels editing and goes back.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        var hasChanges = HasUnsavedChanges();

        if (hasChanges)
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirm)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Generates a new terminal code.
    /// </summary>
    [RelayCommand]
    private async Task GenerateNewCodeAsync()
    {
        await GenerateCodeAsync();
    }

    private bool HasUnsavedChanges()
    {
        if (!IsEditMode)
        {
            return !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Description);
        }

        if (_existingTerminal is null)
        {
            return false;
        }

        return Code != _existingTerminal.Code ||
               Name != _existingTerminal.Name ||
               (Description ?? string.Empty) != (_existingTerminal.Description ?? string.Empty) ||
               SelectedTerminalType != _existingTerminal.TerminalType ||
               SelectedBusinessMode != _existingTerminal.BusinessMode ||
               IsMainRegister != _existingTerminal.IsMainRegister;
    }
}

/// <summary>
/// Business mode option for combo box.
/// </summary>
public class BusinessModeOption
{
    public BusinessMode Mode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
