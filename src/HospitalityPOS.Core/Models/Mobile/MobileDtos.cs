// src/HospitalityPOS.Core/Models/Mobile/MobileDtos.cs
// DTOs for mobile reporting app functionality
// Story 41-1: Mobile Reporting App

namespace HospitalityPOS.Core.Models.Mobile;

#region Enums

/// <summary>
/// Device platform types.
/// </summary>
public enum DevicePlatform
{
    iOS,
    Android,
    Web
}

/// <summary>
/// Push notification types.
/// </summary>
public enum NotificationType
{
    DailySummary,
    LowStock,
    ExpiryAlert,
    LargeTransaction,
    ZReportComplete,
    NewOrder,
    PaymentReceived,
    SystemAlert,
    Custom
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum MobileAlertSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Session status.
/// </summary>
public enum SessionStatus
{
    Active,
    Expired,
    Revoked
}

#endregion

#region Authentication Models

/// <summary>
/// Mobile login request.
/// </summary>
public class MobileLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? PushToken { get; set; }
}

/// <summary>
/// Mobile login response.
/// </summary>
public class MobileLoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; } = 3600; // seconds
    public DateTime ExpiresAt { get; set; }
    public MobileUserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mobile user information.
/// </summary>
public class MobileUserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<int> AccessibleBranchIds { get; set; } = new();
    public NotificationPreferences NotificationPrefs { get; set; } = new();
}

/// <summary>
/// Token refresh request.
/// </summary>
public class TokenRefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// Token refresh response.
/// </summary>
public class TokenRefreshResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mobile session info.
/// </summary>
public class MobileSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? PushToken { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Device registration for push notifications.
/// </summary>
public class DeviceRegistration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string PushToken { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

#endregion

#region Dashboard Models

/// <summary>
/// Mobile dashboard data.
/// </summary>
public class MobileDashboard
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime AsOf { get; set; }
    public DailySalesSummary TodaySales { get; set; } = new();
    public SalesComparison Comparison { get; set; } = new();
    public List<MobileAlert> Alerts { get; set; } = new();
    public WorkPeriodInfo? CurrentWorkPeriod { get; set; }
    public QuickStats QuickStats { get; set; } = new();
}

/// <summary>
/// Daily sales summary for mobile.
/// </summary>
public class DailySalesSummary
{
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTicket => TransactionCount > 0 ? TotalSales / TransactionCount : 0;
    public decimal CashSales { get; set; }
    public decimal MpesaSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal OtherSales { get; set; }
    public int ItemsSold { get; set; }
    public decimal DiscountsGiven { get; set; }
    public int VoidCount { get; set; }
    public decimal VoidAmount { get; set; }
}

/// <summary>
/// Sales comparison to previous period.
/// </summary>
public class SalesComparison
{
    public decimal SalesChangePercent { get; set; }
    public decimal TransactionChangePercent { get; set; }
    public decimal AvgTicketChangePercent { get; set; }
    public string ComparisonPeriod { get; set; } = "Yesterday";
    public decimal PreviousSales { get; set; }
    public int PreviousTransactions { get; set; }
}

/// <summary>
/// Quick stats for mobile dashboard.
/// </summary>
public class QuickStats
{
    public int ActiveTables { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiringItems { get; set; }
    public decimal CashInDrawer { get; set; }
}

/// <summary>
/// Work period information.
/// </summary>
public class WorkPeriodInfo
{
    public int Id { get; set; }
    public DateTime OpenedAt { get; set; }
    public string OpenedBy { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public TimeSpan Duration => IsOpen ? DateTime.Now - OpenedAt : TimeSpan.Zero;
}

#endregion

#region Sales Report Models

/// <summary>
/// Mobile sales report request.
/// </summary>
public class MobileSalesReportRequest
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public int? BranchId { get; set; }
    public bool IncludeCategoryBreakdown { get; set; } = true;
    public bool IncludePaymentBreakdown { get; set; } = true;
    public bool IncludeDailyBreakdown { get; set; } = true;
    public bool IncludeHourlyBreakdown { get; set; }
    public int? TopProductsCount { get; set; } = 10;
}

/// <summary>
/// Mobile sales report response.
/// </summary>
public class MobileSalesReport
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    // Summary
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTicket => TotalTransactions > 0 ? TotalSales / TotalTransactions : 0;
    public int TotalItemsSold { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercent { get; set; }

    // Breakdowns
    public List<CategorySalesItem> ByCategory { get; set; } = new();
    public List<PaymentMethodSalesItem> ByPaymentMethod { get; set; } = new();
    public List<DailySalesItem> DailyBreakdown { get; set; } = new();
    public List<HourlySalesItem>? HourlyBreakdown { get; set; }
    public List<TopProductItem>? TopProducts { get; set; }
}

/// <summary>
/// Sales by category.
/// </summary>
public class CategorySalesItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int Quantity { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Sales by payment method.
/// </summary>
public class PaymentMethodSalesItem
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Daily sales breakdown.
/// </summary>
public class DailySalesItem
{
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
    public decimal AverageTicket => Transactions > 0 ? Sales / Transactions : 0;
}

/// <summary>
/// Hourly sales breakdown.
/// </summary>
public class HourlySalesItem
{
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty; // "9 AM", "10 AM", etc.
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
}

/// <summary>
/// Top selling product.
/// </summary>
public class TopProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public int Rank { get; set; }
}

#endregion

#region Alert Models

/// <summary>
/// Mobile alert.
/// </summary>
public class MobileAlert
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MobileAlertSeverity Severity { get; set; }
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Stock alert for mobile.
/// </summary>
public class StockAlertItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? LastPurchasePrice { get; set; }
}

/// <summary>
/// Expiry alert for mobile.
/// </summary>
public class ExpiryAlertItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal Quantity { get; set; }
    public decimal EstimatedValue { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

/// <summary>
/// Mobile alerts response.
/// </summary>
public class MobileAlertsResponse
{
    public List<StockAlertItem> LowStock { get; set; } = new();
    public List<ExpiryAlertItem> Expiring { get; set; } = new();
    public List<MobileAlert> RecentAlerts { get; set; } = new();
    public int TotalLowStockCount { get; set; }
    public int TotalExpiringCount { get; set; }
    public int UnreadAlertCount { get; set; }
}

#endregion

#region Branch Models

/// <summary>
/// Branch summary for mobile.
/// </summary>
public class MobileBranchSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal TodaySales { get; set; }
    public int TodayTransactions { get; set; }
    public bool WorkPeriodOpen { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringItemsCount { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsOnline { get; set; } = true;
}

/// <summary>
/// Aggregate view across all branches.
/// </summary>
public class AllBranchesSummary
{
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTicket => TotalTransactions > 0 ? TotalSales / TotalTransactions : 0;
    public int TotalBranches { get; set; }
    public int BranchesOpen { get; set; }
    public List<MobileBranchSummary> Branches { get; set; } = new();
    public MobileBranchSummary? TopPerformingBranch { get; set; }
}

#endregion

#region Push Notification Models

/// <summary>
/// Push notification request.
/// </summary>
public class PushNotificationRequest
{
    public List<int>? UserIds { get; set; }
    public List<string>? DeviceTokens { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Dictionary<string, string>? Data { get; set; }
    public string? ImageUrl { get; set; }
    public string? ClickAction { get; set; }
    public int? Badge { get; set; }
    public string? Sound { get; set; } = "default";
    public int? Priority { get; set; } = 10; // High priority
}

/// <summary>
/// Push notification result.
/// </summary>
public class PushNotificationResult
{
    public bool Success { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedTokens { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// Notification preferences for user.
/// </summary>
public class NotificationPreferences
{
    public int UserId { get; set; }
    public bool DailySummaryEnabled { get; set; } = true;
    public TimeOnly DailySummaryTime { get; set; } = new(21, 0); // 9 PM default
    public bool LowStockAlertsEnabled { get; set; } = true;
    public bool ExpiryAlertsEnabled { get; set; } = true;
    public bool LargeTransactionAlertsEnabled { get; set; }
    public decimal LargeTransactionThreshold { get; set; } = 50000;
    public bool ZReportNotificationsEnabled { get; set; } = true;
    public bool QuietHoursEnabled { get; set; }
    public TimeOnly QuietHoursStart { get; set; } = new(22, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);
}

/// <summary>
/// Scheduled notification.
/// </summary>
public class ScheduledNotification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>
/// Notification log entry.
/// </summary>
public class NotificationLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public DateTime SentAt { get; set; }
}

#endregion

#region Offline Support Models

/// <summary>
/// Cached data for offline support.
/// </summary>
public class MobileCachedData
{
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public MobileDashboard? Dashboard { get; set; }
    public MobileAlertsResponse? Alerts { get; set; }
    public List<MobileBranchSummary>? Branches { get; set; }
    public bool IsStale => DateTime.UtcNow > (ExpiresAt ?? CachedAt.AddMinutes(15));
}

/// <summary>
/// Sync status for mobile app.
/// </summary>
public class MobileSyncStatus
{
    public DateTime LastSyncAt { get; set; }
    public bool IsSyncing { get; set; }
    public int PendingChanges { get; set; }
    public string? SyncError { get; set; }
    public bool IsOnline { get; set; }
}

#endregion

#region API Rate Limiting

/// <summary>
/// Rate limit info for API response.
/// </summary>
public class RateLimitInfo
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetsAt { get; set; }
}

#endregion

#region Event Args

/// <summary>
/// Push notification sent event args.
/// </summary>
public class PushNotificationSentEventArgs : EventArgs
{
    public int UserId { get; set; }
    public NotificationType Type { get; set; }
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Mobile session event args.
/// </summary>
public class MobileSessionEventArgs : EventArgs
{
    public int UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
}

#endregion
