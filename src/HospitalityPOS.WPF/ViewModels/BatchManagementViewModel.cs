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
/// ViewModel for managing product batches and tracking expiry dates.
/// </summary>
public partial class BatchManagementViewModel : ViewModelBase
{
    private readonly IProductBatchService _batchService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<ProductBatchDto> _batches = new();

    [ObservableProperty]
    private ProductBatchDto? _selectedBatch;

    [ObservableProperty]
    private BatchStatus? _statusFilter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int? _productIdFilter;

    [ObservableProperty]
    private bool _showExpiringSoon;

    [ObservableProperty]
    private int _expiringWithinDays = 30;

    // Summary counts
    [ObservableProperty]
    private int _activeBatchCount;

    [ObservableProperty]
    private int _expiringCount;

    [ObservableProperty]
    private int _expiredCount;

    [ObservableProperty]
    private int _lowStockBatchCount;

    public ObservableCollection<BatchStatus> StatusOptions { get; } = new()
    {
        BatchStatus.Active,
        BatchStatus.LowStock,
        BatchStatus.Expired,
        BatchStatus.Recalled,
        BatchStatus.Disposed
    };

    public bool CanManageBatches => HasPermission("Inventory.Batch.Manage");
    public bool CanDisposeBatches => HasPermission("Inventory.Batch.Dispose");
    public bool CanOverrideExpiry => HasPermission("Inventory.Batch.OverrideExpiry");

    public BatchManagementViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Batch Management";
        _batchService = App.Services.GetRequiredService<IProductBatchService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            await RefreshBatchesAsync();
            await LoadSummaryCountsAsync();
        }, "Loading batches...");
    }

    private async Task RefreshBatchesAsync()
    {
        var storeId = SessionService.CurrentStoreId;

        var batches = await _batchService.GetBatchesAsync(
            storeId: storeId,
            status: StatusFilter,
            productId: ProductIdFilter,
            expiringWithinDays: ShowExpiringSoon ? ExpiringWithinDays : null);

        Batches.Clear();
        foreach (var batch in batches)
        {
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                if (!batch.BatchNumber.ToLowerInvariant().Contains(searchLower) &&
                    !batch.ProductName?.ToLowerInvariant().Contains(searchLower) == true)
                {
                    continue;
                }
            }

            Batches.Add(batch);
        }
    }

    private async Task LoadSummaryCountsAsync()
    {
        var storeId = SessionService.CurrentStoreId;

        var summary = await _batchService.GetBatchSummaryAsync(storeId);

        ActiveBatchCount = summary.ActiveBatches;
        ExpiringCount = summary.ExpiringWithin30Days;
        ExpiredCount = summary.ExpiredBatches;
        LowStockBatchCount = summary.LowStockBatches;
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await ExecuteAsync(RefreshBatchesAsync, "Filtering...");
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        StatusFilter = null;
        SearchText = string.Empty;
        ProductIdFilter = null;
        ShowExpiringSoon = false;

        await ExecuteAsync(RefreshBatchesAsync, "Loading...");
    }

    [RelayCommand]
    private void ViewBatchDetails()
    {
        if (SelectedBatch == null)
            return;

        _navigationService.NavigateTo<BatchDetailsViewModel>(SelectedBatch.Id);
    }

    [RelayCommand]
    private async Task DisposeBatchAsync()
    {
        if (SelectedBatch == null)
            return;

        if (!RequirePermission("Inventory.Batch.Dispose", "dispose batches"))
            return;

        var confirm = await DialogService.ShowConfirmationAsync(
            "Dispose Batch",
            $"Are you sure you want to dispose batch {SelectedBatch.BatchNumber}?\n" +
            $"Current quantity: {SelectedBatch.CurrentQuantity}");

        if (!confirm)
            return;

        var reason = await DialogService.ShowInputAsync(
            "Disposal Reason",
            "Please provide a reason for disposal:");

        if (string.IsNullOrWhiteSpace(reason))
        {
            ErrorMessage = "A disposal reason is required.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _batchService.DisposeBatchAsync(
                SelectedBatch.Id,
                SelectedBatch.CurrentQuantity,
                DisposalReason.Expired,
                reason,
                SessionService.CurrentUserId);

            if (result)
            {
                await RefreshBatchesAsync();
                await LoadSummaryCountsAsync();
            }
            else
            {
                ErrorMessage = "Failed to dispose batch.";
            }
        }, "Disposing batch...");
    }

    [RelayCommand]
    private async Task PrintBatchLabelAsync()
    {
        if (SelectedBatch == null)
            return;

        await ExecuteAsync(async () =>
        {
            var labelService = App.Services.GetService<ILabelPrintService>();
            if (labelService != null)
            {
                await labelService.PrintBatchLabelAsync(SelectedBatch.Id);
            }
            else
            {
                ErrorMessage = "Label print service not available.";
            }
        }, "Printing label...");
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExecuteAsync(async () =>
        {
            var exportService = App.Services.GetRequiredService<IExportService>();
            var data = Batches.Select(b => new
            {
                b.BatchNumber,
                b.ProductName,
                Status = b.Status.ToString(),
                b.CurrentQuantity,
                b.ReservedQuantity,
                b.ExpiryDate,
                b.DaysUntilExpiry,
                b.UnitCost,
                TotalValue = b.CurrentQuantity * b.UnitCost
            });

            await exportService.ExportToExcelAsync(data, "BatchInventory", "Batch Inventory");
        }, "Exporting...");
    }

    partial void OnStatusFilterChanged(BatchStatus? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnShowExpiringSoonChanged(bool value)
    {
        _ = ApplyFiltersAsync();
    }
}
