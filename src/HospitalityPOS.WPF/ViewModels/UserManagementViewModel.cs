using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the user management view.
/// </summary>
public partial class UserManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<User> _users = [];

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showInactiveUsers;

    [ObservableProperty]
    private bool _isLoading;

    private IReadOnlyList<User> _allUsers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementViewModel"/> class.
    /// </summary>
    public UserManagementViewModel(
        IUserService userService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadUsersAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnShowInactiveUsersChanged(bool value)
    {
        _ = LoadUsersAsync();
    }

    /// <summary>
    /// Loads all users from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            _allUsers = await _userService.GetAllUsersAsync(ShowInactiveUsers).ConfigureAwait(true);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading users");
            await _dialogService.ShowErrorAsync("Error", "Failed to load users. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(u =>
                u.Username.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                u.FullName.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                (u.Email?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Users = new ObservableCollection<User>(filtered);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [RelayCommand]
    private void CreateUser()
    {
        _navigationService.NavigateTo<UserEditorViewModel>(null);
    }

    /// <summary>
    /// Edits the selected user.
    /// </summary>
    [RelayCommand]
    private void EditUser()
    {
        if (SelectedUser is null)
        {
            return;
        }

        _navigationService.NavigateTo<UserEditorViewModel>(SelectedUser.Id);
    }

    /// <summary>
    /// Resets the password for the selected user.
    /// </summary>
    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmAsync(
            "Reset Password",
            $"Are you sure you want to reset the password for '{SelectedUser.FullName}'?\n\nA temporary password will be generated.");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var tempPassword = await _userService.ResetPasswordAsync(
                SelectedUser.Id,
                _sessionService.CurrentUser.Id).ConfigureAwait(true);

            if (tempPassword is not null)
            {
                await _dialogService.ShowInfoAsync(
                    "Password Reset",
                    $"Password has been reset for '{SelectedUser.FullName}'.\n\nTemporary password: {tempPassword}\n\nThe user must change this password on next login.");

                await LoadUsersAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to reset password. User not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error resetting password for user {UserId}", SelectedUser.Id);
            await _dialogService.ShowErrorAsync("Error", $"Failed to reset password: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the active status of the selected user.
    /// </summary>
    [RelayCommand]
    private async Task ToggleActiveStatusAsync()
    {
        if (SelectedUser is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        var action = SelectedUser.IsActive ? "deactivate" : "activate";
        var confirm = await _dialogService.ShowConfirmAsync(
            $"{(SelectedUser.IsActive ? "Deactivate" : "Activate")} User",
            $"Are you sure you want to {action} '{SelectedUser.FullName}'?");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsLoading = true;
            bool success;

            if (SelectedUser.IsActive)
            {
                success = await _userService.DeactivateUserAsync(
                    SelectedUser.Id,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);
            }
            else
            {
                success = await _userService.ActivateUserAsync(
                    SelectedUser.Id,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);
            }

            if (success)
            {
                await LoadUsersAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to {action} user.");
            }
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling active status for user {UserId}", SelectedUser.Id);
            await _dialogService.ShowErrorAsync("Error", $"Failed to {action} user: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sets or changes the PIN for the selected user.
    /// </summary>
    [RelayCommand]
    private async Task SetPinAsync()
    {
        if (SelectedUser is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        // For now, show a simple dialog - in a real implementation, this would be a proper PIN entry dialog
        var hasPIN = !string.IsNullOrEmpty(SelectedUser.PINHash);
        var message = hasPIN
            ? $"Enter a new PIN for '{SelectedUser.FullName}' (4-6 digits), or leave empty to remove the PIN:"
            : $"Enter a PIN for '{SelectedUser.FullName}' (4-6 digits):";

        // This is a simplified implementation - a real app would have a proper PIN entry dialog
        await _dialogService.ShowInfoAsync(
            "Set PIN",
            "PIN management requires editing the user. Please use the Edit button to modify the user's PIN.");
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Gets the role display string for a user.
    /// </summary>
    public static string GetRolesDisplay(User? user)
    {
        if (user?.UserRoles is null || user.UserRoles.Count == 0)
        {
            return "No roles";
        }

        return string.Join(", ", user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role.Name));
    }
}
