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
/// Line item for approval with editable approved quantity.
/// </summary>
public partial class ApprovalLineItem : ObservableObject
{
    public int LineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public int RequestedQuantity { get; set; }
    public int SourceAvailableStock { get; set; }
    public decimal UnitCost { get; set; }

    [ObservableProperty]
    private int _approvedQuantity;

    [ObservableProperty]
    private string? _approvalNotes;

    public bool IsFullyApproved => ApprovedQuantity == RequestedQuantity;
    public bool IsPartiallyApproved => ApprovedQuantity > 0 && ApprovedQuantity < RequestedQuantity;
    public bool IsRejected => ApprovedQuantity == 0;
}

/// <summary>
/// ViewModel for approving stock transfer requests.
/// </summary>
public partial class TransferApprovalViewModel : ViewModelBase
{
    private readonly IStockTransferService _transferService;
    private readonly INavigationService _navigationService;
    private int _transferId;

    [ObservableProperty]
    private TransferRequestDetailDto? _transfer;

    [ObservableProperty]
    private ObservableCollection<ApprovalLineItem> _lineItems = new();

    [ObservableProperty]
    private string _approvalNotes = string.Empty;

    [ObservableProperty]
    private DateTime? _expectedDeliveryDate;

    public int TotalRequested => LineItems.Sum(l => l.RequestedQuantity);
    public int TotalApproved => LineItems.Sum(l => l.ApprovedQuantity);
    public decimal TotalValue => LineItems.Sum(l => l.ApprovedQuantity * l.UnitCost);

    public TransferApprovalViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Approve Transfer Request";
        _transferService = App.Services.GetRequiredService<IStockTransferService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();

        ExpectedDeliveryDate = DateTime.Today.AddDays(2);
    }

    public void Initialize(int transferId)
    {
        _transferId = transferId;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            Transfer = await _transferService.GetTransferRequestByIdAsync(_transferId);

            if (Transfer == null)
            {
                ErrorMessage = "Transfer request not found.";
                return;
            }

            Title = $"Approve: {Transfer.RequestNumber}";

            LineItems.Clear();
            foreach (var line in Transfer.Lines)
            {
                LineItems.Add(new ApprovalLineItem
                {
                    LineId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductSKU = line.ProductSKU,
                    RequestedQuantity = line.RequestedQuantity,
                    SourceAvailableStock = line.SourceAvailableStock,
                    UnitCost = line.UnitCost,
                    ApprovedQuantity = Math.Min(line.RequestedQuantity, line.SourceAvailableStock) // Default to available
                });
            }

            ExpectedDeliveryDate = Transfer.RequestedDeliveryDate ?? DateTime.Today.AddDays(2);

        }, "Loading request...");
    }

    /// <summary>
    /// Approves all items with full quantity.
    /// </summary>
    [RelayCommand]
    private void ApproveAll()
    {
        foreach (var line in LineItems)
        {
            line.ApprovedQuantity = Math.Min(line.RequestedQuantity, line.SourceAvailableStock);
        }

        OnPropertyChanged(nameof(TotalApproved));
        OnPropertyChanged(nameof(TotalValue));
    }

    /// <summary>
    /// Rejects all items (sets quantity to 0).
    /// </summary>
    [RelayCommand]
    private void RejectAll()
    {
        foreach (var line in LineItems)
        {
            line.ApprovedQuantity = 0;
        }

        OnPropertyChanged(nameof(TotalApproved));
        OnPropertyChanged(nameof(TotalValue));
    }

    /// <summary>
    /// Submits the approval decision.
    /// </summary>
    [RelayCommand]
    private async Task SubmitApprovalAsync()
    {
        if (TotalApproved == 0)
        {
            var confirm = await DialogService.ShowConfirmationAsync(
                "Reject All",
                "No items are approved. This will reject the entire request. Continue?");

            if (!confirm)
                return;
        }

        await ExecuteAsync(async () =>
        {
            var approval = new TransferApprovalDto
            {
                TransferRequestId = _transferId,
                ApprovedByUserId = SessionService.CurrentUserId,
                ExpectedDeliveryDate = ExpectedDeliveryDate,
                ApprovalNotes = ApprovalNotes,
                LineApprovals = LineItems.Select(l => new LineApprovalDto
                {
                    LineId = l.LineId,
                    ApprovedQuantity = l.ApprovedQuantity,
                    Notes = l.ApprovalNotes
                }).ToList()
            };

            var result = await _transferService.ApproveTransferRequestAsync(approval);

            if (result)
            {
                await DialogService.ShowMessageAsync("Success", "Transfer request processed successfully.");
                _navigationService.NavigateTo<StockTransferViewModel>();
            }
            else
            {
                ErrorMessage = "Failed to process approval.";
            }
        }, "Processing approval...");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var confirm = await DialogService.ShowConfirmationAsync(
            "Cancel",
            "Discard changes and go back?");

        if (confirm)
        {
            _navigationService.NavigateBack();
        }
    }
}
