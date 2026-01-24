using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Supplier Credit Management - handles supplier credit terms, aging, and payment tracking.
/// </summary>
public partial class SupplierCreditViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<SupplierCreditAccount> _supplierAccounts = new();

    [ObservableProperty]
    private ObservableCollection<SupplierTransaction> _transactions = new();

    [ObservableProperty]
    private ObservableCollection<SupplierAgingBucket> _agingReport = new();

    [ObservableProperty]
    private SupplierCreditAccount? _selectedSupplier;

    [ObservableProperty]
    private SupplierTransaction? _selectedTransaction;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Summary stats
    [ObservableProperty]
    private decimal _totalPayables;

    [ObservableProperty]
    private decimal _currentDue;

    [ObservableProperty]
    private decimal _overduTotal;

    [ObservableProperty]
    private int _suppliersCount;

    // Credit Terms Editor
    [ObservableProperty]
    private bool _isCreditTermsEditorOpen;

    [ObservableProperty]
    private SupplierCreditTermsRequest _editingTerms = new();

    // Payment Dialog
    [ObservableProperty]
    private bool _isPaymentDialogOpen;

    [ObservableProperty]
    private SupplierPaymentRequest _paymentRequest = new();

    #endregion

    public SupplierCreditViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Supplier Credit Management";
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
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            // Load supplier accounts
            var accounts = await supplierCreditService.GetSupplierAccountsAsync();
            SupplierAccounts = new ObservableCollection<SupplierCreditAccount>(accounts);
            SuppliersCount = accounts.Count;
            TotalPayables = accounts.Sum(a => a.OutstandingBalance);

            // Load aging report
            var aging = await supplierCreditService.GetAgingReportAsync();
            AgingReport = new ObservableCollection<SupplierAgingBucket>(aging.Buckets);
            CurrentDue = aging.Current;
            OverduTotal = aging.Days1To30 + aging.Days31To60 + aging.Days61To90 + aging.Over90Days;

            IsLoading = false;
        }, "Loading supplier credit data...");
    }

    [RelayCommand]
    private void EditCreditTerms(SupplierCreditAccount? supplier)
    {
        if (supplier is null) return;

        EditingTerms = new SupplierCreditTermsRequest
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            CreditLimit = supplier.CreditLimit,
            PaymentTermsDays = supplier.PaymentTermsDays,
            DiscountPercent = supplier.EarlyPaymentDiscountPercent,
            DiscountDays = supplier.DiscountDays
        };
        IsCreditTermsEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveCreditTermsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            await supplierCreditService.UpdateCreditTermsAsync(EditingTerms);
            IsCreditTermsEditorOpen = false;
            await LoadDataAsync();
        }, "Saving credit terms...");
    }

    [RelayCommand]
    private void CancelEditTerms()
    {
        IsCreditTermsEditorOpen = false;
    }

    [RelayCommand]
    private async Task ViewSupplierTransactionsAsync(SupplierCreditAccount? supplier)
    {
        if (supplier is null) return;

        SelectedSupplier = supplier;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            var transactions = await supplierCreditService.GetSupplierTransactionsAsync(supplier.SupplierId);
            Transactions = new ObservableCollection<SupplierTransaction>(transactions);
            SelectedTabIndex = 1; // Switch to transactions tab
        }, "Loading transactions...");
    }

    [RelayCommand]
    private void OpenPaymentDialog(SupplierCreditAccount? supplier)
    {
        if (supplier is null) return;

        PaymentRequest = new SupplierPaymentRequest
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            OutstandingBalance = supplier.OutstandingBalance,
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
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            var result = await supplierCreditService.RecordPaymentAsync(PaymentRequest);

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
    private async Task CheckCreditAsync(SupplierCreditAccount? supplier)
    {
        if (supplier is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            var credit = await supplierCreditService.GetAvailableCreditAsync(supplier.SupplierId);
            await DialogService.ShowMessageAsync(
                "Credit Status",
                $"Supplier: {supplier.SupplierName}\n\nCredit Limit: KSh {supplier.CreditLimit:N0}\nUsed: KSh {supplier.OutstandingBalance:N0}\nAvailable: KSh {credit:N0}");
        }, "Checking credit...");
    }

    [RelayCommand]
    private async Task ToggleHoldAsync(SupplierCreditAccount? supplier)
    {
        if (supplier is null) return;

        var action = supplier.IsOnHold ? "release" : "place on hold";
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Confirm Action",
            $"Are you sure you want to {action} supplier '{supplier.SupplierName}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var supplierCreditService = scope.ServiceProvider.GetService<ISupplierCreditService>();

            if (supplierCreditService is null)
            {
                ErrorMessage = "Supplier Credit service not available";
                return;
            }

            if (supplier.IsOnHold)
            {
                await supplierCreditService.ReleaseHoldAsync(supplier.SupplierId);
            }
            else
            {
                await supplierCreditService.PlaceOnHoldAsync(supplier.SupplierId, "Credit limit concerns");
            }

            await LoadDataAsync();
        }, supplier.IsOnHold ? "Releasing hold..." : "Placing on hold...");
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
public class SupplierCreditAccount
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AvailableCredit => CreditLimit - OutstandingBalance;
    public int PaymentTermsDays { get; set; } = 30;
    public decimal? EarlyPaymentDiscountPercent { get; set; }
    public int? DiscountDays { get; set; }
    public DateOnly? LastPaymentDate { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public bool IsOnHold { get; set; }
    public string Status => IsOnHold ? "On Hold" : OutstandingBalance > 0 ? "Active" : "Clear";
}

public class SupplierCreditTermsRequest
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int PaymentTermsDays { get; set; }
    public decimal? DiscountPercent { get; set; }
    public int? DiscountDays { get; set; }
}

public class SupplierTransaction
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Invoice, Payment, Credit Note
    public string Reference { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
}

public class SupplierAgingBucket
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90Days;
}

public class SupplierAgingReport
{
    public decimal TotalPayables { get; set; }
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public List<SupplierAgingBucket> Buckets { get; set; } = new();
}

public class SupplierPaymentRequest
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
    public int? BankAccountId { get; set; }
    public bool AutoAllocate { get; set; } = true;
}
