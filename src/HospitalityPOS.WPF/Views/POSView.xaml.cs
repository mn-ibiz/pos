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

    /// <summary>
    /// Handles keyboard navigation in the search input for dropdown selection.
    /// </summary>
    private void SearchInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not POSViewModel vm) return;

        if (e.Key == Key.Down)
        {
            if (vm.SearchResults.Count > 0)
            {
                vm.MoveSearchSelectionDownCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            if (vm.SearchResults.Count > 0)
            {
                vm.MoveSearchSelectionUpCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Handles mouse click on search result to select and add the product.
    /// </summary>
    private void SearchResultsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is POSViewModel vm && vm.SelectedSearchResult != null)
        {
            vm.SelectSearchResultCommand.Execute(vm.SelectedSearchResult);
            // Return focus to search input
            RetailBarcodeInput?.Focus();
        }
    }

    /// <summary>
    /// Handles click on the favorites header to toggle panel expansion.
    /// </summary>
    private void FavoritesHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is POSViewModel vm)
        {
            vm.ToggleFavoritesPanelCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles click on the customer search overlay background to close the panel.
    /// </summary>
    private void CustomerSearchOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is POSViewModel vm)
        {
            vm.IsLoyaltyPanelVisible = false;
        }
    }

    /// <summary>
    /// Stops the click from propagating to the overlay when clicking on the panel itself.
    /// </summary>
    private void CustomerSearchPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
