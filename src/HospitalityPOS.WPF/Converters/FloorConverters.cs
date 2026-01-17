using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a TableStatus enum to a color brush.
/// </summary>
public class TableStatusColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TableStatus status)
        {
            return status switch
            {
                TableStatus.Available => new SolidColorBrush(Color.FromRgb(76, 175, 80)),     // Green
                TableStatus.Occupied => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Red
                TableStatus.Reserved => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Yellow/Amber
                TableStatus.Unavailable => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
                _ => new SolidColorBrush(Colors.White)
            };
        }

        return new SolidColorBrush(Colors.White);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a TableShape enum to a corner radius.
/// </summary>
public class TableShapeRadiusConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TableShape shape)
        {
            return shape switch
            {
                TableShape.Round => new CornerRadius(50),      // Fully rounded (circle)
                TableShape.Square => new CornerRadius(4),      // Slightly rounded corners
                TableShape.Rectangle => new CornerRadius(4),   // Slightly rounded corners
                _ => new CornerRadius(4)
            };
        }

        return new CornerRadius(4);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a grid cell count to a pixel size.
/// </summary>
public class GridCellSizeConverter : IValueConverter
{
    private const double DefaultCellSize = 60;

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cellCount)
        {
            var cellSize = parameter is double size ? size : DefaultCellSize;
            return cellCount * cellSize;
        }

        return DefaultCellSize;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pixels)
        {
            var cellSize = parameter is double size ? size : DefaultCellSize;
            return (int)(pixels / cellSize);
        }

        return 0;
    }
}

/// <summary>
/// Converts a hex color string to a Color.
/// </summary>
public class HexToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            try
            {
                // Remove # if present
                if (hexColor.StartsWith('#'))
                {
                    hexColor = hexColor[1..];
                }

                if (hexColor.Length == 6)
                {
                    var r = System.Convert.ToByte(hexColor[..2], 16);
                    var g = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    var b = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        return Colors.Gray;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return "#808080";
    }
}

/// <summary>
/// Converts null values to Visibility with inverse logic (null = Visible, non-null = Collapsed).
/// </summary>
public class InverseNullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Compares two values for equality. Returns true if equal, false otherwise.
/// Useful for radio button bindings.
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;

        return Equals(values[0], values[1]);
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
