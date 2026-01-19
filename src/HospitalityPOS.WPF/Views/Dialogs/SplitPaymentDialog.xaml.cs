using System.Windows;
using HospitalityPOS.WPF.ViewModels.Dialogs;
using HospitalityPOS.WPF.ViewModels.Models;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for SplitPaymentDialog.xaml
/// </summary>
public partial class SplitPaymentDialog : Window
{
    private readonly SplitPaymentDialogViewModel _viewModel;

    /// <summary>
    /// Gets the list of payments entered.
    /// </summary>
    public IReadOnlyList<PaymentEntry> Payments => _viewModel.Payments.ToList();

    /// <summary>
    /// Gets whether the dialog was completed successfully.
    /// </summary>
    public bool IsCompleted => _viewModel.IsCompleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitPaymentDialog"/> class.
    /// </summary>
    /// <param name="orderTotal">The total order amount.</param>
    public SplitPaymentDialog(decimal orderTotal)
    {
        InitializeComponent();

        _viewModel = new SplitPaymentDialogViewModel(orderTotal);
        _viewModel.RequestClose += OnRequestClose;
        DataContext = _viewModel;
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        DialogResult = _viewModel.IsCompleted;
        Close();
    }
}
