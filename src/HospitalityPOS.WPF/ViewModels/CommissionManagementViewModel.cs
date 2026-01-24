using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Commission Management - handles commission rules, calculations, payouts, and reporting.
/// </summary>
public partial class CommissionManagementViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<CommissionRule> _commissionRules = new();

    [ObservableProperty]
    private ObservableCollection<CommissionTransaction> _transactions = new();

    [ObservableProperty]
    private ObservableCollection<CommissionPayout> _pendingPayouts = new();

    [ObservableProperty]
    private ObservableCollection<EmployeeCommissionSummary> _employeeSummaries = new();

    [ObservableProperty]
    private CommissionRule? _selectedRule;

    [ObservableProperty]
    private CommissionTransaction? _selectedTransaction;

    [ObservableProperty]
    private CommissionPayout? _selectedPayout;

    [ObservableProperty]
    private EmployeeCommissionSummary? _selectedEmployee;

    [ObservableProperty]
    private CommissionSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

    [ObservableProperty]
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private decimal _totalCommissionEarned;

    [ObservableProperty]
    private decimal _totalCommissionPaid;

    [ObservableProperty]
    private decimal _pendingCommission;

    [ObservableProperty]
    private int _activeRulesCount;

    // Rule Editor
    [ObservableProperty]
    private bool _isRuleEditorOpen;

    [ObservableProperty]
    private CommissionRuleRequest _editingRule = new();

    [ObservableProperty]
    private bool _isNewRule;

    #endregion

    public CommissionManagementViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Commission Management";
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
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            // Load commission rules
            var rules = await commissionService.GetActiveRulesAsync();
            CommissionRules = new ObservableCollection<CommissionRule>(rules);
            ActiveRulesCount = rules.Count;

            // Load pending payouts
            var payouts = await commissionService.GetPendingPayoutsAsync();
            PendingPayouts = new ObservableCollection<CommissionPayout>(payouts);

            // Load report for date range
            var report = await commissionService.GenerateReportAsync(StartDate, EndDate);
            EmployeeSummaries = new ObservableCollection<EmployeeCommissionSummary>(report.Employees);
            TotalCommissionEarned = report.TotalCommission;

            // Calculate pending commission
            PendingCommission = payouts.Where(p => p.Status == CommissionPayoutStatus.Pending).Sum(p => p.NetPayout);
            TotalCommissionPaid = payouts.Where(p => p.Status == CommissionPayoutStatus.Paid).Sum(p => p.NetPayout);

            // Load settings
            Settings = await commissionService.GetSettingsAsync();

            IsLoading = false;
        }, "Loading commission data...");
    }

    [RelayCommand]
    private void CreateNewRule()
    {
        EditingRule = new CommissionRuleRequest
        {
            CalculationMethod = CommissionCalculationMethod.Percentage,
            CommissionRate = 5m,
            Priority = 0
        };
        IsNewRule = true;
        IsRuleEditorOpen = true;
    }

    [RelayCommand]
    private void EditRule(CommissionRule? rule)
    {
        if (rule is null) return;

        EditingRule = new CommissionRuleRequest
        {
            Id = rule.Id,
            Name = rule.Name,
            RuleType = rule.RuleType,
            CalculationMethod = rule.CalculationMethod,
            TargetId = rule.RuleType switch
            {
                CommissionRuleType.Role => rule.RoleId,
                CommissionRuleType.Category => rule.CategoryId,
                CommissionRuleType.Product => rule.ProductId,
                CommissionRuleType.Employee => rule.EmployeeId,
                _ => null
            },
            CommissionRate = rule.CommissionRate,
            FixedAmount = rule.FixedAmount,
            MinimumSaleAmount = rule.MinimumSaleAmount,
            MaximumCommission = rule.MaximumCommission,
            Priority = rule.Priority,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo,
            Tiers = rule.Tiers
        };
        IsNewRule = false;
        IsRuleEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            if (IsNewRule)
            {
                await commissionService.CreateRuleAsync(EditingRule);
            }
            else
            {
                await commissionService.UpdateRuleAsync(EditingRule);
            }

            IsRuleEditorOpen = false;
            await LoadDataAsync();
        }, "Saving commission rule...");
    }

    [RelayCommand]
    private void CancelEditRule()
    {
        IsRuleEditorOpen = false;
    }

    [RelayCommand]
    private async Task DeactivateRuleAsync(CommissionRule? rule)
    {
        if (rule is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Deactivate Rule",
            $"Are you sure you want to deactivate the rule '{rule.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            await commissionService.DeactivateRuleAsync(rule.Id);
            await LoadDataAsync();
        }, "Deactivating rule...");
    }

    [RelayCommand]
    private async Task ApprovePayoutAsync(CommissionPayout? payout)
    {
        if (payout is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Payout",
            $"Approve commission payout of KSh {payout.NetPayout:N0} for {payout.EmployeeName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            await commissionService.ApprovePayoutAsync(payout.Id, SessionService.CurrentUserId);
            await LoadDataAsync();
        }, "Approving payout...");
    }

    [RelayCommand]
    private async Task MarkPayoutPaidAsync(CommissionPayout? payout)
    {
        if (payout is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            await commissionService.MarkPayoutPaidAsync(payout.Id);
            await LoadDataAsync();
        }, "Processing payment...");
    }

    [RelayCommand]
    private async Task ViewEmployeeDetailsAsync(EmployeeCommissionSummary? summary)
    {
        if (summary is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            var transactions = await commissionService.GetEmployeeTransactionsAsync(
                summary.EmployeeId, StartDate, EndDate);
            Transactions = new ObservableCollection<CommissionTransaction>(transactions);
            SelectedEmployee = summary;
        }, "Loading employee transactions...");
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            var export = await commissionService.ExportForPayrollAsync(StartDate, EndDate);

            await DialogService.ShowMessageAsync(
                "Export Complete",
                $"Commission data exported for {export.Employees.Count} employees.\nTotal: KSh {export.TotalCommission:N0}");
        }, "Exporting commission data...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var commissionService = scope.ServiceProvider.GetService<ICommissionService>();

            if (commissionService is null)
            {
                ErrorMessage = "Commission service not available";
                return;
            }

            await commissionService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "Commission settings saved successfully.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnStartDateChanged(DateOnly value)
    {
        if (value <= EndDate)
        {
            _ = LoadDataAsync();
        }
    }

    partial void OnEndDateChanged(DateOnly value)
    {
        if (value >= StartDate)
        {
            _ = LoadDataAsync();
        }
    }
}
