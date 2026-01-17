using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a configured printer (receipt, kitchen, or report).
/// </summary>
public class Printer
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the printer.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the printer type (Receipt, Kitchen, Report).
    /// </summary>
    public PrinterType Type { get; set; } = PrinterType.Receipt;

    /// <summary>
    /// Gets or sets the connection type.
    /// </summary>
    public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.WindowsDriver;

    /// <summary>
    /// Gets or sets the serial/COM port name (e.g., COM1, LPT1).
    /// </summary>
    public string? PortName { get; set; }

    /// <summary>
    /// Gets or sets the network IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the network port (default 9100 for ESC/POS).
    /// </summary>
    public int? Port { get; set; } = 9100;

    /// <summary>
    /// Gets or sets the USB device path.
    /// </summary>
    public string? UsbPath { get; set; }

    /// <summary>
    /// Gets or sets the Windows printer queue name.
    /// </summary>
    public string? WindowsPrinterName { get; set; }

    /// <summary>
    /// Gets or sets the paper width in mm (80 or 58).
    /// </summary>
    public int PaperWidth { get; set; } = 80;

    /// <summary>
    /// Gets or sets the characters per line based on paper width.
    /// </summary>
    public int CharsPerLine { get; set; } = 48;

    /// <summary>
    /// Gets or sets whether this is the default printer for its type.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether the printer is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the current printer status.
    /// </summary>
    public PrinterStatus Status { get; set; } = PrinterStatus.Unknown;

    /// <summary>
    /// Gets or sets when the status was last checked.
    /// </summary>
    public DateTime? LastStatusCheck { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets when the printer was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the printer was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the printer settings.
    /// </summary>
    public PrinterSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the category mappings (for kitchen printers).
    /// </summary>
    public ICollection<PrinterCategoryMapping> CategoryMappings { get; set; } = new List<PrinterCategoryMapping>();

    /// <summary>
    /// Gets or sets the KOT settings (for kitchen printers).
    /// </summary>
    public KOTSettings? KOTSettings { get; set; }
}
