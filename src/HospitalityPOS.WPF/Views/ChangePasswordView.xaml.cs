using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for ChangePasswordView.xaml
/// </summary>
public partial class ChangePasswordView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordView"/> class.
    /// </summary>
    public ChangePasswordView()
    {
        InitializeComponent();
    }

    private void CurrentPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel)
        {
            viewModel.CurrentPassword = CurrentPasswordBox.Password;
        }
    }

    private void NewPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel)
        {
            viewModel.NewPassword = NewPasswordBox.Password;
        }
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel)
        {
            viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }
}
