using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing in-app notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Event raised when a new notification is received.
    /// </summary>
    event EventHandler<Notification>? NotificationReceived;

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for a specific user.
    /// </summary>
    Task<Notification> NotifyUserAsync(
        int userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for users with a specific role.
    /// </summary>
    Task<Notification> NotifyRoleAsync(
        int roleId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for all managers.
    /// </summary>
    Task<Notification> NotifyManagersAsync(
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a user.
    /// </summary>
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications for a user (paginated).
    /// </summary>
    Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a notification.
    /// </summary>
    Task DismissNotificationAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old notifications (cleanup).
    /// </summary>
    Task<int> DeleteOldNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a low stock notification.
    /// </summary>
    Task NotifyLowStockAsync(int productId, string productName, decimal currentStock, decimal reorderPoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a PO generated notification.
    /// </summary>
    Task NotifyPOGeneratedAsync(int purchaseOrderId, string poNumber, decimal totalAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a PO pending approval notification.
    /// </summary>
    Task NotifyPOPendingApprovalAsync(int purchaseOrderId, string poNumber, decimal totalAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a PO overdue notification.
    /// </summary>
    Task NotifyPOOverdueAsync(int purchaseOrderId, string poNumber, DateTime expectedDate, CancellationToken cancellationToken = default);
}
