using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SkiaSharp;
using System.Text;
using System.Text.RegularExpressions;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for rendering visual label previews using SkiaSharp.
/// Supports ZPL, EPL, and TSPL rendering with fallback to Labelary API.
/// </summary>
public class LabelPreviewService : ILabelPreviewService
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string LabelaryApiBase = "http://api.labelary.com/v1/printers";
    private const decimal MmToInch = 25.4m;

    public LabelPreviewService(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<byte[]> RenderPreviewAsync(int templateId, ProductLabelDataDto data)
    {
        using var scope = _scopeFactory.CreateScope();
        var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();
        var printerService = scope.ServiceProvider.GetService<ILabelPrinterService>();

        var template = await templateService.GetTemplateAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        // Get label size
        LabelSizeDto? labelSize = null;
        if (printerService != null)
        {
            var sizes = await printerService.GetAllLabelSizesAsync();
            labelSize = sizes.FirstOrDefault(s => s.Id == template.LabelSizeId);
        }

        var widthMm = labelSize?.WidthMm ?? 38m;
        var heightMm = labelSize?.HeightMm ?? 25m;
        var dpi = labelSize?.Dpi ?? 203;

        return await RenderPreviewAsync(
            template.TemplateContent ?? "",
            template.PrintLanguage,
            widthMm,
            heightMm,
            dpi,
            data);
    }

    public async Task<byte[]> RenderPreviewAsync(
        string content,
        LabelPrintLanguageDto language,
        decimal widthMm,
        decimal heightMm,
        int dpi,
        ProductLabelDataDto? data = null)
    {
        // Replace placeholders with actual data
        var processedContent = SubstitutePlaceholders(content, data);

        // Calculate dimensions in pixels (using 96 DPI for screen display)
        var widthPx = (int)(widthMm * 96 / MmToInch);
        var heightPx = (int)(heightMm * 96 / MmToInch);

        // Calculate scale factor from printer dots to screen pixels
        var scaleFactor = 96.0 / dpi;

        // Try Labelary API for ZPL first (better quality)
        if (language == LabelPrintLanguageDto.ZPL)
        {
            try
            {
                var widthDots = (int)(widthMm * dpi / MmToInch);
                var heightDots = (int)(heightMm * dpi / MmToInch);
                var dpmm = dpi / 25.4;

                var labelaryResult = await RenderZplViaLabelaryAsync(processedContent, widthDots, heightDots, (int)dpmm);
                if (labelaryResult != null)
                {
                    return labelaryResult;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Labelary API unavailable, falling back to local rendering");
            }
        }

        // Fall back to local rendering
        return RenderLocally(processedContent, language, widthPx, heightPx, scaleFactor, dpi);
    }

    public async Task<byte[]?> RenderZplViaLabelaryAsync(string zpl, int widthDots, int heightDots, int dpmm = 8)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LabelaryApi");
            client.Timeout = TimeSpan.FromSeconds(10);

            // Convert dots to inches
            var widthInches = widthDots / (dpmm * 25.4);
            var heightInches = heightDots / (dpmm * 25.4);

            var url = $"{LabelaryApiBase}/{dpmm}dpmm/labels/{widthInches:F2}x{heightInches:F2}/0/";

            var response = await client.PostAsync(url, new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            _logger.Warning("Labelary API returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to render ZPL via Labelary API");
            return null;
        }
    }

    public async Task<bool> IsLabelaryAvailableAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LabelaryApi");
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"{LabelaryApiBase}/8dpmm/labels/1x1/0/");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Local Rendering

    private byte[] RenderLocally(string content, LabelPrintLanguageDto language, int widthPx, int heightPx, double scaleFactor, int dpi)
    {
        using var surface = SKSurface.Create(new SKImageInfo(widthPx, heightPx));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Parse and render based on language
        switch (language)
        {
            case LabelPrintLanguageDto.ZPL:
                RenderZPL(canvas, content, scaleFactor, dpi);
                break;
            case LabelPrintLanguageDto.EPL:
                RenderEPL(canvas, content, scaleFactor, dpi);
                break;
            case LabelPrintLanguageDto.TSPL:
                RenderTSPL(canvas, content, scaleFactor, dpi);
                break;
            default:
                // Just render content as text
                using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 12 })
                {
                    canvas.DrawText(content, 10, 20, paint);
                }
                break;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void RenderZPL(SKCanvas canvas, string content, double scaleFactor, int dpi)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        float currentX = 0, currentY = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Parse field origin ^FO{x},{y}
            var foMatch = Regex.Match(trimmed, @"\^FO(\d+),(\d+)");
            if (foMatch.Success)
            {
                currentX = float.Parse(foMatch.Groups[1].Value) * (float)scaleFactor;
                currentY = float.Parse(foMatch.Groups[2].Value) * (float)scaleFactor;
            }

            // Parse text field ^A{font}{rotation},{height},{width}^FD{data}^FS
            var textMatch = Regex.Match(trimmed, @"\^A(\w)(\w),(\d+),(\d+)\^FD(.+?)\^FS");
            if (textMatch.Success)
            {
                var fontSize = float.Parse(textMatch.Groups[3].Value) * (float)scaleFactor;
                var text = textMatch.Groups[5].Value;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                };

                canvas.DrawText(text, currentX, currentY + fontSize, paint);
            }

            // Parse barcode ^BC or ^BE
            if ((trimmed.Contains("^BC") || trimmed.Contains("^BE") || trimmed.Contains("^B8")) &&
                trimmed.Contains("^FD"))
            {
                var fdMatch = Regex.Match(trimmed, @"\^FD(.+?)\^FS");
                var heightMatch = Regex.Match(trimmed, @",(\d+),");

                if (fdMatch.Success)
                {
                    var barcodeContent = fdMatch.Groups[1].Value;
                    var barcodeHeight = heightMatch.Success ? float.Parse(heightMatch.Groups[1].Value) * (float)scaleFactor : 50f;

                    RenderBarcode(canvas, currentX, currentY, barcodeContent, barcodeHeight, BarcodeFormat.CODE_128);
                }
            }

            // Parse QR code ^BQ
            if (trimmed.Contains("^BQ") && trimmed.Contains("^FD"))
            {
                var fdMatch = Regex.Match(trimmed, @"\^FD(.+?)\^FS");
                if (fdMatch.Success)
                {
                    var qrContent = fdMatch.Groups[1].Value;
                    RenderQRCode(canvas, currentX, currentY, qrContent, 80 * (float)scaleFactor);
                }
            }

            // Parse graphic box ^GB
            var gbMatch = Regex.Match(trimmed, @"\^GB(\d+),(\d+),(\d+)");
            if (gbMatch.Success)
            {
                var width = float.Parse(gbMatch.Groups[1].Value) * (float)scaleFactor;
                var height = float.Parse(gbMatch.Groups[2].Value) * (float)scaleFactor;
                var thickness = float.Parse(gbMatch.Groups[3].Value) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = thickness
                };

                canvas.DrawRect(currentX, currentY, width, height, paint);
            }
        }
    }

    private void RenderEPL(SKCanvas canvas, string content, double scaleFactor, int dpi)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Parse text A{x},{y},{rotation},{font},{h_mult},{v_mult},{reverse},"{data}"
            var textMatch = Regex.Match(trimmed, @"^A(\d+),(\d+),(\d+),(\d+),(\d+),(\d+),\w,""(.+?)""");
            if (textMatch.Success)
            {
                var x = float.Parse(textMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(textMatch.Groups[2].Value) * (float)scaleFactor;
                var font = int.Parse(textMatch.Groups[4].Value);
                var text = textMatch.Groups[7].Value;

                var fontSize = GetEPLFontSize(font) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                };

                canvas.DrawText(text, x, y + fontSize, paint);
            }

            // Parse barcode B{x},{y},{rotation},{type},...,"{data}"
            var barcodeMatch = Regex.Match(trimmed, @"^B(\d+),(\d+),(\d+),(\w+),.*?,(\d+),\w,""(.+?)""");
            if (barcodeMatch.Success)
            {
                var x = float.Parse(barcodeMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(barcodeMatch.Groups[2].Value) * (float)scaleFactor;
                var height = float.Parse(barcodeMatch.Groups[5].Value) * (float)scaleFactor;
                var barcodeContent = barcodeMatch.Groups[6].Value;

                RenderBarcode(canvas, x, y, barcodeContent, height, BarcodeFormat.CODE_128);
            }

            // Parse line LO{x},{y},{width},{height}
            var lineMatch = Regex.Match(trimmed, @"^LO(\d+),(\d+),(\d+),(\d+)");
            if (lineMatch.Success)
            {
                var x = float.Parse(lineMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(lineMatch.Groups[2].Value) * (float)scaleFactor;
                var width = float.Parse(lineMatch.Groups[3].Value) * (float)scaleFactor;
                var height = float.Parse(lineMatch.Groups[4].Value) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Fill
                };

                canvas.DrawRect(x, y, width, height, paint);
            }
        }
    }

    private void RenderTSPL(SKCanvas canvas, string content, double scaleFactor, int dpi)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Parse TEXT {x},{y},"{font}",{rotation},{x_mult},{y_mult},"{content}"
            var textMatch = Regex.Match(trimmed, @"^TEXT\s+(\d+),(\d+),""(\d+)"",(\d+),(\d+),(\d+),""(.+?)""");
            if (textMatch.Success)
            {
                var x = float.Parse(textMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(textMatch.Groups[2].Value) * (float)scaleFactor;
                var font = textMatch.Groups[3].Value;
                var text = textMatch.Groups[7].Value;

                var fontSize = GetTSPLFontSize(font) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                };

                canvas.DrawText(text, x, y + fontSize, paint);
            }

            // Parse BARCODE {x},{y},"{type}",{height},{human_readable},{rotation},"{content}"
            var barcodeMatch = Regex.Match(trimmed, @"^BARCODE\s+(\d+),(\d+),""(\w+)"",(\d+),(\d+),(\d+).*?,""(.+?)""");
            if (barcodeMatch.Success)
            {
                var x = float.Parse(barcodeMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(barcodeMatch.Groups[2].Value) * (float)scaleFactor;
                var height = float.Parse(barcodeMatch.Groups[4].Value) * (float)scaleFactor;
                var barcodeContent = barcodeMatch.Groups[7].Value;

                RenderBarcode(canvas, x, y, barcodeContent, height, BarcodeFormat.CODE_128);
            }

            // Parse QRCODE {x},{y},{level},{cell_width},{mode},{rotation},"{content}"
            var qrMatch = Regex.Match(trimmed, @"^QRCODE\s+(\d+),(\d+),\w,(\d+),\w,(\d+),""(.+?)""");
            if (qrMatch.Success)
            {
                var x = float.Parse(qrMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(qrMatch.Groups[2].Value) * (float)scaleFactor;
                var size = float.Parse(qrMatch.Groups[3].Value) * 10 * (float)scaleFactor;
                var qrContent = qrMatch.Groups[5].Value;

                RenderQRCode(canvas, x, y, qrContent, size);
            }

            // Parse BOX {x},{y},{x2},{y2},{thickness}
            var boxMatch = Regex.Match(trimmed, @"^BOX\s+(\d+),(\d+),(\d+),(\d+),(\d+)");
            if (boxMatch.Success)
            {
                var x1 = float.Parse(boxMatch.Groups[1].Value) * (float)scaleFactor;
                var y1 = float.Parse(boxMatch.Groups[2].Value) * (float)scaleFactor;
                var x2 = float.Parse(boxMatch.Groups[3].Value) * (float)scaleFactor;
                var y2 = float.Parse(boxMatch.Groups[4].Value) * (float)scaleFactor;
                var thickness = float.Parse(boxMatch.Groups[5].Value) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = thickness
                };

                canvas.DrawRect(x1, y1, x2 - x1, y2 - y1, paint);
            }

            // Parse BAR {x},{y},{width},{height}
            var barMatch = Regex.Match(trimmed, @"^BAR\s+(\d+),(\d+),(\d+),(\d+)");
            if (barMatch.Success)
            {
                var x = float.Parse(barMatch.Groups[1].Value) * (float)scaleFactor;
                var y = float.Parse(barMatch.Groups[2].Value) * (float)scaleFactor;
                var width = float.Parse(barMatch.Groups[3].Value) * (float)scaleFactor;
                var height = float.Parse(barMatch.Groups[4].Value) * (float)scaleFactor;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Fill
                };

                canvas.DrawRect(x, y, width, height, paint);
            }
        }
    }

    private float GetEPLFontSize(int font)
    {
        return font switch
        {
            1 => 8,
            2 => 10,
            3 => 12,
            4 => 14,
            5 => 16,
            _ => 12
        };
    }

    private float GetTSPLFontSize(string font)
    {
        return font switch
        {
            "1" => 8,
            "2" => 12,
            "3" => 16,
            "4" => 24,
            "5" => 32,
            _ => 12
        };
    }

    private void RenderBarcode(SKCanvas canvas, float x, float y, string content, float height, BarcodeFormat format)
    {
        try
        {
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Height = (int)height,
                    Width = (int)(height * 2.5),
                    Margin = 0,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content);

            // Convert to SKBitmap
            var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888);
            using var bitmap = new SKBitmap(info);

            var pixels = pixelData.Pixels;
            for (int py = 0; py < pixelData.Height; py++)
            {
                for (int px = 0; px < pixelData.Width; px++)
                {
                    var idx = (py * pixelData.Width + px) * 4;
                    var color = new SKColor(pixels[idx + 2], pixels[idx + 1], pixels[idx], pixels[idx + 3]);
                    bitmap.SetPixel(px, py, color);
                }
            }

            canvas.DrawBitmap(bitmap, x, y);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to render barcode: {Content}", content);

            // Draw placeholder rectangle
            using var paint = new SKPaint
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRect(x, y, height * 2.5f, height, paint);

            using var textPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 8,
                IsAntialias = true
            };
            canvas.DrawText(content, x + 5, y + height / 2, textPaint);
        }
    }

    private void RenderQRCode(SKCanvas canvas, float x, float y, string content, float size)
    {
        try
        {
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = (int)size,
                    Width = (int)size,
                    Margin = 0
                }
            };

            var pixelData = writer.Write(content);

            // Convert to SKBitmap
            var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888);
            using var bitmap = new SKBitmap(info);

            var pixels = pixelData.Pixels;
            for (int py = 0; py < pixelData.Height; py++)
            {
                for (int px = 0; px < pixelData.Width; px++)
                {
                    var idx = (py * pixelData.Width + px) * 4;
                    var color = new SKColor(pixels[idx + 2], pixels[idx + 1], pixels[idx], pixels[idx + 3]);
                    bitmap.SetPixel(px, py, color);
                }
            }

            canvas.DrawBitmap(bitmap, x, y);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to render QR code: {Content}", content);

            // Draw placeholder square
            using var paint = new SKPaint
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRect(x, y, size, size, paint);
        }
    }

    #endregion

    #region Placeholder Substitution

    private string SubstitutePlaceholders(string content, ProductLabelDataDto? data)
    {
        if (data == null || string.IsNullOrEmpty(content))
        {
            return content;
        }

        var result = content;

        result = result.Replace("{{ProductName}}", data.ProductName ?? "Sample Product");
        result = result.Replace("{{Barcode}}", data.Barcode ?? "5901234123457");
        result = result.Replace("{{Price}}", data.Price?.ToString("F2") ?? "199.99");
        result = result.Replace("{{UnitPrice}}", data.UnitPrice ?? "KSh 0.40/ml");
        result = result.Replace("{{SKU}}", data.SKU ?? "SKU-001");
        result = result.Replace("{{CategoryName}}", data.CategoryName ?? "General");
        result = result.Replace("{{UnitOfMeasure}}", data.UnitOfMeasure ?? "pcs");
        result = result.Replace("{{Description}}", data.Description ?? "");
        result = result.Replace("{{Weight}}", data.Weight?.ToString("F2") ?? "0.00");
        result = result.Replace("{{Volume}}", data.Volume?.ToString("F2") ?? "0.00");
        result = result.Replace("{{ExpiryDate}}", data.ExpiryDate?.ToString("dd/MM/yyyy") ?? "");
        result = result.Replace("{{ProductionDate}}", data.ProductionDate?.ToString("dd/MM/yyyy") ?? "");
        result = result.Replace("{{BatchNumber}}", data.BatchNumber ?? "");
        result = result.Replace("{{StoreName}}", data.StoreName ?? "My Store");
        result = result.Replace("{{PrintDate}}", DateTime.Now.ToString("dd/MM/yyyy"));

        return result;
    }

    #endregion
}
