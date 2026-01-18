using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Line item for the transfer request.
/// </summary>
public partial class TransferLineItem : ObservableObject
{
    [ObservableProperty]
    private SourceProductStockDto? _product;

    [ObservableProperty]
    private int _requestedQuantity;

    [ObservableProperty]
    private int _sourceAvailableStock;

    [ObservableProperty]
    private decimal _unitCost;

    [ObservableProperty]
    private string? _notes;

    public decimal LineTotal => RequestedQuantity * UnitCost;

    partial void OnRequestedQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }

    partial void OnUnitCostChanged(decimal value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }
}

/// <summary>
/// ViewModel for creating a new stock transfer request.
/// </summary>
public partial class CreateTransferRequestViewModel : ViewModelBase
{
    private readonly IStockTransferService _transferService;
    private readonly IProductService _productService;
    private readonly INavigationService _navigationService;
    private readonly IInventoryService _inventoryService;

    [ObservableProperty]
    private ObservableCollection<SourceLocationDto> _sourceLocations = new();

    [ObservableProperty]
    private SourceLocationDto? _selectedSourceLocation;

    [ObservableProperty]
    private TransferPriority _priority = TransferPriority.Normal;

    [ObservableProperty]
    private TransferReason _reason = TransferReason.Replenishment;

    [ObservableProperty]
    private DateTime? _requestedDeliveryDate;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TransferLineItem> _lineItems = new();

    [ObservableProperty]
    private TransferLineItem? _selectedLineItem;

    [ObservableProperty]
    private ObservableCollection<SourceProductStockDto> _availableProducts = new();

    [ObservableProperty]
    private SourceProductStockDto? _selectedProduct;

    [ObservableProperty]
    private int _quantityToAdd = 1;

    [ObservableProperty]
    private string _productSearchText = string.Empty;

    public decimal TotalEstimatedValue => LineItems.Sum(l => l.LineTotal);
    public int TotalItemsRequested => LineItems.Sum(l => l.RequestedQuantity);

    public ObservableCollection<TransferPriority> PriorityOptions { get; } = new()
    {
        TransferPriority.Low,
        TransferPriority.Normal,
        TransferPriority.High,
        TransferPriority.Urgent
    };

    public ObservableCollection<TransferReason> ReasonOptions { get; } = new()
    {
        TransferReason.Replenishment,
        TransferReason.Emergency,
        TransferReason.Seasonal,
        TransferReason.Promotion,
        TransferReason.Rebalancing,
        TransferReason.SlowMoving,
        TransferReason.Other
    };

    public CreateTransferRequestViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Create Transfer Request";
        _transferService = App.Services.GetRequiredService<IStockTransferService>();
        _productService = App.Services.GetRequiredService<IProductService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _inventoryService = App.Services.GetRequiredService<IInventoryService>();

        // Default delivery date to 3 days from now
        RequestedDeliveryDate = DateTime.Today.AddDays(3);
    }

    /// <summary>
    /// Called when the view is loaded.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load source locations (HQ/warehouses/other stores)
            var storeId = SessionService.CurrentStoreId ?? 1;
            var locations = await _transferService.GetSourceLocationsAsync(storeId);

            SourceLocations.Clear();
            foreach (var location in locations)
            {
                SourceLocations.Add(location);
            }

        }, "Loading...");
    }

    private async Task LoadProductsAsync()
    {
        if (SelectedSourceLocation == null)
        {
            AvailableProducts.Clear();
            return;
        }

        var products = await _transferService.GetSourceStockAsync(SelectedSourceLocation.Id);

        AvailableProducts.Clear();
        foreach (var product in products.Where(p => p.TransferableQuantity > 0))
        {
            AvailableProducts.Add(product);
        }
    }

    /// <summary>
    /// Searches products by name or SKU.
    /// </summary>
    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (SelectedSourceLocation == null)
        {
            ErrorMessage = "Please select a source location first.";
            return;
        }

        var searchTerm = string.IsNullOrWhiteSpace(ProductSearchText) ? null : ProductSearchText;
        var products = await _transferService.GetSourceStockAsync(SelectedSourceLocation.Id, searchTerm);

        AvailableProducts.Clear();
        foreach (var product in products.Where(p => p.TransferableQuantity > 0))
        {
            AvailableProducts.Add(product);
        }
    }

    /// <summary>
    /// Adds the selected product to the transfer request.
    /// </summary>
    [RelayCommand]
    private Task AddProductAsync()
    {
        if (SelectedProduct == null)
        {
            ErrorMessage = "Please select a product.";
            return Task.CompletedTask;
        }

        if (QuantityToAdd <= 0)
        {
            ErrorMessage = "Quantity must be greater than zero.";
            return Task.CompletedTask;
        }

        if (SelectedSourceLocation == null)
        {
            ErrorMessage = "Please select a source location first.";
            return Task.CompletedTask;
        }

        // Check if product already in list
        var existing = LineItems.FirstOrDefault(l => l.Product?.ProductId == SelectedProduct.ProductId);
        if (existing != null)
        {
            existing.RequestedQuantity += QuantityToAdd;
        }
        else
        {
            var lineItem = new TransferLineItem
            {
                Product = SelectedProduct,
                RequestedQuantity = QuantityToAdd,
                SourceAvailableStock = SelectedProduct.TransferableQuantity,
                UnitCost = SelectedProduct.UnitCost
            };

            LineItems.Add(lineItem);
        }

        // Reset for next product
        SelectedProduct = null;
        QuantityToAdd = 1;

        OnPropertyChanged(nameof(TotalEstimatedValue));
        OnPropertyChanged(nameof(TotalItemsRequested));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the selected line item from the request.
    /// </summary>
    [RelayCommand]
    private void RemoveLineItem()
    {
        if (SelectedLineItem == null)
            return;

        LineItems.Remove(SelectedLineItem);
        SelectedLineItem = null;

        OnPropertyChanged(nameof(TotalEstimatedValue));
        OnPropertyChanged(nameof(TotalItemsRequested));
    }

    /// <summary>
    /// Saves the transfer request as a draft.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsDraftAsync()
    {
        if (!ValidateRequest())
            return;

        await ExecuteAsync(async () =>
        {
            var request = BuildTransferRequest();
            var result = await _transferService.CreateTransferRequestAsync(request, SessionService.CurrentUserId);

            if (result != null)
            {
                await DialogService.ShowMessageAsync("Success", $"Draft saved: {result.RequestNumber}");
                _navigationService.NavigateTo<StockTransferViewModel>();
            }
            else
            {
                ErrorMessage = "Failed to save draft.";
            }
        }, "Saving draft...");
    }

    /// <summary>
    /// Submits the transfer request for approval.
    /// </summary>
    [RelayCommand]
    private async Task SubmitRequestAsync()
    {
        if (!ValidateRequest())
            return;

        var confirm = await DialogService.ShowConfirmationAsync(
            "Submit Request",
            "Once submitted, the request will be sent for approval. Continue?");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var request = BuildTransferRequest();
            var created = await _transferService.CreateTransferRequestAsync(request, SessionService.CurrentUserId);

            if (created != null)
            {
                // Submit the draft request for approval
                var result = await _transferService.SubmitRequestAsync(created.Id, SessionService.CurrentUserId);
                await DialogService.ShowMessageAsync("Success", $"Request submitted: {result.RequestNumber}");
                _navigationService.NavigateTo<StockTransferViewModel>();
            }
            else
            {
                ErrorMessage = "Failed to submit request.";
            }
        }, "Submitting request...");
    }

    /// <summary>
    /// Cancels and goes back.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (LineItems.Count > 0)
        {
            var confirm = await DialogService.ShowConfirmationAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirm)
                return;
        }

        _navigationService.NavigateBack();
    }

    private bool ValidateRequest()
    {
        if (SelectedSourceLocation == null)
        {
            ErrorMessage = "Please select a source location.";
            return false;
        }

        if (LineItems.Count == 0)
        {
            ErrorMessage = "Please add at least one product to the request.";
            return false;
        }

        foreach (var line in LineItems)
        {
            if (line.RequestedQuantity <= 0)
            {
                ErrorMessage = $"Quantity for {line.Product?.ProductName} must be greater than zero.";
                return false;
            }

            if (line.RequestedQuantity > line.SourceAvailableStock)
            {
                ErrorMessage = $"Requested quantity for {line.Product?.ProductName} ({line.RequestedQuantity}) " +
                              $"exceeds available stock ({line.SourceAvailableStock}).";
                return false;
            }
        }

        ErrorMessage = null;
        return true;
    }

    private CreateTransferRequestDto BuildTransferRequest()
    {
        return new CreateTransferRequestDto
        {
            RequestingStoreId = SessionService.CurrentStoreId ?? 1,
            SourceLocationId = SelectedSourceLocation!.Id,
            SourceLocationType = SelectedSourceLocation.LocationType,
            Priority = Priority,
            Reason = Reason,
            RequestedDeliveryDate = RequestedDeliveryDate,
            Notes = Notes,
            Lines = LineItems.Select(l => new CreateTransferRequestLineDto
            {
                ProductId = l.Product!.ProductId,
                RequestedQuantity = l.RequestedQuantity,
                Notes = l.Notes
            }).ToList()
        };
    }

    partial void OnSelectedSourceLocationChanged(SourceLocationDto? value)
    {
        // Clear line items when source changes (stock levels change)
        LineItems.Clear();
        AvailableProducts.Clear();
        OnPropertyChanged(nameof(TotalEstimatedValue));
        OnPropertyChanged(nameof(TotalItemsRequested));

        // Load products for the new source location
        if (value != null)
        {
            _ = LoadProductsAsync();
        }
    }
}
