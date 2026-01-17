using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for bulk transferring multiple tables to another waiter.
/// </summary>
public partial class BulkTransferDialog : Window
{
    private readonly User _fromWaiter;
    private readonly ITableTransferService _transferService;
    private readonly int _currentUserId;
    private User? _selectedWaiter;
    private readonly Dictionary<int, CheckBox> _tableCheckboxes = new();
    private List<Table> _tables = new();

    /// <summary>
    /// Gets the transfer result after successful transfer.
    /// </summary>
    public TransferResult? TransferResult { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkTransferDialog"/> class.
    /// </summary>
    /// <param name="fromWaiter">The waiter whose tables are being transferred.</param>
    /// <param name="transferService">The transfer service.</param>
    /// <param name="currentUserId">The current user ID.</param>
    public BulkTransferDialog(User fromWaiter, ITableTransferService transferService, int currentUserId)
    {
        InitializeComponent();
        _fromWaiter = fromWaiter ?? throw new ArgumentNullException(nameof(fromWaiter));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _currentUserId = currentUserId;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        FromWaiterText.Text = _fromWaiter.FullName;
        await LoadTablesAsync();
        await LoadWaitersAsync();
    }

    private async Task LoadTablesAsync()
    {
        try
        {
            _tables = await _transferService.GetTablesByWaiterAsync(_fromWaiter.Id);

            TablesPanel.Children.Clear();
            _tableCheckboxes.Clear();

            if (!_tables.Any())
            {
                var noTablesText = new TextBlock
                {
                    Text = "No occupied tables found",
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                TablesPanel.Children.Add(noTablesText);
                return;
            }

            foreach (var table in _tables)
            {
                var checkbox = new CheckBox
                {
                    Content = CreateTableContent(table),
                    Tag = table,
                    Style = (Style)FindResource("TableCheckboxStyle"),
                    IsChecked = true
                };
                checkbox.Checked += TableCheckbox_CheckedChanged;
                checkbox.Unchecked += TableCheckbox_CheckedChanged;

                _tableCheckboxes[table.Id] = checkbox;
                TablesPanel.Children.Add(checkbox);
            }

            UpdateSummary();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load tables: {ex.Message}");
        }
    }

    private static UIElement CreateTableContent(Table table)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel();

        var tableText = new TextBlock
        {
            Text = $"Table {table.TableNumber}",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };
        infoStack.Children.Add(tableText);

        if (table.OccupiedSince.HasValue)
        {
            var duration = DateTime.UtcNow - table.OccupiedSince.Value;
            var durationText = new TextBlock
            {
                Text = FormatDuration(duration),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!),
                FontSize = 11
            };
            infoStack.Children.Add(durationText);
        }

        Grid.SetColumn(infoStack, 0);
        grid.Children.Add(infoStack);

        var amountText = new TextBlock
        {
            Text = table.CurrentReceipt != null
                ? $"KSh {table.CurrentReceipt.TotalAmount:N0}"
                : "No bill",
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")!),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        Grid.SetColumn(amountText, 1);
        grid.Children.Add(amountText);

        return grid;
    }

    private async Task LoadWaitersAsync()
    {
        try
        {
            var waiters = await _transferService.GetActiveWaitersAsync();

            var availableWaiters = waiters
                .Where(w => w.Id != _fromWaiter.Id)
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
                    Content = waiter.FullName,
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

    private void TableCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        UpdateSummary();
        ClearError();
    }

    private void UpdateSummary()
    {
        var selectedTables = GetSelectedTables();
        var totalValue = selectedTables.Sum(t => t.CurrentReceipt?.TotalAmount ?? 0);

        SelectedCountText.Text = selectedTables.Count.ToString();
        TotalValueText.Text = $"KSh {totalValue:N0}";

        UpdateTransferButtonState();
    }

    private List<Table> GetSelectedTables()
    {
        return _tableCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag as Table)
            .Where(t => t != null)
            .Cast<Table>()
            .ToList();
    }

    private void WaiterRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is User waiter)
        {
            _selectedWaiter = waiter;
            UpdateTransferButtonState();
            ClearError();
        }
    }

    private void UpdateTransferButtonState()
    {
        TransferButton.IsEnabled = _selectedWaiter != null && GetSelectedTables().Any();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var checkbox in _tableCheckboxes.Values)
        {
            checkbox.IsChecked = true;
        }
    }

    private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var checkbox in _tableCheckboxes.Values)
        {
            checkbox.IsChecked = false;
        }
    }

    private async void TransferButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWaiter == null)
        {
            ShowError("Please select a waiter to transfer to.");
            return;
        }

        var selectedTables = GetSelectedTables();
        if (!selectedTables.Any())
        {
            ShowError("Please select at least one table to transfer.");
            return;
        }

        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            TransferButton.IsEnabled = false;

            var request = new BulkTransferRequest
            {
                TableIds = selectedTables.Select(t => t.Id).ToList(),
                NewWaiterId = _selectedWaiter.Id,
                Reason = string.IsNullOrWhiteSpace(ReasonTextBox.Text) ? "Bulk transfer" : ReasonTextBox.Text.Trim(),
                TransferredByUserId = _currentUserId
            };

            TransferResult = await _transferService.BulkTransferAsync(request);

            if (TransferResult.IsSuccess)
            {
                DialogResult = true;
                Close();
            }
            else if (TransferResult.TransferLogs?.Any() == true)
            {
                // Partial success
                var successCount = TransferResult.TransferLogs.Count;
                var errorCount = TransferResult.Errors?.Count ?? 0;
                MessageBox.Show(
                    $"Transferred {successCount} tables successfully.\n{errorCount} transfers failed.",
                    "Partial Transfer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
            UpdateTransferButtonState();
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
