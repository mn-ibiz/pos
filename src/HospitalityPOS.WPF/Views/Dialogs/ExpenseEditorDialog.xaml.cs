using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result data from the expense editor dialog.
/// </summary>
public class ExpenseEditorResult
{
    public int ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public int? SupplierId { get; set; }
    public bool IsTaxDeductible { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Interaction logic for ExpenseEditorDialog.xaml
/// </summary>
public partial class ExpenseEditorDialog : Window
{
    private readonly Expense? _existingExpense;
    private IReadOnlyList<ExpenseCategory> _categories = new List<ExpenseCategory>();
    private IReadOnlyList<Supplier> _suppliers = new List<Supplier>();
    private IReadOnlyList<PaymentMethod> _paymentMethods = new List<PaymentMethod>();

    public ExpenseEditorResult? Result { get; private set; }

    public ExpenseEditorDialog(Expense? existingExpense = null)
    {
        InitializeComponent();
        _existingExpense = existingExpense;

        if (_existingExpense != null)
        {
            TitleTextBlock.Text = "Edit Expense";
            SubtitleTextBlock.Text = $"Modify expense {_existingExpense.ExpenseNumber}";
            SaveButton.Content = "Update Expense";
        }

        ExpenseDatePicker.SelectedDate = DateTime.Today;

        // Wire up amount changes to update total
        AmountTextBox.TextChanged += (s, e) => UpdateTotalDisplay();
        TaxAmountTextBox.TextChanged += (s, e) => UpdateTotalDisplay();
    }

    /// <summary>
    /// Initializes the dialog with data from the database.
    /// </summary>
    public void Initialize(
        IReadOnlyList<ExpenseCategory> categories,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<PaymentMethod> paymentMethods)
    {
        _categories = categories;
        _suppliers = suppliers;
        _paymentMethods = paymentMethods;

        // Populate category combo
        CategoryComboBox.ItemsSource = _categories;

        // Populate supplier combo with "None" option
        var supplierList = new List<object> { new { Id = (int?)null, Name = "— None —" } };
        supplierList.AddRange(_suppliers.Select(s => new { Id = (int?)s.Id, s.Name }));
        SupplierComboBox.ItemsSource = supplierList;
        SupplierComboBox.SelectedIndex = 0;

        // Populate payment method combo
        var paymentList = new List<object> { new { Id = (int?)null, Name = "— Select —" } };
        paymentList.AddRange(_paymentMethods.Where(p => p.IsActive).Select(p => new { Id = (int?)p.Id, p.Name }));
        PaymentMethodComboBox.ItemsSource = paymentList;
        PaymentMethodComboBox.SelectedIndex = 0;

        // Load existing expense data if editing
        if (_existingExpense != null)
        {
            LoadExistingExpense();
        }
    }

    private void LoadExistingExpense()
    {
        if (_existingExpense == null) return;

        AmountTextBox.Text = _existingExpense.Amount.ToString("F2");
        TaxAmountTextBox.Text = _existingExpense.TaxAmount.ToString("F2");
        DescriptionTextBox.Text = _existingExpense.Description;
        ExpenseDatePicker.SelectedDate = _existingExpense.ExpenseDate;
        PaymentReferenceTextBox.Text = _existingExpense.PaymentReference;
        NotesTextBox.Text = _existingExpense.Notes;
        IsTaxDeductibleCheckBox.IsChecked = _existingExpense.IsTaxDeductible;

        // Select category
        var category = _categories.FirstOrDefault(c => c.Id == _existingExpense.ExpenseCategoryId);
        if (category != null)
        {
            CategoryComboBox.SelectedItem = category;
        }

        // Select supplier
        if (_existingExpense.SupplierId.HasValue)
        {
            for (int i = 0; i < SupplierComboBox.Items.Count; i++)
            {
                dynamic item = SupplierComboBox.Items[i];
                if (item.Id == _existingExpense.SupplierId)
                {
                    SupplierComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        // Select payment method
        if (_existingExpense.PaymentMethodId.HasValue)
        {
            for (int i = 0; i < PaymentMethodComboBox.Items.Count; i++)
            {
                dynamic item = PaymentMethodComboBox.Items[i];
                if (item.Id == _existingExpense.PaymentMethodId)
                {
                    PaymentMethodComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        UpdateTotalDisplay();
    }

    private void UpdateTotalDisplay()
    {
        if (decimal.TryParse(AmountTextBox.Text, out var amount) &&
            decimal.TryParse(TaxAmountTextBox.Text, out var tax))
        {
            var total = amount + tax;
            TotalAmountTextBlock.Text = $"KES {total:N2}";
        }
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex(@"^[0-9]*\.?[0-9]*$");
        var textBox = (System.Windows.Controls.TextBox)sender;
        var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
        e.Handled = !regex.IsMatch(newText);
    }

    private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (System.Windows.Controls.TextBox)sender;
        if (textBox.Text == "0.00")
        {
            textBox.Text = string.Empty;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear previous error
        ErrorBorder.Visibility = Visibility.Collapsed;

        // Validate
        var errors = new List<string>();

        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            errors.Add("Please enter a valid amount greater than 0.");
        }

        if (CategoryComboBox.SelectedItem == null)
        {
            errors.Add("Please select a category.");
        }

        if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
        {
            errors.Add("Please enter a description.");
        }

        if (!ExpenseDatePicker.SelectedDate.HasValue)
        {
            errors.Add("Please select an expense date.");
        }

        decimal.TryParse(TaxAmountTextBox.Text, out var taxAmount);

        if (errors.Count > 0)
        {
            ErrorTextBlock.Text = string.Join("\n", errors);
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        // Get selected IDs
        int? supplierId = null;
        if (SupplierComboBox.SelectedItem != null)
        {
            dynamic supplier = SupplierComboBox.SelectedItem;
            supplierId = supplier.Id;
        }

        int? paymentMethodId = null;
        if (PaymentMethodComboBox.SelectedItem != null)
        {
            dynamic method = PaymentMethodComboBox.SelectedItem;
            paymentMethodId = method.Id;
        }

        // Create result - note: CategoryComboBox.SelectedItem is validated above to not be null
        var selectedCategory = (ExpenseCategory)CategoryComboBox.SelectedItem!;
        Result = new ExpenseEditorResult
        {
            ExpenseCategoryId = selectedCategory.Id,
            Description = DescriptionTextBox.Text.Trim(),
            Amount = amount,
            TaxAmount = taxAmount,
            ExpenseDate = ExpenseDatePicker.SelectedDate!.Value,
            PaymentMethodId = paymentMethodId,
            PaymentReference = string.IsNullOrWhiteSpace(PaymentReferenceTextBox.Text) ? null : PaymentReferenceTextBox.Text.Trim(),
            SupplierId = supplierId,
            IsTaxDeductible = IsTaxDeductibleCheckBox.IsChecked ?? true,
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }
}
