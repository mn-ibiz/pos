using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

public partial class JournalEntriesView : UserControl
{
    public JournalEntriesView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is JournalEntriesViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        };
    }
}
