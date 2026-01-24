using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CashDenominationCountDialog.xaml
/// </summary>
public partial class CashDenominationCountDialog : Window
{
    private readonly CashDenominationCountViewModel _viewModel;

    public CashDenominationCountDialog(CashDenominationCountViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to dialog result changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CashDenominationCountViewModel.DialogResult) &&
                _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };

        Loaded += async (s, e) =>
        {
            await _viewModel.LoadDenominationsAsync();
        };
    }

    /// <summary>
    /// Gets the ViewModel.
    /// </summary>
    public CashDenominationCountViewModel ViewModel => _viewModel;
}
