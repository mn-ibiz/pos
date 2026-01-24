using System.Collections.ObjectModel;
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
/// ViewModel for managing recurring expenses.
/// </summary>
public partial class RecurringExpenseListViewModel : ObservableObject, INavigationAware
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

    [ObservableProperty]
    private ObservableCollection<RecurringExpense> _recurringExpenses = [];

    [ObservableProperty]
    private RecurringExpense? _selectedExpense;

    [ObservableProperty]
    private bool _showPausedExpenses = true;


    [ObservableProperty]
    private ObservableCollection<ExpenseCategory> _categories = [];

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    // Statistics
    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _pausedCount;

    [ObservableProperty]
    private decimal _monthlyProjection;

    [ObservableProperty]
    private int _dueThisWeek;

    [ObservableProperty]
    private decimal _dueThisWeekAmount;

    #endregion

    /// <summary>
    /// Gets the available frequency options for filtering.
    /// </summary>
    public ObservableCollection<FrequencyFilterOption> FrequencyOptions { get; } =
    [
        new FrequencyFilterOption(null, "All Frequencies"),
        new FrequencyFilterOption(RecurrenceFrequency.Daily, "Daily"),
        new FrequencyFilterOption(RecurrenceFrequency.Weekly, "Weekly"),
        new FrequencyFilterOption(RecurrenceFrequency.BiWeekly, "Bi-Weekly"),
        new FrequencyFilterOption(RecurrenceFrequency.Monthly, "Monthly"),
        new FrequencyFilterOption(RecurrenceFrequency.Quarterly, "Quarterly"),
        new FrequencyFilterOption(RecurrenceFrequency.Annually, "Annually")
    ];

    [ObservableProperty]
    private FrequencyFilterOption? _selectedFrequencyOption;

    private int CurrentUserId => _sessionService.CurrentUser?.Id ?? 1;

    public RecurringExpenseListViewModel(
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
            BusyMessage = "Loading recurring expenses...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Load categories
            var categories = await expenseService.GetCategoriesAsync();
            Categories = new ObservableCollection<ExpenseCategory>(categories);

            // Load recurring expenses
            var expenses = await expenseService.GetRecurringExpensesAsync(activeOnly: false);

            // Apply filters
            var filtered = expenses.AsEnumerable();

            if (!ShowPausedExpenses)
            {
                filtered = filtered.Where(e => e.IsActive);
            }

            if (SelectedFrequencyOption?.Value != null)
            {
                filtered = filtered.Where(e => e.Frequency == SelectedFrequencyOption.Value);
            }

            if (SelectedCategory != null)
            {
                filtered = filtered.Where(e => e.ExpenseCategoryId == SelectedCategory.Id);
            }

            RecurringExpenses = new ObservableCollection<RecurringExpense>(filtered.OrderBy(e => e.NextDueDate));

            // Calculate statistics
            CalculateStatistics(expenses);

            _logger.Information("Loaded {Count} recurring expenses", RecurringExpenses.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load recurring expenses");
            ErrorMessage = "Failed to load recurring expenses. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private void CalculateStatistics(IEnumerable<RecurringExpense> allExpenses)
    {
        var expenseList = allExpenses.ToList();

        TotalCount = expenseList.Count;
        ActiveCount = expenseList.Count(e => e.IsActive);
        PausedCount = expenseList.Count(e => !e.IsActive);

        // Monthly projection based on active expenses
        MonthlyProjection = expenseList
            .Where(e => e.IsActive)
            .Sum(e => CalculateMonthlyAmount(e));

        // Due this week
        var weekEnd = DateTime.Today.AddDays(7);
        var dueExpenses = expenseList
            .Where(e => e.IsActive && e.NextDueDate.HasValue && e.NextDueDate.Value.Date <= weekEnd)
            .ToList();

        DueThisWeek = dueExpenses.Count;
        DueThisWeekAmount = dueExpenses.Sum(e => e.Amount);
    }

    private static decimal CalculateMonthlyAmount(RecurringExpense expense)
    {
        return expense.Frequency switch
        {
            RecurrenceFrequency.Daily => expense.Amount * 30,
            RecurrenceFrequency.Weekly => expense.Amount * 4.33m,
            RecurrenceFrequency.BiWeekly => expense.Amount * 2.17m,
            RecurrenceFrequency.Monthly => expense.Amount,
            RecurrenceFrequency.Quarterly => expense.Amount / 3,
            RecurrenceFrequency.Annually => expense.Amount / 12,
            _ => expense.Amount
        };
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
    private async Task AddRecurringExpenseAsync()
    {
        try
        {
            var result = await _dialogService.ShowRecurringExpenseEditorDialogAsync(null);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                await expenseService.CreateRecurringExpenseAsync(result);
                _logger.Information("Created recurring expense: {Name}", result.Name);

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create recurring expense");
            await _dialogService.ShowErrorAsync($"Failed to create recurring expense: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditRecurringExpenseAsync()
    {
        if (SelectedExpense == null) return;

        try
        {
            var result = await _dialogService.ShowRecurringExpenseEditorDialogAsync(SelectedExpense);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                result.Id = SelectedExpense.Id;
                await expenseService.UpdateRecurringExpenseAsync(result);
                _logger.Information("Updated recurring expense: {Name}", result.Name);

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update recurring expense");
            await _dialogService.ShowErrorAsync($"Failed to update recurring expense: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteRecurringExpenseAsync()
    {
        if (SelectedExpense == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Recurring Expense",
            $"Are you sure you want to delete '{SelectedExpense.Name}'? This action cannot be undone.");

        if (!confirm) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            await expenseService.DeleteRecurringExpenseAsync(SelectedExpense.Id);
            _logger.Information("Deleted recurring expense: {Name}", SelectedExpense.Name);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete recurring expense");
            await _dialogService.ShowErrorAsync($"Failed to delete recurring expense: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        if (SelectedExpense == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Toggle the active status
            SelectedExpense.IsActive = !SelectedExpense.IsActive;
            await expenseService.UpdateRecurringExpenseAsync(SelectedExpense);

            var action = SelectedExpense.IsActive ? "resumed" : "paused";
            _logger.Information("{Action} recurring expense: {Name}", action, SelectedExpense.Name);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle recurring expense status");
            await _dialogService.ShowErrorAsync($"Failed to update status: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GenerateNowAsync()
    {
        if (SelectedExpense == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Generate Expense Now",
            $"Generate an expense from '{SelectedExpense.Name}' now? This will create a new expense record.");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Generating expense...";

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var expense = await expenseService.GenerateExpenseFromRecurringAsync(
                SelectedExpense.Id,
                CurrentUserId);

            _logger.Information("Generated expense {ExpenseNumber} from recurring expense {Name}",
                expense.ExpenseNumber, SelectedExpense.Name);

            await _dialogService.ShowSuccessAsync(
                "Expense Generated",
                $"Expense {expense.ExpenseNumber} has been created for KES {expense.TotalAmount:N2}.");

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate expense from recurring");
            await _dialogService.ShowErrorAsync($"Failed to generate expense: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ProcessAllDueAsync()
    {
        if (DueThisWeek == 0)
        {
            await _dialogService.ShowInfoAsync("No Due Expenses", "There are no recurring expenses due at this time.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Process All Due Expenses",
            $"Generate {DueThisWeek} expense(s) totaling KES {DueThisWeekAmount:N2}?");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Processing due expenses...";

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var generated = await expenseService.ProcessDueRecurringExpensesAsync(CurrentUserId);

            _logger.Information("Processed {Count} due recurring expenses", generated.Count);

            await _dialogService.ShowSuccessAsync(
                "Expenses Generated",
                $"Successfully generated {generated.Count} expense(s).");

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process due recurring expenses");
            await _dialogService.ShowErrorAsync($"Failed to process expenses: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    #endregion

    #region Property Change Handlers

    partial void OnShowPausedExpensesChanged(bool value)
    {
        _ = LoadDataAsync();
    }

    partial void OnSelectedFrequencyOptionChanged(FrequencyFilterOption? value)
    {
        _ = LoadDataAsync();
    }

    partial void OnSelectedCategoryChanged(ExpenseCategory? value)
    {
        _ = LoadDataAsync();
    }

    #endregion
}

/// <summary>
/// Represents a frequency filter option for the combobox.
/// </summary>
public class FrequencyFilterOption
{
    public RecurrenceFrequency? Value { get; }
    public string DisplayName { get; }

    public FrequencyFilterOption(RecurrenceFrequency? value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
