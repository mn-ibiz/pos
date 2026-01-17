using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Barcode and scale settings view.
/// </summary>
public partial class BarcodeSettingsView : UserControl
{
    public BarcodeSettingsView(BarcodeSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is BarcodeSettingsViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
