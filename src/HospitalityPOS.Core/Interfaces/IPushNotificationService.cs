// src/HospitalityPOS.Core/Interfaces/IPushNotificationService.cs
// Service interface for push notifications
// Story 41-1: Mobile Reporting App

using HospitalityPOS.Core.Models.Mobile;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for push notifications via Firebase Cloud Messaging.
/// </summary>
public interface IPushNotificationService
{
    #region Send Notifications

    /// <summary>
    /// Sends a push notification to specific users.
    /// </summary>
    /// <param name="request">Notification request.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendAsync(PushNotificationRequest request);

    /// <summary>
    /// Sends notification to all devices of a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="data">Optional data payload.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendToUserAsync(
        int userId, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends notification to multiple users.
    /// </summary>
    /// <param name="userIds">User IDs.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="data">Optional data payload.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendToUsersAsync(
        IEnumerable<int> userIds, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends notification to users with a specific role.
    /// </summary>
    /// <param name="roleName">Role name.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="data">Optional data payload.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendToRoleAsync(
        string roleName, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends notification to users of a specific branch.
    /// </summary>
    /// <param name="branchId">Branch ID.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="data">Optional data payload.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendToBranchAsync(
        int branchId, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null);

    #endregion

    #region Scheduled Notifications

    /// <summary>
    /// Schedules a notification for later.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="scheduledFor">Scheduled time.</param>
    /// <param name="type">Notification type.</param>
    /// <returns>Scheduled notification ID.</returns>
    Task<int> ScheduleNotificationAsync(
        int userId, string title, string body, DateTime scheduledFor,
        NotificationType type = NotificationType.Custom);

    /// <summary>
    /// Cancels a scheduled notification.
    /// </summary>
    /// <param name="notificationId">Notification ID.</param>
    /// <returns>True if cancelled.</returns>
    Task<bool> CancelScheduledNotificationAsync(int notificationId);

    /// <summary>
    /// Gets scheduled notifications for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Scheduled notifications.</returns>
    Task<IReadOnlyList<ScheduledNotification>> GetScheduledNotificationsAsync(int userId);

    /// <summary>
    /// Processes due scheduled notifications.
    /// </summary>
    /// <returns>Number of notifications sent.</returns>
    Task<int> ProcessDueNotificationsAsync();

    #endregion

    #region Alert Notifications

    /// <summary>
    /// Sends daily sales summary to configured users.
    /// </summary>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendDailySummaryAsync(int? branchId = null);

    /// <summary>
    /// Sends low stock alert.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="productName">Product name.</param>
    /// <param name="currentStock">Current stock level.</param>
    /// <param name="reorderLevel">Reorder level.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendLowStockAlertAsync(
        int productId, string productName, decimal currentStock, decimal reorderLevel);

    /// <summary>
    /// Sends expiry alert.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="productName">Product name.</param>
    /// <param name="expiryDate">Expiry date.</param>
    /// <param name="quantity">Quantity expiring.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendExpiryAlertAsync(
        int productId, string productName, DateOnly expiryDate, decimal quantity);

    /// <summary>
    /// Sends large transaction alert.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="amount">Transaction amount.</param>
    /// <param name="branchId">Branch ID.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendLargeTransactionAlertAsync(
        int receiptId, decimal amount, int branchId);

    /// <summary>
    /// Sends Z-Report completion notification.
    /// </summary>
    /// <param name="workPeriodId">Work period ID.</param>
    /// <param name="totalSales">Total sales.</param>
    /// <param name="branchId">Branch ID.</param>
    /// <returns>Send result.</returns>
    Task<PushNotificationResult> SendZReportCompleteAsync(
        int workPeriodId, decimal totalSales, int branchId);

    #endregion

    #region Notification History

    /// <summary>
    /// Gets notification history for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Maximum entries to return.</param>
    /// <returns>Notification logs.</returns>
    Task<IReadOnlyList<NotificationLog>> GetNotificationHistoryAsync(int userId, int limit = 50);

    /// <summary>
    /// Gets notification statistics.
    /// </summary>
    /// <param name="dateFrom">Start date.</param>
    /// <param name="dateTo">End date.</param>
    /// <returns>Statistics dictionary.</returns>
    Task<Dictionary<string, int>> GetNotificationStatsAsync(DateOnly dateFrom, DateOnly dateTo);

    #endregion

    #region Token Management

    /// <summary>
    /// Validates a device push token.
    /// </summary>
    /// <param name="token">Push token.</param>
    /// <returns>True if valid.</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Removes invalid tokens from the system.
    /// </summary>
    /// <param name="tokens">Tokens to remove.</param>
    /// <returns>Number removed.</returns>
    Task<int> RemoveInvalidTokensAsync(IEnumerable<string> tokens);

    #endregion

    #region Events

    /// <summary>Raised when a notification is sent.</summary>
    event EventHandler<PushNotificationSentEventArgs>? NotificationSent;

    #endregion
}
