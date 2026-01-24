using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Database connection configuration window with SQL Server discovery.
/// </summary>
public partial class DatabaseConfigurationWindow : Window
{
    private readonly IDatabaseConnectionService _connectionService;
    private CancellationTokenSource? _scanCancellation;
    private bool _connectionTested;

    /// <summary>
    /// Gets whether the configuration was saved successfully.
    /// </summary>
    public bool ConfigurationSaved { get; private set; }

    /// <summary>
    /// Gets the saved connection string.
    /// </summary>
    public string? ConnectionString { get; private set; }

    public DatabaseConfigurationWindow(IDatabaseConnectionService connectionService)
    {
        InitializeComponent();
        _connectionService = connectionService;

        // Load existing configuration if available
        LoadExistingConfiguration();

        // Start initial scan
        _ = ScanForServersAsync();
    }

    private void LoadExistingConfiguration()
    {
        var config = _connectionService.LoadConnectionConfiguration();
        if (config == null) return;

        ServerComboBox.Text = config.FullServerName;
        DatabaseComboBox.Text = config.Database;

        if (config.UseWindowsAuthentication)
        {
            WindowsAuthRadio.IsChecked = true;
        }
        else
        {
            SqlAuthRadio.IsChecked = true;
            UsernameTextBox.Text = config.Username;
            PasswordBox.Password = config.Password;
        }
    }

    private async Task ScanForServersAsync()
    {
        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();

        try
        {
            ScanningPanel.Visibility = Visibility.Visible;
            ScanButton.IsEnabled = false;

            var servers = await _connectionService.DiscoverSqlServersAsync(_scanCancellation.Token);

            ServerComboBox.ItemsSource = servers;

            if (servers.Any() && string.IsNullOrEmpty(ServerComboBox.Text))
            {
                ServerComboBox.SelectedIndex = 0;
            }
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled
        }
        catch (Exception ex)
        {
            ShowTestResult(false, "Network scan failed", ex.Message);
        }
        finally
        {
            ScanningPanel.Visibility = Visibility.Collapsed;
            ScanButton.IsEnabled = true;
        }
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        await ScanForServersAsync();
    }

    private async void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServerComboBox.SelectedItem is SqlServerInstance instance)
        {
            // Try to load databases from the server
            await LoadDatabasesAsync(instance);
        }

        // Reset test status when server changes
        ResetTestStatus();
    }

    private async Task LoadDatabasesAsync(SqlServerInstance instance)
    {
        try
        {
            var config = BuildConfiguration();
            config.Database = "master";

            var databases = await _connectionService.GetDatabasesAsync(config);

            DatabaseComboBox.ItemsSource = databases;

            // Select HospitalityPOS if it exists
            if (databases.Contains("HospitalityPOS"))
            {
                DatabaseComboBox.SelectedItem = "HospitalityPOS";
            }
            else
            {
                DatabaseComboBox.Text = "HospitalityPOS";
            }
        }
        catch
        {
            // Failed to load databases, user can type manually
            DatabaseComboBox.ItemsSource = null;
        }
    }

    private void AuthenticationChanged(object sender, RoutedEventArgs e)
    {
        SqlAuthPanel.Visibility = SqlAuthRadio.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;

        ResetTestStatus();
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var config = BuildConfiguration();

        if (string.IsNullOrWhiteSpace(config.Server))
        {
            ShowTestResult(false, "Server name is required", "Please enter or select a SQL Server.");
            return;
        }

        TestButton.IsEnabled = false;
        TestButton.Content = "Testing...";

        try
        {
            var result = await _connectionService.TestConnectionAsync(config);

            if (result.IsSuccess)
            {
                _connectionTested = true;
                SaveButton.IsEnabled = true;

                ShowTestResult(true,
                    "Connection successful!",
                    $"Server: {result.ServerVersion}\nResponse time: {result.ConnectionTimeMs}ms");
            }
            else
            {
                _connectionTested = false;
                SaveButton.IsEnabled = false;

                ShowTestResult(false, "Connection failed", result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            ShowTestResult(false, "Connection error", ex.Message);
        }
        finally
        {
            TestButton.IsEnabled = true;
            TestButton.Content = "Test Connection";
        }
    }

    private void ShowTestResult(bool success, string message, string? details = null)
    {
        TestResultBorder.Visibility = Visibility.Visible;

        if (success)
        {
            TestResultBorder.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // Green background
            TestResultIcon.Text = "✅";
            TestResultIcon.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
            TestResultText.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        else
        {
            TestResultBorder.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Red background
            TestResultIcon.Text = "❌";
            TestResultIcon.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            TestResultText.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        TestResultText.Text = message;
        TestResultDetails.Text = details ?? string.Empty;
        TestResultDetails.Visibility = string.IsNullOrEmpty(details) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ResetTestStatus()
    {
        _connectionTested = false;
        SaveButton.IsEnabled = false;
        TestResultBorder.Visibility = Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_connectionTested)
        {
            MessageBox.Show(
                "Please test the connection before saving.",
                "Test Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var config = BuildConfiguration();
            _connectionService.SaveConnectionConfiguration(config);

            ConfigurationSaved = true;
            ConnectionString = config.BuildConnectionString();

            MessageBox.Show(
                "Database connection configuration saved successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save configuration: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private DatabaseConnectionConfig BuildConfiguration()
    {
        var serverText = ServerComboBox.SelectedItem is SqlServerInstance instance
            ? instance.DisplayName
            : ServerComboBox.Text;

        // Parse server and instance name
        string server;
        string? instanceName = null;

        if (serverText.Contains('\\'))
        {
            var parts = serverText.Split('\\', 2);
            server = parts[0];
            instanceName = parts[1];
        }
        else
        {
            server = serverText;
        }

        return new DatabaseConnectionConfig
        {
            Server = server,
            InstanceName = instanceName,
            Database = DatabaseComboBox.Text ?? "HospitalityPOS",
            UseWindowsAuthentication = WindowsAuthRadio.IsChecked == true,
            Username = SqlAuthRadio.IsChecked == true ? UsernameTextBox.Text : null,
            Password = SqlAuthRadio.IsChecked == true ? PasswordBox.Password : null
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        _scanCancellation?.Cancel();
        _scanCancellation?.Dispose();
        base.OnClosed(e);
    }
}
