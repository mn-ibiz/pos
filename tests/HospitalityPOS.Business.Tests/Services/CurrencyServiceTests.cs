using FluentAssertions;
using HospitalityPOS.Core.Models.Currency;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Serilog;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for CurrencyService.
/// </summary>
public class CurrencyServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly CurrencyService _service;

    public CurrencyServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(_loggerMock.Object);
        _service = new CurrencyService(_loggerMock.Object);
    }

    #region Currency Management Tests

    [Fact]
    public async Task GetAllCurrenciesAsync_ShouldReturnPreloadedCurrencies()
    {
        // Act
        var currencies = await _service.GetAllCurrenciesAsync();

        // Assert
        currencies.Should().NotBeEmpty();
        currencies.Should().Contain(c => c.Code == "KES");
        currencies.Should().Contain(c => c.Code == "USD");
        currencies.Should().Contain(c => c.Code == "EUR");
    }

    [Fact]
    public async Task GetAllCurrenciesAsync_ActiveOnly_ShouldFilterInactiveCurrencies()
    {
        // Arrange
        await _service.SetCurrencyActiveStatusAsync("EUR", false);

        // Act
        var activeCurrencies = await _service.GetAllCurrenciesAsync(activeOnly: true);
        var allCurrencies = await _service.GetAllCurrenciesAsync(activeOnly: false);

        // Assert
        activeCurrencies.Should().NotContain(c => c.Code == "EUR");
        allCurrencies.Should().Contain(c => c.Code == "EUR");
    }

    [Fact]
    public async Task GetCurrencyByCodeAsync_ValidCode_ShouldReturnCurrency()
    {
        // Act
        var currency = await _service.GetCurrencyByCodeAsync("USD");

        // Assert
        currency.Should().NotBeNull();
        currency!.Code.Should().Be("USD");
        currency.Symbol.Should().Be("$");
        currency.Name.Should().Be("US Dollar");
    }

    [Fact]
    public async Task GetCurrencyByCodeAsync_InvalidCode_ShouldReturnNull()
    {
        // Act
        var currency = await _service.GetCurrencyByCodeAsync("INVALID");

        // Assert
        currency.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultCurrencyAsync_ShouldReturnKES()
    {
        // Act
        var defaultCurrency = await _service.GetDefaultCurrencyAsync();

        // Assert
        defaultCurrency.Should().NotBeNull();
        defaultCurrency.Code.Should().Be("KES");
        defaultCurrency.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCurrencyAsync_ShouldCreateNewCurrency()
    {
        // Arrange
        var request = new CreateCurrencyRequest
        {
            Code = "CNY",
            Name = "Chinese Yuan",
            Symbol = "¥",
            DecimalPlaces = 2,
            DisplayOrder = 10
        };

        // Act
        var currency = await _service.CreateCurrencyAsync(request);

        // Assert
        currency.Should().NotBeNull();
        currency.Code.Should().Be("CNY");
        currency.Name.Should().Be("Chinese Yuan");
        currency.Symbol.Should().Be("¥");

        // Verify it can be retrieved
        var retrieved = await _service.GetCurrencyByCodeAsync("CNY");
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task SetCurrencyActiveStatusAsync_ShouldToggleStatus()
    {
        // Arrange
        var currency = await _service.GetCurrencyByCodeAsync("GBP");
        currency.Should().NotBeNull();
        currency!.IsActive.Should().BeTrue();

        // Act
        await _service.SetCurrencyActiveStatusAsync("GBP", false);

        // Assert
        var updated = await _service.GetCurrencyByCodeAsync("GBP");
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultCurrencyAsync_ShouldChangeDefaultCurrency()
    {
        // Act
        await _service.SetDefaultCurrencyAsync("USD");

        // Assert
        var newDefault = await _service.GetDefaultCurrencyAsync();
        newDefault.Code.Should().Be("USD");
        newDefault.IsDefault.Should().BeTrue();

        // Old default should no longer be default
        var kes = await _service.GetCurrencyByCodeAsync("KES");
        kes!.IsDefault.Should().BeFalse();

        // Cleanup - reset to KES
        await _service.SetDefaultCurrencyAsync("KES");
    }

    #endregion

    #region Exchange Rate Tests

    [Fact]
    public async Task GetCurrentExchangeRateAsync_ValidPair_ShouldReturnRate()
    {
        // Act
        var rate = await _service.GetCurrentExchangeRateAsync("USD", "KES");

        // Assert
        rate.Should().NotBeNull();
        rate!.FromCurrencyCode.Should().Be("USD");
        rate.ToCurrencyCode.Should().Be("KES");
        rate.BuyRate.Should().BeGreaterThan(0);
        rate.SellRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCurrentExchangeRateAsync_InvalidPair_ShouldReturnNull()
    {
        // Act
        var rate = await _service.GetCurrentExchangeRateAsync("INVALID", "KES");

        // Assert
        rate.Should().BeNull();
    }

    [Fact]
    public async Task SetExchangeRateAsync_ShouldSetNewRate()
    {
        // Arrange
        var request = new SetExchangeRateRequest
        {
            FromCurrencyCode = "USD",
            ToCurrencyCode = "KES",
            BuyRate = 130.00m,
            SellRate = 132.00m,
            EffectiveDate = DateTime.UtcNow.Date,
            ExpiryDate = DateTime.UtcNow.Date.AddDays(7)
        };

        // Act
        var rate = await _service.SetExchangeRateAsync(request, userId: 1);

        // Assert
        rate.Should().NotBeNull();
        rate.BuyRate.Should().Be(130.00m);
        rate.SellRate.Should().Be(132.00m);
    }

    [Fact]
    public async Task GetCurrentExchangeRatesAsync_ShouldReturnAllActiveRates()
    {
        // Act
        var rates = await _service.GetCurrentExchangeRatesAsync();

        // Assert
        rates.Should().NotBeEmpty();
        rates.Should().OnlyContain(r => r.ToCurrencyCode == "KES");
    }

    [Fact]
    public async Task HasValidExchangeRateAsync_SameCurrency_ShouldReturnTrue()
    {
        // Act
        var hasRate = await _service.HasValidExchangeRateAsync("KES", "KES");

        // Assert
        hasRate.Should().BeTrue();
    }

    [Fact]
    public async Task HasValidExchangeRateAsync_ValidPair_ShouldReturnTrue()
    {
        // Act
        var hasRate = await _service.HasValidExchangeRateAsync("USD", "KES");

        // Assert
        hasRate.Should().BeTrue();
    }

    [Fact]
    public async Task GetExchangeRateHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.Date.AddMonths(-1);
        var toDate = DateTime.UtcNow.Date.AddDays(1);

        // Act
        var history = await _service.GetExchangeRateHistoryAsync("USD", "KES", fromDate, toDate);

        // Assert
        history.Should().NotBeEmpty();
    }

    #endregion

    #region Currency Conversion Tests

    [Fact]
    public async Task ConvertAmountAsync_SameCurrency_ShouldReturnSameAmount()
    {
        // Arrange
        var amount = 1000m;

        // Act
        var result = await _service.ConvertAmountAsync(amount, "KES", "KES");

        // Assert
        result.Success.Should().BeTrue();
        result.ConvertedAmount.Should().Be(amount);
        result.ExchangeRateUsed.Should().Be(1m);
    }

    [Fact]
    public async Task ConvertAmountAsync_UsdToKes_ShouldConvertCorrectly()
    {
        // Arrange
        var amount = 10m;

        // Act
        var result = await _service.ConvertAmountAsync(amount, "USD", "KES");

        // Assert
        result.Success.Should().BeTrue();
        result.ConvertedAmount.Should().BeGreaterThan(1000m); // At ~129 rate
        result.ExchangeRateUsed.Should().BeGreaterThan(100m);
    }

    [Fact]
    public async Task ConvertAmountAsync_InvalidCurrency_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ConvertAmountAsync(100m, "INVALID", "KES");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConvertToBaseCurrencyAsync_ShouldConvertToKes()
    {
        // Arrange
        var amount = 100m; // USD

        // Act
        var result = await _service.ConvertToBaseCurrencyAsync(amount, "USD");

        // Assert
        result.Success.Should().BeTrue();
        result.TargetCurrencyCode.Should().Be("KES");
        result.ConvertedAmount.Should().BeGreaterThan(10000m);
    }

    [Fact]
    public async Task ConvertFromBaseCurrencyAsync_ShouldConvertFromKes()
    {
        // Arrange
        var amount = 12950m; // KES (approximately 100 USD)

        // Act
        var result = await _service.ConvertFromBaseCurrencyAsync(amount, "USD");

        // Assert
        result.Success.Should().BeTrue();
        result.TargetCurrencyCode.Should().Be("USD");
        result.ConvertedAmount.Should().BeApproximately(100m, 5m);
    }

    [Fact]
    public async Task CalculateChangeAsync_SufficientPayment_ShouldReturnCorrectChange()
    {
        // Arrange
        var totalDue = 2500m; // KES
        var amountPaid = 20m; // USD (~2564 KES at 128.2)

        // Act
        var result = await _service.CalculateChangeAsync(totalDue, amountPaid, "USD");

        // Assert
        result.TotalDueInBaseCurrency.Should().Be(totalDue);
        result.AmountPaid.Should().Be(amountPaid);
        result.PaymentCurrencyCode.Should().Be("USD");
        result.ChangeCurrencyCode.Should().Be("KES");
        result.ChangeAmount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CalculateChangeAsync_SameCurrency_ShouldCalculateDirectly()
    {
        // Arrange
        var totalDue = 2500m;
        var amountPaid = 3000m;

        // Act
        var result = await _service.CalculateChangeAsync(totalDue, amountPaid, "KES");

        // Assert
        result.ChangeAmount.Should().Be(500m);
        result.ExchangeRateUsed.Should().Be(1m);
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public async Task ApplyRoundingAsync_StandardRounding_ShouldRoundCorrectly()
    {
        // Act
        var rounded = await _service.ApplyRoundingAsync(123.456m, "USD");

        // Assert
        rounded.Should().Be(123.46m); // Standard 2 decimal places
    }

    [Fact]
    public async Task ApplyRoundingAsync_KES_ShouldRoundToTwoDecimals()
    {
        // Act
        var rounded = await _service.ApplyRoundingAsync(123.456m, "KES");

        // Assert
        rounded.Should().Be(123.46m);
    }

    [Fact]
    public async Task ApplyRoundingAsync_UGX_ShouldRoundToNearest10()
    {
        // Act
        var rounded = await _service.ApplyRoundingAsync(123m, "UGX");

        // Assert
        rounded.Should().Be(120m);
    }

    #endregion

    #region Payment Integration Tests

    [Fact]
    public async Task CreateMultiCurrencyPaymentAsync_ShouldCreatePayment()
    {
        // Arrange
        var receiptId = 123;
        var amountDue = 2500m;
        var amountPaid = 20m;
        var currencyCode = "USD";

        // Act
        var payment = await _service.CreateMultiCurrencyPaymentAsync(
            receiptId, amountDue, amountPaid, currencyCode);

        // Assert
        payment.Should().NotBeNull();
        payment.PaymentCurrencyCode.Should().Be("USD");
        payment.PaymentCurrencySymbol.Should().Be("$");
        payment.AmountInPaymentCurrency.Should().Be(20m);
        payment.AmountInBaseCurrency.Should().BeGreaterThan(2000m);
        payment.ExchangeRateUsed.Should().BeGreaterThan(100m);
    }

    [Fact]
    public async Task GetMultiCurrencyPaymentAsync_ExistingPayment_ShouldReturnPayment()
    {
        // Arrange
        var receiptId = 456;
        await _service.CreateMultiCurrencyPaymentAsync(receiptId, 1000m, 10m, "USD");

        // Act
        var payment = await _service.GetMultiCurrencyPaymentAsync(receiptId);

        // Assert
        payment.Should().NotBeNull();
        payment!.AmountInPaymentCurrency.Should().Be(10m);
    }

    [Fact]
    public async Task GetMultiCurrencyPaymentAsync_NonExistentPayment_ShouldReturnNull()
    {
        // Act
        var payment = await _service.GetMultiCurrencyPaymentAsync(99999);

        // Assert
        payment.Should().BeNull();
    }

    #endregion

    #region Cash Drawer Tests

    [Fact]
    public async Task GetCashDrawerCurrenciesAsync_ShouldReturnCurrencies()
    {
        // Act
        var currencies = await _service.GetCashDrawerCurrenciesAsync(workPeriodId: 1);

        // Assert
        currencies.Should().NotBeEmpty();
        currencies.Should().Contain(c => c.CurrencyCode == "KES");
    }

    [Fact]
    public async Task SetOpeningFloatAsync_ShouldSetFloat()
    {
        // Arrange
        var workPeriodId = 100;
        var openingFloat = 5000m;

        // Act
        var result = await _service.SetOpeningFloatAsync(workPeriodId, "KES", openingFloat);
        var drawer = (await _service.GetCashDrawerCurrenciesAsync(workPeriodId))
            .FirstOrDefault(c => c.CurrencyCode == "KES");

        // Assert
        result.Should().BeTrue();
        drawer.Should().NotBeNull();
        drawer!.OpeningFloat.Should().Be(openingFloat);
    }

    [Fact]
    public async Task RecordCashCountAsync_ShouldRecordCount()
    {
        // Arrange
        var workPeriodId = 101;
        await _service.SetOpeningFloatAsync(workPeriodId, "KES", 5000m);

        // Act
        var drawer = await _service.RecordCashCountAsync(workPeriodId, "KES", 4800m);

        // Assert
        drawer.Should().NotBeNull();
        drawer.ActualCounted.Should().Be(4800m);
        drawer.Variance.Should().NotBeNull();
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetCurrencySettingsAsync_ShouldReturnSettings()
    {
        // Act
        var settings = await _service.GetCurrencySettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.BaseCurrencyCode.Should().Be("KES");
        settings.ActiveCurrencies.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IsMultiCurrencyEnabledAsync_Default_ShouldBeFalse()
    {
        // Act
        var enabled = await _service.IsMultiCurrencyEnabledAsync();

        // Assert
        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetMultiCurrencyEnabledAsync_ShouldToggle()
    {
        // Act
        await _service.SetMultiCurrencyEnabledAsync(true);
        var enabled = await _service.IsMultiCurrencyEnabledAsync();

        // Assert
        enabled.Should().BeTrue();

        // Cleanup
        await _service.SetMultiCurrencyEnabledAsync(false);
    }

    [Fact]
    public async Task UpdateCurrencySettingsAsync_ShouldUpdateSettings()
    {
        // Arrange
        var newSettings = new CurrencySettingsDto
        {
            BaseCurrencyCode = "KES",
            MultiCurrencyEnabled = true,
            RateValidityPeriodDays = 7,
            ShowExchangeRateOnReceipt = false
        };

        // Act
        var updated = await _service.UpdateCurrencySettingsAsync(newSettings);

        // Assert
        updated.MultiCurrencyEnabled.Should().BeTrue();
        updated.RateValidityPeriodDays.Should().Be(7);
        updated.ShowExchangeRateOnReceipt.Should().BeFalse();
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public async Task FormatAmountAsync_USD_ShouldFormatCorrectly()
    {
        // Act
        var formatted = await _service.FormatAmountAsync(1234.56m, "USD");

        // Assert
        formatted.Should().Contain("$");
        formatted.Should().Contain("1,234.56");
    }

    [Fact]
    public async Task FormatAmountAsync_KES_ShouldFormatCorrectly()
    {
        // Act
        var formatted = await _service.FormatAmountAsync(1234.56m, "KES");

        // Assert
        formatted.Should().Contain("KSh");
    }

    [Fact]
    public void FormatExchangeRate_ShouldFormatCorrectly()
    {
        // Act
        var formatted = _service.FormatExchangeRate("USD", "KES", 129.50m);

        // Assert
        formatted.Should().Be("1 USD = 129.50 KES");
    }

    #endregion

    #region Reporting Tests

    [Fact]
    public async Task GetCurrencyReportAsync_ShouldReturnReport()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.Date.AddMonths(-1);
        var toDate = DateTime.UtcNow.Date;

        // Act
        var report = await _service.GetCurrencyReportAsync(fromDate, toDate);

        // Assert
        report.Should().NotBeNull();
        report.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkPeriodCurrencySummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var workPeriodId = 200;
        await _service.SetOpeningFloatAsync(workPeriodId, "KES", 5000m);

        // Act
        var summary = await _service.GetWorkPeriodCurrencySummaryAsync(workPeriodId);

        // Assert
        summary.Should().NotBeEmpty();
        summary.Should().Contain(s => s.CurrencyCode == "KES");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ConvertAmountAsync_ZeroAmount_ShouldReturnZero()
    {
        // Act
        var result = await _service.ConvertAmountAsync(0m, "USD", "KES");

        // Assert
        result.Success.Should().BeTrue();
        result.ConvertedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateChangeAsync_ExactPayment_ShouldReturnZeroChange()
    {
        // Arrange - pay exact amount in KES
        var totalDue = 1000m;

        // Act
        var result = await _service.CalculateChangeAsync(totalDue, totalDue, "KES");

        // Assert
        result.ChangeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task GetCurrencyByCodeAsync_CaseInsensitive_ShouldWork()
    {
        // Act
        var lower = await _service.GetCurrencyByCodeAsync("usd");
        var upper = await _service.GetCurrencyByCodeAsync("USD");
        var mixed = await _service.GetCurrencyByCodeAsync("Usd");

        // Assert
        lower.Should().NotBeNull();
        upper.Should().NotBeNull();
        mixed.Should().NotBeNull();
        lower!.Code.Should().Be(upper!.Code).And.Be(mixed!.Code);
    }

    [Fact]
    public async Task CreateCurrencyAsync_AsDefault_ShouldUnsetOtherDefaults()
    {
        // Arrange
        var oldDefault = await _service.GetDefaultCurrencyAsync();

        var request = new CreateCurrencyRequest
        {
            Code = "ZAR",
            Name = "South African Rand",
            Symbol = "R",
            IsDefault = true
        };

        // Act
        var newCurrency = await _service.CreateCurrencyAsync(request);
        var newDefault = await _service.GetDefaultCurrencyAsync();
        var oldCurrency = await _service.GetCurrencyByCodeAsync(oldDefault.Code);

        // Assert
        newDefault.Code.Should().Be("ZAR");
        oldCurrency!.IsDefault.Should().BeFalse();

        // Cleanup
        await _service.SetDefaultCurrencyAsync(oldDefault.Code);
    }

    #endregion
}
