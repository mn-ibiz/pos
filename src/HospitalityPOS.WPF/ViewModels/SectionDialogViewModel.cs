using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Section add/edit dialog.
/// </summary>
public partial class SectionDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the section being edited.
    /// </summary>
    [ObservableProperty]
    private Section? _section;

    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the color code.
    /// </summary>
    [ObservableProperty]
    private string _colorCode = "#4CAF50";

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
    /// Gets or sets the selected color.
    /// </summary>
    [ObservableProperty]
    private Color _selectedColor = Color.FromRgb(76, 175, 80);

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string DialogTitle => IsEdit ? "Edit Section" : "Add Section";

    /// <summary>
    /// Gets the available colors for sections.
    /// </summary>
    public ObservableCollection<SectionColorOption> AvailableColors { get; } =
    [
        new SectionColorOption("Green", "#4CAF50", Color.FromRgb(76, 175, 80)),
        new SectionColorOption("Blue", "#2196F3", Color.FromRgb(33, 150, 243)),
        new SectionColorOption("Orange", "#FF9800", Color.FromRgb(255, 152, 0)),
        new SectionColorOption("Purple", "#9C27B0", Color.FromRgb(156, 39, 176)),
        new SectionColorOption("Cyan", "#00BCD4", Color.FromRgb(0, 188, 212)),
        new SectionColorOption("Deep Orange", "#FF5722", Color.FromRgb(255, 87, 34)),
        new SectionColorOption("Teal", "#009688", Color.FromRgb(0, 150, 136)),
        new SectionColorOption("Pink", "#E91E63", Color.FromRgb(233, 30, 99)),
        new SectionColorOption("Indigo", "#3F51B5", Color.FromRgb(63, 81, 181)),
        new SectionColorOption("Lime", "#CDDC39", Color.FromRgb(205, 220, 57))
    ];

    /// <summary>
    /// Gets or sets the selected color option.
    /// </summary>
    [ObservableProperty]
    private SectionColorOption? _selectedColorOption;

    /// <summary>
    /// Gets or sets the action to close the dialog.
    /// </summary>
    public Action<bool>? CloseDialog { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionDialogViewModel"/> class.
    /// </summary>
    public SectionDialogViewModel(ILogger logger) : base(logger)
    {
        Title = "Section";
    }

    /// <summary>
    /// Initializes the view model for creating a new section.
    /// </summary>
    /// <param name="floorId">The floor ID.</param>
    public void Initialize(int floorId)
    {
        IsEdit = false;
        FloorId = floorId;
        Name = string.Empty;
        ColorCode = "#4CAF50";
        SelectedColorOption = AvailableColors.First();
        Section = null;
    }

    /// <summary>
    /// Initializes the view model for editing an existing section.
    /// </summary>
    /// <param name="floorId">The floor ID.</param>
    /// <param name="section">The section to edit.</param>
    public void Initialize(int floorId, Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        IsEdit = true;
        FloorId = floorId;
        Name = section.Name;
        ColorCode = section.ColorCode;
        SelectedColorOption = AvailableColors.FirstOrDefault(c => c.HexCode == section.ColorCode)
            ?? AvailableColors.First();
        Section = section;
    }

    partial void OnSelectedColorOptionChanged(SectionColorOption? value)
    {
        if (value != null)
        {
            ColorCode = value.HexCode;
            SelectedColor = value.Color;
        }
    }

    /// <summary>
    /// Saves the section and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Section name is required.";
            return;
        }

        Section = new Section
        {
            Id = IsEdit ? Section?.Id ?? 0 : 0,
            Name = Name.Trim(),
            ColorCode = ColorCode,
            FloorId = FloorId,
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
        Section = null;
        CloseDialog?.Invoke(false);
    }
}

/// <summary>
/// Represents a color option for sections.
/// </summary>
public class SectionColorOption
{
    /// <summary>
    /// Gets the color name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the hex color code.
    /// </summary>
    public string HexCode { get; }

    /// <summary>
    /// Gets the WPF color.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Gets the brush for the color.
    /// </summary>
    public SolidColorBrush Brush { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionColorOption"/> class.
    /// </summary>
    public SectionColorOption(string name, string hexCode, Color color)
    {
        Name = name;
        HexCode = hexCode;
        Color = color;
        Brush = new SolidColorBrush(color);
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}
