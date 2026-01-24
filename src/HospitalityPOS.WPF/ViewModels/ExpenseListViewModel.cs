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
/// ViewModel for the expense list view.
/// </summary>
public partial class ExpenseListViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
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

    // Search and Filters
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ExpenseCategory> _categories = new();

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<string> _statusOptions = new()
    {
        "All Statuses",
        "Pending",
        "Approved",
        "Rejected",
        "Paid"
    };

    [ObservableProperty]
    private string _selectedStatus = "All Statuses";

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    // Statistics
    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _pendingAmount;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private decimal _approvedAmount;

    [ObservableProperty]
    private int _approvedCount;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private int _paidCount;

    // Collections
    [ObservableProperty]
    private ObservableCollection<Expense> _expenses = new();

    [ObservableProperty]
    private Expense? _selectedExpense;

    // Action visibility
    [ObservableProperty]
    private bool _canApprove;

    [ObservableProperty]
    private bool _canMarkPaid;

    #endregion

    public ExpenseListViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISessionService sessionService,
        INavigationService navigationService,
        IExportService exportService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        _exportService = exportService;
        _logger = logger;

        // Default date range - current month
        var today = DateTime.Today;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
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

    #region Property Changed Handlers

    partial void OnSearchTextChanged(string value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedCategoryChanged(ExpenseCategory? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedExpenseChanged(Expense? value)
    {
        UpdateActionVisibility();
    }

    private void UpdateActionVisibility()
    {
        if (SelectedExpense == null)
        {
            CanApprove = false;
            CanMarkPaid = false;
            return;
        }

        CanApprove = SelectedExpense.Status == ExpenseStatus.Pending;
        CanMarkPaid = SelectedExpense.Status == ExpenseStatus.Approved;
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
            BusyMessage = "Loading expenses...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            // Load categories for filter
            var categories = await expenseService.GetCategoriesAsync();
            Categories = new ObservableCollection<ExpenseCategory>(categories);
            Categories.Insert(0, new ExpenseCategory { Id = 0, Name = "All Categories" });

            await ApplyFiltersAsync();

            _logger.Information("Expense list loaded with {Count} expenses", Expenses.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load expense list");
            ErrorMessage = "Failed to load expenses. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var filter = new ExpenseFilterDto
            {
                StartDate = StartDate,
                EndDate = EndDate?.AddDays(1).AddTicks(-1),
                CategoryId = SelectedCategory?.Id > 0 ? SelectedCategory.Id : null,
                Status = GetStatusFromString(SelectedStatus),
                SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
            };

            var expenses = await expenseService.GetExpensesAsync(filter);
            Expenses = new ObservableCollection<Expense>(expenses.OrderByDescending(e => e.ExpenseDate));

            // Calculate statistics
            TotalAmount = expenses.Sum(e => e.TotalAmount);
            PendingAmount = expenses.Where(e => e.Status == ExpenseStatus.Pending).Sum(e => e.TotalAmount);
            PendingCount = expenses.Count(e => e.Status == ExpenseStatus.Pending);
            ApprovedAmount = expenses.Where(e => e.Status == ExpenseStatus.Approved).Sum(e => e.TotalAmount);
            ApprovedCount = expenses.Count(e => e.Status == ExpenseStatus.Approved);
            PaidAmount = expenses.Where(e => e.Status == ExpenseStatus.Paid).Sum(e => e.TotalAmount);
            PaidCount = expenses.Count(e => e.Status == ExpenseStatus.Paid);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply expense filters");
        }
    }

    private static ExpenseStatus? GetStatusFromString(string status)
    {
        return status switch
        {
            "Pending" => ExpenseStatus.Pending,
            "Approved" => ExpenseStatus.Approved,
            "Rejected" => ExpenseStatus.Rejected,
            "Paid" => ExpenseStatus.Paid,
            _ => null
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
    private async Task EditExpenseAsync()
    {
        if (SelectedExpense == null) return;

        try
        {
            var result = await _dialogService.ShowExpenseEditorDialogAsync(SelectedExpense);
            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                var dto = new UpdateExpenseDto
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

                await expenseService.UpdateExpenseAsync(SelectedExpense.Id, dto, CurrentUserId);
                _logger.Information("Updated expense: {Description}", result.Description);

                var selectedId = SelectedExpense.Id;
                await LoadDataAsync();
                SelectedExpense = Expenses.FirstOrDefault(e => e.Id == selectedId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update expense");
            ErrorMessage = "Failed to update expense. Please try again.";
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync()
    {
        if (SelectedExpense == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Expense",
                $"Are you sure you want to delete expense '{SelectedExpense.ExpenseNumber}'?\n\n{SelectedExpense.Description}");

            if (confirmed)
            {
                using var scope = _scopeFactory.CreateScope();
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

                await expenseService.DeleteExpenseAsync(SelectedExpense.Id, CurrentUserId);
                _logger.Information("Deleted expense: {ExpenseNumber}", SelectedExpense.ExpenseNumber);

                SelectedExpense = null;
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete expense");
            ErrorMessage = "Failed to delete expense. Please try again.";
        }
    }

    [RelayCommand]
    private async Task ApproveExpenseAsync()
    {
        if (SelectedExpense == null || SelectedExpense.Status != ExpenseStatus.Pending) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            await expenseService.ApproveExpenseAsync(SelectedExpense.Id, CurrentUserId);
            _logger.Information("Approved expense: {ExpenseNumber}", SelectedExpense.ExpenseNumber);

            var selectedId = SelectedExpense.Id;
            await LoadDataAsync();
            SelectedExpense = Expenses.FirstOrDefault(e => e.Id == selectedId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to approve expense");
            ErrorMessage = "Failed to approve expense. Please try again.";
        }
    }

    [RelayCommand]
    private async Task MarkAsPaidAsync()
    {
        if (SelectedExpense == null || SelectedExpense.Status != ExpenseStatus.Approved) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            await expenseService.MarkExpenseAsPaidAsync(SelectedExpense.Id, CurrentUserId);
            _logger.Information("Marked expense as paid: {ExpenseNumber}", SelectedExpense.ExpenseNumber);

            var selectedId = SelectedExpense.Id;
            await LoadDataAsync();
            SelectedExpense = Expenses.FirstOrDefault(e => e.Id == selectedId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to mark expense as paid");
            ErrorMessage = "Failed to mark expense as paid. Please try again.";
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            if (!Expenses.Any())
            {
                await _dialogService.ShowInfoAsync("Export", "No expenses to export.");
                return;
            }

            IsBusy = true;
            BusyMessage = "Preparing export...";

            // Transform expenses to export format
            var exportData = Expenses.Select(e => new
            {
                ExpenseNumber = $"EXP-{e.CreatedAt:yyyyMMdd}-{e.Id:D4}",
                Date = e.ExpenseDate.ToString("yyyy-MM-dd"),
                e.Description,
                Category = e.ExpenseCategory?.Name ?? "Uncategorized",
                Supplier = e.Supplier?.Name ?? "-",
                Amount = e.Amount,
                Tax = e.TaxAmount,
                Total = e.TotalAmount,
                Status = e.Status.ToString(),
                PaymentMethod = e.PaymentMethod?.Name ?? "-",
                Reference = e.PaymentReference ?? "-",
                Notes = e.Notes ?? "-",
                IsTaxDeductible = e.IsTaxDeductible ? "Yes" : "No",
                RecordedBy = e.CreatedByUser?.FullName ?? "Unknown",
                RecordedAt = e.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            var dateRange = StartDate.HasValue && EndDate.HasValue
                ? $"_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}"
                : $"_{DateTime.Now:yyyyMMdd}";
            var defaultFileName = $"Expenses{dateRange}";

            var result = await _exportService.ExportToExcelAsync(
                exportData,
                defaultFileName,
                "Expenses");

            if (result)
            {
                _logger.Information("Exported {Count} expenses to Excel", Expenses.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export expenses");
            ErrorMessage = "Failed to export expenses. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    #endregion
}
