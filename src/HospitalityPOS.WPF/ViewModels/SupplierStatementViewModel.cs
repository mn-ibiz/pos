using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the supplier statement view.
/// </summary>
public partial class SupplierStatementViewModel : ViewModelBase, INavigationAware
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierCreditService _supplierCreditService;
    private readonly IReportPrintService _reportPrintService;
    private readonly IExportService _exportService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private Supplier? _supplier;

    [ObservableProperty]
    private SupplierStatement? _statement;

    [ObservableProperty]
    private ObservableCollection<SupplierStatementLine> _statementLines = [];

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SupplierAgingSummary? _agingSummary;

    private int _supplierId;

    public SupplierStatementViewModel(
        ISupplierService supplierService,
        ISupplierCreditService supplierCreditService,
        IReportPrintService reportPrintService,
        IExportService exportService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _supplierCreditService = supplierCreditService ?? throw new ArgumentNullException(nameof(supplierCreditService));
        _reportPrintService = reportPrintService ?? throw new ArgumentNullException(nameof(reportPrintService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Supplier Statement";
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
                    Title = $"Statement - {Supplier.Name}";
                }

                // Load aging summary
                AgingSummary = await _supplierCreditService.GetAgingSummaryAsync(_supplierId).ConfigureAwait(true);

                await GenerateStatementAsync().ConfigureAwait(true);
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading statement...").ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task GenerateStatementAsync()
    {
        if (Supplier is null) return;

        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                Statement = await _supplierCreditService.GenerateStatementAsync(_supplierId, StartDate, EndDate).ConfigureAwait(true);
                StatementLines = new ObservableCollection<SupplierStatementLine>(Statement.Lines);
            }
            finally
            {
                IsLoading = false;
            }
        }, "Generating statement...").ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        if (Statement is null) return;

        var html = GenerateStatementHtml();
        await _reportPrintService.PrintHtmlReportAsync(html, $"Statement_{Statement.SupplierCode}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}").ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        if (Statement is null) return;

        var csv = GenerateStatementCsv();
        var fileName = $"Statement_{Statement.SupplierCode}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv";

        await _exportService.ExportToFileAsync(fileName, csv).ConfigureAwait(true);
        await _dialogService.ShowMessageAsync("Export Complete", $"Statement exported to {fileName}").ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task PrintStatementAsync()
    {
        if (Statement is null) return;

        var html = GenerateStatementHtml();
        await _reportPrintService.PrintHtmlReportAsync(html).ConfigureAwait(true);
    }

    private string GenerateStatementHtml()
    {
        if (Statement is null) return "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; }}
        .header {{ margin-bottom: 20px; }}
        .supplier-info {{ margin-bottom: 20px; }}
        .summary {{ background: #f5f5f5; padding: 15px; margin-bottom: 20px; border-radius: 8px; }}
        .aging {{ display: flex; gap: 20px; margin-top: 10px; }}
        .aging-item {{ text-align: center; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background: #4F46E5; color: white; }}
        tr:nth-child(even) {{ background: #f9f9f9; }}
        .debit {{ color: #F44336; }}
        .credit {{ color: #4CAF50; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>SUPPLIER STATEMENT</h1>
        <p>Period: {StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy}</p>
    </div>

    <div class='supplier-info'>
        <strong>{Statement.SupplierName}</strong> ({Statement.SupplierCode})<br/>
        {Statement.SupplierAddress}<br/>
        {Statement.SupplierPhone} | {Statement.SupplierEmail}<br/>
        Payment Terms: {Statement.PaymentTermDays} days | Credit Limit: KSh {Statement.CreditLimit:N2}
    </div>

    <div class='summary'>
        <strong>Summary</strong>
        <table style='width: auto; border: none; margin-top: 10px;'>
            <tr><td style='border: none;'>Opening Balance:</td><td style='border: none; text-align: right;'>KSh {Statement.OpeningBalance:N2}</td></tr>
            <tr><td style='border: none;'>Total Invoices:</td><td style='border: none; text-align: right;'>KSh {Statement.TotalInvoices:N2}</td></tr>
            <tr><td style='border: none;'>Total Payments:</td><td style='border: none; text-align: right;'>KSh {Statement.TotalPayments:N2}</td></tr>
            <tr><td style='border: none;'><strong>Closing Balance:</strong></td><td style='border: none; text-align: right;'><strong>KSh {Statement.ClosingBalance:N2}</strong></td></tr>
        </table>
    </div>

    {(AgingSummary != null ? $@"
    <div class='summary'>
        <strong>Aging Summary</strong>
        <div class='aging'>
            <div class='aging-item'><div>Current</div><div>KSh {AgingSummary.Current:N0}</div></div>
            <div class='aging-item'><div>31-60 Days</div><div>KSh {AgingSummary.Days30:N0}</div></div>
            <div class='aging-item'><div>61-90 Days</div><div>KSh {AgingSummary.Days60:N0}</div></div>
            <div class='aging-item'><div>90+ Days</div><div style='color: #F44336;'>KSh {AgingSummary.Days90Plus:N0}</div></div>
        </div>
    </div>
    " : "")}

    <table>
        <thead>
            <tr>
                <th>Date</th>
                <th>Reference</th>
                <th>Description</th>
                <th>Debit</th>
                <th>Credit</th>
                <th>Balance</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("\n", Statement.Lines.Select(l => $@"
            <tr>
                <td>{l.Date:dd/MM/yyyy}</td>
                <td>{l.Reference}</td>
                <td>{l.Description}</td>
                <td class='debit'>{(l.Debit > 0 ? $"KSh {l.Debit:N2}" : "-")}</td>
                <td class='credit'>{(l.Credit > 0 ? $"KSh {l.Credit:N2}" : "-")}</td>
                <td>KSh {l.RunningBalance:N2}</td>
            </tr>
            "))}
        </tbody>
    </table>

    <div class='footer'>
        Generated on {DateTime.Now:dd/MM/yyyy HH:mm} | Statement Period: {StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}
    </div>
</body>
</html>";
    }

    private string GenerateStatementCsv()
    {
        if (Statement is null) return "";

        var lines = new List<string>
        {
            $"Supplier Statement - {Statement.SupplierName} ({Statement.SupplierCode})",
            $"Period: {StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy}",
            "",
            $"Opening Balance,KSh {Statement.OpeningBalance:N2}",
            $"Total Invoices,KSh {Statement.TotalInvoices:N2}",
            $"Total Payments,KSh {Statement.TotalPayments:N2}",
            $"Closing Balance,KSh {Statement.ClosingBalance:N2}",
            "",
            "Date,Reference,Description,Debit,Credit,Balance"
        };

        foreach (var line in Statement.Lines)
        {
            lines.Add($"{line.Date:dd/MM/yyyy},{line.Reference},{line.Description},{line.Debit:N2},{line.Credit:N2},{line.RunningBalance:N2}");
        }

        return string.Join("\n", lines);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
