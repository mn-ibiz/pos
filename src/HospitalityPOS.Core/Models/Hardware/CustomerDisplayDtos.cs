// src/HospitalityPOS.Core/Models/Hardware/CustomerDisplayDtos.cs
// Customer Display DTOs for VFD pole displays and secondary monitors
// Story 43-2: Customer Display Integration

namespace HospitalityPOS.Core.Models.Hardware;

/// <summary>
/// Type of customer display device.
/// </summary>
public enum CustomerDisplayType
{
    /// <summary>VFD (Vacuum Fluorescent Display) pole display - typically 2x20 characters.</summary>
    Vfd,

    /// <summary>Secondary monitor as full-screen customer display.</summary>
    SecondaryMonitor,

    /// <summary>Tablet or small screen as customer display.</summary>
    Tablet,

    /// <summary>Network-connected display (IP-based).</summary>
    NetworkDisplay
}

/// <summary>
/// VFD display protocol types.
/// </summary>
public enum VfdProtocol
{
    /// <summary>Epson ESC/POS display commands.</summary>
    EscPos,

    /// <summary>Generic 2x20 character protocol.</summary>
    Generic2x20,

    /// <summary>OPOS (OLE for POS) standard.</summary>
    Opos,

    /// <summary>Logic Controls LD9000 series.</summary>
    LogicControls,

    /// <summary>Posiflex VFD protocol.</summary>
    Posiflex
}

/// <summary>
/// Display state for status tracking.
/// </summary>
public enum CustomerDisplayState
{
    /// <summary>Display not connected.</summary>
    Disconnected,

    /// <summary>Display connected and ready.</summary>
    Connected,

    /// <summary>Display showing welcome/idle screen.</summary>
    Idle,

    /// <summary>Display showing transaction items.</summary>
    ShowingItems,

    /// <summary>Display showing payment information.</summary>
    ShowingPayment,

    /// <summary>Display showing thank you message.</summary>
    ShowingThankYou,

    /// <summary>Display showing promotional content.</summary>
    ShowingPromotion,

    /// <summary>Display encountered an error.</summary>
    Error
}

/// <summary>
/// Configuration for customer display device.
/// </summary>
public class CustomerDisplayConfiguration
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Type of display device.</summary>
    public CustomerDisplayType DisplayType { get; set; }

    /// <summary>Display name for identification.</summary>
    public string DisplayName { get; set; } = "Customer Display";

    /// <summary>Whether this configuration is active.</summary>
    public bool IsActive { get; set; } = true;

    // VFD-specific settings
    /// <summary>COM port for VFD (e.g., COM1, COM2).</summary>
    public string? PortName { get; set; }

    /// <summary>Baud rate for serial communication.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Data bits for serial communication.</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>Parity for serial communication.</summary>
    public string Parity { get; set; } = "None";

    /// <summary>Stop bits for serial communication.</summary>
    public int StopBits { get; set; } = 1;

    /// <summary>VFD protocol type.</summary>
    public VfdProtocol VfdProtocol { get; set; } = VfdProtocol.EscPos;

    /// <summary>Number of characters per line (typically 20).</summary>
    public int CharactersPerLine { get; set; } = 20;

    /// <summary>Number of lines (typically 2).</summary>
    public int NumberOfLines { get; set; } = 2;

    // Secondary monitor settings
    /// <summary>Monitor index for secondary display (0-based).</summary>
    public int MonitorIndex { get; set; } = 1;

    /// <summary>Whether to display in full-screen mode.</summary>
    public bool FullScreen { get; set; } = true;

    // Network display settings
    /// <summary>IP address for network display.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Port for network display.</summary>
    public int Port { get; set; } = 8080;

    // Content settings
    /// <summary>Welcome message when idle.</summary>
    public string WelcomeMessage { get; set; } = "Welcome!";

    /// <summary>Secondary welcome message line.</summary>
    public string WelcomeMessageLine2 { get; set; } = "We appreciate your business";

    /// <summary>Thank you message after transaction.</summary>
    public string ThankYouMessage { get; set; } = "Thank You!";

    /// <summary>Secondary thank you message line.</summary>
    public string ThankYouMessageLine2 { get; set; } = "Please come again";

    /// <summary>Whether to show store logo (secondary monitor only).</summary>
    public bool ShowLogo { get; set; } = true;

    /// <summary>Path to store logo image.</summary>
    public string? LogoPath { get; set; }

    /// <summary>Whether to show promotions during idle.</summary>
    public bool ShowPromotions { get; set; } = true;

    /// <summary>Seconds to display each item before returning to total.</summary>
    public int ItemDisplaySeconds { get; set; } = 3;

    /// <summary>Seconds to wait before returning to idle after transaction.</summary>
    public int IdleTimeSeconds { get; set; } = 10;

    /// <summary>Currency symbol for display.</summary>
    public string CurrencySymbol { get; set; } = "KSh";

    /// <summary>Background color for secondary monitor (hex).</summary>
    public string BackgroundColor { get; set; } = "#1a1a2e";

    /// <summary>Primary text color for secondary monitor (hex).</summary>
    public string PrimaryTextColor { get; set; } = "#FFFFFF";

    /// <summary>Accent color for secondary monitor (hex).</summary>
    public string AccentColor { get; set; } = "#22C55E";

    /// <summary>Created date.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last updated date.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Item information to display on customer screen.
/// </summary>
public class DisplayItemInfo
{
    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Unit price of the item.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity of items.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Line total (UnitPrice × Quantity).</summary>
    public decimal LineTotal => UnitPrice * Quantity;

    /// <summary>Whether item is sold by weight.</summary>
    public bool IsByWeight { get; set; }

    /// <summary>Weight if sold by weight.</summary>
    public decimal? Weight { get; set; }

    /// <summary>Weight unit if sold by weight.</summary>
    public string? WeightUnit { get; set; }

    /// <summary>Whether a discount was applied.</summary>
    public bool HasDiscount { get; set; }

    /// <summary>Original price before discount.</summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>Format item for VFD display (20 chars max).</summary>
    public string FormatForVfd(int lineWidth = 20)
    {
        var priceStr = UnitPrice.ToString("N0");
        var maxNameLength = lineWidth - priceStr.Length - 1;
        var name = ProductName.Length > maxNameLength
            ? ProductName.Substring(0, maxNameLength)
            : ProductName.PadRight(maxNameLength);
        return $"{name} {priceStr}";
    }

    /// <summary>Format item with weight for VFD display.</summary>
    public string FormatWeighedForVfd(int lineWidth = 20)
    {
        if (!IsByWeight || !Weight.HasValue) return FormatForVfd(lineWidth);
        var weightStr = $"{Weight:N3}{WeightUnit}";
        var priceStr = LineTotal.ToString("N0");
        return $"{weightStr} @ {UnitPrice:N0}/{WeightUnit}".PadRight(lineWidth - priceStr.Length) + priceStr;
    }
}

/// <summary>
/// Running total information for display.
/// </summary>
public class DisplayTotalInfo
{
    /// <summary>Subtotal before tax/discount.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Discount amount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Grand total.</summary>
    public decimal Total { get; set; }

    /// <summary>Number of items in order.</summary>
    public int ItemCount { get; set; }

    /// <summary>Format total for VFD display.</summary>
    public string FormatForVfd(int lineWidth = 20, string currencySymbol = "KSh")
    {
        var totalStr = $"{currencySymbol} {Total:N0}";
        var label = "TOTAL:";
        var padding = lineWidth - label.Length - totalStr.Length;
        return $"{label}{new string(' ', Math.Max(1, padding))}{totalStr}";
    }
}

/// <summary>
/// Payment information for display.
/// </summary>
public class DisplayPaymentInfo
{
    /// <summary>Total amount due.</summary>
    public decimal AmountDue { get; set; }

    /// <summary>Amount tendered/paid.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Change to return.</summary>
    public decimal Change => AmountPaid - AmountDue;

    /// <summary>Payment method name.</summary>
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>Whether payment is complete.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Transaction reference (e.g., M-Pesa code).</summary>
    public string? TransactionReference { get; set; }

    /// <summary>Format amount due for VFD line 1.</summary>
    public string FormatAmountDueForVfd(int lineWidth = 20, string currencySymbol = "KSh")
    {
        var amountStr = $"{currencySymbol} {AmountDue:N0}";
        var label = "Amount Due:";
        var padding = lineWidth - label.Length - amountStr.Length;
        return $"{label}{new string(' ', Math.Max(1, padding))}{amountStr}";
    }

    /// <summary>Format payment for VFD line 2.</summary>
    public string FormatPaymentForVfd(int lineWidth = 20, string currencySymbol = "KSh")
    {
        var amountStr = $"{currencySymbol} {AmountPaid:N0}";
        var label = $"{PaymentMethod}:";
        var padding = lineWidth - label.Length - amountStr.Length;
        return $"{label}{new string(' ', Math.Max(1, padding))}{amountStr}";
    }

    /// <summary>Format change for VFD line 1.</summary>
    public string FormatChangeForVfd(int lineWidth = 20, string currencySymbol = "KSh")
    {
        if (Change <= 0) return string.Empty.PadRight(lineWidth);
        var amountStr = $"{currencySymbol} {Change:N0}";
        var label = "Change:";
        var padding = lineWidth - label.Length - amountStr.Length;
        return $"{label}{new string(' ', Math.Max(1, padding))}{amountStr}";
    }
}

/// <summary>
/// Promotion information for idle display.
/// </summary>
public class DisplayPromotionInfo
{
    /// <summary>Promotion title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Promotion description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Discount percentage if applicable.</summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>Promotional price if applicable.</summary>
    public decimal? PromoPrice { get; set; }

    /// <summary>Image path for secondary monitor.</summary>
    public string? ImagePath { get; set; }

    /// <summary>Duration to display in seconds.</summary>
    public int DisplayDurationSeconds { get; set; } = 5;

    /// <summary>Format for VFD line 1.</summary>
    public string FormatLine1ForVfd(int lineWidth = 20)
    {
        return Title.Length > lineWidth
            ? Title.Substring(0, lineWidth)
            : Title.PadRight(lineWidth);
    }

    /// <summary>Format for VFD line 2.</summary>
    public string FormatLine2ForVfd(int lineWidth = 20)
    {
        var text = DiscountPercent.HasValue
            ? $"Save {DiscountPercent}%!"
            : Description;
        return text.Length > lineWidth
            ? text.Substring(0, lineWidth)
            : text.PadRight(lineWidth);
    }
}

/// <summary>
/// Result of connecting to a customer display.
/// </summary>
public class DisplayConnectionResult
{
    /// <summary>Whether connection was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Display device information.</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>Firmware/driver version if available.</summary>
    public string? Version { get; set; }

    /// <summary>Timestamp of connection attempt.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Create a success result.</summary>
    public static DisplayConnectionResult Successful(string? deviceInfo = null, string? version = null)
        => new() { Success = true, DeviceInfo = deviceInfo, Version = version };

    /// <summary>Create a failure result.</summary>
    public static DisplayConnectionResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of testing display output.
/// </summary>
public class DisplayTestResult
{
    /// <summary>Whether test was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Test message that was displayed.</summary>
    public string? TestMessage { get; set; }

    /// <summary>Timestamp of test.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Available display port information.
/// </summary>
public class DisplayPortInfo
{
    /// <summary>Port name (e.g., COM1).</summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>Port description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether port is currently in use.</summary>
    public bool InUse { get; set; }
}

/// <summary>
/// Monitor information for secondary display.
/// </summary>
public class MonitorInfo
{
    /// <summary>Monitor index (0-based).</summary>
    public int Index { get; set; }

    /// <summary>Monitor name/description.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this is the primary monitor.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Monitor width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Monitor height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Monitor bounds (left, top, width, height).</summary>
    public (int Left, int Top, int Width, int Height) Bounds { get; set; }

    /// <summary>Display string.</summary>
    public override string ToString() => $"{Name} ({Width}x{Height}){(IsPrimary ? " - Primary" : "")}";
}

/// <summary>
/// Event args for display state changes.
/// </summary>
public class DisplayStateChangedEventArgs : EventArgs
{
    /// <summary>Previous state.</summary>
    public CustomerDisplayState PreviousState { get; set; }

    /// <summary>New state.</summary>
    public CustomerDisplayState NewState { get; set; }

    /// <summary>Timestamp of change.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for display errors.
/// </summary>
public class DisplayErrorEventArgs : EventArgs
{
    /// <summary>Error message.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Exception if available.</summary>
    public Exception? Exception { get; set; }

    /// <summary>Whether display is still usable.</summary>
    public bool IsFatal { get; set; }

    /// <summary>Timestamp of error.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Display content to show on customer display.
/// </summary>
public class DisplayContent
{
    /// <summary>Line 1 text (VFD) or main content.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Line 2 text (VFD) or secondary content.</summary>
    public string Line2 { get; set; } = string.Empty;

    /// <summary>Content type for rich displays.</summary>
    public CustomerDisplayState ContentType { get; set; }

    /// <summary>Associated item info if showing item.</summary>
    public DisplayItemInfo? ItemInfo { get; set; }

    /// <summary>Associated total info if showing total.</summary>
    public DisplayTotalInfo? TotalInfo { get; set; }

    /// <summary>Associated payment info if showing payment.</summary>
    public DisplayPaymentInfo? PaymentInfo { get; set; }

    /// <summary>Associated promotion info if showing promotion.</summary>
    public DisplayPromotionInfo? PromotionInfo { get; set; }
}

/// <summary>
/// VFD ESC/POS command constants.
/// </summary>
public static class VfdCommands
{
    // Initialize display
    public static readonly byte[] Initialize = { 0x1B, 0x40 };

    // Clear display
    public static readonly byte[] ClearScreen = { 0x0C };

    // Move cursor to home position
    public static readonly byte[] CursorHome = { 0x0B };

    // Move cursor to line 1, column 1
    public static readonly byte[] MoveLine1 = { 0x1B, 0x6C, 0x01, 0x01 };

    // Move cursor to line 2, column 1
    public static readonly byte[] MoveLine2 = { 0x1B, 0x6C, 0x01, 0x02 };

    // Set overwrite mode
    public static readonly byte[] OverwriteMode = { 0x1F, 0x01 };

    // Set vertical scroll mode
    public static readonly byte[] VerticalScrollMode = { 0x1F, 0x03 };

    // Brightness levels
    public static readonly byte[] BrightnessHigh = { 0x1F, 0x58, 0x04 };
    public static readonly byte[] BrightnessMedium = { 0x1F, 0x58, 0x02 };
    public static readonly byte[] BrightnessLow = { 0x1F, 0x58, 0x01 };

    /// <summary>
    /// Create command to move cursor to specific position.
    /// </summary>
    public static byte[] MoveCursor(int column, int line)
    {
        return new byte[] { 0x1B, 0x6C, (byte)column, (byte)line };
    }
}
