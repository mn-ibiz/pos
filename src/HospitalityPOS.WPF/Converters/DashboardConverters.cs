using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a boolean value to a color Brush (true = Green, false = Red).
/// Use ConverterParameter to customize colors: "GreenYellow" or "BlueGray".
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(34, 197, 94));  // Green-500
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(239, 68, 68));    // Red-500
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(234, 179, 8)); // Yellow-500
    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(59, 130, 246)); // Blue-500
    private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(156, 163, 175)); // Gray-400

    static BoolToColorConverter()
    {
        GreenBrush.Freeze();
        RedBrush.Freeze();
        YellowBrush.Freeze();
        BlueBrush.Freeze();
        GrayBrush.Freeze();
    }

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var colorScheme = parameter?.ToString()?.ToLowerInvariant();

        return colorScheme switch
        {
            "greenyellow" => boolValue ? GreenBrush : YellowBrush,
            "bluegray" => boolValue ? BlueBrush : GrayBrush,
            "redgreen" => boolValue ? RedBrush : GreenBrush, // Inverted for alerts
            _ => boolValue ? GreenBrush : RedBrush
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToColorConverter does not support ConvertBack");
    }
}

/// <summary>
/// Converts a number greater than zero to Visibility.Visible, otherwise Collapsed.
/// Works with int, decimal, double, and other numeric types.
/// </summary>
public class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isGreaterThanZero = value switch
        {
            int i => i > 0,
            decimal d => d > 0,
            double db => db > 0,
            float f => f > 0,
            long l => l > 0,
            short s => s > 0,
            byte b => b > 0,
            _ => false
        };

        // Support inversion via parameter
        var invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        if (invert)
        {
            isGreaterThanZero = !isGreaterThanZero;
        }

        return isGreaterThanZero ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GreaterThanZeroToVisibilityConverter does not support ConvertBack");
    }
}

/// <summary>
/// Converts an hour value to a Brush, highlighting the current hour.
/// Used in hourly sales chart to highlight the current hour's bar.
/// </summary>
public class CurrentHourBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush CurrentHourBrush = new(Color.FromRgb(59, 130, 246));  // Blue-500 (highlight)
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(156, 163, 175));     // Gray-400

    static CurrentHourBrushConverter()
    {
        CurrentHourBrush.Freeze();
        DefaultBrush.Freeze();
    }

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int hour)
        {
            return DefaultBrush;
        }

        var currentHour = DateTime.Now.Hour;
        return hour == currentHour ? CurrentHourBrush : DefaultBrush;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("CurrentHourBrushConverter does not support ConvertBack");
    }
}

/// <summary>
/// Converts a sales value to a bar height for chart visualization.
/// ConverterParameter specifies the maximum height in pixels (default: 200).
/// Binds to MultiBinding with sales value and max sales value for proper scaling.
/// </summary>
public class SalesToHeightConverter : IValueConverter, IMultiValueConverter
{
    private const double DefaultMaxHeight = 200.0;
    private const double MinHeight = 4.0; // Minimum visible height

    /// <inheritdoc />
    /// <remarks>
    /// Single value conversion uses ConverterParameter for max height.
    /// Value should be normalized (0-1) or use MultiValue binding for raw values.
    /// </remarks>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var maxHeight = DefaultMaxHeight;
        if (parameter != null && double.TryParse(parameter.ToString(), out var parsedMaxHeight))
        {
            maxHeight = parsedMaxHeight;
        }

        var salesValue = value switch
        {
            decimal d => (double)d,
            double db => db,
            int i => i,
            float f => f,
            _ => 0.0
        };

        // If value is normalized (0-1), scale directly
        if (salesValue is >= 0 and <= 1)
        {
            return Math.Max(salesValue * maxHeight, salesValue > 0 ? MinHeight : 0);
        }

        // Otherwise treat as raw value - assumes max of 100 for percentage
        var normalizedValue = Math.Min(salesValue / 100.0, 1.0);
        return Math.Max(normalizedValue * maxHeight, salesValue > 0 ? MinHeight : 0);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Multi-value conversion expects [salesValue, maxSalesValue].
    /// ConverterParameter specifies the max height in pixels.
    /// </remarks>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return 0.0;
        }

        var maxHeight = DefaultMaxHeight;
        if (parameter != null && double.TryParse(parameter.ToString(), out var parsedMaxHeight))
        {
            maxHeight = parsedMaxHeight;
        }

        var salesValue = ConvertToDouble(values[0]);
        var maxSalesValue = ConvertToDouble(values[1]);

        if (maxSalesValue <= 0)
        {
            return salesValue > 0 ? MinHeight : 0.0;
        }

        var normalizedValue = Math.Min(salesValue / maxSalesValue, 1.0);
        return Math.Max(normalizedValue * maxHeight, salesValue > 0 ? MinHeight : 0);
    }

    private static double ConvertToDouble(object value)
    {
        return value switch
        {
            decimal d => (double)d,
            double db => db,
            int i => i,
            float f => f,
            long l => l,
            _ => 0.0
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("SalesToHeightConverter does not support ConvertBack");
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("SalesToHeightConverter does not support ConvertBack");
    }
}

/// <summary>
/// Converts connectivity status enum to appropriate color brush.
/// Online = Green, Degraded = Yellow, Offline = Red.
/// </summary>
public class ConnectivityStatusToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush OnlineBrush = new(Color.FromRgb(34, 197, 94));   // Green-500
    private static readonly SolidColorBrush DegradedBrush = new(Color.FromRgb(234, 179, 8)); // Yellow-500
    private static readonly SolidColorBrush OfflineBrush = new(Color.FromRgb(239, 68, 68));  // Red-500

    static ConnectivityStatusToColorConverter()
    {
        OnlineBrush.Freeze();
        DegradedBrush.Freeze();
        OfflineBrush.Freeze();
    }

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();

        return status switch
        {
            "online" => OnlineBrush,
            "degraded" => DegradedBrush,
            "offline" => OfflineBrush,
            _ => OfflineBrush
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts connectivity status enum to status text.
/// </summary>
public class ConnectivityStatusToTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();

        return status switch
        {
            "online" => "Connected",
            "degraded" => "Limited Connectivity",
            "offline" => "Offline",
            _ => "Unknown"
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
