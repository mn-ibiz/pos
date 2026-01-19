using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.ViewModels.Dialogs;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for ReceiptPreviewDialog.xaml
/// </summary>
public partial class ReceiptPreviewDialog : Window
{
    private readonly ReceiptPreviewDialogViewModel _viewModel;

    /// <summary>
    /// Gets whether the user requested printing.
    /// </summary>
    public bool PrintRequested => _viewModel.PrintRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptPreviewDialog"/> class.
    /// </summary>
    public ReceiptPreviewDialog()
    {
        InitializeComponent();
        _viewModel = new ReceiptPreviewDialogViewModel();
        DataContext = _viewModel;

        _viewModel.RequestClose += (s, e) =>
        {
            DialogResult = _viewModel.PrintRequested;
            Close();
        };
    }

    /// <summary>
    /// Initializes a new instance with order data.
    /// </summary>
    /// <param name="order">The order to preview.</param>
    /// <param name="config">The system configuration for the header.</param>
    /// <param name="cashierName">The cashier name.</param>
    /// <param name="receiptNumber">The receipt number.</param>
    public ReceiptPreviewDialog(Order order, SystemConfiguration? config = null, string? cashierName = null, string? receiptNumber = null)
        : this()
    {
        _viewModel.LoadFromOrder(order, config, cashierName);
        if (!string.IsNullOrEmpty(receiptNumber))
        {
            _viewModel.ReceiptNumber = receiptNumber;
        }
    }

    /// <summary>
    /// Sets the payment information for display.
    /// </summary>
    /// <param name="paymentMethod">The payment method name.</param>
    /// <param name="amountTendered">The amount tendered.</param>
    /// <param name="changeDue">The change due.</param>
    /// <param name="mpesaRef">Optional M-Pesa reference.</param>
    public void SetPaymentInfo(string paymentMethod, decimal amountTendered, decimal changeDue, string? mpesaRef = null)
    {
        _viewModel.SetPaymentInfo(paymentMethod, amountTendered, changeDue, mpesaRef);
    }

    /// <summary>
    /// Sets the receipt number.
    /// </summary>
    public void SetReceiptNumber(string receiptNumber)
    {
        _viewModel.ReceiptNumber = receiptNumber;
    }

    /// <summary>
    /// Gets the ViewModel for direct manipulation.
    /// </summary>
    public ReceiptPreviewDialogViewModel ViewModel => _viewModel;
}
