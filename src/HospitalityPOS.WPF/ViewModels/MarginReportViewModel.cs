using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the margin report view.
/// Provides profit margin analysis, low margin alerts, and profitability reports.
/// </summary>
public partial class MarginReportViewModel : ViewModelBase, INavigationAware
{
    private readonly IMarginAnalysisService _marginService;
    private readonly IExportService _exportService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the complete margin analytics report.
    /// </summary>
    [ObservableProperty]
    private MarginAnalyticsReportDto? _analyticsReport;

    /// <summary>
    /// Gets or sets the gross profit summary.
    /// </summary>
    [ObservableProperty]
    private GrossProfitSummaryDto? _grossProfitSummary;

    /// <summary>
    /// Gets or sets the category margins.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryMarginDto> _categoryMargins = [];

    /// <summary>
    /// Gets or sets the product margins.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductMarginDto> _productMargins = [];

    /// <summary>
    /// Gets or sets the low margin alerts.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LowMarginAlertDto> _lowMarginAlerts = [];

    /// <summary>
    /// Gets or sets the margin trend data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MarginTrendPointDto> _marginTrend = [];

    /// <summary>
    /// Gets or sets the declining margin products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductMarginTrendDto> _decliningMarginProducts = [];

    /// <summary>
    /// Gets or sets the report start date.
    /// </summary>
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    /// <summary>
    /// Gets or sets the report end date.
    /// </summary>
    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the minimum margin threshold.
    /// </summary>
    [ObservableProperty]
    private decimal _minimumMarginThreshold = 15.0m;

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    [ObservableProperty]
    private int? _selectedCategoryId;

    /// <summary>
    /// Gets or sets the selected product for details.
    /// </summary>
    [ObservableProperty]
    private ProductMarginDto? _selectedProduct;

    /// <summary>
    /// Gets or sets the sort column for products.
    /// </summary>
    [ObservableProperty]
    private string _productSortColumn = "TotalProfit";

    /// <summary>
    /// Gets or sets whether sorting is descending.
    /// </summary>
    [ObservableProperty]
    private bool _isSortDescending = true;

    /// <summary>
    /// Gets or sets the last refresh time display.
    /// </summary>
    [ObservableProperty]
    private string _lastRefreshDisplay = "Never";

    /// <summary>
    /// Gets or sets the number of low margin products.
    /// </summary>
    [ObservableProperty]
    private int _lowMarginCount;

    /// <summary>
    /// Gets or sets the potential profit loss from low margins.
    /// </summary>
    [ObservableProperty]
    private decimal _potentialProfitLoss;

    /// <summary>
    /// Gets or sets the cost price coverage percentage.
    /// </summary>
    [ObservableProperty]
    private decimal _costPriceCoverage;

    /// <summary>
    /// Gets the available quick date ranges.
    /// </summary>
    public ObservableCollection<QuickDateRangeOption> QuickDateRanges { get; } =
    [
        new("Today", DateRangeType.Today),
        new("Last 7 Days", DateRangeType.Last7Days),
        new("Last 30 Days", DateRangeType.Last30Days),
        new("This Month", DateRangeType.ThisMonth),
        new("Last Month", DateRangeType.LastMonth),
        new("This Quarter", DateRangeType.ThisQuarter),
        new("This Year", DateRangeType.ThisYear)
    ];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MarginReportViewModel"/> class.
    /// </summary>
    public MarginReportViewModel(
        IMarginAnalysisService marginService,
        IExportService exportService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _marginService = marginService ?? throw new ArgumentNullException(nameof(marginService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    #region Navigation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadReportAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh the report.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadReportAsync();
    }

    /// <summary>
    /// Command to apply a quick date range.
    /// </summary>
    [RelayCommand]
    private async Task ApplyQuickDateRangeAsync(string rangeType)
    {
        if (Enum.TryParse<DateRangeType>(rangeType, out var type))
        {
            var (start, end) = GetDateRangeFromType(type);
            StartDate = start;
            EndDate = end;
            await LoadReportAsync();
        }
    }

    /// <summary>
    /// Command to apply custom date range.
    /// </summary>
    [RelayCommand]
    private async Task ApplyCustomDateRangeAsync()
    {
        if (StartDate > EndDate)
        {
            await _dialogService.ShowWarningAsync("Invalid Dates", "Start date must be before end date.");
            return;
        }

        await LoadReportAsync();
    }

    /// <summary>
    /// Command to update margin threshold.
    /// </summary>
    [RelayCommand]
    private async Task UpdateThresholdAsync()
    {
        if (MinimumMarginThreshold < 0 || MinimumMarginThreshold > 100)
        {
            await _dialogService.ShowWarningAsync("Invalid Threshold", "Threshold must be between 0 and 100.");
            return;
        }

        await _marginService.SetMinimumMarginThresholdAsync(MinimumMarginThreshold);
        await LoadReportAsync();
    }

    /// <summary>
    /// Command to export to Excel.
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Preparing export...";

            if (ProductMargins.Any())
            {
                var exportData = ProductMargins.Select(p => new
                {
                    Code = p.ProductCode,
                    Product = p.ProductName,
                    Category = p.CategoryName,
                    SellingPrice = p.SellingPrice,
                    CostPrice = p.CostPrice,
                    MarginKSh = p.Margin,
                    MarginPercent = p.MarginPercent,
                    UnitsSold = p.UnitsSold,
                    TotalRevenue = p.TotalRevenue,
                    TotalProfit = p.TotalProfit,
                    Health = p.Health.ToString()
                }).ToList();

                var defaultFileName = $"MarginReport_{DateTime.Now:yyyyMMdd_HHmmss}";
                var result = await _exportService.ExportToExcelAsync(
                    exportData,
                    defaultFileName,
                    "Product Margins");

                if (result)
                {
                    await _dialogService.ShowInfoAsync(
                        "Export Complete",
                        "Margin report exported successfully.");
                }
            }
            else
            {
                await _dialogService.ShowWarningAsync(
                    "No Data",
                    "No data available to export.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export margin report");
            await _dialogService.ShowErrorAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    /// <summary>
    /// Command to view product details.
    /// </summary>
    [RelayCommand]
    private async Task ViewProductDetailsAsync(ProductMarginDto? product)
    {
        if (product == null) return;

        var healthText = product.Health switch
        {
            MarginHealth.Low => "Low (Below 15%)",
            MarginHealth.Medium => "Medium (15-30%)",
            _ => "Good (Above 30%)"
        };

        await _dialogService.ShowInfoAsync(
            $"Product: {product.ProductName}",
            $"Code: {product.ProductCode}\n" +
            $"Category: {product.CategoryName}\n\n" +
            $"Selling Price: KSh {product.SellingPrice:N0}\n" +
            $"Cost Price: KSh {product.CostPrice:N0}\n" +
            $"Margin: KSh {product.Margin:N0} ({product.MarginPercent:N1}%)\n" +
            $"Margin Health: {healthText}\n\n" +
            $"Units Sold: {product.UnitsSold:N0}\n" +
            $"Total Revenue: KSh {product.TotalRevenue:N0}\n" +
            $"Total Profit: KSh {product.TotalProfit:N0}\n" +
            $"Profit Contribution: {product.ProfitContributionPercent:N1}%");
    }

    /// <summary>
    /// Command to view low margin alert details.
    /// </summary>
    [RelayCommand]
    private async Task ViewAlertDetailsAsync(LowMarginAlertDto? alert)
    {
        if (alert == null) return;

        var severityText = alert.Severity switch
        {
            AlertSeverity.Critical => "Critical - Urgent attention needed",
            AlertSeverity.High => "High - Significant margin issue",
            _ => "Medium - Below target threshold"
        };

        await _dialogService.ShowInfoAsync(
            $"Low Margin Alert: {alert.ProductName}",
            $"Code: {alert.ProductCode}\n" +
            $"Category: {alert.CategoryName}\n\n" +
            $"Current Margin: {alert.CurrentMarginPercent:N1}%\n" +
            $"Threshold: {alert.ThresholdPercent:N1}%\n" +
            $"Gap: {alert.GapPercent:N1}%\n\n" +
            $"Selling Price: KSh {alert.SellingPrice:N0}\n" +
            $"Cost Price: KSh {alert.CostPrice:N0}\n" +
            $"Suggested Price: KSh {alert.SuggestedPrice:N0}\n" +
            $"Price Increase Needed: KSh {alert.PriceIncreaseNeeded:N0}\n\n" +
            $"Recent Sales (30 days): {alert.RecentUnitsSold:N0} units\n" +
            $"Potential Profit Loss: KSh {alert.PotentialProfitLoss:N0}\n\n" +
            $"Severity: {severityText}");
    }

    /// <summary>
    /// Command to view category details.
    /// </summary>
    [RelayCommand]
    private async Task ViewCategoryDetailsAsync(CategoryMarginDto? category)
    {
        if (category == null) return;

        await _dialogService.ShowInfoAsync(
            $"Category: {category.CategoryName}",
            $"Products: {category.ProductCount}\n" +
            $"Profitability Rank: #{category.ProfitabilityRank}\n\n" +
            $"Total Revenue: KSh {category.TotalRevenue:N0}\n" +
            $"Total Cost: KSh {category.TotalCost:N0}\n" +
            $"Total Profit: KSh {category.TotalProfit:N0}\n\n" +
            $"Average Margin: {category.AverageMarginPercent:N1}%\n" +
            $"Weighted Margin: {category.WeightedMarginPercent:N1}%\n" +
            $"Profit Contribution: {category.ProfitContributionPercent:N1}%\n\n" +
            $"Low Margin Products: {category.LowMarginProductCount}");
    }

    /// <summary>
    /// Command to sort products.
    /// </summary>
    [RelayCommand]
    private void SortProducts(string column)
    {
        if (ProductSortColumn == column)
        {
            IsSortDescending = !IsSortDescending;
        }
        else
        {
            ProductSortColumn = column;
            IsSortDescending = true;
        }

        ApplyProductSort();
    }

    /// <summary>
    /// Command to navigate back.
    /// </summary>
    [RelayCommand]
    private void NavigateBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Private Methods

    private async Task LoadReportAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Loading margin report...";

            var request = new MarginReportRequest
            {
                StartDate = StartDate,
                EndDate = EndDate,
                CategoryId = SelectedCategoryId,
                MinimumMarginThreshold = MinimumMarginThreshold,
                OnlyWithSales = true
            };

            AnalyticsReport = await _marginService.GetMarginAnalyticsReportAsync(request);

            if (AnalyticsReport != null)
            {
                GrossProfitSummary = AnalyticsReport.GrossProfitSummary;

                CategoryMargins.Clear();
                foreach (var category in AnalyticsReport.CategoryMargins)
                {
                    CategoryMargins.Add(category);
                }

                ProductMargins.Clear();
                foreach (var product in AnalyticsReport.ProductMargins)
                {
                    ProductMargins.Add(product);
                }

                LowMarginAlerts.Clear();
                foreach (var alert in AnalyticsReport.LowMarginAlerts)
                {
                    LowMarginAlerts.Add(alert);
                }

                MarginTrend.Clear();
                foreach (var point in AnalyticsReport.MarginTrend)
                {
                    MarginTrend.Add(point);
                }

                DecliningMarginProducts.Clear();
                foreach (var product in AnalyticsReport.DecliningMarginProducts)
                {
                    DecliningMarginProducts.Add(product);
                }

                LowMarginCount = AnalyticsReport.LowMarginProductCount;
                CostPriceCoverage = AnalyticsReport.CostPriceCoverage;
                PotentialProfitLoss = LowMarginAlerts.Sum(a => a.PotentialProfitLoss);
            }

            LastRefreshDisplay = $"Updated {DateTime.Now:HH:mm:ss}";
            _logger.Information("Margin report loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load margin report");
            await _dialogService.ShowErrorAsync("Error", $"Failed to load report: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private void ApplyProductSort()
    {
        var sorted = ProductSortColumn switch
        {
            "ProductName" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.ProductName)
                : ProductMargins.OrderBy(p => p.ProductName),
            "CategoryName" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.CategoryName)
                : ProductMargins.OrderBy(p => p.CategoryName),
            "MarginPercent" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.MarginPercent)
                : ProductMargins.OrderBy(p => p.MarginPercent),
            "Margin" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.Margin)
                : ProductMargins.OrderBy(p => p.Margin),
            "TotalRevenue" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.TotalRevenue)
                : ProductMargins.OrderBy(p => p.TotalRevenue),
            "TotalProfit" => IsSortDescending
                ? ProductMargins.OrderByDescending(p => p.TotalProfit)
                : ProductMargins.OrderBy(p => p.TotalProfit),
            _ => ProductMargins.OrderByDescending(p => p.TotalProfit)
        };

        var sortedList = sorted.ToList();
        ProductMargins.Clear();
        foreach (var product in sortedList)
        {
            ProductMargins.Add(product);
        }
    }

    private static (DateTime start, DateTime end) GetDateRangeFromType(DateRangeType type)
    {
        var today = DateTime.Today;
        return type switch
        {
            DateRangeType.Today => (today, today),
            DateRangeType.Last7Days => (today.AddDays(-6), today),
            DateRangeType.Last30Days => (today.AddDays(-29), today),
            DateRangeType.ThisMonth => (new DateTime(today.Year, today.Month, 1), today),
            DateRangeType.LastMonth => (
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            DateRangeType.ThisQuarter => (
                new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1),
                today),
            DateRangeType.ThisYear => (new DateTime(today.Year, 1, 1), today),
            _ => (today.AddDays(-29), today)
        };
    }

    #endregion
}

/// <summary>
/// Date range type for quick selection.
/// </summary>
public enum DateRangeType
{
    Today,
    Last7Days,
    Last30Days,
    ThisMonth,
    LastMonth,
    ThisQuarter,
    ThisYear
}

/// <summary>
/// Quick date range option for combo box.
/// </summary>
public class QuickDateRangeOption
{
    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the date range type.
    /// </summary>
    public DateRangeType RangeType { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public QuickDateRangeOption(string displayName, DateRangeType rangeType)
    {
        DisplayName = displayName;
        RangeType = rangeType;
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
