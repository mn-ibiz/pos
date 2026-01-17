using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for SetupWizardView.xaml
/// </summary>
public partial class SetupWizardView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetupWizardView"/> class.
    /// </summary>
    public SetupWizardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the view with its ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    public void Initialize(SetupWizardViewModel viewModel)
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// Handles mode card click.
    /// </summary>
    private void ModeCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is BusinessModeOption option &&
            DataContext is SetupWizardViewModel viewModel)
        {
            viewModel.SelectModeCommand.Execute(option);
        }
    }
}
