using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

public partial class SupplierInvoiceEditorDialog : Window
{
    private readonly SupplierInvoice? _existingInvoice;
    private readonly Supplier _supplier;

    public SupplierInvoice? Result { get; private set; }

    public SupplierInvoiceEditorDialog(SupplierInvoice? existingInvoice, Supplier supplier)
    {
        InitializeComponent();

        _existingInvoice = existingInvoice;
        _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

        SupplierNameText.Text = $"{supplier.Name} ({supplier.Code})";

        // Set default due date based on payment terms
        var defaultInvoiceDate = DateTime.Today;
        var defaultDueDate = defaultInvoiceDate.AddDays(supplier.PaymentTermDays);

        InvoiceDatePicker.SelectedDate = defaultInvoiceDate;
        DueDatePicker.SelectedDate = defaultDueDate;

        if (existingInvoice != null)
        {
            InvoiceNumberTextBox.Text = existingInvoice.InvoiceNumber;
            InvoiceDatePicker.SelectedDate = existingInvoice.InvoiceDate;
            DueDatePicker.SelectedDate = existingInvoice.DueDate;
            AmountTextBox.Text = existingInvoice.TotalAmount.ToString("N2");
            NotesTextBox.Text = existingInvoice.Notes ?? "";
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
        if (string.IsNullOrWhiteSpace(InvoiceNumberTextBox.Text))
        {
            MessageBox.Show("Please enter an invoice number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!InvoiceDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select an invoice date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DueDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a due date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = _existingInvoice ?? new SupplierInvoice();
        Result.SupplierId = _supplier.Id;
        Result.InvoiceNumber = InvoiceNumberTextBox.Text.Trim();
        Result.InvoiceDate = InvoiceDatePicker.SelectedDate.Value;
        Result.DueDate = DueDatePicker.SelectedDate.Value;
        Result.TotalAmount = amount;
        Result.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

        if (_existingInvoice == null)
        {
            Result.PaidAmount = 0;
            Result.Status = InvoiceStatus.Unpaid;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
