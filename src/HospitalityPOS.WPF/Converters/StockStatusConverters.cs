using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts product stock information to a StockStatus enum.
/// </summary>
public class ProductToStockStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return StockStatus.InStock;

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        if (currentStock <= 0)
            return StockStatus.OutOfStock;

        if (minStock.HasValue && currentStock <= minStock.Value)
            return StockStatus.LowStock;

        return StockStatus.InStock;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts StockStatus to a display text.
/// </summary>
public class StockStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return "Unknown";

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        if (currentStock <= 0)
            return "Out";

        if (minStock.HasValue && currentStock <= minStock.Value)
            return "Low";

        return "OK";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts StockStatus to a background color.
/// </summary>
public class StockStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return StockColors.InStockBrush;

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        if (currentStock <= 0)
            return StockColors.OutOfStockBrush;

        if (minStock.HasValue && currentStock <= minStock.Value)
            return StockColors.LowStockBrush;

        return StockColors.InStockBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a StockStatus enum directly to a background color brush.
/// Use this when binding directly to a StockStatus property.
/// </summary>
public class StockStatusEnumToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StockStatus status)
            return StockColors.InStockBrush;

        return status switch
        {
            StockStatus.OutOfStock => StockColors.OutOfStockBrush,
            StockStatus.LowStock => StockColors.LowStockBrush,
            _ => StockColors.InStockBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a StockStatus enum to display text.
/// Use this when binding directly to a StockStatus property.
/// </summary>
public class StockStatusEnumToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StockStatus status)
            return "-";

        return status switch
        {
            StockStatus.OutOfStock => "OUT",
            StockStatus.LowStock => "LOW",
            StockStatus.InStock => "OK",
            _ => "-"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts StockStatus to Visibility. Shows element for low stock or out-of-stock.
/// </summary>
public class StockAlertToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return Visibility.Collapsed;

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        // Show if out of stock or low stock
        if (currentStock <= 0)
            return Visibility.Visible;

        if (minStock.HasValue && currentStock <= minStock.Value)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts product to out-of-stock visibility.
/// Shows element only when product is out of stock.
/// </summary>
public class OutOfStockToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return Visibility.Collapsed;

        var currentStock = product.Inventory?.CurrentStock ?? 0;

        return currentStock <= 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts product to low stock visibility.
/// Shows element only when product is low on stock (not out of stock).
/// </summary>
public class LowStockToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Product product)
            return Visibility.Collapsed;

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        var minStock = product.MinStockLevel;

        // Low stock: above 0 but below or equal to min level
        if (currentStock > 0 && minStock.HasValue && currentStock <= minStock.Value)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
