using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class AttendanceView : UserControl
{
    public AttendanceView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is AttendanceViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
