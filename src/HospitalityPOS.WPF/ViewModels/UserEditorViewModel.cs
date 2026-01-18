using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for creating a new user.
/// </summary>
public partial class RoleSelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public Role Role { get; }

    public RoleSelectionViewModel(Role role, bool isSelected = false)
    {
        Role = role;
        IsSelected = isSelected;
    }
}

/// <summary>
/// ViewModel for the user editor view.
/// </summary>
public partial class UserEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private int? _userId;
    private User? _existingUser;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _title = "Create User";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _pin;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private ObservableCollection<RoleSelectionViewModel> _roles = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _usernameError;

    [ObservableProperty]
    private string? _passwordError;

    [ObservableProperty]
    private string? _fullNameError;

    [ObservableProperty]
    private string? _pinError;

    [ObservableProperty]
    private string? _rolesError;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEditorViewModel"/> class.
    /// </summary>
    public UserEditorViewModel(
        IUserService userService,
        IRoleService roleService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int userId)
        {
            _userId = userId;
            IsEditMode = true;
            Title = "Edit User";
        }
        else
        {
            _userId = null;
            IsEditMode = false;
            Title = "Create User";
        }

        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load roles
            var allRoles = await _roleService.GetAllRolesAsync().ConfigureAwait(true);

            if (IsEditMode && _userId.HasValue)
            {
                // Load existing user
                _existingUser = await _userService.GetByIdAsync(_userId.Value).ConfigureAwait(true);

                if (_existingUser is null)
                {
                    await _dialogService.ShowErrorAsync("Error", "User not found.");
                    _navigationService.GoBack();
                    return;
                }

                // Populate fields
                Username = _existingUser.Username;
                FullName = _existingUser.FullName;
                Email = _existingUser.Email;
                Phone = _existingUser.Phone;
                IsActive = _existingUser.IsActive;
                // PIN and Password are not loaded for security reasons

                // Set up roles with existing selections
                var userRoleIds = _existingUser.UserRoles.Select(ur => ur.RoleId).ToHashSet();
                Roles = new ObservableCollection<RoleSelectionViewModel>(
                    allRoles.Select(r => new RoleSelectionViewModel(r, userRoleIds.Contains(r.Id))));
            }
            else
            {
                // New user - all roles unselected
                Roles = new ObservableCollection<RoleSelectionViewModel>(
                    allRoles.Select(r => new RoleSelectionViewModel(r)));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading user editor data");
            await _dialogService.ShowErrorAsync("Error", "Failed to load data. Please try again.");
            _navigationService.GoBack();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave()
    {
        // Prevent double-submit
        if (IsLoading)
        {
            return false;
        }

        // Basic validation - more detailed in ValidateAll
        if (string.IsNullOrWhiteSpace(FullName))
        {
            return false;
        }

        if (!IsEditMode)
        {
            // Creating new user - username and password required
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateAll()
    {
        var isValid = true;

        // Clear previous errors
        UsernameError = null;
        PasswordError = null;
        FullNameError = null;
        PinError = null;
        RolesError = null;
        ErrorMessage = null;

        // Username validation (only for new users)
        if (!IsEditMode)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                UsernameError = "Username is required.";
                isValid = false;
            }
            else if (Username.Length < 3 || Username.Length > 50)
            {
                UsernameError = "Username must be 3-50 characters.";
                isValid = false;
            }
            else if (!UsernameRegex().IsMatch(Username))
            {
                UsernameError = "Username can only contain letters, numbers, and underscores.";
                isValid = false;
            }
        }

        // Password validation (only for new users)
        if (!IsEditMode)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordError = "Password is required.";
                isValid = false;
            }
            else
            {
                var passwordValidation = _userService.ValidatePassword(Password);
                if (!passwordValidation.IsValid)
                {
                    PasswordError = string.Join(" ", passwordValidation.Errors);
                    isValid = false;
                }
                else if (Password != ConfirmPassword)
                {
                    PasswordError = "Passwords do not match.";
                    isValid = false;
                }
            }
        }

        // Full name validation
        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullNameError = "Full name is required.";
            isValid = false;
        }
        else if (FullName.Length < 2 || FullName.Length > 100)
        {
            FullNameError = "Full name must be 2-100 characters.";
            isValid = false;
        }

        // PIN validation (optional)
        if (!string.IsNullOrWhiteSpace(Pin))
        {
            if (!_userService.ValidatePinFormat(Pin))
            {
                PinError = "PIN must be 4-6 digits.";
                isValid = false;
            }
        }

        // Role validation
        var selectedRoles = Roles.Where(r => r.IsSelected).ToList();
        if (selectedRoles.Count == 0)
        {
            RolesError = "At least one role must be selected.";
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Saves the user.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_sessionService.CurrentUser is null)
        {
            await _dialogService.ShowErrorAsync("Error", "You must be logged in to perform this action.");
            return;
        }

        if (!ValidateAll())
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var selectedRoleIds = Roles
                .Where(r => r.IsSelected)
                .Select(r => r.Role.Id)
                .ToList();

            if (IsEditMode && _userId.HasValue)
            {
                // Update existing user
                var updateDto = new UpdateUserDto
                {
                    FullName = FullName.Trim(),
                    Email = Email?.Trim(),
                    Phone = Phone?.Trim(),
                    RoleIds = selectedRoleIds,
                    IsActive = IsActive
                };

                var success = await _userService.UpdateUserAsync(
                    _userId.Value,
                    updateDto,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);

                if (!success)
                {
                    ErrorMessage = "Failed to update user.";
                    return;
                }

                // Update PIN if provided
                if (!string.IsNullOrWhiteSpace(Pin))
                {
                    await _userService.SetPinAsync(
                        _userId.Value,
                        Pin,
                        _sessionService.CurrentUser.Id).ConfigureAwait(true);
                }

                _logger.Information("User updated: {UserId}", _userId.Value);
            }
            else
            {
                // Create new user
                var createDto = new CreateUserDto
                {
                    Username = Username.Trim(),
                    Password = Password,
                    FullName = FullName.Trim(),
                    Email = Email?.Trim(),
                    Phone = Phone?.Trim(),
                    PIN = Pin,
                    RoleIds = selectedRoleIds,
                    IsActive = IsActive
                };

                await _userService.CreateUserAsync(
                    createDto,
                    _sessionService.CurrentUser.Id).ConfigureAwait(true);

                _logger.Information("User created: {Username}", Username);
            }

            _navigationService.GoBack();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving user");
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Cancels and returns to the user list.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Selects all roles.
    /// </summary>
    [RelayCommand]
    private void SelectAllRoles()
    {
        foreach (var role in Roles)
        {
            role.IsSelected = true;
        }
        RolesError = null;
    }

    /// <summary>
    /// Deselects all roles.
    /// </summary>
    [RelayCommand]
    private void DeselectAllRoles()
    {
        foreach (var role in Roles)
        {
            role.IsSelected = false;
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();
}
