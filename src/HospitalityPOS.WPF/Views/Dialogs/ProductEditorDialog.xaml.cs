using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the product editor dialog.
/// </summary>
public class ProductEditorResult
{
    /// <summary>
    /// Gets or sets the product code/SKU.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the selling price.
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Gets or sets the cost price.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the tax rate.
    /// </summary>
    public decimal TaxRate { get; set; } = 16.00m;

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the barcode.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal? MinStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets whether the product is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the initial stock (for new products only).
    /// </summary>
    public decimal InitialStock { get; set; } = 0;
}

/// <summary>
/// Interaction logic for ProductEditorDialog.xaml
/// </summary>
public partial class ProductEditorDialog : Window
{
    private readonly Product? _existingProduct;
    private readonly IReadOnlyList<Category> _categories;
    private readonly IImageService? _imageService;
    private readonly ICategoryService? _categoryService;
    private string? _selectedImagePath;
    private List<Category> _filteredCategories = [];
    private List<Category> _allCategories = [];
    private bool _isUpdatingCategoryFilter;
    private Category? _selectedCategory;

    private static readonly string[] UnitOfMeasureOptions =
    [
        "Each",
        "Bottle",
        "Can",
        "Glass",
        "Plate",
        "Portion",
        "Kilogram",
        "Gram",
        "Liter",
        "Milliliter"
    ];

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public ProductEditorResult? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductEditorDialog"/> class for creating a new product.
    /// </summary>
    /// <param name="categories">List of categories for selection.</param>
    /// <param name="defaultCategoryId">Optional default category ID.</param>
    public ProductEditorDialog(IReadOnlyList<Category> categories, int? defaultCategoryId = null)
        : this(null, categories, defaultCategoryId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductEditorDialog"/> class.
    /// </summary>
    /// <param name="existingProduct">The product to edit, or null for creating a new product.</param>
    /// <param name="categories">List of categories for selection.</param>
    /// <param name="defaultCategoryId">Optional default category ID.</param>
    public ProductEditorDialog(Product? existingProduct, IReadOnlyList<Category> categories, int? defaultCategoryId = null)
    {
        InitializeComponent();

        _existingProduct = existingProduct;
        _categories = categories;
        _allCategories = categories.Where(c => c.IsActive).ToList();
        _imageService = App.Services.GetService<IImageService>();
        _categoryService = App.Services.GetService<ICategoryService>();

        SetupDialog(defaultCategoryId);
    }

    private void SetupDialog(int? defaultCategoryId)
    {
        // Setup category dropdown with filtering support
        _filteredCategories = _allCategories.ToList();
        CategoryComboBox.ItemsSource = _filteredCategories;

        // Setup unit of measure dropdown
        UnitOfMeasureComboBox.ItemsSource = UnitOfMeasureOptions;
        UnitOfMeasureComboBox.SelectedIndex = 0;

        if (_existingProduct is not null)
        {
            // Edit mode
            TitleTextBlock.Text = "Edit Product";
            SubtitleTextBlock.Text = $"Editing: {_existingProduct.Name}";
            InitialStockPanel.Visibility = Visibility.Collapsed;

            CodeTextBox.Text = _existingProduct.Code;
            NameTextBox.Text = _existingProduct.Name;
            DescriptionTextBox.Text = _existingProduct.Description;
            _selectedCategory = _allCategories.FirstOrDefault(c => c.Id == _existingProduct.CategoryId);
            if (_selectedCategory is not null)
            {
                CategoryComboBox.SelectedItem = _selectedCategory;
                CategoryComboBox.Text = _selectedCategory.Name;
            }
            BarcodeTextBox.Text = _existingProduct.Barcode;
            SellingPriceTextBox.Text = _existingProduct.SellingPrice.ToString("F2");
            CostPriceTextBox.Text = _existingProduct.CostPrice?.ToString("F2") ?? "";
            TaxRateTextBox.Text = _existingProduct.TaxRate.ToString("F2");
            UnitOfMeasureComboBox.SelectedItem = _existingProduct.UnitOfMeasure;
            MinStockTextBox.Text = _existingProduct.MinStockLevel?.ToString("F0") ?? "";
            MaxStockTextBox.Text = _existingProduct.MaxStockLevel?.ToString("F0") ?? "";
            IsActiveCheckBox.IsChecked = _existingProduct.IsActive;
            _selectedImagePath = _existingProduct.ImagePath;

            if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                LoadImagePreview(_selectedImagePath);
            }
        }
        else
        {
            // Create mode
            TitleTextBlock.Text = "Create Product";
            SubtitleTextBlock.Text = "Add a new product to the catalog";
            InitialStockPanel.Visibility = Visibility.Visible;

            // Set default category if specified
            if (defaultCategoryId.HasValue)
            {
                _selectedCategory = _allCategories.FirstOrDefault(c => c.Id == defaultCategoryId);
                if (_selectedCategory is not null)
                {
                    CategoryComboBox.SelectedItem = _selectedCategory;
                    CategoryComboBox.Text = _selectedCategory.Name;
                }
            }
        }

        // Focus on code field
        CodeTextBox.Focus();
    }

    private void LoadImagePreview(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImagePreview.Source = bitmap;
            ImagePreview.Visibility = Visibility.Visible;
            NoImageText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load image preview from '{path}': {ex.Message}");
            ImagePreview.Visibility = Visibility.Collapsed;
            NoImageText.Visibility = Visibility.Visible;
        }
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow digits and one decimal point
        var textBox = (TextBox)sender;
        var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

        e.Handled = !decimal.TryParse(newText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
    }

    private void CategoryComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingCategoryFilter)
            return;

        _isUpdatingCategoryFilter = true;
        try
        {
            var searchText = CategoryComboBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                _filteredCategories = _allCategories.ToList();
                AddCategoryButton.Visibility = Visibility.Collapsed;
                CategoryHintText.Text = "Type to search or add new category";
            }
            else
            {
                _filteredCategories = _allCategories
                    .Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Check if the exact name already exists (case-insensitive)
                var exactMatch = _allCategories
                    .FirstOrDefault(c => c.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase));

                if (exactMatch is not null)
                {
                    // Exact match found - select it and hide add button
                    AddCategoryButton.Visibility = Visibility.Collapsed;
                    CategoryHintText.Text = "Category found";
                    _selectedCategory = exactMatch;
                }
                else
                {
                    // No exact match - show add button
                    AddCategoryButton.Visibility = Visibility.Visible;
                    CategoryHintText.Text = $"Press '+ Add' to create \"{searchText}\" category";
                }
            }

            CategoryComboBox.ItemsSource = _filteredCategories;

            // Keep the dropdown open while typing
            if (_filteredCategories.Count > 0 && !string.IsNullOrEmpty(searchText))
            {
                CategoryComboBox.IsDropDownOpen = true;
            }
        }
        finally
        {
            _isUpdatingCategoryFilter = false;
        }
    }

    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingCategoryFilter)
            return;

        if (CategoryComboBox.SelectedItem is Category category)
        {
            _selectedCategory = category;
            _isUpdatingCategoryFilter = true;
            try
            {
                CategoryComboBox.Text = category.Name;
                AddCategoryButton.Visibility = Visibility.Collapsed;
                CategoryHintText.Text = "Type to search or add new category";
            }
            finally
            {
                _isUpdatingCategoryFilter = false;
            }
        }
    }

    private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        var categoryName = CategoryComboBox.Text?.Trim();
        if (string.IsNullOrEmpty(categoryName))
            return;

        if (_categoryService is null)
        {
            ShowError("Category service unavailable. Please try again.");
            return;
        }

        // Check if already exists (double-check)
        var existingCategory = _allCategories
            .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (existingCategory is not null)
        {
            _selectedCategory = existingCategory;
            _isUpdatingCategoryFilter = true;
            try
            {
                CategoryComboBox.SelectedItem = existingCategory;
                CategoryComboBox.Text = existingCategory.Name;
                AddCategoryButton.Visibility = Visibility.Collapsed;
                CategoryHintText.Text = "Category selected";
            }
            finally
            {
                _isUpdatingCategoryFilter = false;
            }
            return;
        }

        try
        {
            AddCategoryButton.IsEnabled = false;
            AddCategoryButton.Content = "Adding...";

            // Create new category
            var dto = new CategoryDto
            {
                Name = categoryName,
                IsActive = true,
                DisplayOrder = _allCategories.Count + 1
            };

            var newCategory = await _categoryService.CreateCategoryAsync(dto, 1);

            // Add to local lists
            _allCategories.Add(newCategory);
            _filteredCategories.Add(newCategory);

            // Select the new category
            _selectedCategory = newCategory;
            _isUpdatingCategoryFilter = true;
            try
            {
                CategoryComboBox.ItemsSource = _filteredCategories.ToList();
                CategoryComboBox.SelectedItem = newCategory;
                CategoryComboBox.Text = newCategory.Name;
                AddCategoryButton.Visibility = Visibility.Collapsed;
                CategoryHintText.Text = $"Category \"{categoryName}\" added successfully";
            }
            finally
            {
                _isUpdatingCategoryFilter = false;
            }

            HideError();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create category: {ex.Message}");
            ShowError($"Failed to create category: {ex.Message}");
        }
        finally
        {
            AddCategoryButton.IsEnabled = true;
            AddCategoryButton.Content = "+ Add";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Product Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif|All Files|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var selectedFile = openFileDialog.FileName;

            // Validate image using ImageService if available
            if (_imageService is not null)
            {
                if (!_imageService.ValidateImageFile(selectedFile, out var errorMessage))
                {
                    ShowError(errorMessage ?? "Invalid image file.");
                    return;
                }
            }

            _selectedImagePath = selectedFile;
            HideError();
            LoadImagePreview(_selectedImagePath);
        }
    }

    private void ClearImageButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = null;
        ImagePreview.Source = null;
        ImagePreview.Visibility = Visibility.Collapsed;
        NoImageText.Visibility = Visibility.Visible;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        // Validate code
        var code = CodeTextBox.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Product code is required.");
            CodeTextBox.Focus();
            return;
        }

        if (code.Length < 3)
        {
            ShowError("Product code must be at least 3 characters.");
            CodeTextBox.Focus();
            return;
        }

        // Validate name
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Product name is required.");
            NameTextBox.Focus();
            return;
        }

        if (name.Length < 2)
        {
            ShowError("Product name must be at least 2 characters.");
            NameTextBox.Focus();
            return;
        }

        // Validate category
        var selectedCategory = _selectedCategory ?? CategoryComboBox.SelectedItem as Category;
        if (selectedCategory is null)
        {
            // Check if user typed a category name that matches an existing one
            var typedCategoryName = CategoryComboBox.Text?.Trim();
            if (!string.IsNullOrEmpty(typedCategoryName))
            {
                selectedCategory = _allCategories
                    .FirstOrDefault(c => c.Name.Equals(typedCategoryName, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedCategory is null)
            {
                ShowError("Please select a category or add a new one using the '+ Add' button.");
                CategoryComboBox.Focus();
                return;
            }
        }

        // Validate selling price
        if (!decimal.TryParse(SellingPriceTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var sellingPrice) || sellingPrice <= 0)
        {
            ShowError("Selling price must be greater than zero.");
            SellingPriceTextBox.Focus();
            return;
        }

        // Parse optional cost price
        decimal? costPrice = null;
        if (!string.IsNullOrWhiteSpace(CostPriceTextBox.Text))
        {
            if (!decimal.TryParse(CostPriceTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cp) || cp < 0)
            {
                ShowError("Cost price must be zero or greater.");
                CostPriceTextBox.Focus();
                return;
            }
            costPrice = cp;
        }

        // Parse tax rate
        if (!decimal.TryParse(TaxRateTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var taxRate))
        {
            taxRate = 16.00m;
        }

        if (taxRate < 0 || taxRate > 100)
        {
            ShowError("Tax rate must be between 0 and 100.");
            TaxRateTextBox.Focus();
            return;
        }

        // Get unit of measure
        var unitOfMeasure = UnitOfMeasureComboBox.SelectedItem?.ToString() ?? "Each";

        // Parse optional stock levels
        decimal? minStock = null;
        if (!string.IsNullOrWhiteSpace(MinStockTextBox.Text))
        {
            if (!decimal.TryParse(MinStockTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ms) || ms < 0)
            {
                ShowError("Minimum stock must be zero or greater.");
                MinStockTextBox.Focus();
                return;
            }
            minStock = ms;
        }

        decimal? maxStock = null;
        if (!string.IsNullOrWhiteSpace(MaxStockTextBox.Text))
        {
            if (!decimal.TryParse(MaxStockTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ms) || ms < 0)
            {
                ShowError("Maximum stock must be zero or greater.");
                MaxStockTextBox.Focus();
                return;
            }
            maxStock = ms;
        }

        // Validate stock level relationship
        if (minStock.HasValue && maxStock.HasValue && minStock >= maxStock)
        {
            ShowError("Reorder point must be less than maximum stock.");
            MinStockTextBox.Focus();
            return;
        }

        // Parse initial stock (for new products)
        decimal initialStock = 0;
        if (_existingProduct is null && !string.IsNullOrWhiteSpace(InitialStockTextBox.Text))
        {
            if (!decimal.TryParse(InitialStockTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out initialStock) || initialStock < 0)
            {
                ShowError("Initial stock must be zero or greater.");
                InitialStockTextBox.Focus();
                return;
            }
        }

        Result = new ProductEditorResult
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            CategoryId = selectedCategory.Id,
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            TaxRate = taxRate,
            UnitOfMeasure = unitOfMeasure,
            ImagePath = _selectedImagePath,
            Barcode = string.IsNullOrWhiteSpace(BarcodeTextBox.Text) ? null : BarcodeTextBox.Text.Trim(),
            MinStockLevel = minStock,
            MaxStockLevel = maxStock,
            IsActive = IsActiveCheckBox.IsChecked ?? true,
            InitialStock = initialStock
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Center the window on screen (width is fixed in XAML, height is auto-sized)
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        Left = (screenWidth - ActualWidth) / 2;
        Top = (screenHeight - ActualHeight) / 2;

        // Prevent closing by clicking outside - capture mouse on the content border
        MouseDown += Window_MouseDown;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Only allow interaction within the dialog content
        // This prevents accidental closing by clicking the transparent area
        if (e.OriginalSource == this)
        {
            e.Handled = true;
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        // Keep the dialog focused and prevent it from being dismissed
        if (IsVisible)
        {
            Activate();
        }
    }
}
