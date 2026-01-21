// src/HospitalityPOS.Infrastructure/Services/EmployeePhotoService.cs
// Implementation of employee photo management service.

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing employee photos.
/// </summary>
public class EmployeePhotoService : IEmployeePhotoService
{
    private readonly POSDbContext _context;
    private readonly EmployeePhotoSettings _settings;

    public EmployeePhotoService(POSDbContext context)
    {
        _context = context;
        _settings = new EmployeePhotoSettings(
            BasePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "EmployeePhotos"),
            AllowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" },
            MaxFileSizeBytes: 5 * 1024 * 1024, // 5MB
            ThumbnailWidth: 100,
            ThumbnailHeight: 100
        );

        // Ensure directory exists
        Directory.CreateDirectory(_settings.BasePath);
    }

    public async Task<string> UploadPhotoAsync(int employeeId, byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        // Validate the photo
        var validation = ValidatePhoto(imageData, fileName);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        // Get the employee
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        // Delete existing photo if any
        if (!string.IsNullOrEmpty(employee.PhotoPath))
        {
            await DeletePhotoAsync(employeeId, cancellationToken);
        }

        // Create employee folder
        var employeeFolder = Path.Combine(_settings.BasePath, employeeId.ToString());
        Directory.CreateDirectory(employeeFolder);

        // Get file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".jpg";
        }

        // Save main photo
        var photoFileName = $"photo{extension}";
        var photoPath = Path.Combine(employeeFolder, photoFileName);
        await File.WriteAllBytesAsync(photoPath, imageData, cancellationToken);

        // Generate thumbnail
        var thumbnailPath = await GenerateThumbnailAsync(photoPath, _settings.ThumbnailWidth, _settings.ThumbnailHeight, cancellationToken);

        // Update employee record
        employee.PhotoPath = GetRelativePath(photoPath);
        employee.ThumbnailPath = GetRelativePath(thumbnailPath);

        await _context.SaveChangesAsync(cancellationToken);

        return employee.PhotoPath;
    }

    public async Task<string?> GetPhotoPathAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.PhotoPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee?.PhotoPath == null)
        {
            return null;
        }

        return GetFullPath(employee.PhotoPath);
    }

    public async Task<byte[]?> GetPhotoDataAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var fullPath = await GetPhotoPathAsync(employeeId, cancellationToken);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<byte[]?> GetThumbnailDataAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.ThumbnailPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee?.ThumbnailPath == null)
        {
            return null;
        }

        var fullPath = GetFullPath(employee.ThumbnailPath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<bool> DeletePhotoAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
        if (employee == null)
        {
            return false;
        }

        // Delete files
        if (!string.IsNullOrEmpty(employee.PhotoPath))
        {
            var photoPath = GetFullPath(employee.PhotoPath);
            if (File.Exists(photoPath))
            {
                File.Delete(photoPath);
            }
        }

        if (!string.IsNullOrEmpty(employee.ThumbnailPath))
        {
            var thumbnailPath = GetFullPath(employee.ThumbnailPath);
            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }
        }

        // Try to delete the folder if empty
        var employeeFolder = Path.Combine(_settings.BasePath, employeeId.ToString());
        if (Directory.Exists(employeeFolder) && !Directory.EnumerateFileSystemEntries(employeeFolder).Any())
        {
            Directory.Delete(employeeFolder);
        }

        // Clear database references
        employee.PhotoPath = null;
        employee.ThumbnailPath = null;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public Task<string> GenerateThumbnailAsync(string photoPath, int width = 100, int height = 100, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var directory = Path.GetDirectoryName(photoPath)!;
            var extension = Path.GetExtension(photoPath);
            var thumbnailFileName = $"thumbnail{extension}";
            var thumbnailPath = Path.Combine(directory, thumbnailFileName);

            using var originalImage = Image.FromFile(photoPath);
            using var thumbnail = ResizeImage(originalImage, width, height);

            var encoder = GetEncoder(extension);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);

            thumbnail.Save(thumbnailPath, encoder, encoderParams);
            return thumbnailPath;
        }, cancellationToken);
    }

    public PhotoValidationResult ValidatePhoto(byte[] imageData, string fileName)
    {
        // Check file size
        if (imageData.Length > _settings.MaxFileSizeBytes)
        {
            return new PhotoValidationResult(false, $"File size exceeds maximum allowed ({_settings.MaxFileSizeBytes / 1024 / 1024}MB)");
        }

        // Check extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_settings.AllowedExtensions.Contains(extension))
        {
            return new PhotoValidationResult(false, $"Invalid file type. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");
        }

        // Verify it's a valid image
        try
        {
            using var stream = new MemoryStream(imageData);
            using var image = Image.FromStream(stream);

            // Check minimum dimensions
            if (image.Width < 50 || image.Height < 50)
            {
                return new PhotoValidationResult(false, "Image dimensions too small (minimum 50x50 pixels)");
            }

            // Check maximum dimensions (to prevent memory issues)
            if (image.Width > 4000 || image.Height > 4000)
            {
                return new PhotoValidationResult(false, "Image dimensions too large (maximum 4000x4000 pixels)");
            }
        }
        catch
        {
            return new PhotoValidationResult(false, "Invalid or corrupted image file");
        }

        return new PhotoValidationResult(true);
    }

    public EmployeePhotoSettings GetSettings()
    {
        return _settings;
    }

    #region Private Helpers

    private string GetRelativePath(string fullPath)
    {
        // Store relative to base path
        if (fullPath.StartsWith(_settings.BasePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(_settings.BasePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }

    private string GetFullPath(string relativePath)
    {
        // Handle already-absolute paths
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }
        return Path.Combine(_settings.BasePath, relativePath);
    }

    private static Image ResizeImage(Image image, int width, int height)
    {
        // Calculate aspect ratio
        var ratioX = (double)width / image.Width;
        var ratioY = (double)height / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        var newImage = new Bitmap(newWidth, newHeight);
        using var graphics = Graphics.FromImage(newImage);

        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;

        graphics.DrawImage(image, 0, 0, newWidth, newHeight);

        return newImage;
    }

    private static ImageCodecInfo GetEncoder(string extension)
    {
        var format = extension.ToLowerInvariant() switch
        {
            ".png" => ImageFormat.Png,
            ".gif" => ImageFormat.Gif,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Jpeg
        };

        return ImageCodecInfo.GetImageEncoders()
            .First(codec => codec.FormatID == format.Guid);
    }

    #endregion
}
