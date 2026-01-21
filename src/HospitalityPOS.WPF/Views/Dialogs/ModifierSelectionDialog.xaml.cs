using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for selecting product modifiers.
/// </summary>
public partial class ModifierSelectionDialog : Window, INotifyPropertyChanged
{
    private decimal _basePrice;
    private decimal _modifierTotal;
    private string? _validationError;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModifierSelectionDialog(
        Product product,
        IReadOnlyList<ModifierGroup> modifierGroups)
    {
        InitializeComponent();
        DataContext = this;

        ProductName = product.Name;
        _basePrice = product.SellingPrice;

        // Build modifier groups display model
        foreach (var group in modifierGroups.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder))
        {
            var groupVm = new ModifierGroupDisplayModel
            {
                GroupId = group.Id,
                Name = group.DisplayName ?? group.Name,
                Description = group.Description,
                SelectionType = group.SelectionType,
                MinSelections = group.MinSelections,
                MaxSelections = group.MaxSelections,
                IsRequired = group.IsRequired,
                Items = new ObservableCollection<ModifierItemDisplayModel>(
                    group.Items
                        .Where(i => i.IsActive && i.IsAvailable)
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new ModifierItemDisplayModel
                        {
                            ItemId = i.Id,
                            GroupId = group.Id,
                            Name = i.DisplayName ?? i.Name,
                            Price = i.Price,
                            MaxQuantity = i.MaxQuantity,
                            IsDefault = i.IsDefault,
                            SelectionType = group.SelectionType
                        }))
            };

            // Pre-select default items
            foreach (var item in groupVm.Items.Where(i => i.IsDefault))
            {
                item.IsSelected = true;
                item.Quantity = 1;
            }

            ModifierGroups.Add(groupVm);
        }

        RecalculateTotals();
    }

    public string ProductName { get; }

    public ObservableCollection<ModifierGroupDisplayModel> ModifierGroups { get; } = new();

    public decimal BasePrice => _basePrice;

    public decimal ModifierTotal
    {
        get => _modifierTotal;
        private set
        {
            _modifierTotal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(HasModifierPrice));
        }
    }

    public decimal TotalPrice => _basePrice + _modifierTotal;

    public bool HasModifierPrice => _modifierTotal > 0;

    public string? ValidationError
    {
        get => _validationError;
        private set
        {
            _validationError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationError));
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationError);

    public bool CanAdd => ValidateSelections(out _);

    public ModifierSelectionResult? Result { get; private set; }

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
        if (!ValidateSelections(out var error))
        {
            MessageBox.Show(error, "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Build result
        var selectedModifiers = new List<SelectedModifierData>();
        foreach (var group in ModifierGroups)
        {
            foreach (var item in group.Items.Where(i => i.IsSelected && i.Quantity > 0))
            {
                selectedModifiers.Add(new SelectedModifierData
                {
                    ModifierItemId = item.ItemId,
                    ModifierGroupId = group.GroupId,
                    GroupName = group.Name,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    TotalPrice = item.Price * item.Quantity
                });
            }
        }

        Result = new ModifierSelectionResult
        {
            SelectedModifiers = selectedModifiers,
            ModifierTotal = ModifierTotal,
            ModifierDisplayText = string.Join(", ", selectedModifiers.Select(m =>
                m.Quantity > 1 ? $"{m.Name} x{m.Quantity}" : m.Name))
        };

        DialogResult = true;
        Close();
    }

    private void ModifierItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierItemDisplayModel item)
        {
            var group = ModifierGroups.FirstOrDefault(g => g.GroupId == item.GroupId);
            if (group == null) return;

            if (group.SelectionType == ModifierSelectionType.Single)
            {
                // Single selection - deselect all others
                foreach (var other in group.Items)
                {
                    if (other != item)
                    {
                        other.IsSelected = false;
                        other.Quantity = 0;
                    }
                }
                item.IsSelected = !item.IsSelected;
                item.Quantity = item.IsSelected ? 1 : 0;
            }
            else
            {
                // Multiple selection - toggle
                item.IsSelected = !item.IsSelected;
                item.Quantity = item.IsSelected ? 1 : 0;
            }

            group.NotifySelectionChanged();
            RecalculateTotals();
            OnPropertyChanged(nameof(CanAdd));
        }
    }

    private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierItemDisplayModel item)
        {
            if (item.Quantity < item.MaxQuantity)
            {
                item.Quantity++;
                RecalculateTotals();

                var group = ModifierGroups.FirstOrDefault(g => g.GroupId == item.GroupId);
                group?.NotifySelectionChanged();
            }
        }
        e.Handled = true;
    }

    private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierItemDisplayModel item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                item.Quantity = 0;
                item.IsSelected = false;
            }
            RecalculateTotals();

            var group = ModifierGroups.FirstOrDefault(g => g.GroupId == item.GroupId);
            group?.NotifySelectionChanged();
        }
        e.Handled = true;
    }

    private void RecalculateTotals()
    {
        var total = 0m;
        foreach (var group in ModifierGroups)
        {
            foreach (var item in group.Items.Where(i => i.IsSelected))
            {
                total += item.Price * item.Quantity;
            }
        }
        ModifierTotal = total;

        ValidateSelections(out var error);
        ValidationError = error;
    }

    private bool ValidateSelections(out string? error)
    {
        error = null;

        foreach (var group in ModifierGroups)
        {
            var selectedCount = group.Items.Where(i => i.IsSelected).Sum(i => i.Quantity);

            if (group.IsRequired && selectedCount < group.MinSelections)
            {
                error = $"Please select at least {group.MinSelections} item(s) from '{group.Name}'";
                return false;
            }

            if (group.MaxSelections > 0 && selectedCount > group.MaxSelections)
            {
                error = $"Maximum {group.MaxSelections} item(s) allowed for '{group.Name}'";
                return false;
            }
        }

        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Display model for a modifier group.
/// </summary>
public class ModifierGroupDisplayModel : INotifyPropertyChanged
{
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ModifierSelectionType SelectionType { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public ObservableCollection<ModifierItemDisplayModel> Items { get; set; } = new();

    public string SelectionHint
    {
        get
        {
            if (SelectionType == ModifierSelectionType.Single)
                return "Choose one";

            if (MaxSelections > 0)
                return $"Choose up to {MaxSelections}";

            if (MinSelections > 0)
                return $"Choose at least {MinSelections}";

            return "Optional";
        }
    }

    public string SelectionStatus
    {
        get
        {
            var count = Items.Where(i => i.IsSelected).Sum(i => i.Quantity);
            if (IsRequired && count < MinSelections)
                return $"{count}/{MinSelections} required";
            return $"{count} selected";
        }
    }

    public Brush SelectionStatusColor
    {
        get
        {
            var count = Items.Where(i => i.IsSelected).Sum(i => i.Quantity);
            if (IsRequired && count < MinSelections)
                return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
        }
    }

    public void NotifySelectionChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionStatusColor)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Display model for a modifier item.
/// </summary>
public class ModifierItemDisplayModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _quantity;

    public int ItemId { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int MaxQuantity { get; set; } = 1;
    public bool IsDefault { get; set; }
    public ModifierSelectionType SelectionType { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowQuantityControls)));
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
        }
    }

    public bool HasPrice => Price > 0;

    public string PriceText => Price > 0 ? $"+KSh {Price:N2}" : "Free";

    public bool ShowQuantityControls => IsSelected && MaxQuantity > 1;

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Result from modifier selection dialog.
/// </summary>
public class ModifierSelectionResult
{
    public List<SelectedModifierData> SelectedModifiers { get; set; } = new();
    public decimal ModifierTotal { get; set; }
    public string ModifierDisplayText { get; set; } = "";
}

/// <summary>
/// Data for a selected modifier.
/// </summary>
public class SelectedModifierData
{
    public int ModifierItemId { get; set; }
    public int ModifierGroupId { get; set; }
    public string GroupName { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
