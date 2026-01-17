using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
