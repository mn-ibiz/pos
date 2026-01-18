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
/// ViewModel for viewing transfer request details.
/// </summary>
public partial class TransferDetailsViewModel : ViewModelBase
{
    private readonly IStockTransferService _transferService;
    private readonly INavigationService _navigationService;
    private int _transferId;

    [ObservableProperty]
    private TransferRequestDetailDto? _transfer;

    [ObservableProperty]
    private ObservableCollection<TransferLineDetailDto> _lineItems = new();

    [ObservableProperty]
    private ObservableCollection<TransferActivityLogDto> _activityLog = new();

    public TransferDetailsViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Transfer Details";
        _transferService = App.Services.GetRequiredService<IStockTransferService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
    }

    /// <summary>
    /// Initializes the ViewModel with the transfer ID.
    /// </summary>
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
                RequestingStoreId = request.RequestingStoreId,
                RequestingStoreName = request.RequestingStoreName,
                SourceLocationId = request.SourceLocationId,
                SourceLocationName = request.SourceLocationName,
                SourceLocationType = request.SourceLocationType,
                Status = request.Status,
                Priority = request.Priority,
                Reason = request.Reason,
                SubmittedAt = request.SubmittedAt,
                SubmittedByUserName = request.SubmittedByUserName,
                ApprovedAt = request.ApprovedAt,
                ApprovedByUserName = request.ApprovedByUserName,
                ApprovalNotes = request.ApprovalNotes,
                RequestedDeliveryDate = request.RequestedDeliveryDate,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Notes = request.Notes,
                RejectionReason = request.RejectionReason,
                TotalItemsRequested = request.TotalItemsRequested,
                TotalItemsApproved = request.TotalItemsApproved,
                TotalEstimatedValue = request.TotalEstimatedValue,
                LineCount = request.LineCount,
                CreatedAt = request.CreatedAt,
                Shipment = request.Shipment,
                Lines = request.Lines.Select(l => new TransferLineDetailDto
                {
                    Id = l.Id,
                    TransferRequestId = l.TransferRequestId,
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    ProductSku = l.ProductSku,
                    ProductBarcode = l.ProductBarcode,
                    CategoryName = l.CategoryName,
                    RequestedQuantity = l.RequestedQuantity,
                    ApprovedQuantity = l.ApprovedQuantity,
                    ShippedQuantity = l.ShippedQuantity,
                    ReceivedQuantity = l.ReceivedQuantity,
                    IssueQuantity = l.IssueQuantity,
                    SourceAvailableStock = l.SourceAvailableStock,
                    UnitCost = l.UnitCost,
                    LineTotal = l.LineTotal,
                    Notes = l.Notes,
                    ApprovalNotes = l.ApprovalNotes
                }).ToList()
            };

            Title = $"Transfer: {Transfer.RequestNumber}";

            LineItems.Clear();
            foreach (var line in Transfer.Lines)
            {
                LineItems.Add(line);
            }

            var logs = await _transferService.GetActivityLogAsync(_transferId);
            ActivityLog.Clear();
            foreach (var log in logs)
            {
                ActivityLog.Add(log);
            }
        }, "Loading details...");
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        await ExecuteAsync(async () =>
        {
            var printService = App.Services.GetService<ITransferPrintService>();
            if (printService != null)
            {
                await printService.PrintTransferRequestAsync(_transferId);
            }
        }, "Printing...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateBack();
    }
}
