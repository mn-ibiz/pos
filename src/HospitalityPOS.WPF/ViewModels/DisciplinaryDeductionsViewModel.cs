using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Disciplinary Deductions - handles deduction records, approval workflow, and payroll integration.
/// </summary>
public partial class DisciplinaryDeductionsViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<DisciplinaryDeduction> _deductions = new();

    [ObservableProperty]
    private ObservableCollection<DisciplinaryDeduction> _pendingApprovals = new();

    [ObservableProperty]
    private ObservableCollection<DeductionType> _deductionTypes = new();

    [ObservableProperty]
    private DisciplinaryDeduction? _selectedDeduction;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

    [ObservableProperty]
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    // Summary stats
    [ObservableProperty]
    private decimal _totalDeductions;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _approvedCount;

    [ObservableProperty]
    private int _appliedToPayrollCount;

    // Deduction Editor
    [ObservableProperty]
    private bool _isDeductionEditorOpen;

    [ObservableProperty]
    private DisciplinaryDeductionRequest _editingDeduction = new();

    [ObservableProperty]
    private bool _isNewDeduction;

    #endregion

    public DisciplinaryDeductionsViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Disciplinary Deductions";
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
            var deductionService = scope.ServiceProvider.GetService<IDisciplinaryDeductionService>();

            if (deductionService is null)
            {
                ErrorMessage = "Disciplinary Deduction service not available";
                return;
            }

            // Load deduction types
            var types = await deductionService.GetDeductionTypesAsync();
            DeductionTypes = new ObservableCollection<DeductionType>(types);

            // Load all deductions for date range
            var deductions = await deductionService.GetDeductionsAsync(StartDate, EndDate);
            Deductions = new ObservableCollection<DisciplinaryDeduction>(deductions);

            // Load pending approvals
            var pending = await deductionService.GetPendingApprovalsAsync();
            PendingApprovals = new ObservableCollection<DisciplinaryDeduction>(pending);
            PendingCount = pending.Count;

            // Calculate stats
            TotalDeductions = deductions.Sum(d => d.Amount);
            ApprovedCount = deductions.Count(d => d.Status == "Approved");
            AppliedToPayrollCount = deductions.Count(d => d.AppliedToPayroll);

            IsLoading = false;
        }, "Loading deductions data...");
    }

    [RelayCommand]
    private void CreateDeduction()
    {
        EditingDeduction = new DisciplinaryDeductionRequest
        {
            IncidentDate = DateOnly.FromDateTime(DateTime.Today),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
        };
        IsNewDeduction = true;
        IsDeductionEditorOpen = true;
    }

    [RelayCommand]
    private void EditDeduction(DisciplinaryDeduction? deduction)
    {
        if (deduction is null) return;

        if (deduction.Status != "Pending")
        {
            _ = DialogService.ShowWarningAsync("Cannot Edit", "Only pending deductions can be edited.");
            return;
        }

        EditingDeduction = new DisciplinaryDeductionRequest
        {
            Id = deduction.Id,
            EmployeeId = deduction.EmployeeId,
            DeductionTypeId = deduction.DeductionTypeId,
            IncidentDate = deduction.IncidentDate,
            Description = deduction.Description,
            Amount = deduction.Amount,
            EffectiveDate = deduction.EffectiveDate,
            Notes = deduction.Notes
        };
        IsNewDeduction = false;
        IsDeductionEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveDeductionAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var deductionService = scope.ServiceProvider.GetService<IDisciplinaryDeductionService>();

            if (deductionService is null)
            {
                ErrorMessage = "Disciplinary Deduction service not available";
                return;
            }

            EditingDeduction.CreatedByUserId = SessionService.CurrentUserId;

            if (IsNewDeduction)
            {
                await deductionService.CreateDeductionAsync(EditingDeduction);
            }
            else
            {
                await deductionService.UpdateDeductionAsync(EditingDeduction);
            }

            IsDeductionEditorOpen = false;
            await LoadDataAsync();
        }, "Saving deduction...");
    }

    [RelayCommand]
    private void CancelEditDeduction()
    {
        IsDeductionEditorOpen = false;
    }

    [RelayCommand]
    private async Task ApproveAsync(DisciplinaryDeduction? deduction)
    {
        if (deduction is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Deduction",
            $"Approve deduction of KSh {deduction.Amount:N0} for {deduction.EmployeeName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var deductionService = scope.ServiceProvider.GetService<IDisciplinaryDeductionService>();

            if (deductionService is null)
            {
                ErrorMessage = "Disciplinary Deduction service not available";
                return;
            }

            await deductionService.ApproveAsync(deduction.Id, SessionService.CurrentUserId);
            await LoadDataAsync();
        }, "Approving deduction...");
    }

    [RelayCommand]
    private async Task RejectAsync(DisciplinaryDeduction? deduction)
    {
        if (deduction is null) return;

        var reason = await DialogService.ShowInputAsync("Reject Deduction", "Please provide a reason:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var deductionService = scope.ServiceProvider.GetService<IDisciplinaryDeductionService>();

            if (deductionService is null)
            {
                ErrorMessage = "Disciplinary Deduction service not available";
                return;
            }

            await deductionService.RejectAsync(deduction.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Rejecting deduction...");
    }

    [RelayCommand]
    private async Task VoidAsync(DisciplinaryDeduction? deduction)
    {
        if (deduction is null) return;

        var reason = await DialogService.ShowInputAsync("Void Deduction", "Please provide a reason:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var deductionService = scope.ServiceProvider.GetService<IDisciplinaryDeductionService>();

            if (deductionService is null)
            {
                ErrorMessage = "Disciplinary Deduction service not available";
                return;
            }

            await deductionService.VoidAsync(deduction.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Voiding deduction...");
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(DisciplinaryDeduction? deduction)
    {
        if (deduction is null) return;

        await DialogService.ShowMessageAsync(
            "Deduction History",
            $"Employee: {deduction.EmployeeName}\n" +
            $"Type: {deduction.DeductionTypeName}\n" +
            $"Amount: KSh {deduction.Amount:N0}\n" +
            $"Status: {deduction.Status}\n" +
            $"Incident Date: {deduction.IncidentDate:d}\n" +
            $"Created: {deduction.CreatedAt:g}");
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Deduction report export functionality will be available soon.");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnStartDateChanged(DateOnly value) => _ = LoadDataAsync();
    partial void OnEndDateChanged(DateOnly value) => _ = LoadDataAsync();
}

// DTOs
public class DisciplinaryDeduction
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DeductionTypeId { get; set; }
    public string DeductionTypeName { get; set; } = string.Empty;
    public DateOnly IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Voided
    public bool AppliedToPayroll { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class DisciplinaryDeductionRequest
{
    public int? Id { get; set; }
    public int EmployeeId { get; set; }
    public int DeductionTypeId { get; set; }
    public DateOnly IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
}

public class DeductionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultAmount { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
