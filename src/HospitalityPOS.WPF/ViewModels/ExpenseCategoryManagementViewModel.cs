using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing expense categories.
/// </summary>
public partial class ExpenseCategoryManagementViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    // Type filter selection
    [ObservableProperty]
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _isCOGSSelected;

    [ObservableProperty]
    private bool _isLaborSelected;

    [ObservableProperty]
    private bool _isOccupancySelected;

    [ObservableProperty]
    private bool _isOperatingSelected;

    [ObservableProperty]
    private bool _isMarketingSelected;

    [ObservableProperty]
    private bool _isAdminSelected;

    // Collections
    private IReadOnlyList<ExpenseCategory> _allCategories = new List<ExpenseCategory>();

    [ObservableProperty]
    private ObservableCollection<ExpenseCategory> _filteredCategories = new();

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    [ObservableProperty]
    private bool _hasNoSubcategories;

    #endregion

    public ExpenseCategoryManagementViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;
    }

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Property Changed Handlers

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (value) ApplyFilter(null);
    }

    partial void OnIsCOGSSelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.COGS);
    }

    partial void OnIsLaborSelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.Labor);
    }

    partial void OnIsOccupancySelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.Occupancy);
    }

    partial void OnIsOperatingSelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.Operating);
    }

    partial void OnIsMarketingSelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.Marketing);
    }

    partial void OnIsAdminSelectedChanged(bool value)
    {
        if (value) ApplyFilter(ExpenseCategoryType.Administrative);
    }

    partial void OnSelectedCategoryChanged(ExpenseCategory? value)
    {
        HasNoSubcategories = value?.SubCategories == null || value.SubCategories.Count == 0;
    }

    private void ApplyFilter(ExpenseCategoryType? type)
    {
        IEnumerable<ExpenseCategory> filtered = _allCategories.Where(c => c.ParentCategoryId == null);

        if (type.HasValue)
        {
            filtered = filtered.Where(c => c.Type == type.Value);
        }

        FilteredCategories = new ObservableCollection<ExpenseCategory>(filtered.OrderBy(c => c.SortOrder).ThenBy(c => c.Name));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading categories...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            _allCategories = await expenseService.GetCategoriesAsync(true);

            ApplyFilter(GetCurrentFilterType());

            _logger.Information("Loaded {Count} expense categories", _allCategories.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load expense categories");
            ErrorMessage = "Failed to load categories. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private ExpenseCategoryType? GetCurrentFilterType()
    {
        if (IsCOGSSelected) return ExpenseCategoryType.COGS;
        if (IsLaborSelected) return ExpenseCategoryType.Labor;
        if (IsOccupancySelected) return ExpenseCategoryType.Occupancy;
        if (IsOperatingSelected) return ExpenseCategoryType.Operating;
        if (IsMarketingSelected) return ExpenseCategoryType.Marketing;
        if (IsAdminSelected) return ExpenseCategoryType.Administrative;
        return null;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private void SelectCategory(ExpenseCategory category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        try
        {
            var name = await _dialogService.ShowInputAsync("New Category", "Enter category name:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var description = await _dialogService.ShowInputAsync("Category Description", "Enter description (optional):");

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var category = new ExpenseCategory
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                Type = ExpenseCategoryType.Operating,
                IsActive = true,
                IsSystemCategory = false,
                SortOrder = 999
            };

            await expenseService.CreateCategoryAsync(category);
            _logger.Information("Created expense category: {Name}", category.Name);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create expense category");
            ErrorMessage = "Failed to create category. Please try again.";
        }
    }

    [RelayCommand]
    private async Task AddSubcategoryAsync()
    {
        if (SelectedCategory == null) return;

        try
        {
            var name = await _dialogService.ShowInputAsync("New Subcategory", $"Enter subcategory name for '{SelectedCategory.Name}':");
            if (string.IsNullOrWhiteSpace(name)) return;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var subcategory = new ExpenseCategory
            {
                Name = name.Trim(),
                Type = SelectedCategory.Type,
                ParentCategoryId = SelectedCategory.Id,
                IsActive = true,
                IsSystemCategory = false,
                SortOrder = 999
            };

            await expenseService.CreateCategoryAsync(subcategory);
            _logger.Information("Created subcategory: {Name} under {Parent}", subcategory.Name, SelectedCategory.Name);

            var selectedId = SelectedCategory.Id;
            await LoadDataAsync();
            SelectedCategory = _allCategories.FirstOrDefault(c => c.Id == selectedId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create subcategory");
            ErrorMessage = "Failed to create subcategory. Please try again.";
        }
    }

    [RelayCommand]
    private async Task EditCategoryAsync(ExpenseCategory category)
    {
        if (category == null) return;

        try
        {
            var newName = await _dialogService.ShowInputAsync("Edit Category", "Enter new name:", category.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName == category.Name) return;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            category.Name = newName.Trim();
            await expenseService.UpdateCategoryAsync(category);
            _logger.Information("Updated expense category: {Name}", category.Name);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update expense category");
            ErrorMessage = "Failed to update category. Please try again.";
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(ExpenseCategory category)
    {
        if (category == null || category.IsSystemCategory) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Category",
                $"Are you sure you want to delete '{category.Name}'?\n\nThis will also delete all subcategories and cannot be undone.");

            if (confirmed)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                await expenseService.DeleteCategoryAsync(category.Id);
                _logger.Information("Deleted expense category: {Name}", category.Name);

                if (SelectedCategory?.Id == category.Id)
                {
                    SelectedCategory = null;
                }

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete expense category");
            ErrorMessage = "Failed to delete category. It may have expenses associated with it.";
        }
    }

    #endregion
}
