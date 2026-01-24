using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Bank Reconciliation - handles bank accounts, statement imports, transaction matching, and reconciliation.
/// </summary>
public partial class BankReconciliationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<BankAccount> _bankAccounts = new();

    [ObservableProperty]
    private ObservableCollection<BankTransaction> _bankTransactions = new();

    [ObservableProperty]
    private ObservableCollection<BankTransaction> _unmatchedTransactions = new();

    [ObservableProperty]
    private ObservableCollection<ReconciliationSession> _reconciliationHistory = new();

    [ObservableProperty]
    private BankAccount? _selectedBankAccount;

    [ObservableProperty]
    private BankTransaction? _selectedTransaction;

    [ObservableProperty]
    private ReconciliationSession? _currentSession;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private decimal _totalBankBalance;

    [ObservableProperty]
    private decimal _totalBookBalance;

    [ObservableProperty]
    private int _unmatchedCount;

    [ObservableProperty]
    private decimal _reconciliationDifference;

    // Bank Account Editor
    [ObservableProperty]
    private bool _isBankAccountEditorOpen;

    [ObservableProperty]
    private BankAccountRequest _editingBankAccount = new();

    [ObservableProperty]
    private bool _isNewBankAccount;

    // Import Dialog
    [ObservableProperty]
    private bool _isImportDialogOpen;

    [ObservableProperty]
    private string _importFilePath = string.Empty;

    [ObservableProperty]
    private string _importFormat = "CSV";

    #endregion

    public List<string> ImportFormats { get; } = new() { "CSV", "Excel", "OFX" };

    public BankReconciliationViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Bank Reconciliation";
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
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            // Load bank accounts
            var accounts = await bankService.GetBankAccountsAsync();
            BankAccounts = new ObservableCollection<BankAccount>(accounts);
            TotalBankBalance = accounts.Sum(a => a.CurrentBalance);

            // Load unmatched transactions
            var unmatched = await bankService.GetUnmatchedTransactionsAsync();
            UnmatchedTransactions = new ObservableCollection<BankTransaction>(unmatched);
            UnmatchedCount = unmatched.Count;

            // Load reconciliation history
            var history = await bankService.GetReconciliationHistoryAsync();
            ReconciliationHistory = new ObservableCollection<ReconciliationSession>(history);

            IsLoading = false;
        }, "Loading bank reconciliation data...");
    }

    [RelayCommand]
    private void CreateBankAccount()
    {
        EditingBankAccount = new BankAccountRequest();
        IsNewBankAccount = true;
        IsBankAccountEditorOpen = true;
    }

    [RelayCommand]
    private void EditBankAccount(BankAccount? account)
    {
        if (account is null) return;

        EditingBankAccount = new BankAccountRequest
        {
            Id = account.Id,
            AccountName = account.AccountName,
            AccountNumber = account.AccountNumber,
            BankName = account.BankName,
            BranchCode = account.BranchCode,
            AccountType = account.AccountType,
            Currency = account.Currency,
            GLAccountId = account.GLAccountId
        };
        IsNewBankAccount = false;
        IsBankAccountEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveBankAccountAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            if (IsNewBankAccount)
            {
                await bankService.CreateBankAccountAsync(EditingBankAccount);
            }
            else
            {
                await bankService.UpdateBankAccountAsync(EditingBankAccount);
            }

            IsBankAccountEditorOpen = false;
            await LoadDataAsync();
        }, "Saving bank account...");
    }

    [RelayCommand]
    private void CancelEditBankAccount()
    {
        IsBankAccountEditorOpen = false;
    }

    [RelayCommand]
    private void OpenImportDialog()
    {
        if (SelectedBankAccount is null)
        {
            _ = DialogService.ShowWarningAsync("Select Account", "Please select a bank account first.");
            return;
        }

        ImportFilePath = string.Empty;
        ImportFormat = "CSV";
        IsImportDialogOpen = true;
    }

    [RelayCommand]
    private async Task ImportStatementAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath))
        {
            ErrorMessage = "Please select a file to import";
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            var result = await bankService.ImportStatementAsync(
                SelectedBankAccount!.Id,
                ImportFilePath,
                ImportFormat);

            if (result.Success)
            {
                IsImportDialogOpen = false;
                await DialogService.ShowMessageAsync(
                    "Import Complete",
                    $"Imported {result.TransactionsImported} transactions.\n{result.DuplicatesSkipped} duplicates skipped.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Importing bank statement...");
    }

    [RelayCommand]
    private void CancelImport()
    {
        IsImportDialogOpen = false;
    }

    [RelayCommand]
    private async Task LoadBankTransactionsAsync(BankAccount? account)
    {
        if (account is null) return;

        SelectedBankAccount = account;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            var startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var transactions = await bankService.GetBankTransactionsAsync(account.Id, startDate, endDate);
            BankTransactions = new ObservableCollection<BankTransaction>(transactions);
        }, "Loading transactions...");
    }

    [RelayCommand]
    private async Task AutoMatchAsync()
    {
        if (SelectedBankAccount is null)
        {
            await DialogService.ShowWarningAsync("Select Account", "Please select a bank account first.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            var result = await bankService.AutoMatchTransactionsAsync(SelectedBankAccount.Id);
            await DialogService.ShowMessageAsync(
                "Auto-Match Complete",
                $"Matched {result.MatchedCount} transactions automatically.");
            await LoadDataAsync();
        }, "Auto-matching transactions...");
    }

    [RelayCommand]
    private async Task MatchTransactionAsync(BankTransaction? transaction)
    {
        if (transaction is null) return;

        // This would open a dialog to select a journal entry to match
        await DialogService.ShowMessageAsync("Match Transaction",
            "Manual matching dialog will be implemented.\nThis allows selecting a journal entry to match with the bank transaction.");
    }

    [RelayCommand]
    private async Task StartReconciliationAsync()
    {
        if (SelectedBankAccount is null)
        {
            await DialogService.ShowWarningAsync("Select Account", "Please select a bank account first.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            CurrentSession = await bankService.StartReconciliationSessionAsync(SelectedBankAccount.Id);
            ReconciliationDifference = CurrentSession.BankBalance - CurrentSession.BookBalance;
            await DialogService.ShowMessageAsync("Reconciliation Started",
                $"Reconciliation session started for {SelectedBankAccount.AccountName}.\nBank Balance: KSh {CurrentSession.BankBalance:N0}\nBook Balance: KSh {CurrentSession.BookBalance:N0}");
        }, "Starting reconciliation...");
    }

    [RelayCommand]
    private async Task CompleteReconciliationAsync()
    {
        if (CurrentSession is null)
        {
            await DialogService.ShowWarningAsync("No Session", "No active reconciliation session.");
            return;
        }

        if (Math.Abs(ReconciliationDifference) > 0.01m)
        {
            var confirmed = await DialogService.ShowConfirmationAsync(
                "Unreconciled Difference",
                $"There is an unreconciled difference of KSh {ReconciliationDifference:N2}.\nDo you want to complete anyway?");
            if (!confirmed) return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var bankService = scope.ServiceProvider.GetService<IBankReconciliationService>();

            if (bankService is null)
            {
                ErrorMessage = "Bank Reconciliation service not available";
                return;
            }

            await bankService.CompleteReconciliationAsync(CurrentSession.Id, SessionService.CurrentUserId);
            CurrentSession = null;
            await DialogService.ShowMessageAsync("Success", "Reconciliation completed successfully.");
            await LoadDataAsync();
        }, "Completing reconciliation...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class BankAccount
{
    public int Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public string AccountType { get; set; } = "Current";
    public string Currency { get; set; } = "KES";
    public decimal CurrentBalance { get; set; }
    public int? GLAccountId { get; set; }
    public DateOnly? LastReconciledDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BankAccountRequest
{
    public int? Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public string AccountType { get; set; } = "Current";
    public string Currency { get; set; } = "KES";
    public int? GLAccountId { get; set; }
}

public class BankTransaction
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Debit, Credit
    public bool IsMatched { get; set; }
    public int? MatchedJournalEntryId { get; set; }
    public bool IsReconciled { get; set; }
}

public class ReconciliationSession
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public string BankAccountName { get; set; } = string.Empty;
    public DateOnly StatementDate { get; set; }
    public decimal BankBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal Difference => BankBalance - BookBalance;
    public string Status { get; set; } = "In Progress";
    public int? CompletedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TransactionsImported { get; set; }
    public int DuplicatesSkipped { get; set; }
}

public class MatchResult
{
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
}
