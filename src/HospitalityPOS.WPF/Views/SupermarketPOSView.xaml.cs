using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Code-behind for SupermarketPOSView - Microsoft RMS-style retail POS interface.
/// Handles keyboard navigation and search box focus management.
/// </summary>
public partial class SupermarketPOSView : UserControl
{
    public SupermarketPOSView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-focus the search box when the view loads
        SearchBox.Focus();
    }

    /// <summary>
    /// Handles keyboard navigation in the search box.
    /// </summary>
    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not POSViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                // If there's a selected search result, add it
                if (viewModel.SelectedSearchResult != null)
                {
                    viewModel.SelectSearchResultCommand.Execute(viewModel.SelectedSearchResult);
                    SearchBox.Clear();
                    e.Handled = true;
                }
                // If search results exist but none selected, select the first one
                else if (viewModel.SearchResults.Count > 0)
                {
                    viewModel.SelectedSearchResult = viewModel.SearchResults[0];
                    viewModel.SelectSearchResultCommand.Execute(viewModel.SelectedSearchResult);
                    SearchBox.Clear();
                    e.Handled = true;
                }
                // Otherwise, try to process as barcode
                else if (!string.IsNullOrWhiteSpace(viewModel.BarcodeInput))
                {
                    viewModel.ProcessSearchEnterCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            case Key.Down:
                // Move focus to search results list
                if (viewModel.IsSearchDropdownOpen && viewModel.SearchResults.Count > 0)
                {
                    SearchResultsList.Focus();
                    if (viewModel.SelectedSearchResult == null && viewModel.SearchResults.Count > 0)
                    {
                        viewModel.SelectedSearchResult = viewModel.SearchResults[0];
                    }
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                // Clear search and close dropdown
                SearchBox.Clear();
                viewModel.IsSearchDropdownOpen = false;
                e.Handled = true;
                break;

            // Handle numeric input for quantity prefix
            case Key.D0:
            case Key.D1:
            case Key.D2:
            case Key.D3:
            case Key.D4:
            case Key.D5:
            case Key.D6:
            case Key.D7:
            case Key.D8:
            case Key.D9:
            case Key.NumPad0:
            case Key.NumPad1:
            case Key.NumPad2:
            case Key.NumPad3:
            case Key.NumPad4:
            case Key.NumPad5:
            case Key.NumPad6:
            case Key.NumPad7:
            case Key.NumPad8:
            case Key.NumPad9:
                // Let the TextBox handle numeric input normally for barcode/search
                break;

            case Key.Multiply:
                // Asterisk (*) can be used to set quantity: e.g., "3*" means quantity 3
                // This is handled by the ViewModel
                break;
        }
    }

    /// <summary>
    /// Handles text changes in the search box for real-time filtering.
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // The ViewModel handles debounced search via property change
        // This is just for additional UI handling if needed
    }

    /// <summary>
    /// Handles keyboard navigation in the search results list.
    /// </summary>
    private void SearchResultsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not POSViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                // Add the selected item to the order
                if (viewModel.SelectedSearchResult != null)
                {
                    viewModel.SelectSearchResultCommand.Execute(viewModel.SelectedSearchResult);
                    SearchBox.Clear();
                    SearchBox.Focus();
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                // Return focus to search box
                SearchBox.Focus();
                e.Handled = true;
                break;

            case Key.Up:
                // If at the top of the list, return focus to search box
                if (SearchResultsList.SelectedIndex == 0)
                {
                    SearchBox.Focus();
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handles double-click on search results to add item.
    /// </summary>
    private void SearchResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is POSViewModel viewModel && viewModel.SelectedSearchResult != null)
        {
            viewModel.SelectSearchResultCommand.Execute(viewModel.SelectedSearchResult);
            SearchBox.Clear();
            SearchBox.Focus();
        }
    }
}
