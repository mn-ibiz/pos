using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views.Inventory;

/// <summary>
/// Interaction logic for InventoryView.xaml.
/// Displays product inventory with stock levels and filtering.
/// </summary>
public partial class InventoryView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryView"/> class.
    /// </summary>
    public InventoryView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles click on the low stock summary card to filter for low stock items.
    /// </summary>
    private void LowStockCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is InventoryViewModel viewModel)
        {
            viewModel.ShowOnlyLowStockCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles click on the out-of-stock summary card to filter for out-of-stock items.
    /// </summary>
    private void OutOfStockCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is InventoryViewModel viewModel)
        {
            viewModel.ShowOnlyOutOfStockCommand.Execute(null);
        }
    }
}
