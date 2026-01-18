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
/// ViewModel for the Stock Transfer management view.
/// Displays list of transfer requests with filtering and actions.
/// </summary>
public partial class StockTransferViewModel : ViewModelBase
{
    private readonly IStockTransferService _transferService;
    private readonly INavigationService _navigationService;
    private readonly IStoreService? _storeService;

    [ObservableProperty]
    private ObservableCollection<TransferRequestSummaryDto> _transfers = new();

    [ObservableProperty]
    private TransferRequestSummaryDto? _selectedTransfer;

    [ObservableProperty]
    private TransferRequestStatus? _statusFilter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _inTransitCount;

    [ObservableProperty]
    private int _awaitingReceiptCount;

    public ObservableCollection<TransferRequestStatus> StatusOptions { get; } = new()
    {
        TransferRequestStatus.Draft,
        TransferRequestStatus.Submitted,
        TransferRequestStatus.Approved,
        TransferRequestStatus.PartiallyApproved,
        TransferRequestStatus.Rejected,
        TransferRequestStatus.InTransit,
        TransferRequestStatus.PartiallyReceived,
        TransferRequestStatus.Received,
        TransferRequestStatus.Cancelled
    };

    // Permission flags
    public bool CanCreateRequest => HasPermission("Inventory.Transfer.Create");
    public bool CanApproveRequest => HasPermission("Inventory.Transfer.Approve");
    public bool CanReceiveTransfer => HasPermission("Inventory.Transfer.Receive");
    public bool CanCancelRequest => HasPermission("Inventory.Transfer.Cancel");
    public bool CanViewAll => HasPermission("Inventory.Transfer.ViewAll");

    public StockTransferViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Stock Transfers";
        _transferService = App.Services.GetRequiredService<IStockTransferService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _storeService = App.Services.GetService<IStoreService>();

        // Default date range to last 30 days
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Called when the view is loaded.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;

            // Load transfers with current filters
            await RefreshTransfersAsync();

            // Load summary counts
            await LoadSummaryCountsAsync();

        }, "Loading transfers...");

        IsLoading = false;
    }

    private async Task RefreshTransfersAsync()
    {
        var storeId = SessionService.CurrentStoreId;
        var query = new TransferRequestQueryDto
        {
            RequestingStoreId = CanViewAll ? null : storeId,
            Status = StatusFilter,
            FromDate = FromDate,
            ToDate = ToDate?.AddDays(1), // Include end date
            SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
        };

        var transfers = await _transferService.GetTransferRequestsAsync(query);

        Transfers.Clear();
        foreach (var transfer in transfers)
        {
            Transfers.Add(transfer);
        }
    }

    private async Task LoadSummaryCountsAsync()
    {
        var storeId = CanViewAll ? null : SessionService.CurrentStoreId;

        // Get counts using queries
        var pendingQuery = new TransferRequestQueryDto { RequestingStoreId = storeId, Status = TransferRequestStatus.Submitted };
        var pendingTransfers = await _transferService.GetTransferRequestsAsync(pendingQuery);
        PendingCount = pendingTransfers.Count;

        var inTransitQuery = new TransferRequestQueryDto { RequestingStoreId = storeId, Status = TransferRequestStatus.InTransit };
        var inTransitTransfers = await _transferService.GetTransferRequestsAsync(inTransitQuery);
        InTransitCount = inTransitTransfers.Count;

        var partialQuery = new TransferRequestQueryDto { RequestingStoreId = storeId, Status = TransferRequestStatus.PartiallyReceived };
        var partialTransfers = await _transferService.GetTransferRequestsAsync(partialQuery);
        AwaitingReceiptCount = inTransitTransfers.Count + partialTransfers.Count;
    }

    /// <summary>
    /// Applies the current filters and refreshes the list.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await ExecuteAsync(RefreshTransfersAsync, "Filtering...");
    }

    /// <summary>
    /// Clears all filters and refreshes the list.
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        StatusFilter = null;
        SearchText = string.Empty;
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;

        await ExecuteAsync(RefreshTransfersAsync, "Loading...");
    }

    /// <summary>
    /// Creates a new transfer request.
    /// </summary>
    [RelayCommand]
    private void CreateNewRequest()
    {
        if (!RequirePermission("Inventory.Transfer.Create", "create transfer requests"))
            return;

        _navigationService.NavigateTo<CreateTransferRequestViewModel>();
    }

    /// <summary>
    /// Views the details of the selected transfer.
    /// </summary>
    [RelayCommand]
    private void ViewTransferDetails()
    {
        if (SelectedTransfer == null)
            return;

        _navigationService.NavigateTo<TransferDetailsViewModel>(SelectedTransfer.Id);
    }

    /// <summary>
    /// Opens the approval screen for pending transfers.
    /// </summary>
    [RelayCommand]
    private void ApproveTransfer()
    {
        if (SelectedTransfer == null)
            return;

        if (!RequirePermission("Inventory.Transfer.Approve", "approve transfer requests"))
            return;

        if (SelectedTransfer.Status != TransferRequestStatus.Submitted)
        {
            ErrorMessage = "Only submitted requests can be approved.";
            return;
        }

        _navigationService.NavigateTo<TransferApprovalViewModel>(SelectedTransfer.Id);
    }

    /// <summary>
    /// Opens the receive screen for in-transit transfers.
    /// </summary>
    [RelayCommand]
    private void ReceiveTransfer()
    {
        if (SelectedTransfer == null)
            return;

        if (!RequirePermission("Inventory.Transfer.Receive", "receive transfers"))
            return;

        if (SelectedTransfer.Status != TransferRequestStatus.InTransit &&
            SelectedTransfer.Status != TransferRequestStatus.PartiallyReceived)
        {
            ErrorMessage = "Only in-transit or partially received transfers can be received.";
            return;
        }

        _navigationService.NavigateTo<TransferReceiveViewModel>(SelectedTransfer.Id);
    }

    /// <summary>
    /// Cancels the selected transfer request.
    /// </summary>
    [RelayCommand]
    private async Task CancelTransferAsync()
    {
        if (SelectedTransfer == null)
            return;

        if (!RequirePermission("Inventory.Transfer.Cancel", "cancel transfer requests"))
            return;

        if (SelectedTransfer.Status == TransferRequestStatus.Received ||
            SelectedTransfer.Status == TransferRequestStatus.Cancelled)
        {
            ErrorMessage = "This transfer cannot be cancelled.";
            return;
        }

        var confirm = await DialogService.ShowConfirmationAsync(
            "Cancel Transfer",
            $"Are you sure you want to cancel transfer {SelectedTransfer.RequestNumber}?");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _transferService.CancelRequestAsync(
                SelectedTransfer.Id,
                SessionService.CurrentUserId,
                "Cancelled by user");

            if (result != null)
            {
                await RefreshTransfersAsync();
                await LoadSummaryCountsAsync();
            }
            else
            {
                ErrorMessage = "Failed to cancel transfer request.";
            }
        }, "Cancelling transfer...");
    }

    /// <summary>
    /// Prints the transfer document.
    /// </summary>
    [RelayCommand]
    private async Task PrintTransferAsync()
    {
        if (SelectedTransfer == null)
            return;

        await ExecuteAsync(async () =>
        {
            var printService = App.Services.GetService<ITransferPrintService>();
            if (printService != null)
            {
                await printService.PrintTransferRequestAsync(SelectedTransfer.Id);
            }
            else
            {
                ErrorMessage = "Print service not available.";
            }
        }, "Printing...");
    }

    /// <summary>
    /// Exports the transfers list to Excel.
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExecuteAsync(async () =>
        {
            var exportService = App.Services.GetRequiredService<IExportService>();
            var data = Transfers.Select(t => new
            {
                t.RequestNumber,
                t.RequestingStoreName,
                t.SourceLocationName,
                Status = t.Status.ToString(),
                t.TotalItemsRequested,
                t.TotalEstimatedValue,
                t.SubmittedAt,
                t.ExpectedDeliveryDate
            });

            await exportService.ExportToExcelAsync(data, "StockTransfers", "Stock Transfers");
        }, "Exporting...");
    }

    partial void OnStatusFilterChanged(TransferRequestStatus? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search - only search after user stops typing
        _ = ApplyFiltersAsync();
    }
}
