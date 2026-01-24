namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Interface for processing expired loyalty points.
/// </summary>
public interface IExpirePointsJob
{
    /// <summary>
    /// Processes all members with expired points, setting their balance to 0.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of members processed.</returns>
    Task<int> ProcessExpiredPointsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends expiry warning SMS to members whose points are about to expire.
    /// </summary>
    /// <param name="daysBeforeExpiry">Days before expiry to send warning (default: 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of warnings sent.</returns>
    Task<int> SendExpiryWarningsAsync(int daysBeforeExpiry = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets members who have points approaching expiry.
    /// </summary>
    /// <param name="daysUntilExpiry">Days until expiry to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Members with points approaching expiry.</returns>
    Task<IEnumerable<PointsExpiryInfo>> GetMembersApproachingExpiryAsync(
        int daysUntilExpiry = 30,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a member's points approaching expiry.
/// </summary>
public class PointsExpiryInfo
{
    /// <summary>
    /// The member ID.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The member's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The member's phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The member's current points balance.
    /// </summary>
    public decimal PointsBalance { get; set; }

    /// <summary>
    /// The date of the member's last activity.
    /// </summary>
    public DateTime? LastActivity { get; set; }

    /// <summary>
    /// Days until points expire.
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Whether an expiry warning has already been sent.
    /// </summary>
    public bool WarningAlreadySent { get; set; }
}
