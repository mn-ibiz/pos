using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the supplier invoices view.
/// </summary>
public partial class SupplierInvoicesViewModel : ViewModelBase, INavigationAware
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierCreditService _supplierCreditService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private Supplier? _supplier;

    [ObservableProperty]
    private ObservableCollection<SupplierInvoice> _invoices = [];

    [ObservableProperty]
    private SupplierInvoice? _selectedInvoice;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showPaidInvoices;

    [ObservableProperty]
    private decimal _totalOutstanding;

    [ObservableProperty]
    private int _overdueCount;

    [ObservableProperty]
    private decimal _overdueAmount;

    private int _supplierId;

    public SupplierInvoicesViewModel(
        ISupplierService supplierService,
        ISupplierCreditService supplierCreditService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService,
        ILogger logger) : base(logger)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _supplierCreditService = supplierCreditService ?? throw new ArgumentNullException(nameof(supplierCreditService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        Title = "Supplier Invoices";
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int supplierId)
        {
            _supplierId = supplierId;
            _ = LoadDataAsync();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    partial void OnShowPaidInvoicesChanged(bool value)
    {
        _ = LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                Supplier = await _supplierService.GetSupplierByIdAsync(_supplierId).ConfigureAwait(true);
                if (Supplier != null)
                {
                    Title = $"Invoices - {Supplier.Name}";
                }
                await LoadInvoicesAsync().ConfigureAwait(true);
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading invoices...").ConfigureAwait(true);
    }

    private async Task LoadInvoicesAsync()
    {
        var invoices = await _supplierCreditService.GetSupplierInvoicesAsync(_supplierId, ShowPaidInvoices).ConfigureAwait(true);
        Invoices = new ObservableCollection<SupplierInvoice>(invoices);

        // Calculate summary
        var outstandingInvoices = invoices.Where(i => i.Status != InvoiceStatus.Paid).ToList();
        TotalOutstanding = outstandingInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

        var overdueInvoices = outstandingInvoices.Where(i => i.DueDate < DateTime.UtcNow.Date).ToList();
        OverdueCount = overdueInvoices.Count;
        OverdueAmount = overdueInvoices.Sum(i => i.TotalAmount - i.PaidAmount);
    }

    [RelayCommand]
    private async Task CreateInvoiceAsync()
    {
        if (Supplier is null) return;

        var result = await _dialogService.ShowSupplierInvoiceEditorDialogAsync(null, Supplier).ConfigureAwait(true);

        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                await _supplierCreditService.CreateInvoiceAsync(result).ConfigureAwait(true);
                await LoadInvoicesAsync().ConfigureAwait(true);
                await _dialogService.ShowMessageAsync("Success", "Invoice created successfully.").ConfigureAwait(true);
            }, "Creating invoice...").ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task MakePaymentAsync()
    {
        if (Supplier is null) return;

        var result = await _dialogService.ShowSupplierPaymentDialogAsync(Supplier, SelectedInvoice).ConfigureAwait(true);

        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                await _supplierCreditService.RecordPaymentAsync(result).ConfigureAwait(true);
                await LoadInvoicesAsync().ConfigureAwait(true);
                await _dialogService.ShowMessageAsync("Success", $"Payment of KSh {result.Amount:N2} recorded successfully.").ConfigureAwait(true);
            }, "Recording payment...").ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ViewPaymentsAsync()
    {
        if (SelectedInvoice is null) return;

        var payments = await _supplierCreditService.GetInvoicePaymentsAsync(SelectedInvoice.Id).ConfigureAwait(true);

        if (payments.Count == 0)
        {
            await _dialogService.ShowMessageAsync("No Payments", "No payments have been made for this invoice.").ConfigureAwait(true);
            return;
        }

        var message = string.Join("\n", payments.Select(p =>
            $"{p.PaymentDate:dd/MM/yyyy} - KSh {p.Amount:N2} ({p.PaymentMethod ?? "N/A"})"));

        await _dialogService.ShowMessageAsync("Payment History", message).ConfigureAwait(true);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    public static string GetStatusDisplay(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Paid => "Paid",
            InvoiceStatus.PartiallyPaid => "Partial",
            InvoiceStatus.Unpaid => "Unpaid",
            InvoiceStatus.Overdue => "Overdue",
            _ => status.ToString()
        };
    }

    public static string GetOutstandingAmount(SupplierInvoice invoice)
    {
        var outstanding = invoice.TotalAmount - invoice.PaidAmount;
        return $"KSh {outstanding:N2}";
    }
}
