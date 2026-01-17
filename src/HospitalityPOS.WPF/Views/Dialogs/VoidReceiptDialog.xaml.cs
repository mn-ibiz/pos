using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for voiding a receipt with reason selection.
/// </summary>
public partial class VoidReceiptDialog : Window
{
    private readonly Receipt _receipt;
    private readonly IReadOnlyList<VoidReason> _voidReasons;
    private VoidReason? _selectedReason;

    /// <summary>
    /// Gets the selected void reason ID.
    /// </summary>
    public int? SelectedVoidReasonId => _selectedReason?.Id;

    /// <summary>
    /// Gets the additional notes entered by the user.
    /// </summary>
    public string AdditionalNotes => NotesTextBox.Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoidReceiptDialog"/> class.
    /// </summary>
    public VoidReceiptDialog(Receipt receipt, IReadOnlyList<VoidReason> voidReasons)
    {
        InitializeComponent();
        _receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        _voidReasons = voidReasons ?? throw new ArgumentNullException(nameof(voidReasons));

        InitializeDialog();
    }

    private void InitializeDialog()
    {
        // Populate receipt summary
        ReceiptNumberText.Text = _receipt.ReceiptNumber;
        TableText.Text = _receipt.TableNumber ?? "N/A";
        StatusText.Text = _receipt.Status.ToString();
        TotalText.Text = $"KSh {_receipt.TotalAmount:N2}";
        ItemCountText.Text = _receipt.ReceiptItems.Count.ToString();
        CreatedByText.Text = _receipt.Owner?.DisplayName ?? "Unknown";

        // Update status color
        StatusText.Foreground = _receipt.Status switch
        {
            ReceiptStatus.Pending => new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FBBF24")!),
            ReceiptStatus.Settled => new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")!),
            _ => new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!)
        };

        // Populate void reasons
        PopulateVoidReasons();
    }

    private void PopulateVoidReasons()
    {
        ReasonsPanel.Children.Clear();

        foreach (var reason in _voidReasons)
        {
            var radioButton = new RadioButton
            {
                Content = reason.Name,
                Tag = reason,
                Style = (Style)FindResource("ReasonRadioButton"),
                GroupName = "VoidReasons"
            };

            radioButton.Checked += ReasonRadioButton_Checked;
            ReasonsPanel.Children.Add(radioButton);
        }
    }

    private void ReasonRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is VoidReason reason)
        {
            _selectedReason = reason;
            VoidButton.IsEnabled = true;

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
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void VoidButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate selection
        if (_selectedReason is null)
        {
            ShowError("Please select a void reason.");
            return;
        }

        // Validate notes if required
        if (_selectedReason.RequiresNote && string.IsNullOrWhiteSpace(NotesTextBox.Text))
        {
            ShowError("Additional notes are required for this void reason.");
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
/// Result from the void receipt dialog.
/// </summary>
public class VoidReceiptDialogResult
{
    /// <summary>
    /// Gets or sets the selected void reason ID.
    /// </summary>
    public int VoidReasonId { get; set; }

    /// <summary>
    /// Gets or sets the additional notes.
    /// </summary>
    public string? AdditionalNotes { get; set; }
}
