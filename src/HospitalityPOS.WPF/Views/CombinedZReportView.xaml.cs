using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CombinedZReportView.xaml
/// </summary>
public partial class CombinedZReportView : Window
{
    public CombinedZReportView(CombinedZReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += CombinedZReportView_Loaded;
    }

    private async void CombinedZReportView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CombinedZReportViewModel vm)
        {
            await vm.OnNavigatedToAsync();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Converts a boolean to a color (for ready/not ready status).
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isReady)
        {
            return new SolidColorBrush(isReady ? Color.FromRgb(0x4C, 0xAF, 0x50) : Color.FromRgb(0xFF, 0x6B, 0x6B));
        }
        return new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a boolean to "All Ready" or "Issues Found" text.
/// </summary>
public class BoolToReadyTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isReady)
        {
            return isReady ? "All Terminals Ready" : "Issues Found";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a boolean to opacity (for enabled/disabled buttons).
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ? 1.0 : 0.5;
        }
        return 1.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
