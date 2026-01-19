using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for login audit operations.
/// </summary>
public interface ILoginAuditService
{
    /// <summary>
    /// Records a login attempt.
    /// </summary>
    /// <param name="userId">The user ID (null if login failed with invalid username).</param>
    /// <param name="username">The username attempted.</param>
    /// <param name="success">Whether the login was successful.</param>
    /// <param name="failureReason">The failure reason (if applicable).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RecordLoginAttemptAsync(
        int? userId,
        string username,
        bool success,
        string? failureReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a logout event.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The username.</param>
    /// <param name="loginTime">The original login time to calculate session duration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RecordLogoutAsync(
        int userId,
        string username,
        DateTime? loginTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the login history for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="startDate">The start date filter.</param>
    /// <param name="endDate">The end date filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of login audit records.</returns>
    Task<IReadOnlyList<LoginAudit>> GetUserLoginHistoryAsync(
        int userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all login history with optional filters.
    /// </summary>
    /// <param name="startDate">The start date filter.</param>
    /// <param name="endDate">The end date filter.</param>
    /// <param name="successOnly">Filter by success status (null for all).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of login audit records.</returns>
    Task<IReadOnlyList<LoginAudit>> GetLoginHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? successOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts for suspicious activity detection.
    /// </summary>
    /// <param name="minutes">Time window in minutes to check.</param>
    /// <param name="threshold">Minimum number of failures to flag as suspicious.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary of usernames and their failed attempt counts.</returns>
    Task<IDictionary<string, int>> GetSuspiciousActivityAsync(
        int minutes = 30,
        int threshold = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of failed attempts for a username in the specified time window.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="minutes">Time window in minutes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of failed attempts.</returns>
    Task<int> GetRecentFailedAttemptsAsync(
        string username,
        int minutes = 30,
        CancellationToken cancellationToken = default);
}
