using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CombinedXReportView.xaml
/// </summary>
public partial class CombinedXReportView : Window
{
    public CombinedXReportView(CombinedXReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += CombinedXReportView_Loaded;
    }

    private async void CombinedXReportView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CombinedXReportViewModel vm)
        {
            await vm.OnNavigatedToAsync();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
