using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the receipt preview dialog.
/// </summary>
public partial class ReceiptPreviewDialogViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the business name.
    /// </summary>
    [ObservableProperty]
    private string _businessName = "HospitalityPOS";

    /// <summary>
    /// Gets or sets the business address.
    /// </summary>
    [ObservableProperty]
    private string _businessAddress = string.Empty;

    /// <summary>
    /// Gets or sets the business phone.
    /// </summary>
    [ObservableProperty]
    private string _businessPhone = string.Empty;

    /// <summary>
    /// Gets or sets the business email.
    /// </summary>
    [ObservableProperty]
    private string _businessEmail = string.Empty;

    /// <summary>
    /// Gets or sets the KRA PIN.
    /// </summary>
    [ObservableProperty]
    private string _kraPin = string.Empty;

    /// <summary>
    /// Gets or sets the business logo image.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    private BitmapImage? _logoImage;

    /// <summary>
    /// Gets whether a logo is available.
    /// </summary>
    public bool HasLogo => LogoImage != null;

    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    [ObservableProperty]
    private string _receiptNumber = string.Empty;

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    [ObservableProperty]
    private string _orderNumber = string.Empty;

    /// <summary>
    /// Gets or sets the receipt date/time.
    /// </summary>
    [ObservableProperty]
    private DateTime _receiptDateTime = DateTime.Now;

    /// <summary>
    /// Gets or sets the cashier name.
    /// </summary>
    [ObservableProperty]
    private string _cashierName = string.Empty;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [ObservableProperty]
    private string _customerName = string.Empty;

    /// <summary>
    /// Gets or sets the table name (for hospitality mode).
    /// </summary>
    [ObservableProperty]
    private string _tableName = string.Empty;

    /// <summary>
    /// Gets or sets the receipt items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReceiptItemViewModel> _items = [];

    /// <summary>
    /// Gets or sets the subtotal.
    /// </summary>
    [ObservableProperty]
    private decimal _subtotal;

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    [ObservableProperty]
    private decimal _discountAmount;

    /// <summary>
    /// Gets or sets the discount description.
    /// </summary>
    [ObservableProperty]
    private string _discountDescription = string.Empty;

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    [ObservableProperty]
    private decimal _taxAmount;

    /// <summary>
    /// Gets or sets the total.
    /// </summary>
    [ObservableProperty]
    private decimal _total;

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    [ObservableProperty]
    private string _paymentMethod = "Cash";

    /// <summary>
    /// Gets or sets the amount tendered.
    /// </summary>
    [ObservableProperty]
    private decimal _amountTendered;

    /// <summary>
    /// Gets or sets the change due.
    /// </summary>
    [ObservableProperty]
    private decimal _changeDue;

    /// <summary>
    /// Gets or sets the M-Pesa reference (if applicable).
    /// </summary>
    [ObservableProperty]
    private string _mpesaReference = string.Empty;

    /// <summary>
    /// Gets or sets the footer message.
    /// </summary>
    [ObservableProperty]
    private string _footerMessage = "Thank you for your business!";

    /// <summary>
    /// Gets whether the dialog was confirmed (print requested).
    /// </summary>
    public bool PrintRequested { get; private set; }

    /// <summary>
    /// Gets whether a discount was applied.
    /// </summary>
    public bool HasDiscount => DiscountAmount > 0;

    /// <summary>
    /// Gets whether there's change due.
    /// </summary>
    public bool HasChange => ChangeDue > 0;

    /// <summary>
    /// Gets whether M-Pesa reference should be shown.
    /// </summary>
    public bool HasMpesaReference => !string.IsNullOrEmpty(MpesaReference);

    /// <summary>
    /// Gets whether a customer is assigned.
    /// </summary>
    public bool HasCustomer => !string.IsNullOrEmpty(CustomerName);

    /// <summary>
    /// Gets whether table info should be shown.
    /// </summary>
    public bool HasTable => !string.IsNullOrEmpty(TableName);

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptPreviewDialogViewModel"/> class.
    /// </summary>
    public ReceiptPreviewDialogViewModel()
    {
    }

    /// <summary>
    /// Loads receipt data from an order.
    /// </summary>
    public void LoadFromOrder(Order order, SystemConfiguration? config = null, string? cashierName = null)
    {
        // Business info
        if (config != null)
        {
            BusinessName = config.BusinessName;
            BusinessAddress = config.BusinessAddress ?? string.Empty;
            BusinessPhone = config.BusinessPhone ?? string.Empty;
            BusinessEmail = config.BusinessEmail ?? string.Empty;
            KraPin = config.KraPinNumber ?? string.Empty;
            LoadLogo(config.LogoPath);
        }

        // Order info
        OrderNumber = order.OrderNumber ?? $"#{order.Id}";
        ReceiptDateTime = order.CreatedAt;
        CashierName = cashierName ?? order.User?.FullName ?? "Staff";
        CustomerName = order.CustomerName ?? string.Empty;
        TableName = order.TableNumber ?? string.Empty;

        // Items
        Items.Clear();
        foreach (var item in order.OrderItems)
        {
            Items.Add(new ReceiptItemViewModel
            {
                ProductName = item.Product?.Name ?? "Item",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.TotalAmount,
                HasDiscount = item.DiscountAmount > 0,
                DiscountAmount = item.DiscountAmount
            });
        }

        // Totals
        Subtotal = order.Subtotal;
        DiscountAmount = order.DiscountAmount;
        DiscountDescription = order.DiscountAmount > 0 ? "Discount" : string.Empty;
        TaxAmount = order.TaxAmount;
        Total = order.TotalAmount;
    }

    /// <summary>
    /// Sets payment information.
    /// </summary>
    public void SetPaymentInfo(string paymentMethod, decimal amountTendered, decimal changeDue, string? mpesaRef = null)
    {
        PaymentMethod = paymentMethod;
        AmountTendered = amountTendered;
        ChangeDue = changeDue;
        MpesaReference = mpesaRef ?? string.Empty;
    }

    /// <summary>
    /// Confirms printing and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Print()
    {
        PrintRequested = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels and closes the dialog without printing.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        PrintRequested = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Loads the business logo from the specified path.
    /// </summary>
    private void LoadLogo(string? logoPath)
    {
        if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath))
        {
            LogoImage = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            LogoImage = bitmap;
        }
        catch
        {
            LogoImage = null;
        }
    }
}

/// <summary>
/// Represents a receipt line item for display.
/// </summary>
public class ReceiptItemViewModel
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the line total.
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Gets or sets whether the item has a discount.
    /// </summary>
    public bool HasDiscount { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets the quantity and price display.
    /// </summary>
    public string QuantityDisplay => $"{Quantity:N0} x {UnitPrice:N2}";
}
