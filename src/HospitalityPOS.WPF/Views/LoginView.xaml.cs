using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    /// <summary>
    /// Handles the KeyDown event on the PasswordBox to trigger login on Enter.
    /// </summary>
    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel viewModel)
        {
            if (viewModel.LoginCommand.CanExecute(null))
            {
                viewModel.LoginCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Handles the KeyDown event on the Username TextBox to trigger login on Enter.
    /// </summary>
    private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Move focus to password box
            PasswordBox.Focus();
        }
    }

    /// <summary>
    /// Fills test credentials for development/testing convenience.
    /// </summary>
    private void FillTestCredentials_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Username = "admin";
            viewModel.Password = "Admin@123";
            PasswordBox.Password = "Admin@123";
        }
    }
}
