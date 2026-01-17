using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Represents an exception report type for the UI.
/// </summary>
public class ExceptionReportTypeItem
{
    /// <summary>
    /// Gets the report type identifier.
    /// </summary>
    public string ReportType { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionReportTypeItem"/> class.
    /// </summary>
    public ExceptionReportTypeItem(string reportType, string name, string description)
    {
        ReportType = reportType;
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// ViewModel for the exception reports view.
/// Handles void and discount report generation and filtering.
/// </summary>
public partial class ExceptionReportsViewModel : ViewModelBase, INavigationAware
{
    private readonly IReportService _reportService;
    private readonly IReportPrintService _reportPrintService;
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    #region Observable Properties

    /// <summary>
    /// Gets the available report types.
    /// </summary>
    public ObservableCollection<ExceptionReportTypeItem> ReportTypes { get; } =
    [
        new ExceptionReportTypeItem("Void", "Void Report", "Shows all voided receipts with reasons and authorizers"),
        new ExceptionReportTypeItem("Discount", "Discount Report", "Shows all discounts given with breakdown by type and user")
    ];

    /// <summary>
    /// Gets or sets the selected report type.
    /// </summary>
    [ObservableProperty]
    private ExceptionReportTypeItem? _selectedReportType;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the available users for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<User> _users = [];

    /// <summary>
    /// Gets or sets the selected user filter.
    /// </summary>
    [ObservableProperty]
    private User? _selectedUser;

    /// <summary>
    /// Gets or sets whether a report is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasReport;

    /// <summary>
    /// Gets or sets the void report result.
    /// </summary>
    [ObservableProperty]
    private VoidReportResult? _voidReport;

    /// <summary>
    /// Gets or sets the discount report result.
    /// </summary>
    [ObservableProperty]
    private DiscountReportResult? _discountReport;

    /// <summary>
    /// Gets or sets whether the void report section is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showVoidReport;

    /// <summary>
    /// Gets or sets whether the discount report section is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showDiscountReport;

    /// <summary>
    /// Gets or sets the void report items for DataGrid binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<VoidReportItem> _voidItems = [];

    /// <summary>
    /// Gets or sets the void by reason summary for DataGrid binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<VoidByReasonSummary> _voidByReason = [];

    /// <summary>
    /// Gets or sets the discount report items for DataGrid binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiscountReportItem> _discountItems = [];

    /// <summary>
    /// Gets or sets the discount by type summary for DataGrid binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiscountByTypeSummary> _discountByType = [];

    /// <summary>
    /// Gets or sets the discount by user summary for DataGrid binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiscountByUserSummary> _discountByUser = [];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionReportsViewModel"/> class.
    /// </summary>
    /// <param name="reportService">The report service.</param>
    /// <param name="reportPrintService">The report print service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="exportService">The export service.</param>
    /// <param name="logger">The logger.</param>
    public ExceptionReportsViewModel(
        IReportService reportService,
        IReportPrintService reportPrintService,
        IUserService userService,
        INavigationService navigationService,
        IExportService exportService,
        ILogger logger) : base(logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _reportPrintService = reportPrintService ?? throw new ArgumentNullException(nameof(reportPrintService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

        Title = "Exception Reports";
        SelectedReportType = ReportTypes.First();
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadUsersAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    private async Task LoadUsersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var allUsers = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(allUsers.OrderBy(u => u.FullName));
        }, "Loading users...");
    }

    /// <summary>
    /// Generates the selected report.
    /// </summary>
    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (SelectedReportType == null)
        {
            ErrorMessage = "Please select a report type.";
            return;
        }

        if (FromDate > ToDate)
        {
            ErrorMessage = "From date cannot be after To date.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var parameters = new ExceptionReportParameters
            {
                StartDate = FromDate.Date,
                EndDate = ToDate.Date.AddDays(1).AddTicks(-1), // End of day
                GeneratedByUserId = SessionService.CurrentUserId,
                UserId = SelectedUser?.Id
            };

            if (SelectedReportType.ReportType == "Void")
            {
                VoidReport = await _reportService.GenerateVoidReportAsync(parameters);
                VoidItems = new ObservableCollection<VoidReportItem>(VoidReport.Items);
                VoidByReason = new ObservableCollection<VoidByReasonSummary>(VoidReport.ByReason);

                ShowVoidReport = true;
                ShowDiscountReport = false;

                _logger.Information("Generated void report: {VoidCount} voids, {TotalAmount:C}",
                    VoidReport.TotalCount, VoidReport.TotalAmount);
            }
            else if (SelectedReportType.ReportType == "Discount")
            {
                DiscountReport = await _reportService.GenerateDiscountReportAsync(parameters);
                DiscountItems = new ObservableCollection<DiscountReportItem>(DiscountReport.Items);
                DiscountByType = new ObservableCollection<DiscountByTypeSummary>(DiscountReport.ByType);
                DiscountByUser = new ObservableCollection<DiscountByUserSummary>(DiscountReport.ByUser);

                ShowVoidReport = false;
                ShowDiscountReport = true;

                _logger.Information("Generated discount report: {DiscountCount} discounts, {TotalDiscounts:C}",
                    DiscountReport.Items.Count, DiscountReport.TotalDiscounts);
            }

            HasReport = true;

        }, "Generating report...");
    }

    /// <summary>
    /// Prints the current report.
    /// </summary>
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (!HasReport)
        {
            ErrorMessage = "No report to print. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Convert exception report to SalesReportResult format for printing
            var salesReport = new SalesReportResult
            {
                Parameters = new SalesReportParameters
                {
                    StartDate = FromDate,
                    EndDate = ToDate,
                    GeneratedByUserId = SessionService.CurrentUserId
                },
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = SessionService.CurrentUserDisplayName
            };

            // For now, we'll need to extend the print service to support exception reports
            // This is a temporary implementation using a message dialog
            await DialogService.ShowMessageAsync(
                "Print Report",
                "Exception report printing will be fully implemented in Story 10-5 with export functionality.");

        }, "Preparing print...");
    }

    /// <summary>
    /// Exports the current report to CSV.
    /// </summary>
    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (!HasReport || SelectedReportType == null)
        {
            ErrorMessage = "No report to export. Please generate a report first.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var dialogViewModel = App.Services.GetRequiredService<ExportDialogViewModel>();
            dialogViewModel.Initialize(SelectedReportType.Name, FromDate, ToDate);
            dialogViewModel.ExportAction = ExportCurrentReportTypeAsync;

            var dialog = new ExportDialog { DataContext = dialogViewModel };
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await DialogService.ShowMessageAsync(
                    "Export Complete",
                    $"Report exported successfully to:\n{dialogViewModel.FilePath}");

                _logger.Information("Exception report exported to {FilePath}", dialogViewModel.FilePath);
            }
        }, "Preparing export...");
    }

    private async Task ExportCurrentReportTypeAsync(string filePath, CancellationToken ct)
    {
        if (SelectedReportType?.ReportType == "Void" && VoidReport != null)
        {
            await _exportService.ExportToCsvAsync(VoidItems.ToList(), filePath, ct);
        }
        else if (SelectedReportType?.ReportType == "Discount" && DiscountReport != null)
        {
            await _exportService.ExportToCsvAsync(DiscountItems.ToList(), filePath, ct);
        }
    }

    /// <summary>
    /// Sets the date range to today.
    /// </summary>
    [RelayCommand]
    private void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Sets the date range to yesterday.
    /// </summary>
    [RelayCommand]
    private void SetYesterday()
    {
        FromDate = DateTime.Today.AddDays(-1);
        ToDate = DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Sets the date range to this week.
    /// </summary>
    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var sunday = today.AddDays(-dayOfWeek);
        FromDate = sunday;
        ToDate = today;
    }

    /// <summary>
    /// Sets the date range to this month.
    /// </summary>
    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
    }

    /// <summary>
    /// Clears the user filter.
    /// </summary>
    [RelayCommand]
    private void ClearUserFilter()
    {
        SelectedUser = null;
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedReportTypeChanged(ExceptionReportTypeItem? value)
    {
        // Clear current report when type changes
        HasReport = false;
        VoidReport = null;
        DiscountReport = null;
        ShowVoidReport = false;
        ShowDiscountReport = false;
    }
}
