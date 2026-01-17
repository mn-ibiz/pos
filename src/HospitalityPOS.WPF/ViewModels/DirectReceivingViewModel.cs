using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for direct goods receiving (without PO).
/// </summary>
public partial class DirectReceivingViewModel : ViewModelBase, INavigationAware
{
    private readonly IGoodsReceivingService _goodsReceivingService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the list of available suppliers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    /// <summary>
    /// Gets or sets the selected supplier (optional).
    /// </summary>
    [ObservableProperty]
    private Supplier? _selectedSupplier;

    /// <summary>
    /// Gets or sets the supplier delivery note number.
    /// </summary>
    [ObservableProperty]
    private string _deliveryNoteNumber = string.Empty;

    /// <summary>
    /// Gets or sets the search text for product lookup.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Gets or sets the product search results.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Product> _searchResults = new();

    /// <summary>
    /// Gets or sets a value indicating whether search results are visible.
    /// </summary>
    [ObservableProperty]
    private bool _showSearchResults;

    /// <summary>
    /// Gets or sets the items to receive.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DirectReceiveItemViewModel> _items = new();

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    [ObservableProperty]
    private decimal _totalAmount;

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>
    /// Gets the count of items to receive.
    /// </summary>
    public int ItemCount => Items.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectReceivingViewModel"/> class.
    /// </summary>
    public DirectReceivingViewModel(
        IGoodsReceivingService goodsReceivingService,
        INavigationService navigationService,
        ILogger logger)
        : base(logger)
    {
        _goodsReceivingService = goodsReceivingService ?? throw new ArgumentNullException(nameof(goodsReceivingService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Direct Goods Receiving";
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

    /// <summary>
    /// Loads suppliers and initializes the form.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var suppliers = await _goodsReceivingService.GetActiveSuppliersAsync();
            Suppliers = new ObservableCollection<Supplier>(suppliers);
            ClearForm();
        }, "Loading...");
    }

    /// <summary>
    /// Called when the search text changes.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            SearchResults.Clear();
            ShowSearchResults = false;
            return;
        }

        _ = SearchProductsAsync(value);
    }

    /// <summary>
    /// Searches for products matching the search text.
    /// </summary>
    private async Task SearchProductsAsync(string searchText)
    {
        try
        {
            var products = await _goodsReceivingService.SearchProductsAsync(searchText, 10);
            SearchResults = new ObservableCollection<Product>(products);
            ShowSearchResults = SearchResults.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error searching products");
            ShowSearchResults = false;
        }
    }

    /// <summary>
    /// Adds a product to the receiving list.
    /// </summary>
    [RelayCommand]
    private void AddProduct(Product product)
    {
        if (product == null) return;

        // Check if already in list
        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            var item = new DirectReceiveItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Code,
                CurrentStock = product.Inventory?.CurrentStock ?? 0,
                UnitOfMeasure = product.UnitOfMeasure,
                Quantity = 1,
                UnitCost = product.CostPrice ?? 0
            };

            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DirectReceiveItemViewModel.TotalCost) ||
                    e.PropertyName == nameof(DirectReceiveItemViewModel.Quantity))
                {
                    RecalculateTotal();
                }
            };

            Items.Add(item);
        }

        // Clear search
        SearchText = string.Empty;
        SearchResults.Clear();
        ShowSearchResults = false;

        RecalculateTotal();
        OnPropertyChanged(nameof(ItemCount));
    }

    /// <summary>
    /// Removes an item from the receiving list.
    /// </summary>
    [RelayCommand]
    private void RemoveItem(DirectReceiveItemViewModel item)
    {
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotal();
            OnPropertyChanged(nameof(ItemCount));
        }
    }

    /// <summary>
    /// Recalculates the total amount.
    /// </summary>
    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.TotalCost);
    }

    /// <summary>
    /// Clears the supplier selection.
    /// </summary>
    [RelayCommand]
    private void ClearSupplier()
    {
        SelectedSupplier = null;
    }

    /// <summary>
    /// Hides the search results popup.
    /// </summary>
    [RelayCommand]
    private void HideSearchResults()
    {
        ShowSearchResults = false;
    }

    /// <summary>
    /// Receives the goods and updates stock.
    /// </summary>
    [RelayCommand]
    private async Task ReceiveGoodsAsync()
    {
        if (!Items.Any())
        {
            await DialogService.ShowErrorAsync("Error", "Please add items to receive.");
            return;
        }

        // Validate quantities
        var invalidItems = Items.Where(i => i.Quantity <= 0).ToList();
        if (invalidItems.Any())
        {
            await DialogService.ShowErrorAsync("Error", "All items must have a quantity greater than 0.");
            return;
        }

        // Confirm
        var supplierName = SelectedSupplier?.Name ?? "No Supplier";
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Confirm Direct Receiving",
            $"Receive {Items.Count} item(s) from {supplierName} with a total value of KSh {TotalAmount:N2}?");

        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var grnItemInputs = Items.Select(i => new GRNItemInput
            {
                PurchaseOrderItemId = null,
                ProductId = i.ProductId,
                OrderedQuantity = 0,
                ReceivedQuantity = i.Quantity,
                UnitCost = i.UnitCost,
                Notes = i.Notes
            });

            var grn = await _goodsReceivingService.ReceiveDirectAsync(
                SelectedSupplier?.Id,
                string.IsNullOrWhiteSpace(DeliveryNoteNumber) ? null : DeliveryNoteNumber,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                grnItemInputs);

            await DialogService.ShowInfoAsync("Success",
                $"Goods received successfully.\n\nGRN: {grn.GRNNumber}\n\n" +
                "This was a direct receiving without a purchase order.");

            // Clear form for next entry
            ClearForm();

        }, "Receiving goods and updating stock...");
    }

    /// <summary>
    /// Clears the form for a new entry.
    /// </summary>
    private void ClearForm()
    {
        SelectedSupplier = null;
        DeliveryNoteNumber = string.Empty;
        SearchText = string.Empty;
        SearchResults.Clear();
        ShowSearchResults = false;
        Items.Clear();
        Notes = string.Empty;
        TotalAmount = 0;
        OnPropertyChanged(nameof(ItemCount));
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

/// <summary>
/// ViewModel for a direct receive line item.
/// </summary>
public partial class DirectReceiveItemViewModel : ObservableObject
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
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Gets or sets the quantity to receive.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _quantity;

    /// <summary>
    /// Gets or sets the unit cost.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _unitCost;

    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    [ObservableProperty]
    private string? _notes;

    /// <summary>
    /// Gets the total cost (Quantity * UnitCost).
    /// </summary>
    public decimal TotalCost => Quantity * UnitCost;

    /// <summary>
    /// Called when Quantity changes.
    /// </summary>
    partial void OnQuantityChanged(decimal value)
    {
        if (value < 0)
        {
            Quantity = 0;
        }
    }
}
