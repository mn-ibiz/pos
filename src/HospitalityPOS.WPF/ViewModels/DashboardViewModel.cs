using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Dashboard;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the real-time sales dashboard.
/// Provides live sales metrics, charts, and alert widgets.
/// </summary>
public partial class DashboardViewModel : ViewModelBase, INavigationAware
{
    private readonly IDashboardService _dashboardService;
    private readonly INavigationService _navigationService;
    private readonly DispatcherTimer _refreshTimer;
    private const int DefaultRefreshIntervalSeconds = 30;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the current dashboard data.
    /// </summary>
    [ObservableProperty]
    private DashboardDataDto? _dashboardData;

    /// <summary>
    /// Gets or sets today's sales summary.
    /// </summary>
    [ObservableProperty]
    private TodaySalesSummaryDto? _salesSummary;

    /// <summary>
    /// Gets or sets the hourly sales data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HourlySalesDto> _hourlySales = [];

    /// <summary>
    /// Gets or sets the top selling products.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TopSellingProductDto> _topProducts = [];

    /// <summary>
    /// Gets or sets the payment method breakdown.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentMethodBreakdownDto> _paymentBreakdown = [];

    /// <summary>
    /// Gets or sets the comparison metrics.
    /// </summary>
    [ObservableProperty]
    private ComparisonMetricsDto? _comparison;

    /// <summary>
    /// Gets or sets the low stock alerts.
    /// </summary>
    [ObservableProperty]
    private LowStockAlertDto? _lowStockAlerts;

    /// <summary>
    /// Gets or sets the expiry alerts.
    /// </summary>
    [ObservableProperty]
    private ExpiryAlertDto? _expiryAlerts;

    /// <summary>
    /// Gets or sets the sync status.
    /// </summary>
    [ObservableProperty]
    private SyncStatusDto? _syncStatus;

    /// <summary>
    /// Gets or sets the last refresh time.
    /// </summary>
    [ObservableProperty]
    private DateTime _lastRefreshTime;

    /// <summary>
    /// Gets or sets the last refresh display text.
    /// </summary>
    [ObservableProperty]
    private string _lastRefreshDisplay = "Never";

    /// <summary>
    /// Gets or sets whether auto-refresh is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isAutoRefreshEnabled = true;

    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds.
    /// </summary>
    [ObservableProperty]
    private int _refreshIntervalSeconds = DefaultRefreshIntervalSeconds;

    /// <summary>
    /// Gets or sets the selected store ID for filtering.
    /// </summary>
    [ObservableProperty]
    private int? _selectedStoreId;

    /// <summary>
    /// Gets or sets the available stores.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BranchSummaryDto> _availableStores = [];

    /// <summary>
    /// Gets or sets whether multi-store view is available.
    /// </summary>
    [ObservableProperty]
    private bool _isMultiStoreEnabled;

    /// <summary>
    /// Gets or sets the countdown to next refresh.
    /// </summary>
    [ObservableProperty]
    private int _refreshCountdown;

    /// <summary>
    /// Gets or sets whether data has been loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasData;

    /// <summary>
    /// Gets or sets the trend indicator for today vs yesterday (positive/negative).
    /// </summary>
    [ObservableProperty]
    private bool _isSalesTrendPositive;

    /// <summary>
    /// Gets or sets the trend display text.
    /// </summary>
    [ObservableProperty]
    private string _salesTrendDisplay = "0%";

    /// <summary>
    /// Gets or sets the sparkline plot for Today's Sales KPI card.
    /// </summary>
    [ObservableProperty]
    private PlotModel? _salesSparkline;

    /// <summary>
    /// Gets or sets the sparkline plot for Orders KPI card.
    /// </summary>
    [ObservableProperty]
    private PlotModel? _ordersSparkline;

    /// <summary>
    /// Gets or sets the sparkline plot for Avg Ticket KPI card.
    /// </summary>
    [ObservableProperty]
    private PlotModel? _avgTicketSparkline;

    #endregion

    /// <summary>
    /// Available refresh intervals for the dropdown.
    /// </summary>
    public List<int> RefreshIntervalOptions { get; } = [15, 30, 60, 120, 300];

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    /// <param name="dashboardService">The dashboard service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="logger">The logger.</param>
    public DashboardViewModel(
        IDashboardService dashboardService,
        INavigationService navigationService,
        ILogger logger) : base(logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Sales Dashboard";

        // Initialize refresh timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;
    }

    #region Navigation

    /// <summary>
    /// Called when navigating to this view.
    /// </summary>
    public async void OnNavigatedTo(object? parameter)
    {
        _logger.Information("Navigated to Dashboard");

        // Start with initial data load
        await LoadDashboardDataAsync();

        // Start auto-refresh if enabled
        if (IsAutoRefreshEnabled)
        {
            StartAutoRefresh();
        }
    }

    /// <summary>
    /// Called when navigating away from this view.
    /// </summary>
    public void OnNavigatedFrom()
    {
        _logger.Debug("Navigating away from Dashboard");
        StopAutoRefresh();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Refreshes the dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Toggles auto-refresh on/off.
    /// </summary>
    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        IsAutoRefreshEnabled = !IsAutoRefreshEnabled;

        if (IsAutoRefreshEnabled)
        {
            StartAutoRefresh();
        }
        else
        {
            StopAutoRefresh();
        }
    }

    /// <summary>
    /// Changes the refresh interval.
    /// </summary>
    /// <param name="intervalSeconds">The new interval in seconds.</param>
    [RelayCommand]
    private void ChangeRefreshInterval(int intervalSeconds)
    {
        RefreshIntervalSeconds = intervalSeconds;
        RefreshCountdown = intervalSeconds;
        _logger.Debug("Refresh interval changed to {Seconds} seconds", intervalSeconds);
    }

    /// <summary>
    /// Filters dashboard by store.
    /// </summary>
    /// <param name="storeId">The store ID to filter by, or null for all stores.</param>
    [RelayCommand]
    private async Task FilterByStoreAsync(int? storeId)
    {
        SelectedStoreId = storeId;
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Navigates to the inventory view.
    /// </summary>
    [RelayCommand]
    private void ViewInventory()
    {
        _navigationService.NavigateTo<Views.Inventory.InventoryView>();
    }

    /// <summary>
    /// Navigates to the sales reports view.
    /// </summary>
    [RelayCommand]
    private void ViewSalesReports()
    {
        _navigationService.NavigateTo<Views.SalesReportsView>();
    }

    /// <summary>
    /// Navigates to the batch/expiry tracking view.
    /// </summary>
    [RelayCommand]
    private void ViewExpiryAlerts()
    {
        // Navigate to inventory view - expiry alerts shown there
        _navigationService.NavigateTo<Views.Inventory.InventoryView>();
    }

    /// <summary>
    /// Navigates to product details for a specific product.
    /// </summary>
    /// <param name="productId">The product ID to view.</param>
    [RelayCommand]
    private void ViewProductDetails(int? productId)
    {
        if (productId.HasValue)
        {
            _navigationService.NavigateTo<Views.Inventory.InventoryView>(productId.Value);
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            _logger.Debug("Loading dashboard data for store {StoreId}", SelectedStoreId);

            var data = await _dashboardService.GetDashboardDataAsync(SelectedStoreId);

            // Update all properties
            DashboardData = data;
            SalesSummary = data.SalesSummary;
            Comparison = data.Comparison;
            LowStockAlerts = data.LowStockAlerts;
            ExpiryAlerts = data.ExpiryAlerts;
            SyncStatus = data.SyncStatus;

            // Update collections
            HourlySales.Clear();
            foreach (var hour in data.HourlySales)
            {
                HourlySales.Add(hour);
            }

            TopProducts.Clear();
            foreach (var product in data.TopProducts)
            {
                TopProducts.Add(product);
            }

            PaymentBreakdown.Clear();
            foreach (var payment in data.PaymentBreakdown)
            {
                PaymentBreakdown.Add(payment);
            }

            // Update trend indicators
            if (Comparison != null)
            {
                IsSalesTrendPositive = Comparison.IsBetterThanYesterday;
                var sign = Comparison.VsYesterdayPercent >= 0 ? "+" : "";
                SalesTrendDisplay = $"{sign}{Comparison.VsYesterdayPercent}%";
            }

            // Update refresh time
            LastRefreshTime = DateTime.Now;
            LastRefreshDisplay = LastRefreshTime.ToString("HH:mm:ss");
            HasData = true;

            // Create sparklines from hourly data
            CreateSparklines();

            // Load branch summaries if needed
            if (!IsMultiStoreEnabled)
            {
                await LoadBranchSummariesAsync();
            }

            _logger.Information("Dashboard data loaded successfully");
        }, "Loading dashboard data...");
    }

    private async Task LoadBranchSummariesAsync()
    {
        try
        {
            var stores = await _dashboardService.GetBranchSummariesAsync();

            AvailableStores.Clear();
            foreach (var store in stores)
            {
                AvailableStores.Add(store);
            }

            IsMultiStoreEnabled = stores.Count > 1;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load branch summaries");
            IsMultiStoreEnabled = false;
        }
    }

    private void StartAutoRefresh()
    {
        RefreshCountdown = RefreshIntervalSeconds;
        _refreshTimer.Start();
        _logger.Debug("Auto-refresh started with {Seconds}s interval", RefreshIntervalSeconds);
    }

    private void StopAutoRefresh()
    {
        _refreshTimer.Stop();
        _logger.Debug("Auto-refresh stopped");
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshCountdown--;

        if (RefreshCountdown <= 0)
        {
            RefreshCountdown = RefreshIntervalSeconds;
            await LoadDashboardDataAsync();
        }
    }

    partial void OnRefreshIntervalSecondsChanged(int value)
    {
        // Reset countdown when interval changes
        if (IsAutoRefreshEnabled)
        {
            RefreshCountdown = value;
        }
    }

    partial void OnSelectedStoreIdChanged(int? value)
    {
        // Reload dashboard data when store selection changes
        if (HasData)
        {
            _ = LoadDashboardDataAsync();
        }
    }

    /// <summary>
    /// Creates sparkline plot models from hourly sales data.
    /// </summary>
    private void CreateSparklines()
    {
        if (HourlySales == null || HourlySales.Count == 0)
        {
            SalesSparkline = CreateEmptySparkline("#22C55E");
            OrdersSparkline = CreateEmptySparkline("#3B82F6");
            AvgTicketSparkline = CreateEmptySparkline("#F59E0B");
            return;
        }

        // Create Sales Sparkline (green)
        SalesSparkline = CreateSparklinePlot(
            HourlySales.Select(h => (double)h.Sales).ToList(),
            OxyColor.Parse("#22C55E"));

        // Create Orders Sparkline (blue) - using transaction count from hourly data
        // Assuming each hourly entry has a count field, we'll estimate from sales pattern
        OrdersSparkline = CreateSparklinePlot(
            HourlySales.Select(h => h.Sales > 0 ? Math.Max(1, (double)(h.Sales / (SalesSummary?.AverageTicket ?? 1))) : 0).ToList(),
            OxyColor.Parse("#3B82F6"));

        // Create Avg Ticket Sparkline (amber)
        AvgTicketSparkline = CreateSparklinePlot(
            HourlySales.Select(h => h.Sales > 0 ? (double)(h.Sales / Math.Max(1, h.Sales / (SalesSummary?.AverageTicket ?? 1))) : 0).ToList(),
            OxyColor.Parse("#F59E0B"));
    }

    /// <summary>
    /// Creates a sparkline PlotModel from data points.
    /// </summary>
    private static PlotModel CreateSparklinePlot(List<double> values, OxyColor color)
    {
        var model = new PlotModel
        {
            PlotMargins = new OxyThickness(0),
            Padding = new OxyThickness(0),
            Background = OxyColors.Transparent,
            PlotAreaBorderThickness = new OxyThickness(0)
        };

        // Hide all axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            IsAxisVisible = false,
            MinimumPadding = 0,
            MaximumPadding = 0
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            IsAxisVisible = false,
            MinimumPadding = 0.1,
            MaximumPadding = 0.1
        });

        // Create area series for sparkline effect
        var areaSeries = new AreaSeries
        {
            Color = color,
            Fill = OxyColor.FromAColor(60, color),
            StrokeThickness = 2,
            InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline
        };

        for (int i = 0; i < values.Count; i++)
        {
            areaSeries.Points.Add(new DataPoint(i, values[i]));
        }

        model.Series.Add(areaSeries);
        return model;
    }

    /// <summary>
    /// Creates an empty sparkline for when no data is available.
    /// </summary>
    private static PlotModel CreateEmptySparkline(string colorHex)
    {
        var color = OxyColor.Parse(colorHex);
        var model = new PlotModel
        {
            PlotMargins = new OxyThickness(0),
            Padding = new OxyThickness(0),
            Background = OxyColors.Transparent,
            PlotAreaBorderThickness = new OxyThickness(0)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            IsAxisVisible = false
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            IsAxisVisible = false
        });

        // Create a flat line
        var areaSeries = new AreaSeries
        {
            Color = color,
            Fill = OxyColor.FromAColor(30, color),
            StrokeThickness = 1
        };

        areaSeries.Points.Add(new DataPoint(0, 0));
        areaSeries.Points.Add(new DataPoint(1, 0));
        model.Series.Add(areaSeries);

        return model;
    }

    #endregion
}
