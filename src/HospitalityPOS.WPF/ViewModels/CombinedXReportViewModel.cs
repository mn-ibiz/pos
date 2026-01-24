using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Combined X-Report view.
/// </summary>
public partial class CombinedXReportViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CombinedXReportViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Report Header
    [ObservableProperty]
    private string _businessName = string.Empty;

    [ObservableProperty]
    private string _reportNumber = string.Empty;

    [ObservableProperty]
    private DateTime _generatedAt;

    [ObservableProperty]
    private string _generatedByName = string.Empty;

    [ObservableProperty]
    private DateTime _shiftStarted;

    [ObservableProperty]
    private string _shiftDurationFormatted = string.Empty;

    [ObservableProperty]
    private int _terminalCount;

    // Combined Totals
    [ObservableProperty]
    private decimal _grossSales;

    [ObservableProperty]
    private decimal _discounts;

    [ObservableProperty]
    private decimal _refunds;

    [ObservableProperty]
    private decimal _netSales;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _tipsCollected;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private decimal _averageTransaction;

    [ObservableProperty]
    private int _voidCount;

    [ObservableProperty]
    private int _refundCount;

    [ObservableProperty]
    private int _discountCount;

    // Cash Drawer
    [ObservableProperty]
    private decimal _openingFloat;

    [ObservableProperty]
    private decimal _cashReceived;

    [ObservableProperty]
    private decimal _cashRefunds;

    [ObservableProperty]
    private decimal _cashPayouts;

    [ObservableProperty]
    private decimal _expectedCash;

    // Collections
    [ObservableProperty]
    private ObservableCollection<TerminalSummaryItem> _terminalBreakdown = [];

    [ObservableProperty]
    private ObservableCollection<PaymentBreakdownItem> _paymentBreakdown = [];

    [ObservableProperty]
    private ObservableCollection<CashierSessionItem> _cashierSessions = [];

    [ObservableProperty]
    private TerminalSummaryItem? _selectedTerminal;

    public CombinedXReportViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ILogger<CombinedXReportViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _logger = logger;
    }

    public async Task OnNavigatedToAsync()
    {
        await LoadReportAsync();
    }

    public Task OnNavigatedFromAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        try
        {
            // TODO: Implement print functionality for combined report
            await _dialogService.ShowMessageAsync("Print", "Print functionality will be implemented in MT-026.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print combined X-Report");
            ErrorMessage = $"Failed to print: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            // TODO: Implement export functionality
            await _dialogService.ShowMessageAsync("Export", "Export functionality will be implemented in MT-025.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export combined X-Report");
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewTerminalDetailsAsync(TerminalSummaryItem? terminal)
    {
        if (terminal == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var xReportService = scope.ServiceProvider.GetRequiredService<IXReportService>();

            // Get the X-Report for the specific terminal
            var xReport = await xReportService.GenerateXReportAsync(terminal.TerminalId);

            await _dialogService.ShowXReportDialogAsync(xReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to view terminal {TerminalId} details", terminal.TerminalId);
            ErrorMessage = $"Failed to load terminal details: {ex.Message}";
        }
    }

    private async Task LoadReportAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            using var scope = _scopeFactory.CreateScope();
            var combinedReportService = scope.ServiceProvider.GetRequiredService<ICombinedReportService>();

            var report = await combinedReportService.GenerateCombinedXReportAsync();

            // Populate header
            BusinessName = report.BusinessName;
            ReportNumber = report.ReportNumber;
            GeneratedAt = report.GeneratedAt;
            GeneratedByName = report.GeneratedByName;
            ShiftStarted = report.ShiftStarted;
            ShiftDurationFormatted = report.ShiftDurationFormatted;
            TerminalCount = report.TerminalCount;

            // Populate totals
            GrossSales = report.GrossSales;
            Discounts = report.Discounts;
            Refunds = report.Refunds;
            NetSales = report.NetSales;
            TaxAmount = report.TaxAmount;
            TipsCollected = report.TipsCollected;
            GrandTotal = report.GrandTotal;
            TransactionCount = report.TransactionCount;
            AverageTransaction = report.AverageTransaction;
            VoidCount = report.VoidCount;
            RefundCount = report.RefundCount;
            DiscountCount = report.DiscountCount;

            // Populate cash drawer
            OpeningFloat = report.OpeningFloat;
            CashReceived = report.CashReceived;
            CashRefunds = report.CashRefunds;
            CashPayouts = report.CashPayouts;
            ExpectedCash = report.ExpectedCash;

            // Populate terminal breakdown
            TerminalBreakdown = new ObservableCollection<TerminalSummaryItem>(
                report.TerminalBreakdown.Select(t => new TerminalSummaryItem
                {
                    TerminalId = t.TerminalId,
                    TerminalCode = t.TerminalCode,
                    TerminalName = t.TerminalName,
                    IsOnline = t.IsOnline,
                    GrossSales = t.GrossSales,
                    NetSales = t.NetSales,
                    GrandTotal = t.GrandTotal,
                    TransactionCount = t.TransactionCount,
                    VoidCount = t.VoidCount,
                    RefundCount = t.RefundCount,
                    ExpectedCash = t.ExpectedCash
                }));

            // Populate payment breakdown
            PaymentBreakdown = new ObservableCollection<PaymentBreakdownItem>(
                report.PaymentBreakdown.Select(p => new PaymentBreakdownItem
                {
                    PaymentMethodName = p.PaymentMethodName,
                    Amount = p.Amount,
                    TransactionCount = p.TransactionCount,
                    Percentage = report.GrandTotal > 0 ? (p.Amount / report.GrandTotal) * 100 : 0
                }));

            // Populate cashier sessions
            CashierSessions = new ObservableCollection<CashierSessionItem>(
                report.CashierSessions.Select(cs => new CashierSessionItem
                {
                    CashierName = cs.CashierName,
                    TerminalCode = cs.TerminalCode,
                    StartTime = cs.StartTime,
                    EndTime = cs.EndTime,
                    DurationFormatted = cs.DurationFormatted,
                    TransactionCount = cs.TransactionCount,
                    SalesTotal = cs.SalesTotal
                }));

            _logger.LogInformation("Loaded combined X-Report: {TerminalCount} terminals, {TransactionCount} transactions",
                TerminalCount, TransactionCount);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No active work period"))
        {
            ErrorMessage = "No active work period. Start a work period to generate reports.";
            _logger.LogWarning("No active work period for combined X-Report");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load report: {ex.Message}";
            _logger.LogError(ex, "Failed to load combined X-Report");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Terminal summary item for display.
/// </summary>
public class TerminalSummaryItem
{
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal GrandTotal { get; set; }
    public int TransactionCount { get; set; }
    public int VoidCount { get; set; }
    public int RefundCount { get; set; }
    public decimal ExpectedCash { get; set; }

    public string StatusText => IsOnline ? "Online" : "Offline";
    public string StatusColor => IsOnline ? "#4CAF50" : "#F44336";
}

/// <summary>
/// Payment breakdown item for display.
/// </summary>
public class PaymentBreakdownItem
{
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Cashier session item for display.
/// </summary>
public class CashierSessionItem
{
    public string CashierName { get; set; } = string.Empty;
    public string TerminalCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal SalesTotal { get; set; }

    public string TimeRange => EndTime.HasValue
        ? $"{StartTime.ToLocalTime():HH:mm} - {EndTime.Value.ToLocalTime():HH:mm}"
        : $"{StartTime.ToLocalTime():HH:mm} - Active";
}
