using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Repository interface for LoyaltyMember data access operations.
/// </summary>
public interface ILoyaltyMemberRepository : IRepository<LoyaltyMember>
{
    /// <summary>
    /// Gets a loyalty member by their phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number (254XXXXXXXXX format).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loyalty member if found; otherwise, null.</returns>
    Task<LoyaltyMember?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loyalty member by their membership number.
    /// </summary>
    /// <param name="membershipNumber">The membership number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loyalty member if found; otherwise, null.</returns>
    Task<LoyaltyMember?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a phone number is already registered.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the phone number exists; otherwise, false.</returns>
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for loyalty members by name or phone number.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching members.</returns>
    Task<IEnumerable<LoyaltyMember>> SearchAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next sequential number for membership number generation.
    /// </summary>
    /// <param name="datePrefix">The date prefix (YYYYMMDD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next sequential number.</returns>
    Task<int> GetNextSequenceNumberAsync(string datePrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets loyalty members by tier.
    /// </summary>
    /// <param name="tier">The membership tier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of members in the specified tier.</returns>
    Task<IEnumerable<LoyaltyMember>> GetByTierAsync(Enums.MembershipTier tier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active loyalty members with their last visit after a specified date.
    /// </summary>
    /// <param name="since">The date to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active members.</returns>
    Task<IEnumerable<LoyaltyMember>> GetActivesSinceAsync(DateTime since, CancellationToken cancellationToken = default);
}
