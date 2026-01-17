using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the comparative analytics view.
/// Provides period comparisons, trend analysis, and performance metrics.
/// </summary>
public partial class ComparativeAnalyticsViewModel : ViewModelBase, INavigationAware
{
    private readonly IComparativeAnalyticsService _analyticsService;
    private readonly IExportService _exportService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the complete comparative analytics data.
    /// </summary>
    [ObservableProperty]
    private ComparativeAnalyticsDto? _analyticsData;

    /// <summary>
    /// Gets or sets the period comparison results.
    /// </summary>
    [ObservableProperty]
    private PeriodComparisonDto? _periodComparison;

    /// <summary>
    /// Gets or sets the sales trend comparison data.
    /// </summary>
    [ObservableProperty]
    private SalesTrendComparisonDto? _salesTrend;

    /// <summary>
    /// Gets or sets the top movers summary.
    /// </summary>
    [ObservableProperty]
    private TopMoversDto? _topMovers;

    /// <summary>
    /// Gets or sets the category comparisons.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryComparisonDto> _categoryComparisons = [];

    /// <summary>
    /// Gets or sets the product comparisons.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProductComparisonDto> _productComparisons = [];

    /// <summary>
    /// Gets or sets the day of week patterns.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DayOfWeekPatternDto> _dayOfWeekPatterns = [];

    /// <summary>
    /// Gets or sets the sparkline data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SparklineDataDto> _sparklines = [];

    /// <summary>
    /// Gets or sets the selected comparison period type.
    /// </summary>
    [ObservableProperty]
    private ComparisonPeriodType _selectedPeriodType = ComparisonPeriodType.WeekOverWeek;

    /// <summary>
    /// Gets or sets the current period start date (for custom periods).
    /// </summary>
    [ObservableProperty]
    private DateTime _currentPeriodStart = DateTime.Today.AddDays(-7);

    /// <summary>
    /// Gets or sets the current period end date (for custom periods).
    /// </summary>
    [ObservableProperty]
    private DateTime _currentPeriodEnd = DateTime.Today;

    /// <summary>
    /// Gets or sets the previous period start date (for custom periods).
    /// </summary>
    [ObservableProperty]
    private DateTime _previousPeriodStart = DateTime.Today.AddDays(-14);

    /// <summary>
    /// Gets or sets the previous period end date (for custom periods).
    /// </summary>
    [ObservableProperty]
    private DateTime _previousPeriodEnd = DateTime.Today.AddDays(-7);

    /// <summary>
    /// Gets or sets whether custom dates are enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isCustomPeriod;

    /// <summary>
    /// Gets or sets the selected store ID (null for all stores).
    /// </summary>
    [ObservableProperty]
    private int? _selectedStoreId;

    /// <summary>
    /// Gets or sets the last refresh time display.
    /// </summary>
    [ObservableProperty]
    private string _lastRefreshDisplay = "Never";

    /// <summary>
    /// Gets the available period types for selection.
    /// </summary>
    public ObservableCollection<PeriodTypeOption> PeriodTypes { get; } =
    [
        new("Today vs Yesterday", ComparisonPeriodType.DayOverDay),
        new("This Week vs Last Week", ComparisonPeriodType.WeekOverWeek),
        new("This Month vs Last Month", ComparisonPeriodType.MonthOverMonth),
        new("This Quarter vs Last Quarter", ComparisonPeriodType.QuarterOverQuarter),
        new("This Year vs Last Year", ComparisonPeriodType.YearOverYear),
        new("Custom Dates", ComparisonPeriodType.Custom)
    ];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ComparativeAnalyticsViewModel"/> class.
    /// </summary>
    public ComparativeAnalyticsViewModel(
        IComparativeAnalyticsService analyticsService,
        IExportService exportService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    #region Navigation

    /// <inheritdoc />
    public async Task OnNavigatedToAsync(object? parameter = null)
    {
        await LoadAnalyticsAsync();
    }

    /// <inheritdoc />
    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh the analytics data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAnalyticsAsync();
    }

    /// <summary>
    /// Command to apply a quick comparison type.
    /// </summary>
    [RelayCommand]
    private async Task ApplyQuickComparisonAsync(string periodType)
    {
        if (Enum.TryParse<ComparisonPeriodType>(periodType, out var type))
        {
            SelectedPeriodType = type;
            IsCustomPeriod = type == ComparisonPeriodType.Custom;
            await LoadAnalyticsAsync();
        }
    }

    /// <summary>
    /// Command to apply custom date comparison.
    /// </summary>
    [RelayCommand]
    private async Task ApplyCustomDatesAsync()
    {
        if (CurrentPeriodStart >= CurrentPeriodEnd)
        {
            await _dialogService.ShowWarningAsync("Invalid Dates", "Current period start date must be before end date.");
            return;
        }

        if (PreviousPeriodStart >= PreviousPeriodEnd)
        {
            await _dialogService.ShowWarningAsync("Invalid Dates", "Previous period start date must be before end date.");
            return;
        }

        SelectedPeriodType = ComparisonPeriodType.Custom;
        IsCustomPeriod = true;
        await LoadAnalyticsAsync();
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
            StatusMessage = "Preparing export...";

            // Export category comparisons
            if (CategoryComparisons.Any())
            {
                var categoryExportData = CategoryComparisons.Select(c => new
                {
                    Category = c.CategoryName,
                    CurrentSales = c.CurrentPeriodSales,
                    PreviousSales = c.PreviousPeriodSales,
                    GrowthPercent = c.SalesGrowth.PercentageChange,
                    ContributionPercent = c.CurrentContributionPercent
                }).ToList();

                var defaultFileName = $"ComparativeAnalytics_{DateTime.Now:yyyyMMdd_HHmmss}";
                var result = await _exportService.ExportToExcelAsync(
                    categoryExportData,
                    defaultFileName,
                    "Category Comparison");

                if (result)
                {
                    await _dialogService.ShowInfoAsync(
                        "Export Complete",
                        "Comparative analytics exported successfully.");
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
            Logger.Error(ex, "Failed to export comparative analytics");
            await _dialogService.ShowErrorAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Command to view product details.
    /// </summary>
    [RelayCommand]
    private async Task ViewProductDetailsAsync(ProductComparisonDto? product)
    {
        if (product == null) return;

        // Navigate to product details or show details dialog
        await _dialogService.ShowInfoAsync(
            $"Product: {product.ProductName}",
            $"Code: {product.ProductCode}\n" +
            $"Category: {product.CategoryName}\n\n" +
            $"Current Period Sales: KSh {product.CurrentPeriodSales:N0}\n" +
            $"Previous Period Sales: KSh {product.PreviousPeriodSales:N0}\n" +
            $"Growth: {product.SalesGrowth.FormattedChange}");
    }

    /// <summary>
    /// Command to view category details.
    /// </summary>
    [RelayCommand]
    private async Task ViewCategoryDetailsAsync(CategoryComparisonDto? category)
    {
        if (category == null) return;

        await _dialogService.ShowInfoAsync(
            $"Category: {category.CategoryName}",
            $"Current Period Sales: KSh {category.CurrentPeriodSales:N0}\n" +
            $"Previous Period Sales: KSh {category.PreviousPeriodSales:N0}\n" +
            $"Growth: {category.SalesGrowth.FormattedChange}\n\n" +
            $"Contribution: {category.CurrentContributionPercent:N1}% " +
            $"(Change: {category.ContributionChange:+0.0;-0.0}%)");
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

    private async Task LoadAnalyticsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading analytics...";

            var request = CreateComparisonRequest();

            AnalyticsData = await _analyticsService.GetComparativeAnalyticsAsync(request);

            if (AnalyticsData != null)
            {
                PeriodComparison = AnalyticsData.PeriodComparison;
                SalesTrend = AnalyticsData.SalesTrend;
                TopMovers = AnalyticsData.TopMovers;

                CategoryComparisons.Clear();
                foreach (var category in AnalyticsData.CategoryComparisons)
                {
                    CategoryComparisons.Add(category);
                }

                DayOfWeekPatterns.Clear();
                foreach (var pattern in AnalyticsData.DayOfWeekPatterns)
                {
                    DayOfWeekPatterns.Add(pattern);
                }

                Sparklines.Clear();
                foreach (var sparkline in AnalyticsData.Sparklines)
                {
                    Sparklines.Add(sparkline);
                }

                // Load product comparisons separately for better performance
                var products = await _analyticsService.GetProductComparisonAsync(request, 50);
                ProductComparisons.Clear();
                foreach (var product in products)
                {
                    ProductComparisons.Add(product);
                }
            }

            LastRefreshDisplay = $"Updated {DateTime.Now:HH:mm:ss}";
            Logger.Information("Comparative analytics loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load comparative analytics");
            await _dialogService.ShowErrorAsync("Error", $"Failed to load analytics: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    private PeriodComparisonRequest CreateComparisonRequest()
    {
        var request = new PeriodComparisonRequest
        {
            PeriodType = SelectedPeriodType,
            StoreId = SelectedStoreId
        };

        if (SelectedPeriodType == ComparisonPeriodType.Custom)
        {
            request.CurrentPeriodStart = CurrentPeriodStart;
            request.CurrentPeriodEnd = CurrentPeriodEnd;
            request.PreviousPeriodStart = PreviousPeriodStart;
            request.PreviousPeriodEnd = PreviousPeriodEnd;
        }

        return request;
    }

    partial void OnSelectedPeriodTypeChanged(ComparisonPeriodType value)
    {
        IsCustomPeriod = value == ComparisonPeriodType.Custom;

        if (!IsCustomPeriod)
        {
            // Update date fields based on resolved period
            var (currentStart, currentEnd, previousStart, previousEnd) =
                _analyticsService.ResolvePeriodDates(value);

            CurrentPeriodStart = currentStart;
            CurrentPeriodEnd = currentEnd;
            PreviousPeriodStart = previousStart;
            PreviousPeriodEnd = previousEnd;
        }
    }

    #endregion
}

/// <summary>
/// Period type option for combo box.
/// </summary>
public class PeriodTypeOption
{
    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the period type value.
    /// </summary>
    public ComparisonPeriodType Value { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PeriodTypeOption(string displayName, ComparisonPeriodType value)
    {
        DisplayName = displayName;
        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
