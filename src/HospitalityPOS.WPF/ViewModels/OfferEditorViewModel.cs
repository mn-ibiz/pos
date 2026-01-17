using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the offer editor dialog.
/// </summary>
public partial class OfferEditorViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IOfferService _offerService;
    private readonly ProductOffer? _existingOffer;

    #region Observable Properties

    /// <summary>
    /// Gets whether this is an edit operation.
    /// </summary>
    [ObservableProperty]
    private bool _isEditMode;

    /// <summary>
    /// Gets or sets the available products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    /// <summary>
    /// Gets or sets the selected product.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OriginalPrice))]
    [NotifyPropertyChangedFor(nameof(CalculatedOfferPrice))]
    [NotifyPropertyChangedFor(nameof(Savings))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Product? _selectedProduct;

    /// <summary>
    /// Gets or sets the offer name.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _offerName = string.Empty;

    /// <summary>
    /// Gets or sets the offer description.
    /// </summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// Gets or sets the pricing type.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedPriceMode))]
    [NotifyPropertyChangedFor(nameof(IsPercentageMode))]
    [NotifyPropertyChangedFor(nameof(CalculatedOfferPrice))]
    [NotifyPropertyChangedFor(nameof(Savings))]
    private OfferPricingType _pricingType = OfferPricingType.FixedPrice;

    /// <summary>
    /// Gets or sets the offer price (for fixed price mode).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedOfferPrice))]
    [NotifyPropertyChangedFor(nameof(Savings))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private decimal _offerPrice;

    /// <summary>
    /// Gets or sets the discount percentage (for percentage mode).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedOfferPrice))]
    [NotifyPropertyChangedFor(nameof(Savings))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private decimal? _discountPercent;

    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime _startDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime _endDate = DateTime.Today.AddDays(7);

    /// <summary>
    /// Gets or sets the minimum quantity.
    /// </summary>
    [ObservableProperty]
    private int _minQuantity = 1;

    /// <summary>
    /// Gets or sets the maximum quantity.
    /// </summary>
    [ObservableProperty]
    private int? _maxQuantity;

    /// <summary>
    /// Gets or sets the product search text.
    /// </summary>
    [ObservableProperty]
    private string _productSearchText = string.Empty;

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _validationErrors = [];

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _validationWarnings = [];

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether fixed price mode is selected.
    /// </summary>
    public bool IsFixedPriceMode => PricingType == OfferPricingType.FixedPrice;

    /// <summary>
    /// Gets whether percentage mode is selected.
    /// </summary>
    public bool IsPercentageMode => PricingType == OfferPricingType.PercentageDiscount;

    /// <summary>
    /// Gets the original selling price of the selected product.
    /// </summary>
    public decimal OriginalPrice => SelectedProduct?.SellingPrice ?? 0;

    /// <summary>
    /// Gets the calculated offer price.
    /// </summary>
    public decimal CalculatedOfferPrice
    {
        get
        {
            if (SelectedProduct == null) return 0;

            if (IsPercentageMode && DiscountPercent.HasValue)
            {
                return OriginalPrice * (1 - DiscountPercent.Value / 100);
            }

            return OfferPrice;
        }
    }

    /// <summary>
    /// Gets the savings amount.
    /// </summary>
    public decimal Savings => OriginalPrice - CalculatedOfferPrice;

    #endregion

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<ProductOffer?>? CloseRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfferEditorViewModel"/> class.
    /// </summary>
    public OfferEditorViewModel(
        ILogger logger,
        IProductService productService,
        IOfferService offerService,
        ProductOffer? existingOffer = null)
        : base(logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _existingOffer = existingOffer;

        IsEditMode = existingOffer != null;
        Title = existingOffer != null ? "Edit Offer" : "Create Offer";

        if (existingOffer != null)
        {
            LoadExistingOffer(existingOffer);
        }
    }

    /// <summary>
    /// Initializes the view model asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadProductsAsync();
    }

    private void LoadExistingOffer(ProductOffer offer)
    {
        OfferName = offer.OfferName;
        Description = offer.Description;
        PricingType = offer.PricingType;
        OfferPrice = offer.OfferPrice;
        DiscountPercent = offer.DiscountPercent;
        StartDate = offer.StartDate;
        EndDate = offer.EndDate;
        MinQuantity = offer.MinQuantity;
        MaxQuantity = offer.MaxQuantity;
    }

    partial void OnProductSearchTextChanged(string value)
    {
        _ = SearchProductsAsync(value);
    }

    partial void OnPricingTypeChanged(OfferPricingType value)
    {
        // When switching to fixed price, set initial offer price to product's selling price
        if (value == OfferPricingType.FixedPrice && SelectedProduct != null)
        {
            OfferPrice = SelectedProduct.SellingPrice * 0.9m; // Default to 10% off
        }
    }

    #region Commands

    /// <summary>
    /// Loads available products.
    /// </summary>
    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var products = await _productService.GetActiveProductsAsync();
            Products = new ObservableCollection<Product>(products);

            // If editing, select the existing product
            if (_existingOffer != null)
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == _existingOffer.ProductId);
            }
        }, "Loading products...").ConfigureAwait(true);
    }

    /// <summary>
    /// Searches for products.
    /// </summary>
    private async Task SearchProductsAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            await LoadProductsAsync();
            return;
        }

        await ExecuteAsync(async () =>
        {
            var products = await _productService.SearchAsync(searchText, null, true);
            Products = new ObservableCollection<Product>(products);
        }, "Searching products...").ConfigureAwait(true);
    }

    /// <summary>
    /// Validates the offer.
    /// </summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        if (SelectedProduct == null) return;

        var offer = BuildOffer();
        var result = await _offerService.ValidateOfferAsync(offer);

        ValidationErrors = new ObservableCollection<string>(result.Errors);
        ValidationWarnings = new ObservableCollection<string>(result.Warnings);
    }

    /// <summary>
    /// Saves the offer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await ValidateAsync();

        if (ValidationErrors.Any())
        {
            return;
        }

        CloseRequested?.Invoke(this, BuildOffer());
    }

    private bool CanSave()
    {
        return SelectedProduct != null
            && !string.IsNullOrWhiteSpace(OfferName)
            && EndDate > StartDate
            && ((IsFixedPriceMode && OfferPrice > 0) || (IsPercentageMode && DiscountPercent > 0));
    }

    /// <summary>
    /// Cancels the operation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    /// <summary>
    /// Sets the pricing type to fixed price.
    /// </summary>
    [RelayCommand]
    private void SetFixedPriceMode()
    {
        PricingType = OfferPricingType.FixedPrice;
    }

    /// <summary>
    /// Sets the pricing type to percentage discount.
    /// </summary>
    [RelayCommand]
    private void SetPercentageMode()
    {
        PricingType = OfferPricingType.PercentageDiscount;
    }

    #endregion

    private ProductOffer BuildOffer()
    {
        return new ProductOffer
        {
            Id = _existingOffer?.Id ?? 0,
            ProductId = SelectedProduct?.Id ?? 0,
            OfferName = OfferName,
            Description = Description,
            PricingType = PricingType,
            OfferPrice = IsFixedPriceMode ? OfferPrice : (SelectedProduct?.SellingPrice * (1 - (DiscountPercent ?? 0) / 100) ?? 0),
            DiscountPercent = IsPercentageMode ? DiscountPercent : null,
            StartDate = StartDate,
            EndDate = EndDate,
            MinQuantity = MinQuantity,
            MaxQuantity = MaxQuantity,
            IsActive = true,
            CreatedByUserId = _existingOffer?.CreatedByUserId
        };
    }
}
