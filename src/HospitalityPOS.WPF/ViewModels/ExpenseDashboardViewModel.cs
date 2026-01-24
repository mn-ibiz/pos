using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Category breakdown display model.
/// </summary>
public class CategoryBreakdownItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "\uE7BF";
    public Brush Color { get; set; } = Brushes.Gray;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public double PercentageWidth => (double)Percentage * 2; // Scale for display
}

/// <summary>
/// Recent expense display model.
/// </summary>
public class RecentExpenseItem
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public ExpenseStatus Status { get; set; }
}

/// <summary>
/// ViewModel for the expense management dashboard.
/// </summary>
public partial class ExpenseDashboardViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    // Period selection
    [ObservableProperty]
    private bool _isTodaySelected;

    [ObservableProperty]
    private bool _isWeekSelected;

    [ObservableProperty]
    private bool _isMonthSelected = true;

    [ObservableProperty]
    private bool _isCustomSelected;

    private DateTime _periodStart;
    private DateTime _periodEnd;

    // Summary statistics
    [ObservableProperty]
    private decimal _totalExpenses;

    [ObservableProperty]
    private decimal _expenseTrendPercent;

    [ObservableProperty]
    private bool _isExpenseTrendPositive;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private decimal _pendingAmount;

    [ObservableProperty]
    private decimal _budgetUtilization;

    [ObservableProperty]
    private decimal _budgetRemaining;

    [ObservableProperty]
    private bool _isBudgetOverThreshold;

    [ObservableProperty]
    private bool _isBudgetExceeded;

    [ObservableProperty]
    private int _upcomingRecurringCount;

    [ObservableProperty]
    private decimal _recurringAmount;

    // Prime Cost metrics
    [ObservableProperty]
    private decimal _primeCostPercent;

    [ObservableProperty]
    private decimal _foodCostPercent;

    [ObservableProperty]
    private decimal _laborCostPercent;

    [ObservableProperty]
    private bool _isPrimeCostHealthy = true;

    // Collections
    [ObservableProperty]
    private ObservableCollection<CategoryBreakdownItem> _topCategories = new();

    [ObservableProperty]
    private ObservableCollection<RecentExpenseItem> _recentExpenses = new();

    [ObservableProperty]
    private bool _hasNoRecentExpenses;

    #endregion

    public ExpenseDashboardViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISessionService sessionService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        _logger = logger;

        SetMonthPeriod();
    }

    private int CurrentUserId => _sessionService.CurrentUser?.Id ?? 1;

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Period Selection

    partial void OnIsTodaySelectedChanged(bool value)
    {
        if (value)
        {
            SetTodayPeriod();
            _ = LoadDataAsync();
        }
    }

    partial void OnIsWeekSelectedChanged(bool value)
    {
        if (value)
        {
            SetWeekPeriod();
            _ = LoadDataAsync();
        }
    }

    partial void OnIsMonthSelectedChanged(bool value)
    {
        if (value)
        {
            SetMonthPeriod();
            _ = LoadDataAsync();
        }
    }

    private void SetTodayPeriod()
    {
        _periodStart = DateTime.Today;
        _periodEnd = DateTime.Today.AddDays(1).AddTicks(-1);
    }

    private void SetWeekPeriod()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        _periodStart = today.AddDays(-dayOfWeek);
        _periodEnd = _periodStart.AddDays(7).AddTicks(-1);
    }

    private void SetMonthPeriod()
    {
        var today = DateTime.Today;
        _periodStart = new DateTime(today.Year, today.Month, 1);
        _periodEnd = _periodStart.AddMonths(1).AddTicks(-1);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading expense data...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Load expense summary
            var summary = await expenseService.GetExpenseSummaryAsync(_periodStart, _periodEnd);
            TotalExpenses = summary.TotalAmount;
            PendingCount = summary.PendingCount;
            PendingAmount = summary.PendingAmount;

            // Calculate trend (compare with previous period)
            var periodLength = _periodEnd - _periodStart;
            var previousStart = _periodStart.Subtract(periodLength);
            var previousEnd = _periodStart.AddTicks(-1);
            var previousSummary = await expenseService.GetExpenseSummaryAsync(previousStart, previousEnd);

            if (previousSummary.TotalAmount > 0)
            {
                ExpenseTrendPercent = ((summary.TotalAmount - previousSummary.TotalAmount) / previousSummary.TotalAmount) * 100;
                IsExpenseTrendPositive = ExpenseTrendPercent <= 0; // Lower expenses is positive
            }
            else
            {
                ExpenseTrendPercent = 0;
                IsExpenseTrendPositive = true;
            }

            // Load budget info
            var budgets = await expenseService.GetCurrentBudgetsAsync();
            if (budgets.Any())
            {
                var totalBudget = budgets.Sum(b => b.Amount);
                var totalSpent = budgets.Sum(b => b.SpentAmount);
                BudgetUtilization = totalBudget > 0 ? (totalSpent / totalBudget) * 100 : 0;
                BudgetRemaining = totalBudget - totalSpent;
                IsBudgetOverThreshold = BudgetUtilization >= 80;
                IsBudgetExceeded = BudgetUtilization >= 100;
            }

            // Load recurring expenses due this week
            var upcomingRecurring = await expenseService.GetUpcomingRecurringExpensesAsync(7);
            UpcomingRecurringCount = upcomingRecurring.Count;
            RecurringAmount = upcomingRecurring.Sum(r => r.Amount);

            // Load actual sales data from settled receipts
            var receiptService = scope.ServiceProvider.GetRequiredService<IReceiptService>();
            var totalSales = await receiptService.GetSalesTotalAsync(_periodStart, _periodEnd);

            // Calculate Prime Cost metrics using actual sales data
            var primeCost = await expenseService.CalculatePrimeCostAsync(_periodStart, _periodEnd, totalSales);
            PrimeCostPercent = primeCost.PrimeCostPercentage;
            FoodCostPercent = primeCost.FoodCostPercentage;
            LaborCostPercent = primeCost.LaborCostPercentage;
            IsPrimeCostHealthy = primeCost.PrimeCostPercentage <= 65;

            // Load category breakdown
            var categoryBreakdown = await expenseService.GetExpensesByCategoryAsync(_periodStart, _periodEnd);
            var totalByCategory = categoryBreakdown.Sum(c => c.Value);
            var colors = new[]
            {
                new SolidColorBrush(Color.FromRgb(245, 158, 11)),  // Amber
                new SolidColorBrush(Color.FromRgb(59, 130, 246)),  // Blue
                new SolidColorBrush(Color.FromRgb(16, 185, 129)),  // Green
                new SolidColorBrush(Color.FromRgb(139, 92, 246)),  // Purple
                new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
            };

            TopCategories = new ObservableCollection<CategoryBreakdownItem>(
                categoryBreakdown
                    .OrderByDescending(c => c.Value)
                    .Take(5)
                    .Select((c, i) => new CategoryBreakdownItem
                    {
                        Name = c.Key,
                        Amount = c.Value,
                        Percentage = totalByCategory > 0 ? (c.Value / totalByCategory) * 100 : 0,
                        Color = colors[i % colors.Length],
                        Icon = GetCategoryIcon(c.Key)
                    }));

            // Load recent expenses
            var filter = new ExpenseFilterDto
            {
                StartDate = _periodStart,
                EndDate = _periodEnd
            };
            var expenses = await expenseService.GetExpensesAsync(filter);
            RecentExpenses = new ObservableCollection<RecentExpenseItem>(
                expenses
                    .OrderByDescending(e => e.ExpenseDate)
                    .Take(10)
                    .Select(e => new RecentExpenseItem
                    {
                        Id = e.Id,
                        Description = e.Description,
                        CategoryName = e.ExpenseCategory?.Name ?? "Uncategorized",
                        TotalAmount = e.TotalAmount,
                        ExpenseDate = e.ExpenseDate,
                        Status = e.Status
                    }));

            HasNoRecentExpenses = RecentExpenses.Count == 0;

            _logger.Information("Expense dashboard loaded: {TotalExpenses} in expenses, {PendingCount} pending",
                TotalExpenses, PendingCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load expense dashboard data");
            ErrorMessage = "Failed to load expense data. Please try again.";
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
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        try
        {
            var result = await _dialogService.ShowExpenseEditorDialogAsync(null);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                var dto = new CreateExpenseDto
                {
                    ExpenseCategoryId = result.ExpenseCategoryId,
                    Description = result.Description,
                    Amount = result.Amount,
                    TaxAmount = result.TaxAmount,
                    ExpenseDate = result.ExpenseDate,
                    PaymentMethodId = result.PaymentMethodId,
                    PaymentReference = result.PaymentReference,
                    SupplierId = result.SupplierId,
                    IsTaxDeductible = result.IsTaxDeductible,
                    Notes = result.Notes
                };

                await expenseService.CreateExpenseAsync(dto, CurrentUserId);
                _logger.Information("Created expense: {Description}", result.Description);

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create expense");
            ErrorMessage = "Failed to create expense. Please try again.";
        }
    }

    [RelayCommand]
    private void NavigateToExpenseList()
    {
        _navigationService.NavigateTo<ExpenseListViewModel>();
    }

    [RelayCommand]
    private void NavigateToCategories()
    {
        _navigationService.NavigateTo<ExpenseCategoryManagementViewModel>();
    }

    [RelayCommand]
    private void NavigateToVendors()
    {
        _navigationService.NavigateTo<SuppliersViewModel>();
    }

    [RelayCommand]
    private void NavigateToRecurring()
    {
        _navigationService.NavigateTo<RecurringExpenseListViewModel>();
    }

    [RelayCommand]
    private void NavigateToBudgets()
    {
        _navigationService.NavigateTo<ExpenseBudgetViewModel>();
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        _navigationService.NavigateTo<ExpenseReportsViewModel>();
    }

    #endregion

    #region Helpers

    private static string GetCategoryIcon(string categoryName)
    {
        return categoryName.ToLower() switch
        {
            var n when n.Contains("food") || n.Contains("ingredient") => "\uE7BF",
            var n when n.Contains("labor") || n.Contains("wage") || n.Contains("salary") => "\uE77B",
            var n when n.Contains("rent") || n.Contains("lease") => "\uE80F",
            var n when n.Contains("utility") || n.Contains("electric") || n.Contains("water") => "\uE770",
            var n when n.Contains("marketing") || n.Contains("advertising") => "\uE8BD",
            var n when n.Contains("insurance") => "\uE8E1",
            var n when n.Contains("maintenance") || n.Contains("repair") => "\uE90F",
            var n when n.Contains("equipment") || n.Contains("supply") => "\uE71D",
            var n when n.Contains("tax") => "\uE9D9",
            var n when n.Contains("transport") || n.Contains("delivery") => "\uE806",
            _ => "\uE7BF"
        };
    }

    #endregion
}
