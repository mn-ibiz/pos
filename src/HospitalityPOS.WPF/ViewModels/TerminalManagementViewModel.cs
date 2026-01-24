using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the terminal management view.
/// </summary>
public partial class TerminalManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly ITerminalService _terminalService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<TerminalDisplayItem> _terminals = [];

    [ObservableProperty]
    private TerminalDisplayItem? _selectedTerminal;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showInactiveTerminals;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private TerminalType? _filterTerminalType;

    [ObservableProperty]
    private ObservableCollection<TerminalTypeOption> _terminalTypeOptions = [];

    private IReadOnlyList<Terminal> _allTerminals = [];
    private int _currentStoreId;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalManagementViewModel"/> class.
    /// </summary>
    public TerminalManagementViewModel(
        ITerminalService terminalService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        IServiceProvider serviceProvider,
        ILogger logger) : base(logger)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Initialize terminal type options
        TerminalTypeOptions =
        [
            new TerminalTypeOption { Type = null, DisplayName = "All Types" },
            new TerminalTypeOption { Type = TerminalType.Register, DisplayName = "Register" },
            new TerminalTypeOption { Type = TerminalType.Till, DisplayName = "Till" },
            new TerminalTypeOption { Type = TerminalType.AdminWorkstation, DisplayName = "Admin Workstation" },
            new TerminalTypeOption { Type = TerminalType.KitchenDisplay, DisplayName = "Kitchen Display" },
            new TerminalTypeOption { Type = TerminalType.MobileTerminal, DisplayName = "Mobile Terminal" },
            new TerminalTypeOption { Type = TerminalType.SelfCheckout, DisplayName = "Self Checkout" }
        ];
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _currentStoreId = _sessionService.CurrentStoreId ?? 1;
        _ = LoadTerminalsAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnShowInactiveTerminalsChanged(bool value)
    {
        _ = LoadTerminalsAsync();
    }

    partial void OnFilterTerminalTypeChanged(TerminalType? value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Loads all terminals from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadTerminalsAsync()
    {
        try
        {
            IsLoading = true;
            _allTerminals = await _terminalService.GetTerminalsByStoreAsync(_currentStoreId).ConfigureAwait(true);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading terminals");
            await _dialogService.ShowErrorAsync("Error", "Failed to load terminals. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allTerminals.AsEnumerable();

        // Filter inactive
        if (!ShowInactiveTerminals)
        {
            filtered = filtered.Where(t => t.IsActive);
        }

        // Filter by terminal type
        if (FilterTerminalType.HasValue)
        {
            filtered = filtered.Where(t => t.TerminalType == FilterTerminalType.Value);
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Code.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Terminals = new ObservableCollection<TerminalDisplayItem>(
            filtered.Select(t => new TerminalDisplayItem
            {
                Id = t.Id,
                StoreId = t.StoreId,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                TerminalType = t.TerminalType,
                TerminalTypeName = GetTerminalTypeName(t.TerminalType),
                BusinessMode = t.BusinessMode,
                BusinessModeName = GetBusinessModeName(t.BusinessMode),
                IsMainRegister = t.IsMainRegister,
                MachineIdentifier = t.MachineIdentifier,
                IsAssigned = !string.IsNullOrEmpty(t.MachineIdentifier),
                IsActive = t.IsActive,
                IpAddress = t.IpAddress,
                LastHeartbeat = t.LastHeartbeat,
                IsOnline = t.LastHeartbeat.HasValue && (DateTime.UtcNow - t.LastHeartbeat.Value).TotalSeconds < 60,
                CreatedAt = t.CreatedAt
            }));
    }

    /// <summary>
    /// Creates a new terminal.
    /// </summary>
    [RelayCommand]
    private void CreateTerminal()
    {
        _navigationService.NavigateTo<TerminalEditorViewModel>();
    }

    /// <summary>
    /// Edits the selected terminal.
    /// </summary>
    [RelayCommand]
    private void EditTerminal()
    {
        if (SelectedTerminal is null)
        {
            return;
        }

        _navigationService.NavigateTo<TerminalEditorViewModel>(SelectedTerminal.Id);
    }

    /// <summary>
    /// Opens the configuration editor for the selected terminal.
    /// </summary>
    [RelayCommand]
    private void ConfigureTerminal()
    {
        if (SelectedTerminal is null)
        {
            return;
        }

        try
        {
            // Create and configure the dialog
            var viewModel = _serviceProvider.GetRequiredService<TerminalConfigurationViewModel>();
            viewModel.OnNavigatedTo(SelectedTerminal.Id);

            var dialog = new TerminalConfigurationView
            {
                DataContext = viewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            dialog.ShowDialog();

            // Refresh the list after configuration
            _ = LoadTerminalsAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error opening terminal configuration for {TerminalId}", SelectedTerminal.Id);
            _dialogService.ShowErrorAsync("Error", $"Failed to open configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Views the status of the selected terminal.
    /// </summary>
    [RelayCommand]
    private async Task ViewTerminalStatusAsync()
    {
        if (SelectedTerminal is null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var status = await _terminalService.GetTerminalStatusAsync(SelectedTerminal.Id).ConfigureAwait(true);

            if (status is not null)
            {
                var statusMessage = $"""
                    Terminal: {status.Code} - {status.Name}
                    Type: {GetTerminalTypeName(status.TerminalType)}
                    Status: {(status.IsOnline ? "Online" : "Offline")}
                    IP Address: {status.IpAddress ?? "N/A"}
                    Last Heartbeat: {(status.LastHeartbeat.HasValue ? status.LastHeartbeat.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "Never")}
                    Current User: {status.CurrentUserName ?? "None"}
                    Work Period Open: {(status.IsWorkPeriodOpen ? "Yes" : "No")}
                    Printer Available: {(status.IsPrinterAvailable ? "Yes" : "No")}
                    Cash Drawer Available: {(status.IsCashDrawerAvailable ? "Yes" : "No")}
                    """;

                await _dialogService.ShowInfoAsync("Terminal Status", statusMessage);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to retrieve terminal status.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving terminal status for {TerminalId}", SelectedTerminal.Id);
            await _dialogService.ShowErrorAsync("Error", $"Failed to retrieve terminal status: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the active status of the selected terminal.
    /// </summary>
    [RelayCommand]
    private async Task ToggleActiveStatusAsync()
    {
        if (SelectedTerminal is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        var action = SelectedTerminal.IsActive ? "deactivate" : "reactivate";
        var confirm = await _dialogService.ShowConfirmAsync(
            $"{(SelectedTerminal.IsActive ? "Deactivate" : "Reactivate")} Terminal",
            $"Are you sure you want to {action} terminal '{SelectedTerminal.Code} - {SelectedTerminal.Name}'?");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsLoading = true;
            bool success;

            if (SelectedTerminal.IsActive)
            {
                success = await _terminalService.DeactivateTerminalAsync(
                    SelectedTerminal.Id,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);
            }
            else
            {
                success = await _terminalService.ReactivateTerminalAsync(
                    SelectedTerminal.Id,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);
            }

            if (success)
            {
                await LoadTerminalsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to {action} terminal.");
            }
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling active status for terminal {TerminalId}", SelectedTerminal.Id);
            await _dialogService.ShowErrorAsync("Error", $"Failed to {action} terminal: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Unbinds the machine from the selected terminal.
    /// </summary>
    [RelayCommand]
    private async Task UnbindMachineAsync()
    {
        if (SelectedTerminal is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        if (!SelectedTerminal.IsAssigned)
        {
            await _dialogService.ShowInfoAsync("Info", "This terminal is not bound to any machine.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmAsync(
            "Unbind Machine",
            $"Are you sure you want to unbind the machine from terminal '{SelectedTerminal.Code}'?\n\nThis will allow the terminal to be registered on a different machine.");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var success = await _terminalService.UnbindMachineAsync(
                SelectedTerminal.Id,
                _sessionService.CurrentUser.Id).ConfigureAwait(true);

            if (success)
            {
                await _dialogService.ShowInfoAsync("Success", "Machine has been unbound from the terminal.");
                await LoadTerminalsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to unbind machine.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error unbinding machine from terminal {TerminalId}", SelectedTerminal.Id);
            await _dialogService.ShowErrorAsync("Error", $"Failed to unbind machine: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Generates a new terminal code.
    /// </summary>
    [RelayCommand]
    private async Task GenerateTerminalCodeAsync()
    {
        try
        {
            var code = await _terminalService.GenerateTerminalCodeAsync(
                _currentStoreId,
                TerminalType.Register).ConfigureAwait(true);

            await _dialogService.ShowInfoAsync("Generated Code", $"Next available terminal code: {code}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating terminal code");
            await _dialogService.ShowErrorAsync("Error", $"Failed to generate terminal code: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private static string GetTerminalTypeName(TerminalType type) => type switch
    {
        TerminalType.Register => "Register",
        TerminalType.Till => "Till",
        TerminalType.AdminWorkstation => "Admin Workstation",
        TerminalType.KitchenDisplay => "Kitchen Display",
        TerminalType.MobileTerminal => "Mobile Terminal",
        TerminalType.SelfCheckout => "Self Checkout",
        _ => type.ToString()
    };

    private static string GetBusinessModeName(BusinessMode mode) => mode switch
    {
        BusinessMode.Supermarket => "Supermarket",
        BusinessMode.Restaurant => "Restaurant",
        BusinessMode.Admin => "Admin",
        _ => mode.ToString()
    };
}

/// <summary>
/// Terminal display item for the data grid.
/// </summary>
public class TerminalDisplayItem
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TerminalType TerminalType { get; set; }
    public string TerminalTypeName { get; set; } = string.Empty;
    public BusinessMode BusinessMode { get; set; }
    public string BusinessModeName { get; set; } = string.Empty;
    public bool IsMainRegister { get; set; }
    public string MachineIdentifier { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; }

    public string StatusText => IsActive ? (IsOnline ? "Online" : "Offline") : "Inactive";
    public string StatusColor => IsActive ? (IsOnline ? "#4CAF50" : "#FFB347") : "#FF6B6B";
    public string AssignmentText => IsAssigned ? "Assigned" : "Unassigned";
    public string AssignmentColor => IsAssigned ? "#4CAF50" : "#8888AA";
}

/// <summary>
/// Terminal type filter option.
/// </summary>
public class TerminalTypeOption
{
    public TerminalType? Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
