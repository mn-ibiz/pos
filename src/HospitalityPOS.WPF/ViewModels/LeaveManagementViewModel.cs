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
/// ViewModel for Leave Management - handles leave types, requests, approvals, balances, and reporting.
/// </summary>
public partial class LeaveManagementViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<LeaveType> _leaveTypes = new();

    [ObservableProperty]
    private ObservableCollection<LeaveRequest> _pendingRequests = new();

    [ObservableProperty]
    private ObservableCollection<LeaveRequest> _allRequests = new();

    [ObservableProperty]
    private ObservableCollection<EmployeeLeaveBalance> _employeeBalances = new();

    [ObservableProperty]
    private LeaveType? _selectedLeaveType;

    [ObservableProperty]
    private LeaveRequest? _selectedRequest;

    [ObservableProperty]
    private EmployeeLeaveBalance? _selectedBalance;

    [ObservableProperty]
    private LeaveSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private int _pendingRequestsCount;

    [ObservableProperty]
    private int _approvedThisMonth;

    [ObservableProperty]
    private int _onLeaveToday;

    [ObservableProperty]
    private int _activeLeaveTypes;

    // Leave Type Editor
    [ObservableProperty]
    private bool _isLeaveTypeEditorOpen;

    [ObservableProperty]
    private LeaveTypeRequest _editingLeaveType = new();

    [ObservableProperty]
    private bool _isNewLeaveType;

    // Leave Request Form
    [ObservableProperty]
    private bool _isRequestFormOpen;

    [ObservableProperty]
    private LeaveRequestSubmission _newRequest = new();

    #endregion

    public List<int> AvailableYears { get; } = Enumerable.Range(DateTime.Today.Year - 2, 5).ToList();

    public CommissionManagementViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Leave Management";
        _selectedYear = DateTime.Today.Year;
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
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            // Load leave types
            var types = await leaveService.GetActiveLeaveTypesAsync();
            LeaveTypes = new ObservableCollection<LeaveType>(types);
            ActiveLeaveTypes = types.Count;

            // Load pending requests
            var pending = await leaveService.GetPendingRequestsAsync();
            PendingRequests = new ObservableCollection<LeaveRequest>(pending);
            PendingRequestsCount = pending.Count;

            // Load approved requests for current month
            var startOfMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var approved = await leaveService.GetApprovedRequestsAsync(startOfMonth, endOfMonth);
            ApprovedThisMonth = approved.Count;

            // Count employees on leave today
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayLeaves = await leaveService.GetApprovedRequestsAsync(today, today);
            OnLeaveToday = todayLeaves.Count;

            // Load settings
            Settings = await leaveService.GetSettingsAsync();

            IsLoading = false;
        }, "Loading leave data...");
    }

    [RelayCommand]
    private void CreateLeaveType()
    {
        EditingLeaveType = new LeaveTypeRequest();
        IsNewLeaveType = true;
        IsLeaveTypeEditorOpen = true;
    }

    [RelayCommand]
    private void EditLeaveType(LeaveType? leaveType)
    {
        if (leaveType is null) return;

        EditingLeaveType = new LeaveTypeRequest
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            Description = leaveType.Description,
            DefaultDays = leaveType.DefaultDays,
            IsPaid = leaveType.IsPaid,
            RequiresApproval = leaveType.RequiresApproval,
            AllowCarryOver = leaveType.AllowCarryOver,
            MaxCarryOverDays = leaveType.MaxCarryOverDays
        };
        IsNewLeaveType = false;
        IsLeaveTypeEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveLeaveTypeAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            if (IsNewLeaveType)
            {
                await leaveService.CreateLeaveTypeAsync(EditingLeaveType);
            }
            else
            {
                await leaveService.UpdateLeaveTypeAsync(EditingLeaveType);
            }

            IsLeaveTypeEditorOpen = false;
            await LoadDataAsync();
        }, "Saving leave type...");
    }

    [RelayCommand]
    private void CancelEditLeaveType()
    {
        IsLeaveTypeEditorOpen = false;
    }

    [RelayCommand]
    private async Task DeactivateLeaveTypeAsync(LeaveType? leaveType)
    {
        if (leaveType is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Deactivate Leave Type",
            $"Are you sure you want to deactivate '{leaveType.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            await leaveService.DeactivateLeaveTypeAsync(leaveType.Id);
            await LoadDataAsync();
        }, "Deactivating leave type...");
    }

    [RelayCommand]
    private void OpenRequestForm()
    {
        NewRequest = new LeaveRequestSubmission
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        IsRequestFormOpen = true;
    }

    [RelayCommand]
    private async Task SubmitRequestAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            var result = await leaveService.SubmitRequestAsync(NewRequest);

            if (result.Success)
            {
                IsRequestFormOpen = false;
                await DialogService.ShowMessageAsync("Success", "Leave request submitted successfully.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Submitting leave request...");
    }

    [RelayCommand]
    private void CancelRequest()
    {
        IsRequestFormOpen = false;
    }

    [RelayCommand]
    private async Task ApproveRequestAsync(LeaveRequest? request)
    {
        if (request is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Leave Request",
            $"Approve leave request for {request.EmployeeName}?\n{request.StartDate:d} - {request.EndDate:d}");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            var approvalRequest = new LeaveApprovalRequest
            {
                RequestId = request.Id,
                IsApproved = true,
                ApprovedByUserId = SessionService.CurrentUserId
            };

            await leaveService.ProcessApprovalAsync(approvalRequest);
            await LoadDataAsync();
        }, "Approving request...");
    }

    [RelayCommand]
    private async Task RejectRequestAsync(LeaveRequest? request)
    {
        if (request is null) return;

        var reason = await DialogService.ShowInputAsync(
            "Reject Leave Request",
            "Please provide a reason for rejection:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            var approvalRequest = new LeaveApprovalRequest
            {
                RequestId = request.Id,
                IsApproved = false,
                ApprovedByUserId = SessionService.CurrentUserId,
                RejectionReason = reason
            };

            await leaveService.ProcessApprovalAsync(approvalRequest);
            await LoadDataAsync();
        }, "Rejecting request...");
    }

    [RelayCommand]
    private async Task InitializeYearAllocationsAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Initialize Leave Allocations",
            $"Initialize leave allocations for year {SelectedYear}?\nThis will create allocations for all employees based on leave type defaults.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            var count = await leaveService.InitializeYearAllocationsAsync(SelectedYear);
            await DialogService.ShowMessageAsync("Success", $"Created {count} leave allocations for {SelectedYear}.");
            await LoadDataAsync();
        }, "Initializing allocations...");
    }

    [RelayCommand]
    private async Task ProcessCarryOverAsync()
    {
        var fromYear = SelectedYear - 1;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Process Carry Over",
            $"Carry over unused leave balances from {fromYear} to {SelectedYear}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            var count = await leaveService.ProcessCarryOverAsync(fromYear, SelectedYear);
            await DialogService.ShowMessageAsync("Success", $"Processed {count} carry-overs from {fromYear} to {SelectedYear}.");
            await LoadDataAsync();
        }, "Processing carry-overs...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var leaveService = scope.ServiceProvider.GetService<ILeaveService>();

            if (leaveService is null)
            {
                ErrorMessage = "Leave service not available";
                return;
            }

            await leaveService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "Leave settings saved successfully.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs that might be missing
public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public bool AllowCarryOver { get; set; }
    public decimal? MaxCarryOverDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LeaveTypeRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public bool AllowCarryOver { get; set; }
    public decimal? MaxCarryOverDays { get; set; }
}

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

public class LeaveRequestSubmission
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
}

public class LeaveApprovalRequest
{
    public int RequestId { get; set; }
    public bool IsApproved { get; set; }
    public int ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
}

public class LeaveSettings
{
    public bool RequireManagerApproval { get; set; } = true;
    public int MinimumNoticeDays { get; set; } = 3;
    public bool AllowNegativeBalance { get; set; } = false;
    public bool SendEmailNotifications { get; set; } = true;
    public int LeaveYearStartMonth { get; set; } = 1;
}

public class EmployeeLeaveBalance
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<LeaveTypeBalance> Balances { get; set; } = new();
}

public class LeaveTypeBalance
{
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal Allocated { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }
    public decimal Available => Allocated - Used - Pending;
}
