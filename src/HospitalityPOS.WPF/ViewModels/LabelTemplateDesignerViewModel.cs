using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Models;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Visual WYSIWYG Label Template Designer.
/// </summary>
public partial class LabelTemplateDesignerViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly LabelCodeGeneratorService _codeGenerator;

    #region Observable Properties - Template Info

    [ObservableProperty]
    private int _templateId;

    [ObservableProperty]
    private string _templateName = "New Template";

    [ObservableProperty]
    private LabelSizeDto? _labelSize;

    [ObservableProperty]
    private LabelPrintLanguageDto _printLanguage = LabelPrintLanguageDto.ZPL;

    [ObservableProperty]
    private ObservableCollection<LabelSizeDto> _labelSizes = new();

    public Array PrintLanguages => Enum.GetValues(typeof(LabelPrintLanguageDto));

    #endregion

    #region Observable Properties - Canvas State

    [ObservableProperty]
    private ObservableCollection<LabelDesignElement> _elements = new();

    [ObservableProperty]
    private LabelDesignElement? _selectedElement;

    [ObservableProperty]
    private double _canvasWidth = 300;

    [ObservableProperty]
    private double _canvasHeight = 200;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private int _gridSize = 8;

    #endregion

    #region Observable Properties - Code View

    [ObservableProperty]
    private bool _showCodeView;

    [ObservableProperty]
    private string _generatedCode = string.Empty;

    #endregion

    #region Observable Properties - Toolbox

    [ObservableProperty]
    private ObservableCollection<ToolboxItem> _toolboxItems = new();

    [ObservableProperty]
    private ToolboxItem? _selectedToolboxItem;

    #endregion

    #region Observable Properties - Placeholders

    [ObservableProperty]
    private ObservableCollection<string> _placeholders = new();

    [ObservableProperty]
    private string? _selectedPlaceholder;

    #endregion

    #region Observable Properties - Selected Element Properties

    [ObservableProperty]
    private double _elementX;

    [ObservableProperty]
    private double _elementY;

    [ObservableProperty]
    private double _elementWidth;

    [ObservableProperty]
    private double _elementHeight;

    [ObservableProperty]
    private string _elementContent = string.Empty;

    [ObservableProperty]
    private int _elementFontSize = 24;

    [ObservableProperty]
    private bool _elementIsBold;

    [ObservableProperty]
    private TextAlignment _elementTextAlignment = TextAlignment.Left;

    [ObservableProperty]
    private ElementRotation _elementRotation = ElementRotation.Rotate0;

    [ObservableProperty]
    private BarcodeType _elementBarcodeType = BarcodeType.EAN13;

    [ObservableProperty]
    private int _elementBarcodeHeight = 50;

    [ObservableProperty]
    private bool _elementShowBarcodeText = true;

    [ObservableProperty]
    private int _elementQrSize = 4;

    [ObservableProperty]
    private int _elementLineThickness = 2;

    public Array TextAlignments => Enum.GetValues(typeof(TextAlignment));
    public Array ElementRotations => Enum.GetValues(typeof(ElementRotation));
    public Array BarcodeTypes => Enum.GetValues(typeof(BarcodeType));

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    #endregion

    public double[] ZoomLevels { get; } = { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };

    public LabelTemplateDesignerViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _codeGenerator = new LabelCodeGeneratorService();

        // Initialize toolbox items
        ToolboxItems = new ObservableCollection<ToolboxItem>(ToolboxItem.GetAllItems());

        // Initialize placeholders
        InitializePlaceholders();

        _logger.Information("LabelTemplateDesignerViewModel initialized");
    }

    private void InitializePlaceholders()
    {
        Placeholders = new ObservableCollection<string>
        {
            "{{ProductName}}",
            "{{Barcode}}",
            "{{Price}}",
            "{{UnitPrice}}",
            "{{SKU}}",
            "{{CategoryName}}",
            "{{UnitOfMeasure}}",
            "{{Description}}",
            "{{Weight}}",
            "{{Volume}}",
            "{{ExpiryDate}}",
            "{{ProductionDate}}",
            "{{BatchNumber}}",
            "{{StoreName}}",
            "{{PrintDate}}"
        };
    }

    partial void OnSelectedElementChanged(LabelDesignElement? value)
    {
        if (value != null)
        {
            // Load element properties into the properties panel
            ElementX = value.X;
            ElementY = value.Y;
            ElementWidth = value.Width;
            ElementHeight = value.Height;
            ElementContent = value.Content;
            ElementFontSize = value.FontSize;
            ElementIsBold = value.IsBold;
            ElementTextAlignment = value.TextAlignment;
            ElementRotation = value.Rotation;
            ElementBarcodeType = value.BarcodeType;
            ElementBarcodeHeight = value.BarcodeHeight;
            ElementShowBarcodeText = value.ShowBarcodeText;
            ElementQrSize = value.QrSize;
            ElementLineThickness = value.LineThickness;
        }
    }

    partial void OnLabelSizeChanged(LabelSizeDto? value)
    {
        if (value != null)
        {
            UpdateCanvasSize();
        }
    }

    partial void OnZoomLevelChanged(double value)
    {
        UpdateCanvasSize();
    }

    partial void OnShowCodeViewChanged(bool value)
    {
        if (value)
        {
            GenerateCode();
        }
    }

    private void UpdateCanvasSize()
    {
        if (LabelSize == null) return;

        // Convert mm to dots at 203 DPI (default) then scale to pixels for display
        // Using a scale factor of 3 pixels per dot for good visibility
        const double pixelsPerMm = 8.0; // Display scale

        CanvasWidth = (double)LabelSize.WidthMm * pixelsPerMm * ZoomLevel;
        CanvasHeight = (double)LabelSize.HeightMm * pixelsPerMm * ZoomLevel;
    }

    #region INavigationAware

    public async Task OnNavigatedTo(object? parameter)
    {
        if (parameter is int templateId)
        {
            TemplateId = templateId;
            await LoadTemplateAsync(templateId);
        }
        else
        {
            // New template - load label sizes
            await LoadLabelSizesAsync();
        }
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Load Data

    private async Task LoadLabelSizesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetService<ILabelPrinterService>();

            if (printerService != null)
            {
                var sizes = await printerService.GetAllLabelSizesAsync();
                LabelSizes = new ObservableCollection<LabelSizeDto>(sizes);

                if (LabelSizes.Any())
                {
                    LabelSize = LabelSizes.First();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load label sizes");
        }
    }

    private async Task LoadTemplateAsync(int templateId)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading template...";

            await LoadLabelSizesAsync();

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            var template = await templateService.GetTemplateAsync(templateId);
            if (template == null)
            {
                await _dialogService.ShowMessageAsync("Error", "Template not found.");
                return;
            }

            TemplateId = template.Id;
            TemplateName = template.Name;
            PrintLanguage = template.PrintLanguage;

            // Set the label size
            LabelSize = LabelSizes.FirstOrDefault(s => s.Id == template.LabelSizeId);

            // Parse template content into elements
            if (!string.IsNullOrWhiteSpace(template.TemplateContent))
            {
                var parsedElements = _codeGenerator.ParseZPL(template.TemplateContent);
                Elements = new ObservableCollection<LabelDesignElement>(parsedElements);
            }

            HasUnsavedChanges = false;
            StatusMessage = $"Loaded: {template.Name}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load template {TemplateId}", templateId);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to load template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Element Operations

    [RelayCommand]
    private void AddElement(ToolboxItem? item)
    {
        if (item == null) return;

        var element = new LabelDesignElement(item.ElementType)
        {
            X = 20,
            Y = 20,
            ZIndex = Elements.Count
        };

        Elements.Add(element);
        SelectedElement = element;
        HasUnsavedChanges = true;

        StatusMessage = $"Added {item.Name} element";
    }

    [RelayCommand]
    private void DeleteSelectedElement()
    {
        if (SelectedElement == null) return;

        var element = SelectedElement;
        Elements.Remove(element);
        SelectedElement = null;
        HasUnsavedChanges = true;

        StatusMessage = $"Deleted {element.DisplayName}";
    }

    [RelayCommand]
    private void DuplicateSelectedElement()
    {
        if (SelectedElement == null) return;

        var clone = SelectedElement.Clone();
        clone.X += 20;
        clone.Y += 20;
        clone.ZIndex = Elements.Count;

        Elements.Add(clone);
        SelectedElement = clone;
        HasUnsavedChanges = true;

        StatusMessage = "Element duplicated";
    }

    [RelayCommand]
    private void BringToFront()
    {
        if (SelectedElement == null) return;

        var maxZ = Elements.Max(e => e.ZIndex);
        SelectedElement.ZIndex = maxZ + 1;
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void SendToBack()
    {
        if (SelectedElement == null) return;

        var minZ = Elements.Min(e => e.ZIndex);
        SelectedElement.ZIndex = minZ - 1;
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void ApplyProperties()
    {
        if (SelectedElement == null) return;

        SelectedElement.X = ElementX;
        SelectedElement.Y = ElementY;
        SelectedElement.Width = ElementWidth;
        SelectedElement.Height = ElementHeight;
        SelectedElement.Content = ElementContent;
        SelectedElement.FontSize = ElementFontSize;
        SelectedElement.IsBold = ElementIsBold;
        SelectedElement.TextAlignment = ElementTextAlignment;
        SelectedElement.Rotation = ElementRotation;
        SelectedElement.BarcodeType = ElementBarcodeType;
        SelectedElement.BarcodeHeight = ElementBarcodeHeight;
        SelectedElement.ShowBarcodeText = ElementShowBarcodeText;
        SelectedElement.QrSize = ElementQrSize;
        SelectedElement.LineThickness = ElementLineThickness;

        HasUnsavedChanges = true;

        // Regenerate code if code view is visible
        if (ShowCodeView)
        {
            GenerateCode();
        }

        StatusMessage = "Properties applied";
    }

    [RelayCommand]
    private void InsertPlaceholder(string? placeholder)
    {
        if (SelectedElement == null || placeholder == null) return;

        if (SelectedElement.ElementType == LabelElementType.Text ||
            SelectedElement.ElementType == LabelElementType.Price ||
            SelectedElement.ElementType == LabelElementType.Barcode ||
            SelectedElement.ElementType == LabelElementType.QRCode)
        {
            SelectedElement.Content = placeholder;
            ElementContent = placeholder;
            HasUnsavedChanges = true;
            StatusMessage = $"Inserted {placeholder}";
        }
    }

    public void MoveElement(LabelDesignElement element, double deltaX, double deltaY)
    {
        if (SnapToGrid)
        {
            deltaX = Math.Round(deltaX / GridSize) * GridSize;
            deltaY = Math.Round(deltaY / GridSize) * GridSize;
        }

        element.X = Math.Max(0, element.X + deltaX);
        element.Y = Math.Max(0, element.Y + deltaY);

        if (element == SelectedElement)
        {
            ElementX = element.X;
            ElementY = element.Y;
        }

        HasUnsavedChanges = true;
    }

    public void ResizeElement(LabelDesignElement element, double newWidth, double newHeight)
    {
        if (SnapToGrid)
        {
            newWidth = Math.Round(newWidth / GridSize) * GridSize;
            newHeight = Math.Round(newHeight / GridSize) * GridSize;
        }

        element.Width = Math.Max(10, newWidth);
        element.Height = Math.Max(10, newHeight);

        if (element == SelectedElement)
        {
            ElementWidth = element.Width;
            ElementHeight = element.Height;
        }

        HasUnsavedChanges = true;
    }

    public void SelectElement(LabelDesignElement? element)
    {
        // Deselect previous
        foreach (var e in Elements)
        {
            e.IsSelected = false;
        }

        SelectedElement = element;

        if (element != null)
        {
            element.IsSelected = true;
        }
    }

    #endregion

    #region Code Generation

    [RelayCommand]
    private void GenerateCode()
    {
        if (LabelSize == null)
        {
            GeneratedCode = "// Please select a label size";
            return;
        }

        GeneratedCode = _codeGenerator.GenerateCode(Elements, LabelSize, PrintLanguage);
    }

    [RelayCommand]
    private void ToggleCodeView()
    {
        ShowCodeView = !ShowCodeView;
    }

    #endregion

    #region Save/Load

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (LabelSize == null)
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please select a label size.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a template name.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving template...";

            var code = _codeGenerator.GenerateCode(Elements, LabelSize, PrintLanguage);

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            if (TemplateId > 0)
            {
                // Update existing template
                var updateDto = new UpdateLabelTemplateDto
                {
                    Name = TemplateName,
                    LabelSizeId = LabelSize.Id,
                    PrintLanguage = PrintLanguage,
                    TemplateContent = code
                };

                await templateService.UpdateTemplateAsync(TemplateId, updateDto);
                StatusMessage = "Template updated successfully";
            }
            else
            {
                // Create new template
                var createDto = new CreateLabelTemplateDto
                {
                    Name = TemplateName,
                    LabelSizeId = LabelSize.Id,
                    StoreId = 1, // TODO: Get actual store ID
                    PrintLanguage = PrintLanguage,
                    TemplateContent = code,
                    IsDefault = false,
                    IsPromoTemplate = false
                };

                var created = await templateService.CreateTemplateAsync(createDto);
                TemplateId = created.Id;
                StatusMessage = "Template created successfully";
            }

            HasUnsavedChanges = false;
            await _dialogService.ShowMessageAsync("Success", "Template saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save template");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to save template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseDesignerAsync()
    {
        if (HasUnsavedChanges)
        {
            var save = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                "You have unsaved changes. Do you want to save before closing?");

            if (save)
            {
                await SaveTemplateAsync();
            }
        }

        // Navigate back
        // This would be implemented when navigation service is available
    }

    #endregion

    #region Canvas Operations

    [RelayCommand]
    private void ZoomIn()
    {
        var currentIndex = Array.IndexOf(ZoomLevels, ZoomLevel);
        if (currentIndex < ZoomLevels.Length - 1)
        {
            ZoomLevel = ZoomLevels[currentIndex + 1];
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        var currentIndex = Array.IndexOf(ZoomLevels, ZoomLevel);
        if (currentIndex > 0)
        {
            ZoomLevel = ZoomLevels[currentIndex - 1];
        }
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
    }

    [RelayCommand]
    private void ToggleSnap()
    {
        SnapToGrid = !SnapToGrid;
    }

    [RelayCommand]
    private void ClearCanvas()
    {
        Elements.Clear();
        SelectedElement = null;
        HasUnsavedChanges = true;
        StatusMessage = "Canvas cleared";
    }

    #endregion
}
