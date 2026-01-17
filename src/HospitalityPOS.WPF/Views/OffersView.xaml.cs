using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for OffersView.xaml
/// </summary>
public partial class OffersView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OffersView"/> class.
    /// </summary>
    public OffersView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the view with the ViewModel.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public void Initialize(OffersViewModel viewModel)
    {
        DataContext = viewModel;
    }
}
