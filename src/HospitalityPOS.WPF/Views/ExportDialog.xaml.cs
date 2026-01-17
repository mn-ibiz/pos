using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for ExportDialog.xaml
/// </summary>
public partial class ExportDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDialog"/> class.
    /// </summary>
    public ExportDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExportDialogViewModel viewModel)
        {
            viewModel.CloseDialog = result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
