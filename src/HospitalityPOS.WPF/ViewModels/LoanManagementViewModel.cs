using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Loan Management - handles employee loan applications, approvals, repayments, and statements.
/// </summary>
public partial class LoanManagementViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<LoanApplication> _loanApplications = new();

    [ObservableProperty]
    private ObservableCollection<LoanApplication> _pendingApplications = new();

    [ObservableProperty]
    private ObservableCollection<ActiveLoan> _activeLoans = new();

    [ObservableProperty]
    private ObservableCollection<LoanRepayment> _repaymentHistory = new();

    [ObservableProperty]
    private LoanApplication? _selectedApplication;

    [ObservableProperty]
    private ActiveLoan? _selectedLoan;

    [ObservableProperty]
    private LoanSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private decimal _totalDisbursed;

    [ObservableProperty]
    private decimal _totalOutstanding;

    [ObservableProperty]
    private int _pendingApplicationsCount;

    [ObservableProperty]
    private int _activeLoansCount;

    // Application Form
    [ObservableProperty]
    private bool _isApplicationFormOpen;

    [ObservableProperty]
    private LoanApplicationRequest _newApplication = new();

    #endregion

    public LoanManagementViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Loan Management";
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
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            // Load pending applications
            var pending = await loanService.GetPendingApplicationsAsync();
            PendingApplications = new ObservableCollection<LoanApplication>(pending);
            PendingApplicationsCount = pending.Count;

            // Load active loans
            var active = await loanService.GetActiveLoansAsync();
            ActiveLoans = new ObservableCollection<ActiveLoan>(active);
            ActiveLoansCount = active.Count;

            // Calculate totals
            TotalDisbursed = active.Sum(l => l.PrincipalAmount);
            TotalOutstanding = active.Sum(l => l.OutstandingBalance);

            // Load settings
            Settings = await loanService.GetSettingsAsync();

            IsLoading = false;
        }, "Loading loan data...");
    }

    [RelayCommand]
    private void OpenApplicationForm()
    {
        NewApplication = new LoanApplicationRequest
        {
            ApplicationDate = DateOnly.FromDateTime(DateTime.Today)
        };
        IsApplicationFormOpen = true;
    }

    [RelayCommand]
    private async Task SubmitApplicationAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            var result = await loanService.SubmitApplicationAsync(NewApplication);

            if (result.Success)
            {
                IsApplicationFormOpen = false;
                await DialogService.ShowMessageAsync("Success", "Loan application submitted successfully.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Submitting loan application...");
    }

    [RelayCommand]
    private void CancelApplication()
    {
        IsApplicationFormOpen = false;
    }

    [RelayCommand]
    private async Task ApproveApplicationAsync(LoanApplication? application)
    {
        if (application is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Approve Loan Application",
            $"Approve loan of KSh {application.RequestedAmount:N0} for {application.EmployeeName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            await loanService.ApproveApplicationAsync(application.Id, SessionService.CurrentUserId);
            await LoadDataAsync();
        }, "Approving application...");
    }

    [RelayCommand]
    private async Task RejectApplicationAsync(LoanApplication? application)
    {
        if (application is null) return;

        var reason = await DialogService.ShowInputAsync(
            "Reject Loan Application",
            "Please provide a reason for rejection:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            await loanService.RejectApplicationAsync(application.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Rejecting application...");
    }

    [RelayCommand]
    private async Task DisburseAsync(LoanApplication? application)
    {
        if (application is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Disburse Loan",
            $"Disburse loan of KSh {application.ApprovedAmount:N0} to {application.EmployeeName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            await loanService.DisburseAsync(application.Id, SessionService.CurrentUserId);
            await DialogService.ShowMessageAsync("Success", "Loan disbursed successfully.");
            await LoadDataAsync();
        }, "Disbursing loan...");
    }

    [RelayCommand]
    private async Task ViewRepaymentScheduleAsync(ActiveLoan? loan)
    {
        if (loan is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            var schedule = await loanService.GetRepaymentScheduleAsync(loan.Id);
            RepaymentHistory = new ObservableCollection<LoanRepayment>(schedule);
            SelectedLoan = loan;
        }, "Loading repayment schedule...");
    }

    [RelayCommand]
    private async Task RecordRepaymentAsync(ActiveLoan? loan)
    {
        if (loan is null) return;

        var amountStr = await DialogService.ShowInputAsync(
            "Record Repayment",
            "Enter repayment amount:");

        if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
        {
            ErrorMessage = "Invalid amount entered";
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            await loanService.RecordRepaymentAsync(loan.Id, amount, SessionService.CurrentUserId);
            await DialogService.ShowMessageAsync("Success", $"Repayment of KSh {amount:N0} recorded.");
            await LoadDataAsync();
        }, "Recording repayment...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider.GetService<ILoanService>();

            if (loanService is null)
            {
                ErrorMessage = "Loan service not available";
                return;
            }

            await loanService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "Loan settings saved successfully.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class LoanApplication
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public int RequestedTermMonths { get; set; }
    public int? ApprovedTermMonths { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateOnly ApplicationDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoanApplicationRequest
{
    public int EmployeeId { get; set; }
    public decimal RequestedAmount { get; set; }
    public int RequestedTermMonths { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public DateOnly ApplicationDate { get; set; }
}

public class ActiveLoan
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalRepaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateOnly DisbursementDate { get; set; }
    public DateOnly MaturityDate { get; set; }
    public string Status { get; set; } = "Active";
}

public class LoanRepayment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public string Status { get; set; } = "Pending";
}

public class LoanSettings
{
    public decimal MaxLoanAmount { get; set; } = 100000m;
    public decimal DefaultInterestRate { get; set; } = 10m;
    public int MaxTermMonths { get; set; } = 24;
    public int MinEmploymentMonths { get; set; } = 6;
    public decimal MaxDebtToIncomeRatio { get; set; } = 0.33m;
    public bool RequireManagerApproval { get; set; } = true;
    public bool AllowMultipleLoans { get; set; } = false;
}
