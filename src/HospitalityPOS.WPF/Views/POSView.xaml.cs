using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for POSView.xaml
/// </summary>
public partial class POSView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="POSView"/> class.
    /// </summary>
    public POSView()
    {
        InitializeComponent();
        Loaded += POSView_Loaded;
    }

    /// <summary>
    /// Focuses the barcode input when the view loads.
    /// </summary>
    private void POSView_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus the search field for immediate barcode scanning
        RetailBarcodeInput?.Focus();
        Keyboard.Focus(RetailBarcodeInput);
    }

    /// <summary>
    /// Handles click on the held orders overlay background to close the panel.
    /// </summary>
    private void HeldOrdersOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is POSViewModel vm)
        {
            vm.IsHeldOrdersPanelVisible = false;
        }
    }

    /// <summary>
    /// Stops the click from propagating to the overlay when clicking on the panel itself.
    /// </summary>
    private void HeldOrdersPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
