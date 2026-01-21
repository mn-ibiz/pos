using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for VariantOptionEditorDialog.xaml
/// </summary>
public partial class VariantOptionEditorDialog : Window, INotifyPropertyChanged
{
    private readonly VariantOption? _existingOption;
    private string _name = string.Empty;
    private string? _displayName;
    private string? _description;
    private int _displayOrder;
    private bool _isGlobal = true;
    private VariantOptionType _selectedOptionType = VariantOptionType.Custom;

    public event PropertyChangedEventHandler? PropertyChanged;

    public VariantOptionEditorDialog(VariantOption? existingOption)
    {
        InitializeComponent();
        DataContext = this;
        _existingOption = existingOption;

        // Initialize option types
        OptionTypes = Enum.GetValues<VariantOptionType>().ToList();

        if (existingOption != null)
        {
            DialogTitle = "Edit Variant Option";
            Name = existingOption.Name;
            DisplayName = existingOption.DisplayName;
            Description = existingOption.Description;
            DisplayOrder = existingOption.DisplayOrder;
            IsGlobal = existingOption.IsGlobal;
            SelectedOptionType = existingOption.OptionType;

            // Load existing values
            foreach (var value in existingOption.Values)
            {
                Values.Add(new VariantOptionValueEditorResult
                {
                    Id = value.Id,
                    Value = value.Value,
                    DisplayName = value.DisplayName,
                    ColorCode = value.ColorCode,
                    PriceAdjustment = value.PriceAdjustment,
                    IsPriceAdjustmentPercent = value.IsPriceAdjustmentPercent,
                    DisplayOrder = value.DisplayOrder,
                    SkuSuffix = value.SkuSuffix
                });
            }
        }
        else
        {
            DialogTitle = "Create Variant Option";
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

    public int DisplayOrder
    {
        get => _displayOrder;
        set
        {
            _displayOrder = value;
            OnPropertyChanged();
        }
    }

    public bool IsGlobal
    {
        get => _isGlobal;
        set
        {
            _isGlobal = value;
            OnPropertyChanged();
        }
    }

    public List<VariantOptionType> OptionTypes { get; }

    public VariantOptionType SelectedOptionType
    {
        get => _selectedOptionType;
        set
        {
            _selectedOptionType = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<VariantOptionValueEditorResult> Values { get; } = new();

    public bool HasValues => Values.Count > 0;

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    public VariantOptionEditorResult? Result { get; private set; }

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
            MessageBox.Show("Please enter a name for the variant option.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new VariantOptionEditorResult
        {
            Name = Name,
            DisplayName = DisplayName,
            OptionType = SelectedOptionType,
            Description = Description,
            DisplayOrder = DisplayOrder,
            IsGlobal = IsGlobal,
            Values = Values.ToList()
        };

        DialogResult = true;
        Close();
    }

    private void AddValue_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VariantValueEditorDialog(null, null)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            dialog.Result.DisplayOrder = Values.Count;
            Values.Add(dialog.Result);
            OnPropertyChanged(nameof(HasValues));
        }
    }

    private void EditValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is VariantOptionValueEditorResult value)
        {
            // Create a temporary VariantOptionValue for editing
            var tempValue = new VariantOptionValue
            {
                Id = value.Id,
                Value = value.Value,
                DisplayName = value.DisplayName,
                ColorCode = value.ColorCode,
                PriceAdjustment = value.PriceAdjustment,
                IsPriceAdjustmentPercent = value.IsPriceAdjustmentPercent,
                DisplayOrder = value.DisplayOrder,
                SkuSuffix = value.SkuSuffix
            };

            var dialog = new VariantValueEditorDialog(tempValue, null)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                var index = Values.IndexOf(value);
                if (index >= 0)
                {
                    Values[index] = dialog.Result;
                }
            }
        }
    }

    private void DeleteValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is VariantOptionValueEditorResult value)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the value '{value.Value}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Values.Remove(value);
                OnPropertyChanged(nameof(HasValues));
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
