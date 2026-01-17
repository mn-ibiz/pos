using System.Windows;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for QrPaymentDialog.xaml
/// </summary>
public partial class QrPaymentDialog : Window
{
    private readonly QrPaymentDialogViewModel _viewModel;

    /// <summary>
    /// Gets the dialog result with payment details.
    /// </summary>
    public QrPaymentDialogResult? PaymentResult { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QrPaymentDialog"/> class.
    /// </summary>
    public QrPaymentDialog(QrPaymentDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to dialog close event
        _viewModel.DialogClosed += OnDialogClosed;

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
    /// Initializes and shows the dialog for a payment.
    /// </summary>
    public async Task<QrPaymentDialogResult?> ShowForPaymentAsync(int receiptId, decimal amount, string receiptReference)
    {
        await _viewModel.InitializeAsync(receiptId, amount, receiptReference);
        ShowDialog();
        return PaymentResult;
    }

    private void OnDialogClosed(object? sender, QrPaymentDialogResult result)
    {
        PaymentResult = result;
        _viewModel.DialogClosed -= OnDialogClosed;
        DialogResult = result.Success;
        Close();
    }
}
