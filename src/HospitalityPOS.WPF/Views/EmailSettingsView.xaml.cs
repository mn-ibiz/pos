using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for EmailSettingsView.xaml
/// </summary>
public partial class EmailSettingsView : UserControl
{
    public EmailSettingsView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<EmailSettingsViewModel>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is EmailSettingsViewModel viewModel)
        {
            // Bind password box (WPF PasswordBox doesn't support binding)
            SmtpPasswordBox.PasswordChanged += (s, args) =>
            {
                viewModel.SmtpPassword = SmtpPasswordBox.Password;
            };

            // Set initial password if already configured
            if (!string.IsNullOrEmpty(viewModel.SmtpPassword))
            {
                SmtpPasswordBox.Password = viewModel.SmtpPassword;
            }

            await viewModel.InitializeAsync();
        }
    }
}
