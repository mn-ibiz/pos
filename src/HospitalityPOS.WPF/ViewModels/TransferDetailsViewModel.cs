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
            Transfer = await _transferService.GetTransferRequestByIdAsync(_transferId);

            if (Transfer == null)
            {
                ErrorMessage = "Transfer request not found.";
                return;
            }

            Title = $"Transfer: {Transfer.RequestNumber}";

            LineItems.Clear();
            foreach (var line in Transfer.Lines)
            {
                LineItems.Add(line);
            }

            var logs = await _transferService.GetTransferActivityLogAsync(_transferId);
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
