using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class FinancialReportsView : UserControl
{
    public FinancialReportsView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is FinancialReportsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
