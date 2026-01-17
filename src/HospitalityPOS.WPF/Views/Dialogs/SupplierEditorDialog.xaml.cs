using System.Globalization;
using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the supplier editor dialog.
/// </summary>
public class SupplierEditorResult
{
    /// <summary>
    /// Gets or sets the supplier code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supplier name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact person.
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the tax ID.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the bank account.
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the payment term in days.
    /// </summary>
    public int PaymentTermDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the credit limit.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the supplier is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Interaction logic for SupplierEditorDialog.xaml
/// </summary>
public partial class SupplierEditorDialog : Window
{
    private readonly Supplier? _existingSupplier;
    private readonly ISupplierService? _supplierService;

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public SupplierEditorResult? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierEditorDialog"/> class for creating a new supplier.
    /// </summary>
    public SupplierEditorDialog() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierEditorDialog"/> class.
    /// </summary>
    /// <param name="existingSupplier">The supplier to edit, or null for creating a new supplier.</param>
    public SupplierEditorDialog(Supplier? existingSupplier)
    {
        InitializeComponent();

        _existingSupplier = existingSupplier;
        _supplierService = App.Services.GetService<ISupplierService>();

        SetupDialog();
    }

    private async void SetupDialog()
    {
        if (_existingSupplier is not null)
        {
            // Edit mode
            TitleTextBlock.Text = "Edit Supplier";
            SubtitleTextBlock.Text = $"Editing: {_existingSupplier.Name}";

            CodeTextBox.Text = _existingSupplier.Code;
            NameTextBox.Text = _existingSupplier.Name;
            ContactPersonTextBox.Text = _existingSupplier.ContactPerson;
            PhoneTextBox.Text = _existingSupplier.Phone;
            EmailTextBox.Text = _existingSupplier.Email;
            AddressTextBox.Text = _existingSupplier.Address;
            CityTextBox.Text = _existingSupplier.City;
            CountryTextBox.Text = _existingSupplier.Country ?? "Kenya";
            TaxIdTextBox.Text = _existingSupplier.TaxId;
            BankNameTextBox.Text = _existingSupplier.BankName;
            BankAccountTextBox.Text = _existingSupplier.BankAccount;
            PaymentTermDaysTextBox.Text = _existingSupplier.PaymentTermDays.ToString();
            CreditLimitTextBox.Text = _existingSupplier.CreditLimit.ToString("F2");
            NotesTextBox.Text = _existingSupplier.Notes;
            IsActiveCheckBox.IsChecked = _existingSupplier.IsActive;
        }
        else
        {
            // Create mode
            TitleTextBlock.Text = "Create Supplier";
            SubtitleTextBlock.Text = "Add a new supplier to the system";

            // Generate next code
            if (_supplierService is not null)
            {
                try
                {
                    var nextCode = await _supplierService.GenerateNextCodeAsync();
                    CodeTextBox.Text = nextCode;
                }
                catch (Exception)
                {
                    CodeTextBox.Text = "SUP-0001";
                }
            }
        }

        // Focus on name field for new suppliers (code is auto-generated)
        if (_existingSupplier is null)
        {
            NameTextBox.Focus();
        }
        else
        {
            CodeTextBox.Focus();
        }
    }

    private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (System.Windows.Controls.TextBox)sender;
        var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

        e.Handled = !decimal.TryParse(newText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        // Validate code
        var code = CodeTextBox.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Supplier code is required.");
            CodeTextBox.Focus();
            return;
        }

        if (code.Length < 3)
        {
            ShowError("Supplier code must be at least 3 characters.");
            CodeTextBox.Focus();
            return;
        }

        // Check for duplicate code
        if (_supplierService is not null)
        {
            var isUnique = await _supplierService.IsCodeUniqueAsync(code, _existingSupplier?.Id);
            if (!isUnique)
            {
                ShowError($"A supplier with code '{code}' already exists.");
                CodeTextBox.Focus();
                return;
            }
        }

        // Validate name
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Supplier name is required.");
            NameTextBox.Focus();
            return;
        }

        if (name.Length < 2)
        {
            ShowError("Supplier name must be at least 2 characters.");
            NameTextBox.Focus();
            return;
        }

        // Validate email format if provided
        var email = EmailTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
        {
            ShowError("Please enter a valid email address.");
            EmailTextBox.Focus();
            return;
        }

        // Parse payment term days
        if (!int.TryParse(PaymentTermDaysTextBox.Text, out var paymentTermDays) || paymentTermDays < 0)
        {
            paymentTermDays = 30;
        }

        // Parse credit limit
        if (!decimal.TryParse(CreditLimitTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var creditLimit) || creditLimit < 0)
        {
            creditLimit = 0;
        }

        Result = new SupplierEditorResult
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            ContactPerson = string.IsNullOrWhiteSpace(ContactPersonTextBox.Text) ? null : ContactPersonTextBox.Text.Trim(),
            Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim(),
            City = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim(),
            Country = string.IsNullOrWhiteSpace(CountryTextBox.Text) ? null : CountryTextBox.Text.Trim(),
            TaxId = string.IsNullOrWhiteSpace(TaxIdTextBox.Text) ? null : TaxIdTextBox.Text.Trim(),
            BankName = string.IsNullOrWhiteSpace(BankNameTextBox.Text) ? null : BankNameTextBox.Text.Trim(),
            BankAccount = string.IsNullOrWhiteSpace(BankAccountTextBox.Text) ? null : BankAccountTextBox.Text.Trim(),
            PaymentTermDays = paymentTermDays,
            CreditLimit = creditLimit,
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            IsActive = IsActiveCheckBox.IsChecked ?? true
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
