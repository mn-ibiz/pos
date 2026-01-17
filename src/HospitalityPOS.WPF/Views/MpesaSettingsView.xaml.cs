using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// M-Pesa configuration settings view.
/// </summary>
public partial class MpesaSettingsView : UserControl
{
    public MpesaSettingsView(MpesaSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MpesaSettingsViewModel vm)
        {
            await vm.LoadConfigurationsCommand.ExecuteAsync(null);
        }
    }
}
