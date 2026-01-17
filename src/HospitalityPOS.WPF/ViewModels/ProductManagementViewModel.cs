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
/// ViewModel for the product management screen.
/// </summary>
public partial class ProductManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    /// <summary>
    /// Gets or sets the list of categories for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Gets or sets the selected product.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
    private Product? _selectedProduct;

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    [ObservableProperty]
    private Category? _selectedCategory;

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Gets or sets whether to show inactive products.
    /// </summary>
    [ObservableProperty]
    private bool _showInactiveProducts;

    /// <summary>
    /// Gets the total count of products.
    /// </summary>
    [ObservableProperty]
    private int _totalProductCount;

    /// <summary>
    /// Gets the count of active products.
    /// </summary>
    [ObservableProperty]
    private int _activeProductCount;

    /// <summary>
    /// Gets the count of low stock products.
    /// </summary>
    [ObservableProperty]
    private int _lowStockCount;

    /// <summary>
    /// Gets the count of out of stock products.
    /// </summary>
    [ObservableProperty]
    private int _outOfStockCount;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductManagementViewModel"/> class.
    /// </summary>
    public ProductManagementViewModel(
        ILogger logger,
        IProductService productService,
        ICategoryService categoryService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Product Management";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnShowInactiveProductsChanged(bool value)
    {
        _ = LoadProductsAsync();
    }

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
            Categories = new ObservableCollection<Category>(FlattenCategories(categories));

            // Load products
            await LoadProductsAsync();

            // Load stats
            var lowStockProducts = await _productService.GetLowStockProductsAsync();
            LowStockCount = lowStockProducts.Count;

            _logger.Debug("Loaded {CategoryCount} categories and {ProductCount} products",
                Categories.Count, Products.Count);
        }, "Loading data...").ConfigureAwait(true);
    }

    /// <summary>
    /// Loads products based on current filters.
    /// </summary>
    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        await ExecuteAsync(async () =>
        {
            IReadOnlyList<Product> products;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                products = await _productService.SearchAsync(
                    SearchText,
                    SelectedCategory?.Id,
                    !ShowInactiveProducts);
            }
            else if (SelectedCategory is not null)
            {
                products = await _productService.GetByCategoryAsync(
                    SelectedCategory.Id,
                    ShowInactiveProducts);
            }
            else
            {
                products = ShowInactiveProducts
                    ? await _productService.GetAllProductsAsync()
                    : await _productService.GetActiveProductsAsync();
            }

            Products = new ObservableCollection<Product>(products);
            TotalProductCount = Products.Count;
            ActiveProductCount = Products.Count(p => p.IsActive);
            OutOfStockCount = Products.Count(p => p.Inventory is null || p.Inventory.CurrentStock <= 0);
        }, "Loading products...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [RelayCommand]
    private async Task CreateProductAsync()
    {
        if (!RequirePermission(PermissionNames.Products.Create, "create products"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to create products.");
            return;
        }

        var result = await _dialogService.ShowProductEditorDialogAsync(null, SelectedCategory?.Id);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new CreateProductDto
                {
                    Code = result.Code,
                    Name = result.Name,
                    Description = result.Description,
                    CategoryId = result.CategoryId,
                    SellingPrice = result.SellingPrice,
                    CostPrice = result.CostPrice,
                    TaxRate = result.TaxRate,
                    UnitOfMeasure = result.UnitOfMeasure,
                    ImagePath = result.ImagePath,
                    Barcode = result.Barcode,
                    MinStockLevel = result.MinStockLevel,
                    MaxStockLevel = result.MaxStockLevel,
                    IsActive = result.IsActive,
                    InitialStock = result.InitialStock
                };

                await _productService.CreateProductAsync(dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Product '{result.Name}' has been created.");
                await LoadProductsAsync();
            }, "Creating product...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Edits the selected product.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditProduct))]
    private async Task EditProductAsync()
    {
        if (SelectedProduct is null) return;

        if (!RequirePermission(PermissionNames.Products.Edit, "edit products"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to edit products.");
            return;
        }

        var result = await _dialogService.ShowProductEditorDialogAsync(SelectedProduct, SelectedProduct.CategoryId);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new UpdateProductDto
                {
                    Code = result.Code,
                    Name = result.Name,
                    Description = result.Description,
                    CategoryId = result.CategoryId,
                    SellingPrice = result.SellingPrice,
                    CostPrice = result.CostPrice,
                    TaxRate = result.TaxRate,
                    UnitOfMeasure = result.UnitOfMeasure,
                    ImagePath = result.ImagePath,
                    Barcode = result.Barcode,
                    MinStockLevel = result.MinStockLevel,
                    MaxStockLevel = result.MaxStockLevel,
                    IsActive = result.IsActive
                };

                await _productService.UpdateProductAsync(SelectedProduct.Id, dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Product '{result.Name}' has been updated.");
                await LoadProductsAsync();
            }, "Updating product...").ConfigureAwait(true);
        }
    }

    private bool CanEditProduct() => SelectedProduct is not null;

    /// <summary>
    /// Deletes the selected product.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteProduct))]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct is null) return;

        if (!RequirePermission(PermissionNames.Products.Delete, "delete products"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to delete products.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Product",
            $"Are you sure you want to delete the product '{SelectedProduct.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            try
            {
                var deleted = await _productService.DeleteProductAsync(SelectedProduct.Id, SessionService.CurrentUserId);
                if (deleted)
                {
                    await _dialogService.ShowMessageAsync(
                        "Product Deleted",
                        $"Product '{SelectedProduct.Name}' has been deleted.");
                    await LoadProductsAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        "Delete Failed",
                        "Failed to delete the product. It may have already been deleted.");
                }
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowErrorAsync("Cannot Delete", ex.Message);
            }
        }, "Deleting product...").ConfigureAwait(true);
    }

    private bool CanDeleteProduct() => SelectedProduct is not null;

    /// <summary>
    /// Toggles the active status of the selected product.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleActive))]
    private async Task ToggleActiveAsync()
    {
        if (SelectedProduct is null) return;

        if (!RequirePermission(PermissionNames.Products.Edit, "change product status"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to change product status.");
            return;
        }

        var newStatus = !SelectedProduct.IsActive;
        var action = newStatus ? "activate" : "deactivate";

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"{(newStatus ? "Activate" : "Deactivate")} Product",
            $"Are you sure you want to {action} the product '{SelectedProduct.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _productService.SetProductActiveAsync(SelectedProduct.Id, newStatus, SessionService.CurrentUserId);
            await _dialogService.ShowMessageAsync(
                "Success",
                $"Product '{SelectedProduct.Name}' has been {(newStatus ? "activated" : "deactivated")}.");
            await LoadProductsAsync();
        }, $"{(newStatus ? "Activating" : "Deactivating")} product...").ConfigureAwait(true);
    }

    private bool CanToggleActive() => SelectedProduct is not null;

    /// <summary>
    /// Clears the category filter.
    /// </summary>
    [RelayCommand]
    private void ClearCategoryFilter()
    {
        SelectedCategory = null;
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
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Flattens a hierarchical category tree into a flat list with indentation in names.
    /// </summary>
    private static IEnumerable<Category> FlattenCategories(IEnumerable<Category> categories, int level = 0)
    {
        foreach (var category in categories)
        {
            // Create a display-friendly name with indentation
            var displayCategory = new Category
            {
                Id = category.Id,
                Name = new string(' ', level * 4) + category.Name,
                IsActive = category.IsActive
            };
            yield return displayCategory;

            foreach (var child in FlattenCategories(category.SubCategories, level + 1))
            {
                yield return child;
            }
        }
    }

    #endregion
}
