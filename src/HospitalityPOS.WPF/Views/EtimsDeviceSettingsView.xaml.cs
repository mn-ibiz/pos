using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class EtimsDeviceSettingsView : UserControl
{
    public EtimsDeviceSettingsView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is EtimsDeviceSettingsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
