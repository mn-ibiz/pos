using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing product offers and promotions.
/// </summary>
public interface IOfferService
{
    /// <summary>
    /// Gets all offers, optionally filtered by status.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <returns>Collection of offers.</returns>
    Task<IEnumerable<ProductOffer>> GetAllOffersAsync(OfferStatus? status = null);

    /// <summary>
    /// Gets an offer by ID.
    /// </summary>
    /// <param name="id">The offer ID.</param>
    /// <returns>The offer if found, null otherwise.</returns>
    Task<ProductOffer?> GetOfferByIdAsync(int id);

    /// <summary>
    /// Gets all active offers for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>Collection of active offers.</returns>
    Task<IEnumerable<ProductOffer>> GetActiveOffersForProductAsync(int productId);

    /// <summary>
    /// Gets the best active offer for a product (highest discount).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity being purchased.</param>
    /// <returns>The best offer if available, null otherwise.</returns>
    Task<ProductOffer?> GetBestOfferForProductAsync(int productId, int quantity = 1);

    /// <summary>
    /// Gets all offers for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>Collection of offers.</returns>
    Task<IEnumerable<ProductOffer>> GetOffersForProductAsync(int productId);

    /// <summary>
    /// Creates a new offer.
    /// </summary>
    /// <param name="offer">The offer to create.</param>
    /// <returns>The created offer.</returns>
    Task<ProductOffer> CreateOfferAsync(ProductOffer offer);

    /// <summary>
    /// Updates an existing offer.
    /// </summary>
    /// <param name="offer">The offer to update.</param>
    /// <returns>The updated offer.</returns>
    Task<ProductOffer> UpdateOfferAsync(ProductOffer offer);

    /// <summary>
    /// Deactivates an offer.
    /// </summary>
    /// <param name="id">The offer ID.</param>
    Task DeactivateOfferAsync(int id);

    /// <summary>
    /// Deletes an offer.
    /// </summary>
    /// <param name="id">The offer ID.</param>
    Task DeleteOfferAsync(int id);

    /// <summary>
    /// Validates an offer for overlapping date ranges.
    /// </summary>
    /// <param name="offer">The offer to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    Task<OfferValidationResult> ValidateOfferAsync(ProductOffer offer);

    /// <summary>
    /// Gets upcoming offers that start within the specified days.
    /// </summary>
    /// <param name="days">Number of days to look ahead.</param>
    /// <returns>Collection of upcoming offers.</returns>
    Task<IEnumerable<ProductOffer>> GetUpcomingOffersAsync(int days = 7);

    /// <summary>
    /// Gets recently expired offers.
    /// </summary>
    /// <param name="days">Number of days to look back.</param>
    /// <returns>Collection of expired offers.</returns>
    Task<IEnumerable<ProductOffer>> GetExpiredOffersAsync(int days = 30);

    /// <summary>
    /// Extends an offer's end date.
    /// </summary>
    /// <param name="id">The offer ID.</param>
    /// <param name="newEndDate">The new end date.</param>
    Task ExtendOfferAsync(int id, DateTime newEndDate);

    /// <summary>
    /// Gets all currently active offers.
    /// </summary>
    /// <returns>Collection of active offers.</returns>
    Task<IEnumerable<ProductOffer>> GetActiveOffersAsync();

    /// <summary>
    /// Gets offer performance metrics for reporting.
    /// </summary>
    /// <param name="startDate">Report start date.</param>
    /// <param name="endDate">Report end date.</param>
    /// <returns>Collection of offer performance data.</returns>
    Task<IEnumerable<OfferPerformanceData>> GetOfferPerformanceAsync(DateTime startDate, DateTime endDate);
}

/// <summary>
/// Data transfer object for offer performance metrics.
/// </summary>
public class OfferPerformanceData
{
    public int OfferId { get; set; }
    public string OfferName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal OriginalPrice { get; set; }
    public decimal OfferPrice { get; set; }
    public int RedemptionCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Result of offer validation.
/// </summary>
public class OfferValidationResult
{
    /// <summary>
    /// Whether the offer is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings (offer is still valid).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OfferValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an error.
    /// </summary>
    public static OfferValidationResult Failure(string error) => new()
    {
        IsValid = false,
        Errors = new List<string> { error }
    };
}
