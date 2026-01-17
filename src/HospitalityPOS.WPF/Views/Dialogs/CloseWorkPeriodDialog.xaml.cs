using System.Globalization;
using System.Windows;
using System.Windows.Media;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.Converters;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for CloseWorkPeriodDialog.xaml
/// </summary>
public partial class CloseWorkPeriodDialog : Window
{
    private readonly decimal _expectedCash;
    private readonly decimal _openingFloat;
    private readonly decimal _cashSales;
    private readonly decimal _cashPayouts;

    /// <summary>
    /// Gets the entered closing cash amount.
    /// </summary>
    public decimal ClosingCash { get; private set; }

    /// <summary>
    /// Gets the closing notes.
    /// </summary>
    public string? ClosingNotes { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there were unsettled receipts.
    /// </summary>
    public bool HasUnsettledReceipts { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloseWorkPeriodDialog"/> class.
    /// </summary>
    /// <param name="expectedCash">The expected cash amount.</param>
    /// <param name="openingFloat">The opening float amount.</param>
    /// <param name="cashSales">The total cash sales.</param>
    /// <param name="cashPayouts">The total cash payouts.</param>
    /// <param name="unsettledReceipts">List of unsettled receipts.</param>
    public CloseWorkPeriodDialog(
        decimal expectedCash,
        decimal openingFloat,
        decimal cashSales,
        decimal cashPayouts,
        IReadOnlyList<Receipt>? unsettledReceipts = null)
    {
        InitializeComponent();

        _expectedCash = expectedCash;
        _openingFloat = openingFloat;
        _cashSales = cashSales;
        _cashPayouts = cashPayouts;

        PopulateDialog(unsettledReceipts);
    }

    private void PopulateDialog(IReadOnlyList<Receipt>? unsettledReceipts)
    {
        // Display expected cash breakdown
        OpeningFloatText.Text = $"KSh {_openingFloat:N2}";
        CashSalesText.Text = $"KSh {_cashSales:N2}";
        CashPayoutsText.Text = $"-KSh {_cashPayouts:N2}";
        ExpectedCashText.Text = $"KSh {_expectedCash:N2}";

        // Default cash count to expected
        CashCountInput.Text = _expectedCash.ToString("N2");

        // Show unsettled receipts warning if any
        if (unsettledReceipts is not null && unsettledReceipts.Count > 0)
        {
            HasUnsettledReceipts = true;
            UnsettledWarningBorder.Visibility = Visibility.Visible;
            UnsettledCountText.Text = $"{unsettledReceipts.Count} receipt{(unsettledReceipts.Count > 1 ? "s" : "")} pending settlement";

            var displayList = unsettledReceipts.Take(5).Select(r => new
            {
                r.ReceiptNumber,
                TableInfo = r.Order?.TableNumber ?? "No table",
                r.TotalAmount
            }).ToList();

            UnsettledReceiptsList.ItemsSource = displayList;
        }

        UpdateVarianceDisplay();
    }

    private void CashCountInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateVarianceDisplay();
    }

    private void UpdateVarianceDisplay()
    {
        if (!decimal.TryParse(CashCountInput.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var cashCount))
        {
            VarianceText.Text = "KSh --";
            VarianceText.Foreground = new SolidColorBrush(Colors.White);
            VarianceStatusBorder.Visibility = Visibility.Collapsed;
            return;
        }

        var variance = cashCount - _expectedCash;

        VarianceText.Text = variance >= 0
            ? $"KSh {variance:N2}"
            : $"-KSh {Math.Abs(variance):N2}";

        VarianceStatusBorder.Visibility = Visibility.Visible;

        if (variance < 0)
        {
            // Short - Red
            VarianceText.Foreground = VarianceStatusColors.ShortForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.ShortBackground;
            VarianceStatusText.Text = "SHORT";
            VarianceStatusText.Foreground = VarianceStatusColors.ShortForeground;
        }
        else if (variance > 0)
        {
            // Over - Yellow/Orange
            VarianceText.Foreground = VarianceStatusColors.OverForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.OverBackground;
            VarianceStatusText.Text = "OVER";
            VarianceStatusText.Foreground = VarianceStatusColors.OverForeground;
        }
        else
        {
            // Exact - Green
            VarianceText.Foreground = VarianceStatusColors.ExactForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.ExactBackground;
            VarianceStatusText.Text = "EXACT";
            VarianceStatusText.Foreground = VarianceStatusColors.ExactForeground;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(CashCountInput.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var cashCount))
        {
            MessageBox.Show(
                "Please enter a valid cash count amount.",
                "Invalid Amount",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (cashCount < 0)
        {
            MessageBox.Show(
                "Cash count cannot be negative.",
                "Invalid Amount",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Confirm if there's a large variance
        var variance = cashCount - _expectedCash;
        if (Math.Abs(variance) > 1000)
        {
            var result = MessageBox.Show(
                $"The cash variance is {(variance < 0 ? "short" : "over")} by KSh {Math.Abs(variance):N2}.\n\nAre you sure you want to close the work period with this variance?",
                "Large Variance Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        ClosingCash = cashCount;
        ClosingNotes = string.IsNullOrWhiteSpace(NotesInput.Text) ? null : NotesInput.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
