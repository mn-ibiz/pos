using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for HotelPMSService.
/// </summary>
public class HotelPMSServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly HotelPMSService _service;

    public HotelPMSServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _service = new HotelPMSService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Configuration Tests

    [Fact]
    public async Task CreateConfigurationAsync_ShouldCreateConfiguration()
    {
        // Arrange
        var config = new PMSConfiguration
        {
            Name = "Opera Cloud",
            PMSType = PMSType.Opera,
            PropertyCode = "HOTEL001",
            ApiEndpoint = "https://opera.example.com/api",
            IsDefault = true,
            IsActive = true
        };

        // Act
        var result = await _service.CreateConfigurationAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldClearOtherDefaults()
    {
        // Arrange
        var config1 = await CreateTestPMSConfiguration(true);
        var config2 = new PMSConfiguration
        {
            Name = "Second Config",
            PMSType = PMSType.Mews,
            PropertyCode = "HOTEL002",
            ApiEndpoint = "https://mews.example.com/api",
            IsDefault = true,
            IsActive = true
        };

        // Act
        await _service.CreateConfigurationAsync(config2);

        // Assert
        var updated = await _service.GetConfigurationByIdAsync(config1.Id);
        updated!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task GetDefaultConfigurationAsync_ShouldReturnDefaultConfig()
    {
        // Arrange
        await CreateTestPMSConfiguration(false);
        var defaultConfig = await CreateTestPMSConfiguration(true);

        // Act
        var result = await _service.GetDefaultConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(defaultConfig.Id);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ShouldReturnAllConfigs()
    {
        // Arrange
        await CreateTestPMSConfiguration(false);
        await CreateTestPMSConfiguration(true);

        // Act
        var result = await _service.GetAllConfigurationsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnResult()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);

        // Act
        var result = await _service.TestConnectionAsync(config.Id);

        // Assert
        result.Should().NotBeNull();
        result.TestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Revenue Center Tests

    [Fact]
    public async Task CreateRevenueCenterAsync_ShouldCreateRevenueCenter()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var revenueCenter = new PMSRevenueCenter
        {
            PMSConfigurationId = config.Id,
            RevenueCenterCode = "RC001",
            RevenueCenterName = "Restaurant",
            DefaultChargeType = RoomChargeType.FoodAndBeverage,
            IsEnabled = true,
            IsActive = true
        };

        // Act
        var result = await _service.CreateRevenueCenterAsync(revenueCenter);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRevenueCentersAsync_ShouldReturnCenters()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await _service.CreateRevenueCenterAsync(new PMSRevenueCenter
        {
            PMSConfigurationId = config.Id,
            RevenueCenterCode = "RC001",
            RevenueCenterName = "Restaurant",
            IsActive = true
        });
        await _service.CreateRevenueCenterAsync(new PMSRevenueCenter
        {
            PMSConfigurationId = config.Id,
            RevenueCenterCode = "RC002",
            RevenueCenterName = "Bar",
            IsActive = true
        });

        // Act
        var result = await _service.GetRevenueCentersAsync(config.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Room Charge Posting Tests

    [Fact]
    public async Task PostRoomChargeAsync_ShouldCreatePosting()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");

        var request = new RoomChargeRequest
        {
            ConfigId = config.Id,
            RoomNumber = "101",
            GuestName = "John Doe",
            ChargeType = RoomChargeType.FoodAndBeverage,
            Amount = 5000m,
            TaxAmount = 800m,
            ServiceCharge = 500m,
            Description = "Dinner at Restaurant",
            UserId = 1
        };

        // Act
        var result = await _service.PostRoomChargeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PostingId.Should().BeGreaterThan(0);
        result.Status.Should().Be(PostingStatus.Posted);
    }

    [Fact]
    public async Task PostRoomChargeAsync_ShouldFailWithoutConfig()
    {
        // Arrange
        var request = new RoomChargeRequest
        {
            RoomNumber = "101",
            GuestName = "John Doe",
            Amount = 5000m
        };

        // Act
        var result = await _service.PostRoomChargeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No PMS configuration");
    }

    [Fact]
    public async Task GetPostingsByStatusAsync_ShouldReturnPostings()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");
        await _service.PostRoomChargeAsync(new RoomChargeRequest
        {
            ConfigId = config.Id,
            RoomNumber = "101",
            GuestName = "John Doe",
            Amount = 5000m,
            UserId = 1
        });

        // Act
        var posted = await _service.GetPostingsByStatusAsync(PostingStatus.Posted, config.Id);

        // Assert
        posted.Should().HaveCount(1);
    }

    [Fact]
    public async Task CancelPostingAsync_ShouldCancelPendingPosting()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var posting = new RoomChargePosting
        {
            PMSConfigurationId = config.Id,
            PostingReference = "TEST-001",
            RoomNumber = "101",
            GuestName = "John Doe",
            Amount = 5000m,
            TotalAmount = 5000m,
            Status = PostingStatus.Pending,
            IsActive = true
        };
        _context.RoomChargePostings.Add(posting);
        await _context.SaveChangesAsync();

        // Act
        await _service.CancelPostingAsync(posting.Id, 1, "Customer cancelled");

        // Assert
        var updated = await _service.GetPostingByIdAsync(posting.Id);
        updated!.Status.Should().Be(PostingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelPostingAsync_ShouldThrow_WhenAlreadyPosted()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var posting = new RoomChargePosting
        {
            PMSConfigurationId = config.Id,
            PostingReference = "TEST-001",
            RoomNumber = "101",
            GuestName = "John Doe",
            Amount = 5000m,
            TotalAmount = 5000m,
            Status = PostingStatus.Posted,
            IsActive = true
        };
        _context.RoomChargePostings.Add(posting);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelPostingAsync(posting.Id, 1, "Cancel"));
    }

    #endregion

    #region Guest Lookup Tests

    [Fact]
    public async Task LookupGuestByRoomAsync_ShouldReturnGuest()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);

        // Act
        var result = await _service.LookupGuestByRoomAsync("101", config.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Guest.Should().NotBeNull();
        result.Guest!.RoomNumber.Should().Be("101");
    }

    [Fact]
    public async Task LookupGuestByRoomAsync_ShouldCacheResult()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);

        // First lookup
        var result1 = await _service.LookupGuestByRoomAsync("101", config.Id);

        // Act - Second lookup should be cached
        var result2 = await _service.LookupGuestByRoomAsync("101", config.Id);

        // Assert
        result1.IsCached.Should().BeFalse();
        result2.IsCached.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRoomChargeAsync_ShouldAllowValidCharge()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");

        // Act
        var result = await _service.ValidateRoomChargeAsync("101", 5000m, config.Id);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.GuestStatus.Should().Be(GuestStatus.InHouse);
    }

    [Fact]
    public async Task ValidateRoomChargeAsync_ShouldDenyOverCreditLimit()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101", creditLimit: 50000m, currentBalance: 48000m);

        // Act
        var result = await _service.ValidateRoomChargeAsync("101", 5000m, config.Id);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Contain("Insufficient credit");
    }

    [Fact]
    public async Task GetInHouseGuestsAsync_ShouldReturnInHouseGuests()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");
        await SetupTestGuestCache(config.Id, "102");

        // Act
        var result = await _service.GetInHouseGuestsAsync(config.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Queue Management Tests

    [Fact]
    public async Task AddToQueueAsync_ShouldAddItemToQueue()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var posting = await CreateTestPosting(config.Id);

        // Act
        await _service.AddToQueueAsync(posting.Id);

        // Assert
        var queueItems = await _service.GetPendingQueueItemsAsync();
        queueItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetQueueStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var posting1 = await CreateTestPosting(config.Id);
        var posting2 = await CreateTestPosting(config.Id);
        await _service.AddToQueueAsync(posting1.Id);
        await _service.AddToQueueAsync(posting2.Id);

        // Act
        var status = await _service.GetQueueStatusAsync();

        // Assert
        status.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessQueueAsync_ShouldProcessItems()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");
        var posting = await CreateTestPosting(config.Id);
        await _service.AddToQueueAsync(posting.Id);

        // Act
        var result = await _service.ProcessQueueAsync();

        // Assert
        result.ProcessedCount.Should().Be(1);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateErrorMappingAsync_ShouldCreateMapping()
    {
        // Arrange
        var mapping = new PMSErrorMapping
        {
            PMSType = PMSType.Opera,
            ErrorCode = "ERR001",
            FriendlyMessage = "Guest not found",
            IsRetryable = false,
            Severity = 2,
            IsActive = true
        };

        // Act
        var result = await _service.CreateErrorMappingAsync(mapping);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFriendlyErrorMessageAsync_ShouldReturnMessage()
    {
        // Arrange
        await _service.CreateErrorMappingAsync(new PMSErrorMapping
        {
            PMSType = PMSType.Opera,
            ErrorCode = "ERR001",
            FriendlyMessage = "Guest not found in room",
            IsActive = true
        });

        // Act
        var message = await _service.GetFriendlyErrorMessageAsync("ERR001", PMSType.Opera);

        // Assert
        message.Should().Be("Guest not found in room");
    }

    [Fact]
    public async Task LogActivityAsync_ShouldCreateLog()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var log = new PMSActivityLog
        {
            PMSConfigurationId = config.Id,
            ActivityType = "Test",
            Description = "Test activity",
            IsSuccess = true,
            IsActive = true
        };

        // Act
        await _service.LogActivityAsync(log);
        var logs = await _service.GetActivityLogsAsync(config.Id);

        // Assert
        logs.Should().HaveCount(1);
    }

    #endregion

    #region Reports Tests

    [Fact]
    public async Task GetPostingSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        await SetupTestGuestCache(config.Id, "101");
        await _service.PostRoomChargeAsync(new RoomChargeRequest
        {
            ConfigId = config.Id,
            RoomNumber = "101",
            GuestName = "John Doe",
            ChargeType = RoomChargeType.FoodAndBeverage,
            Amount = 5000m,
            UserId = 1
        });

        // Act
        var report = await _service.GetPostingSummaryAsync(
            config.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1));

        // Assert
        report.Should().NotBeNull();
        report.TotalPostings.Should().Be(1);
    }

    [Fact]
    public async Task GetFailedPostingsReportAsync_ShouldReturnFailedPostings()
    {
        // Arrange
        var config = await CreateTestPMSConfiguration(true);
        var posting = new RoomChargePosting
        {
            PMSConfigurationId = config.Id,
            PostingReference = "FAIL-001",
            RoomNumber = "101",
            GuestName = "John Doe",
            Amount = 5000m,
            TotalAmount = 5000m,
            Status = PostingStatus.Failed,
            ErrorMessage = "Connection timeout",
            AttemptCount = 3,
            IsActive = true
        };
        _context.RoomChargePostings.Add(posting);
        await _context.SaveChangesAsync();

        // Act
        var report = await _service.GetFailedPostingsReportAsync(config.Id);

        // Assert
        report.Should().HaveCount(1);
        report.First().ErrorMessage.Should().Be("Connection timeout");
    }

    #endregion

    #region Helper Methods

    private async Task<PMSConfiguration> CreateTestPMSConfiguration(bool isDefault)
    {
        var config = new PMSConfiguration
        {
            Name = $"Test Config {Guid.NewGuid().ToString()[..6]}",
            PMSType = PMSType.Opera,
            PropertyCode = "TEST001",
            ApiEndpoint = "https://test.example.com/api",
            IsDefault = isDefault,
            Status = PMSConnectionStatus.Connected,
            IsActive = true
        };

        return await _service.CreateConfigurationAsync(config);
    }

    private async Task<RoomChargePosting> CreateTestPosting(int configId)
    {
        var posting = new RoomChargePosting
        {
            PMSConfigurationId = configId,
            PostingReference = $"TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            RoomNumber = "101",
            GuestName = "John Doe",
            ChargeType = RoomChargeType.FoodAndBeverage,
            Amount = 5000m,
            TotalAmount = 5000m,
            Status = PostingStatus.Retry,
            IsActive = true
        };

        _context.RoomChargePostings.Add(posting);
        await _context.SaveChangesAsync();
        return posting;
    }

    private async Task SetupTestGuestCache(int configId, string roomNumber, decimal creditLimit = 100000m, decimal currentBalance = 15000m)
    {
        var guest = new PMSGuestLookup
        {
            PMSConfigurationId = configId,
            RoomNumber = roomNumber,
            FirstName = "John",
            LastName = "Doe",
            FolioNumber = $"F{roomNumber}",
            Status = GuestStatus.InHouse,
            CheckInDate = DateTime.UtcNow.AddDays(-1),
            CheckOutDate = DateTime.UtcNow.AddDays(3),
            CreditLimit = creditLimit,
            CurrentBalance = currentBalance,
            AllowRoomCharges = true,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsActive = true
        };

        _context.PMSGuestLookups.Add(guest);
        await _context.SaveChangesAsync();
    }

    #endregion
}
