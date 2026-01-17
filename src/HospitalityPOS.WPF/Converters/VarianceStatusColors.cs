using System.Windows.Media;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Shared color definitions for variance status display (short/over/exact).
/// </summary>
public static class VarianceStatusColors
{
    /// <summary>
    /// Red foreground color for short (negative) variance.
    /// </summary>
    public static readonly SolidColorBrush ShortForeground;

    /// <summary>
    /// Dark red background color for short (negative) variance.
    /// </summary>
    public static readonly SolidColorBrush ShortBackground;

    /// <summary>
    /// Orange foreground color for over (positive) variance.
    /// </summary>
    public static readonly SolidColorBrush OverForeground;

    /// <summary>
    /// Dark orange background color for over (positive) variance.
    /// </summary>
    public static readonly SolidColorBrush OverBackground;

    /// <summary>
    /// Green foreground color for exact (zero) variance.
    /// </summary>
    public static readonly SolidColorBrush ExactForeground;

    /// <summary>
    /// Dark green background color for exact (zero) variance.
    /// </summary>
    public static readonly SolidColorBrush ExactBackground;

    static VarianceStatusColors()
    {
        ShortForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B")!);
        ShortBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D1F1F")!);
        OverForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB347")!);
        OverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D3D1F")!);
        ExactForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")!);
        ExactBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F4D2A")!);

        // Freeze brushes for performance
        ShortForeground.Freeze();
        ShortBackground.Freeze();
        OverForeground.Freeze();
        OverBackground.Freeze();
        ExactForeground.Freeze();
        ExactBackground.Freeze();
    }
}
