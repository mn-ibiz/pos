using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Label Size Configuration.
/// Manages standard and custom label sizes with size calculator.
/// </summary>
public partial class LabelSizeConfigurationViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;

    private const decimal MmToInch = 25.4m;

    #region Observable Properties - Collections

    [ObservableProperty]
    private ObservableCollection<LabelSizeDisplayDto> _standardSizes = new();

    [ObservableProperty]
    private ObservableCollection<LabelSizeDisplayDto> _customSizes = new();

    [ObservableProperty]
    private LabelSizeDisplayDto? _selectedSize;

    #endregion

    #region Observable Properties - Size Calculator

    [ObservableProperty]
    private decimal _calcWidthMm = 38;

    [ObservableProperty]
    private decimal _calcHeightMm = 25;

    [ObservableProperty]
    private int _calcDpi = 203;

    // Computed values
    public decimal CalcWidthInches => Math.Round(CalcWidthMm / MmToInch, 2);
    public decimal CalcHeightInches => Math.Round(CalcHeightMm / MmToInch, 2);
    public int CalcWidthDots => (int)Math.Round(CalcWidthMm * CalcDpi / MmToInch);
    public int CalcHeightDots => (int)Math.Round(CalcHeightMm * CalcDpi / MmToInch);
    public int CalcTotalDots => CalcWidthDots * CalcHeightDots;

    public int[] DpiPresets { get; } = { 203, 300, 600 };

    #endregion

    #region Observable Properties - Form

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private int _editingSizeId;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private decimal _formWidthMm = 50;

    [ObservableProperty]
    private decimal _formHeightMm = 25;

    [ObservableProperty]
    private int _formDpi = 203;

    [ObservableProperty]
    private string _formDescription = string.Empty;

    // Form computed values
    public decimal FormWidthInches => Math.Round(FormWidthMm / MmToInch, 2);
    public decimal FormHeightInches => Math.Round(FormHeightMm / MmToInch, 2);

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    #endregion

    // Standard sizes that should be pre-populated
    private readonly List<(string Name, decimal Width, decimal Height, int Dpi, string Description)> _builtInSizes = new()
    {
        ("Small Barcode", 25, 25, 203, "1\" x 1\" - Barcode-only labels"),
        ("Standard Shelf", 38, 25, 203, "1.5\" x 1\" - Standard shelf labels"),
        ("Wide Shelf", 50, 25, 203, "2\" x 1\" - Wide shelf labels"),
        ("Price Tag", 50, 30, 203, "2\" x 1.2\" - Price tags"),
        ("Promo Label", 60, 40, 203, "2.4\" x 1.6\" - Promotional labels")
    };

    public LabelSizeConfigurationViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _logger.Information("LabelSizeConfigurationViewModel initialized");
    }

    partial void OnCalcWidthMmChanged(decimal value)
    {
        OnPropertyChanged(nameof(CalcWidthInches));
        OnPropertyChanged(nameof(CalcWidthDots));
        OnPropertyChanged(nameof(CalcTotalDots));
    }

    partial void OnCalcHeightMmChanged(decimal value)
    {
        OnPropertyChanged(nameof(CalcHeightInches));
        OnPropertyChanged(nameof(CalcHeightDots));
        OnPropertyChanged(nameof(CalcTotalDots));
    }

    partial void OnCalcDpiChanged(int value)
    {
        OnPropertyChanged(nameof(CalcWidthDots));
        OnPropertyChanged(nameof(CalcHeightDots));
        OnPropertyChanged(nameof(CalcTotalDots));
    }

    partial void OnFormWidthMmChanged(decimal value) => OnPropertyChanged(nameof(FormWidthInches));
    partial void OnFormHeightMmChanged(decimal value) => OnPropertyChanged(nameof(FormHeightInches));

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
            StatusMessage = "Loading label sizes...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetService<ILabelPrinterService>();

            if (printerService != null)
            {
                var sizes = await printerService.GetAllLabelSizesAsync();

                // Separate standard and custom sizes
                var standardNames = _builtInSizes.Select(b => b.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var standard = new List<LabelSizeDisplayDto>();
                var custom = new List<LabelSizeDisplayDto>();

                foreach (var size in sizes)
                {
                    var dto = new LabelSizeDisplayDto
                    {
                        Id = size.Id,
                        Name = size.Name,
                        WidthMm = size.WidthMm,
                        HeightMm = size.HeightMm,
                        DotsPerMm = size.DotsPerMm,
                        Description = size.Description,
                        TemplateCount = size.TemplateCount,
                        IsBuiltIn = standardNames.Contains(size.Name),
                        WidthInches = Math.Round(size.WidthMm / MmToInch, 2),
                        HeightInches = Math.Round(size.HeightMm / MmToInch, 2)
                    };

                    if (dto.IsBuiltIn)
                        standard.Add(dto);
                    else
                        custom.Add(dto);
                }

                // Check for missing built-in sizes and create them
                foreach (var builtIn in _builtInSizes)
                {
                    if (!standard.Any(s => s.Name.Equals(builtIn.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Create the missing built-in size
                        var createDto = new CreateLabelSizeDto
                        {
                            Name = builtIn.Name,
                            WidthMm = builtIn.Width,
                            HeightMm = builtIn.Height,
                            DotsPerMm = (int)(builtIn.Dpi / MmToInch * 10) / 10, // Convert DPI to dots/mm
                            Description = builtIn.Description
                        };

                        try
                        {
                            var created = await printerService.CreateLabelSizeAsync(createDto);
                            standard.Add(new LabelSizeDisplayDto
                            {
                                Id = created.Id,
                                Name = created.Name,
                                WidthMm = created.WidthMm,
                                HeightMm = created.HeightMm,
                                DotsPerMm = created.DotsPerMm,
                                Description = created.Description,
                                TemplateCount = 0,
                                IsBuiltIn = true,
                                WidthInches = Math.Round(created.WidthMm / MmToInch, 2),
                                HeightInches = Math.Round(created.HeightMm / MmToInch, 2)
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to create built-in size {Name}", builtIn.Name);
                        }
                    }
                }

                StandardSizes = new ObservableCollection<LabelSizeDisplayDto>(standard.OrderBy(s => s.WidthMm).ThenBy(s => s.HeightMm));
                CustomSizes = new ObservableCollection<LabelSizeDisplayDto>(custom.OrderBy(s => s.Name));
            }

            StatusMessage = $"Loaded {StandardSizes.Count} standard sizes, {CustomSizes.Count} custom sizes";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load label sizes");
            StatusMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RefreshData()
    {
        _ = LoadDataAsync();
    }

    #endregion

    #region Size CRUD

    [RelayCommand]
    private void ShowAddForm()
    {
        IsEditMode = false;
        EditingSizeId = 0;
        FormName = string.Empty;
        FormWidthMm = 50;
        FormHeightMm = 25;
        FormDpi = 203;
        FormDescription = string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void ShowEditForm(LabelSizeDisplayDto? size)
    {
        if (size == null || size.IsBuiltIn) return;

        IsEditMode = true;
        EditingSizeId = size.Id;
        FormName = size.Name;
        FormWidthMm = size.WidthMm;
        FormHeightMm = size.HeightMm;
        FormDpi = (int)(size.DotsPerMm * MmToInch / 10) * 10; // Convert dots/mm to DPI (approximate)
        FormDescription = size.Description ?? string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveSizeAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a size name.");
            return;
        }

        if (FormWidthMm <= 0 || FormHeightMm <= 0)
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Width and height must be greater than zero.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = IsEditMode ? "Updating size..." : "Creating size...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            // Convert DPI to dots per mm
            int dotsPerMm = (int)Math.Round(FormDpi / MmToInch);

            if (IsEditMode)
            {
                var dto = new UpdateLabelSizeDto
                {
                    Name = FormName,
                    WidthMm = FormWidthMm,
                    HeightMm = FormHeightMm,
                    DotsPerMm = dotsPerMm,
                    Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription
                };

                await printerService.UpdateLabelSizeAsync(EditingSizeId, dto);
                StatusMessage = "Size updated successfully";
            }
            else
            {
                var dto = new CreateLabelSizeDto
                {
                    Name = FormName,
                    WidthMm = FormWidthMm,
                    HeightMm = FormHeightMm,
                    DotsPerMm = dotsPerMm,
                    Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription
                };

                await printerService.CreateLabelSizeAsync(dto);
                StatusMessage = "Size created successfully";
            }

            IsFormVisible = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save label size");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to save size: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSizeAsync(LabelSizeDisplayDto? size)
    {
        if (size == null) return;

        if (size.IsBuiltIn)
        {
            await _dialogService.ShowMessageAsync("Cannot Delete", "Built-in standard sizes cannot be deleted.");
            return;
        }

        if (size.TemplateCount > 0)
        {
            await _dialogService.ShowMessageAsync("Cannot Delete",
                $"This size is used by {size.TemplateCount} template(s). Please remove or reassign those templates first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Size",
            $"Are you sure you want to delete '{size.Name}'?");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting size...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            await printerService.DeleteLabelSizeAsync(size.Id);

            CustomSizes.Remove(size);
            StatusMessage = "Size deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete size {SizeId}", size.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to delete size: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Calculator Commands

    [RelayCommand]
    private void ApplyToCalculator(LabelSizeDisplayDto? size)
    {
        if (size == null) return;

        CalcWidthMm = size.WidthMm;
        CalcHeightMm = size.HeightMm;
        CalcDpi = (int)(size.DotsPerMm * MmToInch / 10) * 10; // Approximate DPI
    }

    #endregion

    #region Navigation

    [RelayCommand]
    private void GoBack()
    {
        // Navigate back
    }

    #endregion
}

/// <summary>
/// Display DTO for label sizes with additional computed properties.
/// </summary>
public class LabelSizeDisplayDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int DotsPerMm { get; set; }
    public string? Description { get; set; }
    public int TemplateCount { get; set; }
    public bool IsBuiltIn { get; set; }
    public decimal WidthInches { get; set; }
    public decimal HeightInches { get; set; }

    public string DimensionsDisplay => $"{WidthMm:N0} x {HeightMm:N0} mm ({WidthInches:N2}\" x {HeightInches:N2}\")";
    public int ApproxDpi => (int)(DotsPerMm * 25.4m / 10) * 10;
}
