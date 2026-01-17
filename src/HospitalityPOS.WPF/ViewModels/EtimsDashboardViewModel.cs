using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for eTIMS dashboard and status monitoring.
/// </summary>
public partial class EtimsDashboardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private EtimsDashboardData? _dashboardData;

    [ObservableProperty]
    private EtimsQueueStats? _queueStats;

    [ObservableProperty]
    private ObservableCollection<EtimsInvoice> _recentInvoices = [];

    [ObservableProperty]
    private ObservableCollection<EtimsInvoice> _failedInvoices = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDeviceSetup;

    [ObservableProperty]
    private string _statusMessage = "Loading...";

    [ObservableProperty]
    private string _statusColor = "Gray";

    public EtimsDashboardViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await RefreshDashboardAsync();
    }

    [RelayCommand]
    private async Task RefreshDashboardAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            DashboardData = await etimsService.GetDashboardDataAsync();
            QueueStats = await etimsService.GetQueueStatsAsync();

            IsDeviceSetup = DashboardData.IsDeviceRegistered;

            // Update status
            if (!DashboardData.IsDeviceRegistered)
            {
                StatusMessage = "No eTIMS device configured";
                StatusColor = "#F44336"; // Red
            }
            else if (!DashboardData.IsDeviceActive)
            {
                StatusMessage = "eTIMS device inactive";
                StatusColor = "#FF9800"; // Orange
            }
            else if (DashboardData.FailedCount > 0)
            {
                StatusMessage = $"Active - {DashboardData.FailedCount} failed submissions";
                StatusColor = "#FF9800"; // Orange
            }
            else
            {
                StatusMessage = "Active - All submissions successful";
                StatusColor = "#4CAF50"; // Green
            }

            // Load recent invoices
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1).AddSeconds(-1);
            var invoices = await etimsService.GetInvoicesByDateRangeAsync(todayStart, todayEnd);
            RecentInvoices = new ObservableCollection<EtimsInvoice>(invoices.Take(20));

            // Load failed invoices
            var failed = await etimsService.GetInvoicesByStatusAsync(EtimsSubmissionStatus.Failed);
            FailedInvoices = new ObservableCollection<EtimsInvoice>(failed.Take(20));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusColor = "#F44336";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ProcessQueueAsync()
    {
        if (QueueStats?.TotalPending == 0)
        {
            await _dialogService.ShowMessageAsync("Info", "No pending items in queue.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            await etimsService.ProcessQueueAsync();
            await RefreshDashboardAsync();

            await _dialogService.ShowMessageAsync("Success", "Queue processing completed.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to process queue: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RetryFailedAsync()
    {
        if (FailedInvoices.Count == 0)
        {
            await _dialogService.ShowMessageAsync("Info", "No failed submissions to retry.");
            return;
        }

        var result = await _dialogService.ShowConfirmationAsync(
            "Retry Failed Submissions",
            $"This will queue {FailedInvoices.Count} failed submissions for retry. Continue?");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            await etimsService.RetryFailedSubmissionsAsync();
            await RefreshDashboardAsync();

            await _dialogService.ShowMessageAsync("Success", "Failed submissions queued for retry.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to queue retries: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (DashboardData == null || !DashboardData.IsDeviceRegistered)
        {
            await _dialogService.ShowErrorAsync("Error", "No eTIMS device configured.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            var device = await etimsService.GetActiveDeviceAsync();
            if (device == null)
            {
                await _dialogService.ShowErrorAsync("Error", "No active device found.");
                return;
            }

            var success = await etimsService.TestDeviceConnectionAsync(device.Id);
            if (success)
            {
                await _dialogService.ShowMessageAsync("Success", "Connection to eTIMS API successful.");
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Connection test failed. Check device configuration.");
            }

            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Connection test failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
