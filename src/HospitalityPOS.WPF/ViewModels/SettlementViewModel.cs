using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the receipt settlement screen.
/// </summary>
public partial class SettlementViewModel : ViewModelBase, INavigationAware
{
    private readonly IReceiptService _receiptService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IOwnershipService _ownershipService;
    private readonly IPermissionOverrideService _permissionOverrideService;
    private readonly ISessionService _sessionService;
    private readonly ICashDrawerService _cashDrawerService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the receipt being settled.
    /// </summary>
    [ObservableProperty]
    private Receipt? _receipt;

    /// <summary>
    /// Gets or sets the receipt items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReceiptItemViewModel> _receiptItems = [];

    /// <summary>
    /// Gets or sets the available payment methods.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _paymentMethods = [];

    /// <summary>
    /// Gets or sets the selected payment method.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCashPayment))]
    [NotifyPropertyChangedFor(nameof(IsMpesaPayment))]
    [NotifyPropertyChangedFor(nameof(IsCardPayment))]
    [NotifyPropertyChangedFor(nameof(RequiresReference))]
    [NotifyPropertyChangedFor(nameof(IsPaymentMethodSelected))]
    [NotifyPropertyChangedFor(nameof(IsMpesaCodeValid))]
    [NotifyPropertyChangedFor(nameof(MpesaValidationMessage))]
    private PaymentMethod? _selectedPaymentMethod;

    /// <summary>
    /// Gets or sets the amount due (remaining balance).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuickAmounts))]
    private decimal _amountDue;

    /// <summary>
    /// Gets or sets the total paid amount so far.
    /// </summary>
    [ObservableProperty]
    private decimal _amountPaid;

    /// <summary>
    /// Gets or sets the amount tendered by the customer.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeDue))]
    [NotifyPropertyChangedFor(nameof(CanCompletePayment))]
    private decimal _amountTendered;

    /// <summary>
    /// Gets or sets the reference number (for M-Pesa, card payments).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompletePayment))]
    [NotifyPropertyChangedFor(nameof(IsMpesaCodeValid))]
    [NotifyPropertyChangedFor(nameof(MpesaValidationMessage))]
    private string _referenceNumber = "";

    /// <summary>
    /// Gets or sets whether the customer has confirmed the M-Pesa payment.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompletePayment))]
    private bool _customerConfirmed;

    /// <summary>
    /// Gets or sets the last 4 digits of the card (optional for card payments).
    /// </summary>
    [ObservableProperty]
    private string _cardLastFourDigits = "";

    /// <summary>
    /// Gets or sets the list of payments made.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentLineViewModel> _paymentLines = [];

    /// <summary>
    /// Gets or sets whether the payment panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isPaymentPanelVisible;

    /// <summary>
    /// Gets or sets whether the settlement is complete.
    /// </summary>
    [ObservableProperty]
    private bool _isSettlementComplete;

    /// <summary>
    /// Gets or sets the final change amount to give.
    /// </summary>
    [ObservableProperty]
    private decimal _finalChangeAmount;

    /// <summary>
    /// Gets or sets the entered amount as a string (for numeric keypad binding).
    /// </summary>
    [ObservableProperty]
    private string _enteredAmountString = "0";

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether cash payment is selected.
    /// </summary>
    public bool IsCashPayment => SelectedPaymentMethod?.Type == PaymentMethodType.Cash;

    /// <summary>
    /// Gets whether M-Pesa payment is selected.
    /// </summary>
    public bool IsMpesaPayment => SelectedPaymentMethod?.Type == PaymentMethodType.MPesa;

    /// <summary>
    /// Gets whether card payment is selected.
    /// </summary>
    public bool IsCardPayment => SelectedPaymentMethod?.Type == PaymentMethodType.Card;

    /// <summary>
    /// Gets whether the selected payment method requires a reference.
    /// </summary>
    public bool RequiresReference => SelectedPaymentMethod?.RequiresReference ?? false;

    /// <summary>
    /// Gets whether a payment method is selected.
    /// </summary>
    public bool IsPaymentMethodSelected => SelectedPaymentMethod is not null;

    /// <summary>
    /// Gets whether the M-Pesa code is valid.
    /// </summary>
    public bool IsMpesaCodeValid
    {
        get
        {
            if (!IsMpesaPayment || string.IsNullOrEmpty(ReferenceNumber))
                return false;

            return ValidateMpesaCode(ReferenceNumber);
        }
    }

    /// <summary>
    /// Gets the M-Pesa validation message (if any).
    /// </summary>
    public string? MpesaValidationMessage
    {
        get
        {
            if (!IsMpesaPayment || string.IsNullOrEmpty(ReferenceNumber))
                return null;

            return GetMpesaValidationMessage(ReferenceNumber);
        }
    }

    /// <summary>
    /// Gets the calculated change due.
    /// </summary>
    public decimal ChangeDue => IsCashPayment && AmountTendered > AmountDue
        ? AmountTendered - AmountDue
        : 0;

    /// <summary>
    /// Gets whether the payment can be completed.
    /// </summary>
    public bool CanCompletePayment
    {
        get
        {
            if (SelectedPaymentMethod is null) return false;

            if (IsCashPayment)
            {
                return AmountTendered >= AmountDue;
            }

            if (IsMpesaPayment)
            {
                // M-Pesa requires valid code and customer confirmation
                return IsMpesaCodeValid && CustomerConfirmed;
            }

            if (IsCardPayment)
            {
                // Card payment just requires card type selected (method is selected)
                // Last 4 digits are optional
                return true;
            }

            if (RequiresReference)
            {
                return !string.IsNullOrWhiteSpace(ReferenceNumber);
            }

            return true;
        }
    }

    /// <summary>
    /// Gets quick amount buttons for cash payment.
    /// </summary>
    public ObservableCollection<decimal> QuickAmounts
    {
        get
        {
            var amounts = new ObservableCollection<decimal>();
            if (AmountDue <= 0) return amounts;

            // Exact amount
            amounts.Add(AmountDue);

            // Round up to nearest 50
            var roundTo50 = Math.Ceiling(AmountDue / 50) * 50;
            if (roundTo50 > AmountDue && !amounts.Contains(roundTo50))
                amounts.Add(roundTo50);

            // Round up to nearest 100
            var roundTo100 = Math.Ceiling(AmountDue / 100) * 100;
            if (roundTo100 > AmountDue && !amounts.Contains(roundTo100))
                amounts.Add(roundTo100);

            // Common amounts
            var commonAmounts = new[] { 500m, 1000m, 2000m, 5000m, 10000m };
            foreach (var amt in commonAmounts)
            {
                if (amt >= AmountDue && !amounts.Contains(amt) && amounts.Count < 6)
                    amounts.Add(amt);
            }

            return amounts;
        }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SettlementViewModel"/> class.
    /// </summary>
    public SettlementViewModel(
        ILogger logger,
        IReceiptService receiptService,
        INavigationService navigationService,
        IDialogService dialogService,
        IOwnershipService ownershipService,
        IPermissionOverrideService permissionOverrideService,
        ISessionService sessionService,
        ICashDrawerService cashDrawerService)
        : base(logger)
    {
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _ownershipService = ownershipService ?? throw new ArgumentNullException(nameof(ownershipService));
        _permissionOverrideService = permissionOverrideService ?? throw new ArgumentNullException(nameof(permissionOverrideService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));

        Title = "Settle Receipt";
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(object? parameter)
    {
        try
        {
            // Parameter should be receipt ID
            if (parameter is not int receiptId)
            {
                await _dialogService.ShowErrorAsync("Error", "No receipt specified for settlement.");
                _navigationService.GoBack();
                return;
            }

            // Validate ownership before loading
            var ownershipResult = await _ownershipService.ValidateReceiptOwnershipAsync(receiptId);
            if (!ownershipResult.IsValid)
            {
                // User is not the owner - request manager override
                var overrideResult = await RequestOwnershipOverrideAsync(
                    receiptId,
                    ownershipResult.OwnerId ?? 0,
                    ownershipResult.OwnerName ?? "Unknown User",
                    "Settle Receipt");

                if (!overrideResult)
                {
                    _navigationService.GoBack();
                    return;
                }
            }

            await LoadReceiptAsync(receiptId);
            await LoadPaymentMethodsAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize settlement screen");
            await _dialogService.ShowErrorAsync("Error", "Failed to load receipt for settlement.");
            _navigationService.GoBack();
        }
    }

    /// <summary>
    /// Requests a manager override for accessing another user's receipt.
    /// </summary>
    private async Task<bool> RequestOwnershipOverrideAsync(int receiptId, int ownerId, string ownerName, string actionDescription)
    {
        // Show ownership override dialog
        var dialogResult = await _dialogService.ShowOwnershipOverrideDialogAsync(
            ownerName,
            actionDescription);

        if (dialogResult is null)
        {
            // User cancelled
            await _ownershipService.LogOwnershipDenialAsync(
                receiptId,
                ownerId,
                _sessionService.CurrentUserId,
                $"{actionDescription} - User cancelled override");
            return false;
        }

        var (pin, reason) = dialogResult.Value;

        // Validate the override with PIN
        var overrideResult = await _permissionOverrideService.ValidatePinAndAuthorizeAsync(
            pin,
            PermissionNames.Receipts.ModifyOther,
            $"{actionDescription} for receipt owned by {ownerName}",
            _sessionService.CurrentUserId);

        if (!overrideResult.IsAuthorized)
        {
            await _dialogService.ShowErrorAsync(
                "Authorization Denied",
                overrideResult.ErrorMessage ?? "Invalid authorization");

            await _ownershipService.LogOwnershipDenialAsync(
                receiptId,
                ownerId,
                _sessionService.CurrentUserId,
                $"{actionDescription} - Authorization denied: {overrideResult.ErrorMessage}");
            return false;
        }

        // Log successful override
        await _ownershipService.AuthorizeWithOverrideAsync(
            receiptId,
            overrideResult.AuthorizingUserId ?? 0,
            overrideResult.AuthorizingUserName ?? "Unknown",
            reason,
            actionDescription);

        _logger.Information(
            "Receipt {ReceiptId} ownership override authorized. AuthorizedBy: {AuthorizedBy}, Reason: {Reason}",
            receiptId, overrideResult.AuthorizingUserName, reason);

        return true;
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    partial void OnAmountTenderedChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(CanCompletePayment));
    }

    partial void OnEnteredAmountStringChanged(string value)
    {
        if (decimal.TryParse(value, out var amount))
        {
            AmountTendered = amount;
        }
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod? value)
    {
        if (value is not null)
        {
            IsPaymentPanelVisible = true;

            // Pre-fill amount for non-cash payments
            if (value.Type != PaymentMethodType.Cash)
            {
                AmountTendered = AmountDue;
                EnteredAmountString = ((long)AmountDue).ToString();
            }
            else
            {
                AmountTendered = 0;
                EnteredAmountString = "0";
            }

            ReferenceNumber = "";
            CustomerConfirmed = false;
            CardLastFourDigits = "";
        }

        OnPropertyChanged(nameof(QuickAmounts));
        CompletePaymentCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Validates an M-Pesa transaction code format.
    /// Code must be 10 characters, start with a letter, and contain only letters and numbers.
    /// </summary>
    private static bool ValidateMpesaCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;

        if (code.Length != 10)
            return false;

        if (!char.IsLetter(code[0]))
            return false;

        return code.All(c => char.IsLetterOrDigit(c));
    }

    /// <summary>
    /// Gets a validation message for an M-Pesa code.
    /// Returns null if the code is valid.
    /// </summary>
    private static string? GetMpesaValidationMessage(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        if (code.Length != 10)
            return $"Code must be 10 characters (currently {code.Length})";

        if (!char.IsLetter(code[0]))
            return "Code must start with a letter";

        if (!code.All(c => char.IsLetterOrDigit(c)))
            return "Code must contain only letters and numbers";

        return null;
    }

    #region Commands

    /// <summary>
    /// Loads the receipt for settlement.
    /// </summary>
    private async Task LoadReceiptAsync(int receiptId)
    {
        await ExecuteAsync(async () =>
        {
            Receipt = await _receiptService.GetByIdAsync(receiptId);
            if (Receipt is null)
            {
                throw new InvalidOperationException($"Receipt {receiptId} not found.");
            }

            if (Receipt.Status != ReceiptStatus.Pending)
            {
                throw new InvalidOperationException($"Receipt {Receipt.ReceiptNumber} is not pending payment.");
            }

            // Build receipt items for display
            ReceiptItems = new ObservableCollection<ReceiptItemViewModel>(
                Receipt.ReceiptItems.Select(ri => new ReceiptItemViewModel
                {
                    ProductName = ri.ProductName,
                    Quantity = ri.Quantity,
                    UnitPrice = ri.UnitPrice,
                    DiscountAmount = ri.DiscountAmount,
                    TotalAmount = ri.TotalAmount
                }));

            // Set initial amount due
            AmountDue = Receipt.TotalAmount - Receipt.PaidAmount;
            AmountPaid = Receipt.PaidAmount;

            // Load existing payments
            if (Receipt.Payments.Any())
            {
                PaymentLines = new ObservableCollection<PaymentLineViewModel>(
                    Receipt.Payments.Select(p => new PaymentLineViewModel
                    {
                        MethodName = p.PaymentMethod?.Name ?? "Unknown",
                        Amount = p.Amount,
                        Reference = p.Reference
                    }));
            }

            _logger.Debug("Loaded receipt {ReceiptNumber} for settlement, Amount Due: {AmountDue}",
                Receipt.ReceiptNumber, AmountDue);
        }, "Loading receipt...").ConfigureAwait(true);
    }

    /// <summary>
    /// Loads available payment methods.
    /// </summary>
    private async Task LoadPaymentMethodsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var methods = await _receiptService.GetPaymentMethodsAsync();
            PaymentMethods = new ObservableCollection<PaymentMethod>(methods);

            _logger.Debug("Loaded {Count} payment methods", PaymentMethods.Count);
        }, "Loading payment methods...").ConfigureAwait(true);
    }

    /// <summary>
    /// Selects a payment method.
    /// </summary>
    [RelayCommand]
    private void SelectPaymentMethod(PaymentMethod method)
    {
        SelectedPaymentMethod = method;
    }

    /// <summary>
    /// Applies a quick amount.
    /// </summary>
    [RelayCommand]
    private void ApplyQuickAmount(decimal amount)
    {
        AmountTendered = amount;
        EnteredAmountString = ((long)amount).ToString();
    }

    /// <summary>
    /// Completes the current payment.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCompletePayment))]
    private async Task CompletePaymentAsync()
    {
        if (SelectedPaymentMethod is null || Receipt is null) return;

        try
        {
            // Check for duplicate M-Pesa code
            if (IsMpesaPayment && !string.IsNullOrWhiteSpace(ReferenceNumber))
            {
                var existingPayment = await _receiptService.GetPaymentByReferenceAsync(ReferenceNumber.ToUpperInvariant());
                if (existingPayment is not null)
                {
                    await _dialogService.ShowErrorAsync(
                        "Duplicate M-Pesa Code",
                        $"This M-Pesa code has already been used on receipt {existingPayment.Receipt?.ReceiptNumber ?? "Unknown"}.\n\nPlease verify the code and try again.");
                    return;
                }
            }

            // Calculate payment amount and change
            var paymentAmount = Math.Min(AmountTendered, AmountDue);
            var changeAmount = IsCashPayment ? Math.Max(0, AmountTendered - AmountDue) : 0;

            // Determine reference based on payment type
            string? reference = null;
            if (IsMpesaPayment)
            {
                reference = ReferenceNumber?.ToUpperInvariant();
            }
            else if (IsCardPayment && !string.IsNullOrWhiteSpace(CardLastFourDigits))
            {
                reference = $"****{CardLastFourDigits}";
            }
            else if (RequiresReference)
            {
                reference = ReferenceNumber?.ToUpperInvariant();
            }

            // Create payment record
            var payment = new Payment
            {
                ReceiptId = Receipt.Id,
                PaymentMethodId = SelectedPaymentMethod.Id,
                Amount = paymentAmount,
                TenderedAmount = IsCashPayment ? AmountTendered : paymentAmount,
                ChangeAmount = changeAmount,
                Reference = reference
            };

            // Add payment
            await _receiptService.AddPaymentAsync(payment);

            // Add to payment lines for display
            PaymentLines.Add(new PaymentLineViewModel
            {
                PaymentId = payment.Id,
                PaymentMethodId = SelectedPaymentMethod.Id,
                MethodName = SelectedPaymentMethod.Name,
                Amount = paymentAmount,
                Reference = payment.Reference,
                ChangeAmount = changeAmount,
                TenderedAmount = IsCashPayment ? AmountTendered : paymentAmount
            });

            // Update totals
            AmountPaid += paymentAmount;
            AmountDue = Receipt.TotalAmount - AmountPaid;

            _logger.Information("Added {Method} payment of {Amount:C} to receipt {ReceiptNumber}",
                SelectedPaymentMethod.Name, paymentAmount, Receipt.ReceiptNumber);

            // Check if fully settled
            if (AmountDue <= 0)
            {
                await FinalizeSettlementAsync(changeAmount);
            }
            else
            {
                // Reset for next payment
                ResetPaymentPanel();
                await _dialogService.ShowMessageAsync(
                    "Payment Applied",
                    $"Payment of KSh {paymentAmount:N2} applied. Remaining balance: KSh {AmountDue:N2}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process payment");
            await _dialogService.ShowErrorAsync("Payment Error", ex.Message);
        }
    }

    /// <summary>
    /// Settles the receipt with all payments at once.
    /// </summary>
    [RelayCommand]
    private async Task SettleWithCashAsync()
    {
        if (Receipt is null) return;

        // Select cash payment method
        var cashMethod = PaymentMethods.FirstOrDefault(pm => pm.Type == PaymentMethodType.Cash);
        if (cashMethod is null)
        {
            await _dialogService.ShowErrorAsync("Error", "Cash payment method not available.");
            return;
        }

        SelectedPaymentMethod = cashMethod;
        AmountTendered = AmountDue;
        await CompletePaymentAsync();
    }

    /// <summary>
    /// Finalizes the settlement after all payments are complete.
    /// </summary>
    private async Task FinalizeSettlementAsync(decimal totalChange)
    {
        IsSettlementComplete = true;
        FinalChangeAmount = totalChange;
        IsPaymentPanelVisible = false;

        _logger.Information("Receipt {ReceiptNumber} fully settled. Change: {Change:C}",
            Receipt?.ReceiptNumber, totalChange);

        // Open cash drawer if any payment method requires it
        await OpenCashDrawerIfNeededAsync();

        // TODO: Print customer receipt (to be implemented in Epic 12)

        // Show success with change amount
        if (totalChange > 0)
        {
            await _dialogService.ShowMessageAsync(
                "Settlement Complete",
                $"Receipt {Receipt?.ReceiptNumber} has been settled.\n\nCHANGE DUE: KSh {totalChange:N2}");
        }
        else
        {
            await _dialogService.ShowMessageAsync(
                "Settlement Complete",
                $"Receipt {Receipt?.ReceiptNumber} has been settled successfully.");
        }
    }

    /// <summary>
    /// Opens the cash drawer if any payment method requires it.
    /// </summary>
    private async Task OpenCashDrawerIfNeededAsync()
    {
        try
        {
            // Check if any payment method in this settlement opens the drawer
            var shouldOpenDrawer = PaymentLines.Any(pl =>
            {
                var method = PaymentMethods.FirstOrDefault(m => m.Name == pl.MethodName);
                return method?.OpensDrawer ?? false;
            });

            if (shouldOpenDrawer && Receipt is not null)
            {
                var cashPayment = PaymentLines.FirstOrDefault(pl =>
                {
                    var method = PaymentMethods.FirstOrDefault(m => m.Name == pl.MethodName);
                    return method?.OpensDrawer ?? false;
                });

                await _cashDrawerService.OpenDrawerForPaymentAsync(
                    Receipt.ReceiptNumber,
                    "CASH",
                    cashPayment?.Amount ?? 0);

                _logger.Information("Cash drawer opened for receipt {ReceiptNumber}",
                    Receipt.ReceiptNumber);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the settlement if drawer fails to open
            _logger.Warning(ex, "Failed to open cash drawer, continuing with settlement");
        }
    }

    /// <summary>
    /// Resets the payment panel for the next payment.
    /// </summary>
    private void ResetPaymentPanel()
    {
        SelectedPaymentMethod = null;
        AmountTendered = 0;
        EnteredAmountString = "0";
        ReferenceNumber = "";
        CustomerConfirmed = false;
        CardLastFourDigits = "";
        IsPaymentPanelVisible = false;
    }

    /// <summary>
    /// Cancels the current payment method selection.
    /// </summary>
    [RelayCommand]
    private void CancelPayment()
    {
        ResetPaymentPanel();
    }

    /// <summary>
    /// Removes a payment line from the split payment (before finalization).
    /// </summary>
    [RelayCommand]
    private async Task RemovePaymentLineAsync(PaymentLineViewModel paymentLine)
    {
        if (Receipt is null || paymentLine is null) return;

        // Don't allow removal if settlement is complete
        if (IsSettlementComplete)
        {
            await _dialogService.ShowErrorAsync("Cannot Remove", "Settlement is already complete.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Remove Payment",
            $"Remove {paymentLine.MethodName} payment of KSh {paymentLine.Amount:N2}?");

        if (!confirmed) return;

        try
        {
            // Remove from database
            var removed = await _receiptService.RemovePaymentAsync(paymentLine.PaymentId);
            if (!removed)
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to remove payment.");
                return;
            }

            // Remove from display list
            PaymentLines.Remove(paymentLine);

            // Update totals
            AmountPaid -= paymentLine.Amount;
            AmountDue = Receipt.TotalAmount - AmountPaid;

            _logger.Information("Removed {Method} payment of {Amount:C} from receipt {ReceiptNumber}",
                paymentLine.MethodName, paymentLine.Amount, Receipt.ReceiptNumber);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove payment line");
            await _dialogService.ShowErrorAsync("Error", "Failed to remove payment.");
        }
    }

    /// <summary>
    /// Closes the settlement screen and returns to POS.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        if (!IsSettlementComplete && AmountDue > 0)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Unsettled Receipt",
                "This receipt has not been fully settled. Are you sure you want to leave?");

            if (!confirmed) return;
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Starts a new order after settlement.
    /// </summary>
    [RelayCommand]
    private void StartNewOrder()
    {
        _navigationService.GoBack();
    }

    #endregion
}

/// <summary>
/// ViewModel for displaying a receipt item.
/// </summary>
public class ReceiptItemViewModel
{
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string QuantityDisplay => Quantity == Math.Floor(Quantity)
        ? ((int)Quantity).ToString()
        : Quantity.ToString("0.###");

    public bool HasDiscount => DiscountAmount > 0;
}

/// <summary>
/// ViewModel for displaying a payment line.
/// </summary>
public class PaymentLineViewModel
{
    public int PaymentId { get; set; }
    public int PaymentMethodId { get; set; }
    public string MethodName { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal TenderedAmount { get; set; }

    public bool HasReference => !string.IsNullOrEmpty(Reference);
    public bool HasChange => ChangeAmount > 0;
}
