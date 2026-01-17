using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Table add/edit dialog.
/// </summary>
public partial class TableDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the table being edited.
    /// </summary>
    [ObservableProperty]
    private Table? _table;

    /// <summary>
    /// Gets or sets the table number.
    /// </summary>
    [ObservableProperty]
    private string _tableNumber = string.Empty;

    /// <summary>
    /// Gets or sets the seating capacity.
    /// </summary>
    [ObservableProperty]
    private int _capacity = 4;

    /// <summary>
    /// Gets or sets the selected section.
    /// </summary>
    [ObservableProperty]
    private Section? _selectedSection;

    /// <summary>
    /// Gets or sets the table shape.
    /// </summary>
    [ObservableProperty]
    private TableShape _shape = TableShape.Square;

    /// <summary>
    /// Gets or sets the width in grid cells.
    /// </summary>
    [ObservableProperty]
    private int _width = 1;

    /// <summary>
    /// Gets or sets the height in grid cells.
    /// </summary>
    [ObservableProperty]
    private int _height = 1;

    /// <summary>
    /// Gets or sets whether the table is active.
    /// </summary>
    [ObservableProperty]
    private bool _isActive = true;

    /// <summary>
    /// Gets or sets whether this is an edit operation.
    /// </summary>
    [ObservableProperty]
    private bool _isEdit;

    /// <summary>
    /// Gets or sets the floor ID.
    /// </summary>
    [ObservableProperty]
    private int _floorId;

    /// <summary>
    /// Gets or sets the available sections.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Section> _sections = [];

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string DialogTitle => IsEdit ? "Edit Table" : "Add Table";

    /// <summary>
    /// Gets the available table shapes.
    /// </summary>
    public IReadOnlyList<TableShape> AvailableShapes { get; } = Enum.GetValues<TableShape>();

    /// <summary>
    /// Gets or sets the action to close the dialog.
    /// </summary>
    public Action<bool>? CloseDialog { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDialogViewModel"/> class.
    /// </summary>
    public TableDialogViewModel(ILogger logger) : base(logger)
    {
        Title = "Table";
    }

    /// <summary>
    /// Initializes the view model for creating a new table.
    /// </summary>
    /// <param name="floorId">The floor ID.</param>
    /// <param name="sections">The available sections.</param>
    public void Initialize(int floorId, IEnumerable<Section> sections)
    {
        IsEdit = false;
        FloorId = floorId;
        Sections = new ObservableCollection<Section>(sections);
        TableNumber = string.Empty;
        Capacity = 4;
        SelectedSection = null;
        Shape = TableShape.Square;
        Width = 1;
        Height = 1;
        IsActive = true;
        Table = null;
    }

    /// <summary>
    /// Initializes the view model for editing an existing table.
    /// </summary>
    /// <param name="floorId">The floor ID.</param>
    /// <param name="sections">The available sections.</param>
    /// <param name="table">The table to edit.</param>
    public void Initialize(int floorId, IEnumerable<Section> sections, Table table)
    {
        ArgumentNullException.ThrowIfNull(table);

        IsEdit = true;
        FloorId = floorId;
        Sections = new ObservableCollection<Section>(sections);
        TableNumber = table.TableNumber;
        Capacity = table.Capacity;
        SelectedSection = Sections.FirstOrDefault(s => s.Id == table.SectionId);
        Shape = table.Shape;
        Width = table.Width;
        Height = table.Height;
        IsActive = table.IsActive;
        Table = table;
    }

    /// <summary>
    /// Saves the table and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(TableNumber))
        {
            ErrorMessage = "Table number is required.";
            return;
        }

        if (Capacity < 1 || Capacity > 50)
        {
            ErrorMessage = "Capacity must be between 1 and 50.";
            return;
        }

        if (Width < 1 || Width > 5)
        {
            ErrorMessage = "Width must be between 1 and 5 cells.";
            return;
        }

        if (Height < 1 || Height > 5)
        {
            ErrorMessage = "Height must be between 1 and 5 cells.";
            return;
        }

        Table = new Table
        {
            Id = IsEdit ? Table?.Id ?? 0 : 0,
            TableNumber = TableNumber.Trim(),
            Capacity = Capacity,
            FloorId = FloorId,
            SectionId = SelectedSection?.Id,
            Section = SelectedSection,
            Shape = Shape,
            Width = Width,
            Height = Height,
            IsActive = IsActive
        };

        CloseDialog?.Invoke(true);
    }

    /// <summary>
    /// Cancels and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Table = null;
        CloseDialog?.Invoke(false);
    }
}
