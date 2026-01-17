using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Views.Dialogs;

public partial class SupplierPaymentDialog : Window
{
    private readonly Supplier _supplier;
    private readonly SupplierInvoice? _invoice;
    private readonly ISessionService _sessionService;
    private readonly ISupplierCreditService _supplierCreditService;

    public SupplierPayment? Result { get; private set; }

    public SupplierPaymentDialog(
        Supplier supplier,
        SupplierInvoice? invoice,
        ISessionService sessionService,
        ISupplierCreditService supplierCreditService)
    {
        InitializeComponent();

        _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
        _invoice = invoice;
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _supplierCreditService = supplierCreditService ?? throw new ArgumentNullException(nameof(supplierCreditService));

        SupplierNameText.Text = $"{supplier.Name} ({supplier.Code})";
        CurrentBalanceText.Text = $"KSh {supplier.CurrentBalance:N2}";

        // If invoice is specified, default to the outstanding amount
        if (invoice != null)
        {
            var outstanding = invoice.TotalAmount - invoice.PaidAmount;
            AmountTextBox.Text = outstanding.ToString("N2");
        }
        else if (supplier.CurrentBalance > 0)
        {
            AmountTextBox.Text = supplier.CurrentBalance.ToString("N2");
        }
    }

    private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input with one decimal point
        var regex = new Regex(@"^[0-9]*\.?[0-9]*$");
        var textBox = (TextBox)sender;
        var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
        e.Handled = !regex.IsMatch(newText);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var paymentMethod = ((ComboBoxItem)PaymentMethodCombo.SelectedItem)?.Content?.ToString() ?? "Cash";

        Result = new SupplierPayment
        {
            SupplierId = _supplier.Id,
            SupplierInvoiceId = _invoice?.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Reference = string.IsNullOrWhiteSpace(ReferenceTextBox.Text) ? null : ReferenceTextBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            ProcessedByUserId = _sessionService.CurrentUser?.Id ?? 0
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
