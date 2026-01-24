using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Chain Reporting - handles multi-store analytics, comparisons, and consolidated reports.
/// </summary>
public partial class ChainReportingViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<StorePerformance> _storePerformances = new();

    [ObservableProperty]
    private ObservableCollection<ProductPerformance> _productPerformances = new();

    [ObservableProperty]
    private ObservableCollection<StoreComparison> _storeComparisons = new();

    [ObservableProperty]
    private StorePerformance? _selectedStore;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [ObservableProperty]
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    // Chain-wide summary
    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private decimal _averageTicket;

    [ObservableProperty]
    private int _storeCount;

    [ObservableProperty]
    private string _topPerformingStore = string.Empty;

    [ObservableProperty]
    private string _underPerformingStore = string.Empty;

    // KPIs
    [ObservableProperty]
    private decimal _revenueGrowth;

    [ObservableProperty]
    private decimal _customerGrowth;

    [ObservableProperty]
    private decimal _averageMargin;

    #endregion

    public ChainReportingViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Chain Reporting";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;

            using var scope = _scopeFactory.CreateScope();
            var chainService = scope.ServiceProvider.GetService<IChainReportingService>();

            if (chainService is null)
            {
                ErrorMessage = "Chain Reporting service not available";
                return;
            }

            // Load consolidated metrics
            var metrics = await chainService.GetConsolidatedMetricsAsync(StartDate, EndDate);
            TotalRevenue = metrics.TotalRevenue;
            TotalTransactions = metrics.TotalTransactions;
            AverageTicket = metrics.AverageTicket;
            StoreCount = metrics.StoreCount;
            RevenueGrowth = metrics.RevenueGrowthPercent;
            CustomerGrowth = metrics.CustomerGrowthPercent;
            AverageMargin = metrics.AverageMarginPercent;

            // Load store performances
            var stores = await chainService.GetStorePerformancesAsync(StartDate, EndDate);
            StorePerformances = new ObservableCollection<StorePerformance>(stores);

            if (stores.Count > 0)
            {
                var top = stores.OrderByDescending(s => s.Revenue).First();
                var bottom = stores.OrderBy(s => s.Revenue).First();
                TopPerformingStore = $"{top.StoreName} (KSh {top.Revenue:N0})";
                UnderPerformingStore = $"{bottom.StoreName} (KSh {bottom.Revenue:N0})";
            }

            // Load store comparisons
            var comparisons = await chainService.GetStoreComparisonsAsync(StartDate, EndDate);
            StoreComparisons = new ObservableCollection<StoreComparison>(comparisons);

            // Load product performance across chain
            var products = await chainService.GetProductPerformancesAsync(StartDate, EndDate, 20);
            ProductPerformances = new ObservableCollection<ProductPerformance>(products);

            IsLoading = false;
        }, "Loading chain data...");
    }

    [RelayCommand]
    private async Task ViewStoreDetailsAsync(StorePerformance? store)
    {
        if (store is null) return;

        SelectedStore = store;
        await DialogService.ShowMessageAsync(
            $"Store: {store.StoreName}",
            $"Revenue: KSh {store.Revenue:N0}\n" +
            $"Transactions: {store.TransactionCount}\n" +
            $"Avg Ticket: KSh {store.AverageTicket:N0}\n" +
            $"Growth: {store.GrowthPercent:N1}%\n" +
            $"Rank: #{store.Rank}");
    }

    [RelayCommand]
    private async Task ExportConsolidatedReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Consolidated report export will be available soon.");
    }

    [RelayCommand]
    private async Task ExportComparisonReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Comparison report export will be available soon.");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnStartDateChanged(DateOnly value) => _ = LoadDataAsync();
    partial void OnEndDateChanged(DateOnly value) => _ = LoadDataAsync();
}

// DTOs
public class ConsolidatedMetrics
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTicket { get; set; }
    public int StoreCount { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal CustomerGrowthPercent { get; set; }
    public decimal AverageMarginPercent { get; set; }
}

public class StorePerformance
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTicket => TransactionCount > 0 ? Revenue / TransactionCount : 0;
    public decimal GrowthPercent { get; set; }
    public decimal MarginPercent { get; set; }
    public int CustomerCount { get; set; }
    public int Rank { get; set; }
    public string Performance => GrowthPercent >= 0 ? "Growing" : "Declining";
}

public class StoreComparison
{
    public string Metric { get; set; } = string.Empty;
    public Dictionary<string, decimal> StoreValues { get; set; } = new();
    public decimal ChainAverage { get; set; }
}

public class ProductPerformance
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int StoreCount { get; set; } // Number of stores selling this product
    public decimal AveragePrice { get; set; }
    public string TopSellingStore { get; set; } = string.Empty;
}
