using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the change password screen.
/// </summary>
public partial class ChangePasswordViewModel : ViewModelBase, INavigationAware
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the current password.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _currentPassword = string.Empty;

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _newPassword = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the new password.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _confirmPassword = string.Empty;

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a forced password change.
    /// </summary>
    [ObservableProperty]
    private bool _isForcedChange;

    /// <summary>
    /// Gets or sets the password requirements text.
    /// </summary>
    [ObservableProperty]
    private string _passwordRequirements = "Password must be at least 8 characters and include uppercase, lowercase, and a number.";

    /// <summary>
    /// Gets or sets a value indicating whether the current password field should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _showCurrentPassword = true;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordViewModel"/> class.
    /// </summary>
    public ChangePasswordViewModel(
        ILogger logger,
        IUserService userService,
        ISessionService sessionService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Change Password";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        ClearFields();

        // Validate that a user is logged in
        if (_sessionService.CurrentUser is null)
        {
            _logger.Warning("ChangePasswordView accessed without a logged-in user");
            _navigationService.NavigateTo<LoginViewModel>();
            _navigationService.ClearHistory();
            return;
        }

        // Check if this is a forced password change
        if (parameter is bool forced && forced)
        {
            IsForcedChange = true;
            Title = "Password Change Required";
        }

        // Check if the current user must change password
        if (_sessionService.CurrentUser.MustChangePassword)
        {
            IsForcedChange = true;
            Title = "Password Change Required";
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        ClearFields();
    }

    partial void OnNewPasswordChanged(string value)
    {
        ValidatePasswordRealtime();
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        ValidatePasswordMatch();
    }

    private void ValidatePasswordRealtime()
    {
        if (string.IsNullOrEmpty(NewPassword))
        {
            ErrorMessage = string.Empty;
            return;
        }

        var result = _userService.ValidatePassword(NewPassword);
        if (!result.IsValid)
        {
            ErrorMessage = string.Join("\n", result.Errors);
        }
        else
        {
            ErrorMessage = string.Empty;
            ValidatePasswordMatch();
        }
    }

    private void ValidatePasswordMatch()
    {
        if (!string.IsNullOrEmpty(ConfirmPassword) && NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
        }
        else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage == "Passwords do not match")
        {
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Determines whether the change password command can execute.
    /// </summary>
    private bool CanChangePassword() =>
        !string.IsNullOrWhiteSpace(CurrentPassword) &&
        !string.IsNullOrWhiteSpace(NewPassword) &&
        !string.IsNullOrWhiteSpace(ConfirmPassword) &&
        NewPassword == ConfirmPassword &&
        !IsBusy;

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChangePassword))]
    private async Task ChangePasswordAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ErrorMessage = "Current password is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "New password is required";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match";
                return;
            }

            // Validate complexity
            var validationResult = _userService.ValidatePassword(NewPassword);
            if (!validationResult.IsValid)
            {
                ErrorMessage = string.Join("\n", validationResult.Errors);
                return;
            }

            // Get current user ID
            var userId = _sessionService.CurrentUserId;
            if (userId == 0)
            {
                ErrorMessage = "You must be logged in to change your password";
                return;
            }

            // Change password
            var result = await _userService.ChangePasswordAsync(userId, CurrentPassword, NewPassword);

            if (result.IsSuccess)
            {
                await _dialogService.ShowMessageAsync(
                    "Password Changed",
                    "Your password has been changed successfully.");

                // Navigate based on whether this was a forced change
                if (IsForcedChange)
                {
                    // Forced change - continue to main POS screen
                    _navigationService.NavigateTo<POSViewModel>();
                    _navigationService.ClearHistory();
                }
                else
                {
                    // Voluntary change - go back to previous screen
                    _navigationService.GoBack();
                }
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to change password";

                if (result.ValidationErrors.Count > 0)
                {
                    ErrorMessage = string.Join("\n", result.ValidationErrors);
                }
            }
        }, "Changing password...").ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels the password change operation.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsForcedChange)
        {
            // Cannot cancel forced password change - logout instead
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Logout Required",
                "You must change your password before continuing. Would you like to logout instead?");

            if (confirmed)
            {
                _sessionService.ClearSession(Core.Interfaces.LogoutReason.UserInitiated);
                _navigationService.NavigateTo<LoginViewModel>();
                _navigationService.ClearHistory();
            }
        }
        else
        {
            _navigationService.GoBack();
        }
    }

    private void ClearFields()
    {
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = string.Empty;
    }
}
