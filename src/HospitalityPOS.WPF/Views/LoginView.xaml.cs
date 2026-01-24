using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Interfaces;
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
        Loaded += LoginView_Loaded;
    }

    private async void LoginView_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateConnectionStatusAsync();
    }

    /// <summary>
    /// Opens the database configuration window.
    /// </summary>
    private async void DatabaseConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var connectionService = App.Current.Services?.GetService<IDatabaseConnectionService>();
        if (connectionService == null)
        {
            MessageBox.Show(
                "Database connection service not available.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var configWindow = new DatabaseConfigurationWindow(connectionService)
        {
            Owner = Window.GetWindow(this)
        };

        var result = configWindow.ShowDialog();

        if (result == true && configWindow.ConfigurationSaved)
        {
            // Configuration was saved, update status and potentially restart app
            await UpdateConnectionStatusAsync();

            var restartResult = MessageBox.Show(
                "Database connection settings have been updated.\n\n" +
                "The application needs to restart to apply the new settings.\n\n" +
                "Restart now?",
                "Restart Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (restartResult == MessageBoxResult.Yes)
            {
                RestartApplication();
            }
        }
    }

    /// <summary>
    /// Updates the connection status indicator.
    /// </summary>
    private async Task UpdateConnectionStatusAsync()
    {
        var connectionService = App.Current.Services?.GetService<IDatabaseConnectionService>();
        if (connectionService == null)
        {
            SetConnectionStatus(false, "Service unavailable");
            return;
        }

        if (!connectionService.HasConnectionConfiguration)
        {
            SetConnectionStatus(false, "Not configured");
            return;
        }

        try
        {
            var result = await connectionService.TestCurrentConnectionAsync();
            if (result.IsSuccess)
            {
                SetConnectionStatus(true, "Connected");
            }
            else
            {
                SetConnectionStatus(false, "Connection failed");
            }
        }
        catch
        {
            SetConnectionStatus(false, "Connection error");
        }
    }

    private void SetConnectionStatus(bool isConnected, string statusText)
    {
        Dispatcher.Invoke(() =>
        {
            ConnectionStatusIndicator.Fill = isConnected
                ? new SolidColorBrush(Color.FromRgb(34, 197, 94))  // Green
                : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red

            ConnectionStatusText.Text = statusText;
        });
    }

    private void RestartApplication()
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(exePath))
        {
            System.Diagnostics.Process.Start(exePath);
            Application.Current.Shutdown();
        }
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
