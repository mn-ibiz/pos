using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using FluentAssertions;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the MobileMoneyService class.
/// </summary>
public class MobileMoneyServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly IMobileMoneyService _service;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;

    public MobileMoneyServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);

        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object);

        _service = new MobileMoneyService(_context, httpClient);

        // Seed test store
        _context.Stores.Add(new Store { Id = 1, Name = "Test Store", Code = "TST001" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Airtel Money Configuration Tests

    [Fact]
    public async Task SaveAirtelMoneyConfigurationAsync_ShouldSaveConfiguration()
    {
        // Arrange
        var config = new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            ClientId = "client_id",
            ClientSecretEncrypted = "encrypted_secret",
            CallbackUrl = "https://example.com/callback",
            IsEnabled = true
        };

        // Act
        var result = await _service.SaveAirtelMoneyConfigurationAsync(config);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.MerchantCode.Should().Be("MERCHANT123");
    }

    [Fact]
    public async Task GetAirtelMoneyConfigurationAsync_ShouldReturnConfiguration()
    {
        // Arrange
        var config = new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            ClientId = "client_id",
            IsEnabled = true
        };
        await _service.SaveAirtelMoneyConfigurationAsync(config);

        // Act
        var result = await _service.GetAirtelMoneyConfigurationAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.MerchantCode.Should().Be("MERCHANT123");
    }

    [Fact]
    public async Task TestAirtelMoneyConnectionAsync_ShouldTestConnection()
    {
        // Arrange
        var config = await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            ClientId = "client_id"
        });

        // Act
        var result = await _service.TestAirtelMoneyConnectionAsync(config.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetAirtelMoneyEnabledAsync_ShouldToggleEnabled()
    {
        // Arrange
        var config = await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = false
        });

        // Act
        await _service.SetAirtelMoneyEnabledAsync(config.Id, true);

        // Assert
        var updated = await _service.GetAirtelMoneyConfigurationAsync(1);
        updated!.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Airtel Money Payment Tests

    [Fact]
    public async Task InitiateAirtelMoneyPaymentAsync_WithValidPhone_ShouldInitiatePayment()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        var request = new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        };

        // Act
        var result = await _service.InitiateAirtelMoneyPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransactionReference.Should().StartWith("AM");
        result.Status.Should().Be(MobileMoneyTransactionStatus.Pending);
    }

    [Fact]
    public async Task InitiateAirtelMoneyPaymentAsync_WithInvalidPhone_ShouldFail()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        var request = new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0722123456", // M-Pesa number, not Airtel
            Amount = 1000
        };

        // Act
        var result = await _service.InitiateAirtelMoneyPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PHONE");
    }

    [Fact]
    public async Task InitiateAirtelMoneyPaymentAsync_WhenNotConfigured_ShouldFail()
    {
        // Arrange
        var request = new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        };

        // Act
        var result = await _service.InitiateAirtelMoneyPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_CONFIGURED");
    }

    [Fact]
    public async Task CheckAirtelMoneyPaymentStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        var payment = await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        });

        // Act
        var result = await _service.CheckAirtelMoneyPaymentStatusAsync(payment.TransactionReference);

        // Assert
        result.TransactionReference.Should().Be(payment.TransactionReference);
        result.Status.Should().Be(MobileMoneyTransactionStatus.Pending);
        result.Amount.Should().Be(1000);
    }

    [Fact]
    public async Task GetAirtelMoneyTransactionsAsync_ShouldReturnTransactions()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        });

        await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0755123456",
            Amount = 2000
        });

        // Act
        var result = await _service.GetAirtelMoneyTransactionsAsync(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region T-Kash Configuration Tests

    [Fact]
    public async Task SaveTKashConfigurationAsync_ShouldSaveConfiguration()
    {
        // Arrange
        var config = new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            ApiKey = "api_key",
            ApiSecretEncrypted = "encrypted_secret",
            CallbackUrl = "https://example.com/callback",
            IsEnabled = true
        };

        // Act
        var result = await _service.SaveTKashConfigurationAsync(config);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.MerchantId.Should().Be("TKASH123");
    }

    [Fact]
    public async Task GetTKashConfigurationAsync_ShouldReturnConfiguration()
    {
        // Arrange
        var config = new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            ApiKey = "api_key",
            IsEnabled = true
        };
        await _service.SaveTKashConfigurationAsync(config);

        // Act
        var result = await _service.GetTKashConfigurationAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.MerchantId.Should().Be("TKASH123");
    }

    [Fact]
    public async Task TestTKashConnectionAsync_ShouldTestConnection()
    {
        // Arrange
        var config = await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            ApiKey = "api_key"
        });

        // Act
        var result = await _service.TestTKashConnectionAsync(config.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region T-Kash Payment Tests

    [Fact]
    public async Task InitiateTKashPaymentAsync_WithValidPhone_ShouldInitiatePayment()
    {
        // Arrange
        await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            IsEnabled = true
        });

        var request = new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0770123456",
            Amount = 1000
        };

        // Act
        var result = await _service.InitiateTKashPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransactionReference.Should().StartWith("TK");
        result.Status.Should().Be(MobileMoneyTransactionStatus.Pending);
    }

    [Fact]
    public async Task InitiateTKashPaymentAsync_WithInvalidPhone_ShouldFail()
    {
        // Arrange
        await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            IsEnabled = true
        });

        var request = new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0722123456", // M-Pesa number
            Amount = 1000
        };

        // Act
        var result = await _service.InitiateTKashPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PHONE");
    }

    [Fact]
    public async Task CheckTKashPaymentStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            IsEnabled = true
        });

        var payment = await _service.InitiateTKashPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0770123456",
            Amount = 1500
        });

        // Act
        var result = await _service.CheckTKashPaymentStatusAsync(payment.TransactionReference);

        // Assert
        result.TransactionReference.Should().Be(payment.TransactionReference);
        result.Status.Should().Be(MobileMoneyTransactionStatus.Pending);
        result.Amount.Should().Be(1500);
    }

    #endregion

    #region Phone Validation Tests

    [Theory]
    [InlineData("0733123456", MobileMoneyProvider.AirtelMoney, true)]
    [InlineData("0755123456", MobileMoneyProvider.AirtelMoney, true)]
    [InlineData("0722123456", MobileMoneyProvider.AirtelMoney, false)] // M-Pesa number
    [InlineData("0770123456", MobileMoneyProvider.TKash, true)]
    [InlineData("0722123456", MobileMoneyProvider.TKash, false)] // M-Pesa number
    [InlineData("0722123456", MobileMoneyProvider.MPesa, true)]
    [InlineData("0733123456", MobileMoneyProvider.MPesa, false)] // Airtel number
    public async Task ValidatePhoneNumberAsync_ShouldValidateCorrectly(string phone, MobileMoneyProvider provider, bool expectedValid)
    {
        // Act
        var result = await _service.ValidatePhoneNumberAsync(phone, provider);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public async Task ValidatePhoneNumberAsync_WithEmptyPhone_ShouldFail()
    {
        // Act
        var result = await _service.ValidatePhoneNumberAsync("", MobileMoneyProvider.AirtelMoney);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Phone number is required");
    }

    [Fact]
    public async Task ValidatePhoneNumberAsync_ShouldDetectWrongProvider()
    {
        // Act - trying to validate M-Pesa number as Airtel
        var result = await _service.ValidatePhoneNumberAsync("0722123456", MobileMoneyProvider.AirtelMoney);

        // Assert
        result.IsValid.Should().BeFalse();
        result.DetectedProvider.Should().Be(MobileMoneyProvider.MPesa);
    }

    #endregion

    #region Available Providers Tests

    [Fact]
    public async Task GetAvailableProvidersAsync_ShouldReturnAllProviders()
    {
        // Act
        var result = await _service.GetAvailableProvidersAsync(1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Provider == MobileMoneyProvider.MPesa);
        result.Should().Contain(p => p.Provider == MobileMoneyProvider.AirtelMoney);
        result.Should().Contain(p => p.Provider == MobileMoneyProvider.TKash);
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_ShouldShowConfiguredProviders()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        // Act
        var result = await _service.GetAvailableProvidersAsync(1);

        // Assert
        var airtelProvider = result.First(p => p.Provider == MobileMoneyProvider.AirtelMoney);
        airtelProvider.IsConfigured.Should().BeTrue();
        airtelProvider.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Transaction Logs Tests

    [Fact]
    public async Task GetTransactionLogsAsync_ShouldReturnLogs()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        });

        // Act
        var result = await _service.GetTransactionLogsAsync(storeId: 1, provider: MobileMoneyProvider.AirtelMoney);

        // Assert
        result.Should().NotBeEmpty();
        result.First().EntryType.Should().Be("INITIATED");
    }

    #endregion

    #region Reconciliation Report Tests

    [Fact]
    public async Task GetReconciliationReportAsync_ShouldReturnReport()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        });

        await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            IsEnabled = true
        });

        await _service.InitiateTKashPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0770123456",
            Amount = 2000
        });

        // Act
        var result = await _service.GetReconciliationReportAsync(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // Assert
        result.StoreId.Should().Be(1);
        result.AirtelMoneySummary.TotalTransactions.Should().Be(1);
        result.AirtelMoneySummary.TotalAmount.Should().Be(1000);
        result.TKashSummary.TotalTransactions.Should().Be(1);
        result.TKashSummary.TotalAmount.Should().Be(2000);
        result.TotalAmount.Should().Be(3000);
    }

    #endregion

    #region Cancel Payment Tests

    [Fact]
    public async Task CancelPaymentAsync_WithPendingAirtelPayment_ShouldCancel()
    {
        // Arrange
        await _service.SaveAirtelMoneyConfigurationAsync(new AirtelMoneyConfiguration
        {
            StoreId = 1,
            MerchantCode = "MERCHANT123",
            IsEnabled = true
        });

        var payment = await _service.InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0733123456",
            Amount = 1000
        });

        // Act
        var result = await _service.CancelPaymentAsync(payment.TransactionReference, MobileMoneyProvider.AirtelMoney);

        // Assert
        result.Should().BeTrue();
        var status = await _service.CheckAirtelMoneyPaymentStatusAsync(payment.TransactionReference);
        status.Status.Should().Be(MobileMoneyTransactionStatus.Cancelled);
    }

    [Fact]
    public async Task CancelPaymentAsync_WithPendingTKashPayment_ShouldCancel()
    {
        // Arrange
        await _service.SaveTKashConfigurationAsync(new TKashConfiguration
        {
            StoreId = 1,
            MerchantId = "TKASH123",
            IsEnabled = true
        });

        var payment = await _service.InitiateTKashPaymentAsync(new MobileMoneyPaymentRequest
        {
            StoreId = 1,
            PhoneNumber = "0770123456",
            Amount = 1000
        });

        // Act
        var result = await _service.CancelPaymentAsync(payment.TransactionReference, MobileMoneyProvider.TKash);

        // Assert
        result.Should().BeTrue();
        var status = await _service.CheckTKashPaymentStatusAsync(payment.TransactionReference);
        status.Status.Should().Be(MobileMoneyTransactionStatus.Cancelled);
    }

    #endregion
}
