using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for receiving goods against a purchase order.
/// </summary>
public partial class GoodsReceivingViewModel : ViewModelBase, INavigationAware
{
    private readonly IGoodsReceivingService _goodsReceivingService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the list of pending purchase orders.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _pendingPurchaseOrders = new();

    /// <summary>
    /// Gets or sets the selected purchase order.
    /// </summary>
    [ObservableProperty]
    private PurchaseOrder? _selectedPurchaseOrder;

    /// <summary>
    /// Gets or sets the supplier delivery note number.
    /// </summary>
    [ObservableProperty]
    private string _deliveryNoteNumber = string.Empty;

    /// <summary>
    /// Gets or sets the items to receive.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GRNItemViewModel> _items = new();

    /// <summary>
    /// Gets or sets the total amount of items to receive.
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
    public int ItemsToReceiveCount => Items.Count(i => i.ReceivedQuantity > 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="GoodsReceivingViewModel"/> class.
    /// </summary>
    public GoodsReceivingViewModel(
        IGoodsReceivingService goodsReceivingService,
        INavigationService navigationService,
        ILogger logger)
        : base(logger)
    {
        _goodsReceivingService = goodsReceivingService ?? throw new ArgumentNullException(nameof(goodsReceivingService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Goods Receiving (with PO)";
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
    /// Loads the pending purchase orders.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var pendingPOs = await _goodsReceivingService.GetPendingPurchaseOrdersAsync();
            PendingPurchaseOrders = new ObservableCollection<PurchaseOrder>(pendingPOs);
            SelectedPurchaseOrder = null;
            Items.Clear();
            TotalAmount = 0;
            DeliveryNoteNumber = string.Empty;
            Notes = string.Empty;
        }, "Loading purchase orders...");
    }

    /// <summary>
    /// Called when the selected purchase order changes.
    /// </summary>
    partial void OnSelectedPurchaseOrderChanged(PurchaseOrder? value)
    {
        if (value == null)
        {
            Items.Clear();
            TotalAmount = 0;
            return;
        }

        LoadPurchaseOrderItems(value);
    }

    /// <summary>
    /// Loads items from the selected purchase order.
    /// </summary>
    private void LoadPurchaseOrderItems(PurchaseOrder purchaseOrder)
    {
        Items.Clear();

        foreach (var poItem in purchaseOrder.PurchaseOrderItems)
        {
            var remainingQuantity = poItem.OrderedQuantity - poItem.ReceivedQuantity;

            // Only show items with remaining quantity
            if (remainingQuantity > 0)
            {
                var item = new GRNItemViewModel
                {
                    PurchaseOrderItemId = poItem.Id,
                    ProductId = poItem.ProductId,
                    ProductName = poItem.Product?.Name ?? "Unknown",
                    ProductCode = poItem.Product?.Code ?? "",
                    OrderedQuantity = poItem.OrderedQuantity,
                    PreviouslyReceived = poItem.ReceivedQuantity,
                    RemainingQuantity = remainingQuantity,
                    ReceivedQuantity = 0, // Start with 0, user enters what they received
                    UnitCost = poItem.UnitCost
                };

                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(GRNItemViewModel.TotalCost) ||
                        e.PropertyName == nameof(GRNItemViewModel.ReceivedQuantity))
                    {
                        RecalculateTotal();
                        OnPropertyChanged(nameof(ItemsToReceiveCount));
                    }
                };

                Items.Add(item);
            }
        }

        RecalculateTotal();
        OnPropertyChanged(nameof(ItemsToReceiveCount));
    }

    /// <summary>
    /// Recalculates the total amount.
    /// </summary>
    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.TotalCost);
    }

    /// <summary>
    /// Receives all items (sets received quantity to remaining quantity).
    /// </summary>
    [RelayCommand]
    private void ReceiveAll()
    {
        foreach (var item in Items)
        {
            item.ReceivedQuantity = item.RemainingQuantity;
        }

        RecalculateTotal();
        OnPropertyChanged(nameof(ItemsToReceiveCount));
    }

    /// <summary>
    /// Clears all received quantities.
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        foreach (var item in Items)
        {
            item.ReceivedQuantity = 0;
        }

        RecalculateTotal();
        OnPropertyChanged(nameof(ItemsToReceiveCount));
    }

    /// <summary>
    /// Receives the goods and updates stock.
    /// </summary>
    [RelayCommand]
    private async Task ReceiveGoodsAsync()
    {
        if (SelectedPurchaseOrder == null)
        {
            await DialogService.ShowErrorAsync("Error", "Please select a purchase order.");
            return;
        }

        var itemsToReceive = Items.Where(i => i.ReceivedQuantity > 0).ToList();

        if (!itemsToReceive.Any())
        {
            await DialogService.ShowErrorAsync("Error", "No items to receive. Please enter received quantities.");
            return;
        }

        // Confirm
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Confirm Receiving",
            $"Receive {itemsToReceive.Count} item(s) with a total value of KSh {TotalAmount:N2}?");

        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var grnItemInputs = itemsToReceive.Select(i => new GRNItemInput
            {
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ProductId = i.ProductId,
                OrderedQuantity = i.OrderedQuantity,
                ReceivedQuantity = i.ReceivedQuantity,
                UnitCost = i.UnitCost,
                Notes = i.Notes
            });

            var grn = await _goodsReceivingService.ReceiveGoodsAsync(
                SelectedPurchaseOrder.Id,
                string.IsNullOrWhiteSpace(DeliveryNoteNumber) ? null : DeliveryNoteNumber,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                grnItemInputs);

            await DialogService.ShowInfoAsync("Success", $"Goods received successfully.\n\nGRN: {grn.GRNNumber}");

            // Reload data
            await LoadDataAsync();

        }, "Receiving goods and updating stock...");
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
/// ViewModel for a GRN line item.
/// </summary>
public partial class GRNItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the purchase order item ID.
    /// </summary>
    public int PurchaseOrderItemId { get; set; }

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
    /// Gets or sets the ordered quantity.
    /// </summary>
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the previously received quantity.
    /// </summary>
    public decimal PreviouslyReceived { get; set; }

    /// <summary>
    /// Gets or sets the remaining quantity to receive.
    /// </summary>
    public decimal RemainingQuantity { get; set; }

    /// <summary>
    /// Gets or sets the received quantity.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _receivedQuantity;

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
    /// Gets the total cost (ReceivedQuantity * UnitCost).
    /// </summary>
    public decimal TotalCost => ReceivedQuantity * UnitCost;

    /// <summary>
    /// Called when ReceivedQuantity changes.
    /// </summary>
    partial void OnReceivedQuantityChanged(decimal value)
    {
        // Validate that received quantity doesn't exceed remaining
        if (value > RemainingQuantity)
        {
            ReceivedQuantity = RemainingQuantity;
        }
        else if (value < 0)
        {
            ReceivedQuantity = 0;
        }
    }
}
