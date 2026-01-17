using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the offers management screen.
/// </summary>
public partial class OffersViewModel : ViewModelBase, INavigationAware
{
    private readonly IOfferService _offerService;
    private readonly IProductService _productService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of offers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductOffer> _offers = [];

    /// <summary>
    /// Gets or sets the selected offer.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditOfferCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteOfferCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeactivateOfferCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtendOfferCommand))]
    private ProductOffer? _selectedOffer;

    /// <summary>
    /// Gets or sets the selected status filter.
    /// </summary>
    [ObservableProperty]
    private OfferStatus? _selectedStatus;

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Gets the count of active offers.
    /// </summary>
    [ObservableProperty]
    private int _activeOfferCount;

    /// <summary>
    /// Gets the count of upcoming offers.
    /// </summary>
    [ObservableProperty]
    private int _upcomingOfferCount;

    /// <summary>
    /// Gets the count of expired offers.
    /// </summary>
    [ObservableProperty]
    private int _expiredOfferCount;

    /// <summary>
    /// Gets the total count of offers.
    /// </summary>
    [ObservableProperty]
    private int _totalOfferCount;

    /// <summary>
    /// Gets the available status filters.
    /// </summary>
    public ObservableCollection<OfferStatusFilter> StatusFilters { get; } =
    [
        new OfferStatusFilter(null, "All Offers"),
        new OfferStatusFilter(OfferStatus.Active, "Active"),
        new OfferStatusFilter(OfferStatus.Upcoming, "Upcoming"),
        new OfferStatusFilter(OfferStatus.Expired, "Expired"),
        new OfferStatusFilter(OfferStatus.Inactive, "Inactive")
    ];

    /// <summary>
    /// Gets or sets the selected status filter item.
    /// </summary>
    [ObservableProperty]
    private OfferStatusFilter? _selectedStatusFilter;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="OffersViewModel"/> class.
    /// </summary>
    public OffersViewModel(
        ILogger logger,
        IOfferService offerService,
        IProductService productService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Product Offers";
        SelectedStatusFilter = StatusFilters.First();
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

    partial void OnSelectedStatusFilterChanged(OfferStatusFilter? value)
    {
        SelectedStatus = value?.Status;
        _ = LoadOffersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadOffersAsync();
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
            await LoadOffersAsync();
            await LoadStatsAsync();
        }, "Loading offers...").ConfigureAwait(true);
    }

    /// <summary>
    /// Loads offers based on current filters.
    /// </summary>
    [RelayCommand]
    private async Task LoadOffersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var offers = await _offerService.GetAllOffersAsync(SelectedStatus);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                offers = offers.Where(o =>
                    o.OfferName.ToLowerInvariant().Contains(searchLower) ||
                    o.Product.Name.ToLowerInvariant().Contains(searchLower) ||
                    o.Product.Code.ToLowerInvariant().Contains(searchLower));
            }

            Offers = new ObservableCollection<ProductOffer>(offers);
            TotalOfferCount = Offers.Count;
        }, "Loading offers...").ConfigureAwait(true);
    }

    /// <summary>
    /// Loads offer statistics.
    /// </summary>
    private async Task LoadStatsAsync()
    {
        var allOffers = await _offerService.GetAllOffersAsync();
        var offerList = allOffers.ToList();

        ActiveOfferCount = offerList.Count(o => o.Status == OfferStatus.Active);
        UpcomingOfferCount = offerList.Count(o => o.Status == OfferStatus.Upcoming);
        ExpiredOfferCount = offerList.Count(o => o.Status == OfferStatus.Expired);
    }

    /// <summary>
    /// Creates a new offer.
    /// </summary>
    [RelayCommand]
    private async Task CreateOfferAsync()
    {
        var result = await _dialogService.ShowOfferEditorDialogAsync(null);
        if (result != null)
        {
            await ExecuteAsync(async () =>
            {
                result.CreatedByUserId = SessionService.CurrentUserId;
                await _offerService.CreateOfferAsync(result);
                await _dialogService.ShowMessageAsync("Success", $"Offer '{result.OfferName}' has been created.");
                await LoadDataAsync();
            }, "Creating offer...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Edits the selected offer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditOffer))]
    private async Task EditOfferAsync()
    {
        if (SelectedOffer == null) return;

        var result = await _dialogService.ShowOfferEditorDialogAsync(SelectedOffer);
        if (result != null)
        {
            await ExecuteAsync(async () =>
            {
                result.Id = SelectedOffer.Id;
                await _offerService.UpdateOfferAsync(result);
                await _dialogService.ShowMessageAsync("Success", $"Offer '{result.OfferName}' has been updated.");
                await LoadDataAsync();
            }, "Updating offer...").ConfigureAwait(true);
        }
    }

    private bool CanEditOffer() => SelectedOffer != null;

    /// <summary>
    /// Deletes the selected offer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteOffer))]
    private async Task DeleteOfferAsync()
    {
        if (SelectedOffer == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Offer",
            $"Are you sure you want to delete the offer '{SelectedOffer.OfferName}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _offerService.DeleteOfferAsync(SelectedOffer.Id);
            await _dialogService.ShowMessageAsync("Offer Deleted", $"Offer '{SelectedOffer.OfferName}' has been deleted.");
            await LoadDataAsync();
        }, "Deleting offer...").ConfigureAwait(true);
    }

    private bool CanDeleteOffer() => SelectedOffer != null;

    /// <summary>
    /// Deactivates the selected offer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeactivateOffer))]
    private async Task DeactivateOfferAsync()
    {
        if (SelectedOffer == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Deactivate Offer",
            $"Are you sure you want to deactivate the offer '{SelectedOffer.OfferName}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _offerService.DeactivateOfferAsync(SelectedOffer.Id);
            await _dialogService.ShowMessageAsync("Offer Deactivated", $"Offer '{SelectedOffer.OfferName}' has been deactivated.");
            await LoadDataAsync();
        }, "Deactivating offer...").ConfigureAwait(true);
    }

    private bool CanDeactivateOffer() => SelectedOffer != null && SelectedOffer.IsActive;

    /// <summary>
    /// Extends the selected offer's end date.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExtendOffer))]
    private async Task ExtendOfferAsync()
    {
        if (SelectedOffer == null) return;

        // Show date picker dialog
        var newEndDate = await _dialogService.ShowDatePickerDialogAsync(
            "Extend Offer",
            "Select new end date:",
            SelectedOffer.EndDate.AddDays(7));

        if (newEndDate.HasValue && newEndDate.Value > SelectedOffer.EndDate)
        {
            await ExecuteAsync(async () =>
            {
                await _offerService.ExtendOfferAsync(SelectedOffer.Id, newEndDate.Value);
                await _dialogService.ShowMessageAsync("Offer Extended",
                    $"Offer '{SelectedOffer.OfferName}' has been extended to {newEndDate.Value:d}.");
                await LoadDataAsync();
            }, "Extending offer...").ConfigureAwait(true);
        }
    }

    private bool CanExtendOffer() => SelectedOffer != null && SelectedOffer.IsActive;

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
}

/// <summary>
/// Represents a status filter option.
/// </summary>
public class OfferStatusFilter
{
    public OfferStatus? Status { get; }
    public string DisplayName { get; }

    public OfferStatusFilter(OfferStatus? status, string displayName)
    {
        Status = status;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
