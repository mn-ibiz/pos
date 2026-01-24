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
/// Display wrapper for ExpenseBudget with computed display properties.
/// </summary>
public class ExpenseBudgetDisplayItem
{
    public ExpenseBudget Budget { get; set; } = null!;
    public decimal SpentAmount => Budget.SpentAmount;
    public decimal RemainingAmount => Budget.RemainingAmount;
    public decimal UtilizationPercent => Budget.UtilizationPercentage;

    public string Status => Budget.IsExceeded ? "EXCEEDED"
        : Budget.IsOverThreshold ? "WARNING"
        : "OK";

    public Brush StatusColor => Status switch
    {
        "EXCEEDED" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
        "WARNING" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Amber
        _ => new SolidColorBrush(Color.FromRgb(16, 185, 129))            // Green
    };

    public Brush ProgressBarColor => Status switch
    {
        "EXCEEDED" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
        "WARNING" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Amber
        _ => new SolidColorBrush(Color.FromRgb(16, 185, 129))            // Green
    };

    public double ProgressWidth => Math.Min(100, (double)UtilizationPercent) * 2; // Scale for display (max 200px)
}

/// <summary>
/// Period filter option for the combobox.
/// </summary>
public class BudgetPeriodFilterOption
{
    public string Name { get; }
    public int? Year { get; }
    public int? Month { get; }
    public int? Quarter { get; }

    public BudgetPeriodFilterOption(string name, int? year = null, int? month = null, int? quarter = null)
    {
        Name = name;
        Year = year;
        Month = month;
        Quarter = quarter;
    }

    public override string ToString() => Name;
}

/// <summary>
/// ViewModel for managing expense budgets.
/// </summary>
public partial class ExpenseBudgetViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<ExpenseBudgetDisplayItem> _budgets = [];

    [ObservableProperty]
    private ExpenseBudgetDisplayItem? _selectedBudget;

    [ObservableProperty]
    private ObservableCollection<ExpenseCategory> _categories = [];

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    [ObservableProperty]
    private BudgetPeriodFilterOption? _selectedPeriod;

    // Statistics
    [ObservableProperty]
    private int _totalBudgetCount;

    [ObservableProperty]
    private decimal _totalBudgeted;

    [ObservableProperty]
    private decimal _totalSpent;

    [ObservableProperty]
    private decimal _totalRemaining;

    [ObservableProperty]
    private int _budgetsOverThreshold;

    [ObservableProperty]
    private int _budgetsExceeded;

    #endregion

    /// <summary>
    /// Gets the available period filter options.
    /// </summary>
    public ObservableCollection<BudgetPeriodFilterOption> PeriodOptions { get; }

    public ExpenseBudgetViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;

        // Initialize period options
        var now = DateTime.Now;
        PeriodOptions =
        [
            new BudgetPeriodFilterOption("Current Month", now.Year, now.Month),
            new BudgetPeriodFilterOption("Current Quarter", now.Year, quarter: (now.Month - 1) / 3 + 1),
            new BudgetPeriodFilterOption("Current Year", now.Year),
            new BudgetPeriodFilterOption("All Budgets")
        ];

        _selectedPeriod = PeriodOptions[0]; // Default to current month
    }

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

    #region Commands

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading budgets...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Load categories
            var categories = await expenseService.GetCategoriesAsync();
            Categories = new ObservableCollection<ExpenseCategory>(categories);

            // Load budgets
            var budgets = await expenseService.GetBudgetsAsync(SelectedPeriod?.Year);

            // Apply filters
            var filtered = budgets.AsEnumerable();

            if (SelectedPeriod != null)
            {
                if (SelectedPeriod.Month.HasValue)
                {
                    filtered = filtered.Where(b => b.Month == SelectedPeriod.Month && b.Year == SelectedPeriod.Year);
                }
                else if (SelectedPeriod.Quarter.HasValue)
                {
                    filtered = filtered.Where(b => b.Quarter == SelectedPeriod.Quarter && b.Year == SelectedPeriod.Year);
                }
                else if (SelectedPeriod.Year.HasValue)
                {
                    filtered = filtered.Where(b => b.Year == SelectedPeriod.Year);
                }
            }

            if (SelectedCategory != null)
            {
                filtered = filtered.Where(b => b.ExpenseCategoryId == SelectedCategory.Id);
            }

            var displayItems = filtered
                .OrderByDescending(b => b.UtilizationPercentage)
                .Select(b => new ExpenseBudgetDisplayItem { Budget = b })
                .ToList();

            Budgets = new ObservableCollection<ExpenseBudgetDisplayItem>(displayItems);

            // Calculate statistics
            CalculateStatistics(displayItems);

            _logger.Information("Loaded {Count} budgets", Budgets.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load budgets");
            ErrorMessage = "Failed to load budgets. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private void CalculateStatistics(List<ExpenseBudgetDisplayItem> budgets)
    {
        TotalBudgetCount = budgets.Count;
        TotalBudgeted = budgets.Sum(b => b.Budget.Amount);
        TotalSpent = budgets.Sum(b => b.SpentAmount);
        TotalRemaining = TotalBudgeted - TotalSpent;
        BudgetsOverThreshold = budgets.Count(b => b.Budget.IsOverThreshold && !b.Budget.IsExceeded);
        BudgetsExceeded = budgets.Count(b => b.Budget.IsExceeded);
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
    private async Task AddBudgetAsync()
    {
        try
        {
            var result = await _dialogService.ShowExpenseBudgetEditorDialogAsync(null);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                await expenseService.CreateBudgetAsync(result);
                _logger.Information("Created budget: {Name}", result.Name);

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create budget");
            await _dialogService.ShowErrorAsync($"Failed to create budget: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditBudgetAsync()
    {
        if (SelectedBudget == null) return;

        try
        {
            var result = await _dialogService.ShowExpenseBudgetEditorDialogAsync(SelectedBudget.Budget);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                result.Id = SelectedBudget.Budget.Id;
                await expenseService.UpdateBudgetAsync(result);
                _logger.Information("Updated budget: {Name}", result.Name);

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update budget");
            await _dialogService.ShowErrorAsync($"Failed to update budget: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteBudgetAsync()
    {
        if (SelectedBudget == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Budget",
            $"Are you sure you want to delete the budget '{SelectedBudget.Budget.Name}'? This action cannot be undone.");

        if (!confirm) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            await expenseService.DeleteBudgetAsync(SelectedBudget.Budget.Id);
            _logger.Information("Deleted budget: {Name}", SelectedBudget.Budget.Name);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete budget");
            await _dialogService.ShowErrorAsync($"Failed to delete budget: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RecalculateSpentAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Recalculating spent amounts...";

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Recalculate for current year
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            var endOfYear = new DateTime(DateTime.Now.Year, 12, 31);
            await expenseService.RecalculateBudgetSpentAmountsAsync(startOfYear, endOfYear);

            _logger.Information("Recalculated budget spent amounts");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to recalculate spent amounts");
            await _dialogService.ShowErrorAsync($"Failed to recalculate: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    #endregion

    #region Property Change Handlers

    partial void OnSelectedPeriodChanged(BudgetPeriodFilterOption? value)
    {
        _ = LoadDataAsync();
    }

    partial void OnSelectedCategoryChanged(ExpenseCategory? value)
    {
        _ = LoadDataAsync();
    }

    #endregion
}
