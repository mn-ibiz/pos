using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a payment method code to its associated color.
/// </summary>
public class PaymentMethodColorConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var code = values.FirstOrDefault()?.ToString()?.ToUpperInvariant();

        return code switch
        {
            "CASH" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Green #22C55E
            "MPESA" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),     // M-Pesa Green
            "CARD" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),     // Blue #3B82F6
            "CREDIT" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Orange/Amber #F59E0B
            "BANK" or "TRANSFER" => new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple #8B5CF6
            _ => new SolidColorBrush(Color.FromRgb(78, 78, 110))            // Default gray #4E4E6E
        };
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a payment method code to its associated color (single value version).
/// </summary>
public class PaymentMethodCodeToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var code = value?.ToString()?.ToUpperInvariant();

        return code switch
        {
            "CASH" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Green #22C55E
            "MPESA" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),     // M-Pesa Green
            "CARD" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),     // Blue #3B82F6
            "CREDIT" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Orange/Amber #F59E0B
            "BANK" or "TRANSFER" => new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple #8B5CF6
            _ => new SolidColorBrush(Color.FromRgb(78, 78, 110))            // Default gray #4E4E6E
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a hex color string to a SolidColorBrush.
/// </summary>
public class HexColorToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hexColor = value?.ToString();

        if (string.IsNullOrEmpty(hexColor))
        {
            return new SolidColorBrush(Color.FromRgb(78, 78, 110)); // Default gray
        }

        try
        {
            // Ensure hex color starts with #
            if (!hexColor.StartsWith('#'))
            {
                hexColor = "#" + hexColor;
            }

            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return new SolidColorBrush(color);
        }
        catch (Exception)
        {
            return new SolidColorBrush(Color.FromRgb(78, 78, 110)); // Default gray on error
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            var color = brush.Color;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return DependencyProperty.UnsetValue;
    }
}
