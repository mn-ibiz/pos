using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Currency;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing currencies and exchange rates.
/// Supports multi-currency payments for border towns and tourist areas in Kenya.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly ILogger _logger;
    private readonly Dictionary<int, CurrencyEntity> _currencies = new();
    private readonly Dictionary<string, int> _currencyCodeToId = new();
    private readonly List<ExchangeRateEntity> _exchangeRates = new();
    private readonly Dictionary<int, Dictionary<string, CashDrawerCurrencyDto>> _cashDrawerCurrencies = new();
    private readonly Dictionary<int, MultiCurrencyPaymentDto> _multiCurrencyPayments = new();
    private CurrencySettingsDto _settings = new();
    private int _nextCurrencyId = 1;
    private int _nextRateId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyService"/> class.
    /// </summary>
    public CurrencyService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDefaultCurrencies();
    }

    private void InitializeDefaultCurrencies()
    {
        // Initialize with Kenya market currencies
        var defaultCurrencies = KenyaMarketCurrencies.GetAll();
        foreach (var currency in defaultCurrencies)
        {
            var entity = new CurrencyEntity
            {
                Id = _nextCurrencyId++,
                Code = currency.Code,
                Name = currency.Name,
                Symbol = currency.Symbol,
                DecimalPlaces = currency.DecimalPlaces,
                IsDefault = currency.IsDefault,
                IsActive = currency.IsActive,
                DisplayOrder = currency.DisplayOrder,
                RoundingRule = currency.RoundingRule,
                SmallestDenomination = currency.SmallestDenomination,
                CreatedAt = DateTime.UtcNow
            };
            _currencies[entity.Id] = entity;
            _currencyCodeToId[entity.Code] = entity.Id;
        }

        // Initialize default exchange rates (approximate Kenya rates as of 2025)
        var kesId = _currencyCodeToId["KES"];
        SetDefaultExchangeRates(kesId);

        _settings = new CurrencySettingsDto
        {
            BaseCurrencyCode = "KES",
            MultiCurrencyEnabled = false,
            ActiveCurrencies = new List<CurrencyDto> { KenyaMarketCurrencies.KES },
            RateValidityPeriodDays = 1,
            ShowExchangeRateOnReceipt = true,
            ChangeAlwaysInBaseCurrency = true
        };

        _logger.Information("Currency service initialized with {Count} currencies", _currencies.Count);
    }

    private void SetDefaultExchangeRates(int kesId)
    {
        // Default exchange rates (1 Foreign = X KES)
        var defaultRates = new Dictionary<string, (decimal buy, decimal sell)>
        {
            { "USD", (129.50m, 131.50m) },
            { "EUR", (140.00m, 143.00m) },
            { "GBP", (163.00m, 167.00m) },
            { "UGX", (0.035m, 0.037m) },
            { "TZS", (0.050m, 0.053m) }
        };

        foreach (var (code, rates) in defaultRates)
        {
            if (_currencyCodeToId.TryGetValue(code, out var currencyId))
            {
                _exchangeRates.Add(new ExchangeRateEntity
                {
                    Id = _nextRateId++,
                    FromCurrencyId = currencyId,
                    ToCurrencyId = kesId,
                    BuyRate = rates.buy,
                    SellRate = rates.sell,
                    EffectiveDate = DateTime.UtcNow.Date,
                    ExpiryDate = DateTime.UtcNow.Date.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    #region Currency Management

    /// <inheritdoc />
    public Task<List<CurrencyDto>> GetAllCurrenciesAsync(bool activeOnly = true)
    {
        var currencies = _currencies.Values
            .Where(c => !activeOnly || c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(currencies);
    }

    /// <inheritdoc />
    public Task<CurrencyDto?> GetCurrencyByCodeAsync(string currencyCode)
    {
        if (_currencyCodeToId.TryGetValue(currencyCode.ToUpperInvariant(), out var id) &&
            _currencies.TryGetValue(id, out var entity))
        {
            return Task.FromResult<CurrencyDto?>(MapToDto(entity));
        }
        return Task.FromResult<CurrencyDto?>(null);
    }

    /// <inheritdoc />
    public Task<CurrencyDto?> GetCurrencyByIdAsync(int currencyId)
    {
        if (_currencies.TryGetValue(currencyId, out var entity))
        {
            return Task.FromResult<CurrencyDto?>(MapToDto(entity));
        }
        return Task.FromResult<CurrencyDto?>(null);
    }

    /// <inheritdoc />
    public Task<CurrencyDto> GetDefaultCurrencyAsync()
    {
        var defaultCurrency = _currencies.Values.FirstOrDefault(c => c.IsDefault)
            ?? _currencies.Values.First();
        return Task.FromResult(MapToDto(defaultCurrency));
    }

    /// <inheritdoc />
    public Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyRequest request)
    {
        var entity = new CurrencyEntity
        {
            Id = _nextCurrencyId++,
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Symbol = request.Symbol,
            DecimalPlaces = request.DecimalPlaces,
            IsDefault = request.IsDefault,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            RoundingRule = request.RoundingRule,
            SmallestDenomination = request.SmallestDenomination,
            CreatedAt = DateTime.UtcNow
        };

        _currencies[entity.Id] = entity;
        _currencyCodeToId[entity.Code] = entity.Id;

        if (request.IsDefault)
        {
            foreach (var other in _currencies.Values.Where(c => c.Id != entity.Id))
            {
                other.IsDefault = false;
            }
        }

        _logger.Information("Created currency: {Code} ({Name})", entity.Code, entity.Name);
        return Task.FromResult(MapToDto(entity));
    }

    /// <inheritdoc />
    public Task<CurrencyDto?> UpdateCurrencyAsync(int currencyId, CreateCurrencyRequest request)
    {
        if (!_currencies.TryGetValue(currencyId, out var entity))
        {
            return Task.FromResult<CurrencyDto?>(null);
        }

        // Update code mapping if changed
        if (entity.Code != request.Code.ToUpperInvariant())
        {
            _currencyCodeToId.Remove(entity.Code);
            _currencyCodeToId[request.Code.ToUpperInvariant()] = entity.Id;
        }

        entity.Code = request.Code.ToUpperInvariant();
        entity.Name = request.Name;
        entity.Symbol = request.Symbol;
        entity.DecimalPlaces = request.DecimalPlaces;
        entity.DisplayOrder = request.DisplayOrder;
        entity.RoundingRule = request.RoundingRule;
        entity.SmallestDenomination = request.SmallestDenomination;
        entity.UpdatedAt = DateTime.UtcNow;

        if (request.IsDefault && !entity.IsDefault)
        {
            foreach (var other in _currencies.Values)
            {
                other.IsDefault = other.Id == currencyId;
            }
        }

        _logger.Information("Updated currency: {Code}", entity.Code);
        return Task.FromResult<CurrencyDto?>(MapToDto(entity));
    }

    /// <inheritdoc />
    public Task<bool> SetCurrencyActiveStatusAsync(string currencyCode, bool isActive)
    {
        if (!_currencyCodeToId.TryGetValue(currencyCode.ToUpperInvariant(), out var id) ||
            !_currencies.TryGetValue(id, out var entity))
        {
            return Task.FromResult(false);
        }

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _logger.Information("Currency {Code} active status set to: {Status}", currencyCode, isActive);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SetDefaultCurrencyAsync(string currencyCode)
    {
        if (!_currencyCodeToId.TryGetValue(currencyCode.ToUpperInvariant(), out var id))
        {
            return Task.FromResult(false);
        }

        foreach (var currency in _currencies.Values)
        {
            currency.IsDefault = currency.Id == id;
        }

        _settings.BaseCurrencyCode = currencyCode.ToUpperInvariant();
        _logger.Information("Default currency set to: {Code}", currencyCode);
        return Task.FromResult(true);
    }

    #endregion

    #region Exchange Rate Management

    /// <inheritdoc />
    public Task<ExchangeRateDto?> GetCurrentExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode)
    {
        var today = DateTime.UtcNow.Date;

        if (!_currencyCodeToId.TryGetValue(fromCurrencyCode.ToUpperInvariant(), out var fromId) ||
            !_currencyCodeToId.TryGetValue(toCurrencyCode.ToUpperInvariant(), out var toId))
        {
            return Task.FromResult<ExchangeRateDto?>(null);
        }

        var rate = _exchangeRates
            .Where(r => r.FromCurrencyId == fromId && r.ToCurrencyId == toId)
            .Where(r => r.EffectiveDate <= today)
            .Where(r => !r.ExpiryDate.HasValue || r.ExpiryDate.Value >= today)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefault();

        if (rate == null)
        {
            return Task.FromResult<ExchangeRateDto?>(null);
        }

        return Task.FromResult<ExchangeRateDto?>(MapToDto(rate));
    }

    /// <inheritdoc />
    public Task<List<ExchangeRateDto>> GetCurrentExchangeRatesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var defaultCurrency = _currencies.Values.First(c => c.IsDefault);

        var rates = _exchangeRates
            .Where(r => r.ToCurrencyId == defaultCurrency.Id)
            .Where(r => r.EffectiveDate <= today)
            .Where(r => !r.ExpiryDate.HasValue || r.ExpiryDate.Value >= today)
            .GroupBy(r => r.FromCurrencyId)
            .Select(g => g.OrderByDescending(r => r.EffectiveDate).First())
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(rates);
    }

    /// <inheritdoc />
    public Task<ExchangeRateDto> SetExchangeRateAsync(SetExchangeRateRequest request, int userId)
    {
        if (!_currencyCodeToId.TryGetValue(request.FromCurrencyCode.ToUpperInvariant(), out var fromId) ||
            !_currencyCodeToId.TryGetValue(request.ToCurrencyCode.ToUpperInvariant(), out var toId))
        {
            throw new ArgumentException("Invalid currency code");
        }

        // Expire any existing active rates
        var existingRates = _exchangeRates
            .Where(r => r.FromCurrencyId == fromId && r.ToCurrencyId == toId)
            .Where(r => !r.ExpiryDate.HasValue || r.ExpiryDate.Value >= request.EffectiveDate);

        foreach (var existing in existingRates)
        {
            if (existing.EffectiveDate < request.EffectiveDate)
            {
                existing.ExpiryDate = request.EffectiveDate.AddDays(-1);
            }
        }

        var entity = new ExchangeRateEntity
        {
            Id = _nextRateId++,
            FromCurrencyId = fromId,
            ToCurrencyId = toId,
            BuyRate = request.BuyRate,
            SellRate = request.SellRate,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            UpdatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _exchangeRates.Add(entity);
        _logger.Information("Exchange rate set: {From} -> {To}, Buy: {Buy}, Sell: {Sell}",
            request.FromCurrencyCode, request.ToCurrencyCode, request.BuyRate, request.SellRate);

        return Task.FromResult(MapToDto(entity));
    }

    /// <inheritdoc />
    public Task<List<ExchangeRateHistoryDto>> GetExchangeRateHistoryAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        DateTime fromDate,
        DateTime toDate)
    {
        if (!_currencyCodeToId.TryGetValue(fromCurrencyCode.ToUpperInvariant(), out var fromId) ||
            !_currencyCodeToId.TryGetValue(toCurrencyCode.ToUpperInvariant(), out var toId))
        {
            return Task.FromResult(new List<ExchangeRateHistoryDto>());
        }

        var history = _exchangeRates
            .Where(r => r.FromCurrencyId == fromId && r.ToCurrencyId == toId)
            .Where(r => r.EffectiveDate >= fromDate && r.EffectiveDate <= toDate)
            .OrderByDescending(r => r.EffectiveDate)
            .Select(r => new ExchangeRateHistoryDto
            {
                Id = r.Id,
                FromCurrencyCode = fromCurrencyCode,
                ToCurrencyCode = toCurrencyCode,
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                EffectiveDate = r.EffectiveDate,
                ExpiryDate = r.ExpiryDate,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        return Task.FromResult(history);
    }

    /// <inheritdoc />
    public Task<List<ExchangeRateDto>> GetExpiringRatesAsync(int daysUntilExpiry = 1)
    {
        var today = DateTime.UtcNow.Date;
        var expiryThreshold = today.AddDays(daysUntilExpiry);

        var expiringRates = _exchangeRates
            .Where(r => r.ExpiryDate.HasValue && r.ExpiryDate.Value <= expiryThreshold && r.ExpiryDate.Value >= today)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(expiringRates);
    }

    /// <inheritdoc />
    public Task<bool> HasValidExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode)
    {
        var today = DateTime.UtcNow.Date;

        if (!_currencyCodeToId.TryGetValue(fromCurrencyCode.ToUpperInvariant(), out var fromId) ||
            !_currencyCodeToId.TryGetValue(toCurrencyCode.ToUpperInvariant(), out var toId))
        {
            return Task.FromResult(false);
        }

        // Same currency always valid
        if (fromId == toId)
        {
            return Task.FromResult(true);
        }

        var hasValid = _exchangeRates.Any(r =>
            r.FromCurrencyId == fromId &&
            r.ToCurrencyId == toId &&
            r.EffectiveDate <= today &&
            (!r.ExpiryDate.HasValue || r.ExpiryDate.Value >= today));

        return Task.FromResult(hasValid);
    }

    #endregion

    #region Currency Conversion

    /// <inheritdoc />
    public async Task<CurrencyConversionResult> ConvertAmountAsync(decimal amount, string fromCurrencyCode, string toCurrencyCode)
    {
        var result = new CurrencyConversionResult
        {
            OriginalAmount = amount,
            OriginalCurrencyCode = fromCurrencyCode,
            TargetCurrencyCode = toCurrencyCode
        };

        // Same currency - no conversion needed
        if (fromCurrencyCode.Equals(toCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            result.Success = true;
            result.ConvertedAmount = amount;
            result.ExchangeRateUsed = 1m;
            return result;
        }

        var rate = await GetCurrentExchangeRateAsync(fromCurrencyCode, toCurrencyCode);
        if (rate == null)
        {
            result.Success = false;
            result.ErrorMessage = $"No exchange rate found for {fromCurrencyCode} to {toCurrencyCode}";
            return result;
        }

        var toCurrency = await GetCurrencyByCodeAsync(toCurrencyCode);
        var convertedAmount = amount * rate.BuyRate;
        convertedAmount = await ApplyRoundingAsync(convertedAmount, toCurrencyCode);

        result.Success = true;
        result.ConvertedAmount = convertedAmount;
        result.ExchangeRateUsed = rate.BuyRate;

        return result;
    }

    /// <inheritdoc />
    public async Task<CurrencyConversionResult> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode)
    {
        var baseCurrency = await GetDefaultCurrencyAsync();
        return await ConvertAmountAsync(amount, fromCurrencyCode, baseCurrency.Code);
    }

    /// <inheritdoc />
    public async Task<CurrencyConversionResult> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode)
    {
        var baseCurrency = await GetDefaultCurrencyAsync();

        // If converting to base currency, just return the amount
        if (baseCurrency.Code.Equals(toCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return new CurrencyConversionResult
            {
                Success = true,
                OriginalAmount = amount,
                OriginalCurrencyCode = baseCurrency.Code,
                ConvertedAmount = amount,
                TargetCurrencyCode = toCurrencyCode,
                ExchangeRateUsed = 1m
            };
        }

        // Get rate from foreign to base, then invert
        var rate = await GetCurrentExchangeRateAsync(toCurrencyCode, baseCurrency.Code);
        if (rate == null)
        {
            return new CurrencyConversionResult
            {
                Success = false,
                OriginalAmount = amount,
                OriginalCurrencyCode = baseCurrency.Code,
                TargetCurrencyCode = toCurrencyCode,
                ErrorMessage = $"No exchange rate found for {toCurrencyCode}"
            };
        }

        // Invert the rate: if 1 USD = 129 KES, then 129 KES = 1 USD
        var invertedRate = 1m / rate.BuyRate;
        var convertedAmount = amount * invertedRate;
        convertedAmount = await ApplyRoundingAsync(convertedAmount, toCurrencyCode);

        return new CurrencyConversionResult
        {
            Success = true,
            OriginalAmount = amount,
            OriginalCurrencyCode = baseCurrency.Code,
            ConvertedAmount = convertedAmount,
            TargetCurrencyCode = toCurrencyCode,
            ExchangeRateUsed = invertedRate
        };
    }

    /// <inheritdoc />
    public async Task<ChangeCalculationResult> CalculateChangeAsync(
        decimal totalDueInBaseCurrency,
        decimal amountPaid,
        string paymentCurrencyCode)
    {
        var baseCurrency = await GetDefaultCurrencyAsync();
        var result = new ChangeCalculationResult
        {
            TotalDueInBaseCurrency = totalDueInBaseCurrency,
            AmountPaid = amountPaid,
            PaymentCurrencyCode = paymentCurrencyCode,
            ChangeCurrencyCode = baseCurrency.Code
        };

        // Convert payment to base currency
        var conversionResult = await ConvertToBaseCurrencyAsync(amountPaid, paymentCurrencyCode);
        if (!conversionResult.Success)
        {
            result.EquivalentInBaseCurrency = 0;
            result.ChangeAmount = 0;
            return result;
        }

        result.EquivalentInBaseCurrency = conversionResult.ConvertedAmount;
        result.ExchangeRateUsed = conversionResult.ExchangeRateUsed;

        // Calculate change in base currency
        var change = conversionResult.ConvertedAmount - totalDueInBaseCurrency;
        result.ChangeAmount = Math.Max(0, await ApplyRoundingAsync(change, baseCurrency.Code));

        return result;
    }

    /// <inheritdoc />
    public Task<decimal> ApplyRoundingAsync(decimal amount, string currencyCode)
    {
        if (!_currencyCodeToId.TryGetValue(currencyCode.ToUpperInvariant(), out var id) ||
            !_currencies.TryGetValue(id, out var currency))
        {
            return Task.FromResult(Math.Round(amount, 2));
        }

        var rounded = currency.RoundingRule switch
        {
            RoundingRule.RoundUp => Math.Ceiling(amount / currency.SmallestDenomination) * currency.SmallestDenomination,
            RoundingRule.RoundDown => Math.Floor(amount / currency.SmallestDenomination) * currency.SmallestDenomination,
            RoundingRule.RoundToNearest5 => Math.Round(amount / 5m, 0) * 5m,
            RoundingRule.RoundToNearest10 => Math.Round(amount / 10m, 0) * 10m,
            _ => Math.Round(amount, currency.DecimalPlaces)
        };

        return Task.FromResult(rounded);
    }

    #endregion

    #region Payment Integration

    /// <inheritdoc />
    public async Task<MultiCurrencyPaymentDto> CreateMultiCurrencyPaymentAsync(
        int receiptId,
        decimal amountInBaseCurrency,
        decimal amountPaid,
        string paymentCurrencyCode)
    {
        var currency = await GetCurrencyByCodeAsync(paymentCurrencyCode);
        if (currency == null)
        {
            throw new ArgumentException($"Currency not found: {paymentCurrencyCode}");
        }

        var conversionResult = await ConvertToBaseCurrencyAsync(amountPaid, paymentCurrencyCode);
        var baseCurrency = await GetDefaultCurrencyAsync();

        var changeCalculation = await CalculateChangeAsync(amountInBaseCurrency, amountPaid, paymentCurrencyCode);

        var payment = new MultiCurrencyPaymentDto
        {
            PaymentCurrencyId = currency.Id,
            PaymentCurrencyCode = currency.Code,
            PaymentCurrencySymbol = currency.Symbol,
            AmountInPaymentCurrency = amountPaid,
            AmountInBaseCurrency = conversionResult.ConvertedAmount,
            ExchangeRateUsed = conversionResult.ExchangeRateUsed,
            ChangeInBaseCurrency = changeCalculation.ChangeAmount
        };

        _multiCurrencyPayments[receiptId] = payment;

        _logger.Information("Multi-currency payment created for receipt {ReceiptId}: {Amount} {Currency}",
            receiptId, amountPaid, paymentCurrencyCode);

        return payment;
    }

    /// <inheritdoc />
    public Task<MultiCurrencyPaymentDto?> GetMultiCurrencyPaymentAsync(int receiptId)
    {
        _multiCurrencyPayments.TryGetValue(receiptId, out var payment);
        return Task.FromResult(payment);
    }

    #endregion

    #region Cash Drawer Tracking

    /// <inheritdoc />
    public Task<List<CashDrawerCurrencyDto>> GetCashDrawerCurrenciesAsync(int workPeriodId)
    {
        if (!_cashDrawerCurrencies.TryGetValue(workPeriodId, out var currencies))
        {
            // Initialize with base currency
            currencies = new Dictionary<string, CashDrawerCurrencyDto>
            {
                ["KES"] = new CashDrawerCurrencyDto
                {
                    CurrencyCode = "KES",
                    Symbol = "KSh",
                    OpeningFloat = 0,
                    TotalCashIn = 0,
                    TotalCashOut = 0
                }
            };
            _cashDrawerCurrencies[workPeriodId] = currencies;
        }

        return Task.FromResult(currencies.Values.ToList());
    }

    /// <inheritdoc />
    public Task<CashDrawerCurrencyDto> RecordCashCountAsync(int workPeriodId, string currencyCode, decimal actualCount)
    {
        if (!_cashDrawerCurrencies.TryGetValue(workPeriodId, out var currencies))
        {
            currencies = new Dictionary<string, CashDrawerCurrencyDto>();
            _cashDrawerCurrencies[workPeriodId] = currencies;
        }

        if (!currencies.TryGetValue(currencyCode, out var drawer))
        {
            drawer = new CashDrawerCurrencyDto
            {
                CurrencyCode = currencyCode,
                Symbol = GetCurrencyByCodeAsync(currencyCode).Result?.Symbol ?? currencyCode
            };
            currencies[currencyCode] = drawer;
        }

        drawer.ActualCounted = actualCount;
        _logger.Information("Cash count recorded for {Currency}: {Amount}", currencyCode, actualCount);

        return Task.FromResult(drawer);
    }

    /// <inheritdoc />
    public Task<bool> SetOpeningFloatAsync(int workPeriodId, string currencyCode, decimal openingFloat)
    {
        if (!_cashDrawerCurrencies.TryGetValue(workPeriodId, out var currencies))
        {
            currencies = new Dictionary<string, CashDrawerCurrencyDto>();
            _cashDrawerCurrencies[workPeriodId] = currencies;
        }

        if (!currencies.TryGetValue(currencyCode, out var drawer))
        {
            drawer = new CashDrawerCurrencyDto
            {
                CurrencyCode = currencyCode,
                Symbol = GetCurrencyByCodeAsync(currencyCode).Result?.Symbol ?? currencyCode
            };
            currencies[currencyCode] = drawer;
        }

        drawer.OpeningFloat = openingFloat;
        _logger.Information("Opening float set for {Currency}: {Amount}", currencyCode, openingFloat);

        return Task.FromResult(true);
    }

    #endregion

    #region Reporting

    /// <inheritdoc />
    public Task<List<CurrencyReportSummaryDto>> GetCurrencyReportAsync(DateTime fromDate, DateTime toDate)
    {
        // In production, this would query actual payment data
        var report = _currencies.Values
            .Where(c => c.IsActive)
            .Select(c => new CurrencyReportSummaryDto
            {
                CurrencyCode = c.Code,
                Symbol = c.Symbol,
                TransactionCount = 0,
                TotalAmountInCurrency = 0,
                TotalEquivalentInBase = 0,
                AverageExchangeRate = GetLatestRate(c.Code),
                ExchangeGainLoss = 0
            })
            .ToList();

        return Task.FromResult(report);
    }

    private decimal GetLatestRate(string currencyCode)
    {
        if (currencyCode == "KES") return 1m;

        if (_currencyCodeToId.TryGetValue(currencyCode, out var id))
        {
            var kesId = _currencyCodeToId["KES"];
            var rate = _exchangeRates
                .Where(r => r.FromCurrencyId == id && r.ToCurrencyId == kesId)
                .OrderByDescending(r => r.EffectiveDate)
                .FirstOrDefault();
            return rate?.BuyRate ?? 0m;
        }
        return 0m;
    }

    /// <inheritdoc />
    public Task<decimal> GetExchangeGainLossAsync(DateTime fromDate, DateTime toDate)
    {
        // In production, calculate actual gain/loss from payment records
        return Task.FromResult(0m);
    }

    /// <inheritdoc />
    public async Task<List<CurrencyReportSummaryDto>> GetWorkPeriodCurrencySummaryAsync(int workPeriodId)
    {
        var drawers = await GetCashDrawerCurrenciesAsync(workPeriodId);

        return drawers.Select(d => new CurrencyReportSummaryDto
        {
            CurrencyCode = d.CurrencyCode,
            Symbol = d.Symbol,
            TransactionCount = 0, // Would count from payments
            TotalAmountInCurrency = d.TotalCashIn,
            TotalEquivalentInBase = d.CurrencyCode == "KES" ? d.TotalCashIn : d.TotalCashIn * GetLatestRate(d.CurrencyCode)
        }).ToList();
    }

    #endregion

    #region Settings

    /// <inheritdoc />
    public Task<CurrencySettingsDto> GetCurrencySettingsAsync()
    {
        _settings.ActiveCurrencies = _currencies.Values
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(_settings);
    }

    /// <inheritdoc />
    public Task<CurrencySettingsDto> UpdateCurrencySettingsAsync(CurrencySettingsDto settings)
    {
        _settings = settings;
        _logger.Information("Currency settings updated. Multi-currency enabled: {Enabled}",
            settings.MultiCurrencyEnabled);
        return Task.FromResult(_settings);
    }

    /// <inheritdoc />
    public Task<bool> IsMultiCurrencyEnabledAsync()
    {
        return Task.FromResult(_settings.MultiCurrencyEnabled);
    }

    /// <inheritdoc />
    public Task<bool> SetMultiCurrencyEnabledAsync(bool enabled)
    {
        _settings.MultiCurrencyEnabled = enabled;
        _logger.Information("Multi-currency support {Status}", enabled ? "enabled" : "disabled");
        return Task.FromResult(true);
    }

    #endregion

    #region Formatting

    /// <inheritdoc />
    public Task<string> FormatAmountAsync(decimal amount, string currencyCode)
    {
        if (!_currencyCodeToId.TryGetValue(currencyCode.ToUpperInvariant(), out var id) ||
            !_currencies.TryGetValue(id, out var currency))
        {
            return Task.FromResult($"{amount:N2} {currencyCode}");
        }

        var format = currency.DecimalPlaces == 0 ? "N0" : $"N{currency.DecimalPlaces}";
        return Task.FromResult($"{currency.Symbol} {amount.ToString(format)}");
    }

    /// <inheritdoc />
    public string FormatExchangeRate(string fromCurrencyCode, string toCurrencyCode, decimal rate)
    {
        return $"1 {fromCurrencyCode} = {rate:N2} {toCurrencyCode}";
    }

    #endregion

    #region Mapping Helpers

    private CurrencyDto MapToDto(CurrencyEntity entity)
    {
        return new CurrencyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Symbol = entity.Symbol,
            DecimalPlaces = entity.DecimalPlaces,
            IsDefault = entity.IsDefault,
            IsActive = entity.IsActive,
            DisplayOrder = entity.DisplayOrder,
            RoundingRule = entity.RoundingRule,
            SmallestDenomination = entity.SmallestDenomination
        };
    }

    private ExchangeRateDto MapToDto(ExchangeRateEntity entity)
    {
        return new ExchangeRateDto
        {
            Id = entity.Id,
            FromCurrencyId = entity.FromCurrencyId,
            FromCurrencyCode = _currencies.TryGetValue(entity.FromCurrencyId, out var from) ? from.Code : "",
            ToCurrencyId = entity.ToCurrencyId,
            ToCurrencyCode = _currencies.TryGetValue(entity.ToCurrencyId, out var to) ? to.Code : "",
            BuyRate = entity.BuyRate,
            SellRate = entity.SellRate,
            EffectiveDate = entity.EffectiveDate,
            ExpiryDate = entity.ExpiryDate,
            UpdatedByUserId = entity.UpdatedByUserId,
            CreatedAt = entity.CreatedAt
        };
    }

    #endregion
}
