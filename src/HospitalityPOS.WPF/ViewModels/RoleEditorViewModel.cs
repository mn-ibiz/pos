using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Represents a permission with a selected state for the UI.
/// </summary>
public partial class PermissionViewModel : ObservableObject
{
    /// <summary>
    /// Gets the underlying permission.
    /// </summary>
    public Permission Permission { get; }

    /// <summary>
    /// Gets or sets whether this permission is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionViewModel"/> class.
    /// </summary>
    public PermissionViewModel(Permission permission, bool isSelected = false)
    {
        Permission = permission;
        IsSelected = isSelected;
    }
}

/// <summary>
/// Represents a category of permissions.
/// </summary>
public class PermissionCategoryViewModel
{
    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the permissions in this category.
    /// </summary>
    public ObservableCollection<PermissionViewModel> Permissions { get; }

    /// <summary>
    /// Gets whether all permissions in this category are selected.
    /// </summary>
    public bool AllSelected => Permissions.All(p => p.IsSelected);

    /// <summary>
    /// Gets whether some (but not all) permissions are selected.
    /// </summary>
    public bool SomeSelected => Permissions.Any(p => p.IsSelected) && !AllSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionCategoryViewModel"/> class.
    /// </summary>
    public PermissionCategoryViewModel(string category, IEnumerable<PermissionViewModel> permissions)
    {
        Category = category;
        Permissions = new ObservableCollection<PermissionViewModel>(permissions);
    }

    /// <summary>
    /// Selects or deselects all permissions in this category.
    /// </summary>
    public void ToggleAll(bool selected)
    {
        foreach (var permission in Permissions)
        {
            permission.IsSelected = selected;
        }
    }
}

/// <summary>
/// ViewModel for the role editor screen.
/// </summary>
public partial class RoleEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRoleService _roleService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private int? _roleId;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _roleName = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    [ObservableProperty]
    private string _roleDescription = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a system role.
    /// </summary>
    [ObservableProperty]
    private bool _isSystemRole;

    /// <summary>
    /// Gets or sets whether this is edit mode (vs create mode).
    /// </summary>
    [ObservableProperty]
    private bool _isEditMode;

    /// <summary>
    /// Gets or sets the permission categories.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PermissionCategoryViewModel> _permissionCategories = [];

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleEditorViewModel"/> class.
    /// </summary>
    public RoleEditorViewModel(
        ILogger logger,
        IRoleService roleService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Create Role";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int roleId)
        {
            _roleId = roleId;
            IsEditMode = true;
            Title = "Edit Role";
            _ = LoadRoleAsync(roleId);
        }
        else
        {
            _roleId = null;
            IsEditMode = false;
            Title = "Create Role";
            _ = LoadPermissionsAsync();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnRoleNameChanged(string value)
    {
        ValidateRoleName();
    }

    private void ValidateRoleName()
    {
        if (string.IsNullOrWhiteSpace(RoleName))
        {
            ErrorMessage = "Role name is required";
        }
        else if (RoleName.Length > 50)
        {
            ErrorMessage = "Role name must be 50 characters or less";
        }
        else
        {
            ErrorMessage = string.Empty;
        }
    }

    #region Private Methods

    private async Task LoadRoleAsync(int roleId)
    {
        await ExecuteAsync(async () =>
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Role not found.");
                _navigationService.GoBack();
                return;
            }

            RoleName = role.Name;
            RoleDescription = role.Description ?? string.Empty;
            IsSystemRole = role.IsSystem;

            // Get selected permission IDs
            var selectedPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            // Load all permissions and mark selected ones
            await LoadPermissionsAsync(selectedPermissionIds);
        }, "Loading role...").ConfigureAwait(true);
    }

    private async Task LoadPermissionsAsync(HashSet<int>? selectedIds = null)
    {
        selectedIds ??= [];

        var permissionsByCategory = await _roleService.GetPermissionsByCategoryAsync();

        var categories = permissionsByCategory
            .Select(kvp => new PermissionCategoryViewModel(
                kvp.Key,
                kvp.Value.Select(p => new PermissionViewModel(p, selectedIds.Contains(p.Id)))))
            .OrderBy(c => c.Category)
            .ToList();

        PermissionCategories = new ObservableCollection<PermissionCategoryViewModel>(categories);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Saves the role.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            // Validate name
            if (string.IsNullOrWhiteSpace(RoleName))
            {
                ErrorMessage = "Role name is required";
                return;
            }

            // Check name uniqueness
            var isUnique = await _roleService.IsRoleNameUniqueAsync(RoleName, _roleId);
            if (!isUnique)
            {
                ErrorMessage = "A role with this name already exists";
                return;
            }

            // Get selected permission IDs
            var permissionIds = PermissionCategories
                .SelectMany(c => c.Permissions)
                .Where(p => p.IsSelected)
                .Select(p => p.Permission.Id)
                .ToList();

            // Validate at least one permission (except for custom roles during edit)
            if (permissionIds.Count == 0 && !IsEditMode)
            {
                ErrorMessage = "At least one permission must be selected";
                return;
            }

            var dto = new RoleDto
            {
                Name = RoleName.Trim(),
                Description = string.IsNullOrWhiteSpace(RoleDescription) ? null : RoleDescription.Trim(),
                PermissionIds = permissionIds
            };

            try
            {
                if (IsEditMode && _roleId.HasValue)
                {
                    await _roleService.UpdateRoleAsync(_roleId.Value, dto);
                    await _dialogService.ShowMessageAsync("Success", "Role updated successfully.");
                }
                else
                {
                    await _roleService.CreateRoleAsync(dto);
                    await _dialogService.ShowMessageAsync("Success", "Role created successfully.");
                }

                _navigationService.GoBack();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
        }, "Saving role...").ConfigureAwait(true);
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(RoleName) &&
        RoleName.Length <= 50 &&
        !IsBusy;

    /// <summary>
    /// Cancels and goes back.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Toggles all permissions in a category.
    /// </summary>
    [RelayCommand]
    private void ToggleCategoryPermissions(PermissionCategoryViewModel category)
    {
        if (category is null) return;

        // Toggle based on current state - if any selected, deselect all; otherwise select all
        var newState = !category.AllSelected;
        category.ToggleAll(newState);
    }

    /// <summary>
    /// Selects all permissions.
    /// </summary>
    [RelayCommand]
    private void SelectAllPermissions()
    {
        foreach (var category in PermissionCategories)
        {
            category.ToggleAll(true);
        }
    }

    /// <summary>
    /// Deselects all permissions.
    /// </summary>
    [RelayCommand]
    private void DeselectAllPermissions()
    {
        foreach (var category in PermissionCategories)
        {
            category.ToggleAll(false);
        }
    }

    #endregion
}
