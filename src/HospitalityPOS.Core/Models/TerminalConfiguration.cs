using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Models;

/// <summary>
/// Local terminal configuration stored in terminal.json.
/// </summary>
public class TerminalConfiguration
{
    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the terminal identity information.
    /// </summary>
    public TerminalIdentity Terminal { get; set; } = new();

    /// <summary>
    /// Gets or sets the database connection settings.
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Gets or sets the hardware configuration.
    /// </summary>
    public HardwareSettings Hardware { get; set; } = new();

    /// <summary>
    /// Gets or sets the terminal-specific settings.
    /// </summary>
    public TerminalSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets when the terminal was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets the last sync timestamp.
    /// </summary>
    public DateTime? LastSync { get; set; }
}

/// <summary>
/// Terminal identity information.
/// </summary>
public class TerminalIdentity
{
    /// <summary>
    /// Gets or sets the terminal ID from the database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the terminal code (e.g., REG-001).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal friendly name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType Type { get; set; } = TerminalType.Register;

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; } = BusinessMode.Supermarket;

    /// <summary>
    /// Gets or sets the machine identifier.
    /// </summary>
    public string MachineIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the main register.
    /// </summary>
    public bool IsMainRegister { get; set; }
}

/// <summary>
/// Database connection settings.
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Gets or sets the database server.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use integrated security.
    /// </summary>
    public bool IntegratedSecurity { get; set; } = true;

    /// <summary>
    /// Gets or sets the username (if not using integrated security).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password (encrypted).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
}

/// <summary>
/// Hardware configuration settings.
/// </summary>
public class HardwareSettings
{
    /// <summary>
    /// Gets or sets the receipt printer configuration.
    /// </summary>
    public PrinterConfig? ReceiptPrinter { get; set; }

    /// <summary>
    /// Gets or sets the kitchen printer configuration.
    /// </summary>
    public PrinterConfig? KitchenPrinter { get; set; }

    /// <summary>
    /// Gets or sets the cash drawer configuration.
    /// </summary>
    public CashDrawerConfig? CashDrawer { get; set; }

    /// <summary>
    /// Gets or sets the barcode scanner configuration.
    /// </summary>
    public BarcodeScannerConfig? BarcodeScanner { get; set; }

    /// <summary>
    /// Gets or sets the customer display configuration.
    /// </summary>
    public CustomerDisplayConfig? CustomerDisplay { get; set; }

    /// <summary>
    /// Gets or sets the scale configuration.
    /// </summary>
    public ScaleConfig? Scale { get; set; }
}

/// <summary>
/// Printer configuration.
/// </summary>
public class PrinterConfig
{
    /// <summary>
    /// Gets or sets the printer ID in the database.
    /// </summary>
    public int? PrinterId { get; set; }

    /// <summary>
    /// Gets or sets the printer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the printer type (e.g., ESC/POS).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the paper width in mm.
    /// </summary>
    public int PaperWidth { get; set; } = 80;
}

/// <summary>
/// Cash drawer configuration.
/// </summary>
public class CashDrawerConfig
{
    /// <summary>
    /// Gets or sets the cash drawer ID in the database.
    /// </summary>
    public int? CashDrawerId { get; set; }

    /// <summary>
    /// Gets or sets the drawer type.
    /// </summary>
    public string Type { get; set; } = "PrinterTriggered";

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the open code sequence.
    /// </summary>
    public string OpenCode { get; set; } = "27,112,0,25,250";
}

/// <summary>
/// Barcode scanner configuration.
/// </summary>
public class BarcodeScannerConfig
{
    /// <summary>
    /// Gets or sets whether the scanner is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the scanner type.
    /// </summary>
    public string Type { get; set; } = "USB_HID";

    /// <summary>
    /// Gets or sets the scan prefix.
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scan suffix.
    /// </summary>
    public string Suffix { get; set; } = "\r";
}

/// <summary>
/// Customer display configuration.
/// </summary>
public class CustomerDisplayConfig
{
    /// <summary>
    /// Gets or sets whether the display is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the display type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public string Port { get; set; } = string.Empty;
}

/// <summary>
/// Scale configuration.
/// </summary>
public class ScaleConfig
{
    /// <summary>
    /// Gets or sets whether the scale is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the scale type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public string Port { get; set; } = string.Empty;
}

/// <summary>
/// Terminal-specific settings.
/// </summary>
public class TerminalSettings
{
    /// <summary>
    /// Gets or sets the auto-logout timeout in minutes.
    /// </summary>
    public int AutoLogoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether cash count is required on Z-Report.
    /// </summary>
    public bool RequireCashCountOnZReport { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to print receipt automatically.
    /// </summary>
    public bool PrintReceiptAutomatically { get; set; } = true;

    /// <summary>
    /// Gets or sets whether sound is enabled.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default customer display message.
    /// </summary>
    public string? WelcomeMessage { get; set; }
}
