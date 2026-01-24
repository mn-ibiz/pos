using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the expense category editor dialog.
/// </summary>
public class ExpenseCategoryEditorResult
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category type.
    /// </summary>
    public ExpenseCategoryType Type { get; set; } = ExpenseCategoryType.Operating;

    /// <summary>
    /// Gets or sets the category description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon code.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the color hex code.
    /// </summary>
    public string? Color { get; set; }
}

/// <summary>
/// Interaction logic for ExpenseCategoryEditorDialog.xaml
/// </summary>
public partial class ExpenseCategoryEditorDialog : Window
{
    private readonly ExpenseCategory? _existingCategory;

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public ExpenseCategoryEditorResult? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseCategoryEditorDialog"/> class for creating a new category.
    /// </summary>
    public ExpenseCategoryEditorDialog() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseCategoryEditorDialog"/> class.
    /// </summary>
    /// <param name="existingCategory">The category to edit, or null for creating a new category.</param>
    public ExpenseCategoryEditorDialog(ExpenseCategory? existingCategory)
    {
        InitializeComponent();

        _existingCategory = existingCategory;

        SetupDialog();
    }

    private void SetupDialog()
    {
        if (_existingCategory is not null)
        {
            // Edit mode
            TitleTextBlock.Text = "Edit Category";
            SubtitleTextBlock.Text = $"Editing: {_existingCategory.Name}";

            NameTextBox.Text = _existingCategory.Name;
            DescriptionTextBox.Text = _existingCategory.Description;
            IconTextBox.Text = _existingCategory.Icon ?? "\uE7BF";
            ColorTextBox.Text = _existingCategory.Color ?? "#2D2D44";

            // Select the correct type in the combobox
            SelectTypeInComboBox(_existingCategory.Type);
        }
        else
        {
            // Create mode
            TitleTextBlock.Text = "Create Category";
            SubtitleTextBlock.Text = "Add a new expense category";
        }

        NameTextBox.Focus();
    }

    private void SelectTypeInComboBox(ExpenseCategoryType type)
    {
        var tagToFind = type.ToString();
        foreach (ComboBoxItem item in TypeComboBox.Items)
        {
            if (item.Tag?.ToString() == tagToFind)
            {
                TypeComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private ExpenseCategoryType GetSelectedType()
    {
        if (TypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "COGS" => ExpenseCategoryType.COGS,
                "Labor" => ExpenseCategoryType.Labor,
                "Occupancy" => ExpenseCategoryType.Occupancy,
                "Operating" => ExpenseCategoryType.Operating,
                "Marketing" => ExpenseCategoryType.Marketing,
                "Administrative" => ExpenseCategoryType.Administrative,
                "Other" => ExpenseCategoryType.Other,
                _ => ExpenseCategoryType.Operating
            };
        }
        return ExpenseCategoryType.Operating;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        // Validate name
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Category name is required.");
            NameTextBox.Focus();
            return;
        }

        if (name.Length < 2)
        {
            ShowError("Category name must be at least 2 characters.");
            NameTextBox.Focus();
            return;
        }

        // Validate color format if provided
        var color = ColorTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(color) && !IsValidHexColor(color))
        {
            ShowError("Please enter a valid hex color (e.g., #F59E0B).");
            ColorTextBox.Focus();
            return;
        }

        Result = new ExpenseCategoryEditorResult
        {
            Name = name,
            Type = GetSelectedType(),
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            Icon = string.IsNullOrWhiteSpace(IconTextBox.Text) ? null : IconTextBox.Text.Trim(),
            Color = string.IsNullOrWhiteSpace(color) ? null : color
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

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color))
            return false;

        if (!color.StartsWith('#'))
            return false;

        if (color.Length != 7 && color.Length != 4)
            return false;

        for (int i = 1; i < color.Length; i++)
        {
            char c = color[i];
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return false;
        }

        return true;
    }
}
