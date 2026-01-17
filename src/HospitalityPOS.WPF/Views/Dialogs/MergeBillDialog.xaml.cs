using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for merging multiple receipts into one.
/// </summary>
public partial class MergeBillDialog : Window
{
    private readonly List<Receipt> _receipts;
    private readonly Dictionary<int, CheckBox> _receiptCheckboxes = new();

    /// <summary>
    /// Gets the selected receipt IDs for merging.
    /// </summary>
    public List<int> SelectedReceiptIds { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeBillDialog"/> class.
    /// </summary>
    public MergeBillDialog(IEnumerable<Receipt> receipts)
    {
        InitializeComponent();
        _receipts = receipts?.ToList() ?? new List<Receipt>();

        InitializeDialog();
    }

    private void InitializeDialog()
    {
        ReceiptsPanel.Children.Clear();
        _receiptCheckboxes.Clear();

        if (!_receipts.Any())
        {
            EmptyStateText.Visibility = Visibility.Visible;
            MergeButton.IsEnabled = false;
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;

        foreach (var receipt in _receipts)
        {
            var checkbox = new CheckBox
            {
                Tag = receipt,
                Style = (Style)FindResource("ReceiptCheckboxStyle"),
                Content = CreateReceiptContent(receipt)
            };
            checkbox.Checked += ReceiptCheckbox_CheckedChanged;
            checkbox.Unchecked += ReceiptCheckbox_CheckedChanged;

            _receiptCheckboxes[receipt.Id] = checkbox;
            ReceiptsPanel.Children.Add(checkbox);
        }

        UpdateSelectionSummary();
    }

    private static UIElement CreateReceiptContent(Receipt receipt)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel();

        // Receipt number and status
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
        var receiptNumberText = new TextBlock
        {
            Text = receipt.ReceiptNumber,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };
        headerStack.Children.Add(receiptNumberText);
        infoStack.Children.Add(headerStack);

        // Table and customer info
        var detailsText = new TextBlock
        {
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
            FontSize = 12
        };

        var details = new List<string>();
        if (!string.IsNullOrEmpty(receipt.TableNumber))
            details.Add($"Table {receipt.TableNumber}");
        if (!string.IsNullOrEmpty(receipt.CustomerName))
            details.Add(receipt.CustomerName);
        if (receipt.Owner != null)
            details.Add($"By: {receipt.Owner.DisplayName}");

        detailsText.Text = string.Join(" | ", details);
        infoStack.Children.Add(detailsText);

        // Item count
        var itemCountText = new TextBlock
        {
            Text = $"{receipt.ReceiptItems.Count} items",
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")!),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        };
        infoStack.Children.Add(itemCountText);

        Grid.SetColumn(infoStack, 0);
        grid.Children.Add(infoStack);

        // Total amount
        var totalText = new TextBlock
        {
            Text = $"KSh {receipt.TotalAmount:N2}",
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")!),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0)
        };
        Grid.SetColumn(totalText, 1);
        grid.Children.Add(totalText);

        return grid;
    }

    private void ReceiptCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        UpdateSelectionSummary();
        ClearError();
    }

    private void UpdateSelectionSummary()
    {
        var selectedReceipts = _receiptCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag as Receipt)
            .Where(r => r is not null)
            .ToList();

        var count = selectedReceipts.Count;
        var total = selectedReceipts.Sum(r => r!.TotalAmount);

        SelectedCountText.Text = count == 1 ? "1 receipt" : $"{count} receipts";
        CombinedTotalText.Text = $"KSh {total:N2}";

        // Enable merge button only when 2+ receipts are selected
        MergeButton.IsEnabled = count >= 2;
    }

    private List<int> GetSelectedReceiptIds()
    {
        return _receiptCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag as Receipt)
            .Where(r => r is not null)
            .Select(r => r!.Id)
            .ToList();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void MergeButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = GetSelectedReceiptIds();
        if (selectedIds.Count < 2)
        {
            ShowError("Please select at least 2 receipts to merge.");
            return;
        }

        SelectedReceiptIds = selectedIds;
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
