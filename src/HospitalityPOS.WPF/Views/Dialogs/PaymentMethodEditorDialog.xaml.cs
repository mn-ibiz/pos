using System.Windows;
using System.Windows.Media;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the payment method editor dialog.
/// </summary>
public class PaymentMethodEditorResult
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public PaymentMethodType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresReference { get; set; }
    public string? ReferenceLabel { get; set; }
    public int? ReferenceMinLength { get; set; }
    public int? ReferenceMaxLength { get; set; }
    public bool SupportsChange { get; set; }
    public bool OpensDrawer { get; set; }
    public int DisplayOrder { get; set; }
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// Interaction logic for PaymentMethodEditorDialog.xaml
/// </summary>
public partial class PaymentMethodEditorDialog : Window
{
    private readonly PaymentMethod? _existingMethod;

    public PaymentMethodEditorResult? Result { get; private set; }

    public PaymentMethodEditorDialog(PaymentMethod? existingMethod)
    {
        InitializeComponent();
        _existingMethod = existingMethod;

        // Populate payment method type ComboBox
        TypeComboBox.ItemsSource = Enum.GetValues<PaymentMethodType>();
        TypeComboBox.SelectedItem = PaymentMethodType.Cash;

        if (existingMethod != null)
        {
            TitleText.Text = $"Edit {existingMethod.Name}";
            SubtitleText.Text = "Update the payment method settings";
            LoadExistingMethod(existingMethod);
        }
        else
        {
            TitleText.Text = "New Payment Method";
            SubtitleText.Text = "Configure the new payment method";
        }

        // Focus on name field
        Loaded += (s, e) => NameTextBox.Focus();
    }

    private void LoadExistingMethod(PaymentMethod method)
    {
        NameTextBox.Text = method.Name;
        CodeTextBox.Text = method.Code;
        TypeComboBox.SelectedItem = method.Type;
        DescriptionTextBox.Text = method.Description ?? string.Empty;
        RequiresReferenceCheckBox.IsChecked = method.RequiresReference;
        ReferenceLabelTextBox.Text = method.ReferenceLabel ?? "Reference Number";
        MinLengthTextBox.Text = method.ReferenceMinLength?.ToString() ?? "0";
        MaxLengthTextBox.Text = method.ReferenceMaxLength?.ToString() ?? "50";
        SupportsChangeCheckBox.IsChecked = method.SupportsChange;
        OpensDrawerCheckBox.IsChecked = method.OpensDrawer;
        IsActiveCheckBox.IsChecked = method.IsActive;
        BackgroundColorTextBox.Text = method.BackgroundColor ?? "#4E4E6E";
        DisplayOrderTextBox.Text = method.DisplayOrder.ToString();

        UpdateColorPreview();
    }

    private void BackgroundColorTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        try
        {
            var hexColor = BackgroundColorTextBox.Text;
            if (!hexColor.StartsWith('#'))
            {
                hexColor = "#" + hexColor;
            }

            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            ColorPreview.Background = new SolidColorBrush(color);
        }
        catch (Exception)
        {
            ColorPreview.Background = new SolidColorBrush(Color.FromRgb(78, 78, 110));
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ValidationMessage.Text = "Name is required.";
            NameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
        {
            ValidationMessage.Text = "Code is required.";
            CodeTextBox.Focus();
            return;
        }

        if (!int.TryParse(DisplayOrderTextBox.Text, out var displayOrder) || displayOrder < 0)
        {
            ValidationMessage.Text = "Display order must be a non-negative number.";
            DisplayOrderTextBox.Focus();
            return;
        }

        int? minLength = null;
        int? maxLength = null;

        if (RequiresReferenceCheckBox.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(ReferenceLabelTextBox.Text))
            {
                ValidationMessage.Text = "Reference label is required when reference is required.";
                ReferenceLabelTextBox.Focus();
                return;
            }

            if (int.TryParse(MinLengthTextBox.Text, out var min))
            {
                minLength = min;
            }

            if (int.TryParse(MaxLengthTextBox.Text, out var max))
            {
                maxLength = max;
            }

            if (minLength.HasValue && maxLength.HasValue && minLength > maxLength)
            {
                ValidationMessage.Text = "Min length cannot be greater than max length.";
                MinLengthTextBox.Focus();
                return;
            }
        }

        // Validate color format
        var bgColor = BackgroundColorTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(bgColor))
        {
            try
            {
                if (!bgColor.StartsWith('#'))
                {
                    bgColor = "#" + bgColor;
                }
                ColorConverter.ConvertFromString(bgColor);
            }
            catch (Exception)
            {
                ValidationMessage.Text = "Invalid color format. Use hex format (e.g., #4CAF50).";
                BackgroundColorTextBox.Focus();
                return;
            }
        }

        Result = new PaymentMethodEditorResult
        {
            Name = NameTextBox.Text.Trim(),
            Code = CodeTextBox.Text.Trim().ToUpperInvariant(),
            Type = (PaymentMethodType)TypeComboBox.SelectedItem,
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            IsActive = IsActiveCheckBox.IsChecked == true,
            RequiresReference = RequiresReferenceCheckBox.IsChecked == true,
            ReferenceLabel = RequiresReferenceCheckBox.IsChecked == true ? ReferenceLabelTextBox.Text.Trim() : null,
            ReferenceMinLength = minLength,
            ReferenceMaxLength = maxLength,
            SupportsChange = SupportsChangeCheckBox.IsChecked == true,
            OpensDrawer = OpensDrawerCheckBox.IsChecked == true,
            DisplayOrder = displayOrder,
            BackgroundColor = string.IsNullOrWhiteSpace(bgColor) ? null : bgColor
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
