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
/// ViewModel for the audit reports view.
/// Handles audit report type selection, filtering, and report generation.
/// </summary>
public partial class AuditReportsViewModel : ViewModelBase, INavigationAware
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
    public ObservableCollection<AuditReportTypeItem> ReportTypes { get; } =
    [
        new AuditReportTypeItem(AuditReportType.AllActivity, "All Activity", "Complete audit trail of all system actions"),
        new AuditReportTypeItem(AuditReportType.UserActivity, "User Activity", "Login, logout, and user session activity"),
        new AuditReportTypeItem(AuditReportType.TransactionLog, "Transaction Log", "Orders, receipts, and payment transactions"),
        new AuditReportTypeItem(AuditReportType.VoidRefundLog, "Void/Refund Log", "Voided receipts and refunds with reasons"),
        new AuditReportTypeItem(AuditReportType.PriceChangeLog, "Price Change Log", "Product price changes and who made them"),
        new AuditReportTypeItem(AuditReportType.PermissionOverrideLog, "Permission Override Log", "Permission override requests and authorizations")
    ];

    /// <summary>
    /// Gets or sets the selected report type.
    /// </summary>
    [ObservableProperty]
    private AuditReportTypeItem? _selectedReportType;

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
    /// Gets or sets the list of users for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<User> _users = [];

    /// <summary>
    /// Gets or sets the selected user for filtering.
    /// </summary>
    [ObservableProperty]
    private User? _selectedUser;

    /// <summary>
    /// Gets or sets the list of available actions.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _availableActions = [];

    /// <summary>
    /// Gets or sets the selected action for filtering.
    /// </summary>
    [ObservableProperty]
    private string? _selectedAction;

    /// <summary>
    /// Gets or sets whether a report is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasReport;

    // Report visibility flags
    [ObservableProperty]
    private bool _showAllActivityReport;

    [ObservableProperty]
    private bool _showUserActivityReport;

    [ObservableProperty]
    private bool _showTransactionLogReport;

    [ObservableProperty]
    private bool _showVoidRefundLogReport;

    [ObservableProperty]
    private bool _showPriceChangeLogReport;

    [ObservableProperty]
    private bool _showPermissionOverrideLogReport;

    // Report data
    [ObservableProperty]
    private AuditTrailReportResult? _auditTrailReport;

    [ObservableProperty]
    private ObservableCollection<AuditTrailItem> _auditTrailItems = [];

    [ObservableProperty]
    private UserActivityReportResult? _userActivityReport;

    [ObservableProperty]
    private ObservableCollection<UserActivityItem> _userActivityItems = [];

    [ObservableProperty]
    private TransactionLogReportResult? _transactionLogReport;

    [ObservableProperty]
    private ObservableCollection<TransactionLogItem> _transactionLogItems = [];

    [ObservableProperty]
    private VoidRefundLogReportResult? _voidRefundLogReport;

    [ObservableProperty]
    private ObservableCollection<VoidRefundLogItem> _voidRefundLogItems = [];

    [ObservableProperty]
    private PriceChangeLogReportResult? _priceChangeLogReport;

    [ObservableProperty]
    private ObservableCollection<PriceChangeLogItem> _priceChangeLogItems = [];

    [ObservableProperty]
    private PermissionOverrideLogReportResult? _permissionOverrideLogReport;

    [ObservableProperty]
    private ObservableCollection<PermissionOverrideLogItem> _permissionOverrideLogItems = [];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditReportsViewModel"/> class.
    /// </summary>
    public AuditReportsViewModel(
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

        Title = "Audit Trail Reports";
        SelectedReportType = ReportTypes.First();
    }

    #region INavigationAware

    /// <inheritdoc />
    public async Task OnNavigatedToAsync(object? parameter)
    {
        await LoadFiltersAsync();
    }

    /// <inheritdoc />
    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to generate the selected report.
    /// </summary>
    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (SelectedReportType is null)
            return;

        await ExecuteAsync(async () =>
        {
            ClearReportData();

            var parameters = new AuditReportParameters
            {
                FromDate = FromDate,
                ToDate = ToDate,
                UserId = SelectedUser?.Id,
                Action = SelectedAction
            };

            switch (SelectedReportType.ReportType)
            {
                case AuditReportType.AllActivity:
                    await GenerateAllActivityReportAsync(parameters);
                    break;
                case AuditReportType.UserActivity:
                    await GenerateUserActivityReportAsync(parameters);
                    break;
                case AuditReportType.TransactionLog:
                    await GenerateTransactionLogReportAsync(parameters);
                    break;
                case AuditReportType.VoidRefundLog:
                    await GenerateVoidRefundLogReportAsync(parameters);
                    break;
                case AuditReportType.PriceChangeLog:
                    await GeneratePriceChangeLogReportAsync(parameters);
                    break;
                case AuditReportType.PermissionOverrideLog:
                    await GeneratePermissionOverrideLogReportAsync(parameters);
                    break;
            }

            HasReport = true;
        }, "Generating report...");
    }

    /// <summary>
    /// Command to set the date range to today.
    /// </summary>
    [RelayCommand]
    private void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Command to set the date range to this week.
    /// </summary>
    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        FromDate = today.AddDays(-dayOfWeek);
        ToDate = today;
    }

    /// <summary>
    /// Command to set the date range to this month.
    /// </summary>
    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
    }

    /// <summary>
    /// Command to set the date range to last 30 days.
    /// </summary>
    [RelayCommand]
    private void SetLast30Days()
    {
        ToDate = DateTime.Today;
        FromDate = DateTime.Today.AddDays(-30);
    }

    /// <summary>
    /// Command to clear the user filter.
    /// </summary>
    [RelayCommand]
    private void ClearUserFilter()
    {
        SelectedUser = null;
    }

    /// <summary>
    /// Command to clear the action filter.
    /// </summary>
    [RelayCommand]
    private void ClearActionFilter()
    {
        SelectedAction = null;
    }

    /// <summary>
    /// Command to print the current report.
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
            // Build report content based on current report type
            var content = BuildPrintContent();
            await _reportPrintService.PrintReportAsync(content, $"Audit Report - {SelectedReportType?.DisplayName}");
        }, "Printing report...");
    }

    /// <summary>
    /// Command to export the report to CSV.
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
            dialogViewModel.Initialize(SelectedReportType.DisplayName, FromDate, ToDate);
            dialogViewModel.ExportAction = ExportCurrentAuditReportAsync;

            var dialog = new ExportDialog { DataContext = dialogViewModel };
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await DialogService.ShowMessageAsync(
                    "Export Complete",
                    $"Report exported successfully to:\n{dialogViewModel.FilePath}");

                Logger.Information("Audit report exported to {FilePath}", dialogViewModel.FilePath);
            }
        }, "Preparing export...");
    }

    private async Task ExportCurrentAuditReportAsync(string filePath, CancellationToken ct)
    {
        switch (SelectedReportType?.ReportType)
        {
            case AuditReportType.AllActivity:
                await _exportService.ExportToCsvAsync(AuditTrailItems.ToList(), filePath, ct);
                break;
            case AuditReportType.UserActivity:
                await _exportService.ExportToCsvAsync(UserActivityItems.ToList(), filePath, ct);
                break;
            case AuditReportType.TransactionLog:
                await _exportService.ExportToCsvAsync(TransactionLogItems.ToList(), filePath, ct);
                break;
            case AuditReportType.VoidRefundLog:
                await _exportService.ExportToCsvAsync(VoidRefundLogItems.ToList(), filePath, ct);
                break;
            case AuditReportType.PriceChangeLog:
                await _exportService.ExportToCsvAsync(PriceChangeLogItems.ToList(), filePath, ct);
                break;
            case AuditReportType.PermissionOverrideLog:
                await _exportService.ExportToCsvAsync(PermissionOverrideLogItems.ToList(), filePath, ct);
                break;
        }
    }

    /// <summary>
    /// Command to go back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedReportTypeChanged(AuditReportTypeItem? value)
    {
        // Clear current report when report type changes
        ClearReportData();
        HasReport = false;
    }

    #endregion

    #region Private Methods

    private async Task LoadFiltersAsync()
    {
        try
        {
            // Load users
            var users = await _userService.GetAllUsersAsync(includeInactive: false);
            Users = new ObservableCollection<User>(users);

            // Load available actions
            var actions = await _reportService.GetDistinctAuditActionsAsync();
            AvailableActions = new ObservableCollection<string>(actions);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load filters for audit reports");
        }
    }

    private void ClearReportData()
    {
        ShowAllActivityReport = false;
        ShowUserActivityReport = false;
        ShowTransactionLogReport = false;
        ShowVoidRefundLogReport = false;
        ShowPriceChangeLogReport = false;
        ShowPermissionOverrideLogReport = false;

        AuditTrailReport = null;
        AuditTrailItems = [];
        UserActivityReport = null;
        UserActivityItems = [];
        TransactionLogReport = null;
        TransactionLogItems = [];
        VoidRefundLogReport = null;
        VoidRefundLogItems = [];
        PriceChangeLogReport = null;
        PriceChangeLogItems = [];
        PermissionOverrideLogReport = null;
        PermissionOverrideLogItems = [];
    }

    private async Task GenerateAllActivityReportAsync(AuditReportParameters parameters)
    {
        AuditTrailReport = await _reportService.GenerateAuditTrailReportAsync(parameters);
        AuditTrailItems = new ObservableCollection<AuditTrailItem>(AuditTrailReport.Items);
        ShowAllActivityReport = true;
    }

    private async Task GenerateUserActivityReportAsync(AuditReportParameters parameters)
    {
        UserActivityReport = await _reportService.GenerateUserActivityReportAsync(parameters);
        UserActivityItems = new ObservableCollection<UserActivityItem>(UserActivityReport.Items);
        ShowUserActivityReport = true;
    }

    private async Task GenerateTransactionLogReportAsync(AuditReportParameters parameters)
    {
        TransactionLogReport = await _reportService.GenerateTransactionLogReportAsync(parameters);
        TransactionLogItems = new ObservableCollection<TransactionLogItem>(TransactionLogReport.Items);
        ShowTransactionLogReport = true;
    }

    private async Task GenerateVoidRefundLogReportAsync(AuditReportParameters parameters)
    {
        VoidRefundLogReport = await _reportService.GenerateVoidRefundLogReportAsync(parameters);
        VoidRefundLogItems = new ObservableCollection<VoidRefundLogItem>(VoidRefundLogReport.Items);
        ShowVoidRefundLogReport = true;
    }

    private async Task GeneratePriceChangeLogReportAsync(AuditReportParameters parameters)
    {
        PriceChangeLogReport = await _reportService.GeneratePriceChangeLogReportAsync(parameters);
        PriceChangeLogItems = new ObservableCollection<PriceChangeLogItem>(PriceChangeLogReport.Items);
        ShowPriceChangeLogReport = true;
    }

    private async Task GeneratePermissionOverrideLogReportAsync(AuditReportParameters parameters)
    {
        PermissionOverrideLogReport = await _reportService.GeneratePermissionOverrideLogReportAsync(parameters);
        PermissionOverrideLogItems = new ObservableCollection<PermissionOverrideLogItem>(PermissionOverrideLogReport.Items);
        ShowPermissionOverrideLogReport = true;
    }

    private string BuildPrintContent()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"AUDIT TRAIL REPORT - {SelectedReportType?.DisplayName}");
        sb.AppendLine($"Date Range: {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}");
        sb.AppendLine(new string('=', 48));
        sb.AppendLine();

        if (ShowAllActivityReport && AuditTrailReport != null)
        {
            sb.AppendLine($"Total Actions: {AuditTrailReport.TotalActions}");
            sb.AppendLine($"Unique Users: {AuditTrailReport.UniqueUsers}");
            sb.AppendLine();
            foreach (var item in AuditTrailItems.Take(100))
            {
                sb.AppendLine($"{item.Timestamp:HH:mm:ss} | {item.UserName} | {item.ActionDisplayName}");
            }
        }
        else if (ShowUserActivityReport && UserActivityReport != null)
        {
            sb.AppendLine($"Logins: {UserActivityReport.LoginCount}");
            sb.AppendLine($"Logouts: {UserActivityReport.LogoutCount}");
            sb.AppendLine($"Failed Logins: {UserActivityReport.FailedLoginCount}");
            sb.AppendLine();
            foreach (var item in UserActivityItems.Take(100))
            {
                sb.AppendLine($"{item.Timestamp:HH:mm:ss} | {item.UserName} | {item.ActionDisplayName}");
            }
        }
        else if (ShowVoidRefundLogReport && VoidRefundLogReport != null)
        {
            sb.AppendLine($"Total Voids: {VoidRefundLogReport.TotalVoids}");
            sb.AppendLine($"Total Value: {VoidRefundLogReport.TotalVoidValue:C}");
            sb.AppendLine();
            foreach (var item in VoidRefundLogItems.Take(100))
            {
                sb.AppendLine($"{item.Timestamp:HH:mm:ss} | {item.RequestedByUser} | {item.ReceiptNumber} | {item.VoidedAmount:C}");
            }
        }
        else if (ShowPriceChangeLogReport && PriceChangeLogReport != null)
        {
            sb.AppendLine($"Total Price Changes: {PriceChangeLogReport.TotalPriceChanges}");
            sb.AppendLine($"Products Affected: {PriceChangeLogReport.ProductsAffected}");
            sb.AppendLine();
            foreach (var item in PriceChangeLogItems.Take(100))
            {
                sb.AppendLine($"{item.Timestamp:HH:mm:ss} | {item.ProductName} | {item.OldPrice:C} -> {item.NewPrice:C}");
            }
        }
        else if (ShowPermissionOverrideLogReport && PermissionOverrideLogReport != null)
        {
            sb.AppendLine($"Total Overrides: {PermissionOverrideLogReport.TotalOverrides}");
            sb.AppendLine();
            foreach (var item in PermissionOverrideLogItems.Take(100))
            {
                sb.AppendLine($"{item.Timestamp:HH:mm:ss} | {item.RequestedByUser} -> {item.AuthorizedByUser} | {item.PermissionDisplayName}");
            }
        }

        return sb.ToString();
    }

    private string BuildCsvContent()
    {
        var sb = new System.Text.StringBuilder();

        if (ShowAllActivityReport)
        {
            sb.AppendLine("Timestamp,User,Action,EntityType,EntityId,MachineName");
            foreach (var item in AuditTrailItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.UserName}\",\"{item.Action}\",\"{item.EntityType}\",\"{item.EntityId}\",\"{item.MachineName}\"");
            }
        }
        else if (ShowUserActivityReport)
        {
            sb.AppendLine("Timestamp,User,Action,MachineName,IPAddress");
            foreach (var item in UserActivityItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.UserName}\",\"{item.Action}\",\"{item.MachineName}\",\"{item.IpAddress}\"");
            }
        }
        else if (ShowTransactionLogReport)
        {
            sb.AppendLine("Timestamp,User,Action,EntityType,EntityId,Amount");
            foreach (var item in TransactionLogItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.UserName}\",\"{item.Action}\",\"{item.EntityType}\",\"{item.EntityId}\",\"{item.Amount}\"");
            }
        }
        else if (ShowVoidRefundLogReport)
        {
            sb.AppendLine("Timestamp,RequestedBy,AuthorizedBy,ReceiptNumber,Reason,Amount");
            foreach (var item in VoidRefundLogItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.RequestedByUser}\",\"{item.AuthorizedByUser}\",\"{item.ReceiptNumber}\",\"{item.VoidReason}\",\"{item.VoidedAmount}\"");
            }
        }
        else if (ShowPriceChangeLogReport)
        {
            sb.AppendLine("Timestamp,User,ProductCode,ProductName,OldPrice,NewPrice,Difference,ChangePercent");
            foreach (var item in PriceChangeLogItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.UserName}\",\"{item.ProductCode}\",\"{item.ProductName}\",\"{item.OldPrice}\",\"{item.NewPrice}\",\"{item.PriceDifference}\",\"{item.ChangePercentage}%\"");
            }
        }
        else if (ShowPermissionOverrideLogReport)
        {
            sb.AppendLine("Timestamp,RequestedBy,AuthorizedBy,Permission,Reason,EntityReference");
            foreach (var item in PermissionOverrideLogItems)
            {
                sb.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{item.RequestedByUser}\",\"{item.AuthorizedByUser}\",\"{item.Permission}\",\"{item.Reason}\",\"{item.EntityReference}\"");
            }
        }

        return sb.ToString();
    }

    #endregion
}

/// <summary>
/// Represents an audit report type item for display.
/// </summary>
public class AuditReportTypeItem
{
    /// <summary>
    /// Gets the report type.
    /// </summary>
    public AuditReportType ReportType { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditReportTypeItem"/> class.
    /// </summary>
    public AuditReportTypeItem(AuditReportType reportType, string displayName, string description)
    {
        ReportType = reportType;
        DisplayName = displayName;
        Description = description;
    }
}
