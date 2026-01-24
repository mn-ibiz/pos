using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace HospitalityPOS.WPF.Models;

/// <summary>
/// Types of elements that can be placed on a label.
/// </summary>
public enum LabelElementType
{
    Text,
    Barcode,
    Price,
    QRCode,
    Image,
    Box,
    Line
}

/// <summary>
/// Barcode types supported for labels.
/// </summary>
public enum BarcodeType
{
    EAN13,
    EAN8,
    Code128,
    Code39,
    UPCA,
    UPCE,
    ITF
}

/// <summary>
/// Text alignment options.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Rotation angles for elements.
/// </summary>
public enum ElementRotation
{
    Rotate0 = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270
}

/// <summary>
/// Represents a design element on the label canvas.
/// </summary>
public partial class LabelDesignElement : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private LabelElementType _elementType;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _fontFamily = "0"; // ZPL font name

    [ObservableProperty]
    private int _fontSize = 24;

    [ObservableProperty]
    private bool _isBold;

    [ObservableProperty]
    private TextAlignment _textAlignment = TextAlignment.Left;

    [ObservableProperty]
    private ElementRotation _rotation = ElementRotation.Rotate0;

    [ObservableProperty]
    private BarcodeType _barcodeType = BarcodeType.EAN13;

    [ObservableProperty]
    private int _barcodeHeight = 50;

    [ObservableProperty]
    private bool _showBarcodeText = true;

    [ObservableProperty]
    private int _qrSize = 4; // QR code module size

    [ObservableProperty]
    private string _qrErrorCorrection = "M"; // L, M, Q, H

    [ObservableProperty]
    private string? _imageSource;

    [ObservableProperty]
    private int _lineThickness = 2;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _zIndex;

    public LabelDesignElement()
    {
        Id = _nextId++;
    }

    public LabelDesignElement(LabelElementType type) : this()
    {
        ElementType = type;
        SetDefaultsForType();
    }

    private void SetDefaultsForType()
    {
        switch (ElementType)
        {
            case LabelElementType.Text:
                Width = 150;
                Height = 30;
                Content = "Text";
                FontSize = 24;
                break;

            case LabelElementType.Barcode:
                Width = 180;
                Height = 70;
                Content = "{{Barcode}}";
                BarcodeType = BarcodeType.EAN13;
                BarcodeHeight = 50;
                break;

            case LabelElementType.Price:
                Width = 100;
                Height = 35;
                Content = "{{Price}}";
                FontSize = 30;
                IsBold = true;
                break;

            case LabelElementType.QRCode:
                Width = 80;
                Height = 80;
                Content = "{{Barcode}}";
                QrSize = 4;
                break;

            case LabelElementType.Image:
                Width = 60;
                Height = 60;
                break;

            case LabelElementType.Box:
                Width = 100;
                Height = 50;
                LineThickness = 2;
                break;

            case LabelElementType.Line:
                Width = 100;
                Height = 2;
                LineThickness = 2;
                break;
        }
    }

    /// <summary>
    /// Gets the display name for this element type.
    /// </summary>
    public string DisplayName => ElementType switch
    {
        LabelElementType.Text => "Text",
        LabelElementType.Barcode => "Barcode",
        LabelElementType.Price => "Price",
        LabelElementType.QRCode => "QR Code",
        LabelElementType.Image => "Image",
        LabelElementType.Box => "Box",
        LabelElementType.Line => "Line",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the icon character for this element type.
    /// </summary>
    public string IconChar => ElementType switch
    {
        LabelElementType.Text => "\uE8D2",    // TextFont
        LabelElementType.Barcode => "\uE9B9", // Barcode
        LabelElementType.Price => "\uE7BF",   // Money
        LabelElementType.QRCode => "\uE707",  // QRCode
        LabelElementType.Image => "\uEB9F",   // Photo
        LabelElementType.Box => "\uE739",     // Checkbox
        LabelElementType.Line => "\uE738",    // Line
        _ => "\uE946"                         // Unknown
    };

    /// <summary>
    /// Gets the bounds of this element.
    /// </summary>
    public Rect Bounds => new(X, Y, Width, Height);

    /// <summary>
    /// Creates a deep copy of this element.
    /// </summary>
    public LabelDesignElement Clone()
    {
        return new LabelDesignElement
        {
            ElementType = ElementType,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Content = Content,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            TextAlignment = TextAlignment,
            Rotation = Rotation,
            BarcodeType = BarcodeType,
            BarcodeHeight = BarcodeHeight,
            ShowBarcodeText = ShowBarcodeText,
            QrSize = QrSize,
            QrErrorCorrection = QrErrorCorrection,
            ImageSource = ImageSource,
            LineThickness = LineThickness,
            ZIndex = ZIndex
        };
    }
}

/// <summary>
/// Represents a toolbox item for dragging onto the canvas.
/// </summary>
public class ToolboxItem
{
    public LabelElementType ElementType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IconChar { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public static List<ToolboxItem> GetAllItems() => new()
    {
        new() { ElementType = LabelElementType.Text, Name = "Text", IconChar = "\uE8D2", Description = "Add text label" },
        new() { ElementType = LabelElementType.Barcode, Name = "Barcode", IconChar = "\uE9B9", Description = "Add barcode" },
        new() { ElementType = LabelElementType.Price, Name = "Price", IconChar = "\uE7BF", Description = "Add price field" },
        new() { ElementType = LabelElementType.QRCode, Name = "QR Code", IconChar = "\uE707", Description = "Add QR code" },
        new() { ElementType = LabelElementType.Image, Name = "Image", IconChar = "\uEB9F", Description = "Add image" },
        new() { ElementType = LabelElementType.Box, Name = "Box", IconChar = "\uE739", Description = "Add rectangle" },
        new() { ElementType = LabelElementType.Line, Name = "Line", IconChar = "\uE738", Description = "Add line" }
    };
}
