using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Represents the login mode selected by the user.
/// </summary>
public enum LoginMode
{
    None,
    Supermarket,
    Restaurant,
    Admin
}

/// <summary>
/// ViewModel for the mode selection screen.
/// Users choose between Supermarket, Restaurant, or Admin login.
/// </summary>
public partial class ModeSelectionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IUiShellService _uiShellService;

    /// <summary>
    /// Static property to track the selected login mode across navigation.
    /// </summary>
    public static LoginMode SelectedLoginMode { get; set; } = LoginMode.None;

    public ModeSelectionViewModel(
        ILogger logger,
        INavigationService navigationService,
        IUiShellService uiShellService)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _uiShellService = uiShellService ?? throw new ArgumentNullException(nameof(uiShellService));

        Title = "Select Mode";
    }

    /// <summary>
    /// Navigate to Supermarket login (username/password for cashiers who stay logged in all shift).
    /// </summary>
    [RelayCommand]
    private void SelectSupermarket()
    {
        SelectedLoginMode = LoginMode.Supermarket;
        _uiShellService.SetMode(BusinessMode.Supermarket);
        _logger.Information("User selected Supermarket mode");
        _navigationService.NavigateTo<LoginViewModel>();
    }

    /// <summary>
    /// Navigate to Restaurant login (PIN-based for quick user switching).
    /// </summary>
    [RelayCommand]
    private void SelectRestaurant()
    {
        SelectedLoginMode = LoginMode.Restaurant;
        _uiShellService.SetMode(BusinessMode.Restaurant);
        _logger.Information("User selected Restaurant mode");
        _navigationService.NavigateTo<LoginViewModel>();
    }

    /// <summary>
    /// Navigate to Admin login (username/password for full admin access).
    /// </summary>
    [RelayCommand]
    private void SelectAdmin()
    {
        SelectedLoginMode = LoginMode.Admin;
        // Admin can access both modes, but we'll default to showing the sidebar
        _logger.Information("User selected Admin mode");
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
