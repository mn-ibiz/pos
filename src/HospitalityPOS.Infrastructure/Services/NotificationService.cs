using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing in-app notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly POSDbContext _context;
    private const string ManagerRoleName = "Manager";

    /// <inheritdoc />
    public event EventHandler<Notification>? NotificationReceived;

    public NotificationService(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.CreatedAt = DateTime.UtcNow;
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Raise event for real-time updates
        NotificationReceived?.Invoke(this, notification);

        return notification;
    }

    /// <inheritdoc />
    public async Task<Notification> NotifyUserAsync(
        int userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Priority = priority,
            ActionType = actionType,
            ActionParameter = actionParameter
        };

        return await CreateNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Notification> NotifyRoleAsync(
        int roleId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            RoleId = roleId,
            Title = title,
            Message = message,
            Type = type,
            Priority = priority,
            ActionType = actionType,
            ActionParameter = actionParameter
        };

        return await CreateNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Notification> NotifyManagersAsync(
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionType = null,
        string? actionParameter = null,
        CancellationToken cancellationToken = default)
    {
        // Find manager role
        var managerRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == ManagerRoleName || r.Name == "Admin", cancellationToken)
            .ConfigureAwait(false);

        if (managerRole == null)
        {
            // Create a notification for all users if no manager role exists
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                ActionType = actionType,
                ActionParameter = actionParameter
            };
            return await CreateNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        return await NotifyRoleAsync(
            managerRole.Id,
            title,
            message,
            type,
            priority,
            actionType,
            actionParameter,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId, int? limit = null, CancellationToken cancellationToken = default)
    {
        // Get user's role IDs
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var query = _context.Notifications
            .Where(n => !n.IsRead && !n.IsDeleted && !n.IsDismissed)
            .Where(n =>
                n.UserId == userId ||
                (n.UserId == null && n.RoleId == null) ||
                (n.UserId == null && n.RoleId != null && userRoleIds.Contains(n.RoleId.Value)))
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        // Get user's role IDs
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _context.Notifications
            .Where(n => !n.IsDeleted && !n.IsDismissed)
            .Where(n =>
                n.UserId == userId ||
                (n.UserId == null && n.RoleId == null) ||
                (n.UserId == null && n.RoleId != null && userRoleIds.Contains(n.RoleId.Value)))
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get user's role IDs
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _context.Notifications
            .Where(n => !n.IsRead && !n.IsDeleted && !n.IsDismissed)
            .Where(n =>
                n.UserId == userId ||
                (n.UserId == null && n.RoleId == null) ||
                (n.UserId == null && n.RoleId != null && userRoleIds.Contains(n.RoleId.Value)))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get user's role IDs
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var unreadNotifications = await _context.Notifications
            .Where(n => !n.IsRead && !n.IsDeleted && !n.IsDismissed)
            .Where(n =>
                n.UserId == userId ||
                (n.UserId == null && n.RoleId == null) ||
                (n.UserId == null && n.RoleId != null && userRoleIds.Contains(n.RoleId.Value)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DismissNotificationAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (notification != null)
        {
            notification.IsDismissed = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var notification in oldNotifications)
        {
            notification.IsDeleted = true;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return oldNotifications.Count;
    }

    /// <inheritdoc />
    public async Task NotifyLowStockAsync(int productId, string productName, decimal currentStock, decimal reorderPoint, CancellationToken cancellationToken = default)
    {
        await NotifyManagersAsync(
            "Low Stock Alert",
            $"Product '{productName}' is running low. Current stock: {currentStock:N0}, Reorder point: {reorderPoint:N0}",
            NotificationType.LowStock,
            NotificationPriority.High,
            "Inventory",
            productId.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task NotifyPOGeneratedAsync(int purchaseOrderId, string poNumber, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        await NotifyManagersAsync(
            "Purchase Order Generated",
            $"Purchase order {poNumber} has been auto-generated with a total value of {totalAmount:C}",
            NotificationType.POGenerated,
            NotificationPriority.Normal,
            "PurchaseOrders",
            purchaseOrderId.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task NotifyPOPendingApprovalAsync(int purchaseOrderId, string poNumber, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        await NotifyManagersAsync(
            "PO Pending Approval",
            $"Purchase order {poNumber} ({totalAmount:C}) requires your approval",
            NotificationType.POPendingApproval,
            NotificationPriority.High,
            "PurchaseOrderReview",
            purchaseOrderId.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task NotifyPOOverdueAsync(int purchaseOrderId, string poNumber, DateTime expectedDate, CancellationToken cancellationToken = default)
    {
        var daysOverdue = (DateTime.Today - expectedDate.Date).Days;
        await NotifyManagersAsync(
            "Overdue Purchase Order",
            $"Purchase order {poNumber} is {daysOverdue} day(s) overdue (expected: {expectedDate:yyyy-MM-dd})",
            NotificationType.POOverdue,
            NotificationPriority.Urgent,
            "PurchaseOrders",
            purchaseOrderId.ToString(),
            cancellationToken).ConfigureAwait(false);
    }
}
