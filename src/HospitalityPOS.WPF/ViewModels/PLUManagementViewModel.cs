using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for PLU code management.
/// </summary>
public partial class PLUManagementViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // PLU List
    [ObservableProperty]
    private ObservableCollection<PLUCode> _pluCodes = [];

    [ObservableProperty]
    private PLUCode? _selectedPLU;

    // Filter
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showActiveOnly = true;

    // Products for selection
    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    [ObservableProperty]
    private Product? _selectedProduct;

    // Edit Form
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private int _editId;

    [ObservableProperty]
    private string _editCode = string.Empty;

    [ObservableProperty]
    private int? _editProductId;

    [ObservableProperty]
    private string? _editDisplayName;

    [ObservableProperty]
    private bool _editIsWeighted;

    [ObservableProperty]
    private decimal? _editTareWeight;

    [ObservableProperty]
    private int _editSortOrder;

    [ObservableProperty]
    private bool _editIsActive = true;

    public PLUManagementViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            var allPLUs = await barcodeService.GetAllPLUCodesAsync(!ShowActiveOnly);
            PLUCodes = new ObservableCollection<PLUCode>(FilterPLUs(allPLUs));

            var products = await productService.GetAllProductsAsync();
            Products = new ObservableCollection<Product>(products);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IEnumerable<PLUCode> FilterPLUs(IEnumerable<PLUCode> pluCodes)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return pluCodes;

        var search = SearchText.ToLower();
        return pluCodes.Where(p =>
            p.Code.ToLower().Contains(search) ||
            (p.DisplayName?.ToLower().Contains(search) ?? false) ||
            (p.Product?.Name?.ToLower().Contains(search) ?? false));
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadDataAsync();
    }

    partial void OnShowActiveOnlyChanged(bool value)
    {
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task NewPLUAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

        EditId = 0;
        EditCode = await barcodeService.GenerateNextPLUCodeAsync();
        EditProductId = null;
        SelectedProduct = null;
        EditDisplayName = null;
        EditIsWeighted = false;
        EditTareWeight = null;
        EditSortOrder = 0;
        EditIsActive = true;

        IsEditing = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void EditPLU()
    {
        if (SelectedPLU == null) return;

        EditId = SelectedPLU.Id;
        EditCode = SelectedPLU.Code;
        EditProductId = SelectedPLU.ProductId;
        SelectedProduct = Products.FirstOrDefault(p => p.Id == SelectedPLU.ProductId);
        EditDisplayName = SelectedPLU.DisplayName;
        EditIsWeighted = SelectedPLU.IsWeighted;
        EditTareWeight = SelectedPLU.TareWeight;
        EditSortOrder = SelectedPLU.SortOrder;
        EditIsActive = SelectedPLU.IsActive;

        IsEditing = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task SavePLUAsync()
    {
        if (string.IsNullOrWhiteSpace(EditCode) || SelectedProduct == null)
        {
            ErrorMessage = "Please enter a PLU code and select a product.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            // Validate code
            var isValid = await barcodeService.ValidatePLUCodeAsync(EditCode, EditId == 0 ? null : EditId);
            if (!isValid)
            {
                ErrorMessage = "PLU code is invalid or already exists.";
                return;
            }

            if (EditId == 0)
            {
                // Create new PLU
                await barcodeService.AddPLUCodeAsync(
                    SelectedProduct.Id,
                    EditCode,
                    EditIsWeighted,
                    EditTareWeight);

                SuccessMessage = "PLU code created successfully!";
            }
            else
            {
                // Update existing PLU
                var plu = new PLUCode
                {
                    Id = EditId,
                    Code = EditCode,
                    ProductId = SelectedProduct.Id,
                    DisplayName = EditDisplayName,
                    IsWeighted = EditIsWeighted,
                    TareWeight = EditTareWeight,
                    SortOrder = EditSortOrder,
                    IsActive = EditIsActive
                };

                await barcodeService.UpdatePLUCodeAsync(plu);
                SuccessMessage = "PLU code updated successfully!";
            }

            IsEditing = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeletePLUAsync()
    {
        if (SelectedPLU == null) return;

        var result = MessageBox.Show(
            $"Delete PLU code '{SelectedPLU.Code}' for product '{SelectedPLU.Product?.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            await barcodeService.DeletePLUCodeAsync(SelectedPLU.Id);
            SuccessMessage = "PLU code deleted successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null && IsEditing)
        {
            EditProductId = value.Id;
            if (string.IsNullOrWhiteSpace(EditDisplayName))
            {
                EditDisplayName = value.Name;
            }
        }
    }
}
