namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Maps printers to product categories for kitchen order routing.
/// </summary>
public class PrinterCategoryMapping
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the printer ID.
    /// </summary>
    public int PrinterId { get; set; }

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets whether the mapping is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the mapping was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the associated printer.
    /// </summary>
    public Printer Printer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the associated category.
    /// </summary>
    public Category Category { get; set; } = null!;
}
