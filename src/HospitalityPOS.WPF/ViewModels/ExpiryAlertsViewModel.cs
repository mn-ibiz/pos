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
/// Alert item for display.
/// </summary>
public class ExpiryAlertItem
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public int CurrentQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => CurrentQuantity * UnitCost;
    public string AlertLevel => DaysUntilExpiry switch
    {
        < 0 => "Expired",
        <= 7 => "Critical",
        <= 30 => "Warning",
        _ => "Normal"
    };
    public string AlertColor => AlertLevel switch
    {
        "Expired" => "#EF4444",  // Red
        "Critical" => "#F97316", // Orange
        "Warning" => "#EAB308",  // Yellow
        _ => "#22C55E"           // Green
    };
}

/// <summary>
/// ViewModel for viewing and managing expiry alerts.
/// </summary>
public partial class ExpiryAlertsViewModel : ViewModelBase
{
    private readonly IProductBatchService _batchService;
    private readonly IExpiryValidationService _expiryService;
    private readonly INavigationService _navigationService;
    private readonly IReportPrintService _reportPrintService;

    [ObservableProperty]
    private ObservableCollection<ExpiryAlertItem> _alerts = new();

    [ObservableProperty]
    private ExpiryAlertItem? _selectedAlert;

    [ObservableProperty]
    private string _alertLevelFilter = "All";

    [ObservableProperty]
    private int _daysToShow = 30;

    [ObservableProperty]
    private bool _includeExpired = true;

    // Summary
    [ObservableProperty]
    private int _expiredCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private decimal _totalAtRiskValue;

    public ObservableCollection<string> AlertLevelOptions { get; } = new()
    {
        "All",
        "Expired",
        "Critical",
        "Warning"
    };

    public bool CanDisposeBatches => HasPermission("Inventory.Batch.Dispose");
    public bool CanOverrideExpiry => HasPermission("Inventory.Batch.OverrideExpiry");

    public ExpiryAlertsViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Expiry Alerts";
        _batchService = App.Services.GetRequiredService<IProductBatchService>();
        _expiryService = App.Services.GetRequiredService<IExpiryValidationService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _reportPrintService = App.Services.GetRequiredService<IReportPrintService>();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            await RefreshAlertsAsync();
        }, "Loading alerts...");
    }

    private async Task RefreshAlertsAsync()
    {
        var storeId = SessionService.CurrentStoreId;

        // Get expiring batches
        var expiringBatches = await _batchService.GetExpiringBatchesAsync(
            storeId,
            DaysToShow,
            IncludeExpired);

        Alerts.Clear();
        ExpiredCount = 0;
        CriticalCount = 0;
        WarningCount = 0;
        TotalAtRiskValue = 0;

        foreach (var batch in expiringBatches)
        {
            var alert = new ExpiryAlertItem
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                ProductId = batch.ProductId,
                ProductName = batch.ProductName ?? "Unknown",
                ProductSKU = batch.ProductSKU,
                CurrentQuantity = batch.CurrentQuantity,
                ExpiryDate = batch.ExpiryDate ?? DateTime.MaxValue,
                DaysUntilExpiry = batch.DaysUntilExpiry ?? 999,
                UnitCost = batch.UnitCost
            };

            // Apply filter
            if (AlertLevelFilter != "All" && alert.AlertLevel != AlertLevelFilter)
                continue;

            // Update counts
            switch (alert.AlertLevel)
            {
                case "Expired":
                    ExpiredCount++;
                    break;
                case "Critical":
                    CriticalCount++;
                    break;
                case "Warning":
                    WarningCount++;
                    break;
            }

            TotalAtRiskValue += alert.TotalValue;
            Alerts.Add(alert);
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await ExecuteAsync(RefreshAlertsAsync, "Filtering...");
    }

    [RelayCommand]
    private void ViewBatchDetails()
    {
        if (SelectedAlert == null)
            return;

        _navigationService.NavigateTo<BatchDetailsViewModel>(SelectedAlert.BatchId);
    }

    [RelayCommand]
    private async Task QuickDisposeAsync()
    {
        if (SelectedAlert == null)
            return;

        if (!RequirePermission("Inventory.Batch.Dispose", "dispose batches"))
            return;

        var confirm = await DialogService.ShowConfirmationAsync(
            "Dispose Expired Batch",
            $"Dispose batch {SelectedAlert.BatchNumber} ({SelectedAlert.ProductName})?\n" +
            $"Quantity: {SelectedAlert.CurrentQuantity}\n" +
            $"Value: {SelectedAlert.TotalValue:C}");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _batchService.DisposeBatchAsync(
                SelectedAlert.BatchId,
                SelectedAlert.CurrentQuantity,
                DisposalReason.Expired,
                "Disposed due to expiry",
                SessionService.CurrentUserId);

            if (result)
            {
                await RefreshAlertsAsync();
            }
            else
            {
                ErrorMessage = "Failed to dispose batch.";
            }
        }, "Disposing...");
    }

    [RelayCommand]
    private async Task BulkDisposeExpiredAsync()
    {
        if (!RequirePermission("Inventory.Batch.Dispose", "dispose batches"))
            return;

        var expiredAlerts = Alerts.Where(a => a.AlertLevel == "Expired").ToList();
        if (expiredAlerts.Count == 0)
        {
            ErrorMessage = "No expired batches to dispose.";
            return;
        }

        var totalValue = expiredAlerts.Sum(a => a.TotalValue);
        var confirm = await DialogService.ShowConfirmationAsync(
            "Bulk Dispose Expired",
            $"Dispose all {expiredAlerts.Count} expired batches?\n" +
            $"Total value: {totalValue:C}\n\n" +
            "This action cannot be undone.");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var disposed = 0;
            foreach (var alert in expiredAlerts)
            {
                var result = await _batchService.DisposeBatchAsync(
                    alert.BatchId,
                    alert.CurrentQuantity,
                    DisposalReason.Expired,
                    "Bulk disposal of expired stock",
                    SessionService.CurrentUserId);

                if (result) disposed++;
            }

            await DialogService.ShowMessageAsync(
                "Bulk Disposal Complete",
                $"Disposed {disposed} of {expiredAlerts.Count} expired batches.");

            await RefreshAlertsAsync();
        }, "Disposing expired batches...");
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExecuteAsync(async () =>
        {
            var exportService = App.Services.GetRequiredService<IExportService>();
            var data = Alerts.Select(a => new
            {
                a.BatchNumber,
                a.ProductName,
                a.ProductSKU,
                a.CurrentQuantity,
                a.ExpiryDate,
                a.DaysUntilExpiry,
                a.AlertLevel,
                a.UnitCost,
                a.TotalValue
            });

            await exportService.ExportToExcelAsync(data, "ExpiryAlerts", "Expiry Alerts");
        }, "Exporting...");
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (Alerts.Count == 0)
            {
                await DialogService.ShowMessageAsync("Print", "No alerts to print.");
                return;
            }

            // Convert alerts to report items
            var reportItems = Alerts.Select(a => new ExpiryAlertReportItem
            {
                BatchNumber = a.BatchNumber,
                ProductName = a.ProductName,
                ProductSKU = a.ProductSKU,
                CurrentQuantity = a.CurrentQuantity,
                ExpiryDate = a.ExpiryDate,
                DaysUntilExpiry = a.DaysUntilExpiry,
                UnitCost = a.UnitCost,
                TotalValue = a.TotalValue,
                AlertLevel = a.AlertLevel
            });

            // Create summary
            var summary = new ExpiryAlertSummary
            {
                ExpiredCount = ExpiredCount,
                CriticalCount = CriticalCount,
                WarningCount = WarningCount,
                TotalCount = Alerts.Count,
                TotalAtRiskValue = TotalAtRiskValue,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = SessionService.CurrentUserName ?? "Unknown"
            };

            // Print using the print service
            await _reportPrintService.PrintExpiryAlertsReportAsync(reportItems, summary);
        }, "Generating report...");
    }

    partial void OnAlertLevelFilterChanged(string value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnDaysToShowChanged(int value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnIncludeExpiredChanged(bool value)
    {
        _ = ApplyFiltersAsync();
    }
}
