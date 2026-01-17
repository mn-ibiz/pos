using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for manually adjusting stock quantities with reason selection.
/// </summary>
public partial class StockAdjustmentDialog : Window
{
    private readonly Product _product;
    private readonly decimal _currentStock;
    private readonly IReadOnlyList<AdjustmentReason> _adjustmentReasons;
    private readonly List<AdjustmentReason> _allReasons;
    private AdjustmentReason? _selectedReason;
    private bool _isDecrease;

    /// <summary>
    /// Gets the new stock quantity after adjustment.
    /// </summary>
    public decimal NewStockQuantity { get; private set; }

    /// <summary>
    /// Gets the selected adjustment reason ID.
    /// </summary>
    public int? SelectedReasonId => _selectedReason?.Id;

    /// <summary>
    /// Gets the selected adjustment reason name.
    /// </summary>
    public string? SelectedReasonName => _selectedReason?.Name;

    /// <summary>
    /// Gets the additional notes entered by the user.
    /// </summary>
    public string AdditionalNotes => NotesTextBox.Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="StockAdjustmentDialog"/> class.
    /// </summary>
    /// <param name="product">The product to adjust stock for.</param>
    /// <param name="currentStock">The current stock level.</param>
    /// <param name="adjustmentReasons">The list of available adjustment reasons.</param>
    public StockAdjustmentDialog(
        Product product,
        decimal currentStock,
        IReadOnlyList<AdjustmentReason> adjustmentReasons)
    {
        InitializeComponent();
        _product = product ?? throw new ArgumentNullException(nameof(product));
        _currentStock = currentStock;
        _adjustmentReasons = adjustmentReasons ?? throw new ArgumentNullException(nameof(adjustmentReasons));
        _allReasons = adjustmentReasons.ToList();
        NewStockQuantity = currentStock;

        InitializeDialog();
    }

    private void InitializeDialog()
    {
        // Populate product info
        ProductNameText.Text = _product.Name;
        CategoryText.Text = _product.Category?.Name ?? "Uncategorized";
        UnitText.Text = _product.UnitOfMeasure;
        CurrentStockText.Text = _currentStock.ToString("N2");
        NewStockText.Text = _currentStock.ToString("N2");

        // Set initial adjustment type
        AdjustByRadio.IsChecked = true;
        DirectionToggle.Visibility = Visibility.Visible;
        DirectionToggle.IsChecked = false; // Start with increase (+)
        _isDecrease = false;

        // Populate adjustment reasons (filtered by direction)
        FilterAndPopulateReasons();
    }

    private void FilterAndPopulateReasons()
    {
        ReasonsPanel.Children.Clear();
        _selectedReason = null;
        UpdateApplyButtonState();

        // Filter reasons based on direction
        var filteredReasons = _allReasons
            .Where(r => (_isDecrease && r.IsDecrease) || (!_isDecrease && r.IsIncrease))
            .OrderBy(r => r.DisplayOrder)
            .ToList();

        foreach (var reason in filteredReasons)
        {
            var radioButton = new RadioButton
            {
                Content = reason.Name,
                Tag = reason,
                Style = (Style)FindResource("ReasonRadioButton"),
                GroupName = "AdjustmentReasons"
            };

            radioButton.Checked += ReasonRadioButton_Checked;
            ReasonsPanel.Children.Add(radioButton);
        }
    }

    private void AdjustmentType_Changed(object sender, RoutedEventArgs e)
    {
        if (SetExactRadio.IsChecked == true)
        {
            // Set Exact Quantity mode
            DirectionToggle.Visibility = Visibility.Collapsed;
            AdjustmentTextBox.Text = _currentStock.ToString("0");
        }
        else
        {
            // Adjust By Amount mode
            DirectionToggle.Visibility = Visibility.Visible;
            AdjustmentTextBox.Text = "0";
        }

        CalculateNewStock();
    }

    private void DirectionToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDecrease = DirectionToggle.IsChecked == true;
        FilterAndPopulateReasons();
        CalculateNewStock();
    }

    private void IncrementButton_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(AdjustmentTextBox.Text, out var current))
        {
            AdjustmentTextBox.Text = (current + 1).ToString("0");
        }
    }

    private void DecrementButton_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(AdjustmentTextBox.Text, out var current) && current > 0)
        {
            AdjustmentTextBox.Text = (current - 1).ToString("0");
        }
    }

    private void AdjustmentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CalculateNewStock();
    }

    private void AdjustmentTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only numbers and decimal point
        e.Handled = !Regex.IsMatch(e.Text, @"^[\d.]$");
    }

    private void CalculateNewStock()
    {
        if (!decimal.TryParse(AdjustmentTextBox.Text, out var inputValue))
        {
            NewStockText.Text = _currentStock.ToString("N2");
            NewStockQuantity = _currentStock;
            UpdateNewStockColor();
            UpdateApplyButtonState();
            return;
        }

        if (SetExactRadio.IsChecked == true)
        {
            // Set Exact Quantity mode
            NewStockQuantity = Math.Max(0, inputValue);
            _isDecrease = NewStockQuantity < _currentStock;
            FilterAndPopulateReasons();
        }
        else
        {
            // Adjust By Amount mode
            if (_isDecrease)
            {
                NewStockQuantity = Math.Max(0, _currentStock - Math.Abs(inputValue));
            }
            else
            {
                NewStockQuantity = _currentStock + Math.Abs(inputValue);
            }
        }

        NewStockText.Text = NewStockQuantity.ToString("N2");
        UpdateNewStockColor();
        UpdateApplyButtonState();
    }

    private void UpdateNewStockColor()
    {
        if (NewStockQuantity > _currentStock)
        {
            NewStockText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")!);
        }
        else if (NewStockQuantity < _currentStock)
        {
            NewStockText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444")!);
        }
        else
        {
            NewStockText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6")!);
        }
    }

    private void ReasonRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is AdjustmentReason reason)
        {
            _selectedReason = reason;

            // Update notes label based on whether notes are required
            if (reason.RequiresNote)
            {
                NotesLabel.Text = "Additional Notes (Required):";
                NotesLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444")!);
            }
            else
            {
                NotesLabel.Text = "Additional Notes (Optional):";
                NotesLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!);
            }

            ClearError();
            UpdateApplyButtonState();
        }
    }

    private void UpdateApplyButtonState()
    {
        // Enable apply button only if:
        // 1. A reason is selected
        // 2. The new stock is different from current stock
        ApplyButton.IsEnabled = _selectedReason != null && NewStockQuantity != _currentStock;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate selection
        if (_selectedReason is null)
        {
            ShowError("Please select an adjustment reason.");
            return;
        }

        // Validate notes if required
        if (_selectedReason.RequiresNote && string.IsNullOrWhiteSpace(NotesTextBox.Text))
        {
            ShowError("Additional notes are required for this adjustment reason.");
            return;
        }

        // Validate that stock actually changed
        if (NewStockQuantity == _currentStock)
        {
            ShowError("No adjustment made. The new stock is the same as current stock.");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorMessageText.Text = message;
        ErrorMessageText.Visibility = Visibility.Visible;
    }

    private void ClearError()
    {
        ErrorMessageText.Text = "";
        ErrorMessageText.Visibility = Visibility.Collapsed;
    }
}

/// <summary>
/// Result from the stock adjustment dialog.
/// </summary>
public class StockAdjustmentDialogResult
{
    /// <summary>
    /// Gets or sets the new stock quantity.
    /// </summary>
    public decimal NewStockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the selected adjustment reason ID.
    /// </summary>
    public int ReasonId { get; set; }

    /// <summary>
    /// Gets or sets the selected adjustment reason name.
    /// </summary>
    public string ReasonName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the additional notes.
    /// </summary>
    public string? Notes { get; set; }
}
