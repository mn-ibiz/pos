using System.Text;

namespace HospitalityPOS.Core.Printing;

/// <summary>
/// Fluent builder for creating ESC/POS print documents.
/// </summary>
public class EscPosPrintDocument
{
    private readonly List<byte> _buffer = new();
    private readonly int _paperWidth;
    private readonly Encoding _encoding;

    /// <summary>
    /// Default paper width in characters (58mm paper = 32 chars, 80mm paper = 48 chars).
    /// </summary>
    public const int DefaultPaperWidth58mm = 32;
    public const int DefaultPaperWidth80mm = 48;

    /// <summary>
    /// Initializes a new ESC/POS print document.
    /// </summary>
    /// <param name="paperWidth">Paper width in characters.</param>
    /// <param name="encoding">Text encoding (default: ASCII for compatibility).</param>
    public EscPosPrintDocument(int paperWidth = DefaultPaperWidth80mm, Encoding? encoding = null)
    {
        _paperWidth = paperWidth;
        _encoding = encoding ?? Encoding.ASCII;

        // Initialize printer
        _buffer.AddRange(EscPosCommands.Initialize);
    }

    #region Static Factory Methods

    /// <summary>
    /// Creates a new document for 58mm paper.
    /// </summary>
    public static EscPosPrintDocument Create58mm() => new(DefaultPaperWidth58mm);

    /// <summary>
    /// Creates a new document for 80mm paper.
    /// </summary>
    public static EscPosPrintDocument Create80mm() => new(DefaultPaperWidth80mm);

    /// <summary>
    /// Creates a new document with custom paper width.
    /// </summary>
    public static EscPosPrintDocument Create(int paperWidth) => new(paperWidth);

    #endregion

    #region Text Content

    /// <summary>
    /// Appends text to the document.
    /// </summary>
    public EscPosPrintDocument Text(string text)
    {
        _buffer.AddRange(_encoding.GetBytes(text));
        return this;
    }

    /// <summary>
    /// Appends text followed by a line feed.
    /// </summary>
    public EscPosPrintDocument TextLine(string text)
    {
        _buffer.AddRange(_encoding.GetBytes(text));
        _buffer.AddRange(EscPosCommands.LineFeed);
        return this;
    }

    /// <summary>
    /// Appends an empty line.
    /// </summary>
    public EscPosPrintDocument EmptyLine()
    {
        _buffer.AddRange(EscPosCommands.LineFeed);
        return this;
    }

    /// <summary>
    /// Appends multiple empty lines.
    /// </summary>
    public EscPosPrintDocument EmptyLines(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _buffer.AddRange(EscPosCommands.LineFeed);
        }
        return this;
    }

    #endregion

    #region Text Formatting

    /// <summary>
    /// Enables bold text.
    /// </summary>
    public EscPosPrintDocument Bold()
    {
        _buffer.AddRange(EscPosCommands.BoldOn);
        return this;
    }

    /// <summary>
    /// Disables bold text.
    /// </summary>
    public EscPosPrintDocument NoBold()
    {
        _buffer.AddRange(EscPosCommands.BoldOff);
        return this;
    }

    /// <summary>
    /// Appends bold text and resets formatting.
    /// </summary>
    public EscPosPrintDocument BoldText(string text)
    {
        return Bold().Text(text).NoBold();
    }

    /// <summary>
    /// Appends bold text line and resets formatting.
    /// </summary>
    public EscPosPrintDocument BoldLine(string text)
    {
        return Bold().TextLine(text).NoBold();
    }

    /// <summary>
    /// Enables underline (1-dot).
    /// </summary>
    public EscPosPrintDocument Underline()
    {
        _buffer.AddRange(EscPosCommands.UnderlineOn);
        return this;
    }

    /// <summary>
    /// Enables thick underline (2-dot).
    /// </summary>
    public EscPosPrintDocument ThickUnderline()
    {
        _buffer.AddRange(EscPosCommands.UnderlineThickOn);
        return this;
    }

    /// <summary>
    /// Disables underline.
    /// </summary>
    public EscPosPrintDocument NoUnderline()
    {
        _buffer.AddRange(EscPosCommands.UnderlineOff);
        return this;
    }

    /// <summary>
    /// Enables double-strike mode.
    /// </summary>
    public EscPosPrintDocument DoubleStrike()
    {
        _buffer.AddRange(EscPosCommands.DoubleStrikeOn);
        return this;
    }

    /// <summary>
    /// Disables double-strike mode.
    /// </summary>
    public EscPosPrintDocument NoDoubleStrike()
    {
        _buffer.AddRange(EscPosCommands.DoubleStrikeOff);
        return this;
    }

    /// <summary>
    /// Enables inverted (white on black) printing.
    /// </summary>
    public EscPosPrintDocument Invert()
    {
        _buffer.AddRange(EscPosCommands.InvertOn);
        return this;
    }

    /// <summary>
    /// Disables inverted printing.
    /// </summary>
    public EscPosPrintDocument NoInvert()
    {
        _buffer.AddRange(EscPosCommands.InvertOff);
        return this;
    }

    #endregion

    #region Font Size

    /// <summary>
    /// Sets normal character size.
    /// </summary>
    public EscPosPrintDocument NormalSize()
    {
        _buffer.AddRange(EscPosCommands.NormalSize);
        return this;
    }

    /// <summary>
    /// Sets double height.
    /// </summary>
    public EscPosPrintDocument DoubleHeight()
    {
        _buffer.AddRange(EscPosCommands.DoubleHeight);
        return this;
    }

    /// <summary>
    /// Sets double width.
    /// </summary>
    public EscPosPrintDocument DoubleWidth()
    {
        _buffer.AddRange(EscPosCommands.DoubleWidth);
        return this;
    }

    /// <summary>
    /// Sets double width and height.
    /// </summary>
    public EscPosPrintDocument DoubleSize()
    {
        _buffer.AddRange(EscPosCommands.DoubleSize);
        return this;
    }

    /// <summary>
    /// Sets custom character size multiplier.
    /// </summary>
    /// <param name="widthMultiplier">Width multiplier (1-8).</param>
    /// <param name="heightMultiplier">Height multiplier (1-8).</param>
    public EscPosPrintDocument SetSize(int widthMultiplier, int heightMultiplier)
    {
        _buffer.AddRange(EscPosCommands.SetCharacterSize(widthMultiplier, heightMultiplier));
        return this;
    }

    /// <summary>
    /// Appends large text (double size) and resets to normal.
    /// </summary>
    public EscPosPrintDocument LargeLine(string text)
    {
        return DoubleSize().TextLine(text).NormalSize();
    }

    #endregion

    #region Alignment

    /// <summary>
    /// Sets left alignment.
    /// </summary>
    public EscPosPrintDocument AlignLeft()
    {
        _buffer.AddRange(EscPosCommands.AlignLeft);
        return this;
    }

    /// <summary>
    /// Sets center alignment.
    /// </summary>
    public EscPosPrintDocument AlignCenter()
    {
        _buffer.AddRange(EscPosCommands.AlignCenter);
        return this;
    }

    /// <summary>
    /// Sets right alignment.
    /// </summary>
    public EscPosPrintDocument AlignRight()
    {
        _buffer.AddRange(EscPosCommands.AlignRight);
        return this;
    }

    /// <summary>
    /// Appends centered text and resets to left alignment.
    /// </summary>
    public EscPosPrintDocument CenteredLine(string text)
    {
        return AlignCenter().TextLine(text).AlignLeft();
    }

    /// <summary>
    /// Appends right-aligned text and resets to left alignment.
    /// </summary>
    public EscPosPrintDocument RightLine(string text)
    {
        return AlignRight().TextLine(text).AlignLeft();
    }

    #endregion

    #region Lines and Separators

    /// <summary>
    /// Appends a separator line using specified character.
    /// </summary>
    public EscPosPrintDocument Separator(char character = '-')
    {
        return TextLine(new string(character, _paperWidth));
    }

    /// <summary>
    /// Appends a double separator line.
    /// </summary>
    public EscPosPrintDocument DoubleSeparator()
    {
        return Separator('=');
    }

    /// <summary>
    /// Appends a star separator line.
    /// </summary>
    public EscPosPrintDocument StarSeparator()
    {
        return Separator('*');
    }

    /// <summary>
    /// Appends a two-column line with left and right text.
    /// </summary>
    public EscPosPrintDocument TwoColumnLine(string left, string right)
    {
        int padding = _paperWidth - left.Length - right.Length;
        if (padding < 1) padding = 1;

        string line = left + new string(' ', padding) + right;
        if (line.Length > _paperWidth)
        {
            line = line[.._paperWidth];
        }

        return TextLine(line);
    }

    /// <summary>
    /// Appends a three-column line.
    /// </summary>
    public EscPosPrintDocument ThreeColumnLine(string left, string center, string right)
    {
        int totalContent = left.Length + center.Length + right.Length;
        int totalPadding = _paperWidth - totalContent;

        if (totalPadding < 2)
        {
            // Not enough space, truncate
            return TwoColumnLine(left, right);
        }

        int leftPadding = totalPadding / 2;
        int rightPadding = totalPadding - leftPadding;

        string line = left + new string(' ', leftPadding) + center + new string(' ', rightPadding) + right;
        if (line.Length > _paperWidth)
        {
            line = line[.._paperWidth];
        }

        return TextLine(line);
    }

    /// <summary>
    /// Appends a labeled value line (label: value format).
    /// </summary>
    public EscPosPrintDocument LabelValue(string label, string value, char separator = ':')
    {
        string labelPart = $"{label}{separator} ";
        int valueWidth = _paperWidth - labelPart.Length;

        if (valueWidth < 1)
        {
            return TextLine(label).TextLine($"  {value}");
        }

        if (value.Length > valueWidth)
        {
            // Wrap long values
            return TextLine(labelPart + value[..valueWidth])
                .TextLine(new string(' ', labelPart.Length) + value[valueWidth..]);
        }

        return TextLine(labelPart + value);
    }

    /// <summary>
    /// Appends a table row with multiple columns.
    /// </summary>
    public EscPosPrintDocument TableRow(params (string text, int width)[] columns)
    {
        var sb = new StringBuilder();

        foreach (var (text, width) in columns)
        {
            string cell = text.Length > width ? text[..width] : text.PadRight(width);
            sb.Append(cell);
        }

        string line = sb.ToString();
        if (line.Length > _paperWidth)
        {
            line = line[.._paperWidth];
        }

        return TextLine(line);
    }

    /// <summary>
    /// Appends a receipt item line with quantity, description, and amount.
    /// </summary>
    public EscPosPrintDocument ItemLine(int quantity, string description, decimal amount)
    {
        string qtyStr = quantity.ToString();
        string amtStr = amount.ToString("N2");

        // Format: QTY DESCRIPTION          AMOUNT
        int descWidth = _paperWidth - qtyStr.Length - amtStr.Length - 2; // 2 spaces

        if (description.Length > descWidth)
        {
            description = description[..descWidth];
        }

        return TwoColumnLine($"{qtyStr} {description}", amtStr);
    }

    /// <summary>
    /// Appends a total line with label and amount.
    /// </summary>
    public EscPosPrintDocument TotalLine(string label, decimal amount)
    {
        return Bold().TwoColumnLine(label, amount.ToString("N2")).NoBold();
    }

    /// <summary>
    /// Appends a grand total line (large, bold).
    /// </summary>
    public EscPosPrintDocument GrandTotalLine(string label, decimal amount)
    {
        return DoubleSize().Bold()
            .TwoColumnLine(label, amount.ToString("N2"))
            .NoBold().NormalSize();
    }

    /// <summary>
    /// Appends an item line with offer pricing (Was: X, Now: Y).
    /// </summary>
    /// <param name="quantity">Item quantity.</param>
    /// <param name="description">Item description.</param>
    /// <param name="originalPrice">Original price before offer.</param>
    /// <param name="offerPrice">Discounted offer price.</param>
    /// <param name="totalAmount">Line total amount.</param>
    /// <param name="offerName">Name of the applied offer.</param>
    public EscPosPrintDocument OfferItemLine(int quantity, string description, decimal originalPrice, decimal offerPrice, decimal totalAmount, string? offerName = null)
    {
        // Main item line: QTY DESCRIPTION     AMOUNT
        ItemLine(quantity, description, totalAmount);

        // Offer details line: Was: X.XX  Now: Y.YY
        string wasNow = $"  Was: {originalPrice:N2}  Now: {offerPrice:N2}";
        TextLine(wasNow);

        // Savings line with offer name
        decimal savings = (originalPrice - offerPrice) * quantity;
        string savingsLine = string.IsNullOrEmpty(offerName)
            ? $"  You Save: {savings:N2}"
            : $"  {offerName} - Save: {savings:N2}";
        TextLine(savingsLine);

        return this;
    }

    /// <summary>
    /// Appends a total savings line for all offers.
    /// </summary>
    /// <param name="totalSavings">Total savings amount from all offers.</param>
    /// <param name="itemCount">Number of items with offers applied.</param>
    public EscPosPrintDocument TotalSavingsLine(decimal totalSavings, int itemCount = 0)
    {
        if (totalSavings <= 0) return this;

        Separator('-');

        if (itemCount > 0)
        {
            TwoColumnLine($"TOTAL SAVINGS ({itemCount} items)", totalSavings.ToString("N2"));
        }
        else
        {
            Bold().TwoColumnLine("TOTAL SAVINGS", totalSavings.ToString("N2")).NoBold();
        }

        return this;
    }

    #endregion

    #region Paper Control

    /// <summary>
    /// Feeds paper by specified number of lines.
    /// </summary>
    public EscPosPrintDocument Feed(int lines = 1)
    {
        _buffer.AddRange(EscPosCommands.FeedLines(lines));
        return this;
    }

    /// <summary>
    /// Performs a full paper cut.
    /// </summary>
    public EscPosPrintDocument FullCut()
    {
        _buffer.AddRange(EscPosCommands.FullCut);
        return this;
    }

    /// <summary>
    /// Performs a partial paper cut.
    /// </summary>
    public EscPosPrintDocument PartialCut()
    {
        _buffer.AddRange(EscPosCommands.PartialCut);
        return this;
    }

    /// <summary>
    /// Feeds paper and performs full cut.
    /// </summary>
    public EscPosPrintDocument FeedAndCut(int lines = 3)
    {
        _buffer.AddRange(EscPosCommands.FeedAndFullCut(lines));
        return this;
    }

    /// <summary>
    /// Feeds paper and performs partial cut.
    /// </summary>
    public EscPosPrintDocument FeedAndPartialCut(int lines = 3)
    {
        _buffer.AddRange(EscPosCommands.FeedAndPartialCut(lines));
        return this;
    }

    #endregion

    #region Cash Drawer

    /// <summary>
    /// Opens the cash drawer (pin 2).
    /// </summary>
    public EscPosPrintDocument OpenCashDrawer()
    {
        _buffer.AddRange(EscPosCommands.OpenCashDrawer);
        return this;
    }

    /// <summary>
    /// Opens the cash drawer (pin 5).
    /// </summary>
    public EscPosPrintDocument OpenCashDrawer2()
    {
        _buffer.AddRange(EscPosCommands.OpenCashDrawer2);
        return this;
    }

    /// <summary>
    /// Opens cash drawer with custom timing.
    /// </summary>
    public EscPosPrintDocument OpenCashDrawerCustom(int pin, int onTime, int offTime)
    {
        _buffer.AddRange(EscPosCommands.OpenCashDrawerCustom(pin, onTime, offTime));
        return this;
    }

    #endregion

    #region Barcode

    /// <summary>
    /// Prints a barcode.
    /// </summary>
    /// <param name="type">Barcode type.</param>
    /// <param name="data">Barcode data.</param>
    /// <param name="height">Bar height in dots (1-255).</param>
    /// <param name="width">Bar width (2-6).</param>
    /// <param name="textPosition">HRI text position (0=none, 1=above, 2=below, 3=both).</param>
    public EscPosPrintDocument Barcode(BarcodeType type, string data, int height = 50, int width = 3, int textPosition = 2)
    {
        _buffer.AddRange(EscPosCommands.SetBarcodeHeight(height));
        _buffer.AddRange(EscPosCommands.SetBarcodeWidth(width));
        _buffer.AddRange(EscPosCommands.SetBarcodeTextPosition(textPosition));
        _buffer.AddRange(EscPosCommands.PrintBarcode(type, data));
        return this;
    }

    /// <summary>
    /// Prints a CODE128 barcode (common for receipts).
    /// </summary>
    public EscPosPrintDocument Code128(string data, int height = 50)
    {
        return AlignCenter()
            .Barcode(BarcodeType.CODE128, data, height)
            .AlignLeft();
    }

    /// <summary>
    /// Prints an EAN-13 barcode.
    /// </summary>
    public EscPosPrintDocument Ean13(string data, int height = 50)
    {
        return AlignCenter()
            .Barcode(BarcodeType.EAN13, data, height)
            .AlignLeft();
    }

    #endregion

    #region QR Code

    /// <summary>
    /// Prints a QR code.
    /// </summary>
    /// <param name="data">QR code data.</param>
    /// <param name="size">Module size (1-16).</param>
    /// <param name="errorCorrection">Error correction level.</param>
    public EscPosPrintDocument QRCode(string data, int size = 4, QRErrorCorrection errorCorrection = QRErrorCorrection.M)
    {
        _buffer.AddRange(EscPosCommands.QRCodeModel2);
        _buffer.AddRange(EscPosCommands.SetQRCodeSize(size));
        _buffer.AddRange(EscPosCommands.SetQRCodeErrorCorrection(errorCorrection));
        _buffer.AddRange(EscPosCommands.StoreQRCodeData(data));
        _buffer.AddRange(EscPosCommands.PrintQRCode);
        return this;
    }

    /// <summary>
    /// Prints a centered QR code.
    /// </summary>
    public EscPosPrintDocument CenteredQRCode(string data, int size = 4)
    {
        return AlignCenter()
            .QRCode(data, size)
            .AlignLeft();
    }

    #endregion

    #region Graphics

    /// <summary>
    /// Appends raster image data.
    /// </summary>
    /// <param name="imageData">Image data in ESC/POS raster format.</param>
    /// <param name="widthBytes">Width in bytes (pixels / 8).</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="mode">Print mode (0=normal, 1=double width, 2=double height, 3=quad).</param>
    public EscPosPrintDocument RasterImage(byte[] imageData, int widthBytes, int height, int mode = 0)
    {
        _buffer.AddRange(EscPosCommands.RasterBitImage(widthBytes, height, mode));
        _buffer.AddRange(imageData);
        return this;
    }

    #endregion

    #region Beeper

    /// <summary>
    /// Sounds the printer beeper.
    /// </summary>
    public EscPosPrintDocument Beep(int times = 1, int duration = 2)
    {
        _buffer.AddRange(EscPosCommands.Beep(times, duration));
        return this;
    }

    #endregion

    #region Settings

    /// <summary>
    /// Sets the character code page.
    /// </summary>
    public EscPosPrintDocument SetCodePage(byte codePage)
    {
        _buffer.AddRange(EscPosCommands.SetCodePage(codePage));
        return this;
    }

    /// <summary>
    /// Sets print density.
    /// </summary>
    public EscPosPrintDocument SetDensity(int density)
    {
        _buffer.AddRange(EscPosCommands.SetDensity(density));
        return this;
    }

    /// <summary>
    /// Sets line spacing.
    /// </summary>
    public EscPosPrintDocument SetLineSpacing(int dots)
    {
        _buffer.AddRange(EscPosCommands.SetLineSpacing(dots));
        return this;
    }

    /// <summary>
    /// Resets to default line spacing.
    /// </summary>
    public EscPosPrintDocument DefaultLineSpacing()
    {
        _buffer.AddRange(EscPosCommands.DefaultLineSpacing);
        return this;
    }

    /// <summary>
    /// Resets all formatting to defaults.
    /// </summary>
    public EscPosPrintDocument Reset()
    {
        _buffer.AddRange(EscPosCommands.Initialize);
        return this;
    }

    #endregion

    #region Custom Commands

    /// <summary>
    /// Appends raw bytes to the document.
    /// </summary>
    public EscPosPrintDocument Raw(byte[] data)
    {
        _buffer.AddRange(data);
        return this;
    }

    /// <summary>
    /// Appends raw bytes to the document.
    /// </summary>
    public EscPosPrintDocument Raw(params byte[] data)
    {
        _buffer.AddRange(data);
        return this;
    }

    #endregion

    #region Receipt Templates

    /// <summary>
    /// Appends a standard receipt header.
    /// </summary>
    public EscPosPrintDocument ReceiptHeader(string businessName, string? address = null, string? phone = null, string? taxId = null)
    {
        AlignCenter().DoubleSize().Bold()
            .TextLine(businessName)
            .NoBold().NormalSize();

        if (!string.IsNullOrEmpty(address))
        {
            TextLine(address);
        }

        if (!string.IsNullOrEmpty(phone))
        {
            TextLine($"Tel: {phone}");
        }

        if (!string.IsNullOrEmpty(taxId))
        {
            TextLine($"PIN: {taxId}");
        }

        return AlignLeft().DoubleSeparator();
    }

    /// <summary>
    /// Appends a standard receipt footer.
    /// </summary>
    public EscPosPrintDocument ReceiptFooter(string? message = null, string? thankYouMessage = null)
    {
        Separator();

        if (!string.IsNullOrEmpty(message))
        {
            AlignCenter().TextLine(message).AlignLeft();
        }

        if (!string.IsNullOrEmpty(thankYouMessage))
        {
            AlignCenter().BoldLine(thankYouMessage ?? "Thank you for your business!").AlignLeft();
        }

        return EmptyLines(2).FeedAndPartialCut();
    }

    /// <summary>
    /// Appends receipt metadata (date, receipt number, cashier).
    /// </summary>
    public EscPosPrintDocument ReceiptInfo(string receiptNumber, DateTime dateTime, string? cashier = null, string? tableNumber = null)
    {
        TwoColumnLine("Receipt #:", receiptNumber);
        TwoColumnLine("Date:", dateTime.ToString("dd/MM/yyyy"));
        TwoColumnLine("Time:", dateTime.ToString("HH:mm:ss"));

        if (!string.IsNullOrEmpty(cashier))
        {
            TwoColumnLine("Cashier:", cashier);
        }

        if (!string.IsNullOrEmpty(tableNumber))
        {
            TwoColumnLine("Table:", tableNumber);
        }

        return Separator();
    }

    #endregion

    #region KOT Templates

    /// <summary>
    /// Appends a Kitchen Order Ticket header.
    /// </summary>
    public EscPosPrintDocument KOTHeader(string orderNumber, string? tableNumber = null, string? waiter = null, DateTime? time = null, bool isIncremental = false)
    {
        AlignCenter();

        if (isIncremental)
        {
            Invert().DoubleSize().Bold()
                .TextLine(" ** ADDITION ** ")
                .NoBold().NormalSize().NoInvert();
        }
        else
        {
            DoubleSize().Bold()
                .TextLine("KITCHEN ORDER")
                .NoBold().NormalSize();
        }

        AlignLeft().DoubleSeparator();

        DoubleSize().TwoColumnLine("Order:", orderNumber).NormalSize();

        if (!string.IsNullOrEmpty(tableNumber))
        {
            TwoColumnLine("Table:", tableNumber);
        }

        if (!string.IsNullOrEmpty(waiter))
        {
            TwoColumnLine("Waiter:", waiter);
        }

        TwoColumnLine("Time:", (time ?? DateTime.Now).ToString("HH:mm"));

        return DoubleSeparator();
    }

    /// <summary>
    /// Appends a KOT item line.
    /// </summary>
    public EscPosPrintDocument KOTItem(int quantity, string name, string? modifier = null, string? notes = null, bool isVoided = false)
    {
        if (isVoided)
        {
            Invert().Bold()
                .TextLine($"VOID: {quantity}x {name}")
                .NoBold().NoInvert();
        }
        else
        {
            DoubleSize().TextLine($"{quantity}x {name}").NormalSize();
        }

        if (!string.IsNullOrEmpty(modifier))
        {
            TextLine($"   > {modifier}");
        }

        if (!string.IsNullOrEmpty(notes))
        {
            Bold().TextLine($"   NOTE: {notes}").NoBold();
        }

        return this;
    }

    /// <summary>
    /// Appends a KOT footer.
    /// </summary>
    public EscPosPrintDocument KOTFooter()
    {
        return DoubleSeparator()
            .EmptyLines(2)
            .FeedAndFullCut();
    }

    #endregion

    #region Build

    /// <summary>
    /// Gets the current buffer size.
    /// </summary>
    public int Length => _buffer.Count;

    /// <summary>
    /// Gets the paper width in characters.
    /// </summary>
    public int PaperWidth => _paperWidth;

    /// <summary>
    /// Builds the document as a byte array.
    /// </summary>
    public byte[] Build()
    {
        return _buffer.ToArray();
    }

    /// <summary>
    /// Builds the document with final cut.
    /// </summary>
    public byte[] BuildWithCut()
    {
        var result = new List<byte>(_buffer);
        result.AddRange(EscPosCommands.FeedAndPartialCut(3));
        return result.ToArray();
    }

    /// <summary>
    /// Clears the buffer and reinitializes.
    /// </summary>
    public EscPosPrintDocument Clear()
    {
        _buffer.Clear();
        _buffer.AddRange(EscPosCommands.Initialize);
        return this;
    }

    #endregion
}
