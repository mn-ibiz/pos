using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Product search dialog for looking up and selecting products.
/// </summary>
public partial class ProductSearchDialog : Window
{
    private readonly IProductService _productService;
    private List<ProductSearchResult> _allProducts = [];
    private List<ProductSearchResult> _filteredProducts = [];
    private System.Timers.Timer? _searchDebounceTimer;

    /// <summary>
    /// Gets the selected product, or null if cancelled.
    /// </summary>
    public ProductSearchResult? SelectedProduct { get; private set; }

    /// <summary>
    /// Initializes a new instance of the ProductSearchDialog.
    /// </summary>
    public ProductSearchDialog(IProductService productService, string? initialSearchText = null)
    {
        InitializeComponent();
        _productService = productService;

        Loaded += async (s, e) =>
        {
            await LoadProductsAsync();
            SearchInput.Focus();

            if (!string.IsNullOrEmpty(initialSearchText))
            {
                SearchInput.Text = initialSearchText;
                FilterProducts(initialSearchText);
            }
            else
            {
                UpdateEmptyState(true, "Type to search for products");
            }
        };

        ResultsListBox.SelectionChanged += (s, e) =>
        {
            SelectButton.IsEnabled = ResultsListBox.SelectedItem != null;
        };
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            _allProducts = products.Select(p => new ProductSearchResult
            {
                Id = p.Id,
                Code = p.Code ?? "",
                Barcode = p.Barcode ?? "",
                Name = p.Name,
                Price = p.SellingPrice,
                CurrentStock = p.Inventory?.CurrentStock ?? 0,
                IsOutOfStock = p.Inventory == null || p.Inventory.CurrentStock <= 0
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading products: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterProducts(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredProducts = [];
            ResultsListBox.ItemsSource = _filteredProducts;
            UpdateEmptyState(true, "Type to search for products");
            return;
        }

        var search = searchText.ToLowerInvariant().Trim();

        _filteredProducts = _allProducts
            .Where(p =>
                p.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p =>
            {
                // Exact code match first
                if (p.Code.Equals(search, StringComparison.OrdinalIgnoreCase))
                    return 0;
                // Exact barcode match second
                if (p.Barcode.Equals(search, StringComparison.OrdinalIgnoreCase))
                    return 1;
                // Starts with search term
                if (p.Code.StartsWith(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                    return 2;
                // Contains search term
                return 3;
            })
            .ThenBy(p => p.Name)
            .Take(100) // Limit results for performance
            .ToList();

        ResultsListBox.ItemsSource = _filteredProducts;
        ResultCountText.Text = $"{_filteredProducts.Count} items found";

        if (_filteredProducts.Count == 0)
        {
            UpdateEmptyState(true, "No products found matching your search");
        }
        else
        {
            UpdateEmptyState(false, "");
            // Auto-select first item
            ResultsListBox.SelectedIndex = 0;
        }
    }

    private void UpdateEmptyState(bool show, string message)
    {
        EmptyState.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        EmptyStateText.Text = message;
        ResultCountText.Text = show ? "0 items found" : $"{_filteredProducts.Count} items found";
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Update placeholder visibility
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchInput.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Debounce search
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Dispose();

        _searchDebounceTimer = new System.Timers.Timer(200);
        _searchDebounceTimer.Elapsed += (s, args) =>
        {
            _searchDebounceTimer?.Stop();
            Dispatcher.Invoke(() => FilterProducts(SearchInput.Text));
        };
        _searchDebounceTimer.AutoReset = false;
        _searchDebounceTimer.Start();
    }

    private void SearchInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (ResultsListBox.Items.Count > 0)
            {
                if (ResultsListBox.SelectedItem == null)
                    ResultsListBox.SelectedIndex = 0;
                SelectProduct();
            }
        }
        else if (e.Key == Key.Down)
        {
            if (ResultsListBox.Items.Count > 0)
            {
                ResultsListBox.Focus();
                if (ResultsListBox.SelectedIndex < 0)
                    ResultsListBox.SelectedIndex = 0;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void ResultsListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SelectProduct();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        SelectProduct();
    }

    private void SelectProduct()
    {
        if (ResultsListBox.SelectedItem is ProductSearchResult product)
        {
            SelectedProduct = product;
            DialogResult = true;
            Close();
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        FilterProducts(SearchInput.Text);
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectProduct();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Toggle maximize on double-click (optional)
        }
        else
        {
            DragMove();
        }
    }
}

/// <summary>
/// Simplified product data for search results.
/// </summary>
public class ProductSearchResult
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsOutOfStock { get; set; }
}
