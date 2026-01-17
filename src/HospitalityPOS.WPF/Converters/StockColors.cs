using System.Windows.Media;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Shared color definitions for stock status display.
/// </summary>
public static class StockColors
{
    /// <summary>
    /// Green color for in-stock items.
    /// </summary>
    public static readonly SolidColorBrush InStockBrush;

    /// <summary>
    /// Orange/Yellow color for low stock items.
    /// </summary>
    public static readonly SolidColorBrush LowStockBrush;

    /// <summary>
    /// Red color for out-of-stock items.
    /// </summary>
    public static readonly SolidColorBrush OutOfStockBrush;

    static StockColors()
    {
        InStockBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
        LowStockBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
        OutOfStockBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

        // Freeze brushes for performance
        InStockBrush.Freeze();
        LowStockBrush.Freeze();
        OutOfStockBrush.Freeze();
    }
}
