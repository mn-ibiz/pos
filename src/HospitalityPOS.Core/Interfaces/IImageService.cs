namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing product images.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Gets the base path where images are stored.
    /// </summary>
    string ImagesBasePath { get; }

    /// <summary>
    /// Saves a product image to the organized folder structure.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source image file.</param>
    /// <param name="productCode">The product code (used for naming).</param>
    /// <returns>The path to the saved image.</returns>
    Task<string> SaveProductImageAsync(string sourceFilePath, string productCode);

    /// <summary>
    /// Deletes a product image.
    /// </summary>
    /// <param name="productCode">The product code.</param>
    Task DeleteProductImageAsync(string productCode);

    /// <summary>
    /// Gets the path to display for a product image.
    /// Returns placeholder if the image doesn't exist.
    /// </summary>
    /// <param name="imagePath">The stored image path.</param>
    /// <returns>The path to display (actual image or placeholder).</returns>
    string GetDisplayImagePath(string? imagePath);

    /// <summary>
    /// Checks if a file is a valid image format.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a valid image format.</returns>
    bool IsValidImageFormat(string filePath);

    /// <summary>
    /// Validates an image file for upload.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if the file is valid for upload.</returns>
    bool ValidateImageFile(string filePath, out string? errorMessage);
}
