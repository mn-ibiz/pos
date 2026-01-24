using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Label Template Management.
/// Provides full CRUD for templates with grouping by label size.
/// </summary>
public partial class LabelTemplateManagementViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    #region Observable Properties - Collections

    [ObservableProperty]
    private ObservableCollection<TemplateGroupViewModel> _templateGroups = new();

    [ObservableProperty]
    private ObservableCollection<LabelSizeDto> _labelSizes = new();

    [ObservableProperty]
    private ObservableCollection<LabelTemplateLibraryDto> _libraryTemplates = new();

    [ObservableProperty]
    private LabelTemplateDto? _selectedTemplate;

    [ObservableProperty]
    private LabelSizeDto? _selectedFilterSize;

    #endregion

    #region Observable Properties - Filters

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedTypeFilter = "All";

    public string[] TypeFilters { get; } = { "All", "Standard", "Promo" };

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isPreviewLoading;

    [ObservableProperty]
    private string? _previewImageBase64;

    [ObservableProperty]
    private string? _previewContent;

    [ObservableProperty]
    private List<string> _availablePlaceholders = new();

    #endregion

    #region Observable Properties - Create Template Form

    [ObservableProperty]
    private bool _isCreateFormVisible;

    [ObservableProperty]
    private string _newTemplateName = string.Empty;

    [ObservableProperty]
    private int? _newTemplateSizeId;

    [ObservableProperty]
    private LabelPrintLanguageDto _newTemplateLanguage = LabelPrintLanguageDto.ZPL;

    [ObservableProperty]
    private bool _newTemplateIsPromo;

    [ObservableProperty]
    private string _newTemplateDescription = string.Empty;

    [ObservableProperty]
    private string _createFromOption = "Blank";

    [ObservableProperty]
    private LabelTemplateLibraryDto? _selectedLibraryTemplate;

    [ObservableProperty]
    private LabelTemplateDto? _selectedSourceTemplate;

    public string[] CreateFromOptions { get; } = { "Blank", "Library", "Existing" };
    public Array PrintLanguages => Enum.GetValues(typeof(LabelPrintLanguageDto));

    #endregion

    #region Observable Properties - Import Library Form

    [ObservableProperty]
    private bool _isImportFormVisible;

    [ObservableProperty]
    private string _importTemplateName = string.Empty;

    #endregion

    #region Observable Properties - Import File Form

    [ObservableProperty]
    private bool _isImportFileFormVisible;

    [ObservableProperty]
    private string? _importFilePath;

    [ObservableProperty]
    private TemplateImportValidationResult? _importValidation;

    [ObservableProperty]
    private ConflictResolution _selectedConflictResolution = ConflictResolution.Rename;

    [ObservableProperty]
    private bool _importCreateMissingSize = true;

    public Array ConflictResolutions => Enum.GetValues(typeof(ConflictResolution));

    /// <summary>
    /// Returns true if the import validation shows NO matching size (needs to create one).
    /// </summary>
    public bool NeedsSizeCreation => ImportValidation != null && !ImportValidation.HasMatchingSize;

    #endregion

    private List<LabelTemplateDto> _allTemplates = new();

    public LabelTemplateManagementViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        _logger.Information("LabelTemplateManagementViewModel initialized");
    }

    partial void OnSearchTextChanged(string value) => FilterTemplates();
    partial void OnSelectedFilterSizeChanged(LabelSizeDto? value) => FilterTemplates();
    partial void OnSelectedTypeFilterChanged(string value) => FilterTemplates();
    partial void OnImportValidationChanged(TemplateImportValidationResult? value) => OnPropertyChanged(nameof(NeedsSizeCreation));

    partial void OnSelectedTemplateChanged(LabelTemplateDto? value)
    {
        if (value != null)
        {
            _ = GeneratePreviewAsync();
        }
        else
        {
            PreviewImageBase64 = null;
            PreviewContent = null;
        }
    }

    #region INavigationAware

    public Task OnNavigatedTo(object? parameter)
    {
        return LoadDataAsync();
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Load Data

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading templates...";

            using var scope = _scopeFactory.CreateScope();

            // Load label sizes
            var printerService = scope.ServiceProvider.GetService<ILabelPrinterService>();
            if (printerService != null)
            {
                var sizes = await printerService.GetAllLabelSizesAsync();
                LabelSizes = new ObservableCollection<LabelSizeDto>(sizes);
            }

            // Load templates
            var templateService = scope.ServiceProvider.GetService<ILabelTemplateService>();
            if (templateService != null)
            {
                _allTemplates = await templateService.GetAllTemplatesAsync(1); // TODO: Get actual store ID
                AvailablePlaceholders = templateService.GetAvailablePlaceholders();
            }

            FilterTemplates();

            StatusMessage = $"Loaded {_allTemplates.Count} templates";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load templates");
            StatusMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterTemplates()
    {
        var filtered = _allTemplates.AsEnumerable();

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t => t.Name.ToLowerInvariant().Contains(search) ||
                                           (t.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        // Filter by size
        if (SelectedFilterSize != null)
        {
            filtered = filtered.Where(t => t.LabelSizeId == SelectedFilterSize.Id);
        }

        // Filter by type
        if (SelectedTypeFilter == "Promo")
        {
            filtered = filtered.Where(t => t.IsPromoTemplate);
        }
        else if (SelectedTypeFilter == "Standard")
        {
            filtered = filtered.Where(t => !t.IsPromoTemplate);
        }

        // Group by label size
        var groups = filtered
            .GroupBy(t => t.LabelSizeId)
            .Select(g => new TemplateGroupViewModel
            {
                LabelSizeId = g.Key,
                LabelSizeName = g.First().LabelSizeName ?? $"Size {g.Key}",
                Templates = new ObservableCollection<LabelTemplateDto>(g.OrderBy(t => t.Name)),
                IsExpanded = true
            })
            .OrderBy(g => g.LabelSizeName)
            .ToList();

        TemplateGroups = new ObservableCollection<TemplateGroupViewModel>(groups);
    }

    [RelayCommand]
    private void RefreshData()
    {
        _ = LoadDataAsync();
    }

    #endregion

    #region Template CRUD

    [RelayCommand]
    private void ShowCreateForm()
    {
        NewTemplateName = string.Empty;
        NewTemplateSizeId = LabelSizes.FirstOrDefault()?.Id;
        NewTemplateLanguage = LabelPrintLanguageDto.ZPL;
        NewTemplateIsPromo = false;
        NewTemplateDescription = string.Empty;
        CreateFromOption = "Blank";
        SelectedLibraryTemplate = null;
        SelectedSourceTemplate = null;
        IsCreateFormVisible = true;
    }

    [RelayCommand]
    private void CancelCreateForm()
    {
        IsCreateFormVisible = false;
    }

    [RelayCommand]
    private async Task CreateTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTemplateName))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a template name.");
            return;
        }

        if (NewTemplateSizeId == null)
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please select a label size.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Creating template...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            string templateContent = string.Empty;

            if (CreateFromOption == "Library" && SelectedLibraryTemplate != null)
            {
                // Import from library
                var importDto = new ImportTemplateFromLibraryDto
                {
                    LibraryTemplateId = SelectedLibraryTemplate.Id,
                    Name = NewTemplateName,
                    LabelSizeId = NewTemplateSizeId.Value,
                    StoreId = 1 // TODO: Get actual store ID
                };
                await templateService.ImportFromLibraryAsync(importDto);
            }
            else if (CreateFromOption == "Existing" && SelectedSourceTemplate != null)
            {
                // Duplicate existing
                await templateService.DuplicateTemplateAsync(SelectedSourceTemplate.Id, NewTemplateName);
            }
            else
            {
                // Create blank
                var dto = new CreateLabelTemplateDto
                {
                    Name = NewTemplateName,
                    LabelSizeId = NewTemplateSizeId.Value,
                    StoreId = 1, // TODO: Get actual store ID
                    PrintLanguage = NewTemplateLanguage,
                    TemplateContent = GetBlankTemplateContent(NewTemplateLanguage),
                    IsDefault = false,
                    IsPromoTemplate = NewTemplateIsPromo,
                    Description = string.IsNullOrWhiteSpace(NewTemplateDescription) ? null : NewTemplateDescription
                };
                await templateService.CreateTemplateAsync(dto);
            }

            IsCreateFormVisible = false;
            await LoadDataAsync();
            StatusMessage = "Template created successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create template");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to create template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetBlankTemplateContent(LabelPrintLanguageDto language)
    {
        return language switch
        {
            LabelPrintLanguageDto.ZPL => "^XA\n^FO50,50^A0N,30,30^FD{{ProductName}}^FS\n^FO50,100^BY2^BCN,50,Y,N,N^FD{{Barcode}}^FS\n^FO50,170^A0N,25,25^FDKSh {{Price}}^FS\n^XZ",
            LabelPrintLanguageDto.EPL => "N\nA50,50,0,4,1,1,N,\"{{ProductName}}\"\nB50,100,0,1,2,2,50,N,\"{{Barcode}}\"\nA50,170,0,3,1,1,N,\"KSh {{Price}}\"\nP1",
            LabelPrintLanguageDto.TSPL => "SIZE 38 mm, 25 mm\nCLS\nTEXT 50,50,\"3\",0,1,1,\"{{ProductName}}\"\nBARCODE 50,100,\"128\",50,1,0,2,2,\"{{Barcode}}\"\nTEXT 50,170,\"2\",0,1,1,\"KSh {{Price}}\"\nPRINT 1",
            _ => "{{ProductName}}\n{{Barcode}}\n{{Price}}"
        };
    }

    [RelayCommand]
    private async Task DuplicateTemplateAsync(LabelTemplateDto? template)
    {
        if (template == null) return;

        var newName = await _dialogService.ShowInputDialogAsync(
            "Duplicate Template",
            "Enter a name for the duplicated template:",
            $"{template.Name} (Copy)");

        if (string.IsNullOrWhiteSpace(newName)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Duplicating template...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            await templateService.DuplicateTemplateAsync(template.Id, newName);

            await LoadDataAsync();
            StatusMessage = "Template duplicated successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to duplicate template {TemplateId}", template.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to duplicate template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(LabelTemplateDto? template)
    {
        if (template == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Template",
            $"Are you sure you want to delete the template '{template.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting template...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            await templateService.DeleteTemplateAsync(template.Id);

            _allTemplates.RemoveAll(t => t.Id == template.Id);
            FilterTemplates();
            StatusMessage = "Template deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete template {TemplateId}", template.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to delete template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SetDefaultTemplateAsync(LabelTemplateDto? template)
    {
        if (template == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Setting '{template.Name}' as default...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            await templateService.SetDefaultTemplateAsync(template.Id, 1); // TODO: Get actual store ID

            await LoadDataAsync();
            StatusMessage = $"'{template.Name}' is now the default template";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set default template {TemplateId}", template.Id);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditTemplate(LabelTemplateDto? template)
    {
        if (template == null) return;

        // Navigate to visual template designer with template ID
        _navigationService.NavigateTo<LabelTemplateDesignerViewModel>(template.Id);
        _logger.Information("Navigating to template designer for template {TemplateId}", template.Id);
    }

    [RelayCommand]
    private async Task ExportTemplateAsync(LabelTemplateDto? template)
    {
        if (template == null) return;

        try
        {
            // Show save file dialog
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Label Template",
                Filter = "Label Template Files (*.lbt)|*.lbt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".lbt",
                FileName = $"{template.Name.Replace(" ", "_")}.lbt"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsLoading = true;
            StatusMessage = $"Exporting '{template.Name}'...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            var options = new TemplateExportOptionsDto
            {
                IncludeFields = true,
                IncludeSizeDefinition = true,
                PrettyPrint = true
            };

            var exportData = await templateService.ExportTemplateAsync(template.Id, options);

            await File.WriteAllBytesAsync(saveDialog.FileName, exportData);

            StatusMessage = $"Template exported to {Path.GetFileName(saveDialog.FileName)}";
            _logger.Information("Template {TemplateId} exported to {FilePath}", template.Id, saveDialog.FileName);

            await _dialogService.ShowMessageAsync("Export Complete",
                $"Template '{template.Name}' has been exported successfully.\n\nFile: {saveDialog.FileName}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export template {TemplateId}", template.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Export Failed", $"Failed to export template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Import from File

    [RelayCommand]
    private async Task ShowImportFileDialogAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Import Label Template",
            Filter = "Label Template Files (*.lbt)|*.lbt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".lbt"
        };

        if (openDialog.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Validating import file...";
            ImportFilePath = openDialog.FileName;

            var fileData = await File.ReadAllBytesAsync(openDialog.FileName);

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            ImportValidation = await templateService.ValidateImportFileAsync(fileData, 1); // TODO: Get actual store ID

            if (ImportValidation.Errors.Any())
            {
                var errorMsg = string.Join("\n", ImportValidation.Errors);
                await _dialogService.ShowMessageAsync("Validation Errors",
                    $"The import file has the following errors:\n\n{errorMsg}");
                return;
            }

            SelectedConflictResolution = ImportValidation.HasNameConflict
                ? ConflictResolution.Rename
                : ConflictResolution.Skip;
            ImportCreateMissingSize = !ImportValidation.HasMatchingSize;

            IsImportFileFormVisible = true;
            StatusMessage = ImportValidation.IsValid ? "File validated successfully" : "Validation completed with warnings";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate import file {FilePath}", openDialog.FileName);
            await _dialogService.ShowMessageAsync("Validation Failed", $"Failed to validate file: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelImportFileForm()
    {
        IsImportFileFormVisible = false;
        ImportFilePath = null;
        ImportValidation = null;
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        if (string.IsNullOrEmpty(ImportFilePath) || ImportValidation == null)
        {
            await _dialogService.ShowMessageAsync("Error", "No file selected for import.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Importing template...";

            var fileData = await File.ReadAllBytesAsync(ImportFilePath);

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            var options = new TemplateImportOptionsDto
            {
                StoreId = 1, // TODO: Get actual store ID
                ConflictResolution = SelectedConflictResolution,
                CreateMissingSize = ImportCreateMissingSize
            };

            var result = await templateService.ImportTemplateAsync(fileData, options);

            if (result.Success)
            {
                IsImportFileFormVisible = false;
                ImportFilePath = null;
                ImportValidation = null;

                await LoadDataAsync();
                StatusMessage = $"Template '{result.Template?.Name}' imported successfully";

                var message = $"Template imported successfully!\n\nName: {result.Template?.Name}";
                if (result.SizeCreated)
                {
                    message += $"\n\nA new label size '{result.SizeName}' was also created.";
                }
                await _dialogService.ShowMessageAsync("Import Complete", message);
            }
            else
            {
                var errorMsg = string.Join("\n", result.Errors);
                await _dialogService.ShowMessageAsync("Import Failed",
                    $"Failed to import template:\n\n{errorMsg}");
                StatusMessage = "Import failed";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to import template from {FilePath}", ImportFilePath);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Import Failed", $"Failed to import template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Import from Library

    [RelayCommand]
    private async Task ShowImportFormAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading template library...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            var library = await templateService.GetLibraryTemplatesAsync();
            LibraryTemplates = new ObservableCollection<LabelTemplateLibraryDto>(library);

            IsImportFormVisible = true;
            StatusMessage = $"Loaded {library.Count} library templates";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load library templates");
            await _dialogService.ShowMessageAsync("Error", $"Failed to load library: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelImportForm()
    {
        IsImportFormVisible = false;
    }

    [RelayCommand]
    private async Task ImportFromLibraryAsync()
    {
        if (SelectedLibraryTemplate == null)
        {
            await _dialogService.ShowMessageAsync("Selection Required", "Please select a template from the library.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ImportTemplateName))
        {
            ImportTemplateName = SelectedLibraryTemplate.Name;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Importing template...";

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            // Find or create matching label size
            var matchingSize = LabelSizes.FirstOrDefault(s =>
                Math.Abs(s.WidthMm - SelectedLibraryTemplate.WidthMm) < 0.5m &&
                Math.Abs(s.HeightMm - SelectedLibraryTemplate.HeightMm) < 0.5m);

            if (matchingSize == null)
            {
                await _dialogService.ShowMessageAsync("Size Mismatch",
                    $"No matching label size found for {SelectedLibraryTemplate.WidthMm}x{SelectedLibraryTemplate.HeightMm}mm. Please create the label size first.");
                return;
            }

            var dto = new ImportTemplateFromLibraryDto
            {
                LibraryTemplateId = SelectedLibraryTemplate.Id,
                Name = ImportTemplateName,
                LabelSizeId = matchingSize.Id,
                StoreId = 1 // TODO: Get actual store ID
            };

            await templateService.ImportFromLibraryAsync(dto);

            IsImportFormVisible = false;
            await LoadDataAsync();
            StatusMessage = "Template imported successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to import template from library");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to import template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Preview

    private async Task GeneratePreviewAsync()
    {
        if (SelectedTemplate == null) return;

        try
        {
            IsPreviewLoading = true;

            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ILabelTemplateService>();

            var sampleData = new ProductLabelDataDto
            {
                ProductId = 1,
                ProductName = "Coca Cola 500ml",
                Barcode = "5901234123457",
                Price = 199.99m,
                UnitPrice = "KSh 0.40/ml",
                SKU = "COC-500",
                CategoryName = "Beverages",
                UnitOfMeasure = "ml"
            };

            // First try to render a visual preview using the preview service
            var previewService = scope.ServiceProvider.GetService<ILabelPreviewService>();
            if (previewService != null)
            {
                try
                {
                    var imageBytes = await previewService.RenderPreviewAsync(SelectedTemplate.Id, sampleData);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        PreviewImageBase64 = Convert.ToBase64String(imageBytes);
                        PreviewContent = null; // Clear text preview when we have an image
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Visual preview failed, falling back to text preview");
                }
            }

            // Fall back to text-based preview
            var request = new LabelPreviewRequestDto
            {
                TemplateId = SelectedTemplate.Id,
                SampleData = sampleData
            };

            var result = await templateService.GeneratePreviewAsync(request);

            if (result.Success)
            {
                PreviewImageBase64 = result.PreviewImageBase64;
                PreviewContent = result.LabelContent;
            }
            else
            {
                PreviewContent = $"Preview Error: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate preview");
            PreviewContent = $"Error: {ex.Message}";
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    #endregion

    #region Navigation

    [RelayCommand]
    private void GoBack()
    {
        // Navigate back to previous view
    }

    #endregion
}

/// <summary>
/// Represents a group of templates by label size.
/// </summary>
public partial class TemplateGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private int _labelSizeId;

    [ObservableProperty]
    private string _labelSizeName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LabelTemplateDto> _templates = new();

    [ObservableProperty]
    private bool _isExpanded = true;

    public int TemplateCount => Templates.Count;
}
