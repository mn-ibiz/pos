using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for TableDialog.xaml
/// </summary>
public partial class TableDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableDialog"/> class.
    /// </summary>
    public TableDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TableDialogViewModel viewModel)
        {
            viewModel.CloseDialog = result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
