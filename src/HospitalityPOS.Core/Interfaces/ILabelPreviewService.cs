using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for rendering visual label previews.
/// </summary>
public interface ILabelPreviewService
{
    /// <summary>
    /// Renders a preview image for a template with sample data.
    /// </summary>
    /// <param name="templateId">The template ID to render.</param>
    /// <param name="data">The product data to substitute into placeholders.</param>
    /// <returns>PNG image bytes.</returns>
    Task<byte[]> RenderPreviewAsync(int templateId, ProductLabelDataDto data);

    /// <summary>
    /// Renders a preview image from raw template content.
    /// </summary>
    /// <param name="content">The template content (ZPL/EPL/TSPL).</param>
    /// <param name="language">The print language.</param>
    /// <param name="widthMm">Label width in millimeters.</param>
    /// <param name="heightMm">Label height in millimeters.</param>
    /// <param name="dpi">Printer DPI (default 203).</param>
    /// <param name="data">Optional product data for placeholder substitution.</param>
    /// <returns>PNG image bytes.</returns>
    Task<byte[]> RenderPreviewAsync(
        string content,
        LabelPrintLanguageDto language,
        decimal widthMm,
        decimal heightMm,
        int dpi,
        ProductLabelDataDto? data = null);

    /// <summary>
    /// Renders ZPL content using the Labelary API (requires internet).
    /// </summary>
    /// <param name="zpl">The ZPL content to render.</param>
    /// <param name="widthDots">Label width in dots.</param>
    /// <param name="heightDots">Label height in dots.</param>
    /// <param name="dpmm">Dots per millimeter (default 8 for 203 DPI).</param>
    /// <returns>PNG image bytes, or null if service unavailable.</returns>
    Task<byte[]?> RenderZplViaLabelaryAsync(string zpl, int widthDots, int heightDots, int dpmm = 8);

    /// <summary>
    /// Checks if the Labelary API is available.
    /// </summary>
    Task<bool> IsLabelaryAvailableAsync();
}
