using HospitalityPOS.Core.DTOs;
using HospitalityPOS.WPF.Models;
using System.Text;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service to generate ZPL/EPL code from visual label design elements.
/// </summary>
public class LabelCodeGeneratorService
{
    /// <summary>
    /// Generates ZPL code from design elements.
    /// </summary>
    public string GenerateZPL(IEnumerable<LabelDesignElement> elements, LabelSizeDto labelSize)
    {
        var sb = new StringBuilder();

        // Start ZPL command
        sb.AppendLine("^XA");

        // Set label size (in dots)
        var widthDots = (int)(labelSize.WidthMm * labelSize.Dpi / 25.4m);
        var heightDots = (int)(labelSize.HeightMm * labelSize.Dpi / 25.4m);
        sb.AppendLine($"^LL{heightDots}");
        sb.AppendLine($"^PW{widthDots}");

        // Sort elements by Z-index
        foreach (var element in elements.OrderBy(e => e.ZIndex))
        {
            var elementCode = GenerateZPLElement(element);
            if (!string.IsNullOrEmpty(elementCode))
            {
                sb.AppendLine(elementCode);
            }
        }

        // End ZPL command
        sb.AppendLine("^XZ");

        return sb.ToString();
    }

    /// <summary>
    /// Generates EPL code from design elements.
    /// </summary>
    public string GenerateEPL(IEnumerable<LabelDesignElement> elements, LabelSizeDto labelSize)
    {
        var sb = new StringBuilder();

        // Clear buffer
        sb.AppendLine("N");

        // Set label size
        var widthDots = (int)(labelSize.WidthMm * labelSize.Dpi / 25.4m);
        var heightDots = (int)(labelSize.HeightMm * labelSize.Dpi / 25.4m);
        sb.AppendLine($"q{widthDots}");

        // Sort elements by Z-index
        foreach (var element in elements.OrderBy(e => e.ZIndex))
        {
            var elementCode = GenerateEPLElement(element);
            if (!string.IsNullOrEmpty(elementCode))
            {
                sb.AppendLine(elementCode);
            }
        }

        // Print 1 label
        sb.AppendLine("P1");

        return sb.ToString();
    }

    /// <summary>
    /// Generates TSPL code from design elements.
    /// </summary>
    public string GenerateTSPL(IEnumerable<LabelDesignElement> elements, LabelSizeDto labelSize)
    {
        var sb = new StringBuilder();

        // Set label size in mm
        sb.AppendLine($"SIZE {labelSize.WidthMm} mm, {labelSize.HeightMm} mm");
        sb.AppendLine("CLS");

        // Sort elements by Z-index
        foreach (var element in elements.OrderBy(e => e.ZIndex))
        {
            var elementCode = GenerateTSPLElement(element);
            if (!string.IsNullOrEmpty(elementCode))
            {
                sb.AppendLine(elementCode);
            }
        }

        // Print 1 label
        sb.AppendLine("PRINT 1");

        return sb.ToString();
    }

    /// <summary>
    /// Generates code in the specified language.
    /// </summary>
    public string GenerateCode(IEnumerable<LabelDesignElement> elements, LabelSizeDto labelSize, LabelPrintLanguageDto language)
    {
        return language switch
        {
            LabelPrintLanguageDto.ZPL => GenerateZPL(elements, labelSize),
            LabelPrintLanguageDto.EPL => GenerateEPL(elements, labelSize),
            LabelPrintLanguageDto.TSPL => GenerateTSPL(elements, labelSize),
            _ => GenerateZPL(elements, labelSize)
        };
    }

    #region ZPL Element Generation

    private string GenerateZPLElement(LabelDesignElement element)
    {
        var x = (int)element.X;
        var y = (int)element.Y;
        var rotation = GetZPLRotation(element.Rotation);

        return element.ElementType switch
        {
            LabelElementType.Text => GenerateZPLText(element, x, y, rotation),
            LabelElementType.Barcode => GenerateZPLBarcode(element, x, y, rotation),
            LabelElementType.Price => GenerateZPLPrice(element, x, y, rotation),
            LabelElementType.QRCode => GenerateZPLQRCode(element, x, y),
            LabelElementType.Box => GenerateZPLBox(element, x, y),
            LabelElementType.Line => GenerateZPLLine(element, x, y),
            LabelElementType.Image => string.Empty, // Images require special handling
            _ => string.Empty
        };
    }

    private string GenerateZPLText(LabelDesignElement element, int x, int y, string rotation)
    {
        var font = element.FontFamily;
        var size = element.FontSize;
        var content = element.Content;

        // ^FO{x},{y} - Field Origin
        // ^A{font}{rotation},{height},{width} - Scalable font
        // ^FD{data}^FS - Field Data
        return $"^FO{x},{y}^A{font}{rotation},{size},{size}^FD{content}^FS";
    }

    private string GenerateZPLBarcode(LabelDesignElement element, int x, int y, string rotation)
    {
        var height = element.BarcodeHeight;
        var showText = element.ShowBarcodeText ? "Y" : "N";
        var content = element.Content;

        var barcodeCommand = element.BarcodeType switch
        {
            BarcodeType.EAN13 => $"^BY2,2,{height}^BE{rotation},{height},{showText}",
            BarcodeType.EAN8 => $"^BY2,2,{height}^B8{rotation},{height},{showText}",
            BarcodeType.Code128 => $"^BY2,2,{height}^BC{rotation},{height},{showText},N,N",
            BarcodeType.Code39 => $"^BY2,2,{height}^B3{rotation},N,{height},{showText},N",
            BarcodeType.UPCA => $"^BY2,2,{height}^BU{rotation},{height},{showText},{showText}",
            BarcodeType.UPCE => $"^BY2,2,{height}^B9{rotation},{height},{showText},{showText}",
            BarcodeType.ITF => $"^BY2,2,{height}^B2{rotation},{height},{showText},N,N",
            _ => $"^BY2,2,{height}^BC{rotation},{height},{showText},N,N"
        };

        return $"^FO{x},{y}{barcodeCommand}^FD{content}^FS";
    }

    private string GenerateZPLPrice(LabelDesignElement element, int x, int y, string rotation)
    {
        var size = element.FontSize;
        var content = element.Content;

        // Use larger font for price
        return $"^FO{x},{y}^A0{rotation},{size},{size}^FD{content}^FS";
    }

    private string GenerateZPLQRCode(LabelDesignElement element, int x, int y)
    {
        var size = element.QrSize;
        var errorCorrection = element.QrErrorCorrection;
        var content = element.Content;

        // ^BQN,{model},{magnification},{error_correction}
        return $"^FO{x},{y}^BQN,2,{size},{errorCorrection}^FD{content}^FS";
    }

    private string GenerateZPLBox(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // ^GB{width},{height},{thickness}
        return $"^FO{x},{y}^GB{width},{height},{thickness}^FS";
    }

    private string GenerateZPLLine(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // For horizontal line, height = thickness; for vertical, width = thickness
        if (width > height)
        {
            return $"^FO{x},{y}^GB{width},{thickness},{thickness}^FS";
        }
        else
        {
            return $"^FO{x},{y}^GB{thickness},{height},{thickness}^FS";
        }
    }

    private string GetZPLRotation(ElementRotation rotation) => rotation switch
    {
        ElementRotation.Rotate0 => "N",
        ElementRotation.Rotate90 => "R",
        ElementRotation.Rotate180 => "I",
        ElementRotation.Rotate270 => "B",
        _ => "N"
    };

    #endregion

    #region EPL Element Generation

    private string GenerateEPLElement(LabelDesignElement element)
    {
        var x = (int)element.X;
        var y = (int)element.Y;
        var rotation = GetEPLRotation(element.Rotation);

        return element.ElementType switch
        {
            LabelElementType.Text => GenerateEPLText(element, x, y, rotation),
            LabelElementType.Barcode => GenerateEPLBarcode(element, x, y, rotation),
            LabelElementType.Price => GenerateEPLPrice(element, x, y, rotation),
            LabelElementType.QRCode => string.Empty, // EPL QR support limited
            LabelElementType.Box => GenerateEPLBox(element, x, y),
            LabelElementType.Line => GenerateEPLLine(element, x, y),
            LabelElementType.Image => string.Empty,
            _ => string.Empty
        };
    }

    private string GenerateEPLText(LabelDesignElement element, int x, int y, int rotation)
    {
        var font = GetEPLFont(element.FontSize);
        var content = element.Content;

        // A{x},{y},{rotation},{font},{h_mult},{v_mult},{reverse},"{data}"
        return $"A{x},{y},{rotation},{font},1,1,N,\"{content}\"";
    }

    private string GenerateEPLBarcode(LabelDesignElement element, int x, int y, int rotation)
    {
        var height = element.BarcodeHeight;
        var showText = element.ShowBarcodeText ? "B" : "N";
        var content = element.Content;

        var barcodeType = element.BarcodeType switch
        {
            BarcodeType.EAN13 => "E30",
            BarcodeType.EAN8 => "E80",
            BarcodeType.Code128 => "1",
            BarcodeType.Code39 => "3",
            BarcodeType.UPCA => "UA0",
            BarcodeType.UPCE => "UE0",
            BarcodeType.ITF => "2",
            _ => "1"
        };

        // B{x},{y},{rotation},{barcode_type},{narrow},{wide},{height},{print_text},"{data}"
        return $"B{x},{y},{rotation},{barcodeType},2,2,{height},{showText},\"{content}\"";
    }

    private string GenerateEPLPrice(LabelDesignElement element, int x, int y, int rotation)
    {
        var font = GetEPLFont(element.FontSize);
        var content = element.Content;

        return $"A{x},{y},{rotation},{font},2,2,N,\"{content}\"";
    }

    private string GenerateEPLBox(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // X{x},{y},{thickness},{x2},{y2}
        var x2 = x + width;
        var y2 = y + height;
        return $"X{x},{y},{thickness},{x2},{y2}";
    }

    private string GenerateEPLLine(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // LO{x},{y},{width},{height}
        if (width > height)
        {
            return $"LO{x},{y},{width},{thickness}";
        }
        else
        {
            return $"LO{x},{y},{thickness},{height}";
        }
    }

    private int GetEPLRotation(ElementRotation rotation) => rotation switch
    {
        ElementRotation.Rotate0 => 0,
        ElementRotation.Rotate90 => 1,
        ElementRotation.Rotate180 => 2,
        ElementRotation.Rotate270 => 3,
        _ => 0
    };

    private int GetEPLFont(int fontSize)
    {
        // Map font size to EPL font number (1-5)
        return fontSize switch
        {
            <= 12 => 1,
            <= 18 => 2,
            <= 24 => 3,
            <= 32 => 4,
            _ => 5
        };
    }

    #endregion

    #region TSPL Element Generation

    private string GenerateTSPLElement(LabelDesignElement element)
    {
        var x = (int)element.X;
        var y = (int)element.Y;
        var rotation = GetTSPLRotation(element.Rotation);

        return element.ElementType switch
        {
            LabelElementType.Text => GenerateTSPLText(element, x, y, rotation),
            LabelElementType.Barcode => GenerateTSPLBarcode(element, x, y, rotation),
            LabelElementType.Price => GenerateTSPLPrice(element, x, y, rotation),
            LabelElementType.QRCode => GenerateTSPLQRCode(element, x, y, rotation),
            LabelElementType.Box => GenerateTSPLBox(element, x, y),
            LabelElementType.Line => GenerateTSPLLine(element, x, y),
            LabelElementType.Image => string.Empty,
            _ => string.Empty
        };
    }

    private string GenerateTSPLText(LabelDesignElement element, int x, int y, int rotation)
    {
        var font = GetTSPLFont(element.FontSize);
        var content = element.Content;

        // TEXT {x},{y},"{font}",{rotation},{x_mult},{y_mult},"{content}"
        return $"TEXT {x},{y},\"{font}\",{rotation},1,1,\"{content}\"";
    }

    private string GenerateTSPLBarcode(LabelDesignElement element, int x, int y, int rotation)
    {
        var height = element.BarcodeHeight;
        var content = element.Content;

        var barcodeType = element.BarcodeType switch
        {
            BarcodeType.EAN13 => "EAN13",
            BarcodeType.EAN8 => "EAN8",
            BarcodeType.Code128 => "128",
            BarcodeType.Code39 => "39",
            BarcodeType.UPCA => "UPCA",
            BarcodeType.UPCE => "UPCE",
            BarcodeType.ITF => "ITF14",
            _ => "128"
        };

        // BARCODE {x},{y},"{type}",{height},{human_readable},{rotation},{narrow},{wide},"{content}"
        var humanReadable = element.ShowBarcodeText ? 1 : 0;
        return $"BARCODE {x},{y},\"{barcodeType}\",{height},{humanReadable},{rotation},2,2,\"{content}\"";
    }

    private string GenerateTSPLPrice(LabelDesignElement element, int x, int y, int rotation)
    {
        var font = GetTSPLFont(element.FontSize);
        var content = element.Content;

        return $"TEXT {x},{y},\"{font}\",{rotation},2,2,\"{content}\"";
    }

    private string GenerateTSPLQRCode(LabelDesignElement element, int x, int y, int rotation)
    {
        var size = element.QrSize;
        var errorCorrection = element.QrErrorCorrection;
        var content = element.Content;

        // QRCODE {x},{y},{ECC_level},{cell_width},{mode},{rotation},"{content}"
        return $"QRCODE {x},{y},{errorCorrection},{size},A,{rotation},\"{content}\"";
    }

    private string GenerateTSPLBox(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // BOX {x},{y},{x2},{y2},{thickness}
        var x2 = x + width;
        var y2 = y + height;
        return $"BOX {x},{y},{x2},{y2},{thickness}";
    }

    private string GenerateTSPLLine(LabelDesignElement element, int x, int y)
    {
        var width = (int)element.Width;
        var height = (int)element.Height;
        var thickness = element.LineThickness;

        // BAR {x},{y},{width},{height}
        if (width > height)
        {
            return $"BAR {x},{y},{width},{thickness}";
        }
        else
        {
            return $"BAR {x},{y},{thickness},{height}";
        }
    }

    private int GetTSPLRotation(ElementRotation rotation) => rotation switch
    {
        ElementRotation.Rotate0 => 0,
        ElementRotation.Rotate90 => 90,
        ElementRotation.Rotate180 => 180,
        ElementRotation.Rotate270 => 270,
        _ => 0
    };

    private string GetTSPLFont(int fontSize)
    {
        // Map font size to TSPL font number
        return fontSize switch
        {
            <= 12 => "1",
            <= 18 => "2",
            <= 24 => "3",
            <= 32 => "4",
            _ => "5"
        };
    }

    #endregion

    #region Element Parsing (from code to elements)

    /// <summary>
    /// Parses ZPL code and extracts design elements.
    /// </summary>
    public List<LabelDesignElement> ParseZPL(string zplCode)
    {
        var elements = new List<LabelDesignElement>();
        var lines = zplCode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int currentX = 0, currentY = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Parse field origin
            if (trimmed.Contains("^FO"))
            {
                var foMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^FO(\d+),(\d+)");
                if (foMatch.Success)
                {
                    currentX = int.Parse(foMatch.Groups[1].Value);
                    currentY = int.Parse(foMatch.Groups[2].Value);
                }
            }

            // Parse text field
            if (trimmed.Contains("^A") && trimmed.Contains("^FD"))
            {
                var fdMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^FD(.+?)\^FS");
                var fontMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^A(\w)(\w),(\d+),(\d+)");

                if (fdMatch.Success)
                {
                    var element = new LabelDesignElement(LabelElementType.Text)
                    {
                        X = currentX,
                        Y = currentY,
                        Content = fdMatch.Groups[1].Value
                    };

                    if (fontMatch.Success)
                    {
                        element.FontFamily = fontMatch.Groups[1].Value;
                        element.FontSize = int.Parse(fontMatch.Groups[3].Value);
                    }

                    elements.Add(element);
                }
            }

            // Parse barcode
            if ((trimmed.Contains("^BC") || trimmed.Contains("^BE") || trimmed.Contains("^B8")) && trimmed.Contains("^FD"))
            {
                var fdMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^FD(.+?)\^FS");
                if (fdMatch.Success)
                {
                    var element = new LabelDesignElement(LabelElementType.Barcode)
                    {
                        X = currentX,
                        Y = currentY,
                        Content = fdMatch.Groups[1].Value
                    };

                    // Detect barcode type
                    if (trimmed.Contains("^BE")) element.BarcodeType = BarcodeType.EAN13;
                    else if (trimmed.Contains("^B8")) element.BarcodeType = BarcodeType.EAN8;
                    else if (trimmed.Contains("^BC")) element.BarcodeType = BarcodeType.Code128;

                    elements.Add(element);
                }
            }

            // Parse QR code
            if (trimmed.Contains("^BQ") && trimmed.Contains("^FD"))
            {
                var fdMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^FD(.+?)\^FS");
                if (fdMatch.Success)
                {
                    var element = new LabelDesignElement(LabelElementType.QRCode)
                    {
                        X = currentX,
                        Y = currentY,
                        Content = fdMatch.Groups[1].Value
                    };
                    elements.Add(element);
                }
            }

            // Parse graphic box
            if (trimmed.Contains("^GB"))
            {
                var gbMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\^GB(\d+),(\d+),(\d+)");
                if (gbMatch.Success)
                {
                    var width = int.Parse(gbMatch.Groups[1].Value);
                    var height = int.Parse(gbMatch.Groups[2].Value);
                    var thickness = int.Parse(gbMatch.Groups[3].Value);

                    var elementType = (width > height && height <= thickness * 2) ||
                                     (height > width && width <= thickness * 2)
                        ? LabelElementType.Line
                        : LabelElementType.Box;

                    var element = new LabelDesignElement(elementType)
                    {
                        X = currentX,
                        Y = currentY,
                        Width = width,
                        Height = height,
                        LineThickness = thickness
                    };
                    elements.Add(element);
                }
            }
        }

        return elements;
    }

    #endregion
}
