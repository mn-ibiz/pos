using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Event args for table position changes in the grid.
/// </summary>
public class TablePositionChangedEventArgs : EventArgs
{
    public int TableId { get; set; }
    public int NewX { get; set; }
    public int NewY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// ViewModel for the Floor Plan Configuration view.
/// Manages floors, sections, and tables for hospitality table management.
/// </summary>
public partial class FloorManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly IFloorService _floorService;
    private readonly INavigationService _navigationService;

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
    /// Gets or sets the tables on the selected floor.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Table> _tables = [];

    /// <summary>
    /// Gets or sets the sections on the selected floor.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Section> _sections = [];

    /// <summary>
    /// Gets or sets the currently selected table.
    /// </summary>
    [ObservableProperty]
    private Table? _selectedTable;

    /// <summary>
    /// Gets or sets the currently selected section.
    /// </summary>
    [ObservableProperty]
    private Section? _selectedSection;

    /// <summary>
    /// Gets or sets whether the layout has unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Gets or sets the total table count on the selected floor.
    /// </summary>
    [ObservableProperty]
    private int _tableCount;

    /// <summary>
    /// Gets or sets the total capacity on the selected floor.
    /// </summary>
    [ObservableProperty]
    private int _totalCapacity;

    #endregion

    /// <summary>
    /// Gets whether the user can add floors.
    /// </summary>
    public bool CanAddFloor => HasPermission("ManageFloors");

    /// <summary>
    /// Gets whether the user can add tables.
    /// </summary>
    public bool CanAddTable => HasPermission("ManageTables") && SelectedFloor != null;

    /// <summary>
    /// Gets whether the user can add sections.
    /// </summary>
    public bool CanAddSection => HasPermission("ManageSections") && SelectedFloor != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorManagementViewModel"/> class.
    /// </summary>
    public FloorManagementViewModel(
        IFloorService floorService,
        INavigationService navigationService,
        ILogger logger) : base(logger)
    {
        _floorService = floorService ?? throw new ArgumentNullException(nameof(floorService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Floor Plan Configuration";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadFloorsAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Prompt for unsaved changes if any
        if (HasUnsavedChanges)
        {
            // Changes will be lost - user should save first
        }
    }

    private async Task LoadFloorsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var floors = await _floorService.GetAllFloorsAsync();
            Floors = new ObservableCollection<Floor>(floors);

            if (Floors.Count > 0 && SelectedFloor == null)
            {
                SelectedFloor = Floors.First();
            }
        }, "Loading floors...");
    }

    partial void OnSelectedFloorChanged(Floor? value)
    {
        if (value != null)
        {
            _ = LoadFloorDetailsAsync(value.Id);
        }
        else
        {
            Tables.Clear();
            Sections.Clear();
            TableCount = 0;
            TotalCapacity = 0;
        }

        OnPropertyChanged(nameof(CanAddTable));
        OnPropertyChanged(nameof(CanAddSection));
    }

    private async Task LoadFloorDetailsAsync(int floorId)
    {
        await ExecuteAsync(async () =>
        {
            var floor = await _floorService.GetFloorWithTablesAsync(floorId);
            if (floor != null)
            {
                Tables = new ObservableCollection<Table>(floor.Tables.Where(t => t.IsActive));
                Sections = new ObservableCollection<Section>(floor.Sections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder));
                TableCount = Tables.Count;
                TotalCapacity = Tables.Sum(t => t.Capacity);
            }
        }, "Loading floor details...");
    }

    #region Floor Operations

    /// <summary>
    /// Adds a new floor.
    /// </summary>
    [RelayCommand]
    private async Task AddFloorAsync()
    {
        if (!RequirePermission("ManageFloors", "add floors"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<FloorDialogViewModel>();
            viewModel.Initialize();

            var dialog = new FloorDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Floor != null)
            {
                var dto = new FloorDto
                {
                    Name = viewModel.Floor.Name,
                    DisplayOrder = Floors.Count,
                    GridWidth = viewModel.Floor.GridWidth,
                    GridHeight = viewModel.Floor.GridHeight,
                    IsActive = true
                };

                var createdFloor = await _floorService.CreateFloorAsync(dto, SessionService.CurrentUserId);
                Floors.Add(createdFloor);
                SelectedFloor = createdFloor;

                _logger.Information("Floor '{FloorName}' created", createdFloor.Name);
            }
        }, "Adding floor...");
    }

    /// <summary>
    /// Edits the selected floor.
    /// </summary>
    [RelayCommand]
    private async Task EditFloorAsync()
    {
        if (SelectedFloor == null)
            return;

        if (!RequirePermission("ManageFloors", "edit floors"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<FloorDialogViewModel>();
            viewModel.Initialize(SelectedFloor);

            var dialog = new FloorDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Floor != null)
            {
                var dto = new FloorDto
                {
                    Name = viewModel.Floor.Name,
                    DisplayOrder = SelectedFloor.DisplayOrder,
                    GridWidth = viewModel.Floor.GridWidth,
                    GridHeight = viewModel.Floor.GridHeight,
                    IsActive = SelectedFloor.IsActive
                };

                var updatedFloor = await _floorService.UpdateFloorAsync(SelectedFloor.Id, dto, SessionService.CurrentUserId);

                // Update in collection
                var index = Floors.IndexOf(SelectedFloor);
                if (index >= 0)
                {
                    Floors[index] = updatedFloor;
                    SelectedFloor = updatedFloor;
                }

                _logger.Information("Floor '{FloorName}' updated", updatedFloor.Name);
            }
        }, "Updating floor...");
    }

    /// <summary>
    /// Deletes the selected floor.
    /// </summary>
    [RelayCommand]
    private async Task DeleteFloorAsync()
    {
        if (SelectedFloor == null)
            return;

        if (!RequirePermission("ManageFloors", "delete floors"))
            return;

        var confirm = await DialogService.ShowConfirmAsync(
            "Delete Floor",
            $"Are you sure you want to delete '{SelectedFloor.Name}'?\n\nThis will also deactivate all tables on this floor.");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _floorService.DeleteFloorAsync(SelectedFloor.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                var floorName = SelectedFloor.Name;
                Floors.Remove(SelectedFloor);
                SelectedFloor = Floors.FirstOrDefault();

                _logger.Information("Floor '{FloorName}' deleted", floorName);
            }
        }, "Deleting floor...");
    }

    #endregion

    #region Table Operations

    /// <summary>
    /// Adds a new table to the selected floor.
    /// </summary>
    [RelayCommand]
    private async Task AddTableAsync()
    {
        if (SelectedFloor == null)
            return;

        if (!RequirePermission("ManageTables", "add tables"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<TableDialogViewModel>();
            viewModel.Initialize(SelectedFloor.Id, Sections.ToList());

            var dialog = new TableDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Table != null)
            {
                var dto = new TableDto
                {
                    TableNumber = viewModel.Table.TableNumber,
                    Capacity = viewModel.Table.Capacity,
                    FloorId = SelectedFloor.Id,
                    SectionId = viewModel.Table.SectionId,
                    GridX = FindNextAvailableX(),
                    GridY = FindNextAvailableY(),
                    Width = viewModel.Table.Width,
                    Height = viewModel.Table.Height,
                    Shape = viewModel.Table.Shape,
                    IsActive = true
                };

                var createdTable = await _floorService.CreateTableAsync(dto, SessionService.CurrentUserId);
                Tables.Add(createdTable);
                TableCount = Tables.Count;
                TotalCapacity = Tables.Sum(t => t.Capacity);

                _logger.Information("Table '{TableNumber}' created on floor '{FloorName}'",
                    createdTable.TableNumber, SelectedFloor.Name);
            }
        }, "Adding table...");
    }

    /// <summary>
    /// Edits the selected table.
    /// </summary>
    [RelayCommand]
    private async Task EditTableAsync(Table? table)
    {
        table ??= SelectedTable;
        if (table == null || SelectedFloor == null)
            return;

        if (!RequirePermission("ManageTables", "edit tables"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<TableDialogViewModel>();
            viewModel.Initialize(SelectedFloor.Id, Sections.ToList(), table);

            var dialog = new TableDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Table != null)
            {
                var dto = new TableDto
                {
                    TableNumber = viewModel.Table.TableNumber,
                    Capacity = viewModel.Table.Capacity,
                    FloorId = SelectedFloor.Id,
                    SectionId = viewModel.Table.SectionId,
                    GridX = table.GridX,
                    GridY = table.GridY,
                    Width = viewModel.Table.Width,
                    Height = viewModel.Table.Height,
                    Shape = viewModel.Table.Shape,
                    IsActive = viewModel.Table.IsActive
                };

                var updatedTable = await _floorService.UpdateTableAsync(table.Id, dto, SessionService.CurrentUserId);

                // Update in collection
                var index = Tables.IndexOf(table);
                if (index >= 0)
                {
                    Tables[index] = updatedTable;
                }

                TotalCapacity = Tables.Sum(t => t.Capacity);

                _logger.Information("Table '{TableNumber}' updated", updatedTable.TableNumber);
            }
        }, "Updating table...");
    }

    /// <summary>
    /// Deletes the specified table.
    /// </summary>
    [RelayCommand]
    private async Task DeleteTableAsync(Table? table)
    {
        table ??= SelectedTable;
        if (table == null)
            return;

        if (!RequirePermission("ManageTables", "delete tables"))
            return;

        if (table.Status == TableStatus.Occupied)
        {
            await DialogService.ShowErrorAsync("Cannot Delete", "Cannot delete an occupied table. Please settle the bill first.");
            return;
        }

        var confirm = await DialogService.ShowConfirmAsync(
            "Delete Table",
            $"Are you sure you want to delete Table {table.TableNumber}?");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _floorService.DeleteTableAsync(table.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                var tableNumber = table.TableNumber;
                Tables.Remove(table);
                TableCount = Tables.Count;
                TotalCapacity = Tables.Sum(t => t.Capacity);

                if (SelectedTable == table)
                {
                    SelectedTable = null;
                }

                _logger.Information("Table '{TableNumber}' deleted", tableNumber);
            }
        }, "Deleting table...");
    }

    #endregion

    #region Section Operations

    /// <summary>
    /// Adds a new section to the selected floor.
    /// </summary>
    [RelayCommand]
    private async Task AddSectionAsync()
    {
        if (SelectedFloor == null)
            return;

        if (!RequirePermission("ManageSections", "add sections"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<SectionDialogViewModel>();
            viewModel.Initialize(SelectedFloor.Id);

            var dialog = new SectionDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Section != null)
            {
                var dto = new SectionDto
                {
                    Name = viewModel.Section.Name,
                    ColorCode = viewModel.Section.ColorCode,
                    FloorId = SelectedFloor.Id,
                    DisplayOrder = Sections.Count,
                    IsActive = true
                };

                var createdSection = await _floorService.CreateSectionAsync(dto, SessionService.CurrentUserId);
                Sections.Add(createdSection);

                _logger.Information("Section '{SectionName}' created on floor '{FloorName}'",
                    createdSection.Name, SelectedFloor.Name);
            }
        }, "Adding section...");
    }

    /// <summary>
    /// Edits the selected section.
    /// </summary>
    [RelayCommand]
    private async Task EditSectionAsync(Section? section)
    {
        section ??= SelectedSection;
        if (section == null || SelectedFloor == null)
            return;

        if (!RequirePermission("ManageSections", "edit sections"))
            return;

        await ExecuteAsync(async () =>
        {
            var viewModel = App.Services.GetRequiredService<SectionDialogViewModel>();
            viewModel.Initialize(SelectedFloor.Id, section);

            var dialog = new SectionDialog { DataContext = viewModel };
            var result = dialog.ShowDialog();

            if (result == true && viewModel.Section != null)
            {
                var dto = new SectionDto
                {
                    Name = viewModel.Section.Name,
                    ColorCode = viewModel.Section.ColorCode,
                    FloorId = SelectedFloor.Id,
                    DisplayOrder = section.DisplayOrder,
                    IsActive = viewModel.Section.IsActive
                };

                var updatedSection = await _floorService.UpdateSectionAsync(section.Id, dto, SessionService.CurrentUserId);

                // Update in collection
                var index = Sections.IndexOf(section);
                if (index >= 0)
                {
                    Sections[index] = updatedSection;
                }

                _logger.Information("Section '{SectionName}' updated", updatedSection.Name);
            }
        }, "Updating section...");
    }

    /// <summary>
    /// Deletes the specified section.
    /// </summary>
    [RelayCommand]
    private async Task DeleteSectionAsync(Section? section)
    {
        section ??= SelectedSection;
        if (section == null)
            return;

        if (!RequirePermission("ManageSections", "delete sections"))
            return;

        var confirm = await DialogService.ShowConfirmAsync(
            "Delete Section",
            $"Are you sure you want to delete '{section.Name}'?\n\nTables in this section will be unassigned.");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _floorService.DeleteSectionAsync(section.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                var sectionName = section.Name;
                Sections.Remove(section);

                // Update tables that were in this section
                foreach (var table in Tables.Where(t => t.SectionId == section.Id))
                {
                    table.SectionId = null;
                    table.Section = null;
                }

                if (SelectedSection == section)
                {
                    SelectedSection = null;
                }

                _logger.Information("Section '{SectionName}' deleted", sectionName);
            }
        }, "Deleting section...");
    }

    #endregion

    #region Layout Operations

    /// <summary>
    /// Saves the current layout positions.
    /// </summary>
    [RelayCommand]
    private async Task SaveLayoutAsync()
    {
        if (!HasUnsavedChanges || SelectedFloor == null)
            return;

        if (!RequirePermission("ManageTables", "save layout"))
            return;

        await ExecuteAsync(async () =>
        {
            var positions = Tables.Select(t => new TablePositionDto
            {
                TableId = t.Id,
                GridX = t.GridX,
                GridY = t.GridY,
                Width = t.Width,
                Height = t.Height
            }).ToList();

            await _floorService.UpdateTableLayoutAsync(positions, SessionService.CurrentUserId);

            HasUnsavedChanges = false;

            await DialogService.ShowMessageAsync("Layout Saved", "Floor layout has been saved successfully.");

            _logger.Information("Layout saved for floor '{FloorName}'", SelectedFloor.Name);

        }, "Saving layout...");
    }

    /// <summary>
    /// Updates a table's position when dragged in the grid.
    /// </summary>
    public void UpdateTablePosition(TablePositionChangedEventArgs args)
    {
        var table = Tables.FirstOrDefault(t => t.Id == args.TableId);
        if (table != null)
        {
            table.GridX = args.NewX;
            table.GridY = args.NewY;
            table.Width = args.Width;
            table.Height = args.Height;
            HasUnsavedChanges = true;
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirm = await DialogService.ShowConfirmAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to leave?");

            if (!confirm)
                return;
        }

        _navigationService.GoBack();
    }

    #endregion

    #region Helpers

    private int FindNextAvailableX()
    {
        if (Tables.Count == 0) return 0;

        var maxX = SelectedFloor?.GridWidth ?? 10;
        var nextX = (Tables.Max(t => t.GridX + t.Width) % maxX);
        return nextX;
    }

    private int FindNextAvailableY()
    {
        if (Tables.Count == 0) return 0;

        var maxX = SelectedFloor?.GridWidth ?? 10;
        if (Tables.Max(t => t.GridX + t.Width) >= maxX)
        {
            return Tables.Max(t => t.GridY + t.Height);
        }
        return Tables.Max(t => t.GridY);
    }

    #endregion
}
