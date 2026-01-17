using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Kitchen Order Ticket (KOT) formatting settings for a kitchen printer.
/// </summary>
public class KOTSettings
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated printer ID.
    /// </summary>
    public int PrinterId { get; set; }

    #region Font Sizes

    /// <summary>
    /// Gets or sets the title font size.
    /// </summary>
    public KOTFontSize TitleFontSize { get; set; } = KOTFontSize.Large;

    /// <summary>
    /// Gets or sets the item font size.
    /// </summary>
    public KOTFontSize ItemFontSize { get; set; } = KOTFontSize.Normal;

    /// <summary>
    /// Gets or sets the modifier font size.
    /// </summary>
    public KOTFontSize ModifierFontSize { get; set; } = KOTFontSize.Small;

    #endregion

    #region Display Options

    /// <summary>
    /// Gets or sets whether to show the table number.
    /// </summary>
    public bool ShowTableNumber { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the waiter name.
    /// </summary>
    public bool ShowWaiterName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the order time.
    /// </summary>
    public bool ShowOrderTime { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the order number.
    /// </summary>
    public bool ShowOrderNumber { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show category headers.
    /// </summary>
    public bool ShowCategoryHeader { get; set; } = true;

    #endregion

    #region Item Display

    /// <summary>
    /// Gets or sets whether to group items by category.
    /// </summary>
    public bool GroupByCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show quantity in large font.
    /// </summary>
    public bool ShowQuantityLarge { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show modifiers indented.
    /// </summary>
    public bool ShowModifiersIndented { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to highlight notes/special requests.
    /// </summary>
    public bool ShowNotesHighlighted { get; set; } = true;

    #endregion

    #region Alerts

    /// <summary>
    /// Gets or sets whether to print rush orders with special formatting.
    /// </summary>
    public bool PrintRushOrders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to highlight allergies.
    /// </summary>
    public bool HighlightAllergies { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to beep on print.
    /// </summary>
    public bool BeepOnPrint { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of beeps.
    /// </summary>
    public int BeepCount { get; set; } = 2;

    #endregion

    #region Copies

    /// <summary>
    /// Gets or sets the number of copies per order.
    /// </summary>
    public int CopiesPerOrder { get; set; } = 1;

    #endregion

    /// <summary>
    /// Gets or sets the associated printer.
    /// </summary>
    public Printer Printer { get; set; } = null!;
}
