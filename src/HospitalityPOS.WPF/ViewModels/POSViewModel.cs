using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

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
    private readonly IReceiptSplitService _receiptSplitService;
    private readonly IReceiptMergeService _receiptMergeService;
    private readonly IReceiptVoidService _receiptVoidService;
    private readonly IKitchenPrintService _kitchenPrintService;
    private readonly IOfferService _offerService;
    private readonly ILoyaltyService _loyaltyService;

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
        IReceiptSplitService receiptSplitService,
        IReceiptMergeService receiptMergeService,
        IReceiptVoidService receiptVoidService,
        IKitchenPrintService kitchenPrintService,
        IOfferService offerService,
        ILoyaltyService loyaltyService)
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
        _receiptSplitService = receiptSplitService ?? throw new ArgumentNullException(nameof(receiptSplitService));
        _receiptMergeService = receiptMergeService ?? throw new ArgumentNullException(nameof(receiptMergeService));
        _receiptVoidService = receiptVoidService ?? throw new ArgumentNullException(nameof(receiptVoidService));
        _kitchenPrintService = kitchenPrintService ?? throw new ArgumentNullException(nameof(kitchenPrintService));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        Title = "Point of Sale";
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(object? parameter)
    {
        try
        {
            // Verify work period is open
            var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync();
            if (currentPeriod is null)
            {
                await _dialogService.ShowErrorAsync("No Work Period", "Please start a work period before using the POS.");
                _navigationService.GoBack();
                return;
            }

            // Set current user name
            CurrentUserName = SessionService.CurrentUser?.FullName ?? "Unknown";

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

            // Display products
            RefreshProductGrid();

            _logger.Debug("POS loaded {CategoryCount} categories and {ProductCount} products",
                Categories.Count, _allProducts.Count);
        }, "Loading POS...").ConfigureAwait(true);
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

        var productVms = pageProducts.Select(p =>
        {
            var vm = new ProductTileViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Price = p.SellingPrice,
                ImagePath = _imageService.GetDisplayImagePath(p.ImagePath),
                IsOutOfStock = p.Inventory is null || p.Inventory.CurrentStock <= 0,
                CurrentStock = p.Inventory?.CurrentStock ?? 0
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
                ProductName = product.Name,
                Price = product.Price,
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
    /// Edits notes for an order item.
    /// </summary>
    [RelayCommand]
    private async Task EditItemNotesAsync(OrderItemViewModel item)
    {
        var result = await _dialogService.ShowInputAsync(
            "Item Notes",
            $"Enter notes for {item.ProductName}:",
            item.Notes);

        if (result is not null)
        {
            item.Notes = result;
            RecalculateOrderTotals();
            _logger.Debug("Updated notes for {ProductName}: {Notes}", item.ProductName, result);
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
        // Check if user has discount permission
        var currentUser = SessionService.CurrentUser;
        if (currentUser?.Role?.Permissions is null ||
            !currentUser.Role.Permissions.Any(p => p.SystemName.StartsWith("Discounts")))
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
            if (percent > 50 && !currentUser!.Role!.Permissions.Any(p => p.SystemName == "Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 50%.");
                return;
            }
            else if (percent > 20 && !currentUser!.Role!.Permissions.Any(p =>
                p.SystemName == "Discounts.Apply50" || p.SystemName == "Discounts.ApplyAny"))
            {
                await _dialogService.ShowWarningAsync("Permission Denied", "You can only apply discounts up to 20%.");
                return;
            }
            else if (percent > 10 && !currentUser!.Role!.Permissions.Any(p =>
                p.SystemName == "Discounts.Apply20" || p.SystemName == "Discounts.Apply50" || p.SystemName == "Discounts.ApplyAny"))
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
            var currentUser = SessionService.CurrentUser;
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

            SubmitOrderAsyncCommand.NotifyCanExecuteChanged();
            HoldOrderAsyncCommand.NotifyCanExecuteChanged();
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
            var availableStock = item.Product?.Inventory?.CurrentStock ?? 999;

            OrderItems.Add(new OrderItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = productName,
                Price = item.UnitPrice,
                Quantity = item.Quantity,
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

        // Notify command can execute changed
        OnPropertyChanged(nameof(CanSubmitOrder));
        OnPropertyChanged(nameof(CanHoldOrder));
        OnPropertyChanged(nameof(OfferItemsCount));
        SubmitOrderAsyncCommand.NotifyCanExecuteChanged();
        HoldOrderAsyncCommand.NotifyCanExecuteChanged();

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

        SessionService.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
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
        if (!CurrentReceiptId.HasValue)
        {
            await _dialogService.ShowWarningAsync("No Receipt", "Please submit the order first to create a receipt.");
            return;
        }

        try
        {
            // Check if receipt can be split
            var (canSplit, reason) = await _receiptSplitService.CanSplitReceiptAsync(CurrentReceiptId.Value);
            if (!canSplit)
            {
                await _dialogService.ShowWarningAsync("Cannot Split", reason!);
                return;
            }

            // Get the receipt with items
            var receipt = await _receiptService.GetByIdAsync(CurrentReceiptId.Value);
            if (receipt is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Receipt not found.");
                return;
            }

            // Show split dialog
            var dialogResult = await _dialogService.ShowSplitBillDialogAsync(receipt);
            if (dialogResult is null)
            {
                return; // User cancelled
            }

            // Perform the split
            Core.Models.SplitResult splitResult;
            if (dialogResult.IsEqualSplit)
            {
                splitResult = await _receiptSplitService.SplitReceiptEquallyAsync(
                    CurrentReceiptId.Value,
                    dialogResult.NumberOfWays);
            }
            else
            {
                splitResult = await _receiptSplitService.SplitReceiptByItemsAsync(
                    CurrentReceiptId.Value,
                    dialogResult.SplitRequests);
            }

            if (!splitResult.Success)
            {
                await _dialogService.ShowErrorAsync("Split Failed", splitResult.ErrorMessage ?? "An error occurred while splitting the receipt.");
                return;
            }

            // Show success message
            var splitType = dialogResult.IsEqualSplit ? "equally" : "by items";
            var splitCount = splitResult.SplitReceipts.Count;
            var splitNumbers = string.Join(", ", splitResult.SplitReceipts.Select(r => r.ReceiptNumber));

            await _dialogService.ShowMessageAsync(
                "Bill Split",
                $"Receipt successfully split {splitType} into {splitCount} receipts:\n{splitNumbers}\n\nYou can settle each split receipt individually.");

            _logger.Information(
                "Split receipt {ReceiptNumber} {SplitType} into {SplitCount} receipts: {SplitNumbers}",
                receipt.ReceiptNumber,
                splitType,
                splitCount,
                splitNumbers);

            // Clear the current order since original receipt is now split
            ClearCurrentOrder();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to split bill for receipt {ReceiptId}", CurrentReceiptId);
            await _dialogService.ShowErrorAsync("Error", $"Failed to split bill: {ex.Message}");
        }
    }

    /// <summary>
    /// Merges multiple pending receipts into one.
    /// </summary>
    [RelayCommand]
    private async Task MergeBillsAsync()
    {
        try
        {
            // Get all pending receipts available for merging
            var mergeableReceipts = await _receiptMergeService.GetMergeableReceiptsAsync();
            var receiptList = mergeableReceipts.ToList();

            if (receiptList.Count < 2)
            {
                await _dialogService.ShowWarningAsync(
                    "Not Enough Receipts",
                    "At least 2 pending receipts are required to merge. Currently there are only " +
                    $"{receiptList.Count} pending receipts.");
                return;
            }

            // Show merge dialog
            var selectedIds = await _dialogService.ShowMergeBillDialogAsync(receiptList);
            if (selectedIds is null || selectedIds.Count < 2)
            {
                return; // User cancelled
            }

            // Perform the merge
            var mergeResult = await _receiptMergeService.MergeReceiptsAsync(selectedIds);

            if (!mergeResult.Success)
            {
                await _dialogService.ShowErrorAsync("Merge Failed", mergeResult.ErrorMessage ?? "An error occurred while merging receipts.");
                return;
            }

            // Show success message
            var sourceNumbers = string.Join(", ", mergeResult.SourceReceipts.Select(r => r.ReceiptNumber));
            await _dialogService.ShowMessageAsync(
                "Bills Merged",
                $"Successfully merged {mergeResult.SourceReceipts.Count} receipts into:\n" +
                $"{mergeResult.MergedReceipt!.ReceiptNumber}\n\n" +
                $"Combined total: KSh {mergeResult.MergedReceipt.TotalAmount:N2}");

            _logger.Information(
                "Merged {SourceCount} receipts ({SourceNumbers}) into {MergedNumber}. Total: {TotalAmount}",
                mergeResult.SourceReceipts.Count,
                sourceNumbers,
                mergeResult.MergedReceipt.ReceiptNumber,
                mergeResult.MergedReceipt.TotalAmount);

            // Clear current order if it was one of the merged receipts
            if (CurrentReceiptId.HasValue && selectedIds.Contains(CurrentReceiptId.Value))
            {
                ClearCurrentOrder();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to merge bills");
            await _dialogService.ShowErrorAsync("Error", $"Failed to merge bills: {ex.Message}");
        }
    }

    /// <summary>
    /// Voids the current receipt.
    /// </summary>
    [RelayCommand]
    private async Task VoidReceiptAsync()
    {
        if (!CurrentReceiptId.HasValue)
        {
            await _dialogService.ShowWarningAsync("No Receipt", "Please select a receipt to void.");
            return;
        }

        try
        {
            // Get the receipt details
            var receipt = await _receiptService.GetByIdAsync(CurrentReceiptId.Value);
            if (receipt is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Receipt not found.");
                return;
            }

            // Check if receipt can be voided
            var (canVoid, reason) = await _receiptVoidService.CanVoidReceiptAsync(CurrentReceiptId.Value);
            if (!canVoid)
            {
                await _dialogService.ShowWarningAsync("Cannot Void", reason ?? "This receipt cannot be voided.");
                return;
            }

            // Get void reasons
            var voidReasons = (await _receiptVoidService.GetVoidReasonsAsync()).ToList();
            if (!voidReasons.Any())
            {
                await _dialogService.ShowErrorAsync("Error", "No void reasons are configured in the system.");
                return;
            }

            // Show void dialog
            var dialogResult = await _dialogService.ShowVoidReceiptDialogAsync(receipt, voidReasons);
            if (dialogResult is null)
            {
                return; // User cancelled
            }

            // Confirm void action
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Confirm Void",
                $"Are you sure you want to void receipt {receipt.ReceiptNumber}?\n\n" +
                $"Total amount: KSh {receipt.TotalAmount:N2}\n\n" +
                "This action cannot be undone and any paid amounts will need to be refunded.",
                "Yes, Void",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            // Perform the void
            var voidRequest = new HospitalityPOS.Core.Models.VoidRequest
            {
                ReceiptId = CurrentReceiptId.Value,
                VoidReasonId = dialogResult.VoidReasonId,
                AdditionalNotes = dialogResult.AdditionalNotes
            };

            var voidResult = await _receiptVoidService.VoidReceiptAsync(voidRequest);

            if (!voidResult.Success)
            {
                await _dialogService.ShowErrorAsync("Void Failed", voidResult.ErrorMessage ?? "An error occurred while voiding the receipt.");
                return;
            }

            // Show success message
            await _dialogService.ShowMessageAsync(
                "Receipt Voided",
                $"Receipt {receipt.ReceiptNumber} has been voided.\n\n" +
                $"Voided amount: KSh {receipt.TotalAmount:N2}" +
                (voidResult.VoidRecord?.StockRestored == true ? "\n\nStock has been restored for inventory-tracked items." : ""));

            _logger.Information(
                "Receipt {ReceiptNumber} voided. Amount: {Amount}. Reason: {ReasonId}",
                receipt.ReceiptNumber,
                receipt.TotalAmount,
                dialogResult.VoidReasonId);

            // Clear current order
            ClearCurrentOrder();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to void receipt {ReceiptId}", CurrentReceiptId);
            await _dialogService.ShowErrorAsync("Error", $"Failed to void receipt: {ex.Message}");
        }
    }

    #endregion

    #region Loyalty Commands

    /// <summary>
    /// Toggles the loyalty panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleLoyaltyPanel()
    {
        IsLoyaltyPanelVisible = !IsLoyaltyPanelVisible;
        if (IsLoyaltyPanelVisible)
        {
            LoyaltySearchPhone = string.Empty;
            LoyaltySearchResults.Clear();
        }
    }

    /// <summary>
    /// Searches for loyalty members by phone number.
    /// </summary>
    [RelayCommand]
    private async Task SearchLoyaltyMemberAsync()
    {
        if (string.IsNullOrWhiteSpace(LoyaltySearchPhone))
        {
            LoyaltySearchResults.Clear();
            return;
        }

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
                // Try to find by exact phone match
                var normalized = _loyaltyService.NormalizePhoneNumber(LoyaltySearchPhone);
                if (normalized != null)
                {
                    var exactMatch = await _loyaltyService.GetByPhoneAsync(normalized);
                    if (exactMatch != null)
                    {
                        LoyaltySearchResults.Add(exactMatch);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to search loyalty members");
            await _dialogService.ShowErrorAsync("Error", "Failed to search loyalty members.");
        }
    }

    /// <summary>
    /// Attaches a loyalty member to the current order.
    /// </summary>
    [RelayCommand]
    private async Task AttachLoyaltyMemberAsync(LoyaltyMemberDto? member)
    {
        if (member == null) return;

        if (!member.IsActive)
        {
            await _dialogService.ShowMessageAsync("Inactive Member",
                "This member account is inactive. Please contact support.");
            return;
        }

        AttachedLoyaltyMember = member;
        IsLoyaltyPanelVisible = false;
        LoyaltySearchPhone = string.Empty;
        LoyaltySearchResults.Clear();

        // Calculate estimated points to earn
        await UpdateEstimatedPointsAsync();

        // Calculate max redeemable points
        await UpdateRedemptionPreviewAsync();

        _logger.Information("Attached loyalty member {MemberId} ({Phone}) to order",
            member.Id, member.PhoneNumber);
    }

    /// <summary>
    /// Detaches the loyalty member from the current order.
    /// </summary>
    [RelayCommand]
    private void DetachLoyaltyMember()
    {
        if (AttachedLoyaltyMember == null) return;

        _logger.Information("Detached loyalty member {MemberId} from order", AttachedLoyaltyMember.Id);

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
    private async Task SetPointsToRedeemAsync(decimal points)
    {
        if (AttachedLoyaltyMember == null) return;

        if (points < 0)
            points = 0;

        if (points > MaxRedeemablePoints)
            points = MaxRedeemablePoints;

        if (points > AttachedLoyaltyMember.PointsBalance)
            points = AttachedLoyaltyMember.PointsBalance;

        PointsToRedeem = points;

        if (points > 0)
        {
            PointsRedemptionValue = await _loyaltyService.ConvertPointsToValueAsync(points);
        }
        else
        {
            PointsRedemptionValue = 0;
        }

        // Recalculate order totals (redemption value as discount)
        RecalculateOrderTotals();
    }

    /// <summary>
    /// Redeems maximum available points.
    /// </summary>
    [RelayCommand]
    private async Task RedeemMaxPointsAsync()
    {
        if (AttachedLoyaltyMember == null) return;

        var maxPoints = Math.Min(MaxRedeemablePoints, AttachedLoyaltyMember.PointsBalance);
        await SetPointsToRedeemAsync(maxPoints);
    }

    /// <summary>
    /// Clears any points redemption.
    /// </summary>
    [RelayCommand]
    private async Task ClearPointsRedemptionAsync()
    {
        await SetPointsToRedeemAsync(0);
    }

    /// <summary>
    /// Updates the estimated points to be earned for the current order.
    /// </summary>
    private async Task UpdateEstimatedPointsAsync()
    {
        if (AttachedLoyaltyMember == null || OrderTotal == 0)
        {
            EstimatedPointsToEarn = 0;
            return;
        }

        try
        {
            var result = await _loyaltyService.CalculatePointsAsync(
                OrderTotal,
                OrderDiscount,
                OrderTax,
                AttachedLoyaltyMember.Id);

            EstimatedPointsToEarn = result.PointsEarned;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to calculate estimated points");
            EstimatedPointsToEarn = 0;
        }
    }

    /// <summary>
    /// Updates the redemption preview for the current order.
    /// </summary>
    private async Task UpdateRedemptionPreviewAsync()
    {
        if (AttachedLoyaltyMember == null || OrderTotal == 0)
        {
            MaxRedeemablePoints = 0;
            return;
        }

        try
        {
            var preview = await _loyaltyService.CalculateRedemptionAsync(
                AttachedLoyaltyMember.Id,
                OrderTotal);

            MaxRedeemablePoints = preview.MaxRedeemablePoints;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to calculate redemption preview");
            MaxRedeemablePoints = 0;
        }
    }

    /// <summary>
    /// Awards points to the attached loyalty member after settlement.
    /// </summary>
    private async Task AwardLoyaltyPointsAsync(int receiptId, string receiptNumber, decimal totalAmount)
    {
        if (AttachedLoyaltyMember == null) return;

        try
        {
            var currentUserId = SessionService.CurrentUserId ?? 0;

            // Award points
            var result = await _loyaltyService.AwardPointsAsync(
                AttachedLoyaltyMember.Id,
                receiptId,
                receiptNumber,
                totalAmount,
                OrderDiscount,
                OrderTax,
                currentUserId);

            if (result.Success)
            {
                _logger.Information("Awarded {Points} points to member {MemberId} for receipt {ReceiptNumber}",
                    result.PointsAwarded, AttachedLoyaltyMember.Id, receiptNumber);

                // Check for tier upgrade
                await _loyaltyService.CheckAndUpgradeTierAsync(AttachedLoyaltyMember.Id);
            }
            else
            {
                _logger.Warning("Failed to award loyalty points: {Error}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error awarding loyalty points for receipt {ReceiptId}", receiptId);
            // Don't fail the transaction if loyalty points fail
        }
    }

    /// <summary>
    /// Redeems points as payment for the current order.
    /// Call this before processing other payments if points are being redeemed.
    /// </summary>
    private async Task<decimal> RedeemLoyaltyPointsAsync(int receiptId, string receiptNumber, decimal totalAmount)
    {
        if (AttachedLoyaltyMember == null || PointsToRedeem <= 0)
            return 0;

        try
        {
            var currentUserId = SessionService.CurrentUserId ?? 0;

            var result = await _loyaltyService.RedeemPointsAsync(
                AttachedLoyaltyMember.Id,
                PointsToRedeem,
                receiptId,
                receiptNumber,
                totalAmount,
                currentUserId);

            if (result.Success)
            {
                _logger.Information("Redeemed {Points} points (KES {Value}) for member {MemberId}",
                    result.PointsRedeemed, result.ValueApplied, AttachedLoyaltyMember.Id);
                return result.ValueApplied;
            }
            else
            {
                _logger.Warning("Failed to redeem loyalty points: {Error}", result.Message);
                await _dialogService.ShowMessageAsync("Redemption Failed", result.Message ?? "Failed to redeem points.");
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error redeeming loyalty points for receipt {ReceiptId}", receiptId);
            return 0;
        }
    }

    /// <summary>
    /// Navigates to the customer enrollment screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToEnrollCustomer()
    {
        _navigationService.NavigateTo<CustomerEnrollmentViewModel>();
    }

    /// <summary>
    /// Navigates to the customer list screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToCustomerList()
    {
        _navigationService.NavigateTo<CustomerListViewModel>();
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
