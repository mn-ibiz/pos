using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Employee Termination - handles termination process, final settlement, and exit documentation.
/// </summary>
public partial class EmployeeTerminationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Termination> _terminations = new();

    [ObservableProperty]
    private ObservableCollection<Termination> _pendingApprovals = new();

    [ObservableProperty]
    private Termination? _selectedTermination;

    [ObservableProperty]
    private FinalSettlement? _currentSettlement;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Summary stats
    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _processedThisMonth;

    [ObservableProperty]
    private decimal _totalSettlementsPending;

    // Termination Form
    [ObservableProperty]
    private bool _isTerminationFormOpen;

    [ObservableProperty]
    private TerminationRequest _editingTermination = new();

    // Settlement Calculator
    [ObservableProperty]
    private bool _isSettlementCalculatorOpen;

    #endregion

    public List<string> TerminationReasons { get; } = new()
    {
        "Resignation", "Retirement", "End of Contract", "Redundancy",
        "Misconduct", "Poor Performance", "Mutual Agreement", "Death", "Other"
    };

    public EmployeeTerminationViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Employee Termination";
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
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            // Load all terminations
            var terminations = await terminationService.GetTerminationsAsync();
            Terminations = new ObservableCollection<Termination>(terminations);

            // Load pending approvals
            var pending = await terminationService.GetPendingApprovalsAsync();
            PendingApprovals = new ObservableCollection<Termination>(pending);
            PendingCount = pending.Count;

            // Calculate stats
            var startOfMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            ProcessedThisMonth = terminations.Count(t => t.EffectiveDate >= startOfMonth && t.Status == "Completed");
            TotalSettlementsPending = pending.Sum(t => t.EstimatedSettlement);

            IsLoading = false;
        }, "Loading termination data...");
    }

    [RelayCommand]
    private void InitiateTermination()
    {
        EditingTermination = new TerminationRequest
        {
            NoticeDate = DateOnly.FromDateTime(DateTime.Today),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        };
        IsTerminationFormOpen = true;
    }

    [RelayCommand]
    private async Task SaveTerminationAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            EditingTermination.InitiatedByUserId = SessionService.CurrentUserId;
            var result = await terminationService.InitiateTerminationAsync(EditingTermination);

            if (result.Success)
            {
                IsTerminationFormOpen = false;
                await DialogService.ShowMessageAsync("Success", "Termination initiated. Settlement calculation is pending.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Initiating termination...");
    }

    [RelayCommand]
    private void CancelTermination()
    {
        IsTerminationFormOpen = false;
    }

    [RelayCommand]
    private async Task CalculateSettlementAsync(Termination? termination)
    {
        if (termination is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            CurrentSettlement = await terminationService.CalculateFinalSettlementAsync(termination.Id);
            SelectedTermination = termination;
            IsSettlementCalculatorOpen = true;
        }, "Calculating settlement...");
    }

    [RelayCommand]
    private void CloseSettlementCalculator()
    {
        IsSettlementCalculatorOpen = false;
    }

    [RelayCommand]
    private async Task ApproveAsync(Termination? termination)
    {
        if (termination is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Termination",
            $"Approve termination for {termination.EmployeeName}?\nEffective Date: {termination.EffectiveDate:d}");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            await terminationService.ApproveAsync(termination.Id, SessionService.CurrentUserId);
            await LoadDataAsync();
        }, "Approving termination...");
    }

    [RelayCommand]
    private async Task RejectAsync(Termination? termination)
    {
        if (termination is null) return;

        var reason = await DialogService.ShowInputAsync("Reject Termination", "Please provide a reason:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            await terminationService.RejectAsync(termination.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Rejecting termination...");
    }

    [RelayCommand]
    private async Task ProcessSettlementAsync(Termination? termination)
    {
        if (termination is null || CurrentSettlement is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Process Final Settlement",
            $"Process final settlement of KSh {CurrentSettlement.NetSettlement:N0} for {termination.EmployeeName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            await terminationService.ProcessSettlementAsync(termination.Id, SessionService.CurrentUserId);
            IsSettlementCalculatorOpen = false;
            await DialogService.ShowMessageAsync("Success", "Final settlement processed.");
            await LoadDataAsync();
        }, "Processing settlement...");
    }

    [RelayCommand]
    private async Task GenerateDocumentsAsync(Termination? termination)
    {
        if (termination is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var terminationService = scope.ServiceProvider.GetService<ITerminationService>();

            if (terminationService is null)
            {
                ErrorMessage = "Termination service not available";
                return;
            }

            await terminationService.GenerateExitDocumentsAsync(termination.Id);
            await DialogService.ShowMessageAsync("Success",
                "Exit documents generated:\n- Termination Letter\n- Final Settlement Statement\n- Certificate of Service\n- P9 Tax Form");
        }, "Generating documents...");
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(Termination? termination)
    {
        if (termination is null) return;

        await DialogService.ShowMessageAsync(
            $"Termination: {termination.EmployeeName}",
            $"Employee: {termination.EmployeeName}\n" +
            $"Department: {termination.Department}\n" +
            $"Reason: {termination.Reason}\n" +
            $"Notice Date: {termination.NoticeDate:d}\n" +
            $"Effective Date: {termination.EffectiveDate:d}\n" +
            $"Status: {termination.Status}\n" +
            $"Settlement: KSh {termination.EstimatedSettlement:N0}");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class Termination
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReasonDetails { get; set; }
    public DateOnly NoticeDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? LastWorkingDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Processing, Completed
    public decimal EstimatedSettlement { get; set; }
    public bool IsSettlementProcessed { get; set; }
    public int InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TerminationRequest
{
    public int EmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReasonDetails { get; set; }
    public DateOnly NoticeDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? LastWorkingDate { get; set; }
    public int InitiatedByUserId { get; set; }
}

public class TerminationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Termination? Termination { get; set; }
}

public class FinalSettlement
{
    public int TerminationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    // Earnings
    public decimal FinalSalary { get; set; }
    public decimal ProRataLeave { get; set; }
    public decimal Gratuity { get; set; }
    public decimal Severance { get; set; }
    public decimal PendingCommissions { get; set; }
    public decimal OtherEarnings { get; set; }
    public decimal GrossSettlement => FinalSalary + ProRataLeave + Gratuity + Severance + PendingCommissions + OtherEarnings;

    // Deductions
    public decimal TaxDeduction { get; set; }
    public decimal NSSFContribution { get; set; }
    public decimal NHIFContribution { get; set; }
    public decimal LoanBalance { get; set; }
    public decimal AdvanceBalance { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions => TaxDeduction + NSSFContribution + NHIFContribution + LoanBalance + AdvanceBalance + OtherDeductions;

    public decimal NetSettlement => GrossSettlement - TotalDeductions;

    // Breakdown
    public int YearsOfService { get; set; }
    public int MonthsOfService { get; set; }
    public decimal DailyRate { get; set; }
    public decimal LeaveDaysBalance { get; set; }
}
