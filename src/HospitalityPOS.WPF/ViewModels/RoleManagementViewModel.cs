using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the role management screen.
/// </summary>
public partial class RoleManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly IRoleService _roleService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of roles.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Role> _roles = [];

    /// <summary>
    /// Gets or sets the selected role.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditRoleCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloneRoleCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRoleCommand))]
    private Role? _selectedRole;

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManagementViewModel"/> class.
    /// </summary>
    public RoleManagementViewModel(
        ILogger logger,
        IRoleService roleService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Role Management";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadRolesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnSearchTextChanged(string value)
    {
        // Could add filtering logic here if needed
    }

    #region Commands

    /// <summary>
    /// Loads all roles.
    /// </summary>
    [RelayCommand]
    private async Task LoadRolesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var roles = await _roleService.GetAllRolesAsync();
            Roles = new ObservableCollection<Role>(roles);
            _logger.Debug("Loaded {RoleCount} roles", Roles.Count);
        }, "Loading roles...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [RelayCommand]
    private void CreateRole()
    {
        _navigationService.NavigateTo<RoleEditorViewModel>();
    }

    /// <summary>
    /// Edits the selected role.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditRole))]
    private void EditRole()
    {
        if (SelectedRole is not null)
        {
            _navigationService.NavigateTo<RoleEditorViewModel>(SelectedRole.Id);
        }
    }

    private bool CanEditRole() => SelectedRole is not null;

    /// <summary>
    /// Clones the selected role.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloneRole))]
    private async Task CloneRoleAsync()
    {
        if (SelectedRole is null) return;

        var newName = await _dialogService.ShowInputAsync(
            "Clone Role",
            "Enter a name for the new role:",
            $"{SelectedRole.Name} (Copy)");

        if (string.IsNullOrWhiteSpace(newName)) return;

        await ExecuteAsync(async () =>
        {
            try
            {
                var clonedRole = await _roleService.CloneRoleAsync(SelectedRole.Id, newName);
                await _dialogService.ShowMessageAsync(
                    "Role Cloned",
                    $"Role '{clonedRole.Name}' has been created with the same permissions as '{SelectedRole.Name}'.");

                await LoadRolesAsync();
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowErrorAsync("Clone Failed", ex.Message);
            }
        }, "Cloning role...").ConfigureAwait(true);
    }

    private bool CanCloneRole() => SelectedRole is not null;

    /// <summary>
    /// Deletes the selected role.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteRole))]
    private async Task DeleteRoleAsync()
    {
        if (SelectedRole is null) return;

        if (SelectedRole.IsSystem)
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                "System roles cannot be deleted.");
            return;
        }

        var canDelete = await _roleService.CanDeleteRoleAsync(SelectedRole.Id);
        if (!canDelete)
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                "This role has users assigned to it. Please reassign or remove the users first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Role",
            $"Are you sure you want to delete the role '{SelectedRole.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _roleService.DeleteRoleAsync(SelectedRole.Id);
            if (deleted)
            {
                await _dialogService.ShowMessageAsync(
                    "Role Deleted",
                    $"Role '{SelectedRole.Name}' has been deleted.");

                await LoadRolesAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    "Delete Failed",
                    "Failed to delete the role. Please try again.");
            }
        }, "Deleting role...").ConfigureAwait(true);
    }

    private bool CanDeleteRole() =>
        SelectedRole is not null && !SelectedRole.IsSystem;

    /// <summary>
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}
