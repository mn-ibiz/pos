using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Printing;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Printing;

/// <summary>
/// Converts images to ESC/POS raster format for thermal printing.
/// </summary>
public class ImageConverter : IImageConverter
{
    private readonly ILogger<ImageConverter> _logger;

    public ImageConverter(ILogger<ImageConverter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImageConversionResult> ConvertFromFileAsync(string imagePath, ImageConversionOptions? options = null)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return ImageConversionResult.Failed($"Image file not found: {imagePath}");
            }

            var imageData = await File.ReadAllBytesAsync(imagePath);
            return await ConvertFromBytesAsync(imageData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image from file: {Path}", imagePath);
            return ImageConversionResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ImageConversionResult> ConvertFromBytesAsync(byte[] imageData, ImageConversionOptions? options = null)
    {
        return await Task.Run(() => ConvertFromBytes(imageData, options ?? new ImageConversionOptions()));
    }

    /// <inheritdoc />
    public async Task<ImageConversionResult> ConvertFromBase64Async(string base64Image, ImageConversionOptions? options = null)
    {
        try
        {
            // Remove data URI prefix if present
            var base64 = base64Image;
            if (base64.Contains(','))
            {
                base64 = base64.Substring(base64.IndexOf(',') + 1);
            }

            var imageData = Convert.FromBase64String(base64);
            return await ConvertFromBytesAsync(imageData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image from Base64");
            return ImageConversionResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public byte[] GetPrintCommand(ImageConversionResult conversionResult, int mode = 0)
    {
        if (!conversionResult.Success || conversionResult.ImageData.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var header = EscPosCommands.RasterBitImage(conversionResult.WidthBytes, conversionResult.Height, mode);
        var result = new byte[header.Length + conversionResult.ImageData.Length];

        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(conversionResult.ImageData, 0, result, header.Length, conversionResult.ImageData.Length);

        return result;
    }

    /// <inheritdoc />
    public async Task<byte[]?> CreateLogoPrintCommandAsync(string imagePath, int maxWidth = 384)
    {
        var options = new ImageConversionOptions { MaxWidth = maxWidth };
        var result = await ConvertFromFileAsync(imagePath, options);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to convert logo: {Error}", result.ErrorMessage);
            return null;
        }

        // Center alignment + image + reset alignment
        var alignCenter = EscPosCommands.AlignCenter;
        var imageCommand = GetPrintCommand(result);
        var alignLeft = EscPosCommands.AlignLeft;
        var lineFeed = EscPosCommands.LineFeed;

        var total = alignCenter.Length + imageCommand.Length + lineFeed.Length + alignLeft.Length;
        var command = new byte[total];

        int offset = 0;
        Buffer.BlockCopy(alignCenter, 0, command, offset, alignCenter.Length);
        offset += alignCenter.Length;
        Buffer.BlockCopy(imageCommand, 0, command, offset, imageCommand.Length);
        offset += imageCommand.Length;
        Buffer.BlockCopy(lineFeed, 0, command, offset, lineFeed.Length);
        offset += lineFeed.Length;
        Buffer.BlockCopy(alignLeft, 0, command, offset, alignLeft.Length);

        return command;
    }

    private ImageConversionResult ConvertFromBytes(byte[] imageData, ImageConversionOptions options)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            using var original = Image.FromStream(ms);
            using var bitmap = new Bitmap(original);

            // Resize if needed
            var targetWidth = Math.Min(bitmap.Width, options.MaxWidth);

            // Ensure width is multiple of 8 for ESC/POS
            targetWidth = (targetWidth / 8) * 8;
            if (targetWidth == 0) targetWidth = 8;

            var scale = (double)targetWidth / bitmap.Width;
            var targetHeight = (int)(bitmap.Height * scale);

            using var resized = new Bitmap(targetWidth, targetHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
            }

            // Apply adjustments
            var adjusted = ApplyAdjustments(resized, options);

            // Convert to monochrome
            var monochrome = ConvertToMonochrome(adjusted, options);

            // Convert to raster format
            var rasterData = ConvertToRaster(monochrome);

            adjusted.Dispose();
            monochrome.Dispose();

            return ImageConversionResult.Succeeded(rasterData, targetWidth, targetHeight);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image to raster format");
            return ImageConversionResult.Failed(ex.Message);
        }
    }

    private Bitmap ApplyAdjustments(Bitmap source, ImageConversionOptions options)
    {
        if (options.Brightness == 0 && options.Contrast == 0 && !options.Invert)
        {
            return (Bitmap)source.Clone();
        }

        var result = new Bitmap(source.Width, source.Height);
        var brightness = options.Brightness / 100f;
        var contrast = (100f + options.Contrast) / 100f;

        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var bytes = sourceData.Stride * source.Height;
            var sourcePixels = new byte[bytes];
            var resultPixels = new byte[bytes];

            Marshal.Copy(sourceData.Scan0, sourcePixels, 0, bytes);

            for (int i = 0; i < bytes; i += 4)
            {
                for (int c = 0; c < 3; c++)
                {
                    var value = sourcePixels[i + c] / 255f;

                    // Apply contrast
                    value = ((value - 0.5f) * contrast) + 0.5f;

                    // Apply brightness
                    value += brightness;

                    // Invert if needed
                    if (options.Invert)
                    {
                        value = 1f - value;
                    }

                    resultPixels[i + c] = (byte)Math.Clamp((int)(value * 255), 0, 255);
                }
                resultPixels[i + 3] = sourcePixels[i + 3]; // Alpha
            }

            Marshal.Copy(resultPixels, 0, resultData.Scan0, bytes);
        }
        finally
        {
            source.UnlockBits(sourceData);
            result.UnlockBits(resultData);
        }

        return result;
    }

    private Bitmap ConvertToMonochrome(Bitmap source, ImageConversionOptions options)
    {
        return options.Dithering switch
        {
            DitheringMethod.FloydSteinberg => ApplyFloydSteinbergDithering(source, options.Threshold),
            DitheringMethod.Ordered => ApplyOrderedDithering(source),
            DitheringMethod.Atkinson => ApplyAtkinsonDithering(source, options.Threshold),
            _ => ApplyThreshold(source, options.Threshold)
        };
    }

    private Bitmap ApplyThreshold(Bitmap source, int threshold)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    var gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);

                    if (gray >= threshold)
                    {
                        // Set white pixel
                        SetPixel1bpp(resultData, x, y, true);
                    }
                }
            }
        }
        finally
        {
            result.UnlockBits(resultData);
        }

        return result;
    }

    private Bitmap ApplyFloydSteinbergDithering(Bitmap source, int threshold)
    {
        // Work with grayscale float array
        var width = source.Width;
        var height = source.Height;
        var errors = new float[width, height];

        // Convert to grayscale
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = source.GetPixel(x, y);
                errors[x, y] = pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f;
            }
        }

        var result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
        var rect = new Rectangle(0, 0, width, height);
        var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var oldPixel = errors[x, y];
                    var newPixel = oldPixel >= threshold ? 255f : 0f;
                    var error = oldPixel - newPixel;

                    if (newPixel > 0)
                    {
                        SetPixel1bpp(resultData, x, y, true);
                    }

                    // Distribute error
                    if (x + 1 < width)
                        errors[x + 1, y] += error * 7f / 16f;
                    if (y + 1 < height)
                    {
                        if (x > 0)
                            errors[x - 1, y + 1] += error * 3f / 16f;
                        errors[x, y + 1] += error * 5f / 16f;
                        if (x + 1 < width)
                            errors[x + 1, y + 1] += error * 1f / 16f;
                    }
                }
            }
        }
        finally
        {
            result.UnlockBits(resultData);
        }

        return result;
    }

    private Bitmap ApplyAtkinsonDithering(Bitmap source, int threshold)
    {
        var width = source.Width;
        var height = source.Height;
        var errors = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = source.GetPixel(x, y);
                errors[x, y] = pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f;
            }
        }

        var result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
        var rect = new Rectangle(0, 0, width, height);
        var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var oldPixel = errors[x, y];
                    var newPixel = oldPixel >= threshold ? 255f : 0f;
                    var error = (oldPixel - newPixel) / 8f;

                    if (newPixel > 0)
                    {
                        SetPixel1bpp(resultData, x, y, true);
                    }

                    // Atkinson pattern
                    if (x + 1 < width)
                        errors[x + 1, y] += error;
                    if (x + 2 < width)
                        errors[x + 2, y] += error;
                    if (y + 1 < height)
                    {
                        if (x > 0)
                            errors[x - 1, y + 1] += error;
                        errors[x, y + 1] += error;
                        if (x + 1 < width)
                            errors[x + 1, y + 1] += error;
                    }
                    if (y + 2 < height)
                        errors[x, y + 2] += error;
                }
            }
        }
        finally
        {
            result.UnlockBits(resultData);
        }

        return result;
    }

    private Bitmap ApplyOrderedDithering(Bitmap source)
    {
        // 4x4 Bayer matrix
        int[,] bayerMatrix = {
            {  0,  8,  2, 10 },
            { 12,  4, 14,  6 },
            {  3, 11,  1,  9 },
            { 15,  7, 13,  5 }
        };

        var width = source.Width;
        var height = source.Height;
        var result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
        var rect = new Rectangle(0, 0, width, height);
        var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    var gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    var threshold = (bayerMatrix[x % 4, y % 4] + 1) * 16;

                    if (gray >= threshold)
                    {
                        SetPixel1bpp(resultData, x, y, true);
                    }
                }
            }
        }
        finally
        {
            result.UnlockBits(resultData);
        }

        return result;
    }

    private static void SetPixel1bpp(BitmapData data, int x, int y, bool white)
    {
        int index = y * data.Stride + (x >> 3);
        byte mask = (byte)(0x80 >> (x & 7));

        unsafe
        {
            byte* ptr = (byte*)data.Scan0 + index;
            if (white)
                *ptr |= mask;
            else
                *ptr &= (byte)~mask;
        }
    }

    private byte[] ConvertToRaster(Bitmap monochrome)
    {
        var width = monochrome.Width;
        var height = monochrome.Height;
        var widthBytes = (width + 7) / 8;
        var rasterData = new byte[widthBytes * height];

        var rect = new Rectangle(0, 0, width, height);
        var data = monochrome.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < widthBytes; x++)
                {
                    // Note: ESC/POS raster format has inverted bit meaning
                    // 1 = black (print), 0 = white (no print)
                    // But Windows bitmaps are: 1 = white, 0 = black
                    // So we need to invert
                    unsafe
                    {
                        byte* ptr = (byte*)data.Scan0 + y * data.Stride + x;
                        rasterData[y * widthBytes + x] = (byte)~(*ptr);
                    }
                }
            }
        }
        finally
        {
            monochrome.UnlockBits(data);
        }

        return rasterData;
    }
}
