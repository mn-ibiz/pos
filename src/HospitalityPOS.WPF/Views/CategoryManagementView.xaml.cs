using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CategoryManagementView.xaml
/// </summary>
public partial class CategoryManagementView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryManagementView"/> class.
    /// </summary>
    public CategoryManagementView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles TreeView selection changes to update the ViewModel's SelectedCategory.
    /// </summary>
    private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is CategoryManagementViewModel viewModel && e.NewValue is Category category)
        {
            viewModel.SelectedCategory = category;
        }
        else if (DataContext is CategoryManagementViewModel vm)
        {
            vm.SelectedCategory = null;
        }
    }
}
