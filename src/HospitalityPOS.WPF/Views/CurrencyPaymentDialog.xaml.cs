using System.Windows;
using HospitalityPOS.Core.Models.Currency;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for CurrencyPaymentDialog.xaml
/// </summary>
public partial class CurrencyPaymentDialog : Window
{
    private readonly CurrencyPaymentViewModel _viewModel;

    /// <summary>
    /// Gets the payment result if confirmed.
    /// </summary>
    public MultiCurrencyPaymentDto? PaymentResult { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyPaymentDialog"/> class.
    /// </summary>
    public CurrencyPaymentDialog(CurrencyPaymentViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to events
        _viewModel.PaymentConfirmed += OnPaymentConfirmed;
        _viewModel.Cancelled += OnCancelled;

        // Allow dragging the window
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    /// <summary>
    /// Shows the dialog for a payment amount.
    /// </summary>
    public async Task<MultiCurrencyPaymentDto?> ShowForPaymentAsync(decimal amountDueInBaseCurrency)
    {
        await _viewModel.InitializeAsync(amountDueInBaseCurrency);
        ShowDialog();
        return PaymentResult;
    }

    private void OnPaymentConfirmed(object? sender, MultiCurrencyPaymentDto payment)
    {
        PaymentResult = payment;
        _viewModel.PaymentConfirmed -= OnPaymentConfirmed;
        _viewModel.Cancelled -= OnCancelled;
        DialogResult = true;
        Close();
    }

    private void OnCancelled(object? sender, EventArgs e)
    {
        PaymentResult = null;
        _viewModel.PaymentConfirmed -= OnPaymentConfirmed;
        _viewModel.Cancelled -= OnCancelled;
        DialogResult = false;
        Close();
    }
}
