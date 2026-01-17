using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for transferring a table to another waiter.
/// </summary>
public partial class TableTransferDialog : Window
{
    private readonly Table _table;
    private readonly ITableTransferService _transferService;
    private readonly int _currentUserId;
    private User? _selectedWaiter;

    /// <summary>
    /// Gets the transfer result after successful transfer.
    /// </summary>
    public TransferResult? TransferResult { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableTransferDialog"/> class.
    /// </summary>
    /// <param name="table">The table to transfer.</param>
    /// <param name="transferService">The transfer service.</param>
    /// <param name="currentUserId">The current user ID.</param>
    public TableTransferDialog(Table table, ITableTransferService transferService, int currentUserId)
    {
        InitializeComponent();
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _currentUserId = currentUserId;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeTableInfo();
        await LoadWaitersAsync();
    }

    private void InitializeTableInfo()
    {
        TableInfoText.Text = $"Table {_table.TableNumber}";
        BillAmountText.Text = _table.CurrentReceipt != null
            ? $"KSh {_table.CurrentReceipt.TotalAmount:N0}"
            : "No active bill";

        if (_table.OccupiedSince.HasValue)
        {
            var duration = DateTime.UtcNow - _table.OccupiedSince.Value;
            OccupiedTimeText.Text = FormatDuration(duration);
        }
        else
        {
            OccupiedTimeText.Text = "Unknown";
        }

        CurrentWaiterText.Text = _table.AssignedUser?.FullName ?? "Not Assigned";
    }

    private async Task LoadWaitersAsync()
    {
        try
        {
            var waiters = await _transferService.GetActiveWaitersAsync();

            // Filter out the current waiter
            var availableWaiters = waiters
                .Where(w => w.Id != _table.AssignedUserId)
                .OrderBy(w => w.FullName)
                .ToList();

            WaitersPanel.Children.Clear();

            if (!availableWaiters.Any())
            {
                var noWaitersText = new TextBlock
                {
                    Text = "No other waiters available",
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                WaitersPanel.Children.Add(noWaitersText);
                return;
            }

            foreach (var waiter in availableWaiters)
            {
                var radioButton = new RadioButton
                {
                    Content = CreateWaiterContent(waiter),
                    Tag = waiter,
                    Style = (Style)FindResource("WaiterRadioStyle"),
                    GroupName = "Waiters"
                };
                radioButton.Checked += WaiterRadioButton_Checked;

                WaitersPanel.Children.Add(radioButton);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load waiters: {ex.Message}");
        }
    }

    private static UIElement CreateWaiterContent(User waiter)
    {
        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

        var nameText = new TextBlock
        {
            Text = waiter.FullName,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };
        stackPanel.Children.Add(nameText);

        if (!string.IsNullOrEmpty(waiter.Username))
        {
            var usernameText = new TextBlock
            {
                Text = $"@{waiter.Username}",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
                FontSize = 11
            };
            stackPanel.Children.Add(usernameText);
        }

        return stackPanel;
    }

    private void WaiterRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is User waiter)
        {
            _selectedWaiter = waiter;
            TransferButton.IsEnabled = true;
            ClearError();
        }
    }

    private async void TransferButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWaiter == null)
        {
            ShowError("Please select a waiter to transfer to.");
            return;
        }

        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            TransferButton.IsEnabled = false;

            var request = new TransferTableRequest
            {
                TableId = _table.Id,
                NewWaiterId = _selectedWaiter.Id,
                Reason = string.IsNullOrWhiteSpace(ReasonTextBox.Text) ? null : ReasonTextBox.Text.Trim(),
                TransferredByUserId = _currentUserId
            };

            TransferResult = await _transferService.TransferTableAsync(request);

            if (TransferResult.IsSuccess)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError(TransferResult.ErrorMessage ?? "Transfer failed.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            TransferButton.IsEnabled = _selectedWaiter != null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
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

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes} min";
    }
}
