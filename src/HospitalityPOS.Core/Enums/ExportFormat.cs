namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Supported export file formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Comma-Separated Values format (Excel/Spreadsheet compatible).
    /// </summary>
    Csv = 0,

    /// <summary>
    /// Portable Document Format.
    /// </summary>
    Pdf = 1,

    /// <summary>
    /// Microsoft Excel format (xlsx).
    /// </summary>
    Excel = 2
}
