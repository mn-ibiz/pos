using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value (true = Visible, false = Collapsed).
/// Supports inversion via ConverterParameter = "Invert".
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

        if (invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = value is Visibility v && v == Visibility.Visible;
        var invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

        if (invert)
        {
            visibility = !visibility;
        }

        return visibility;
    }
}

/// <summary>
/// Converts a boolean value to its inverse Visibility value (true = Collapsed, false = Visible).
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

/// <summary>
/// Alias for InverseBoolToVisibilityConverter for compatibility with views that use the longer name.
/// </summary>
public class InverseBooleanToVisibilityConverter : InverseBoolToVisibilityConverter
{
}

/// <summary>
/// Converts a string to Visibility (non-empty = Visible, empty/null = Collapsed).
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var stringValue = value as string;
        return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a boolean value to its inverse (true = false, false = true).
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// Converts null to Visibility (null = Collapsed, non-null = Visible).
/// Supports inversion via ConverterParameter = "Inverse".
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
        var invert = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

        if (invert)
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts zero to Visibility (0 = Visible, non-zero = Collapsed).
/// Useful for showing empty state messages.
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isZero = value is int i && i == 0;
        return isZero ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a positive numeric value to Visibility (positive = Visible, zero/negative = Collapsed).
/// Supports int, decimal, and double values.
/// </summary>
public class PositiveToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isPositive = value switch
        {
            int i => i > 0,
            decimal d => d > 0,
            double db => db > 0,
            float f => f > 0,
            long l => l > 0,
            _ => false
        };

        return isPositive ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts seconds to a minutes:seconds or seconds format (e.g., "4:30" or "45s").
/// </summary>
public class SecondsToMinSecConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            return minutes > 0 ? $"{minutes}:{secs:D2}" : $"{secs}s";
        }
        return "0s";
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a string/int length to boolean based on minimum length parameter.
/// </summary>
public class MinLengthToBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var length = value switch
        {
            int i => i,
            string s => s?.Length ?? 0,
            _ => 0
        };

        if (parameter is string minStr && int.TryParse(minStr, out int min))
        {
            return length >= min;
        }
        return false;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a string to Visibility by comparing with ConverterParameter.
/// Visible if string equals parameter, Collapsed otherwise.
/// </summary>
public class StringEqualsToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var stringValue = value?.ToString();
        var parameterValue = parameter?.ToString();

        if (string.IsNullOrEmpty(stringValue) || string.IsNullOrEmpty(parameterValue))
        {
            return Visibility.Collapsed;
        }

        return stringValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts an enum value to a boolean based on the ConverterParameter.
/// Used for binding radio buttons to enum properties.
/// </summary>
public class EnumBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return enumValue?.Equals(targetValue, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return Binding.DoNothing;
    }
}

/// <summary>
/// Alias for NullToVisibilityConverter for simpler naming in XAML.
/// </summary>
public class NullToCollapsedConverter : NullToVisibilityConverter
{
}

/// <summary>
/// Converts a PIN string to masked display (shows bullets instead of digits).
/// </summary>
public class PinMaskConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string pin && !string.IsNullOrEmpty(pin))
        {
            // Return bullets for each character
            return new string('\u2022', pin.Length);
        }
        return string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Multi-value converter to determine if a Purchase Order is overdue.
/// Values[0]: ExpectedDate (DateTime?)
/// Values[1]: Status (PurchaseOrderStatus)
/// Returns Visibility.Visible if overdue, Collapsed otherwise.
/// </summary>
public class PurchaseOrderOverdueConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Visibility.Collapsed;
        }

        var expectedDate = values[0] as DateTime?;
        var status = values[1] is PurchaseOrderStatus s ? s : PurchaseOrderStatus.Draft;

        // Check if overdue: expected date has passed and status is not Complete or Cancelled
        var isOverdue = expectedDate.HasValue &&
                        expectedDate.Value.Date < DateTime.Today &&
                        status != PurchaseOrderStatus.Complete &&
                        status != PurchaseOrderStatus.Cancelled;

        return isOverdue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Multi-value converter that returns a background color based on overdue status.
/// Values[0]: ExpectedDate (DateTime?)
/// Values[1]: Status (PurchaseOrderStatus)
/// Returns red-tinted color if overdue, transparent otherwise.
/// </summary>
public class PurchaseOrderOverdueBackgroundConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Brushes.Transparent;
        }

        var expectedDate = values[0] as DateTime?;
        var status = values[1] is PurchaseOrderStatus s ? s : PurchaseOrderStatus.Draft;

        // Check if overdue
        var isOverdue = expectedDate.HasValue &&
                        expectedDate.Value.Date < DateTime.Today &&
                        status != PurchaseOrderStatus.Complete &&
                        status != PurchaseOrderStatus.Cancelled;

        if (isOverdue)
        {
            // Return a subtle red background for overdue rows
            return new SolidColorBrush(Color.FromArgb(0x20, 0xF4, 0x43, 0x36)); // Semi-transparent red
        }

        return Brushes.Transparent;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a date to show how many days overdue it is.
/// Returns empty string if not overdue.
/// </summary>
public class OverdueDaysConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return string.Empty;
        }

        var expectedDate = values[0] as DateTime?;
        var status = values[1] is PurchaseOrderStatus s ? s : PurchaseOrderStatus.Draft;

        // Check if overdue
        if (!expectedDate.HasValue ||
            expectedDate.Value.Date >= DateTime.Today ||
            status == PurchaseOrderStatus.Complete ||
            status == PurchaseOrderStatus.Cancelled)
        {
            return string.Empty;
        }

        var daysOverdue = (DateTime.Today - expectedDate.Value.Date).Days;
        return daysOverdue == 1 ? "1 day overdue" : $"{daysOverdue} days overdue";
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
