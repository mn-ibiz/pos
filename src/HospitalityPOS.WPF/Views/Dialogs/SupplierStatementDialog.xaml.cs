using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for SupplierStatementDialog.xaml
/// </summary>
public partial class SupplierStatementDialog : Window
{
    private readonly Supplier _supplier;
    private SupplierStatement? _currentStatement;

    public SupplierStatementDialog(Supplier supplier)
    {
        InitializeComponent();
        _supplier = supplier;

        // Initialize UI
        InitializeSupplierInfo();
        InitializeDateRange();

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
    }

    private void InitializeSupplierInfo()
    {
        SupplierNameText.Text = _supplier.Name;
        SupplierCodeText.Text = _supplier.Code;
        SupplierContactText.Text = _supplier.ContactPerson ?? "N/A";
        SupplierEmailText.Text = _supplier.Email ?? "N/A";
        CurrentBalanceText.Text = $"KSh {_supplier.CurrentBalance:N2}";
        CreditLimitText.Text = $"KSh {_supplier.CreditLimit:N2}";
    }

    private void InitializeDateRange()
    {
        // Default to last 3 months
        EndDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-3);
    }

    private async void LoadStatement_Click(object sender, RoutedEventArgs e)
    {
        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select both start and end dates.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadStatementAsync();
    }

    private async Task LoadStatementAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;

            using var scope = App.Services.CreateScope();
            var supplierCreditService = scope.ServiceProvider.GetRequiredService<ISupplierCreditService>();

            var startDate = StartDatePicker.SelectedDate!.Value;
            var endDate = EndDatePicker.SelectedDate!.Value.AddDays(1).AddSeconds(-1); // Include full end day

            _currentStatement = await supplierCreditService.GenerateStatementAsync(_supplier.Id, startDate, endDate);

            // Update summary cards
            OpeningBalanceText.Text = $"KSh {_currentStatement.OpeningBalance:N2}";
            TotalInvoicesText.Text = $"KSh {_currentStatement.TotalInvoices:N2}";
            TotalPaymentsText.Text = $"KSh {_currentStatement.TotalPayments:N2}";
            ClosingBalanceText.Text = $"KSh {_currentStatement.ClosingBalance:N2}";

            // Update grid
            TransactionsGrid.ItemsSource = _currentStatement.Lines;

            // Update transaction count
            TransactionCountText.Text = $"{_currentStatement.Lines.Count} transaction(s) found";

            // Show empty state if no transactions
            if (_currentStatement.Lines.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void Last30Days_Click(object sender, RoutedEventArgs e)
    {
        EndDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
    }

    private void Last90Days_Click(object sender, RoutedEventArgs e)
    {
        EndDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = DateTime.Today.AddDays(-90);
    }

    private void YTD_Click(object sender, RoutedEventArgs e)
    {
        EndDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStatement == null || _currentStatement.Lines.Count == 0)
        {
            MessageBox.Show("Please load a statement first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var reportPrintService = scope.ServiceProvider.GetRequiredService<IReportPrintService>();

            var html = GenerateStatementHtml();
            var filename = $"SupplierStatement_{_supplier.Code}_{StartDatePicker.SelectedDate:yyyyMMdd}_{EndDatePicker.SelectedDate:yyyyMMdd}.pdf";

            await reportPrintService.ExportToPdfAsync(html, filename);
            MessageBox.Show($"Statement exported to {filename}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStatement == null || _currentStatement.Lines.Count == 0)
        {
            MessageBox.Show("Please load a statement first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var reportPrintService = scope.ServiceProvider.GetRequiredService<IReportPrintService>();

            var html = GenerateStatementHtml();
            var filename = $"SupplierStatement_{_supplier.Code}_{StartDatePicker.SelectedDate:yyyyMMdd}_{EndDatePicker.SelectedDate:yyyyMMdd}.pdf";

            await reportPrintService.ExportToPdfAsync(html, filename);
            MessageBox.Show($"Statement exported to {filename}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GenerateStatementHtml()
    {
        if (_currentStatement == null) return "<html><body>No data</body></html>";

        var lines = string.Join("\n", _currentStatement.Lines.Select(l =>
            $@"<tr>
                <td>{l.Date:d}</td>
                <td>{l.Reference}</td>
                <td>{l.Description}</td>
                <td class='type-{l.Type.ToLower()}'>{l.Type}</td>
                <td class='debit'>{(l.Debit > 0 ? $"KSh {l.Debit:N2}" : "-")}</td>
                <td class='credit'>{(l.Credit > 0 ? $"KSh {l.Credit:N2}" : "-")}</td>
                <td class='balance'>KSh {l.RunningBalance:N2}</td>
            </tr>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; margin-bottom: 5px; }}
        .subtitle {{ color: #666; margin-bottom: 20px; }}
        .supplier-info {{ background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 20px; }}
        .supplier-info h2 {{ margin: 0 0 10px 0; }}
        .supplier-info p {{ margin: 5px 0; }}
        .summary {{ display: flex; justify-content: space-between; margin-bottom: 20px; }}
        .summary-box {{ background: #f0f0f0; padding: 10px 20px; border-radius: 8px; text-align: center; }}
        .summary-box label {{ display: block; font-size: 12px; color: #666; }}
        .summary-box value {{ display: block; font-size: 18px; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th {{ background: #333; color: white; padding: 10px; text-align: left; }}
        td {{ padding: 8px 10px; border-bottom: 1px solid #ddd; }}
        .debit {{ color: #D32F2F; }}
        .credit {{ color: #388E3C; }}
        .balance {{ font-weight: bold; }}
        .type-invoice {{ color: #D32F2F; }}
        .type-payment {{ color: #388E3C; }}
        .footer {{ margin-top: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <h1>Supplier Statement</h1>
    <p class='subtitle'>Period: {_currentStatement.StartDate:d} to {_currentStatement.EndDate:d}</p>

    <div class='supplier-info'>
        <h2>{_currentStatement.SupplierName}</h2>
        <p>Code: {_currentStatement.SupplierCode}</p>
        <p>Address: {_currentStatement.SupplierAddress ?? "N/A"}</p>
        <p>Phone: {_currentStatement.SupplierPhone ?? "N/A"} | Email: {_currentStatement.SupplierEmail ?? "N/A"}</p>
        <p>Credit Limit: KSh {_currentStatement.CreditLimit:N2} | Payment Terms: {_currentStatement.PaymentTermDays} days</p>
    </div>

    <table>
        <tr class='summary'>
            <td style='background:#e8f5e9; text-align:center; padding:15px;'>
                <strong>Opening Balance</strong><br/>KSh {_currentStatement.OpeningBalance:N2}
            </td>
            <td style='background:#ffebee; text-align:center; padding:15px;'>
                <strong>Total Invoices</strong><br/>KSh {_currentStatement.TotalInvoices:N2}
            </td>
            <td style='background:#e8f5e9; text-align:center; padding:15px;'>
                <strong>Total Payments</strong><br/>KSh {_currentStatement.TotalPayments:N2}
            </td>
            <td style='background:#fff3e0; text-align:center; padding:15px;'>
                <strong>Closing Balance</strong><br/>KSh {_currentStatement.ClosingBalance:N2}
            </td>
        </tr>
    </table>

    <table style='margin-top:20px;'>
        <thead>
            <tr>
                <th>Date</th>
                <th>Reference</th>
                <th>Description</th>
                <th>Type</th>
                <th>Debit</th>
                <th>Credit</th>
                <th>Balance</th>
            </tr>
        </thead>
        <tbody>
            {lines}
        </tbody>
    </table>

    <div class='footer'>
        Generated on {DateTime.Now:g} | {_currentStatement.Lines.Count} transaction(s)
    </div>
</body>
</html>";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
