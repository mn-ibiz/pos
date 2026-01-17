using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class ChartOfAccountsView : UserControl
{
    public ChartOfAccountsView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is ChartOfAccountsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
