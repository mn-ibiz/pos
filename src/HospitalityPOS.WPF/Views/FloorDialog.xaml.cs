using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for FloorDialog.xaml
/// </summary>
public partial class FloorDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloorDialog"/> class.
    /// </summary>
    public FloorDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FloorDialogViewModel viewModel)
        {
            viewModel.CloseDialog = result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
