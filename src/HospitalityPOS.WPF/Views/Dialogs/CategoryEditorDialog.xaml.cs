using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HospitalityPOS.Core.Entities;
using Microsoft.Win32;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the category editor dialog.
/// </summary>
public class CategoryEditorResult
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent category ID.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Interaction logic for CategoryEditorDialog.xaml
/// </summary>
public partial class CategoryEditorDialog : Window
{
    private readonly Category? _existingCategory;
    private readonly IReadOnlyList<Category> _availableParents;
    private string? _selectedImagePath;

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public CategoryEditorResult? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorDialog"/> class for creating a new category.
    /// </summary>
    /// <param name="availableParents">List of categories that can be selected as parent.</param>
    /// <param name="defaultParentId">Optional default parent category ID.</param>
    public CategoryEditorDialog(IReadOnlyList<Category> availableParents, int? defaultParentId = null)
        : this(null, availableParents, defaultParentId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorDialog"/> class.
    /// </summary>
    /// <param name="existingCategory">The category to edit, or null for creating a new category.</param>
    /// <param name="availableParents">List of categories that can be selected as parent.</param>
    /// <param name="defaultParentId">Optional default parent category ID (used when creating subcategory).</param>
    public CategoryEditorDialog(Category? existingCategory, IReadOnlyList<Category> availableParents, int? defaultParentId = null)
    {
        InitializeComponent();

        _existingCategory = existingCategory;
        _availableParents = availableParents;

        SetupDialog(defaultParentId);
    }

    private void SetupDialog(int? defaultParentId)
    {
        // Setup parent category dropdown
        var parentItems = new List<ParentCategoryItem>
        {
            new() { Id = null, Name = "(None - Root Category)" }
        };

        foreach (var category in _availableParents)
        {
            // Exclude the current category and its descendants from parent selection
            if (_existingCategory is null || (category.Id != _existingCategory.Id && !IsDescendantOf(category, _existingCategory.Id)))
            {
                parentItems.Add(new ParentCategoryItem { Id = category.Id, Name = category.Name });
            }
        }

        ParentCategoryComboBox.ItemsSource = parentItems;
        ParentCategoryComboBox.DisplayMemberPath = "Name";
        ParentCategoryComboBox.SelectedValuePath = "Id";

        if (_existingCategory is not null)
        {
            // Edit mode
            TitleTextBlock.Text = "Edit Category";
            SubtitleTextBlock.Text = $"Editing: {_existingCategory.Name}";

            NameTextBox.Text = _existingCategory.Name;
            ParentCategoryComboBox.SelectedValue = _existingCategory.ParentCategoryId;
            DisplayOrderTextBox.Text = _existingCategory.DisplayOrder.ToString();
            IsActiveCheckBox.IsChecked = _existingCategory.IsActive;
            _selectedImagePath = _existingCategory.ImagePath;

            if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                LoadImagePreview(_selectedImagePath);
            }
        }
        else
        {
            // Create mode
            TitleTextBlock.Text = "Create Category";
            SubtitleTextBlock.Text = "Add a new product category";

            // Set default parent if specified (for creating subcategory)
            if (defaultParentId.HasValue)
            {
                ParentCategoryComboBox.SelectedValue = defaultParentId;
                var parent = _availableParents.FirstOrDefault(c => c.Id == defaultParentId);
                if (parent is not null)
                {
                    SubtitleTextBlock.Text = $"Creating subcategory under: {parent.Name}";
                }
            }
            else
            {
                ParentCategoryComboBox.SelectedIndex = 0; // None
            }
        }

        // Focus on name field
        NameTextBox.Focus();
    }

    private bool IsDescendantOf(Category category, int ancestorId)
    {
        var current = category.ParentCategory;
        while (current is not null)
        {
            if (current.Id == ancestorId)
                return true;
            current = current.ParentCategory;
        }
        return false;
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
            // Log the exception for debugging purposes
            Debug.WriteLine($"Failed to load image preview from '{path}': {ex.Message}");
            ImagePreview.Visibility = Visibility.Collapsed;
            NoImageText.Visibility = Visibility.Visible;
        }
    }

    private void DisplayOrderTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Category Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedImagePath = openFileDialog.FileName;
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
        // Validate
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Category name is required.");
            NameTextBox.Focus();
            return;
        }

        if (name.Length > 100)
        {
            ShowError("Category name must be 100 characters or less.");
            NameTextBox.Focus();
            return;
        }

        if (!int.TryParse(DisplayOrderTextBox.Text, out var displayOrder))
        {
            displayOrder = 0;
        }

        // Get parent category ID
        int? parentCategoryId = null;
        if (ParentCategoryComboBox.SelectedValue is int selectedId)
        {
            parentCategoryId = selectedId;
        }

        Result = new CategoryEditorResult
        {
            Name = name,
            ParentCategoryId = parentCategoryId,
            ImagePath = _selectedImagePath,
            DisplayOrder = displayOrder,
            IsActive = IsActiveCheckBox.IsChecked ?? true
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

    /// <summary>
    /// Helper class for parent category dropdown items.
    /// </summary>
    private class ParentCategoryItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
