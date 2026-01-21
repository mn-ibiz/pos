using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for selecting product variant options.
/// </summary>
public partial class VariantSelectionDialog : Window, INotifyPropertyChanged
{
    private readonly Product _product;
    private readonly IReadOnlyList<ProductVariant> _variants;
    private decimal _totalPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public VariantSelectionDialog(
        Product product,
        IReadOnlyList<VariantOption> availableOptions,
        IReadOnlyList<ProductVariant> productVariants)
    {
        InitializeComponent();
        DataContext = this;

        _product = product;
        _variants = productVariants;

        ProductName = product.Name;
        _totalPrice = product.SellingPrice;

        // Build variant options display model
        foreach (var option in availableOptions.OrderBy(o => o.DisplayOrder))
        {
            var optionVm = new VariantOptionDisplayModel
            {
                OptionId = option.Id,
                Name = option.DisplayName ?? option.Name,
                Values = new ObservableCollection<VariantValueDisplayModel>(
                    option.Values
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new VariantValueDisplayModel
                        {
                            ValueId = v.Id,
                            OptionId = option.Id,
                            Value = v.Value,
                            DisplayName = v.DisplayName ?? v.Value,
                            PriceAdjustment = v.PriceAdjustment,
                            IsPriceAdjustmentPercent = v.IsPriceAdjustmentPercent,
                            ColorCode = v.ColorCode
                        }))
            };
            VariantOptions.Add(optionVm);
        }

        // Add converter resources
        Resources.Add("BoolToSelectedConverter", new BoolToSelectedConverter());
        Resources.Add("BoolToVisibilityConverter", new BooleanToVisibilityConverter());
    }

    public string ProductName { get; }

    public ObservableCollection<VariantOptionDisplayModel> VariantOptions { get; } = new();

    public decimal TotalPrice
    {
        get => _totalPrice;
        private set
        {
            _totalPrice = value;
            OnPropertyChanged();
        }
    }

    public bool CanAdd => VariantOptions.All(o => o.SelectedValue != null);

    public VariantSelectionResult? Result { get; private set; }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanAdd)
        {
            MessageBox.Show("Please select all options.", "Selection Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Find matching product variant
        var selectedValueIds = VariantOptions
            .Select(o => o.SelectedValue!.ValueId)
            .ToHashSet();

        var matchingVariant = _variants.FirstOrDefault(v =>
            v.VariantValues.Select(vv => vv.VariantOptionValueId).ToHashSet().SetEquals(selectedValueIds));

        if (matchingVariant != null)
        {
            Result = new VariantSelectionResult
            {
                ProductVariantId = matchingVariant.Id,
                VariantSku = matchingVariant.SKU,
                Price = matchingVariant.SellingPrice ?? TotalPrice,
                VariantDescription = string.Join(", ", VariantOptions.Select(o =>
                    $"{o.Name}: {o.SelectedValue!.DisplayName}"))
            };
        }
        else
        {
            // No exact match found, create result without specific variant ID
            Result = new VariantSelectionResult
            {
                Price = TotalPrice,
                VariantDescription = string.Join(", ", VariantOptions.Select(o =>
                    $"{o.Name}: {o.SelectedValue!.DisplayName}"))
            };
        }

        DialogResult = true;
        Close();
    }

    private void VariantValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is VariantValueDisplayModel value)
        {
            // Find the parent option
            var option = VariantOptions.FirstOrDefault(o => o.OptionId == value.OptionId);
            if (option != null)
            {
                // Deselect all other values in this option
                foreach (var v in option.Values)
                {
                    v.IsSelected = false;
                }

                // Select this value
                value.IsSelected = true;
                option.SelectedValue = value;
            }

            RecalculatePrice();
            OnPropertyChanged(nameof(CanAdd));
        }
    }

    private void RecalculatePrice()
    {
        var basePrice = _product.SellingPrice;
        var adjustments = 0m;

        foreach (var option in VariantOptions)
        {
            if (option.SelectedValue != null)
            {
                var value = option.SelectedValue;
                if (value.IsPriceAdjustmentPercent)
                {
                    adjustments += basePrice * (value.PriceAdjustment / 100);
                }
                else
                {
                    adjustments += value.PriceAdjustment;
                }
            }
        }

        TotalPrice = basePrice + adjustments;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Display model for a variant option.
/// </summary>
public class VariantOptionDisplayModel : INotifyPropertyChanged
{
    public int OptionId { get; set; }
    public string Name { get; set; } = "";
    public ObservableCollection<VariantValueDisplayModel> Values { get; set; } = new();
    public VariantValueDisplayModel? SelectedValue { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Display model for a variant value.
/// </summary>
public class VariantValueDisplayModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public int ValueId { get; set; }
    public int OptionId { get; set; }
    public string Value { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public decimal PriceAdjustment { get; set; }
    public bool IsPriceAdjustmentPercent { get; set; }
    public string? ColorCode { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public bool HasPriceAdjustment => PriceAdjustment != 0;

    public string PriceAdjustmentText
    {
        get
        {
            if (PriceAdjustment == 0) return "";
            var sign = PriceAdjustment > 0 ? "+" : "";
            return IsPriceAdjustmentPercent
                ? $"{sign}{PriceAdjustment:N0}%"
                : $"{sign}KSh {PriceAdjustment:N2}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Result from variant selection dialog.
/// </summary>
public class VariantSelectionResult
{
    public int? ProductVariantId { get; set; }
    public string? VariantSku { get; set; }
    public decimal Price { get; set; }
    public string VariantDescription { get; set; } = "";
}

/// <summary>
/// Converter for boolean to "Selected" tag.
/// </summary>
public class BoolToSelectedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is true ? "Selected" : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
