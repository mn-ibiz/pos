using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the category management screen.
/// </summary>
public partial class CategoryManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly ICategoryService _categoryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of root categories (hierarchical tree).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Gets or sets the selected category.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCategoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateSubcategoryCommand))]
    private Category? _selectedCategory;

    /// <summary>
    /// Gets the total count of all categories.
    /// </summary>
    [ObservableProperty]
    private int _totalCategoryCount;

    /// <summary>
    /// Gets the count of active categories.
    /// </summary>
    [ObservableProperty]
    private int _activeCategoryCount;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryManagementViewModel"/> class.
    /// </summary>
    public CategoryManagementViewModel(
        ILogger logger,
        ICategoryService categoryService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Category Management";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadCategoriesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    #region Commands

    /// <summary>
    /// Loads all categories in a tree structure.
    /// </summary>
    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var categories = await _categoryService.GetCategoryTreeAsync();
            Categories = new ObservableCollection<Category>(categories);

            // Update counts
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            TotalCategoryCount = allCategories.Count;
            ActiveCategoryCount = allCategories.Count(c => c.IsActive);

            _logger.Debug("Loaded {CategoryCount} root categories ({TotalCount} total)",
                Categories.Count, TotalCategoryCount);
        }, "Loading categories...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates a new root category.
    /// </summary>
    [RelayCommand]
    private async Task CreateCategoryAsync()
    {
        if (!RequirePermission(PermissionNames.Products.Manage, "create categories"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to create categories.");
            return;
        }

        var result = await _dialogService.ShowCategoryEditorDialogAsync(null, null);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new CategoryDto
                {
                    Name = result.Name,
                    ParentCategoryId = result.ParentCategoryId,
                    ImagePath = result.ImagePath,
                    DisplayOrder = result.DisplayOrder,
                    IsActive = result.IsActive
                };

                await _categoryService.CreateCategoryAsync(dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Category '{result.Name}' has been created.");
                await LoadCategoriesAsync();
            }, "Creating category...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Creates a subcategory under the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreateSubcategory))]
    private async Task CreateSubcategoryAsync()
    {
        if (SelectedCategory is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "create categories"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to create categories.");
            return;
        }

        var result = await _dialogService.ShowCategoryEditorDialogAsync(null, SelectedCategory.Id);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new CategoryDto
                {
                    Name = result.Name,
                    ParentCategoryId = result.ParentCategoryId,
                    ImagePath = result.ImagePath,
                    DisplayOrder = result.DisplayOrder,
                    IsActive = result.IsActive
                };

                await _categoryService.CreateCategoryAsync(dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Subcategory '{result.Name}' has been created under '{SelectedCategory.Name}'.");
                await LoadCategoriesAsync();
            }, "Creating subcategory...").ConfigureAwait(true);
        }
    }

    private bool CanCreateSubcategory() => SelectedCategory is not null;

    /// <summary>
    /// Edits the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditCategory))]
    private async Task EditCategoryAsync()
    {
        if (SelectedCategory is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "edit categories"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to edit categories.");
            return;
        }

        var result = await _dialogService.ShowCategoryEditorDialogAsync(SelectedCategory, SelectedCategory.ParentCategoryId);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new CategoryDto
                {
                    Name = result.Name,
                    ParentCategoryId = result.ParentCategoryId,
                    ImagePath = result.ImagePath,
                    DisplayOrder = result.DisplayOrder,
                    IsActive = result.IsActive
                };

                await _categoryService.UpdateCategoryAsync(SelectedCategory.Id, dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Category '{result.Name}' has been updated.");
                await LoadCategoriesAsync();
            }, "Updating category...").ConfigureAwait(true);
        }
    }

    private bool CanEditCategory() => SelectedCategory is not null;

    /// <summary>
    /// Deletes the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteCategory))]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "delete categories"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to delete categories.");
            return;
        }

        // Check if category has products
        if (await _categoryService.HasProductsAsync(SelectedCategory.Id))
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                "This category has products assigned to it. Please remove or move the products first.");
            return;
        }

        // Check if category has subcategories
        if (await _categoryService.HasSubcategoriesAsync(SelectedCategory.Id))
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                "This category has subcategories. Please delete the subcategories first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Category",
            $"Are you sure you want to delete the category '{SelectedCategory.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _categoryService.DeleteCategoryAsync(SelectedCategory.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                await _dialogService.ShowMessageAsync(
                    "Category Deleted",
                    $"Category '{SelectedCategory.Name}' has been deleted.");

                await LoadCategoriesAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    "Delete Failed",
                    "Failed to delete the category. Please try again.");
            }
        }, "Deleting category...").ConfigureAwait(true);
    }

    private bool CanDeleteCategory() => SelectedCategory is not null;

    /// <summary>
    /// Toggles the active status of the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleActive))]
    private async Task ToggleActiveAsync()
    {
        if (SelectedCategory is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "change category status"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to change category status.");
            return;
        }

        var newStatus = !SelectedCategory.IsActive;
        var action = newStatus ? "activate" : "deactivate";

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"{(newStatus ? "Activate" : "Deactivate")} Category",
            $"Are you sure you want to {action} the category '{SelectedCategory.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _categoryService.SetCategoryActiveAsync(SelectedCategory.Id, newStatus, SessionService.CurrentUserId);
            await _dialogService.ShowMessageAsync(
                "Success",
                $"Category '{SelectedCategory.Name}' has been {(newStatus ? "activated" : "deactivated")}.");
            await LoadCategoriesAsync();
        }, $"{(newStatus ? "Activating" : "Deactivating")} category...").ConfigureAwait(true);
    }

    private bool CanToggleActive() => SelectedCategory is not null;

    /// <summary>
    /// Moves the selected category up in display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveUpAsync()
    {
        if (SelectedCategory is null) return;

        await ExecuteAsync(async () =>
        {
            var siblings = await GetSiblingsAsync(SelectedCategory.ParentCategoryId);
            var ordered = siblings.OrderBy(c => c.DisplayOrder).ToList();
            var currentIndex = ordered.FindIndex(c => c.Id == SelectedCategory.Id);

            if (currentIndex > 0)
            {
                // Swap display orders
                var orderings = new List<CategoryOrderDto>
                {
                    new() { CategoryId = SelectedCategory.Id, DisplayOrder = ordered[currentIndex - 1].DisplayOrder, ParentCategoryId = SelectedCategory.ParentCategoryId },
                    new() { CategoryId = ordered[currentIndex - 1].Id, DisplayOrder = SelectedCategory.DisplayOrder, ParentCategoryId = ordered[currentIndex - 1].ParentCategoryId }
                };

                await _categoryService.ReorderCategoriesAsync(orderings, SessionService.CurrentUserId);
                await LoadCategoriesAsync();
            }
        }, "Reordering...").ConfigureAwait(true);
    }

    /// <summary>
    /// Moves the selected category down in display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveDownAsync()
    {
        if (SelectedCategory is null) return;

        await ExecuteAsync(async () =>
        {
            var siblings = await GetSiblingsAsync(SelectedCategory.ParentCategoryId);
            var ordered = siblings.OrderBy(c => c.DisplayOrder).ToList();
            var currentIndex = ordered.FindIndex(c => c.Id == SelectedCategory.Id);

            if (currentIndex < ordered.Count - 1)
            {
                // Swap display orders
                var orderings = new List<CategoryOrderDto>
                {
                    new() { CategoryId = SelectedCategory.Id, DisplayOrder = ordered[currentIndex + 1].DisplayOrder, ParentCategoryId = SelectedCategory.ParentCategoryId },
                    new() { CategoryId = ordered[currentIndex + 1].Id, DisplayOrder = SelectedCategory.DisplayOrder, ParentCategoryId = ordered[currentIndex + 1].ParentCategoryId }
                };

                await _categoryService.ReorderCategoriesAsync(orderings, SessionService.CurrentUserId);
                await LoadCategoriesAsync();
            }
        }, "Reordering...").ConfigureAwait(true);
    }

    /// <summary>
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Helpers

    private async Task<IReadOnlyList<Category>> GetSiblingsAsync(int? parentCategoryId)
    {
        var all = await _categoryService.GetAllCategoriesAsync();
        return all.Where(c => c.ParentCategoryId == parentCategoryId).ToList();
    }

    #endregion
}
