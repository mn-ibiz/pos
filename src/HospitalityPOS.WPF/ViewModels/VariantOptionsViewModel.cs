using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing product variant options (Size, Color, Flavor, etc.).
/// </summary>
public partial class VariantOptionsViewModel : ViewModelBase, INavigationAware
{
    private readonly IProductVariantService _variantService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of variant options.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<VariantOption> _variantOptions = [];

    /// <summary>
    /// Gets or sets the selected variant option.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditVariantOptionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVariantOptionCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddValueCommand))]
    private VariantOption? _selectedVariantOption;

    /// <summary>
    /// Gets or sets the total count of variant options.
    /// </summary>
    [ObservableProperty]
    private int _totalOptionsCount;

    /// <summary>
    /// Gets or sets the count of active variant options.
    /// </summary>
    [ObservableProperty]
    private int _activeOptionsCount;

    /// <summary>
    /// Gets or sets the total count of variant values across all options.
    /// </summary>
    [ObservableProperty]
    private int _totalValuesCount;

    /// <summary>
    /// Gets or sets the count of products using variants.
    /// </summary>
    [ObservableProperty]
    private int _productsUsingVariantsCount;

    /// <summary>
    /// Gets or sets the search text for filtering.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    #endregion

    private IReadOnlyList<VariantOption>? _allOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariantOptionsViewModel"/> class.
    /// </summary>
    public VariantOptionsViewModel(
        ILogger logger,
        IProductVariantService variantService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _variantService = variantService ?? throw new ArgumentNullException(nameof(variantService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Variant Options Management";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = FilterOptionsAsync(value);
    }

    private async Task FilterOptionsAsync(string searchText)
    {
        if (_allOptions == null)
        {
            _allOptions = await _variantService.GetAllVariantOptionsAsync();
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            VariantOptions = new ObservableCollection<VariantOption>(_allOptions);
        }
        else
        {
            var filtered = _allOptions
                .Where(o => o.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           (o.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            VariantOptions = new ObservableCollection<VariantOption>(filtered);
        }
    }

    #region Commands

    /// <summary>
    /// Loads all variant options.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            _allOptions = await _variantService.GetAllVariantOptionsAsync();
            VariantOptions = new ObservableCollection<VariantOption>(_allOptions);

            // Update counts
            TotalOptionsCount = _allOptions.Count;
            ActiveOptionsCount = _allOptions.Count(o => o.IsActive);
            TotalValuesCount = _allOptions.Sum(o => o.Values.Count);

            // Get products using variants count
            var variantStats = await _variantService.GetVariantStatisticsAsync();
            ProductsUsingVariantsCount = variantStats.ProductsWithVariants;

            _logger.Debug("Loaded {OptionsCount} variant options with {ValuesCount} total values",
                TotalOptionsCount, TotalValuesCount);
        }, "Loading variant options...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates a new variant option.
    /// </summary>
    [RelayCommand]
    private async Task CreateVariantOptionAsync()
    {
        if (!RequirePermission(PermissionNames.Products.Manage, "create variant options"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to create variant options.");
            return;
        }

        var result = await _dialogService.ShowVariantOptionEditorDialogAsync(null);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new VariantOptionDto
                {
                    Name = result.Name,
                    DisplayName = result.DisplayName,
                    OptionType = result.OptionType,
                    Description = result.Description,
                    DisplayOrder = result.DisplayOrder,
                    IsGlobal = result.IsGlobal,
                    Values = result.Values.Select(v => new VariantOptionValueDto
                    {
                        Value = v.Value,
                        DisplayName = v.DisplayName,
                        ColorCode = v.ColorCode,
                        PriceAdjustment = v.PriceAdjustment,
                        IsPriceAdjustmentPercent = v.IsPriceAdjustmentPercent,
                        DisplayOrder = v.DisplayOrder,
                        SkuSuffix = v.SkuSuffix
                    }).ToList()
                };

                await _variantService.CreateVariantOptionAsync(dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Variant option '{result.Name}' has been created.");
                await LoadDataAsync();
            }, "Creating variant option...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Edits the selected variant option.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditVariantOption))]
    private async Task EditVariantOptionAsync()
    {
        if (SelectedVariantOption is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "edit variant options"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to edit variant options.");
            return;
        }

        var result = await _dialogService.ShowVariantOptionEditorDialogAsync(SelectedVariantOption);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new VariantOptionDto
                {
                    Name = result.Name,
                    DisplayName = result.DisplayName,
                    OptionType = result.OptionType,
                    Description = result.Description,
                    DisplayOrder = result.DisplayOrder,
                    IsGlobal = result.IsGlobal,
                    Values = result.Values.Select(v => new VariantOptionValueDto
                    {
                        Id = v.Id,
                        Value = v.Value,
                        DisplayName = v.DisplayName,
                        ColorCode = v.ColorCode,
                        PriceAdjustment = v.PriceAdjustment,
                        IsPriceAdjustmentPercent = v.IsPriceAdjustmentPercent,
                        DisplayOrder = v.DisplayOrder,
                        SkuSuffix = v.SkuSuffix
                    }).ToList()
                };

                await _variantService.UpdateVariantOptionAsync(SelectedVariantOption.Id, dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Variant option '{result.Name}' has been updated.");
                await LoadDataAsync();
            }, "Updating variant option...").ConfigureAwait(true);
        }
    }

    private bool CanEditVariantOption() => SelectedVariantOption is not null;

    /// <summary>
    /// Deletes the selected variant option.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteVariantOption))]
    private async Task DeleteVariantOptionAsync()
    {
        if (SelectedVariantOption is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "delete variant options"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to delete variant options.");
            return;
        }

        // Check if option is in use
        var usageCount = await _variantService.GetVariantOptionUsageCountAsync(SelectedVariantOption.Id);
        if (usageCount > 0)
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                $"This variant option is used by {usageCount} product(s). Please remove it from all products first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Variant Option",
            $"Are you sure you want to delete the variant option '{SelectedVariantOption.Name}'?\n\n" +
            $"This will also delete all {SelectedVariantOption.Values.Count} value(s) associated with it.\n\n" +
            "This action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _variantService.DeleteVariantOptionAsync(SelectedVariantOption.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                await _dialogService.ShowMessageAsync(
                    "Variant Option Deleted",
                    $"Variant option '{SelectedVariantOption.Name}' has been deleted.");
                await LoadDataAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    "Delete Failed",
                    "Failed to delete the variant option. Please try again.");
            }
        }, "Deleting variant option...").ConfigureAwait(true);
    }

    private bool CanDeleteVariantOption() => SelectedVariantOption is not null;

    /// <summary>
    /// Adds a new value to the selected variant option.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddValue))]
    private async Task AddValueAsync()
    {
        if (SelectedVariantOption is null) return;

        if (!RequirePermission(PermissionNames.Products.Manage, "add variant values"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to add variant values.");
            return;
        }

        var result = await _dialogService.ShowVariantValueEditorDialogAsync(null, SelectedVariantOption);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new VariantOptionValueDto
                {
                    Value = result.Value,
                    DisplayName = result.DisplayName,
                    ColorCode = result.ColorCode,
                    PriceAdjustment = result.PriceAdjustment,
                    IsPriceAdjustmentPercent = result.IsPriceAdjustmentPercent,
                    DisplayOrder = result.DisplayOrder,
                    SkuSuffix = result.SkuSuffix
                };

                await _variantService.AddVariantOptionValueAsync(SelectedVariantOption.Id, dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Value '{result.Value}' has been added to '{SelectedVariantOption.Name}'.");
                await LoadDataAsync();
            }, "Adding variant value...").ConfigureAwait(true);
        }
    }

    private bool CanAddValue() => SelectedVariantOption is not null;

    /// <summary>
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}
