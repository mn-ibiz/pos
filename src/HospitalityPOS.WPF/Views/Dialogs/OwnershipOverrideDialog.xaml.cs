using System.Windows;
using System.Windows.Controls;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for requesting manager authorization when accessing another user's receipt.
/// </summary>
public partial class OwnershipOverrideDialog : Window
{
    private const int MaxPinLength = 6;
    private string _pin = string.Empty;

    /// <summary>
    /// Gets the name of the receipt owner.
    /// </summary>
    public string OwnerName { get; }

    /// <summary>
    /// Gets the description of the action being requested.
    /// </summary>
    public string ActionDescription { get; }

    /// <summary>
    /// Gets the entered PIN after successful dialog completion.
    /// </summary>
    public string? EnteredPin { get; private set; }

    /// <summary>
    /// Gets the reason entered for the override.
    /// </summary>
    public string? OverrideReason { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnershipOverrideDialog"/> class.
    /// </summary>
    /// <param name="ownerName">Name of the receipt owner.</param>
    /// <param name="actionDescription">Description of the action being requested.</param>
    public OwnershipOverrideDialog(string ownerName, string actionDescription)
    {
        InitializeComponent();

        OwnerName = ownerName;
        ActionDescription = actionDescription;

        OwnerNameText.Text = ownerName;
        ActionDescriptionText.Text = actionDescription;

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

        var reason = ReasonTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            SetError("Please provide a reason for the override");
            return;
        }

        EnteredPin = _pin;
        OverrideReason = reason;
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
