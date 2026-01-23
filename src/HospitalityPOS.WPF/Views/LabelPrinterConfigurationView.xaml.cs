using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for LabelPrinterConfigurationView.xaml
/// </summary>
public partial class LabelPrinterConfigurationView : UserControl
{
    public LabelPrinterConfigurationView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LabelPrinterConfigurationViewModel viewModel)
        {
            await viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
