using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the login screen with username/password and PIN authentication modes.
/// </summary>
public partial class LoginViewModel : ViewModelBase, INavigationAware
{
    private readonly IUserService _userService;
    private readonly ILoginAuditService _loginAuditService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    /// <summary>
    /// Gets or sets the PIN for quick authentication.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginByPinCommand))]
    private string _pin = string.Empty;

    /// <summary>
    /// Gets or sets whether PIN mode is active.
    /// </summary>
    [ObservableProperty]
    private bool _isPinMode;

    /// <summary>
    /// Gets or sets the selected user for PIN authentication.
    /// </summary>
    [ObservableProperty]
    private User? _selectedUser;

    /// <summary>
    /// Gets or sets the list of users available for quick login.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<User> _quickLoginUsers = [];

    /// <summary>
    /// Gets or sets the masked PIN display (asterisks).
    /// </summary>
    [ObservableProperty]
    private string _pinDisplay = string.Empty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="loginAuditService">The login audit service.</param>
    /// <param name="sessionService">The session service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="dialogService">The dialog service.</param>
    public LoginViewModel(
        ILogger logger,
        IUserService userService,
        ILoginAuditService loginAuditService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _loginAuditService = loginAuditService ?? throw new ArgumentNullException(nameof(loginAuditService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Login";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // Clear any previous credentials
        ClearCredentials();

        // Load quick login users
        _ = LoadQuickLoginUsersAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clear credentials when leaving the view
        ClearCredentials();
    }

    #region Commands

    /// <summary>
    /// Logs in using username and password.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Check if account is locked
            if (await _userService.IsAccountLockedAsync(Username).ConfigureAwait(true))
            {
                var remainingTime = await _userService.GetLockoutRemainingTimeAsync(Username).ConfigureAwait(true);
                var minutes = (int)Math.Ceiling(remainingTime.TotalMinutes);

                // Record locked account attempt
                await _loginAuditService.RecordLoginAttemptAsync(null, Username, false, "Account locked").ConfigureAwait(true);

                await _dialogService.ShowErrorAsync(
                    "Account Locked",
                    $"This account is locked. Please try again in {minutes} minute(s).")
                    .ConfigureAwait(true);
                return;
            }

            var user = await _userService.AuthenticateAsync(Username, Password).ConfigureAwait(true);

            if (user is null)
            {
                // Record failed login attempt
                await _loginAuditService.RecordLoginAttemptAsync(null, Username, false, "Invalid credentials").ConfigureAwait(true);

                await _dialogService.ShowErrorAsync(
                    "Login Failed",
                    "Invalid username or password. Please try again.")
                    .ConfigureAwait(true);
                return;
            }

            // Record successful login
            await _loginAuditService.RecordLoginAttemptAsync(user.Id, user.Username, true).ConfigureAwait(true);

            await CompleteLoginAsync(user).ConfigureAwait(true);
        }, "Authenticating...").ConfigureAwait(true);
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !IsBusy;

    /// <summary>
    /// Logs in using PIN.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLoginByPin))]
    private async Task LoginByPinAsync()
    {
        await ExecuteAsync(async () =>
        {
            var user = await _userService.AuthenticateByPinAsync(Pin).ConfigureAwait(true);

            if (user is null)
            {
                // Record failed PIN login attempt
                var pinUsername = SelectedUser?.Username ?? "[PIN]";
                await _loginAuditService.RecordLoginAttemptAsync(null, pinUsername, false, "Invalid PIN").ConfigureAwait(true);

                await _dialogService.ShowErrorAsync(
                    "Login Failed",
                    "PIN not recognized. Please try again or use username and password.")
                    .ConfigureAwait(true);
                ClearPin();
                return;
            }

            // Record successful PIN login
            await _loginAuditService.RecordLoginAttemptAsync(user.Id, user.Username, true).ConfigureAwait(true);

            await CompleteLoginAsync(user).ConfigureAwait(true);
        }, "Authenticating...").ConfigureAwait(true);
    }

    private bool CanLoginByPin() =>
        !string.IsNullOrWhiteSpace(Pin) &&
        Pin.Length >= 4 &&
        !IsBusy;

    /// <summary>
    /// Toggles between username/password and PIN authentication modes.
    /// </summary>
    [RelayCommand]
    private void ToggleMode()
    {
        IsPinMode = !IsPinMode;
        ClearCredentials();
    }

    /// <summary>
    /// Adds a digit to the PIN.
    /// </summary>
    [RelayCommand]
    private void AddPinDigit(string digit)
    {
        if (Pin.Length >= 6) return;

        Pin += digit;
        UpdatePinDisplay();

        // Auto-submit when PIN is complete (4-6 digits, try at 4)
        if (Pin.Length >= 4 && CanLoginByPin())
        {
            _ = LoginByPinAsync();
        }
    }

    /// <summary>
    /// Clears the PIN entry.
    /// </summary>
    [RelayCommand]
    private void ClearPin()
    {
        Pin = string.Empty;
        UpdatePinDisplay();
    }

    /// <summary>
    /// Removes the last digit from the PIN.
    /// </summary>
    [RelayCommand]
    private void BackspacePin()
    {
        if (Pin.Length > 0)
        {
            Pin = Pin[..^1];
            UpdatePinDisplay();
        }
    }

    /// <summary>
    /// Fills in test credentials for development/testing.
    /// </summary>
    [RelayCommand]
    private void FillTestCredentials()
    {
        Username = "admin";
        Password = "Admin@123";
        // The PasswordBox needs to be updated via code-behind binding
        OnPropertyChanged(nameof(Password));
    }

    /// <summary>
    /// Selects a user for PIN entry.
    /// </summary>
    [RelayCommand]
    private void SelectUser(User user)
    {
        SelectedUser = user;
        ClearPin();
    }

    #endregion

    #region Private Methods

    private async Task CompleteLoginAsync(User user)
    {
        _sessionService.SetCurrentUser(user);
        _logger.Information("User {Username} logged in successfully", user.Username);

        // Clear credentials after successful login
        ClearCredentials();

        // Clear navigation history for fresh session
        _navigationService.ClearHistory();

        // Check if user must change password
        if (user.MustChangePassword)
        {
            _logger.Information("User {Username} must change password", user.Username);
            _navigationService.NavigateTo<ChangePasswordViewModel>(true); // true = forced change
            return;
        }

        // Route based on user role:
        // - Admin/Manager/Supervisor: Full UI with sidebar (Dashboard)
        // - Cashier/Waiter only: Dedicated cashier shell (CashierShellViewModel)
        if (MainWindowViewModel.IsCashierOnlyRole(user))
        {
            _logger.Information("User {Username} is cashier-only, navigating to CashierShell", user.Username);
            _navigationService.NavigateTo<CashierShellViewModel>();
        }
        else
        {
            _logger.Information("User {Username} has admin/manager role, navigating to Dashboard", user.Username);
            _navigationService.NavigateTo<DashboardViewModel>();
        }
    }

    private async Task LoadQuickLoginUsersAsync()
    {
        try
        {
            var users = await _userService.GetActiveUsersForQuickLoginAsync().ConfigureAwait(true);
            QuickLoginUsers = new ObservableCollection<User>(users);

            // Default to PIN mode if we have quick login users
            if (QuickLoginUsers.Count > 0)
            {
                IsPinMode = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load quick login users");
        }
    }

    private void ClearCredentials()
    {
        Username = string.Empty;
        Password = string.Empty;
        Pin = string.Empty;
        SelectedUser = null;
        UpdatePinDisplay();
    }

    private void UpdatePinDisplay()
    {
        PinDisplay = new string('*', Pin.Length);
    }

    #endregion
}
