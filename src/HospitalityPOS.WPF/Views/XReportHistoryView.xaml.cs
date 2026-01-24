using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for XReportHistoryView.xaml
/// </summary>
public partial class XReportHistoryView : UserControl
{
    public XReportHistoryView(XReportHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
