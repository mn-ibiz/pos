using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class EmployeesView : UserControl
{
    public EmployeesView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is EmployeesViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
