using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for UserEditorView.xaml
/// </summary>
public partial class UserEditorView : UserControl
{
    public UserEditorView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is UserEditorViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is UserEditorViewModel viewModel)
        {
            viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }

    private void PinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is UserEditorViewModel viewModel)
        {
            viewModel.Pin = PinBox.Password;
        }
    }
}
