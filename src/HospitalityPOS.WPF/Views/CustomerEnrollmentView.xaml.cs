using System.Windows.Controls;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CustomerEnrollmentView.xaml
/// </summary>
public partial class CustomerEnrollmentView : UserControl
{
    public CustomerEnrollmentView()
    {
        InitializeComponent();

        // Set focus to phone number field when loaded
        Loaded += (s, e) =>
        {
            PhoneNumberTextBox.Focus();
        };
    }
}
