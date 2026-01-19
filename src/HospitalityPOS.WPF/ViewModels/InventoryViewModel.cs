using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Converters;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the inventory management view.
/// Displays product stock levels with filtering and color-coded status.
/// </summary>
public partial class InventoryViewModel : ViewModelBase, INavigationAware
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IInventoryService _inventoryService;
    private readonly INavigationService _navigationService;

    private List<ProductStockViewModel> _allProducts = [];

    #region Observable Properties

    /// <summary>
    /// Gets or sets the filtered list of products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductStockViewModel> _products = [];

    /// <summary>
    /// Gets or sets the list of categories for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryDisplayItem> _categories = [];

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Gets or sets whether to show low stock items.
    /// </summary>
    [ObservableProperty]
    private bool _showLowStock = true;

    /// <summary>
    /// Gets or sets whether to show out-of-stock items.
    /// </summary>
    [ObservableProperty]
    private bool _showOutOfStock = true;

    /// <summary>
    /// Gets or sets whether to show items with adequate stock.
    /// </summary>
    [ObservableProperty]
    private bool _showInStock = true;

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    [ObservableProperty]
    private CategoryDisplayItem? _selectedCategory;

    /// <summary>
    /// Gets or sets the low stock count.
    /// </summary>
    [ObservableProperty]
    private int _lowStockCount;

    /// <summary>
    /// Gets or sets the out-of-stock count.
    /// </summary>
    [ObservableProperty]
    private int _outOfStockCount;

    /// <summary>
    /// Gets or sets the in-stock count.
    /// </summary>
    [ObservableProperty]
    private int _inStockCount;

    /// <summary>
    /// Gets or sets the total product count.
    /// </summary>
    [ObservableProperty]
    private int _totalProductCount;

    /// <summary>
    /// Gets or sets the selected product.
    /// </summary>
    [ObservableProperty]
    private ProductStockViewModel? _selectedProduct;

    /// <summary>
    /// Gets or sets whether the stock history panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isHistoryPanelVisible;

    /// <summary>
    /// Gets or sets the stock movements for the selected product.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<StockMovementViewModel> _stockMovements = [];

    /// <summary>
    /// Gets or sets the history start date filter.
    /// </summary>
    [ObservableProperty]
    private DateTime _historyStartDate = DateTime.Today.AddDays(-30);

    /// <summary>
    /// Gets or sets the history end date filter.
    /// </summary>
    [ObservableProperty]
    private DateTime _historyEndDate = DateTime.Today;

    /// <summary>
    /// Gets or sets whether history is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isHistoryLoading;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryViewModel"/> class.
    /// </summary>
    public InventoryViewModel(
        ILogger logger,
        IProductService productService,
        ICategoryService categoryService,
        IInventoryService inventoryService,
        INavigationService navigationService)
        : base(logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Inventory Management";
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(object? parameter)
    {
        try
        {
            await LoadDataAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load inventory data on navigation");
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowLowStockChanged(bool value) => ApplyFilters();
    partial void OnShowOutOfStockChanged(bool value) => ApplyFilters();
    partial void OnShowInStockChanged(bool value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(CategoryDisplayItem? value) => ApplyFilters();

    #region Commands

    /// <summary>
    /// Loads all data (categories and products).
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load categories for filter
            var categories = await _categoryService.GetCategoryTreeAsync();
            Categories = new ObservableCollection<CategoryDisplayItem>(FlattenCategories(categories));

            // Load all products with inventory
            var products = await _productService.GetAllProductsAsync();

            _allProducts = products
                .Where(p => p.TrackInventory)
                .Select(p => new ProductStockViewModel
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    CategoryId = p.CategoryId ?? 0,
                    CategoryName = p.Category?.Name ?? "Uncategorized",
                    CurrentStock = p.Inventory?.CurrentStock ?? 0,
                    MinStockLevel = p.MinStockLevel ?? 0,
                    MaxStockLevel = p.MaxStockLevel ?? 0,
                    StockUnit = p.UnitOfMeasure ?? "pcs",
                    StockStatus = GetStockStatus(p),
                    IsActive = p.IsActive
                })
                .OrderBy(p => p.StockStatus == StockStatus.OutOfStock ? 0 :
                              p.StockStatus == StockStatus.LowStock ? 1 : 2)
                .ThenBy(p => p.ProductName)
                .ToList();

            // Update counts from all products (not just filtered)
            UpdateCounts();
            TotalProductCount = _allProducts.Count;

            // Apply filters
            ApplyFilters();

            _logger.Debug("Loaded {ProductCount} products for inventory view", _allProducts.Count);
        }, "Loading inventory data...").ConfigureAwait(true);
    }

    /// <summary>
    /// Refreshes the inventory data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        ShowLowStock = true;
        ShowOutOfStock = true;
        ShowInStock = true;
        ApplyFilters();
    }

    /// <summary>
    /// Clears the search text.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    /// <summary>
    /// Clears the category filter.
    /// </summary>
    [RelayCommand]
    private void ClearCategoryFilter()
    {
        SelectedCategory = null;
    }

    /// <summary>
    /// Shows only low stock items.
    /// </summary>
    [RelayCommand]
    private void ShowOnlyLowStock()
    {
        ShowLowStock = true;
        ShowOutOfStock = false;
        ShowInStock = false;
    }

    /// <summary>
    /// Shows only out-of-stock items.
    /// </summary>
    [RelayCommand]
    private void ShowOnlyOutOfStock()
    {
        ShowLowStock = false;
        ShowOutOfStock = true;
        ShowInStock = false;
    }

    /// <summary>
    /// Shows all items with issues (low and out of stock).
    /// </summary>
    [RelayCommand]
    private void ShowAllIssues()
    {
        ShowLowStock = true;
        ShowOutOfStock = true;
        ShowInStock = false;
    }

    /// <summary>
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Views the stock history for the selected product.
    /// </summary>
    [RelayCommand]
    private async Task ViewStockHistoryAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        IsHistoryPanelVisible = true;
        await LoadStockHistoryAsync();
    }

    /// <summary>
    /// Closes the stock history panel.
    /// </summary>
    [RelayCommand]
    private void CloseHistoryPanel()
    {
        IsHistoryPanelVisible = false;
        StockMovements.Clear();
    }

    /// <summary>
    /// Loads stock history for the selected product.
    /// </summary>
    [RelayCommand]
    private async Task LoadStockHistoryAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        IsHistoryLoading = true;
        try
        {
            var movements = await _inventoryService.GetStockMovementsAsync(
                SelectedProduct.ProductId,
                HistoryStartDate,
                HistoryEndDate.AddDays(1)); // Include end date

            StockMovements = new ObservableCollection<StockMovementViewModel>(
                movements.OrderByDescending(m => m.CreatedAt).Select(m => new StockMovementViewModel
                {
                    Id = m.Id,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    PreviousStock = m.PreviousStock,
                    NewStock = m.NewStock,
                    ReferenceType = m.ReferenceType,
                    ReferenceId = m.ReferenceId,
                    Reason = m.Reason,
                    Notes = m.Notes,
                    UserName = m.User?.FullName ?? "System",
                    CreatedAt = m.CreatedAt
                }));

            _logger.Debug("Loaded {Count} stock movements for product {ProductId}",
                StockMovements.Count, SelectedProduct.ProductId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load stock history for product {ProductId}", SelectedProduct.ProductId);
        }
        finally
        {
            IsHistoryLoading = false;
        }
    }

    /// <summary>
    /// Opens the stock adjustment dialog for the selected product.
    /// </summary>
    [RelayCommand]
    private async Task AdjustStockAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Get the product entity
            var product = await _productService.GetByIdAsync(SelectedProduct.ProductId);
            if (product is null)
            {
                _logger.Warning("Product {ProductId} not found for adjustment", SelectedProduct.ProductId);
                return;
            }

            // Get adjustment reasons
            var reasons = await _inventoryService.GetAdjustmentReasonsAsync();

            // Show dialog
            var dialog = new Views.Dialogs.StockAdjustmentDialog(
                product,
                SelectedProduct.CurrentStock,
                reasons);

            if (dialog.ShowDialog() == true)
            {
                // Apply adjustment
                await _inventoryService.AdjustStockAsync(
                    product.Id,
                    dialog.NewStockQuantity,
                    dialog.SelectedReasonId ?? 0,
                    dialog.AdditionalNotes);

                _logger.Information(
                    "Stock adjusted for {ProductName}: {OldStock} -> {NewStock}, Reason: {Reason}",
                    product.Name,
                    SelectedProduct.CurrentStock,
                    dialog.NewStockQuantity,
                    dialog.SelectedReasonName);

                // Refresh the inventory list
                await LoadDataAsync();
            }
        }, "Processing stock adjustment...").ConfigureAwait(true);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Determines the stock status for a product.
    /// </summary>
    private static StockStatus GetStockStatus(Product product)
    {
        if (!product.TrackInventory)
            return StockStatus.InStock; // Not tracked, treat as OK

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        if (currentStock <= 0)
            return StockStatus.OutOfStock;

        if (minStock.HasValue && currentStock <= minStock.Value)
            return StockStatus.LowStock;

        return StockStatus.InStock;
    }

    /// <summary>
    /// Updates the stock counts from all products.
    /// </summary>
    private void UpdateCounts()
    {
        LowStockCount = _allProducts.Count(p => p.StockStatus == StockStatus.LowStock);
        OutOfStockCount = _allProducts.Count(p => p.StockStatus == StockStatus.OutOfStock);
        InStockCount = _allProducts.Count(p => p.StockStatus == StockStatus.InStock);
    }

    /// <summary>
    /// Applies current filters to the product list.
    /// </summary>
    private void ApplyFilters()
    {
        var filtered = _allProducts.Where(p =>
        {
            // Text search
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                if (!p.ProductName.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                    !p.ProductCode.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                    !p.CategoryName.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Category filter
            if (SelectedCategory != null && p.CategoryId != SelectedCategory.CategoryId)
                return false;

            // Status filter
            return p.StockStatus switch
            {
                StockStatus.LowStock => ShowLowStock,
                StockStatus.OutOfStock => ShowOutOfStock,
                StockStatus.InStock => ShowInStock,
                _ => true
            };
        });

        Products = new ObservableCollection<ProductStockViewModel>(filtered);
    }

    /// <summary>
    /// Flattens a hierarchical category tree into a flat list with indentation in display names.
    /// </summary>
    private static IEnumerable<CategoryDisplayItem> FlattenCategories(IEnumerable<Category> categories, int level = 0)
    {
        foreach (var category in categories)
        {
            yield return new CategoryDisplayItem(category, level);

            foreach (var child in FlattenCategories(category.SubCategories, level + 1))
            {
                yield return child;
            }
        }
    }

    #endregion
}

/// <summary>
/// Represents a product's stock information for display.
/// </summary>
public partial class ProductStockViewModel : ObservableObject
{

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock quantity.
    /// </summary>
    [ObservableProperty]
    private decimal _currentStock;

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal MinStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the stock unit.
    /// </summary>
    public string StockUnit { get; set; } = "pcs";

    /// <summary>
    /// Gets or sets the stock status.
    /// </summary>
    public StockStatus StockStatus { get; set; }

    /// <summary>
    /// Gets or sets whether the product is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets the stock display text.
    /// </summary>
    public string StockDisplay => $"{CurrentStock:N0} {StockUnit}";

    /// <summary>
    /// Gets the minimum level display text.
    /// </summary>
    public string MinDisplay => MinStockLevel > 0 ? $"{MinStockLevel:N0}" : "-";

    /// <summary>
    /// Gets the maximum level display text.
    /// </summary>
    public string MaxDisplay => MaxStockLevel > 0 ? $"{MaxStockLevel:N0}" : "-";

    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText => StockStatus switch
    {
        StockStatus.InStock => "OK",
        StockStatus.LowStock => "LOW",
        StockStatus.OutOfStock => "OUT",
        _ => "-"
    };

    /// <summary>
    /// Gets the status background brush.
    /// </summary>
    public Brush StatusBackground => StockStatus switch
    {
        StockStatus.InStock => StockColors.InStockBrush,
        StockStatus.LowStock => StockColors.LowStockBrush,
        StockStatus.OutOfStock => StockColors.OutOfStockBrush,
        _ => StockColors.InStockBrush
    };

    /// <summary>
    /// Gets the status icon text.
    /// </summary>
    public string StatusIcon => StockStatus switch
    {
        StockStatus.InStock => "[OK]",
        StockStatus.LowStock => "[!]",
        StockStatus.OutOfStock => "[X]",
        _ => ""
    };

    /// <summary>
    /// Gets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock => StockStatus == StockStatus.OutOfStock;

    /// <summary>
    /// Gets whether the product is low on stock.
    /// </summary>
    public bool IsLowStock => StockStatus == StockStatus.LowStock;
}

/// <summary>
/// Represents a category for display with indentation, while preserving original category reference.
/// </summary>
public class CategoryDisplayItem
{
    private readonly Category _category;
    private readonly int _level;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryDisplayItem"/> class.
    /// </summary>
    /// <param name="category">The original category entity.</param>
    /// <param name="level">The indentation level (0 for root categories).</param>
    public CategoryDisplayItem(Category category, int level)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _level = level;
    }

    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public int CategoryId => _category.Id;

    /// <summary>
    /// Gets the original category entity.
    /// </summary>
    public Category Category => _category;

    /// <summary>
    /// Gets the display name with indentation.
    /// </summary>
    public string DisplayName => new string(' ', _level * 4) + _category.Name;

    /// <summary>
    /// Gets the original category name without indentation.
    /// </summary>
    public string Name => _category.Name;

    /// <summary>
    /// Gets whether the category is active.
    /// </summary>
    public bool IsActive => _category.IsActive;

    /// <summary>
    /// Gets the indentation level.
    /// </summary>
    public int Level => _level;

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}

/// <summary>
/// Represents a stock movement for display in the history panel.
/// </summary>
public class StockMovementViewModel
{
    /// <summary>
    /// Gets or sets the movement ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movement type.
    /// </summary>
    public MovementType MovementType { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the previous stock level.
    /// </summary>
    public decimal PreviousStock { get; set; }

    /// <summary>
    /// Gets or sets the new stock level.
    /// </summary>
    public decimal NewStock { get; set; }

    /// <summary>
    /// Gets or sets the reference type.
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Gets or sets the reference ID.
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the user name who made the movement.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets the movement type display name.
    /// </summary>
    public string MovementTypeDisplay => MovementType switch
    {
        MovementType.Sale => "Sale",
        MovementType.Purchase => "Purchase",
        MovementType.PurchaseReceive => "Received",
        MovementType.Adjustment => "Adjustment",
        MovementType.StockTake => "Stock Take",
        MovementType.Transfer => "Transfer",
        MovementType.Void => "Void",
        MovementType.Return => "Return",
        MovementType.Waste => "Waste",
        _ => MovementType.ToString()
    };

    /// <summary>
    /// Gets the quantity display with sign.
    /// </summary>
    public string QuantityDisplay => MovementType switch
    {
        MovementType.Sale or MovementType.Waste or MovementType.Transfer => $"-{Quantity:N0}",
        MovementType.PurchaseReceive or MovementType.Return or MovementType.Void => $"+{Quantity:N0}",
        MovementType.Adjustment or MovementType.StockTake => NewStock > PreviousStock ? $"+{Math.Abs(Quantity):N0}" : $"-{Math.Abs(Quantity):N0}",
        _ => $"{Quantity:N0}"
    };

    /// <summary>
    /// Gets whether this is an incoming movement (increases stock).
    /// </summary>
    public bool IsIncoming => MovementType is MovementType.PurchaseReceive or MovementType.Return or MovementType.Void
        || (MovementType == MovementType.Adjustment && NewStock > PreviousStock)
        || (MovementType == MovementType.StockTake && NewStock > PreviousStock);

    /// <summary>
    /// Gets the movement icon.
    /// </summary>
    public string MovementIcon => IsIncoming ? "\uE74A" : "\uE74B"; // Up arrow or Down arrow

    /// <summary>
    /// Gets the reference display text.
    /// </summary>
    public string ReferenceDisplay => !string.IsNullOrEmpty(ReferenceType)
        ? $"{ReferenceType} #{ReferenceId}"
        : Reason ?? "-";

    /// <summary>
    /// Gets the date display.
    /// </summary>
    public string DateDisplay => CreatedAt.ToString("dd MMM yyyy HH:mm");
}
