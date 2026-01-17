using HospitalityPOS.Core.Printing;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Converts images to ESC/POS raster format for thermal printing.
/// </summary>
public interface IImageConverter
{
    /// <summary>
    /// Converts an image file to ESC/POS raster format.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>Conversion result with raster data.</returns>
    Task<ImageConversionResult> ConvertFromFileAsync(string imagePath, ImageConversionOptions? options = null);

    /// <summary>
    /// Converts image bytes to ESC/POS raster format.
    /// </summary>
    /// <param name="imageData">Image file bytes.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>Conversion result with raster data.</returns>
    Task<ImageConversionResult> ConvertFromBytesAsync(byte[] imageData, ImageConversionOptions? options = null);

    /// <summary>
    /// Converts a Base64 encoded image to ESC/POS raster format.
    /// </summary>
    /// <param name="base64Image">Base64 encoded image.</param>
    /// <param name="options">Conversion options.</param>
    /// <returns>Conversion result with raster data.</returns>
    Task<ImageConversionResult> ConvertFromBase64Async(string base64Image, ImageConversionOptions? options = null);

    /// <summary>
    /// Gets the ESC/POS raster command with image data.
    /// </summary>
    /// <param name="conversionResult">The converted image result.</param>
    /// <param name="mode">Print mode (0=normal, 1=double width, 2=double height, 3=quad).</param>
    /// <returns>Complete ESC/POS command bytes.</returns>
    byte[] GetPrintCommand(ImageConversionResult conversionResult, int mode = 0);

    /// <summary>
    /// Creates a centered logo command ready for printing.
    /// </summary>
    /// <param name="imagePath">Path to logo image.</param>
    /// <param name="maxWidth">Maximum width in pixels.</param>
    /// <returns>ESC/POS command bytes or null if failed.</returns>
    Task<byte[]?> CreateLogoPrintCommandAsync(string imagePath, int maxWidth = 384);
}
