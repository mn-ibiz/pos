using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for ModifierGroupEditorDialog.xaml
/// </summary>
public partial class ModifierGroupEditorDialog : Window, INotifyPropertyChanged
{
    private readonly ModifierGroup? _existingGroup;
    private string _name = string.Empty;
    private string? _displayName;
    private string? _description;
    private ModifierSelectionType _selectedSelectionType = ModifierSelectionType.Multiple;
    private int _minSelections;
    private int _maxSelections = 5;
    private bool _isRequired;
    private int _displayOrder;
    private string? _colorCode;
    private string? _kitchenStation;
    private bool _printOnKOT = true;
    private bool _showOnReceipt = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModifierGroupEditorDialog(ModifierGroup? existingGroup)
    {
        InitializeComponent();
        DataContext = this;
        _existingGroup = existingGroup;

        // Initialize selection types
        SelectionTypes = Enum.GetValues<ModifierSelectionType>().ToList();

        if (existingGroup != null)
        {
            DialogTitle = "Edit Modifier Group";
            Name = existingGroup.Name;
            DisplayName = existingGroup.DisplayName;
            Description = existingGroup.Description;
            SelectedSelectionType = existingGroup.SelectionType;
            MinSelections = existingGroup.MinSelections;
            MaxSelections = existingGroup.MaxSelections;
            IsRequired = existingGroup.IsRequired;
            DisplayOrder = existingGroup.DisplayOrder;
            ColorCode = existingGroup.ColorCode;
            KitchenStation = existingGroup.KitchenStation;
            PrintOnKOT = existingGroup.PrintOnKOT;
            ShowOnReceipt = existingGroup.ShowOnReceipt;

            // Load existing items
            foreach (var item in existingGroup.Items)
            {
                Items.Add(new ModifierItemEditorResult
                {
                    Id = item.Id,
                    Name = item.Name,
                    DisplayName = item.DisplayName,
                    ShortCode = item.ShortCode,
                    Description = item.Description,
                    Price = item.Price,
                    CostPrice = item.CostPrice,
                    MaxQuantity = item.MaxQuantity,
                    DisplayOrder = item.DisplayOrder,
                    ColorCode = item.ColorCode,
                    IsAvailable = item.IsAvailable,
                    IsDefault = item.IsDefault,
                    KOTText = item.KOTText,
                    TaxRate = item.TaxRate,
                    InventoryProductId = item.InventoryProductId,
                    InventoryDeductQuantity = item.InventoryDeductQuantity,
                    Allergens = item.Allergens
                });
            }
        }
        else
        {
            DialogTitle = "Create Modifier Group";
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

    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public List<ModifierSelectionType> SelectionTypes { get; }

    public ModifierSelectionType SelectedSelectionType
    {
        get => _selectedSelectionType;
        set
        {
            _selectedSelectionType = value;
            OnPropertyChanged();
        }
    }

    public int MinSelections
    {
        get => _minSelections;
        set
        {
            _minSelections = value;
            OnPropertyChanged();
        }
    }

    public int MaxSelections
    {
        get => _maxSelections;
        set
        {
            _maxSelections = value;
            OnPropertyChanged();
        }
    }

    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            _isRequired = value;
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

    public string? KitchenStation
    {
        get => _kitchenStation;
        set
        {
            _kitchenStation = value;
            OnPropertyChanged();
        }
    }

    public bool PrintOnKOT
    {
        get => _printOnKOT;
        set
        {
            _printOnKOT = value;
            OnPropertyChanged();
        }
    }

    public bool ShowOnReceipt
    {
        get => _showOnReceipt;
        set
        {
            _showOnReceipt = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ModifierItemEditorResult> Items { get; } = new();

    public bool HasItems => Items.Count > 0;

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    public ModifierGroupEditorResult? Result { get; private set; }

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
            MessageBox.Show("Please enter a name for the modifier group.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new ModifierGroupEditorResult
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            SelectionType = SelectedSelectionType,
            MinSelections = MinSelections,
            MaxSelections = MaxSelections,
            IsRequired = IsRequired,
            DisplayOrder = DisplayOrder,
            ColorCode = ColorCode,
            KitchenStation = KitchenStation,
            PrintOnKOT = PrintOnKOT,
            ShowOnReceipt = ShowOnReceipt,
            Items = Items.ToList()
        };

        DialogResult = true;
        Close();
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ModifierItemEditorDialog(null, null)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            dialog.Result.DisplayOrder = Items.Count;
            Items.Add(dialog.Result);
            OnPropertyChanged(nameof(HasItems));
        }
    }

    private void EditItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ModifierItemEditorResult item)
        {
            var tempItem = new ModifierItem
            {
                Id = item.Id,
                Name = item.Name,
                DisplayName = item.DisplayName,
                ShortCode = item.ShortCode,
                Description = item.Description,
                Price = item.Price,
                CostPrice = item.CostPrice,
                MaxQuantity = item.MaxQuantity,
                DisplayOrder = item.DisplayOrder,
                ColorCode = item.ColorCode,
                IsAvailable = item.IsAvailable,
                IsDefault = item.IsDefault,
                KOTText = item.KOTText,
                TaxRate = item.TaxRate,
                InventoryProductId = item.InventoryProductId,
                InventoryDeductQuantity = item.InventoryDeductQuantity,
                Allergens = item.Allergens
            };

            var dialog = new ModifierItemEditorDialog(tempItem, null)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                var index = Items.IndexOf(item);
                if (index >= 0)
                {
                    Items[index] = dialog.Result;
                }
            }
        }
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ModifierItemEditorResult item)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the item '{item.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Items.Remove(item);
                OnPropertyChanged(nameof(HasItems));
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
