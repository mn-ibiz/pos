using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Time-Based Pricing (Happy Hour) - handles time pricing rules, schedules, and effectiveness reports.
/// </summary>
public partial class TimePricingViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<TimePricingRule> _pricingRules = new();

    [ObservableProperty]
    private ObservableCollection<TimePricingRule> _activeRulesNow = new();

    [ObservableProperty]
    private ObservableCollection<TimePricingUsage> _usageReport = new();

    [ObservableProperty]
    private TimePricingRule? _selectedRule;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private int _totalRules;

    [ObservableProperty]
    private int _activeNow;

    [ObservableProperty]
    private decimal _revenueImpact;

    [ObservableProperty]
    private int _transactionsAffected;

    // Rule Editor
    [ObservableProperty]
    private bool _isRuleEditorOpen;

    [ObservableProperty]
    private TimePricingRuleRequest _editingRule = new();

    [ObservableProperty]
    private bool _isNewRule;

    // Date range for reports
    [ObservableProperty]
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [ObservableProperty]
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    #endregion

    public List<DayOfWeek> DaysOfWeek { get; } = Enum.GetValues<DayOfWeek>().ToList();

    public TimePricingViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Time-Based Pricing";
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
            var timePricingService = scope.ServiceProvider.GetService<ITimePricingService>();

            if (timePricingService is null)
            {
                ErrorMessage = "Time pricing service not available";
                return;
            }

            // Load all rules
            var rules = await timePricingService.GetAllRulesAsync();
            PricingRules = new ObservableCollection<TimePricingRule>(rules);
            TotalRules = rules.Count;

            // Load currently active rules
            var currentTime = TimeOnly.FromDateTime(DateTime.Now);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var activeNow = await timePricingService.GetActiveRulesAsync(today, currentTime);
            ActiveRulesNow = new ObservableCollection<TimePricingRule>(activeNow);
            ActiveNow = activeNow.Count;

            // Load usage report
            var usage = await timePricingService.GetUsageReportAsync(StartDate, EndDate);
            UsageReport = new ObservableCollection<TimePricingUsage>(usage);
            RevenueImpact = usage.Sum(u => u.DiscountAmount);
            TransactionsAffected = usage.Sum(u => u.TransactionCount);

            IsLoading = false;
        }, "Loading time pricing data...");
    }

    [RelayCommand]
    private void CreateNewRule()
    {
        EditingRule = new TimePricingRuleRequest
        {
            StartTime = new TimeOnly(16, 0), // Default 4 PM
            EndTime = new TimeOnly(19, 0),   // Default 7 PM
            DiscountPercent = 20m,
            IsActive = true
        };
        IsNewRule = true;
        IsRuleEditorOpen = true;
    }

    [RelayCommand]
    private void EditRule(TimePricingRule? rule)
    {
        if (rule is null) return;

        EditingRule = new TimePricingRuleRequest
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime,
            DaysOfWeek = rule.DaysOfWeek,
            DiscountPercent = rule.DiscountPercent,
            FixedPrice = rule.FixedPrice,
            AppliesTo = rule.AppliesTo,
            CategoryIds = rule.CategoryIds,
            ProductIds = rule.ProductIds,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo,
            IsActive = rule.IsActive
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
            var timePricingService = scope.ServiceProvider.GetService<ITimePricingService>();

            if (timePricingService is null)
            {
                ErrorMessage = "Time pricing service not available";
                return;
            }

            if (IsNewRule)
            {
                await timePricingService.CreateRuleAsync(EditingRule);
            }
            else
            {
                await timePricingService.UpdateRuleAsync(EditingRule);
            }

            IsRuleEditorOpen = false;
            await LoadDataAsync();
        }, "Saving pricing rule...");
    }

    [RelayCommand]
    private void CancelEditRule()
    {
        IsRuleEditorOpen = false;
    }

    [RelayCommand]
    private async Task ToggleRuleActiveAsync(TimePricingRule? rule)
    {
        if (rule is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var timePricingService = scope.ServiceProvider.GetService<ITimePricingService>();

            if (timePricingService is null)
            {
                ErrorMessage = "Time pricing service not available";
                return;
            }

            if (rule.IsActive)
            {
                await timePricingService.DeactivateRuleAsync(rule.Id);
            }
            else
            {
                await timePricingService.ActivateRuleAsync(rule.Id);
            }

            await LoadDataAsync();
        }, rule.IsActive ? "Deactivating rule..." : "Activating rule...");
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(TimePricingRule? rule)
    {
        if (rule is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Delete Rule",
            $"Are you sure you want to delete '{rule.Name}'?\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var timePricingService = scope.ServiceProvider.GetService<ITimePricingService>();

            if (timePricingService is null)
            {
                ErrorMessage = "Time pricing service not available";
                return;
            }

            await timePricingService.DeleteRuleAsync(rule.Id);
            await LoadDataAsync();
        }, "Deleting rule...");
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class TimePricingRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public decimal? DiscountPercent { get; set; }
    public decimal? FixedPrice { get; set; }
    public string AppliesTo { get; set; } = "All"; // All, Category, Product
    public List<int>? CategoryIds { get; set; }
    public List<int>? ProductIds { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public string TimeRange => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
    public string DaysDisplay => DaysOfWeek.Count == 7 ? "Every day" : string.Join(", ", DaysOfWeek.Select(d => d.ToString()[..3]));
}

public class TimePricingRuleRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public decimal? DiscountPercent { get; set; }
    public decimal? FixedPrice { get; set; }
    public string AppliesTo { get; set; } = "All";
    public List<int>? CategoryIds { get; set; }
    public List<int>? ProductIds { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TimePricingUsage
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetSales { get; set; }
    public DateOnly Date { get; set; }
}
