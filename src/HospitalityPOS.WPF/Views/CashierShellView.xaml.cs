using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CashierShellView.xaml
/// Full-screen cashier interface following Microsoft RMS design principles.
/// </summary>
public partial class CashierShellView : UserControl
{
    public CashierShellView()
    {
        InitializeComponent();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string digit)
        {
            if (DataContext is CashierShellViewModel vm)
            {
                vm.UnlockPin += digit;
            }
        }
    }

    private void ClearPin_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CashierShellViewModel vm)
        {
            vm.UnlockPin = string.Empty;
        }
    }

    private void BackspacePin_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CashierShellViewModel vm && !string.IsNullOrEmpty(vm.UnlockPin))
        {
            vm.UnlockPin = vm.UnlockPin[..^1];
        }
    }
}
