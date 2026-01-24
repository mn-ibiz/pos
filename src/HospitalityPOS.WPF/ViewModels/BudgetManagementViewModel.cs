using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Budget Management - handles budget creation, approval workflows, variance analysis, and compliance tracking.
/// </summary>
public partial class BudgetManagementViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Budget> _budgets = new();

    [ObservableProperty]
    private ObservableCollection<BudgetLineItem> _budgetLineItems = new();

    [ObservableProperty]
    private ObservableCollection<BudgetVariance> _varianceReport = new();

    [ObservableProperty]
    private Budget? _selectedBudget;

    [ObservableProperty]
    private BudgetLineItem? _selectedLineItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    // Summary stats
    [ObservableProperty]
    private decimal _totalBudgeted;

    [ObservableProperty]
    private decimal _totalActual;

    [ObservableProperty]
    private decimal _totalVariance;

    [ObservableProperty]
    private decimal _utilizationPercent;

    // Budget Editor
    [ObservableProperty]
    private bool _isBudgetEditorOpen;

    [ObservableProperty]
    private BudgetRequest _editingBudget = new();

    [ObservableProperty]
    private bool _isNewBudget;

    // Line Item Editor
    [ObservableProperty]
    private bool _isLineItemEditorOpen;

    [ObservableProperty]
    private BudgetLineItemRequest _editingLineItem = new();

    #endregion

    public List<int> AvailableYears { get; } = Enumerable.Range(DateTime.Today.Year - 1, 3).ToList();
    public List<int> AvailableMonths { get; } = Enumerable.Range(1, 12).ToList();

    public BudgetManagementViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Budget Management";
        _selectedYear = DateTime.Today.Year;
        _selectedMonth = DateTime.Today.Month;
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
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            // Load budgets for selected year
            var budgets = await budgetService.GetBudgetsAsync(SelectedYear);
            Budgets = new ObservableCollection<Budget>(budgets);

            // Load variance report
            var variance = await budgetService.GetVarianceReportAsync(SelectedYear, SelectedMonth);
            VarianceReport = new ObservableCollection<BudgetVariance>(variance.LineItems);

            // Set summary totals
            TotalBudgeted = variance.TotalBudgeted;
            TotalActual = variance.TotalActual;
            TotalVariance = variance.TotalVariance;
            UtilizationPercent = TotalBudgeted > 0 ? (TotalActual / TotalBudgeted) * 100 : 0;

            IsLoading = false;
        }, "Loading budget data...");
    }

    [RelayCommand]
    private void CreateBudget()
    {
        EditingBudget = new BudgetRequest
        {
            Year = SelectedYear,
            StartDate = new DateOnly(SelectedYear, 1, 1),
            EndDate = new DateOnly(SelectedYear, 12, 31)
        };
        IsNewBudget = true;
        IsBudgetEditorOpen = true;
    }

    [RelayCommand]
    private void EditBudget(Budget? budget)
    {
        if (budget is null) return;

        EditingBudget = new BudgetRequest
        {
            Id = budget.Id,
            Name = budget.Name,
            Description = budget.Description,
            Year = budget.Year,
            DepartmentId = budget.DepartmentId,
            StartDate = budget.StartDate,
            EndDate = budget.EndDate
        };
        IsNewBudget = false;
        IsBudgetEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            if (IsNewBudget)
            {
                await budgetService.CreateBudgetAsync(EditingBudget);
            }
            else
            {
                await budgetService.UpdateBudgetAsync(EditingBudget);
            }

            IsBudgetEditorOpen = false;
            await LoadDataAsync();
        }, "Saving budget...");
    }

    [RelayCommand]
    private void CancelEditBudget()
    {
        IsBudgetEditorOpen = false;
    }

    [RelayCommand]
    private async Task ViewBudgetDetailsAsync(Budget? budget)
    {
        if (budget is null) return;

        SelectedBudget = budget;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            var lineItems = await budgetService.GetBudgetLineItemsAsync(budget.Id);
            BudgetLineItems = new ObservableCollection<BudgetLineItem>(lineItems);
        }, "Loading budget details...");
    }

    [RelayCommand]
    private void AddLineItem()
    {
        if (SelectedBudget is null)
        {
            _ = DialogService.ShowWarningAsync("Select Budget", "Please select a budget first.");
            return;
        }

        EditingLineItem = new BudgetLineItemRequest
        {
            BudgetId = SelectedBudget.Id
        };
        IsLineItemEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveLineItemAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            await budgetService.AddLineItemAsync(EditingLineItem);
            IsLineItemEditorOpen = false;
            await ViewBudgetDetailsAsync(SelectedBudget);
        }, "Saving line item...");
    }

    [RelayCommand]
    private void CancelEditLineItem()
    {
        IsLineItemEditorOpen = false;
    }

    [RelayCommand]
    private async Task SubmitForApprovalAsync(Budget? budget)
    {
        if (budget is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Submit for Approval",
            $"Submit budget '{budget.Name}' for approval?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            await budgetService.SubmitForApprovalAsync(budget.Id, SessionService.CurrentUserId);
            await DialogService.ShowMessageAsync("Success", "Budget submitted for approval.");
            await LoadDataAsync();
        }, "Submitting budget...");
    }

    [RelayCommand]
    private async Task ApproveAsync(Budget? budget)
    {
        if (budget is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Budget",
            $"Approve budget '{budget.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            await budgetService.ApproveBudgetAsync(budget.Id, SessionService.CurrentUserId);
            await DialogService.ShowMessageAsync("Success", "Budget approved.");
            await LoadDataAsync();
        }, "Approving budget...");
    }

    [RelayCommand]
    private async Task RejectAsync(Budget? budget)
    {
        if (budget is null) return;

        var reason = await DialogService.ShowInputAsync("Reject Budget", "Please provide a reason:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetService<IBudgetService>();

            if (budgetService is null)
            {
                ErrorMessage = "Budget service not available";
                return;
            }

            await budgetService.RejectBudgetAsync(budget.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Rejecting budget...");
    }

    [RelayCommand]
    private async Task ExportVarianceReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Variance report export functionality will be available soon.");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedYearChanged(int value) => _ = LoadDataAsync();
    partial void OnSelectedMonthChanged(int value) => _ = LoadDataAsync();
}

// DTOs
public class Budget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Pending, Approved, Rejected
    public DateTime CreatedAt { get; set; }
}

public class BudgetRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class BudgetLineItem
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public int? GLAccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance => BudgetedAmount - ActualAmount;
    public decimal VariancePercent => BudgetedAmount > 0 ? (Variance / BudgetedAmount) * 100 : 0;
    public string? Notes { get; set; }
}

public class BudgetLineItemRequest
{
    public int BudgetId { get; set; }
    public int? GLAccountId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public string? Notes { get; set; }
}

public class BudgetVariance
{
    public string Category { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Budgeted - Actual;
    public decimal VariancePercent => Budgeted > 0 ? (Variance / Budgeted) * 100 : 0;
    public string Status => Variance >= 0 ? "Under Budget" : "Over Budget";
}

public class VarianceReport
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance => TotalBudgeted - TotalActual;
    public List<BudgetVariance> LineItems { get; set; } = new();
}
