using System.Windows;
using System.Windows.Controls;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for entering a manager PIN to authorize a permission override.
/// </summary>
public partial class AuthorizationOverrideDialog : Window
{
    private const int MaxPinLength = 6;
    private string _pin = string.Empty;

    /// <summary>
    /// Gets the description of the action requiring authorization.
    /// </summary>
    public string ActionDescription { get; }

    /// <summary>
    /// Gets the permission that is required.
    /// </summary>
    public string PermissionRequired { get; }

    /// <summary>
    /// Gets the entered PIN after successful dialog completion.
    /// </summary>
    public string? EnteredPin { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationOverrideDialog"/> class.
    /// </summary>
    /// <param name="actionDescription">Description of the action requiring authorization.</param>
    /// <param name="permissionRequired">The permission name that is required.</param>
    public AuthorizationOverrideDialog(string actionDescription, string permissionRequired)
    {
        InitializeComponent();

        ActionDescription = actionDescription;
        PermissionRequired = permissionRequired;

        ActionDescriptionText.Text = actionDescription;
        PermissionRequiredText.Text = permissionRequired;

        DataContext = this;
        UpdatePinDisplay();
    }

    /// <summary>
    /// Sets an error message to display.
    /// </summary>
    /// <param name="message">The error message, or null to clear.</param>
    public void SetError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ErrorMessageText.Visibility = Visibility.Collapsed;
            ErrorMessageText.Text = string.Empty;
        }
        else
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Clears the entered PIN.
    /// </summary>
    public void ClearPin()
    {
        _pin = string.Empty;
        UpdatePinDisplay();
    }

    private void NumpadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (_pin.Length >= MaxPinLength) return;

        var digit = button.Content?.ToString();
        if (!string.IsNullOrEmpty(digit) && char.IsDigit(digit[0]))
        {
            _pin += digit;
            UpdatePinDisplay();
            SetError(null);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPin();
        SetError(null);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_pin))
        {
            SetError("Please enter a PIN");
            return;
        }

        if (_pin.Length < 4)
        {
            SetError("PIN must be at least 4 digits");
            return;
        }

        EnteredPin = _pin;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdatePinDisplay()
    {
        // Display asterisks for entered digits
        PinDisplay.Text = new string('*', _pin.Length);
    }
}
