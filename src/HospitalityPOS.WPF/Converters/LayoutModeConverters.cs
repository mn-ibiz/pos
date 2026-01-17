using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts PosLayoutMode to visibility for layout-specific UI elements.
/// </summary>
public class LayoutModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PosLayoutMode currentLayout || parameter is not string targetLayoutStr)
        {
            return Visibility.Visible;
        }

        var targetLayout = targetLayoutStr.ToLowerInvariant() switch
        {
            "restaurant" => PosLayoutMode.Restaurant,
            "supermarket" or "retail" => PosLayoutMode.Supermarket,
            _ => PosLayoutMode.Restaurant
        };

        return currentLayout == targetLayout ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts business mode to visibility for hybrid mode elements.
/// </summary>
public class IsHybridModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BusinessMode mode)
        {
            return mode == BusinessMode.Hybrid ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts PosLayoutMode to icon for toggle button.
/// </summary>
public class LayoutModeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PosLayoutMode layout)
        {
            return layout switch
            {
                PosLayoutMode.Restaurant => "\uE8BD", // Restaurant icon
                PosLayoutMode.Supermarket => "\uE7BF", // Shopping cart icon
                _ => "\uE8BD"
            };
        }

        return "\uE8BD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts PosLayoutMode to display text.
/// </summary>
public class LayoutModeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PosLayoutMode layout)
        {
            return layout switch
            {
                PosLayoutMode.Restaurant => "Restaurant Mode",
                PosLayoutMode.Supermarket => "Retail Mode",
                _ => "Unknown Mode"
            };
        }

        return "Unknown Mode";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts layout mode to toggle button text (shows opposite mode).
/// </summary>
public class LayoutModeToToggleTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PosLayoutMode layout)
        {
            return layout switch
            {
                PosLayoutMode.Restaurant => "Switch to Retail",
                PosLayoutMode.Supermarket => "Switch to Restaurant",
                _ => "Switch Layout"
            };
        }

        return "Switch Layout";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
