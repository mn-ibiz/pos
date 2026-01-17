using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Floor add/edit dialog.
/// </summary>
public partial class FloorDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the floor being edited.
    /// </summary>
    [ObservableProperty]
    private Floor? _floor;

    /// <summary>
    /// Gets or sets the floor name.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the grid width.
    /// </summary>
    [ObservableProperty]
    private int _gridWidth = 10;

    /// <summary>
    /// Gets or sets the grid height.
    /// </summary>
    [ObservableProperty]
    private int _gridHeight = 10;

    /// <summary>
    /// Gets or sets whether this is an edit operation.
    /// </summary>
    [ObservableProperty]
    private bool _isEdit;

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string DialogTitle => IsEdit ? "Edit Floor" : "Add Floor";

    /// <summary>
    /// Gets or sets the action to close the dialog.
    /// </summary>
    public Action<bool>? CloseDialog { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorDialogViewModel"/> class.
    /// </summary>
    public FloorDialogViewModel(ILogger logger) : base(logger)
    {
        Title = "Floor";
    }

    /// <summary>
    /// Initializes the view model for creating a new floor.
    /// </summary>
    public void Initialize()
    {
        IsEdit = false;
        Name = string.Empty;
        GridWidth = 10;
        GridHeight = 10;
        Floor = null;
    }

    /// <summary>
    /// Initializes the view model for editing an existing floor.
    /// </summary>
    /// <param name="floor">The floor to edit.</param>
    public void Initialize(Floor floor)
    {
        ArgumentNullException.ThrowIfNull(floor);

        IsEdit = true;
        Name = floor.Name;
        GridWidth = floor.GridWidth;
        GridHeight = floor.GridHeight;
        Floor = floor;
    }

    /// <summary>
    /// Saves the floor and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Floor name is required.";
            return;
        }

        if (GridWidth < 5 || GridWidth > 50)
        {
            ErrorMessage = "Grid width must be between 5 and 50.";
            return;
        }

        if (GridHeight < 5 || GridHeight > 50)
        {
            ErrorMessage = "Grid height must be between 5 and 50.";
            return;
        }

        Floor = new Floor
        {
            Id = IsEdit ? Floor?.Id ?? 0 : 0,
            Name = Name.Trim(),
            GridWidth = GridWidth,
            GridHeight = GridHeight,
            IsActive = true
        };

        CloseDialog?.Invoke(true);
    }

    /// <summary>
    /// Cancels and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Floor = null;
        CloseDialog?.Invoke(false);
    }
}
