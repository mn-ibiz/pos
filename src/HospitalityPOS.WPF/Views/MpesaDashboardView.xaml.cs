using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// M-Pesa dashboard view.
/// </summary>
public partial class MpesaDashboardView : UserControl
{
    public MpesaDashboardView(MpesaDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MpesaDashboardViewModel vm)
        {
            await vm.LoadDashboardCommand.ExecuteAsync(null);
        }
    }
}
