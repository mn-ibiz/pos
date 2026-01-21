using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for VariantValueEditorDialog.xaml
/// </summary>
public partial class VariantValueEditorDialog : Window, INotifyPropertyChanged
{
    private readonly VariantOptionValue? _existingValue;
    private readonly int _existingId;
    private string _value = string.Empty;
    private string? _displayName;
    private string? _skuSuffix;
    private string? _colorCode;
    private decimal _priceAdjustment;
    private bool _isPriceAdjustmentPercent;
    private int _displayOrder;

    public event PropertyChangedEventHandler? PropertyChanged;

    public VariantValueEditorDialog(VariantOptionValue? existingValue, VariantOption? parentOption)
    {
        InitializeComponent();
        DataContext = this;
        _existingValue = existingValue;

        if (existingValue != null)
        {
            DialogTitle = "Edit Variant Value";
            _existingId = existingValue.Id;
            Value = existingValue.Value;
            DisplayName = existingValue.DisplayName;
            SkuSuffix = existingValue.SkuSuffix;
            ColorCode = existingValue.ColorCode;
            PriceAdjustment = existingValue.PriceAdjustment;
            IsPriceAdjustmentPercent = existingValue.IsPriceAdjustmentPercent;
            DisplayOrder = existingValue.DisplayOrder;
        }
        else
        {
            DialogTitle = "Add Variant Value";
        }

        ValueTextBox.Focus();
    }

    public string DialogTitle { get; }

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public string? DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public string? SkuSuffix
    {
        get => _skuSuffix;
        set
        {
            _skuSuffix = value;
            OnPropertyChanged();
        }
    }

    public string? ColorCode
    {
        get => _colorCode;
        set
        {
            _colorCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorPreviewBrush));
        }
    }

    public Brush ColorPreviewBrush
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ColorCode))
                return new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(ColorCode);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            }
        }
    }

    public decimal PriceAdjustment
    {
        get => _priceAdjustment;
        set
        {
            _priceAdjustment = value;
            OnPropertyChanged();
        }
    }

    public bool IsPriceAdjustmentPercent
    {
        get => _isPriceAdjustmentPercent;
        set
        {
            _isPriceAdjustmentPercent = value;
            OnPropertyChanged();
        }
    }

    public int DisplayOrder
    {
        get => _displayOrder;
        set
        {
            _displayOrder = value;
            OnPropertyChanged();
        }
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(Value);

    public VariantOptionValueEditorResult? Result { get; private set; }

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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanSave)
        {
            MessageBox.Show("Please enter a value.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new VariantOptionValueEditorResult
        {
            Id = _existingId,
            Value = Value,
            DisplayName = DisplayName,
            SkuSuffix = SkuSuffix,
            ColorCode = ColorCode,
            PriceAdjustment = PriceAdjustment,
            IsPriceAdjustmentPercent = IsPriceAdjustmentPercent,
            DisplayOrder = DisplayOrder
        };

        DialogResult = true;
        Close();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
