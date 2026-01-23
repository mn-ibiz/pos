# feat: Implement Visual Label Preview Rendering

**Labels:** `enhancement` `backend` `frontend` `printing` `labels` `priority-medium`

## Overview

Implement visual rendering of label templates to show users an actual image preview of what the printed label will look like. Currently, `GeneratePreviewAsync` returns template content text, not a rendered image.

## Background

Users need to see their label design before printing to:
- Verify layout and positioning
- Check text fits within bounds
- Preview barcode appearance
- Confirm overall design quality
- Reduce wasted labels from trial prints

## Requirements

### Preview Service Interface

Create `ILabelPreviewService`:

```csharp
public interface ILabelPreviewService
{
    /// <summary>
    /// Render a template with sample/actual data as a PNG image
    /// </summary>
    Task<byte[]> RenderPreviewAsync(int templateId, ProductLabelDataDto data);

    /// <summary>
    /// Render raw template content (for designer preview)
    /// </summary>
    Task<byte[]> RenderPreviewAsync(
        string templateContent,
        LabelPrintLanguageDto language,
        decimal widthMm,
        decimal heightMm,
        int dpi,
        ProductLabelDataDto? data = null);

    /// <summary>
    /// Render ZPL using Labelary API (online service)
    /// </summary>
    Task<byte[]> RenderZplViaLabelaryAsync(string zplContent, int widthDots, int heightDots);

    /// <summary>
    /// Check if rendering is available
    /// </summary>
    Task<PreviewCapabilities> GetCapabilitiesAsync();
}

public class PreviewCapabilities
{
    public bool LocalRenderingAvailable { get; set; }
    public bool LabelaryApiAvailable { get; set; }
    public string? LabelaryApiUrl { get; set; }
    public List<string> SupportedLanguages { get; set; }
}
```

### Implementation Options

#### Option A: Local Rendering with SkiaSharp

```csharp
public class SkiaLabelPreviewService : ILabelPreviewService
{
    public async Task<byte[]> RenderPreviewAsync(
        string templateContent,
        LabelPrintLanguageDto language,
        decimal widthMm,
        decimal heightMm,
        int dpi,
        ProductLabelDataDto? data = null)
    {
        // Calculate pixel dimensions (for screen display)
        int widthPx = (int)(widthMm * 96 / 25.4m); // 96 DPI screen
        int heightPx = (int)(heightMm * 96 / 25.4m);

        using var surface = SKSurface.Create(new SKImageInfo(widthPx, heightPx));
        var canvas = surface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.LightGray,
            StrokeWidth = 1
        };
        canvas.DrawRect(0, 0, widthPx - 1, heightPx - 1, borderPaint);

        // Parse template and render elements
        var elements = ParseTemplate(templateContent, language);
        foreach (var element in elements)
        {
            RenderElement(canvas, element, data, widthMm, heightMm, widthPx, heightPx, dpi);
        }

        // Export as PNG
        using var image = surface.Snapshot();
        using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
        return pngData.ToArray();
    }

    private void RenderElement(SKCanvas canvas, LabelElement element,
        ProductLabelDataDto? data, decimal widthMm, decimal heightMm,
        int widthPx, int heightPx, int dpi)
    {
        // Scale from dots to pixels
        float scaleX = widthPx / (float)(widthMm * dpi / 25.4m);
        float scaleY = heightPx / (float)(heightMm * dpi / 25.4m);

        float x = element.PositionX * scaleX;
        float y = element.PositionY * scaleY;

        switch (element.ElementType)
        {
            case LabelFieldType.Text:
            case LabelFieldType.Price:
                RenderText(canvas, element, x, y, scaleX, scaleY, data);
                break;
            case LabelFieldType.Barcode:
                RenderBarcode(canvas, element, x, y, scaleX, scaleY, data);
                break;
            case LabelFieldType.QRCode:
                RenderQrCode(canvas, element, x, y, scaleX, scaleY, data);
                break;
            case LabelFieldType.Box:
                RenderBox(canvas, element, x, y, scaleX, scaleY);
                break;
            case LabelFieldType.Line:
                RenderLine(canvas, element, x, y, scaleX, scaleY);
                break;
        }
    }

    private void RenderText(SKCanvas canvas, LabelElement element,
        float x, float y, float scaleX, float scaleY, ProductLabelDataDto? data)
    {
        var text = ReplacePlaceholders(element.Content, data);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = element.FontSize * scaleY,
            IsAntialias = true,
            Typeface = element.IsBold
                ? SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                : SKTypeface.FromFamilyName("Arial")
        };

        canvas.DrawText(text, x, y + paint.TextSize, paint);
    }

    private void RenderBarcode(SKCanvas canvas, LabelElement element,
        float x, float y, float scaleX, float scaleY, ProductLabelDataDto? data)
    {
        var barcodeValue = ReplacePlaceholders(element.Content, data);

        // Use BarcodeLib or ZXing to generate barcode image
        var barcodeWriter = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.EAN_13,
            Options = new EncodingOptions
            {
                Width = (int)(element.Width * scaleX),
                Height = (int)((element.BarcodeHeight ?? 50) * scaleY),
                PureBarcode = !(element.ShowBarcodeText ?? true)
            }
        };

        var pixels = barcodeWriter.Write(barcodeValue);
        // Convert to SKBitmap and draw
        // ...
    }
}
```

#### Option B: Labelary API Integration (Online)

```csharp
public class LabelaryPreviewService : ILabelPreviewService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://api.labelary.com/v1/printers";

    public async Task<byte[]> RenderZplViaLabelaryAsync(
        string zplContent, int widthDots, int heightDots)
    {
        // Convert dots to inches (Labelary uses inches)
        var widthInches = widthDots / 203.0; // Assume 203 DPI
        var heightInches = heightDots / 203.0;

        // Labelary API format: /v1/printers/{dpmm}dpmm/labels/{width}x{height}/{index}/
        // dpmm = dots per mm (8 for 203 DPI)
        var url = $"{BaseUrl}/8dpmm/labels/{widthInches:F2}x{heightInches:F2}/0/";

        using var content = new StringContent(zplContent, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Labelary API error: {response.StatusCode}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> RenderPreviewAsync(
        string templateContent,
        LabelPrintLanguageDto language,
        decimal widthMm,
        decimal heightMm,
        int dpi,
        ProductLabelDataDto? data = null)
    {
        if (language != LabelPrintLanguageDto.ZPL)
        {
            throw new NotSupportedException("Labelary only supports ZPL");
        }

        // Replace placeholders with sample data
        var content = data != null
            ? ReplacePlaceholders(templateContent, data)
            : templateContent;

        // Convert mm to dots
        var widthDots = (int)(widthMm * dpi / 25.4m);
        var heightDots = (int)(heightMm * dpi / 25.4m);

        return await RenderZplViaLabelaryAsync(content, widthDots, heightDots);
    }
}
```

### Preview Display Component

Update preview panels in template management and designer:

```xaml
<!-- In LabelTemplateDesignerView.xaml -->
<Border x:Name="PreviewBorder" Grid.Row="1" Grid.Column="1"
        Background="#FAFAFA" BorderBrush="#DDD" BorderThickness="1"
        Margin="10">
    <Grid>
        <!-- Loading indicator -->
        <ProgressBar IsIndeterminate="True"
                     Visibility="{Binding IsPreviewLoading, Converter={StaticResource BoolToVisibilityConverter}}"
                     Height="4" VerticalAlignment="Top"/>

        <!-- Preview image -->
        <Image Source="{Binding PreviewImage}"
               Stretch="Uniform"
               RenderOptions.BitmapScalingMode="HighQuality"
               Visibility="{Binding HasPreview, Converter={StaticResource BoolToVisibilityConverter}}"/>

        <!-- No preview message -->
        <TextBlock Text="Preview will appear here"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"
                   Foreground="#999"
                   Visibility="{Binding HasPreview, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>

        <!-- Error message -->
        <Border Background="#FEE" Padding="10" CornerRadius="4"
                Visibility="{Binding HasPreviewError, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel>
                <TextBlock Text="Preview failed" FontWeight="Bold" Foreground="#C00"/>
                <TextBlock Text="{Binding PreviewErrorMessage}" Foreground="#800" TextWrapping="Wrap"/>
            </StackPanel>
        </Border>
    </Grid>
</Border>
```

### ViewModel Updates

```csharp
public partial class LabelTemplateDesignerViewModel
{
    private readonly ILabelPreviewService _previewService;

    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private bool _isPreviewLoading;
    [ObservableProperty] private bool _hasPreviewError;
    [ObservableProperty] private string? _previewErrorMessage;

    public bool HasPreview => PreviewImage != null && !HasPreviewError;

    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        try
        {
            IsPreviewLoading = true;
            HasPreviewError = false;
            PreviewErrorMessage = null;

            var templateContent = GenerateTemplateContent();
            var sampleData = GetSampleProductData();

            var imageBytes = await _previewService.RenderPreviewAsync(
                templateContent,
                PrintLanguage,
                LabelSize.WidthMm,
                LabelSize.HeightMm,
                LabelSize.DotsPerMm * 25, // Convert to approximate DPI
                sampleData);

            PreviewImage = BytesToBitmapImage(imageBytes);
        }
        catch (Exception ex)
        {
            HasPreviewError = true;
            PreviewErrorMessage = ex.Message;
            PreviewImage = null;
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    private BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
```

## Acceptance Criteria

### Rendering
- [ ] Can render ZPL templates to PNG image
- [ ] Can render EPL templates to PNG image (local only)
- [ ] Placeholders replaced with sample data in preview
- [ ] Text renders at correct position and size
- [ ] Barcodes render as actual barcode images
- [ ] QR codes render correctly
- [ ] Boxes and lines render correctly
- [ ] Preview matches actual print output closely

### Performance
- [ ] Preview generates in < 2 seconds
- [ ] Preview caches to avoid regeneration
- [ ] Preview updates on template change (debounced)
- [ ] Loading indicator during generation

### Integration
- [ ] Preview shows in Template Management view
- [ ] Preview shows in Template Designer view
- [ ] Preview updates as designer elements change
- [ ] Preview shows in print dialog

### Error Handling
- [ ] Graceful fallback if Labelary API unavailable
- [ ] Clear error message if rendering fails
- [ ] Option to retry preview generation

## Technical Notes

### Dependencies for Local Rendering

```xml
<!-- In .csproj -->
<PackageReference Include="SkiaSharp" Version="2.88.7" />
<PackageReference Include="SkiaSharp.Views.WPF" Version="2.88.7" />
<PackageReference Include="ZXing.Net" Version="0.16.9" />
```

### Labelary API

- **URL**: `http://api.labelary.com/v1/printers/{dpmm}dpmm/labels/{width}x{height}/{index}/`
- **Method**: POST
- **Body**: Raw ZPL code
- **Response**: PNG image
- **Rate Limit**: Fair use (no hard limit)
- **Free**: Yes, for development and light production use

### Sample Data for Preview

```csharp
private ProductLabelDataDto GetSampleProductData()
{
    return new ProductLabelDataDto
    {
        ProductId = 1,
        ProductName = "Coca Cola 500ml",
        Barcode = "5449000000996",
        Price = 199.99m,
        UnitPrice = "KSh 0.40/ml",
        Description = "Carbonated soft drink",
        SKU = "BEV-CC-500",
        CategoryName = "Beverages",
        OriginalPrice = 249.99m,
        PromoText = "SPECIAL OFFER!",
        UnitOfMeasure = "ml",
        EffectiveDate = DateTime.Today
    };
}
```

### Barcode Rendering with ZXing

```csharp
private SKBitmap RenderBarcode(string value, BarcodeFormat format, int width, int height)
{
    var writer = new BarcodeWriterPixelData
    {
        Format = format,
        Options = new EncodingOptions
        {
            Width = width,
            Height = height,
            Margin = 2
        }
    };

    var pixelData = writer.Write(value);

    var bitmap = new SKBitmap(pixelData.Width, pixelData.Height);
    for (int y = 0; y < pixelData.Height; y++)
    {
        for (int x = 0; x < pixelData.Width; x++)
        {
            int i = (y * pixelData.Width + x) * 4;
            bitmap.SetPixel(x, y, new SKColor(
                pixelData.Pixels[i + 2],  // R
                pixelData.Pixels[i + 1],  // G
                pixelData.Pixels[i],      // B
                pixelData.Pixels[i + 3]   // A
            ));
        }
    }

    return bitmap;
}
```

## Test Cases

1. **Render ZPL Text** - Text at correct position
2. **Render ZPL Barcode** - EAN-13 barcode renders
3. **Render EPL** - Local rendering works
4. **Placeholder Replacement** - {{Price}} shows "199.99"
5. **Multiple Elements** - Complex template renders
6. **Labelary Fallback** - Uses local when API down
7. **Performance** - < 2 second render time
8. **Cache** - Same template doesn't re-render
9. **Error Display** - Invalid template shows error
10. **Dimensions** - Output matches label dimensions

## Dependencies

- Issue #012: Visual Template Designer (preview panel)

## Blocks

- None

## Estimated Complexity

**Medium-High** - Graphics rendering, API integration, barcode generation
