// src/HospitalityPOS.Infrastructure/Services/PushNotificationService.cs
// Service implementation for push notifications via Firebase Cloud Messaging
// Story 41-1: Mobile Reporting App

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Mobile;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of push notification service.
/// Handles sending notifications via Firebase Cloud Messaging (FCM).
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    // In-memory storage (replace with database in production)
    private readonly List<NotificationLog> _notificationLogs = new();
    private readonly List<ScheduledNotification> _scheduledNotifications = new();
    private readonly Dictionary<int, List<string>> _userDeviceTokens = new();
    private readonly Dictionary<int, NotificationPreferences> _preferences = new();
    private readonly Dictionary<string, List<int>> _roleUsers = new();

    private int _notificationLogIdCounter;
    private int _scheduledNotificationIdCounter;

    // FCM configuration (would come from IConfiguration in production)
    private readonly string _fcmServerKey = "YOUR_FCM_SERVER_KEY";
    private readonly string _fcmSenderId = "YOUR_FCM_SENDER_ID";

    public PushNotificationService()
    {
        // Initialize sample data
        _userDeviceTokens[1] = new List<string> { "device_token_admin_1", "device_token_admin_2" };
        _userDeviceTokens[2] = new List<string> { "device_token_manager_1" };

        _roleUsers["Administrator"] = new List<int> { 1 };
        _roleUsers["Manager"] = new List<int> { 2 };
        _roleUsers["Owner"] = new List<int> { 1 };

        // Default preferences
        _preferences[1] = new NotificationPreferences
        {
            UserId = 1,
            DailySummaryEnabled = true,
            LowStockAlertsEnabled = true,
            ExpiryAlertsEnabled = true,
            LargeTransactionAlertsEnabled = true,
            LargeTransactionThreshold = 50000,
            ZReportNotificationsEnabled = true
        };
        _preferences[2] = new NotificationPreferences
        {
            UserId = 2,
            DailySummaryEnabled = true,
            LowStockAlertsEnabled = true,
            ExpiryAlertsEnabled = true,
            ZReportNotificationsEnabled = true
        };
    }

    #region Events

    public event EventHandler<PushNotificationSentEventArgs>? NotificationSent;

    protected virtual void OnNotificationSent(PushNotificationSentEventArgs e) => NotificationSent?.Invoke(this, e);

    #endregion

    #region Send Notifications

    public async Task<PushNotificationResult> SendAsync(PushNotificationRequest request)
    {
        await Task.CompletedTask;

        var tokens = new List<string>();

        // Collect tokens from user IDs
        if (request.UserIds?.Any() == true)
        {
            foreach (var userId in request.UserIds)
            {
                if (_userDeviceTokens.TryGetValue(userId, out var userTokens))
                {
                    tokens.AddRange(userTokens);
                }
            }
        }

        // Add directly specified tokens
        if (request.DeviceTokens?.Any() == true)
        {
            tokens.AddRange(request.DeviceTokens);
        }

        if (!tokens.Any())
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = "No device tokens found"
            };
        }

        // Simulate sending to FCM
        var result = await SimulateFcmSendAsync(tokens, request);

        // Log notifications
        foreach (var userId in request.UserIds ?? new List<int>())
        {
            var log = new NotificationLog
            {
                Id = ++_notificationLogIdCounter,
                UserId = userId,
                DeviceToken = string.Join(",", _userDeviceTokens.GetValueOrDefault(userId) ?? new List<string>()),
                Type = request.Type,
                Title = request.Title,
                Body = request.Body,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                MessageId = result.MessageId,
                SentAt = DateTime.UtcNow
            };
            _notificationLogs.Add(log);

            OnNotificationSent(new PushNotificationSentEventArgs
            {
                UserId = userId,
                Type = request.Type,
                Success = result.Success,
                MessageId = result.MessageId,
                SentAt = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<PushNotificationResult> SendToUserAsync(
        int userId, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null)
    {
        // Check user preferences
        if (!ShouldSendNotification(userId, type))
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = "User has disabled this notification type"
            };
        }

        return await SendAsync(new PushNotificationRequest
        {
            UserIds = new List<int> { userId },
            Title = title,
            Body = body,
            Type = type,
            Data = data
        });
    }

    public async Task<PushNotificationResult> SendToUsersAsync(
        IEnumerable<int> userIds, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null)
    {
        // Filter users based on preferences
        var eligibleUsers = userIds.Where(id => ShouldSendNotification(id, type)).ToList();

        if (!eligibleUsers.Any())
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = "No eligible users to notify"
            };
        }

        return await SendAsync(new PushNotificationRequest
        {
            UserIds = eligibleUsers,
            Title = title,
            Body = body,
            Type = type,
            Data = data
        });
    }

    public async Task<PushNotificationResult> SendToRoleAsync(
        string roleName, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null)
    {
        if (!_roleUsers.TryGetValue(roleName, out var userIds) || !userIds.Any())
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = $"No users found with role: {roleName}"
            };
        }

        return await SendToUsersAsync(userIds, title, body, type, data);
    }

    public async Task<PushNotificationResult> SendToBranchAsync(
        int branchId, string title, string body,
        NotificationType type = NotificationType.Custom,
        Dictionary<string, string>? data = null)
    {
        // In production, would query database for users associated with branch
        // For now, send to all managers
        return await SendToRoleAsync("Manager", title, body, type, data);
    }

    private bool ShouldSendNotification(int userId, NotificationType type)
    {
        if (!_preferences.TryGetValue(userId, out var prefs))
        {
            return true; // Default to allow if no preferences set
        }

        // Check quiet hours
        if (prefs.QuietHoursEnabled)
        {
            var currentTime = TimeOnly.FromDateTime(DateTime.Now);
            if (prefs.QuietHoursStart < prefs.QuietHoursEnd)
            {
                if (currentTime >= prefs.QuietHoursStart && currentTime <= prefs.QuietHoursEnd)
                    return false;
            }
            else
            {
                if (currentTime >= prefs.QuietHoursStart || currentTime <= prefs.QuietHoursEnd)
                    return false;
            }
        }

        return type switch
        {
            NotificationType.DailySummary => prefs.DailySummaryEnabled,
            NotificationType.LowStock => prefs.LowStockAlertsEnabled,
            NotificationType.ExpiryAlert => prefs.ExpiryAlertsEnabled,
            NotificationType.LargeTransaction => prefs.LargeTransactionAlertsEnabled,
            NotificationType.ZReportComplete => prefs.ZReportNotificationsEnabled,
            _ => true
        };
    }

    private async Task<PushNotificationResult> SimulateFcmSendAsync(
        List<string> tokens, PushNotificationRequest request)
    {
        await Task.Delay(50); // Simulate network call

        // Simulate successful send
        return new PushNotificationResult
        {
            Success = true,
            SentCount = tokens.Count,
            FailedCount = 0,
            MessageId = Guid.NewGuid().ToString("N")
        };
    }

    #endregion

    #region Scheduled Notifications

    public async Task<int> ScheduleNotificationAsync(
        int userId, string title, string body, DateTime scheduledFor,
        NotificationType type = NotificationType.Custom)
    {
        await Task.CompletedTask;

        var notification = new ScheduledNotification
        {
            Id = ++_scheduledNotificationIdCounter,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            ScheduledFor = scheduledFor,
            IsSent = false,
            IsCancelled = false
        };

        _scheduledNotifications.Add(notification);
        return notification.Id;
    }

    public async Task<bool> CancelScheduledNotificationAsync(int notificationId)
    {
        await Task.CompletedTask;

        var notification = _scheduledNotifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification == null || notification.IsSent) return false;

        notification.IsCancelled = true;
        return true;
    }

    public async Task<IReadOnlyList<ScheduledNotification>> GetScheduledNotificationsAsync(int userId)
    {
        await Task.CompletedTask;

        return _scheduledNotifications
            .Where(n => n.UserId == userId && !n.IsSent && !n.IsCancelled)
            .OrderBy(n => n.ScheduledFor)
            .ToList();
    }

    public async Task<int> ProcessDueNotificationsAsync()
    {
        var now = DateTime.UtcNow;
        var dueNotifications = _scheduledNotifications
            .Where(n => !n.IsSent && !n.IsCancelled && n.ScheduledFor <= now)
            .ToList();

        int sentCount = 0;
        foreach (var notification in dueNotifications)
        {
            var result = await SendToUserAsync(
                notification.UserId,
                notification.Title,
                notification.Body,
                notification.Type);

            if (result.Success)
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                sentCount++;
            }
        }

        return sentCount;
    }

    #endregion

    #region Alert Notifications

    public async Task<PushNotificationResult> SendDailySummaryAsync(int? branchId = null)
    {
        // Get all users who have daily summary enabled
        var eligibleUsers = _preferences
            .Where(p => p.Value.DailySummaryEnabled)
            .Select(p => p.Key)
            .ToList();

        if (!eligibleUsers.Any())
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = "No users have daily summary enabled"
            };
        }

        // Generate sample summary
        var random = new Random(DateTime.Today.DayOfYear);
        var totalSales = random.Next(80000, 150000);
        var transactions = random.Next(100, 200);

        var branchName = branchId.HasValue ? $"Branch {branchId}" : "All Stores";
        var title = "Daily Sales Summary";
        var body = $"{branchName}: KSh {totalSales:N0} from {transactions} transactions";

        return await SendToUsersAsync(
            eligibleUsers,
            title,
            body,
            NotificationType.DailySummary,
            new Dictionary<string, string>
            {
                { "type", "daily_summary" },
                { "branchId", branchId?.ToString() ?? "all" },
                { "totalSales", totalSales.ToString() },
                { "transactions", transactions.ToString() }
            });
    }

    public async Task<PushNotificationResult> SendLowStockAlertAsync(
        int productId, string productName, decimal currentStock, decimal reorderLevel)
    {
        var title = "Low Stock Alert";
        var body = $"{productName} is below reorder level ({currentStock:N0} remaining)";

        return await SendToRoleAsync(
            "Manager",
            title,
            body,
            NotificationType.LowStock,
            new Dictionary<string, string>
            {
                { "type", "low_stock" },
                { "productId", productId.ToString() },
                { "productName", productName },
                { "currentStock", currentStock.ToString() },
                { "reorderLevel", reorderLevel.ToString() }
            });
    }

    public async Task<PushNotificationResult> SendExpiryAlertAsync(
        int productId, string productName, DateOnly expiryDate, decimal quantity)
    {
        var daysUntil = (expiryDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
        var urgency = daysUntil <= 1 ? "URGENT: " : daysUntil <= 3 ? "Warning: " : "";

        var title = $"{urgency}Expiry Alert";
        var body = $"{productName}: {quantity:N0} units expire {(daysUntil == 0 ? "today" : daysUntil == 1 ? "tomorrow" : $"in {daysUntil} days")}";

        return await SendToRoleAsync(
            "Manager",
            title,
            body,
            NotificationType.ExpiryAlert,
            new Dictionary<string, string>
            {
                { "type", "expiry_alert" },
                { "productId", productId.ToString() },
                { "productName", productName },
                { "expiryDate", expiryDate.ToString("yyyy-MM-dd") },
                { "quantity", quantity.ToString() },
                { "daysUntilExpiry", daysUntil.ToString() }
            });
    }

    public async Task<PushNotificationResult> SendLargeTransactionAlertAsync(
        int receiptId, decimal amount, int branchId)
    {
        // Check which users have this alert enabled with threshold
        var eligibleUsers = _preferences
            .Where(p => p.Value.LargeTransactionAlertsEnabled && amount >= p.Value.LargeTransactionThreshold)
            .Select(p => p.Key)
            .ToList();

        if (!eligibleUsers.Any())
        {
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = "No users have large transaction alerts enabled for this amount"
            };
        }

        var title = "Large Transaction";
        var body = $"Receipt #{receiptId}: KSh {amount:N0} at Branch {branchId}";

        return await SendToUsersAsync(
            eligibleUsers,
            title,
            body,
            NotificationType.LargeTransaction,
            new Dictionary<string, string>
            {
                { "type", "large_transaction" },
                { "receiptId", receiptId.ToString() },
                { "amount", amount.ToString() },
                { "branchId", branchId.ToString() }
            });
    }

    public async Task<PushNotificationResult> SendZReportCompleteAsync(
        int workPeriodId, decimal totalSales, int branchId)
    {
        var title = "Z-Report Complete";
        var body = $"Work period #{workPeriodId} closed. Total sales: KSh {totalSales:N0}";

        return await SendToRoleAsync(
            "Owner",
            title,
            body,
            NotificationType.ZReportComplete,
            new Dictionary<string, string>
            {
                { "type", "z_report_complete" },
                { "workPeriodId", workPeriodId.ToString() },
                { "totalSales", totalSales.ToString() },
                { "branchId", branchId.ToString() }
            });
    }

    #endregion

    #region Notification History

    public async Task<IReadOnlyList<NotificationLog>> GetNotificationHistoryAsync(int userId, int limit = 50)
    {
        await Task.CompletedTask;

        return _notificationLogs
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .Take(limit)
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetNotificationStatsAsync(DateOnly dateFrom, DateOnly dateTo)
    {
        await Task.CompletedTask;

        var logs = _notificationLogs
            .Where(n => DateOnly.FromDateTime(n.SentAt) >= dateFrom && DateOnly.FromDateTime(n.SentAt) <= dateTo)
            .ToList();

        return new Dictionary<string, int>
        {
            { "TotalSent", logs.Count },
            { "Successful", logs.Count(n => n.Success) },
            { "Failed", logs.Count(n => !n.Success) },
            { "DailySummary", logs.Count(n => n.Type == NotificationType.DailySummary) },
            { "LowStock", logs.Count(n => n.Type == NotificationType.LowStock) },
            { "ExpiryAlert", logs.Count(n => n.Type == NotificationType.ExpiryAlert) },
            { "LargeTransaction", logs.Count(n => n.Type == NotificationType.LargeTransaction) },
            { "ZReportComplete", logs.Count(n => n.Type == NotificationType.ZReportComplete) }
        };
    }

    #endregion

    #region Token Management

    public async Task<bool> ValidateTokenAsync(string token)
    {
        await Task.CompletedTask;

        // In production, would call FCM to validate token
        // For now, just check if it's not empty
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<int> RemoveInvalidTokensAsync(IEnumerable<string> tokens)
    {
        await Task.CompletedTask;

        int removedCount = 0;
        var tokenSet = tokens.ToHashSet();

        foreach (var userTokens in _userDeviceTokens.Values)
        {
            removedCount += userTokens.RemoveAll(t => tokenSet.Contains(t));
        }

        return removedCount;
    }

    #endregion

    #region Helper Methods for Testing

    /// <summary>
    /// Registers a device token for a user (for testing).
    /// </summary>
    public void RegisterDeviceToken(int userId, string token)
    {
        if (!_userDeviceTokens.ContainsKey(userId))
        {
            _userDeviceTokens[userId] = new List<string>();
        }
        if (!_userDeviceTokens[userId].Contains(token))
        {
            _userDeviceTokens[userId].Add(token);
        }
    }

    /// <summary>
    /// Updates notification preferences for a user (for testing).
    /// </summary>
    public void SetNotificationPreferences(int userId, NotificationPreferences prefs)
    {
        _preferences[userId] = prefs;
    }

    /// <summary>
    /// Adds users to a role (for testing).
    /// </summary>
    public void AddUsersToRole(string role, params int[] userIds)
    {
        if (!_roleUsers.ContainsKey(role))
        {
            _roleUsers[role] = new List<int>();
        }
        foreach (var userId in userIds)
        {
            if (!_roleUsers[role].Contains(userId))
            {
                _roleUsers[role].Add(userId);
            }
        }
    }

    #endregion
}
