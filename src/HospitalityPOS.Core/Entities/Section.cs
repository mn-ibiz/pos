namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a section or zone within a floor (e.g., Outdoor Area, VIP, Indoor).
/// </summary>
public class Section : BaseEntity
{
    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the color code for visual representation (hex format).
    /// </summary>
    public string ColorCode { get; set; } = "#4CAF50";

    /// <summary>
    /// Gets or sets the floor ID this section belongs to.
    /// </summary>
    public int FloorId { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting sections.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the floor this section belongs to.
    /// </summary>
    public virtual Floor Floor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the tables in this section.
    /// </summary>
    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
}
