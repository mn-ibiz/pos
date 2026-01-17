using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class EmployeeEditorView : UserControl
{
    public EmployeeEditorView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is EmployeeEditorViewModel viewModel)
            {
                // Check if parameter was passed
                var employeeId = viewModel.GetType().GetProperty("EmployeeId")?.GetValue(viewModel) as int?;
                await viewModel.InitializeAsync(employeeId);
            }
        };
    }
}
