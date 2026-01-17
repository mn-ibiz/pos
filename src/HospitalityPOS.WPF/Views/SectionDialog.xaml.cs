using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for SectionDialog.xaml
/// </summary>
public partial class SectionDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SectionDialog"/> class.
    /// </summary>
    public SectionDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SectionDialogViewModel viewModel)
        {
            viewModel.CloseDialog = result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
