using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for KitchenPrinterSettingsView.xaml
/// </summary>
public partial class KitchenPrinterSettingsView : UserControl
{
    public KitchenPrinterSettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the view with its ViewModel.
    /// </summary>
    public async Task InitializeAsync(KitchenPrinterSettingsViewModel viewModel)
    {
        DataContext = viewModel;
        await viewModel.InitializeAsync();
    }
}
