# Story 11.2: Table Status Display

Status: done

## Story

As a waiter,
I want to see table statuses at a glance,
So that I know which tables need attention.

## Acceptance Criteria

1. **Given** floor plan is configured
   **When** viewing table map
   **Then** tables should show visual status: Available (green), Occupied (red), Reserved (yellow)

2. **Given** occupied tables
   **When** viewing table map
   **Then** occupied tables should show: assigned waiter, current bill amount

3. **Given** table timing
   **When** viewing table map
   **Then** time since seated can be displayed (optional)

4. **Given** table interaction
   **When** tapping a table
   **Then** tapping a table should open/show its current order

## Tasks / Subtasks

- [x] Task 1: Create Table Map View
  - [x] Create TableMapView.xaml
  - [x] Create TableMapViewModel
  - [x] Display floor selector
  - [x] Render table grid

- [x] Task 2: Implement Status Visualization
  - [x] Color-coded table status (TableStatusColorConverter)
  - [x] Occupied time display (OccupiedDuration)
  - [x] Bill amount display (BillAmountDisplay)
  - [x] Guest count display

- [x] Task 3: Implement Real-time Updates
  - [x] Auto-refresh table status (30-second DispatcherTimer)
  - [x] Polling mechanism for updates
  - [x] Handle status changes via LoadTablesAsync

- [x] Task 4: Implement Table Interaction
  - [x] Single tap to view/interact with table
  - [x] Double tap for quick action (open order)
  - [x] Context menu for actions (right-click)
  - [x] Table selection highlight

- [x] Task 5: Add Filter and Search
  - [x] Filter by status (TableStatusFilterOption)
  - [x] Filter by waiter (SelectedWaiterFilter)
  - [x] Floor selector for multi-floor support

## Dev Notes

### Table Map Screen

```
+------------------------------------------+
|      TABLE MAP                            |
+------------------------------------------+
| Floor: [Main Floor_______] [v]  [Refresh] |
| Filter: [All_____] [v]  Waiter: [All] [v] |
+------------------------------------------+
|                                           |
|   +-------+  +-------+  +-------+         |
|   |  01   |  |  02   |  |  03   |         |
|   | Empty |  | John  |  | Mary  |         |
|   |       |  |  850  |  | 1,250 |         |
|   | Avail |  | 45min |  | 1h15m |         |
|   +-------+  +-------+  +-------+         |
|                                           |
|   +-------+  +-------+  +-------+         |
|   |  04   |  |  05   |  |  06   |         |
|   | Resvd |  | Empty |  | Peter |         |
|   | 18:00 |  |       |  | 2,100 |         |
|   | VIP   |  | Avail |  | 30min |         |
|   +-------+  +-------+  +-------+         |
|                                           |
|   +-------------+  +-------+              |
|   |     07      |  |  08   |              |
|   |   John      |  | Empty |              |
|   |   3,500     |  |       |              |
|   |   2h30m     |  | Avail |              |
|   +-------------+  +-------+              |
|                                           |
+------------------------------------------+
| Legend: [Green] Available [Red] Occupied  |
|         [Yellow] Reserved [Gray] N/A      |
+------------------------------------------+
```

### Table Card Display

```
+------------------+
|       01         |  <- Table Number (large)
|                  |
|   John Smith     |  <- Assigned Waiter
|   KSh 2,350      |  <- Current Bill Amount
|                  |
|   45 min         |  <- Time Occupied
|   4/4 guests     |  <- Guests/Capacity
+------------------+
   [OCCUPIED]         <- Status Badge
```

### TableMapViewModel

```csharp
public partial class TableMapViewModel : BaseViewModel
{
    private readonly IFloorService _floorService;
    private readonly IReceiptService _receiptService;
    private readonly INavigationService _navigationService;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<Floor> _floors = new();

    [ObservableProperty]
    private Floor? _selectedFloor;

    [ObservableProperty]
    private ObservableCollection<TableDisplayItem> _tables = new();

    [ObservableProperty]
    private TableStatus? _statusFilter;

    [ObservableProperty]
    private User? _waiterFilter;

    [ObservableProperty]
    private ObservableCollection<User> _waiters = new();

    [ObservableProperty]
    private TableDisplayItem? _selectedTable;

    public TableMapViewModel(
        IFloorService floorService,
        IReceiptService receiptService,
        INavigationService navigationService)
    {
        _floorService = floorService;
        _receiptService = receiptService;
        _navigationService = navigationService;

        // Auto-refresh every 30 seconds
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshAsync();
    }

    public async Task InitializeAsync()
    {
        Floors = new ObservableCollection<Floor>(
            await _floorService.GetAllFloorsAsync());

        if (Floors.Any())
        {
            SelectedFloor = Floors.First();
        }

        Waiters = new ObservableCollection<User>(
            await _userService.GetActiveWaitersAsync());

        _refreshTimer.Start();
    }

    partial void OnSelectedFloorChanged(Floor? value)
    {
        if (value != null)
        {
            _ = LoadTablesAsync();
        }
    }

    private async Task LoadTablesAsync()
    {
        if (SelectedFloor == null) return;

        var floor = await _floorService.GetFloorWithTablesAsync(SelectedFloor.Id);
        var tableItems = floor.Tables
            .Where(t => t.IsActive)
            .Select(t => new TableDisplayItem
            {
                Table = t,
                TableNumber = t.TableNumber,
                Status = t.Status,
                StatusColor = GetStatusColor(t.Status),
                WaiterName = t.AssignedUser?.FullName ?? string.Empty,
                BillAmount = t.CurrentReceipt?.TotalAmount ?? 0,
                OccupiedDuration = t.OccupiedSince.HasValue
                    ? FormatDuration(DateTime.Now - t.OccupiedSince.Value)
                    : string.Empty,
                GuestCount = t.CurrentReceipt?.GuestCount ?? 0,
                Capacity = t.Capacity,
                GridX = t.GridX,
                GridY = t.GridY,
                Width = t.Width,
                Height = t.Height,
                Shape = t.Shape,
                SectionColor = t.Section?.ColorCode ?? "#4CAF50"
            })
            .ToList();

        // Apply filters
        if (StatusFilter.HasValue)
        {
            tableItems = tableItems.Where(t => t.Status == StatusFilter.Value).ToList();
        }

        if (WaiterFilter != null)
        {
            tableItems = tableItems.Where(t =>
                t.Table.AssignedUserId == WaiterFilter.Id).ToList();
        }

        Tables = new ObservableCollection<TableDisplayItem>(tableItems);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTablesAsync();
    }

    [RelayCommand]
    private async Task TableTappedAsync(TableDisplayItem tableItem)
    {
        SelectedTable = tableItem;

        if (tableItem.Status == TableStatus.Available)
        {
            // Show options: New Order, Reserve
            var action = await _dialogService.ShowActionSheetAsync(
                $"Table {tableItem.TableNumber}",
                new[] { "New Order", "Reserve Table", "Cancel" });

            switch (action)
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
                await _navigationService.NavigateToAsync<OrderViewModel>(
                    new { ReceiptId = tableItem.Table.CurrentReceiptId.Value });
            }
        }
        else if (tableItem.Status == TableStatus.Reserved)
        {
            // Show reservation details or seat guests
            var action = await _dialogService.ShowActionSheetAsync(
                $"Table {tableItem.TableNumber} - Reserved",
                new[] { "Seat Guests", "Cancel Reservation", "Cancel" });

            if (action == "Seat Guests")
            {
                await SeatGuestsAsync(tableItem);
            }
            else if (action == "Cancel Reservation")
            {
                await CancelReservationAsync(tableItem);
            }
        }
    }

    [RelayCommand]
    private async Task TableDoubleTappedAsync(TableDisplayItem tableItem)
    {
        // Quick action: Open order or create new
        if (tableItem.Status == TableStatus.Occupied)
        {
            if (tableItem.Table.CurrentReceiptId.HasValue)
            {
                await _navigationService.NavigateToAsync<OrderViewModel>(
                    new { ReceiptId = tableItem.Table.CurrentReceiptId.Value });
            }
        }
        else if (tableItem.Status == TableStatus.Available)
        {
            await CreateNewOrderAsync(tableItem);
        }
    }

    private async Task CreateNewOrderAsync(TableDisplayItem tableItem)
    {
        // Create receipt and assign to table
        var receipt = await _receiptService.CreateReceiptAsync(new CreateReceiptRequest
        {
            TableId = tableItem.Table.Id
        });

        // Update table status
        tableItem.Table.Status = TableStatus.Occupied;
        tableItem.Table.CurrentReceiptId = receipt.Id;
        tableItem.Table.AssignedUserId = _authService.CurrentUser?.Id;
        tableItem.Table.OccupiedSince = DateTime.Now;

        await _floorService.UpdateTableAsync(tableItem.Table);

        // Navigate to order screen
        await _navigationService.NavigateToAsync<OrderViewModel>(
            new { ReceiptId = receipt.Id });
    }

    private async Task SeatGuestsAsync(TableDisplayItem tableItem)
    {
        // Change status from Reserved to Occupied
        var guestCount = await _dialogService.ShowNumberInputAsync(
            "Guest Count",
            "How many guests?",
            tableItem.Capacity);

        if (guestCount > 0)
        {
            var receipt = await _receiptService.CreateReceiptAsync(new CreateReceiptRequest
            {
                TableId = tableItem.Table.Id,
                GuestCount = guestCount
            });

            tableItem.Table.Status = TableStatus.Occupied;
            tableItem.Table.CurrentReceiptId = receipt.Id;
            tableItem.Table.OccupiedSince = DateTime.Now;

            await _floorService.UpdateTableAsync(tableItem.Table);
            await RefreshAsync();
        }
    }

    private string GetStatusColor(TableStatus status)
    {
        return status switch
        {
            TableStatus.Available => "#4CAF50",    // Green
            TableStatus.Occupied => "#F44336",     // Red
            TableStatus.Reserved => "#FFC107",     // Yellow
            TableStatus.Unavailable => "#9E9E9E", // Gray
            _ => "#FFFFFF"
        };
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes} min";
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
    }
}

public class TableDisplayItem
{
    public Table Table { get; set; } = null!;
    public string TableNumber { get; set; } = string.Empty;
    public TableStatus Status { get; set; }
    public string StatusColor { get; set; } = string.Empty;
    public string WaiterName { get; set; } = string.Empty;
    public decimal BillAmount { get; set; }
    public string OccupiedDuration { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int Capacity { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TableShape Shape { get; set; }
    public string SectionColor { get; set; } = string.Empty;

    public string BillAmountDisplay => BillAmount > 0
        ? $"KSh {BillAmount:N0}"
        : string.Empty;

    public string GuestDisplay => Status == TableStatus.Occupied
        ? $"{GuestCount}/{Capacity}"
        : string.Empty;
}
```

### Table Map View XAML

```xaml
<UserControl x:Class="HospitalityPOS.WPF.Views.TableMapView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:HospitalityPOS.WPF.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header with filters -->
        <Border Grid.Row="0" Background="#2196F3" Padding="16">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <ComboBox Grid.Column="0"
                          ItemsSource="{Binding Floors}"
                          SelectedItem="{Binding SelectedFloor}"
                          DisplayMemberPath="Name"
                          Width="200"
                          Margin="0,0,16,0"/>

                <ComboBox Grid.Column="1"
                          ItemsSource="{Binding StatusFilters}"
                          SelectedItem="{Binding StatusFilter}"
                          Width="150"
                          Margin="0,0,16,0"/>

                <ComboBox Grid.Column="2"
                          ItemsSource="{Binding Waiters}"
                          SelectedItem="{Binding WaiterFilter}"
                          DisplayMemberPath="FullName"
                          Width="150"
                          Margin="0,0,16,0"/>

                <Button Grid.Column="3"
                        Content="Refresh"
                        Command="{Binding RefreshCommand}"
                        Width="100"/>
            </Grid>
        </Border>

        <!-- Table Grid -->
        <ScrollViewer Grid.Row="1" HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding Tables}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <Canvas Width="1000" Height="800"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border x:Name="TableCard"
                                Width="{Binding Width, Converter={StaticResource GridSizeConverter}}"
                                Height="{Binding Height, Converter={StaticResource GridSizeConverter}}"
                                Background="{Binding StatusColor}"
                                BorderBrush="#333"
                                BorderThickness="2"
                                CornerRadius="{Binding Shape, Converter={StaticResource ShapeConverter}}"
                                Canvas.Left="{Binding GridX, Converter={StaticResource GridPositionConverter}}"
                                Canvas.Top="{Binding GridY, Converter={StaticResource GridPositionConverter}}"
                                Cursor="Hand">
                            <Border.InputBindings>
                                <MouseBinding MouseAction="LeftClick"
                                              Command="{Binding DataContext.TableTappedCommand,
                                                       RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                                              CommandParameter="{Binding}"/>
                                <MouseBinding MouseAction="LeftDoubleClick"
                                              Command="{Binding DataContext.TableDoubleTappedCommand,
                                                       RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                                              CommandParameter="{Binding}"/>
                            </Border.InputBindings>

                            <Grid Margin="8">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="*"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>

                                <!-- Table Number -->
                                <TextBlock Grid.Row="0"
                                           Text="{Binding TableNumber}"
                                           FontSize="24"
                                           FontWeight="Bold"
                                           Foreground="White"
                                           HorizontalAlignment="Center"/>

                                <!-- Details -->
                                <StackPanel Grid.Row="1" VerticalAlignment="Center">
                                    <TextBlock Text="{Binding WaiterName}"
                                               FontSize="12"
                                               Foreground="White"
                                               HorizontalAlignment="Center"
                                               Visibility="{Binding WaiterName,
                                                           Converter={StaticResource StringToVisibilityConverter}}"/>

                                    <TextBlock Text="{Binding BillAmountDisplay}"
                                               FontSize="14"
                                               FontWeight="SemiBold"
                                               Foreground="White"
                                               HorizontalAlignment="Center"
                                               Visibility="{Binding BillAmountDisplay,
                                                           Converter={StaticResource StringToVisibilityConverter}}"/>
                                </StackPanel>

                                <!-- Footer -->
                                <StackPanel Grid.Row="2" Orientation="Horizontal"
                                            HorizontalAlignment="Center">
                                    <TextBlock Text="{Binding OccupiedDuration}"
                                               FontSize="11"
                                               Foreground="White"
                                               Margin="0,0,8,0"/>
                                    <TextBlock Text="{Binding GuestDisplay}"
                                               FontSize="11"
                                               Foreground="White"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Legend -->
        <Border Grid.Row="2" Background="#F5F5F5" Padding="16">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                <StackPanel Orientation="Horizontal" Margin="0,0,24,0">
                    <Border Width="20" Height="20" Background="#4CAF50"
                            CornerRadius="4" Margin="0,0,8,0"/>
                    <TextBlock Text="Available" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,24,0">
                    <Border Width="20" Height="20" Background="#F44336"
                            CornerRadius="4" Margin="0,0,8,0"/>
                    <TextBlock Text="Occupied" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,24,0">
                    <Border Width="20" Height="20" Background="#FFC107"
                            CornerRadius="4" Margin="0,0,8,0"/>
                    <TextBlock Text="Reserved" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal">
                    <Border Width="20" Height="20" Background="#9E9E9E"
                            CornerRadius="4" Margin="0,0,8,0"/>
                    <TextBlock Text="Unavailable" VerticalAlignment="Center"/>
                </StackPanel>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

### Table Status Summary Print (80mm)

```
================================================
     TABLE STATUS SUMMARY
     MAIN FLOOR
     2025-12-20 18:30
================================================

STATUS OVERVIEW:
------------------------------------------------
Available:     4 tables
Occupied:      6 tables
Reserved:      2 tables
------------------------------------------------
Total:        12 tables

OCCUPIED TABLES:
------------------------------------------------
Table | Waiter   | Amount  | Time
------|----------|---------|--------
  02  | John     |     850 | 45 min
  03  | Mary     |   1,250 | 1h 15m
  06  | Peter    |   2,100 | 30 min
  07  | John     |   3,500 | 2h 30m
  09  | Mary     |   4,200 | 1h 45m
  11  | Peter    |   1,850 | 55 min
------|----------|---------|--------
Total              13,750

RESERVED TABLES:
------------------------------------------------
Table | Time  | Name      | Guests
------|-------|-----------|--------
  04  | 18:00 | VIP Party |    6
  10  | 19:30 | Birthday  |    8
================================================
```

### Real-time Status Updates (SignalR)

```csharp
public class TableHub : Hub
{
    public async Task JoinFloor(int floorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"floor_{floorId}");
    }

    public async Task LeaveFloor(int floorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"floor_{floorId}");
    }
}

public class TableStatusNotifier
{
    private readonly IHubContext<TableHub> _hubContext;

    public async Task NotifyTableStatusChangedAsync(Table table)
    {
        await _hubContext.Clients
            .Group($"floor_{table.FloorId}")
            .SendAsync("TableStatusChanged", new TableStatusUpdate
            {
                TableId = table.Id,
                Status = table.Status,
                WaiterName = table.AssignedUser?.FullName,
                BillAmount = table.CurrentReceipt?.TotalAmount,
                OccupiedSince = table.OccupiedSince
            });
    }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.9.2-Table-Status]
- [Source: docs/PRD_Hospitality_POS_System.md#TM-011 to TM-020]

## Dev Agent Record

### Agent Model Used
Claude claude-opus-4-5-20251101

### Completion Notes List
- Created TableDisplayItem model with all display properties and factory method
- Created TableMapViewModel with floor selector, status filtering, waiter filtering, auto-refresh timer
- Implemented table interaction commands (tap, double-tap, context menu)
- Created TableMapView.xaml with Canvas-based table layout matching floor positions
- Added ZeroToVisibilityConverter for empty state display
- Implemented status counts and total bill amount summary in footer
- Used DispatcherTimer for 30-second auto-refresh polling (SignalR deferred)
- Registered TableMapViewModel in DI container

### File List
- src/HospitalityPOS.WPF/Models/TableDisplayItem.cs (NEW)
- src/HospitalityPOS.WPF/ViewModels/TableMapViewModel.cs (NEW)
- src/HospitalityPOS.WPF/Views/TableMapView.xaml (NEW)
- src/HospitalityPOS.WPF/Views/TableMapView.xaml.cs (NEW)
- src/HospitalityPOS.WPF/Converters/BoolToVisibilityConverters.cs (UPDATED - added ZeroToVisibilityConverter)
- src/HospitalityPOS.WPF/Resources/Converters.xaml (UPDATED - registered new converters)
- src/HospitalityPOS.Core/Interfaces/IFloorService.cs (UPDATED - added UpdateTableStatusAsync, ClearTableAsync)
- src/HospitalityPOS.Infrastructure/Services/FloorService.cs (UPDATED - implemented new methods)
- src/HospitalityPOS.WPF/App.xaml.cs (UPDATED - registered TableMapViewModel)
