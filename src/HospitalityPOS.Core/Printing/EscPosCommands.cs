namespace HospitalityPOS.Core.Printing;

/// <summary>
/// ESC/POS command constants for thermal printer control.
/// </summary>
public static class EscPosCommands
{
    #region Initialization

    /// <summary>
    /// Initialize printer - ESC @
    /// </summary>
    public static readonly byte[] Initialize = { 0x1B, 0x40 };

    #endregion

    #region Text Formatting

    /// <summary>
    /// Turn emphasized (bold) mode on - ESC E 1
    /// </summary>
    public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };

    /// <summary>
    /// Turn emphasized (bold) mode off - ESC E 0
    /// </summary>
    public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };

    /// <summary>
    /// Turn underline mode on (1-dot) - ESC - 1
    /// </summary>
    public static readonly byte[] UnderlineOn = { 0x1B, 0x2D, 0x01 };

    /// <summary>
    /// Turn underline mode on (2-dot) - ESC - 2
    /// </summary>
    public static readonly byte[] UnderlineThickOn = { 0x1B, 0x2D, 0x02 };

    /// <summary>
    /// Turn underline mode off - ESC - 0
    /// </summary>
    public static readonly byte[] UnderlineOff = { 0x1B, 0x2D, 0x00 };

    /// <summary>
    /// Turn double-strike mode on - ESC G 1
    /// </summary>
    public static readonly byte[] DoubleStrikeOn = { 0x1B, 0x47, 0x01 };

    /// <summary>
    /// Turn double-strike mode off - ESC G 0
    /// </summary>
    public static readonly byte[] DoubleStrikeOff = { 0x1B, 0x47, 0x00 };

    /// <summary>
    /// Select inverted (white on black) printing on - GS B 1
    /// </summary>
    public static readonly byte[] InvertOn = { 0x1D, 0x42, 0x01 };

    /// <summary>
    /// Select inverted (white on black) printing off - GS B 0
    /// </summary>
    public static readonly byte[] InvertOff = { 0x1D, 0x42, 0x00 };

    #endregion

    #region Font Size

    /// <summary>
    /// Normal character size - GS ! 0
    /// </summary>
    public static readonly byte[] NormalSize = { 0x1D, 0x21, 0x00 };

    /// <summary>
    /// Double height - GS ! 1
    /// </summary>
    public static readonly byte[] DoubleHeight = { 0x1D, 0x21, 0x01 };

    /// <summary>
    /// Double width - GS ! 16
    /// </summary>
    public static readonly byte[] DoubleWidth = { 0x1D, 0x21, 0x10 };

    /// <summary>
    /// Double width and double height - GS ! 17
    /// </summary>
    public static readonly byte[] DoubleSize = { 0x1D, 0x21, 0x11 };

    /// <summary>
    /// Triple width - GS ! 32
    /// </summary>
    public static readonly byte[] TripleWidth = { 0x1D, 0x21, 0x20 };

    /// <summary>
    /// Quadruple width - GS ! 48
    /// </summary>
    public static readonly byte[] QuadrupleWidth = { 0x1D, 0x21, 0x30 };

    /// <summary>
    /// Set character size with multiplier (1-8 for width, 1-8 for height)
    /// </summary>
    public static byte[] SetCharacterSize(int widthMultiplier, int heightMultiplier)
    {
        widthMultiplier = Math.Clamp(widthMultiplier, 1, 8) - 1;
        heightMultiplier = Math.Clamp(heightMultiplier, 1, 8) - 1;
        return new byte[] { 0x1D, 0x21, (byte)((widthMultiplier << 4) | heightMultiplier) };
    }

    #endregion

    #region Alignment

    /// <summary>
    /// Select left justification - ESC a 0
    /// </summary>
    public static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };

    /// <summary>
    /// Select center justification - ESC a 1
    /// </summary>
    public static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };

    /// <summary>
    /// Select right justification - ESC a 2
    /// </summary>
    public static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };

    #endregion

    #region Paper Control

    /// <summary>
    /// Full cut - GS V 0
    /// </summary>
    public static readonly byte[] FullCut = { 0x1D, 0x56, 0x00 };

    /// <summary>
    /// Partial cut - GS V 1
    /// </summary>
    public static readonly byte[] PartialCut = { 0x1D, 0x56, 0x01 };

    /// <summary>
    /// Feed paper and full cut - GS V A n
    /// </summary>
    public static byte[] FeedAndFullCut(int lines = 3)
    {
        return new byte[] { 0x1D, 0x56, 0x41, (byte)lines };
    }

    /// <summary>
    /// Feed paper and partial cut - GS V B n
    /// </summary>
    public static byte[] FeedAndPartialCut(int lines = 3)
    {
        return new byte[] { 0x1D, 0x56, 0x42, (byte)lines };
    }

    /// <summary>
    /// Line feed - LF
    /// </summary>
    public static readonly byte[] LineFeed = { 0x0A };

    /// <summary>
    /// Carriage return - CR
    /// </summary>
    public static readonly byte[] CarriageReturn = { 0x0D };

    /// <summary>
    /// Feed paper n lines - ESC d n
    /// </summary>
    public static byte[] FeedLines(int lines)
    {
        return new byte[] { 0x1B, 0x64, (byte)Math.Clamp(lines, 0, 255) };
    }

    /// <summary>
    /// Feed paper n dots - ESC J n
    /// </summary>
    public static byte[] FeedDots(int dots)
    {
        return new byte[] { 0x1B, 0x4A, (byte)Math.Clamp(dots, 0, 255) };
    }

    #endregion

    #region Cash Drawer

    /// <summary>
    /// Open cash drawer (pin 2) - ESC p 0 25 250
    /// </summary>
    public static readonly byte[] OpenCashDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA };

    /// <summary>
    /// Open cash drawer (pin 5) - ESC p 1 25 250
    /// </summary>
    public static readonly byte[] OpenCashDrawer2 = { 0x1B, 0x70, 0x01, 0x19, 0xFA };

    /// <summary>
    /// Open cash drawer with custom timing
    /// </summary>
    /// <param name="pin">0 for pin 2, 1 for pin 5</param>
    /// <param name="onTime">On time in 2ms units (0-255)</param>
    /// <param name="offTime">Off time in 2ms units (0-255)</param>
    public static byte[] OpenCashDrawerCustom(int pin, int onTime, int offTime)
    {
        return new byte[] { 0x1B, 0x70, (byte)(pin & 1), (byte)onTime, (byte)offTime };
    }

    #endregion

    #region Beeper

    /// <summary>
    /// Sound buzzer - ESC B n t
    /// </summary>
    /// <param name="times">Number of times to beep (1-9)</param>
    /// <param name="duration">Duration unit (1-9, each unit is about 100ms)</param>
    public static byte[] Beep(int times = 1, int duration = 2)
    {
        times = Math.Clamp(times, 1, 9);
        duration = Math.Clamp(duration, 1, 9);
        return new byte[] { 0x1B, 0x42, (byte)times, (byte)duration };
    }

    #endregion

    #region Print Density

    /// <summary>
    /// Set print density (0-7, default 7)
    /// </summary>
    public static byte[] SetDensity(int density)
    {
        return new byte[] { 0x1D, 0x7C, (byte)Math.Clamp(density, 0, 7) };
    }

    #endregion

    #region Character Sets

    /// <summary>
    /// Select character code table PC437 (USA) - ESC t 0
    /// </summary>
    public static readonly byte[] CodePagePC437 = { 0x1B, 0x74, 0x00 };

    /// <summary>
    /// Select character code table PC850 (Multilingual) - ESC t 2
    /// </summary>
    public static readonly byte[] CodePagePC850 = { 0x1B, 0x74, 0x02 };

    /// <summary>
    /// Select character code table PC858 (Euro) - ESC t 19
    /// </summary>
    public static readonly byte[] CodePagePC858 = { 0x1B, 0x74, 0x13 };

    /// <summary>
    /// Select character code table WPC1252 (Windows Latin1) - ESC t 16
    /// </summary>
    public static readonly byte[] CodePageWPC1252 = { 0x1B, 0x74, 0x10 };

    /// <summary>
    /// Select character code table - ESC t n
    /// </summary>
    public static byte[] SetCodePage(byte codePage)
    {
        return new byte[] { 0x1B, 0x74, codePage };
    }

    #endregion

    #region Line Spacing

    /// <summary>
    /// Set default line spacing - ESC 2
    /// </summary>
    public static readonly byte[] DefaultLineSpacing = { 0x1B, 0x32 };

    /// <summary>
    /// Set line spacing to n dots - ESC 3 n
    /// </summary>
    public static byte[] SetLineSpacing(int dots)
    {
        return new byte[] { 0x1B, 0x33, (byte)Math.Clamp(dots, 0, 255) };
    }

    #endregion

    #region Graphics

    /// <summary>
    /// Raster bit image header - GS v 0 m wL wH hL hH
    /// </summary>
    /// <param name="widthBytes">Width in bytes (pixels / 8)</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="mode">0 = normal, 1 = double width, 2 = double height, 3 = quadruple</param>
    public static byte[] RasterBitImage(int widthBytes, int height, int mode = 0)
    {
        var wL = (byte)(widthBytes % 256);
        var wH = (byte)(widthBytes / 256);
        var hL = (byte)(height % 256);
        var hH = (byte)(height / 256);
        return new byte[] { 0x1D, 0x76, 0x30, (byte)mode, wL, wH, hL, hH };
    }

    /// <summary>
    /// Print stored bit image - FS p n m
    /// </summary>
    public static byte[] PrintStoredImage(int imageNumber, int mode = 0)
    {
        return new byte[] { 0x1C, 0x70, (byte)imageNumber, (byte)mode };
    }

    #endregion

    #region Barcode

    /// <summary>
    /// Set barcode height - GS h n (n = 1-255 dots)
    /// </summary>
    public static byte[] SetBarcodeHeight(int height)
    {
        return new byte[] { 0x1D, 0x68, (byte)Math.Clamp(height, 1, 255) };
    }

    /// <summary>
    /// Set barcode width - GS w n (n = 2-6)
    /// </summary>
    public static byte[] SetBarcodeWidth(int width)
    {
        return new byte[] { 0x1D, 0x77, (byte)Math.Clamp(width, 2, 6) };
    }

    /// <summary>
    /// Set barcode text position - GS H n
    /// 0 = not printed, 1 = above, 2 = below, 3 = both
    /// </summary>
    public static byte[] SetBarcodeTextPosition(int position)
    {
        return new byte[] { 0x1D, 0x48, (byte)Math.Clamp(position, 0, 3) };
    }

    /// <summary>
    /// Print barcode - GS k m d1...dk NUL
    /// </summary>
    public static byte[] PrintBarcode(BarcodeType type, string data)
    {
        var bytes = new List<byte> { 0x1D, 0x6B, (byte)type };
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(data));
        bytes.Add(0x00); // NUL terminator
        return bytes.ToArray();
    }

    #endregion

    #region QR Code

    /// <summary>
    /// Set QR code model - GS ( k pL pH cn fn n
    /// </summary>
    public static readonly byte[] QRCodeModel2 = { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 };

    /// <summary>
    /// Set QR code size (1-16) - GS ( k pL pH cn fn n
    /// </summary>
    public static byte[] SetQRCodeSize(int size)
    {
        size = Math.Clamp(size, 1, 16);
        return new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)size };
    }

    /// <summary>
    /// Set QR code error correction level - GS ( k pL pH cn fn n
    /// </summary>
    public static byte[] SetQRCodeErrorCorrection(QRErrorCorrection level)
    {
        return new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, (byte)level };
    }

    /// <summary>
    /// Store QR code data - GS ( k pL pH cn fn d1...dk
    /// </summary>
    public static byte[] StoreQRCodeData(string data)
    {
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var length = dataBytes.Length + 3;
        var pL = (byte)(length % 256);
        var pH = (byte)(length / 256);

        var result = new List<byte> { 0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 };
        result.AddRange(dataBytes);
        return result.ToArray();
    }

    /// <summary>
    /// Print stored QR code - GS ( k pL pH cn fn m
    /// </summary>
    public static readonly byte[] PrintQRCode = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };

    #endregion

    #region Status

    /// <summary>
    /// Transmit real-time status - DLE EOT n
    /// </summary>
    public static byte[] GetStatus(StatusType type)
    {
        return new byte[] { 0x10, 0x04, (byte)type };
    }

    #endregion
}

/// <summary>
/// Barcode types for ESC/POS
/// </summary>
public enum BarcodeType : byte
{
    UPC_A = 0,
    UPC_E = 1,
    EAN13 = 2,
    EAN8 = 3,
    CODE39 = 4,
    ITF = 5,
    CODABAR = 6,
    CODE93 = 72,
    CODE128 = 73
}

/// <summary>
/// QR Code error correction levels
/// </summary>
public enum QRErrorCorrection : byte
{
    L = 48, // 7% recovery
    M = 49, // 15% recovery
    Q = 50, // 25% recovery
    H = 51  // 30% recovery
}

/// <summary>
/// Printer status query types
/// </summary>
public enum StatusType : byte
{
    Printer = 1,
    OfflineCause = 2,
    ErrorCause = 3,
    PaperRollSensor = 4
}
