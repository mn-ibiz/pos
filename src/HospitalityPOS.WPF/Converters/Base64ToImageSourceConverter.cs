using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a base64 encoded string to an ImageSource.
/// </summary>
public class Base64ToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string base64String || string.IsNullOrEmpty(base64String))
        {
            return null;
        }

        try
        {
            var imageBytes = System.Convert.FromBase64String(base64String);
            var image = new BitmapImage();

            using var stream = new MemoryStream(imageBytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
