using System.Windows;
using System.Windows.Controls;

namespace HospitalityPOS.WPF.Controls;

/// <summary>
/// A touch-friendly numeric keypad control for entering amounts.
/// </summary>
public partial class NumericKeypad : UserControl
{
    /// <summary>
    /// Identifies the Value dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(NumericKeypad),
            new FrameworkPropertyMetadata(
                "0",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    /// <summary>
    /// Identifies the MaxLength dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(
            nameof(MaxLength),
            typeof(int),
            typeof(NumericKeypad),
            new PropertyMetadata(10));

    /// <summary>
    /// Gets or sets the current value as a string.
    /// </summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum length of the value.
    /// </summary>
    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Event raised when a digit is pressed.
    /// </summary>
    public event EventHandler<string>? DigitPressed;

    /// <summary>
    /// Event raised when clear is pressed.
    /// </summary>
    public event EventHandler? ClearPressed;

    /// <summary>
    /// Event raised when backspace is pressed.
    /// </summary>
    public event EventHandler? BackspacePressed;

    /// <summary>
    /// Event raised when the value changes.
    /// </summary>
    public event EventHandler<string>? ValueChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericKeypad"/> class.
    /// </summary>
    public NumericKeypad()
    {
        InitializeComponent();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumericKeypad keypad)
        {
            keypad.ValueChanged?.Invoke(keypad, (string)e.NewValue);
        }
    }

    private void DigitButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string digit)
        {
            AppendDigit(digit);
            DigitPressed?.Invoke(this, digit);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Value = "0";
        ClearPressed?.Invoke(this, EventArgs.Empty);
    }

    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (Value.Length > 1)
        {
            Value = Value[..^1];
        }
        else
        {
            Value = "0";
        }

        BackspacePressed?.Invoke(this, EventArgs.Empty);
    }

    private void AppendDigit(string digit)
    {
        if (Value.Length >= MaxLength)
        {
            return;
        }

        if (Value == "0")
        {
            Value = digit;
        }
        else
        {
            Value += digit;
        }
    }

    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    /// <returns>The parsed decimal value, or 0 if parsing fails.</returns>
    public decimal GetDecimalValue()
    {
        return decimal.TryParse(Value, out var result) ? result : 0;
    }

    /// <summary>
    /// Sets the value from a decimal.
    /// </summary>
    /// <param name="amount">The amount to set.</param>
    public void SetDecimalValue(decimal amount)
    {
        Value = ((long)amount).ToString();
    }

    /// <summary>
    /// Clears the value to zero.
    /// </summary>
    public void Clear()
    {
        Value = "0";
    }
}
