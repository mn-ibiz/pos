namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents ESC/POS and print settings for a printer.
/// </summary>
public class PrinterSettings
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated printer ID.
    /// </summary>
    public int PrinterId { get; set; }

    /// <summary>
    /// Gets or sets whether to use ESC/POS commands.
    /// </summary>
    public bool UseEscPos { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-cut paper after printing.
    /// </summary>
    public bool AutoCut { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use partial cut (leaves small portion attached).
    /// </summary>
    public bool PartialCut { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to open cash drawer on print.
    /// </summary>
    public bool OpenCashDrawer { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of feed lines before cutting.
    /// </summary>
    public int CutFeedLines { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to print the logo.
    /// </summary>
    public bool PrintLogo { get; set; } = true;

    /// <summary>
    /// Gets or sets the logo bitmap data (monochrome).
    /// </summary>
    public byte[]? LogoBitmap { get; set; }

    /// <summary>
    /// Gets or sets the logo width in pixels.
    /// </summary>
    public int LogoWidth { get; set; } = 200;

    /// <summary>
    /// Gets or sets whether to beep on print.
    /// </summary>
    public bool BeepOnPrint { get; set; }

    /// <summary>
    /// Gets or sets the number of beeps.
    /// </summary>
    public int BeepCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the print density (0-15).
    /// </summary>
    public int PrintDensity { get; set; } = 7;

    /// <summary>
    /// Gets or sets the associated printer.
    /// </summary>
    public Printer Printer { get; set; } = null!;
}
