using System.Windows;
using System.Windows.Controls;

namespace HospitalityPOS.WPF.Controls;

/// <summary>
/// Alphanumeric keypad control for touch input (QWERTY layout with numbers).
/// Used for M-Pesa transaction code entry and similar alphanumeric inputs.
/// </summary>
public partial class AlphanumericKeypad : UserControl
{
    /// <summary>
    /// Identifies the <see cref="Value"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(AlphanumericKeypad),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    /// <summary>
    /// Identifies the <see cref="MaxLength"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(
            nameof(MaxLength),
            typeof(int),
            typeof(AlphanumericKeypad),
            new PropertyMetadata(10));

    /// <summary>
    /// Identifies the <see cref="AutoUppercase"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoUppercaseProperty =
        DependencyProperty.Register(
            nameof(AutoUppercase),
            typeof(bool),
            typeof(AlphanumericKeypad),
            new PropertyMetadata(true));

    /// <summary>
    /// Initializes a new instance of the <see cref="AlphanumericKeypad"/> class.
    /// </summary>
    public AlphanumericKeypad()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the current value of the keypad.
    /// </summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum length of the input.
    /// Default is 10 (for M-Pesa transaction codes).
    /// </summary>
    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Gets or sets whether input is automatically converted to uppercase.
    /// Default is true.
    /// </summary>
    public bool AutoUppercase
    {
        get => (bool)GetValue(AutoUppercaseProperty);
        set => SetValue(AutoUppercaseProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Value changed callback - can be used for validation feedback
    }

    private void OnKeyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string key)
        {
            var currentValue = Value ?? string.Empty;

            // Check max length
            if (currentValue.Length >= MaxLength)
            {
                return;
            }

            // Apply uppercase if enabled
            var charToAdd = AutoUppercase ? key.ToUpperInvariant() : key;

            Value = currentValue + charToAdd;
        }
    }

    private void OnBackspaceClick(object sender, RoutedEventArgs e)
    {
        var currentValue = Value ?? string.Empty;

        if (currentValue.Length > 0)
        {
            Value = currentValue[..^1];
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        Value = string.Empty;
    }
}
