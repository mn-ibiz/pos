namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Type of barcode.
/// </summary>
public enum BarcodeType
{
    /// <summary>
    /// Standard EAN-13 barcode (e.g., 5901234123457).
    /// </summary>
    EAN13 = 0,

    /// <summary>
    /// Standard EAN-8 barcode.
    /// </summary>
    EAN8 = 1,

    /// <summary>
    /// UPC-A barcode.
    /// </summary>
    UPCA = 2,

    /// <summary>
    /// UPC-E barcode.
    /// </summary>
    UPCE = 3,

    /// <summary>
    /// EAN-128/GS1-128 barcode.
    /// </summary>
    EAN128 = 4,

    /// <summary>
    /// Code 128 barcode.
    /// </summary>
    Code128 = 5,

    /// <summary>
    /// Code 39 barcode.
    /// </summary>
    Code39 = 6,

    /// <summary>
    /// QR Code.
    /// </summary>
    QRCode = 7,

    /// <summary>
    /// Internal/store-generated barcode.
    /// </summary>
    Internal = 8
}

/// <summary>
/// Type of barcode prefix for weighted/priced items.
/// </summary>
public enum WeightedBarcodePrefix
{
    /// <summary>
    /// Not a weighted barcode (standard product lookup).
    /// </summary>
    None = 0,

    /// <summary>
    /// Prefix 20: Price-embedded barcode.
    /// </summary>
    Prefix20 = 20,

    /// <summary>
    /// Prefix 21: Price-embedded barcode.
    /// </summary>
    Prefix21 = 21,

    /// <summary>
    /// Prefix 22: Price-embedded barcode.
    /// </summary>
    Prefix22 = 22,

    /// <summary>
    /// Prefix 23: Weight-embedded barcode.
    /// </summary>
    Prefix23 = 23,

    /// <summary>
    /// Prefix 24: Weight-embedded barcode.
    /// </summary>
    Prefix24 = 24,

    /// <summary>
    /// Prefix 25: Weight-embedded barcode.
    /// </summary>
    Prefix25 = 25,

    /// <summary>
    /// Prefix 26: Weight-embedded barcode.
    /// </summary>
    Prefix26 = 26,

    /// <summary>
    /// Prefix 27: Weight-embedded barcode.
    /// </summary>
    Prefix27 = 27,

    /// <summary>
    /// Prefix 28: Weight-embedded barcode.
    /// </summary>
    Prefix28 = 28,

    /// <summary>
    /// Prefix 29: Weight-embedded barcode.
    /// </summary>
    Prefix29 = 29
}

/// <summary>
/// Method used to embed data in weighted barcode.
/// </summary>
public enum WeightedBarcodeFormat
{
    /// <summary>
    /// Format: PPAAAAVVVVVC (P=prefix, A=article, V=value, C=check digit).
    /// </summary>
    StandardPrice = 0,

    /// <summary>
    /// Format: PPAAAAWWWWWC (P=prefix, A=article, W=weight in grams, C=check digit).
    /// </summary>
    StandardWeight = 1,

    /// <summary>
    /// Format: PPVVVVAAAAAC (P=prefix, V=value, A=article, C=check digit).
    /// </summary>
    PriceFirst = 2,

    /// <summary>
    /// Custom format defined by store.
    /// </summary>
    Custom = 3
}

/// <summary>
/// Type of weighing scale.
/// </summary>
public enum ScaleType
{
    /// <summary>
    /// Basic weight-only scale.
    /// </summary>
    Basic = 0,

    /// <summary>
    /// Label-printing scale.
    /// </summary>
    LabelPrinting = 1,

    /// <summary>
    /// Receipt-printing scale.
    /// </summary>
    ReceiptPrinting = 2,

    /// <summary>
    /// POS-integrated scale (weight sent to POS).
    /// </summary>
    POSIntegrated = 3
}

/// <summary>
/// Communication protocol for scale.
/// </summary>
public enum ScaleProtocol
{
    /// <summary>
    /// No protocol (manual weight entry).
    /// </summary>
    None = 0,

    /// <summary>
    /// Serial RS-232 connection.
    /// </summary>
    Serial = 1,

    /// <summary>
    /// USB HID connection.
    /// </summary>
    USB = 2,

    /// <summary>
    /// Network/Ethernet connection.
    /// </summary>
    Network = 3,

    /// <summary>
    /// Bluetooth connection.
    /// </summary>
    Bluetooth = 4
}

/// <summary>
/// Status of scale connection.
/// </summary>
public enum ScaleStatus
{
    /// <summary>
    /// Not connected.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Connected and ready.
    /// </summary>
    Connected = 1,

    /// <summary>
    /// Scale is busy/stabilizing.
    /// </summary>
    Busy = 2,

    /// <summary>
    /// Scale error.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Scale overloaded.
    /// </summary>
    Overload = 4
}

/// <summary>
/// Unit of measurement for weight.
/// </summary>
public enum WeightUnit
{
    /// <summary>
    /// Kilograms.
    /// </summary>
    Kilograms = 0,

    /// <summary>
    /// Grams.
    /// </summary>
    Grams = 1,

    /// <summary>
    /// Pounds.
    /// </summary>
    Pounds = 2,

    /// <summary>
    /// Ounces.
    /// </summary>
    Ounces = 3
}
