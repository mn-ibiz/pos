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
/// ViewModel for the X-Report History view.
/// Allows viewing and reprinting historical X-Reports by terminal.
/// </summary>
public partial class XReportHistoryViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ITerminalSessionContext _terminalContext;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the start date for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-7);

    /// <summary>
    /// Gets or sets the end date for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the selected terminal for filtering.
    /// </summary>
    [ObservableProperty]
    private Terminal? _selectedTerminal;

    /// <summary>
    /// Gets or sets the list of terminals for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Terminal> _terminals = [];

    /// <summary>
    /// Gets or sets the list of X-Reports.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<XReportSummary> _xReports = [];

    /// <summary>
    /// Gets or sets the selected X-Report.
    /// </summary>
    [ObservableProperty]
    private XReportSummary? _selectedXReport;

    /// <summary>
    /// Gets the total count of X-Reports displayed.
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// Gets the total net sales across displayed X-Reports.
    /// </summary>
    [ObservableProperty]
    private decimal _totalNetSales;

    /// <summary>
    /// Indicates whether to show only current terminal's reports.
    /// </summary>
    [ObservableProperty]
    private bool _showCurrentTerminalOnly = true;

    /// <summary>
    /// Current X-Report data for display in the details panel.
    /// </summary>
    [ObservableProperty]
    private XReportData? _currentReportData;

    /// <summary>
    /// Whether to show the details panel.
    /// </summary>
    [ObservableProperty]
    private bool _showDetailsPanel;

    #endregion

    public XReportHistoryViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ITerminalSessionContext terminalContext)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _terminalContext = terminalContext ?? throw new ArgumentNullException(nameof(terminalContext));

        Title = "X-Report History";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    #region Commands

    /// <summary>
    /// Loads the X-Report history and terminals.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminalService = scope.ServiceProvider.GetRequiredService<ITerminalService>();

            // Load terminals for filter dropdown
            var allTerminals = await terminalService.GetAllTerminalsAsync();
            Terminals = new ObservableCollection<Terminal>(
                allTerminals.Where(t => t.IsActive).OrderBy(t => t.Code));

            // Set default to current terminal if available
            if (_terminalContext.CurrentTerminalId.HasValue && ShowCurrentTerminalOnly)
            {
                SelectedTerminal = Terminals.FirstOrDefault(t => t.Id == _terminalContext.CurrentTerminalId.Value);
            }

            // Load X-Report history
            await RefreshXReportsAsync();
        }, "Loading X-Report history...");
    }

    /// <summary>
    /// Refreshes the X-Reports based on current filters.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ExecuteAsync(async () =>
        {
            await RefreshXReportsAsync();
        }, "Refreshing...");
    }

    private async Task RefreshXReportsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var xReportService = scope.ServiceProvider.GetRequiredService<IXReportService>();

        var terminalId = SelectedTerminal?.Id ?? (_terminalContext.CurrentTerminalId ?? 0);

        var reports = await xReportService.GetXReportHistoryByTerminalAsync(
            terminalId,
            StartDate,
            EndDate.AddDays(1));

        var summaries = reports
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new XReportSummary
            {
                Id = r.Id,
                ReportNumber = r.ReportNumber,
                GeneratedAt = r.GeneratedAt,
                GeneratedByName = r.GeneratedByName,
                TerminalCode = r.TerminalCode,
                GrossSales = r.GrossSales,
                NetSales = r.NetSales,
                TaxAmount = r.TaxAmount,
                TotalPayments = r.TotalPayments,
                ExpectedCash = r.ExpectedCash,
                TransactionCount = r.TransactionCount
            })
            .ToList();

        XReports = new ObservableCollection<XReportSummary>(summaries);
        TotalCount = summaries.Count;
        TotalNetSales = summaries.Sum(r => r.NetSales);
    }

    /// <summary>
    /// Generates a new X-Report for the current terminal.
    /// </summary>
    [RelayCommand]
    private async Task GenerateNewXReportAsync()
    {
        if (!_terminalContext.CurrentTerminalId.HasValue)
        {
            await _dialogService.ShowErrorAsync("No Terminal", "No terminal is currently configured for this workstation.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Generate X-Report",
            "This will generate a new X-Report for the current shift. Continue?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var xReportService = scope.ServiceProvider.GetRequiredService<IXReportService>();

            var report = await xReportService.GenerateXReportAsync(_terminalContext.CurrentTerminalId.Value);

            // Save the report
            await xReportService.SaveXReportAsync(report);

            CurrentReportData = report;
            ShowDetailsPanel = true;

            await _dialogService.ShowXReportDialogAsync(report, autoPrint: true);

            _logger.Information("X-Report {ReportNumber} generated for terminal {TerminalCode}",
                report.ReportNumber, report.TerminalCode);

            // Refresh the list
            await RefreshXReportsAsync();
        }, "Generating X-Report...");
    }

    /// <summary>
    /// Views the selected X-Report.
    /// </summary>
    [RelayCommand]
    private async Task ViewXReportAsync()
    {
        if (SelectedXReport == null)
        {
            await _dialogService.ShowErrorAsync("No Selection", "Please select an X-Report to view.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var xReportService = scope.ServiceProvider.GetRequiredService<IXReportService>();

            // Regenerate report for the work period at that time
            // Note: For historical accuracy, we would need to load from stored data
            var report = await xReportService.GenerateXReportForWorkPeriodAsync(
                SelectedXReport.WorkPeriodId,
                SelectedXReport.TerminalId);

            CurrentReportData = report;
            ShowDetailsPanel = true;

            await _dialogService.ShowXReportDialogAsync(report, autoPrint: false);
        }, "Loading X-Report...");
    }

    /// <summary>
    /// Reprints the selected X-Report.
    /// </summary>
    [RelayCommand]
    private async Task ReprintXReportAsync()
    {
        if (SelectedXReport == null)
        {
            await _dialogService.ShowErrorAsync("No Selection", "Please select an X-Report to reprint.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Reprint X-Report",
            $"Are you sure you want to reprint X-Report {SelectedXReport.ReportNumber}?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var xReportService = scope.ServiceProvider.GetRequiredService<IXReportService>();

            var report = await xReportService.GenerateXReportForWorkPeriodAsync(
                SelectedXReport.WorkPeriodId,
                SelectedXReport.TerminalId);

            await _dialogService.ShowXReportDialogAsync(report, autoPrint: true);

            _logger.Information("X-Report {ReportNumber} reprinted", SelectedXReport.ReportNumber);
        }, "Reprinting X-Report...");
    }

    /// <summary>
    /// Clears the terminal filter.
    /// </summary>
    [RelayCommand]
    private void ClearTerminalFilter()
    {
        SelectedTerminal = null;
        _ = RefreshAsync();
    }

    /// <summary>
    /// Closes the details panel.
    /// </summary>
    [RelayCommand]
    private void CloseDetailsPanel()
    {
        ShowDetailsPanel = false;
        CurrentReportData = null;
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    partial void OnStartDateChanged(DateTime value)
    {
        _ = RefreshAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        _ = RefreshAsync();
    }

    partial void OnSelectedTerminalChanged(Terminal? value)
    {
        _ = RefreshAsync();
    }

    partial void OnShowCurrentTerminalOnlyChanged(bool value)
    {
        if (value && _terminalContext.CurrentTerminalId.HasValue)
        {
            SelectedTerminal = Terminals.FirstOrDefault(t => t.Id == _terminalContext.CurrentTerminalId.Value);
        }
        else
        {
            SelectedTerminal = null;
        }
        _ = RefreshAsync();
    }
}

/// <summary>
/// Summary model for X-Report display in the history list.
/// </summary>
public class XReportSummary
{
    public int Id { get; set; }
    public int WorkPeriodId { get; set; }
    public int TerminalId { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string TerminalCode { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedByName { get; set; } = string.Empty;
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ExpectedCash { get; set; }
    public int TransactionCount { get; set; }

    public string DateDisplay => GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd");
    public string TimeDisplay => GeneratedAt.ToLocalTime().ToString("HH:mm:ss");
}
