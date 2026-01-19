using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.WPF.ViewModels.Models;

namespace HospitalityPOS.WPF.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Split Payment dialog.
/// </summary>
public partial class SplitPaymentDialogViewModel : ObservableObject
{
    /// <summary>
    /// Gets the order total amount.
    /// </summary>
    public decimal OrderTotal { get; }

    /// <summary>
    /// Gets or sets the list of payments added.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentEntry> _payments = [];

    /// <summary>
    /// Gets or sets the current payment amount input.
    /// </summary>
    [ObservableProperty]
    private string _paymentAmount = "";

    /// <summary>
    /// Gets the total amount paid so far.
    /// </summary>
    public decimal TotalPaid => Payments.Sum(p => p.Amount);

    /// <summary>
    /// Gets the remaining balance to pay.
    /// </summary>
    public decimal RemainingBalance => Math.Max(0, OrderTotal - TotalPaid);

    /// <summary>
    /// Gets whether the order is fully paid.
    /// </summary>
    public bool IsFullyPaid => RemainingBalance <= 0;

    /// <summary>
    /// Gets or sets whether the dialog was completed successfully.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitPaymentDialogViewModel"/> class.
    /// </summary>
    /// <param name="orderTotal">The total order amount.</param>
    public SplitPaymentDialogViewModel(decimal orderTotal)
    {
        OrderTotal = orderTotal;
        PaymentAmount = orderTotal.ToString("F2");
    }

    /// <summary>
    /// Adds a cash payment.
    /// </summary>
    [RelayCommand]
    private void AddCashPayment()
    {
        if (!TryParseAmount(out var amount)) return;

        Payments.Add(new PaymentEntry
        {
            Method = SplitPaymentMethod.Cash,
            MethodName = "Cash",
            MethodIcon = "\uE8C7",
            MethodColor = "#22C55E",
            Amount = Math.Min(amount, RemainingBalance)
        });

        RefreshBalances();
    }

    /// <summary>
    /// Adds an M-Pesa payment.
    /// </summary>
    [RelayCommand]
    private void AddMpesaPayment()
    {
        if (!TryParseAmount(out var amount)) return;

        Payments.Add(new PaymentEntry
        {
            Method = SplitPaymentMethod.Mpesa,
            MethodName = "M-Pesa",
            MethodIcon = "\uE8EA",
            MethodColor = "#16A34A",
            Amount = Math.Min(amount, RemainingBalance)
        });

        RefreshBalances();
    }

    /// <summary>
    /// Adds a card payment.
    /// </summary>
    [RelayCommand]
    private void AddCardPayment()
    {
        if (!TryParseAmount(out var amount)) return;

        Payments.Add(new PaymentEntry
        {
            Method = SplitPaymentMethod.Card,
            MethodName = "Card",
            MethodIcon = "\uE8C7",
            MethodColor = "#3B82F6",
            Amount = Math.Min(amount, RemainingBalance)
        });

        RefreshBalances();
    }

    /// <summary>
    /// Removes a payment from the list.
    /// </summary>
    [RelayCommand]
    private void RemovePayment(PaymentEntry? payment)
    {
        if (payment != null)
        {
            Payments.Remove(payment);
            RefreshBalances();
        }
    }

    /// <summary>
    /// Sets the payment amount to the remaining balance.
    /// </summary>
    [RelayCommand]
    private void SetRemaining()
    {
        PaymentAmount = RemainingBalance.ToString("F2");
    }

    /// <summary>
    /// Sets the payment amount to half the order total.
    /// </summary>
    [RelayCommand]
    private void SetHalf()
    {
        PaymentAmount = (OrderTotal / 2).ToString("F2");
    }

    /// <summary>
    /// Clears the payment amount for custom entry.
    /// </summary>
    [RelayCommand]
    private void ClearAmount()
    {
        PaymentAmount = "";
    }

    /// <summary>
    /// Completes the split payment.
    /// </summary>
    [RelayCommand]
    private void Complete()
    {
        if (IsFullyPaid)
        {
            IsCompleted = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Cancels the split payment.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        IsCompleted = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private bool TryParseAmount(out decimal amount)
    {
        amount = 0;
        if (!decimal.TryParse(PaymentAmount, out amount) || amount <= 0)
        {
            return false;
        }
        return true;
    }

    private void RefreshBalances()
    {
        OnPropertyChanged(nameof(TotalPaid));
        OnPropertyChanged(nameof(RemainingBalance));
        OnPropertyChanged(nameof(IsFullyPaid));
        PaymentAmount = RemainingBalance.ToString("F2");
    }
}
