using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Models;

/// <summary>
/// Represents a discovered printer on the network or system.
/// </summary>
public class DiscoveredPrinter
{
    /// <summary>
    /// Gets or sets the IP address (for network printers).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; } = 9100;

    /// <summary>
    /// Gets or sets the printer model/name.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the Windows printer queue name.
    /// </summary>
    public string? WindowsPrinterName { get; set; }

    /// <summary>
    /// Gets or sets the connection type.
    /// </summary>
    public PrinterConnectionType ConnectionType { get; set; }

    /// <summary>
    /// Gets or sets the serial port name.
    /// </summary>
    public string? PortName { get; set; }
}

/// <summary>
/// Represents the result of a print test operation.
/// </summary>
public class PrintTestResult
{
    /// <summary>
    /// Gets or sets whether the test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional details about the test.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PrintTestResult Successful(string? details = null)
    {
        return new PrintTestResult
        {
            Success = true,
            Details = details
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PrintTestResult Failed(string errorMessage)
    {
        return new PrintTestResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Represents data for a Kitchen Order Ticket (KOT).
/// </summary>
public class KOTData
{
    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table number.
    /// </summary>
    public string? TableNumber { get; set; }

    /// <summary>
    /// Gets or sets the waiter/server name.
    /// </summary>
    public string? WaiterName { get; set; }

    /// <summary>
    /// Gets or sets the order time.
    /// </summary>
    public DateTime OrderTime { get; set; }

    /// <summary>
    /// Gets or sets whether this is an incremental order (additions).
    /// </summary>
    public bool IsIncremental { get; set; }

    /// <summary>
    /// Gets or sets the batch number for incremental orders.
    /// </summary>
    public int BatchNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the items on this KOT.
    /// </summary>
    public List<KOTItemData> Items { get; set; } = new();

    /// <summary>
    /// Gets the total item count.
    /// </summary>
    public int TotalItemCount => Items.Sum(i => (int)i.Quantity);
}

/// <summary>
/// Represents a single item on a KOT.
/// </summary>
public class KOTItemData
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Gets or sets the modifiers as a list.
    /// </summary>
    public List<string> Modifiers { get; set; } = new();

    /// <summary>
    /// Gets or sets special notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether this item is voided.
    /// </summary>
    public bool IsVoided { get; set; }

    /// <summary>
    /// Gets or sets allergy information.
    /// </summary>
    public string? AllergyInfo { get; set; }
}

/// <summary>
/// Represents the result of a kitchen routing operation.
/// </summary>
public class KitchenRoutingResult
{
    /// <summary>
    /// Gets or sets whether routing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of printers the order was routed to.
    /// </summary>
    public int PrinterCount { get; set; }

    /// <summary>
    /// Gets or sets the total items routed.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets details about each printer's items.
    /// </summary>
    public List<PrinterRouteInfo> Routes { get; set; } = new();
}

/// <summary>
/// Information about items routed to a specific printer.
/// </summary>
public class PrinterRouteInfo
{
    /// <summary>
    /// Gets or sets the printer name.
    /// </summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of items routed to this printer.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets whether printing was successful.
    /// </summary>
    public bool Printed { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents the status check result for a printer.
/// </summary>
public class PrinterStatusResult
{
    /// <summary>
    /// Gets or sets the printer status.
    /// </summary>
    public PrinterStatus Status { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the status was checked.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates an online status result.
    /// </summary>
    public static PrinterStatusResult Online()
    {
        return new PrinterStatusResult { Status = PrinterStatus.Online };
    }

    /// <summary>
    /// Creates an offline status result.
    /// </summary>
    public static PrinterStatusResult Offline(string? error = null)
    {
        return new PrinterStatusResult
        {
            Status = PrinterStatus.Offline,
            ErrorMessage = error
        };
    }

    /// <summary>
    /// Creates an error status result.
    /// </summary>
    public static PrinterStatusResult Error(string message)
    {
        return new PrinterStatusResult
        {
            Status = PrinterStatus.Error,
            ErrorMessage = message
        };
    }
}
