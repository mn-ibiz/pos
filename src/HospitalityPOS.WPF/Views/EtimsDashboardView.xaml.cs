using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class EtimsDashboardView : UserControl
{
    public EtimsDashboardView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is EtimsDashboardViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
