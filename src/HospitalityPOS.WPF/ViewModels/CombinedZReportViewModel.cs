using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Combined Z-Report preview view.
/// Shows aggregated Z-Report data from all terminals.
/// </summary>
public partial class CombinedZReportViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly IReportPrintService _reportPrintService;
    private readonly ILogger<CombinedZReportViewModel> _logger;
    private int _workPeriodId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Preview Header
    [ObservableProperty]
    private DateTime _workPeriodStart;

    [ObservableProperty]
    private string _workPeriodDuration = string.Empty;

    [ObservableProperty]
    private int _terminalCount;

    [ObservableProperty]
    private int _completedZReportCount;

    [ObservableProperty]
    private int _pendingZReportCount;

    // Combined Totals
    [ObservableProperty]
    private decimal _totalGrossSales;

    [ObservableProperty]
    private decimal _totalNetSales;

    [ObservableProperty]
    private decimal _totalGrandTotal;

    [ObservableProperty]
    private int _totalTransactionCount;

    [ObservableProperty]
    private decimal _totalExpectedCash;

    [ObservableProperty]
    private decimal _totalActualCash;

    [ObservableProperty]
    private decimal _totalVariance;

    // Collections
    [ObservableProperty]
    private ObservableCollection<TerminalZSummaryItem> _terminalBreakdown = [];

    // Validation
    [ObservableProperty]
    private bool _allTerminalsReady;

    [ObservableProperty]
    private ObservableCollection<ValidationIssueItem> _validationIssues = [];

    [ObservableProperty]
    private int _totalUnsettledReceipts;

    [ObservableProperty]
    private int _totalOpenOrders;

    public CombinedZReportViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        IReportPrintService reportPrintService,
        ILogger<CombinedZReportViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _reportPrintService = reportPrintService;
        _logger = logger;
    }

    public void SetWorkPeriodId(int workPeriodId)
    {
        _workPeriodId = workPeriodId;
    }

    public async Task OnNavigatedToAsync()
    {
        await LoadPreviewAsync();
    }

    public Task OnNavigatedFromAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPreviewAsync();
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var combinedReportService = scope.ServiceProvider.GetRequiredService<ICombinedReportService>();

            // Get current work period if not set
            if (_workPeriodId == 0)
            {
                var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
                var currentWorkPeriod = await workPeriodService.GetCurrentWorkPeriodAsync();
                if (currentWorkPeriod == null)
                {
                    await _dialogService.ShowErrorAsync("No active work period found.");
                    return;
                }
                _workPeriodId = currentWorkPeriod.Id;
            }

            // Get the combined Z-Report preview data
            var preview = await combinedReportService.PreviewCombinedZReportAsync(_workPeriodId);

            // Print the report using the print service
            await _reportPrintService.PrintCombinedZReportAsync(preview);

            _logger.LogInformation("Combined Z-Report preview printed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print combined Z-Report preview");
            ErrorMessage = $"Failed to print: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            // Show format selection dialog
            var format = await _dialogService.ShowExportFormatDialogAsync();
            if (format == null) return; // User cancelled

            using var scope = _scopeFactory.CreateScope();
            var combinedReportService = scope.ServiceProvider.GetRequiredService<ICombinedReportService>();
            var reportExportService = scope.ServiceProvider.GetRequiredService<IReportExportService>();

            // Get current work period if not set
            if (_workPeriodId == 0)
            {
                var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
                var currentWorkPeriod = await workPeriodService.GetCurrentWorkPeriodAsync();
                if (currentWorkPeriod == null)
                {
                    await _dialogService.ShowErrorAsync("No active work period found.");
                    return;
                }
                _workPeriodId = currentWorkPeriod.Id;
            }

            // Get preview data
            var preview = await combinedReportService.PreviewCombinedZReportAsync(_workPeriodId);

            // Export the report
            var exportedPath = await reportExportService.ExportCombinedZReportAsync(preview, format.Value);

            if (!string.IsNullOrEmpty(exportedPath))
            {
                await _dialogService.ShowMessageAsync("Export Complete", $"Report exported to:\n{exportedPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export combined Z-Report preview");
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewTerminalZReportAsync(TerminalZSummaryItem? terminal)
    {
        if (terminal == null) return;

        if (!terminal.HasZReport)
        {
            await _dialogService.ShowMessageAsync(
                "No Z-Report",
                $"Terminal {terminal.TerminalCode} has not generated a Z-Report yet.");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var zReportService = scope.ServiceProvider.GetRequiredService<IZReportService>();

            // Get the Z-Report for this terminal
            var zReport = await zReportService.GetZReportByIdAsync(terminal.ZReportId!.Value);
            if (zReport != null)
            {
                // Convert to ZReport model and show dialog
                await _dialogService.ShowZReportDialogAsync(ConvertToZReportModel(zReport));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to view terminal {TerminalId} Z-Report", terminal.TerminalId);
            ErrorMessage = $"Failed to load Z-Report: {ex.Message}";
        }
    }

    private async Task LoadPreviewAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            using var scope = _scopeFactory.CreateScope();
            var combinedReportService = scope.ServiceProvider.GetRequiredService<ICombinedReportService>();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            // Get current work period if not set
            if (_workPeriodId == 0)
            {
                var currentWorkPeriod = await workPeriodService.GetCurrentWorkPeriodAsync();
                if (currentWorkPeriod == null)
                {
                    ErrorMessage = "No active work period found.";
                    return;
                }
                _workPeriodId = currentWorkPeriod.Id;
            }

            // Get preview
            var preview = await combinedReportService.PreviewCombinedZReportAsync(_workPeriodId);

            // Populate header
            WorkPeriodStart = preview.WorkPeriodStart;
            WorkPeriodDuration = FormatDuration(DateTime.UtcNow - preview.WorkPeriodStart);
            TerminalCount = preview.TerminalCount;
            CompletedZReportCount = preview.CompletedZReportCount;
            PendingZReportCount = preview.PendingZReportCount;

            // Populate totals
            TotalGrossSales = preview.TotalGrossSales;
            TotalNetSales = preview.TotalNetSales;
            TotalGrandTotal = preview.TotalGrandTotal;
            TotalTransactionCount = preview.TotalTransactionCount;

            // Calculate combined cash drawer totals
            TotalExpectedCash = preview.TerminalBreakdown.Sum(t => t.ExpectedCash);
            TotalActualCash = preview.TerminalBreakdown.Where(t => t.ActualCash.HasValue).Sum(t => t.ActualCash!.Value);
            TotalVariance = preview.TerminalBreakdown.Where(t => t.Variance.HasValue).Sum(t => t.Variance!.Value);

            // Populate terminal breakdown
            TerminalBreakdown = new ObservableCollection<TerminalZSummaryItem>(
                preview.TerminalBreakdown.Select(t => new TerminalZSummaryItem
                {
                    TerminalId = t.TerminalId,
                    TerminalCode = t.TerminalCode,
                    TerminalName = t.TerminalName,
                    HasZReport = t.HasZReport,
                    ZReportId = t.ZReportId,
                    ZReportNumber = t.ZReportNumber,
                    GrossSales = t.GrossSales,
                    NetSales = t.NetSales,
                    GrandTotal = t.GrandTotal,
                    TransactionCount = t.TransactionCount,
                    ExpectedCash = t.ExpectedCash,
                    ActualCash = t.ActualCash,
                    Variance = t.Variance
                }));

            // Get validation
            var validation = await combinedReportService.ValidateCombinedZReportAsync(_workPeriodId);
            AllTerminalsReady = validation.AllTerminalsReady;
            TotalUnsettledReceipts = validation.TotalUnsettledReceiptCount;
            TotalOpenOrders = validation.TotalOpenOrderCount;

            ValidationIssues = new ObservableCollection<ValidationIssueItem>(
                validation.TerminalIssues.Select(ti => new ValidationIssueItem
                {
                    TerminalCode = ti.TerminalCode,
                    Issues = string.Join("; ", ti.Issues)
                }));

            _logger.LogInformation(
                "Loaded combined Z-Report preview: {TerminalCount} terminals, {CompletedCount} completed, {PendingCount} pending",
                TerminalCount, CompletedZReportCount, PendingZReportCount);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load preview: {ex.Message}";
            _logger.LogError(ex, "Failed to load combined Z-Report preview");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return $"{hours}h {minutes:D2}m";
    }

    private static HospitalityPOS.Core.Models.Reports.ZReport ConvertToZReportModel(
        HospitalityPOS.Core.Entities.ZReportRecord record)
    {
        return new HospitalityPOS.Core.Models.Reports.ZReport
        {
            ZReportNumber = record.ReportNumber,
            ReportNumberFormatted = record.ReportNumberFormatted ?? $"Z-{record.ReportNumber:D4}",
            TerminalId = record.TerminalId ?? 0,
            TerminalCode = record.TerminalCode ?? "",
            WorkPeriodId = record.WorkPeriodId,
            WorkPeriodOpenedAt = record.PeriodOpenedAt,
            WorkPeriodClosedAt = record.PeriodClosedAt,
            Duration = record.PeriodClosedAt - record.PeriodOpenedAt,
            GrossSales = record.GrossSales,
            NetSales = record.NetSales,
            TaxCollected = record.TaxCollected,
            TotalDiscounts = record.TotalDiscounts,
            GrandTotal = record.GrandTotal,
            TransactionCount = record.TransactionCount,
            OpeningFloat = record.OpeningFloat,
            CashSales = record.CashSales,
            CashPayouts = record.CashPayouts,
            ExpectedCash = record.ExpectedCash,
            ActualCash = record.ActualCash,
            Variance = record.Variance
        };
    }
}

/// <summary>
/// Terminal Z-Report summary item for display.
/// </summary>
public class TerminalZSummaryItem
{
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public bool HasZReport { get; set; }
    public int? ZReportId { get; set; }
    public string? ZReportNumber { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal GrandTotal { get; set; }
    public int TransactionCount { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Variance { get; set; }

    public string StatusText => HasZReport ? "Completed" : "Pending";
    public string StatusColor => HasZReport ? "#4CAF50" : "#FFB347";
    public string VarianceDisplay => Variance.HasValue
        ? (Variance.Value >= 0 ? $"KSh {Variance.Value:N2}" : $"-KSh {Math.Abs(Variance.Value):N2}")
        : "-";
    public string VarianceColor => Variance.HasValue
        ? (Variance.Value == 0 ? "#4CAF50" : (Variance.Value > 0 ? "#FFB347" : "#FF6B6B"))
        : "#8888AA";
}

/// <summary>
/// Validation issue item for display.
/// </summary>
public class ValidationIssueItem
{
    public string TerminalCode { get; set; } = string.Empty;
    public string Issues { get; set; } = string.Empty;
}
