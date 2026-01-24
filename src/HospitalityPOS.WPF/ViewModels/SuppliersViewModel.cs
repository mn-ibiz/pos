using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the suppliers management view.
/// </summary>
public partial class SuppliersViewModel : ViewModelBase, INavigationAware
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierCreditService _supplierCreditService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = [];

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showInactiveSuppliers;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalSupplierCount;

    [ObservableProperty]
    private int _activeSupplierCount;

    [ObservableProperty]
    private decimal _totalOutstandingBalance;

    [ObservableProperty]
    private int _overdueInvoiceCount;

    private IReadOnlyList<Supplier> _allSuppliers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SuppliersViewModel"/> class.
    /// </summary>
    public SuppliersViewModel(
        ISupplierService supplierService,
        ISupplierCreditService supplierCreditService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _supplierCreditService = supplierCreditService ?? throw new ArgumentNullException(nameof(supplierCreditService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Supplier Management";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadSuppliersAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnShowInactiveSuppliersChanged(bool value)
    {
        _ = LoadSuppliersAsync();
    }

    /// <summary>
    /// Loads all suppliers from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                _allSuppliers = await _supplierService.GetAllSuppliersAsync(ShowInactiveSuppliers).ConfigureAwait(true);
                TotalSupplierCount = await _supplierService.GetSupplierCountAsync(true).ConfigureAwait(true);
                ActiveSupplierCount = await _supplierService.GetSupplierCountAsync(false).ConfigureAwait(true);

                // Calculate total outstanding balance
                var suppliersWithBalance = await _supplierService.GetSuppliersWithBalanceAsync().ConfigureAwait(true);
                TotalOutstandingBalance = suppliersWithBalance.Sum(s => s.CurrentBalance);

                // Get overdue invoice count
                var overdueInvoices = await _supplierCreditService.GetOverdueInvoicesAsync().ConfigureAwait(true);
                OverdueInvoiceCount = overdueInvoices.Count;

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading suppliers...").ConfigureAwait(true);
    }

    private void ApplyFilter()
    {
        var filtered = _allSuppliers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s =>
                s.Code.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                (s.ContactPerson?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Email?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Phone?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Suppliers = new ObservableCollection<Supplier>(filtered);
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    [RelayCommand]
    private async Task CreateSupplierAsync()
    {
        var result = await _dialogService.ShowSupplierEditorDialogAsync(null).ConfigureAwait(true);

        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var supplier = new Supplier
                {
                    Code = result.Code,
                    Name = result.Name,
                    ContactPerson = result.ContactPerson,
                    Phone = result.Phone,
                    Email = result.Email,
                    Address = result.Address,
                    City = result.City,
                    Country = result.Country,
                    TaxId = result.TaxId,
                    BankAccount = result.BankAccount,
                    BankName = result.BankName,
                    PaymentTermDays = result.PaymentTermDays,
                    CreditLimit = result.CreditLimit,
                    Notes = result.Notes,
                    IsActive = result.IsActive
                };

                await _supplierService.CreateSupplierAsync(supplier).ConfigureAwait(true);
                await LoadSuppliersAsync().ConfigureAwait(true);

                await _dialogService.ShowMessageAsync("Success", $"Supplier '{supplier.Name}' created successfully.").ConfigureAwait(true);
            }, "Creating supplier...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Edits the selected supplier.
    /// </summary>
    [RelayCommand]
    private async Task EditSupplierAsync()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        var result = await _dialogService.ShowSupplierEditorDialogAsync(SelectedSupplier).ConfigureAwait(true);

        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                SelectedSupplier.Code = result.Code;
                SelectedSupplier.Name = result.Name;
                SelectedSupplier.ContactPerson = result.ContactPerson;
                SelectedSupplier.Phone = result.Phone;
                SelectedSupplier.Email = result.Email;
                SelectedSupplier.Address = result.Address;
                SelectedSupplier.City = result.City;
                SelectedSupplier.Country = result.Country;
                SelectedSupplier.TaxId = result.TaxId;
                SelectedSupplier.BankAccount = result.BankAccount;
                SelectedSupplier.BankName = result.BankName;
                SelectedSupplier.PaymentTermDays = result.PaymentTermDays;
                SelectedSupplier.CreditLimit = result.CreditLimit;
                SelectedSupplier.Notes = result.Notes;
                SelectedSupplier.IsActive = result.IsActive;

                await _supplierService.UpdateSupplierAsync(SelectedSupplier).ConfigureAwait(true);
                await LoadSuppliersAsync().ConfigureAwait(true);

                await _dialogService.ShowMessageAsync("Success", $"Supplier '{result.Name}' updated successfully.").ConfigureAwait(true);
            }, "Updating supplier...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Toggles the active status of the selected supplier.
    /// </summary>
    [RelayCommand]
    private async Task ToggleActiveStatusAsync()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        var action = SelectedSupplier.IsActive ? "deactivate" : "activate";
        var confirm = await _dialogService.ShowConfirmationAsync(
            $"{(SelectedSupplier.IsActive ? "Deactivate" : "Activate")} Supplier",
            $"Are you sure you want to {action} supplier '{SelectedSupplier.Name}'?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            bool success;

            if (SelectedSupplier.IsActive)
            {
                success = await _supplierService.DeactivateSupplierAsync(SelectedSupplier.Id).ConfigureAwait(true);
            }
            else
            {
                success = await _supplierService.ActivateSupplierAsync(SelectedSupplier.Id).ConfigureAwait(true);
            }

            if (success)
            {
                await LoadSuppliersAsync().ConfigureAwait(true);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to {action} supplier.").ConfigureAwait(true);
            }
        }, $"{(SelectedSupplier.IsActive ? "Deactivating" : "Activating")} supplier...").ConfigureAwait(true);
    }

    /// <summary>
    /// Views purchase orders for the selected supplier.
    /// </summary>
    [RelayCommand]
    private void ViewPurchaseOrders()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        _navigationService.NavigateTo<PurchaseOrdersViewModel>(SelectedSupplier.Id);
    }

    /// <summary>
    /// Views invoices for the selected supplier.
    /// </summary>
    [RelayCommand]
    private void ViewInvoices()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        _navigationService.NavigateTo<SupplierInvoicesViewModel>(SelectedSupplier.Id);
    }

    /// <summary>
    /// Opens the payment dialog for the selected supplier.
    /// </summary>
    [RelayCommand]
    private async Task MakePaymentAsync()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        var result = await _dialogService.ShowSupplierPaymentDialogAsync(SelectedSupplier).ConfigureAwait(true);

        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                await _supplierCreditService.RecordPaymentAsync(result).ConfigureAwait(true);
                await LoadSuppliersAsync().ConfigureAwait(true);
                await _dialogService.ShowMessageAsync("Success", $"Payment of KSh {result.Amount:N2} recorded successfully.").ConfigureAwait(true);
            }, "Recording payment...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Views the statement for the selected supplier.
    /// </summary>
    [RelayCommand]
    private async Task ViewStatementAsync()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        await _dialogService.ShowSupplierStatementDialogAsync(SelectedSupplier);
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Gets the status display text for a supplier.
    /// </summary>
    public static string GetStatusDisplay(Supplier? supplier)
    {
        if (supplier is null)
        {
            return string.Empty;
        }

        return supplier.IsActive ? "Active" : "Inactive";
    }

    /// <summary>
    /// Gets the balance display text for a supplier.
    /// </summary>
    public static string GetBalanceDisplay(Supplier? supplier)
    {
        if (supplier is null)
        {
            return string.Empty;
        }

        return supplier.CurrentBalance > 0
            ? $"KSh {supplier.CurrentBalance:N2}"
            : "-";
    }
}
