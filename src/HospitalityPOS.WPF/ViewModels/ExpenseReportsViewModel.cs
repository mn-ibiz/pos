using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Expenses;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for expense reports and analytics.
/// </summary>
public partial class ExpenseReportsViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    // Report selection
    [ObservableProperty]
    private ObservableCollection<string> _reportTypes = new()
    {
        "Expense Trends",
        "Category Breakdown",
        "Supplier Breakdown",
        "Prime Cost Analysis"
    };

    [ObservableProperty]
    private string _selectedReportType = "Expense Trends";

    // Date range
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    // Grouping interval
    [ObservableProperty]
    private ObservableCollection<string> _groupingIntervals = new()
    {
        "Daily",
        "Weekly",
        "Monthly"
    };

    [ObservableProperty]
    private string _selectedInterval = "Daily";

    // Summary statistics
    [ObservableProperty]
    private decimal _totalExpenses;

    [ObservableProperty]
    private decimal _previousPeriodExpenses;

    [ObservableProperty]
    private decimal _changePercent;

    [ObservableProperty]
    private bool _isChangePositive;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private decimal _averageExpense;

    // Trend data
    [ObservableProperty]
    private ObservableCollection<ExpenseTrendItemViewModel> _trendData = new();

    // Category breakdown
    [ObservableProperty]
    private ObservableCollection<CategoryBreakdownItemViewModel> _categoryBreakdown = new();

    // Supplier breakdown
    [ObservableProperty]
    private ObservableCollection<SupplierBreakdownItemViewModel> _supplierBreakdown = new();

    // Prime cost data
    [ObservableProperty]
    private decimal _cogsTotal;

    [ObservableProperty]
    private decimal _laborCostTotal;

    [ObservableProperty]
    private decimal _primeCostTotal;

    [ObservableProperty]
    private decimal _revenue;

    [ObservableProperty]
    private decimal _primeCostPercent;

    // Visibility flags
    [ObservableProperty]
    private bool _showTrendsReport;

    [ObservableProperty]
    private bool _showCategoryReport;

    [ObservableProperty]
    private bool _showSupplierReport;

    [ObservableProperty]
    private bool _showPrimeCostReport;

    #endregion

    public ExpenseReportsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        IExportService exportService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _exportService = exportService;
        _logger = logger;

        ShowTrendsReport = true; // Default to trends
    }

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = GenerateReportAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedReportTypeChanged(string value)
    {
        UpdateReportVisibility();
        _ = GenerateReportAsync();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        _ = GenerateReportAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        _ = GenerateReportAsync();
    }

    partial void OnSelectedIntervalChanged(string value)
    {
        if (SelectedReportType == "Expense Trends")
        {
            _ = GenerateReportAsync();
        }
    }

    private void UpdateReportVisibility()
    {
        ShowTrendsReport = SelectedReportType == "Expense Trends";
        ShowCategoryReport = SelectedReportType == "Category Breakdown";
        ShowSupplierReport = SelectedReportType == "Supplier Breakdown";
        ShowPrimeCostReport = SelectedReportType == "Prime Cost Analysis";
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Generating report...";
            ErrorMessage = null;

            switch (SelectedReportType)
            {
                case "Expense Trends":
                    await LoadTrendsAsync();
                    break;
                case "Category Breakdown":
                    await LoadCategoryBreakdownAsync();
                    break;
                case "Supplier Breakdown":
                    await LoadSupplierBreakdownAsync();
                    break;
                case "Prime Cost Analysis":
                    await LoadPrimeCostAsync();
                    break;
            }

            _logger.Information("Generated {ReportType} report for {StartDate:d} to {EndDate:d}",
                SelectedReportType, StartDate, EndDate);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate {ReportType} report", SelectedReportType);
            ErrorMessage = $"Failed to generate report: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private async Task LoadTrendsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

        var interval = SelectedInterval switch
        {
            "Daily" => TrendInterval.Daily,
            "Weekly" => TrendInterval.Weekly,
            "Monthly" => TrendInterval.Monthly,
            _ => TrendInterval.Daily
        };

        var trends = await expenseService.GetExpenseTrendsAsync(StartDate, EndDate, interval);
        TrendData = new ObservableCollection<ExpenseTrendItemViewModel>(
            trends.Select(t => new ExpenseTrendItemViewModel
            {
                PeriodLabel = t.PeriodLabel,
                Total = t.Total,
                Count = t.Count
            }));

        // Calculate summary
        TotalExpenses = TrendData.Sum(t => t.Total);
        TotalTransactions = TrendData.Sum(t => t.Count);
        AverageExpense = TotalTransactions > 0 ? TotalExpenses / TotalTransactions : 0;

        // Get comparison
        var comparison = await expenseService.GetExpenseComparisonAsync(StartDate, EndDate);
        PreviousPeriodExpenses = comparison.PreviousPeriodTotal;
        ChangePercent = comparison.ChangePercent;
        IsChangePositive = ChangePercent > 0;
    }

    private async Task LoadCategoryBreakdownAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

        var breakdown = await expenseService.GetExpensesByCategoryAsync(StartDate, EndDate);
        var total = breakdown.Sum(b => b.Total);

        CategoryBreakdown = new ObservableCollection<CategoryBreakdownItemViewModel>(
            breakdown.OrderByDescending(b => b.Total).Select(b => new CategoryBreakdownItemViewModel
            {
                CategoryName = b.CategoryName,
                Total = b.Total,
                Count = b.Count,
                Percentage = total > 0 ? Math.Round(b.Total / total * 100, 1) : 0
            }));

        TotalExpenses = total;
        TotalTransactions = CategoryBreakdown.Sum(c => c.Count);
        AverageExpense = TotalTransactions > 0 ? TotalExpenses / TotalTransactions : 0;
    }

    private async Task LoadSupplierBreakdownAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

        var breakdown = await expenseService.GetExpensesBySupplierAsync(StartDate, EndDate);
        var total = breakdown.Sum(b => b.Total);

        SupplierBreakdown = new ObservableCollection<SupplierBreakdownItemViewModel>(
            breakdown.OrderByDescending(b => b.Total).Select(b => new SupplierBreakdownItemViewModel
            {
                SupplierName = b.SupplierName ?? "No Supplier",
                Total = b.Total,
                Count = b.Count,
                Percentage = total > 0 ? Math.Round(b.Total / total * 100, 1) : 0
            }));

        TotalExpenses = total;
        TotalTransactions = SupplierBreakdown.Sum(s => s.Count);
        AverageExpense = TotalTransactions > 0 ? TotalExpenses / TotalTransactions : 0;
    }

    private async Task LoadPrimeCostAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
        var receiptService = scope.ServiceProvider.GetRequiredService<IReceiptService>();

        var primeCost = await expenseService.CalculatePrimeCostAsync(StartDate, EndDate);

        CogsTotal = primeCost.CogsCost;
        LaborCostTotal = primeCost.LaborCost;
        PrimeCostTotal = primeCost.TotalPrimeCost;

        // Get revenue for the period
        Revenue = await receiptService.GetSalesTotalAsync(StartDate, EndDate);
        PrimeCostPercent = Revenue > 0 ? Math.Round(PrimeCostTotal / Revenue * 100, 1) : 0;

        TotalExpenses = PrimeCostTotal;
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Preparing export...";

            object exportData;
            string fileName;
            string sheetName;

            switch (SelectedReportType)
            {
                case "Expense Trends":
                    exportData = TrendData.Select(t => new
                    {
                        Period = t.PeriodLabel,
                        t.Total,
                        TransactionCount = t.Count
                    }).ToList();
                    fileName = $"ExpenseTrends_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
                    sheetName = "Expense Trends";
                    break;

                case "Category Breakdown":
                    exportData = CategoryBreakdown.Select(c => new
                    {
                        Category = c.CategoryName,
                        Amount = c.Total,
                        TransactionCount = c.Count,
                        Percentage = $"{c.Percentage}%"
                    }).ToList();
                    fileName = $"ExpensesByCategory_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
                    sheetName = "Category Breakdown";
                    break;

                case "Supplier Breakdown":
                    exportData = SupplierBreakdown.Select(s => new
                    {
                        Supplier = s.SupplierName,
                        Amount = s.Total,
                        TransactionCount = s.Count,
                        Percentage = $"{s.Percentage}%"
                    }).ToList();
                    fileName = $"ExpensesBySupplier_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
                    sheetName = "Supplier Breakdown";
                    break;

                case "Prime Cost Analysis":
                    exportData = new[]
                    {
                        new { Item = "Cost of Goods Sold (COGS)", Amount = CogsTotal },
                        new { Item = "Labor Costs", Amount = LaborCostTotal },
                        new { Item = "Total Prime Cost", Amount = PrimeCostTotal },
                        new { Item = "Revenue", Amount = Revenue },
                        new { Item = "Prime Cost %", Amount = PrimeCostPercent }
                    }.ToList();
                    fileName = $"PrimeCostAnalysis_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
                    sheetName = "Prime Cost";
                    break;

                default:
                    return;
            }

            var result = await _exportService.ExportToExcelAsync(exportData, fileName, sheetName);
            if (result)
            {
                _logger.Information("Exported {ReportType} report to Excel", SelectedReportType);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export report to Excel");
            ErrorMessage = "Failed to export report. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _ = GenerateReportAsync();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}

/// <summary>
/// View model item for expense trend data.
/// </summary>
public class ExpenseTrendItemViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// View model item for category breakdown.
/// </summary>
public class CategoryBreakdownItemViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// View model item for supplier breakdown.
/// </summary>
public class SupplierBreakdownItemViewModel
{
    public string SupplierName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
