using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Models;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views.Dialogs;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Table Map view.
/// Displays real-time table status with color-coded visualization and interactive features.
/// </summary>
public partial class TableMapViewModel : ViewModelBase, INavigationAware, IDisposable
{
    private readonly IFloorService _floorService;
    private readonly IReceiptService _receiptService;
    private readonly INavigationService _navigationService;
    private readonly IUserService _userService;
    private readonly IPrinterService _printerService;
    private readonly ITableTransferService _tableTransferService;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isDisposed;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the collection of floors.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Floor> _floors = [];

    /// <summary>
    /// Gets or sets the currently selected floor.
    /// </summary>
    [ObservableProperty]
    private Floor? _selectedFloor;

    /// <summary>
    /// Gets or sets the table display items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TableDisplayItem> _tables = [];

    /// <summary>
    /// Gets or sets the selected table.
    /// </summary>
    [ObservableProperty]
    private TableDisplayItem? _selectedTable;

    /// <summary>
    /// Gets or sets the status filter options.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TableStatusFilterOption> _statusFilters = [];

    /// <summary>
    /// Gets or sets the selected status filter.
    /// </summary>
    [ObservableProperty]
    private TableStatusFilterOption? _selectedStatusFilter;

    /// <summary>
    /// Gets or sets the available waiters for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<User> _waiters = [];

    /// <summary>
    /// Gets or sets the selected waiter filter.
    /// </summary>
    [ObservableProperty]
    private User? _selectedWaiterFilter;

    /// <summary>
    /// Gets or sets the grid width of the selected floor.
    /// </summary>
    [ObservableProperty]
    private int _gridWidth = 10;

    /// <summary>
    /// Gets or sets the grid height of the selected floor.
    /// </summary>
    [ObservableProperty]
    private int _gridHeight = 10;

    /// <summary>
    /// Gets or sets the available table count.
    /// </summary>
    [ObservableProperty]
    private int _availableCount;

    /// <summary>
    /// Gets or sets the occupied table count.
    /// </summary>
    [ObservableProperty]
    private int _occupiedCount;

    /// <summary>
    /// Gets or sets the reserved table count.
    /// </summary>
    [ObservableProperty]
    private int _reservedCount;

    /// <summary>
    /// Gets or sets the unavailable table count.
    /// </summary>
    [ObservableProperty]
    private int _unavailableCount;

    /// <summary>
    /// Gets or sets the total bill amount of all occupied tables.
    /// </summary>
    [ObservableProperty]
    private decimal _totalBillAmount;

    /// <summary>
    /// Gets or sets whether auto-refresh is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _autoRefreshEnabled = true;

    /// <summary>
    /// Gets or sets the last refresh time.
    /// </summary>
    [ObservableProperty]
    private DateTime _lastRefreshTime;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TableMapViewModel"/> class.
    /// </summary>
    public TableMapViewModel(
        IFloorService floorService,
        IReceiptService receiptService,
        INavigationService navigationService,
        IUserService userService,
        IPrinterService printerService,
        ITableTransferService tableTransferService,
        ILogger logger) : base(logger)
    {
        _floorService = floorService ?? throw new ArgumentNullException(nameof(floorService));
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
        _tableTransferService = tableTransferService ?? throw new ArgumentNullException(nameof(tableTransferService));

        Title = "Table Map";

        // Initialize status filters
        InitializeStatusFilters();

        // Setup auto-refresh timer (every 30 seconds)
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) =>
        {
            if (AutoRefreshEnabled)
            {
                await RefreshAsync();
            }
        };
    }

    private void InitializeStatusFilters()
    {
        StatusFilters =
        [
            new TableStatusFilterOption { DisplayName = "All Tables", Status = null },
            new TableStatusFilterOption { DisplayName = "Available", Status = TableStatus.Available },
            new TableStatusFilterOption { DisplayName = "Occupied", Status = TableStatus.Occupied },
            new TableStatusFilterOption { DisplayName = "Reserved", Status = TableStatus.Reserved },
            new TableStatusFilterOption { DisplayName = "Unavailable", Status = TableStatus.Unavailable }
        ];

        SelectedStatusFilter = StatusFilters.First();
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = InitializeAsync();
        _refreshTimer.Start();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _refreshTimer.Stop();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load floors
            var floors = await _floorService.GetActiveFloorsWithTablesAsync();
            Floors = new ObservableCollection<Floor>(floors);

            if (Floors.Count > 0 && SelectedFloor == null)
            {
                SelectedFloor = Floors.First();
            }

            // Load waiters for filter
            var users = await _userService.GetUsersByRoleAsync("Waiter");
            Waiters = new ObservableCollection<User>(users);
            // Add "All" option at the beginning
            Waiters.Insert(0, new User { Id = 0, FullName = "All Waiters" });

        }, "Loading table map...");
    }

    partial void OnSelectedFloorChanged(Floor? value)
    {
        if (value != null)
        {
            GridWidth = value.GridWidth;
            GridHeight = value.GridHeight;
            _ = LoadTablesAsync();
        }
        else
        {
            Tables.Clear();
            UpdateStatusCounts();
        }
    }

    partial void OnSelectedStatusFilterChanged(TableStatusFilterOption? value)
    {
        _ = LoadTablesAsync();
    }

    partial void OnSelectedWaiterFilterChanged(User? value)
    {
        _ = LoadTablesAsync();
    }

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private async Task LoadTablesAsync()
    {
        if (SelectedFloor == null) return;

        await ExecuteAsync(async () =>
        {
            var floor = await _floorService.GetFloorWithTablesAsync(SelectedFloor.Id);
            if (floor == null) return;

            var tableItems = floor.Tables
                .Where(t => t.IsActive)
                .Select(TableDisplayItem.FromTable)
                .ToList();

            // Apply status filter
            if (SelectedStatusFilter?.Status.HasValue == true)
            {
                tableItems = tableItems.Where(t => t.Status == SelectedStatusFilter.Status.Value).ToList();
            }

            // Apply waiter filter
            if (SelectedWaiterFilter != null && SelectedWaiterFilter.Id > 0)
            {
                tableItems = tableItems.Where(t =>
                    t.Table.AssignedUserId == SelectedWaiterFilter.Id).ToList();
            }

            Tables = new ObservableCollection<TableDisplayItem>(tableItems);

            // Update counts (from unfiltered data)
            var allTables = floor.Tables.Where(t => t.IsActive).ToList();
            AvailableCount = allTables.Count(t => t.Status == TableStatus.Available);
            OccupiedCount = allTables.Count(t => t.Status == TableStatus.Occupied);
            ReservedCount = allTables.Count(t => t.Status == TableStatus.Reserved);
            UnavailableCount = allTables.Count(t => t.Status == TableStatus.Unavailable);
            TotalBillAmount = allTables
                .Where(t => t.Status == TableStatus.Occupied && t.CurrentReceipt != null)
                .Sum(t => t.CurrentReceipt!.TotalAmount);

            LastRefreshTime = DateTime.Now;

        }, showBusy: false); // Don't show busy indicator for auto-refresh
    }

    private void UpdateStatusCounts()
    {
        AvailableCount = 0;
        OccupiedCount = 0;
        ReservedCount = 0;
        UnavailableCount = 0;
        TotalBillAmount = 0;
    }

    #region Commands

    /// <summary>
    /// Refreshes the table data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTablesAsync();
    }

    /// <summary>
    /// Handles table tap/click.
    /// </summary>
    [RelayCommand]
    private async Task TableTappedAsync(TableDisplayItem? tableItem)
    {
        if (tableItem == null) return;

        SelectedTable = tableItem;

        if (tableItem.Status == TableStatus.Available)
        {
            // Show options: New Order, Reserve
            var result = await DialogService.ShowActionSheetAsync(
                $"Table {tableItem.TableNumber}",
                "What would you like to do?",
                ["New Order", "Reserve Table", "Cancel"]);

            switch (result)
            {
                case "New Order":
                    await CreateNewOrderAsync(tableItem);
                    break;
                case "Reserve Table":
                    await ReserveTableAsync(tableItem);
                    break;
            }
        }
        else if (tableItem.Status == TableStatus.Occupied)
        {
            // Open current order
            if (tableItem.Table.CurrentReceiptId.HasValue)
            {
                _navigationService.NavigateTo<POSViewModel>(new { ReceiptId = tableItem.Table.CurrentReceiptId.Value });
            }
            else
            {
                await DialogService.ShowErrorAsync("No Order", "This table has no current order associated with it.");
            }
        }
        else if (tableItem.Status == TableStatus.Reserved)
        {
            // Show reservation details or seat guests
            var result = await DialogService.ShowActionSheetAsync(
                $"Table {tableItem.TableNumber} - Reserved",
                tableItem.ReservationName.Length > 0 ? $"Reserved: {tableItem.ReservationName}" : "This table is reserved",
                ["Seat Guests", "Cancel Reservation", "Cancel"]);

            switch (result)
            {
                case "Seat Guests":
                    await SeatGuestsAsync(tableItem);
                    break;
                case "Cancel Reservation":
                    await CancelReservationAsync(tableItem);
                    break;
            }
        }
        else if (tableItem.Status == TableStatus.Unavailable)
        {
            var result = await DialogService.ShowActionSheetAsync(
                $"Table {tableItem.TableNumber} - Unavailable",
                "This table is marked as unavailable",
                ["Make Available", "Cancel"]);

            if (result == "Make Available")
            {
                await MakeTableAvailableAsync(tableItem);
            }
        }
    }

    /// <summary>
    /// Handles table double-tap/double-click for quick action.
    /// </summary>
    [RelayCommand]
    private async Task TableDoubleTappedAsync(TableDisplayItem? tableItem)
    {
        if (tableItem == null) return;

        if (tableItem.Status == TableStatus.Occupied)
        {
            // Quick action: Open order
            if (tableItem.Table.CurrentReceiptId.HasValue)
            {
                _navigationService.NavigateTo<POSViewModel>(new { ReceiptId = tableItem.Table.CurrentReceiptId.Value });
            }
        }
        else if (tableItem.Status == TableStatus.Available)
        {
            // Quick action: Create new order
            await CreateNewOrderAsync(tableItem);
        }
    }

    /// <summary>
    /// Opens the table context menu.
    /// </summary>
    [RelayCommand]
    private async Task TableContextMenuAsync(TableDisplayItem? tableItem)
    {
        if (tableItem == null) return;

        var options = new List<string>();

        switch (tableItem.Status)
        {
            case TableStatus.Available:
                options.AddRange(["New Order", "Reserve Table", "Mark Unavailable"]);
                break;
            case TableStatus.Occupied:
                options.AddRange(["View Order", "Print Bill", "Transfer Table"]);
                break;
            case TableStatus.Reserved:
                options.AddRange(["Seat Guests", "Cancel Reservation"]);
                break;
            case TableStatus.Unavailable:
                options.AddRange(["Make Available"]);
                break;
        }

        options.Add("Cancel");

        var result = await DialogService.ShowActionSheetAsync(
            $"Table {tableItem.TableNumber}",
            $"Status: {tableItem.StatusText}",
            options.ToArray());

        await HandleTableActionAsync(tableItem, result);
    }

    /// <summary>
    /// Navigates to floor management.
    /// </summary>
    [RelayCommand]
    private void ManageFloors()
    {
        _navigationService.NavigateTo<FloorManagementViewModel>();
    }

    /// <summary>
    /// Navigates back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Table Actions

    private async Task CreateNewOrderAsync(TableDisplayItem tableItem)
    {
        await ExecuteAsync(async () =>
        {
            // Create new receipt
            var receipt = await _receiptService.CreateReceiptAsync(tableItem.Table.Id);

            // Update table status
            var dto = new TableDto
            {
                TableNumber = tableItem.Table.TableNumber,
                Capacity = tableItem.Table.Capacity,
                FloorId = tableItem.Table.FloorId,
                SectionId = tableItem.Table.SectionId,
                GridX = tableItem.Table.GridX,
                GridY = tableItem.Table.GridY,
                Width = tableItem.Table.Width,
                Height = tableItem.Table.Height,
                Shape = tableItem.Table.Shape,
                IsActive = true
            };

            // Update the table with status change through UpdateTableStatusAsync
            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Occupied,
                receipt.Id,
                SessionService.CurrentUserId,
                SessionService.CurrentUserId);

            _logger.Information("New order created for table {TableNumber}", tableItem.TableNumber);

            // Navigate to POS view
            _navigationService.NavigateTo<POSViewModel>(new { ReceiptId = receipt.Id });

        }, "Creating order...");
    }

    private async Task ReserveTableAsync(TableDisplayItem tableItem)
    {
        // Show reservation dialog
        var guestName = await DialogService.ShowInputAsync(
            "Reserve Table",
            "Enter guest name for reservation:");

        if (string.IsNullOrWhiteSpace(guestName)) return;

        await ExecuteAsync(async () =>
        {
            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Reserved,
                null,
                null,
                SessionService.CurrentUserId);

            _logger.Information("Table {TableNumber} reserved for {GuestName}",
                tableItem.TableNumber, guestName);

            await RefreshAsync();

        }, "Reserving table...");
    }

    private async Task SeatGuestsAsync(TableDisplayItem tableItem)
    {
        // Get guest count
        var guestCountStr = await DialogService.ShowInputAsync(
            "Seat Guests",
            "How many guests?",
            tableItem.Capacity.ToString());

        if (!int.TryParse(guestCountStr, out var guestCount) || guestCount <= 0) return;

        await ExecuteAsync(async () =>
        {
            // Create receipt with guest count
            var receipt = await _receiptService.CreateReceiptAsync(tableItem.Table.Id, guestCount);

            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Occupied,
                receipt.Id,
                SessionService.CurrentUserId,
                SessionService.CurrentUserId);

            _logger.Information("Guests seated at table {TableNumber}", tableItem.TableNumber);

            await RefreshAsync();

        }, "Seating guests...");
    }

    private async Task CancelReservationAsync(TableDisplayItem tableItem)
    {
        var confirm = await DialogService.ShowConfirmAsync(
            "Cancel Reservation",
            $"Are you sure you want to cancel the reservation for Table {tableItem.TableNumber}?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Available,
                null,
                null,
                SessionService.CurrentUserId);

            _logger.Information("Reservation cancelled for table {TableNumber}", tableItem.TableNumber);

            await RefreshAsync();

        }, "Cancelling reservation...");
    }

    private async Task MakeTableAvailableAsync(TableDisplayItem tableItem)
    {
        await ExecuteAsync(async () =>
        {
            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Available,
                null,
                null,
                SessionService.CurrentUserId);

            _logger.Information("Table {TableNumber} marked as available", tableItem.TableNumber);

            await RefreshAsync();

        }, "Updating table...");
    }

    private async Task MarkTableUnavailableAsync(TableDisplayItem tableItem)
    {
        await ExecuteAsync(async () =>
        {
            await _floorService.UpdateTableStatusAsync(
                tableItem.Table.Id,
                TableStatus.Unavailable,
                null,
                null,
                SessionService.CurrentUserId);

            _logger.Information("Table {TableNumber} marked as unavailable", tableItem.TableNumber);

            await RefreshAsync();

        }, "Updating table...");
    }

    private async Task HandleTableActionAsync(TableDisplayItem tableItem, string action)
    {
        switch (action)
        {
            case "New Order":
                await CreateNewOrderAsync(tableItem);
                break;
            case "Reserve Table":
                await ReserveTableAsync(tableItem);
                break;
            case "Seat Guests":
                await SeatGuestsAsync(tableItem);
                break;
            case "Cancel Reservation":
                await CancelReservationAsync(tableItem);
                break;
            case "Make Available":
                await MakeTableAvailableAsync(tableItem);
                break;
            case "Mark Unavailable":
                await MarkTableUnavailableAsync(tableItem);
                break;
            case "View Order":
                if (tableItem.Table.CurrentReceiptId.HasValue)
                {
                    _navigationService.NavigateTo<POSViewModel>(new { ReceiptId = tableItem.Table.CurrentReceiptId.Value });
                }
                break;
            case "Print Bill":
                if (tableItem.Table.CurrentReceiptId.HasValue)
                {
                    try
                    {
                        var receipt = await _receiptService.GetReceiptByIdAsync(tableItem.Table.CurrentReceiptId.Value);
                        if (receipt != null)
                        {
                            var result = await _printerService.PrintReceiptAsync(receipt);
                            if (!result.Success)
                            {
                                await DialogService.ShowErrorAsync($"Failed to print bill: {result.ErrorMessage}");
                            }
                        }
                        else
                        {
                            await DialogService.ShowErrorAsync("Receipt not found for this table.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to print bill for table {TableName}", tableItem.Table.Name);
                        await DialogService.ShowErrorAsync($"Failed to print bill: {ex.Message}");
                    }
                }
                else
                {
                    await DialogService.ShowMessageAsync("Print Bill", "No active order on this table.");
                }
                break;
            case "Transfer Table":
                await TransferTableAsync(tableItem);
                break;
        }
    }

    /// <summary>
    /// Shows the table transfer dialog and processes the transfer.
    /// </summary>
    /// <param name="tableItem">The table to transfer.</param>
    private async Task TransferTableAsync(TableDisplayItem tableItem)
    {
        try
        {
            // Only occupied tables can be transferred
            if (tableItem.Table.Status != TableStatus.Occupied)
            {
                await DialogService.ShowMessageAsync("Transfer Table", "Only occupied tables can be transferred.");
                return;
            }

            // Get current user ID from session
            var currentUserId = SessionService.CurrentUserId ?? 0;
            if (currentUserId == 0)
            {
                await DialogService.ShowErrorAsync("Unable to identify current user. Please log in again.");
                return;
            }

            // Show the transfer dialog
            var dialog = new TableTransferDialog(
                tableItem.Table,
                _tableTransferService,
                currentUserId)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();

            if (result == true && dialog.TransferResult?.IsSuccess == true)
            {
                Logger.Information(
                    "Table {TableName} transferred from waiter {FromWaiter} to {ToWaiter}",
                    tableItem.Table.Name,
                    tableItem.Table.AssignedUser?.FullName ?? "Unknown",
                    dialog.TransferResult.NewWaiterName);

                // Refresh the table data
                await LoadTablesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to transfer table {TableName}", tableItem.Table.Name);
            await DialogService.ShowErrorAsync($"Failed to transfer table: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _refreshTimer.Stop();
        }

        _isDisposed = true;
    }
}

/// <summary>
/// Filter option for table status.
/// </summary>
public class TableStatusFilterOption
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status value (null means all).
    /// </summary>
    public TableStatus? Status { get; set; }

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
