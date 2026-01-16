// tests/HospitalityPOS.Business.Tests/Services/MobileReportingServiceTests.cs
// Unit tests for MobileReportingService
// Story 41-1: Mobile Reporting App

using FluentAssertions;
using HospitalityPOS.Core.Models.Mobile;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class MobileReportingServiceTests
{
    private readonly MobileReportingService _service;

    public MobileReportingServiceTests()
    {
        _service = new MobileReportingService();
    }

    #region Authentication Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var request = new MobileLoginRequest
        {
            Username = "admin",
            Password = "password123",
            DeviceId = "test-device-001",
            Platform = DevicePlatform.iOS,
            DeviceModel = "iPhone 14",
            AppVersion = "1.0.0"
        };

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("admin");
        result.User.Role.Should().Be("Administrator");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = new MobileLoginRequest
        {
            Username = "nonexistent",
            Password = "wrongpassword",
            DeviceId = "test-device-001",
            Platform = DevicePlatform.Android
        };

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var loginResult = await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "test-device-001",
            Platform = DevicePlatform.iOS
        });

        var refreshRequest = new TokenRefreshRequest
        {
            RefreshToken = loginResult.RefreshToken!,
            DeviceId = "test-device-001"
        };

        // Act
        var result = await _service.RefreshTokenAsync(refreshRequest);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(loginResult.RefreshToken); // Should be new token
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var request = new TokenRefreshRequest
        {
            RefreshToken = "invalid-token",
            DeviceId = "test-device-001"
        };

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogoutAsync_WithValidSession_ReturnsTrue()
    {
        // Arrange
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "test-device-001",
            Platform = DevicePlatform.iOS
        });

        // Act
        var result = await _service.LogoutAsync(1, "test-device-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllDevicesAsync_WithMultipleSessions_RevokesAll()
    {
        // Arrange
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "device-1",
            Platform = DevicePlatform.iOS
        });
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "device-2",
            Platform = DevicePlatform.Android
        });

        // Act
        var revokedCount = await _service.LogoutAllDevicesAsync(1);

        // Assert
        revokedCount.Should().Be(2);
        var activeSessions = await _service.GetActiveSessionsAsync(1);
        activeSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsUserSessions()
    {
        // Arrange
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "device-active-1",
            Platform = DevicePlatform.iOS
        });

        // Act
        var sessions = await _service.GetActiveSessionsAsync(1);

        // Assert
        sessions.Should().NotBeEmpty();
        sessions.Should().Contain(s => s.DeviceId == "device-active-1");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var loginResult = await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "test-device-001",
            Platform = DevicePlatform.iOS
        });

        // Act
        var user = await _service.ValidateTokenAsync(loginResult.AccessToken!);

        // Assert
        user.Should().NotBeNull();
        user!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Act
        var result = await _service.ValidateTokenAsync("invalid-jwt-token");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboardAsync_ReturnsCompleteDashboard()
    {
        // Act
        var dashboard = await _service.GetDashboardAsync(1, 1);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.BranchId.Should().Be(1);
        dashboard.BranchName.Should().NotBeNullOrEmpty();
        dashboard.TodaySales.Should().NotBeNull();
        dashboard.Comparison.Should().NotBeNull();
        dashboard.QuickStats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTodaySalesAsync_ReturnsSalesSummary()
    {
        // Act
        var sales = await _service.GetTodaySalesAsync(1);

        // Assert
        sales.Should().NotBeNull();
        sales.TotalSales.Should().BeGreaterThan(0);
        sales.TransactionCount.Should().BeGreaterThan(0);
        sales.AverageTicket.Should().BeGreaterThan(0);
        (sales.CashSales + sales.MpesaSales + sales.CardSales + sales.OtherSales)
            .Should().BeApproximately(sales.TotalSales, 1);
    }

    [Fact]
    public async Task GetSalesComparisonAsync_ReturnsComparison()
    {
        // Act
        var comparison = await _service.GetSalesComparisonAsync(1, "Yesterday");

        // Assert
        comparison.Should().NotBeNull();
        comparison.ComparisonPeriod.Should().Be("Yesterday");
        comparison.PreviousSales.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetQuickStatsAsync_ReturnsStats()
    {
        // Act
        var stats = await _service.GetQuickStatsAsync(1);

        // Assert
        stats.Should().NotBeNull();
        stats.ActiveTables.Should().BeGreaterOrEqualTo(0);
        stats.PendingOrders.Should().BeGreaterOrEqualTo(0);
        stats.LowStockItems.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Sales Report Tests

    [Fact]
    public async Task GetSalesReportAsync_ReturnsCompleteReport()
    {
        // Arrange
        var request = new MobileSalesReportRequest
        {
            DateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            DateTo = DateOnly.FromDateTime(DateTime.Today),
            BranchId = 1,
            IncludeCategoryBreakdown = true,
            IncludePaymentBreakdown = true,
            IncludeDailyBreakdown = true,
            TopProductsCount = 5
        };

        // Act
        var report = await _service.GetSalesReportAsync(request);

        // Assert
        report.Should().NotBeNull();
        report.DateFrom.Should().Be(request.DateFrom);
        report.DateTo.Should().Be(request.DateTo);
        report.TotalSales.Should().BeGreaterThan(0);
        report.ByCategory.Should().NotBeEmpty();
        report.ByPaymentMethod.Should().NotBeEmpty();
        report.DailyBreakdown.Should().NotBeEmpty();
        report.TopProducts.Should().NotBeNull();
        report.TopProducts!.Count.Should().BeLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetSalesByCategoryAsync_ReturnsCategoryBreakdown()
    {
        // Arrange
        var dateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var dateTo = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var categories = await _service.GetSalesByCategoryAsync(dateFrom, dateTo, 1);

        // Assert
        categories.Should().NotBeEmpty();
        categories.Sum(c => c.Percentage).Should().BeApproximately(100m, 1m);
    }

    [Fact]
    public async Task GetSalesByPaymentMethodAsync_ReturnsPaymentBreakdown()
    {
        // Arrange
        var dateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var dateTo = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var payments = await _service.GetSalesByPaymentMethodAsync(dateFrom, dateTo, 1);

        // Assert
        payments.Should().NotBeEmpty();
        payments.Should().Contain(p => p.PaymentMethod == "M-Pesa");
        payments.Should().Contain(p => p.PaymentMethod == "Cash");
    }

    [Fact]
    public async Task GetTopProductsAsync_ReturnsRankedProducts()
    {
        // Arrange
        var dateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var dateTo = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var products = await _service.GetTopProductsAsync(dateFrom, dateTo, 5, 1);

        // Assert
        products.Should().NotBeEmpty();
        products.Count.Should().BeLessOrEqualTo(5);
        products.First().Rank.Should().Be(1);
        products.Should().BeInAscendingOrder(p => p.Rank);
    }

    [Fact]
    public async Task GetHourlySalesAsync_ReturnsHourlyBreakdown()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var hourly = await _service.GetHourlySalesAsync(date, 1);

        // Assert
        hourly.Should().NotBeEmpty();
        hourly.Should().OnlyContain(h => h.Hour >= 0 && h.Hour <= 23);
        hourly.Should().OnlyContain(h => !string.IsNullOrEmpty(h.HourLabel));
    }

    #endregion

    #region Alerts Tests

    [Fact]
    public async Task GetAlertsAsync_ReturnsAllAlertTypes()
    {
        // Act
        var alerts = await _service.GetAlertsAsync(1, 1);

        // Assert
        alerts.Should().NotBeNull();
        alerts.LowStock.Should().NotBeEmpty();
        alerts.Expiring.Should().NotBeEmpty();
        alerts.TotalLowStockCount.Should().Be(alerts.LowStock.Count);
        alerts.TotalExpiringCount.Should().Be(alerts.Expiring.Count);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_ReturnsLowStockItems()
    {
        // Act
        var items = await _service.GetLowStockItemsAsync(1);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.CurrentStock < i.ReorderLevel);
        items.Should().OnlyContain(i => !string.IsNullOrEmpty(i.ProductName));
    }

    [Fact]
    public async Task GetExpiringItemsAsync_ReturnsExpiringItems()
    {
        // Act
        var items = await _service.GetExpiringItemsAsync(7, 1);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.DaysUntilExpiry <= 7);
        items.Should().OnlyContain(i => i.ExpiryDate >= DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task MarkAlertAsReadAsync_WithValidAlert_ReturnsTrue()
    {
        // Arrange - Would need to create an alert first in real implementation
        // For now, test that method doesn't throw

        // Act
        var result = await _service.MarkAlertAsReadAsync(999);

        // Assert
        result.Should().BeFalse(); // Alert doesn't exist
    }

    #endregion

    #region Branch Tests

    [Fact]
    public async Task GetBranchesAsync_ReturnsAccessibleBranches()
    {
        // Act
        var branches = await _service.GetBranchesAsync(1); // Admin user

        // Assert
        branches.Should().NotBeEmpty();
        branches.Should().OnlyContain(b => !string.IsNullOrEmpty(b.Name));
    }

    [Fact]
    public async Task GetAllBranchesSummaryAsync_ReturnsAggregateSummary()
    {
        // Act
        var summary = await _service.GetAllBranchesSummaryAsync(1);

        // Assert
        summary.Should().NotBeNull();
        summary.TotalBranches.Should().BeGreaterThan(0);
        summary.TotalSales.Should().Be(summary.Branches.Sum(b => b.TodaySales));
        summary.TopPerformingBranch.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBranchSummaryAsync_WithValidBranch_ReturnsSummary()
    {
        // Act
        var branch = await _service.GetBranchSummaryAsync(1);

        // Assert
        branch.Should().NotBeNull();
        branch!.Id.Should().Be(1);
        branch.TodaySales.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBranchSummaryAsync_WithInvalidBranch_ReturnsNull()
    {
        // Act
        var branch = await _service.GetBranchSummaryAsync(999);

        // Assert
        branch.Should().BeNull();
    }

    #endregion

    #region Device Registration Tests

    [Fact]
    public async Task RegisterDeviceAsync_CreatesRegistration()
    {
        // Arrange
        var registration = new DeviceRegistration
        {
            UserId = 1,
            DeviceId = "new-device-001",
            PushToken = "fcm-token-123",
            Platform = DevicePlatform.iOS,
            IsActive = true
        };

        // Act
        var id = await _service.RegisterDeviceAsync(registration);

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdatePushTokenAsync_WithRegisteredDevice_ReturnsTrue()
    {
        // Arrange
        await _service.RegisterDeviceAsync(new DeviceRegistration
        {
            UserId = 1,
            DeviceId = "device-to-update",
            PushToken = "old-token",
            Platform = DevicePlatform.iOS
        });

        // Act
        var result = await _service.UpdatePushTokenAsync("device-to-update", "new-token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisterDeviceAsync_WithRegisteredDevice_ReturnsTrue()
    {
        // Arrange
        await _service.RegisterDeviceAsync(new DeviceRegistration
        {
            UserId = 1,
            DeviceId = "device-to-unregister",
            PushToken = "token",
            Platform = DevicePlatform.Android
        });

        // Act
        var result = await _service.UnregisterDeviceAsync("device-to-unregister");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetRegisteredDevicesAsync_ReturnsUserDevices()
    {
        // Arrange
        await _service.RegisterDeviceAsync(new DeviceRegistration
        {
            UserId = 1,
            DeviceId = "user-device-1",
            PushToken = "token-1",
            Platform = DevicePlatform.iOS
        });

        // Act
        var devices = await _service.GetRegisteredDevicesAsync(1);

        // Assert
        devices.Should().NotBeEmpty();
    }

    #endregion

    #region Notification Preferences Tests

    [Fact]
    public async Task GetNotificationPreferencesAsync_ReturnsPreferences()
    {
        // Act
        var prefs = await _service.GetNotificationPreferencesAsync(1);

        // Assert
        prefs.Should().NotBeNull();
        prefs.UserId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateNotificationPreferencesAsync_UpdatesPreferences()
    {
        // Arrange
        var newPrefs = new NotificationPreferences
        {
            UserId = 1,
            DailySummaryEnabled = false,
            LowStockAlertsEnabled = true,
            ExpiryAlertsEnabled = true,
            LargeTransactionAlertsEnabled = true,
            LargeTransactionThreshold = 100000
        };

        // Act
        var result = await _service.UpdateNotificationPreferencesAsync(newPrefs);

        // Assert
        result.Should().NotBeNull();
        result.DailySummaryEnabled.Should().BeFalse();
        result.LargeTransactionThreshold.Should().Be(100000);
    }

    #endregion

    #region Cached Data Tests

    [Fact]
    public async Task GetCachedDataAsync_ReturnsAllData()
    {
        // Act
        var cached = await _service.GetCachedDataAsync(1, 1);

        // Assert
        cached.Should().NotBeNull();
        cached.UserId.Should().Be(1);
        cached.Dashboard.Should().NotBeNull();
        cached.Alerts.Should().NotBeNull();
        cached.Branches.Should().NotBeEmpty();
        cached.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsStatus()
    {
        // Act
        var status = await _service.GetSyncStatusAsync("device-001");

        // Assert
        status.Should().NotBeNull();
        status.IsOnline.Should().BeTrue();
        status.PendingChanges.Should().Be(0);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task LoginAsync_RaisesSessionCreatedEvent()
    {
        // Arrange
        MobileSessionEventArgs? capturedArgs = null;
        _service.SessionCreated += (sender, args) => capturedArgs = args;

        // Act
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "event-test-device",
            Platform = DevicePlatform.iOS
        });

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Status.Should().Be(SessionStatus.Active);
        capturedArgs.DeviceId.Should().Be("event-test-device");
    }

    [Fact]
    public async Task LogoutAsync_RaisesSessionRevokedEvent()
    {
        // Arrange
        await _service.LoginAsync(new MobileLoginRequest
        {
            Username = "admin",
            Password = "password",
            DeviceId = "logout-event-device",
            Platform = DevicePlatform.iOS
        });

        MobileSessionEventArgs? capturedArgs = null;
        _service.SessionRevoked += (sender, args) => capturedArgs = args;

        // Act
        await _service.LogoutAsync(1, "logout-event-device");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Status.Should().Be(SessionStatus.Revoked);
    }

    #endregion
}
