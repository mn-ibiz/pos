using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the stock alert dashboard widget.
/// Displays low stock and out-of-stock product alerts.
/// </summary>
public partial class StockAlertWidgetViewModel : ViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of low stock products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductStockAlert> _lowStockItems = [];

    /// <summary>
    /// Gets or sets the list of out-of-stock products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductStockAlert> _outOfStockItems = [];

    /// <summary>
    /// Gets or sets the count of low stock items.
    /// </summary>
    [ObservableProperty]
    private int _lowStockCount;

    /// <summary>
    /// Gets or sets the count of out-of-stock items.
    /// </summary>
    [ObservableProperty]
    private int _outOfStockCount;

    /// <summary>
    /// Gets or sets the total alerts count.
    /// </summary>
    [ObservableProperty]
    private int _totalAlerts;

    /// <summary>
    /// Gets or sets whether the widget has any alerts to show.
    /// </summary>
    [ObservableProperty]
    private bool _hasAlerts;

    /// <summary>
    /// Gets or sets whether the widget is currently expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="StockAlertWidgetViewModel"/> class.
    /// </summary>
    public StockAlertWidgetViewModel(
        ILogger logger,
        IInventoryService inventoryService,
        INavigationService navigationService)
        : base(logger)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Stock Alerts";
    }

    #region Commands

    /// <summary>
    /// Loads the stock alert data.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load low stock products
            var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();
            LowStockItems = new ObservableCollection<ProductStockAlert>(
                lowStockProducts.Select(p => new ProductStockAlert
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? "Uncategorized",
                    CurrentStock = p.Inventory?.CurrentStock ?? 0,
                    MinStockLevel = p.MinStockLevel ?? 0,
                    StockUnit = p.UnitOfMeasure ?? "pcs"
                }));

            // Load out-of-stock products
            var outOfStockProducts = await _inventoryService.GetOutOfStockProductsAsync();
            OutOfStockItems = new ObservableCollection<ProductStockAlert>(
                outOfStockProducts.Select(p => new ProductStockAlert
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? "Uncategorized",
                    CurrentStock = 0,
                    MinStockLevel = p.MinStockLevel ?? 0,
                    StockUnit = p.UnitOfMeasure ?? "pcs"
                }));

            // Update counts
            LowStockCount = LowStockItems.Count;
            OutOfStockCount = OutOfStockItems.Count;
            TotalAlerts = LowStockCount + OutOfStockCount;
            HasAlerts = TotalAlerts > 0;

            _logger.Debug("Loaded {LowStockCount} low stock items and {OutOfStockCount} out-of-stock items",
                LowStockCount, OutOfStockCount);
        }, "Loading stock alerts...").ConfigureAwait(true);
    }

    /// <summary>
    /// Refreshes the stock alert data.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    /// <summary>
    /// Navigates to the full inventory view.
    /// </summary>
    [RelayCommand]
    public void ViewInventory()
    {
        _navigationService.NavigateTo<InventoryViewModel>();
    }

    /// <summary>
    /// Toggles the expanded state of the widget.
    /// </summary>
    [RelayCommand]
    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    #endregion
}

/// <summary>
/// Represents a product stock alert item for display.
/// </summary>
public partial class ProductStockAlert : ObservableObject
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

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
    /// Gets or sets the stock unit (e.g., "pcs", "kg").
    /// </summary>
    public string StockUnit { get; set; } = "pcs";

    /// <summary>
    /// Gets the display text for remaining stock.
    /// </summary>
    public string RemainingText => CurrentStock > 0
        ? $"{CurrentStock:N0} {StockUnit ?? "pcs"} remaining"
        : "Out of stock";

    /// <summary>
    /// Gets the stock display text.
    /// </summary>
    public string StockDisplay => $"{CurrentStock:N0} {StockUnit}";

    /// <summary>
    /// Gets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock => CurrentStock <= 0;
}
