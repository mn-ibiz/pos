using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for FeatureSettingsView.xaml
/// </summary>
public partial class FeatureSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureSettingsView"/> class.
    /// </summary>
    public FeatureSettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the view with its ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    public async Task InitializeAsync(FeatureSettingsViewModel viewModel)
    {
        DataContext = viewModel;
        await viewModel.InitializeAsync();
    }
}
