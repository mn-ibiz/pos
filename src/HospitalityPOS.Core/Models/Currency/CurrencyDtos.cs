namespace HospitalityPOS.Core.Models.Currency;

/// <summary>
/// Represents a currency supported by the system.
/// </summary>
public class CurrencyDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code (e.g., KES, USD, EUR).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency name (e.g., Kenyan Shilling, US Dollar).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency symbol (e.g., KSh, $, €).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of decimal places for this currency.</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Gets or sets whether this is the default system currency.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this currency is active for transactions.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the display order in currency selectors.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets the rounding rule for this currency.</summary>
    public RoundingRule RoundingRule { get; set; } = RoundingRule.Standard;

    /// <summary>Gets or sets the smallest denomination for this currency.</summary>
    public decimal SmallestDenomination { get; set; } = 0.01m;
}

/// <summary>
/// Rounding rules for currency calculations.
/// </summary>
public enum RoundingRule
{
    /// <summary>Standard mathematical rounding.</summary>
    Standard = 0,

    /// <summary>Always round up.</summary>
    RoundUp = 1,

    /// <summary>Always round down.</summary>
    RoundDown = 2,

    /// <summary>Round to nearest 5 (e.g., 1.02 -> 1.00, 1.03 -> 1.05).</summary>
    RoundToNearest5 = 3,

    /// <summary>Round to nearest 10.</summary>
    RoundToNearest10 = 4
}

/// <summary>
/// Request to create a new currency.
/// </summary>
public class CreateCurrencyRequest
{
    /// <summary>Gets or sets the ISO 4217 currency code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of decimal places.</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Gets or sets whether this is the default currency.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets the rounding rule.</summary>
    public RoundingRule RoundingRule { get; set; } = RoundingRule.Standard;

    /// <summary>Gets or sets the smallest denomination.</summary>
    public decimal SmallestDenomination { get; set; } = 0.01m;
}

/// <summary>
/// Represents an exchange rate between two currencies.
/// </summary>
public class ExchangeRateDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the source currency ID.</summary>
    public int FromCurrencyId { get; set; }

    /// <summary>Gets or sets the source currency code.</summary>
    public string FromCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the target currency ID.</summary>
    public int ToCurrencyId { get; set; }

    /// <summary>Gets or sets the target currency code.</summary>
    public string ToCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the buy rate (when customer pays in foreign currency).</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Gets or sets the sell rate (when giving change in foreign currency).</summary>
    public decimal SellRate { get; set; }

    /// <summary>Gets or sets the effective date of this rate.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>Gets or sets the expiry date of this rate (null for no expiry).</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Gets or sets whether this rate is currently active.</summary>
    public bool IsActive => EffectiveDate <= DateTime.UtcNow.Date &&
                            (!ExpiryDate.HasValue || ExpiryDate.Value >= DateTime.UtcNow.Date);

    /// <summary>Gets or sets the user ID who last updated this rate.</summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>Gets or sets the name of user who last updated this rate.</summary>
    public string? UpdatedByUserName { get; set; }

    /// <summary>Gets or sets when this rate was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create or update an exchange rate.
/// </summary>
public class SetExchangeRateRequest
{
    /// <summary>Gets or sets the source currency code.</summary>
    public string FromCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the target currency code.</summary>
    public string ToCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the buy rate.</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Gets or sets the sell rate.</summary>
    public decimal SellRate { get; set; }

    /// <summary>Gets or sets the effective date.</summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>Gets or sets the expiry date (optional).</summary>
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Currency entity for database storage.
/// </summary>
public class CurrencyEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of decimal places.</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Gets or sets whether this is the default currency.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this currency is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the display order.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets the rounding rule.</summary>
    public RoundingRule RoundingRule { get; set; } = RoundingRule.Standard;

    /// <summary>Gets or sets the smallest denomination.</summary>
    public decimal SmallestDenomination { get; set; } = 0.01m;

    /// <summary>Gets or sets when this was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when this was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Exchange rate entity for database storage.
/// </summary>
public class ExchangeRateEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the source currency ID.</summary>
    public int FromCurrencyId { get; set; }

    /// <summary>Gets or sets the target currency ID.</summary>
    public int ToCurrencyId { get; set; }

    /// <summary>Gets or sets the buy rate.</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Gets or sets the sell rate.</summary>
    public decimal SellRate { get; set; }

    /// <summary>Gets or sets the effective date.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>Gets or sets the expiry date.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Gets or sets the user who updated this rate.</summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>Gets or sets when this was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of a currency conversion.
/// </summary>
public class CurrencyConversionResult
{
    /// <summary>Gets or sets whether the conversion was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the original amount.</summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>Gets or sets the original currency code.</summary>
    public string OriginalCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the converted amount.</summary>
    public decimal ConvertedAmount { get; set; }

    /// <summary>Gets or sets the target currency code.</summary>
    public string TargetCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange rate used.</summary>
    public decimal ExchangeRateUsed { get; set; }

    /// <summary>Gets or sets the error message if conversion failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents change calculation result.
/// </summary>
public class ChangeCalculationResult
{
    /// <summary>Gets or sets the total amount due in base currency.</summary>
    public decimal TotalDueInBaseCurrency { get; set; }

    /// <summary>Gets or sets the amount paid in payment currency.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Gets or sets the payment currency code.</summary>
    public string PaymentCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the equivalent in base currency.</summary>
    public decimal EquivalentInBaseCurrency { get; set; }

    /// <summary>Gets or sets the change amount.</summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>Gets or sets the change currency code (usually base currency).</summary>
    public string ChangeCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange rate used.</summary>
    public decimal ExchangeRateUsed { get; set; }
}

/// <summary>
/// Multi-currency payment details.
/// </summary>
public class MultiCurrencyPaymentDto
{
    /// <summary>Gets or sets the payment currency ID.</summary>
    public int PaymentCurrencyId { get; set; }

    /// <summary>Gets or sets the payment currency code.</summary>
    public string PaymentCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the payment currency symbol.</summary>
    public string PaymentCurrencySymbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount in payment currency.</summary>
    public decimal AmountInPaymentCurrency { get; set; }

    /// <summary>Gets or sets the amount in base currency (KES).</summary>
    public decimal AmountInBaseCurrency { get; set; }

    /// <summary>Gets or sets the exchange rate used.</summary>
    public decimal ExchangeRateUsed { get; set; }

    /// <summary>Gets or sets the change amount (always in base currency).</summary>
    public decimal ChangeInBaseCurrency { get; set; }
}

/// <summary>
/// Cash drawer currency tracking.
/// </summary>
public class CashDrawerCurrencyDto
{
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the opening float amount.</summary>
    public decimal OpeningFloat { get; set; }

    /// <summary>Gets or sets the total cash in.</summary>
    public decimal TotalCashIn { get; set; }

    /// <summary>Gets or sets the total cash out.</summary>
    public decimal TotalCashOut { get; set; }

    /// <summary>Gets or sets the expected closing amount.</summary>
    public decimal ExpectedClosing => OpeningFloat + TotalCashIn - TotalCashOut;

    /// <summary>Gets or sets the actual counted amount.</summary>
    public decimal? ActualCounted { get; set; }

    /// <summary>Gets or sets the variance.</summary>
    public decimal? Variance => ActualCounted.HasValue ? ActualCounted.Value - ExpectedClosing : null;
}

/// <summary>
/// Exchange rate history entry.
/// </summary>
public class ExchangeRateHistoryDto
{
    /// <summary>Gets or sets the rate ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the source currency code.</summary>
    public string FromCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the target currency code.</summary>
    public string ToCurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the buy rate.</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Gets or sets the sell rate.</summary>
    public decimal SellRate { get; set; }

    /// <summary>Gets or sets the effective date.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>Gets or sets the expiry date.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Gets or sets who updated the rate.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets when the rate was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Currency report summary.
/// </summary>
public class CurrencyReportSummaryDto
{
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Gets or sets the total transactions in this currency.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the total amount in this currency.</summary>
    public decimal TotalAmountInCurrency { get; set; }

    /// <summary>Gets or sets the total equivalent in base currency.</summary>
    public decimal TotalEquivalentInBase { get; set; }

    /// <summary>Gets or sets the average exchange rate used.</summary>
    public decimal AverageExchangeRate { get; set; }

    /// <summary>Gets or sets the exchange gain/loss.</summary>
    public decimal ExchangeGainLoss { get; set; }
}

/// <summary>
/// Currency settings for the system.
/// </summary>
public class CurrencySettingsDto
{
    /// <summary>Gets or sets the base currency code.</summary>
    public string BaseCurrencyCode { get; set; } = "KES";

    /// <summary>Gets or sets whether multi-currency is enabled.</summary>
    public bool MultiCurrencyEnabled { get; set; } = false;

    /// <summary>Gets or sets the list of active currencies.</summary>
    public List<CurrencyDto> ActiveCurrencies { get; set; } = new();

    /// <summary>Gets or sets the rate validity period in days.</summary>
    public int RateValidityPeriodDays { get; set; } = 1;

    /// <summary>Gets or sets whether to show exchange rate on receipt.</summary>
    public bool ShowExchangeRateOnReceipt { get; set; } = true;

    /// <summary>Gets or sets whether change is always in base currency.</summary>
    public bool ChangeAlwaysInBaseCurrency { get; set; } = true;
}

/// <summary>
/// Pre-defined currencies for Kenya market.
/// </summary>
public static class KenyaMarketCurrencies
{
    /// <summary>Kenyan Shilling (default).</summary>
    public static CurrencyDto KES => new()
    {
        Code = "KES",
        Name = "Kenyan Shilling",
        Symbol = "KSh",
        DecimalPlaces = 2,
        IsDefault = true,
        IsActive = true,
        DisplayOrder = 1,
        RoundingRule = RoundingRule.Standard,
        SmallestDenomination = 1.00m
    };

    /// <summary>US Dollar.</summary>
    public static CurrencyDto USD => new()
    {
        Code = "USD",
        Name = "US Dollar",
        Symbol = "$",
        DecimalPlaces = 2,
        IsDefault = false,
        IsActive = true,
        DisplayOrder = 2,
        RoundingRule = RoundingRule.Standard,
        SmallestDenomination = 0.01m
    };

    /// <summary>Euro.</summary>
    public static CurrencyDto EUR => new()
    {
        Code = "EUR",
        Name = "Euro",
        Symbol = "€",
        DecimalPlaces = 2,
        IsDefault = false,
        IsActive = true,
        DisplayOrder = 3,
        RoundingRule = RoundingRule.Standard,
        SmallestDenomination = 0.01m
    };

    /// <summary>British Pound.</summary>
    public static CurrencyDto GBP => new()
    {
        Code = "GBP",
        Name = "British Pound",
        Symbol = "£",
        DecimalPlaces = 2,
        IsDefault = false,
        IsActive = true,
        DisplayOrder = 4,
        RoundingRule = RoundingRule.Standard,
        SmallestDenomination = 0.01m
    };

    /// <summary>Ugandan Shilling.</summary>
    public static CurrencyDto UGX => new()
    {
        Code = "UGX",
        Name = "Ugandan Shilling",
        Symbol = "USh",
        DecimalPlaces = 0,
        IsDefault = false,
        IsActive = true,
        DisplayOrder = 5,
        RoundingRule = RoundingRule.RoundToNearest10,
        SmallestDenomination = 50m
    };

    /// <summary>Tanzanian Shilling.</summary>
    public static CurrencyDto TZS => new()
    {
        Code = "TZS",
        Name = "Tanzanian Shilling",
        Symbol = "TSh",
        DecimalPlaces = 0,
        IsDefault = false,
        IsActive = true,
        DisplayOrder = 6,
        RoundingRule = RoundingRule.RoundToNearest10,
        SmallestDenomination = 50m
    };

    /// <summary>Get all pre-defined currencies.</summary>
    public static List<CurrencyDto> GetAll() => new() { KES, USD, EUR, GBP, UGX, TZS };
}
