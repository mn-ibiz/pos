using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Accounts Receivable - handles customer credit accounts, aging reports, statements, and payments.
/// </summary>
public partial class AccountsReceivableViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<CustomerAccount> _customerAccounts = new();

    [ObservableProperty]
    private ObservableCollection<ARTransaction> _transactions = new();

    [ObservableProperty]
    private ObservableCollection<AgingBucket> _agingReport = new();

    [ObservableProperty]
    private CustomerAccount? _selectedCustomer;

    [ObservableProperty]
    private ARTransaction? _selectedTransaction;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Summary stats
    [ObservableProperty]
    private decimal _totalReceivables;

    [ObservableProperty]
    private decimal _currentDue;

    [ObservableProperty]
    private decimal _overdue30;

    [ObservableProperty]
    private decimal _overdue60;

    [ObservableProperty]
    private decimal _overdue90Plus;

    // Customer Editor
    [ObservableProperty]
    private bool _isCustomerEditorOpen;

    [ObservableProperty]
    private CustomerAccountRequest _editingCustomer = new();

    [ObservableProperty]
    private bool _isNewCustomer;

    // Payment Dialog
    [ObservableProperty]
    private bool _isPaymentDialogOpen;

    [ObservableProperty]
    private PaymentAllocationRequest _paymentRequest = new();

    #endregion

    public AccountsReceivableViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Accounts Receivable";
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
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            // Load customer accounts
            var accounts = await arService.GetCustomerAccountsAsync();
            CustomerAccounts = new ObservableCollection<CustomerAccount>(accounts);

            // Load aging report
            var aging = await arService.GetAgingReportAsync();
            AgingReport = new ObservableCollection<AgingBucket>(aging.Buckets);

            // Set summary totals
            TotalReceivables = aging.TotalReceivables;
            CurrentDue = aging.Current;
            Overdue30 = aging.Days1To30;
            Overdue60 = aging.Days31To60;
            Overdue90Plus = aging.Days61To90 + aging.Over90Days;

            IsLoading = false;
        }, "Loading accounts receivable data...");
    }

    [RelayCommand]
    private void CreateCustomerAccount()
    {
        EditingCustomer = new CustomerAccountRequest
        {
            CreditLimit = 50000m
        };
        IsNewCustomer = true;
        IsCustomerEditorOpen = true;
    }

    [RelayCommand]
    private void EditCustomerAccount(CustomerAccount? customer)
    {
        if (customer is null) return;

        EditingCustomer = new CustomerAccountRequest
        {
            Id = customer.Id,
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            CreditLimit = customer.CreditLimit,
            PaymentTerms = customer.PaymentTerms,
            ContactEmail = customer.ContactEmail,
            ContactPhone = customer.ContactPhone
        };
        IsNewCustomer = false;
        IsCustomerEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveCustomerAccountAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            if (IsNewCustomer)
            {
                await arService.CreateCustomerAccountAsync(EditingCustomer);
            }
            else
            {
                await arService.UpdateCustomerAccountAsync(EditingCustomer);
            }

            IsCustomerEditorOpen = false;
            await LoadDataAsync();
        }, "Saving customer account...");
    }

    [RelayCommand]
    private void CancelEditCustomer()
    {
        IsCustomerEditorOpen = false;
    }

    [RelayCommand]
    private async Task ViewCustomerTransactionsAsync(CustomerAccount? customer)
    {
        if (customer is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            var transactions = await arService.GetCustomerTransactionsAsync(customer.Id);
            Transactions = new ObservableCollection<ARTransaction>(transactions);
            SelectedCustomer = customer;
            SelectedTabIndex = 1; // Switch to transactions tab
        }, "Loading transactions...");
    }

    [RelayCommand]
    private void OpenPaymentDialog(CustomerAccount? customer)
    {
        if (customer is null) return;

        PaymentRequest = new PaymentAllocationRequest
        {
            CustomerAccountId = customer.Id,
            CustomerName = customer.CustomerName,
            OutstandingBalance = customer.OutstandingBalance,
            PaymentDate = DateOnly.FromDateTime(DateTime.Today)
        };
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            var result = await arService.RecordPaymentAsync(PaymentRequest);

            if (result.Success)
            {
                IsPaymentDialogOpen = false;
                await DialogService.ShowMessageAsync("Success", $"Payment of KSh {PaymentRequest.Amount:N0} recorded.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Recording payment...");
    }

    [RelayCommand]
    private void CancelPayment()
    {
        IsPaymentDialogOpen = false;
    }

    [RelayCommand]
    private async Task GenerateStatementAsync(CustomerAccount? customer)
    {
        if (customer is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            var statement = await arService.GenerateStatementAsync(customer.Id);
            await DialogService.ShowMessageAsync(
                "Statement Generated",
                $"Statement for {customer.CustomerName}\nPeriod: {statement.PeriodStart:d} - {statement.PeriodEnd:d}\nTotal Due: KSh {statement.TotalDue:N0}");
        }, "Generating statement...");
    }

    [RelayCommand]
    private async Task SendStatementAsync(CustomerAccount? customer)
    {
        if (customer is null) return;

        if (string.IsNullOrEmpty(customer.ContactEmail))
        {
            await DialogService.ShowWarningAsync("No Email", "Customer does not have an email address configured.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var arService = scope.ServiceProvider.GetService<IAccountsReceivableService>();

            if (arService is null)
            {
                ErrorMessage = "Accounts Receivable service not available";
                return;
            }

            await arService.SendStatementAsync(customer.Id);
            await DialogService.ShowMessageAsync("Success", $"Statement sent to {customer.ContactEmail}");
        }, "Sending statement...");
    }

    [RelayCommand]
    private async Task ExportAgingReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Aging report export functionality will be available soon.");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class CustomerAccount
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AvailableCredit => CreditLimit - OutstandingBalance;
    public int PaymentTerms { get; set; } = 30; // Days
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateOnly? LastPaymentDate { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public bool IsOnHold { get; set; }
    public string Status => IsOnHold ? "On Hold" : OutstandingBalance > 0 ? "Active" : "Clear";
}

public class CustomerAccountRequest
{
    public int? Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int PaymentTerms { get; set; } = 30;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ARTransaction
{
    public int Id { get; set; }
    public int CustomerAccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Invoice, Payment, Credit
    public string Reference { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
}

public class AgingBucket
{
    public int CustomerAccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90Days;
}

public class AgingReport
{
    public decimal TotalReceivables { get; set; }
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public List<AgingBucket> Buckets { get; set; } = new();
}

public class PaymentAllocationRequest
{
    public int CustomerAccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
    public bool AutoAllocate { get; set; } = true; // FIFO allocation
}

public class ARStatement
{
    public int CustomerAccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDue { get; set; }
    public List<ARTransaction> Transactions { get; set; } = new();
}
