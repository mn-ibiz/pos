using System.Windows.Controls;
using System.Windows.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for LabelTemplateManagementView.xaml.
/// </summary>
public partial class LabelTemplateManagementView : UserControl
{
    public LabelTemplateManagementView()
    {
        InitializeComponent();
    }

    private void TemplateItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is LabelTemplateDto template)
        {
            if (DataContext is LabelTemplateManagementViewModel viewModel)
            {
                viewModel.SelectedTemplate = template;
            }
        }
    }
}
