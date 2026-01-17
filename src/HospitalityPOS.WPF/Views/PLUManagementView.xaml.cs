using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// PLU code management view.
/// </summary>
public partial class PLUManagementView : UserControl
{
    public PLUManagementView(PLUManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PLUManagementViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
