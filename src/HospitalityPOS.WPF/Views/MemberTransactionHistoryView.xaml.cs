using System.Windows.Controls;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for MemberTransactionHistoryView.xaml
/// </summary>
public partial class MemberTransactionHistoryView : UserControl
{
    public MemberTransactionHistoryView()
    {
        InitializeComponent();
    }

    private async void DateRangePreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is DateRangePreset preset)
        {
            if (DataContext is MemberTransactionHistoryViewModel viewModel)
            {
                await viewModel.ApplyDateRangePresetCommand.ExecuteAsync(preset);
            }
        }
    }
}
