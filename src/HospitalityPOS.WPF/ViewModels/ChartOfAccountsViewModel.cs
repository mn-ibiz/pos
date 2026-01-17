using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing chart of accounts.
/// </summary>
public partial class ChartOfAccountsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<ChartOfAccount> _accounts = [];

    [ObservableProperty]
    private ObservableCollection<ChartOfAccount> _filteredAccounts = [];

    [ObservableProperty]
    private ChartOfAccount? _selectedAccount;

    [ObservableProperty]
    private AccountType? _selectedAccountType;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editAccountCode = string.Empty;

    [ObservableProperty]
    private string _editAccountName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private AccountType _editAccountType = AccountType.Asset;

    [ObservableProperty]
    private int? _editParentAccountId;

    [ObservableProperty]
    private bool _editIsActive = true;

    public ObservableCollection<AccountType> AccountTypes { get; } =
        new(Enum.GetValues<AccountType>());

    public ObservableCollection<ChartOfAccount> ParentAccounts { get; } = [];

    public ChartOfAccountsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadAccountsAsync();
    }

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var accounts = await accountingService.GetAllAccountsAsync();
            Accounts = new ObservableCollection<ChartOfAccount>(accounts);

            // Update parent accounts list
            ParentAccounts.Clear();
            foreach (var account in accounts.Where(a => a.IsActive))
            {
                ParentAccounts.Add(account);
            }

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeedDefaultAccountsAsync()
    {
        var result = await _dialogService.ShowConfirmationAsync(
            "Seed Default Accounts",
            "This will create default chart of accounts. Existing accounts will not be affected. Continue?");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            await accountingService.SeedDefaultAccountsAsync();
            await LoadAccountsAsync();

            await _dialogService.ShowMessageAsync("Success", "Default accounts have been created.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to seed accounts: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NewAccount()
    {
        SelectedAccount = null;
        EditAccountCode = string.Empty;
        EditAccountName = string.Empty;
        EditDescription = string.Empty;
        EditAccountType = AccountType.Asset;
        EditParentAccountId = null;
        EditIsActive = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditAccount()
    {
        if (SelectedAccount == null) return;

        EditAccountCode = SelectedAccount.AccountCode;
        EditAccountName = SelectedAccount.AccountName;
        EditDescription = SelectedAccount.Description ?? string.Empty;
        EditAccountType = SelectedAccount.AccountType;
        EditParentAccountId = SelectedAccount.ParentAccountId;
        EditIsActive = SelectedAccount.IsActive;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(EditAccountCode) || string.IsNullOrWhiteSpace(EditAccountName))
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Account code and name are required.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            if (SelectedAccount == null)
            {
                // Create new account
                var newAccount = new ChartOfAccount
                {
                    AccountCode = EditAccountCode,
                    AccountName = EditAccountName,
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription,
                    AccountType = EditAccountType,
                    ParentAccountId = EditParentAccountId,
                    IsActive = EditIsActive
                };

                await accountingService.CreateAccountAsync(newAccount);
            }
            else
            {
                // Update existing account
                SelectedAccount.AccountCode = EditAccountCode;
                SelectedAccount.AccountName = EditAccountName;
                SelectedAccount.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription;
                SelectedAccount.AccountType = EditAccountType;
                SelectedAccount.ParentAccountId = EditParentAccountId;
                SelectedAccount.IsActive = EditIsActive;

                await accountingService.UpdateAccountAsync(SelectedAccount);
            }

            IsEditing = false;
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to save account: {ex.Message}");
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
    private async Task DeleteAccountAsync()
    {
        if (SelectedAccount == null) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "Delete Account",
            $"Are you sure you want to delete account '{SelectedAccount.AccountName}'? This cannot be undone.");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            var deleted = await accountingService.DeleteAccountAsync(SelectedAccount.Id);
            if (deleted)
            {
                await LoadAccountsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Cannot delete account. It may have journal entries or child accounts.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to delete account: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedAccountTypeChanged(AccountType? value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = Accounts.AsEnumerable();

        if (SelectedAccountType.HasValue)
        {
            filtered = filtered.Where(a => a.AccountType == SelectedAccountType.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(a =>
                a.AccountCode.ToLower().Contains(search) ||
                a.AccountName.ToLower().Contains(search) ||
                (a.Description?.ToLower().Contains(search) ?? false));
        }

        FilteredAccounts = new ObservableCollection<ChartOfAccount>(
            filtered.OrderBy(a => a.AccountCode));
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedAccountType = null;
        SearchText = string.Empty;
    }
}
