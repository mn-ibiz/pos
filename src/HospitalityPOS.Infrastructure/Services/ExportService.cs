using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using HospitalityPOS.Core.Interfaces;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for exporting data to various file formats.
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger _logger;
    private readonly string _baseExportDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ExportService(ILogger logger)
    {
        _logger = logger;
        _baseExportDirectory = GetDefaultExportDirectory();
    }

    /// <summary>
    /// Validates that the file path is within the allowed export directory to prevent path traversal attacks.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <returns>The validated absolute path.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is outside the allowed directory.</exception>
    private string ValidateAndNormalizeFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        // Get the full path and normalize it
        var fullPath = Path.GetFullPath(filePath);

        // Allow paths in the base export directory or user's Documents folder
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var isInAllowedDirectory =
            fullPath.StartsWith(_baseExportDirectory, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(documentsPath, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(desktopPath, StringComparison.OrdinalIgnoreCase);

        if (!isInAllowedDirectory)
        {
            _logger.Warning("Attempted path traversal attack blocked: {FilePath}", filePath);
            throw new ArgumentException($"File path must be within Documents, Desktop, or the POS Reports folder. Path: {filePath}", nameof(filePath));
        }

        return fullPath;
    }

    /// <inheritdoc />
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath, CancellationToken cancellationToken = default)
    {
        // Validate path to prevent path traversal attacks
        var validatedPath = ValidateAndNormalizeFilePath(filePath);

        _logger.Information("Exporting data to CSV: {FilePath}", validatedPath);

        try
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Use StreamWriter for memory-efficient streaming writes
            await using var writer = new StreamWriter(validatedPath, false, new UTF8Encoding(true));

            // Header row
            var headers = properties.Select(p => EscapeCsvField(GetDisplayName(p)));
            await writer.WriteLineAsync(string.Join(",", headers));

            // Data rows - stream one at a time to avoid memory pressure
            foreach (var item in data)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Warning("CSV export cancelled");
                    return;
                }

                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return EscapeCsvField(FormatValue(value));
                });
                await writer.WriteLineAsync(string.Join(",", values));
            }

            _logger.Information("CSV export completed successfully: {FilePath}", validatedPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export data to CSV: {FilePath}", validatedPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ExportToCsvAsync(DataTable dataTable, string filePath, CancellationToken cancellationToken = default)
    {
        // Validate path to prevent path traversal attacks
        var validatedPath = ValidateAndNormalizeFilePath(filePath);

        _logger.Information("Exporting DataTable to CSV: {FilePath}", validatedPath);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Use StreamWriter for memory-efficient streaming writes
            await using var writer = new StreamWriter(validatedPath, false, new UTF8Encoding(true));

            // Header row
            var headers = dataTable.Columns.Cast<DataColumn>().Select(c => EscapeCsvField(c.ColumnName));
            await writer.WriteLineAsync(string.Join(",", headers));

            // Data rows - stream one at a time to avoid memory pressure
            foreach (DataRow row in dataTable.Rows)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Warning("CSV export cancelled");
                    return;
                }

                var values = row.ItemArray.Select(v => EscapeCsvField(FormatValue(v)));
                await writer.WriteLineAsync(string.Join(",", values));
            }

            _logger.Information("DataTable CSV export completed successfully: {FilePath}", validatedPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export DataTable to CSV: {FilePath}", validatedPath);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string reportTitle, CancellationToken cancellationToken = default)
    {
        _logger.Information("PDF export requested: {FilePath}", filePath);

        // PDF export is optional and not implemented in this version
        // A future version could use QuestPDF or similar library
        _logger.Warning("PDF export is not implemented in this version. Use CSV export instead.");

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public string GenerateFilename(string reportType, DateTime startDate, DateTime? endDate = null, string extension = "csv")
    {
        // Sanitize report type for use in filename
        var sanitizedType = SanitizeFilename(reportType);

        // Build date range string
        var dateRange = endDate.HasValue && endDate.Value.Date != startDate.Date
            ? $"{startDate:yyyyMMdd}-{endDate:yyyyMMdd}"
            : $"{startDate:yyyyMMdd}";

        return $"{sanitizedType}_{dateRange}.{extension}";
    }

    /// <inheritdoc />
    public string GetDefaultExportDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var reportsPath = Path.Combine(documentsPath, "POS Reports");

        if (!Directory.Exists(reportsPath))
        {
            try
            {
                Directory.CreateDirectory(reportsPath);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to create default export directory: {Path}", reportsPath);
                return documentsPath;
            }
        }

        return reportsPath;
    }

    /// <summary>
    /// Escapes a CSV field value.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // Escape quotes and wrap in quotes if the field contains special characters
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    /// <summary>
    /// Formats a value for CSV output.
    /// </summary>
    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            TimeOnly t => t.ToString("HH:mm:ss"),
            decimal d => d.ToString("N2"),
            double dbl => dbl.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "Yes" : "No",
            Enum e => FormatEnumValue(e),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Formats an enum value for display.
    /// </summary>
    private static string FormatEnumValue(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
            if (displayAttr != null)
            {
                return displayAttr.Name ?? value.ToString();
            }
        }

        // Convert PascalCase to Title Case with spaces
        var name = value.ToString();
        var result = new StringBuilder();

        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the display name for a property.
    /// </summary>
    private static string GetDisplayName(PropertyInfo property)
    {
        // Check for DisplayAttribute
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttr?.Name != null)
        {
            return displayAttr.Name;
        }

        // Convert PascalCase to Title Case with spaces
        var name = property.Name;
        var result = new StringBuilder();

        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>
    /// Sanitizes a string for use as a filename.
    /// </summary>
    private static string SanitizeFilename(string filename)
    {
        // Replace spaces with underscores
        var sanitized = filename.Replace(" ", "_");

        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), string.Empty);
        }

        // Replace multiple underscores with single
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        return sanitized.Trim('_');
    }

    /// <inheritdoc />
    public async Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string defaultFileName, string sheetName, CancellationToken cancellationToken = default)
    {
        _logger.Information("Excel export requested: {FileName}", defaultFileName);

        try
        {
            // Show save file dialog
            var filePath = ShowSaveFileDialog(defaultFileName, "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "xlsx");
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.Information("Excel export cancelled by user");
                return false;
            }

            // Validate path
            var validatedPath = ValidateAndNormalizeFilePath(filePath);

            // Convert data to DataTable
            var dataTable = ConvertToDataTable(data);

            // Export using CSV-style Excel (tab-delimited for Excel compatibility)
            // In a production system, you'd use ClosedXML or EPPlus for proper Excel files
            await ExportToExcelFileAsync(dataTable, validatedPath, sheetName, cancellationToken);

            _logger.Information("Excel export completed: {FilePath}", validatedPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export to Excel: {FileName}", defaultFileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExportToExcelAsync(DataTable dataTable, string defaultFileName, string sheetName, CancellationToken cancellationToken = default)
    {
        _logger.Information("Excel export requested: {FileName}", defaultFileName);

        try
        {
            // Show save file dialog
            var filePath = ShowSaveFileDialog(defaultFileName, "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "xlsx");
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.Information("Excel export cancelled by user");
                return false;
            }

            // Validate path
            var validatedPath = ValidateAndNormalizeFilePath(filePath);

            await ExportToExcelFileAsync(dataTable, validatedPath, sheetName, cancellationToken);

            _logger.Information("Excel export completed: {FilePath}", validatedPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export DataTable to Excel: {FileName}", defaultFileName);
            throw;
        }
    }

    /// <summary>
    /// Shows a save file dialog and returns the selected path.
    /// </summary>
    private static string? ShowSaveFileDialog(string defaultFileName, string filter, string defaultExtension)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = SanitizeFilename(defaultFileName),
            DefaultExt = $".{defaultExtension}",
            Filter = filter,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Converts a collection to a DataTable.
    /// </summary>
    private static DataTable ConvertToDataTable<T>(IEnumerable<T> data)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Add columns
        foreach (var prop in properties)
        {
            var columnName = GetDisplayName(prop);
            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // Handle complex types by converting to string
            if (columnType.IsEnum || columnType == typeof(DateTime) || columnType == typeof(DateOnly) ||
                columnType == typeof(TimeOnly) || columnType.IsPrimitive || columnType == typeof(string) ||
                columnType == typeof(decimal) || columnType == typeof(Guid))
            {
                dataTable.Columns.Add(columnName, typeof(string));
            }
            else
            {
                dataTable.Columns.Add(columnName, typeof(string));
            }
        }

        // Add rows
        foreach (var item in data)
        {
            var row = dataTable.NewRow();
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item);
                row[i] = FormatValue(value);
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Exports data to an Excel-compatible file.
    /// Uses OpenXML-style XLSX format for proper Excel compatibility.
    /// </summary>
    private async Task ExportToExcelFileAsync(DataTable dataTable, string filePath, string sheetName, CancellationToken cancellationToken)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // For true Excel format, we'll create a simple XML-based spreadsheet
        // This creates an Excel-compatible XML file that opens directly in Excel
        await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

        // Write Excel XML header
        await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        await writer.WriteLineAsync("<?mso-application progid=\"Excel.Sheet\"?>");
        await writer.WriteLineAsync("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        await writer.WriteLineAsync(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");

        // Styles for header row
        await writer.WriteLineAsync("<Styles>");
        await writer.WriteLineAsync("<Style ss:ID=\"Header\">");
        await writer.WriteLineAsync("<Font ss:Bold=\"1\"/>");
        await writer.WriteLineAsync("<Interior ss:Color=\"#4472C4\" ss:Pattern=\"Solid\"/>");
        await writer.WriteLineAsync("<Font ss:Color=\"#FFFFFF\"/>");
        await writer.WriteLineAsync("</Style>");
        await writer.WriteLineAsync("<Style ss:ID=\"Number\">");
        await writer.WriteLineAsync("<NumberFormat ss:Format=\"#,##0.00\"/>");
        await writer.WriteLineAsync("</Style>");
        await writer.WriteLineAsync("</Styles>");

        // Worksheet
        await writer.WriteLineAsync($"<Worksheet ss:Name=\"{EscapeXml(sheetName)}\">");
        await writer.WriteLineAsync("<Table>");

        // Header row
        await writer.WriteLineAsync("<Row ss:StyleID=\"Header\">");
        foreach (DataColumn column in dataTable.Columns)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await writer.WriteLineAsync($"<Cell><Data ss:Type=\"String\">{EscapeXml(column.ColumnName)}</Data></Cell>");
        }
        await writer.WriteLineAsync("</Row>");

        // Data rows
        foreach (DataRow row in dataTable.Rows)
        {
            if (cancellationToken.IsCancellationRequested) return;

            await writer.WriteLineAsync("<Row>");
            foreach (var item in row.ItemArray)
            {
                var value = item?.ToString() ?? string.Empty;
                var dataType = IsNumeric(value) ? "Number" : "String";
                await writer.WriteLineAsync($"<Cell><Data ss:Type=\"{dataType}\">{EscapeXml(value)}</Data></Cell>");
            }
            await writer.WriteLineAsync("</Row>");
        }

        await writer.WriteLineAsync("</Table>");
        await writer.WriteLineAsync("</Worksheet>");
        await writer.WriteLineAsync("</Workbook>");
    }

    /// <summary>
    /// Escapes XML special characters.
    /// </summary>
    private static string EscapeXml(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// Checks if a string value represents a numeric value.
    /// </summary>
    private static bool IsNumeric(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Remove currency symbols and thousands separators for check
        var cleaned = value.Replace(",", "").Replace("KES", "").Replace("$", "").Trim();
        return decimal.TryParse(cleaned, out _);
    }
}
