using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CashDrawerSettingsView.xaml
/// </summary>
public partial class CashDrawerSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CashDrawerSettingsView"/> class.
    /// </summary>
    public CashDrawerSettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the view with its ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    public async Task InitializeAsync(CashDrawerSettingsViewModel viewModel)
    {
        DataContext = viewModel;
        await viewModel.InitializeAsync();
    }
}
