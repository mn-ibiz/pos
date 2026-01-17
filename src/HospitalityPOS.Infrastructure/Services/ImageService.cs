using Serilog;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing product images.
/// </summary>
public class ImageService : IImageService
{
    private readonly ILogger _logger;
    private readonly string _imagesBasePath;

    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB
    private static readonly string[] ValidExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ImageService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set base path relative to application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _imagesBasePath = Path.Combine(appDirectory, "Images", "Products");

        // Ensure directory exists
        Directory.CreateDirectory(_imagesBasePath);
    }

    /// <inheritdoc />
    public string ImagesBasePath => _imagesBasePath;

    /// <inheritdoc />
    public async Task<string> SaveProductImageAsync(string sourceFilePath, string productCode)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Source file path is required.", nameof(sourceFilePath));

        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code is required.", nameof(productCode));

        if (!ValidateImageFile(sourceFilePath, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        // Sanitize product code for filename
        var safeProductCode = SanitizeFileName(productCode);
        var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        var targetFileName = $"{safeProductCode}{extension}";
        var targetPath = Path.Combine(_imagesBasePath, targetFileName);

        // Delete any existing images for this product (different extensions)
        await DeleteProductImageAsync(productCode);

        // Copy the file to the target location
        await Task.Run(() => File.Copy(sourceFilePath, targetPath, overwrite: true));

        _logger.Information("Product image saved: {SourcePath} -> {TargetPath}",
            sourceFilePath, targetPath);

        return targetPath;
    }

    /// <inheritdoc />
    public Task DeleteProductImageAsync(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return Task.CompletedTask;

        var safeProductCode = SanitizeFileName(productCode);

        foreach (var extension in ValidExtensions)
        {
            var filePath = Path.Combine(_imagesBasePath, $"{safeProductCode}{extension}");
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.Information("Deleted product image: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to delete product image: {FilePath}", filePath);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string GetDisplayImagePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return string.Empty; // No image, UI should show placeholder

        if (File.Exists(imagePath))
            return imagePath;

        // Try to find in the products folder by filename
        var fileName = Path.GetFileName(imagePath);
        var productPath = Path.Combine(_imagesBasePath, fileName);

        if (File.Exists(productPath))
            return productPath;

        _logger.Debug("Image not found: {ImagePath}", imagePath);
        return string.Empty; // Image not found, UI should show placeholder
    }

    /// <inheritdoc />
    public bool IsValidImageFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return ValidExtensions.Contains(extension);
    }

    /// <inheritdoc />
    public bool ValidateImageFile(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "File path is required.";
            return false;
        }

        if (!File.Exists(filePath))
        {
            errorMessage = "File does not exist.";
            return false;
        }

        if (!IsValidImageFormat(filePath))
        {
            errorMessage = "Invalid image format. Supported formats: JPG, PNG, GIF.";
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            errorMessage = $"File size exceeds 2MB limit. Current size: {fileInfo.Length / (1024 * 1024.0):F2}MB.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sanitizes a string to be used as a filename.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new System.Text.StringBuilder(fileName.Length);

        foreach (var c in fileName)
        {
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitized.ToString();
    }
}
