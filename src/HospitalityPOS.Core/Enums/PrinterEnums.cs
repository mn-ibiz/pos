namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Defines the type/purpose of a printer.
/// </summary>
public enum PrinterType
{
    /// <summary>
    /// Receipt printer for customer receipts.
    /// </summary>
    Receipt = 0,

    /// <summary>
    /// Kitchen printer for order tickets.
    /// </summary>
    Kitchen = 1,

    /// <summary>
    /// Report printer for daily reports.
    /// </summary>
    Report = 2
}

/// <summary>
/// Defines how the printer is connected.
/// </summary>
public enum PrinterConnectionType
{
    /// <summary>
    /// Connected via USB port.
    /// </summary>
    USB = 0,

    /// <summary>
    /// Connected via Serial/COM port.
    /// </summary>
    Serial = 1,

    /// <summary>
    /// Connected via network (TCP/IP).
    /// </summary>
    Network = 2,

    /// <summary>
    /// Using Windows print driver.
    /// </summary>
    WindowsDriver = 3
}

/// <summary>
/// Defines the current status of a printer.
/// </summary>
public enum PrinterStatus
{
    /// <summary>
    /// Printer status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Printer is online and ready.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Printer is offline or disconnected.
    /// </summary>
    Offline = 2,

    /// <summary>
    /// Printer has an error.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Printer is out of paper.
    /// </summary>
    PaperOut = 4,

    /// <summary>
    /// Printer cover is open.
    /// </summary>
    CoverOpen = 5
}

/// <summary>
/// Defines font sizes for Kitchen Order Ticket (KOT) printing.
/// </summary>
public enum KOTFontSize
{
    /// <summary>
    /// Small font size.
    /// </summary>
    Small = 0,

    /// <summary>
    /// Normal/default font size.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Large font size.
    /// </summary>
    Large = 2,

    /// <summary>
    /// Extra large font size.
    /// </summary>
    ExtraLarge = 3
}
