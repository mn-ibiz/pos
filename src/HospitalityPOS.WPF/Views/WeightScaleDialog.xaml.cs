using System.Windows;
using HospitalityPOS.Core.Models.Hardware;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for WeightScaleDialog.xaml
/// </summary>
public partial class WeightScaleDialog : Window
{
    private readonly WeightScaleDialogViewModel _viewModel;

    /// <summary>
    /// Gets the weighed order item result if added.
    /// </summary>
    public WeighedOrderItem? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeightScaleDialog"/> class.
    /// </summary>
    public WeightScaleDialog(WeightScaleDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to dialog close event
        _viewModel.DialogClosed += OnDialogClosed;

        // Allow dragging the window
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    /// <summary>
    /// Shows the dialog to weigh a product.
    /// </summary>
    public async Task<WeighedOrderItem?> ShowForProductAsync(
        int productId,
        string productName,
        decimal pricePerUnit,
        WeightUnit unit = WeightUnit.Kilogram)
    {
        await _viewModel.InitializeAsync(productId, productName, pricePerUnit, unit);
        ShowDialog();
        return Result;
    }

    private void OnDialogClosed(object? sender, WeighedOrderItem? item)
    {
        Result = item;
        _viewModel.DialogClosed -= OnDialogClosed;
        DialogResult = item != null;
        Close();
    }

    /// <summary>
    /// Clean up when window is closing.
    /// </summary>
    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        await _viewModel.CleanupAsync();
        base.OnClosing(e);
    }
}
