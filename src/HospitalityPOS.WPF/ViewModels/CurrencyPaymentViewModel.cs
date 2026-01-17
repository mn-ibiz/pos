using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Currency;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for multi-currency payment selection.
/// Allows cashiers to accept payments in foreign currencies.
/// </summary>
public partial class CurrencyPaymentViewModel : ViewModelBase
{
    private readonly ICurrencyService _currencyService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the available currencies.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CurrencyDto> _availableCurrencies = new();

    /// <summary>
    /// Gets or sets the selected currency.
    /// </summary>
    [ObservableProperty]
    private CurrencyDto? _selectedCurrency;

    /// <summary>
    /// Gets or sets the base (default) currency.
    /// </summary>
    [ObservableProperty]
    private CurrencyDto? _baseCurrency;

    /// <summary>
    /// Gets or sets the amount due in base currency.
    /// </summary>
    [ObservableProperty]
    private decimal _amountDueInBaseCurrency;

    /// <summary>
    /// Gets or sets the amount due in selected currency.
    /// </summary>
    [ObservableProperty]
    private decimal _amountDueInSelectedCurrency;

    /// <summary>
    /// Gets or sets the amount paid by customer.
    /// </summary>
    [ObservableProperty]
    private decimal _amountPaid;

    /// <summary>
    /// Gets or sets the change amount.
    /// </summary>
    [ObservableProperty]
    private decimal _changeAmount;

    /// <summary>
    /// Gets or sets the change currency code.
    /// </summary>
    [ObservableProperty]
    private string _changeCurrencyCode = "KES";

    /// <summary>
    /// Gets or sets the current exchange rate.
    /// </summary>
    [ObservableProperty]
    private decimal _exchangeRate = 1m;

    /// <summary>
    /// Gets or sets the exchange rate display text.
    /// </summary>
    [ObservableProperty]
    private string _exchangeRateDisplay = string.Empty;

    /// <summary>
    /// Gets or sets whether multi-currency is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isMultiCurrencyEnabled;

    /// <summary>
    /// Gets or sets whether loading is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets whether a currency is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrencySelected;

    /// <summary>
    /// Gets or sets whether there's an exchange rate warning.
    /// </summary>
    [ObservableProperty]
    private bool _hasExchangeRateWarning;

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    [ObservableProperty]
    private string? _warningMessage;

    /// <summary>
    /// Gets the formatted amount due in base currency.
    /// </summary>
    public string FormattedAmountDueBase => BaseCurrency != null
        ? $"{BaseCurrency.Symbol} {AmountDueInBaseCurrency:N0}"
        : $"KSh {AmountDueInBaseCurrency:N0}";

    /// <summary>
    /// Gets the formatted amount due in selected currency.
    /// </summary>
    public string FormattedAmountDueSelected => SelectedCurrency != null
        ? $"{SelectedCurrency.Symbol} {AmountDueInSelectedCurrency:N2}"
        : FormattedAmountDueBase;

    /// <summary>
    /// Gets the formatted change amount.
    /// </summary>
    public string FormattedChange => BaseCurrency != null
        ? $"{BaseCurrency.Symbol} {ChangeAmount:N0}"
        : $"KSh {ChangeAmount:N0}";

    #endregion

    /// <summary>
    /// Event raised when payment is confirmed.
    /// </summary>
    public event EventHandler<MultiCurrencyPaymentDto>? PaymentConfirmed;

    /// <summary>
    /// Event raised when currency selection is cancelled.
    /// </summary>
    public event EventHandler? Cancelled;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyPaymentViewModel"/> class.
    /// </summary>
    public CurrencyPaymentViewModel(ICurrencyService currencyService, ILogger logger) : base(logger)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
    }

    #region Public Methods

    /// <summary>
    /// Initializes the view model with payment details.
    /// </summary>
    public async Task InitializeAsync(decimal amountDueInBaseCurrency)
    {
        try
        {
            IsLoading = true;
            AmountDueInBaseCurrency = amountDueInBaseCurrency;

            // Check if multi-currency is enabled
            IsMultiCurrencyEnabled = await _currencyService.IsMultiCurrencyEnabledAsync();

            // Load base currency
            BaseCurrency = await _currencyService.GetDefaultCurrencyAsync();
            ChangeCurrencyCode = BaseCurrency.Code;

            // Load available currencies
            var currencies = await _currencyService.GetAllCurrenciesAsync(activeOnly: true);
            AvailableCurrencies.Clear();
            foreach (var currency in currencies)
            {
                AvailableCurrencies.Add(currency);
            }

            // Default to base currency
            SelectedCurrency = currencies.FirstOrDefault(c => c.IsDefault) ?? currencies.FirstOrDefault();
            await UpdateExchangeRateAsync();

            Logger.Information("Currency payment initialized. Amount: {Amount}, Currencies: {Count}",
                amountDueInBaseCurrency, currencies.Count);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize currency payment");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedCurrencyChanged(CurrencyDto? value)
    {
        IsCurrencySelected = value != null;
        _ = UpdateExchangeRateAsync();
        OnPropertyChanged(nameof(FormattedAmountDueSelected));
    }

    partial void OnAmountPaidChanged(decimal value)
    {
        _ = CalculateChangeAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to select a currency.
    /// </summary>
    [RelayCommand]
    private async Task SelectCurrencyAsync(CurrencyDto currency)
    {
        SelectedCurrency = currency;
        await UpdateExchangeRateAsync();
    }

    /// <summary>
    /// Command to set quick amount.
    /// </summary>
    [RelayCommand]
    private void SetQuickAmount(decimal amount)
    {
        AmountPaid = amount;
    }

    /// <summary>
    /// Command to set exact amount.
    /// </summary>
    [RelayCommand]
    private void SetExactAmount()
    {
        AmountPaid = AmountDueInSelectedCurrency;
    }

    /// <summary>
    /// Command to confirm payment.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (SelectedCurrency == null)
        {
            WarningMessage = "Please select a currency";
            return;
        }

        if (AmountPaid < AmountDueInSelectedCurrency)
        {
            WarningMessage = "Amount paid is less than amount due";
            return;
        }

        try
        {
            var payment = new MultiCurrencyPaymentDto
            {
                PaymentCurrencyId = SelectedCurrency.Id,
                PaymentCurrencyCode = SelectedCurrency.Code,
                PaymentCurrencySymbol = SelectedCurrency.Symbol,
                AmountInPaymentCurrency = AmountPaid,
                AmountInBaseCurrency = AmountPaid * ExchangeRate,
                ExchangeRateUsed = ExchangeRate,
                ChangeInBaseCurrency = ChangeAmount
            };

            Logger.Information("Payment confirmed: {Amount} {Currency}",
                AmountPaid, SelectedCurrency.Code);

            PaymentConfirmed?.Invoke(this, payment);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to confirm payment");
            WarningMessage = "Failed to process payment";
        }
    }

    /// <summary>
    /// Command to cancel selection.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Command to refresh exchange rates.
    /// </summary>
    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        await UpdateExchangeRateAsync();
    }

    #endregion

    #region Private Methods

    private async Task UpdateExchangeRateAsync()
    {
        if (SelectedCurrency == null || BaseCurrency == null)
        {
            ExchangeRate = 1m;
            ExchangeRateDisplay = string.Empty;
            AmountDueInSelectedCurrency = AmountDueInBaseCurrency;
            HasExchangeRateWarning = false;
            return;
        }

        // Same currency - no conversion
        if (SelectedCurrency.Code == BaseCurrency.Code)
        {
            ExchangeRate = 1m;
            ExchangeRateDisplay = string.Empty;
            AmountDueInSelectedCurrency = AmountDueInBaseCurrency;
            HasExchangeRateWarning = false;
            return;
        }

        try
        {
            // Get rate from selected currency to base currency
            var rate = await _currencyService.GetCurrentExchangeRateAsync(
                SelectedCurrency.Code,
                BaseCurrency.Code);

            if (rate == null)
            {
                HasExchangeRateWarning = true;
                WarningMessage = $"No exchange rate available for {SelectedCurrency.Code}";
                ExchangeRate = 0m;
                AmountDueInSelectedCurrency = 0m;
                return;
            }

            // Check if rate is expiring soon
            if (rate.ExpiryDate.HasValue)
            {
                var daysUntilExpiry = (rate.ExpiryDate.Value - DateTime.UtcNow.Date).TotalDays;
                if (daysUntilExpiry <= 1)
                {
                    HasExchangeRateWarning = true;
                    WarningMessage = "Exchange rate expires today/tomorrow";
                }
                else
                {
                    HasExchangeRateWarning = false;
                    WarningMessage = null;
                }
            }

            ExchangeRate = rate.BuyRate;
            ExchangeRateDisplay = _currencyService.FormatExchangeRate(
                SelectedCurrency.Code,
                BaseCurrency.Code,
                rate.BuyRate);

            // Convert base amount to selected currency (invert the rate)
            var invertedRate = 1m / rate.BuyRate;
            var converted = AmountDueInBaseCurrency * invertedRate;
            AmountDueInSelectedCurrency = await _currencyService.ApplyRoundingAsync(
                converted,
                SelectedCurrency.Code);

            OnPropertyChanged(nameof(FormattedAmountDueSelected));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update exchange rate");
            HasExchangeRateWarning = true;
            WarningMessage = "Failed to fetch exchange rate";
        }
    }

    private async Task CalculateChangeAsync()
    {
        if (SelectedCurrency == null || BaseCurrency == null)
        {
            ChangeAmount = 0;
            return;
        }

        try
        {
            var result = await _currencyService.CalculateChangeAsync(
                AmountDueInBaseCurrency,
                AmountPaid,
                SelectedCurrency.Code);

            ChangeAmount = result.ChangeAmount;
            ChangeCurrencyCode = result.ChangeCurrencyCode;
            OnPropertyChanged(nameof(FormattedChange));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to calculate change");
            ChangeAmount = 0;
        }
    }

    #endregion
}

/// <summary>
/// ViewModel for currency settings management.
/// </summary>
public partial class CurrencySettingsViewModel : ViewModelBase
{
    private readonly ICurrencyService _currencyService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets all currencies.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CurrencyDto> _currencies = new();

    /// <summary>
    /// Gets or sets current exchange rates.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ExchangeRateDto> _exchangeRates = new();

    /// <summary>
    /// Gets or sets the selected currency.
    /// </summary>
    [ObservableProperty]
    private CurrencyDto? _selectedCurrency;

    /// <summary>
    /// Gets or sets whether multi-currency is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isMultiCurrencyEnabled;

    /// <summary>
    /// Gets or sets whether loading is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    // Exchange rate entry fields
    [ObservableProperty]
    private string _fromCurrencyCode = string.Empty;

    [ObservableProperty]
    private decimal _buyRate;

    [ObservableProperty]
    private decimal _sellRate;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencySettingsViewModel"/> class.
    /// </summary>
    public CurrencySettingsViewModel(ICurrencyService currencyService, ILogger logger) : base(logger)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
    }

    #region Public Methods

    /// <summary>
    /// Loads the currency settings.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            // Load settings
            var settings = await _currencyService.GetCurrencySettingsAsync();
            IsMultiCurrencyEnabled = settings.MultiCurrencyEnabled;

            // Load currencies
            var currencies = await _currencyService.GetAllCurrenciesAsync(activeOnly: false);
            Currencies.Clear();
            foreach (var currency in currencies)
            {
                Currencies.Add(currency);
            }

            // Load exchange rates
            var rates = await _currencyService.GetCurrentExchangeRatesAsync();
            ExchangeRates.Clear();
            foreach (var rate in rates)
            {
                ExchangeRates.Add(rate);
            }

            Logger.Information("Currency settings loaded");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load currency settings");
            StatusMessage = "Failed to load settings";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to toggle multi-currency support.
    /// </summary>
    [RelayCommand]
    private async Task ToggleMultiCurrencyAsync()
    {
        try
        {
            await _currencyService.SetMultiCurrencyEnabledAsync(IsMultiCurrencyEnabled);
            StatusMessage = IsMultiCurrencyEnabled
                ? "Multi-currency enabled"
                : "Multi-currency disabled";

            Logger.Information("Multi-currency toggled: {Enabled}", IsMultiCurrencyEnabled);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to toggle multi-currency");
            StatusMessage = "Failed to update setting";
        }
    }

    /// <summary>
    /// Command to toggle currency active status.
    /// </summary>
    [RelayCommand]
    private async Task ToggleCurrencyActiveAsync(CurrencyDto currency)
    {
        try
        {
            var newStatus = !currency.IsActive;
            await _currencyService.SetCurrencyActiveStatusAsync(currency.Code, newStatus);
            currency.IsActive = newStatus;

            StatusMessage = $"{currency.Code} {(newStatus ? "activated" : "deactivated")}";
            Logger.Information("Currency {Code} active status: {Status}", currency.Code, newStatus);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to toggle currency status");
            StatusMessage = "Failed to update currency";
        }
    }

    /// <summary>
    /// Command to set exchange rate.
    /// </summary>
    [RelayCommand]
    private async Task SetExchangeRateAsync()
    {
        if (string.IsNullOrWhiteSpace(FromCurrencyCode) || BuyRate <= 0 || SellRate <= 0)
        {
            StatusMessage = "Please enter valid rate details";
            return;
        }

        try
        {
            var request = new SetExchangeRateRequest
            {
                FromCurrencyCode = FromCurrencyCode.ToUpperInvariant(),
                ToCurrencyCode = "KES",
                BuyRate = BuyRate,
                SellRate = SellRate,
                EffectiveDate = DateTime.UtcNow.Date,
                ExpiryDate = DateTime.UtcNow.Date.AddDays(1)
            };

            var rate = await _currencyService.SetExchangeRateAsync(request, 1); // TODO: Get current user ID

            // Refresh rates list
            var rates = await _currencyService.GetCurrentExchangeRatesAsync();
            ExchangeRates.Clear();
            foreach (var r in rates)
            {
                ExchangeRates.Add(r);
            }

            StatusMessage = $"Exchange rate set: 1 {FromCurrencyCode} = {BuyRate} KES";
            Logger.Information("Exchange rate set: {From} -> KES = {Rate}", FromCurrencyCode, BuyRate);

            // Clear form
            FromCurrencyCode = string.Empty;
            BuyRate = 0;
            SellRate = 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set exchange rate");
            StatusMessage = "Failed to set rate";
        }
    }

    /// <summary>
    /// Command to refresh data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    #endregion
}
