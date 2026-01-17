using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    protected readonly ILogger _logger;

    /// <summary>
    /// Gets the authorization service for permission checking.
    /// Lazily resolved from the service provider.
    /// </summary>
    protected static IAuthorizationService AuthorizationService =>
        App.Services.GetRequiredService<IAuthorizationService>();

    /// <summary>
    /// Gets the permission override service for authorization overrides.
    /// Lazily resolved from the service provider.
    /// </summary>
    protected static IPermissionOverrideService PermissionOverrideService =>
        App.Services.GetRequiredService<IPermissionOverrideService>();

    /// <summary>
    /// Gets the dialog service.
    /// Lazily resolved from the service provider.
    /// </summary>
    protected static IDialogService DialogService =>
        App.Services.GetRequiredService<IDialogService>();

    /// <summary>
    /// Gets the session service.
    /// Lazily resolved from the service provider.
    /// </summary>
    protected static ISessionService SessionService =>
        App.Services.GetRequiredService<ISessionService>();

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy performing an operation.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gets or sets the busy message displayed during operations.
    /// </summary>
    [ObservableProperty]
    private string _busyMessage = string.Empty;

    /// <summary>
    /// Gets or sets the title of this ViewModel/View.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets a value indicating whether there is an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ViewModelBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an async operation with busy indicator and error handling.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="busyMessage">The message to display while busy.</param>
    protected async Task ExecuteAsync(Func<Task> operation, string busyMessage = "Please wait...")
    {
        if (IsBusy) return;

        IsBusy = true;
        BusyMessage = busyMessage;
        ErrorMessage = null;

        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Operation failed in {ViewModel}", GetType().Name);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    /// <summary>
    /// Executes an async operation with busy indicator and error handling.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="busyMessage">The message to display while busy.</param>
    /// <returns>The result of the operation, or default if an error occurred.</returns>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string busyMessage = "Please wait...")
    {
        if (IsBusy) return default;

        IsBusy = true;
        BusyMessage = busyMessage;
        ErrorMessage = null;

        try
        {
            return await operation().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Operation failed in {ViewModel}", GetType().Name);
            ErrorMessage = ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    /// <summary>
    /// Clears the current error message.
    /// </summary>
    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = null;
    }

    #region Permission Helpers

    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    protected bool HasPermission(string permissionName)
    {
        return AuthorizationService.HasPermission(permissionName);
    }

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissionNames">The permission names to check.</param>
    /// <returns>True if the user has any permission; otherwise, false.</returns>
    protected bool HasAnyPermission(params string[] permissionNames)
    {
        return AuthorizationService.HasAnyPermission(permissionNames);
    }

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    /// <param name="permissionNames">The permission names to check.</param>
    /// <returns>True if the user has all permissions; otherwise, false.</returns>
    protected bool HasAllPermissions(params string[] permissionNames)
    {
        return AuthorizationService.HasAllPermissions(permissionNames);
    }

    /// <summary>
    /// Checks permission and sets error message if denied.
    /// Use in CanExecute methods for commands that should be disabled without permission.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    protected bool CheckPermission(string permissionName)
    {
        if (HasPermission(permissionName))
        {
            return true;
        }

        _logger.Warning("Permission denied: {Permission}", permissionName);
        return false;
    }

    /// <summary>
    /// Checks permission and shows unauthorized message if denied.
    /// Use in command execution to block action with user feedback.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <param name="actionDescription">Description of the action for the error message.</param>
    /// <returns>True if authorized; false with error message set if not authorized.</returns>
    protected bool RequirePermission(string permissionName, string? actionDescription = null)
    {
        if (HasPermission(permissionName))
        {
            return true;
        }

        var message = string.IsNullOrEmpty(actionDescription)
            ? $"You do not have permission to perform this action. Required: {permissionName}"
            : $"You do not have permission to {actionDescription}. Required: {permissionName}";

        ErrorMessage = message;
        _logger.Warning("Unauthorized action: {Action}, Permission: {Permission}", actionDescription ?? "Unknown", permissionName);
        return false;
    }

    /// <summary>
    /// Checks any permission and shows unauthorized message if all denied.
    /// </summary>
    /// <param name="actionDescription">Description of the action for the error message.</param>
    /// <param name="permissionNames">The permission names to check.</param>
    /// <returns>True if authorized; false with error message set if not authorized.</returns>
    protected bool RequireAnyPermission(string? actionDescription, params string[] permissionNames)
    {
        if (HasAnyPermission(permissionNames))
        {
            return true;
        }

        var message = string.IsNullOrEmpty(actionDescription)
            ? $"You do not have permission to perform this action. Required one of: {string.Join(", ", permissionNames)}"
            : $"You do not have permission to {actionDescription}. Required one of: {string.Join(", ", permissionNames)}";

        ErrorMessage = message;
        _logger.Warning("Unauthorized action: {Action}, Permissions: [{Permissions}]", actionDescription ?? "Unknown", string.Join(", ", permissionNames));
        return false;
    }

    /// <summary>
    /// Checks permission and offers authorization override if user lacks permission.
    /// This method will show a PIN dialog if the user doesn't have the required permission.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <param name="actionDescription">Description of the action for the override dialog.</param>
    /// <returns>OverrideResult indicating whether to proceed with the action.</returns>
    protected async Task<OverrideResult> RequirePermissionOrOverrideAsync(string permissionName, string actionDescription)
    {
        // First check if user already has permission
        if (HasPermission(permissionName))
        {
            return OverrideResult.Success(
                SessionService.CurrentUserId,
                SessionService.CurrentUserDisplayName,
                permissionName,
                actionDescription);
        }

        // Show override dialog
        var pin = await DialogService.ShowAuthorizationOverrideAsync(actionDescription, permissionName);

        if (string.IsNullOrEmpty(pin))
        {
            // User cancelled
            return OverrideResult.Cancelled();
        }

        // Validate PIN and authorize
        var result = await PermissionOverrideService.ValidatePinAndAuthorizeAsync(
            pin,
            permissionName,
            actionDescription,
            SessionService.CurrentUserId);

        if (!result.IsAuthorized && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            ErrorMessage = result.ErrorMessage;
        }

        return result;
    }

    /// <summary>
    /// Checks if user has permission, and if not, prompts for authorization override with retry.
    /// Shows error messages and allows retry on invalid PIN.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <param name="actionDescription">Description of the action for the override dialog.</param>
    /// <returns>True if authorized (either directly or via override); false if cancelled or max retries exceeded.</returns>
    protected async Task<bool> RequirePermissionOrOverrideWithRetryAsync(string permissionName, string actionDescription)
    {
        // First check if user already has permission
        if (HasPermission(permissionName))
        {
            return true;
        }

        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Show override dialog
            var pin = await DialogService.ShowAuthorizationOverrideAsync(actionDescription, permissionName);

            if (string.IsNullOrEmpty(pin))
            {
                // User cancelled
                return false;
            }

            // Validate PIN and authorize
            var result = await PermissionOverrideService.ValidatePinAndAuthorizeAsync(
                pin,
                permissionName,
                actionDescription,
                SessionService.CurrentUserId);

            if (result.IsAuthorized)
            {
                return true;
            }

            // Show error and allow retry (unless last attempt)
            if (attempt < maxRetries - 1)
            {
                await DialogService.ShowErrorAsync("Authorization Failed", $"{result.ErrorMessage}\n\nPlease try again.");
            }
            else
            {
                await DialogService.ShowErrorAsync("Authorization Failed", $"{result.ErrorMessage}\n\nMaximum attempts exceeded.");
            }
        }

        return false;
    }

    #endregion
}
