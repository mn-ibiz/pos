using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for PrinterSettingsView.xaml
/// </summary>
public partial class PrinterSettingsView : UserControl
{
    public PrinterSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PrinterSettingsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
