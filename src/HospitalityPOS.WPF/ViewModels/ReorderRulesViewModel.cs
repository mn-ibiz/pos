using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for reorder rules configuration.
/// </summary>
public partial class ReorderRulesViewModel : ViewModelBase, INavigationAware
{
    private readonly IInventoryAnalyticsService _analyticsService;
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<ReorderRule> _reorderRules = [];

    [ObservableProperty]
    private ReorderRule? _selectedRule;

    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = [];

    [ObservableProperty]
    private ObservableCollection<Category> _categories = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategoryFilter;

    [ObservableProperty]
    private bool _showOnlyEnabled = false;

    [ObservableProperty]
    private bool _isLoading;

    // Edit form properties
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ReorderRule? _editingRule;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    private IReadOnlyList<ReorderRule> _allRules = [];

    public ReorderRulesViewModel(
        IInventoryAnalyticsService analyticsService,
        IProductService productService,
        ISupplierService supplierService,
        ICategoryService categoryService,
        IDialogService dialogService,
        ISessionService sessionService,
        INavigationService navigationService,
        ILogger logger) : base(logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Reorder Rules Configuration";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedCategoryFilterChanged(Category? value)
    {
        ApplyFilter();
    }

    partial void OnShowOnlyEnabledChanged(bool value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Loads all data.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                // Load reference data
                var categories = await _categoryService.GetAllCategoriesAsync().ConfigureAwait(true);
                Categories = new ObservableCollection<Category>(categories);

                var suppliers = await _supplierService.GetAllSuppliersAsync().ConfigureAwait(true);
                Suppliers = new ObservableCollection<Supplier>(suppliers);

                var products = await _productService.GetAllProductsAsync().ConfigureAwait(true);
                Products = new ObservableCollection<Product>(products.Where(p => p.TrackInventory));

                // Load reorder rules
                var storeId = _sessionService.CurrentStoreId ?? 1;
                var rules = await _analyticsService.GetReorderRulesAsync(storeId).ConfigureAwait(true);
                _allRules = rules.ToList();

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading reorder rules...").ConfigureAwait(true);
    }

    private void ApplyFilter()
    {
        var filtered = _allRules.AsEnumerable();

        // Filter by enabled status
        if (ShowOnlyEnabled)
        {
            filtered = filtered.Where(r => r.IsAutoReorderEnabled);
        }

        // Filter by category
        if (SelectedCategoryFilter != null)
        {
            filtered = filtered.Where(r => r.Product?.CategoryId == SelectedCategoryFilter.Id);
        }

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(r =>
                (r.Product?.Name?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Product?.Code?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Product?.SKU?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        ReorderRules = new ObservableCollection<ReorderRule>(filtered);
    }

    /// <summary>
    /// Creates a new reorder rule.
    /// </summary>
    [RelayCommand]
    private void CreateRule()
    {
        EditingRule = new ReorderRule
        {
            StoreId = _sessionService.CurrentStoreId ?? 1,
            IsAutoReorderEnabled = true,
            ConsolidateReorders = true,
            SafetyStock = 5,
            LeadTimeDays = 7,
            ReorderPoint = 10,
            ReorderQuantity = 50
        };
        SelectedProduct = null;
        SelectedSupplier = null;
        IsEditing = true;
    }

    /// <summary>
    /// Edits the selected rule.
    /// </summary>
    [RelayCommand]
    private void EditRule()
    {
        if (SelectedRule == null)
        {
            return;
        }

        EditingRule = new ReorderRule
        {
            Id = SelectedRule.Id,
            StoreId = SelectedRule.StoreId,
            ProductId = SelectedRule.ProductId,
            ReorderPoint = SelectedRule.ReorderPoint,
            ReorderQuantity = SelectedRule.ReorderQuantity,
            MaxStockLevel = SelectedRule.MaxStockLevel,
            SafetyStock = SelectedRule.SafetyStock,
            LeadTimeDays = SelectedRule.LeadTimeDays,
            PreferredSupplierId = SelectedRule.PreferredSupplierId,
            IsAutoReorderEnabled = SelectedRule.IsAutoReorderEnabled,
            ConsolidateReorders = SelectedRule.ConsolidateReorders,
            MinOrderQuantity = SelectedRule.MinOrderQuantity,
            OrderMultiple = SelectedRule.OrderMultiple,
            EconomicOrderQuantity = SelectedRule.EconomicOrderQuantity
        };

        SelectedProduct = Products.FirstOrDefault(p => p.Id == SelectedRule.ProductId);
        SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == SelectedRule.PreferredSupplierId);
        IsEditing = true;
    }

    /// <summary>
    /// Saves the current rule.
    /// </summary>
    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if (EditingRule == null)
        {
            return;
        }

        if (SelectedProduct == null)
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Please select a product.").ConfigureAwait(true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            EditingRule.ProductId = SelectedProduct.Id;
            EditingRule.PreferredSupplierId = SelectedSupplier?.Id;

            await _analyticsService.SaveReorderRuleAsync(EditingRule).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", "Reorder rule saved successfully.").ConfigureAwait(true);

            IsEditing = false;
            EditingRule = null;
            await LoadDataAsync().ConfigureAwait(true);
        }, "Saving reorder rule...").ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels editing.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingRule = null;
        SelectedProduct = null;
        SelectedSupplier = null;
    }

    /// <summary>
    /// Deletes the selected rule.
    /// </summary>
    [RelayCommand]
    private async Task DeleteRuleAsync()
    {
        if (SelectedRule == null)
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Reorder Rule",
            $"Are you sure you want to delete the reorder rule for {SelectedRule.Product?.Name ?? "this product"}?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _analyticsService.DeleteReorderRuleAsync(SelectedRule.Id).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", "Reorder rule deleted successfully.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Deleting reorder rule...").ConfigureAwait(true);
    }

    /// <summary>
    /// Toggles auto-reorder for the selected rule.
    /// </summary>
    [RelayCommand]
    private async Task ToggleAutoReorderAsync()
    {
        if (SelectedRule == null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            SelectedRule.IsAutoReorderEnabled = !SelectedRule.IsAutoReorderEnabled;
            await _analyticsService.SaveReorderRuleAsync(SelectedRule).ConfigureAwait(true);

            var status = SelectedRule.IsAutoReorderEnabled ? "enabled" : "disabled";
            await _dialogService.ShowMessageAsync("Success", $"Auto-reorder {status} for {SelectedRule.Product?.Name}.").ConfigureAwait(true);
        }, "Updating reorder rule...").ConfigureAwait(true);
    }

    /// <summary>
    /// Calculates Economic Order Quantity for the selected rule.
    /// </summary>
    [RelayCommand]
    private async Task CalculateEOQAsync()
    {
        if (EditingRule == null || SelectedProduct == null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            // EOQ = sqrt((2 * D * S) / H)
            // D = annual demand, S = ordering cost, H = holding cost
            // Using simplified calculation based on average daily sales
            var averageDailySales = EditingRule.AverageDailySales ?? 5; // Default to 5 units/day
            var annualDemand = averageDailySales * 365;
            var orderingCost = 500m; // Default ordering cost in KSh
            var holdingCostRate = 0.2m; // 20% of unit cost
            var unitCost = SelectedProduct.Cost ?? SelectedProduct.SellingPrice * 0.6m; // Assume 60% margin if no cost

            var holdingCost = unitCost * holdingCostRate;

            if (holdingCost > 0 && annualDemand > 0)
            {
                var eoq = (decimal)Math.Sqrt((double)(2 * annualDemand * orderingCost / holdingCost));
                EditingRule.EconomicOrderQuantity = Math.Round(eoq, 0);
                EditingRule.LastCalculatedDate = DateTime.UtcNow;

                await _dialogService.ShowMessageAsync("EOQ Calculated",
                    $"Economic Order Quantity: {EditingRule.EconomicOrderQuantity:N0} units").ConfigureAwait(true);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Calculation Error",
                    "Unable to calculate EOQ. Please ensure product cost is set.").ConfigureAwait(true);
            }
        }, "Calculating EOQ...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates rules for all products without rules.
    /// </summary>
    [RelayCommand]
    private async Task CreateBulkRulesAsync()
    {
        var productsWithoutRules = Products
            .Where(p => !_allRules.Any(r => r.ProductId == p.Id))
            .ToList();

        if (!productsWithoutRules.Any())
        {
            await _dialogService.ShowMessageAsync("Info", "All products already have reorder rules.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Create Bulk Rules",
            $"Create default reorder rules for {productsWithoutRules.Count} products without rules?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var storeId = _sessionService.CurrentStoreId ?? 1;
            var created = 0;

            foreach (var product in productsWithoutRules)
            {
                var rule = new ReorderRule
                {
                    StoreId = storeId,
                    ProductId = product.Id,
                    ReorderPoint = product.ReorderLevel > 0 ? product.ReorderLevel : 10,
                    ReorderQuantity = product.MinStockLevel > 0 ? (product.MinStockLevel.Value * 2) : 50,
                    MaxStockLevel = product.MaxStockLevel > 0 ? product.MaxStockLevel : null,
                    SafetyStock = 5,
                    LeadTimeDays = 7,
                    IsAutoReorderEnabled = false, // Disabled by default
                    ConsolidateReorders = true
                };

                await _analyticsService.SaveReorderRuleAsync(rule).ConfigureAwait(true);
                created++;
            }

            await _dialogService.ShowMessageAsync("Success",
                $"Created {created} reorder rules. Enable auto-reorder individually for each product.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Creating reorder rules...").ConfigureAwait(true);
    }

    /// <summary>
    /// Navigates back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        if (IsEditing)
        {
            CancelEdit();
        }
        else
        {
            _navigationService.GoBack();
        }
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategoryFilter = null;
        ShowOnlyEnabled = false;
    }
}
