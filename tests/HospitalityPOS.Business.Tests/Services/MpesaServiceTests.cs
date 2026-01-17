using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the MpesaService class.
/// Tests cover STK Push, callback processing, manual entry, and configuration management.
/// </summary>
public class MpesaServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger<MpesaService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly MpesaService _mpesaService;

    public MpesaServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger<MpesaService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // Setup HttpClient mock
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://sandbox.safaricom.co.ke")
        };
        _httpClientFactoryMock
            .Setup(f => f.CreateClient("MpesaApi"))
            .Returns(httpClient);

        _mpesaService = new MpesaService(
            _context,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var config = new MpesaConfiguration
        {
            Id = 1,
            Name = "Test Config",
            Environment = MpesaEnvironment.Sandbox,
            ConsumerKey = "test_consumer_key",
            ConsumerSecret = "test_consumer_secret",
            BusinessShortCode = "174379",
            Passkey = "test_passkey",
            TransactionType = MpesaTransactionType.CustomerBuyGoodsOnline,
            CallbackUrl = "https://example.com/callback",
            ApiBaseUrl = "https://sandbox.safaricom.co.ke",
            AccountReferencePrefix = "POS",
            IsActive = true
        };

        _context.Set<MpesaConfiguration>().Add(config);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Configuration Tests

    [Fact]
    public async Task GetActiveConfigurationAsync_ShouldReturnActiveConfig()
    {
        // Act
        var config = await _mpesaService.GetActiveConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config!.IsActive.Should().BeTrue();
        config.Name.Should().Be("Test Config");
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ShouldReturnAllConfigs()
    {
        // Arrange
        _context.Set<MpesaConfiguration>().Add(new MpesaConfiguration
        {
            Name = "Second Config",
            Environment = MpesaEnvironment.Production,
            ConsumerKey = "key2",
            ConsumerSecret = "secret2",
            BusinessShortCode = "999999",
            Passkey = "passkey2",
            CallbackUrl = "https://example.com/callback2",
            ApiBaseUrl = "https://api.safaricom.co.ke",
            IsActive = false
        });
        await _context.SaveChangesAsync();

        // Act
        var configs = await _mpesaService.GetAllConfigurationsAsync();

        // Assert
        configs.Should().HaveCount(2);
        configs.First().IsActive.Should().BeTrue(); // Active first
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldCreateNewConfig()
    {
        // Arrange
        var newConfig = new MpesaConfiguration
        {
            Name = "New Config",
            Environment = MpesaEnvironment.Production,
            ConsumerKey = "new_key",
            ConsumerSecret = "new_secret",
            BusinessShortCode = "123456",
            Passkey = "new_passkey",
            CallbackUrl = "https://example.com/new",
            ApiBaseUrl = "https://api.safaricom.co.ke",
            IsActive = false
        };

        // Act
        var saved = await _mpesaService.SaveConfigurationAsync(newConfig);

        // Assert
        saved.Id.Should().BeGreaterThan(0);
        var retrieved = await _context.Set<MpesaConfiguration>().FindAsync(saved.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("New Config");
    }

    [Fact]
    public async Task ActivateConfigurationAsync_ShouldDeactivateOthers()
    {
        // Arrange
        var secondConfig = new MpesaConfiguration
        {
            Name = "Second Config",
            Environment = MpesaEnvironment.Sandbox,
            ConsumerKey = "key2",
            ConsumerSecret = "secret2",
            BusinessShortCode = "999999",
            Passkey = "passkey2",
            CallbackUrl = "https://example.com/callback2",
            ApiBaseUrl = "https://sandbox.safaricom.co.ke",
            IsActive = false
        };
        _context.Set<MpesaConfiguration>().Add(secondConfig);
        await _context.SaveChangesAsync();

        // Act
        await _mpesaService.ActivateConfigurationAsync(secondConfig.Id);

        // Assert
        var configs = await _context.Set<MpesaConfiguration>().ToListAsync();
        configs.Single(c => c.IsActive).Id.Should().Be(secondConfig.Id);
    }

    #endregion

    #region Phone Number Validation Tests

    [Theory]
    [InlineData("254712345678", true)]
    [InlineData("254700000000", true)]
    [InlineData("0712345678", true)] // Will be formatted
    [InlineData("712345678", true)] // Will be formatted
    [InlineData("12345", false)]
    [InlineData("", false)]
    public async Task ValidatePhoneNumberAsync_ShouldValidateCorrectly(string phone, bool expectedValid)
    {
        // Act
        var formatted = _mpesaService.FormatPhoneNumber(phone);
        var isValid = await _mpesaService.ValidatePhoneNumberAsync(formatted);

        // Assert
        if (expectedValid && !string.IsNullOrEmpty(phone) && phone.Length >= 9)
        {
            isValid.Should().BeTrue($"Phone {phone} should be valid when formatted as {formatted}");
        }
    }

    [Theory]
    [InlineData("0712345678", "254712345678")]
    [InlineData("712345678", "254712345678")]
    [InlineData("254712345678", "254712345678")]
    [InlineData("+254712345678", "254712345678")]
    public void FormatPhoneNumber_ShouldFormatCorrectly(string input, string expected)
    {
        // Act
        var result = _mpesaService.FormatPhoneNumber(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region STK Push Tests

    [Fact]
    public async Task InitiateStkPushAsync_ShouldFailWithNoActiveConfig()
    {
        // Arrange
        var config = await _context.Set<MpesaConfiguration>().FirstAsync();
        config.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _mpesaService.InitiateStkPushAsync(
            "254712345678", 100m, "R001", "Test payment");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active M-Pesa configuration");
    }

    [Fact]
    public async Task InitiateStkPushAsync_ShouldFailWithInvalidPhone()
    {
        // Act
        var result = await _mpesaService.InitiateStkPushAsync(
            "12345", 100m, "R001", "Test payment");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid phone number");
    }

    [Fact]
    public async Task InitiateStkPushAsync_ShouldCreateRequestRecord()
    {
        // Arrange
        SetupMockHttpResponses(
            tokenResponse: new { access_token = "test_token", expires_in = 3600 },
            stkResponse: new
            {
                MerchantRequestID = "MR123",
                CheckoutRequestID = "CR123",
                ResponseCode = "0",
                ResponseDescription = "Success",
                CustomerMessage = "Success. Request accepted"
            });

        // Act
        var result = await _mpesaService.InitiateStkPushAsync(
            "254712345678", 100m, "R001", "Test payment", receiptId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.CheckoutRequestId.Should().Be("CR123");

        var request = await _context.Set<MpesaStkPushRequest>()
            .FirstOrDefaultAsync(r => r.CheckoutRequestId == "CR123");
        request.Should().NotBeNull();
        request!.Amount.Should().Be(100m);
        request.PhoneNumber.Should().Be("254712345678");
        request.Status.Should().Be(MpesaStkStatus.Processing);
    }

    #endregion

    #region Callback Processing Tests

    [Fact]
    public async Task ProcessStkCallbackAsync_ShouldUpdateRequestOnSuccess()
    {
        // Arrange
        var request = new MpesaStkPushRequest
        {
            ConfigurationId = 1,
            CheckoutRequestId = "CB_TEST_123",
            MerchantRequestId = "MR_TEST_123",
            PhoneNumber = "254712345678",
            Amount = 500m,
            AccountReference = "POS-R001",
            TransactionDescription = "Test",
            Status = MpesaStkStatus.Processing
        };
        _context.Set<MpesaStkPushRequest>().Add(request);
        await _context.SaveChangesAsync();

        var callbackJson = JsonSerializer.Serialize(new
        {
            Body = new
            {
                stkCallback = new
                {
                    MerchantRequestID = "MR_TEST_123",
                    CheckoutRequestID = "CB_TEST_123",
                    ResultCode = 0,
                    ResultDesc = "The service request is processed successfully.",
                    CallbackMetadata = new
                    {
                        Item = new[]
                        {
                            new { Name = "MpesaReceiptNumber", Value = "PGK12345678" },
                            new { Name = "TransactionDate", Value = "20260116120000" },
                            new { Name = "PhoneNumber", Value = "254712345678" }
                        }
                    }
                }
            }
        });

        // Act
        await _mpesaService.ProcessStkCallbackAsync(callbackJson);

        // Assert
        var updated = await _context.Set<MpesaStkPushRequest>()
            .FirstOrDefaultAsync(r => r.CheckoutRequestId == "CB_TEST_123");
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(MpesaStkStatus.Success);
        updated.MpesaReceiptNumber.Should().Be("PGK12345678");
        updated.CallbackReceivedAt.Should().NotBeNull();

        // Should also create transaction record
        var transaction = await _context.Set<MpesaTransaction>()
            .FirstOrDefaultAsync(t => t.MpesaReceiptNumber == "PGK12345678");
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task ProcessStkCallbackAsync_ShouldMarkAsCancelledOnUserCancel()
    {
        // Arrange
        var request = new MpesaStkPushRequest
        {
            ConfigurationId = 1,
            CheckoutRequestId = "CB_CANCEL_123",
            MerchantRequestId = "MR_CANCEL_123",
            PhoneNumber = "254712345678",
            Amount = 100m,
            AccountReference = "POS-R002",
            TransactionDescription = "Test",
            Status = MpesaStkStatus.Processing
        };
        _context.Set<MpesaStkPushRequest>().Add(request);
        await _context.SaveChangesAsync();

        var callbackJson = JsonSerializer.Serialize(new
        {
            Body = new
            {
                stkCallback = new
                {
                    MerchantRequestID = "MR_CANCEL_123",
                    CheckoutRequestID = "CB_CANCEL_123",
                    ResultCode = 1032,
                    ResultDesc = "Request cancelled by user"
                }
            }
        });

        // Act
        await _mpesaService.ProcessStkCallbackAsync(callbackJson);

        // Assert
        var updated = await _context.Set<MpesaStkPushRequest>()
            .FirstOrDefaultAsync(r => r.CheckoutRequestId == "CB_CANCEL_123");
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(MpesaStkStatus.Cancelled);
    }

    #endregion

    #region Manual Entry Tests

    [Fact]
    public async Task RecordManualTransactionAsync_ShouldCreateTransaction()
    {
        // Act
        var transaction = await _mpesaService.RecordManualTransactionAsync(
            "PGK999999999",
            250m,
            "0712345678",
            DateTime.UtcNow,
            "Manual entry for testing",
            userId: 1);

        // Assert
        transaction.Should().NotBeNull();
        transaction.MpesaReceiptNumber.Should().Be("PGK999999999");
        transaction.Amount.Should().Be(250m);
        transaction.IsManualEntry.Should().BeTrue();
        transaction.IsVerified.Should().BeFalse();
        transaction.RecordedByUserId.Should().Be(1);
    }

    [Fact]
    public async Task RecordManualTransactionAsync_ShouldRejectDuplicateReceipt()
    {
        // Arrange
        var existingTx = new MpesaTransaction
        {
            MpesaReceiptNumber = "DUPLICATE123",
            Amount = 100m,
            PhoneNumber = "254712345678",
            TransactionDate = DateTime.UtcNow,
            Status = MpesaTransactionStatus.Completed,
            IsManualEntry = true
        };
        _context.Set<MpesaTransaction>().Add(existingTx);
        await _context.SaveChangesAsync();

        // Act & Assert
        var action = async () => await _mpesaService.RecordManualTransactionAsync(
            "DUPLICATE123", 200m, "0712345678", DateTime.UtcNow, null, 1);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task VerifyTransactionAsync_ShouldUpdateVerificationStatus()
    {
        // Arrange
        var tx = new MpesaTransaction
        {
            MpesaReceiptNumber = "VERIFY123",
            Amount = 100m,
            PhoneNumber = "254712345678",
            TransactionDate = DateTime.UtcNow,
            Status = MpesaTransactionStatus.Completed,
            IsManualEntry = true,
            IsVerified = false
        };
        _context.Set<MpesaTransaction>().Add(tx);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mpesaService.VerifyTransactionAsync(tx.Id, verifiedByUserId: 2);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.Set<MpesaTransaction>().FindAsync(tx.Id);
        updated!.IsVerified.Should().BeTrue();
        updated.VerifiedByUserId.Should().Be(2);
        updated.VerifiedAt.Should().NotBeNull();
    }

    #endregion

    #region Transaction History Tests

    [Fact]
    public async Task GetTransactionByReceiptNumberAsync_ShouldFindTransaction()
    {
        // Arrange
        var tx = new MpesaTransaction
        {
            MpesaReceiptNumber = "FIND123",
            Amount = 100m,
            PhoneNumber = "254712345678",
            TransactionDate = DateTime.UtcNow,
            Status = MpesaTransactionStatus.Completed
        };
        _context.Set<MpesaTransaction>().Add(tx);
        await _context.SaveChangesAsync();

        // Act
        var found = await _mpesaService.GetTransactionByReceiptNumberAsync("FIND123");

        // Assert
        found.Should().NotBeNull();
        found!.MpesaReceiptNumber.Should().Be("FIND123");
    }

    [Fact]
    public async Task GetTransactionsByDateRangeAsync_ShouldFilterByDate()
    {
        // Arrange
        var today = DateTime.Today;
        var transactions = new[]
        {
            new MpesaTransaction { MpesaReceiptNumber = "TX1", Amount = 100m, PhoneNumber = "254712345678", TransactionDate = today, Status = MpesaTransactionStatus.Completed },
            new MpesaTransaction { MpesaReceiptNumber = "TX2", Amount = 200m, PhoneNumber = "254712345678", TransactionDate = today.AddDays(-5), Status = MpesaTransactionStatus.Completed },
            new MpesaTransaction { MpesaReceiptNumber = "TX3", Amount = 300m, PhoneNumber = "254712345678", TransactionDate = today.AddDays(-10), Status = MpesaTransactionStatus.Completed }
        };
        _context.Set<MpesaTransaction>().AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mpesaService.GetTransactionsByDateRangeAsync(
            today.AddDays(-7), today);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.MpesaReceiptNumber == "TX1");
        result.Should().Contain(t => t.MpesaReceiptNumber == "TX2");
    }

    [Fact]
    public async Task GetUnverifiedTransactionsAsync_ShouldReturnOnlyUnverifiedManual()
    {
        // Arrange
        var transactions = new[]
        {
            new MpesaTransaction { MpesaReceiptNumber = "UNVER1", Amount = 100m, PhoneNumber = "254712345678", TransactionDate = DateTime.UtcNow, Status = MpesaTransactionStatus.Completed, IsManualEntry = true, IsVerified = false },
            new MpesaTransaction { MpesaReceiptNumber = "UNVER2", Amount = 200m, PhoneNumber = "254712345678", TransactionDate = DateTime.UtcNow, Status = MpesaTransactionStatus.Completed, IsManualEntry = true, IsVerified = true },
            new MpesaTransaction { MpesaReceiptNumber = "UNVER3", Amount = 300m, PhoneNumber = "254712345678", TransactionDate = DateTime.UtcNow, Status = MpesaTransactionStatus.Completed, IsManualEntry = false, IsVerified = false }
        };
        _context.Set<MpesaTransaction>().AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mpesaService.GetUnverifiedTransactionsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Single().MpesaReceiptNumber.Should().Be("UNVER1");
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnCorrectStats()
    {
        // Arrange
        var today = DateTime.Today;
        var transactions = new[]
        {
            new MpesaTransaction { MpesaReceiptNumber = "DASH1", Amount = 100m, PhoneNumber = "254712345678", TransactionDate = today.AddHours(10), Status = MpesaTransactionStatus.Completed },
            new MpesaTransaction { MpesaReceiptNumber = "DASH2", Amount = 200m, PhoneNumber = "254712345678", TransactionDate = today.AddHours(14), Status = MpesaTransactionStatus.Completed },
            new MpesaTransaction { MpesaReceiptNumber = "DASH3", Amount = 300m, PhoneNumber = "254712345678", TransactionDate = today.AddDays(-5), Status = MpesaTransactionStatus.Completed }
        };
        _context.Set<MpesaTransaction>().AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _mpesaService.GetDashboardDataAsync();

        // Assert
        dashboard.IsConfigured.Should().BeTrue();
        dashboard.IsTestMode.Should().BeTrue(); // Sandbox
        dashboard.ShortCode.Should().Be("174379");
        dashboard.TodayTransactions.Should().Be(2);
        dashboard.TodayAmount.Should().Be(300m);
        dashboard.MonthTransactions.Should().BeGreaterOrEqualTo(2);
    }

    #endregion

    #region Helper Methods

    private void SetupMockHttpResponses(object? tokenResponse = null, object? stkResponse = null)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("oauth")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(tokenResponse ?? new { access_token = "test_token", expires_in = 3600 })
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("stkpush")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(stkResponse ?? new
                {
                    MerchantRequestID = "MR_DEFAULT",
                    CheckoutRequestID = "CR_DEFAULT",
                    ResponseCode = "0",
                    ResponseDescription = "Success"
                })
            });
    }

    #endregion
}
