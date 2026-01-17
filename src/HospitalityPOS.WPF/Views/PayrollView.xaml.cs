using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class PayrollView : UserControl
{
    public PayrollView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is PayrollViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
