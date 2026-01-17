using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a page number and current page number to determine if page is selected.
/// Returns true if the page number matches the current page number.
/// </summary>
public class IsCurrentPageConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
            return false;

        if (values[0] is int pageNumber && values[1] is int currentPage)
        {
            return pageNumber == currentPage;
        }

        return false;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
