using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Represents an inventory report type for the UI.
/// </summary>
public class InventoryReportTypeItem
{
    /// <summary>
    /// Gets the report type identifier.
    /// </summary>
    public string ReportType { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets whether this report type requires date range.
    /// </summary>
    public bool RequiresDateRange { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryReportTypeItem"/> class.
    /// </summary>
    public InventoryReportTypeItem(string reportType, string name, string description, bool requiresDateRange = false)
    {
        ReportType = reportType;
        Name = name;
        Description = description;
        RequiresDateRange = requiresDateRange;
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// ViewModel for the inventory reports view.
/// Handles current stock, low stock, stock movement, valuation, and dead stock reports.
/// </summary>
public partial class InventoryReportsViewModel : ViewModelBase, INavigationAware
{
    private readonly IReportService _reportService;
    private readonly IReportPrintService _reportPrintService;
    private readonly ICategoryService _categoryService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    #region Observable Properties

    /// <summary>
    /// Gets the available report types.
    /// </summary>
    public ObservableCollection<InventoryReportTypeItem> ReportTypes { get; } =
    [
        new InventoryReportTypeItem("CurrentStock", "Current Stock", "Shows current stock levels for all products"),
        new InventoryReportTypeItem("LowStock", "Low Stock Alert", "Shows products below minimum stock levels"),
        new InventoryReportTypeItem("StockMovement", "Stock Movement", "Shows stock movement history within date range", true),
        new InventoryReportTypeItem("StockValuation", "Stock Valuation", "Shows stock value by category"),
        new InventoryReportTypeItem("DeadStock", "Dead Stock", "Shows products with no movement for specified days")
    ];

    /// <summary>
    /// Gets or sets the selected report type.
    /// </summary>
    [ObservableProperty]
    private InventoryReportTypeItem? _selectedReportType;

    /// <summary>
    /// Gets or sets the from date (for movement report).
    /// </summary>
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-30);

    /// <summary>
    /// Gets or sets the to date (for movement report).
    /// </summary>
    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the dead stock days threshold.
    /// </summary>
    [ObservableProperty]
    private int _deadStockDays = 30;

    /// <summary>
    /// Gets or sets whether to include out of stock items.
    /// </summary>
    [ObservableProperty]
    private bool _includeOutOfStock = true;

    /// <summary>
    /// Gets or sets the available categories for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    [ObservableProperty]
    private Category? _selectedCategory;

    /// <summary>
    /// Gets or sets whether a report is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasReport;

    /// <summary>
    /// Gets or sets whether the date range filter is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showDateRange;

    /// <summary>
    /// Gets or sets whether the dead stock days filter is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showDeadStockFilter;

    // Current Stock Report
    [ObservableProperty]
    private CurrentStockReportResult? _currentStockReport;

    [ObservableProperty]
    private ObservableCollection<CurrentStockItem> _currentStockItems = [];

    [ObservableProperty]
    private bool _showCurrentStockReport;

    // Low Stock Report
    [ObservableProperty]
    private LowStockReportResult? _lowStockReport;

    [ObservableProperty]
    private ObservableCollection<LowStockItem> _lowStockItems = [];

    [ObservableProperty]
    private bool _showLowStockReport;

    // Stock Movement Report
    [ObservableProperty]
    private StockMovementReportResult? _stockMovementReport;

    [ObservableProperty]
    private ObservableCollection<StockMovementItem> _stockMovementItems = [];

    [ObservableProperty]
    private bool _showStockMovementReport;

    // Stock Valuation Report
    [ObservableProperty]
    private StockValuationReportResult? _stockValuationReport;

    [ObservableProperty]
    private ObservableCollection<CategoryValuation> _categoryValuations = [];

    [ObservableProperty]
    private bool _showStockValuationReport;

    // Dead Stock Report
    [ObservableProperty]
    private DeadStockReportResult? _deadStockReport;

    [ObservableProperty]
    private ObservableCollection<HospitalityPOS.Core.Models.Reports.DeadStockItem> _deadStockItems = [];

    [ObservableProperty]
    private bool _showDeadStockReport;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryReportsViewModel"/> class.
    /// </summary>
    /// <param name="reportService">The report service.</param>
    /// <param name="reportPrintService">The report print service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="exportService">The export service.</param>
    /// <param name="logger">The logger.</param>
    public InventoryReportsViewModel(
        IReportService reportService,
        IReportPrintService reportPrintService,
        ICategoryService categoryService,
        INavigationService navigationService,
        IExportService exportService,
        ILogger logger) : base(logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _reportPrintService = reportPrintService ?? throw new ArgumentNullException(nameof(reportPrintService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

        Title = "Inventory Reports";
        SelectedReportType = ReportTypes.First();
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadCategoriesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    private async Task LoadCategoriesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            Categories = new ObservableCollection<Category>(allCategories.OrderBy(c => c.Name));
        }, "Loading categories...");
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

        if (SelectedReportType.RequiresDateRange && FromDate > ToDate)
        {
            ErrorMessage = "From date cannot be after To date.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var parameters = new InventoryReportParameters
            {
                StartDate = FromDate.Date,
                EndDate = ToDate.Date.AddDays(1).AddTicks(-1), // End of day
                GeneratedByUserId = SessionService.CurrentUserId,
                CategoryId = SelectedCategory?.Id,
                DeadStockDaysThreshold = DeadStockDays,
                IncludeOutOfStock = IncludeOutOfStock
            };

            ClearAllReports();

            switch (SelectedReportType.ReportType)
            {
                case "CurrentStock":
                    CurrentStockReport = await _reportService.GenerateCurrentStockReportAsync(parameters);
                    CurrentStockItems = new ObservableCollection<CurrentStockItem>(CurrentStockReport.Items);
                    ShowCurrentStockReport = true;
                    _logger.Information("Generated current stock report: {SkuCount} SKUs, Value: {Value:C}",
                        CurrentStockReport.TotalSkuCount, CurrentStockReport.TotalStockValue);
                    break;

                case "LowStock":
                    LowStockReport = await _reportService.GenerateLowStockReportAsync(parameters);
                    LowStockItems = new ObservableCollection<LowStockItem>(LowStockReport.Items);
                    ShowLowStockReport = true;
                    _logger.Information("Generated low stock report: {CriticalCount} critical, {LowCount} low",
                        LowStockReport.CriticalCount, LowStockReport.LowStockCount);
                    break;

                case "StockMovement":
                    StockMovementReport = await _reportService.GenerateStockMovementReportAsync(parameters);
                    StockMovementItems = new ObservableCollection<StockMovementItem>(StockMovementReport.Items);
                    ShowStockMovementReport = true;
                    _logger.Information("Generated stock movement report: {MovementCount} movements",
                        StockMovementReport.Items.Count);
                    break;

                case "StockValuation":
                    StockValuationReport = await _reportService.GenerateStockValuationReportAsync(parameters);
                    CategoryValuations = new ObservableCollection<CategoryValuation>(StockValuationReport.Categories);
                    ShowStockValuationReport = true;
                    _logger.Information("Generated stock valuation report: Total Cost: {Cost:C}, Retail: {Retail:C}",
                        StockValuationReport.TotalCostValue, StockValuationReport.TotalRetailValue);
                    break;

                case "DeadStock":
                    DeadStockReport = await _reportService.GenerateDeadStockReportAsync(parameters);
                    DeadStockItems = new ObservableCollection<HospitalityPOS.Core.Models.Reports.DeadStockItem>(DeadStockReport.Items);
                    ShowDeadStockReport = true;
                    _logger.Information("Generated dead stock report: {Count} items, Value: {Value:C}",
                        DeadStockReport.TotalCount, DeadStockReport.TotalValue);
                    break;
            }

            HasReport = true;

        }, "Generating report...");
    }

    private void ClearAllReports()
    {
        CurrentStockReport = null;
        LowStockReport = null;
        StockMovementReport = null;
        StockValuationReport = null;
        DeadStockReport = null;

        CurrentStockItems.Clear();
        LowStockItems.Clear();
        StockMovementItems.Clear();
        CategoryValuations.Clear();
        DeadStockItems.Clear();

        ShowCurrentStockReport = false;
        ShowLowStockReport = false;
        ShowStockMovementReport = false;
        ShowStockValuationReport = false;
        ShowDeadStockReport = false;
    }

    /// <summary>
    /// Prints the current report.
    /// </summary>
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (!HasReport || SelectedReportType == null)
        {
            ErrorMessage = "No report to print. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            switch (SelectedReportType.ReportType)
            {
                case "CurrentStock":
                    if (CurrentStockReport != null)
                        await _reportPrintService.PrintCurrentStockReportAsync(CurrentStockReport);
                    break;

                case "LowStock":
                    if (LowStockReport != null)
                        await _reportPrintService.PrintLowStockReportAsync(LowStockReport);
                    break;

                case "StockMovement":
                    if (StockMovementReport != null)
                        await _reportPrintService.PrintStockMovementReportAsync(StockMovementReport);
                    break;

                case "StockValuation":
                    if (StockValuationReport != null)
                        await _reportPrintService.PrintStockValuationReportAsync(StockValuationReport);
                    break;

                case "DeadStock":
                    if (DeadStockReport != null)
                        await _reportPrintService.PrintDeadStockReportAsync(DeadStockReport);
                    break;
            }

            _logger.Information("Printed {ReportType} report", SelectedReportType.Name);

        }, "Preparing print...");
    }

    /// <summary>
    /// Exports the current report to CSV.
    /// </summary>
    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (!HasReport || SelectedReportType == null)
        {
            ErrorMessage = "No report to export. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var dialogViewModel = App.Services.GetRequiredService<ExportDialogViewModel>();
            dialogViewModel.Initialize(SelectedReportType.Name, FromDate, ToDate);
            dialogViewModel.ExportAction = ExportCurrentInventoryReportAsync;

            var dialog = new ExportDialog { DataContext = dialogViewModel };
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await DialogService.ShowMessageAsync(
                    "Export Complete",
                    $"Report exported successfully to:\n{dialogViewModel.FilePath}");

                _logger.Information("Inventory report exported to {FilePath}", dialogViewModel.FilePath);
            }
        }, "Preparing export...");
    }

    private async Task ExportCurrentInventoryReportAsync(string filePath, CancellationToken ct)
    {
        switch (SelectedReportType?.ReportType)
        {
            case "CurrentStock":
                await _exportService.ExportToCsvAsync(CurrentStockItems.ToList(), filePath, ct);
                break;
            case "LowStock":
                await _exportService.ExportToCsvAsync(LowStockItems.ToList(), filePath, ct);
                break;
            case "StockMovement":
                await _exportService.ExportToCsvAsync(StockMovementItems.ToList(), filePath, ct);
                break;
            case "StockValuation":
                await _exportService.ExportToCsvAsync(CategoryValuations.ToList(), filePath, ct);
                break;
            case "DeadStock":
                await _exportService.ExportToCsvAsync(DeadStockItems.ToList(), filePath, ct);
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
    /// Sets the date range to last 30 days.
    /// </summary>
    [RelayCommand]
    private void SetLast30Days()
    {
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Clears the category filter.
    /// </summary>
    [RelayCommand]
    private void ClearCategoryFilter()
    {
        SelectedCategory = null;
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedReportTypeChanged(InventoryReportTypeItem? value)
    {
        // Clear current report when type changes
        HasReport = false;
        ClearAllReports();

        // Update filter visibility based on report type
        ShowDateRange = value?.RequiresDateRange ?? false;
        ShowDeadStockFilter = value?.ReportType == "DeadStock";
    }
}
