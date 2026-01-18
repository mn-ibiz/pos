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
/// Line item for receiving with quantity and issue tracking.
/// </summary>
public partial class ReceiveLineItem : ObservableObject
{
    public int LineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public int ShippedQuantity { get; set; }
    public int PreviouslyReceivedQuantity { get; set; }

    [ObservableProperty]
    private int _receivedQuantity;

    [ObservableProperty]
    private int _damagedQuantity;

    [ObservableProperty]
    private int _missingQuantity;

    [ObservableProperty]
    private string? _notes;

    public int ExpectedQuantity => ShippedQuantity - PreviouslyReceivedQuantity;
    public int TotalAccounted => ReceivedQuantity + DamagedQuantity + MissingQuantity;
    public bool HasIssues => DamagedQuantity > 0 || MissingQuantity > 0;
    public bool IsComplete => TotalAccounted >= ExpectedQuantity;
}

/// <summary>
/// ViewModel for receiving stock transfers.
/// </summary>
public partial class TransferReceiveViewModel : ViewModelBase
{
    private readonly IStockTransferService _transferService;
    private readonly INavigationService _navigationService;
    private int _transferId;

    [ObservableProperty]
    private TransferRequestDetailDto? _transfer;

    [ObservableProperty]
    private ObservableCollection<ReceiveLineItem> _lineItems = new();

    [ObservableProperty]
    private string _receiptNotes = string.Empty;

    public int TotalExpected => LineItems.Sum(l => l.ExpectedQuantity);
    public int TotalReceived => LineItems.Sum(l => l.ReceivedQuantity);
    public int TotalDamaged => LineItems.Sum(l => l.DamagedQuantity);
    public int TotalMissing => LineItems.Sum(l => l.MissingQuantity);
    public bool HasIssues => TotalDamaged > 0 || TotalMissing > 0;

    public TransferReceiveViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Receive Transfer";
        _transferService = App.Services.GetRequiredService<IStockTransferService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
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
            var request = await _transferService.GetTransferRequestAsync(_transferId);

            if (request == null)
            {
                ErrorMessage = "Transfer request not found.";
                return;
            }

            // Map to TransferRequestDetailDto
            Transfer = new TransferRequestDetailDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                Lines = request.Lines.Select(l => new TransferLineDetailDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    ProductSku = l.ProductSku,
                    ApprovedQuantity = l.ApprovedQuantity,
                    ShippedQuantity = l.ShippedQuantity,
                    ReceivedQuantity = l.ReceivedQuantity
                }).ToList()
            };

            Title = $"Receive: {Transfer.RequestNumber}";

            LineItems.Clear();
            foreach (var line in Transfer.Lines.Where(l => l.ApprovedQuantity > 0))
            {
                LineItems.Add(new ReceiveLineItem
                {
                    LineId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductSKU = line.ProductSku,
                    ShippedQuantity = line.ShippedQuantity,
                    PreviouslyReceivedQuantity = line.ReceivedQuantity,
                    ReceivedQuantity = line.ShippedQuantity - line.ReceivedQuantity // Default to expected
                });
            }

        }, "Loading transfer...");
    }

    /// <summary>
    /// Receives all items with full quantity (no issues).
    /// </summary>
    [RelayCommand]
    private void ReceiveAll()
    {
        foreach (var line in LineItems)
        {
            line.ReceivedQuantity = line.ExpectedQuantity;
            line.DamagedQuantity = 0;
            line.MissingQuantity = 0;
        }

        UpdateTotals();
    }

    /// <summary>
    /// Clears all received quantities.
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        foreach (var line in LineItems)
        {
            line.ReceivedQuantity = 0;
            line.DamagedQuantity = 0;
            line.MissingQuantity = 0;
        }

        UpdateTotals();
    }

    /// <summary>
    /// Submits the receipt.
    /// </summary>
    [RelayCommand]
    private async Task SubmitReceiptAsync()
    {
        if (TotalReceived == 0 && TotalDamaged == 0 && TotalMissing == 0)
        {
            ErrorMessage = "Please enter received quantities.";
            return;
        }

        if (HasIssues)
        {
            var confirm = await DialogService.ShowConfirmationAsync(
                "Issues Detected",
                $"There are issues with this shipment:\n" +
                $"- Damaged: {TotalDamaged} items\n" +
                $"- Missing: {TotalMissing} items\n\n" +
                $"Do you want to continue?");

            if (!confirm)
                return;
        }

        await ExecuteAsync(async () =>
        {
            var receipt = new CreateReceiptDto
            {
                TransferRequestId = _transferId,
                Notes = ReceiptNotes,
                Lines = LineItems.Select(l => new CreateReceiptLineDto
                {
                    RequestLineId = l.LineId,
                    ReceivedQuantity = l.ReceivedQuantity,
                    IssueQuantity = l.DamagedQuantity + l.MissingQuantity,
                    Notes = l.Notes
                }).ToList()
            };

            var result = await _transferService.CreateReceiptAsync(receipt, SessionService.CurrentUserId);

            if (result != null)
            {
                await DialogService.ShowMessageAsync("Success", "Transfer received successfully.");
                _navigationService.NavigateTo<StockTransferViewModel>();
            }
            else
            {
                ErrorMessage = "Failed to receive transfer.";
            }
        }, "Processing receipt...");
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

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(TotalReceived));
        OnPropertyChanged(nameof(TotalDamaged));
        OnPropertyChanged(nameof(TotalMissing));
        OnPropertyChanged(nameof(HasIssues));
    }
}
