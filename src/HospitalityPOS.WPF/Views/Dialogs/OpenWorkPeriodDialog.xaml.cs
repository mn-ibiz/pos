using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for opening a new work period with an opening cash float.
/// </summary>
public partial class OpenWorkPeriodDialog : Window
{
    private string _amountString = "0";
    private decimal? _previousClosingBalance;
    private const int MaxDigits = 12;

    /// <summary>
    /// Gets the opening float amount entered by the user.
    /// </summary>
    public decimal OpeningFloat { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenWorkPeriodDialog"/> class.
    /// </summary>
    /// <param name="previousClosingBalance">The previous work period's closing balance, if any.</param>
    public OpenWorkPeriodDialog(decimal? previousClosingBalance = null)
    {
        InitializeComponent();
        _previousClosingBalance = previousClosingBalance;

        if (previousClosingBalance.HasValue && previousClosingBalance.Value > 0)
        {
            PreviousClosingPanel.Visibility = Visibility.Visible;
            PreviousClosingText.Text = FormatCurrency(previousClosingBalance.Value);
        }

        UpdateAmountDisplay();
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

    private void NumpadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var digit = button.Content?.ToString();
        if (string.IsNullOrEmpty(digit) || !char.IsDigit(digit[0])) return;

        AppendDigit(digit);
    }

    private void DoubleZeroButton_Click(object sender, RoutedEventArgs e)
    {
        AppendDigit("00");
    }

    private void TripleZeroButton_Click(object sender, RoutedEventArgs e)
    {
        AppendDigit("000");
    }

    private void DecimalButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_amountString.Contains('.'))
        {
            _amountString += ".";
            UpdateAmountDisplay();
        }
        SetError(null);
    }

    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_amountString.Length > 1)
        {
            _amountString = _amountString[..^1];
        }
        else
        {
            _amountString = "0";
        }
        UpdateAmountDisplay();
        SetError(null);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _amountString = "0";
        UpdateAmountDisplay();
        SetError(null);
    }

    private void CarryForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_previousClosingBalance.HasValue)
        {
            _amountString = _previousClosingBalance.Value.ToString("F2", CultureInfo.InvariantCulture);
            UpdateAmountDisplay();
            SetError(null);
        }
    }

    private void OpenPeriodButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(_amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            SetError("Please enter a valid amount");
            return;
        }

        if (amount < 0)
        {
            SetError("Opening float cannot be negative");
            return;
        }

        OpeningFloat = amount;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AppendDigit(string digits)
    {
        // Check if we already have a decimal point and would exceed 2 decimal places
        var decimalIndex = _amountString.IndexOf('.');
        if (decimalIndex >= 0)
        {
            var decimalPlaces = _amountString.Length - decimalIndex - 1;
            if (decimalPlaces >= 2) return;
        }

        // Prevent leading zeros (except for decimal numbers)
        if (_amountString == "0" && digits != "." && !digits.StartsWith("0"))
        {
            _amountString = digits;
        }
        else if (_amountString == "0" && digits.All(c => c == '0'))
        {
            // Don't add more zeros to a leading zero
            return;
        }
        else
        {
            // Check max length
            var newLength = _amountString.Replace(".", "").Length + digits.Length;
            if (newLength > MaxDigits) return;

            _amountString += digits;
        }

        UpdateAmountDisplay();
        SetError(null);
    }

    private void UpdateAmountDisplay()
    {
        if (decimal.TryParse(_amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            AmountDisplay.Text = FormatCurrency(amount);
        }
        else
        {
            AmountDisplay.Text = "KSh " + _amountString;
        }
    }

    private static string FormatCurrency(decimal amount)
    {
        return $"KSh {amount:N2}";
    }
}
