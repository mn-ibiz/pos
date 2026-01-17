namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a physical floor in the establishment that contains tables.
/// </summary>
public class Floor : BaseEntity
{
    /// <summary>
    /// Gets or sets the floor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order for sorting floors.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the grid width (number of columns) for table placement.
    /// </summary>
    public int GridWidth { get; set; } = 10;

    /// <summary>
    /// Gets or sets the grid height (number of rows) for table placement.
    /// </summary>
    public int GridHeight { get; set; } = 10;

    // Navigation properties

    /// <summary>
    /// Gets or sets the tables on this floor.
    /// </summary>
    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();

    /// <summary>
    /// Gets or sets the sections on this floor.
    /// </summary>
    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}
