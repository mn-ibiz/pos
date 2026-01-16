// src/HospitalityPOS.Core/Interfaces/IMobileReportingService.cs
// Service interface for mobile reporting app functionality
// Story 41-1: Mobile Reporting App

using HospitalityPOS.Core.Models.Mobile;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for mobile reporting app.
/// Handles authentication, dashboard, reports, alerts, and push notifications.
/// </summary>
public interface IMobileReportingService
{
    #region Authentication

    /// <summary>
    /// Authenticates a mobile user.
    /// </summary>
    /// <param name="request">Login request.</param>
    /// <returns>Login response with tokens.</returns>
    Task<MobileLoginResponse> LoginAsync(MobileLoginRequest request);

    /// <summary>
    /// Refreshes access token.
    /// </summary>
    /// <param name="request">Refresh request.</param>
    /// <returns>New tokens.</returns>
    Task<TokenRefreshResponse> RefreshTokenAsync(TokenRefreshRequest request);

    /// <summary>
    /// Logs out and invalidates session.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="deviceId">Device ID.</param>
    /// <returns>True if logged out.</returns>
    Task<bool> LogoutAsync(int userId, string deviceId);

    /// <summary>
    /// Logs out from all devices.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Number of sessions revoked.</returns>
    Task<int> LogoutAllDevicesAsync(int userId);

    /// <summary>
    /// Gets active sessions for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>List of active sessions.</returns>
    Task<IReadOnlyList<MobileSession>> GetActiveSessionsAsync(int userId);

    /// <summary>
    /// Validates an access token.
    /// </summary>
    /// <param name="accessToken">Access token to validate.</param>
    /// <returns>User info if valid, null otherwise.</returns>
    Task<MobileUserInfo?> ValidateTokenAsync(string accessToken);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets mobile dashboard data.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="branchId">Optional branch ID (null for default branch).</param>
    /// <returns>Dashboard data.</returns>
    Task<MobileDashboard> GetDashboardAsync(int userId, int? branchId = null);

    /// <summary>
    /// Gets today's sales summary.
    /// </summary>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Daily sales summary.</returns>
    Task<DailySalesSummary> GetTodaySalesAsync(int? branchId = null);

    /// <summary>
    /// Gets sales comparison with previous period.
    /// </summary>
    /// <param name="branchId">Optional branch ID.</param>
    /// <param name="comparisonPeriod">Period to compare (Yesterday, LastWeek, LastMonth).</param>
    /// <returns>Sales comparison.</returns>
    Task<SalesComparison> GetSalesComparisonAsync(int? branchId = null, string comparisonPeriod = "Yesterday");

    /// <summary>
    /// Gets quick stats for dashboard.
    /// </summary>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Quick stats.</returns>
    Task<QuickStats> GetQuickStatsAsync(int? branchId = null);

    #endregion

    #region Sales Reports

    /// <summary>
    /// Gets sales report for mobile.
    /// </summary>
    /// <param name="request">Report request.</param>
    /// <returns>Sales report.</returns>
    Task<MobileSalesReport> GetSalesReportAsync(MobileSalesReportRequest request);

    /// <summary>
    /// Gets sales by category.
    /// </summary>
    /// <param name="dateFrom">Start date.</param>
    /// <param name="dateTo">End date.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Sales by category.</returns>
    Task<IReadOnlyList<CategorySalesItem>> GetSalesByCategoryAsync(
        DateOnly dateFrom, DateOnly dateTo, int? branchId = null);

    /// <summary>
    /// Gets sales by payment method.
    /// </summary>
    /// <param name="dateFrom">Start date.</param>
    /// <param name="dateTo">End date.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Sales by payment method.</returns>
    Task<IReadOnlyList<PaymentMethodSalesItem>> GetSalesByPaymentMethodAsync(
        DateOnly dateFrom, DateOnly dateTo, int? branchId = null);

    /// <summary>
    /// Gets top selling products.
    /// </summary>
    /// <param name="dateFrom">Start date.</param>
    /// <param name="dateTo">End date.</param>
    /// <param name="count">Number of products to return.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Top products.</returns>
    Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(
        DateOnly dateFrom, DateOnly dateTo, int count = 10, int? branchId = null);

    /// <summary>
    /// Gets hourly sales breakdown.
    /// </summary>
    /// <param name="date">Date to analyze.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Hourly breakdown.</returns>
    Task<IReadOnlyList<HourlySalesItem>> GetHourlySalesAsync(DateOnly date, int? branchId = null);

    #endregion

    #region Alerts

    /// <summary>
    /// Gets all alerts for mobile.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Alerts response.</returns>
    Task<MobileAlertsResponse> GetAlertsAsync(int userId, int? branchId = null);

    /// <summary>
    /// Gets low stock items.
    /// </summary>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Low stock items.</returns>
    Task<IReadOnlyList<StockAlertItem>> GetLowStockItemsAsync(int? branchId = null);

    /// <summary>
    /// Gets expiring items.
    /// </summary>
    /// <param name="daysAhead">Days ahead to check.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Expiring items.</returns>
    Task<IReadOnlyList<ExpiryAlertItem>> GetExpiringItemsAsync(int daysAhead = 7, int? branchId = null);

    /// <summary>
    /// Marks an alert as read.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <returns>True if marked.</returns>
    Task<bool> MarkAlertAsReadAsync(int alertId);

    /// <summary>
    /// Marks all alerts as read for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Number of alerts marked.</returns>
    Task<int> MarkAllAlertsAsReadAsync(int userId);

    /// <summary>
    /// Dismisses an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <returns>True if dismissed.</returns>
    Task<bool> DismissAlertAsync(int alertId);

    #endregion

    #region Branches

    /// <summary>
    /// Gets branches accessible by user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Branch summaries.</returns>
    Task<IReadOnlyList<MobileBranchSummary>> GetBranchesAsync(int userId);

    /// <summary>
    /// Gets aggregate summary across all accessible branches.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>All branches summary.</returns>
    Task<AllBranchesSummary> GetAllBranchesSummaryAsync(int userId);

    /// <summary>
    /// Gets detailed branch summary.
    /// </summary>
    /// <param name="branchId">Branch ID.</param>
    /// <returns>Branch summary.</returns>
    Task<MobileBranchSummary?> GetBranchSummaryAsync(int branchId);

    #endregion

    #region Device Registration

    /// <summary>
    /// Registers a device for push notifications.
    /// </summary>
    /// <param name="registration">Device registration.</param>
    /// <returns>Registration ID.</returns>
    Task<int> RegisterDeviceAsync(DeviceRegistration registration);

    /// <summary>
    /// Updates push token for a device.
    /// </summary>
    /// <param name="deviceId">Device ID.</param>
    /// <param name="pushToken">New push token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> UpdatePushTokenAsync(string deviceId, string pushToken);

    /// <summary>
    /// Unregisters a device.
    /// </summary>
    /// <param name="deviceId">Device ID.</param>
    /// <returns>True if unregistered.</returns>
    Task<bool> UnregisterDeviceAsync(string deviceId);

    /// <summary>
    /// Gets registered devices for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Registered devices.</returns>
    Task<IReadOnlyList<DeviceRegistration>> GetRegisteredDevicesAsync(int userId);

    #endregion

    #region Notification Preferences

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Notification preferences.</returns>
    Task<NotificationPreferences> GetNotificationPreferencesAsync(int userId);

    /// <summary>
    /// Updates notification preferences.
    /// </summary>
    /// <param name="preferences">New preferences.</param>
    /// <returns>Updated preferences.</returns>
    Task<NotificationPreferences> UpdateNotificationPreferencesAsync(NotificationPreferences preferences);

    #endregion

    #region Cached Data

    /// <summary>
    /// Gets cached data for offline support.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="branchId">Optional branch ID.</param>
    /// <returns>Cached data.</returns>
    Task<MobileCachedData> GetCachedDataAsync(int userId, int? branchId = null);

    /// <summary>
    /// Gets sync status.
    /// </summary>
    /// <param name="deviceId">Device ID.</param>
    /// <returns>Sync status.</returns>
    Task<MobileSyncStatus> GetSyncStatusAsync(string deviceId);

    #endregion

    #region Events

    /// <summary>Raised when a session is created.</summary>
    event EventHandler<MobileSessionEventArgs>? SessionCreated;

    /// <summary>Raised when a session is revoked.</summary>
    event EventHandler<MobileSessionEventArgs>? SessionRevoked;

    #endregion
}
