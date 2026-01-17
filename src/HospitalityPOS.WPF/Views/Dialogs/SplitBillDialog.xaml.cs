using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for splitting a bill either equally or by selected items.
/// </summary>
public partial class SplitBillDialog : Window
{
    private readonly Receipt _receipt;
    private readonly List<ReceiptItem> _items;
    private int _splitCount = 2;
    private const int MinSplitCount = 2;
    private const int MaxSplitCount = 10;

    private readonly Dictionary<int, CheckBox> _itemCheckboxes = new();

    /// <summary>
    /// Gets whether the user chose equal split.
    /// </summary>
    public bool IsEqualSplit => EqualSplitRadio.IsChecked == true;

    /// <summary>
    /// Gets the number of ways to split (for equal split).
    /// </summary>
    public int NumberOfWays => _splitCount;

    /// <summary>
    /// Gets the split requests for item-based split.
    /// </summary>
    public List<SplitItemRequest> SplitRequests { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitBillDialog"/> class.
    /// </summary>
    public SplitBillDialog(Receipt receipt)
    {
        InitializeComponent();
        _receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        _items = receipt.ReceiptItems?.ToList() ?? new List<ReceiptItem>();

        InitializeDialog();
    }

    private void InitializeDialog()
    {
        // Set receipt info
        ReceiptInfoText.Text = $"Receipt #{_receipt.ReceiptNumber} - KSh {_receipt.TotalAmount:N2}";

        // Initialize equal split display
        UpdateEqualSplitDisplay();

        // Populate items for item-based split
        PopulateItems();

        // Update button states
        UpdateButtonStates();
    }

    private void PopulateItems()
    {
        ItemsPanel.Children.Clear();
        _itemCheckboxes.Clear();

        foreach (var item in _items)
        {
            var checkbox = new CheckBox
            {
                Tag = item,
                Style = (Style)FindResource("ItemCheckboxStyle"),
                Content = CreateItemContent(item)
            };
            checkbox.Checked += ItemCheckbox_CheckedChanged;
            checkbox.Unchecked += ItemCheckbox_CheckedChanged;

            _itemCheckboxes[item.Id] = checkbox;
            ItemsPanel.Children.Add(checkbox);
        }
    }

    private static UIElement CreateItemContent(ReceiptItem item)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameStack = new StackPanel();

        var nameText = new TextBlock
        {
            Text = $"{item.Quantity}x {item.ProductName}",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14
        };
        nameStack.Children.Add(nameText);

        if (!string.IsNullOrEmpty(item.Notes))
        {
            var notesText = new TextBlock
            {
                Text = item.Notes,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
                FontSize = 11,
                FontStyle = FontStyles.Italic
            };
            nameStack.Children.Add(notesText);
        }

        Grid.SetColumn(nameStack, 0);
        grid.Children.Add(nameStack);

        var priceText = new TextBlock
        {
            Text = $"KSh {item.TotalAmount:N2}",
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")!),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        Grid.SetColumn(priceText, 1);
        grid.Children.Add(priceText);

        return grid;
    }

    private void SplitTypeChanged(object sender, RoutedEventArgs e)
    {
        if (EqualSplitPanel is null || ItemSplitPanel is null) return;

        if (EqualSplitRadio.IsChecked == true)
        {
            EqualSplitPanel.Visibility = Visibility.Visible;
            ItemSplitPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            EqualSplitPanel.Visibility = Visibility.Collapsed;
            ItemSplitPanel.Visibility = Visibility.Visible;
        }

        ClearError();
    }

    private void DecreaseSplit_Click(object sender, RoutedEventArgs e)
    {
        if (_splitCount > MinSplitCount)
        {
            _splitCount--;
            UpdateEqualSplitDisplay();
        }
    }

    private void IncreaseSplit_Click(object sender, RoutedEventArgs e)
    {
        if (_splitCount < MaxSplitCount)
        {
            _splitCount++;
            UpdateEqualSplitDisplay();
        }
    }

    private void UpdateEqualSplitDisplay()
    {
        SplitCountText.Text = _splitCount.ToString();

        var amountPerSplit = Math.Ceiling(_receipt.TotalAmount / _splitCount * 100) / 100;
        AmountPerSplitText.Text = $"Each split: KSh {amountPerSplit:N2}";

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        DecreaseSplitButton.IsEnabled = _splitCount > MinSplitCount;
        IncreaseSplitButton.IsEnabled = _splitCount < MaxSplitCount;
    }

    private void ItemCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        var selectedTotal = GetSelectedItemsTotal();
        SelectedTotalText.Text = $"KSh {selectedTotal:N2}";
        ClearError();
    }

    private decimal GetSelectedItemsTotal()
    {
        return _itemCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag as ReceiptItem)
            .Where(item => item is not null)
            .Sum(item => item!.TotalAmount);
    }

    private List<int> GetSelectedItemIds()
    {
        return _itemCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag as ReceiptItem)
            .Where(item => item is not null)
            .Select(item => item!.Id)
            .ToList();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsEqualSplit)
        {
            // Validate equal split
            if (_splitCount < MinSplitCount || _splitCount > MaxSplitCount)
            {
                ShowError($"Split count must be between {MinSplitCount} and {MaxSplitCount}.");
                return;
            }

            DialogResult = true;
            Close();
        }
        else
        {
            // Validate item-based split
            var selectedIds = GetSelectedItemIds();
            if (!selectedIds.Any())
            {
                ShowError("Please select at least one item to split.");
                return;
            }

            // All items selected is allowed - they'll be moved to a new receipt
            // and original will be marked as split

            SplitRequests = new List<SplitItemRequest>
            {
                new SplitItemRequest
                {
                    ItemIds = selectedIds
                }
            };

            DialogResult = true;
            Close();
        }
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
