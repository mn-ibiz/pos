using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for LoginView.xaml
/// </summary>
public partial class LoginView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginView"/> class.
    /// </summary>
    public LoginView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the PasswordChanged event to sync with the ViewModel.
    /// </summary>
    /// <remarks>
    /// PasswordBox doesn't support binding for security reasons,
    /// so we use this event handler to sync the password with the ViewModel.
    /// </remarks>
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = ((PasswordBox)sender).Password;
        }
    }
}
