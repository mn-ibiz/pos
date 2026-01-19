using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the sales reports view.
/// Handles report type selection, date filtering, and report generation.
/// </summary>
public partial class SalesReportsViewModel : ViewModelBase, INavigationAware
{
    private readonly IReportService _reportService;
    private readonly IReportPrintService _reportPrintService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;
    private readonly ISessionService _sessionService;
    private readonly Func<ExportDialogViewModel> _exportDialogFactory;

    #region Observable Properties

    /// <summary>
    /// Gets the available report types.
    /// </summary>
    public ObservableCollection<SalesReportTypeItem> ReportTypes { get; } =
    [
        new SalesReportTypeItem(SalesReportType.DailySummary, "Daily Sales Summary", "Overview of daily sales, discounts, and voids"),
        new SalesReportTypeItem(SalesReportType.ByProduct, "Sales by Product", "Breakdown of sales per product"),
        new SalesReportTypeItem(SalesReportType.ByCategory, "Sales by Category", "Breakdown of sales per category"),
        new SalesReportTypeItem(SalesReportType.ByCashier, "Sales by Cashier", "Performance breakdown by staff member"),
        new SalesReportTypeItem(SalesReportType.ByPaymentMethod, "Sales by Payment Method", "Payment method breakdown"),
        new SalesReportTypeItem(SalesReportType.HourlySales, "Hourly Sales Analysis", "Sales distribution across hours")
    ];

    /// <summary>
    /// Gets or sets the selected report type.
    /// </summary>
    [ObservableProperty]
    private SalesReportTypeItem? _selectedReportType;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    /// <summary>
    /// Gets or sets whether a report is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasReport;

    /// <summary>
    /// Gets or sets the current report result.
    /// </summary>
    [ObservableProperty]
    private SalesReportResult? _currentReport;

    /// <summary>
    /// Gets or sets the daily summary data.
    /// </summary>
    [ObservableProperty]
    private DailySalesSummary? _dailySummary;

    /// <summary>
    /// Gets or sets the product sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductSalesReport> _productSales = [];

    /// <summary>
    /// Gets or sets the category sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategorySalesReport> _categorySales = [];

    /// <summary>
    /// Gets or sets the cashier sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CashierSalesReport> _cashierSales = [];

    /// <summary>
    /// Gets or sets the payment method sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentMethodSalesReport> _paymentMethodSales = [];

    /// <summary>
    /// Gets or sets the hourly sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HourlySalesReport> _hourlySales = [];

    /// <summary>
    /// Gets or sets the visibility of the daily summary section.
    /// </summary>
    [ObservableProperty]
    private bool _showDailySummary;

    /// <summary>
    /// Gets or sets the visibility of the product sales section.
    /// </summary>
    [ObservableProperty]
    private bool _showProductSales;

    /// <summary>
    /// Gets or sets the visibility of the category sales section.
    /// </summary>
    [ObservableProperty]
    private bool _showCategorySales;

    /// <summary>
    /// Gets or sets the visibility of the cashier sales section.
    /// </summary>
    [ObservableProperty]
    private bool _showCashierSales;

    /// <summary>
    /// Gets or sets the visibility of the payment method sales section.
    /// </summary>
    [ObservableProperty]
    private bool _showPaymentMethodSales;

    /// <summary>
    /// Gets or sets the visibility of the hourly sales section.
    /// </summary>
    [ObservableProperty]
    private bool _showHourlySales;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesReportsViewModel"/> class.
    /// </summary>
    /// <param name="reportService">The report service.</param>
    /// <param name="reportPrintService">The report print service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="exportService">The export service.</param>
    /// <param name="sessionService">The session service.</param>
    /// <param name="exportDialogFactory">Factory for creating export dialog view models.</param>
    /// <param name="logger">The logger.</param>
    public SalesReportsViewModel(
        IReportService reportService,
        IReportPrintService reportPrintService,
        INavigationService navigationService,
        IExportService exportService,
        ISessionService sessionService,
        Func<ExportDialogViewModel> exportDialogFactory,
        ILogger logger) : base(logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _reportPrintService = reportPrintService ?? throw new ArgumentNullException(nameof(reportPrintService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _exportDialogFactory = exportDialogFactory ?? throw new ArgumentNullException(nameof(exportDialogFactory));

        Title = "Sales Reports";
        SelectedReportType = ReportTypes.First();
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // No initialization needed
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    /// <summary>
    /// Generates the selected report.
    /// </summary>
    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (SelectedReportType == null)
        {
            ErrorMessage = "Please select a report type.";
            return;
        }

        if (FromDate > ToDate)
        {
            ErrorMessage = "From date cannot be after To date.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var parameters = new SalesReportParameters
            {
                StartDate = FromDate.Date,
                EndDate = ToDate.Date.AddDays(1).AddTicks(-1), // End of day
                GeneratedByUserId = _sessionService.CurrentUserId
            };

            CurrentReport = await _reportService.GenerateSalesReportAsync(
                SelectedReportType.ReportType,
                parameters);

            // Update view data
            DailySummary = CurrentReport.Summary;
            ProductSales = new ObservableCollection<ProductSalesReport>(CurrentReport.ProductSales);
            CategorySales = new ObservableCollection<CategorySalesReport>(CurrentReport.CategorySales);
            CashierSales = new ObservableCollection<CashierSalesReport>(CurrentReport.CashierSales);
            PaymentMethodSales = new ObservableCollection<PaymentMethodSalesReport>(CurrentReport.PaymentMethodSales);
            HourlySales = new ObservableCollection<HourlySalesReport>(CurrentReport.HourlySales);

            // Set visibility based on report type
            UpdateSectionVisibility(SelectedReportType.ReportType);

            HasReport = true;

            _logger.Information("Generated {ReportType} report for {FromDate} to {ToDate}",
                SelectedReportType.Name, FromDate, ToDate);

        }, "Generating report...");
    }

    /// <summary>
    /// Prints the current report.
    /// </summary>
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (CurrentReport == null)
        {
            ErrorMessage = "No report to print. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var printed = await Task.Run(() => _reportPrintService.PrintSalesReport(CurrentReport));

            if (printed)
            {
                _logger.Information("Sales report printed successfully for {ReportType}",
                    SelectedReportType?.Name ?? "Unknown");
            }
            else
            {
                _logger.Information("Print operation was cancelled by user");
            }
        }, "Preparing print...");
    }

    /// <summary>
    /// Exports the current report to CSV.
    /// </summary>
    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (CurrentReport == null || SelectedReportType == null)
        {
            ErrorMessage = "No report to export. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Use factory instead of service locator
            var dialogViewModel = _exportDialogFactory();
            dialogViewModel.Initialize(SelectedReportType.Name, FromDate, ToDate);
            dialogViewModel.ExportAction = ExportCurrentReportTypeAsync;

            // Show dialog on UI thread without blocking async context
            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new ExportDialog { DataContext = dialogViewModel };
                return dialog.ShowDialog();
            });

            if (result == true)
            {
                await DialogService.ShowMessageAsync(
                    "Export Complete",
                    $"Report exported successfully to:\n{dialogViewModel.FilePath}");

                _logger.Information("Sales report exported to {FilePath}", dialogViewModel.FilePath);
            }
        }, "Preparing export...");
    }

    private async Task ExportCurrentReportTypeAsync(string filePath, CancellationToken ct)
    {
        switch (SelectedReportType?.ReportType)
        {
            case SalesReportType.ByProduct:
                await _exportService.ExportToCsvAsync(ProductSales.ToList(), filePath, ct);
                break;
            case SalesReportType.ByCategory:
                await _exportService.ExportToCsvAsync(CategorySales.ToList(), filePath, ct);
                break;
            case SalesReportType.ByCashier:
                await _exportService.ExportToCsvAsync(CashierSales.ToList(), filePath, ct);
                break;
            case SalesReportType.ByPaymentMethod:
                await _exportService.ExportToCsvAsync(PaymentMethodSales.ToList(), filePath, ct);
                break;
            case SalesReportType.HourlySales:
                await _exportService.ExportToCsvAsync(HourlySales.ToList(), filePath, ct);
                break;
            default:
                if (DailySummary != null)
                {
                    await _exportService.ExportToCsvAsync(new List<DailySalesSummary> { DailySummary }, filePath, ct);
                }
                break;
        }
    }

    /// <summary>
    /// Sets the date range to today.
    /// </summary>
    [RelayCommand]
    private void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Sets the date range to yesterday.
    /// </summary>
    [RelayCommand]
    private void SetYesterday()
    {
        FromDate = DateTime.Today.AddDays(-1);
        ToDate = DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Sets the date range to this week.
    /// </summary>
    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var sunday = today.AddDays(-dayOfWeek);
        FromDate = sunday;
        ToDate = today;
    }

    /// <summary>
    /// Sets the date range to this month.
    /// </summary>
    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
    }

    /// <summary>
    /// Sets the date range to last week (Monday to Sunday).
    /// </summary>
    [RelayCommand]
    private void SetLastWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var thisSunday = today.AddDays(-dayOfWeek);
        var lastMonday = thisSunday.AddDays(-6);
        FromDate = lastMonday;
        ToDate = thisSunday;
    }

    /// <summary>
    /// Sets the date range to last month.
    /// </summary>
    [RelayCommand]
    private void SetLastMonth()
    {
        var today = DateTime.Today;
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
        var lastOfPreviousMonth = firstOfThisMonth.AddDays(-1);
        var firstOfPreviousMonth = new DateTime(lastOfPreviousMonth.Year, lastOfPreviousMonth.Month, 1);
        FromDate = firstOfPreviousMonth;
        ToDate = lastOfPreviousMonth;
    }

    /// <summary>
    /// Sets the date range to year to date.
    /// </summary>
    [RelayCommand]
    private void SetYearToDate()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, 1, 1);
        ToDate = today;
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private void UpdateSectionVisibility(SalesReportType reportType)
    {
        // Always show summary
        ShowDailySummary = true;

        // Show specific section based on report type
        ShowProductSales = reportType == SalesReportType.ByProduct;
        ShowCategorySales = reportType == SalesReportType.ByCategory;
        ShowCashierSales = reportType == SalesReportType.ByCashier;
        ShowPaymentMethodSales = reportType == SalesReportType.ByPaymentMethod;
        ShowHourlySales = reportType == SalesReportType.HourlySales;
    }

    partial void OnSelectedReportTypeChanged(SalesReportTypeItem? value)
    {
        // Clear current report when type changes
        HasReport = false;
        CurrentReport = null;
    }
}

/// <summary>
/// Represents a sales report type item for the UI.
/// </summary>
public class SalesReportTypeItem
{
    /// <summary>
    /// Gets the report type.
    /// </summary>
    public SalesReportType ReportType { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesReportTypeItem"/> class.
    /// </summary>
    public SalesReportTypeItem(SalesReportType reportType, string name, string description)
    {
        ReportType = reportType;
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}
