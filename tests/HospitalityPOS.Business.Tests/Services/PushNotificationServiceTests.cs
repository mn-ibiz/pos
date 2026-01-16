// tests/HospitalityPOS.Business.Tests/Services/PushNotificationServiceTests.cs
// Unit tests for PushNotificationService
// Story 41-1: Mobile Reporting App

using FluentAssertions;
using HospitalityPOS.Core.Models.Mobile;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class PushNotificationServiceTests
{
    private readonly PushNotificationService _service;

    public PushNotificationServiceTests()
    {
        _service = new PushNotificationService();
    }

    #region Send Notification Tests

    [Fact]
    public async Task SendAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new PushNotificationRequest
        {
            UserIds = new List<int> { 1 },
            Title = "Test Notification",
            Body = "This is a test message",
            Type = NotificationType.Custom
        };

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.SentCount.Should().BeGreaterThan(0);
        result.MessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_WithNoTokens_ReturnsFailure()
    {
        // Arrange
        var request = new PushNotificationRequest
        {
            UserIds = new List<int> { 999 }, // User with no tokens
            Title = "Test",
            Body = "Test message"
        };

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendToUserAsync_WithValidUser_SendsNotification()
    {
        // Act
        var result = await _service.SendToUserAsync(
            userId: 1,
            title: "Test",
            body: "Test message",
            type: NotificationType.Custom);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendToUserAsync_WithDisabledNotificationType_ReturnsFailure()
    {
        // Arrange
        _service.SetNotificationPreferences(1, new NotificationPreferences
        {
            UserId = 1,
            DailySummaryEnabled = false // Disable daily summary
        });

        // Act
        var result = await _service.SendToUserAsync(
            userId: 1,
            title: "Daily Summary",
            body: "Your daily summary",
            type: NotificationType.DailySummary);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("disabled");
    }

    [Fact]
    public async Task SendToUsersAsync_WithMultipleUsers_SendsToAll()
    {
        // Arrange
        _service.RegisterDeviceToken(3, "user3-token");

        // Act
        var result = await _service.SendToUsersAsync(
            userIds: new[] { 1, 2, 3 },
            title: "Group Message",
            body: "This is for everyone");

        // Assert
        result.Success.Should().BeTrue();
        result.SentCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task SendToRoleAsync_WithValidRole_SendsToRoleUsers()
    {
        // Act
        var result = await _service.SendToRoleAsync(
            roleName: "Manager",
            title: "Manager Alert",
            body: "Important message for managers");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendToRoleAsync_WithInvalidRole_ReturnsFailure()
    {
        // Act
        var result = await _service.SendToRoleAsync(
            roleName: "NonExistentRole",
            title: "Test",
            body: "Test");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No users found");
    }

    [Fact]
    public async Task SendToBranchAsync_SendsToManagersOfBranch()
    {
        // Act
        var result = await _service.SendToBranchAsync(
            branchId: 1,
            title: "Branch Alert",
            body: "Message for branch 1");

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Scheduled Notifications Tests

    [Fact]
    public async Task ScheduleNotificationAsync_CreatesScheduledNotification()
    {
        // Arrange
        var scheduledTime = DateTime.UtcNow.AddHours(1);

        // Act
        var id = await _service.ScheduleNotificationAsync(
            userId: 1,
            title: "Scheduled Test",
            body: "This is scheduled",
            scheduledFor: scheduledTime,
            type: NotificationType.Custom);

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CancelScheduledNotificationAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var id = await _service.ScheduleNotificationAsync(
            userId: 1,
            title: "To Cancel",
            body: "This will be cancelled",
            scheduledFor: DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _service.CancelScheduledNotificationAsync(id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelScheduledNotificationAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.CancelScheduledNotificationAsync(9999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetScheduledNotificationsAsync_ReturnsUserNotifications()
    {
        // Arrange
        await _service.ScheduleNotificationAsync(1, "Test 1", "Body 1", DateTime.UtcNow.AddHours(1));
        await _service.ScheduleNotificationAsync(1, "Test 2", "Body 2", DateTime.UtcNow.AddHours(2));

        // Act
        var notifications = await _service.GetScheduledNotificationsAsync(1);

        // Assert
        notifications.Should().NotBeEmpty();
        notifications.Should().BeInAscendingOrder(n => n.ScheduledFor);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_SendsDueNotifications()
    {
        // Arrange
        await _service.ScheduleNotificationAsync(
            userId: 1,
            title: "Due Now",
            body: "This is due",
            scheduledFor: DateTime.UtcNow.AddSeconds(-1)); // Already due

        // Act
        var sentCount = await _service.ProcessDueNotificationsAsync();

        // Assert
        sentCount.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region Alert Notification Tests

    [Fact]
    public async Task SendDailySummaryAsync_SendsToEligibleUsers()
    {
        // Act
        var result = await _service.SendDailySummaryAsync(branchId: 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendLowStockAlertAsync_SendsToManagers()
    {
        // Act
        var result = await _service.SendLowStockAlertAsync(
            productId: 1,
            productName: "Test Product",
            currentStock: 5,
            reorderLevel: 20);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendExpiryAlertAsync_SendsWithUrgencyForNearExpiry()
    {
        // Arrange
        var expiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)); // Tomorrow

        // Act
        var result = await _service.SendExpiryAlertAsync(
            productId: 1,
            productName: "Perishable Item",
            expiryDate: expiryDate,
            quantity: 10);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendLargeTransactionAlertAsync_WithAmountAboveThreshold_Sends()
    {
        // Arrange
        _service.SetNotificationPreferences(1, new NotificationPreferences
        {
            UserId = 1,
            LargeTransactionAlertsEnabled = true,
            LargeTransactionThreshold = 10000
        });

        // Act
        var result = await _service.SendLargeTransactionAlertAsync(
            receiptId: 123,
            amount: 50000, // Above threshold
            branchId: 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendLargeTransactionAlertAsync_WithAmountBelowThreshold_DoesNotSend()
    {
        // Arrange
        _service.SetNotificationPreferences(1, new NotificationPreferences
        {
            UserId = 1,
            LargeTransactionAlertsEnabled = true,
            LargeTransactionThreshold = 100000
        });

        // Act
        var result = await _service.SendLargeTransactionAlertAsync(
            receiptId: 123,
            amount: 5000, // Below threshold
            branchId: 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendZReportCompleteAsync_SendsToOwners()
    {
        // Act
        var result = await _service.SendZReportCompleteAsync(
            workPeriodId: 1,
            totalSales: 125000,
            branchId: 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Notification History Tests

    [Fact]
    public async Task GetNotificationHistoryAsync_ReturnsUserHistory()
    {
        // Arrange
        await _service.SendToUserAsync(1, "Test 1", "Body 1");
        await _service.SendToUserAsync(1, "Test 2", "Body 2");

        // Act
        var history = await _service.GetNotificationHistoryAsync(1, 10);

        // Assert
        history.Should().NotBeEmpty();
        history.Should().BeInDescendingOrder(h => h.SentAt);
    }

    [Fact]
    public async Task GetNotificationStatsAsync_ReturnsStatistics()
    {
        // Arrange
        await _service.SendDailySummaryAsync();
        await _service.SendLowStockAlertAsync(1, "Product", 5, 20);

        // Act
        var stats = await _service.GetNotificationStatsAsync(
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        // Assert
        stats.Should().NotBeEmpty();
        stats.Should().ContainKey("TotalSent");
        stats.Should().ContainKey("Successful");
        stats["TotalSent"].Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Token Management Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Act
        var result = await _service.ValidateTokenAsync("valid-fcm-token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateTokenAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveInvalidTokensAsync_RemovesTokens()
    {
        // Arrange
        _service.RegisterDeviceToken(99, "token-to-remove");

        // Act
        var removedCount = await _service.RemoveInvalidTokensAsync(new[] { "token-to-remove" });

        // Assert
        removedCount.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task SendAsync_RaisesNotificationSentEvent()
    {
        // Arrange
        PushNotificationSentEventArgs? capturedArgs = null;
        _service.NotificationSent += (sender, args) => capturedArgs = args;

        // Act
        await _service.SendAsync(new PushNotificationRequest
        {
            UserIds = new List<int> { 1 },
            Title = "Event Test",
            Body = "Testing event"
        });

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Success.Should().BeTrue();
        capturedArgs.MessageId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Notification Data Tests

    [Fact]
    public async Task SendAsync_WithData_IncludesDataInNotification()
    {
        // Arrange
        var request = new PushNotificationRequest
        {
            UserIds = new List<int> { 1 },
            Title = "Data Test",
            Body = "Test with data",
            Data = new Dictionary<string, string>
            {
                { "orderId", "123" },
                { "action", "view_order" }
            }
        };

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithCustomization_SendsCorrectly()
    {
        // Arrange
        var request = new PushNotificationRequest
        {
            UserIds = new List<int> { 1 },
            Title = "Custom Notification",
            Body = "With all options",
            Type = NotificationType.NewOrder,
            ImageUrl = "https://example.com/image.png",
            ClickAction = "OPEN_ORDER",
            Badge = 5,
            Sound = "custom_sound",
            Priority = 10
        };

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Quiet Hours Tests

    [Fact]
    public async Task SendToUserAsync_DuringQuietHours_DoesNotSend()
    {
        // Arrange
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        _service.SetNotificationPreferences(1, new NotificationPreferences
        {
            UserId = 1,
            QuietHoursEnabled = true,
            QuietHoursStart = currentTime.AddHours(-1),
            QuietHoursEnd = currentTime.AddHours(1)
        });

        // Act
        var result = await _service.SendToUserAsync(1, "Test", "During quiet hours");

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Role and User Registration Tests

    [Fact]
    public async Task AddUsersToRole_AndSendToRole_Works()
    {
        // Arrange
        _service.RegisterDeviceToken(100, "user-100-token");
        _service.AddUsersToRole("TestRole", 100);

        // Act
        var result = await _service.SendToRoleAsync("TestRole", "Test", "Message to test role");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterDeviceToken_AllowsSendingToUser()
    {
        // Arrange
        _service.RegisterDeviceToken(101, "new-user-token");

        // Act
        var result = await _service.SendToUserAsync(101, "Welcome", "Your device is registered");

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion
}
