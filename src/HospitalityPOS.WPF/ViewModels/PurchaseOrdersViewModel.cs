using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the purchase orders management view.
/// </summary>
public partial class PurchaseOrdersViewModel : ViewModelBase, INavigationAware
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISupplierService _supplierService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _purchaseOrders = [];

    [ObservableProperty]
    private PurchaseOrder? _selectedPurchaseOrder;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private PurchaseOrderStatus? _selectedStatusFilter;

    [ObservableProperty]
    private int? _filterSupplierId;

    [ObservableProperty]
    private Supplier? _filterSupplier;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = [];

    [ObservableProperty]
    private int _draftCount;

    [ObservableProperty]
    private int _sentCount;

    [ObservableProperty]
    private int _partiallyReceivedCount;

    private IReadOnlyList<PurchaseOrder> _allPurchaseOrders = [];

    /// <summary>
    /// Gets the available status options for filtering.
    /// </summary>
    public static IReadOnlyList<PurchaseOrderStatus?> StatusOptions { get; } =
    [
        null, // All
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.Complete,
        PurchaseOrderStatus.Cancelled
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrdersViewModel"/> class.
    /// </summary>
    public PurchaseOrdersViewModel(
        IPurchaseOrderService purchaseOrderService,
        ISupplierService supplierService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService,
        ILogger logger) : base(logger)
    {
        _purchaseOrderService = purchaseOrderService ?? throw new ArgumentNullException(nameof(purchaseOrderService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        Title = "Purchase Orders";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // Check if navigating with a supplier filter
        if (parameter is int supplierId)
        {
            FilterSupplierId = supplierId;
        }

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

    partial void OnSelectedStatusFilterChanged(PurchaseOrderStatus? value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Loads all data including purchase orders and suppliers.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                // Load suppliers for filter dropdown
                var suppliers = await _supplierService.GetAllSuppliersAsync().ConfigureAwait(true);
                Suppliers = new ObservableCollection<Supplier>(suppliers);

                // Set filter supplier if navigated with ID
                if (FilterSupplierId.HasValue)
                {
                    FilterSupplier = suppliers.FirstOrDefault(s => s.Id == FilterSupplierId.Value);
                }

                // Load purchase orders
                if (FilterSupplierId.HasValue)
                {
                    _allPurchaseOrders = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(FilterSupplierId.Value, true).ConfigureAwait(true);
                }
                else
                {
                    _allPurchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync(true).ConfigureAwait(true);
                }

                // Load status counts
                DraftCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Draft).ConfigureAwait(true);
                SentCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Sent).ConfigureAwait(true);
                PartiallyReceivedCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.PartiallyReceived).ConfigureAwait(true);

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading purchase orders...").ConfigureAwait(true);
    }

    private void ApplyFilter()
    {
        var filtered = _allPurchaseOrders.AsEnumerable();

        // Apply status filter
        if (SelectedStatusFilter.HasValue)
        {
            filtered = filtered.Where(po => po.Status == SelectedStatusFilter.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(po =>
                po.PONumber.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                po.Supplier.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                po.Supplier.Code.Contains(searchLower, StringComparison.OrdinalIgnoreCase));
        }

        PurchaseOrders = new ObservableCollection<PurchaseOrder>(filtered);
    }

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    [RelayCommand]
    private async Task CreatePurchaseOrderAsync()
    {
        // Show supplier selection dialog or navigate to PO editor
        if (Suppliers.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "No suppliers available. Please create a supplier first.").ConfigureAwait(true);
            return;
        }

        // For now, navigate to a simplified creation dialog
        // In a full implementation, you'd have a PurchaseOrderEditorDialog
        var supplierCode = await _dialogService.ShowInputAsync("New Purchase Order", "Enter supplier code:").ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            return;
        }

        var supplier = await _supplierService.GetSupplierByCodeAsync(supplierCode.ToUpperInvariant()).ConfigureAwait(true);

        if (supplier is null)
        {
            await _dialogService.ShowErrorAsync("Error", $"Supplier with code '{supplierCode}' not found.").ConfigureAwait(true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var purchaseOrder = new PurchaseOrder
            {
                SupplierId = supplier.Id,
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Draft
            };

            var currentUserId = _sessionService.CurrentUserId;

            await _purchaseOrderService.CreatePurchaseOrderAsync(purchaseOrder, currentUserId).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {purchaseOrder.PONumber} created for {supplier.Name}.").ConfigureAwait(true);
        }, "Creating purchase order...").ConfigureAwait(true);
    }

    /// <summary>
    /// Views/edits the selected purchase order.
    /// </summary>
    [RelayCommand]
    private async Task ViewPurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        // In a full implementation, navigate to PO detail view
        var details = $"PO Number: {SelectedPurchaseOrder.PONumber}\n" +
                      $"Supplier: {SelectedPurchaseOrder.Supplier.Name}\n" +
                      $"Order Date: {SelectedPurchaseOrder.OrderDate:yyyy-MM-dd}\n" +
                      $"Status: {SelectedPurchaseOrder.Status}\n" +
                      $"Items: {SelectedPurchaseOrder.PurchaseOrderItems.Count}\n" +
                      $"SubTotal: KSh {SelectedPurchaseOrder.SubTotal:N2}\n" +
                      $"Tax: KSh {SelectedPurchaseOrder.TaxAmount:N2}\n" +
                      $"Total: KSh {SelectedPurchaseOrder.TotalAmount:N2}";

        await _dialogService.ShowMessageAsync("Purchase Order Details", details).ConfigureAwait(true);
    }

    /// <summary>
    /// Sends the selected purchase order to the supplier.
    /// </summary>
    [RelayCommand]
    private async Task SendToSupplierAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        if (SelectedPurchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            await _dialogService.ShowErrorAsync("Error", "Only draft purchase orders can be sent to suppliers.").ConfigureAwait(true);
            return;
        }

        if (SelectedPurchaseOrder.PurchaseOrderItems.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "Cannot send a purchase order with no items.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Send to Supplier",
            $"Are you sure you want to send PO {SelectedPurchaseOrder.PONumber} to {SelectedPurchaseOrder.Supplier.Name}?\n\nThis action cannot be undone.").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _purchaseOrderService.SendToSupplierAsync(SelectedPurchaseOrder.Id).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {SelectedPurchaseOrder.PONumber} has been sent to the supplier.").ConfigureAwait(true);
        }, "Sending to supplier...").ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels the selected purchase order.
    /// </summary>
    [RelayCommand]
    private async Task CancelPurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        if (SelectedPurchaseOrder.Status == PurchaseOrderStatus.Complete)
        {
            await _dialogService.ShowErrorAsync("Error", "Cannot cancel a completed purchase order.").ConfigureAwait(true);
            return;
        }

        if (SelectedPurchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            await _dialogService.ShowErrorAsync("Error", "Purchase order is already cancelled.").ConfigureAwait(true);
            return;
        }

        var reason = await _dialogService.ShowInputAsync(
            "Cancel Purchase Order",
            $"Enter cancellation reason for PO {SelectedPurchaseOrder.PONumber}:").ConfigureAwait(true);

        if (reason is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _purchaseOrderService.CancelPurchaseOrderAsync(SelectedPurchaseOrder.Id, reason).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {SelectedPurchaseOrder.PONumber} has been cancelled.").ConfigureAwait(true);
        }, "Cancelling purchase order...").ConfigureAwait(true);
    }

    /// <summary>
    /// Clears the supplier filter.
    /// </summary>
    [RelayCommand]
    private async Task ClearSupplierFilterAsync()
    {
        FilterSupplierId = null;
        FilterSupplier = null;
        await LoadDataAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Gets the status display color for a purchase order status.
    /// </summary>
    public static string GetStatusColor(PurchaseOrderStatus status)
    {
        return status switch
        {
            PurchaseOrderStatus.Draft => "#6E6E8E",
            PurchaseOrderStatus.Sent => "#2196F3",
            PurchaseOrderStatus.PartiallyReceived => "#FF9800",
            PurchaseOrderStatus.Complete => "#4CAF50",
            PurchaseOrderStatus.Cancelled => "#F44336",
            _ => "#6E6E8E"
        };
    }

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public static string GetStatusDisplay(PurchaseOrderStatus? status)
    {
        if (!status.HasValue)
        {
            return "All Statuses";
        }

        return status.Value switch
        {
            PurchaseOrderStatus.Draft => "Draft",
            PurchaseOrderStatus.Sent => "Sent",
            PurchaseOrderStatus.PartiallyReceived => "Partially Received",
            PurchaseOrderStatus.Complete => "Complete",
            PurchaseOrderStatus.Cancelled => "Cancelled",
            _ => status.Value.ToString()
        };
    }
}
