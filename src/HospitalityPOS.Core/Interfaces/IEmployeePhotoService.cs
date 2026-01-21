// src/HospitalityPOS.Core/Interfaces/IEmployeePhotoService.cs
// Service interface for managing employee photos.

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing employee photos.
/// </summary>
public interface IEmployeePhotoService
{
    /// <summary>
    /// Uploads and saves an employee photo.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="imageData">The image file bytes.</param>
    /// <param name="fileName">Original file name for extension detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The relative path to the saved photo.</returns>
    Task<string> UploadPhotoAsync(int employeeId, byte[] imageData, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full path to an employee's photo.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to the photo file, or null if not found.</returns>
    Task<string?> GetPhotoPathAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the photo as a byte array for display.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Photo bytes, or null if not found.</returns>
    Task<byte[]?> GetPhotoDataAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the thumbnail as a byte array for list views.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Thumbnail bytes, or null if not found.</returns>
    Task<byte[]?> GetThumbnailDataAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an employee's photo.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeletePhotoAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail from the main photo.
    /// </summary>
    /// <param name="photoPath">Path to the main photo.</param>
    /// <param name="width">Thumbnail width.</param>
    /// <param name="height">Thumbnail height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the generated thumbnail.</returns>
    Task<string> GenerateThumbnailAsync(string photoPath, int width = 100, int height = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the image file.
    /// </summary>
    /// <param name="imageData">Image bytes.</param>
    /// <param name="fileName">File name.</param>
    /// <returns>Validation result.</returns>
    PhotoValidationResult ValidatePhoto(byte[] imageData, string fileName);

    /// <summary>
    /// Gets the photo storage settings.
    /// </summary>
    EmployeePhotoSettings GetSettings();
}

/// <summary>
/// Result of photo validation.
/// </summary>
public record PhotoValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// Employee photo storage settings.
/// </summary>
public record EmployeePhotoSettings(
    string BasePath,
    string[] AllowedExtensions,
    long MaxFileSizeBytes,
    int ThumbnailWidth,
    int ThumbnailHeight
);
