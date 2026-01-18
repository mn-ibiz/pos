using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing journal entries.
/// </summary>
public partial class JournalEntriesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<JournalEntry> _journalEntries = [];

    [ObservableProperty]
    private JournalEntry? _selectedEntry;

    [ObservableProperty]
    private ObservableCollection<JournalEntryLine> _entryLines = [];

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editEntryNumber = string.Empty;

    [ObservableProperty]
    private DateTime _editEntryDate = DateTime.Today;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editReference = string.Empty;

    [ObservableProperty]
    private ObservableCollection<JournalLineEdit> _editLines = [];

    [ObservableProperty]
    private decimal _totalDebits;

    [ObservableProperty]
    private decimal _totalCredits;

    [ObservableProperty]
    private bool _isBalanced;

    [ObservableProperty]
    private ObservableCollection<ChartOfAccount> _accounts = [];

    [ObservableProperty]
    private ObservableCollection<AccountingPeriod> _periods = [];

    [ObservableProperty]
    private AccountingPeriod? _currentPeriod;

    public JournalEntriesViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _sessionService = sessionService;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            // Load accounts for dropdown
            var accounts = await accountingService.GetAllAccountsAsync();
            Accounts = new ObservableCollection<ChartOfAccount>(accounts.Where(a => a.IsActive));

            // Load periods
            var periods = await accountingService.GetAllPeriodsAsync();
            Periods = new ObservableCollection<AccountingPeriod>(periods);

            // Get current period
            CurrentPeriod = await accountingService.GetCurrentPeriodAsync();

            // Load journal entries
            await LoadEntriesAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var entries = await accountingService.GetJournalEntriesByDateRangeAsync(StartDate, EndDate);
            JournalEntries = new ObservableCollection<JournalEntry>(
                entries.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.EntryNumber));
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedEntryChanged(JournalEntry? value)
    {
        if (value != null)
        {
            EntryLines = new ObservableCollection<JournalEntryLine>(value.JournalEntryLines);
        }
        else
        {
            EntryLines.Clear();
        }
    }

    [RelayCommand]
    private async Task NewEntryAsync()
    {
        if (CurrentPeriod == null)
        {
            await _dialogService.ShowErrorAsync("Error", "No open accounting period. Please create one first.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

        SelectedEntry = null;
        EditEntryNumber = await accountingService.GenerateEntryNumberAsync();
        EditEntryDate = DateTime.Today;
        EditDescription = string.Empty;
        EditReference = string.Empty;
        EditLines = new ObservableCollection<JournalLineEdit>
        {
            new() { IsDebit = true },
            new() { IsDebit = false }
        };
        UpdateTotals();
        IsEditing = true;
    }

    [RelayCommand]
    private void AddLine()
    {
        EditLines.Add(new JournalLineEdit());
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveLine(JournalLineEdit line)
    {
        if (EditLines.Count > 2)
        {
            EditLines.Remove(line);
            UpdateTotals();
        }
    }

    public void UpdateTotals()
    {
        TotalDebits = EditLines.Where(l => l.IsDebit).Sum(l => l.Amount);
        TotalCredits = EditLines.Where(l => !l.IsDebit).Sum(l => l.Amount);
        IsBalanced = Math.Abs(TotalDebits - TotalCredits) < 0.01m;
    }

    [RelayCommand]
    private async Task SaveEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditDescription))
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Description is required.");
            return;
        }

        if (!IsBalanced)
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Journal entry must be balanced (debits = credits).");
            return;
        }

        if (EditLines.Any(l => l.AccountId == 0))
        {
            await _dialogService.ShowErrorAsync("Validation Error", "All lines must have an account selected.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var entry = new JournalEntry
            {
                EntryNumber = EditEntryNumber,
                EntryDate = EditEntryDate,
                Description = EditDescription,
                ReferenceType = string.IsNullOrWhiteSpace(EditReference) ? null : EditReference,
                AccountingPeriodId = CurrentPeriod!.Id,
                CreatedByUserId = _sessionService.CurrentUser?.Id ?? 1,
                JournalEntryLines = EditLines.Select(l => new JournalEntryLine
                {
                    AccountId = l.AccountId,
                    Description = l.Description,
                    DebitAmount = l.IsDebit ? l.Amount : 0,
                    CreditAmount = l.IsDebit ? 0 : l.Amount
                }).ToList()
            };

            await accountingService.CreateJournalEntryAsync(entry);
            IsEditing = false;
            await LoadEntriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to save journal entry: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task PostEntryAsync()
    {
        if (SelectedEntry == null) return;

        if (SelectedEntry.IsPosted)
        {
            await _dialogService.ShowErrorAsync("Error", "Entry is already posted.");
            return;
        }

        var result = await _dialogService.ShowConfirmationAsync(
            "Post Entry",
            $"Are you sure you want to post entry '{SelectedEntry.EntryNumber}'? Posted entries cannot be edited.");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            await accountingService.PostJournalEntryAsync(SelectedEntry.Id);
            await LoadEntriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to post entry: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ReverseEntryAsync()
    {
        if (SelectedEntry == null) return;

        if (!SelectedEntry.IsPosted)
        {
            await _dialogService.ShowErrorAsync("Error", "Only posted entries can be reversed.");
            return;
        }

        var reason = await _dialogService.ShowInputAsync("Reverse Entry", "Enter reason for reversal:");
        if (string.IsNullOrWhiteSpace(reason)) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            await accountingService.ReverseJournalEntryAsync(SelectedEntry.Id, reason);
            await LoadEntriesAsync();

            await _dialogService.ShowMessageAsync("Success", "Reversal entry has been created and posted.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to reverse entry: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreatePeriodAsync()
    {
        // Simple period creation - in production, this would be a dialog
        var name = await _dialogService.ShowInputAsync("Create Period", "Enter period name (e.g., 'January 2025'):");
        if (string.IsNullOrWhiteSpace(name)) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            await accountingService.CreatePeriodAsync(name, startDate, endDate);
            await LoadDataAsync();

            await _dialogService.ShowMessageAsync("Success", "Accounting period created.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to create period: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClosePeriodAsync()
    {
        if (CurrentPeriod == null) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "Close Period",
            $"Are you sure you want to close period '{CurrentPeriod.PeriodName}'? No more entries can be made to this period.");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var userId = _sessionService.CurrentUser?.Id ?? 1;
            await accountingService.ClosePeriodAsync(CurrentPeriod.Id, userId);
            await LoadDataAsync();

            await _dialogService.ShowMessageAsync("Success", "Accounting period closed.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to close period: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Helper class for editing journal entry lines.
/// </summary>
public partial class JournalLineEdit : ObservableObject
{
    [ObservableProperty]
    private int _accountId;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private bool _isDebit = true;
}
