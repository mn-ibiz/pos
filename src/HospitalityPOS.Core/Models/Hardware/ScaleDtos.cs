namespace HospitalityPOS.Core.Models.Hardware;

/// <summary>
/// Represents a weight reading from a scale.
/// </summary>
public class WeightReading
{
    /// <summary>Gets or sets the weight value.</summary>
    public decimal Weight { get; set; }

    /// <summary>Gets or sets the weight unit.</summary>
    public WeightUnit Unit { get; set; } = WeightUnit.Kilogram;

    /// <summary>Gets or sets whether the reading is stable.</summary>
    public bool IsStable { get; set; }

    /// <summary>Gets or sets when the reading was taken.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the gross weight (before tare).</summary>
    public decimal GrossWeight { get; set; }

    /// <summary>Gets or sets the tare weight applied.</summary>
    public decimal TareWeight { get; set; }

    /// <summary>Gets or sets whether the scale is zeroed.</summary>
    public bool IsZeroed { get; set; }

    /// <summary>Gets or sets whether the reading is in motion (unstable).</summary>
    public bool IsInMotion { get; set; }

    /// <summary>Gets or sets whether there's an overload condition.</summary>
    public bool IsOverload { get; set; }

    /// <summary>Gets the net weight (gross minus tare).</summary>
    public decimal NetWeight => GrossWeight - TareWeight;

    /// <summary>Gets the formatted weight display.</summary>
    public string FormattedWeight => $"{Weight:N3} {GetUnitAbbreviation(Unit)}";

    private static string GetUnitAbbreviation(WeightUnit unit) => unit switch
    {
        WeightUnit.Kilogram => "kg",
        WeightUnit.Gram => "g",
        WeightUnit.Pound => "lb",
        WeightUnit.Ounce => "oz",
        _ => "kg"
    };
}

/// <summary>
/// Weight measurement units.
/// </summary>
public enum WeightUnit
{
    /// <summary>Kilograms (default for Kenya).</summary>
    Kilogram = 0,

    /// <summary>Grams.</summary>
    Gram = 1,

    /// <summary>Pounds.</summary>
    Pound = 2,

    /// <summary>Ounces.</summary>
    Ounce = 3
}

/// <summary>
/// Scale connection types.
/// </summary>
public enum ScaleConnectionType
{
    /// <summary>USB HID device.</summary>
    UsbHid = 0,

    /// <summary>RS-232 serial connection.</summary>
    Serial = 1,

    /// <summary>Network/TCP connection.</summary>
    Network = 2,

    /// <summary>Bluetooth connection.</summary>
    Bluetooth = 3
}

/// <summary>
/// Scale communication protocols.
/// </summary>
public enum ScaleProtocol
{
    /// <summary>Generic USB HID scale (common Chinese scales).</summary>
    GenericUsbHid = 0,

    /// <summary>CAS protocol (popular in Kenya).</summary>
    Cas = 1,

    /// <summary>Toledo/Mettler-Toledo protocol.</summary>
    Toledo = 2,

    /// <summary>ADAM/CBK protocol.</summary>
    Adam = 3,

    /// <summary>Jadever protocol.</summary>
    Jadever = 4,

    /// <summary>Ohaus protocol.</summary>
    Ohaus = 5,

    /// <summary>Custom configurable protocol.</summary>
    Custom = 99
}

/// <summary>
/// Scale status enumeration.
/// </summary>
public enum ScaleStatus
{
    /// <summary>Scale not connected.</summary>
    Disconnected = 0,

    /// <summary>Attempting to connect.</summary>
    Connecting = 1,

    /// <summary>Connected and ready.</summary>
    Ready = 2,

    /// <summary>Reading weight in progress.</summary>
    Reading = 3,

    /// <summary>Scale error.</summary>
    Error = 4,

    /// <summary>Scale overloaded.</summary>
    Overload = 5
}

/// <summary>
/// Scale configuration settings.
/// </summary>
public class ScaleConfiguration
{
    /// <summary>Gets or sets the configuration ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = "Default Scale";

    /// <summary>Gets or sets the connection type.</summary>
    public ScaleConnectionType ConnectionType { get; set; } = ScaleConnectionType.Serial;

    /// <summary>Gets or sets the protocol.</summary>
    public ScaleProtocol Protocol { get; set; } = ScaleProtocol.Cas;

    /// <summary>Gets or sets the port name (COM1, COM2 for serial).</summary>
    public string? PortName { get; set; }

    /// <summary>Gets or sets the USB Vendor ID.</summary>
    public int? UsbVendorId { get; set; }

    /// <summary>Gets or sets the USB Product ID.</summary>
    public int? UsbProductId { get; set; }

    /// <summary>Gets or sets the baud rate for serial connections.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Gets or sets the data bits.</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>Gets or sets the parity.</summary>
    public string Parity { get; set; } = "None";

    /// <summary>Gets or sets the stop bits.</summary>
    public int StopBits { get; set; } = 1;

    /// <summary>Gets or sets the IP address for network scales.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the port for network scales.</summary>
    public int? TcpPort { get; set; }

    /// <summary>Gets or sets the default weight unit.</summary>
    public WeightUnit DefaultUnit { get; set; } = WeightUnit.Kilogram;

    /// <summary>Gets or sets whether to auto-read when weight stabilizes.</summary>
    public bool AutoReadOnStable { get; set; } = true;

    /// <summary>Gets or sets the stability timeout in milliseconds.</summary>
    public int StabilityTimeoutMs { get; set; } = 2000;

    /// <summary>Gets or sets the read timeout in milliseconds.</summary>
    public int ReadTimeoutMs { get; set; } = 5000;

    /// <summary>Gets or sets whether this is the active configuration.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the decimal places for weight display.</summary>
    public int DecimalPlaces { get; set; } = 3;

    /// <summary>Gets or sets custom command for reading weight.</summary>
    public string? WeightCommand { get; set; }

    /// <summary>Gets or sets custom command for tare.</summary>
    public string? TareCommand { get; set; }

    /// <summary>Gets or sets custom command for zero.</summary>
    public string? ZeroCommand { get; set; }
}

/// <summary>
/// Request to create/update scale configuration.
/// </summary>
public class ScaleConfigurationRequest
{
    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = "Default Scale";

    /// <summary>Gets or sets the connection type.</summary>
    public ScaleConnectionType ConnectionType { get; set; }

    /// <summary>Gets or sets the protocol.</summary>
    public ScaleProtocol Protocol { get; set; }

    /// <summary>Gets or sets the port name.</summary>
    public string? PortName { get; set; }

    /// <summary>Gets or sets the baud rate.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Gets or sets the default unit.</summary>
    public WeightUnit DefaultUnit { get; set; } = WeightUnit.Kilogram;
}

/// <summary>
/// Product weight configuration for weighed items.
/// </summary>
public class WeighedProductConfig
{
    /// <summary>Gets or sets the product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets whether the product is sold by weight.</summary>
    public bool IsWeighed { get; set; }

    /// <summary>Gets or sets the price per weight unit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Gets or sets the weight unit.</summary>
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kilogram;

    /// <summary>Gets or sets the default tare weight (container).</summary>
    public decimal DefaultTareWeight { get; set; }

    /// <summary>Gets or sets the minimum weight for sale.</summary>
    public decimal? MinimumWeight { get; set; }

    /// <summary>Gets or sets the maximum weight for sale.</summary>
    public decimal? MaximumWeight { get; set; }
}

/// <summary>
/// Weighed order item details.
/// </summary>
public class WeighedOrderItem
{
    /// <summary>Gets or sets the product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets or sets the weight.</summary>
    public decimal Weight { get; set; }

    /// <summary>Gets or sets the weight unit.</summary>
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kilogram;

    /// <summary>Gets or sets the price per unit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Gets or sets the tare weight applied.</summary>
    public decimal TareWeight { get; set; }

    /// <summary>Gets or sets the gross weight.</summary>
    public decimal GrossWeight { get; set; }

    /// <summary>Gets the net weight (for billing).</summary>
    public decimal NetWeight => GrossWeight - TareWeight;

    /// <summary>Gets the total price.</summary>
    public decimal TotalPrice => Weight * PricePerUnit;

    /// <summary>Gets the formatted line for receipt.</summary>
    public string ReceiptLine => $"{Weight:N3} {GetUnitAbbreviation()} @ KSh {PricePerUnit:N2}/{GetUnitAbbreviation()}";

    private string GetUnitAbbreviation() => WeightUnit switch
    {
        WeightUnit.Kilogram => "kg",
        WeightUnit.Gram => "g",
        WeightUnit.Pound => "lb",
        WeightUnit.Ounce => "oz",
        _ => "kg"
    };
}

/// <summary>
/// Scale connection result.
/// </summary>
public class ScaleConnectionResult
{
    /// <summary>Gets or sets whether connection was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the error message if failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the scale model/description if detected.</summary>
    public string? ScaleModel { get; set; }

    /// <summary>Gets or sets the firmware version if available.</summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>Gets or sets the maximum capacity.</summary>
    public decimal? MaxCapacity { get; set; }

    /// <summary>Gets or sets the scale resolution.</summary>
    public decimal? Resolution { get; set; }
}

/// <summary>
/// Available serial ports for scale connection.
/// </summary>
public class AvailablePort
{
    /// <summary>Gets or sets the port name (e.g., COM1).</summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>Gets or sets the port description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the port is currently in use.</summary>
    public bool IsInUse { get; set; }
}

/// <summary>
/// Scale event arguments for weight changes.
/// </summary>
public class WeightChangedEventArgs : EventArgs
{
    /// <summary>Gets or sets the weight reading.</summary>
    public WeightReading Reading { get; set; } = new();

    /// <summary>Gets or sets whether this is a stable reading.</summary>
    public bool IsStable => Reading.IsStable;
}

/// <summary>
/// Scale status changed event arguments.
/// </summary>
public class ScaleStatusChangedEventArgs : EventArgs
{
    /// <summary>Gets or sets the previous status.</summary>
    public ScaleStatus PreviousStatus { get; set; }

    /// <summary>Gets or sets the new status.</summary>
    public ScaleStatus NewStatus { get; set; }

    /// <summary>Gets or sets the status message.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Scale test result.
/// </summary>
public class ScaleTestResult
{
    /// <summary>Gets or sets whether the test was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the test reading (if successful).</summary>
    public WeightReading? Reading { get; set; }

    /// <summary>Gets or sets the error message (if failed).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets test duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Gets or sets recommendations based on test.</summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Weight conversion utilities.
/// </summary>
public static class WeightConversion
{
    private const decimal KgToGram = 1000m;
    private const decimal KgToPound = 2.20462m;
    private const decimal KgToOunce = 35.274m;

    /// <summary>
    /// Converts weight from one unit to another.
    /// </summary>
    public static decimal Convert(decimal weight, WeightUnit from, WeightUnit to)
    {
        if (from == to) return weight;

        // First convert to kg (base unit)
        var weightInKg = from switch
        {
            WeightUnit.Kilogram => weight,
            WeightUnit.Gram => weight / KgToGram,
            WeightUnit.Pound => weight / KgToPound,
            WeightUnit.Ounce => weight / KgToOunce,
            _ => weight
        };

        // Then convert from kg to target unit
        return to switch
        {
            WeightUnit.Kilogram => weightInKg,
            WeightUnit.Gram => weightInKg * KgToGram,
            WeightUnit.Pound => weightInKg * KgToPound,
            WeightUnit.Ounce => weightInKg * KgToOunce,
            _ => weightInKg
        };
    }

    /// <summary>
    /// Gets the unit symbol.
    /// </summary>
    public static string GetSymbol(WeightUnit unit) => unit switch
    {
        WeightUnit.Kilogram => "kg",
        WeightUnit.Gram => "g",
        WeightUnit.Pound => "lb",
        WeightUnit.Ounce => "oz",
        _ => "kg"
    };

    /// <summary>
    /// Parses a weight unit from string.
    /// </summary>
    public static WeightUnit ParseUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return WeightUnit.Kilogram;

        return unit.ToLowerInvariant().Trim() switch
        {
            "kg" or "kilogram" or "kilograms" => WeightUnit.Kilogram,
            "g" or "gram" or "grams" => WeightUnit.Gram,
            "lb" or "lbs" or "pound" or "pounds" => WeightUnit.Pound,
            "oz" or "ounce" or "ounces" => WeightUnit.Ounce,
            _ => WeightUnit.Kilogram
        };
    }
}
