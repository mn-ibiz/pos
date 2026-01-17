using HospitalityPOS.Core.Models.Currency;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing currencies and exchange rates.
/// Supports multi-currency payments for border towns and tourist areas.
/// </summary>
public interface ICurrencyService
{
    #region Currency Management

    /// <summary>
    /// Gets all currencies in the system.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active currencies.</param>
    Task<List<CurrencyDto>> GetAllCurrenciesAsync(bool activeOnly = true);

    /// <summary>
    /// Gets a currency by its code.
    /// </summary>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    Task<CurrencyDto?> GetCurrencyByCodeAsync(string currencyCode);

    /// <summary>
    /// Gets a currency by its ID.
    /// </summary>
    /// <param name="currencyId">The currency ID.</param>
    Task<CurrencyDto?> GetCurrencyByIdAsync(int currencyId);

    /// <summary>
    /// Gets the default (base) currency.
    /// </summary>
    Task<CurrencyDto> GetDefaultCurrencyAsync();

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    /// <param name="request">The currency creation request.</param>
    Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyRequest request);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    /// <param name="currencyId">The currency ID to update.</param>
    /// <param name="request">The update request.</param>
    Task<CurrencyDto?> UpdateCurrencyAsync(int currencyId, CreateCurrencyRequest request);

    /// <summary>
    /// Activates or deactivates a currency.
    /// </summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <param name="isActive">Whether to activate or deactivate.</param>
    Task<bool> SetCurrencyActiveStatusAsync(string currencyCode, bool isActive);

    /// <summary>
    /// Sets a currency as the default.
    /// </summary>
    /// <param name="currencyCode">The currency code to set as default.</param>
    Task<bool> SetDefaultCurrencyAsync(string currencyCode);

    #endregion

    #region Exchange Rate Management

    /// <summary>
    /// Gets the current exchange rate between two currencies.
    /// </summary>
    /// <param name="fromCurrencyCode">The source currency code.</param>
    /// <param name="toCurrencyCode">The target currency code.</param>
    Task<ExchangeRateDto?> GetCurrentExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode);

    /// <summary>
    /// Gets all current exchange rates from base currency.
    /// </summary>
    Task<List<ExchangeRateDto>> GetCurrentExchangeRatesAsync();

    /// <summary>
    /// Sets an exchange rate between two currencies.
    /// </summary>
    /// <param name="request">The exchange rate request.</param>
    /// <param name="userId">The user setting the rate.</param>
    Task<ExchangeRateDto> SetExchangeRateAsync(SetExchangeRateRequest request, int userId);

    /// <summary>
    /// Gets exchange rate history for a currency pair.
    /// </summary>
    /// <param name="fromCurrencyCode">The source currency code.</param>
    /// <param name="toCurrencyCode">The target currency code.</param>
    /// <param name="fromDate">Start date for history.</param>
    /// <param name="toDate">End date for history.</param>
    Task<List<ExchangeRateHistoryDto>> GetExchangeRateHistoryAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        DateTime fromDate,
        DateTime toDate);

    /// <summary>
    /// Gets exchange rates that are expiring soon.
    /// </summary>
    /// <param name="daysUntilExpiry">Number of days to look ahead.</param>
    Task<List<ExchangeRateDto>> GetExpiringRatesAsync(int daysUntilExpiry = 1);

    /// <summary>
    /// Checks if a currency pair has a valid (non-expired) exchange rate.
    /// </summary>
    /// <param name="fromCurrencyCode">The source currency code.</param>
    /// <param name="toCurrencyCode">The target currency code.</param>
    Task<bool> HasValidExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode);

    #endregion

    #region Currency Conversion

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrencyCode">The source currency code.</param>
    /// <param name="toCurrencyCode">The target currency code.</param>
    Task<CurrencyConversionResult> ConvertAmountAsync(decimal amount, string fromCurrencyCode, string toCurrencyCode);

    /// <summary>
    /// Converts an amount to base currency (KES).
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrencyCode">The source currency code.</param>
    Task<CurrencyConversionResult> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode);

    /// <summary>
    /// Converts an amount from base currency to target currency.
    /// </summary>
    /// <param name="amount">The amount in base currency.</param>
    /// <param name="toCurrencyCode">The target currency code.</param>
    Task<CurrencyConversionResult> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode);

    /// <summary>
    /// Calculates change for a multi-currency payment.
    /// </summary>
    /// <param name="totalDueInBaseCurrency">Total due in base currency.</param>
    /// <param name="amountPaid">Amount paid by customer.</param>
    /// <param name="paymentCurrencyCode">Currency code of payment.</param>
    Task<ChangeCalculationResult> CalculateChangeAsync(
        decimal totalDueInBaseCurrency,
        decimal amountPaid,
        string paymentCurrencyCode);

    /// <summary>
    /// Applies rounding rules to an amount based on currency settings.
    /// </summary>
    /// <param name="amount">The amount to round.</param>
    /// <param name="currencyCode">The currency code.</param>
    Task<decimal> ApplyRoundingAsync(decimal amount, string currencyCode);

    #endregion

    #region Payment Integration

    /// <summary>
    /// Creates a multi-currency payment record.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="amountInBaseCurrency">Amount due in base currency.</param>
    /// <param name="amountPaid">Amount paid by customer.</param>
    /// <param name="paymentCurrencyCode">Currency of payment.</param>
    Task<MultiCurrencyPaymentDto> CreateMultiCurrencyPaymentAsync(
        int receiptId,
        decimal amountInBaseCurrency,
        decimal amountPaid,
        string paymentCurrencyCode);

    /// <summary>
    /// Gets multi-currency payment details for a receipt.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    Task<MultiCurrencyPaymentDto?> GetMultiCurrencyPaymentAsync(int receiptId);

    #endregion

    #region Cash Drawer Tracking

    /// <summary>
    /// Gets cash drawer balances per currency for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    Task<List<CashDrawerCurrencyDto>> GetCashDrawerCurrenciesAsync(int workPeriodId);

    /// <summary>
    /// Records a cash count for a specific currency.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="currencyCode">The currency code.</param>
    /// <param name="actualCount">The counted amount.</param>
    Task<CashDrawerCurrencyDto> RecordCashCountAsync(int workPeriodId, string currencyCode, decimal actualCount);

    /// <summary>
    /// Sets opening float for a currency.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="currencyCode">The currency code.</param>
    /// <param name="openingFloat">The opening float amount.</param>
    Task<bool> SetOpeningFloatAsync(int workPeriodId, string currencyCode, decimal openingFloat);

    #endregion

    #region Reporting

    /// <summary>
    /// Gets currency report summary for a date range.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    Task<List<CurrencyReportSummaryDto>> GetCurrencyReportAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets exchange gain/loss report.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    Task<decimal> GetExchangeGainLossAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets foreign currency summary for X/Z reports.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    Task<List<CurrencyReportSummaryDto>> GetWorkPeriodCurrencySummaryAsync(int workPeriodId);

    #endregion

    #region Settings

    /// <summary>
    /// Gets current currency settings.
    /// </summary>
    Task<CurrencySettingsDto> GetCurrencySettingsAsync();

    /// <summary>
    /// Updates currency settings.
    /// </summary>
    /// <param name="settings">The settings to update.</param>
    Task<CurrencySettingsDto> UpdateCurrencySettingsAsync(CurrencySettingsDto settings);

    /// <summary>
    /// Checks if multi-currency is enabled.
    /// </summary>
    Task<bool> IsMultiCurrencyEnabledAsync();

    /// <summary>
    /// Enables or disables multi-currency support.
    /// </summary>
    /// <param name="enabled">Whether to enable multi-currency.</param>
    Task<bool> SetMultiCurrencyEnabledAsync(bool enabled);

    #endregion

    #region Formatting

    /// <summary>
    /// Formats an amount with the currency symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currencyCode">The currency code.</param>
    Task<string> FormatAmountAsync(decimal amount, string currencyCode);

    /// <summary>
    /// Formats exchange rate display text.
    /// </summary>
    /// <param name="fromCurrencyCode">Source currency.</param>
    /// <param name="toCurrencyCode">Target currency.</param>
    /// <param name="rate">The exchange rate.</param>
    string FormatExchangeRate(string fromCurrencyCode, string toCurrencyCode, decimal rate);

    #endregion
}
