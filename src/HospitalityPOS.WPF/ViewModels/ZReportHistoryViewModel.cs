using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Z-Report History view.
/// Allows admins to browse and reprint historical Z-Reports by cashier.
/// </summary>
public partial class ZReportHistoryViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the start date for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    /// <summary>
    /// Gets or sets the end date for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the selected cashier for filtering.
    /// </summary>
    [ObservableProperty]
    private User? _selectedCashier;

    /// <summary>
    /// Gets or sets the list of cashiers for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<User> _cashiers = [];

    /// <summary>
    /// Gets or sets the list of work periods (Z-Reports).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkPeriodSummary> _workPeriods = [];

    /// <summary>
    /// Gets or sets the selected work period.
    /// </summary>
    [ObservableProperty]
    private WorkPeriodSummary? _selectedWorkPeriod;

    /// <summary>
    /// Gets the total count of Z-Reports displayed.
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// Gets the total sales across displayed Z-Reports.
    /// </summary>
    [ObservableProperty]
    private decimal _totalSales;

    #endregion

    public ZReportHistoryViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Z-Report History";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    #region Commands

    /// <summary>
    /// Loads the work period history and cashiers.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            // Load cashiers for filter dropdown
            var allUsers = await userService.GetAllUsersAsync();
            Cashiers = new ObservableCollection<User>(
                allUsers.Where(u => u.IsActive).OrderBy(u => u.FullName));

            // Load work period history
            await RefreshWorkPeriodsAsync(workPeriodService);
        }, "Loading Z-Report history...");
    }

    /// <summary>
    /// Refreshes the work periods based on current filters.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
            await RefreshWorkPeriodsAsync(workPeriodService);
        }, "Refreshing...");
    }

    private async Task RefreshWorkPeriodsAsync(IWorkPeriodService workPeriodService)
    {
        var periods = await workPeriodService.GetWorkPeriodHistoryAsync(
            StartDate,
            EndDate.AddDays(1)); // Include end date

        // Filter by cashier if selected
        var filtered = periods
            .Where(p => p.Status == Core.Enums.WorkPeriodStatus.Closed)
            .Where(p => SelectedCashier == null || p.ClosedByUserId == SelectedCashier.Id)
            .OrderByDescending(p => p.ClosedAt)
            .Select(p => new WorkPeriodSummary
            {
                Id = p.Id,
                ZReportNumber = p.ZReportNumber ?? 0,
                OpenedAt = p.OpenedAt,
                ClosedAt = p.ClosedAt ?? DateTime.MinValue,
                OpenedByName = p.OpenedByUser?.FullName ?? "Unknown",
                ClosedByName = p.ClosedByUser?.FullName ?? "Unknown",
                ClosedByUserId = p.ClosedByUserId,
                OpeningFloat = p.OpeningFloat,
                ClosingCash = p.ClosingCash ?? 0,
                ExpectedCash = p.ExpectedCash ?? 0,
                Variance = p.Variance ?? 0
            })
            .ToList();

        WorkPeriods = new ObservableCollection<WorkPeriodSummary>(filtered);
        TotalCount = filtered.Count;
        // Total sales would need to be calculated from receipts, showing total closing cash instead
        TotalSales = filtered.Sum(p => p.ClosingCash);
    }

    /// <summary>
    /// Views the selected Z-Report.
    /// </summary>
    [RelayCommand]
    private async Task ViewZReportAsync()
    {
        if (SelectedWorkPeriod == null)
        {
            await _dialogService.ShowErrorAsync("No Selection", "Please select a Z-Report to view.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            var zReport = await workPeriodService.GenerateZReportAsync(SelectedWorkPeriod.Id);
            await _dialogService.ShowZReportDialogAsync(zReport, autoPrint: false);
        }, "Loading Z-Report...");
    }

    /// <summary>
    /// Reprints the selected Z-Report.
    /// </summary>
    [RelayCommand]
    private async Task ReprintZReportAsync()
    {
        if (SelectedWorkPeriod == null)
        {
            await _dialogService.ShowErrorAsync("No Selection", "Please select a Z-Report to reprint.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Reprint Z-Report",
            $"Are you sure you want to reprint Z-Report #{SelectedWorkPeriod.ZReportNumber:D4}?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            var zReport = await workPeriodService.GenerateZReportAsync(SelectedWorkPeriod.Id);
            await _dialogService.ShowZReportDialogAsync(zReport, autoPrint: true);

            _logger.Information("Z-Report #{ZReportNumber} reprinted by admin", SelectedWorkPeriod.ZReportNumber);
        }, "Reprinting Z-Report...");
    }

    /// <summary>
    /// Clears the cashier filter.
    /// </summary>
    [RelayCommand]
    private void ClearCashierFilter()
    {
        SelectedCashier = null;
        _ = RefreshAsync();
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    partial void OnStartDateChanged(DateTime value)
    {
        _ = RefreshAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        _ = RefreshAsync();
    }

    partial void OnSelectedCashierChanged(User? value)
    {
        _ = RefreshAsync();
    }
}

/// <summary>
/// Summary model for work period display in the history list.
/// </summary>
public class WorkPeriodSummary
{
    public int Id { get; set; }
    public int ZReportNumber { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime ClosedAt { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public string ClosedByName { get; set; } = string.Empty;
    public int? ClosedByUserId { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal Variance { get; set; }

    public string ZReportDisplay => $"Z-{ZReportNumber:D4}";
    public string DateDisplay => ClosedAt.ToLocalTime().ToString("yyyy-MM-dd");
    public string TimeDisplay => $"{OpenedAt.ToLocalTime():HH:mm} - {ClosedAt.ToLocalTime():HH:mm}";
    public TimeSpan Duration => ClosedAt - OpenedAt;
    public string DurationDisplay => $"{(int)Duration.TotalHours}h {Duration.Minutes:D2}m";

    public bool IsShort => Variance < 0;
    public bool IsOver => Variance > 0;
    public bool IsExact => Variance == 0;
    public string VarianceStatus => IsShort ? "SHORT" : (IsOver ? "OVER" : "EXACT");
}
