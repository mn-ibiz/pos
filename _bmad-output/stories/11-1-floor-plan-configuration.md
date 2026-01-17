# Story 11.1: Floor Plan Configuration

Status: done

## Story

As an administrator,
I want to configure the floor plan with tables,
So that orders can be associated with physical locations.

## Acceptance Criteria

1. **Given** admin access
   **When** configuring floor plan
   **Then** admin can add tables with: number, capacity, section/zone

2. **Given** floor configuration
   **When** managing floors
   **Then** admin can define multiple floors (if applicable)

3. **Given** table management
   **When** placing tables
   **Then** admin can set table positions on a visual grid

4. **Given** table lifecycle
   **When** managing tables
   **Then** tables can be activated/deactivated

## Tasks / Subtasks

- [x] Task 1: Create Floor and Table Entities
  - [x] Create Floor entity
  - [x] Create Table entity
  - [x] Create Section/Zone entity
  - [x] Configure EF Core mappings
  - [x] Create database migration

- [x] Task 2: Create Floor Management Screen
  - [x] Create FloorManagementView.xaml
  - [x] Create FloorManagementViewModel
  - [x] Floor CRUD operations
  - [x] Floor list display

- [x] Task 3: Create Table Management Screen
  - [x] Create TableManagementView.xaml (integrated into FloorManagementView)
  - [x] Create TableManagementViewModel (integrated into FloorManagementViewModel)
  - [x] Table CRUD operations
  - [x] Visual grid editor

- [x] Task 4: Implement Visual Grid Editor
  - [x] Create grid canvas control (FloorGridControl)
  - [x] Drag and drop table placement
  - [x] Table resize functionality
  - [x] Save table positions

- [x] Task 5: Implement Section/Zone Management
  - [x] Create SectionManagementView.xaml (SectionDialog)
  - [x] Section CRUD operations
  - [x] Assign tables to sections
  - [x] Color coding for sections

- [x] Task 6: Unit Tests
  - [x] FloorServiceTests with comprehensive coverage for floors, sections, and tables

## Dev Notes

### Floor Entity

```csharp
public class Floor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int GridWidth { get; set; } = 10;  // Grid columns
    public int GridHeight { get; set; } = 10; // Grid rows
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Table> Tables { get; set; } = new List<Table>();
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
```

### Section Entity

```csharp
public class Section
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorCode { get; set; } = "#4CAF50";  // Default green
    public int FloorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Floor Floor { get; set; } = null!;
    public ICollection<Table> Tables { get; set; } = new List<Table>();
}
```

### Table Entity

```csharp
public class Table
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public int FloorId { get; set; }
    public int? SectionId { get; set; }

    // Grid position
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int Width { get; set; } = 1;  // Grid cells wide
    public int Height { get; set; } = 1; // Grid cells tall
    public TableShape Shape { get; set; } = TableShape.Square;

    public TableStatus Status { get; set; } = TableStatus.Available;
    public int? CurrentReceiptId { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime? OccupiedSince { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Floor Floor { get; set; } = null!;
    public Section? Section { get; set; }
    public Receipt? CurrentReceipt { get; set; }
    public User? AssignedUser { get; set; }
}

public enum TableShape
{
    Square,
    Round,
    Rectangle
}

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Unavailable
}
```

### Floor Management Screen

```
+------------------------------------------+
|      FLOOR PLAN CONFIGURATION             |
+------------------------------------------+
| Floors                                    |
| +--------------------------------------+  |
| | [+] Add Floor                        |  |
| +--------------------------------------+  |
| | Main Floor     | 12 tables | [Edit]  |  |
| | Outdoor Area   |  8 tables | [Edit]  |  |
| | VIP Section    |  6 tables | [Edit]  |  |
| +--------------------------------------+  |
|                                           |
| Selected: Main Floor                      |
| +--------------------------------------+  |
| |                                      |  |
| |  [01]    [02]    [03]    [04]        |  |
| |                                      |  |
| |  [05]    [06]    [07]    [08]        |  |
| |                                      |  |
| |     [09]         [10]                |  |
| |                                      |  |
| |  [11]    [12]                        |  |
| |                                      |  |
| +--------------------------------------+  |
|                                           |
| [Add Table]  [Add Section]  [Save Layout] |
+------------------------------------------+
```

### Table Editor Dialog

```
+------------------------------------------+
|      ADD/EDIT TABLE                       |
+------------------------------------------+
|                                           |
|  Table Number: [____12____]               |
|                                           |
|  Capacity:     [____4_____] guests        |
|                                           |
|  Section:      [Outdoor________] [v]      |
|                                           |
|  Shape:                                   |
|  (x) Square  ( ) Round  ( ) Rectangle     |
|                                           |
|  Size:                                    |
|  Width:  [__1__] cells                    |
|  Height: [__1__] cells                    |
|                                           |
|  Status:                                  |
|  [x] Active                               |
|                                           |
|  [Cancel]                     [Save]      |
+------------------------------------------+
```

### FloorManagementViewModel

```csharp
public partial class FloorManagementViewModel : BaseViewModel
{
    private readonly IFloorRepository _floorRepo;
    private readonly ITableRepository _tableRepo;
    private readonly ISectionRepository _sectionRepo;

    [ObservableProperty]
    private ObservableCollection<Floor> _floors = new();

    [ObservableProperty]
    private Floor? _selectedFloor;

    [ObservableProperty]
    private ObservableCollection<Table> _tables = new();

    [ObservableProperty]
    private ObservableCollection<Section> _sections = new();

    [ObservableProperty]
    private Table? _selectedTable;

    public async Task InitializeAsync()
    {
        Floors = new ObservableCollection<Floor>(
            await _floorRepo.GetAllActiveAsync());

        if (Floors.Any())
        {
            SelectedFloor = Floors.First();
        }
    }

    partial void OnSelectedFloorChanged(Floor? value)
    {
        if (value != null)
        {
            LoadFloorTablesAsync(value.Id).ConfigureAwait(false);
        }
    }

    private async Task LoadFloorTablesAsync(int floorId)
    {
        Tables = new ObservableCollection<Table>(
            await _tableRepo.GetByFloorIdAsync(floorId));
        Sections = new ObservableCollection<Section>(
            await _sectionRepo.GetByFloorIdAsync(floorId));
    }

    [RelayCommand]
    private async Task AddFloorAsync()
    {
        var dialog = new FloorDialog();
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true && dialog.Floor != null)
        {
            await _floorRepo.AddAsync(dialog.Floor);
            Floors.Add(dialog.Floor);
            SelectedFloor = dialog.Floor;
        }
    }

    [RelayCommand]
    private async Task AddTableAsync()
    {
        if (SelectedFloor == null) return;

        var dialog = new TableDialog
        {
            FloorId = SelectedFloor.Id,
            Sections = Sections.ToList()
        };

        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true && dialog.Table != null)
        {
            // Find first available grid position
            dialog.Table.GridX = FindNextAvailableX();
            dialog.Table.GridY = FindNextAvailableY();

            await _tableRepo.AddAsync(dialog.Table);
            Tables.Add(dialog.Table);
        }
    }

    [RelayCommand]
    private async Task EditTableAsync(Table table)
    {
        var dialog = new TableDialog
        {
            Table = table.Clone(),
            Sections = Sections.ToList(),
            IsEdit = true
        };

        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            await _tableRepo.UpdateAsync(dialog.Table);
            var index = Tables.IndexOf(table);
            Tables[index] = dialog.Table;
        }
    }

    [RelayCommand]
    private async Task DeleteTableAsync(Table table)
    {
        if (table.Status == TableStatus.Occupied)
        {
            await _dialogService.ShowMessageAsync(
                "Cannot Delete",
                "Cannot delete an occupied table. Please settle the bill first.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmAsync(
            "Delete Table",
            $"Are you sure you want to delete Table {table.TableNumber}?");

        if (confirm)
        {
            table.IsActive = false;
            await _tableRepo.UpdateAsync(table);
            Tables.Remove(table);
        }
    }

    [RelayCommand]
    private async Task SaveLayoutAsync()
    {
        foreach (var table in Tables)
        {
            await _tableRepo.UpdateAsync(table);
        }

        await _dialogService.ShowMessageAsync(
            "Layout Saved",
            "Floor layout has been saved successfully.");
    }

    [RelayCommand]
    private void UpdateTablePosition(TablePositionChangedEventArgs args)
    {
        var table = Tables.FirstOrDefault(t => t.Id == args.TableId);
        if (table != null)
        {
            table.GridX = args.NewX;
            table.GridY = args.NewY;
        }
    }

    private int FindNextAvailableX()
    {
        if (!Tables.Any()) return 0;
        return (Tables.Max(t => t.GridX + t.Width) % (SelectedFloor?.GridWidth ?? 10));
    }

    private int FindNextAvailableY()
    {
        if (!Tables.Any()) return 0;
        var maxX = SelectedFloor?.GridWidth ?? 10;
        if (Tables.Max(t => t.GridX + t.Width) >= maxX)
            return Tables.Max(t => t.GridY + t.Height);
        return Tables.Max(t => t.GridY);
    }
}
```

### Visual Grid Control (XAML)

```xaml
<UserControl x:Class="HospitalityPOS.WPF.Controls.FloorGridControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Canvas x:Name="GridCanvas"
                Background="#F5F5F5"
                AllowDrop="True"
                Drop="GridCanvas_Drop"
                DragOver="GridCanvas_DragOver">

            <!-- Grid lines drawn programmatically -->

            <ItemsControl ItemsSource="{Binding Tables}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <Canvas/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border x:Name="TableBorder"
                                Width="{Binding Width, Converter={StaticResource GridCellConverter}}"
                                Height="{Binding Height, Converter={StaticResource GridCellConverter}}"
                                Background="{Binding Status, Converter={StaticResource TableStatusColorConverter}}"
                                BorderBrush="#333"
                                BorderThickness="2"
                                CornerRadius="{Binding Shape, Converter={StaticResource ShapeRadiusConverter}}"
                                Canvas.Left="{Binding GridX, Converter={StaticResource GridCellConverter}}"
                                Canvas.Top="{Binding GridY, Converter={StaticResource GridCellConverter}}"
                                MouseLeftButtonDown="Table_MouseLeftButtonDown"
                                MouseMove="Table_MouseMove"
                                Cursor="Hand">
                            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                                <TextBlock Text="{Binding TableNumber}"
                                           FontSize="16"
                                           FontWeight="Bold"
                                           HorizontalAlignment="Center"/>
                                <TextBlock Text="{Binding Capacity, StringFormat='({0})'}"
                                           FontSize="12"
                                           HorizontalAlignment="Center"
                                           Foreground="#666"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Canvas>
    </Grid>
</UserControl>
```

### Table Status Colors

```csharp
public class TableStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            TableStatus.Available => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
            TableStatus.Occupied => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Red
            TableStatus.Reserved => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Yellow
            TableStatus.Unavailable => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
            _ => new SolidColorBrush(Colors.White)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### Section Management

```csharp
[RelayCommand]
private async Task AddSectionAsync()
{
    if (SelectedFloor == null) return;

    var dialog = new SectionDialog { FloorId = SelectedFloor.Id };
    var result = await _dialogService.ShowDialogAsync(dialog);

    if (result == true && dialog.Section != null)
    {
        await _sectionRepo.AddAsync(dialog.Section);
        Sections.Add(dialog.Section);
    }
}

public class SectionDialog
{
    public int FloorId { get; set; }
    public Section? Section { get; set; }

    public string SectionName { get; set; } = string.Empty;
    public Color SelectedColor { get; set; } = Colors.Green;

    public List<Color> AvailableColors => new()
    {
        Color.FromRgb(76, 175, 80),   // Green
        Color.FromRgb(33, 150, 243),  // Blue
        Color.FromRgb(255, 152, 0),   // Orange
        Color.FromRgb(156, 39, 176),  // Purple
        Color.FromRgb(0, 188, 212),   // Cyan
        Color.FromRgb(255, 87, 34)    // Deep Orange
    };
}
```

### Floor Service

```csharp
public interface IFloorService
{
    Task<List<Floor>> GetAllFloorsAsync();
    Task<Floor> GetFloorWithTablesAsync(int floorId);
    Task<Floor> CreateFloorAsync(Floor floor);
    Task UpdateFloorAsync(Floor floor);
    Task<bool> DeleteFloorAsync(int floorId);
    Task<Table> CreateTableAsync(Table table);
    Task UpdateTableAsync(Table table);
    Task<bool> DeleteTableAsync(int tableId);
    Task UpdateTableLayoutAsync(IEnumerable<Table> tables);
}

public class FloorService : IFloorService
{
    private readonly ApplicationDbContext _context;

    public async Task<List<Floor>> GetAllFloorsAsync()
    {
        return await _context.Floors
            .Where(f => f.IsActive)
            .Include(f => f.Tables.Where(t => t.IsActive))
            .Include(f => f.Sections.Where(s => s.IsActive))
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Floor> GetFloorWithTablesAsync(int floorId)
    {
        return await _context.Floors
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.Section)
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.CurrentReceipt)
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.AssignedUser)
            .FirstOrDefaultAsync(f => f.Id == floorId)
            ?? throw new NotFoundException($"Floor {floorId} not found");
    }

    public async Task UpdateTableLayoutAsync(IEnumerable<Table> tables)
    {
        foreach (var table in tables)
        {
            var existing = await _context.Tables.FindAsync(table.Id);
            if (existing != null)
            {
                existing.GridX = table.GridX;
                existing.GridY = table.GridY;
                existing.Width = table.Width;
                existing.Height = table.Height;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _context.SaveChangesAsync();
    }
}
```

### Print Floor Layout (80mm)

```
================================================
     FLOOR LAYOUT - MAIN FLOOR
     2025-12-20
================================================

SECTIONS:
- Outdoor Area (Green)
- Indoor (Blue)
- VIP (Purple)

TABLES:
------------------------------------------------
| No  | Section   | Capacity | Status     |
|-------------------------------------------------
| 01  | Indoor    |    4     | Active     |
| 02  | Indoor    |    4     | Active     |
| 03  | Indoor    |    6     | Active     |
| 04  | Indoor    |    4     | Active     |
| 05  | Outdoor   |    4     | Active     |
| 06  | Outdoor   |    4     | Active     |
| 07  | Outdoor   |    8     | Active     |
| 08  | VIP       |    6     | Active     |
| 09  | VIP       |    8     | Active     |
| 10  | VIP       |   10     | Active     |
------------------------------------------------
Total Tables: 10
Total Capacity: 58 guests
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.9-Table-Management]
- [Source: docs/PRD_Hospitality_POS_System.md#TM-001 to TM-010]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created Floor, Section, and Table entities with full EF Core configuration
- Implemented IFloorService interface with comprehensive DTOs (FloorDto, SectionDto, TableDto, TablePositionDto)
- Created FloorService with CRUD operations for floors, sections, and tables
- Added audit logging for all floor/section/table operations
- Created FloorManagementViewModel with table drag-and-drop and layout save functionality
- Created dialog ViewModels for Floor, Section, and Table dialogs
- Built FloorManagementView with 3-column layout (floors list, visual grid, tables/sections)
- Implemented FloorGridControl custom UserControl with visual grid and drag-and-drop
- Created value converters for table status colors, shapes, and grid cell sizing
- Added comprehensive unit tests (60+ test cases) for FloorService

### File List
- src/HospitalityPOS.Core/Entities/Floor.cs
- src/HospitalityPOS.Core/Entities/Section.cs
- src/HospitalityPOS.Core/Entities/Table.cs
- src/HospitalityPOS.Core/Enums/TableEnums.cs
- src/HospitalityPOS.Core/Interfaces/IFloorService.cs
- src/HospitalityPOS.Infrastructure/Data/Configurations/FloorConfiguration.cs
- src/HospitalityPOS.Infrastructure/Services/FloorService.cs
- src/HospitalityPOS.WPF/ViewModels/FloorManagementViewModel.cs
- src/HospitalityPOS.WPF/ViewModels/FloorDialogViewModel.cs
- src/HospitalityPOS.WPF/ViewModels/TableDialogViewModel.cs
- src/HospitalityPOS.WPF/ViewModels/SectionDialogViewModel.cs
- src/HospitalityPOS.WPF/Views/FloorManagementView.xaml
- src/HospitalityPOS.WPF/Views/FloorManagementView.xaml.cs
- src/HospitalityPOS.WPF/Views/FloorDialog.xaml
- src/HospitalityPOS.WPF/Views/FloorDialog.xaml.cs
- src/HospitalityPOS.WPF/Views/TableDialog.xaml
- src/HospitalityPOS.WPF/Views/TableDialog.xaml.cs
- src/HospitalityPOS.WPF/Views/SectionDialog.xaml
- src/HospitalityPOS.WPF/Views/SectionDialog.xaml.cs
- src/HospitalityPOS.WPF/Controls/FloorGridControl.xaml
- src/HospitalityPOS.WPF/Controls/FloorGridControl.xaml.cs
- src/HospitalityPOS.WPF/Converters/FloorConverters.cs
- src/HospitalityPOS.WPF/Resources/Converters.xaml (updated)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (updated)
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
- tests/HospitalityPOS.Business.Tests/Services/FloorServiceTests.cs
