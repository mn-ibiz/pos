using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for ModifierItemEditorDialog.xaml
/// </summary>
public partial class ModifierItemEditorDialog : Window, INotifyPropertyChanged
{
    private readonly ModifierItem? _existingItem;
    private readonly int _existingId;
    private string _name = string.Empty;
    private string? _displayName;
    private string? _shortCode;
    private string? _description;
    private decimal _price;
    private decimal? _costPrice;
    private int _maxQuantity = 10;
    private int _displayOrder;
    private string? _colorCode;
    private bool _isAvailable = true;
    private bool _isDefault;
    private string? _kotText;
    private decimal _taxRate = 16.00m;
    private int? _inventoryProductId;
    private decimal _inventoryDeductQuantity;
    private string? _allergens;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModifierItemEditorDialog(ModifierItem? existingItem, ModifierGroup? parentGroup)
    {
        InitializeComponent();
        DataContext = this;
        _existingItem = existingItem;

        if (existingItem != null)
        {
            DialogTitle = "Edit Modifier Item";
            _existingId = existingItem.Id;
            Name = existingItem.Name;
            DisplayName = existingItem.DisplayName;
            ShortCode = existingItem.ShortCode;
            Description = existingItem.Description;
            Price = existingItem.Price;
            CostPrice = existingItem.CostPrice;
            MaxQuantity = existingItem.MaxQuantity;
            DisplayOrder = existingItem.DisplayOrder;
            ColorCode = existingItem.ColorCode;
            IsAvailable = existingItem.IsAvailable;
            IsDefault = existingItem.IsDefault;
            KOTText = existingItem.KOTText;
            TaxRate = existingItem.TaxRate;
            InventoryProductId = existingItem.InventoryProductId;
            InventoryDeductQuantity = existingItem.InventoryDeductQuantity;
            Allergens = existingItem.Allergens;
        }
        else
        {
            DialogTitle = "Add Modifier Item";
        }

        NameTextBox.Focus();
    }

    public string DialogTitle { get; }

    public new string Name
    {
        get => _name;
        set
        {
            _name = value;
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

    public string? ShortCode
    {
        get => _shortCode;
        set
        {
            _shortCode = value;
            OnPropertyChanged();
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            _price = value;
            OnPropertyChanged();
        }
    }

    public decimal? CostPrice
    {
        get => _costPrice;
        set
        {
            _costPrice = value;
            OnPropertyChanged();
        }
    }

    public int MaxQuantity
    {
        get => _maxQuantity;
        set
        {
            _maxQuantity = value;
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

    public string? ColorCode
    {
        get => _colorCode;
        set
        {
            _colorCode = value;
            OnPropertyChanged();
        }
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            _isAvailable = value;
            OnPropertyChanged();
        }
    }

    public bool IsDefault
    {
        get => _isDefault;
        set
        {
            _isDefault = value;
            OnPropertyChanged();
        }
    }

    public string? KOTText
    {
        get => _kotText;
        set
        {
            _kotText = value;
            OnPropertyChanged();
        }
    }

    public decimal TaxRate
    {
        get => _taxRate;
        set
        {
            _taxRate = value;
            OnPropertyChanged();
        }
    }

    public int? InventoryProductId
    {
        get => _inventoryProductId;
        set
        {
            _inventoryProductId = value;
            OnPropertyChanged();
        }
    }

    public decimal InventoryDeductQuantity
    {
        get => _inventoryDeductQuantity;
        set
        {
            _inventoryDeductQuantity = value;
            OnPropertyChanged();
        }
    }

    public string? Allergens
    {
        get => _allergens;
        set
        {
            _allergens = value;
            OnPropertyChanged();
        }
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    public ModifierItemEditorResult? Result { get; private set; }

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
            MessageBox.Show("Please enter a name for the modifier item.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new ModifierItemEditorResult
        {
            Id = _existingId,
            Name = Name,
            DisplayName = DisplayName,
            ShortCode = ShortCode,
            Description = Description,
            Price = Price,
            CostPrice = CostPrice,
            MaxQuantity = MaxQuantity,
            DisplayOrder = DisplayOrder,
            ColorCode = ColorCode,
            IsAvailable = IsAvailable,
            IsDefault = IsDefault,
            KOTText = KOTText,
            TaxRate = TaxRate,
            InventoryProductId = InventoryProductId,
            InventoryDeductQuantity = InventoryDeductQuantity,
            Allergens = Allergens
        };

        DialogResult = true;
        Close();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
