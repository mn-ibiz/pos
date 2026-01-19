using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for PurchaseOrdersView.xaml
/// </summary>
public partial class PurchaseOrdersView : UserControl
{
    public PurchaseOrdersView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles click on the suggestions panel header to toggle expansion.
    /// </summary>
    private void SuggestionsHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PurchaseOrdersViewModel vm)
        {
            vm.ToggleSuggestionsPanelCommand.Execute(null);
        }
    }
}
