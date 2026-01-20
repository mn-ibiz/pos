using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Windows.Threading;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels.Models;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the main POS screen.
/// </summary>
public partial class POSViewModel : ViewModelBase, INavigationAware
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IImageService _imageService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly IOrderService _orderService;
    private readonly IReceiptService _receiptService;
    private readonly ISessionService _sessionService;
    private readonly IKitchenPrintService _kitchenPrintService;
    private readonly IOfferService _offerService;
    private readonly IUiShellService _uiShellService;
    private readonly IBarcodeService _barcodeService;
    private readonly IMpesaService? _mpesaService;
    private readonly ILoyaltyService? _loyaltyService;

    private const int ItemsPerPage = 15;
    private const decimal TaxRate = 0.16m; // Kenya VAT 16%
    private const string AutoSaveFileName = "current_order.json";
    private const int AutoSaveStalenessHours = 4;

    private static string AutoSaveFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HospitalityPOS",
        "Data");

    private List<Product> _allProducts = [];
    private int _currentPage = 1;
    private int _totalPages = 1;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of categories.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> _categories = [];

    /// <summary>
    /// Gets or sets the selected category.
    /// </summary>
    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    /// <summary>
    /// Gets or sets the list of products to display in the grid.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductTileViewModel> _products = [];

    /// <summary>
    /// Gets or sets the user's favorite products for quick access.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFavorites))]
    private ObservableCollection<ProductTileViewModel> _favorites = [];

    /// <summary>
    /// Gets whether the user has any favorites.
    /// </summary>
    public bool HasFavorites => Favorites.Count > 0;

    /// <summary>
    /// Gets or sets whether the favorites panel is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isFavoritesPanelExpanded = true;

    /// <summary>
    /// Gets or sets the current order items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<OrderItemViewModel> _orderItems = [];

    /// <summary>
    /// Gets the order subtotal.
    /// </summary>
    [ObservableProperty]
    private decimal _orderSubtotal;

    /// <summary>
    /// Gets the order tax.
    /// </summary>
    [ObservableProperty]
    private decimal _orderTax;

    /// <summary>
    /// Gets the order total.
    /// </summary>
    [ObservableProperty]
    private decimal _orderTotal;

    /// <summary>
    /// Gets the total discount applied to the order.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrderDiscount))]
    private decimal _orderDiscount;

    /// <summary>
    /// Gets whether any discount is applied to the order.
    /// </summary>
    public bool HasOrderDiscount => OrderDiscount > 0;

    // ==================== Profit Margin Properties ====================

    /// <summary>
    /// Gets the total profit margin for the order.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrderProfit))]
    private decimal _orderProfit;

    /// <summary>
    /// Gets the profit margin percentage for the order.
    /// </summary>
    [ObservableProperty]
    private decimal _orderProfitPercent;

    /// <summary>
    /// Gets whether the order has profit margin data available.
    /// </summary>
    public bool HasOrderProfit => OrderItems.Any(oi => oi.HasCostPrice);

    // ==================== Business Mode Properties ====================

    /// <summary>
    /// Gets whether the system is in Supermarket mode.
    /// </summary>
    public bool IsSupermarketMode => _uiShellService?.CurrentMode == BusinessMode.Supermarket;

    /// <summary>
    /// Gets whether the system is in Restaurant mode.
    /// </summary>
    public bool IsRestaurantMode => _uiShellService?.CurrentMode == BusinessMode.Restaurant;

    /// <summary>
    /// Gets whether table management UI should be shown.
    /// </summary>
    public bool ShowTableManagement => _uiShellService?.ShouldShowTableManagement ?? false;

    /// <summary>
    /// Gets whether barcode search should be prominently displayed.
    /// </summary>
    public bool ShowBarcodeSearch => _uiShellService?.ShouldAutoFocusBarcode ?? false;

    /// <summary>
    /// Gets the search input placeholder text based on mode.
    /// </summary>
    public string SearchPlaceholder => IsSupermarketMode
        ? "Scan barcode or search products..."
        : "Search products...";

    /// <summary>
    /// Gets the order header label based on mode.
    /// </summary>
    public string OrderHeaderLabel => IsSupermarketMode ? "Current Sale" : "Current Order";

    /// <summary>
    /// Gets or sets the barcode/search input.
    /// </summary>
    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    #region Real-Time Search Properties

    /// <summary>
    /// Debounce timer for search input.
    /// </summary>
    private DispatcherTimer? _searchDebounceTimer;

    /// <summary>
    /// Gets or sets the search results for the dropdown.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductSearchResult> _searchResults = [];

    /// <summary>
    /// Gets or sets the selected search result.
    /// </summary>
    [ObservableProperty]
    private ProductSearchResult? _selectedSearchResult;

    /// <summary>
    /// Gets or sets whether the search dropdown is open.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchDropdownOpen;

    /// <summary>
    /// Gets or sets whether a search is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoSearchResults))]
    private bool _isSearching;

    /// <summary>
    /// Gets whether there are no search results.
    /// </summary>
    public bool HasNoSearchResults => !IsSearching && SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(BarcodeInput) && BarcodeInput.Length >= 2;

    #endregion

    /// <summary>
    /// Gets or sets the numeric keypad input buffer.
    /// </summary>
    [ObservableProperty]
    private string _keypadInput = "";

    /// <summary>
    /// Gets the pending quantity display text.
    /// </summary>
    public string PendingQuantityDisplay => string.IsNullOrEmpty(KeypadInput) ? "" : $"Qty: {KeypadInput}";

    /// <summary>
    /// Gets whether there's a pending quantity to apply.
    /// </summary>
    public bool HasPendingQuantity => !string.IsNullOrEmpty(KeypadInput);

    /// <summary>
    /// Gets the total savings from applied offers.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOfferSavings))]
    private decimal _orderSavings;

    /// <summary>
    /// Gets whether any offer savings are applied to the order.
    /// </summary>
    public bool HasOfferSavings => OrderSavings > 0;

    /// <summary>
    /// Gets the count of items with offers applied.
    /// </summary>
    public int OfferItemsCount => OrderItems?.Count(oi => oi.HasOfferApplied) ?? 0;

    /// <summary>
    /// Gets or sets the current page numbers for pagination.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<int> _pageNumbers = [];

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber = 1;

    /// <summary>
    /// Gets or sets the table name/number.
    /// </summary>
    [ObservableProperty]
    private string _tableName = "Counter";

    /// <summary>
    /// Gets or sets the customer name for the order.
    /// </summary>
    [ObservableProperty]
    private string _customerName = "";

    /// <summary>
    /// Gets or sets the order notes/special instructions.
    /// </summary>
    [ObservableProperty]
    private string _orderNotes = "";

    /// <summary>
    /// Gets or sets the current user display name.
    /// </summary>
    [ObservableProperty]
    private string _currentUserName = "";

    /// <summary>
    /// Gets or sets the current order ID (null for new orders).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingExistingOrder))]
    [NotifyPropertyChangedFor(nameof(CanHoldOrder))]
    private int? _currentOrderId;

    /// <summary>
    /// Gets or sets the current order number being edited.
    /// </summary>
    [ObservableProperty]
    private string _currentOrderNumber = "";

    /// <summary>
    /// Gets or sets the current receipt ID (set after order is printed and receipt is created).
    /// </summary>
    [ObservableProperty]
    private int? _currentReceiptId;

    /// <summary>
    /// Gets or sets the current receipt number.
    /// </summary>
    [ObservableProperty]
    private string _currentReceiptNumber = "";

    /// <summary>
    /// Gets or sets the list of held orders.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HeldOrderViewModel> _heldOrders = [];

    /// <summary>
    /// Gets or sets whether the held orders panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isHeldOrdersPanelVisible;

    /// <summary>
    /// Gets whether we're editing an existing order vs creating a new one.
    /// </summary>
    public bool IsEditingExistingOrder => CurrentOrderId.HasValue;

    /// <summary>
    /// Gets whether the order can be held (must have items and be new or already saved).
    /// </summary>
    public bool CanHoldOrder => OrderItems.Any() && CurrentOrderId.HasValue;

    // ==================== Loyalty Member Properties ====================

    /// <summary>
    /// Gets or sets the attached loyalty member for this order.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoyaltyMember))]
    [NotifyPropertyChangedFor(nameof(LoyaltyMemberDisplay))]
    private LoyaltyMemberDto? _attachedLoyaltyMember;

    /// <summary>
    /// Gets whether a loyalty member is attached to this order.
    /// </summary>
    public bool HasLoyaltyMember => AttachedLoyaltyMember != null;

    /// <summary>
    /// Gets the display string for the attached loyalty member.
    /// </summary>
    public string LoyaltyMemberDisplay => AttachedLoyaltyMember != null
        ? $"{AttachedLoyaltyMember.Name ?? AttachedLoyaltyMember.PhoneNumber} | {AttachedLoyaltyMember.PointsBalance:N0} pts | {AttachedLoyaltyMember.Tier}"
        : "No member attached";

    /// <summary>
    /// Gets or sets the phone number being searched for loyalty lookup.
    /// </summary>
    [ObservableProperty]
    private string _loyaltySearchPhone = string.Empty;

    /// <summary>
    /// Gets or sets whether the loyalty panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isLoyaltyPanelVisible;

    /// <summary>
    /// Gets or sets the loyalty search results.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LoyaltyMemberDto> _loyaltySearchResults = [];

    /// <summary>
    /// Gets or sets the estimated points to be earned for this order.
    /// </summary>
    [ObservableProperty]
    private decimal _estimatedPointsToEarn;

    /// <summary>
    /// Gets or sets the points being redeemed on this order.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPointsRedemption))]
    [NotifyPropertyChangedFor(nameof(PointsRedemptionValue))]
    private decimal _pointsToRedeem;

    /// <summary>
    /// Gets whether points are being redeemed.
    /// </summary>
    public bool HasPointsRedemption => PointsToRedeem > 0;

    /// <summary>
    /// Gets or sets the KES value of points being redeemed.
    /// </summary>
    [ObservableProperty]
    private decimal _pointsRedemptionValue;

    /// <summary>
    /// Gets or sets the maximum redeemable points for current order.
    /// </summary>
    [ObservableProperty]
    private decimal _maxRedeemablePoints;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="POSViewModel"/> class.
    /// </summary>
    public POSViewModel(
        ILogger logger,
        IProductService productService,
        ICategoryService categoryService,
        IImageService imageService,
        INavigationService navigationService,
        IDialogService dialogService,
        IWorkPeriodService workPeriodService,
        IOrderService orderService,
        IReceiptService receiptService,
        ISessionService sessionService,
        IKitchenPrintService kitchenPrintService,
        IOfferService offerService,
        IUiShellService uiShellService,
        IBarcodeService barcodeService,
        IMpesaService? mpesaService = null,
        ILoyaltyService? loyaltyService = null)
        : base(logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _kitchenPrintService = kitchenPrintService ?? throw new ArgumentNullException(nameof(kitchenPrintService));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _uiShellService = uiShellService ?? throw new ArgumentNullException(nameof(uiShellService));
        _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
        _mpesaService = mpesaService; // Optional - may not be configured
        _loyaltyService = loyaltyService; // Optional - loyalty program

        Title = IsSupermarketMode ? "Point of Sale - Retail" : "Point of Sale";
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(object? parameter)
    {
        try
        {
            // Refresh UI shell service to ensure correct business mode
            await _uiShellService.RefreshAsync();

            // Notify mode-dependent properties after refresh
            OnPropertyChanged(nameof(IsSupermarketMode));
            OnPropertyChanged(nameof(IsRestaurantMode));
            OnPropertyChanged(nameof(ShowTableManagement));
            OnPropertyChanged(nameof(ShowBarcodeSearch));
            OnPropertyChanged(nameof(SearchPlaceholder));
            OnPropertyChanged(nameof(OrderHeaderLabel));

            // Update title based on mode
            Title = IsSupermarketMode ? "Point of Sale - Retail" : "Point of Sale";

            // Verify work period is open
            var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync();
            if (currentPeriod is null)
            {
                await _dialogService.ShowErrorAsync(
                    "Work Period Required",
                    "A work period must be open to use the POS.\n\nPlease log in through Admin mode to open a work period first.");

                // Navigate back to mode selection - don't use GoBack() as history may be empty
                ModeSelectionViewModel.SelectedLoginMode = LoginMode.None;
                _sessionService.ClearSession(LogoutReason.UserInitiated);
                _navigationService.NavigateTo<ModeSelectionViewModel>();
                _navigationService.ClearHistory();
                return;
            }

            // Set current user name
            CurrentUserName = _sessionService.CurrentUser?.FullName ?? "Unknown";

            await LoadDataAsync();

            // Try to recover any auto-saved order
            await RecoverAutoSavedOrderAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize POS screen");
            await _dialogService.ShowErrorAsync("Error", "Failed to load POS screen. Please try again.");
            _navigationService.GoBack();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        if (value is not null)
        {
            // Deselect all other categories
            foreach (var cat in Categories)
            {
                cat.IsSelected = cat == value;
            }

            // Fire-and-forget with proper exception handling
            _ = FilterProductsByCategoryAsync(value.Id).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                {
                    _logger.Error(t.Exception, "Failed to filter products by category {CategoryId}", value.Id);
                }
            }, TaskScheduler.Default);
        }
    }

    partial void OnTableNameChanged(string value)
    {
        AutoSaveOrder();
    }

    partial void OnCustomerNameChanged(string value)
    {
        AutoSaveOrder();
    }

    partial void OnOrderNotesChanged(string value)
    {
        AutoSaveOrder();
    }

    #region Commands

    /// <summary>
    /// Loads all data.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load categories
            var categories = await _categoryService.GetActiveCategoriesAsync();
            var categoryVms = new List<CategoryViewModel>
            {
                new() { Id = 0, Name = "All", IsSelected = true }
            };
            categoryVms.AddRange(categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IsSelected = false
            }));
            Categories = new ObservableCollection<CategoryViewModel>(categoryVms);
            SelectedCategory = Categories.FirstOrDefault();

            // Load all active products
            _allProducts = (await _productService.GetActiveProductsAsync()).ToList();

            // Load favorites
            await LoadFavoritesAsync();

            // Display products
            RefreshProductGrid();

            _logger.Debug("POS loaded {CategoryCount} categories and {ProductCount} products",
                Categories.Count, _allProducts.Count);
        }, "Loading POS...").ConfigureAwait(true);
    }

    /// <summary>
    /// Loads the user's favorite products.
    /// </summary>
    private async Task LoadFavoritesAsync()
    {
        var userId = _sessionService.CurrentUserId;
        if (userId == 0) return;

        var favoriteProducts = await _productService.GetFavoritesAsync(userId);
        var favoriteVms = new List<ProductTileViewModel>();

        foreach (var product in favoriteProducts)
        {
            var vm = CreateProductTileFromEntity(product);
            vm.IsFavorite = true;
            favoriteVms.Add(vm);
        }

        Favorites = new ObservableCollection<ProductTileViewModel>(favoriteVms);
        OnPropertyChanged(nameof(HasFavorites));
    }

    /// <summary>
    /// Filters products by category.
    /// </summary>
    private async Task FilterProductsByCategoryAsync(int categoryId)
    {
        await ExecuteAsync(async () =>
        {
            if (categoryId == 0)
            {
                // "All" category - show all active products
                _allProducts = (await _productService.GetActiveProductsAsync()).ToList();
            }
            else
            {
                // Filter by specific category
                _allProducts = (await _productService.GetByCategoryAsync(categoryId, includeInactive: false)).ToList();
            }

            _currentPage = 1;
            RefreshProductGrid();
        }, "Loading products...").ConfigureAwait(true);
    }

    /// <summary>
    /// Selects a category.
    /// </summary>
    [RelayCommand]
    private void SelectCategory(CategoryViewModel category)
    {
        SelectedCategory = category;
    }

    /// <summary>
    /// Handles barcode/product search input (triggered on Enter key).
    /// </summary>
    [RelayCommand]
    private async Task SearchBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
        {
            // Empty input - open product lookup dialog
            await OpenProductSearchDialogAsync(null);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var searchTerm = BarcodeInput.Trim();

            // First, try to find by exact barcode match
            var product = await _barcodeService.GetProductByBarcodeAsync(searchTerm);

            if (product != null)
            {
                // Found by barcode - add to order directly with pending quantity
                var tile = CreateProductTileFromEntity(product);
                await AddToOrderWithQuantityAsync(tile);
                BarcodeInput = string.Empty; // Clear for next scan
                _logger.Debug("Product {Name} added via barcode {Barcode}", product.Name, searchTerm);
                return;
            }

            // If no barcode match, search by exact code match
            var exactCodeMatch = _allProducts.FirstOrDefault(p =>
                p.Code?.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);

            if (exactCodeMatch != null)
            {
                var tile = CreateProductTileFromEntity(exactCodeMatch);
                await AddToOrderWithQuantityAsync(tile);
                BarcodeInput = string.Empty;
                _logger.Debug("Product {Name} added via exact code {Code}", exactCodeMatch.Name, searchTerm);
                return;
            }

            // No exact match - open product search dialog with the search term
            await OpenProductSearchDialogAsync(searchTerm);

        }, "Searching...").ConfigureAwait(true);
    }

    /// <summary>
    /// Opens the product search dialog.
    /// </summary>
    private async Task OpenProductSearchDialogAsync(string? initialSearchText)
    {
        var dialog = new Views.Dialogs.ProductSearchDialog(_productService, initialSearchText);
        dialog.Owner = Application.Current.MainWindow;

        var result = dialog.ShowDialog();

        if (result == true && dialog.SelectedProduct != null)
        {
            var selectedProduct = dialog.SelectedProduct;

            // Find the full product entity
            var product = _allProducts.FirstOrDefault(p => p.Id == selectedProduct.Id);
            if (product != null)
            {
                var tile = CreateProductTileFromEntity(product);
                await AddToOrderWithQuantityAsync(tile);
                _logger.Debug("Product {Name} added via search dialog", product.Name);
            }
        }

        BarcodeInput = string.Empty;
    }

    #region Real-Time Search Methods

    /// <summary>
    /// Called when BarcodeInput changes - triggers debounced search.
    /// </summary>
    partial void OnBarcodeInputChanged(string value)
    {
        // Stop any existing timer
        _searchDebounceTimer?.Stop();

        // Clear results if input is too short
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            SearchResults.Clear();
            IsSearchDropdownOpen = false;
            OnPropertyChanged(nameof(HasNoSearchResults));
            return;
        }

        // Start debounce timer (300ms delay)
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer?.Stop();
            await PerformRealTimeSearchAsync(value);
        };
        _searchDebounceTimer.Start();
    }

    /// <summary>
    /// Performs the real-time search for products.
    /// </summary>
    private async Task PerformRealTimeSearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            SearchResults.Clear();
            IsSearchDropdownOpen = false;
            return;
        }

        IsSearching = true;
        IsSearchDropdownOpen = true;

        try
        {
            var results = await Task.Run(() =>
            {
                var term = searchText.ToLower();
                return _allProducts
                    .Where(p =>
                        (p.Name?.ToLower().Contains(term) ?? false) ||
                        (p.Code?.ToLower().Contains(term) ?? false) ||
                        (p.Barcode?.ToLower().Contains(term) ?? false))
                    .OrderByDescending(p => p.Name?.ToLower().StartsWith(term) ?? false)
                    .ThenBy(p => p.Name)
                    .Take(10)
                    .Select(p => new ProductSearchResult
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code ?? "",
                        Barcode = p.Barcode,
                        Price = p.SellingPrice,
                        CurrentStock = p.Inventory?.CurrentStock ?? 0,
                        IsOutOfStock = p.Inventory == null || p.Inventory.CurrentStock <= 0,
                        IsLowStock = p.IsLowStock,
                        ImagePath = _imageService.GetDisplayImagePath(p.ImagePath)
                    })
                    .ToList();
            });

            SearchResults = new ObservableCollection<ProductSearchResult>(results);
            SelectedSearchResult = results.FirstOrDefault();
            OnPropertyChanged(nameof(HasNoSearchResults));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error performing real-time search");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Selects a search result and adds it to the order.
    /// </summary>
    [RelayCommand]
    private async Task SelectSearchResultAsync(ProductSearchResult? result)
    {
        if (result == null) return;

        var product = _allProducts.FirstOrDefault(p => p.Id == result.Id);
        if (product != null)
        {
            var tile = CreateProductTileFromEntity(product);
            await AddToOrderWithQuantityAsync(tile);
            _logger.Debug("Product {Name} added via real-time search", product.Name);
        }

        // Clear search state
        BarcodeInput = string.Empty;
        SearchResults.Clear();
        IsSearchDropdownOpen = false;
        SelectedSearchResult = null;
    }

    /// <summary>
    /// Closes the search dropdown.
    /// </summary>
    [RelayCommand]
    private void CloseSearchDropdown()
    {
        IsSearchDropdownOpen = false;
        SearchResults.Clear();
        SelectedSearchResult = null;
    }

    /// <summary>
    /// Moves selection up in search results.
    /// </summary>
    [RelayCommand]
    private void MoveSearchSelectionUp()
    {
        if (SearchResults.Count == 0) return;

        var currentIndex = SelectedSearchResult != null
            ? SearchResults.IndexOf(SelectedSearchResult)
            : 0;

        if (currentIndex > 0)
        {
            SelectedSearchResult = SearchResults[currentIndex - 1];
        }
    }

    /// <summary>
    /// Moves selection down in search results.
    /// </summary>
    [RelayCommand]
    private void MoveSearchSelectionDown()
    {
        if (SearchResults.Count == 0) return;

        var currentIndex = SelectedSearchResult != null
            ? SearchResults.IndexOf(SelectedSearchResult)
            : -1;

        if (currentIndex < SearchResults.Count - 1)
        {
            SelectedSearchResult = SearchResults[currentIndex + 1];
        }
    }

    /// <summary>
    /// Processes Enter key in search - either selects dropdown item or does barcode lookup.
    /// </summary>
    [RelayCommand]
    private async Task ProcessSearchEnterAsync()
    {
        // If dropdown is open and we have a selection, use it
        if (IsSearchDropdownOpen && SelectedSearchResult != null)
        {
            await SelectSearchResultAsync(SelectedSearchResult);
            return;
        }

        // Otherwise, fall back to barcode lookup behavior
        await SearchBarcodeAsync();
    }

    #endregion

    /// <summary>
    /// Adds product to order, applying any pending quantity from keypad.
    /// </summary>
    private async Task AddToOrderWithQuantityAsync(ProductTileViewModel tile)
    {
        // Check if there's a pending quantity from keypad
        int quantity = 1;
        if (!string.IsNullOrEmpty(KeypadInput) && int.TryParse(KeypadInput, out int pendingQty) && pendingQty > 0)
        {
            quantity = pendingQty;
            KeypadClear();
        }

        // Add to order with specified quantity
        for (int i = 0; i < quantity; i++)
        {
            await AddToOrderAsync(tile);
        }
    }

    /// <summary>
    /// Creates a ProductTileViewModel from a Product entity.
    /// </summary>
    private ProductTileViewModel CreateProductTileFromEntity(Product p)
    {
        return new ProductTileViewModel
        {
            Id = p.Id,
            Code = p.Code ?? "",
            Name = p.Name,
            Price = p.SellingPrice,
            CostPrice = p.CostPrice,
            ImagePath = _imageService.GetDisplayImagePath(p.ImagePath),
            IsOutOfStock = p.Inventory is null || p.Inventory.CurrentStock <= 0,
            IsLowStock = p.IsLowStock,
            CurrentStock = p.Inventory?.CurrentStock ?? 0
        };
    }

    /// <summary>
    /// Refreshes the product grid with current page (synchronous for non-async callers).
    /// </summary>
    private void RefreshProductGrid()
    {
        _ = RefreshProductGridAsync();
    }

    /// <summary>
    /// Refreshes the product grid with current page and offer data.
    /// </summary>
    private async Task RefreshProductGridAsync()
    {
        _totalPages = Math.Max(1, (int)Math.Ceiling(_allProducts.Count / (double)ItemsPerPage));
        _currentPage = Math.Clamp(_currentPage, 1, _totalPages);

        // Calculate page numbers (show up to 5 pages)
        var pageNums = new List<int>();
        for (var i = 1; i <= Math.Min(_totalPages, 5); i++)
        {
            pageNums.Add(i);
        }
        PageNumbers = new ObservableCollection<int>(pageNums);
        CurrentPageNumber = _currentPage;

        // Get products for current page
        var skip = (_currentPage - 1) * ItemsPerPage;
        var pageProducts = _allProducts.Skip(skip).Take(ItemsPerPage).ToList();

        // Get all active offers for quick lookup
        var activeOffers = await _offerService.GetActiveOffersAsync();
        var offersByProduct = activeOffers
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.OfferPrice).First());

        // Get favorite product IDs for quick lookup
        var favoriteIds = Favorites.Select(f => f.Id).ToHashSet();

        var productVms = pageProducts.Select(p =>
        {
            var vm = new ProductTileViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Price = p.SellingPrice,
                CostPrice = p.CostPrice,
                ImagePath = _imageService.GetDisplayImagePath(p.ImagePath),
                IsOutOfStock = p.Inventory is null || p.Inventory.CurrentStock <= 0,
                CurrentStock = p.Inventory?.CurrentStock ?? 0,
                IsFavorite = favoriteIds.Contains(p.Id)
            };

            // Check for active offer
            if (offersByProduct.TryGetValue(p.Id, out var offer) && offer.IsCurrentlyActive)
            {
                vm.HasActiveOffer = true;
                vm.OfferPrice = offer.CalculateOfferPrice(p.SellingPrice);
                vm.OfferName = offer.OfferName;
                vm.OfferId = offer.Id;
            }

            return vm;
        });

        Products = new ObservableCollection<ProductTileViewModel>(productVms);
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    [RelayCommand]
    private void GoToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= _totalPages)
        {
            _currentPage = pageNumber;
            RefreshProductGrid();
        }
    }

    #region Favorites Commands

    /// <summary>
    /// Toggles a product's favorite status.
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(ProductTileViewModel product)
    {
        var userId = _sessionService.CurrentUserId;
        if (userId == 0) return;

        await ExecuteAsync(async () =>
        {
            if (product.IsFavorite)
            {
                // Remove from favorites
                var removed = await _productService.RemoveFromFavoritesAsync(userId, product.Id);
                if (removed)
                {
                    product.IsFavorite = false;
                    var favoriteToRemove = Favorites.FirstOrDefault(f => f.Id == product.Id);
                    if (favoriteToRemove != null)
                    {
                        Favorites.Remove(favoriteToRemove);
                    }
                    OnPropertyChanged(nameof(HasFavorites));
                    _logger.Debug("Removed {ProductName} from favorites", product.Name);
                }
            }
            else
            {
                // Add to favorites
                var added = await _productService.AddToFavoritesAsync(userId, product.Id);
                if (added)
                {
                    product.IsFavorite = true;
                    var favoriteVm = CreateProductTileFromEntity(
                        _allProducts.FirstOrDefault(p => p.Id == product.Id)!);
                    favoriteVm.IsFavorite = true;
                    Favorites.Add(favoriteVm);
                    OnPropertyChanged(nameof(HasFavorites));
                    _logger.Debug("Added {ProductName} to favorites", product.Name);
                }
            }
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// Adds a favorite product to the order.
    /// </summary>
    [RelayCommand]
    private async Task AddFavoriteToOrderAsync(ProductTileViewModel favorite)
    {
        await AddToOrderAsync(favorite);
    }

    /// <summary>
    /// Toggles the favorites panel expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleFavoritesPanel()
    {
        IsFavoritesPanelExpanded = !IsFavoritesPanelExpanded;
    }

    #endregion

    /// <summary>
    /// Adds a product to the current order.
    /// </summary>
    [RelayCommand]
    private async Task AddToOrderAsync(ProductTileViewModel product)
    {
        if (product.IsOutOfStock)
        {
            await _dialogService.ShowErrorAsync("Out of Stock", $"{product.Name} is currently out of stock.");
            return;
        }

        // Check if product already in order
        var existing = OrderItems.FirstOrDefault(oi => oi.ProductId == product.Id);
        if (existing is not null)
        {
            // Check stock before increasing
            if (existing.Quantity >= (int)existing.AvailableStock)
            {
                await _dialogService.ShowWarningAsync("Stock Limit", $"Only {(int)existing.AvailableStock} units of {product.Name} available.");
                return;
            }
            existing.Quantity++;

            // Recalculate offer for quantity-based offers
            await RecalculateOfferForItemAsync(existing);
        }
        else
        {
            // Check for active offers on this product
            var offer = await _offerService.GetBestOfferForProductAsync(product.Id, 1);

            var orderItem = new OrderItemViewModel
            {
                ProductId = product.Id,
                ProductCode = product.Code ?? "",
                ProductName = product.Name,
                Price = product.Price,
                CostPrice = product.CostPrice,
                AvailableStock = product.CurrentStock,
                Quantity = 1
            };

            // Apply offer if available
            if (offer != null && offer.IsCurrentlyActive)
            {
                orderItem.OriginalPrice = product.Price;
                orderItem.Price = offer.CalculateOfferPrice(product.Price);
                orderItem.AppliedOfferId = offer.Id;
                orderItem.AppliedOfferName = offer.OfferName;

                _logger.Debug("Applied offer {OfferName} to {ProductName}: {OriginalPrice} -> {OfferPrice}",
                    offer.OfferName, product.Name, product.Price, orderItem.Price);
            }

            OrderItems.Add(orderItem);
        }

        RecalculateOrderTotals();

        _logger.Debug("Added {ProductName} to order", product.Name);
    }

    /// <summary>
    /// Recalculates the offer for an item when quantity changes.
    /// </summary>
    private async Task RecalculateOfferForItemAsync(OrderItemViewModel item)
    {
        // Check if a better offer applies with the new quantity
        var offer = await _offerService.GetBestOfferForProductAsync(item.ProductId, item.Quantity);

        if (offer != null && offer.IsCurrentlyActive)
        {
            // Get original price
            var originalPrice = item.OriginalPrice ?? item.Price;
            item.OriginalPrice = originalPrice;
            item.Price = offer.CalculateOfferPrice(originalPrice);
            item.AppliedOfferId = offer.Id;
            item.AppliedOfferName = offer.OfferName;
        }
        else if (item.HasOfferApplied && item.OriginalPrice.HasValue)
        {
            // Remove offer if no longer applicable
            item.Price = item.OriginalPrice.Value;
            item.OriginalPrice = null;
            item.AppliedOfferId = null;
            item.AppliedOfferName = null;
        }
    }

    /// <summary>
    /// Increases the quantity of an order item.
    /// </summary>
    [RelayCommand]
    private async Task IncreaseQuantityAsync(OrderItemViewModel item)
    {
        // Check stock before increasing
        if (item.Quantity >= (int)item.AvailableStock)
        {
            await _dialogService.ShowWarningAsync("Stock Limit", $"Only {(int)item.AvailableStock} units of {item.ProductName} available.");
            return;
        }

        item.Quantity++;

        // Recalculate offer for new quantity (some offers may have minimum quantity requirements)
        await RecalculateOfferForItemAsync(item);

        RecalculateOrderTotals();
    }

    /// <summary>
    /// Decreases the quantity of an order item.
    /// </summary>
    [RelayCommand]
    private async Task DecreaseQuantityAsync(OrderItemViewModel item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;

            // Recalculate offer for new quantity (some offers may have minimum quantity requirements)
            await RecalculateOfferForItemAsync(item);
        }
        else
        {
            OrderItems.Remove(item);
        }
        RecalculateOrderTotals();
    }

    /// <summary>
    /// Removes an order item.
    /// </summary>
    [RelayCommand]
    private void RemoveOrderItem(OrderItemViewModel item)
    {
        OrderItems.Remove(item);
        RecalculateOrderTotals();
    }

    /// <summary>
    /// Edits notes for an order item using the special instructions dialog.
    /// </summary>
    [RelayCommand]
    private void EditItemNotes(OrderItemViewModel item)
    {
        var dialog = new Views.Dialogs.OrderItemNoteDialog(item.ProductName, item.Notes);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            item.Notes = dialog.NoteText.Trim();
            AutoSaveOrder();
            _logger.Debug("Updated notes for {ProductName}: {Notes}", item.ProductName, item.Notes);
        }
    }

    /// <summary>
    /// Edits modifiers for an order item.
    /// </summary>
    [RelayCommand]
    private async Task EditItemModifiersAsync(OrderItemViewModel item)
    {
        var result = await _dialogService.ShowInputAsync(
            "Item Modifiers",
            $"Enter modifiers for {item.ProductName} (comma-separated):",
            item.Modifiers);

        if (result is not null)
        {
            item.Modifiers = result;
            RecalculateOrderTotals();
            _logger.Debug("Updated modifiers for {ProductName}: {Modifiers}", item.ProductName, result);
        }
    }

    /// <summary>
    /// Applies a discount to an order item.
    /// </summary>
    [RelayCommand]
    private async Task ApplyItemDiscountAsync(OrderItemViewModel item)
    {
        // Check if user has any discount permission
        var hasBasicDiscount = _sessionService.HasPermission("Discounts.Apply10") ||
                               _sessionService.HasPermission("Discounts.Apply20") ||
                               _sessionService.HasPermission("Discounts.Apply50") ||
                               _sessionService.HasPermission("Discounts.ApplyAny");

        if (!hasBasicDiscount)
        {
            await _dialogService.ShowWarningAsync("Permission Denied", "You do not have permission to apply discounts.");
            return;
        }

        var result = await _dialogService.ShowInputAsync(
            "Apply Discount",
            $"Enter discount percentage for {item.ProductName} (0-100):",
            item.DiscountPercent > 0 ? item.DiscountPercent.ToString("0") : "");

        if (result is not null && decimal.TryParse(result, out var percent))
        {
            // Validate discount percentage
            percent = Math.Clamp(percent, 0, 100);

            // Check permission level for discount amount
            if (percent > 50 && !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 50%.");
                return;
            }
            else if (percent > 20 && !_sessionService.HasPermission("Discounts.Apply50") &&
                     !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 20%.");
                return;
            }
            else if (percent > 10 && !_sessionService.HasPermission("Discounts.Apply20") &&
                     !_sessionService.HasPermission("Discounts.Apply50") &&
                     !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 10%.");
                return;
            }

            item.DiscountPercent = percent;
            item.DiscountAmount = 0; // Clear fixed amount when using percentage
            RecalculateOrderTotals();
            _logger.Information("Applied {DiscountPercent}% discount to {ProductName}", percent, item.ProductName);
        }
    }

    /// <summary>
    /// Removes discount from an order item.
    /// </summary>
    [RelayCommand]
    private void RemoveItemDiscount(OrderItemViewModel item)
    {
        item.DiscountAmount = 0;
        item.DiscountPercent = 0;
        RecalculateOrderTotals();
    }

    /// <summary>
    /// Clears all order items.
    /// </summary>
    [RelayCommand]
    private async Task ClearOrderAsync()
    {
        if (!OrderItems.Any()) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Clear Order",
            "Are you sure you want to clear all items from this order?");

        if (confirmed)
        {
            ClearCurrentOrder();
        }
    }

    /// <summary>
    /// Starts a new order, clearing the current one.
    /// </summary>
    [RelayCommand]
    private async Task NewOrderAsync()
    {
        if (OrderItems.Any())
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "New Order",
                "You have items in your current order. Do you want to start a new order?");

            if (!confirmed) return;
        }

        ClearCurrentOrder();
    }

    /// <summary>
    /// Gets whether the order can be submitted.
    /// </summary>
    public bool CanSubmitOrder => OrderItems.Any() && OrderTotal > 0;

    /// <summary>
    /// Submits the order and prints KOT (incremental for existing orders).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSubmitOrder))]
    private async Task SubmitOrderAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order before submitting.");
            return;
        }

        try
        {
            // Get current user and work period
            var currentUser = _sessionService.CurrentUser;
            if (currentUser is null)
            {
                await _dialogService.ShowErrorAsync("Session Error", "No user is logged in.");
                return;
            }

            var currentWorkPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync();
            if (currentWorkPeriod is null)
            {
                await _dialogService.ShowErrorAsync("Work Period", "No work period is open. Please open a work period first.");
                return;
            }

            Order savedOrder;
            IEnumerable<OrderItem> itemsToPrint;

            if (CurrentOrderId.HasValue)
            {
                // Adding items to existing order - incremental printing
                var newItems = OrderItems.Where(i => !i.IsPrinted).ToList();

                if (!newItems.Any())
                {
                    await _dialogService.ShowMessageAsync("No New Items", "There are no new items to print.");
                    return;
                }

                // Add new items to existing order
                var orderItemsToAdd = newItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    DiscountAmount = item.LineDiscount,
                    TaxAmount = item.LineTotal * TaxRate,
                    TotalAmount = item.LineTotal * (1 + TaxRate),
                    Modifiers = string.IsNullOrWhiteSpace(item.Modifiers) ? null : item.Modifiers,
                    Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes,
                    PrintedToKitchen = false
                });

                savedOrder = await _orderService.AddItemsToOrderAsync(CurrentOrderId.Value, orderItemsToAdd);

                // Get only the unprinted items for printing
                itemsToPrint = await _orderService.GetUnprintedItemsAsync(savedOrder.Id);

                // Print addition KOT
                var printSuccess = await _kitchenPrintService.PrintAdditionKotAsync(savedOrder, itemsToPrint.ToList());

                if (printSuccess)
                {
                    var itemIds = itemsToPrint.Select(oi => oi.Id);
                    await _orderService.MarkItemsAsPrintedAsync(itemIds);

                    // Mark items as printed in UI
                    foreach (var item in newItems)
                    {
                        item.IsPrinted = true;
                    }

                    _logger.Information("Added {Count} items to order {OrderNumber} and printed", newItems.Count, savedOrder.OrderNumber);
                }
                else
                {
                    _logger.Warning("Items added to order {OrderNumber} but printing failed", savedOrder.OrderNumber);
                    await _dialogService.ShowWarningAsync(
                        "Print Warning",
                        $"Items were added to order {savedOrder.OrderNumber}, but printing to kitchen failed. Please print manually.");
                }

                await _dialogService.ShowMessageAsync(
                    "Items Added",
                    $"{newItems.Count} items added to order {savedOrder.OrderNumber}.");
            }
            else
            {
                // Creating new order
                var order = new Order
                {
                    UserId = currentUser.Id,
                    WorkPeriodId = currentWorkPeriod.Id,
                    TableNumber = TableName,
                    CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName,
                    Notes = string.IsNullOrWhiteSpace(OrderNotes) ? null : OrderNotes,
                    Subtotal = OrderSubtotal,
                    TaxAmount = OrderTax,
                    DiscountAmount = OrderDiscount,
                    TotalAmount = OrderTotal
                };

                // Add order items
                foreach (var item in OrderItems)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        DiscountAmount = item.LineDiscount,
                        TaxAmount = item.LineTotal * TaxRate,
                        TotalAmount = item.LineTotal * (1 + TaxRate),
                        Modifiers = string.IsNullOrWhiteSpace(item.Modifiers) ? null : item.Modifiers,
                        Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes,
                        PrintedToKitchen = false,
                        // Offer tracking
                        OriginalUnitPrice = item.OriginalPrice,
                        AppliedOfferId = item.AppliedOfferId,
                        AppliedOfferName = item.AppliedOfferName
                    });
                }

                // Save order to database
                savedOrder = await _orderService.CreateOrderAsync(order);
                itemsToPrint = savedOrder.OrderItems;

                // Print KOT
                var printSuccess = await _kitchenPrintService.PrintKotAsync(savedOrder, itemsToPrint.ToList());

                if (printSuccess)
                {
                    // Mark items as printed
                    var itemIds = savedOrder.OrderItems.Select(oi => oi.Id);
                    await _orderService.MarkItemsAsPrintedAsync(itemIds);

                    _logger.Information("Order {OrderNumber} submitted and KOT printed successfully", savedOrder.OrderNumber);
                }
                else
                {
                    _logger.Warning("Order {OrderNumber} submitted but KOT printing failed", savedOrder.OrderNumber);
                    await _dialogService.ShowWarningAsync(
                        "Print Warning",
                        $"Order {savedOrder.OrderNumber} was saved, but printing to kitchen failed. Please print manually.");
                }

                // Set order ID so it can be held or have more items added
                CurrentOrderId = savedOrder.Id;
                CurrentOrderNumber = savedOrder.OrderNumber;

                // Mark all items as printed in UI
                foreach (var item in OrderItems)
                {
                    item.IsPrinted = true;
                }

                // Create receipt from order
                try
                {
                    var receipt = await _receiptService.CreateReceiptFromOrderAsync(savedOrder.Id);
                    CurrentReceiptId = receipt.Id;
                    CurrentReceiptNumber = receipt.ReceiptNumber;
                    _logger.Information("Created receipt {ReceiptNumber} for order {OrderNumber}",
                        receipt.ReceiptNumber, savedOrder.OrderNumber);
                }
                catch (Exception receiptEx)
                {
                    _logger.Warning(receiptEx, "Failed to create receipt for order {OrderNumber}", savedOrder.OrderNumber);
                    // Don't fail the order submission if receipt creation fails
                }

                // Show success message
                await _dialogService.ShowMessageAsync(
                    "Order Submitted",
                    $"Order {savedOrder.OrderNumber} has been submitted successfully." +
                    (string.IsNullOrEmpty(CurrentReceiptNumber) ? "" : $"\nReceipt: {CurrentReceiptNumber}"));
            }

            SubmitOrderCommand.NotifyCanExecuteChanged();
            HoldOrderCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to submit order");
            await _dialogService.ShowErrorAsync("Order Error", $"Failed to submit order: {ex.Message}");
        }
    }

    /// <summary>
    /// Holds the current order for later.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanHoldOrder))]
    private async Task HoldOrderAsync()
    {
        if (!CurrentOrderId.HasValue)
        {
            await _dialogService.ShowWarningAsync("Cannot Hold", "Please submit the order first before holding it.");
            return;
        }

        try
        {
            var order = await _orderService.HoldOrderAsync(CurrentOrderId.Value);

            _logger.Information("Order {OrderNumber} placed on hold", order.OrderNumber);
            await _dialogService.ShowMessageAsync("Order Held", $"Order {order.OrderNumber} has been placed on hold.");

            // Clear current order
            ClearCurrentOrder();

            // Refresh held orders list
            await LoadHeldOrdersAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to hold order {OrderId}", CurrentOrderId);
            await _dialogService.ShowErrorAsync("Hold Error", $"Failed to hold order: {ex.Message}");
        }
    }

    /// <summary>
    /// Recalls a held order to continue working on it.
    /// </summary>
    [RelayCommand]
    private async Task RecallOrderAsync(HeldOrderViewModel heldOrder)
    {
        if (heldOrder is null) return;

        // Warn if current order has items
        if (OrderItems.Any())
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Replace Current Order",
                "You have items in your current order. Do you want to replace it with the held order?");

            if (!confirmed) return;
        }

        try
        {
            var order = await _orderService.RecallOrderAsync(heldOrder.OrderId);
            if (order is null)
            {
                await _dialogService.ShowErrorAsync("Recall Error", "Order not found.");
                return;
            }

            // Load order into UI
            LoadOrderIntoUI(order);

            _logger.Information("Recalled order {OrderNumber} from hold", order.OrderNumber);

            // Close held orders panel
            IsHeldOrdersPanelVisible = false;

            // Refresh held orders list
            await LoadHeldOrdersAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to recall order {OrderId}", heldOrder.OrderId);
            await _dialogService.ShowErrorAsync("Recall Error", $"Failed to recall order: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads held orders for the current work period.
    /// </summary>
    [RelayCommand]
    private async Task LoadHeldOrdersAsync()
    {
        try
        {
            var heldOrders = await _orderService.GetHeldOrdersAsync();
            HeldOrders = new ObservableCollection<HeldOrderViewModel>(
                heldOrders.Select(o => new HeldOrderViewModel
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    TableNumber = o.TableNumber ?? "Counter",
                    CustomerName = o.CustomerName ?? "",
                    ItemCount = o.OrderItems.Count,
                    Total = o.TotalAmount,
                    HeldAt = o.UpdatedAt ?? o.CreatedAt,
                    ServerName = o.User?.FullName ?? "Unknown"
                }));

            _logger.Debug("Loaded {Count} held orders", HeldOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load held orders");
        }
    }

    /// <summary>
    /// Toggles the held orders panel visibility.
    /// </summary>
    [RelayCommand]
    private async Task ToggleHeldOrdersPanelAsync()
    {
        IsHeldOrdersPanelVisible = !IsHeldOrdersPanelVisible;

        if (IsHeldOrdersPanelVisible)
        {
            await LoadHeldOrdersAsync();
        }
    }

    /// <summary>
    /// Clears the current order without confirmation.
    /// </summary>
    private void ClearCurrentOrder()
    {
        OrderItems.Clear();
        CustomerName = "";
        OrderNotes = "";
        TableName = "Counter";
        CurrentOrderId = null;
        CurrentOrderNumber = "";
        CurrentReceiptId = null;
        CurrentReceiptNumber = "";
        RecalculateOrderTotals();
    }

    /// <summary>
    /// Loads an order from the database into the UI for editing.
    /// </summary>
    private void LoadOrderIntoUI(Order order)
    {
        ClearCurrentOrder();

        CurrentOrderId = order.Id;
        CurrentOrderNumber = order.OrderNumber;
        TableName = order.TableNumber ?? "Counter";
        CustomerName = order.CustomerName ?? "";
        OrderNotes = order.Notes ?? "";

        foreach (var item in order.OrderItems)
        {
            var productName = item.Product?.Name ?? $"Product #{item.ProductId}";
            var productCode = item.Product?.Code ?? "";
            var availableStock = item.Product?.Inventory?.CurrentStock ?? 999;

            OrderItems.Add(new OrderItemViewModel
            {
                ProductId = item.ProductId,
                ProductCode = productCode,
                ProductName = productName,
                Price = item.UnitPrice,
                CostPrice = item.Product?.CostPrice,
                Quantity = (int)item.Quantity,
                AvailableStock = availableStock,
                Notes = item.Notes ?? "",
                Modifiers = item.Modifiers ?? "",
                DiscountAmount = item.DiscountAmount,
                DiscountPercent = 0, // Fixed discount was saved as amount
                IsPrinted = item.PrintedToKitchen,
                // Offer tracking
                OriginalPrice = item.OriginalUnitPrice,
                AppliedOfferId = item.AppliedOfferId,
                AppliedOfferName = item.AppliedOfferName
            });
        }

        RecalculateOrderTotals();
    }

    /// <summary>
    /// Recalculates order totals.
    /// </summary>
    private void RecalculateOrderTotals()
    {
        // Calculate total discount from all items
        OrderDiscount = OrderItems.Sum(oi => oi.LineDiscount);

        // Calculate total offer savings
        OrderSavings = OrderItems.Sum(oi => oi.OfferSavings);

        // Subtotal is net of discounts
        OrderSubtotal = OrderItems.Sum(oi => oi.LineTotal);
        OrderTax = OrderSubtotal * TaxRate;
        OrderTotal = OrderSubtotal + OrderTax;

        // Calculate profit margin for items with cost price
        OrderProfit = OrderItems.Sum(oi => oi.LineProfit);
        var totalCost = OrderItems.Where(oi => oi.HasCostPrice).Sum(oi => oi.CostPrice!.Value * oi.Quantity);
        OrderProfitPercent = totalCost > 0 ? Math.Round((OrderProfit / totalCost) * 100, 1) : 0;

        // Notify command can execute changed
        OnPropertyChanged(nameof(CanSubmitOrder));
        OnPropertyChanged(nameof(CanHoldOrder));
        OnPropertyChanged(nameof(OfferItemsCount));
        OnPropertyChanged(nameof(HasOrderProfit));
        SubmitOrderCommand.NotifyCanExecuteChanged();
        HoldOrderCommand.NotifyCanExecuteChanged();

        // Auto-save after each change
        AutoSaveOrder();
    }

    #region Auto-Save

    /// <summary>
    /// Auto-saves the current order to local storage.
    /// </summary>
    private void AutoSaveOrder()
    {
        try
        {
            if (!OrderItems.Any())
            {
                // No items - clear any existing auto-save
                ClearAutoSave();
                return;
            }

            var orderData = new LocalOrderData
            {
                TableName = TableName,
                CustomerName = CustomerName,
                Notes = OrderNotes,
                Items = OrderItems.Select(oi => new LocalOrderItemData
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Price = oi.Price,
                    CostPrice = oi.CostPrice,
                    Quantity = oi.Quantity,
                    AvailableStock = oi.AvailableStock,
                    Notes = oi.Notes,
                    Modifiers = oi.Modifiers,
                    DiscountAmount = oi.DiscountAmount,
                    DiscountPercent = oi.DiscountPercent
                }).ToList(),
                SavedAt = DateTime.UtcNow
            };

            // Ensure directory exists
            if (!Directory.Exists(AutoSaveFolder))
            {
                Directory.CreateDirectory(AutoSaveFolder);
            }

            var filePath = Path.Combine(AutoSaveFolder, AutoSaveFileName);
            var json = JsonSerializer.Serialize(orderData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            _logger.Debug("Order auto-saved with {ItemCount} items", OrderItems.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to auto-save order");
        }
    }

    /// <summary>
    /// Recovers an unsaved order from local storage.
    /// </summary>
    private async Task RecoverAutoSavedOrderAsync()
    {
        try
        {
            var filePath = Path.Combine(AutoSaveFolder, AutoSaveFileName);
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var orderData = JsonSerializer.Deserialize<LocalOrderData>(json);

            if (orderData?.Items is null || !orderData.Items.Any())
            {
                ClearAutoSave();
                return;
            }

            // Check if order is stale
            if ((DateTime.UtcNow - orderData.SavedAt).TotalHours > AutoSaveStalenessHours)
            {
                ClearAutoSave();
                return;
            }

            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Recover Order",
                $"An unsaved order with {orderData.Items.Count} items was found. Do you want to recover it?");

            if (!confirmed)
            {
                ClearAutoSave();
                return;
            }

            // Restore order data
            TableName = orderData.TableName ?? "Counter";
            CustomerName = orderData.CustomerName ?? "";
            OrderNotes = orderData.Notes ?? "";

            foreach (var item in orderData.Items)
            {
                OrderItems.Add(new OrderItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    CostPrice = item.CostPrice,
                    Quantity = item.Quantity,
                    AvailableStock = item.AvailableStock,
                    Notes = item.Notes ?? "",
                    Modifiers = item.Modifiers ?? "",
                    DiscountAmount = item.DiscountAmount,
                    DiscountPercent = item.DiscountPercent
                });
            }

            RecalculateOrderTotals();

            _logger.Information("Recovered auto-saved order with {ItemCount} items", orderData.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to recover auto-saved order");
        }
    }

    /// <summary>
    /// Clears the auto-saved order file.
    /// </summary>
    private void ClearAutoSave()
    {
        try
        {
            var filePath = Path.Combine(AutoSaveFolder, AutoSaveFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to clear auto-save file");
        }
    }

    #endregion

    /// <summary>
    /// Sets the table for the order.
    /// </summary>
    [RelayCommand]
    private void SetTable(string tableName)
    {
        TableName = tableName;
        AutoSaveOrder();
    }

    /// <summary>
    /// Goes back to the main menu.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (OrderItems.Any())
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Unsaved Order",
                "You have items in your order. Are you sure you want to leave?");

            if (!confirmed) return;
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (OrderItems.Any())
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Unsaved Order",
                "You have items in your order. Are you sure you want to logout?");

            if (!confirmed) return;
        }

        _sessionService.ClearSession();
        _navigationService.NavigateTo<LoginViewModel>();
    }

    /// <summary>
    /// Navigates to the product management (admin) screen.
    /// </summary>
    [RelayCommand]
    private void GoToAdmin()
    {
        _navigationService.NavigateTo<ProductManagementViewModel>();
    }

    /// <summary>
    /// Navigates to the settlement screen to settle the current receipt.
    /// </summary>
    [RelayCommand]
    private async Task SettleReceiptAsync()
    {
        if (!CurrentReceiptId.HasValue)
        {
            await _dialogService.ShowWarningAsync("No Receipt", "Please submit the order first to create a receipt.");
            return;
        }

        _navigationService.NavigateTo<SettlementViewModel>(CurrentReceiptId.Value);
    }

    /// <summary>
    /// Splits the current receipt.
    /// </summary>
    [RelayCommand]
    private async Task SplitBillAsync()
    {
        // Split bill feature requires IReceiptSplitService which is not yet available
        await _dialogService.ShowInfoAsync("Coming Soon", "Split bill feature is coming soon.");
    }

    /// <summary>
    /// Merges multiple pending receipts into one.
    /// </summary>
    [RelayCommand]
    private async Task MergeBillsAsync()
    {
        // Merge bills feature requires IReceiptMergeService which is not yet available
        await _dialogService.ShowInfoAsync("Coming Soon", "Merge bills feature is coming soon.");
    }

    /// <summary>
    /// Voids the current receipt.
    /// </summary>
    [RelayCommand]
    private async Task VoidReceiptAsync()
    {
        // Void receipt feature requires IReceiptVoidService which is not yet available
        await _dialogService.ShowInfoAsync("Coming Soon", "Void receipt feature is coming soon.");
    }

    /// <summary>
    /// Shows the held orders panel (alias for ToggleHeldOrdersPanel).
    /// </summary>
    [RelayCommand]
    private async Task ShowHeldOrdersAsync()
    {
        await ToggleHeldOrdersPanelAsync();
    }

    /// <summary>
    /// Shows the loyalty panel (alias for ToggleLoyaltyPanel).
    /// </summary>
    [RelayCommand]
    private async Task ShowLoyaltyPanelAsync()
    {
        await ToggleLoyaltyPanelAsync();
    }

    /// <summary>
    /// Prints the current order and proceeds to settlement.
    /// </summary>
    [RelayCommand]
    private async Task PrintOrderAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order before printing.");
            return;
        }

        // Submit order first if not already saved
        if (!CurrentOrderId.HasValue)
        {
            await SubmitOrderAsync();
        }

        // Navigate to settlement
        if (CurrentReceiptId.HasValue)
        {
            await SettleReceiptAsync();
        }
    }

    /// <summary>
    /// Applies a discount to the entire order.
    /// </summary>
    [RelayCommand]
    private async Task ApplyOrderDiscountAsync()
    {
        // Check if user has any discount permission
        var hasBasicDiscount = _sessionService.HasPermission("Discounts.Apply10") ||
                               _sessionService.HasPermission("Discounts.Apply20") ||
                               _sessionService.HasPermission("Discounts.Apply50") ||
                               _sessionService.HasPermission("Discounts.ApplyAny");

        if (!hasBasicDiscount)
        {
            await _dialogService.ShowWarningAsync("Permission Denied", "You do not have permission to apply discounts.");
            return;
        }

        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order before applying a discount.");
            return;
        }

        var result = await _dialogService.ShowInputAsync(
            "Apply Order Discount",
            "Enter discount percentage for the entire order (0-100):",
            "");

        if (result is not null && decimal.TryParse(result, out var percent))
        {
            // Validate discount percentage
            percent = Math.Clamp(percent, 0, 100);

            // Check permission level for discount amount
            if (percent > 50 && !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 50%.");
                return;
            }
            else if (percent > 20 && !_sessionService.HasPermission("Discounts.Apply50") &&
                     !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 20%.");
                return;
            }
            else if (percent > 10 && !_sessionService.HasPermission("Discounts.Apply20") &&
                     !_sessionService.HasPermission("Discounts.Apply50") &&
                     !_sessionService.HasPermission("Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 10%.");
                return;
            }

            // Apply discount to all items
            foreach (var item in OrderItems)
            {
                item.DiscountPercent = percent;
                item.DiscountAmount = 0; // Clear fixed amount when using percentage
            }

            RecalculateOrderTotals();
            _logger.Information("Applied {DiscountPercent}% discount to entire order", percent);
        }
    }

    /// <summary>
    /// Initiates M-Pesa STK push payment using the dedicated dialog.
    /// </summary>
    [RelayCommand]
    private async Task InitiateMpesaPaymentAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order before processing payment.");
            return;
        }

        // Submit order first if not already saved
        if (!CurrentOrderId.HasValue)
        {
            await SubmitOrderAsync();
        }

        // Get the account reference (receipt or order number)
        var accountReference = CurrentReceiptNumber ?? CurrentOrderNumber ?? $"POS-{DateTime.Now:yyyyMMddHHmmss}";

        // Show M-Pesa payment dialog
        var dialog = new Views.Dialogs.MpesaPaymentDialog(
            OrderTotal,
            accountReference,
            _mpesaService,
            _logger)
        {
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();

        if (result == true)
        {
            if (dialog.UsedCash)
            {
                // User chose to use cash instead - navigate to settlement
                _logger.Information("User chose cash payment instead of M-Pesa");
                if (CurrentReceiptId.HasValue)
                {
                    await SettleReceiptAsync();
                }
            }
            else if (dialog.PaymentSuccessful)
            {
                // M-Pesa payment was successful
                _logger.Information("M-Pesa payment completed successfully. Receipt: {MpesaReceipt}, Phone: {Phone}",
                    dialog.MpesaReceiptNumber, dialog.PhoneNumber);

                await _dialogService.ShowMessageAsync("Payment Successful",
                    $"M-Pesa payment received!\n\n" +
                    $"Amount: KSh {OrderTotal:N2}\n" +
                    $"M-Pesa Receipt: {dialog.MpesaReceiptNumber}\n\n" +
                    "Thank you for your payment.");

                // Clear current order after successful payment
                ClearCurrentOrder();
            }
        }
        else
        {
            _logger.Debug("M-Pesa payment dialog was cancelled");
        }
    }

    #endregion

    #region Loyalty Commands

    /// <summary>
    /// Toggles the loyalty panel visibility.
    /// </summary>
    [RelayCommand]
    private async Task ToggleLoyaltyPanelAsync()
    {
        if (_loyaltyService == null)
        {
            await _dialogService.ShowInfoAsync("Not Available", "Loyalty program is not configured.");
            return;
        }

        IsLoyaltyPanelVisible = !IsLoyaltyPanelVisible;
        if (IsLoyaltyPanelVisible)
        {
            LoyaltySearchPhone = string.Empty;
            LoyaltySearchResults.Clear();
        }
    }

    /// <summary>
    /// Searches for loyalty members by phone number or name.
    /// </summary>
    [RelayCommand]
    private async Task SearchLoyaltyMemberAsync()
    {
        if (_loyaltyService == null || string.IsNullOrWhiteSpace(LoyaltySearchPhone)) return;

        try
        {
            var results = await _loyaltyService.SearchMembersAsync(LoyaltySearchPhone, 10);
            LoyaltySearchResults.Clear();
            foreach (var member in results)
            {
                LoyaltySearchResults.Add(member);
            }

            if (!LoyaltySearchResults.Any())
            {
                await _dialogService.ShowInfoAsync("No Results", "No customers found matching the search criteria.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to search loyalty members");
            await _dialogService.ShowErrorAsync("Search Failed", "Failed to search for customers.");
        }
    }

    /// <summary>
    /// Attaches a loyalty member to the current order.
    /// </summary>
    [RelayCommand]
    private async Task AttachLoyaltyMemberAsync(LoyaltyMemberDto? member)
    {
        if (member == null) return;

        AttachedLoyaltyMember = member;
        IsLoyaltyPanelVisible = false;
        LoyaltySearchResults.Clear();

        await UpdateEstimatedPointsAsync();
        await UpdateRedemptionPreviewAsync();

        _logger.Information("Attached loyalty member {MemberName} ({Phone}) to order",
            member.Name ?? "Unknown", member.PhoneNumber);
    }

    /// <summary>
    /// Detaches the loyalty member from the current order.
    /// </summary>
    [RelayCommand]
    private void DetachLoyaltyMember()
    {
        // Clear loyalty-related properties
        AttachedLoyaltyMember = null;
        PointsToRedeem = 0;
        PointsRedemptionValue = 0;
        MaxRedeemablePoints = 0;
        EstimatedPointsToEarn = 0;
    }

    /// <summary>
    /// Sets the points to redeem.
    /// </summary>
    [RelayCommand]
    private Task SetPointsToRedeemAsync(decimal points)
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        return Task.CompletedTask;
    }

    /// <summary>
    /// Redeems maximum available points.
    /// </summary>
    [RelayCommand]
    private Task RedeemMaxPointsAsync()
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears any points redemption.
    /// </summary>
    [RelayCommand]
    private Task ClearPointsRedemptionAsync()
    {
        PointsToRedeem = 0;
        PointsRedemptionValue = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the estimated points to be earned for the current order.
    /// </summary>
    private Task UpdateEstimatedPointsAsync()
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        EstimatedPointsToEarn = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the redemption preview for the current order.
    /// </summary>
    private Task UpdateRedemptionPreviewAsync()
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        MaxRedeemablePoints = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Awards points to the attached loyalty member after settlement.
    /// </summary>
    private Task AwardLoyaltyPointsAsync(int receiptId, string receiptNumber, decimal totalAmount)
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        return Task.CompletedTask;
    }

    /// <summary>
    /// Redeems points as payment for the current order.
    /// Call this before processing other payments if points are being redeemed.
    /// </summary>
    private Task<decimal> RedeemLoyaltyPointsAsync(int receiptId, string receiptNumber, decimal totalAmount)
    {
        // Loyalty feature requires ILoyaltyService which is not yet available
        return Task.FromResult(0m);
    }

    /// <summary>
    /// Navigates to the customer enrollment screen.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToEnrollCustomerAsync()
    {
        await _dialogService.ShowInfoAsync("Coming Soon", "Customer enrollment feature is coming soon.");
    }

    /// <summary>
    /// Navigates to the customer list screen.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToCustomerListAsync()
    {
        await _dialogService.ShowInfoAsync("Coming Soon", "Customer list feature is coming soon.");
    }

    // ==================== Keyboard Shortcut Commands ====================

    /// <summary>
    /// Focuses the barcode input field (F1).
    /// </summary>
    [RelayCommand]
    private void FocusBarcodeInput()
    {
        // This is handled in the View by focusing the TextBox
        // The command exists to allow binding
        OnPropertyChanged(nameof(BarcodeInput));
    }

    /// <summary>
    /// Shows the keyboard shortcuts help dialog (?).
    /// </summary>
    [RelayCommand]
    private void ShowKeyboardShortcuts()
    {
        var dialog = new Views.Dialogs.KeyboardShortcutsDialog
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    /// <summary>
    /// Performs a price check on the selected or last item (F7).
    /// </summary>
    [RelayCommand]
    private async Task PriceCheckAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowInfoAsync("Price Check", "No items in the order to check.");
            return;
        }

        var lastItem = OrderItems.LastOrDefault();
        if (lastItem != null)
        {
            var message = $"Item: {lastItem.ProductName}\n" +
                          $"Code: {lastItem.ProductCode}\n" +
                          $"Unit Price: KSh {lastItem.Price:N2}\n" +
                          $"Quantity: {lastItem.Quantity}\n" +
                          $"Line Total: KSh {lastItem.LineTotal:N2}";

            if (lastItem.HasOfferApplied)
            {
                message += $"\n\nOffer Applied: {lastItem.AppliedOfferName}";
            }

            await _dialogService.ShowInfoAsync("Price Check", message);
        }
    }

    /// <summary>
    /// Initiates card payment (F12).
    /// </summary>
    [RelayCommand]
    private async Task CardPaymentAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order first.");
            return;
        }

        await _dialogService.ShowInfoAsync("Card Payment", $"Card payment for KSh {OrderTotal:N2}\n\nCard payment integration coming soon.");
    }

    /// <summary>
    /// Opens the split payment dialog.
    /// </summary>
    [RelayCommand]
    private async Task OpenSplitPaymentAsync()
    {
        if (!OrderItems.Any())
        {
            await _dialogService.ShowWarningAsync("Empty Order", "Please add items to the order before processing payment.");
            return;
        }

        var dialog = new Views.Dialogs.SplitPaymentDialog(OrderTotal)
        {
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();

        if (result == true && dialog.IsCompleted)
        {
            var payments = dialog.Payments;
            _logger.Information("Split payment completed with {Count} payment methods:", payments.Count);
            foreach (var payment in payments)
            {
                _logger.Information("  - {Method}: KSh {Amount:N2}", payment.MethodName, payment.Amount);
            }

            // TODO: Process each payment method and record in order
            // For now, show success message and clear order
            var paymentSummary = string.Join("\n", payments.Select(p => $"  {p.MethodName}: KSh {p.Amount:N2}"));
            await _dialogService.ShowMessageAsync("Payment Successful",
                $"Split payment completed!\n\nTotal: KSh {OrderTotal:N2}\n\nPayments:\n{paymentSummary}");

            ClearCurrentOrder();
        }
    }

    /// <summary>
    /// Voids the last item added to the order (F6).
    /// </summary>
    [RelayCommand]
    private void VoidLastItem()
    {
        if (OrderItems.Any())
        {
            var lastItem = OrderItems.Last();
            OrderItems.Remove(lastItem);
            RecalculateOrderTotals();
            _logger.Debug("Voided last item: {ProductName}", lastItem.ProductName);
        }
    }

    /// <summary>
    /// Removes the currently selected item from the order (Delete key).
    /// </summary>
    [RelayCommand]
    private void RemoveSelectedItem()
    {
        // For now, remove the last item if no selection mechanism exists
        VoidLastItem();
    }

    /// <summary>
    /// Increases quantity of the last item (+ key).
    /// </summary>
    [RelayCommand]
    private async Task IncreaseSelectedQuantityAsync()
    {
        if (OrderItems.Any())
        {
            var lastItem = OrderItems.Last();
            if (lastItem.Quantity < (int)lastItem.AvailableStock)
            {
                lastItem.Quantity++;
                await RecalculateOfferForItemAsync(lastItem);
                RecalculateOrderTotals();
            }
            else
            {
                await _dialogService.ShowWarningAsync("Stock Limit", $"Only {(int)lastItem.AvailableStock} units available.");
            }
        }
    }

    /// <summary>
    /// Decreases quantity of the last item (- key).
    /// </summary>
    [RelayCommand]
    private async Task DecreaseSelectedQuantityAsync()
    {
        if (OrderItems.Any())
        {
            var lastItem = OrderItems.Last();
            if (lastItem.Quantity > 1)
            {
                lastItem.Quantity--;
                await RecalculateOfferForItemAsync(lastItem);
                RecalculateOrderTotals();
            }
            else
            {
                // Remove item if quantity would go to 0
                OrderItems.Remove(lastItem);
                RecalculateOrderTotals();
            }
        }
    }

    // ==================== Numeric Keypad Commands ====================

    /// <summary>
    /// Appends a digit to the keypad input buffer.
    /// </summary>
    [RelayCommand]
    private void KeypadDigit(string digit)
    {
        if (KeypadInput.Length < 4) // Limit to 4 digits (max qty 9999)
        {
            KeypadInput += digit;
            OnPropertyChanged(nameof(PendingQuantityDisplay));
            OnPropertyChanged(nameof(HasPendingQuantity));
        }
    }

    /// <summary>
    /// Clears the keypad input buffer.
    /// </summary>
    [RelayCommand]
    private void KeypadClear()
    {
        KeypadInput = "";
        OnPropertyChanged(nameof(PendingQuantityDisplay));
        OnPropertyChanged(nameof(HasPendingQuantity));
    }

    /// <summary>
    /// Applies the quantity from keypad to the last item in order or sets it as pending for next scan.
    /// </summary>
    [RelayCommand]
    private async Task ApplyQuantityAsync()
    {
        if (string.IsNullOrEmpty(KeypadInput))
        {
            await _dialogService.ShowInfoAsync("Quantity", "Enter a quantity using the number pad, then press QTY.");
            return;
        }

        if (!int.TryParse(KeypadInput, out int qty) || qty <= 0)
        {
            await _dialogService.ShowWarningAsync("Invalid Quantity", "Please enter a valid quantity greater than 0.");
            KeypadClear();
            return;
        }

        if (OrderItems.Any())
        {
            // Apply to the last item in the order
            var lastItem = OrderItems.Last();
            if (qty <= (int)lastItem.AvailableStock)
            {
                lastItem.Quantity = qty;
                await RecalculateOfferForItemAsync(lastItem);
                RecalculateOrderTotals();
                _logger.Debug("Applied quantity {Qty} to item {ProductName}", qty, lastItem.ProductName);
            }
            else
            {
                await _dialogService.ShowWarningAsync("Stock Limit", $"Only {(int)lastItem.AvailableStock} units available in stock.");
            }
        }
        else
        {
            await _dialogService.ShowInfoAsync("Quantity Set", $"Quantity {qty} will be applied to the next scanned item.");
            // Keep the quantity in buffer for the next scanned item
            return;
        }

        KeypadClear();
    }

    /// <summary>
    /// Called when KeypadInput changes - used to update related properties.
    /// </summary>
    partial void OnKeypadInputChanged(string value)
    {
        OnPropertyChanged(nameof(PendingQuantityDisplay));
        OnPropertyChanged(nameof(HasPendingQuantity));
    }

    #endregion
}

/// <summary>
/// ViewModel for a category in the category panel.
/// </summary>
public partial class CategoryViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets whether this category is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// ViewModel for a product tile in the product grid.
/// </summary>
public class ProductTileViewModel
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the regular product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product image path.
    /// </summary>
    public string ImagePath { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock { get; set; }

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets whether the product is low on stock.
    /// </summary>
    public bool IsLowStock { get; set; }

    /// <summary>
    /// Gets whether the product has an image.
    /// </summary>
    public bool HasImage => !string.IsNullOrEmpty(ImagePath);

    /// <summary>
    /// Gets whether this product is in stock.
    /// </summary>
    public bool IsInStock => !IsOutOfStock;

    #region Offer Properties

    /// <summary>
    /// Gets or sets whether this product has an active offer.
    /// </summary>
    public bool HasActiveOffer { get; set; }

    /// <summary>
    /// Gets or sets the offer price (discounted price).
    /// </summary>
    public decimal? OfferPrice { get; set; }

    /// <summary>
    /// Gets or sets the offer name/description.
    /// </summary>
    public string? OfferName { get; set; }

    /// <summary>
    /// Gets or sets the offer ID.
    /// </summary>
    public int? OfferId { get; set; }

    /// <summary>
    /// Gets the display price (offer price if active, otherwise regular price).
    /// </summary>
    public decimal DisplayPrice => HasActiveOffer && OfferPrice.HasValue ? OfferPrice.Value : Price;

    /// <summary>
    /// Gets the savings percentage.
    /// </summary>
    public decimal SavingsPercent => HasActiveOffer && OfferPrice.HasValue && Price > 0
        ? Math.Round((1 - (OfferPrice.Value / Price)) * 100, 0)
        : 0;

    /// <summary>
    /// Gets the savings amount.
    /// </summary>
    public decimal SavingsAmount => HasActiveOffer && OfferPrice.HasValue ? Price - OfferPrice.Value : 0;

    #endregion

    #region Favorites Properties

    /// <summary>
    /// Gets or sets whether this product is in the user's favorites.
    /// </summary>
    public bool IsFavorite { get; set; }

    #endregion

    #region Profit Margin Properties

    /// <summary>
    /// Gets or sets the cost price for margin calculations.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets whether cost price is available for margin display.
    /// </summary>
    public bool HasCostPrice => CostPrice.HasValue && CostPrice.Value > 0;

    /// <summary>
    /// Gets the profit margin amount (selling price - cost price).
    /// </summary>
    public decimal ProfitMargin => HasCostPrice ? DisplayPrice - CostPrice!.Value : 0;

    /// <summary>
    /// Gets the profit margin percentage.
    /// </summary>
    public decimal ProfitMarginPercent => HasCostPrice && CostPrice!.Value > 0
        ? Math.Round(((DisplayPrice - CostPrice.Value) / CostPrice.Value) * 100, 1)
        : 0;

    /// <summary>
    /// Gets whether the product is profitable (margin > 0).
    /// </summary>
    public bool IsProfitable => ProfitMargin > 0;

    #endregion
}

/// <summary>
/// ViewModel for an order item in the order ticket.
/// </summary>
public partial class OrderItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code/SKU.
    /// </summary>
    public string ProductCode { get; set; } = "";

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = "";

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the maximum available stock for this product.
    /// </summary>
    public decimal AvailableStock { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineSubtotal))]
    [NotifyPropertyChangedFor(nameof(LineDiscount))]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int _quantity = 1;

    /// <summary>
    /// Gets or sets the item notes/special instructions.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    private string _notes = "";

    /// <summary>
    /// Gets or sets the item modifiers (comma-separated).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModifiers))]
    private string _modifiers = "";

    /// <summary>
    /// Gets or sets the discount amount (fixed).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineDiscount))]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(HasDiscount))]
    private decimal _discountAmount;

    /// <summary>
    /// Gets or sets the discount percentage.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineDiscount))]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(HasDiscount))]
    private decimal _discountPercent;

    /// <summary>
    /// Gets or sets whether this item has been printed to kitchen.
    /// </summary>
    [ObservableProperty]
    private bool _isPrinted;

    /// <summary>
    /// Gets or sets the original price before offer was applied.
    /// </summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>
    /// Gets or sets the applied offer ID.
    /// </summary>
    public int? AppliedOfferId { get; set; }

    /// <summary>
    /// Gets or sets the applied offer name.
    /// </summary>
    public string? AppliedOfferName { get; set; }

    /// <summary>
    /// Gets whether an offer is applied to this item.
    /// </summary>
    public bool HasOfferApplied => AppliedOfferId.HasValue;

    /// <summary>
    /// Gets the savings from the applied offer.
    /// </summary>
    public decimal OfferSavings => OriginalPrice.HasValue
        ? (OriginalPrice.Value - Price) * Quantity
        : 0;

    /// <summary>
    /// Gets the line subtotal before discount.
    /// </summary>
    public decimal LineSubtotal => Price * Quantity;

    /// <summary>
    /// Gets the calculated discount for this line.
    /// </summary>
    public decimal LineDiscount => DiscountAmount > 0 ? DiscountAmount : (LineSubtotal * DiscountPercent / 100);

    /// <summary>
    /// Gets the line total after discount.
    /// </summary>
    public decimal LineTotal => LineSubtotal - LineDiscount;

    /// <summary>
    /// Gets whether this item has notes.
    /// </summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    /// <summary>
    /// Gets whether this item has modifiers.
    /// </summary>
    public bool HasModifiers => !string.IsNullOrWhiteSpace(Modifiers);

    /// <summary>
    /// Gets whether this item has a discount applied.
    /// </summary>
    public bool HasDiscount => DiscountAmount > 0 || DiscountPercent > 0;

    #region Profit Margin Properties

    /// <summary>
    /// Gets or sets the cost price for margin calculations.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets whether cost price is available for margin display.
    /// </summary>
    public bool HasCostPrice => CostPrice.HasValue && CostPrice.Value > 0;

    /// <summary>
    /// Gets the profit margin for this line item.
    /// </summary>
    public decimal LineProfit => HasCostPrice ? LineTotal - (CostPrice!.Value * Quantity) : 0;

    /// <summary>
    /// Gets the profit margin percentage for this line item.
    /// </summary>
    public decimal LineProfitPercent => HasCostPrice && (CostPrice!.Value * Quantity) > 0
        ? Math.Round((LineProfit / (CostPrice.Value * Quantity)) * 100, 1)
        : 0;

    #endregion
}

/// <summary>
/// Data structure for auto-saving orders locally.
/// </summary>
internal class LocalOrderData
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the order notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the order items.
    /// </summary>
    public List<LocalOrderItemData> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets when the order was saved.
    /// </summary>
    public DateTime SavedAt { get; set; }
}

/// <summary>
/// Data structure for auto-saving order items locally.
/// </summary>
internal class LocalOrderItemData
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = "";

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the cost price for margin calculations.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the available stock.
    /// </summary>
    public decimal AvailableStock { get; set; }

    /// <summary>
    /// Gets or sets the item notes.
    /// </summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// Gets or sets the item modifiers.
    /// </summary>
    public string Modifiers { get; set; } = "";

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage.
    /// </summary>
    public decimal DiscountPercent { get; set; }
}

/// <summary>
/// ViewModel for a held order in the held orders panel.
/// </summary>
public class HeldOrderViewModel
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = "";

    /// <summary>
    /// Gets or sets the table number.
    /// </summary>
    public string TableNumber { get; set; } = "";

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Gets or sets the number of items in the order.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the order total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Gets or sets when the order was placed on hold.
    /// </summary>
    public DateTime HeldAt { get; set; }

    /// <summary>
    /// Gets or sets the server name who created the order.
    /// </summary>
    public string ServerName { get; set; } = "";

    /// <summary>
    /// Gets a display string for the held order.
    /// </summary>
    public string DisplayText => $"{OrderNumber} - {TableNumber} ({ItemCount} items)";
}
